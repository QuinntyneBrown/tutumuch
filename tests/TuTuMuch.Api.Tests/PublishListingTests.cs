// Acceptance Test
// Traces to: L2-014, L2-015, L2-017, L2-016, L2-018, L2-019, L2-031, L2-051
// Description: A seller publishes a draft once the label is confirmed and every field is within
// its limits; every failing field is reported together; 20 publishes a UTC day are allowed.

using System.Net;
using System.Net.Http.Json;
using TuTuMuch.Api.Tests.Support;

namespace TuTuMuch.Api.Tests;

public sealed class PublishListingTests : IDisposable
{
    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _client;

    public PublishListingTests()
    {
        _client = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task A_complete_confirmed_listing_becomes_active_and_prompts_for_payouts()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        var reference = await _client.CreateDraftWithPhotoAsync(Listings.Complete("Clothing", "Tops"));

        using var response = await _client.PublishAsync(reference);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<PublishResult>())!;
        Assert.Equal(new PublishResult(reference, "Active", PayoutOnboardingRequired: true), result);
        _factory.InDatabase(db =>
        {
            var listing = db.Listings.Single();
            Assert.NotNull(listing.PublishedAt);
        });
    }

    [Fact]
    public async Task A_cameras_and_tech_listing_publishes_for_girls()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        var reference = await _client.CreateDraftWithPhotoAsync(Listings.Complete("Cameras and tech", "Cameras"));

        using var response = await _client.PublishAsync(reference);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _factory.InDatabase(db =>
        {
            var listing = db.Listings.Single();
            Assert.Equal("Cameras and tech", listing.Category);
            Assert.Equal("Girls'", listing.Size);
            Assert.NotNull(listing.PublishedAt);
        });
    }

    [Fact]
    public async Task Every_missing_field_is_identified()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        var reference = await _client.CreateDraftAsync(new { category = "Clothing" });

        using var response = await _client.PublishAsync(reference);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("LISTING_INVALID", problem.Code);
        Assert.Equal(
            ["photos", "subcategory", "title", "description", "brand", "condition", "price", "shippingCost"],
            problem.Errors!.Keys);
        Assert.Equal("Add at least one photo", problem.Errors["photos"].Message);
        Assert.Equal("Title must be 5 to 80 characters", problem.Errors["title"].Message);
        Assert.Equal("Enter a price from $1.00 to $5,000.00", problem.Errors["price"].Message);
    }

    [Fact]
    public async Task Used_fair_without_flaws_is_refused_with_describe_the_flaws()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        var reference = await _client.CreateDraftWithPhotoAsync(Listings.Complete().With("condition", "Used - Fair"));

        using var response = await _client.PublishAsync(reference);

        var problem = await Problem.ReadAsync(response);
        Assert.Equal("Describe the item's flaws", problem.Errors!["flaws"].Message);
    }

    [Fact]
    public async Task A_brand_outside_the_suggested_list_and_script_text_are_stored_as_typed()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        var reference = await _client.CreateDraftWithPhotoAsync(
            Listings.Complete().With("brand", "  Small Batch Cobblers ").With("title", "<script>alert(1)</script>"));

        using var response = await _client.PublishAsync(reference);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await _client.GetFromJsonAsync<MyListing>($"/api/me/listings/{reference}");
        Assert.Equal("Small Batch Cobblers", stored!.Brand);
        Assert.Equal("<script>alert(1)</script>", stored.Title);
    }

    [Fact]
    public async Task A_listing_that_is_not_a_draft_cannot_be_published_again()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        var reference = await _client.CreateDraftWithPhotoAsync(Listings.Complete());
        using var first = await _client.PublishAsync(reference);

        using var second = await _client.PublishAsync(reference);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("LISTING_NOT_DRAFT", (await Problem.ReadAsync(second)).Code);
    }

    [Fact]
    public async Task Another_member_cannot_publish_a_draft()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        var reference = await _client.CreateDraftWithPhotoAsync(Listings.Complete());
        using var other = _factory.CreateMemberClient();
        await other.SignInVerifiedMemberAsync(_factory, "tutu_tessa", "tessa@example.com");

        using var response = await other.PublishAsync(reference);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_21st_publish_of_a_utc_day_is_429_but_drafts_still_save()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        for (var i = 0; i < 20; i++)
        {
            var draft = await _client.CreateDraftWithPhotoAsync(Listings.Complete());
            using var published = await _client.PublishAsync(draft);
            Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        }

        var reference = await _client.CreateDraftWithPhotoAsync(Listings.Complete());
        using var limited = await _client.PublishAsync(reference);

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal("RATE_LIMITED", (await Problem.ReadAsync(limited)).Code);
        var now = _factory.Clock.GetUtcNow();
        var expected = (int)Math.Ceiling((now.Date.AddDays(1) - now.UtcDateTime).TotalSeconds);
        Assert.Equal(expected.ToString(System.Globalization.CultureInfo.InvariantCulture), limited.Headers.GetValues("Retry-After").Single());

        _factory.Clock.Advance(now.Date.AddDays(1) - now.UtcDateTime);
        using var refreshed = await _client.PostAsync("/api/auth/refresh", null);
        using var tomorrow = await _client.PublishAsync(reference);
        Assert.Equal(HttpStatusCode.OK, tomorrow.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
