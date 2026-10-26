using System.Globalization;
using System.Text.Json;
using TutuMuch.Domain.Notifications;

namespace TutuMuch.Application.Notifications;

/// <summary>
/// A rendered email before it gets its recipient address. <paramref name="Identifiers"/> are the
/// values the template put in that are known not to be secrets, such as a tracking number, which
/// the content guard skips.
/// </summary>
public sealed record RenderedEmail(string Subject, string Body, IReadOnlyList<string> Identifiers);

/// <summary>
/// The plain-text template of each notification type (L2-042). Every email greets the member,
/// says what happened and links to the related page in Tutu Much, built from <c>App:BaseUrl</c>.
/// Final copy is still to be supplied; the subject "You made a sale" comes from L2-042.
/// </summary>
public static class EmailTemplates
{
    private const string Footer = "\n\n12L, the marketplace for size 12 shoes and size L clothing.\nManage your emails: {0}/account/emails";

    public static Type ModelType(NotificationType type) => type switch
    {
        NotificationType.EmailVerification => typeof(EmailVerificationEmail),
        NotificationType.SaleMade => typeof(SaleMadeEmail),
        NotificationType.OrderShipped => typeof(OrderShippedEmail),
        NotificationType.OrderDelivered => typeof(OrderDeliveredEmail),
        NotificationType.ShipByReminder => typeof(ShipByReminderEmail),
        NotificationType.OrderCancelled => typeof(OrderCancelledEmail),
        NotificationType.OrderRefunded => typeof(OrderRefundedEmail),
        NotificationType.CancellationRequested => typeof(CancellationRequestedEmail),
        NotificationType.ProblemReported => typeof(ProblemReportedEmail),
        NotificationType.ProblemResolved => typeof(ProblemResolvedEmail),
        NotificationType.Marketing => typeof(MarketingEmail),
        NotificationType.DraftExpiryReminder => typeof(DraftExpiryReminderEmail),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "No template for this notification type."),
    };

    public static RenderedEmail Render(NotificationType type, string payload, string displayName, string baseUrl)
    {
        var root = baseUrl.TrimEnd('/');
        var model = JsonSerializer.Deserialize(payload, ModelType(type), EmailOutbox.Json)
            ?? throw new InvalidOperationException("The outbox message has no model.");
        var (subject, text) = model switch
        {
            EmailVerificationEmail m => (
                "Verify your email for Tutu Much",
                $"Open this link to verify your email and start buying and selling:\n{m.Link}\n\nThe link works for 24 hours."),
            SaleMadeEmail m => (
                "You made a sale",
                $"{m.ItemTitle} sold for {Money(m.Total)} (order {m.OrderReference}).\n\nShip it by {Day(m.ShipBy)} and add the tracking number:\n{root}/sales/{m.OrderId}"),
            OrderShippedEmail m => (
                $"Your order {m.OrderReference} has shipped",
                $"{m.ItemTitle} is on its way with {m.Carrier}, tracking number {m.TrackingNumber}.\nTrack it: {m.TrackingUrl}\n\nSee your order:\n{root}/orders/{m.OrderId}"),
            OrderDeliveredEmail m => (
                $"Your order {m.OrderReference} was delivered",
                $"{m.ItemTitle} was delivered. Confirm you received it, or report a problem within 3 days:\n{root}/orders/{m.OrderId}"),
            ShipByReminderEmail m => (
                $"Ship order {m.OrderReference} by {Day(m.ShipBy)}",
                $"{m.ItemTitle} has not shipped yet. Add the tracking number by {Day(m.ShipBy)}, or the order is cancelled and the buyer refunded:\n{root}/sales/{m.OrderId}"),
            OrderCancelledEmail { ToSeller: true } m => (
                $"Order {m.OrderReference} was cancelled",
                $"Order {m.OrderReference} for {m.ItemTitle} was cancelled because it was not shipped in time. The buyer gets a full refund.\n{root}/sales/{m.OrderId}"),
            OrderCancelledEmail { Reason: { } reason } m => (
                $"Your order {m.OrderReference} was cancelled",
                $"The seller cancelled {m.ItemTitle}. Reason: {reason}.{(m.Note is null ? string.Empty : $"\nTheir note: “{m.Note}”")}\nYou get a full refund.\n{root}/orders/{m.OrderId}"),
            OrderCancelledEmail m => (
                $"Your order {m.OrderReference} was cancelled",
                $"The seller did not ship {m.ItemTitle} in time, so your order was cancelled. You get a full refund.\n{root}/orders/{m.OrderId}"),
            OrderRefundedEmail m => (
                $"Your refund for order {m.OrderReference}",
                $"We refunded {Money(m.Amount)} to {Card(m.CardBrand, m.CardLast4)}. It can take 5 to 10 business days to show.\n{root}/orders/{m.OrderId}"),
            CancellationRequestedEmail m => (
                $"The buyer asked to cancel order {m.OrderReference}",
                $"The buyer of {m.ItemTitle} asked you to cancel order {m.OrderReference}.{(m.Message is null ? string.Empty : $"\n“{m.Message}”")}\n\nAccept to refund them in full, or decline and ship it:\n{root}/sales/{m.OrderId}"),
            ProblemReportedEmail { ToAdministrator: true } m => (
                $"Problem {m.ProblemReference} reported on order {m.OrderReference}",
                $"The buyer of {m.ItemTitle} reported a problem: {m.Reason}.\n“{m.Description}”\n\nReview it and decide:\n{root}/admin/problems/{m.ProblemReference}"),
            ProblemReportedEmail m => (
                $"The buyer reported a problem with order {m.OrderReference}",
                $"The buyer of {m.ItemTitle} reported a problem: {m.Reason}.\n“{m.Description}”\n\nYour payout is on hold while our team reviews it. You can reply once:\n{root}/sales/{m.OrderId}"),
            ProblemResolvedEmail m => (
                $"We decided the problem with order {m.OrderReference}",
                $"Our team reviewed the problem with {m.ItemTitle} and decided to {m.Decision}.\nReason: {m.Reason}\n{root}/{(m.ToSeller ? "sales" : "orders")}/{m.OrderId}"),
            MarketingEmail m => (m.Subject, m.Text),
            DraftExpiryReminderEmail m => (
                $"Your draft {m.Title} will be deleted on {Day(m.DeleteOn)}",
                $"You have not edited your draft {m.Title} for a while. Drafts not edited for 90 days are deleted with their photos.\n\nEdit or publish it before {Day(m.DeleteOn)} to keep it:\n{root}/sell/{m.ListingReference}"),
            _ => throw new InvalidOperationException($"No template for {type}."),
        };

        IReadOnlyList<string> identifiers = model switch
        {
            EmailVerificationEmail m => [m.Link],
            OrderShippedEmail m => [m.TrackingNumber, m.TrackingUrl],
            _ => [],
        };
        return new RenderedEmail(subject, $"Hi {displayName},\n\n{text}{string.Format(CultureInfo.InvariantCulture, Footer, root)}", identifiers);
    }

    private static string Money(decimal amount) => "$" + amount.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Day(DateTime at) => at.ToString("dddd, MMMM d", CultureInfo.InvariantCulture);

    private static string Card(string? brand, string? last4) =>
        brand is null || last4 is null ? "your card" : $"{brand} ending {last4}";
}
