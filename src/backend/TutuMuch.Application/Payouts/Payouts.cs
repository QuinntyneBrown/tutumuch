using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TutuMuch.Application.Abstractions;
using TutuMuch.Application.Checkouts;
using TutuMuch.Application.Common;
using TutuMuch.Application.Listings;
using TutuMuch.Application.Orders;
using TutuMuch.Domain.Orders;
using TutuMuch.Domain.Payouts;

namespace TutuMuch.Application.Payouts;

/// <summary>Errors of the payouts endpoints.</summary>
public static class PayoutErrors
{
    public static readonly Error ProviderUnavailable = new(502, "PAYOUT_PROVIDER_UNAVAILABLE", "Payout setup isn’t available right now. Try again in a moment.");

    public static readonly Error InvalidSignature = new(400, "WEBHOOK_SIGNATURE_INVALID", "The webhook signature is not valid");
}

/// <summary>The provider-hosted onboarding link.</summary>
public sealed record OnboardingLinkDto(string Url);

/// <summary>
/// The seller asks to set up payouts (L2-031 AC1): the connected account is created on first use,
/// then the provider returns a link to its hosted onboarding, which comes back to /account/payouts.
/// </summary>
public sealed record CreateOnboardingLinkCommand(Guid MemberId) : IRequest<Result<OnboardingLinkDto>>;

public sealed class CreateOnboardingLinkHandler(ITutuMuchDbContext db, IPayoutProvider provider, IOptions<AppOptions> app, TimeProvider clock) :
    IRequestHandler<CreateOnboardingLinkCommand, Result<OnboardingLinkDto>>
{
    public async Task<Result<OnboardingLinkDto>> Handle(CreateOnboardingLinkCommand command, CancellationToken cancellationToken)
    {
        var baseUrl = app.Value.BaseUrl.TrimEnd('/');
        try
        {
            var account = await db.SellerPayoutAccounts.SingleOrDefaultAsync(a => a.MemberId == command.MemberId, cancellationToken);
            if (account is null)
            {
                var email = await db.Members.Where(m => m.Id == command.MemberId).Select(m => m.Email).SingleAsync(cancellationToken);
                var accountId = await provider.CreateConnectedAccountAsync(command.MemberId, email, cancellationToken);
                account = SellerPayoutAccount.Start(command.MemberId, accountId, clock.GetUtcNow().UtcDateTime);
                db.SellerPayoutAccounts.Add(account);
                await db.SaveChangesAsync(cancellationToken);
            }

            var url = await provider.CreateOnboardingLinkAsync(
                account.ProviderAccountId,
                $"{baseUrl}/account/payouts?onboarding=return",
                $"{baseUrl}/account/payouts?onboarding=refresh",
                cancellationToken);
            return new OnboardingLinkDto(url);
        }
        catch (HttpRequestException)
        {
            return PayoutErrors.ProviderUnavailable;
        }
    }
}

/// <summary>One sale in the payouts history: what the seller gets, the fee and where the payout is.</summary>
public sealed record PayoutRowDto(
    Guid OrderId,
    string OrderReference,
    CheckoutItemDto Item,
    string Amount,
    string Fee,
    string Status,
    string? HoldReason,
    DateTime? PaidAt);

/// <summary>
/// The payouts page (mocks pages/payouts): totals paid out and pending, onboarding state, the bank
/// for display, the banner to show (info, warning, danger or none) and the history, newest first.
/// </summary>
public sealed record PayoutSummaryDto(
    string PaidOut,
    string Pending,
    string Onboarding,
    string? BankDisplay,
    string? Banner,
    IReadOnlyList<PayoutRowDto> History);

/// <summary>The signed-in member's payouts. Payouts of Cancelled or Refunded orders are never paid and are left out.</summary>
public sealed record GetPayoutSummaryQuery(Guid MemberId) : IRequest<PayoutSummaryDto>;

public sealed class GetPayoutSummaryHandler(ITutuMuchDbContext db, IOptions<PhotoOptions> photoOptions) :
    IRequestHandler<GetPayoutSummaryQuery, PayoutSummaryDto>
{
    private static readonly PayoutStatus[] PendingStatuses = [PayoutStatus.Held, PayoutStatus.Releasable, PayoutStatus.Sent];

    public async Task<PayoutSummaryDto> Handle(GetPayoutSummaryQuery query, CancellationToken cancellationToken)
    {
        var rows = await (
            from payout in db.Payouts.AsNoTracking()
            join order in db.Orders.AsNoTracking() on payout.OrderId equals order.Id
            where payout.SellerId == query.MemberId && order.Status != OrderStatus.Cancelled && order.Status != OrderStatus.Refunded
            orderby order.PaidAt descending
            select new { Payout = payout, order.Reference, order.ListingId })
            .ToListAsync(cancellationToken);
        var account = await db.SellerPayoutAccounts.AsNoTracking().SingleOrDefaultAsync(a => a.MemberId == query.MemberId, cancellationToken);
        var onboarding = account?.Status ?? PayoutAccountStatus.NotStarted;

        var history = new List<PayoutRowDto>(rows.Count);
        foreach (var row in rows)
        {
            var payout = row.Payout;
            history.Add(new PayoutRowDto(
                payout.OrderId,
                row.Reference,
                await OrderViews.ItemAsync(db, row.ListingId, photoOptions.Value, cancellationToken),
                CheckoutMoney.Format(payout.NetAmount),
                CheckoutMoney.Format(payout.FeeAmount),
                payout.Status.ToString(),
                payout.HoldReason?.ToString(),
                payout.PaidAt));
        }

        var payouts = rows.Select(r => r.Payout).ToList();
        var pending = payouts.Where(p => PendingStatuses.Contains(p.Status)).Sum(p => p.NetAmount);
        return new PayoutSummaryDto(
            CheckoutMoney.Format(payouts.Where(p => p.Status == PayoutStatus.Paid).Sum(p => p.NetAmount)),
            CheckoutMoney.Format(pending),
            onboarding.ToString(),
            account?.BankDisplay,
            Banner(payouts, onboarding, pending),
            history);
    }

    /// <summary>
    /// Danger while the latest sent payout failed, warning while money waits for onboarding, info
    /// while a payout is on its way; otherwise none. The success banner is the browser's, once,
    /// on return from onboarding.
    /// </summary>
    private static string? Banner(IReadOnlyList<Payout> payouts, PayoutAccountStatus onboarding, decimal pending)
    {
        var latestSent = payouts.Where(p => p.SentAt is not null).MaxBy(p => p.SentAt);
        if (latestSent?.Status == PayoutStatus.Failed)
        {
            return "danger";
        }

        if (onboarding != PayoutAccountStatus.Complete && pending > 0)
        {
            return "warning";
        }

        return payouts.Any(p => p.Status == PayoutStatus.Sent) ? "info" : null;
    }
}

/// <summary>
/// Completion makes the order's payout releasable, or holds it for onboarding (L2-031, ADR
/// orders/0004). Published once after the completion is saved.
/// </summary>
public sealed class OrderCompletedPayoutHandler(ITutuMuchDbContext db, ISellerPayoutAccountReader accounts, TimeProvider clock) :
    INotificationHandler<OrderCompleted>
{
    public async Task Handle(OrderCompleted notification, CancellationToken cancellationToken)
    {
        var payout = await db.Payouts.SingleOrDefaultAsync(p => p.OrderId == notification.OrderId, cancellationToken);
        if (payout is null)
        {
            return;
        }

        payout.OrderCompleted(await accounts.IsOnboardedAsync(notification.SellerId, cancellationToken), clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// The release job's pass (L2-031 AC3): every Releasable payout whose order is Completed and whose
/// seller is onboarded is sent with its ID as the idempotency key. A provider error leaves it
/// Releasable for the next pass. Returns how many were sent.
/// </summary>
public sealed record ReleasePayoutsCommand : IRequest<int>;

public sealed class ReleasePayoutsHandler(ITutuMuchDbContext db, IPayoutProvider provider, TimeProvider clock, ILogger<ReleasePayoutsHandler> logger) :
    IRequestHandler<ReleasePayoutsCommand, int>
{
    public async Task<int> Handle(ReleasePayoutsCommand command, CancellationToken cancellationToken)
    {
        await CatchUpLostCompletionsAsync(cancellationToken);
        var due = await (
            from payout in db.Payouts
            join order in db.Orders on payout.OrderId equals order.Id
            join account in db.SellerPayoutAccounts on payout.SellerId equals account.MemberId
            where payout.Status == PayoutStatus.Releasable && order.Status == OrderStatus.Completed && account.Status == PayoutAccountStatus.Complete
            select new { Payout = payout, account.ProviderAccountId })
            .ToListAsync(cancellationToken);
        var sent = 0;
        foreach (var item in due)
        {
            try
            {
                var providerPayoutId = await provider.CreatePayoutAsync(item.ProviderAccountId, item.Payout.NetAmount, item.Payout.Id, cancellationToken);
                item.Payout.MarkSent(providerPayoutId, clock.GetUtcNow().UtcDateTime);
                await db.SaveChangesAsync(cancellationToken);
                sent++;
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(exception, "Payout {PayoutId} was not sent; it stays Releasable", item.Payout.Id);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another pass sent it between the read and the write; the provider deduplicated by key.
                db.ChangeTracker.Clear();
            }
        }

        return sent;
    }

    /// <summary>
    /// A payout still waiting for its order although the order is Completed lost its OrderCompleted
    /// event (published after the save, ADR orders/0004); the job applies it, so the payout is late, not lost.
    /// </summary>
    private async Task CatchUpLostCompletionsAsync(CancellationToken cancellationToken)
    {
        var stale = await (
            from payout in db.Payouts
            join order in db.Orders on payout.OrderId equals order.Id
            where payout.Status == PayoutStatus.Held
                && (payout.HoldReason == PayoutHoldReason.OrderNotCompleted || payout.HoldReason == PayoutHoldReason.OpenProblem)
                && order.Status == OrderStatus.Completed
            select payout)
            .ToListAsync(cancellationToken);
        if (stale.Count == 0)
        {
            return;
        }

        var sellers = stale.Select(p => p.SellerId).Distinct().ToList();
        var onboarded = await db.SellerPayoutAccounts
            .Where(a => sellers.Contains(a.MemberId) && a.Status == PayoutAccountStatus.Complete)
            .Select(a => a.MemberId)
            .ToListAsync(cancellationToken);
        var now = clock.GetUtcNow().UtcDateTime;
        stale.ForEach(p => p.OrderCompleted(onboarded.Contains(p.SellerId), now));
        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// A raw payout webhook from the provider: account.updated records onboarding and the bank, and
/// releases payouts held only for onboarding; payout.paid and payout.failed settle a sent payout.
/// Repeated events change nothing.
/// </summary>
public sealed record HandlePayoutWebhookCommand(string Body, string? Signature) : IRequest<Result<WebhookOutcome>>;

public sealed class PayoutWebhookHandler(ITutuMuchDbContext db, IPayoutProvider provider, TimeProvider clock) :
    IRequestHandler<HandlePayoutWebhookCommand, Result<WebhookOutcome>>
{
    public async Task<Result<WebhookOutcome>> Handle(HandlePayoutWebhookCommand command, CancellationToken cancellationToken)
    {
        if (provider.ReadWebhook(command.Body, command.Signature) is not { } payoutEvent)
        {
            return PayoutErrors.InvalidSignature;
        }

        if (await db.PaymentWebhookEvents.AnyAsync(e => e.ProviderEventId == payoutEvent.EventId, cancellationToken))
        {
            return WebhookOutcome.Duplicate;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        db.PaymentWebhookEvents.Add(PaymentWebhookEvent.Processed(payoutEvent.EventId, payoutEvent.Type.ToString(), now));
        var outcome = payoutEvent.Type == PayoutEventType.AccountUpdated
            ? await AccountUpdatedAsync(payoutEvent, now, cancellationToken)
            : await PayoutSettledAsync(payoutEvent, now, cancellationToken);
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

    private async Task<WebhookOutcome> AccountUpdatedAsync(PayoutEvent e, DateTime now, CancellationToken cancellationToken)
    {
        var account = await db.SellerPayoutAccounts.SingleOrDefaultAsync(a => a.ProviderAccountId == e.ProviderAccountId, cancellationToken);
        if (account is null)
        {
            return WebhookOutcome.Ignored;
        }

        if (account.Update(e.AccountComplete, e.BankName, e.BankLast4, now))
        {
            var held = await db.Payouts
                .Where(p => p.SellerId == account.MemberId && p.Status == PayoutStatus.Held && p.HoldReason == PayoutHoldReason.OnboardingIncomplete)
                .ToListAsync(cancellationToken);
            held.ForEach(p => p.SellerOnboarded(now));
        }

        return WebhookOutcome.Processed;
    }

    private async Task<WebhookOutcome> PayoutSettledAsync(PayoutEvent e, DateTime now, CancellationToken cancellationToken)
    {
        var payout = await db.Payouts.SingleOrDefaultAsync(p => p.Id == e.PayoutId, cancellationToken);
        if (payout?.Status != PayoutStatus.Sent)
        {
            return WebhookOutcome.Ignored;
        }

        if (e.Type == PayoutEventType.PayoutPaid)
        {
            payout.MarkPaid(now);
        }
        else
        {
            payout.MarkFailed(e.FailureReason ?? "The bank returned the payout.", now);
        }

        return WebhookOutcome.Processed;
    }
}

/// <summary>The payout panel of a sale (mocks sale-detail/completed): gross, fee, net and where it is.</summary>
public sealed record SalePayoutDto(string Gross, decimal FeePercent, string Fee, string Net, string Status, DateTime? SentAt, string? BankDisplay);

public static class SalePayouts
{
    public static async Task<SalePayoutDto?> ForAsync(ITutuMuchDbContext db, Order order, CancellationToken cancellationToken)
    {
        var payout = await db.Payouts.AsNoTracking().SingleOrDefaultAsync(p => p.OrderId == order.Id, cancellationToken);
        if (payout is null)
        {
            return null;
        }

        var account = await db.SellerPayoutAccounts.AsNoTracking().SingleOrDefaultAsync(a => a.MemberId == order.SellerId, cancellationToken);
        return new SalePayoutDto(
            CheckoutMoney.Format(payout.GrossAmount),
            decimal.Parse((payout.FeeRate * 100m).ToString("0.##", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
            CheckoutMoney.Format(payout.FeeAmount),
            CheckoutMoney.Format(payout.NetAmount),
            payout.Status.ToString(),
            payout.SentAt,
            account?.BankDisplay);
    }
}
