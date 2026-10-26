// Acceptance Test
// Traces to: L2-043
// Description: A member reads and saves their email settings at /api/me/notification-preferences:
// account security and order emails are always on and no request can turn them off, review emails
// are on and marketing emails off until the member opts in, and a save needs the version it read
// (If-Match). The dispatcher honours the stored choice at send time, and every marketing email
// carries a one-click unsubscribe link and List-Unsubscribe headers that turn marketing off
// without signing in.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TuTuMuch.Api.Tests.Support;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Notifications;
using TuTuMuch.Domain.Notifications;

namespace TuTuMuch.Api.Tests;

public sealed class NotificationPreferencesTests : IDisposable
{
    private const string Email = "marcus.bell@example.com";

    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _member;

    public NotificationPreferencesTests()
    {
        _member = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task A_new_member_has_security_and_order_emails_locked_on_reviews_on_and_marketing_off()
    {
        await _member.SignInVerifiedMemberAsync(_factory);

        using var response = await _member.GetAsync("/api/me/notification-preferences");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("\"0\"", response.Headers.ETag?.Tag);
        var preferences = (await response.Content.ReadFromJsonAsync<PreferencesView>())!;
        Assert.Equal(
            [new("accountSecurity", true, true), new("orders", true, true), new("reviews", true, false), new("marketing", false, false)],
            preferences.Categories);
    }

    [Fact]
    public async Task A_visitor_cannot_read_or_save_email_settings()
    {
        var visitor = _factory.CreateMemberClient();

        using var read = await visitor.GetAsync("/api/me/notification-preferences");
        using var save = await visitor.PutAsJsonAsync("/api/me/notification-preferences", new { reviewEmails = false, marketingEmails = false });

        Assert.Equal(HttpStatusCode.Unauthorized, read.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, save.StatusCode);
    }

    [Fact]
    public async Task Saving_turns_reviews_off_and_marketing_on_but_never_the_always_on_categories()
    {
        await _member.SignInVerifiedMemberAsync(_factory);
        var etag = await ReadEtagAsync();

        using var response = await SaveAsync(new { reviewEmails = false, marketingEmails = true, accountSecurity = false, orders = false }, etag);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("\"1\"", response.Headers.ETag?.Tag);
        var saved = (await response.Content.ReadFromJsonAsync<PreferencesView>())!;
        Assert.Equal(
            [new("accountSecurity", true, true), new("orders", true, true), new("reviews", false, false), new("marketing", true, false)],
            saved.Categories);
        _factory.InDatabase(db =>
        {
            var row = Assert.Single(db.NotificationPreferences);
            Assert.False(row.ReviewEmails);
            Assert.True(row.MarketingEmails);
            Assert.NotNull(row.MarketingOptedInAt);
            Assert.Equal(MarketingChangeSource.Settings, row.LastMarketingChangeSource);
        });
    }

    [Fact]
    public async Task A_save_needs_the_version_it_read_and_a_stale_one_is_refused()
    {
        await _member.SignInVerifiedMemberAsync(_factory);
        var etag = await ReadEtagAsync();
        using var first = await SaveAsync(new { reviewEmails = false, marketingEmails = false }, etag);
        first.EnsureSuccessStatusCode();

        using var stale = await SaveAsync(new { reviewEmails = true, marketingEmails = true }, etag);
        using var missing = await SaveAsync(new { reviewEmails = true, marketingEmails = true }, null);
        using var invalid = await SaveAsync(new { reviewEmails = true }, "\"1\"");

        Assert.Equal(HttpStatusCode.PreconditionFailed, stale.StatusCode);
        Assert.Equal("NOTIFICATION_PREFERENCES_CHANGED", (await Problem.ReadAsync(stale)).Code);
        Assert.Equal((HttpStatusCode)428, missing.StatusCode);
        Assert.Equal("PRECONDITION_REQUIRED", (await Problem.ReadAsync(missing)).Code);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        Assert.Equal("NOTIFICATION_PREFERENCES_INVALID", (await Problem.ReadAsync(invalid)).Code);
        _factory.InDatabase(db => Assert.False(db.NotificationPreferences.Single().MarketingEmails));
    }

    [Fact]
    public async Task The_dispatcher_does_not_send_marketing_to_a_member_who_has_not_opted_in_but_still_sends_order_emails()
    {
        await _member.SignInVerifiedMemberAsync(_factory);
        await _factory.DispatchEmailsAsync();
        var memberId = MemberId();

        QueueMarketing(memberId);
        Queue(NotificationType.OrderDelivered, memberId, new OrderDeliveredEmail(Guid.NewGuid(), "TM-10001", "Floral party dress, pink"));
        var sent = await _factory.DispatchEmailsAsync();

        var email = Assert.Single(sent);
        Assert.Contains("delivered", email.Subject, StringComparison.Ordinal);
        _factory.InDatabase(db =>
        {
            var marketing = db.OutboxMessages.Single(m => m.NotificationType == NotificationType.Marketing);
            Assert.Equal(OutboxMessageStatus.Suppressed, marketing.Status);
            Assert.Equal("preference_off", marketing.SuppressionReason);
        });
    }

    [Fact]
    public async Task A_marketing_email_carries_a_one_click_unsubscribe_that_works_without_signing_in()
    {
        await OptInToMarketingAsync();
        QueueMarketing(MemberId());

        var email = Assert.Single(await _factory.DispatchEmailsAsync());
        var link = UnsubscribeLink(email.Body);
        Assert.StartsWith("https://tutumuch.test/emails/unsubscribe?token=", link, StringComparison.Ordinal);
        Assert.NotNull(email.Headers);
        Assert.Equal("List-Unsubscribe=One-Click", email.Headers!["List-Unsubscribe-Post"]);
        Assert.Matches("^<https://.+/api/notifications/unsubscribe\\?token=.+>$", email.Headers["List-Unsubscribe"]);
        Assert.DoesNotContain(Email, link, StringComparison.OrdinalIgnoreCase);

        var anonymous = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var token = Uri.UnescapeDataString(link[(link.IndexOf("token=", StringComparison.Ordinal) + "token=".Length)..]);
        using var response = await anonymous.PostAsJsonAsync("/api/notifications/unsubscribe", new { token });
        var oneClick = new Uri(email.Headers["List-Unsubscribe"].Trim('<', '>'));
        using var repeated = await anonymous.PostAsync(oneClick.PathAndQuery, new FormUrlEncodedContent([new("List-Unsubscribe", "One-Click")]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"category\":\"Marketing\",\"unsubscribed\":true}", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        _factory.InDatabase(db =>
        {
            var row = db.NotificationPreferences.Single();
            Assert.False(row.MarketingEmails);
            Assert.Equal(MarketingChangeSource.Unsubscribe, row.LastMarketingChangeSource);
        });
        QueueMarketing(MemberId());
        Assert.Empty(await _factory.DispatchEmailsAsync());
    }

    [Fact]
    public async Task A_tampered_or_missing_unsubscribe_token_is_refused_and_changes_nothing()
    {
        await OptInToMarketingAsync();
        QueueMarketing(MemberId());
        var link = UnsubscribeLink(Assert.Single(await _factory.DispatchEmailsAsync()).Body);
        var token = Uri.UnescapeDataString(link[(link.IndexOf("token=", StringComparison.Ordinal) + "token=".Length)..]);
        var tampered = token[..^2] + (token[^2] == 'A' ? "BB" : "AA");
        var anonymous = _factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        using var bad = await anonymous.PostAsJsonAsync("/api/notifications/unsubscribe", new { token = tampered });
        using var none = await anonymous.PostAsJsonAsync("/api/notifications/unsubscribe", new { token = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal("UNSUBSCRIBE_LINK_INVALID", (await Problem.ReadAsync(bad)).Code);
        Assert.Equal(HttpStatusCode.BadRequest, none.StatusCode);
        _factory.InDatabase(db => Assert.True(db.NotificationPreferences.Single().MarketingEmails));
    }

    public void Dispose()
    {
        _member.Dispose();
        _factory.Dispose();
    }

    private async Task OptInToMarketingAsync()
    {
        await _member.SignInVerifiedMemberAsync(_factory);
        await _factory.DispatchEmailsAsync();
        using var response = await SaveAsync(new { reviewEmails = true, marketingEmails = true }, await ReadEtagAsync());
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> ReadEtagAsync()
    {
        using var response = await _member.GetAsync("/api/me/notification-preferences");
        response.EnsureSuccessStatusCode();
        return response.Headers.ETag!.Tag;
    }

    private Task<HttpResponseMessage> SaveAsync(object body, string? etag)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/me/notification-preferences") { Content = JsonContent.Create(body) };
        if (etag is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", etag);
        }

        return _member.SendAsync(request);
    }

    private Guid MemberId()
    {
        var id = Guid.Empty;
        _factory.InDatabase(db => id = db.Members.Single(m => m.Email == Email).Id);
        return id;
    }

    private void QueueMarketing(Guid memberId) =>
        Queue(NotificationType.Marketing, memberId, new MarketingEmail("New in your size", "Fresh party dresses this week."));

    private void Queue<TModel>(NotificationType type, Guid memberId, TModel model)
        where TModel : class
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IEmailOutbox>().Enqueue(type, memberId, model);
        scope.ServiceProvider.GetRequiredService<ITuTuMuchDbContext>().SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    private static string UnsubscribeLink(string body)
    {
        var start = body.IndexOf("https://tutumuch.test/emails/unsubscribe?token=", StringComparison.Ordinal);
        Assert.True(start >= 0, "the body has the unsubscribe link");
        var end = body.IndexOfAny([' ', '\n', '\r'], start);
        return end < 0 ? body[start..] : body[start..end];
    }

    private sealed record PreferencesView(IReadOnlyList<CategoryView> Categories);

    private sealed record CategoryView(string Key, bool Enabled, bool Locked);
}
