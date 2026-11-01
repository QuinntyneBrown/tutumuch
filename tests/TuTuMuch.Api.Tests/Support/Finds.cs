using System.Globalization;
using System.Net.Http.Json;
using TuTuMuch.Domain.Members;

namespace TuTuMuch.Api.Tests.Support;

/// <summary>Bodies, calls and seeding for shelf finds and grabs (L2-113 to L2-121).</summary>
public static class Finds
{
    public static string Day(this TuTuMuchApiFactory factory, int days = 0) =>
        DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime).AddDays(days).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static object Find(
        TuTuMuchApiFactory factory,
        string title = "Glitter ballet flats, pink",
        string category = "Dance",
        decimal? shelfPrice = 40.00m,
        string storeName = "Target",
        string storeAddress = "88 Atlantic Ave, Brooklyn, NY",
        int seenDaysAgo = 1,
        string? note = "On the clearance end cap",
        decimal? grabFee = null) =>
        new { title, category, shelfPrice, storeName, storeAddress, seenOn = factory.Day(-seenDaysAgo), note, grabFee };

    public static Task<HttpResponseMessage> PostFindAsync(this HttpClient client, object body) =>
        client.PostAsJsonAsync("/api/finds", body);

    public static async Task<FindView> PostedFindAsync(this HttpClient client, object body)
    {
        using var response = await client.PostFindAsync(body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FindView>())!;
    }

    /// <summary>A verified member signed in on a new client, without the email round trip.</summary>
    public static HttpClient CreateVerifiedClient(this TuTuMuchApiFactory factory, string displayName)
    {
        Member member = null!;
        factory.EnsureDatabase();
        factory.InDatabase(db =>
        {
            member = Member.Register(displayName, $"{displayName}@example.com", "hash", DateTime.UtcNow);
            member.MarkEmailVerified(DateTime.UtcNow);
            db.Members.Add(member);
            db.SaveChanges();
        });
        return factory.CreateMemberClient().WithSession(factory, member);
    }

    public static Task<HttpResponseMessage> QuoteGrabAsync(this HttpClient client, string reference, object? address = null) =>
        client.PostAsJsonAsync("/api/grabs/quote", new { findReference = reference, address = address ?? Checkouts.Brooklyn() });

    public static Task<HttpResponseMessage> StartGrabAsync(this HttpClient client, string reference, string expectedTotal, bool acknowledged = true, Guid? key = null, string token = "tok_visa")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/grabs")
        {
            Content = JsonContent.Create(new
            {
                findReference = reference,
                address = Checkouts.Brooklyn(),
                depositAcknowledged = acknowledged,
                paymentMethodToken = token,
                expectedTotal = decimal.Parse(expectedTotal, CultureInfo.InvariantCulture),
            }),
        };
        request.Headers.Add("Idempotency-Key", (key ?? Guid.NewGuid()).ToString());
        return client.SendAsync(request);
    }

    /// <summary>Quotes and pays; returns the PaymentPending grab.</summary>
    public static async Task<GrabView> PaidGrabAsync(this HttpClient client, string reference)
    {
        using var quote = await client.QuoteGrabAsync(reference);
        quote.EnsureSuccessStatusCode();
        var total = (await quote.Content.ReadFromJsonAsync<GrabQuoteView>())!.Total;
        using var start = await client.StartGrabAsync(reference, total);
        start.EnsureSuccessStatusCode();
        return (await start.Content.ReadFromJsonAsync<GrabView>())!;
    }

    /// <summary>Pays and confirms by webhook; returns the Requested grab.</summary>
    public static async Task<GrabView> RequestedGrabAsync(this HttpClient client, TuTuMuchApiFactory factory, string reference)
    {
        var grab = await client.PaidGrabAsync(reference);
        (await factory.CreateMemberClient().PostWebhookAsync(FakePaymentProvider.Webhook(PaymentIdOf(factory, grab.Id)))).EnsureSuccessStatusCode();
        return (await client.GetFromJsonAsync<GrabView>($"/api/me/grabs/{grab.Id}"))!;
    }

    public static Guid PaymentIdOf(TuTuMuchApiFactory factory, Guid grabId)
    {
        Guid id = Guid.Empty;
        factory.InDatabase(db => id = db.Payments.Single(p => p.GrabId == grabId).Id);
        return id;
    }

    public static Task<HttpResponseMessage> AdminActAsync(this HttpClient admin, Guid grabId, string action, string? reason = null) =>
        action == "unavailable"
            ? admin.PostAsJsonAsync($"/api/admin/grabs/{grabId}/unavailable", new { reason })
            : admin.PostAsync($"/api/admin/grabs/{grabId}/{action}", null);
}

public sealed record FindView(
    string Reference,
    string Title,
    string Category,
    string ShelfPrice,
    string StoreName,
    string StoreAddress,
    string SeenOn,
    string ExpiresOn,
    string? Note,
    string Status,
    string PosterDisplayName,
    string? GrabFee,
    bool GrabOpen,
    bool Mine);

public sealed record FindCardView(string Reference, string Title, string Category, string ShelfPrice, string StoreName, string SeenOn, bool GrabAvailable);

public sealed record FindPageView(IReadOnlyList<FindCardView> Items, int Page, int PageSize, int TotalCount);

public sealed record GrabQuoteView(string FindReference, string FindTitle, string StoreName, string ShelfPrice, string GrabFee, string SalesTax, string Total, string Deposit, string RefundAfterAcceptance);

public sealed record GrabView(
    Guid Id,
    string Reference,
    string Status,
    string FindReference,
    string Total,
    string Deposit,
    string? RefundIfCancelled,
    string? RefundedAmount,
    string? UnavailableReason,
    string? PaymentDeclineReason,
    DateTime? AnswerBy,
    DateTime? BuyBy);

public sealed record AdminGrabView(Guid Id, string Reference, string Status, string ShopperDisplayName, string ShopperEmail, string Total, string Deposit, DateTime? Deadline, object? Address);
