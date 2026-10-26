// Acceptance Test
// Traces to: L2-032, L2-033
// Description: The seller of a Paid order sees the buyer's address and ship-by date and adds a
// carrier and tracking number; the order becomes Shipped with the history recorded and the buyer
// sees the tracking link. A malformed number is refused, only the seller may ship, and a Shipped
// order cannot be shipped again. A signed carrier delivery event marks the order Delivered, and
// the ship deadline job cancels and fully refunds an order with no tracking after 5 business days.

using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TutuMuch.Api.Tests.Support;
using TutuMuch.Application.Orders;
using TutuMuch.Domain.Listings;
using TutuMuch.Domain.Orders;

namespace TutuMuch.Api.Tests;

public sealed class ShipOrderTests : IDisposable
{
    private readonly TutuMuchApiFactory _factory = new();
    private readonly HttpClient _buyer;
    private readonly HttpClient _anonymous;

    public ShipOrderTests()
    {
        _buyer = _factory.CreateMemberClient();
        _anonymous = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task The_seller_sees_the_paid_sale_with_the_address_and_ship_by_date()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);

        var sale = (await seller.GetFromJsonAsync<SaleView>($"/api/sales/{orderId}"))!;

        Assert.Equal("Paid", sale.Status);
        Assert.Equal("TM-10001", sale.Reference);
        Assert.Equal(BusinessDays.Add(sale.PaidAt, 5), sale.ShipBy);
        Assert.Equal("marcus_12", sale.BuyerDisplayName);
        Assert.Equal(new AddressView("Marcus Bell", "214 Kent Ave", "Apt 3R", "Brooklyn", "NY", "11249", "US"), sale.ShipTo);
        Assert.Equal("85.00", sale.ItemPrice);
        Assert.Equal("102.34", sale.Total);
        Assert.Null(sale.Tracking);
        Assert.Equal(["Paid", "Shipped", "Delivered", "Completed"], sale.Timeline.Select(s => s.Status));
        Assert.Equal(sale.PaidAt, sale.Timeline[0].At);
        Assert.Null(sale.Timeline[1].At);
    }

    [Fact]
    public async Task Adding_tracking_ships_the_order_records_the_change_and_shows_the_buyer_the_link()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        _factory.Clock.Advance(TimeSpan.FromMinutes(5));

        using var response = await seller.AddTrackingAsync(orderId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sale = (await response.Content.ReadFromJsonAsync<SaleView>())!;
        Assert.Equal("Shipped", sale.Status);
        Assert.Equal(new TrackingView("USPS", "9400111899223456120477", "https://tools.usps.com/go/TrackConfirmAction?tLabels=9400111899223456120477"), sale.Tracking);
        Assert.Equal(_factory.Clock.GetUtcNow().UtcDateTime, sale.Timeline[1].At);
        _factory.InDatabase(db =>
        {
            var changes = db.Set<OrderStatusChange>().Where(c => c.OrderId == orderId).OrderBy(c => c.OccurredAt).ToList();
            Assert.Equal(2, changes.Count);
            Assert.Equal((OrderStatus?)OrderStatus.Paid, changes[1].From);
            Assert.Equal(OrderStatus.Shipped, changes[1].To);
            Assert.Equal(OrderActor.Seller, changes[1].Actor);
            Assert.Equal(_factory.Clock.GetUtcNow().UtcDateTime, changes[1].OccurredAt);
        });

        var order = (await _buyer.GetFromJsonAsync<OrderView>($"/api/purchases/{orderId}"))!;
        Assert.Equal("Shipped", order.Status);
        Assert.Equal(sale.Tracking, order.Tracking);
    }

    [Theory]
    [InlineData("USPS", "")]
    [InlineData("USPS", "9400 1118")]
    [InlineData("UPS", "9400 1118 9922 3456 1204 77")]
    public async Task A_missing_or_malformed_tracking_number_is_refused(string carrier, string number)
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);

        using var response = await seller.AddTrackingAsync(orderId, carrier, number);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await Problem.ReadAsync(response);
        Assert.Equal("TRACKING_INVALID", problem.Code);
        Assert.True(problem.Errors!.ContainsKey("trackingNumber"));
        _factory.InDatabase(db => Assert.Equal(OrderStatus.Paid, db.Orders.Single().Status));
    }

    [Fact]
    public async Task The_usps_message_says_the_number_is_usually_22_digits()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);

        using var response = await seller.AddTrackingAsync(orderId, "USPS", "9400 1118");

        var problem = await Problem.ReadAsync(response);
        Assert.Equal("That doesn’t look like a USPS tracking number. It’s usually 22 digits.", problem.Errors!["trackingNumber"].Message);
    }

    [Fact]
    public async Task An_unknown_carrier_is_refused()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);

        using var response = await seller.AddTrackingAsync(orderId, "Pony Express");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.True((await Problem.ReadAsync(response)).Errors!.ContainsKey("carrier"));
    }

    [Fact]
    public async Task Only_the_seller_can_see_or_ship_the_sale()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, _) = await _factory.SeedPaidOrderAsync(_buyer);

        using var buyerView = await _buyer.GetAsync($"/api/sales/{orderId}");
        using var buyerShip = await _buyer.AddTrackingAsync(orderId);
        using var visitorShip = await _anonymous.AddTrackingAsync(orderId);

        Assert.Equal(HttpStatusCode.NotFound, buyerView.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, buyerShip.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, visitorShip.StatusCode);
    }

    [Fact]
    public async Task A_shipped_order_cannot_be_shipped_again()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        (await seller.AddTrackingAsync(orderId)).EnsureSuccessStatusCode();

        using var again = await seller.AddTrackingAsync(orderId, "UPS", "1Z999AA10123456784");

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("ORDER_INVALID_TRANSITION", (await Problem.ReadAsync(again)).Code);
    }

    [Fact]
    public async Task A_signed_carrier_delivery_event_marks_the_order_delivered_once()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        (await seller.AddTrackingAsync(orderId)).EnsureSuccessStatusCode();
        var deliveredAt = _factory.Clock.GetUtcNow().UtcDateTime.AddDays(3);
        var body = FakeCarrierTracking.Delivered("9400111899223456120477", deliveredAt);

        using var first = await _anonymous.PostCarrierWebhookAsync(body);
        using var repeat = await _anonymous.PostCarrierWebhookAsync(body);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, repeat.StatusCode);
        var order = (await _buyer.GetFromJsonAsync<OrderView>($"/api/purchases/{orderId}"))!;
        Assert.Equal("Delivered", order.Status);
        Assert.Equal(deliveredAt, order.Timeline[2].At);
        _factory.InDatabase(db =>
        {
            Assert.Equal(3, db.Set<OrderStatusChange>().Count(c => c.OrderId == orderId));
            Assert.Equal(OrderActor.Carrier, db.Set<OrderStatusChange>().Single(c => c.To == OrderStatus.Delivered).Actor);
        });
    }

    [Fact]
    public async Task A_carrier_event_with_a_bad_signature_is_refused_and_changes_nothing()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        (await seller.AddTrackingAsync(orderId)).EnsureSuccessStatusCode();

        using var response = await _anonymous.PostCarrierWebhookAsync(FakeCarrierTracking.Delivered("9400111899223456120477", DateTime.UtcNow), "forged");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _factory.InDatabase(db => Assert.Equal(OrderStatus.Shipped, db.Orders.Single().Status));
    }

    [Fact]
    public async Task The_deadline_job_cancels_and_fully_refunds_an_order_with_no_tracking_after_5_business_days()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, _) = await _factory.SeedPaidOrderAsync(_buyer);

        _factory.Clock.Advance(TimeSpan.FromDays(2));
        Assert.Equal(0, await RunDeadlineJobAsync());
        _factory.Clock.Advance(TimeSpan.FromDays(7));
        Assert.Equal(1, await RunDeadlineJobAsync());
        Assert.Equal(0, await RunDeadlineJobAsync());

        var refund = Assert.Single(_factory.Payments.Refunds);
        Assert.Equal(102.34m, refund.Amount);
        _factory.InDatabase(db =>
        {
            Assert.Equal(OrderStatus.Cancelled, db.Orders.Single(o => o.Id == orderId).Status);
            var stored = db.Refunds.Single();
            Assert.Equal(RefundReason.ShipDeadlinePassed, stored.Reason);
            Assert.Equal(RefundStatus.Issued, stored.Status);
            Assert.Equal(OrderActor.System, db.Set<OrderStatusChange>().Single(c => c.To == OrderStatus.Cancelled).Actor);
            Assert.Equal(ListingStatus.Removed, db.Listings.Single().Status);
            Assert.Equal(CancellationReason.ShipDeadlinePassed, db.Orders.Single().CancellationReason);
        });
    }

    [Fact]
    public async Task The_buyer_sees_a_cancelled_order()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, _) = await _factory.SeedPaidOrderAsync(_buyer);
        _factory.InDatabase(db =>
        {
            var order = db.Orders.Single();
            order.Cancel(CancellationReason.ShipDeadlinePassed, null, OrderActor.System, order.ShipBy);
            db.SaveChanges();
        });

        var view = (await _buyer.GetFromJsonAsync<OrderView>($"/api/purchases/{orderId}"))!;

        Assert.Equal("Cancelled", view.Status);
    }

    [Fact]
    public async Task The_deadline_job_leaves_a_shipped_order_alone()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        (await seller.AddTrackingAsync(orderId)).EnsureSuccessStatusCode();
        _factory.Clock.Advance(TimeSpan.FromDays(9));

        Assert.Equal(0, await RunDeadlineJobAsync());

        _factory.InDatabase(db => Assert.Equal(OrderStatus.Shipped, db.Orders.Single().Status));
        Assert.Empty(_factory.Payments.Refunds);
    }

    public void Dispose()
    {
        _buyer.Dispose();
        _anonymous.Dispose();
        _factory.Dispose();
    }

    private async Task<int> RunDeadlineJobAsync()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CancelUnshippedOrdersCommand());
    }
}
