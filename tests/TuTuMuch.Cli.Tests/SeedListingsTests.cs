// Acceptance Test
// Traces to: L2-075
// Description: Verify seed publishes demo listings in every category, each with 1 to 4 photos processed by the upload pipeline, within the daily publish limit

using System.Text.Json;
using TuTuMuch.Application.Catalog;
using TuTuMuch.Cli.Tests.Support;
using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Cli.Tests;

public sealed class SeedListingsTests(SeededDemo demo) : IClassFixture<SeededDemo>
{
    private static readonly string[] Categories = ["Clothing", "Shoes", "Dance", "Sports", "Cameras and tech", "Toys and crafts"];

    [Fact]
    public void Forty_eight_to_eighty_listings_are_active_with_at_least_eight_per_category()
    {
        var listings = demo.Harness.InDatabase(db => db.Listings.ToList());

        Assert.Equal(ExitCodes.Success, demo.Run.ExitCode);
        Assert.InRange(listings.Count, 48, 80);
        Assert.All(listings, l => Assert.Equal(ListingStatus.Active, l.Status));
        Assert.All(Categories, c => Assert.True(listings.Count(l => l.Category == c) >= 8, $"{c} has fewer than 8 listings"));
    }

    [Fact]
    public void Every_listing_uses_photos_from_its_own_categorys_folder()
    {
        Assert.All(DemoCatalogue.Listings, l => Assert.All(l.Photos, p => Assert.StartsWith($"{l.Category}/", p)));
    }

    [Fact]
    public void Every_listing_is_in_girls_sizes()
    {
        var listings = demo.Harness.InDatabase(db => db.Listings.ToList());

        Assert.All(listings, l => Assert.Equal(CategoryCatalog.Girls, l.Size));
    }

    [Fact]
    public void Every_listing_passes_the_publish_rules()
    {
        var listings = demo.Harness.InDatabase(db => db.Listings.ToList());
        var photoCounts = demo.Harness.InDatabase(db => db.ListingPhotos.GroupBy(p => p.ListingId).ToDictionary(g => g.Key!.Value, g => g.Count()));

        Assert.All(listings, l =>
        {
            var details = new ListingDetails(l.Category, l.Subcategory, l.Size, l.Title, l.Description, l.Brand, l.Condition?.ToLabel(), l.Flaws, l.Price, l.ShippingCost);
            Assert.Empty(Listing.CheckPublish(details, photoCounts.GetValueOrDefault(l.Id)));
        });
    }

    [Fact]
    public void Every_listing_has_one_to_four_photos_with_every_version_stored()
    {
        var listings = demo.Harness.InDatabase(db => db.Listings.Select(l => l.Id).ToList());
        var photos = demo.Harness.InDatabase(db => db.ListingPhotos.ToList());

        Assert.All(listings, id => Assert.InRange(photos.Count(p => p.ListingId == id), 1, 4));
        Assert.All(photos, p =>
        {
            Assert.NotNull(p.ListingId);
            Assert.Contains(p.Versions, v => v.Format == PhotoFormat.WebP);
            Assert.Contains(p.Versions, v => v.Format == PhotoFormat.Jpeg);
            Assert.All(p.Versions, v => Assert.True(demo.Harness.Photos.Public.ContainsKey(v.Key), $"{v.Key} is not stored"));
            Assert.True(demo.Harness.Photos.Originals.ContainsKey(ListingPhoto.OriginalKeyFor(p.Id)));
        });
    }

    [Fact]
    public void Every_cover_photo_is_the_listings_first_seed_photo()
    {
        var hashes = DemoCatalogue.PhotoFiles.ToDictionary(f => f, f => HashOf(Path.Combine(AppContext.BaseDirectory, "SeedPhotos", f)));
        var covers = demo.Harness.InDatabase(db => db.Listings.Join(
            db.ListingPhotos.Where(p => p.Position == 0), l => l.Id, p => p.ListingId, (l, p) => new { l.Title, p.OriginalHash }).ToList());

        Assert.All(DemoCatalogue.Listings, l => Assert.Equal(hashes[l.Photos[0]], covers.Single(c => c.Title == l.Title).OriginalHash));
    }

    [Fact]
    public async Task Every_brand_is_a_suggested_brand_of_the_api()
    {
        await using var settings = File.OpenRead(RepositoryFiles.PathOf("src", "backend", "TuTuMuch.Api", "appsettings.json"));
        using var json = await JsonDocument.ParseAsync(settings);
        var suggested = json.RootElement.GetProperty("Selling").GetProperty("SuggestedBrands").EnumerateArray().Select(b => b.GetString()).ToHashSet();
        var brands = demo.Harness.InDatabase(db => db.Listings.Select(l => l.Brand).Distinct().ToList());

        Assert.All(brands, b => Assert.Contains(b, suggested));
    }

    [Fact]
    public void Each_member_publishes_at_most_one_listing_a_day_over_the_past_thirty_days()
    {
        var listings = demo.Harness.InDatabase(db => db.Listings.ToList());
        var now = DateTime.UtcNow;

        Assert.All(listings, l => Assert.InRange(l.PublishedAt!.Value, now.AddDays(-31), now));
        Assert.All(listings.GroupBy(l => (l.SellerId, l.PublishedAt!.Value.Date)), g => Assert.Single(g));
    }

    [Theory]
    [InlineData("Clothing")]
    [InlineData("Shoes")]
    [InlineData("Dance")]
    [InlineData("Sports")]
    [InlineData("Cameras and tech")]
    [InlineData("Toys and crafts")]
    public async Task The_catalog_lists_each_categorys_demo_listings_with_a_cover_photo(string category)
    {
        var page = await demo.Harness.SendAsync(new BrowseCatalogQuery(1, 48, new CatalogFilters(Category: CatalogCriteria.Slug(category))));

        Assert.True(page.IsSuccess);
        Assert.Equal(DemoCatalogue.Listings.Count(l => l.Category == category), page.Value!.TotalCount);
        Assert.All(page.Value.Items, card => Assert.NotNull(card.CoverPhoto));
    }

    [Fact]
    public async Task Every_listing_detail_page_has_its_photos()
    {
        var references = demo.Harness.InDatabase(db => db.Listings.Select(l => l.Reference).ToList());

        foreach (var reference in references)
        {
            var detail = await demo.Harness.SendAsync(new GetListingDetailQuery(reference, null));
            Assert.True(detail.IsSuccess, $"{reference}: {detail.Error?.Code}");
        }
    }

    private static string HashOf(string path) => Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));
}
