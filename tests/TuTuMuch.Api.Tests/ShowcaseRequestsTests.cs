// Acceptance Test
// Traces to: L2-107, L2-108, L2-109, L2-110, L2-111
// Description: The administrator marks their own Draft or Active listing as a showcase listing; it
// browses like any Active listing but checkout refuses it with 409 LISTING_SHOWCASE and nothing is
// reserved. A signed-in member sends one request per showcase listing with an optional note of up
// to 500 characters, at most 20 new ones a day; the administrator lists every request, and a
// request is private to its member and the administrator and is erased with the member.

using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TuTuMuch.Api.Tests.Support;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Requests;

namespace TuTuMuch.Api.Tests;

public sealed class ShowcaseRequestsTests : IDisposable
{
    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _admin;
    private readonly HttpClient _shopper;

    public ShowcaseRequestsTests()
    {
        _shopper = _factory.CreateMemberClient();
        _admin = _factory.CreateAdministratorClient();
    }

    [Fact]
    public async Task The_administrator_marks_their_active_listing_and_it_stays_active()
    {
        var reference = await _admin.CreateDraftWithPhotoAsync(Listings.Complete());
        (await _admin.PublishAsync(reference)).EnsureSuccessStatusCode();

        using var response = await _admin.SetShowcaseAsync(reference, true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new ShowcaseView(reference, true), await response.Content.ReadFromJsonAsync<ShowcaseView>());
        var detail = await _shopper.GetFromJsonAsync<ShowcaseDetailView>($"/api/catalog/listings/{reference}");
        Assert.Equal(new ShowcaseDetailView(reference, "Active", true), detail);
    }

    [Fact]
    public async Task A_draft_can_be_marked_and_stays_a_draft()
    {
        var reference = await _admin.CreateDraftWithPhotoAsync(Listings.Complete());

        using var response = await _admin.SetShowcaseAsync(reference, true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _factory.InDatabase(db =>
        {
            var listing = db.Listings.Single(l => l.Reference == reference);
            Assert.Equal(ListingStatus.Draft, listing.Status);
            Assert.True(listing.IsShowcase);
        });
    }

    [Fact]
    public async Task Only_an_administrator_may_mark_a_listing()
    {
        var reference = await _admin.CreateDraftWithPhotoAsync(Listings.Complete());
        await _shopper.SignInVerifiedMemberAsync(_factory);
        var visitor = _factory.CreateMemberClient();

        using var member = await _shopper.SetShowcaseAsync(reference, true);
        using var anonymous = await visitor.SetShowcaseAsync(reference, true);

        Assert.Equal(HttpStatusCode.Forbidden, member.StatusCode);
        Assert.Equal("ADMINISTRATOR_REQUIRED", (await Problem.ReadAsync(member)).Code);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        _factory.InDatabase(db => Assert.False(db.Listings.Single().IsShowcase));
    }

    [Fact]
    public async Task Another_members_listing_answers_not_found()
    {
        await _shopper.SignInVerifiedMemberAsync(_factory);
        var reference = await _shopper.CreateDraftWithPhotoAsync(Listings.Complete());
        (await _shopper.PublishAsync(reference)).EnsureSuccessStatusCode();

        using var response = await _admin.SetShowcaseAsync(reference, true);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("LISTING_NOT_FOUND", (await Problem.ReadAsync(response)).Code);
    }

    [Fact]
    public async Task A_reserved_or_sold_listing_cannot_be_marked()
    {
        var reference = await _admin.CreateDraftWithPhotoAsync(Listings.Complete());
        (await _admin.PublishAsync(reference)).EnsureSuccessStatusCode();
        await _shopper.SignInVerifiedMemberAsync(_factory);
        await _shopper.StartedCheckoutAsync(reference);

        using var reserved = await _admin.SetShowcaseAsync(reference, true);

        Assert.Equal(HttpStatusCode.Conflict, reserved.StatusCode);
        Assert.Equal("SHOWCASE_NOT_ALLOWED", (await Problem.ReadAsync(reserved)).Code);
        _factory.InDatabase(db => Assert.False(db.Listings.Single().IsShowcase));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    public async Task Marking_works_in_every_launch_phase(string phase)
    {
        using var factory = new TuTuMuchApiFactory(launchPhase: phase);
        factory.EnsureDatabase();
        using var admin = factory.CreateAdministratorClient();
        var reference = await admin.CreateDraftWithPhotoAsync(Listings.Complete());
        (await admin.PublishAsync(reference)).EnsureSuccessStatusCode();

        using var response = await admin.SetShowcaseAsync(reference, true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_showcase_listing_browses_like_any_active_listing()
    {
        var reference = await _admin.PublishShowcaseAsync();

        var page = await _shopper.GetStringAsync("/api/catalog/listings?category=dance");

        Assert.Contains(reference, page, StringComparison.Ordinal);
        Assert.DoesNotContain("showcase", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Checkout_refuses_a_showcase_listing_and_reserves_nothing()
    {
        var reference = await _admin.PublishShowcaseAsync();
        await _shopper.SignInVerifiedMemberAsync(_factory);

        using var response = await _shopper.StartCheckoutAsync(reference);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("LISTING_SHOWCASE", problem.Code);
        Assert.Equal("This one has already found a home", problem.Title);
        _factory.InDatabase(db =>
        {
            Assert.Equal(ListingStatus.Active, db.Listings.Single().Status);
            Assert.Empty(db.Checkouts);
        });
    }

    [Fact]
    public async Task An_unmarked_listing_can_be_bought_again()
    {
        var reference = await _admin.PublishShowcaseAsync();
        await _shopper.SignInVerifiedMemberAsync(_factory);
        (await _admin.SetShowcaseAsync(reference, false)).EnsureSuccessStatusCode();

        using var response = await _shopper.StartCheckoutAsync(reference);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Publishing_a_showcase_draft_matches_no_item_request()
    {
        await _shopper.SignInVerifiedMemberAsync(_factory);
        await _shopper.CreateItemRequestAsync(ItemRequests.Body(category: "Clothing", keywords: "tulle tutu"));
        var reference = await _admin.CreateDraftWithPhotoAsync(Listings.Complete());
        (await _admin.SetShowcaseAsync(reference, true)).EnsureSuccessStatusCode();

        (await _admin.PublishAsync(reference)).EnsureSuccessStatusCode();

        _factory.InDatabase(db => Assert.Empty(db.ItemRequestMatches));
        Assert.DoesNotContain(await _factory.DispatchEmailsAsync(), e => e.Subject.StartsWith("A match", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_listing_marked_after_it_matched_is_no_longer_shown_as_a_match()
    {
        await _shopper.SignInVerifiedMemberAsync(_factory);
        await _shopper.CreateItemRequestAsync(ItemRequests.Body(category: "Clothing", keywords: "tulle tutu"));
        var reference = await _admin.CreateDraftWithPhotoAsync(Listings.Complete());
        (await _admin.PublishAsync(reference)).EnsureSuccessStatusCode();
        Assert.Single((await _shopper.ListItemRequestsAsync()).Items.Single().Matches);

        (await _admin.SetShowcaseAsync(reference, true)).EnsureSuccessStatusCode();

        Assert.Empty((await _shopper.ListItemRequestsAsync()).Items.Single().Matches);
    }

    [Fact]
    public async Task A_member_sends_a_request_and_sending_again_updates_it()
    {
        var reference = await _admin.PublishShowcaseAsync();
        await _shopper.SignInVerifiedMemberAsync(_factory);

        using var created = await _shopper.SendShowcaseRequestAsync(reference, "  Size 7, for her spring recital.  ");
        _factory.Clock.Advance(TimeSpan.FromMinutes(5));
        using var updated = await _shopper.SendShowcaseRequestAsync(reference, "Any colour but pink.");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("Size 7, for her spring recital.", (await created.Content.ReadFromJsonAsync<ShowcaseRequestView>())!.Note);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var mine = await _shopper.GetFromJsonAsync<ShowcaseRequestView>($"/api/me/showcase-requests/{reference}");
        Assert.Equal(reference, mine!.ListingReference);
        Assert.Equal("Any colour but pink.", mine.Note);
        Assert.True(mine.UpdatedAt > mine.RequestedAt);
        _factory.InDatabase(db => Assert.Single(db.ShowcaseRequests));
    }

    [Fact]
    public async Task An_unverified_member_may_request_and_an_empty_note_is_stored_as_none()
    {
        var reference = await _admin.PublishShowcaseAsync();
        await _shopper.SignInUnverifiedMemberAsync();

        using var response = await _shopper.SendShowcaseRequestAsync(reference, "   ");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null((await response.Content.ReadFromJsonAsync<ShowcaseRequestView>())!.Note);
    }

    [Fact]
    public async Task A_note_over_500_characters_is_refused_and_nothing_is_stored()
    {
        var reference = await _admin.PublishShowcaseAsync();
        await _shopper.SignInVerifiedMemberAsync(_factory);

        using var response = await _shopper.SendShowcaseRequestAsync(reference, new string('a', 501));
        using var longest = await _shopper.SendShowcaseRequestAsync(reference, new string('a', 500));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("SHOWCASE_NOTE_LENGTH", problem.Code);
        Assert.Equal("Keep the note to 500 characters", problem.Errors!["note"].Message);
        Assert.Equal(HttpStatusCode.Created, longest.StatusCode);
    }

    [Fact]
    public async Task A_visitor_gets_401_from_every_request_endpoint()
    {
        var reference = await _admin.PublishShowcaseAsync();

        using var send = await _shopper.SendShowcaseRequestAsync(reference, null);
        using var read = await _shopper.MyShowcaseRequestAsync(reference);

        Assert.Equal(HttpStatusCode.Unauthorized, send.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, read.StatusCode);
    }

    [Fact]
    public async Task Only_an_active_showcase_listing_can_be_requested_and_never_by_its_seller()
    {
        var showcase = await _admin.PublishShowcaseAsync();
        var ordinary = await _admin.CreateDraftWithPhotoAsync(Listings.Complete());
        (await _admin.PublishAsync(ordinary)).EnsureSuccessStatusCode();
        await _shopper.SignInVerifiedMemberAsync(_factory);

        using var plain = await _shopper.SendShowcaseRequestAsync(ordinary, null);
        using var unknown = await _shopper.SendShowcaseRequestAsync("L-99999", null);
        using var own = await _admin.SendShowcaseRequestAsync(showcase, null);

        Assert.Equal(HttpStatusCode.NotFound, plain.StatusCode);
        Assert.Equal("LISTING_NOT_FOUND", (await Problem.ReadAsync(plain)).Code);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, own.StatusCode);
        Assert.Equal("CANNOT_REQUEST_OWN_LISTING", (await Problem.ReadAsync(own)).Code);
    }

    [Fact]
    public async Task A_withdrawn_showcase_listing_cannot_be_requested()
    {
        var reference = await _admin.PublishShowcaseAsync();
        (await _admin.PostAsync($"/api/listings/{reference}/withdraw", null)).EnsureSuccessStatusCode();
        await _shopper.SignInVerifiedMemberAsync(_factory);

        using var response = await _shopper.SendShowcaseRequestAsync(reference, null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_21st_new_request_of_a_day_is_refused_but_updates_still_work()
    {
        var first = await _admin.PublishShowcaseAsync();
        var second = await _admin.PublishShowcaseAsync();
        await _shopper.SignInVerifiedMemberAsync(_factory);
        (await _shopper.SendShowcaseRequestAsync(first, "first")).EnsureSuccessStatusCode();
        AddRequestsCreatedToday(19);

        using var refused = await _shopper.SendShowcaseRequestAsync(second, null);
        using var update = await _shopper.SendShowcaseRequestAsync(first, "changed");

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.NotNull(refused.Headers.RetryAfter);
        Assert.Equal("That’s 20 requests today", (await Problem.ReadAsync(refused)).Title);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
    }

    [Fact]
    public async Task The_administrator_lists_every_request_most_recent_first()
    {
        var reference = await _admin.PublishShowcaseAsync();
        await _shopper.SignInVerifiedMemberAsync(_factory, "tutu_tessa", "tessa.bell@example.com");
        var other = _factory.CreateMemberClient();
        await other.SignInVerifiedMemberAsync(_factory, "pia_reyes", "pia.reyes@example.com");
        (await _shopper.SendShowcaseRequestAsync(reference, "<b>Size 7</b>")).EnsureSuccessStatusCode();
        _factory.Clock.Advance(TimeSpan.FromMinutes(1));
        (await other.SendShowcaseRequestAsync(reference, null)).EnsureSuccessStatusCode();

        using var response = await _admin.AdminShowcaseRequestsAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = (await response.Content.ReadFromJsonAsync<List<AdminShowcaseRequestView>>())!;
        Assert.Equal(["pia_reyes", "tutu_tessa"], list.Select(r => r.MemberDisplayName));
        Assert.Equal("tessa.bell@example.com", list[1].MemberEmail);
        Assert.Equal("<b>Size 7</b>", list[1].Note);
        Assert.Equal(reference, list[1].ListingReference);
        Assert.Equal("Rainbow tulle tutu, pink", list[1].ListingTitle);
    }

    [Fact]
    public async Task Only_an_administrator_may_list_requests()
    {
        await _shopper.SignInVerifiedMemberAsync(_factory);
        var visitor = _factory.CreateMemberClient();

        using var member = await _shopper.AdminShowcaseRequestsAsync();
        using var anonymous = await visitor.AdminShowcaseRequestsAsync();

        Assert.Equal(HttpStatusCode.Forbidden, member.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task Public_responses_never_reveal_a_request_and_another_member_cannot_read_it()
    {
        var reference = await _admin.PublishShowcaseAsync();
        await _shopper.SignInVerifiedMemberAsync(_factory, "tutu_tessa", "tessa.bell@example.com");
        (await _shopper.SendShowcaseRequestAsync(reference, "Size 7")).EnsureSuccessStatusCode();
        var other = _factory.CreateMemberClient();
        await other.SignInVerifiedMemberAsync(_factory, "pia_reyes", "pia.reyes@example.com");

        var detail = await other.GetStringAsync($"/api/catalog/listings/{reference}");
        var catalog = await other.GetStringAsync("/api/catalog/listings");
        using var theirs = await other.MyShowcaseRequestAsync(reference);

        foreach (var body in new[] { detail, catalog })
        {
            Assert.DoesNotContain("tutu_tessa", body, StringComparison.Ordinal);
            Assert.DoesNotContain("tessa.bell", body, StringComparison.Ordinal);
            Assert.DoesNotContain("Size 7", body, StringComparison.Ordinal);
            Assert.DoesNotContain("request", body, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Equal(HttpStatusCode.NotFound, theirs.StatusCode);
        Assert.Equal("SHOWCASE_REQUEST_NOT_FOUND", (await Problem.ReadAsync(theirs)).Code);
    }

    [Fact]
    public async Task A_closed_members_requests_leave_the_list_and_erasure_removes_them()
    {
        var reference = await _admin.PublishShowcaseAsync();
        await _shopper.SignInVerifiedMemberAsync(_factory);
        (await _shopper.SendShowcaseRequestAsync(reference, "Size 7")).EnsureSuccessStatusCode();
        _factory.InDatabase(db =>
        {
            db.Members.Single(m => m.Email == "marcus.bell@example.com").Close(_factory.Clock.GetUtcNow().UtcDateTime);
            db.SaveChanges();
        });

        var list = await _admin.GetFromJsonAsync<List<AdminShowcaseRequestView>>("/api/admin/showcase-requests");

        Assert.Empty(list!);
        _factory.InDatabase(db =>
        {
            // Erasure deletes the member row; the database cascades to their requests.
            var memberId = db.Members.Single(m => m.Email == "marcus.bell@example.com").Id;
            db.Database.ExecuteSql($"DELETE FROM Members WHERE Id = {memberId}");
        });
        _factory.InDatabase(db => Assert.Empty(db.ShowcaseRequests));
    }

    public void Dispose()
    {
        _admin.Dispose();
        _shopper.Dispose();
        _factory.Dispose();
    }

    /// <summary>Requests the shopper created earlier today on other showcase listings, straight in the database.</summary>
    private void AddRequestsCreatedToday(int count) => _factory.InDatabase(db =>
    {
        var memberId = db.Members.Single(m => m.Email == "marcus.bell@example.com").Id;
        var sellerId = db.Listings.First().SellerId;
        var today = _factory.Clock.GetUtcNow().UtcDateTime.Date;
        var number = db.Listings.Max(l => l.Number);
        for (var i = 1; i <= count; i++)
        {
            var listing = Listing.CreateDraft(sellerId, number + i, new ListingDetails("Dance", null, null, null, null, null, null, null, null, null), today);
            db.Listings.Add(listing);
            db.ShowcaseRequests.Add(ShowcaseRequest.Create(memberId, listing.Id, null, today));
        }

        db.SaveChanges();
    });
}
