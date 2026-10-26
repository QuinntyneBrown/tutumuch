// Acceptance Test
// Traces to: L2-034
// Description: The buyer confirms receipt of a Shipped or Delivered order and it becomes Completed
// with the change recorded against the buyer; any other status answers 409 and only the buyer may
// confirm. A Delivered order completes automatically 3 days after delivery, recorded against the
// system. Every completion publishes OrderCompleted once, which makes the payout releasable.

using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TuTuMuch.Api.Tests.Support;
using TuTuMuch.Application.Orders;
using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Api.Tests;

public sealed class CompleteOrderTests : IDisposable
{
    private readonly TuTuMuchApiFactory _factory = new();
    private readonly HttpClient _buyer;
    private readonly HttpClient _anonymous;

    public CompleteOrderTests()
    {
        _buyer = _factory.CreateMemberClient();
        _anonymous = _factory.CreateMemberClient();
    }

    [Fact]
    public async Task The_buyer_confirms_receipt_of_a_delivered_order_and_it_completes()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        await _factory.DeliverAsync(orderId, seller, Now().AddHours(-1));

        using var response = await _buyer.ConfirmReceiptAsync(orderId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = (await response.Content.ReadFromJsonAsync<OrderView>())!;
        Assert.Equal("Completed", order.Status);
        Assert.Equal(Now(), order.Timeline[3].At);
        Assert.Null(order.AutoCompletesAt);
        _factory.InDatabase(db =>
        {
            var stored = db.Orders.Single();
            Assert.Equal(Now(), stored.CompletedAt);
            Assert.Equal(OrderActor.Buyer, db.Set<OrderStatusChange>().Single(c => c.To == OrderStatus.Completed).Actor);
        });
        var completed = Assert.Single(_factory.Completed.Events);
        Assert.Equal(orderId, completed.OrderId);
        Assert.Equal(Now(), completed.CompletedAt);
    }

    [Fact]
    public async Task Confirming_a_shipped_order_records_delivered_and_completed_against_the_buyer()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        (await seller.AddTrackingAsync(orderId)).EnsureSuccessStatusCode();

        using var response = await _buyer.ConfirmReceiptAsync(orderId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = (await response.Content.ReadFromJsonAsync<OrderView>())!;
        Assert.Equal("Completed", order.Status);
        Assert.Equal(Now(), order.Timeline[2].At);
        _factory.InDatabase(db =>
        {
            var changes = db.Set<OrderStatusChange>().Where(c => c.OrderId == orderId).ToList();
            Assert.Equal(4, changes.Count);
            Assert.Equal(OrderActor.Buyer, changes.Single(c => c.To == OrderStatus.Delivered).Actor);
            Assert.Equal(OrderActor.Buyer, changes.Single(c => c.To == OrderStatus.Completed).Actor);
        });
        Assert.Single(_factory.Completed.Events);
    }

    [Fact]
    public async Task A_paid_or_completed_order_cannot_be_confirmed()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);

        using var paid = await _buyer.ConfirmReceiptAsync(orderId);
        Assert.Equal(HttpStatusCode.Conflict, paid.StatusCode);
        Assert.Equal("ORDER_INVALID_TRANSITION", (await Problem.ReadAsync(paid)).Code);

        await _factory.DeliverAsync(orderId, seller, Now());
        (await _buyer.ConfirmReceiptAsync(orderId)).EnsureSuccessStatusCode();
        using var again = await _buyer.ConfirmReceiptAsync(orderId);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("ORDER_INVALID_TRANSITION", (await Problem.ReadAsync(again)).Code);
        Assert.Single(_factory.Completed.Events);
    }

    [Fact]
    public async Task Only_the_buyer_can_confirm_receipt()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        await _factory.DeliverAsync(orderId, seller, Now());

        using var bySeller = await seller.ConfirmReceiptAsync(orderId);
        using var byVisitor = await _anonymous.ConfirmReceiptAsync(orderId);

        Assert.Equal(HttpStatusCode.NotFound, bySeller.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, byVisitor.StatusCode);
        _factory.InDatabase(db => Assert.Equal(OrderStatus.Delivered, db.Orders.Single().Status));
    }

    [Fact]
    public async Task The_buyer_sees_when_a_delivered_order_completes_automatically()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        var deliveredAt = Now();
        await _factory.DeliverAsync(orderId, seller, deliveredAt);

        var order = (await _buyer.GetFromJsonAsync<OrderView>($"/api/purchases/{orderId}"))!;

        Assert.Equal("Delivered", order.Status);
        Assert.Equal(deliveredAt.AddDays(3), order.AutoCompletesAt);
    }

    [Fact]
    public async Task The_completion_job_completes_a_delivered_order_3_days_after_delivery()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (orderId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        await _factory.DeliverAsync(orderId, seller, Now());

        _factory.Clock.Advance(TimeSpan.FromDays(3) - TimeSpan.FromMinutes(1));
        Assert.Equal(0, await RunCompletionJobAsync());
        _factory.Clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(1, await RunCompletionJobAsync());
        Assert.Equal(0, await RunCompletionJobAsync());

        _factory.InDatabase(db =>
        {
            var stored = db.Orders.Single(o => o.Id == orderId);
            Assert.Equal(OrderStatus.Completed, stored.Status);
            Assert.Equal(Now(), stored.CompletedAt);
            Assert.Equal(OrderActor.System, db.Set<OrderStatusChange>().Single(c => c.To == OrderStatus.Completed).Actor);
        });
        Assert.Equal(orderId, Assert.Single(_factory.Completed.Events).OrderId);
    }

    [Fact]
    public async Task The_completion_job_leaves_shipped_and_paid_orders_alone()
    {
        await _buyer.SignInVerifiedMemberAsync(_factory);
        var (shippedId, seller) = await _factory.SeedPaidOrderAsync(_buyer);
        (await seller.AddTrackingAsync(shippedId)).EnsureSuccessStatusCode();
        await _factory.SeedPaidOrderAsync(_buyer, 30002);
        _factory.Clock.Advance(TimeSpan.FromDays(4));

        Assert.Equal(0, await RunCompletionJobAsync());

        _factory.InDatabase(db => Assert.DoesNotContain(db.Orders, o => o.Status == OrderStatus.Completed));
        Assert.Empty(_factory.Completed.Events);
    }

    public void Dispose()
    {
        _buyer.Dispose();
        _anonymous.Dispose();
        _factory.Dispose();
    }

    private DateTime Now() => _factory.Clock.GetUtcNow().UtcDateTime;

    private async Task<int> RunCompletionJobAsync()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CompleteDeliveredOrdersCommand());
    }
}
