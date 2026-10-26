// Acceptance Test
// Traces to: L2-035, L2-042
// Description: The seller cancels a Paid order with a reason and an optional note; the order
// becomes Cancelled, the buyer is refunded the full total through the payment provider and emailed
// the reason, and the listing is removed from the catalog. The buyer asks the seller to cancel; the
// seller is emailed and accepts (Cancelled and refunded) or declines (stays Paid). A Shipped order
// cannot be cancelled or asked about (409). The seller relists the listing of a cancelled order,
// which returns to the catalog with its original details and can be bought again.

using System.Net;
using System.Net.Http.Json;
using TuTuMuch.Api.Tests.Support;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Notifications;
using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Api.Tests;

public sealed class CancelOrderTests : IDisposable
{
    private const string BuyerEmail = "marcus.bell@example.com";
    private const string SellerEmail = "kicks30001@example.com";

    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _buyer;
    private readonly HttpClient _anonymous;

    public CancelOrderTests()
    {
        _buyer = _factory.CreateMemberClient();
        _anonymous = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task The_seller_cancels_a_paid_order_and_the_buyer_is_refunded_in_full_and_emailed_the_reason()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        await _factory.DispatchEmailsAsync();

        using var response = await seller.CancelOrderAsync(orderId, "ItemDamaged", "  Found a crack in the heel counter.  ");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sale = (await response.Content.ReadFromJsonAsync<CancelledSaleView>())!;
        Assert.Equal("Cancelled", sale.Status);
        Assert.Equal(new CancellationView("ItemDamaged", "Item damaged", "Found a crack in the heel counter."), sale.Cancellation);
        Assert.True(sale.CanRelist);
        var refund = Assert.Single(_factory.Payments.Refunds);
        Assert.Equal(102.34m, refund.Amount);
        _factory.InDatabase(db =>
        {
            var order = db.Orders.Single();
            Assert.Equal(OrderStatus.Cancelled, order.Status);
            Assert.Equal(OrderActor.Seller, db.Set<OrderStatusChange>().Single(c => c.To == OrderStatus.Cancelled).Actor);
            var stored = db.Refunds.Single();
            Assert.Equal(RefundReason.OrderCancelled, stored.Reason);
            Assert.Equal(RefundStatus.Issued, stored.Status);
            Assert.Equal(stored.Id, refund.Key);
            var listing = db.Listings.Single();
            Assert.Equal(ListingStatus.Removed, listing.Status);
            Assert.Equal(ListingRemovalReason.OrderCancelled, listing.RemovalReason);
        });

        var sent = await _factory.DispatchEmailsAsync();
        var cancelled = Assert.Single(sent, e => e.Subject.Contains("cancelled", StringComparison.Ordinal));
        Assert.Equal(BuyerEmail, cancelled.To);
        Assert.Contains("Item damaged", cancelled.Body);
        Assert.Contains("Found a crack in the heel counter.", cancelled.Body);
        Assert.Contains($"https://tutumuch.test/orders/{orderId}", cancelled.Body);
        var refunded = Assert.Single(sent, e => e.Subject.Contains("refund", StringComparison.Ordinal));
        Assert.Equal(BuyerEmail, refunded.To);
        Assert.Contains("$102.34", refunded.Body);
        Assert.DoesNotContain(sent, e => e.To == SellerEmail);

        var purchase = (await _buyer.GetFromJsonAsync<CancelledPurchaseView>($"/api/purchases/{orderId}"))!;
        Assert.Equal("Cancelled", purchase.Status);
        Assert.Equal("Found a crack in the heel counter.", purchase.Cancellation!.Note);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ShipDeadlinePassed")]
    [InlineData("Bored")]
    public async Task A_seller_cancellation_needs_one_of_the_four_reasons(string? reason)
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);

        using var response = await seller.CancelOrderAsync(orderId, reason);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("CANCELLATION_INVALID", problem.Code);
        Assert.Equal("REASON_REQUIRED", problem.Errors!["reason"].Code);
        Assert.Equal("Choose a reason", problem.Errors!["reason"].Message);
        _factory.InDatabase(db => Assert.Equal(OrderStatus.Paid, db.Orders.Single().Status));
        Assert.Empty(_factory.Payments.Refunds);
    }

    [Fact]
    public async Task A_note_over_500_characters_is_refused()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);

        using var response = await seller.CancelOrderAsync(orderId, "NoLongerHave", new string('x', 501));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("NOTE_LENGTH", (await Problem.ReadAsync(response)).Errors!["note"].Code);
    }

    [Fact]
    public async Task Only_the_seller_can_cancel_and_only_the_buyer_can_ask()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);

        using var buyerCancels = await _buyer.CancelOrderAsync(orderId, "ItemDamaged");
        using var sellerAsks = await seller.RequestCancellationAsync(orderId, "please");
        using var buyerResponds = await _buyer.RespondToCancellationAsync(orderId, "Accept");
        using var visitorCancels = await _anonymous.CancelOrderAsync(orderId, "ItemDamaged");

        Assert.Equal(HttpStatusCode.NotFound, buyerCancels.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, sellerAsks.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, buyerResponds.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, visitorCancels.StatusCode);
        _factory.InDatabase(db => Assert.Equal(OrderStatus.Paid, db.Orders.Single().Status));
    }

    [Fact]
    public async Task A_shipped_order_cannot_be_cancelled_or_asked_about()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        (await seller.AddTrackingAsync(orderId)).EnsureSuccessStatusCode();

        using var cancel = await seller.CancelOrderAsync(orderId, "ItemDamaged");
        using var ask = await _buyer.RequestCancellationAsync(orderId, "please");

        Assert.Equal(HttpStatusCode.Conflict, cancel.StatusCode);
        Assert.Equal("ORDER_INVALID_TRANSITION", (await Problem.ReadAsync(cancel)).Code);
        Assert.Equal(HttpStatusCode.Conflict, ask.StatusCode);
        Assert.Equal("ORDER_INVALID_TRANSITION", (await Problem.ReadAsync(ask)).Code);
        Assert.Empty(_factory.Payments.Refunds);
    }

    [Fact]
    public async Task A_cancelled_order_cannot_be_cancelled_again()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        (await seller.CancelOrderAsync(orderId, "ListedByMistake")).EnsureSuccessStatusCode();

        using var again = await seller.CancelOrderAsync(orderId, "ListedByMistake");

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Single(_factory.Payments.Refunds);
    }

    [Fact]
    public async Task The_buyer_asks_to_cancel_and_the_seller_is_emailed_and_sees_the_request()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        await _factory.DispatchEmailsAsync();

        using var response = await _buyer.RequestCancellationAsync(orderId, " Ordered the wrong colour, sorry. ");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var purchase = (await response.Content.ReadFromJsonAsync<CancelledPurchaseView>())!;
        Assert.Equal("Paid", purchase.Status);
        Assert.Equal("Pending", purchase.CancellationRequest!.Status);
        Assert.Equal("Ordered the wrong colour, sorry.", purchase.CancellationRequest.Message);

        var sale = (await seller.GetFromJsonAsync<CancelledSaleView>($"/api/sales/{orderId}"))!;
        Assert.Equal("Paid", sale.Status);
        Assert.Equal("Pending", sale.CancellationRequest!.Status);
        Assert.Equal("Ordered the wrong colour, sorry.", sale.CancellationRequest.Message);

        var email = Assert.Single(await _factory.DispatchEmailsAsync());
        Assert.Equal(SellerEmail, email.To);
        Assert.Equal("The buyer asked to cancel order TM-10001", email.Subject);
        Assert.Contains("Ordered the wrong colour, sorry.", email.Body);
        Assert.Contains($"https://tutumuch.test/sales/{orderId}", email.Body);
        _factory.InDatabase(db => Assert.Contains(db.OutboxMessages, m => m.NotificationType == NotificationType.CancellationRequested));
    }

    [Fact]
    public async Task The_buyer_can_ask_only_once()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        (await _buyer.RequestCancellationAsync(orderId, null)).EnsureSuccessStatusCode();
        (await seller.RespondToCancellationAsync(orderId, "Decline")).EnsureSuccessStatusCode();

        using var again = await _buyer.RequestCancellationAsync(orderId, "please?");

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("CANCELLATION_ALREADY_REQUESTED", (await Problem.ReadAsync(again)).Code);
    }

    [Fact]
    public async Task Accepting_the_request_cancels_and_refunds_the_order()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        (await _buyer.RequestCancellationAsync(orderId, "Wrong colour")).EnsureSuccessStatusCode();

        using var response = await seller.RespondToCancellationAsync(orderId, "Accept");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sale = (await response.Content.ReadFromJsonAsync<CancelledSaleView>())!;
        Assert.Equal("Cancelled", sale.Status);
        Assert.Equal("Accepted", sale.CancellationRequest!.Status);
        Assert.Equal("BuyerAskedToCancel", sale.Cancellation!.Reason);
        Assert.Equal(102.34m, Assert.Single(_factory.Payments.Refunds).Amount);
        _factory.InDatabase(db => Assert.Equal(ListingStatus.Removed, db.Listings.Single().Status));
    }

    [Fact]
    public async Task Declining_the_request_keeps_the_order_paid()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        (await _buyer.RequestCancellationAsync(orderId, "Wrong colour")).EnsureSuccessStatusCode();

        using var response = await seller.RespondToCancellationAsync(orderId, "Decline");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sale = (await response.Content.ReadFromJsonAsync<CancelledSaleView>())!;
        Assert.Equal("Paid", sale.Status);
        Assert.Equal("Declined", sale.CancellationRequest!.Status);
        Assert.Empty(_factory.Payments.Refunds);
        var purchase = (await _buyer.GetFromJsonAsync<CancelledPurchaseView>($"/api/purchases/{orderId}"))!;
        Assert.Equal("Declined", purchase.CancellationRequest!.Status);
        (await seller.AddTrackingAsync(orderId)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task A_response_needs_a_pending_request_and_a_known_decision()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);

        using var none = await seller.RespondToCancellationAsync(orderId, "Accept");
        (await _buyer.RequestCancellationAsync(orderId, null)).EnsureSuccessStatusCode();
        using var unknown = await seller.RespondToCancellationAsync(orderId, "Maybe");

        Assert.Equal(HttpStatusCode.Conflict, none.StatusCode);
        Assert.Equal("CANCELLATION_NO_PENDING_REQUEST", (await Problem.ReadAsync(none)).Code);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknown.StatusCode);
    }

    [Fact]
    public async Task The_seller_relists_a_cancelled_orders_listing_and_it_can_be_bought_again()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        (await seller.CancelOrderAsync(orderId, "ListedByMistake")).EnsureSuccessStatusCode();
        string reference = null!;
        decimal? price = null;
        _factory.InDatabase(db => (reference, price) = (db.Listings.Single().Reference, db.Listings.Single().Price));

        using var other = await _buyer.RelistAsync(reference);
        using var relist = await seller.RelistAsync(reference);
        using var again = await seller.RelistAsync(reference);

        Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, relist.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("LISTING_NOT_RELISTABLE", (await Problem.ReadAsync(again)).Code);
        _factory.InDatabase(db =>
        {
            var listing = db.Listings.Single();
            Assert.Equal(ListingStatus.Active, listing.Status);
            Assert.Null(listing.RemovalReason);
            Assert.Equal(price, listing.Price);
        });
        var sale = (await seller.GetFromJsonAsync<CancelledSaleView>($"/api/sales/{orderId}"))!;
        Assert.False(sale.CanRelist);

        await _anonymous.SignInVerifiedMemberAsync(_factory, "tall_tom", "tom@example.com");
        var (_, payment) = await _anonymous.CheckoutAndPayAsync(reference);
        (await _anonymous.PostWebhookAsync(FakePaymentProvider.Webhook(payment.Id))).EnsureSuccessStatusCode();
        _factory.InDatabase(db =>
        {
            Assert.Equal(2, db.Orders.Count());
            Assert.Equal(ListingStatus.Sold, db.Listings.Single().Status);
        });
    }

    [Fact]
    public async Task An_active_listing_cannot_be_relisted()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        string reference = null!;
        _factory.InDatabase(db => reference = db.Listings.Single().Reference);

        using var response = await seller.RelistAsync(reference);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var sale = (await seller.GetFromJsonAsync<CancelledSaleView>($"/api/sales/{orderId}"))!;
        Assert.False(sale.CanRelist);
    }

    public void Dispose()
    {
        _buyer.Dispose();
        _anonymous.Dispose();
        _factory.Dispose();
    }
}
