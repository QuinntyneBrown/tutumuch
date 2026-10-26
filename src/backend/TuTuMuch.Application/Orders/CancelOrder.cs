using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Checkouts;
using TuTuMuch.Application.Common;
using TuTuMuch.Application.Listings;
using TuTuMuch.Application.Notifications;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Notifications;
using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Application.Orders;

/// <summary>Errors of the cancellation endpoints (L2-035).</summary>
public static class CancellationErrors
{
    public static readonly Error AlreadyRequested = new(409, "CANCELLATION_ALREADY_REQUESTED", "You already asked to cancel this order");

    public static readonly Error NoPendingRequest = new(409, "CANCELLATION_NO_PENDING_REQUEST", "There is no request to cancel this order");

    public static Error Invalid(string field, string code, string message) =>
        Error.Invalid("CANCELLATION_INVALID", new Dictionary<string, FieldError> { [field] = new(code, message) });

    /// <summary>The trimmed text, null when blank; false when it is longer than <see cref="Order.MaxCancellationText"/>.</summary>
    public static bool TryClean(string? text, out string? cleaned)
    {
        cleaned = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        return cleaned is null || cleaned.Length <= Order.MaxCancellationText;
    }
}

/// <summary>
/// The one cancellation path (L2-035, L2-033 AC2), shared by the seller, the seller accepting a
/// buyer's request and the ship deadline job. In one save it cancels the order, removes its listing
/// from the catalog until relisted, records the full refund and queues the emails; the refund is
/// then issued through the payment provider with the refund ID as idempotency key.
/// </summary>
public sealed class OrderCancellation(ITuTuMuchDbContext db, IPaymentProvider provider, IEmailOutbox notifications, TimeProvider clock)
{
    /// <summary>
    /// Cancels and saves. Throws <see cref="InvalidOrderTransitionException"/> when the order is not
    /// Paid, or <see cref="DbUpdateConcurrencyException"/> when it changed since it was read.
    /// </summary>
    public async Task<(Refund Refund, Payment Payment)> CancelAsync(Order order, CancellationReason reason, string? note, OrderActor actor, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        order.Cancel(reason, note, actor, now);
        var listing = await db.Listings.SingleAsync(l => l.Id == order.ListingId, cancellationToken);
        if (listing.Status == ListingStatus.Sold)
        {
            listing.RemoveAfterCancellation(now);
        }

        var payment = await db.Payments.SingleAsync(p => p.Id == order.PaymentId, cancellationToken);
        var refund = Refund.ForCancellation(payment, reason, now);
        db.Refunds.Add(refund);
        var title = listing.Title ?? order.Reference;
        if (reason == CancellationReason.ShipDeadlinePassed)
        {
            notifications.Enqueue(NotificationType.OrderCancelled, order.BuyerId, new OrderCancelledEmail(order.Id, order.Reference, title, ToSeller: false));
            notifications.Enqueue(NotificationType.OrderCancelled, order.SellerId, new OrderCancelledEmail(order.Id, order.Reference, title, ToSeller: true));
        }
        else
        {
            notifications.Enqueue(NotificationType.OrderCancelled, order.BuyerId, new OrderCancelledEmail(order.Id, order.Reference, title, ToSeller: false, reason.ToLabel(), note));
        }

        await db.SaveChangesAsync(cancellationToken);
        return (refund, payment);
    }

    /// <summary>
    /// Refunds through the provider with the refund ID as key and emails the buyer the refund; a
    /// failure leaves it Pending for a retry.
    /// </summary>
    public async Task IssueRefundAsync(Order order, Refund refund, Payment payment, CancellationToken cancellationToken)
    {
        try
        {
            refund.Issued(await provider.RefundAsync(payment.ProviderPaymentId!, refund.Amount, refund.Id, cancellationToken));
            notifications.Enqueue(
                NotificationType.OrderRefunded,
                order.BuyerId,
                new OrderRefundedEmail(order.Id, order.Reference, refund.Amount, payment.CardBrand, payment.CardLast4));
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Stays Pending; dispatching pending refunds belongs to the outbox (see the design).
        }
    }
}

/// <summary>The seller cancels a Paid order with one of the four reasons and an optional note (L2-035 AC1, AC3).</summary>
public sealed record CancelOrderCommand(Guid SellerId, Guid OrderId, string? Reason, string? Note) : IRequest<Result<SaleDto>>;

public sealed class CancelOrderCommandHandler(ITuTuMuchDbContext db, OrderCancellation cancellation, IOptions<PhotoOptions> photoOptions) :
    IRequestHandler<CancelOrderCommand, Result<SaleDto>>
{
    public async Task<Result<SaleDto>> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(o => o.StatusChanges)
            .SingleOrDefaultAsync(o => o.Id == command.OrderId && o.SellerId == command.SellerId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.NotFound;
        }

        var reason = CancellationReasons.SellerReasons.Cast<CancellationReason?>().FirstOrDefault(r => r.ToString() == command.Reason);
        if (reason is null)
        {
            return CancellationErrors.Invalid("reason", "REASON_REQUIRED", "Choose a reason");
        }

        if (!CancellationErrors.TryClean(command.Note, out var note))
        {
            return CancellationErrors.Invalid("note", "NOTE_LENGTH", $"Keep the note to {Order.MaxCancellationText} characters");
        }

        Refund refund;
        Payment payment;
        try
        {
            (refund, payment) = await cancellation.CancelAsync(order, reason.Value, note, OrderActor.Seller, cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidOrderTransitionException or DbUpdateConcurrencyException)
        {
            // Not Paid, or shipped or cancelled by the deadline job between the read and the write.
            return OrderErrors.InvalidTransition;
        }

        await cancellation.IssueRefundAsync(order, refund, payment, cancellationToken);
        return await OrderViews.SaleAsync(db, order, photoOptions.Value, cancellationToken);
    }
}

/// <summary>The buyer asks the seller to cancel a Paid order, once; the seller is emailed (L2-035 AC2).</summary>
public sealed record RequestCancellationCommand(Guid BuyerId, Guid OrderId, string? Message) : IRequest<Result<PurchaseDto>>;

public sealed class RequestCancellationCommandHandler(ITuTuMuchDbContext db, IEmailOutbox notifications, TimeProvider clock, IOptions<PhotoOptions> photoOptions) :
    IRequestHandler<RequestCancellationCommand, Result<PurchaseDto>>
{
    public async Task<Result<PurchaseDto>> Handle(RequestCancellationCommand command, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(o => o.StatusChanges)
            .SingleOrDefaultAsync(o => o.Id == command.OrderId && o.BuyerId == command.BuyerId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.NotFound;
        }

        if (!CancellationErrors.TryClean(command.Message, out var message))
        {
            return CancellationErrors.Invalid("message", "MESSAGE_LENGTH", $"Keep the message to {Order.MaxCancellationText} characters");
        }

        if (order.Status != OrderStatus.Paid)
        {
            return OrderErrors.InvalidTransition;
        }

        if (order.CancellationRequestStatus is not null)
        {
            return CancellationErrors.AlreadyRequested;
        }

        try
        {
            order.RequestCancellation(message, clock.GetUtcNow().UtcDateTime);
            notifications.Enqueue(
                NotificationType.CancellationRequested,
                order.SellerId,
                new CancellationRequestedEmail(order.Id, order.Reference, await OrderEmails.ItemTitleAsync(db, order, cancellationToken), message));
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Shipped or cancelled between the read and the write.
            return OrderErrors.InvalidTransition;
        }

        return await PurchaseViews.PurchaseAsync(db, order, photoOptions.Value, clock, cancellationToken);
    }
}

/// <summary>
/// The seller answers the buyer's request (L2-035 AC2): Accept cancels and refunds the order with
/// reason "Buyer asked me to cancel"; Decline keeps it Paid.
/// </summary>
public sealed record RespondToCancellationCommand(Guid SellerId, Guid OrderId, string? Decision) : IRequest<Result<SaleDto>>;

public sealed class RespondToCancellationCommandHandler(ITuTuMuchDbContext db, OrderCancellation cancellation, TimeProvider clock, IOptions<PhotoOptions> photoOptions) :
    IRequestHandler<RespondToCancellationCommand, Result<SaleDto>>
{
    public async Task<Result<SaleDto>> Handle(RespondToCancellationCommand command, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(o => o.StatusChanges)
            .SingleOrDefaultAsync(o => o.Id == command.OrderId && o.SellerId == command.SellerId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.NotFound;
        }

        if (command.Decision is not ("Accept" or "Decline"))
        {
            return CancellationErrors.Invalid("decision", "DECISION_INVALID", "Accept or decline the request");
        }

        if (order.CancellationRequestStatus != CancellationRequestStatus.Pending)
        {
            return CancellationErrors.NoPendingRequest;
        }

        try
        {
            if (command.Decision == "Accept")
            {
                var (refund, payment) = await cancellation.CancelAsync(order, CancellationReason.BuyerAskedToCancel, null, OrderActor.Seller, cancellationToken);
                await cancellation.IssueRefundAsync(order, refund, payment, cancellationToken);
            }
            else
            {
                order.DeclineCancellation(clock.GetUtcNow().UtcDateTime);
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception exception) when (exception is InvalidOrderTransitionException or DbUpdateConcurrencyException)
        {
            return OrderErrors.InvalidTransition;
        }

        return await OrderViews.SaleAsync(db, order, photoOptions.Value, cancellationToken);
    }
}
