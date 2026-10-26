// Acceptance Test
// Traces to: L2-037
// Description: A member's "Purchases" and "Sales" list their orders newest first with the item,
// the other party, the total, the status and the date, plus the counts the page subtitles read.
// The seller sees the buyer's shipping address on the sale until 30 days after the order ends.

using System.Net;
using System.Net.Http.Json;
using TutuMuch.Api.Tests.Support;

namespace TutuMuch.Api.Tests;

public sealed class OrderHistoryTests : IDisposable
{
    private readonly TutuMuchApiFactory _factory = new();
    private readonly HttpClient _buyer;

    public OrderHistoryTests()
    {
        _buyer = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task Purchases_are_listed_newest_first_with_item_seller_total_status_and_date()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (first, _) = await _factory.SeedPaidOrderAsync(_buyer, 30001);
        _factory.Clock.Advance(TimeSpan.FromMinutes(1));
        var (second, seller2) = await _factory.SeedPaidOrderAsync(_buyer, 30002);
        _factory.Clock.Advance(TimeSpan.FromMinutes(1));
        var (third, _) = await _factory.SeedPaidOrderAsync(_buyer, 30003);
        (await seller2.AddTrackingAsync(second)).EnsureSuccessStatusCode();

        var list = await _buyer.GetFromJsonAsync<OrderListView>("/api/purchases");

        Assert.Equal([third, second, first], list!.Items.Select(i => i.Id));
        var shipped = list.Items[1];
        Assert.Equal("Shipped", shipped.Status);
        Assert.Equal("kicks_30002", shipped.OtherParty);
        Assert.Equal("Floral party dress, pink", shipped.Item.Title);
        Assert.Equal("102.34", shipped.Total);
        Assert.StartsWith("TM-", shipped.Reference);
        Assert.Equal(new OrderCountsView(1, 0, 2), list.Counts);
    }

    [Fact]
    public async Task Sales_list_only_the_sellers_orders_with_the_buyer()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);

        var sales = await seller.GetFromJsonAsync<OrderListView>("/api/sales");
        var buyerSales = await _buyer.GetFromJsonAsync<OrderListView>("/api/sales");
        var sellerPurchases = await seller.GetFromJsonAsync<OrderListView>("/api/purchases");

        var row = Assert.Single(sales!.Items);
        Assert.Equal(orderId, row.Id);
        Assert.Equal("Paid", row.Status);
        Assert.Equal(await BuyerNameAsync(), row.OtherParty);
        Assert.NotNull(row.ShipBy);
        Assert.Equal(1, sales.Counts.ToShip);
        Assert.Empty(buyerSales!.Items);
        Assert.Empty(sellerPurchases!.Items);
    }

    [Fact]
    public async Task Visitors_cannot_read_order_history()
    {
        using var visitor = _factory.CreateMemberClient();

        using var purchases = await visitor.GetAsync("/api/purchases");
        using var sales = await visitor.GetAsync("/api/sales");

        Assert.Equal(HttpStatusCode.Unauthorized, purchases.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, sales.StatusCode);
    }

    [Fact]
    public async Task The_seller_sees_the_address_until_30_days_after_the_order_ends()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);

        var paid = await seller.GetFromJsonAsync<AddressWindowView>($"/api/sales/{orderId}");
        Assert.NotNull(paid!.ShipTo);

        await _factory.DeliverAsync(orderId, seller, _factory.Clock.GetUtcNow().UtcDateTime);
        (await _buyer.ConfirmReceiptAsync(orderId)).EnsureSuccessStatusCode();
        EndedDaysAgo(orderId, 30);
        var day30 = await seller.GetFromJsonAsync<AddressWindowView>($"/api/sales/{orderId}");
        EndedDaysAgo(orderId, 31);
        var day31 = await seller.GetFromJsonAsync<AddressWindowView>($"/api/sales/{orderId}");

        Assert.NotNull(day30!.ShipTo);
        Assert.Equal("Completed", day31!.Status);
        Assert.Null(day31.ShipTo);
    }

    /// <summary>Backdates the order's end, because a 31-day clock jump would also expire the seller's session.</summary>
    private void EndedDaysAgo(Guid orderId, int days) =>
        _factory.InDatabase(db =>
        {
            var order = db.Orders.Single(o => o.Id == orderId);
            db.Entry(order).Property(o => o.EndedAt).CurrentValue = _factory.Clock.GetUtcNow().UtcDateTime.AddDays(-days);
            db.SaveChanges();
        });

    private async Task<string> BuyerNameAsync()
    {
        var name = string.Empty;
        var list = await _buyer.GetFromJsonAsync<OrderListView>("/api/purchases");
        _factory.InDatabase(db => name = db.Members.Single(m => m.Id == db.Orders.Single(o => o.Id == list!.Items[0].Id).BuyerId).DisplayName);
        return name;
    }

    public void Dispose()
    {
        _buyer.Dispose();
        _factory.Dispose();
    }
}

public sealed record OrderCountsView(int OnTheWay, int ToConfirm, int ToShip);

public sealed record OrderRowView(Guid Id, string Reference, string Status, CheckoutItemView Item, string Total, string OtherParty, DateTime PaidAt, DateTime? ShipBy);

public sealed record OrderListView(IReadOnlyList<OrderRowView> Items, OrderCountsView Counts);

public sealed record AddressWindowView(Guid Id, string Status, AddressView? ShipTo);
