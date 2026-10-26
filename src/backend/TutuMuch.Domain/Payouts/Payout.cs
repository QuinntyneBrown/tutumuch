using TutuMuch.Domain.Orders;

namespace TutuMuch.Domain.Payouts;

/// <summary>Where a payout is (L2-031). Held until the order completes and the seller is onboarded.</summary>
public enum PayoutStatus
{
    Held,
    Releasable,
    Sent,
    Paid,
    Failed,

    /// <summary>Never paid: the order was refunded on a problem (L2-036 AC3).</summary>
    Cancelled,
}

/// <summary>Why a payout is Held.</summary>
public enum PayoutHoldReason
{
    OrderNotCompleted,
    OnboardingIncomplete,

    /// <summary>The buyer reported a problem; held until an administrator decides (L2-036).</summary>
    OpenProblem,
}

/// <summary>
/// The Tutu Much fee (L2-031): <c>round(rate × (item price + shipping), 2)</c>, midpoint away from zero.
/// Sales tax is not part of the gross amount. A 10% fee on $80.00 plus $10.00 shipping is $9.00.
/// </summary>
public static class PayoutFees
{
    public static decimal Fee(decimal rate, decimal gross) => decimal.Round(rate * gross, 2, MidpointRounding.AwayFromZero);

    /// <summary>The configured percentage (10 for 10%) as the rate snapshotted on the order (0.10).</summary>
    public static decimal RateFromPercent(decimal percent) => decimal.Round(percent / 100m, 4);
}

/// <summary>
/// The transfer of one sale's proceeds, less the Tutu Much fee, to its seller (L2-031). Created Held with
/// its order; the order's completion makes it Releasable, or Held until onboarding when the seller
/// has not finished it. The release job sends it with its ID as the idempotency key.
/// </summary>
public sealed class Payout
{
    private Payout()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid SellerId { get; private set; }

    /// <summary>Item price plus shipping.</summary>
    public decimal GrossAmount { get; private set; }

    public decimal FeeRate { get; private set; }

    public decimal FeeAmount { get; private set; }

    /// <summary>Gross less the fee: the amount paid out.</summary>
    public decimal NetAmount { get; private set; }

    public PayoutStatus Status { get; private set; }

    public PayoutHoldReason? HoldReason { get; private set; }

    public string? ProviderPayoutId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ReleasableAt { get; private set; }

    public DateTime? SentAt { get; private set; }

    public DateTime? PaidAt { get; private set; }

    public DateTime? FailedAt { get; private set; }

    public string? FailureReason { get; private set; }

    /// <summary>The Held payout of a new order, with the fee the order snapshotted.</summary>
    public static Payout CreateHeld(Order order, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = order.Id,
        SellerId = order.SellerId,
        GrossAmount = order.ItemPrice + order.ShippingCost,
        FeeRate = order.FeeRate,
        FeeAmount = order.FeeAmount,
        NetAmount = order.ItemPrice + order.ShippingCost - order.FeeAmount,
        Status = PayoutStatus.Held,
        HoldReason = PayoutHoldReason.OrderNotCompleted,
        CreatedAt = now,
    };

    /// <summary>A problem was opened on the order: a Held payout stays Held for the problem (L2-036 AC1).</summary>
    public void HoldForProblem()
    {
        EnsureStatus(PayoutStatus.Held);
        HoldReason = PayoutHoldReason.OpenProblem;
    }

    /// <summary>The order was refunded on a problem: the payout is never made (L2-036 AC3).</summary>
    public void Cancel()
    {
        EnsureStatus(PayoutStatus.Held);
        Status = PayoutStatus.Cancelled;
        HoldReason = null;
    }

    /// <summary>The order completed: Releasable when the seller is onboarded, otherwise Held for onboarding.</summary>
    public void OrderCompleted(bool sellerOnboarded, DateTime now)
    {
        if (Status != PayoutStatus.Held)
        {
            return;
        }

        if (sellerOnboarded)
        {
            MakeReleasable(now);
        }
        else
        {
            HoldReason = PayoutHoldReason.OnboardingIncomplete;
        }
    }

    /// <summary>The seller finished onboarding: a payout waiting only for that becomes Releasable.</summary>
    public void SellerOnboarded(DateTime now)
    {
        if (Status == PayoutStatus.Held && HoldReason == PayoutHoldReason.OnboardingIncomplete)
        {
            MakeReleasable(now);
        }
    }

    public void MarkSent(string providerPayoutId, DateTime now)
    {
        EnsureStatus(PayoutStatus.Releasable);
        Status = PayoutStatus.Sent;
        ProviderPayoutId = providerPayoutId;
        SentAt = now;
    }

    public void MarkPaid(DateTime now)
    {
        EnsureStatus(PayoutStatus.Sent);
        Status = PayoutStatus.Paid;
        PaidAt = now;
    }

    public void MarkFailed(string reason, DateTime now)
    {
        EnsureStatus(PayoutStatus.Sent);
        Status = PayoutStatus.Failed;
        FailureReason = reason;
        FailedAt = now;
    }

    private void MakeReleasable(DateTime now)
    {
        Status = PayoutStatus.Releasable;
        HoldReason = null;
        ReleasableAt = now;
    }

    private void EnsureStatus(PayoutStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"The payout is {Status}, not {expected}.");
        }
    }
}
