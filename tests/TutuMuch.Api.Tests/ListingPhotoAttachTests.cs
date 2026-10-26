// Acceptance Test
// Traces to: L2-010, L2-015, L2-017
// Description: A listing save carries photo IDs in display order; position 0 is the cover. A
// ninth photo, another member's photo and a publish with no photos are refused.

using System.Net;
using System.Net.Http.Json;
using TutuMuch.Api.Tests.Support;

namespace TutuMuch.Api.Tests;

public sealed class ListingPhotoAttachTests : IDisposable
{
    private readonly TutuMuchApiFactory _factory = new();
    private readonly HttpClient _client;

    public ListingPhotoAttachTests()
    {
        _client = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task Photos_keep_the_order_of_the_save_and_the_first_is_the_cover()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        var (a, b, c) = (await _client.UploadValidPhotoAsync(), await _client.UploadValidPhotoAsync(), await _client.UploadValidPhotoAsync());

        using var created = await _client.PostAsJsonAsync("/api/listings", Listings.Complete().With("photoIds", new[] { a, b, c }));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var listing = (await created.Content.ReadFromJsonAsync<MyListing>())!;
        Assert.Equal([a, b, c], listing.Photos.Select(p => p.Id));
        Assert.Matches("^/photos/[0-9a-f]{64}/400\\.webp$", listing.Photos[0].Url);
    }

    [Fact]
    public async Task Moving_the_third_photo_first_makes_it_the_cover()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        var (a, b, c) = (await _client.UploadValidPhotoAsync(), await _client.UploadValidPhotoAsync(), await _client.UploadValidPhotoAsync());
        var reference = await _client.CreateDraftAsync(Listings.Complete().With("photoIds", new[] { a, b, c }));

        using var updated = await _client.PutAsJsonAsync($"/api/listings/{reference}", Listings.Complete().With("photoIds", new[] { c, a, b }));

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var stored = await _client.GetFromJsonAsync<MyListing>($"/api/me/listings/{reference}");
        Assert.Equal([c, a, b], stored!.Photos.Select(p => p.Id));
        _factory.InDatabase(db => Assert.Equal(0, db.ListingPhotos.Single(p => p.Id == c).Position));
    }

    [Fact]
    public async Task A_photo_left_out_of_the_save_is_removed_from_the_listing()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        var (a, b) = (await _client.UploadValidPhotoAsync(), await _client.UploadValidPhotoAsync());
        var reference = await _client.CreateDraftAsync(Listings.Complete().With("photoIds", new[] { a, b }));

        using var updated = await _client.PutAsJsonAsync($"/api/listings/{reference}", Listings.Complete().With("photoIds", new[] { b }));

        var listing = (await updated.Content.ReadFromJsonAsync<MyListing>())!;
        Assert.Equal([b], listing.Photos.Select(p => p.Id));
        _factory.InDatabase(db => Assert.Null(db.ListingPhotos.Single(p => p.Id == a).ListingId));
    }

    [Fact]
    public async Task A_save_without_photo_ids_keeps_the_photos()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        var a = await _client.UploadValidPhotoAsync();
        var reference = await _client.CreateDraftAsync(Listings.Complete().With("photoIds", new[] { a }));

        using var updated = await _client.PutAsJsonAsync($"/api/listings/{reference}", Listings.Complete());

        var listing = (await updated.Content.ReadFromJsonAsync<MyListing>())!;
        Assert.Equal([a], listing.Photos.Select(p => p.Id));
    }

    [Fact]
    public async Task A_ninth_photo_is_refused_by_the_api()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        var ids = new List<Guid>();
        for (var i = 0; i < 9; i++)
        {
            ids.Add(await _client.UploadValidPhotoAsync());
        }

        using var response = await _client.PostAsJsonAsync("/api/listings", Listings.Complete().With("photoIds", ids));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("PHOTO_LIMIT", problem.Code);
        Assert.Equal("A listing can have up to 8 photos", problem.Errors!["photos"].Message);
    }

    [Fact]
    public async Task Another_members_photo_cannot_be_attached()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        using var other = _factory.CreateMemberClient();
        await other.SignInVerifiedMemberAsync(_factory, "kicks_12", "kicks@example.com");
        var theirs = await other.UploadValidPhotoAsync();

        using var response = await _client.PostAsJsonAsync("/api/listings", Listings.Complete().With("photoIds", new[] { theirs }));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("PHOTO_NOT_FOUND", problem.Code);
        Assert.Equal("That photo is no longer available. Upload it again.", problem.Errors!["photos"].Message);
    }

    [Fact]
    public async Task Publishing_a_listing_with_no_photos_is_refused_with_add_at_least_one_photo()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        var reference = await _client.CreateDraftAsync(Listings.Complete());

        using var response = await _client.PublishAsync(reference);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("PHOTOS_REQUIRED", problem.Code);
        Assert.Equal("Add at least one photo", problem.Errors!["photos"].Message);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
