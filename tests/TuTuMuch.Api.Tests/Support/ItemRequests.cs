using System.Net.Http.Json;

namespace TuTuMuch.Api.Tests.Support;

/// <summary>Request bodies and calls for the item request endpoints (L2-097).</summary>
public static class ItemRequests
{
    public static Dictionary<string, object?> Body(string category = "Dance", string keywords = "ballet slippers", decimal? maxPrice = null, string? condition = null) => new()
    {
        ["category"] = category,
        ["keywords"] = keywords,
        ["maxPrice"] = maxPrice,
        ["minimumCondition"] = condition,
    };

    public static async Task<ItemRequestView> CreateItemRequestAsync(this HttpClient client, object body)
    {
        using var response = await client.PostAsJsonAsync("/api/me/item-requests", body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ItemRequestView>())!;
    }

    public static async Task<MyItemRequestsView> ListItemRequestsAsync(this HttpClient client)
    {
        using var response = await client.GetAsync("/api/me/item-requests");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MyItemRequestsView>())!;
    }
}

public sealed record ItemRequestView(
    Guid Id,
    string Category,
    string Size,
    string Keywords,
    decimal? MaxPrice,
    string? MinimumCondition,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime ExpiresAt,
    string Status,
    int MatchCount,
    IReadOnlyList<MatchCardView> Matches);

public sealed record MatchCardView(string Reference, string Title, string Price, string Status);

public sealed record MyItemRequestsView(IReadOnlyList<ItemRequestView> Items, int OpenCount, int Limit);
