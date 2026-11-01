using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Domain.Finds;

/// <summary>
/// Where a grab stands (L2-117 to L2-119, ADR orders/0009). PaymentPending, Requested, Accepted and
/// Bought are open: a find has at most one open grab. The others are terminal.
/// </summary>
public enum GrabStatus
{
    PaymentPending,
    PaymentFailed,
    Requested,
    Accepted,
    Bought,
    Received,
    Declined,
    Unavailable,
    Expired,
    Cancelled,
}

/// <summary>Why TuTuMuch couldn't get the item (L2-118).</summary>
public enum GrabUnavailableReason
{
    Gone,
    PriceWentUp,
}

public static class GrabUnavailableReasons
{
    public static string ToLabel(this GrabUnavailableReason reason) =>
        reason == GrabUnavailableReason.Gone ? "Gone from the shelf" : "Price went up";
}

/// <summary>
/// A shopper's request that TuTuMuch buy a shelf find and send it to them (L2-117). The shopper
/// pays the whole total up front; a stated deposit is kept only when they cancel after TuTuMuch
/// accepts, and every other ending refunds the total (ADR orders/0009). Each change checks the
/// status first; the status is a concurrency token, so TuTuMuch and the deadline job cannot both
/// move one grab.
/// </summary>
public sealed class Grab
{
    /// <summary>How long TuTuMuch has to accept once the grab is paid.</summary>
    public static readonly TimeSpan AnswerWithin = TimeSpan.FromHours(48);

    /// <summary>How long TuTuMuch has to buy the item once it accepts.</summary>
    public static readonly TimeSpan BuyWithin = TimeSpan.FromDays(3);

    /// <summary>A Bought grab counts as received this long after it was bought.</summary>
    public static readonly TimeSpan ReceivedAfter = TimeSpan.FromDays(14);

    /// <summary>First grab number; references start at GR-1001.</summary>
    public const long FirstNumber = 1001;

    public static readonly IReadOnlyList<GrabStatus> OpenStatuses =
        [GrabStatus.PaymentPending, GrabStatus.Requested, GrabStatus.Accepted, GrabStatus.Bought];

    private Grab()
    {
    }

    public Guid Id { get; private set; }

    public long Number { get; private set; }

    public string Reference { get; private set; } = string.Empty;

    public Guid FindId { get; private set; }

    public Guid ShopperId { get; private set; }

    public GrabStatus Status { get; private set; }

    public decimal ShelfPrice { get; private set; }

    public decimal GrabFee { get; private set; }

    public decimal SalesTax { get; private set; }

    public decimal Total { get; private set; }

    public decimal Deposit { get; private set; }

    public ShippingAddress Address { get; private set; } = null!;

    /// <summary>The payment that charged the total; set when it is sent to the provider.</summary>
    public Guid? PaymentId { get; private set; }

    public string? PaymentDeclineReason { get; private set; }

    public GrabUnavailableReason? UnavailableReason { get; private set; }

    /// <summary>What was refunded when the grab ended; null when nothing was.</summary>
    public decimal? RefundedAmount { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? PaidAt { get; private set; }

    public DateTime? AnswerBy { get; private set; }

    public DateTime? AcceptedAt { get; private set; }

    public DateTime? BuyBy { get; private set; }

    public DateTime? BoughtAt { get; private set; }

    public DateTime? ReceivedAt { get; private set; }

    public DateTime? EndedAt { get; private set; }

    public bool IsOpen => OpenStatuses.Contains(Status);

    /// <summary>The next deadline: accept by while Requested, buy by while Accepted.</summary>
    public DateTime? Deadline => Status switch
    {
        GrabStatus.Requested => AnswerBy,
        GrabStatus.Accepted => BuyBy,
        _ => null,
    };

    /// <summary>What the shopper would get back if they cancelled now; null when they cannot (L2-119).</summary>
    public decimal? RefundIfCancelled => Status switch
    {
        GrabStatus.Requested => Total,
        GrabStatus.Accepted => Total - Deposit,
        _ => null,
    };

    public static string ReferenceFor(long number) => $"GR-{number}";

    public static Grab Start(long number, Guid findId, Guid shopperId, GrabAmounts amounts, ShippingAddress address, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        Number = number,
        Reference = ReferenceFor(number),
        FindId = findId,
        ShopperId = shopperId,
        Status = GrabStatus.PaymentPending,
        ShelfPrice = amounts.ShelfPrice,
        GrabFee = amounts.GrabFee,
        SalesTax = amounts.SalesTax,
        Total = amounts.Total,
        Deposit = amounts.Deposit,
        Address = address,
        CreatedAt = now,
    };

    public void AttachPayment(Guid paymentId)
    {
        Require(GrabStatus.PaymentPending);
        PaymentId = paymentId;
    }

    /// <summary>The provider's webhook confirmed the payment: TuTuMuch has 48 hours to accept.</summary>
    public void PaymentSucceeded(DateTime now)
    {
        Require(GrabStatus.PaymentPending);
        Status = GrabStatus.Requested;
        PaidAt = now;
        AnswerBy = now + AnswerWithin;
    }

    /// <summary>The card was declined: the grab ends, nothing was charged and the find is free again.</summary>
    public void PaymentFailed(string reason, DateTime now)
    {
        Require(GrabStatus.PaymentPending);
        Status = GrabStatus.PaymentFailed;
        PaymentDeclineReason = reason;
        EndedAt = now;
    }

    public bool CanAccept(DateTime now) => Status == GrabStatus.Requested && now < AnswerBy;

    public void Accept(DateTime now)
    {
        if (!CanAccept(now))
        {
            throw new InvalidOperationException("Only a Requested grab inside its 48 hours can be accepted.");
        }

        Status = GrabStatus.Accepted;
        AcceptedAt = now;
        BuyBy = now + BuyWithin;
    }

    /// <summary>TuTuMuch declines: the total is refunded. Returns the amount to refund.</summary>
    public decimal Decline(DateTime now)
    {
        Require(GrabStatus.Requested);
        return End(GrabStatus.Declined, Total, now);
    }

    public void MarkBought(DateTime now)
    {
        Require(GrabStatus.Accepted);
        Status = GrabStatus.Bought;
        BoughtAt = now;
    }

    /// <summary>TuTuMuch couldn't get it: the total is refunded. Returns the amount to refund.</summary>
    public decimal MarkUnavailable(GrabUnavailableReason reason, DateTime now)
    {
        Require(GrabStatus.Accepted);
        UnavailableReason = reason;
        return End(GrabStatus.Unavailable, Total, now);
    }

    /// <summary>The shopper cancels: the total before acceptance, the total less the deposit after. Returns the amount to refund.</summary>
    public decimal Cancel(DateTime now)
    {
        var refund = RefundIfCancelled ?? throw new InvalidOperationException("Only a Requested or Accepted grab can be cancelled.");
        return End(GrabStatus.Cancelled, refund, now);
    }

    public void ConfirmReceived(DateTime now)
    {
        Require(GrabStatus.Bought);
        Status = GrabStatus.Received;
        ReceivedAt = now;
        EndedAt = now;
    }

    /// <summary>True when TuTuMuch missed its deadline to accept or to buy (L2-119).</summary>
    public bool IsOverdue(DateTime now) => Deadline is { } deadline && now >= deadline;

    /// <summary>The deadline passed: the total is refunded. Returns the amount to refund.</summary>
    public decimal Expire(DateTime now)
    {
        if (!IsOverdue(now))
        {
            throw new InvalidOperationException("The grab is not past its deadline.");
        }

        return End(GrabStatus.Expired, Total, now);
    }

    public bool IsDueToReceive(DateTime now) => Status == GrabStatus.Bought && now >= BoughtAt + ReceivedAfter;

    private decimal End(GrabStatus status, decimal refund, DateTime now)
    {
        Status = status;
        RefundedAmount = refund;
        EndedAt = now;
        return refund;
    }

    private void Require(GrabStatus status)
    {
        if (Status != status)
        {
            throw new InvalidOperationException($"The grab is {Status}, not {status}.");
        }
    }
}
