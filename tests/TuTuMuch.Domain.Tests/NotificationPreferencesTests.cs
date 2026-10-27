// Unit Test
// Traces to: L2-043, L2-103
// Description: Account security and order emails are always allowed; review emails follow the
// member's choice and are on by default; marketing emails are off until the member opts in, and an
// unsubscribe turns them off again and records where the change came from.

using TuTuMuch.Domain.Notifications;

namespace TuTuMuch.Domain.Tests;

public sealed class NotificationPreferencesTests
{
    private static readonly DateTime Now = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Defaults_allow_security_order_and_review_emails_but_not_marketing()
    {
        var preferences = NotificationPreferences.CreateDefault(Guid.NewGuid(), Now);

        Assert.True(preferences.IsAllowed(NotificationCategory.AccountSecurity));
        Assert.True(preferences.IsAllowed(NotificationCategory.Orders));
        Assert.True(preferences.IsAllowed(NotificationCategory.Reviews));
        Assert.False(preferences.IsAllowed(NotificationCategory.Marketing));
    }

    [Fact]
    public void Turning_everything_optional_off_leaves_security_and_order_emails_on()
    {
        var preferences = NotificationPreferences.CreateDefault(Guid.NewGuid(), Now);

        preferences.Update(reviewEmails: false, marketingEmails: false, Now);

        Assert.False(preferences.IsAllowed(NotificationCategory.Reviews));
        Assert.True(preferences.IsAllowed(NotificationCategory.AccountSecurity));
        Assert.True(preferences.IsAllowed(NotificationCategory.Orders));
    }

    [Fact]
    public void Opting_in_and_unsubscribing_record_the_time_and_the_source()
    {
        var preferences = NotificationPreferences.CreateDefault(Guid.NewGuid(), Now);

        preferences.Update(reviewEmails: true, marketingEmails: true, Now);
        Assert.Equal(Now, preferences.MarketingOptedInAt);
        Assert.Equal(MarketingChangeSource.Settings, preferences.LastMarketingChangeSource);
        Assert.Equal(1, preferences.Version);

        preferences.UnsubscribeMarketing(Now.AddDays(1));
        Assert.False(preferences.IsAllowed(NotificationCategory.Marketing));
        Assert.Equal(Now.AddDays(1), preferences.MarketingOptedOutAt);
        Assert.Equal(MarketingChangeSource.Unsubscribe, preferences.LastMarketingChangeSource);
        Assert.Equal(2, preferences.Version);
    }

    [Fact]
    public void Every_notification_type_has_a_category_and_only_marketing_is_optional_today()
    {
        foreach (var type in Enum.GetValues<NotificationType>())
        {
            var category = NotificationCategories.Of(type);
            Assert.Equal(type == NotificationType.Marketing, category == NotificationCategory.Marketing);
            Assert.NotEqual(NotificationCategory.Reviews, category);
        }

        Assert.Equal(NotificationCategory.AccountSecurity, NotificationCategories.Of(NotificationType.EmailVerification));
        Assert.Equal(NotificationCategory.Orders, NotificationCategories.Of(NotificationType.SaleMade));
    }

    [Fact]
    public void Item_request_emails_are_on_by_default_kept_when_not_sent_and_turned_off_by_an_unsubscribe()
    {
        var preferences = NotificationPreferences.CreateDefault(Guid.NewGuid(), Now);
        Assert.True(preferences.IsAllowed(NotificationCategory.ItemRequests));
        Assert.Equal(NotificationCategory.ItemRequests, NotificationCategories.Of(NotificationType.ItemRequestMatched));

        preferences.Update(reviewEmails: true, marketingEmails: true, Now);
        Assert.True(preferences.IsAllowed(NotificationCategory.ItemRequests));

        preferences.Unsubscribe(NotificationCategory.ItemRequests, Now);
        Assert.False(preferences.IsAllowed(NotificationCategory.ItemRequests));
        Assert.True(preferences.IsAllowed(NotificationCategory.Marketing));

        preferences.Update(reviewEmails: true, marketingEmails: true, Now, itemRequestEmails: true);
        Assert.True(preferences.IsAllowed(NotificationCategory.ItemRequests));
    }
}
