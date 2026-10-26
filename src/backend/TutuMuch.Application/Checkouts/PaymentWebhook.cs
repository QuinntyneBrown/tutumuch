using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TutuMuch.Application.Abstractions;
using TutuMuch.Application.Common;
using TutuMuch.Application.Listings;
using TutuMuch.Application.Notifications;
using TutuMuch.Domain.Listings;
using TutuMuch.Domain.Notifications;
using TutuMuch.Domain.Orders;
using TutuMuch.Domain.Payouts;

namespace TutuMuch.Application.Checkouts;

/// <summary>What a webhook delivery did: processed, a repeat, or ignored because it names no Tutu Much payment.</summary>
public enum WebhookOutcome
{
    Processed,
    Duplicate,
    Ignored,
}

/// <summary>
/// A raw provider webhook. The payment result comes only from here, never from the browser
/// (L2-030): a success creates the Paid order with its held payout (L2-031) and sells the listing, a decline lets the buyer
/// retry, and a success for a listing that already has an order is refunded (L2-029 AC2).
/// </summary>
public sealed record HandlePaymentWebhookCommand(string Body, string? Signature) : IRequest<Result<WebhookOutcome>>;

public sealed class PaymentWebhookHandler(
    ITutuMuchDbContext db,
    IPaymentProvider provider,
    IEmailOutbox notifications,
    IOptions<SellingOptions> selling,
    TimeProvider clock) :
    IRequestHandler<HandlePaymentWebhookCommand, Result<WebhookOutcome>>
{
    /// <summary>First order number; references start at TM-10001.</summary>
    public const long FirstOrderNumber = 10001;

    public static readonly Error InvalidSignature = new(400, "WEBHOOK_SIGNATURE_INVALID", "The webhook signature is not valid");

    public async Task<Result<WebhookOutcome>> Handle(HandlePaymentWebhookCommand command, CancellationToken cancellationToken)
    {
        if (provider.ReadWebhook(command.Body, command.Signature) is not { } paymentEvent)
        {
            return InvalidSignature;
        }

        if (await db.PaymentWebhookEvents.AnyAsync(e => e.ProviderEventId == paymentEvent.EventId, cancellationToken))
        {
            return WebhookOutcome.Duplicate;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var payment = await db.Payments.SingleOrDefaultAsync(p => p.Id == paymentEvent.PaymentId, cancellationToken);
        db.PaymentWebhookEvents.Add(PaymentWebhookEvent.Processed(paymentEvent.EventId, paymentEvent.Type.ToString(), now));
        if (payment is null || payment.Status != PaymentStatus.Pending)
        {
            return await SaveAsync(WebhookOutcome.Ignored, cancellationToken);
        }

        var checkout = await db.Checkouts.SingleAsync(c => c.Id == payment.CheckoutId, cancellationToken);
        if (paymentEvent.Type == PaymentEventType.Failed)
        {
            payment.Fail(paymentEvent.ProviderPaymentId, paymentEvent.DeclineReason ?? "Your bank declined the card.", now);
            checkout.RecordDecline(payment.DeclineReason!);
            return await SaveAsync(WebhookOutcome.Processed, cancellationToken);
        }

        payment.Succeed(paymentEvent.ProviderPaymentId, paymentEvent.CardBrand, paymentEvent.CardLast4, now);
        var listing = await db.Listings.SingleAsync(l => l.Id == checkout.ListingId, cancellationToken);
        var sold = await db.Orders.AnyAsync(o => o.ListingId == listing.Id && o.Status != OrderStatus.Cancelled, cancellationToken);
        var stillOurs = (listing.Status == ListingStatus.Reserved && listing.ReservedByCheckoutId == checkout.Id) || listing.Status == ListingStatus.Active;
        Refund? refund = null;
        if (!sold && stillOurs)
        {
            var last = await db.Orders.MaxAsync(o => (long?)o.Number, cancellationToken);
            var feeRate = PayoutFees.RateFromPercent(selling.Value.PlatformFeePercent);
            var order = Order.CreatePaid((last ?? FirstOrderNumber - 1) + 1, checkout, listing.SellerId, payment, feeRate, now);
            db.Orders.Add(order);
            db.Payouts.Add(Payout.CreateHeld(order, now));
            listing.MarkSold(now);
            checkout.MarkPaid(order.Id, now);
            notifications.Enqueue(
                NotificationType.SaleMade,
                order.SellerId,
                new SaleMadeEmail(order.Id, order.Reference, listing.Title ?? order.Reference, order.Total, order.ShipBy));
            var saved = await db.SavedAddresses.SingleOrDefaultAsync(a => a.MemberId == checkout.BuyerId, cancellationToken);
            if (saved is null)
            {
                db.SavedAddresses.Add(SavedAddress.For(checkout.BuyerId, order.ShippingAddress, now));
            }
            else
            {
                saved.Replace(order.ShippingAddress, now);
            }
        }
        else
        {
            refund = Refund.ForDuplicate(payment, now);
            db.Refunds.Add(refund);
            checkout.Release(CheckoutStatus.Expired, now);
        }

        var outcome = await SaveAsync(WebhookOutcome.Processed, cancellationToken);
        if (refund is not null && outcome.IsSuccess && outcome.Value == WebhookOutcome.Processed)
        {
            await IssueAsync(refund, payment, cancellationToken);
        }

        return outcome;
    }

    /// <summary>Commits the event with its changes; a concurrent copy of the same event loses on the event key.</summary>
    private async Task<Result<WebhookOutcome>> SaveAsync(WebhookOutcome outcome, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return outcome;
        }
        catch (DbUpdateException)
        {
            return WebhookOutcome.Duplicate;
        }
    }

    /// <summary>Refunds through the provider with the refund ID as key; a failure leaves it Pending for a retry.</summary>
    private async Task IssueAsync(Refund refund, Payment payment, CancellationToken cancellationToken)
    {
        try
        {
            refund.Issued(await provider.RefundAsync(payment.ProviderPaymentId!, refund.Amount, refund.Id, cancellationToken));
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Stays Pending; dispatching pending refunds belongs to the outbox (see the design).
        }
    }
}
