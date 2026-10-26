// Unit Test
// Traces to: L2-042
// Description: Every notification type renders with a link to its page in TuTuMuch and passes the
// content guard; the guard refuses a card number that passes the Luhn check, a password and a
// security code, and lets tracking numbers, order references and amounts through.

using TuTuMuch.Application.Notifications;
using TuTuMuch.Domain.Notifications;

namespace TuTuMuch.Application.Tests;

public sealed class EmailTemplateTests
{
    private static readonly Guid OrderId = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");

    public static TheoryData<NotificationType, object, string> Emails => new()
    {
        { NotificationType.EmailVerification, new EmailVerificationEmail("https://tutumuch.test/verify?token=abc"), "https://tutumuch.test/verify?token=abc" },
        { NotificationType.SaleMade, new SaleMadeEmail(OrderId, "TM-10001", "Jordan 1 Retro", 102.34m, new DateTime(2026, 11, 2)), $"https://tutumuch.test/sales/{OrderId}" },
        { NotificationType.OrderShipped, new OrderShippedEmail(OrderId, "TM-10001", "Jordan 1 Retro", "USPS", "9400111899223456120477", "https://tools.usps.com/x"), $"https://tutumuch.test/orders/{OrderId}" },
        { NotificationType.OrderDelivered, new OrderDeliveredEmail(OrderId, "TM-10001", "Jordan 1 Retro"), $"https://tutumuch.test/orders/{OrderId}" },
        { NotificationType.ShipByReminder, new ShipByReminderEmail(OrderId, "TM-10001", "Jordan 1 Retro", new DateTime(2026, 11, 2)), $"https://tutumuch.test/sales/{OrderId}" },
        { NotificationType.OrderCancelled, new OrderCancelledEmail(OrderId, "TM-10001", "Jordan 1 Retro", ToSeller: false), $"https://tutumuch.test/orders/{OrderId}" },
        { NotificationType.OrderCancelled, new OrderCancelledEmail(OrderId, "TM-10001", "Jordan 1 Retro", ToSeller: true), $"https://tutumuch.test/sales/{OrderId}" },
        { NotificationType.OrderCancelled, new OrderCancelledEmail(OrderId, "TM-10001", "Jordan 1 Retro", ToSeller: false, "Item damaged", "Sorry"), $"https://tutumuch.test/orders/{OrderId}" },
        { NotificationType.CancellationRequested, new CancellationRequestedEmail(OrderId, "TM-10001", "Jordan 1 Retro", "Wrong colour"), $"https://tutumuch.test/sales/{OrderId}" },
        { NotificationType.OrderRefunded, new OrderRefundedEmail(OrderId, "TM-10001", 102.34m, "Visa", "4242"), $"https://tutumuch.test/orders/{OrderId}" },
    };

    [Theory]
    [MemberData(nameof(Emails))]
    public void Every_email_links_to_its_page_and_passes_the_guard(NotificationType type, object model, string link)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(model, model.GetType(), new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        var email = EmailTemplates.Render(type, payload, "marcus_12", "https://tutumuch.test/");

        Assert.Contains(link, email.Body);
        Assert.StartsWith("Hi marcus_12,", email.Body);
        Assert.Contains("https://tutumuch.test/account/emails", email.Body);
        Assert.Null(EmailContentGuard.Check(email.Subject + "\n" + email.Body, email.Identifiers));
    }

    [Fact]
    public void Digits_of_an_order_id_or_a_tracking_number_never_pass_for_a_card_number()
    {
        // Both hold a run of digits that passes the Luhn check.
        var orderId = Guid.Parse("0f8fad5b-4242-4242-4242-4242abcdefab");
        var model = new OrderShippedEmail(orderId, "TM-10001", "Jordan 1 Retro", "FedEx", "123456789010002", "https://www.fedex.com/fedextrack/?trknbr=123456789010002");
        var payload = System.Text.Json.JsonSerializer.Serialize(model, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        var email = EmailTemplates.Render(NotificationType.OrderShipped, payload, "marcus_12", "https://tutumuch.test");

        Assert.Equal("card_number", EmailContentGuard.Check("Card 4242-4242-4242-4242"));
        Assert.Equal("card_number", EmailContentGuard.Check("Tracking 123456789010002"));
        Assert.Null(EmailContentGuard.Check($"https://tutumuch.test/orders/{orderId}"));
        Assert.Null(EmailContentGuard.Check(email.Subject + "\n" + email.Body, email.Identifiers));
    }

    [Fact]
    public void Every_notification_type_has_a_template()
    {
        Assert.All(Enum.GetValues<NotificationType>(), type => Assert.NotNull(EmailTemplates.ModelType(type)));
    }

    [Theory]
    [InlineData("Card 4242424242424242 on file", "card_number")]
    [InlineData("Card 4242 4242 4242 4242 on file", "card_number")]
    [InlineData("Card 4242-4242-4242-4242", "card_number")]
    [InlineData("Your password: hunter2", "password")]
    [InlineData("CVV 123", "security_code")]
    [InlineData("cvc: 1234", "security_code")]
    public void The_guard_refuses_secrets(string content, string reason)
    {
        Assert.Equal(reason, EmailContentGuard.Check(content));
    }

    [Theory]
    [InlineData("Tracking 9400111899223456120477")]
    [InlineData("Order TM-10001 for $102.34")]
    [InlineData("Visa ending 4242")]
    [InlineData("Number 4242424242424241 fails Luhn")]
    [InlineData("Reset your password at https://tutumuch.test")]
    public void The_guard_lets_ordinary_content_through(string content)
    {
        Assert.Null(EmailContentGuard.Check(content));
    }
}
