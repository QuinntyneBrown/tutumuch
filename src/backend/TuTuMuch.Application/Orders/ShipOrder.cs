using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Common;
using TuTuMuch.Application.Listings;
using TuTuMuch.Application.Notifications;
using TuTuMuch.Domain.Notifications;
using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Application.Orders;

/// <summary>Errors of the order endpoints.</summary>
public static class OrderErrors
{
    public static readonly Error InvalidTransition = new(409, "ORDER_INVALID_TRANSITION", "The order can’t make that change in its current status");

    public static Error NotFound => Error.NotFound("ORDER_NOT_FOUND");
}

/// <summary>The seller's view of one of their orders; 404 for anyone else (L2-010).</summary>
public sealed record GetSaleQuery(Guid SellerId, Guid OrderId) : IRequest<Result<SaleDto>>;

public sealed class GetSaleQueryHandler(ITuTuMuchDbContext db, IOptions<PhotoOptions> photoOptions, TimeProvider clock) :
    IRequestHandler<GetSaleQuery, Result<SaleDto>>
{
    public async Task<Result<SaleDto>> Handle(GetSaleQuery query, CancellationToken cancellationToken)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.StatusChanges)
            .SingleOrDefaultAsync(o => o.Id == query.OrderId && o.SellerId == query.SellerId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.NotFound;
        }

        var sale = await OrderViews.SaleAsync(db, order, photoOptions.Value, cancellationToken);
        return ShippingAddressVisibility.IsVisibleToSeller(order, clock.GetUtcNow().UtcDateTime) ? sale : sale with { ShipTo = null };
    }
}

/// <summary>The seller adds a carrier and tracking number to a Paid order, which becomes Shipped (L2-033 AC1, AC5).</summary>
public sealed record AddTrackingCommand(Guid SellerId, Guid OrderId, string? Carrier, string? TrackingNumber) : IRequest<Result<SaleDto>>;

public sealed class AddTrackingCommandHandler(ITuTuMuchDbContext db, IEmailOutbox notifications, TimeProvider clock, IOptions<PhotoOptions> photoOptions) :
    IRequestHandler<AddTrackingCommand, Result<SaleDto>>
{
    public async Task<Result<SaleDto>> Handle(AddTrackingCommand command, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(o => o.StatusChanges)
            .SingleOrDefaultAsync(o => o.Id == command.OrderId && o.SellerId == command.SellerId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.NotFound;
        }

        if (TrackingNumbers.Parse(command.Carrier) is not { } carrier)
        {
            return Invalid("carrier", "CARRIER_REQUIRED", "Choose the carrier.");
        }

        if (string.IsNullOrWhiteSpace(command.TrackingNumber))
        {
            return Invalid("trackingNumber", "TRACKING_REQUIRED", "Enter the tracking number.");
        }

        if (TrackingNumbers.Normalise(carrier, command.TrackingNumber) is not { } number)
        {
            return Invalid("trackingNumber", "TRACKING_MALFORMED", TrackingNumbers.InvalidMessage(carrier));
        }

        try
        {
            order.MarkShipped(carrier, number, clock.GetUtcNow().UtcDateTime);
            notifications.Enqueue(
                NotificationType.OrderShipped,
                order.BuyerId,
                new OrderShippedEmail(order.Id, order.Reference, await OrderEmails.ItemTitleAsync(db, order, cancellationToken), carrier.ToString(), number, TrackingNumbers.Url(carrier, number)));
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidOrderTransitionException or DbUpdateConcurrencyException)
        {
            // Not Paid, or the deadline job cancelled it between the read and the write.
            return OrderErrors.InvalidTransition;
        }

        return await OrderViews.SaleAsync(db, order, photoOptions.Value, cancellationToken);
    }

    private static Error Invalid(string field, string code, string message) =>
        Error.Invalid("TRACKING_INVALID", new Dictionary<string, FieldError> { [field] = new(code, message) });
}

/// <summary>What a carrier webhook delivery did.</summary>
public enum CarrierWebhookOutcome
{
    Delivered,
    Ignored,
}

/// <summary>
/// A raw carrier tracking webhook. A delivery event for a Shipped order marks it Delivered with the
/// carrier as actor (L2-033 AC4); repeats and events for any other order change nothing.
/// </summary>
public sealed record HandleCarrierWebhookCommand(string Body, string? Signature) : IRequest<Result<CarrierWebhookOutcome>>;

public sealed class CarrierWebhookHandler(ITuTuMuchDbContext db, ICarrierTracking carriers, IEmailOutbox notifications) :
    IRequestHandler<HandleCarrierWebhookCommand, Result<CarrierWebhookOutcome>>
{
    public static readonly Error InvalidSignature = new(400, "WEBHOOK_SIGNATURE_INVALID", "The webhook signature is not valid");

    public async Task<Result<CarrierWebhookOutcome>> Handle(HandleCarrierWebhookCommand command, CancellationToken cancellationToken)
    {
        if (carriers.ReadWebhook(command.Body, command.Signature) is not { } carrierEvent)
        {
            return InvalidSignature;
        }

        if (!carrierEvent.Delivered || TrackingNumbers.Normalise(carrierEvent.Carrier, carrierEvent.TrackingNumber) is not { } number)
        {
            return CarrierWebhookOutcome.Ignored;
        }

        var order = await db.Orders.Include(o => o.StatusChanges)
            .SingleOrDefaultAsync(o => o.Carrier == carrierEvent.Carrier && o.TrackingNumber == number && o.Status == OrderStatus.Shipped, cancellationToken);
        if (order is null)
        {
            return CarrierWebhookOutcome.Ignored;
        }

        try
        {
            order.MarkDelivered(OrderActor.Carrier, carrierEvent.OccurredAt);
            notifications.Enqueue(
                NotificationType.OrderDelivered,
                order.BuyerId,
                new OrderDeliveredEmail(order.Id, order.Reference, await OrderEmails.ItemTitleAsync(db, order, cancellationToken)));
            await db.SaveChangesAsync(cancellationToken);
            return CarrierWebhookOutcome.Delivered;
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent copy of the event, or the buyer's confirmation, got there first.
            return CarrierWebhookOutcome.Ignored;
        }
    }
}

/// <summary>
/// The ship deadline job's pass: every Paid order whose ship-by date has passed is cancelled by
/// the system and its payment refunded in full (L2-033 AC2), through the same path as a seller
/// cancellation. Returns how many were cancelled.
/// </summary>
public sealed record CancelUnshippedOrdersCommand : IRequest<int>;

public sealed class CancelUnshippedOrdersHandler(ITuTuMuchDbContext db, OrderCancellation cancellation, TimeProvider clock) :
    IRequestHandler<CancelUnshippedOrdersCommand, int>
{
    public async Task<int> Handle(CancelUnshippedOrdersCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var ids = await db.Orders.Where(o => o.Status == OrderStatus.Paid && o.ShipBy <= now).Select(o => o.Id).ToListAsync(cancellationToken);
        var cancelled = 0;
        foreach (var id in ids)
        {
            var order = await db.Orders.Include(o => o.StatusChanges).SingleAsync(o => o.Id == id, cancellationToken);
            Refund refund;
            Payment payment;
            try
            {
                (refund, payment) = await cancellation.CancelAsync(order, CancellationReason.ShipDeadlinePassed, null, OrderActor.System, cancellationToken);
            }
            catch (Exception exception) when (exception is InvalidOrderTransitionException or DbUpdateConcurrencyException)
            {
                // Shipped between the select and the update: leave it.
                db.ChangeTracker.Clear();
                continue;
            }

            cancelled++;
            await cancellation.IssueRefundAsync(order, refund, payment, cancellationToken);
        }

        return cancelled;
    }
}
