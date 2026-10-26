namespace TutuMuch.Domain.Orders;

public enum PaymentStatus
{
    Pending,
    Succeeded,
    Failed,
}

/// <summary>
/// One payment attempt of a checkout. Only the provider's reference, the card brand and the last
/// four digits are kept; card numbers and security codes never reach Tutu Much (L2-030).
/// </summary>
public sealed class Payment
{
    private Payment()
    {
    }

    public Guid Id { get; private set; }

    public Guid CheckoutId { get; private set; }

    /// <summary>The client's key for this attempt; a repeated request with it changes nothing (L2-030 AC6).</summary>
    public Guid IdempotencyKey { get; private set; }

    public decimal Amount { get; private set; }

    public PaymentStatus Status { get; private set; }

    public string? ProviderPaymentId { get; private set; }

    public string? CardBrand { get; private set; }

    public string? CardLast4 { get; private set; }

    public string? DeclineReason { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? SettledAt { get; private set; }

    public static Payment Create(Guid checkoutId, Guid idempotencyKey, decimal amount, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        CheckoutId = checkoutId,
        IdempotencyKey = idempotencyKey,
        Amount = amount,
        Status = PaymentStatus.Pending,
        CreatedAt = now,
    };

    public void Accepted(string providerPaymentId) => ProviderPaymentId ??= providerPaymentId;

    public void Succeed(string providerPaymentId, string? cardBrand, string? cardLast4, DateTime now)
    {
        Status = PaymentStatus.Succeeded;
        ProviderPaymentId = providerPaymentId;
        CardBrand = cardBrand;
        CardLast4 = cardLast4 is { Length: 4 } last4 && last4.All(char.IsAsciiDigit) ? last4 : null;
        SettledAt = now;
    }

    public void Fail(string providerPaymentId, string reason, DateTime now)
    {
        Status = PaymentStatus.Failed;
        ProviderPaymentId = providerPaymentId;
        DeclineReason = reason;
        SettledAt = now;
    }
}
