// Acceptance Test
// Traces to: L2-113, L2-114, L2-115, L2-116, L2-121
// Description: A member who may sell posts a shelf find with the store, its address, the shelf price
// and the day it was seen; only administrators offer grabs. Anyone browses live finds newest first
// and reads one; the poster marks it gone or withdraws it unless a grab is open; a closed poster's
// finds disappear.

using System.Net;
using System.Net.Http.Json;
using TuTuMuch.Api.Tests.Support;
using TuTuMuch.Domain.Finds;

namespace TuTuMuch.Api.Tests;

public sealed class ShelfFindsTests : IDisposable
{
    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _poster;
    private readonly HttpClient _visitor;

    public ShelfFindsTests()
    {
        _visitor = _factory.CreateMemberClient();
        _poster = _factory.CreateVerifiedClient("lena_finds");
    }

    [Fact]
    public async Task A_verified_member_in_phase_3_posts_a_find_and_anyone_reads_it()
    {
        using var response = await _poster.PostFindAsync(Finds.Find(_factory));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var posted = (await response.Content.ReadFromJsonAsync<FindView>())!;
        Assert.Equal("F-1001", posted.Reference);
        var read = await _visitor.GetFromJsonAsync<FindView>($"/api/finds/{posted.Reference}");
        Assert.Equal(
            new FindView("F-1001", "Glitter ballet flats, pink", "Dance", "40.00", "Target", "88 Atlantic Ave, Brooklyn, NY", _factory.Day(-1), _factory.Day(13), "On the clearance end cap", "Active", "lena_finds", null, false, false),
            read);
        Assert.True((await _poster.GetFromJsonAsync<FindView>($"/api/finds/{posted.Reference}"))!.Mine);
    }

    [Fact]
    public async Task In_phase_1_only_an_administrator_may_post()
    {
        using var factory = new TuTuMuchApiFactory(launchPhase: "1");
        factory.EnsureDatabase();
        using var member = factory.CreateVerifiedClient("lena_finds");
        using var admin = factory.CreateAdministratorClient();

        using var refused = await member.PostFindAsync(Finds.Find(factory));
        using var posted = await admin.PostFindAsync(Finds.Find(factory, grabFee: 8m));

        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Equal("SELLING_NOT_OPEN", (await Problem.ReadAsync(refused)).Code);
        Assert.Equal(HttpStatusCode.Created, posted.StatusCode);
        Assert.Equal("8.00", (await posted.Content.ReadFromJsonAsync<FindView>())!.GrabFee);
    }

    [Fact]
    public async Task An_unverified_member_and_a_visitor_cannot_post()
    {
        var unverified = _factory.CreateMemberClient();
        await unverified.SignInUnverifiedMemberAsync();

        using var forbidden = await unverified.PostFindAsync(Finds.Find(_factory));
        using var anonymous = await _visitor.PostFindAsync(Finds.Find(_factory));

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal("EMAIL_NOT_VERIFIED", (await Problem.ReadAsync(forbidden)).Code);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task An_unsupported_category_is_refused_whatever_the_client_sends()
    {
        using var response = await _poster.PostFindAsync(Finds.Find(_factory, category: "Men's shoes"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("CATEGORY_NOT_SUPPORTED", (await Problem.ReadAsync(response)).Code);
        _factory.InDatabase(db => Assert.Empty(db.ShelfFinds));
    }

    [Fact]
    public async Task Every_field_out_of_range_has_its_error()
    {
        var body = new { title = "Hats", category = "Dance", shelfPrice = 5000.01m, storeName = "Target", storeAddress = new string('a', 201), seenOn = _factory.Day(-8), note = new string('n', 501) };

        using var response = await _poster.PostFindAsync(body);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal(["note", "seenOn", "shelfPrice", "storeAddress", "title"], problem.Errors!.Keys.Order().ToArray());
    }

    [Fact]
    public async Task Only_an_administrator_offers_a_grab_and_the_fee_is_1_to_50_dollars()
    {
        using var admin = _factory.CreateAdministratorClient();

        using var member = await _poster.PostFindAsync(Finds.Find(_factory, grabFee: 8m));
        using var tooHigh = await admin.PostFindAsync(Finds.Find(_factory, grabFee: 50.01m));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, member.StatusCode);
        Assert.Equal("GRAB_OFFER_NOT_ALLOWED", (await Problem.ReadAsync(member)).Code);
        Assert.Equal("GRAB_FEE_OUT_OF_RANGE", (await Problem.ReadAsync(tooHigh)).Code);
        _factory.InDatabase(db => Assert.Empty(db.ShelfFinds));
    }

    [Fact]
    public async Task The_21st_find_of_the_day_is_refused()
    {
        for (var i = 0; i < ShelfFind.PostsPerDay; i++)
        {
            await _poster.PostedFindAsync(Finds.Find(_factory));
        }

        using var response = await _poster.PostFindAsync(Finds.Find(_factory));

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter);
        Assert.Equal("That's 20 finds today", (await Problem.ReadAsync(response)).Title);
    }

    [Fact]
    public async Task The_list_shows_live_finds_newest_first_24_at_a_time()
    {
        _factory.InDatabase(db =>
        {
            var posterId = db.Members.Single().Id;
            var now = _factory.Clock.GetUtcNow().UtcDateTime;
            for (var i = 0; i < 30; i++)
            {
                db.ShelfFinds.Add(ShelfFind.Post(posterId, 2000 + i, Details(DateOnly.FromDateTime(now)), now.AddMinutes(i)));
            }

            var old = ShelfFind.Post(posterId, 3000, Details(DateOnly.FromDateTime(now).AddDays(-7)), now.AddDays(-7));
            var gone = ShelfFind.Post(posterId, 3001, Details(DateOnly.FromDateTime(now)), now);
            gone.MarkGone(now);
            var withdrawn = ShelfFind.Post(posterId, 3002, Details(DateOnly.FromDateTime(now)), now);
            withdrawn.Withdraw(now);
            db.ShelfFinds.AddRange(old, gone, withdrawn);
            db.SaveChanges();
        });
        _factory.Clock.Advance(TimeSpan.FromDays(8));

        var first = (await _visitor.GetFromJsonAsync<FindPageView>("/api/finds"))!;
        var second = (await _visitor.GetFromJsonAsync<FindPageView>("/api/finds?page=2"))!;

        Assert.Equal(30, first.TotalCount);
        Assert.Equal(24, first.Items.Count);
        Assert.Equal("F-2029", first.Items[0].Reference);
        Assert.Equal(6, second.Items.Count);
        Assert.DoesNotContain(first.Items.Concat(second.Items), c => c.Reference is "F-3000" or "F-3001" or "F-3002");
    }

    [Fact]
    public async Task Gone_finds_read_as_gone_and_withdrawn_ones_as_not_found()
    {
        var gone = await _poster.PostedFindAsync(Finds.Find(_factory));
        var withdrawn = await _poster.PostedFindAsync(Finds.Find(_factory));

        using var marked = await _poster.PostAsync($"/api/finds/{gone.Reference}/gone", null);
        using var deleted = await _poster.DeleteAsync($"/api/finds/{withdrawn.Reference}");

        Assert.Equal(HttpStatusCode.OK, marked.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal("Gone", (await _visitor.GetFromJsonAsync<FindView>($"/api/finds/{gone.Reference}"))!.Status);
        using var missing = await _visitor.GetAsync($"/api/finds/{withdrawn.Reference}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("FIND_NOT_FOUND", (await Problem.ReadAsync(missing)).Code);
    }

    [Fact]
    public async Task An_expired_find_reads_as_expired()
    {
        var find = await _poster.PostedFindAsync(Finds.Find(_factory, seenDaysAgo: 7));
        _factory.Clock.Advance(TimeSpan.FromDays(8));

        Assert.Equal("Expired", (await _visitor.GetFromJsonAsync<FindView>($"/api/finds/{find.Reference}"))!.Status);
    }

    [Fact]
    public async Task Another_members_find_cannot_be_marked_or_withdrawn()
    {
        var find = await _poster.PostedFindAsync(Finds.Find(_factory));
        using var other = _factory.CreateVerifiedClient("someone_else");

        using var marked = await other.PostAsync($"/api/finds/{find.Reference}/gone", null);
        using var deleted = await other.DeleteAsync($"/api/finds/{find.Reference}");
        using var anonymous = await _visitor.DeleteAsync($"/api/finds/{find.Reference}");

        Assert.Equal(HttpStatusCode.NotFound, marked.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task A_find_with_an_open_grab_cannot_be_marked_or_withdrawn()
    {
        using var admin = _factory.CreateAdministratorClient();
        var find = await admin.PostedFindAsync(Finds.Find(_factory, grabFee: 8m));
        await _poster.PaidGrabAsync(find.Reference);

        using var marked = await admin.PostAsync($"/api/finds/{find.Reference}/gone", null);
        using var deleted = await admin.DeleteAsync($"/api/finds/{find.Reference}");

        Assert.Equal(HttpStatusCode.Conflict, marked.StatusCode);
        Assert.Equal("GRAB_IN_PROGRESS", (await Problem.ReadAsync(marked)).Code);
        Assert.Equal(HttpStatusCode.Conflict, deleted.StatusCode);
        var card = Assert.Single((await _visitor.GetFromJsonAsync<FindPageView>("/api/finds"))!.Items);
        Assert.False(card.GrabAvailable);
        Assert.True((await _visitor.GetFromJsonAsync<FindView>($"/api/finds/{find.Reference}"))!.GrabOpen);
    }

    [Fact]
    public async Task A_closed_posters_finds_disappear()
    {
        var find = await _poster.PostedFindAsync(Finds.Find(_factory));
        _factory.InDatabase(db =>
        {
            db.Members.Single(m => m.DisplayName == "lena_finds").Close(DateTime.UtcNow);
            db.SaveChanges();
        });

        Assert.Empty((await _visitor.GetFromJsonAsync<FindPageView>("/api/finds"))!.Items);
        using var missing = await _visitor.GetAsync($"/api/finds/{find.Reference}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Erasing_a_member_erases_their_finds()
    {
        await _poster.PostedFindAsync(Finds.Find(_factory));

        _factory.InDatabase(db =>
        {
            db.Members.Remove(db.Members.Single(m => m.DisplayName == "lena_finds"));
            db.SaveChanges();
        });

        _factory.InDatabase(db => Assert.Empty(db.ShelfFinds));
    }

    public void Dispose()
    {
        _poster.Dispose();
        _visitor.Dispose();
        _factory.Dispose();
    }

    private static ShelfFindDetails Details(DateOnly seenOn) =>
        new("Glitter ballet flats, pink", "Dance", 24.99m, "Target", "88 Atlantic Ave, Brooklyn, NY", seenOn, null, null);
}
