using TuTuMuch.Application.Abstractions;
using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Infrastructure.Payments;

/// <summary>
/// Stands in for the payment provider adapter until a provider and its credentials are chosen
/// (ADR orders/0001). It charges nothing, quotes no tax and accepts no webhook, so checkout stops
/// safely at the address step outside the tests, which replace this port with a fake.
/// </summary>
internal sealed class UnconfiguredPaymentProvider : IPaymentProvider
{
    public Task<decimal> QuoteTaxAsync(decimal taxableAmount, ShippingAddress address, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("No payment provider is configured.");

    public Task<string> CreatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken) =>
        throw new HttpRequestException("No payment provider is configured.");

    public Task<string> RefundAsync(string providerPaymentId, decimal amount, Guid idempotencyKey, CancellationToken cancellationToken) =>
        throw new HttpRequestException("No payment provider is configured.");

    public PaymentEvent? ReadWebhook(string body, string? signature) => null;
}
