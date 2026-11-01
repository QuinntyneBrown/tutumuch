// Acceptance Test
// Traces to: L2-117, L2-118, L2-119, L2-120, L2-121
// Description: On an administrator's find that offers a grab, a verified member sees the shelf price,
// grab fee, tax, total and non-refundable deposit, ticks the acknowledgement and pays the total once;
// the webhook makes the grab Requested and emails the poster. TuTuMuch accepts, declines, buys or
// can't get it; the shopper cancels with a full refund before acceptance and loses only the deposit
// after; the deadline job expires missed grabs with full refunds. Grabs are private to the shopper
// and administrators.

using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TuTuMuch.Api.Tests.Support;
using TuTuMuch.Application.Grabs;
using TuTuMuch.Domain.Finds;
using TuTuMuch.Domain.Notifications;
using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Api.Tests;

public sealed class GrabsTests : IDisposable
{
    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _admin;
    private readonly HttpClient _shopper;
    private readonly HttpClient _provider;

    public GrabsTests()
    {
        _factory.EnsureDatabase();
        _admin = _factory.CreateAdministratorClient();
        _shopper = _factory.CreateVerifiedClient("marcus_12");
        _provider = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task The_quote_shows_price_fee_tax_total_and_deposit()
    {
        var find = await GrabbableAsync();

        using var response = await _shopper.QuoteGrabAsync(find);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            new GrabQuoteView(find, "Glitter ballet flats, pink", "Target", "40.00", "8.00", "4.26", "52.26", "8.00", "44.26"),
            await response.Content.ReadFromJsonAsync<GrabQuoteView>());
    }

    [Theory]
    [InlineData("12.00", "5.00")]
    [InlineData("3.00", "3.00")]
    public async Task The_deposit_is_at_least_5_dollars_and_never_more_than_the_price(string price, string deposit)
    {
        var find = await GrabbableAsync(decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture));

        using var response = await _shopper.QuoteGrabAsync(find);

        Assert.Equal(deposit, (await response.Content.ReadFromJsonAsync<GrabQuoteView>())!.Deposit);
    }

    [Fact]
    public async Task Paying_without_acknowledging_the_deposit_is_refused()
    {
        var find = await GrabbableAsync();

        using var response = await _shopper.StartGrabAsync(find, "52.26", acknowledged: false);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("DEPOSIT_NOT_ACKNOWLEDGED", (await Problem.ReadAsync(response)).Code);
        Assert.Empty(_factory.Payments.Charges);
    }

    [Fact]
    public async Task A_confirmed_payment_makes_the_grab_requested_and_emails_the_poster()
    {
        var find = await GrabbableAsync();

        var pending = await _shopper.PaidGrabAsync(find);
        Assert.Equal("PaymentPending", pending.Status);
        Assert.Equal(52.26m, Assert.Single(_factory.Payments.Charges).Value.Amount);
        using var webhook = await _provider.PostWebhookAsync(FakePaymentProvider.Webhook(Finds.PaymentIdOf(_factory, pending.Id)));

        Assert.Equal(HttpStatusCode.OK, webhook.StatusCode);
        var grab = (await _shopper.GetFromJsonAsync<GrabView>($"/api/me/grabs/{pending.Id}"))!;
        Assert.Equal("Requested", grab.Status);
        Assert.Equal("GR-1001", grab.Reference);
        Assert.Equal("52.26", grab.RefundIfCancelled);
        var email = Assert.Single(await _factory.DispatchEmailsAsync());
        Assert.Equal("ana_ruiz@tutumuch.test", email.To);
        Assert.Equal("marcus_12 asked TuTuMuch to grab Glitter ballet flats, pink", email.Subject);
    }

    [Fact]
    public async Task A_declined_card_frees_the_find_and_the_shopper_can_try_again()
    {
        var find = await GrabbableAsync();
        var first = await _shopper.PaidGrabAsync(find);

        (await _provider.PostWebhookAsync(FakePaymentProvider.Webhook(Finds.PaymentIdOf(_factory, first.Id), "Insufficient funds."))).EnsureSuccessStatusCode();

        var failed = (await _shopper.GetFromJsonAsync<GrabView>($"/api/me/grabs/{first.Id}"))!;
        Assert.Equal("PaymentFailed", failed.Status);
        Assert.Equal("Insufficient funds.", failed.PaymentDeclineReason);
        Assert.False((await _shopper.GetFromJsonAsync<FindView>($"/api/finds/{find}"))!.GrabOpen);
        var retry = await _shopper.PaidGrabAsync(find);
        Assert.Equal("PaymentPending", retry.Status);
    }

    [Fact]
    public async Task The_same_idempotency_key_charges_once()
    {
        var find = await GrabbableAsync();
        var key = Guid.NewGuid();

        using var first = await _shopper.StartGrabAsync(find, "52.26", key: key);
        using var second = await _shopper.StartGrabAsync(find, "52.26", key: key);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Single(_factory.Payments.Charges);
        _factory.InDatabase(db => Assert.Single(db.Grabs));
    }

    [Fact]
    public async Task Another_open_grab_blocks_a_second_one()
    {
        var find = await GrabbableAsync();
        await _shopper.PaidGrabAsync(find);
        using var other = _factory.CreateVerifiedClient("other_shopper");

        using var quote = await other.QuoteGrabAsync(find);
        using var pay = await other.StartGrabAsync(find, "52.26");

        Assert.Equal(HttpStatusCode.Conflict, quote.StatusCode);
        Assert.Equal("GRAB_IN_PROGRESS", (await Problem.ReadAsync(quote)).Code);
        Assert.Equal(HttpStatusCode.Conflict, pay.StatusCode);
    }

    [Fact]
    public async Task Of_many_shoppers_paying_at_once_exactly_one_grab_is_stored()
    {
        using var factory = new TuTuMuchApiFactory(concurrent: true);
        factory.EnsureDatabase();
        using var admin = factory.CreateAdministratorClient();
        var find = (await admin.PostedFindAsync(Finds.Find(factory, grabFee: 8m))).Reference;
        var shoppers = Enumerable.Range(0, 10).Select(i => factory.CreateVerifiedClient($"racer_{i}")).ToList();

        var responses = await Task.WhenAll(shoppers.Select(s => s.StartGrabAsync(find, "52.26")));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.Created), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        factory.InDatabase(db => Assert.Single(db.Grabs));
    }

    [Fact]
    public async Task Own_finds_finds_without_an_offer_and_gone_finds_are_refused()
    {
        var mine = await GrabbableAsync();
        var noOffer = (await _admin.PostedFindAsync(Finds.Find(_factory))).Reference;
        var gone = await GrabbableAsync();
        (await _admin.PostAsync($"/api/finds/{gone}/gone", null)).EnsureSuccessStatusCode();

        using var own = await _admin.QuoteGrabAsync(mine);
        using var notOffered = await _shopper.QuoteGrabAsync(noOffer);
        using var missing = await _shopper.QuoteGrabAsync(gone);

        Assert.Equal("CANNOT_GRAB_OWN_FIND", (await Problem.ReadAsync(own)).Code);
        Assert.Equal(HttpStatusCode.Conflict, notOffered.StatusCode);
        Assert.Equal("GRAB_NOT_OFFERED", (await Problem.ReadAsync(notOffered)).Code);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("FIND_NOT_FOUND", (await Problem.ReadAsync(missing)).Code);
    }

    [Fact]
    public async Task A_changed_total_is_refused_and_nothing_is_charged()
    {
        var find = await GrabbableAsync();

        using var response = await _shopper.StartGrabAsync(find, "52.25");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("TOTAL_CHANGED", (await Problem.ReadAsync(response)).Code);
        Assert.Empty(_factory.Payments.Charges);
    }

    [Fact]
    public async Task Visitors_unverified_members_and_non_us_addresses_are_refused()
    {
        var find = await GrabbableAsync();
        var unverified = _factory.CreateMemberClient();
        await unverified.SignInUnverifiedMemberAsync("not_yet", "not.yet@example.com");

        using var anonymous = await _factory.CreateMemberClient().QuoteGrabAsync(find);
        using var forbidden = await unverified.QuoteGrabAsync(find);
        using var abroad = await _shopper.QuoteGrabAsync(find, Checkouts.Brooklyn(country: "CA"));

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal("EMAIL_NOT_VERIFIED", (await Problem.ReadAsync(forbidden)).Code);
        Assert.Equal("NON_US_ADDRESS", (await Problem.ReadAsync(abroad)).Code);
    }

    [Fact]
    public async Task Accepting_then_buying_marks_the_find_gone_and_emails_the_shopper()
    {
        var find = await GrabbableAsync();
        var grab = await _shopper.RequestedGrabAsync(_factory, find);
        await _factory.DispatchEmailsAsync();

        using var accepted = await _admin.AdminActAsync(grab.Id, "accept");
        var acceptedView = (await accepted.Content.ReadFromJsonAsync<AdminGrabView>())!;
        using var bought = await _admin.AdminActAsync(grab.Id, "bought");

        Assert.Equal("Accepted", acceptedView.Status);
        Assert.NotNull(acceptedView.Address);
        Assert.Equal("Bought", (await bought.Content.ReadFromJsonAsync<AdminGrabView>())!.Status);
        Assert.Equal("Gone", (await _shopper.GetFromJsonAsync<FindView>($"/api/finds/{find}"))!.Status);
        var emails = await _factory.DispatchEmailsAsync();
        Assert.Equal(["TuTuMuch accepted your grab GR-1001", "TuTuMuch has Glitter ballet flats, pink"], emails.Select(e => e.Subject).ToArray());
    }

    [Fact]
    public async Task Declining_refunds_the_total()
    {
        var grab = await _shopper.RequestedGrabAsync(_factory, await GrabbableAsync());

        using var response = await _admin.AdminActAsync(grab.Id, "decline");

        Assert.Equal("Declined", (await response.Content.ReadFromJsonAsync<AdminGrabView>())!.Status);
        Assert.Equal(52.26m, Assert.Single(_factory.Payments.Refunds).Amount);
        Assert.Equal("52.26", (await _shopper.GetFromJsonAsync<GrabView>($"/api/me/grabs/{grab.Id}"))!.RefundedAmount);
    }

    [Fact]
    public async Task Couldnt_get_it_refunds_the_total_marks_the_find_gone_and_says_why()
    {
        var find = await GrabbableAsync();
        var grab = await _shopper.RequestedGrabAsync(_factory, find);
        (await _admin.AdminActAsync(grab.Id, "accept")).EnsureSuccessStatusCode();
        await _factory.DispatchEmailsAsync();

        using var response = await _admin.AdminActAsync(grab.Id, "unavailable", "PriceWentUp");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(52.26m, Assert.Single(_factory.Payments.Refunds).Amount);
        Assert.Equal("Gone", (await _shopper.GetFromJsonAsync<FindView>($"/api/finds/{find}"))!.Status);
        var email = Assert.Single(await _factory.DispatchEmailsAsync());
        Assert.Contains("TuTuMuch couldn't get it (Price went up). You've been refunded $52.26.", email.Body);
    }

    [Fact]
    public async Task An_action_that_does_not_fit_the_status_is_refused()
    {
        var grab = await _shopper.RequestedGrabAsync(_factory, await GrabbableAsync());
        (await _admin.AdminActAsync(grab.Id, "decline")).EnsureSuccessStatusCode();

        using var response = await _admin.AdminActAsync(grab.Id, "accept");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("GRAB_STATE_CHANGED", (await Problem.ReadAsync(response)).Code);
    }

    [Fact]
    public async Task Only_administrators_handle_grabs()
    {
        var grab = await _shopper.RequestedGrabAsync(_factory, await GrabbableAsync());

        using var list = await _shopper.GetAsync("/api/admin/grabs");
        using var accept = await _shopper.AdminActAsync(grab.Id, "accept");

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal("ADMINISTRATOR_REQUIRED", (await Problem.ReadAsync(accept)).Code);
    }

    [Fact]
    public async Task The_admin_list_shows_the_shopper_and_hides_the_address_until_accepted()
    {
        var grab = await _shopper.RequestedGrabAsync(_factory, await GrabbableAsync());

        var row = Assert.Single((await _admin.GetFromJsonAsync<List<AdminGrabView>>("/api/admin/grabs"))!);

        Assert.Equal(new AdminGrabView(grab.Id, "GR-1001", "Requested", "marcus_12", "marcus_12@example.com", "52.26", "8.00", grab.AnswerBy, null), row);
    }

    [Fact]
    public async Task Cancelling_before_acceptance_refunds_everything()
    {
        var grab = await _shopper.RequestedGrabAsync(_factory, await GrabbableAsync());

        using var response = await _shopper.PostAsync($"/api/me/grabs/{grab.Id}/cancel", null);

        var cancelled = (await response.Content.ReadFromJsonAsync<GrabView>())!;
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal("52.26", cancelled.RefundedAmount);
        Assert.Equal(52.26m, Assert.Single(_factory.Payments.Refunds).Amount);
        Assert.False((await _shopper.GetFromJsonAsync<FindView>($"/api/finds/{cancelled.FindReference}"))!.GrabOpen);
    }

    [Fact]
    public async Task Cancelling_after_acceptance_keeps_the_deposit()
    {
        var grab = await _shopper.RequestedGrabAsync(_factory, await GrabbableAsync());
        (await _admin.AdminActAsync(grab.Id, "accept")).EnsureSuccessStatusCode();
        Assert.Equal("44.26", (await _shopper.GetFromJsonAsync<GrabView>($"/api/me/grabs/{grab.Id}"))!.RefundIfCancelled);
        await _factory.DispatchEmailsAsync();

        using var response = await _shopper.PostAsync($"/api/me/grabs/{grab.Id}/cancel", null);

        Assert.Equal("44.26", (await response.Content.ReadFromJsonAsync<GrabView>())!.RefundedAmount);
        Assert.Equal(44.26m, Assert.Single(_factory.Payments.Refunds).Amount);
        var email = Assert.Single(await _factory.DispatchEmailsAsync());
        Assert.Contains("You've been refunded $44.26. TuTuMuch kept the $8.00 deposit.", email.Body);
    }

    [Fact]
    public async Task A_bought_grab_cannot_be_cancelled_and_is_received()
    {
        var grab = await _shopper.RequestedGrabAsync(_factory, await GrabbableAsync());
        (await _admin.AdminActAsync(grab.Id, "accept")).EnsureSuccessStatusCode();
        (await _admin.AdminActAsync(grab.Id, "bought")).EnsureSuccessStatusCode();

        using var cancel = await _shopper.PostAsync($"/api/me/grabs/{grab.Id}/cancel", null);
        using var received = await _shopper.PostAsync($"/api/me/grabs/{grab.Id}/received", null);

        Assert.Equal(HttpStatusCode.Conflict, cancel.StatusCode);
        Assert.Equal("GRAB_STATE_CHANGED", (await Problem.ReadAsync(cancel)).Code);
        Assert.Equal("Received", (await received.Content.ReadFromJsonAsync<GrabView>())!.Status);
        Assert.Empty(_factory.Payments.Refunds);
    }

    [Fact]
    public async Task The_job_expires_missed_deadlines_with_full_refunds_and_receives_old_bought_grabs()
    {
        var unanswered = await _shopper.RequestedGrabAsync(_factory, await GrabbableAsync());
        var unbought = await _shopper.RequestedGrabAsync(_factory, await GrabbableAsync());
        var bought = await _shopper.RequestedGrabAsync(_factory, await GrabbableAsync());
        (await _admin.AdminActAsync(unbought.Id, "accept")).EnsureSuccessStatusCode();
        (await _admin.AdminActAsync(bought.Id, "accept")).EnsureSuccessStatusCode();
        (await _admin.AdminActAsync(bought.Id, "bought")).EnsureSuccessStatusCode();
        await _factory.DispatchEmailsAsync();

        _factory.Clock.Advance(TimeSpan.FromHours(48));
        var first = await RunDeadlinesAsync();
        _factory.Clock.Advance(TimeSpan.FromDays(1));
        var second = await RunDeadlinesAsync();
        _factory.Clock.Advance(TimeSpan.FromDays(11));
        var third = await RunDeadlinesAsync();

        Assert.Equal(new GrabDeadlineResult(1, 0), first);
        Assert.Equal(new GrabDeadlineResult(1, 0), second);
        Assert.Equal(new GrabDeadlineResult(0, 1), third);
        Assert.Equal([52.26m, 52.26m], _factory.Payments.Refunds.Select(r => r.Amount).ToArray());
        _factory.InDatabase(db =>
        {
            Assert.Equal(GrabStatus.Expired, db.Grabs.Single(g => g.Id == unanswered.Id).Status);
            Assert.Equal(GrabStatus.Expired, db.Grabs.Single(g => g.Id == unbought.Id).Status);
            Assert.Equal(GrabStatus.Received, db.Grabs.Single(g => g.Id == bought.Id).Status);
            Assert.Equal(2, db.OutboxMessages.Count(m => m.NotificationType == NotificationType.GrabUpdated && m.Status == OutboxMessageStatus.Pending));
        });
    }

    [Fact]
    public async Task Another_members_grab_is_not_found_and_the_find_names_no_shopper()
    {
        var grab = await _shopper.RequestedGrabAsync(_factory, await GrabbableAsync());
        using var other = _factory.CreateVerifiedClient("other_shopper");

        using var read = await other.GetAsync($"/api/me/grabs/{grab.Id}");
        using var cancel = await other.PostAsync($"/api/me/grabs/{grab.Id}/cancel", null);
        var publicFind = await _factory.CreateMemberClient().GetStringAsync($"/api/finds/{grab.FindReference}");

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal("GRAB_NOT_FOUND", (await Problem.ReadAsync(read)).Code);
        Assert.Equal(HttpStatusCode.NotFound, cancel.StatusCode);
        Assert.DoesNotContain("marcus", publicFind, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"grabOpen\":true", publicFind);
    }

    [Fact]
    public async Task Erasing_a_shopper_erases_their_grabs()
    {
        await _shopper.RequestedGrabAsync(_factory, await GrabbableAsync());

        _factory.InDatabase(db =>
        {
            db.Members.Remove(db.Members.Single(m => m.DisplayName == "marcus_12"));
            db.SaveChanges();
        });

        _factory.InDatabase(db => Assert.Empty(db.Grabs));
    }

    public void Dispose()
    {
        _admin.Dispose();
        _shopper.Dispose();
        _provider.Dispose();
        _factory.Dispose();
    }

    private async Task<string> GrabbableAsync(decimal price = 40m) =>
        (await _admin.PostedFindAsync(Finds.Find(_factory, shelfPrice: price, grabFee: 8m))).Reference;

    private async Task<GrabDeadlineResult> RunDeadlinesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new RunGrabDeadlinesCommand());
    }
}
