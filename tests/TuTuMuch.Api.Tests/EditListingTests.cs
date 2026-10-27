// Acceptance Test
// Traces to: L2-013, L2-014, L2-020, L2-049
// Description: A seller edits an Active listing under optimistic concurrency: the form reads the
// listing with an ETag and sends it back in If-Match. A stale or missing version is refused, a
// Reserved or Sold listing cannot change, the publish and size rules still hold, and protected
// fields cannot be set.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TuTuMuch.Api.Tests.Support;
using TuTuMuch.Domain.Members;

namespace TuTuMuch.Api.Tests;

public sealed class EditListingTests : IDisposable
{
    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _seller;

    public EditListingTests()
    {
        _seller = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task The_seller_reads_their_listing_with_an_etag()
    {
        var reference = await ActiveListingAsync();

        using var response = await _seller.GetAsync($"/api/me/listings/{reference}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        Assert.False(response.Headers.ETag!.IsWeak);
    }

    [Fact]
    public async Task A_save_with_the_current_etag_changes_the_listing_and_the_catalog()
    {
        var reference = await ActiveListingAsync();
        var etag = await EtagAsync(reference);

        using var response = await PutAsync(reference, Edited().With("price", 40.00m), etag);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = (await response.Content.ReadFromJsonAsync<MyListing>())!;
        Assert.Equal("Active", saved.Status);
        Assert.Equal(40.00m, saved.Price);
        Assert.NotNull(response.Headers.ETag);
        Assert.NotEqual(etag, response.Headers.ETag!.Tag);
        using var visitor = _factory.CreateMemberClient();
        var detail = await visitor.GetStringAsync($"/api/catalog/listings/{reference}");
        Assert.Contains("40.00", detail);
    }

    [Fact]
    public async Task A_save_with_a_stale_etag_is_refused_and_changes_nothing()
    {
        var reference = await ActiveListingAsync();
        var stale = await EtagAsync(reference);
        (await PutAsync(reference, Edited().With("price", 50.00m), stale)).EnsureSuccessStatusCode();

        using var response = await PutAsync(reference, Edited().With("price", 40.00m), stale);

        Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);
        Assert.Equal("LISTING_CHANGED", (await Problem.ReadAsync(response)).Code);
        Assert.Equal(50.00m, (await MineAsync(reference)).Price);
    }

    [Fact]
    public async Task A_save_without_if_match_is_refused()
    {
        var reference = await ActiveListingAsync();

        using var response = await PutAsync(reference, Edited().With("price", 40.00m), etag: null);

        Assert.Equal((HttpStatusCode)428, response.StatusCode);
        Assert.Equal("PRECONDITION_REQUIRED", (await Problem.ReadAsync(response)).Code);
        Assert.Equal(60.00m, (await MineAsync(reference)).Price);
    }

    [Fact]
    public async Task A_reserved_listing_cannot_be_edited()
    {
        var reference = await ActiveListingAsync();
        var etag = await EtagAsync(reference);
        await ReserveAsync(reference);

        using var response = await PutAsync(reference, Edited().With("price", 40.00m), etag);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("LISTING_RESERVED", problem.Code);
        Assert.Equal("This item is in someone's checkout", problem.Title);
        Assert.Equal(60.00m, (await MineAsync(reference)).Price);
    }

    [Fact]
    public async Task A_sold_listing_cannot_be_edited()
    {
        using var buyer = _factory.CreateMemberClient();
        await buyer.SignInVerifiedMemberAsync(_factory);
        var (_, seller) = await _factory.SeedPaidOrderAsync(buyer);
        using var read = await seller.GetAsync("/api/me/listings/L-30001");
        var etag = read.Headers.ETag!.Tag;

        using var response = await PutAsync(seller, "L-30001", Edited(), etag);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("LISTING_SOLD", (await Problem.ReadAsync(response)).Code);
    }

    [Fact]
    public async Task An_active_listing_keeps_the_publish_rules()
    {
        var reference = await ActiveListingAsync();
        var etag = await EtagAsync(reference);

        using var response = await PutAsync(reference, Edited().With("condition", "Used - Fair").With("flaws", null).With("price", 5200m), etag);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("Describe the item's flaws", problem.Errors!["flaws"].Message);
        Assert.Equal("Enter a price from $1.00 to $5,000.00", problem.Errors["price"].Message);
        Assert.Equal(60.00m, (await MineAsync(reference)).Price);
    }

    [Fact]
    public async Task A_size_other_than_the_accepted_size_is_refused_on_update()
    {
        var reference = await ActiveListingAsync();
        var etag = await EtagAsync(reference);

        using var response = await PutAsync(reference, Edited().With("size", "11"), etag);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("SIZE_NOT_ACCEPTED", (await Problem.ReadAsync(response)).Code);
    }

    [Fact]
    public async Task Changing_the_category_sets_the_size_from_the_category()
    {
        var reference = await ActiveListingAsync();
        var etag = await EtagAsync(reference);

        using var response = await PutAsync(reference, Edited().With("category", "Cameras and tech").With("subcategory", "Cameras"), etag);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Girls'", (await MineAsync(reference)).Size);
    }

    [Fact]
    public async Task Protected_fields_in_the_body_are_ignored()
    {
        var reference = await ActiveListingAsync();
        var etag = await EtagAsync(reference);

        using var response = await PutAsync(reference, Edited().With("status", "Sold").With("sellerId", Guid.NewGuid()), etag);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Active", (await MineAsync(reference)).Status);
        var stored = await _seller.GetFromJsonAsync<MyListingsView>("/api/me/listings");
        Assert.Contains(stored!.Items, i => i.Reference == reference);
    }

    [Fact]
    public async Task Another_members_listing_is_not_found()
    {
        var reference = await ActiveListingAsync();
        var etag = await EtagAsync(reference);
        using var other = _factory.CreateMemberClient();
        await other.SignInVerifiedMemberAsync(_factory, "rival_12", "rival@example.com");

        using var response = await PutAsync(other, reference, Edited(), etag);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>The seller signs in and publishes the Complete party dress ($60.00) with one photo.</summary>
    private async Task<string> ActiveListingAsync()
    {
        await _seller.SignInVerifiedMemberAsync(_factory);
        var reference = await _seller.CreateDraftWithPhotoAsync(Listings.Complete());
        (await _seller.PublishAsync(reference)).EnsureSuccessStatusCode();
        return reference;
    }

    /// <summary>A second member starts checkout, which reserves the listing for 10 minutes.</summary>
    private async Task ReserveAsync(string reference)
    {
        Member buyer = null!;
        _factory.InDatabase(db =>
        {
            buyer = Member.Register("tall_buyer", "tall.buyer@example.com", "hash", DateTime.UtcNow);
            buyer.MarkEmailVerified(DateTime.UtcNow);
            db.Members.Add(buyer);
            db.SaveChanges();
        });
        using var client = _factory.CreateMemberClient().WithSession(_factory, buyer);
        (await client.StartCheckoutAsync(reference)).EnsureSuccessStatusCode();
    }

    private static Dictionary<string, object?> Edited() => Listings.Complete();

    private async Task<string> EtagAsync(string reference)
    {
        using var response = await _seller.GetAsync($"/api/me/listings/{reference}");
        return response.Headers.ETag!.Tag;
    }

    private async Task<MyListing> MineAsync(string reference) =>
        (await _seller.GetFromJsonAsync<MyListing>($"/api/me/listings/{reference}"))!;

    private Task<HttpResponseMessage> PutAsync(string reference, object body, string? etag) => PutAsync(_seller, reference, body, etag);

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, string reference, object body, string? etag)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/listings/{reference}") { Content = JsonContent.Create(body) };
        if (etag is not null)
        {
            request.Headers.IfMatch.Add(EntityTagHeaderValue.Parse(etag));
        }

        return client.SendAsync(request);
    }

    public void Dispose()
    {
        _seller.Dispose();
        _factory.Dispose();
    }
}
