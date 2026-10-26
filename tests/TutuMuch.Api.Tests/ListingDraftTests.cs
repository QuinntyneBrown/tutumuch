// Acceptance Test
// Traces to: L2-003, L2-010, L2-011, L2-013, L2-016, L2-019
// Description: A verified member saves a draft that needs only a category. The server sets the
// size from the category and refuses any other size; only the seller can read or change a draft.

using System.Net;
using System.Net.Http.Json;
using TutuMuch.Api.Tests.Support;

namespace TutuMuch.Api.Tests;

public sealed class ListingDraftTests : IDisposable
{
    private readonly TutuMuchApiFactory _factory = new();
    private readonly HttpClient _client;

    public ListingDraftTests()
    {
        _client = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task A_visitor_cannot_create_a_listing()
    {
        using var response = await _client.PostAsJsonAsync("/api/listings", Listings.Complete());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task An_unverified_member_is_refused_with_email_not_verified()
    {
        await _client.SignInUnverifiedMemberAsync();

        using var response = await _client.PostAsJsonAsync("/api/listings", Listings.Complete());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("EMAIL_NOT_VERIFIED", (await Problem.ReadAsync(response)).Code);
    }

    [Fact]
    public async Task A_newly_verified_member_saves_a_draft_with_only_a_category_and_title()
    {
        await _client.SignInVerifiedMemberAsync(_factory);

        using var response = await _client.PostAsJsonAsync("/api/listings", new { category = "Tops", title = "Grey hoodie" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var draft = (await response.Content.ReadFromJsonAsync<MyListing>())!;
        Assert.Matches("^L-[0-9]+$", draft.Reference);
        Assert.Equal($"/api/me/listings/{draft.Reference}", response.Headers.Location?.ToString());
        Assert.Equal("Draft", draft.Status);
        Assert.Equal("Girls'", draft.Size);
        Assert.Null(draft.PublishedAt);
    }

    [Fact]
    public async Task A_dresses_listing_without_a_size_is_stored_as_girls()
    {
        await _client.SignInVerifiedMemberAsync(_factory);

        var reference = await _client.CreateDraftAsync(Listings.Complete());

        var stored = await _client.GetFromJsonAsync<MyListing>($"/api/me/listings/{reference}");
        Assert.Equal("Girls'", stored!.Size);
        Assert.Equal(60.00m, stored.Price);
    }

    [Theory]
    [InlineData("Dresses", "Dresses", "11")]
    [InlineData("Tops", "Hoodies", "M")]
    [InlineData("Tops", "Hoodies", "XL")]
    public async Task Any_size_other_than_the_accepted_size_is_refused(string category, string subcategory, string size)
    {
        await _client.SignInVerifiedMemberAsync(_factory);

        using var response = await _client.PostAsJsonAsync("/api/listings", Listings.Complete(category, subcategory).With("size", size));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("SIZE_NOT_ACCEPTED", (await Problem.ReadAsync(response)).Code);
        _factory.InDatabase(db => Assert.False(db.Listings.Any()));
    }

    [Theory]
    [InlineData("Dresses", "Hoodies")]
    [InlineData("Accessories", null)]
    [InlineData("Tops", "Hats")]
    [InlineData(null, null)]
    public async Task A_category_or_subcategory_outside_the_table_is_refused(string? category, string? subcategory)
    {
        await _client.SignInVerifiedMemberAsync(_factory);

        using var response = await _client.PostAsJsonAsync("/api/listings", new { category, subcategory });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("CATEGORY_INVALID", (await Problem.ReadAsync(response)).Code);
        _factory.InDatabase(db => Assert.False(db.Listings.Any()));
    }

    [Fact]
    public async Task A_condition_outside_the_five_values_is_refused()
    {
        await _client.SignInVerifiedMemberAsync(_factory);

        using var response = await _client.PostAsJsonAsync("/api/listings", Listings.Complete().With("condition", "Like new"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("CONDITION_INVALID", problem.Code);
        Assert.Equal("CONDITION_INVALID", problem.Errors!["condition"].Code);
    }

    [Fact]
    public async Task Client_cannot_set_the_seller_status_or_confirmation()
    {
        await _client.SignInVerifiedMemberAsync(_factory);

        var reference = await _client.CreateDraftAsync(
            Listings.Complete().With("status", "Active").With("sellerId", Guid.NewGuid()).With("sizeConfirmedAt", DateTime.UtcNow));

        var stored = await _client.GetFromJsonAsync<MyListing>($"/api/me/listings/{reference}");
        Assert.Equal("Draft", stored!.Status);
        _factory.InDatabase(db => Assert.Null(db.Listings.Single().SizeConfirmedAt));
    }

    [Fact]
    public async Task Updating_a_draft_to_dresses_sets_the_size_to_girls()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        var reference = await _client.CreateDraftAsync(Listings.Complete("Tops", "Hoodies"));

        using var response = await _client.PutAsJsonAsync($"/api/listings/{reference}", Listings.Complete("Dresses", "Tutus"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await _client.GetFromJsonAsync<MyListing>($"/api/me/listings/{reference}");
        Assert.Equal("Girls'", stored!.Size);
        Assert.Equal("Tutus", stored.Subcategory);
    }

    [Fact]
    public async Task Another_member_gets_404_for_a_draft_and_cannot_change_it()
    {
        await _client.SignInVerifiedMemberAsync(_factory);
        var reference = await _client.CreateDraftAsync(Listings.Complete());
        using var other = _factory.CreateMemberClient();
        await other.SignInVerifiedMemberAsync(_factory, "tutu_tessa", "tessa@example.com");

        using var read = await other.GetAsync($"/api/me/listings/{reference}");
        using var update = await other.PutAsJsonAsync($"/api/listings/{reference}", Listings.Complete().With("title", "Taken over"));

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        var stored = await _client.GetFromJsonAsync<MyListing>($"/api/me/listings/{reference}");
        Assert.Equal("Rainbow tulle tutu, pink", stored!.Title);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
