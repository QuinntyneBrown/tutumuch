using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TutuMuch.Application.Abstractions;
using TutuMuch.Domain.Orders;

namespace TutuMuch.Api.Tests.Support;

/// <summary>
/// The payment provider in memory (AGENTS.md: fake the port). Tax is 8.875% of item plus shipping.
/// Payments are deduplicated by idempotency key like a real provider. Webhooks are JSON signed
/// with HMAC-SHA256 under <see cref="Secret"/>; <see cref="Webhook"/> builds them for the tests.
/// </summary>
public sealed class FakePaymentProvider : IPaymentProvider
{
    public const string Secret = "whsec_test_only";
    public const decimal TaxRate = 0.08875m;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Every payment request received, including repeats with the same key.</summary>
    public ConcurrentQueue<PaymentRequest> Requests { get; } = new();

    /// <summary>The payments charged: one per idempotency key.</summary>
    public ConcurrentDictionary<Guid, PaymentRequest> Charges { get; } = new();

    public ConcurrentQueue<(string ProviderPaymentId, decimal Amount, Guid Key)> Refunds { get; } = new();

    public Task<decimal> QuoteTaxAsync(decimal taxableAmount, ShippingAddress address, CancellationToken cancellationToken) =>
        Task.FromResult(decimal.Round(taxableAmount * TaxRate, 2, MidpointRounding.AwayFromZero));

    public Task<string> CreatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        Requests.Enqueue(request);
        var charge = Charges.GetOrAdd(request.IdempotencyKey, request);
        return Task.FromResult(ProviderIdOf(charge.PaymentId));
    }

    public Task<string> RefundAsync(string providerPaymentId, decimal amount, Guid idempotencyKey, CancellationToken cancellationToken)
    {
        Refunds.Enqueue((providerPaymentId, amount, idempotencyKey));
        return Task.FromResult($"re_{idempotencyKey:N}");
    }

    public PaymentEvent? ReadWebhook(string body, string? signature)
    {
        if (signature is null || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Sign(body)), Encoding.ASCII.GetBytes(signature)))
        {
            return null;
        }

        var e = JsonSerializer.Deserialize<WebhookBody>(body, Json)!;
        var type = e.Type == "payment.succeeded" ? PaymentEventType.Succeeded : PaymentEventType.Failed;
        return new PaymentEvent(e.Id, type, e.PaymentId, e.ProviderPaymentId, e.CardBrand, e.CardLast4, e.DeclineReason);
    }

    public static string ProviderIdOf(Guid paymentId) => $"pi_{paymentId:N}";

    public static string Sign(string body) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(body)));

    /// <summary>A webhook body for the payment; a decline when <paramref name="declineReason"/> is set.</summary>
    public static string Webhook(Guid paymentId, string? declineReason = null, string? eventId = null) =>
        JsonSerializer.Serialize(
            new WebhookBody(
                eventId ?? $"evt_{Guid.NewGuid():N}",
                declineReason is null ? "payment.succeeded" : "payment.failed",
                paymentId,
                ProviderIdOf(paymentId),
                declineReason is null ? "Visa" : null,
                declineReason is null ? "4242" : null,
                declineReason),
            Json);

    private sealed record WebhookBody(string Id, string Type, Guid PaymentId, string ProviderPaymentId, string? CardBrand, string? CardLast4, string? DeclineReason);
}
