// Acceptance Test
// Traces to: L2-020, L2-035
// Description: A seller withdraws an Active listing. It becomes Removed, leaves the catalog for good
// and can no longer be edited or relisted. A Reserved, Sold or draft listing cannot be withdrawn.

using System.Net;
using System.Net.Http.Json;
using TuTuMuch.Api.Tests.Support;
using TuTuMuch.Domain.Members;

namespace TuTuMuch.Api.Tests;

public sealed class WithdrawListingTests : IDisposable
{
    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _seller;

    public WithdrawListingTests()
    {
        _seller = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task Withdrawing_an_active_listing_removes_it_from_the_catalog()
    {
        var reference = await ActiveListingAsync();

        using var response = await WithdrawAsync(reference);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Removed", (await response.Content.ReadFromJsonAsync<WithdrawView>())!.Status);
        var mine = await _seller.GetFromJsonAsync<MyListingView>($"/api/me/listings/{reference}");
        Assert.Equal("Removed", mine!.Status);
        Assert.Equal("Withdrawn", mine.RemovalReason);
        using var visitor = _factory.CreateMemberClient();
        using var detail = await visitor.GetAsync($"/api/catalog/listings/{reference}");
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
        var catalog = await visitor.GetStringAsync("/api/catalog/listings");
        Assert.DoesNotContain(reference, catalog);
    }

    [Fact]
    public async Task Withdrawing_again_answers_ok()
    {
        var reference = await ActiveListingAsync();
        (await WithdrawAsync(reference)).EnsureSuccessStatusCode();

        using var response = await WithdrawAsync(reference);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_withdrawn_listing_cannot_be_edited_or_relisted()
    {
        var reference = await ActiveListingAsync();
        (await WithdrawAsync(reference)).EnsureSuccessStatusCode();
        using var read = await _seller.GetAsync($"/api/me/listings/{reference}");
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/listings/{reference}") { Content = JsonContent.Create(Listings.Complete().With("sizeConfirmed", true)) };
        request.Headers.IfMatch.Add(read.Headers.ETag!);

        using var edit = await _seller.SendAsync(request);
        using var relist = await _seller.RelistAsync(reference);

        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);
        Assert.Equal("LISTING_NOT_EDITABLE", (await Problem.ReadAsync(edit)).Code);
        Assert.Equal(HttpStatusCode.Conflict, relist.StatusCode);
        Assert.Equal("LISTING_NOT_RELISTABLE", (await Problem.ReadAsync(relist)).Code);
    }

    [Fact]
    public async Task A_reserved_listing_cannot_be_withdrawn()
    {
        var reference = await ActiveListingAsync();
        await ReserveAsync(reference);

        using var response = await WithdrawAsync(reference);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("LISTING_RESERVED", (await Problem.ReadAsync(response)).Code);
        Assert.Equal("Reserved", (await _seller.GetFromJsonAsync<MyListingView>($"/api/me/listings/{reference}"))!.Status);
    }

    [Fact]
    public async Task A_sold_listing_cannot_be_withdrawn()
    {
        using var buyer = _factory.CreateMemberClient();
        await buyer.SignInVerifiedMemberAsync(_factory);
        var (_, seller) = await _factory.SeedPaidOrderAsync(buyer);

        using var response = await seller.PostAsync("/api/listings/L-30001/withdraw", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("LISTING_SOLD", (await Problem.ReadAsync(response)).Code);
    }

    [Fact]
    public async Task A_draft_cannot_be_withdrawn()
    {
        await _seller.SignInVerifiedMemberAsync(_factory);
        var reference = await _seller.CreateDraftAsync(new { category = "Clothing" });

        using var response = await WithdrawAsync(reference);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("LISTING_NOT_EDITABLE", (await Problem.ReadAsync(response)).Code);
    }

    [Fact]
    public async Task Another_members_listing_is_not_found()
    {
        var reference = await ActiveListingAsync();
        using var other = _factory.CreateMemberClient();
        await other.SignInVerifiedMemberAsync(_factory, "rival_12", "rival@example.com");

        using var response = await other.PostAsync($"/api/listings/{reference}/withdraw", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Active", (await _seller.GetFromJsonAsync<MyListingView>($"/api/me/listings/{reference}"))!.Status);
    }

    private async Task<string> ActiveListingAsync()
    {
        await _seller.SignInVerifiedMemberAsync(_factory);
        var reference = await _seller.CreateDraftWithPhotoAsync(Listings.Complete());
        (await _seller.PublishAsync(reference)).EnsureSuccessStatusCode();
        return reference;
    }

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

    private Task<HttpResponseMessage> WithdrawAsync(string reference) => _seller.PostAsync($"/api/listings/{reference}/withdraw", null);

    public void Dispose()
    {
        _seller.Dispose();
        _factory.Dispose();
    }
}

public sealed record WithdrawView(string Reference, string Status);

public sealed record MyListingView(string Reference, string Status, string? RemovalReason);
