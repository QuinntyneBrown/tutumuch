namespace TuTuMuch.Domain.Notifications;

/// <summary>
/// The emails TuTuMuch sends (L2-042) for events that exist in the code. Each has a template and a deep
/// link; types for features not built yet (password reset, lockout) are added
/// with those features. Marketing is the only type a member can turn off today (L2-043); nothing
/// queues it until marketing campaigns are built.
/// </summary>
public enum NotificationType
{
    EmailVerification,
    SaleMade,
    OrderShipped,
    OrderDelivered,
    ShipByReminder,
    OrderCancelled,
    OrderRefunded,
    CancellationRequested,
    ProblemReported,
    ProblemResolved,
    Marketing,
    DraftExpiryReminder,

    /// <summary>A listing that became Active matched the member's open item requests (L2-101).</summary>
    ItemRequestMatched,
}
