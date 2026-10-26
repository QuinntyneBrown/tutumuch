// Acceptance Test
// Traces to: L2-023, L2-024
// Description: A visitor narrows the catalog by keyword, category, subcategory, brand, condition
// and price, and sorts it; the panel's live count and the brand list come from the same rules.

using System.Net;
using System.Net.Http.Json;
using TutuMuch.Api.Tests.Support;
using TutuMuch.Domain.Listings;
using TutuMuch.Domain.Members;

namespace TutuMuch.Api.Tests;

public sealed class SearchAndFilterCatalogTests : IDisposable
{
    private readonly TutuMuchApiFactory _factory = new();
    private readonly HttpClient _client;
    private readonly DateTime _start = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _sellerId;
    private long _number = 10000;
    private int _minutes;

    public SearchAndFilterCatalogTests()
    {
        _client = _factory.CreateMemberClient();
        var seller = Member.Register("marcus_12", "marcus.bell@example.com", "hash", _start);
        _factory.InDatabase(db =>
        {
            db.Members.Add(seller);
            db.SaveChanges();
        });
        _sellerId = seller.Id;
    }

    [Fact]
    public async Task A_keyword_matches_title_words_in_any_case()
    {
        var airMax = Seed("Air Max 90 white");
        Seed("Samba OG", brand: "Adidas");

        Assert.Equal([airMax], await References("q=AIR%20max"));
    }

    [Fact]
    public async Task A_keyword_matches_the_brand_ignoring_case()
    {
        var fleece = Seed("Retro fleece", brand: "Patagonia");
        Seed("Air Max 90 white");

        Assert.Equal([fleece], await References("q=PATAGONIA"));
    }

    [Fact]
    public async Task Title_and_brand_matches_rank_above_description_matches()
    {
        var inTitle = Seed("Blundstone boots", description: "Worn twice, still like new.");
        var inDescription = Seed("Chelsea leather", description: "Comfortable boots, worn twice.");
        Seed("Samba OG", brand: "Adidas");

        // inDescription is newer, so only the match tier puts inTitle first.
        Assert.Equal([inTitle, inDescription], await References("q=boots"));
    }

    [Fact]
    public async Task A_keyword_longer_than_100_characters_is_truncated_before_searching()
    {
        var long96 = new string('z', 96);
        var airMax = Seed("Air Max 90 white", description: $"Code {long96} on the tongue.");
        var term = $"air {long96}q";

        Assert.Equal([airMax], await References($"q={Uri.EscapeDataString(term)}"));
    }

    [Fact]
    public async Task Category_and_condition_combine_with_and()
    {
        var match = Seed("Samba OG", condition: "New with tags");
        Seed("Air Max 90 white", condition: "Used - Good");
        Seed("Oxford shirt", category: "Tops", subcategory: "Shirts", condition: "New with tags");

        Assert.Equal([match], await References("category=shoes&condition=new-with-tags"));
    }

    [Fact]
    public async Task Subcategories_and_brands_each_combine_with_or()
    {
        var nike = Seed("Air Max 90 white", brand: "Nike");
        var adidas = Seed("Samba OG", brand: "Adidas");
        Seed("Retro fleece", brand: "Patagonia");
        var boots = Seed("Iron Ranger", brand: "Red Wing", subcategory: "Boots");
        Seed("Dress Oxford", brand: "Allen Edmonds", subcategory: "Dress shoes");

        Assert.Equal([adidas, nike], await References("brand=Nike&brand=adidas"));
        Assert.Equal([boots], await References("category=shoes&subcategory=boots"));
    }

    [Fact]
    public async Task A_price_range_is_inclusive_and_price_sorts_ascending_and_descending()
    {
        var twenty = Seed("Twenty", price: 20m);
        var fifty = Seed("Fifty", price: 50m);
        var middle = Seed("Middle", price: 35.5m);
        Seed("Too cheap", price: 19.99m);
        Seed("Too dear", price: 50.01m);

        Assert.Equal([twenty, middle, fifty], await References("min=20&max=50&sort=price-asc"));
        Assert.Equal([fifty, middle, twenty], await References("min=20&max=50&sort=price-desc"));
    }

    [Fact]
    public async Task A_minimum_above_the_maximum_is_refused()
    {
        using var response = await _client.GetAsync("/api/catalog/listings?min=200&max=50");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("CATALOG_PRICE_RANGE_INVALID", problem.Code);
        Assert.Equal("Minimum price must be lower than maximum", problem.Title);
    }

    [Theory]
    [InlineData("category=hats")]
    [InlineData("category=tops&subcategory=boots")]
    [InlineData("subcategory=boots")]
    [InlineData("condition=mint")]
    [InlineData("sort=cheapest")]
    [InlineData("min=-1")]
    [InlineData("max=12.345")]
    [InlineData("min=ten")]
    public async Task An_unknown_or_malformed_filter_is_refused(string query)
    {
        using var response = await _client.GetAsync($"/api/catalog/listings?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("CATALOG_FILTER_INVALID", (await Problem.ReadAsync(response)).Code);
    }

    [Fact]
    public async Task The_count_applies_the_same_filters()
    {
        Seed("Samba OG", condition: "New with tags");
        Seed("Air Max 90 white", condition: "New with tags");
        Seed("Old pair", condition: "Used - Fair");
        Seed("Unpublished draft", condition: "New with tags", publish: false);

        var count = await _client.GetFromJsonAsync<CountDto>("/api/catalog/listings/count?condition=new-with-tags");

        Assert.Equal(2, count!.TotalCount);
        using var refused = await _client.GetAsync("/api/catalog/listings/count?min=9&max=1");
        Assert.Equal("CATALOG_PRICE_RANGE_INVALID", (await Problem.ReadAsync(refused)).Code);
    }

    [Fact]
    public async Task Brands_of_visible_listings_are_listed_by_count_then_name()
    {
        Seed("Air Max 90 white", brand: "Nike");
        Seed("Air Force 1", brand: "Nike");
        Seed("Samba OG", brand: "Adidas");
        Seed("Retro fleece", brand: "Patagonia");
        Seed("Unpublished", brand: "Zara", publish: false);

        var brands = await _client.GetFromJsonAsync<List<BrandDto>>("/api/catalog/brands");

        Assert.Equal([new BrandDto("Nike", 2), new BrandDto("Adidas", 1), new BrandDto("Patagonia", 1)], brands);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<List<string>> References(string query)
    {
        using var response = await _client.GetAsync($"/api/catalog/listings?{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = (await response.Content.ReadFromJsonAsync<CatalogPage>())!;
        return page.Items.Select(i => i.Reference).ToList();
    }

    private string Seed(
        string title,
        string brand = "Nike",
        string description = "Light creasing, soles have plenty left.",
        string category = "Shoes",
        string subcategory = "Sneakers",
        string condition = "Used - Good",
        decimal price = 85m,
        bool publish = true)
    {
        var now = _start.AddMinutes(++_minutes);
        var number = ++_number;
        var details = new ListingDetails(category, subcategory, null, title, description, brand, condition, condition == "Used - Fair" ? "Scuffed toe box." : null, price, 8m);
        var listing = Listing.CreateDraft(_sellerId, number, details, now);
        var hash = new string('a', 64);
        var versions = new[] { 400, 800, 1600 }.SelectMany(size => new[] { PhotoFormat.WebP, PhotoFormat.Jpeg }.Select(format =>
            new ListingPhotoVersion(size, format, Math.Min(size, 1200), Math.Min(size, 1200) * 4 / 3, ListingPhotoVersion.KeyFor(hash, size, format), 1000)));
        var photo = ListingPhoto.Create(Guid.NewGuid(), _sellerId, "image/jpeg", 1000, hash, 1200, 1600, versions, now);
        photo.AttachTo(listing.Id, 0);
        if (publish)
        {
            Assert.Empty(listing.Publish(sizeConfirmed: true, now, photoCount: 1));
        }

        _factory.InDatabase(db =>
        {
            db.Listings.Add(listing);
            db.ListingPhotos.Add(photo);
            db.SaveChanges();
        });
        return listing.Reference;
    }

    private sealed record CountDto(int TotalCount);

    private sealed record BrandDto(string Name, int ListingCount);

    private sealed record CatalogPage(List<Card> Items, int Page, int PageSize, int TotalCount);

    private sealed record Card(string Reference, string Title);
}
