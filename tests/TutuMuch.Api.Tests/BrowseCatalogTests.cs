// Acceptance Test
// Traces to: L2-022, L2-059
// Description: Anyone can page through the Active and Reserved listings, newest first, 24 at a
// time, as cards whose cover photo is served at 400, 800 and 1600 px in WebP and JPEG.

using System.Net;
using System.Net.Http.Json;
using TutuMuch.Api.Tests.Support;
using TutuMuch.Domain.Listings;
using TutuMuch.Domain.Members;

namespace TutuMuch.Api.Tests;

public sealed class BrowseCatalogTests : IDisposable
{
    private readonly TutuMuchApiFactory _factory = new();
    private readonly HttpClient _client;
    private readonly DateTime _start = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _sellerId;
    private long _number = 10000;

    public BrowseCatalogTests()
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
    public async Task A_visitor_sees_active_listings_newest_first_as_cards()
    {
        var older = Seed("Floral party dress, pink", minutes: 1);
        var newer = Seed("Oxford shirt, blue", minutes: 2, category: "Tops", subcategory: "Shirts", price: 18.50m);
        Seed("Unpublished draft", minutes: 3, publish: false);

        using var response = await _client.GetAsync("/api/catalog/listings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = (await response.Content.ReadFromJsonAsync<CatalogPage>())!;
        Assert.Equal(1, page.Page);
        Assert.Equal(24, page.PageSize);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal([newer, older], page.Items.Select(i => i.Reference));
        var card = page.Items[0];
        Assert.Equal("Oxford shirt, blue", card.Title);
        Assert.Equal("Gymboree", card.Brand);
        Assert.Equal("Tops", card.Category);
        Assert.Equal("Girls", card.SizeLabel);
        Assert.Equal("Used - Good", card.Condition);
        Assert.Equal("18.50", card.Price);
        Assert.Equal("Active", card.Status);
        Assert.Equal("Photo 1 of Gymboree Oxford shirt, blue", card.CoverPhoto.Alt);
        Assert.Equal(1200, card.CoverPhoto.Width);
        Assert.Equal(1600, card.CoverPhoto.Height);
        var hash = new string('a', 64);
        Assert.Equal($"/photos/{hash}/400.webp 400w, /photos/{hash}/800.webp 800w, /photos/{hash}/1600.webp 1200w", card.CoverPhoto.Webp);
        Assert.Equal($"/photos/{hash}/400.jpg 400w, /photos/{hash}/800.jpg 800w, /photos/{hash}/1600.jpg 1200w", card.CoverPhoto.Jpeg);
        Assert.Equal($"/photos/{hash}/400.jpg", card.CoverPhoto.Src);
        Assert.Equal("Girls", page.Items[1].SizeLabel);
    }

    [Fact]
    public async Task Pages_hold_24_cards_and_the_second_page_continues_the_first()
    {
        for (var i = 0; i < 30; i++)
        {
            Seed($"Listing number {i}", minutes: i);
        }

        var first = (await _client.GetFromJsonAsync<CatalogPage>("/api/catalog/listings"))!;
        var second = (await _client.GetFromJsonAsync<CatalogPage>("/api/catalog/listings?page=2"))!;

        Assert.Equal(30, first.TotalCount);
        Assert.Equal(24, first.Items.Count);
        Assert.Equal(6, second.Items.Count);
        Assert.Equal("Listing number 29", first.Items[0].Title);
        Assert.Equal("Listing number 0", second.Items[^1].Title);
        Assert.Empty(first.Items.Select(i => i.Reference).Intersect(second.Items.Select(i => i.Reference)));
    }

    [Fact]
    public async Task A_page_size_above_48_is_capped_at_48()
    {
        for (var i = 0; i < 50; i++)
        {
            Seed($"Listing number {i}", minutes: i);
        }

        var page = (await _client.GetFromJsonAsync<CatalogPage>("/api/catalog/listings?pageSize=100"))!;

        Assert.Equal(48, page.PageSize);
        Assert.Equal(48, page.Items.Count);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    public async Task A_page_or_page_size_below_1_is_400(string query)
    {
        using var response = await _client.GetAsync($"/api/catalog/listings?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_FAILED", (await Problem.ReadAsync(response)).Code);
    }

    [Fact]
    public async Task A_category_narrows_the_catalog()
    {
        Seed("Floral party dress, pink", minutes: 1);
        var shirt = Seed("Oxford shirt, blue", minutes: 2, category: "Tops", subcategory: "Shirts");

        var page = (await _client.GetFromJsonAsync<CatalogPage>("/api/catalog/listings?category=Tops"))!;

        Assert.Equal(1, page.TotalCount);
        Assert.Equal([shirt], page.Items.Select(i => i.Reference));
    }

    [Fact]
    public async Task An_unknown_category_is_400()
    {
        using var response = await _client.GetAsync("/api/catalog/listings?category=Hats");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("CATALOG_FILTER_INVALID", (await Problem.ReadAsync(response)).Code);
    }

    private string Seed(string title, int minutes, string category = "Dresses", string subcategory = "Dresses", decimal price = 85m, bool publish = true)
    {
        var now = _start.AddMinutes(minutes);
        var number = ++_number;
        var details = new ListingDetails(category, subcategory, null, title, "Light wear, colours still bright.", "Gymboree", "Used - Good", null, price, 8m);
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

    private sealed record CatalogPage(List<Card> Items, int Page, int PageSize, int TotalCount);

    private sealed record Card(
        Guid Id, string Reference, string Title, string Brand, string Category, string? Subcategory, string SizeLabel,
        string Condition, string Price, string Status, DateTime PublishedAt, CoverPhoto CoverPhoto);

    private sealed record CoverPhoto(int Width, int Height, string Alt, string Src, string Webp, string Jpeg);

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
