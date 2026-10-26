// Acceptance Test
// Traces to: L2-019, L2-021
// Description: A seller's "My listings" shows every listing they own in each status, newest first,
// with the cover, title, price, status and date, the count per status, and a filter by status.
// A sold row carries its order so the page can open the sale.

using System.Net;
using System.Net.Http.Json;
using TutuMuch.Api.Tests.Support;

namespace TutuMuch.Api.Tests;

public sealed class MyListingsTests : IDisposable
{
    private readonly TutuMuchApiFactory _factory = new();
    private readonly HttpClient _buyer;

    public MyListingsTests()
    {
        _buyer = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task Every_listing_of_the_seller_is_shown_newest_first_with_counts()
    {
        var (seller, orderId) = await SellerWithSoldDraftsAndActiveAsync();

        var page = await seller.GetFromJsonAsync<MyListingsView>("/api/me/listings");

        Assert.Equal(["Draft", "Active", "Draft", "Sold"], page!.Items.Select(i => i.Status));
        var untitled = page.Items[0];
        Assert.Null(untitled.Title);
        Assert.Equal("Tops", untitled.Category);
        Assert.Null(untitled.Price);
        Assert.Null(untitled.Photo);
        var active = page.Items[1];
        Assert.Equal("Rainbow tulle tutu, pink", active.Title);
        Assert.Equal(60.00m, active.Price);
        Assert.NotNull(active.Photo);
        Assert.Null(active.SaleOrderId);
        var sold = page.Items[3];
        Assert.Equal("Floral party dress, pink", sold.Title);
        Assert.Equal(orderId, sold.SaleOrderId);
        Assert.Equal(new MyListingCountsView(4, 2, 1, 0, 1, 0), page.Counts);
    }

    [Fact]
    public async Task Filtering_by_sold_shows_only_the_sold_listing_and_keeps_every_count()
    {
        var (seller, _) = await SellerWithSoldDraftsAndActiveAsync();

        var page = await seller.GetFromJsonAsync<MyListingsView>("/api/me/listings?status=Sold");

        var row = Assert.Single(page!.Items);
        Assert.Equal("Sold", row.Status);
        Assert.Equal(4, page.Counts.All);
    }

    [Fact]
    public async Task A_seller_with_no_listings_gets_an_empty_page()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        _factory.SeedActiveListing();

        var page = await _buyer.GetFromJsonAsync<MyListingsView>("/api/me/listings");

        Assert.Empty(page!.Items);
        Assert.Equal(new MyListingCountsView(0, 0, 0, 0, 0, 0), page.Counts);
    }

    [Fact]
    public async Task An_unknown_status_is_refused()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);

        using var response = await _buyer.GetAsync("/api/me/listings?status=Lost");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_FAILED", (await Problem.ReadAsync(response)).Code);
    }

    [Fact]
    public async Task A_visitor_cannot_read_my_listings()
    {
        using var response = await _buyer.GetAsync("/api/me/listings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// A seller whose party dress was bought, who then saved a titled draft, published a listing and
    /// saved an untitled draft, in that order. Returns the seller and the order of the sold listing.
    /// </summary>
    private async Task<(HttpClient Seller, Guid OrderId)> SellerWithSoldDraftsAndActiveAsync()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        var draft = await seller.CreateDraftAsync(new { category = "Dresses", title = "Smocked romper, yellow" });
        var active = await seller.CreateDraftWithPhotoAsync(Listings.Complete());
        (await seller.PublishAsync(active)).EnsureSuccessStatusCode();
        var untitled = await seller.CreateDraftAsync(new { category = "Tops" });
        CreatedMinutesAgo("L-30001", 40);
        CreatedMinutesAgo(draft, 30);
        CreatedMinutesAgo(active, 20);
        CreatedMinutesAgo(untitled, 10);
        return (seller, orderId);
    }

    private void CreatedMinutesAgo(string reference, int minutes) =>
        _factory.InDatabase(db =>
        {
            var listing = db.Listings.Single(l => l.Reference == reference);
            db.Entry(listing).Property(l => l.CreatedAt).CurrentValue = _factory.Clock.GetUtcNow().UtcDateTime.AddMinutes(-minutes);
            db.SaveChanges();
        });

    public void Dispose()
    {
        _buyer.Dispose();
        _factory.Dispose();
    }
}

public sealed record MyListingPhotoView(int Width, int Height, string Src, string Webp, string Jpeg);

public sealed record MyListingRowView(string Reference, string Status, string? Title, string Category, decimal? Price, MyListingPhotoView? Photo, DateTime CreatedAt, Guid? SaleOrderId);

public sealed record MyListingCountsView(int All, int Draft, int Active, int Reserved, int Sold, int Removed);

public sealed record MyListingsView(IReadOnlyList<MyListingRowView> Items, MyListingCountsView Counts);
