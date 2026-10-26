namespace TuTuMuch.Domain.Orders;

/// <summary>A provider webhook event already processed; its ID is the key, so a repeat changes nothing (L2-030 AC5).</summary>
public sealed class PaymentWebhookEvent
{
    private PaymentWebhookEvent()
    {
    }

    public string ProviderEventId { get; private set; } = string.Empty;

    public string Type { get; private set; } = string.Empty;

    public DateTime ProcessedAt { get; private set; }

    public static PaymentWebhookEvent Processed(string providerEventId, string type, DateTime now) =>
        new() { ProviderEventId = providerEventId, Type = type, ProcessedAt = now };
}
