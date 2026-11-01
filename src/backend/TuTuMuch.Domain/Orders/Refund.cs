namespace TuTuMuch.Domain.Orders;

public enum RefundReason
{
    DuplicatePayment,
    ShipDeadlinePassed,
    OrderCancelled,
    ProblemResolved,

    /// <summary>A grab ended with money back to the shopper (ADR orders/0009).</summary>
    GrabRefunded,
}

public enum RefundStatus
{
    Pending,
    Issued,
}

/// <summary>
/// Money returned to a buyer. A payment for a listing that already has an order (L2-029 AC2) and
/// a cancelled order (L2-035, L2-033 AC2) and an order whose problem was decided for the buyer
/// (L2-036) are refunded in full.
/// </summary>
public sealed class Refund
{
    private Refund()
    {
    }

    public Guid Id { get; private set; }

    public Guid PaymentId { get; private set; }

    public decimal Amount { get; private set; }

    public RefundReason Reason { get; private set; }

    public RefundStatus Status { get; private set; }

    public string? ProviderRefundId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static Refund ForDuplicate(Payment payment, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        PaymentId = payment.Id,
        Amount = payment.Amount,
        Reason = RefundReason.DuplicatePayment,
        Status = RefundStatus.Pending,
        CreatedAt = now,
    };

    /// <summary>The full refund of a cancelled order: the seller cancelled, or the ship deadline passed.</summary>
    public static Refund ForCancellation(Payment payment, CancellationReason reason, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        PaymentId = payment.Id,
        Amount = payment.Amount,
        Reason = reason == CancellationReason.ShipDeadlinePassed ? RefundReason.ShipDeadlinePassed : RefundReason.OrderCancelled,
        Status = RefundStatus.Pending,
        CreatedAt = now,
    };

    /// <summary>The full refund an administrator decides on an order problem (L2-036 AC3).</summary>
    public static Refund ForProblem(Payment payment, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        PaymentId = payment.Id,
        Amount = payment.Amount,
        Reason = RefundReason.ProblemResolved,
        Status = RefundStatus.Pending,
        CreatedAt = now,
    };

    /// <summary>
    /// A grab's refund: the whole total, or the total less the deposit when the shopper cancelled
    /// after TuTuMuch accepted (L2-119).
    /// </summary>
    public static Refund ForGrab(Payment payment, decimal amount, DateTime now)
    {
        if (amount <= 0m || amount > payment.Amount)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        return new()
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            Amount = amount,
            Reason = RefundReason.GrabRefunded,
            Status = RefundStatus.Pending,
            CreatedAt = now,
        };
    }

    public void Issued(string providerRefundId)
    {
        Status = RefundStatus.Issued;
        ProviderRefundId = providerRefundId;
    }
}
