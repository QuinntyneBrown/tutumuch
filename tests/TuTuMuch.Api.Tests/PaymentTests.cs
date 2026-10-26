// Acceptance Test
// Traces to: L2-027, L2-029, L2-030
// Description: The buyer pays the total they were shown with a payment method token from the
// provider's hosted fields; the provider's signed webhook, never the browser, creates the Paid
// order and sells the listing. A decline lets the buyer retry, a bad signature is 400, repeats of
// a webhook or a payment request change nothing, and a second payment for a sold listing is refunded.

using System.Net;
using System.Net.Http.Json;
using TuTuMuch.Api.Tests.Support;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Api.Tests;

public sealed class PaymentTests : IDisposable
{
    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _buyer;
    private readonly HttpClient _other;
    private readonly HttpClient _provider;

    public PaymentTests()
    {
        _buyer = _factory.CreateMemberClient();
        _other = _factory.CreateMemberClient();
        _provider = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task Paying_sends_only_the_token_and_the_signed_webhook_creates_the_paid_order()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var reference = _factory.SeedActiveListing();

        var (checkout, payment) = await _buyer.CheckoutAndPayAsync(reference);

        Assert.Equal("Pending", payment.Status);
        var charge = Assert.Single(_factory.Payments.Charges.Values);
        Assert.Equal(102.34m, charge.Amount);
        Assert.Equal("tok_visa", charge.PaymentMethodToken);
        var processing = await _buyer.GetFromJsonAsync<CheckoutView>($"/api/checkouts/{checkout.Id}");
        Assert.Equal("Processing", processing!.Status);
        _factory.InDatabase(db => Assert.Empty(db.Orders));

        using var webhook = await _provider.PostWebhookAsync(FakePaymentProvider.Webhook(payment.Id));

        Assert.Equal(HttpStatusCode.OK, webhook.StatusCode);
        var paid = (await _buyer.GetFromJsonAsync<CheckoutView>($"/api/checkouts/{checkout.Id}"))!;
        Assert.Equal("Paid", paid.Status);
        Assert.NotNull(paid.OrderId);
        _factory.InDatabase(db =>
        {
            var order = db.Orders.Single();
            Assert.Equal(OrderStatus.Paid, order.Status);
            Assert.Equal(102.34m, order.Total);
            Assert.Equal("TM-10001", order.Reference);
            Assert.Equal(ListingStatus.Sold, db.Listings.Single().Status);
            var stored = db.Payments.Single();
            Assert.Equal(PaymentStatus.Succeeded, stored.Status);
            Assert.Equal("Visa", stored.CardBrand);
            Assert.Equal("4242", stored.CardLast4);
            Assert.Equal(FakePaymentProvider.ProviderIdOf(stored.Id), stored.ProviderPaymentId);
        });
        var purchase = (await _buyer.GetFromJsonAsync<PurchaseView>($"/api/purchases/{paid.OrderId}"))!;
        Assert.Equal("TM-10001", purchase.Reference);
        Assert.Equal("102.34", purchase.Total);
        Assert.Equal("Visa", purchase.CardBrand);
        Assert.Equal("4242", purchase.CardLast4);
        Assert.Equal("kicks_30001", purchase.SellerDisplayName);
        Assert.Equal("Floral party dress, pink", purchase.Item.Title);
    }

    [Fact]
    public async Task Only_the_buyer_can_read_their_purchase()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        await _other.SignInVerifiedMemberAsync(_factory, "jordan_l", "jordan@example.com");
        var (checkout, payment) = await _buyer.CheckoutAndPayAsync(_factory.SeedActiveListing());
        (await _provider.PostWebhookAsync(FakePaymentProvider.Webhook(payment.Id))).Dispose();
        var orderId = (await _buyer.GetFromJsonAsync<CheckoutView>($"/api/checkouts/{checkout.Id}"))!.OrderId;

        using var response = await _other.GetAsync($"/api/purchases/{orderId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_paid_address_is_offered_at_the_next_checkout()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (_, payment) = await _buyer.CheckoutAndPayAsync(_factory.SeedActiveListing());
        (await _provider.PostWebhookAsync(FakePaymentProvider.Webhook(payment.Id))).Dispose();

        var next = await _buyer.StartedCheckoutAsync(_factory.SeedActiveListing(number: 30002));

        Assert.Equal(new AddressView("Marcus Bell", "214 Kent Ave", "Apt 3R", "Brooklyn", "NY", "11249", "US"), next.SavedAddress);
    }

    [Fact]
    public async Task A_double_click_with_the_same_key_charges_once()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var checkout = await _buyer.StartedCheckoutAsync(_factory.SeedActiveListing());
        (await _buyer.SetAddressAsync(checkout.Id, Checkouts.Brooklyn())).Dispose();
        var key = Guid.NewGuid();

        using var first = await _buyer.PayAsync(checkout.Id, key, "102.34");
        using var second = await _buyer.PayAsync(checkout.Id, key, "102.34");

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);
        var a = (await first.Content.ReadFromJsonAsync<PaymentView>())!;
        var b = (await second.Content.ReadFromJsonAsync<PaymentView>())!;
        Assert.Equal(a.Id, b.Id);
        Assert.Single(_factory.Payments.Charges);
        _factory.InDatabase(db => Assert.Single(db.Payments));
    }

    [Fact]
    public async Task Payment_needs_a_uuid_key_an_address_and_the_shown_total()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var checkout = await _buyer.StartedCheckoutAsync(_factory.SeedActiveListing());

        using var noKey = await _buyer.PostAsJsonAsync($"/api/checkouts/{checkout.Id}/payments", new { paymentMethodToken = "tok_visa", expectedTotal = 94m });
        using var noAddress = await _buyer.PayAsync(checkout.Id, Guid.NewGuid(), "94.00");
        (await _buyer.SetAddressAsync(checkout.Id, Checkouts.Brooklyn())).Dispose();
        using var changed = await _buyer.PayAsync(checkout.Id, Guid.NewGuid(), "102.33");

        Assert.Equal(HttpStatusCode.BadRequest, noKey.StatusCode);
        Assert.Equal("IDEMPOTENCY_KEY_REQUIRED", (await Problem.ReadAsync(noKey)).Code);
        Assert.Equal(HttpStatusCode.Conflict, noAddress.StatusCode);
        Assert.Equal("ADDRESS_REQUIRED", (await Problem.ReadAsync(noAddress)).Code);
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        Assert.Equal("TOTAL_CHANGED", (await Problem.ReadAsync(changed)).Code);
        Assert.Empty(_factory.Payments.Requests);
    }

    [Fact]
    public async Task A_declined_card_shows_the_reason_and_the_buyer_can_retry()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (checkout, payment) = await _buyer.CheckoutAndPayAsync(_factory.SeedActiveListing());

        using var webhook = await _provider.PostWebhookAsync(FakePaymentProvider.Webhook(payment.Id, "Insufficient funds."));

        Assert.Equal(HttpStatusCode.OK, webhook.StatusCode);
        var declined = (await _buyer.GetFromJsonAsync<CheckoutView>($"/api/checkouts/{checkout.Id}"))!;
        Assert.Equal("Open", declined.Status);
        Assert.Equal("Insufficient funds.", declined.LastDeclineReason);
        _factory.InDatabase(db =>
        {
            Assert.Empty(db.Orders);
            Assert.Equal(ListingStatus.Reserved, db.Listings.Single().Status);
        });
        using var retry = await _buyer.PayAsync(checkout.Id, Guid.NewGuid(), "102.34", "tok_mastercard");
        Assert.Equal(HttpStatusCode.Accepted, retry.StatusCode);
    }

    [Fact]
    public async Task A_webhook_with_an_invalid_signature_is_400_and_changes_nothing()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (checkout, payment) = await _buyer.CheckoutAndPayAsync(_factory.SeedActiveListing());

        using var response = await _provider.PostWebhookAsync(FakePaymentProvider.Webhook(payment.Id), signature: "forged");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("WEBHOOK_SIGNATURE_INVALID", (await Problem.ReadAsync(response)).Code);
        var still = (await _buyer.GetFromJsonAsync<CheckoutView>($"/api/checkouts/{checkout.Id}"))!;
        Assert.Equal("Processing", still.Status);
        _factory.InDatabase(db =>
        {
            Assert.Empty(db.Orders);
            Assert.Empty(db.PaymentWebhookEvents);
        });
    }

    [Fact]
    public async Task The_same_webhook_delivered_twice_changes_nothing_the_second_time()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (_, payment) = await _buyer.CheckoutAndPayAsync(_factory.SeedActiveListing());
        var body = FakePaymentProvider.Webhook(payment.Id, eventId: "evt_once");

        using var first = await _provider.PostWebhookAsync(body);
        using var second = await _provider.PostWebhookAsync(body);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        _factory.InDatabase(db =>
        {
            Assert.Single(db.Orders);
            Assert.Single(db.PaymentWebhookEvents);
            Assert.Empty(db.Refunds);
        });
    }

    [Fact]
    public async Task A_second_payment_for_a_sold_listing_is_refunded_and_makes_no_order()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        await _other.SignInVerifiedMemberAsync(_factory, "jordan_l", "jordan@example.com");
        var reference = _factory.SeedActiveListing();
        var (_, late) = await _buyer.CheckoutAndPayAsync(reference);
        _factory.Clock.Advance(TimeSpan.FromMinutes(10));
        var (_, winner) = await _other.CheckoutAndPayAsync(reference);
        (await _provider.PostWebhookAsync(FakePaymentProvider.Webhook(winner.Id))).Dispose();

        using var response = await _provider.PostWebhookAsync(FakePaymentProvider.Webhook(late.Id));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _factory.InDatabase(db =>
        {
            var order = db.Orders.Single();
            Assert.Equal(winner.Id, order.PaymentId);
            var refund = db.Refunds.Single();
            Assert.Equal(late.Id, refund.PaymentId);
            Assert.Equal(RefundReason.DuplicatePayment, refund.Reason);
            Assert.Equal(RefundStatus.Issued, refund.Status);
            Assert.Equal(102.34m, refund.Amount);
        });
        var issued = Assert.Single(_factory.Payments.Refunds);
        Assert.Equal(FakePaymentProvider.ProviderIdOf(late.Id), issued.ProviderPaymentId);
    }

    public void Dispose()
    {
        _buyer.Dispose();
        _other.Dispose();
        _provider.Dispose();
        _factory.Dispose();
    }
}
