using System.Net.Http.Json;

namespace TuTuMuch.Api.Tests.Support;

/// <summary>Calls for the payouts endpoints and the provider's payout webhook.</summary>
public static class Payouts
{
    public static async Task<OnboardingLinkView> OnboardingLinkAsync(this HttpClient client)
    {
        using var response = await client.PostAsync("/api/payouts/onboarding-link", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OnboardingLinkView>())!;
    }

    public static async Task<PayoutSummaryView> PayoutSummaryAsync(this HttpClient client) =>
        (await client.GetFromJsonAsync<PayoutSummaryView>("/api/payouts"))!;

    /// <summary>The seller requests the onboarding link and the provider reports the account complete.</summary>
    public static async Task OnboardAsync(this TuTuMuchApiFactory factory, HttpClient seller)
    {
        var link = await seller.OnboardingLinkAsync();
        var accountId = link.Url[(link.Url.LastIndexOf('/') + 1)..];
        (await factory.PostPayoutWebhookAsync(FakePayoutProvider.AccountUpdated(accountId))).EnsureSuccessStatusCode();
    }

    public static async Task<HttpResponseMessage> PostPayoutWebhookAsync(this TuTuMuchApiFactory factory, string body, string? signature = null)
    {
        using var client = factory.CreateMemberClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payouts/webhook") { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
        request.Headers.Add("Payout-Signature", signature ?? FakePayoutProvider.Sign(body));
        return await client.SendAsync(request);
    }
}

public sealed record OnboardingLinkView(string Url);

public sealed record PayoutRowView(Guid OrderId, string OrderReference, CheckoutItemView Item, string Amount, string Fee, string Status, string? HoldReason, DateTime? PaidAt);

public sealed record PayoutSummaryView(string PaidOut, string Pending, string Onboarding, string? BankDisplay, string? Banner, IReadOnlyList<PayoutRowView> History);

public sealed record SalePayoutView(string Gross, decimal FeePercent, string Fee, string Net, string Status, DateTime? SentAt, string? BankDisplay);

public sealed record SaleWithPayoutView(Guid Id, string Status, SalePayoutView? Payout);
