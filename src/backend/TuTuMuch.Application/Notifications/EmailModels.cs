namespace TuTuMuch.Application.Notifications;

// The template model of each notification type (L2-042). No model has a property for a password,
// a card number or a security code: a card appears only as brand and last 4 digits, and a
// single-use token only inside the verification link that L2-002 requires.

public sealed record EmailVerificationEmail(string Link);

public sealed record SaleMadeEmail(Guid OrderId, string OrderReference, string ItemTitle, decimal Total, DateTime ShipBy);

public sealed record OrderShippedEmail(Guid OrderId, string OrderReference, string ItemTitle, string Carrier, string TrackingNumber, string TrackingUrl);

public sealed record OrderDeliveredEmail(Guid OrderId, string OrderReference, string ItemTitle);

public sealed record ShipByReminderEmail(Guid OrderId, string OrderReference, string ItemTitle, DateTime ShipBy);

/// <summary>
/// <paramref name="ToSeller"/> picks the seller's wording and the sale link instead of the buyer's
/// order link. <paramref name="Reason"/> and <paramref name="Note"/> are the seller's when the
/// seller cancelled (L2-035 AC1); both are null when the ship deadline passed.
/// </summary>
public sealed record OrderCancelledEmail(Guid OrderId, string OrderReference, string ItemTitle, bool ToSeller, string? Reason = null, string? Note = null);

/// <summary>The buyer asked the seller to cancel, with their optional message (L2-035 AC2).</summary>
public sealed record CancellationRequestedEmail(Guid OrderId, string OrderReference, string ItemTitle, string? Message);

public sealed record OrderRefundedEmail(Guid OrderId, string OrderReference, decimal Amount, string? CardBrand, string? CardLast4);

/// <summary>
/// The buyer reported a problem (L2-036 AC1). <paramref name="ToAdministrator"/> picks the
/// operations alert with the link to the problem page instead of the seller's sale link.
/// </summary>
public sealed record ProblemReportedEmail(Guid OrderId, string OrderReference, string ProblemReference, string ItemTitle, string Reason, string Description, bool ToAdministrator);

/// <summary>An administrator decided a problem; <paramref name="Decision"/> and <paramref name="Reason"/> go to both members (L2-036).</summary>
public sealed record ProblemResolvedEmail(Guid OrderId, string OrderReference, string ItemTitle, string Decision, string Reason, bool ToSeller);

/// <summary>
/// A marketing email (L2-043): sent only to members who opted in, always with the one-click
/// unsubscribe the dispatcher adds. Nothing queues it until marketing campaigns are built.
/// </summary>
public sealed record MarketingEmail(string Subject, string Text);

/// <summary>A seller's draft is deleted on <paramref name="DeleteOn"/> unless they edit it (L2-019).</summary>
public sealed record DraftExpiryReminderEmail(string ListingReference, string Title, DateTime DeleteOn);
