// Acceptance Test
// Traces to: L2-011, L2-016, L2-031
// Description: The accepted categories, their sizes and the selling options are served by TuTuMuch API
// to anyone, so the form never hard-codes them.

using System.Net;
using System.Net.Http.Json;
using TuTuMuch.Api.Tests.Support;

namespace TuTuMuch.Api.Tests;

public sealed class CatalogueTests : IDisposable
{
    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _client;

    public CatalogueTests()
    {
        _client = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task Categories_are_the_4_categories_and_12_subcategories_with_their_sizes()
    {
        using var response = await _client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("public, max-age=3600", response.Headers.CacheControl?.ToString());
        var categories = (await response.Content.ReadFromJsonAsync<List<CategoryResponse>>())!;
        Assert.Equal(["Dresses", "Tops", "Outerwear", "Bottoms"], categories.Select(c => c.Name));
        Assert.Equal(13, categories.Sum(c => c.Subcategories.Count));
        Assert.Equal(["Girls'", "Girls'", "Girls'", "Girls'"], categories.Select(c => c.AcceptedSize));
        Assert.Equal(["Girls", "Girls", "Girls", "Girls"], categories.Select(c => c.SizeLabel));
    }

    [Fact]
    public async Task Selling_options_carry_the_fee_conditions_and_brands()
    {
        var options = await _client.GetFromJsonAsync<SellingOptionsResponse>("/api/selling/options");

        Assert.Equal(10m, options!.FeePercent);
        Assert.Equal(["New with tags", "New without tags", "Used - Excellent", "Used - Good", "Used - Fair"], options.Conditions);
        Assert.Contains("Gymboree", options.SuggestedBrands);
        Assert.Equal(8, options.MaxPhotos);
    }

    private sealed record CategoryResponse(string Name, List<string> Subcategories, string AcceptedSize, string SizeLabel);

    private sealed record SellingOptionsResponse(decimal FeePercent, List<string> Conditions, List<string> SuggestedBrands, int MaxPhotos);

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
