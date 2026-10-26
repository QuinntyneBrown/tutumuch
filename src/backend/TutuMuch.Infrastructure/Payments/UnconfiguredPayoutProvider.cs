using TutuMuch.Application.Abstractions;

namespace TutuMuch.Infrastructure.Payments;

/// <summary>
/// Stands in for the provider's payout adapter until a provider and its credentials are chosen
/// (ADR orders/0001, orders/0005). Onboarding answers 502, the release job leaves every payout
/// Releasable and no webhook is accepted; the tests replace this port with a fake.
/// </summary>
internal sealed class UnconfiguredPayoutProvider : IPayoutProvider
{
    public Task<string> CreateConnectedAccountAsync(Guid memberId, string email, CancellationToken cancellationToken) =>
        throw new HttpRequestException("No payout provider is configured.");

    public Task<string> CreateOnboardingLinkAsync(string accountId, string returnUrl, string refreshUrl, CancellationToken cancellationToken) =>
        throw new HttpRequestException("No payout provider is configured.");

    public Task<string> CreatePayoutAsync(string accountId, decimal amount, Guid idempotencyKey, CancellationToken cancellationToken) =>
        throw new HttpRequestException("No payout provider is configured.");

    public PayoutEvent? ReadWebhook(string body, string? signature) => null;
}
