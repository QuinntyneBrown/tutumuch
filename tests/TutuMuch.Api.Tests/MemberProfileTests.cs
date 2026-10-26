// Acceptance Test
// Traces to: L2-040, L2-041, L2-054
// Description: Anyone can open a member's public profile by display name, ignoring case, and
// page through their Active listings 24 at a time. The response holds public fields only;
// an unknown or closed member is one uniform 404 MEMBER_UNAVAILABLE.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TutuMuch.Api.Tests.Support;
using TutuMuch.Domain.Listings;
using TutuMuch.Domain.Members;
using TutuMuch.Domain.Orders;

namespace TutuMuch.Api.Tests;

public sealed class MemberProfileTests : IDisposable
{
    private readonly TutuMuchApiFactory _factory = new();
    private readonly HttpClient _client;
    private readonly DateTime _joined = new(2025, 1, 14, 9, 30, 0, DateTimeKind.Utc);
    private readonly Member _marcus;
    private long _number = 40000;

    public MemberProfileTests()
    {
        _client = _factory.CreateMemberClient();
        _marcus = Member.Register("marcus_12", "marcus.bell@example.com", "hash", _joined);
        _factory.InDatabase(db =>
        {
            db.Members.Add(_marcus);
            db.SaveChanges();
        });
    }

    [Fact]
    public async Task A_visitor_sees_the_public_profile_with_no_reviews_yet()
    {
        Seed(ListingStatus.Active);
        Seed(ListingStatus.Active);
        Seed(ListingStatus.Sold);

        using var response = await _client.GetAsync("/api/members/MARCUS_12");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = (await response.Content.ReadFromJsonAsync<Profile>())!;
        Assert.Equal(_marcus.Id, profile.MemberId);
        Assert.Equal("marcus_12", profile.DisplayName);
        Assert.Equal(new DateOnly(2025, 1, 1), profile.MemberSince);
        Assert.Null(profile.Rating.Average);
        Assert.Equal(0, profile.Rating.Count);
        Assert.Equal(0, profile.CompletedSales);
        Assert.Equal(2, profile.ActiveListingCount);
    }

    [Fact]
    public async Task Completed_sales_count_only_completed_orders_as_seller()
    {
        using var buyer = _factory.CreateMemberClient();
        await buyer.SignInVerifiedMemberAsync(_factory, "jordan_l", "jordan@example.com");
        var (completed, _) = await _factory.SeedPaidOrderAsync(buyer, 30001);
        await _factory.SeedPaidOrderAsync(buyer, 30002);
        string seller = string.Empty;
        _factory.InDatabase(db =>
        {
            var order = db.Orders.Single(o => o.Id == completed);
            db.Entry(order).Property(o => o.Status).CurrentValue = OrderStatus.Completed;
            db.SaveChanges();
            seller = db.Members.Single(m => m.Id == order.SellerId).DisplayName;
        });

        var profile = (await _client.GetFromJsonAsync<Profile>($"/api/members/{seller}"))!;

        Assert.Equal(1, profile.CompletedSales);
        var detail = await _client.GetFromJsonAsync<JsonElement>($"/api/catalog/listings/{Listing.ReferenceFor(30001)}");
        Assert.Equal(1, detail.GetProperty("seller").GetProperty("completedSalesCount").GetInt32());
    }

    [Fact]
    public async Task The_profile_response_holds_public_fields_only()
    {
        using var response = await _client.GetAsync("/api/members/marcus_12");

        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("@", json);
        Assert.DoesNotContain("hash", json, StringComparison.OrdinalIgnoreCase);
        using var document = JsonDocument.Parse(json);
        var names = document.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet();
        Assert.Subset(
            new HashSet<string> { "memberId", "displayName", "avatarUrl", "bio", "publicLocation", "memberSince", "rating", "completedSales", "activeListingCount" },
            names);
    }

    [Fact]
    public async Task Unknown_and_closed_members_are_the_same_404()
    {
        _factory.InDatabase(db =>
        {
            db.Members.Single(m => m.Id == _marcus.Id).Close(DateTime.UtcNow);
            db.SaveChanges();
        });

        using var closed = await _client.GetAsync("/api/members/marcus_12");
        using var unknown = await _client.GetAsync("/api/members/nobody_here");
        using var closedListings = await _client.GetAsync("/api/members/marcus_12/listings");

        Assert.Equal(HttpStatusCode.NotFound, closed.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, closedListings.StatusCode);
        var closedProblem = await Problem.ReadAsync(closed);
        Assert.Equal("MEMBER_UNAVAILABLE", closedProblem.Code);
        Assert.Equal(closedProblem with { CorrelationId = null }, (await Problem.ReadAsync(unknown)) with { CorrelationId = null });
        Assert.Contains("no-store", closed.Headers.CacheControl!.ToString());
    }

    [Fact]
    public async Task Active_listings_page_24_at_a_time_newest_first()
    {
        for (var i = 0; i < 26; i++)
        {
            Seed(ListingStatus.Active, _joined.AddDays(i));
        }

        Seed(ListingStatus.Reserved, _joined.AddDays(40));

        var first = (await _client.GetFromJsonAsync<ListingsPage>("/api/members/marcus_12/listings"))!;
        var second = (await _client.GetFromJsonAsync<ListingsPage>("/api/members/marcus_12/listings?page=2"))!;

        Assert.Equal(26, first.TotalCount);
        Assert.Equal(24, first.PageSize);
        Assert.Equal(24, first.Items.Count);
        Assert.Equal(2, second.Items.Count);
        Assert.True(first.Items[0].PublishedAt > first.Items[1].PublishedAt);
        Assert.All(first.Items.Concat(second.Items), card => Assert.Equal("Active", card.Status));
    }

    [Fact]
    public async Task A_page_below_one_is_refused()
    {
        using var response = await _client.GetAsync("/api/members/marcus_12/listings?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_FAILED", (await Problem.ReadAsync(response)).Code);
    }

    private void Seed(ListingStatus status, DateTime? publishedAt = null)
    {
        var at = publishedAt ?? _joined;
        var details = new ListingDetails("Shoes", "Sneakers", null, "Air Max 90, white", "Light creasing, soles have plenty left.", "Nike", "Used - Good", null, 85m, 8m);
        var listing = Listing.CreateDraft(_marcus.Id, ++_number, details, at);
        Assert.Empty(listing.Publish(sizeConfirmed: true, at, photoCount: 1));
        _factory.InDatabase(db =>
        {
            db.Listings.Add(listing);
            if (status != ListingStatus.Active)
            {
                db.Entry(listing).Property(l => l.Status).CurrentValue = status;
            }

            db.SaveChanges();
        });
    }

    private sealed record Rating(decimal? Average, int Count);

    private sealed record Profile(
        Guid MemberId, string DisplayName, string? AvatarUrl, string? Bio, string? PublicLocation, DateOnly MemberSince,
        Rating Rating, int CompletedSales, int ActiveListingCount);

    private sealed record Card(string Reference, string Status, DateTime PublishedAt);

    private sealed record ListingsPage(List<Card> Items, int Page, int PageSize, int TotalCount);

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
