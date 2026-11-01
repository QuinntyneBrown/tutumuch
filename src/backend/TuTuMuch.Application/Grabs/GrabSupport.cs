using Microsoft.EntityFrameworkCore;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Checkouts;
using TuTuMuch.Application.Common;
using TuTuMuch.Application.Notifications;
using TuTuMuch.Domain.Finds;
using TuTuMuch.Domain.Notifications;
using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Application.Grabs;

/// <summary>The refusals of the grab endpoints.</summary>
public static class GrabRefusals
{
    public static readonly Error NotFound = Error.NotFound("GRAB_NOT_FOUND");

    public static readonly Error NotOffered = new(409, "GRAB_NOT_OFFERED", "TuTuMuch doesn't offer to grab this one");

    public static readonly Error OwnFind = new(422, "CANNOT_GRAB_OWN_FIND", "You can't ask for a grab of your own find");

    public static readonly Error InProgress = new(409, "GRAB_IN_PROGRESS", "Someone already asked TuTuMuch to grab this one");

    public static readonly Error DepositNotAcknowledged = Error.Invalid(
        "DEPOSIT_NOT_ACKNOWLEDGED",
        new Dictionary<string, FieldError> { ["depositAcknowledged"] = new("DEPOSIT_NOT_ACKNOWLEDGED", "Tick the box to confirm you understand the deposit") },
        "Tick the box to confirm you understand the deposit");

    public static readonly Error StateChanged = new(409, "GRAB_STATE_CHANGED", "This grab has moved on. Refresh to see where it is");
}

/// <summary>What grab handlers share: open-grab checks, address rules, refunds, emails and DTOs.</summary>
internal static class GrabSupport
{
    public static IQueryable<Grab> Open(this IQueryable<Grab> grabs) =>
        grabs.Where(g => g.Status == GrabStatus.PaymentPending || g.Status == GrabStatus.Requested || g.Status == GrabStatus.Accepted || g.Status == GrabStatus.Bought);

    /// <summary>The checkout rules of L2-027 for a US shipping address: the address, or the 422 to answer.</summary>
    public static Result<ShippingAddress> ReadAddress(AddressInput? input)
    {
        input ??= new AddressInput(null, null, null, null, null, null, null);
        if (ShippingAddress.IsOutsideUnitedStates(input.Country))
        {
            return Error.Invalid(
                "NON_US_ADDRESS",
                new Dictionary<string, FieldError> { ["country"] = new("NON_US_ADDRESS", "Choose a US address to continue.") },
                "TuTuMuch ships within the United States only");
        }

        var violations = ShippingAddress.Check(input.FullName, input.Line1, input.Line2, input.City, input.State, input.Zip);
        if (violations.Count > 0)
        {
            return Error.Invalid("ADDRESS_INVALID", violations.ToDictionary(v => v.Field, v => new FieldError(v.Code, v.Message)));
        }

        return ShippingAddress.Create(input.FullName!, input.Line1!, input.Line2, input.City!, input.State!, input.Zip!);
    }

    /// <summary>Adds the refund of a grab's payment to the unit of work; issue it after saving.</summary>
    public static async Task<(Refund Refund, Payment Payment)?> AddRefundAsync(ITuTuMuchDbContext db, Grab grab, decimal amount, DateTime now, CancellationToken cancellationToken)
    {
        if (amount <= 0m || grab.PaymentId is not { } paymentId)
        {
            return null;
        }

        var payment = await db.Payments.SingleAsync(p => p.Id == paymentId, cancellationToken);
        var refund = Refund.ForGrab(payment, amount, now);
        db.Refunds.Add(refund);
        return (refund, payment);
    }

    /// <summary>Refunds through the provider with the refund ID as key; a failure leaves it Pending (ADR orders/0009).</summary>
    public static async Task IssueAsync(ITuTuMuchDbContext db, IPaymentProvider provider, (Refund Refund, Payment Payment)? pending, CancellationToken cancellationToken)
    {
        if (pending is not { } refund || refund.Payment.ProviderPaymentId is null)
        {
            return;
        }

        try
        {
            refund.Refund.Issued(await provider.RefundAsync(refund.Payment.ProviderPaymentId, refund.Refund.Amount, refund.Refund.Id, cancellationToken));
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Stays Pending, as for orders.
        }
    }

    /// <summary>Queues the shopper's email for the grab's new status (L2-120).</summary>
    public static void EmailShopper(IEmailOutbox outbox, Grab grab, string findTitle) =>
        outbox.Enqueue(
            NotificationType.GrabUpdated,
            grab.ShopperId,
            new GrabUpdatedEmail(
                grab.Id,
                grab.Reference,
                findTitle,
                grab.Status.ToString(),
                grab.RefundedAmount,
                grab.Status == GrabStatus.Cancelled && grab.RefundedAmount < grab.Total ? grab.Total - grab.RefundedAmount : null,
                grab.UnavailableReason?.ToLabel(),
                grab.BuyBy));

    public static GrabAddressDto ToDto(ShippingAddress a) => new(a.FullName, a.Line1, a.Line2, a.City, a.State, a.Zip);

    public static GrabDto ToDto(Grab grab, ShelfFind find) => new(
        grab.Id,
        grab.Reference,
        grab.Status.ToString(),
        find.Reference,
        find.Title,
        find.Category,
        find.StoreName,
        find.StoreAddress,
        CheckoutMoney.Format(grab.ShelfPrice),
        CheckoutMoney.Format(grab.GrabFee),
        CheckoutMoney.Format(grab.SalesTax),
        CheckoutMoney.Format(grab.Total),
        CheckoutMoney.Format(grab.Deposit),
        CheckoutMoney.Format(grab.RefundIfCancelled),
        CheckoutMoney.Format(grab.RefundedAmount),
        grab.UnavailableReason?.ToLabel(),
        grab.PaymentDeclineReason,
        grab.AnswerBy,
        grab.BuyBy,
        grab.BoughtAt,
        grab.CreatedAt,
        ToDto(grab.Address));
}
