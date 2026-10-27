namespace TuTuMuch.Domain.Notifications;

/// <summary>
/// The groups of email that share one setting (L2-043). Account security and orders are always
/// on; reviews, marketing and item request matches (L2-103) are the member's choice.
/// </summary>
public enum NotificationCategory
{
    AccountSecurity,
    Orders,
    Reviews,
    Marketing,
    ItemRequests,
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
        NotificationType.ItemRequestMatched => NotificationCategory.ItemRequests,

        // Listing notices, such as the draft expiry reminder (L2-019), are transactional and always on.
        _ => NotificationCategory.Orders,
    };
}

/// <summary>
/// A member's email choices (L2-043, L2-103), one row per member. A member without a row has the
/// defaults: review and item request emails on, marketing off until they opt in. <see cref="Version"/> is the
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

    /// <summary>Whether item request match emails are sent (L2-103); on until the member turns them off.</summary>
    public bool ItemRequestEmails { get; private set; }

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
        ItemRequestEmails = true,
        UpdatedAt = now,
    };

    /// <summary>
    /// The settings page saved the optional choices; a marketing change records its time. A null
    /// <paramref name="itemRequestEmails"/> keeps the stored choice (L2-103).
    /// </summary>
    public void Update(bool reviewEmails, bool marketingEmails, DateTime now, bool? itemRequestEmails = null)
    {
        ReviewEmails = reviewEmails;
        ItemRequestEmails = itemRequestEmails ?? ItemRequestEmails;
        SetMarketing(marketingEmails, MarketingChangeSource.Settings, now);
        Touch(now);
    }

    /// <summary>The one-click unsubscribe: marketing off, from any state.</summary>
    public void UnsubscribeMarketing(DateTime now) => Unsubscribe(NotificationCategory.Marketing, now);

    /// <summary>A one-click unsubscribe from marketing or item request emails, from any state (L2-043, L2-103).</summary>
    public void Unsubscribe(NotificationCategory category, DateTime now)
    {
        switch (category)
        {
            case NotificationCategory.Marketing:
                SetMarketing(false, MarketingChangeSource.Unsubscribe, now);
                break;
            case NotificationCategory.ItemRequests:
                ItemRequestEmails = false;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(category), category, "Only marketing and item request emails have an unsubscribe link.");
        }

        Touch(now);
    }

    public bool IsAllowed(NotificationCategory category) => category switch
    {
        NotificationCategory.Reviews => ReviewEmails,
        NotificationCategory.Marketing => MarketingEmails,
        NotificationCategory.ItemRequests => ItemRequestEmails,
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
