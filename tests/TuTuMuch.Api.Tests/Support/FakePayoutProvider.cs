using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TuTuMuch.Application.Abstractions;

namespace TuTuMuch.Api.Tests.Support;

/// <summary>
/// The payout side of the payment provider in memory (AGENTS.md: fake the port). Payouts are
/// deduplicated by idempotency key like a real provider. Webhooks are JSON signed with HMAC-SHA256
/// under <see cref="Secret"/>; the static builders make them for the tests.
/// </summary>
public sealed class FakePayoutProvider : IPayoutProvider
{
    public const string Secret = "whsec_payouts_test_only";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>When set, every call that reaches the provider fails as an outage would.</summary>
    public bool Unavailable { get; set; }

    /// <summary>Connected accounts created, by provider account ID, with the member they belong to.</summary>
    public ConcurrentDictionary<string, Guid> Accounts { get; } = new();

    /// <summary>Every onboarding link requested: the account and the return URL.</summary>
    public ConcurrentQueue<(string AccountId, string ReturnUrl)> Links { get; } = new();

    /// <summary>Every payout request received, including repeats with the same key.</summary>
    public ConcurrentQueue<(string AccountId, decimal Amount, Guid Key)> Requests { get; } = new();

    /// <summary>The payouts made: one per idempotency key.</summary>
    public ConcurrentDictionary<Guid, (string AccountId, decimal Amount)> Payouts { get; } = new();

    public Task<string> CreateConnectedAccountAsync(Guid memberId, string email, CancellationToken cancellationToken)
    {
        ThrowIfUnavailable();
        var id = AccountIdOf(memberId);
        Accounts[id] = memberId;
        return Task.FromResult(id);
    }

    public Task<string> CreateOnboardingLinkAsync(string accountId, string returnUrl, string refreshUrl, CancellationToken cancellationToken)
    {
        ThrowIfUnavailable();
        Links.Enqueue((accountId, returnUrl));
        return Task.FromResult($"https://payments.example.com/onboarding/{accountId}");
    }

    public Task<string> CreatePayoutAsync(string accountId, decimal amount, Guid idempotencyKey, CancellationToken cancellationToken)
    {
        ThrowIfUnavailable();
        Requests.Enqueue((accountId, amount, idempotencyKey));
        Payouts.GetOrAdd(idempotencyKey, (accountId, amount));
        return Task.FromResult($"po_{idempotencyKey:N}");
    }

    public PayoutEvent? ReadWebhook(string body, string? signature)
    {
        if (signature is null || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Sign(body)), Encoding.ASCII.GetBytes(signature)))
        {
            return null;
        }

        var e = JsonSerializer.Deserialize<WebhookBody>(body, Json)!;
        var type = e.Type switch
        {
            "account.updated" => PayoutEventType.AccountUpdated,
            "payout.paid" => PayoutEventType.PayoutPaid,
            _ => PayoutEventType.PayoutFailed,
        };
        return new PayoutEvent(e.Id, type, e.AccountId, e.Complete, e.BankName, e.BankLast4, e.PayoutId, e.FailureReason);
    }

    public static string AccountIdOf(Guid memberId) => $"acct_{memberId:N}";

    public static string Sign(string body) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(body)));

    /// <summary>An account.updated event: onboarding finished (or not) with the given bank.</summary>
    public static string AccountUpdated(string accountId, bool complete = true, string bankName = "Chase", string bankLast4 = "6021") =>
        Serialize(new WebhookBody($"evt_{Guid.NewGuid():N}", "account.updated", accountId, complete, bankName, bankLast4, null, null));

    public static string PayoutPaid(Guid payoutId) =>
        Serialize(new WebhookBody($"evt_{Guid.NewGuid():N}", "payout.paid", null, false, null, null, payoutId, null));

    public static string PayoutFailed(Guid payoutId, string reason = "The bank account was closed.") =>
        Serialize(new WebhookBody($"evt_{Guid.NewGuid():N}", "payout.failed", null, false, null, null, payoutId, reason));

    private static string Serialize(WebhookBody body) => JsonSerializer.Serialize(body, Json);

    private void ThrowIfUnavailable()
    {
        if (Unavailable)
        {
            throw new HttpRequestException("The payout provider is unavailable.");
        }
    }

    private sealed record WebhookBody(string Id, string Type, string? AccountId, bool Complete, string? BankName, string? BankLast4, Guid? PayoutId, string? FailureReason);
}
