// Acceptance Test
// Traces to: L2-042, L2-033
// Description: Every order event that exists in Tutu Much writes an email request to the outbox with the
// change, and the dispatcher sends it through the email provider port: "You made a sale" to the
// seller, shipping with tracking, delivery, cancellation and refund to the buyer, and the ship-by
// reminder to the seller after 3 business days. Each email links to its page in Tutu Much and carries
// no password, card number or security code. A failed send is retried with back-off for 24 hours.

using System.Net;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TutuMuch.Api.Tests.Support;
using TutuMuch.Application.Abstractions;
using TutuMuch.Application.Orders;
using TutuMuch.Domain.Notifications;

namespace TutuMuch.Api.Tests;

public sealed partial class EmailNotificationTests : IDisposable
{
    private const string Seller = "kicks30001@example.com";
    private const string Buyer = "marcus.bell@example.com";

    private readonly TutuMuchApiFactory _factory = new();
    private readonly HttpClient _buyer;

    public EmailNotificationTests()
    {
        _buyer = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task A_purchase_queues_and_sends_the_seller_a_sale_email_that_links_to_the_sale()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        await _factory.DispatchEmailsAsync();
        var (orderId, _) = await _factory.SeedPaidOrderAsync(_buyer);

        _factory.InDatabase(db =>
        {
            var queued = Assert.Single(db.OutboxMessages, m => m.Status == OutboxMessageStatus.Pending);
            Assert.Equal(NotificationType.SaleMade, queued.NotificationType);
            Assert.True(queued.NextAttemptAt <= queued.CreatedAt, "due at once, so the next poll sends it");
        });
        var sent = await _factory.DispatchEmailsAsync();

        var email = Assert.Single(sent);
        Assert.Equal(Seller, email.To);
        Assert.Equal("You made a sale", email.Subject);
        Assert.Contains("TM-10001", email.Body);
        Assert.Contains($"https://tutumuch.test/sales/{orderId}", email.Body);
        AssertNoSecrets(email);
        _factory.InDatabase(db => Assert.All(db.OutboxMessages, m => Assert.Equal(OutboxMessageStatus.Sent, m.Status)));
    }

    [Fact]
    public async Task Shipping_emails_the_buyer_the_tracking_number_and_delivery_emails_the_buyer_again()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        await _factory.DispatchEmailsAsync();

        await _factory.DeliverAsync(orderId, seller, _factory.Clock.GetUtcNow().UtcDateTime);
        var sent = await _factory.DispatchEmailsAsync();

        Assert.Equal(2, sent.Count);
        var shipped = Assert.Single(sent, e => e.Subject.Contains("shipped", StringComparison.Ordinal));
        Assert.Equal(Buyer, shipped.To);
        Assert.Contains("9400111899223456120477", shipped.Body);
        Assert.Contains($"https://tutumuch.test/orders/{orderId}", shipped.Body);
        var delivered = Assert.Single(sent, e => e.Subject.Contains("delivered", StringComparison.Ordinal));
        Assert.Equal(Buyer, delivered.To);
        Assert.Contains($"https://tutumuch.test/orders/{orderId}", delivered.Body);
        Assert.All(sent, AssertNoSecrets);
    }

    [Fact]
    public async Task A_missed_ship_deadline_emails_the_cancellation_to_both_and_the_refund_to_the_buyer()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, _) = await _factory.SeedPaidOrderAsync(_buyer);
        await _factory.DispatchEmailsAsync();
        _factory.Clock.Advance(TimeSpan.FromDays(9));

        await SendAsync(new CancelUnshippedOrdersCommand());
        var sent = await _factory.DispatchEmailsAsync();

        var cancelled = sent.Where(e => e.Subject.Contains("cancelled", StringComparison.Ordinal)).ToList();
        Assert.Equal([Seller, Buyer], cancelled.Select(e => e.To).Order());
        Assert.Contains(cancelled, e => e.To == Buyer && e.Body.Contains($"https://tutumuch.test/orders/{orderId}"));
        Assert.Contains(cancelled, e => e.To == Seller && e.Body.Contains($"https://tutumuch.test/sales/{orderId}"));
        var refunded = Assert.Single(sent, e => e.Subject.Contains("refund", StringComparison.Ordinal));
        Assert.Equal(Buyer, refunded.To);
        Assert.Contains("$102.34", refunded.Body);
        Assert.Contains("Visa ending 4242", refunded.Body);
        Assert.All(sent, AssertNoSecrets);
    }

    [Fact]
    public async Task The_reminder_job_reminds_an_unshipped_seller_once_after_3_business_days()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, _) = await _factory.SeedPaidOrderAsync(_buyer);
        await _factory.DispatchEmailsAsync();

        Assert.Equal(0, await SendAsync(new SendShipRemindersCommand()));
        _factory.Clock.Advance(TimeSpan.FromDays(5.1));
        Assert.Equal(1, await SendAsync(new SendShipRemindersCommand()));
        Assert.Equal(0, await SendAsync(new SendShipRemindersCommand()));
        var sent = await _factory.DispatchEmailsAsync();

        var reminder = Assert.Single(sent);
        Assert.Equal(Seller, reminder.To);
        Assert.Contains("Ship", reminder.Subject);
        Assert.Contains($"https://tutumuch.test/sales/{orderId}", reminder.Body);
    }

    [Fact]
    public async Task A_shipped_order_gets_no_reminder()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        (await seller.AddTrackingAsync(orderId)).EnsureSuccessStatusCode();
        _factory.Clock.Advance(TimeSpan.FromDays(6));

        Assert.Equal(0, await SendAsync(new SendShipRemindersCommand()));
    }

    [Fact]
    public async Task A_failed_send_is_retried_with_back_off_and_each_failure_is_logged()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        await _factory.DispatchEmailsAsync();
        await _factory.SeedPaidOrderAsync(_buyer);
        _factory.Email.FailWith = new HttpRequestException("provider unavailable", null, HttpStatusCode.ServiceUnavailable);

        Assert.Empty(await _factory.DispatchEmailsAsync());
        _factory.InDatabase(db =>
        {
            var message = db.OutboxMessages.Single(m => m.NotificationType == NotificationType.SaleMade);
            Assert.Equal(OutboxMessageStatus.Pending, message.Status);
            Assert.Equal(1, message.AttemptCount);
            Assert.InRange(message.NextAttemptAt - _factory.Clock.GetUtcNow().UtcDateTime, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1.2));
            Assert.Contains("503", message.LastError);
        });
        Assert.Empty(await _factory.DispatchEmailsAsync());

        _factory.Email.FailWith = null;
        _factory.Clock.Advance(TimeSpan.FromMinutes(2));
        var sent = Assert.Single(await _factory.DispatchEmailsAsync());
        Assert.Equal("You made a sale", sent.Subject);
    }

    [Fact]
    public async Task A_message_still_failing_after_24_hours_fails_for_good()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        await _factory.DispatchEmailsAsync();
        await _factory.SeedPaidOrderAsync(_buyer);
        _factory.Email.FailWith = new HttpRequestException("timeout");

        for (var hour = 0; hour < 26; hour++)
        {
            await _factory.DispatchEmailsAsync();
            _factory.Clock.Advance(TimeSpan.FromHours(1));
        }

        _factory.InDatabase(db =>
        {
            var message = db.OutboxMessages.Single(m => m.NotificationType == NotificationType.SaleMade);
            Assert.Equal(OutboxMessageStatus.Failed, message.Status);
            Assert.InRange(message.AttemptCount, 5, 30);
        });
    }

    [Fact]
    public async Task A_rejected_address_fails_at_once()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        await _factory.DispatchEmailsAsync();
        await _factory.SeedPaidOrderAsync(_buyer);
        _factory.Email.FailWith = new EmailRejectedException("422 address rejected");

        await _factory.DispatchEmailsAsync();

        _factory.InDatabase(db => Assert.Equal(OutboxMessageStatus.Failed, db.OutboxMessages.Single(m => m.NotificationType == NotificationType.SaleMade).Status));
    }

    [Fact]
    public async Task The_provider_gets_the_outbox_message_as_idempotency_key()
    {
        await _buyer.RegisterAsync();

        var email = Assert.Single(await _factory.DispatchEmailsAsync());

        _factory.InDatabase(db => Assert.Equal(db.OutboxMessages.Single().Id, email.IdempotencyKey));
    }

    private static void AssertNoSecrets(EmailMessage email)
    {
        var content = email.Subject + "\n" + email.Body;
        Assert.DoesNotContain(Members.Password, content);
        Assert.DoesNotMatch(CardNumber(), content);
        Assert.DoesNotContain("CVV", content, StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"(?<!\d)\d{13,19}(?!\d)")]
    private static partial Regex CardNumber();

    private async Task<int> SendAsync(IRequest<int> command)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(command);
    }

    public void Dispose()
    {
        _buyer.Dispose();
        _factory.Dispose();
    }
}
