// Acceptance Test
// Traces to: L2-089, L2-090, L2-091, L2-092, L2-095
// Description: One setting chooses the launch phase. GET /api/launch reports it; in Phases 1 and 2
// only administrators sell (403 SELLING_NOT_OPEN for everyone else); Phase 1 keeps fewer than 40
// listings (409 PRODUCT_LIMIT_REACHED); buying and existing data are the same in every phase.

using System.Net;
using System.Net.Http.Json;
using TuTuMuch.Api.Tests.Support;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Members;

namespace TuTuMuch.Api.Tests;

public sealed class LaunchPhaseTests
{
    [Theory]
    [InlineData("1", 1, false, false)]
    [InlineData("2", 2, true, false)]
    [InlineData("3", 3, true, true)]
    public async Task The_launch_endpoint_reports_the_phase_to_anyone(string setting, int phase, bool categoryMenusVisible, bool memberSellingOpen)
    {
        using var factory = new TuTuMuchApiFactory(launchPhase: setting);
        using var client = factory.CreateMemberClient();

        using var response = await client.GetAsync("/api/launch");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new LaunchView(phase, categoryMenusVisible, memberSellingOpen), await response.Content.ReadFromJsonAsync<LaunchView>());
        Assert.Equal("public, max-age=60", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Without_a_launch_setting_the_api_runs_in_phase_1()
    {
        using var factory = new TuTuMuchApiFactory(launchPhase: null);
        using var client = factory.CreateMemberClient();

        var launch = await client.GetFromJsonAsync<LaunchView>("/api/launch");

        Assert.Equal(new LaunchView(1, false, false), launch);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("4")]
    [InlineData("two")]
    public void An_invalid_phase_stops_the_api_at_startup(string setting)
    {
        using var factory = new TuTuMuchApiFactory(launchPhase: setting);

        var error = Assert.ThrowsAny<Exception>(() => factory.CreateMemberClient());

        Assert.Contains("Launch:Phase must be 1, 2 or 3", error.ToString());
    }

    [Fact]
    public async Task The_current_member_says_whether_they_are_an_administrator()
    {
        using var factory = new TuTuMuchApiFactory(launchPhase: "1");
        factory.EnsureDatabase();
        using var admin = factory.CreateAdministratorClient();
        using var member = factory.CreateMemberClient();
        await member.SignInVerifiedMemberAsync(factory);

        Assert.True((await admin.GetFromJsonAsync<MeView>("/api/auth/me"))!.IsAdministrator);
        Assert.False((await member.GetFromJsonAsync<MeView>("/api/auth/me"))!.IsAdministrator);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    public async Task A_member_who_is_not_an_administrator_cannot_sell_in_phases_1_and_2(string setting)
    {
        using var factory = new TuTuMuchApiFactory(launchPhase: setting);
        using var client = factory.CreateMemberClient();
        var member = SeedVerifiedMember(factory, "tessa_b");
        client.WithSession(factory, member);
        var active = factory.SeedActiveListing(member.Id);

        var calls = new Func<Task<HttpResponseMessage>>[]
        {
            () => client.PostAsJsonAsync("/api/listings", Listings.Complete()),
            () => client.PutAsJsonAsync($"/api/listings/{active}", Listings.Complete()),
            () => client.PublishAsync(active),
            () => client.PostAsync($"/api/listings/{active}/withdraw", null),
            () => client.PostAsync($"/api/listings/{active}/relist", null),
            () => client.GetAsync("/api/me/listings"),
            () => client.GetAsync($"/api/me/listings/{active}"),
            () => client.UploadPhotoAsync(Images.Jpeg(1200, 1600)),
        };
        foreach (var call in calls)
        {
            using var response = await call();
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var problem = await Problem.ReadAsync(response);
            Assert.Equal("SELLING_NOT_OPEN", problem.Code);
            Assert.Equal("Selling opens soon", problem.Title);
        }

        factory.InDatabase(db =>
        {
            Assert.Equal(ListingStatus.Active, db.Listings.Single().Status);
            Assert.Empty(db.ListingPhotos);
        });
    }

    [Fact]
    public async Task An_unverified_member_in_phase_1_is_told_selling_is_not_open()
    {
        using var factory = new TuTuMuchApiFactory(launchPhase: "1");
        using var client = factory.CreateMemberClient();
        await client.SignInUnverifiedMemberAsync();

        using var response = await client.PostAsJsonAsync("/api/listings", Listings.Complete());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("SELLING_NOT_OPEN", (await Problem.ReadAsync(response)).Code);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    public async Task The_administrator_lists_and_publishes_in_phases_1_and_2(string setting)
    {
        using var factory = new TuTuMuchApiFactory(launchPhase: setting);
        factory.EnsureDatabase();
        using var admin = factory.CreateAdministratorClient();
        var reference = await admin.CreateDraftWithPhotoAsync(Listings.Complete("Dance", "Tutus"));

        using var response = await admin.PublishAsync(reference);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Active", (await response.Content.ReadFromJsonAsync<PublishResult>())!.Status);
    }

    [Fact]
    public async Task Every_verified_member_sells_in_phase_3()
    {
        using var factory = new TuTuMuchApiFactory(launchPhase: "3");
        using var client = factory.CreateMemberClient();
        await client.SignInVerifiedMemberAsync(factory);
        var reference = await client.CreateDraftWithPhotoAsync(Listings.Complete());

        using var response = await client.PublishAsync(reference);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task In_phase_1_a_new_draft_is_saved_until_the_catalog_holds_39_listings()
    {
        using var factory = new TuTuMuchApiFactory(launchPhase: "1");
        factory.EnsureDatabase();
        using var admin = factory.CreateAdministratorClient();
        SeedListings(factory, 38);

        using var thirtyNinth = await admin.PostAsJsonAsync("/api/listings", Listings.Complete());
        using var fortieth = await admin.PostAsJsonAsync("/api/listings", Listings.Complete());

        Assert.Equal(HttpStatusCode.Created, thirtyNinth.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, fortieth.StatusCode);
        var problem = await Problem.ReadAsync(fortieth);
        Assert.Equal("PRODUCT_LIMIT_REACHED", problem.Code);
        Assert.Equal("The catalog is full for now", problem.Title);
        factory.InDatabase(db => Assert.Equal(39, db.Listings.Count()));
    }

    [Fact]
    public async Task In_phase_1_at_39_listings_an_existing_draft_still_saves_and_publishes()
    {
        using var factory = new TuTuMuchApiFactory(launchPhase: "1");
        factory.EnsureDatabase();
        using var admin = factory.CreateAdministratorClient();
        SeedListings(factory, 38);
        var reference = await admin.CreateDraftWithPhotoAsync(Listings.Complete());

        using var saved = await admin.PutAsJsonAsync($"/api/listings/{reference}", Listings.Complete().With("title", "Rainbow tulle tutu, lilac"));
        using var published = await admin.PublishAsync(reference);

        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
    }

    [Fact]
    public async Task In_phase_1_a_draft_does_not_publish_once_the_catalog_holds_40_listings()
    {
        using var factory = new TuTuMuchApiFactory(launchPhase: "1");
        factory.EnsureDatabase();
        using var admin = factory.CreateAdministratorClient();
        SeedListings(factory, 38);
        var reference = await admin.CreateDraftWithPhotoAsync(Listings.Complete());
        SeedListings(factory, 1, firstNumber: 21001);

        using var response = await admin.PublishAsync(reference);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("PRODUCT_LIMIT_REACHED", (await Problem.ReadAsync(response)).Code);
        factory.InDatabase(db => Assert.Equal(ListingStatus.Draft, db.Listings.Single(l => l.Reference == reference).Status));
    }

    [Fact]
    public async Task Phase_2_has_no_product_limit()
    {
        using var factory = new TuTuMuchApiFactory(launchPhase: "2");
        factory.EnsureDatabase();
        using var admin = factory.CreateAdministratorClient();
        SeedListings(factory, 45);

        using var response = await admin.PostAsJsonAsync("/api/listings", Listings.Complete());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Listings_of_members_stay_in_the_catalog_and_can_be_bought_after_a_move_back_to_phase_1()
    {
        using var factory = new TuTuMuchApiFactory(launchPhase: "1");
        using var buyer = factory.CreateMemberClient();
        await buyer.SignInVerifiedMemberAsync(factory);
        var reference = factory.SeedActiveListing();

        var catalog = await buyer.GetFromJsonAsync<CatalogCountView>("/api/catalog/listings/count");
        using var checkout = await buyer.StartCheckoutAsync(reference);

        Assert.Equal(1, catalog!.TotalCount);
        Assert.Equal(HttpStatusCode.Created, checkout.StatusCode);
    }

    private static Member SeedVerifiedMember(TuTuMuchApiFactory factory, string displayName)
    {
        Member member = null!;
        factory.InDatabase(db =>
        {
            member = Member.Register(displayName, $"{displayName}@example.com", "hash", DateTime.UtcNow);
            member.MarkEmailVerified(DateTime.UtcNow);
            db.Members.Add(member);
            db.SaveChanges();
        });
        return member;
    }

    /// <summary>Adds <paramref name="count"/> Active listings by one seller, numbered from <paramref name="firstNumber"/>.</summary>
    private static void SeedListings(TuTuMuchApiFactory factory, int count, long firstNumber = 20001)
    {
        var seller = SeedVerifiedMember(factory, $"seller_{firstNumber}");
        factory.InDatabase(db =>
        {
            for (var i = 0; i < count; i++)
            {
                var details = new ListingDetails("Clothing", "Dresses", null, "Floral party dress, pink", "Worn a handful of times. No stains.", "Gymboree", "Used - Excellent", null, 85m, 9m);
                var listing = Listing.CreateDraft(seller.Id, firstNumber + i, details, DateTime.UtcNow);
                Assert.Empty(listing.Publish(DateTime.UtcNow, photoCount: 1));
                db.Listings.Add(listing);
            }

            db.SaveChanges();
        });
    }

    private sealed record LaunchView(int Phase, bool CategoryMenusVisible, bool MemberSellingOpen);

    private sealed record MeView(string DisplayName, bool IsAdministrator);

    private sealed record CatalogCountView(int TotalCount);
}
