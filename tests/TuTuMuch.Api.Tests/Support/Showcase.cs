using System.Net.Http.Json;

namespace TuTuMuch.Api.Tests.Support;

/// <summary>Calls for showcase listings and showcase requests (L2-107 to L2-111).</summary>
public static class Showcase
{
    public static Task<HttpResponseMessage> SetShowcaseAsync(this HttpClient client, string reference, bool showcase) =>
        client.PutAsJsonAsync($"/api/admin/listings/{reference}/showcase", new { showcase });

    /// <summary>The administrator publishes a complete listing and marks it as a showcase listing; returns its reference.</summary>
    public static async Task<string> PublishShowcaseAsync(this HttpClient admin, string category = "Dance", string subcategory = "Tutus")
    {
        var reference = await admin.CreateDraftWithPhotoAsync(Listings.Complete(category, subcategory));
        (await admin.PublishAsync(reference)).EnsureSuccessStatusCode();
        (await admin.SetShowcaseAsync(reference, true)).EnsureSuccessStatusCode();
        return reference;
    }

    public static Task<HttpResponseMessage> SendShowcaseRequestAsync(this HttpClient client, string reference, string? note) =>
        client.PutAsJsonAsync($"/api/me/showcase-requests/{reference}", new { note });

    public static Task<HttpResponseMessage> MyShowcaseRequestAsync(this HttpClient client, string reference) =>
        client.GetAsync($"/api/me/showcase-requests/{reference}");

    public static Task<HttpResponseMessage> AdminShowcaseRequestsAsync(this HttpClient client) =>
        client.GetAsync("/api/admin/showcase-requests");
}

public sealed record ShowcaseView(string Reference, bool Showcase);

public sealed record ShowcaseRequestView(string ListingReference, string? Note, DateTime RequestedAt, DateTime UpdatedAt);

public sealed record AdminShowcaseRequestView(string ListingReference, string ListingTitle, string MemberDisplayName, string MemberEmail, string? Note, DateTime RequestedAt);

public sealed record ShowcaseDetailView(string Reference, string Status, bool Showcase);
