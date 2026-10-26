// Acceptance Test
// Traces to: L2-031
// Description: A seller onboards with the payment provider through a provider-hosted link and the
// signed account.updated webhook. Every order gets a payout held until the order is Completed; the
// fee (10% of item plus shipping) is recorded on the order. The release job pays only Completed
// orders of onboarded sellers, once per payout, and the summary shows paid out, pending, the bank,
// the history and the banner.

using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutuMuch.Api.Tests.Support;
using TutuMuch.Application.Payouts;
using TutuMuch.Domain.Orders;
using TutuMuch.Domain.Payouts;

namespace TutuMuch.Api.Tests;

public sealed class PayoutTests : IDisposable
{
    private readonly TutuMuchApiFactory _factory = new();
    private readonly HttpClient _buyer;

    public PayoutTests()
    {
        _buyer = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task A_seller_gets_a_provider_hosted_onboarding_link_and_one_connected_account()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (_, seller) = await _factory.SeedPaidOrderAsync(_buyer);

        var first = await seller.OnboardingLinkAsync();
        var second = await seller.OnboardingLinkAsync();

        Assert.StartsWith("https://payments.example.com/onboarding/acct_", first.Url);
        Assert.Equal(first.Url, second.Url);
        Assert.Single(_factory.PayoutProvider.Accounts);
        Assert.All(_factory.PayoutProvider.Links, link => Assert.Equal("https://tutumuch.test/account/payouts?onboarding=return", link.ReturnUrl));
        var summary = await seller.PayoutSummaryAsync();
        Assert.Equal("Pending", summary.Onboarding);
        Assert.Equal("warning", summary.Banner);
    }

    [Fact]
    public async Task An_unavailable_provider_answers_502()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        _factory.PayoutProvider.Unavailable = true;

        using var response = await _buyer.PostAsync("/api/payouts/onboarding-link", null);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("PAYOUT_PROVIDER_UNAVAILABLE", (await Problem.ReadAsync(response)).Code);
    }

    [Fact]
    public async Task Visitors_cannot_read_payouts()
    {
        using var visitor = _factory.CreateMemberClient();

        using var response = await visitor.GetAsync("/api/payouts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_seller_without_sales_has_an_empty_summary_and_no_banner()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);

        var summary = await _buyer.PayoutSummaryAsync();

        Assert.Equal(new PayoutSummaryView("0.00", "0.00", "NotStarted", null, null, []), summary with { History = [] });
        Assert.Empty(summary.History);
    }

    [Fact]
    public async Task Every_order_records_the_fee_and_a_held_payout()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);

        _factory.InDatabase(db =>
        {
            var order = db.Orders.Single(o => o.Id == orderId);
            Assert.Equal(0.10m, order.FeeRate);
            Assert.Equal(9.40m, order.FeeAmount);
            var payout = db.Payouts.Single(p => p.OrderId == orderId);
            Assert.Equal(PayoutStatus.Held, payout.Status);
            Assert.Equal(PayoutHoldReason.OrderNotCompleted, payout.HoldReason);
            Assert.Equal(94.00m, payout.GrossAmount);
            Assert.Equal(84.60m, payout.NetAmount);
        });
        var summary = await seller.PayoutSummaryAsync();
        Assert.Equal("0.00", summary.PaidOut);
        Assert.Equal("84.60", summary.Pending);
        Assert.Equal("NotStarted", summary.Onboarding);
        Assert.Equal("warning", summary.Banner);
        var row = Assert.Single(summary.History);
        Assert.Equal(orderId, row.OrderId);
        Assert.Equal("Held", row.Status);
        Assert.Equal("OrderNotCompleted", row.HoldReason);
        Assert.Equal("84.60", row.Amount);
        Assert.Equal("9.40", row.Fee);
        Assert.Equal("Floral party dress, pink", row.Item.Title);
    }

    [Fact]
    public async Task The_job_releases_nothing_for_paid_shipped_or_delivered_orders()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (_, seller) = await _factory.SeedPaidOrderAsync(_buyer, 30001);
        var (shipped, seller2) = await _factory.SeedPaidOrderAsync(_buyer, 30002);
        var (delivered, seller3) = await _factory.SeedPaidOrderAsync(_buyer, 30003);
        await _factory.DeliverAsync(delivered, seller3, Now());
        (await seller2.AddTrackingAsync(shipped)).EnsureSuccessStatusCode();
        foreach (var client in new[] { seller, seller2, seller3 })
        {
            await _factory.OnboardAsync(client);
        }

        Assert.Equal(0, await ReleaseAsync());

        Assert.Empty(_factory.PayoutProvider.Requests);
        _factory.InDatabase(db => Assert.All(db.Payouts, p => Assert.Equal(PayoutStatus.Held, p.Status)));
    }

    [Fact]
    public async Task A_completed_order_of_an_onboarded_seller_is_paid_once_with_the_payout_id_as_key()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        await _factory.OnboardAsync(seller);
        await _factory.DeliverAsync(orderId, seller, Now());
        (await _buyer.ConfirmReceiptAsync(orderId)).EnsureSuccessStatusCode();

        Assert.Equal(1, await ReleaseAsync());
        Assert.Equal(0, await ReleaseAsync());

        var payoutId = Guid.Empty;
        _factory.InDatabase(db =>
        {
            var payout = db.Payouts.Single();
            Assert.Equal(PayoutStatus.Sent, payout.Status);
            Assert.Equal(Now(), payout.SentAt);
            payoutId = payout.Id;
        });
        var request = Assert.Single(_factory.PayoutProvider.Requests);
        Assert.Equal((FakePayoutProvider.AccountIdOf(SellerIdOf(orderId)), 84.60m, payoutId), request);
        var sent = await seller.PayoutSummaryAsync();
        Assert.Equal("info", sent.Banner);
        Assert.Equal("84.60", sent.Pending);
        Assert.Equal("Chase •••• 6021", sent.BankDisplay);

        (await _factory.PostPayoutWebhookAsync(FakePayoutProvider.PayoutPaid(payoutId))).EnsureSuccessStatusCode();

        var paid = await seller.PayoutSummaryAsync();
        Assert.Equal("84.60", paid.PaidOut);
        Assert.Equal("0.00", paid.Pending);
        Assert.Null(paid.Banner);
        var row = Assert.Single(paid.History);
        Assert.Equal("Paid", row.Status);
        Assert.Equal(Now(), row.PaidAt);
    }

    [Fact]
    public async Task A_completed_order_waits_for_onboarding_and_is_paid_after_it()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        await _factory.DeliverAsync(orderId, seller, Now());
        (await _buyer.ConfirmReceiptAsync(orderId)).EnsureSuccessStatusCode();

        Assert.Equal(0, await ReleaseAsync());
        _factory.InDatabase(db => Assert.Equal(PayoutHoldReason.OnboardingIncomplete, db.Payouts.Single().HoldReason));

        await _factory.OnboardAsync(seller);

        Assert.Equal(1, await ReleaseAsync());
        Assert.Single(_factory.PayoutProvider.Payouts);
    }

    [Fact]
    public async Task A_completion_whose_event_was_lost_is_still_paid_by_the_job()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        await _factory.OnboardAsync(seller);
        await _factory.DeliverAsync(orderId, seller, Now());
        _factory.InDatabase(db =>
        {
            // Completed without publishing OrderCompleted, as a crash after the save would leave it.
            db.Orders.Include(o => o.StatusChanges).Single(o => o.Id == orderId).Complete(OrderActor.System, Now());
            db.SaveChanges();
        });

        Assert.Equal(1, await ReleaseAsync());
        Assert.Single(_factory.PayoutProvider.Payouts);
    }

    [Fact]
    public async Task A_failed_payout_shows_the_danger_banner()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        await _factory.OnboardAsync(seller);
        await _factory.DeliverAsync(orderId, seller, Now());
        (await _buyer.ConfirmReceiptAsync(orderId)).EnsureSuccessStatusCode();
        await ReleaseAsync();
        var payoutId = Guid.Empty;
        _factory.InDatabase(db => payoutId = db.Payouts.Single().Id);

        (await _factory.PostPayoutWebhookAsync(FakePayoutProvider.PayoutFailed(payoutId))).EnsureSuccessStatusCode();

        var summary = await seller.PayoutSummaryAsync();
        Assert.Equal("danger", summary.Banner);
        Assert.Equal("Failed", Assert.Single(summary.History).Status);
    }

    [Fact]
    public async Task An_invalid_payout_webhook_signature_is_rejected_and_a_repeat_is_ignored()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (_, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        await seller.OnboardingLinkAsync();
        var body = FakePayoutProvider.AccountUpdated(_factory.PayoutProvider.Accounts.Keys.Single());

        using var invalid = await _factory.PostPayoutWebhookAsync(body, "bad");
        using var first = await _factory.PostPayoutWebhookAsync(body);
        using var repeat = await _factory.PostPayoutWebhookAsync(body);

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, repeat.StatusCode);
        Assert.Equal("Complete", (await seller.PayoutSummaryAsync()).Onboarding);
    }

    [Fact]
    public async Task The_sale_of_a_completed_order_shows_the_payout_after_the_fee()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        await _factory.OnboardAsync(seller);
        await _factory.DeliverAsync(orderId, seller, Now());
        (await _buyer.ConfirmReceiptAsync(orderId)).EnsureSuccessStatusCode();
        await ReleaseAsync();

        var sale = (await seller.GetFromJsonAsync<SaleWithPayoutView>($"/api/sales/{orderId}"))!;

        Assert.Equal(new SalePayoutView("94.00", 10m, "9.40", "84.60", "Sent", Now(), "Chase •••• 6021"), sale.Payout);
    }

    [Fact]
    public async Task An_onboarded_seller_is_not_prompted_for_payouts_when_publishing()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (_, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        await _factory.OnboardAsync(seller);
        var sellerId = Guid.Empty;
        _factory.InDatabase(db => sellerId = db.Orders.Single().SellerId);

        using var scope = _factory.Services.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<TutuMuch.Application.Abstractions.ISellerPayoutAccountReader>();

        Assert.True(await reader.IsOnboardedAsync(sellerId, CancellationToken.None));
    }

    public void Dispose()
    {
        _buyer.Dispose();
        _factory.Dispose();
    }

    private Guid SellerIdOf(Guid orderId)
    {
        var sellerId = Guid.Empty;
        _factory.InDatabase(db => sellerId = db.Orders.Single(o => o.Id == orderId).SellerId);
        return sellerId;
    }

    private async Task<int> ReleaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new ReleasePayoutsCommand());
    }

    private DateTime Now() => _factory.Clock.GetUtcNow().UtcDateTime;
}
