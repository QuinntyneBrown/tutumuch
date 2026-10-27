// Acceptance Test
// Traces to: L2-097, L2-098, L2-099, L2-105, L2-106
// Description: A signed-in member saves, lists, edits and deletes their item requests at
// /api/me/item-requests: fields are checked with the listing rules, the size comes from the
// category, at most 20 requests are open, a request is open for 90 days after each save and stays
// listed as Expired, another member's request answers 404 and a visitor gets 401. Requests work
// in every launch phase.

using System.Net;
using System.Net.Http.Json;
using TuTuMuch.Api.Tests.Support;

namespace TuTuMuch.Api.Tests;

public sealed class ItemRequestsTests : IDisposable
{
    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _member;

    public ItemRequestsTests()
    {
        _member = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task A_member_saves_a_request_and_the_size_comes_from_the_category()
    {
        await _member.SignInVerifiedMemberAsync(_factory);
        var now = _factory.Clock.GetUtcNow().UtcDateTime;

        using var response = await _member.PostAsJsonAsync("/api/me/item-requests", ItemRequests.Body(keywords: "  ballet   slippers ", maxPrice: 20m, condition: "Used - Good"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<ItemRequestView>())!;
        Assert.Equal($"/api/me/item-requests/{created.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal("Dance", created.Category);
        Assert.Equal("Girls'", created.Size);
        Assert.Equal("ballet slippers", created.Keywords);
        Assert.Equal(20m, created.MaxPrice);
        Assert.Equal("Used - Good", created.MinimumCondition);
        Assert.Equal("Open", created.Status);
        Assert.Equal(now.AddDays(90), created.ExpiresAt, TimeSpan.FromSeconds(1));
        Assert.Equal(0, created.MatchCount);
        Assert.Empty(created.Matches);
    }

    [Fact]
    public async Task An_unverified_member_may_request_too()
    {
        await _member.SignInUnverifiedMemberAsync();

        using var response = await _member.PostAsJsonAsync("/api/me/item-requests", ItemRequests.Body());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    public async Task Requests_work_in_every_launch_phase(string phase)
    {
        using var factory = new TuTuMuchApiFactory(launchPhase: phase);
        var member = factory.CreateMemberClient();
        await member.SignInVerifiedMemberAsync(factory);

        using var response = await member.PostAsJsonAsync("/api/me/item-requests", ItemRequests.Body());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_fields_are_refused_together_and_nothing_is_stored()
    {
        await _member.SignInVerifiedMemberAsync(_factory);

        using var response = await _member.PostAsJsonAsync("/api/me/item-requests", new { category = "Furniture", keywords = "ab", maxPrice = 0.99m, minimumCondition = "Like new" });
        using var size = await _member.PostAsJsonAsync("/api/me/item-requests", new { category = "Clothing", size = "Boys' 8", keywords = "party dress" });
        using var keywords = await _member.PostAsJsonAsync("/api/me/item-requests", ItemRequests.Body(keywords: new string('a', 101)));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("CATEGORY_INVALID", problem.Code);
        Assert.Equal("Choose a category", problem.Errors!["category"].Message);
        Assert.Equal("Describe the item in 3 to 100 characters", problem.Errors["keywords"].Message);
        Assert.Equal("Enter a price from $1.00 to $5,000.00", problem.Errors["maxPrice"].Message);
        Assert.Equal("Choose one of the 5 conditions", problem.Errors["minimumCondition"].Message);
        Assert.Equal("SIZE_NOT_ACCEPTED", (await Problem.ReadAsync(size)).Code);
        Assert.Equal("REQUEST_KEYWORDS_LENGTH", (await Problem.ReadAsync(keywords)).Code);
        _factory.InDatabase(db => Assert.Empty(db.ItemRequests));
    }

    [Fact]
    public async Task My_requests_lists_the_members_requests_newest_first_with_the_open_count()
    {
        await _member.SignInVerifiedMemberAsync(_factory);
        var first = await _member.CreateItemRequestAsync(ItemRequests.Body(keywords: "ballet slippers"));
        _factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await _member.CreateItemRequestAsync(ItemRequests.Body(category: "Sports", keywords: "scooter"));
        _factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await _member.CreateItemRequestAsync(ItemRequests.Body(category: "Cameras and tech", keywords: "kids smartwatch"));
        var other = _factory.CreateMemberClient();
        await other.SignInVerifiedMemberAsync(_factory, "pia_reyes", "pia.reyes@example.com");
        await other.CreateItemRequestAsync(ItemRequests.Body(keywords: "tutu"));

        var mine = await _member.ListItemRequestsAsync();

        Assert.Equal(["kids smartwatch", "scooter", "ballet slippers"], mine.Items.Select(r => r.Keywords));
        Assert.Equal(3, mine.OpenCount);
        Assert.Equal(20, mine.Limit);
        using var one = await _member.GetAsync($"/api/me/item-requests/{first.Id}");
        Assert.Equal(HttpStatusCode.OK, one.StatusCode);
        Assert.Equal("ballet slippers", (await one.Content.ReadFromJsonAsync<ItemRequestView>())!.Keywords);
    }

    [Fact]
    public async Task Editing_saves_the_fields_and_renews_the_request()
    {
        await _member.SignInVerifiedMemberAsync(_factory);
        var request = await _member.CreateItemRequestAsync(ItemRequests.Body());
        ExpireOne("ballet slippers", daysAgo: -80);
        var now = _factory.Clock.GetUtcNow().UtcDateTime;

        using var response = await _member.PutAsJsonAsync($"/api/me/item-requests/{request.Id}", ItemRequests.Body(keywords: "pink ballet slippers", maxPrice: 15m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = (await response.Content.ReadFromJsonAsync<ItemRequestView>())!;
        Assert.Equal("pink ballet slippers", saved.Keywords);
        Assert.Equal(15m, saved.MaxPrice);
        Assert.Equal(now.AddDays(90), saved.ExpiresAt, TimeSpan.FromSeconds(1));
        using var invalid = await _member.PutAsJsonAsync($"/api/me/item-requests/{request.Id}", ItemRequests.Body(keywords: "x"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        _factory.InDatabase(db => Assert.Equal("pink ballet slippers", db.ItemRequests.Single().Keywords));
    }

    [Fact]
    public async Task Deleting_removes_the_request()
    {
        await _member.SignInVerifiedMemberAsync(_factory);
        var request = await _member.CreateItemRequestAsync(ItemRequests.Body());

        using var response = await _member.DeleteAsync($"/api/me/item-requests/{request.Id}");
        using var again = await _member.DeleteAsync($"/api/me/item-requests/{request.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
        Assert.Empty((await _member.ListItemRequestsAsync()).Items);
    }

    [Fact]
    public async Task Another_members_request_answers_404_and_is_unchanged()
    {
        await _member.SignInVerifiedMemberAsync(_factory);
        var request = await _member.CreateItemRequestAsync(ItemRequests.Body());
        var other = _factory.CreateMemberClient();
        await other.SignInVerifiedMemberAsync(_factory, "pia_reyes", "pia.reyes@example.com");

        using var read = await other.GetAsync($"/api/me/item-requests/{request.Id}");
        using var edit = await other.PutAsJsonAsync($"/api/me/item-requests/{request.Id}", ItemRequests.Body(keywords: "something else"));
        using var delete = await other.DeleteAsync($"/api/me/item-requests/{request.Id}");

        foreach (var response in new[] { read, edit, delete })
        {
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("REQUEST_NOT_FOUND", (await Problem.ReadAsync(response)).Code);
        }

        _factory.InDatabase(db => Assert.Equal("ballet slippers", db.ItemRequests.Single().Keywords));
    }

    [Fact]
    public async Task A_visitor_gets_401_from_every_endpoint()
    {
        var visitor = _factory.CreateMemberClient();
        var id = Guid.NewGuid();

        using var list = await visitor.GetAsync("/api/me/item-requests");
        using var read = await visitor.GetAsync($"/api/me/item-requests/{id}");
        using var create = await visitor.PostAsJsonAsync("/api/me/item-requests", ItemRequests.Body());
        using var edit = await visitor.PutAsJsonAsync($"/api/me/item-requests/{id}", ItemRequests.Body());
        using var delete = await visitor.DeleteAsync($"/api/me/item-requests/{id}");

        Assert.All(new[] { list, read, create, edit, delete }, r => Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode));
    }

    [Fact]
    public async Task The_21st_open_request_is_refused_but_an_expired_one_does_not_count()
    {
        await _member.SignInVerifiedMemberAsync(_factory);
        for (var i = 0; i < 19; i++)
        {
            await _member.CreateItemRequestAsync(ItemRequests.Body(keywords: $"ballet slippers {i}"));
        }

        using var twentieth = await _member.PostAsJsonAsync("/api/me/item-requests", ItemRequests.Body(keywords: "tutu"));
        using var refused = await _member.PostAsJsonAsync("/api/me/item-requests", ItemRequests.Body(keywords: "leotard"));

        Assert.Equal(HttpStatusCode.Created, twentieth.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        var problem = await Problem.ReadAsync(refused);
        Assert.Equal("REQUEST_LIMIT_REACHED", problem.Code);
        Assert.Equal("You have 20 open requests", problem.Title);
        _factory.InDatabase(db => Assert.Equal(20, db.ItemRequests.Count()));

        ExpireOne("tutu", daysAgo: 1);
        using var afterExpiry = await _member.PostAsJsonAsync("/api/me/item-requests", ItemRequests.Body(keywords: "leotard"));
        Assert.Equal(HttpStatusCode.Created, afterExpiry.StatusCode);
    }

    [Fact]
    public async Task An_expired_request_is_listed_as_expired_and_saving_it_renews_it_unless_20_are_open()
    {
        await _member.SignInVerifiedMemberAsync(_factory);
        var expired = await _member.CreateItemRequestAsync(ItemRequests.Body(keywords: "swim goggles", category: "Sports"));
        ExpireOne("swim goggles", daysAgo: 2);

        var list = await _member.ListItemRequestsAsync();
        Assert.Equal("Expired", Assert.Single(list.Items).Status);
        Assert.Equal(0, list.OpenCount);

        for (var i = 0; i < 20; i++)
        {
            await _member.CreateItemRequestAsync(ItemRequests.Body(keywords: $"ballet slippers {i}"));
        }

        using var refused = await _member.PutAsJsonAsync($"/api/me/item-requests/{expired.Id}", ItemRequests.Body(keywords: "swim goggles", category: "Sports"));
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("REQUEST_LIMIT_REACHED", (await Problem.ReadAsync(refused)).Code);

        var open = (await _member.ListItemRequestsAsync()).Items.First(r => r.Status == "Open");
        using var editOpen = await _member.PutAsJsonAsync($"/api/me/item-requests/{open.Id}", ItemRequests.Body(keywords: "ballet slippers again"));
        Assert.Equal(HttpStatusCode.OK, editOpen.StatusCode);

        using var deleted = await _member.DeleteAsync($"/api/me/item-requests/{open.Id}");
        using var renewed = await _member.PutAsJsonAsync($"/api/me/item-requests/{expired.Id}", ItemRequests.Body(keywords: "swim goggles", category: "Sports"));
        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        Assert.Equal("Open", (await renewed.Content.ReadFromJsonAsync<ItemRequestView>())!.Status);
    }

    public void Dispose()
    {
        _member.Dispose();
        _factory.Dispose();
    }

    /// <summary>Moves a request's expiry to <paramref name="daysAgo"/> days before now (negative: still open), as if time had passed since it was saved.</summary>
    private void ExpireOne(string keywords, int daysAgo) => _factory.InDatabase(db =>
    {
        var request = db.ItemRequests.Single(r => r.Keywords == keywords);
        db.Entry(request).Property(r => r.ExpiresAt).CurrentValue = _factory.Clock.GetUtcNow().UtcDateTime.AddDays(-daysAgo);
        db.SaveChanges();
    });
}
