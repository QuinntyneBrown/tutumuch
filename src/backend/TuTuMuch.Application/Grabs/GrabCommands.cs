using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Checkouts;
using TuTuMuch.Application.Common;
using TuTuMuch.Application.Finds;
using TuTuMuch.Application.Notifications;
using TuTuMuch.Domain.Finds;
using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Application.Grabs;

/// <summary>Prices a grab for the shopper's US address (L2-117).</summary>
public sealed record QuoteGrabCommand(Guid ShopperId, string FindReference, AddressInput? Address) : IRequest<Result<GrabQuoteDto>>;

/// <summary>
/// Starts a grab and charges its total once per idempotency key (L2-117, L2-030 AC6). The result
/// comes only from the provider's webhook.
/// </summary>
public sealed record StartGrabCommand(
    Guid ShopperId,
    string FindReference,
    AddressInput? Address,
    bool DepositAcknowledged,
    string? PaymentMethodToken,
    decimal ExpectedTotal,
    Guid IdempotencyKey) : IRequest<Result<GrabDto>>;

public sealed record GetMyGrabsQuery(Guid ShopperId) : IRequest<IReadOnlyList<GrabSummaryDto>>;

public sealed record GetMyGrabQuery(Guid ShopperId, Guid GrabId) : IRequest<Result<GrabDto>>;

/// <summary>The shopper cancels: a full refund before acceptance, the total less the deposit after (L2-119).</summary>
public sealed record CancelGrabCommand(Guid ShopperId, Guid GrabId) : IRequest<Result<GrabDto>>;

/// <summary>The shopper says "I've got it" on a Bought grab (L2-119).</summary>
public sealed record ConfirmGrabReceivedCommand(Guid ShopperId, Guid GrabId) : IRequest<Result<GrabDto>>;

/// <summary>The shopper's side of grabs (L1-025, ADR orders/0009). The shopper comes from the session (L2-010).</summary>
public sealed class GrabCommandHandlers(
    ITuTuMuchDbContext db,
    IPaymentProvider provider,
    IEmailOutbox outbox,
    IOptions<GrabOptions> options,
    TimeProvider clock) :
    IRequestHandler<QuoteGrabCommand, Result<GrabQuoteDto>>,
    IRequestHandler<StartGrabCommand, Result<GrabDto>>,
    IRequestHandler<GetMyGrabsQuery, IReadOnlyList<GrabSummaryDto>>,
    IRequestHandler<GetMyGrabQuery, Result<GrabDto>>,
    IRequestHandler<CancelGrabCommand, Result<GrabDto>>,
    IRequestHandler<ConfirmGrabReceivedCommand, Result<GrabDto>>
{
    public async Task<Result<GrabQuoteDto>> Handle(QuoteGrabCommand command, CancellationToken cancellationToken)
    {
        var priced = await PriceAsync(command.ShopperId, command.FindReference, command.Address, cancellationToken);
        if (!priced.IsSuccess)
        {
            return priced.Error!;
        }

        var (find, _, amounts) = priced.Value!;
        return new GrabQuoteDto(
            find.Reference,
            find.Title,
            find.StoreName,
            CheckoutMoney.Format(amounts.ShelfPrice),
            CheckoutMoney.Format(amounts.GrabFee),
            CheckoutMoney.Format(amounts.SalesTax),
            CheckoutMoney.Format(amounts.Total),
            CheckoutMoney.Format(amounts.Deposit),
            CheckoutMoney.Format(amounts.RefundAfterAcceptance));
    }

    public async Task<Result<GrabDto>> Handle(StartGrabCommand command, CancellationToken cancellationToken)
    {
        var repeat = await db.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.IdempotencyKey == command.IdempotencyKey, cancellationToken);
        if (repeat is not null)
        {
            // A double click: the first request is the grab.
            return repeat.GrabId is { } repeatId ? await Handle(new GetMyGrabQuery(command.ShopperId, repeatId), cancellationToken) : GrabRefusals.NotFound;
        }

        if (!command.DepositAcknowledged)
        {
            return GrabRefusals.DepositNotAcknowledged;
        }

        var priced = await PriceAsync(command.ShopperId, command.FindReference, command.Address, cancellationToken);
        if (!priced.IsSuccess)
        {
            return priced.Error!;
        }

        var (find, address, amounts) = priced.Value!;
        if (command.ExpectedTotal != amounts.Total)
        {
            return CheckoutCommandHandlers.TotalChanged;
        }

        if (string.IsNullOrWhiteSpace(command.PaymentMethodToken))
        {
            return Error.Invalid(
                "PAYMENT_METHOD_REQUIRED",
                new Dictionary<string, FieldError> { ["paymentMethodToken"] = new("PAYMENT_METHOD_REQUIRED", "Enter your card details") });
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var last = await db.Grabs.MaxAsync(g => (long?)g.Number, cancellationToken);
        var grab = Grab.Start((last ?? Grab.FirstNumber - 1) + 1, find.Id, command.ShopperId, amounts, address, now);
        var payment = Payment.ForGrab(grab.Id, command.IdempotencyKey, amounts.Total, now);
        grab.AttachPayment(payment.Id);
        db.Grabs.Add(grab);
        db.Payments.Add(payment);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The filtered unique index on open grabs, the grab number or the payment key decided a race.
            db.ChangeTracker.Clear();
            var first = await db.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.IdempotencyKey == command.IdempotencyKey, cancellationToken);
            return first?.GrabId is { } firstId ? await Handle(new GetMyGrabQuery(command.ShopperId, firstId), cancellationToken) : GrabRefusals.InProgress;
        }

        try
        {
            // For a grab the request carries the grab ID as the checkout and the find as the listing (ADR orders/0009).
            var providerPaymentId = await provider.CreatePaymentAsync(
                new PaymentRequest(payment.Id, grab.Id, find.Id, amounts.Total, command.PaymentMethodToken, command.IdempotencyKey),
                cancellationToken);
            payment.Accepted(providerPaymentId);
        }
        catch (HttpRequestException)
        {
            payment.Fail(string.Empty, "The payment couldn't be started", now);
            grab.PaymentFailed("We couldn't reach the payment provider. Try again.", now);
            await db.SaveChangesAsync(cancellationToken);
            return CheckoutCommandHandlers.ProviderUnavailable;
        }

        await db.SaveChangesAsync(cancellationToken);
        return GrabSupport.ToDto(grab, find);
    }

    public async Task<IReadOnlyList<GrabSummaryDto>> Handle(GetMyGrabsQuery query, CancellationToken cancellationToken)
    {
        var rows = await db.Grabs.AsNoTracking()
            .Where(g => g.ShopperId == query.ShopperId && g.Status != GrabStatus.PaymentPending && g.Status != GrabStatus.PaymentFailed)
            .Join(db.ShelfFinds, g => g.FindId, f => f.Id, (g, f) => new { Grab = g, f.Reference, f.Title, f.StoreName })
            .OrderByDescending(x => x.Grab.CreatedAt)
            .ToListAsync(cancellationToken);
        return rows
            .Select(x => new GrabSummaryDto(x.Grab.Id, x.Grab.Reference, x.Grab.Status.ToString(), x.Reference, x.Title, x.StoreName, CheckoutMoney.Format(x.Grab.Total), x.Grab.Deadline, x.Grab.CreatedAt))
            .ToList();
    }

    public async Task<Result<GrabDto>> Handle(GetMyGrabQuery query, CancellationToken cancellationToken)
    {
        var found = await FindMineAsync(query.ShopperId, query.GrabId, tracked: false, cancellationToken);
        return found is { } x ? GrabSupport.ToDto(x.Grab, x.Find) : GrabRefusals.NotFound;
    }

    public async Task<Result<GrabDto>> Handle(CancelGrabCommand command, CancellationToken cancellationToken)
    {
        if (await FindMineAsync(command.ShopperId, command.GrabId, tracked: true, cancellationToken) is not { } x)
        {
            return GrabRefusals.NotFound;
        }

        if (x.Grab.RefundIfCancelled is null)
        {
            return GrabRefusals.StateChanged;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var amount = x.Grab.Cancel(now);
        var refund = await GrabSupport.AddRefundAsync(db, x.Grab, amount, now, cancellationToken);
        GrabSupport.EmailShopper(outbox, x.Grab, x.Find.Title);
        if (!await TrySaveAsync(cancellationToken))
        {
            return GrabRefusals.StateChanged;
        }

        await GrabSupport.IssueAsync(db, provider, refund, cancellationToken);
        return GrabSupport.ToDto(x.Grab, x.Find);
    }

    public async Task<Result<GrabDto>> Handle(ConfirmGrabReceivedCommand command, CancellationToken cancellationToken)
    {
        if (await FindMineAsync(command.ShopperId, command.GrabId, tracked: true, cancellationToken) is not { } x)
        {
            return GrabRefusals.NotFound;
        }

        if (x.Grab.Status != GrabStatus.Bought)
        {
            return GrabRefusals.StateChanged;
        }

        x.Grab.ConfirmReceived(clock.GetUtcNow().UtcDateTime);
        return await TrySaveAsync(cancellationToken) ? GrabSupport.ToDto(x.Grab, x.Find) : GrabRefusals.StateChanged;
    }

    /// <summary>The find, the address and the amounts, or the refusal: L2-117 AC6, AC7 and AC10.</summary>
    private async Task<Result<(ShelfFind Find, ShippingAddress Address, GrabAmounts Amounts)>> PriceAsync(
        Guid shopperId, string findReference, AddressInput? input, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var find = await db.ShelfFinds.AsNoTracking()
            .Where(f => f.Reference == findReference && db.Members.Any(m => m.Id == f.PosterId && m.ClosedAt == null))
            .SingleOrDefaultAsync(cancellationToken);
        if (find is null || !find.IsLive(now))
        {
            return FindRefusals.NotFound;
        }

        if (find.PosterId == shopperId)
        {
            return GrabRefusals.OwnFind;
        }

        if (find.GrabFee is not { } fee)
        {
            return GrabRefusals.NotOffered;
        }

        if (await db.Grabs.Open().AnyAsync(g => g.FindId == find.Id, cancellationToken))
        {
            return GrabRefusals.InProgress;
        }

        var address = GrabSupport.ReadAddress(input);
        if (!address.IsSuccess)
        {
            return address.Error!;
        }

        var tax = decimal.Round(await provider.QuoteTaxAsync(find.ShelfPrice + fee, address.Value!, cancellationToken), 2, MidpointRounding.AwayFromZero);
        var amounts = GrabPricing.Quote(find.ShelfPrice, fee, tax, options.Value.DepositPercent, options.Value.MinimumDeposit);
        return (find, address.Value!, amounts);
    }

    /// <summary>Another member's grab answers as missing (L2-121).</summary>
    private async Task<(Grab Grab, ShelfFind Find)?> FindMineAsync(Guid shopperId, Guid grabId, bool tracked, CancellationToken cancellationToken)
    {
        var grabs = tracked ? db.Grabs : db.Grabs.AsNoTracking();
        var grab = await grabs.SingleOrDefaultAsync(g => g.Id == grabId && g.ShopperId == shopperId, cancellationToken);
        if (grab is null)
        {
            return null;
        }

        var find = await db.ShelfFinds.AsNoTracking().SingleAsync(f => f.Id == grab.FindId, cancellationToken);
        return (grab, find);
    }

    private async Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
