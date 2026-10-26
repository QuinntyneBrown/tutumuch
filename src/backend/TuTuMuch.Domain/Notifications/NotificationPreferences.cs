namespace TuTuMuch.Domain.Notifications;

/// <summary>
/// The groups of email that share one setting (L2-043). Account security and orders are always
/// on; reviews and marketing are the member's choice.
/// </summary>
public enum NotificationCategory
{
    AccountSecurity,
    Orders,
    Reviews,
    Marketing,
}

/// <summary>Where the last marketing change came from: the settings page or an unsubscribe link.</summary>
public enum MarketingChangeSource
{
    Settings,
    Unsubscribe,
}

/// <summary>The category each notification type belongs to, which decides whether a member may turn it off.</summary>
public static class NotificationCategories
{
    public static NotificationCategory Of(NotificationType type) => type switch
    {
        NotificationType.EmailVerification => NotificationCategory.AccountSecurity,
        NotificationType.Marketing => NotificationCategory.Marketing,

        // Listing notices, such as the draft expiry reminder (L2-019), are transactional and always on.
        _ => NotificationCategory.Orders,
    };
}

/// <summary>
/// A member's email choices (L2-043), one row per member. A member without a row has the
/// defaults: review emails on, marketing off until they opt in. <see cref="Version"/> is the
/// concurrency token the API exposes as the ETag.
/// </summary>
public sealed class NotificationPreferences
{
    private NotificationPreferences()
    {
    }

    public Guid MemberId { get; private set; }

    public bool ReviewEmails { get; private set; }

    public bool MarketingEmails { get; private set; }

    public DateTime? MarketingOptedInAt { get; private set; }

    public DateTime? MarketingOptedOutAt { get; private set; }

    public MarketingChangeSource? LastMarketingChangeSource { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public int Version { get; private set; }

    public static NotificationPreferences CreateDefault(Guid memberId, DateTime now) => new()
    {
        MemberId = memberId,
        ReviewEmails = true,
        MarketingEmails = false,
        UpdatedAt = now,
    };

    /// <summary>The settings page saved both optional choices; a marketing change records its time.</summary>
    public void Update(bool reviewEmails, bool marketingEmails, DateTime now)
    {
        ReviewEmails = reviewEmails;
        SetMarketing(marketingEmails, MarketingChangeSource.Settings, now);
        Touch(now);
    }

    /// <summary>The one-click unsubscribe: marketing off, from any state.</summary>
    public void UnsubscribeMarketing(DateTime now)
    {
        SetMarketing(false, MarketingChangeSource.Unsubscribe, now);
        Touch(now);
    }

    public bool IsAllowed(NotificationCategory category) => category switch
    {
        NotificationCategory.Reviews => ReviewEmails,
        NotificationCategory.Marketing => MarketingEmails,
        _ => true,
    };

    private void SetMarketing(bool on, MarketingChangeSource source, DateTime now)
    {
        if (on == MarketingEmails)
        {
            return;
        }

        MarketingEmails = on;
        LastMarketingChangeSource = source;
        if (on)
        {
            MarketingOptedInAt = now;
        }
        else
        {
            MarketingOptedOutAt = now;
        }
    }

    private void Touch(DateTime now)
    {
        UpdatedAt = now;
        Version++;
    }
}
