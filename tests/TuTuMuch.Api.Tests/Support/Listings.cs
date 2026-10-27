using System.Net.Http.Json;

namespace TuTuMuch.Api.Tests.Support;

/// <summary>Request bodies and calls for the listings endpoints.</summary>
public static class Listings
{
    public static Dictionary<string, object?> Complete(string category = "Clothing", string subcategory = "Dresses") => new()
    {
        ["category"] = category,
        ["subcategory"] = subcategory,
        ["title"] = "Rainbow tulle tutu, pink",
        ["description"] = "Light wear, colours still bright. Cleaned and ready to wear.",
        ["brand"] = "Gymboree",
        ["condition"] = "Used - Good",
        ["price"] = 60.00m,
        ["shippingCost"] = 8.00m,
    };

    public static Dictionary<string, object?> With(this Dictionary<string, object?> body, string key, object? value)
    {
        var copy = new Dictionary<string, object?>(body) { [key] = value };
        return copy;
    }

    public static async Task<string> CreateDraftAsync(this HttpClient client, object body)
    {
        using var response = await client.PostAsJsonAsync("/api/listings", body);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<MyListing>();
        return created!.Reference;
    }

    /// <summary>Uploads a photo and creates a draft that carries it, so the draft can be published.</summary>
    public static async Task<string> CreateDraftWithPhotoAsync(this HttpClient client, Dictionary<string, object?> body)
    {
        var photoId = await client.UploadValidPhotoAsync();
        return await client.CreateDraftAsync(body.With("photoIds", new[] { photoId }));
    }

    public static Task<HttpResponseMessage> PublishAsync(this HttpClient client, string reference) =>
        client.PostAsync($"/api/listings/{reference}/publish", content: null);
}

public sealed record MyListing(
    string Reference,
    string Status,
    string Category,
    string? Subcategory,
    string Size,
    string? Title,
    string? Description,
    string? Brand,
    string? Condition,
    string? Flaws,
    decimal? Price,
    decimal? ShippingCost,
    DateTime? PublishedAt,
    IReadOnlyList<PhotoUpload> Photos);

public sealed record PublishResult(string Reference, string Status, bool PayoutOnboardingRequired);
