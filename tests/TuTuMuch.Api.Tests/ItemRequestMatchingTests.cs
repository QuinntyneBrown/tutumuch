// Acceptance Test
// Traces to: L2-098, L2-100, L2-101, L2-102, L2-103, L2-106
// Description: When a listing becomes Active by publish or relist it is matched against open item
// requests by category, price, condition and every keyword word, never against the seller's own,
// an expired or a closed member's request, and a request and a listing are matched once. Each
// member gets one email per matching listing through the outbox, unless they turned item request
// emails off; the email's one-click unsubscribe turns them off without signing in. My requests
// shows the matches still Active, and the clean-up job deletes requests expired 30 days ago.

using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TuTuMuch.Api.Tests.Support;
using TuTuMuch.Application.Requests;
using TuTuMuch.Domain.Notifications;

namespace TuTuMuch.Api.Tests;

public sealed class ItemRequestMatchingTests : IDisposable
{
    private const string Title = "Rainbow tulle tutu, pink";

    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _shopper;
    private readonly HttpClient _seller;

    public ItemRequestMatchingTests()
    {
        _shopper = _factory.CreateMemberClient();
        _seller = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task Publishing_a_matching_listing_records_a_match_and_emails_the_member_once()
    {
        await SignInBothAsync();
        await _shopper.CreateItemRequestAsync(ItemRequests.Body(category: "Clothing", keywords: "TULLE tutu", maxPrice: 60m, condition: "Used - Good"));
        await _shopper.CreateItemRequestAsync(ItemRequests.Body(category: "Clothing", keywords: "pink"));

        var reference = await PublishAsync();

        _factory.InDatabase(db => Assert.Equal(2, db.ItemRequestMatches.Count()));
        var email = Assert.Single(await _factory.DispatchEmailsAsync(), e => e.To == "pia.reyes@example.com");
        Assert.Equal($"A match for your request: {Title}", email.Subject);
        Assert.Contains("$60.00", email.Body, StringComparison.Ordinal);
        Assert.Contains($"https://tutumuch.test/items/{reference}", email.Body, StringComparison.Ordinal);
        Assert.Contains("https://tutumuch.test/account/requests", email.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("marcus.bell@example.com", email.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("List-Unsubscribe=One-Click", email.Headers!["List-Unsubscribe-Post"]);
        Assert.Matches("^<https://.+/api/notifications/unsubscribe\\?token=.+>$", email.Headers["List-Unsubscribe"]);
    }

    [Theory]
    [InlineData("Dance", "tulle tutu", null, null)]
    [InlineData("Clothing", "tulle tutu", "59.99", null)]
    [InlineData("Clothing", "tulle tutu", null, "Used - Excellent")]
    [InlineData("Clothing", "tulle tutu leotard", null, null)]
    public async Task A_listing_that_misses_any_part_of_the_rule_does_not_match(string category, string keywords, string? maxPrice, string? condition)
    {
        await SignInBothAsync();
        await _shopper.CreateItemRequestAsync(ItemRequests.Body(category, keywords, maxPrice is null ? null : decimal.Parse(maxPrice, System.Globalization.CultureInfo.InvariantCulture), condition));

        await PublishAsync();

        _factory.InDatabase(db => Assert.Empty(db.ItemRequestMatches));
        Assert.DoesNotContain(await _factory.DispatchEmailsAsync(), e => e.Subject.StartsWith("A match", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_sellers_own_request_an_expired_request_and_a_closed_members_request_never_match()
    {
        await SignInBothAsync();
        await _seller.CreateItemRequestAsync(ItemRequests.Body(category: "Clothing", keywords: "tulle tutu"));
        await _shopper.CreateItemRequestAsync(ItemRequests.Body(category: "Clothing", keywords: "tulle tutu"));
        var closed = _factory.CreateMemberClient();
        await closed.SignInVerifiedMemberAsync(_factory, "tall_tom", "tom@example.com");
        await closed.CreateItemRequestAsync(ItemRequests.Body(category: "Clothing", keywords: "rainbow tutu"));
        var now = _factory.Clock.GetUtcNow().UtcDateTime;
        _factory.InDatabase(db =>
        {
            var shopperId = db.Members.Single(m => m.Email == "pia.reyes@example.com").Id;
            var expired = db.ItemRequests.Single(r => r.MemberId == shopperId);
            db.Entry(expired).Property(r => r.ExpiresAt).CurrentValue = now.AddMinutes(-1);
            db.Members.Single(m => m.Email == "tom@example.com").Close(now);
            db.SaveChanges();
        });

        await PublishAsync();

        _factory.InDatabase(db => Assert.Empty(db.ItemRequestMatches));
    }

    [Fact]
    public async Task Launch_phase_1_matches_the_administrators_listings()
    {
        using var factory = new TuTuMuchApiFactory(launchPhase: "1");
        var shopper = factory.CreateMemberClient();
        await shopper.SignInVerifiedMemberAsync(factory, "pia_reyes", "pia.reyes@example.com");
        await shopper.CreateItemRequestAsync(ItemRequests.Body(category: "Clothing", keywords: "tulle tutu"));
        using var admin = factory.CreateAdministratorClient();

        var reference = await admin.CreateDraftWithPhotoAsync(Listings.Complete());
        (await admin.PublishAsync(reference)).EnsureSuccessStatusCode();

        factory.InDatabase(db => Assert.Single(db.ItemRequestMatches));
    }

    [Fact]
    public async Task Relisting_matches_new_requests_but_never_a_request_and_listing_twice()
    {
        await _shopper.SignInVerifiedMemberAsync(_factory, "pia_reyes", "pia.reyes@example.com");
        await _shopper.CreateItemRequestAsync(ItemRequests.Body(category: "Clothing", keywords: "party dress"));
        var buyer = _factory.CreateMemberClient();
        await buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(buyer);
        (await seller.CancelOrderAsync(orderId, "ListedByMistake")).EnsureSuccessStatusCode();
        string reference = null!;
        _factory.InDatabase(db => reference = db.Listings.Single().Reference);
        await _factory.DispatchEmailsAsync();

        using var relist = await seller.RelistAsync(reference);

        Assert.Equal(HttpStatusCode.NoContent, relist.StatusCode);
        Assert.Single(await _factory.DispatchEmailsAsync(), e => e.Subject == "A match for your request: Floral party dress, pink");

        var later = _factory.CreateMemberClient();
        await later.SignInVerifiedMemberAsync(_factory, "tall_tom", "tom@example.com");
        await later.CreateItemRequestAsync(ItemRequests.Body(category: "Clothing", keywords: "floral dress"));
        var (checkout, payment) = await buyer.CheckoutAndPayAsync(reference);
        (await buyer.PostWebhookAsync(FakePaymentProvider.Webhook(payment.Id))).EnsureSuccessStatusCode();
        var secondOrder = (await buyer.GetFromJsonAsync<CheckoutView>($"/api/checkouts/{checkout.Id}"))!.OrderId!.Value;
        (await seller.CancelOrderAsync(secondOrder, "ListedByMistake")).EnsureSuccessStatusCode();
        await _factory.DispatchEmailsAsync();

        (await seller.RelistAsync(reference)).EnsureSuccessStatusCode();

        var matchEmails = (await _factory.DispatchEmailsAsync()).Where(e => e.Subject.StartsWith("A match", StringComparison.Ordinal)).ToList();
        Assert.Equal("tom@example.com", Assert.Single(matchEmails).To);
        _factory.InDatabase(db => Assert.Equal(2, db.ItemRequestMatches.Count()));
    }

    [Fact]
    public async Task My_requests_shows_matches_while_they_are_active_and_counts_those_that_went()
    {
        await SignInBothAsync();
        await _shopper.CreateItemRequestAsync(ItemRequests.Body(category: "Clothing", keywords: "tulle tutu"));
        var reference = await PublishAsync();

        var request = Assert.Single((await _shopper.ListItemRequestsAsync()).Items);
        Assert.Equal(1, request.MatchCount);
        var card = Assert.Single(request.Matches);
        Assert.Equal(reference, card.Reference);
        Assert.Equal(Title, card.Title);
        Assert.Equal("60.00", card.Price);

        (await _seller.PostAsync($"/api/listings/{reference}/withdraw", null)).EnsureSuccessStatusCode();

        request = Assert.Single((await _shopper.ListItemRequestsAsync()).Items);
        Assert.Equal(1, request.MatchCount);
        Assert.Empty(request.Matches);
    }

    [Fact]
    public async Task A_seller_sees_nothing_about_who_requested_their_listing()
    {
        await SignInBothAsync();
        await _shopper.CreateItemRequestAsync(ItemRequests.Body(category: "Clothing", keywords: "tulle tutu"));
        var reference = await PublishAsync();

        var listing = await _seller.GetStringAsync($"/api/me/listings/{reference}");
        var mine = await _seller.GetStringAsync("/api/me/listings");
        var detail = await _seller.GetStringAsync($"/api/catalog/listings/{reference}");

        foreach (var body in new[] { listing, mine, detail })
        {
            Assert.DoesNotContain("pia", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("request", body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Turning_item_request_emails_off_suppresses_the_email_but_still_records_the_match()
    {
        await SignInBothAsync();
        await _shopper.CreateItemRequestAsync(ItemRequests.Body(category: "Clothing", keywords: "tulle tutu"));
        using var read = await _shopper.GetAsync("/api/me/notification-preferences");
        var etag = read.Headers.ETag!.Tag;
        using var save = new HttpRequestMessage(HttpMethod.Put, "/api/me/notification-preferences")
        {
            Content = JsonContent.Create(new { reviewEmails = true, marketingEmails = false, itemRequestEmails = false }),
        };
        save.Headers.IfMatch.ParseAdd(etag);
        (await _shopper.SendAsync(save)).EnsureSuccessStatusCode();

        await PublishAsync();

        Assert.DoesNotContain(await _factory.DispatchEmailsAsync(), e => e.Subject.StartsWith("A match", StringComparison.Ordinal));
        _factory.InDatabase(db =>
        {
            Assert.Single(db.ItemRequestMatches);
            var message = db.OutboxMessages.Single(m => m.NotificationType == NotificationType.ItemRequestMatched);
            Assert.Equal(OutboxMessageStatus.Suppressed, message.Status);
            Assert.Equal("preference_off", message.SuppressionReason);
        });
    }

    [Fact]
    public async Task The_match_emails_one_click_unsubscribe_turns_item_request_emails_off_without_signing_in()
    {
        await SignInBothAsync();
        await _shopper.CreateItemRequestAsync(ItemRequests.Body(category: "Clothing", keywords: "tulle tutu"));
        await PublishAsync();
        var email = Assert.Single(await _factory.DispatchEmailsAsync(), e => e.Subject.StartsWith("A match", StringComparison.Ordinal));
        var link = email.Body.Split('\n').Select(l => l.Trim()).Last(l => l.Contains("/emails/unsubscribe?token=", StringComparison.Ordinal));
        link = link[link.IndexOf("https://", StringComparison.Ordinal)..];
        var token = Uri.UnescapeDataString(link[(link.IndexOf("token=", StringComparison.Ordinal) + "token=".Length)..]);
        var anonymous = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        using var response = await anonymous.PostAsJsonAsync("/api/notifications/unsubscribe", new { token });
        var oneClick = new Uri(email.Headers!["List-Unsubscribe"].Trim('<', '>'));
        using var repeated = await anonymous.PostAsync(oneClick.PathAndQuery, new FormUrlEncodedContent([new("List-Unsubscribe", "One-Click")]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"category\":\"ItemRequests\",\"unsubscribed\":true}", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        using var settings = await _shopper.GetAsync("/api/me/notification-preferences");
        var categories = (await settings.Content.ReadFromJsonAsync<PreferencesView>())!.Categories;
        Assert.Equal(
            [new("accountSecurity", true, true), new("orders", true, true), new("itemRequests", false, false), new("reviews", true, false), new("marketing", false, false)],
            categories);
    }

    [Fact]
    public async Task Requests_expired_30_days_ago_are_deleted_with_their_matches()
    {
        await SignInBothAsync();
        await _shopper.CreateItemRequestAsync(ItemRequests.Body(category: "Clothing", keywords: "tulle tutu"));
        await _shopper.CreateItemRequestAsync(ItemRequests.Body(category: "Clothing", keywords: "rainbow tutu"));
        await PublishAsync();
        var now = _factory.Clock.GetUtcNow().UtcDateTime;
        _factory.InDatabase(db =>
        {
            db.Entry(db.ItemRequests.Single(r => r.Keywords == "tulle tutu")).Property(r => r.ExpiresAt).CurrentValue = now.AddDays(-30);
            db.Entry(db.ItemRequests.Single(r => r.Keywords == "rainbow tutu")).Property(r => r.ExpiresAt).CurrentValue = now.AddDays(-29);
            db.SaveChanges();
        });

        using var scope = _factory.Services.CreateScope();
        var deleted = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CleanUpItemRequestsCommand());

        Assert.Equal(1, deleted);
        _factory.InDatabase(db =>
        {
            Assert.Equal("rainbow tutu", db.ItemRequests.Single().Keywords);
            Assert.Single(db.ItemRequestMatches);
        });
    }

    public void Dispose()
    {
        _shopper.Dispose();
        _seller.Dispose();
        _factory.Dispose();
    }

    private async Task SignInBothAsync()
    {
        await _seller.SignInVerifiedMemberAsync(_factory);
        await _shopper.SignInVerifiedMemberAsync(_factory, "pia_reyes", "pia.reyes@example.com");
        await _factory.DispatchEmailsAsync();
    }

    /// <summary>The seller publishes a complete Clothing listing: "Rainbow tulle tutu, pink", Gymboree, Used - Good, $60.</summary>
    private async Task<string> PublishAsync()
    {
        var reference = await _seller.CreateDraftWithPhotoAsync(Listings.Complete());
        (await _seller.PublishAsync(reference)).EnsureSuccessStatusCode();
        return reference;
    }

    private sealed record PreferencesView(IReadOnlyList<CategoryView> Categories);

    private sealed record CategoryView(string Key, bool Enabled, bool Locked);
}
