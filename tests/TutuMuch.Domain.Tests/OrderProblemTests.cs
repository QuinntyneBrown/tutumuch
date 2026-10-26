// Unit Test
// Traces to: L2-036, L2-031
// Description: A buyer may report a problem while the order is Shipped, or Delivered until 3 days
// after delivery. A problem takes at most 4 photos, one seller reply while Open and one decision.
// A problem holds the payout; a refund cancels it and release after completion makes it releasable.

using TutuMuch.Domain.Orders;
using TutuMuch.Domain.Payouts;

namespace TutuMuch.Domain.Tests;

public sealed class OrderProblemTests
{
    /// <summary>Saturday 24 October 2026, delivered as in the order-detail mock.</summary>
    private static readonly DateTime DeliveredSaturday = new(2026, 10, 24, 15, 0, 0, DateTimeKind.Utc);

    private static readonly ShippingAddress Denver = ShippingAddress.Create("Tom Reyes", "1450 Larimer St", "Apt 6B", "Denver", "CO", "80202");

    [Fact]
    public void A_paid_order_has_no_problem_window_and_a_shipped_one_has_no_deadline()
    {
        var order = PaidOrder();
        Assert.False(order.CanReportProblem(DeliveredSaturday));

        order.MarkShipped(Carrier.Usps, "9400111899223456120477", DeliveredSaturday.AddDays(-2));

        Assert.True(order.CanReportProblem(DeliveredSaturday.AddDays(30)));
        Assert.Null(order.ProblemDeadline);
    }

    [Fact]
    public void A_delivered_order_takes_problems_until_3_days_after_delivery()
    {
        var order = DeliveredOrder();

        Assert.Equal(new DateTime(2026, 10, 27, 15, 0, 0, DateTimeKind.Utc), order.ProblemDeadline);
        Assert.True(order.CanReportProblem(DeliveredSaturday.AddDays(3).AddSeconds(-1)));
        Assert.False(order.CanReportProblem(DeliveredSaturday.AddDays(3)));
    }

    [Fact]
    public void Opening_outside_the_window_or_with_5_photos_is_refused()
    {
        var order = DeliveredOrder();

        Assert.Throws<InvalidOperationException>(() => Open(order, DeliveredSaturday.AddDays(4)));
        Assert.Throws<ArgumentException>(() =>
            OrderProblem.Open(1001, order, ProblemReason.WrongSize, "Too small.", [.. Enumerable.Range(0, 5).Select(_ => Guid.NewGuid())], DeliveredSaturday));
    }

    [Fact]
    public void A_problem_is_referenced_by_number_and_keeps_its_photos_in_order()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var problem = OrderProblem.Open(1001, DeliveredOrder(), ProblemReason.WrongSize, "Label says M.", ids, DeliveredSaturday);

        Assert.Equal("P-1001", problem.Reference);
        Assert.Equal(ProblemStatus.Open, problem.Status);
        Assert.Equal(ids, problem.Photos.Select(p => p.Id));
        Assert.Equal($"problems/{problem.Id}/{ids[1]}.jpg", problem.Photos[1].StorageKey);
        Assert.Equal("Wrong size", problem.Reason.ToLabel());
    }

    [Fact]
    public void The_seller_replies_once_and_only_while_open()
    {
        var problem = Open(DeliveredOrder(), DeliveredSaturday);

        problem.RecordSellerResponse("Happy to take them back.", DeliveredSaturday.AddHours(2));

        Assert.Equal("Happy to take them back.", problem.SellerResponse);
        Assert.Throws<InvalidOperationException>(() => problem.RecordSellerResponse("Again.", DeliveredSaturday.AddHours(3)));
        var resolved = Open(DeliveredOrder(), DeliveredSaturday);
        resolved.Resolve(ProblemOutcome.ReleaseToSeller, null, "Fine.", Guid.NewGuid(), DeliveredSaturday);
        Assert.Throws<InvalidOperationException>(() => resolved.RecordSellerResponse("Late.", DeliveredSaturday));
    }

    [Fact]
    public void A_problem_is_resolved_once()
    {
        var problem = Open(DeliveredOrder(), DeliveredSaturday);
        var admin = Guid.NewGuid();

        problem.Resolve(ProblemOutcome.FullRefund, 48.15m, "Hole not disclosed.", admin, DeliveredSaturday.AddDays(1));

        Assert.Equal(ProblemStatus.Resolved, problem.Status);
        Assert.Equal(admin, problem.ResolvedBy);
        Assert.Equal(48.15m, problem.RefundAmount);
        Assert.Throws<InvalidOperationException>(() => problem.Resolve(ProblemOutcome.ReleaseToSeller, null, "x", admin, DeliveredSaturday));
    }

    [Fact]
    public void An_administrator_refund_moves_a_delivered_order_to_refunded()
    {
        var order = DeliveredOrder();

        order.RefundForProblem(DeliveredSaturday.AddDays(1));

        Assert.Equal(OrderStatus.Refunded, order.Status);
        Assert.Equal(OrderActor.Administrator, order.StatusChanges[^1].Actor);
        Assert.Equal(DeliveredSaturday.AddDays(1), order.EndedAt);
    }

    [Fact]
    public void A_problem_holds_the_payout_until_a_refund_cancels_it_or_completion_releases_it()
    {
        var refunded = Payout.CreateHeld(DeliveredOrder(), DeliveredSaturday);
        refunded.HoldForProblem();
        Assert.Equal(PayoutHoldReason.OpenProblem, refunded.HoldReason);
        refunded.OrderCompleted(sellerOnboarded: true, DeliveredSaturday);
        Assert.Equal(PayoutStatus.Releasable, refunded.Status);

        var cancelled = Payout.CreateHeld(DeliveredOrder(), DeliveredSaturday);
        cancelled.HoldForProblem();
        cancelled.Cancel();
        Assert.Equal(PayoutStatus.Cancelled, cancelled.Status);
        Assert.Null(cancelled.HoldReason);
        Assert.Throws<InvalidOperationException>(cancelled.HoldForProblem);
    }

    private static OrderProblem Open(Order order, DateTime now) =>
        OrderProblem.Open(1001, order, ProblemReason.NotAsDescribed, "Pilling.", [], now);

    private static Order DeliveredOrder()
    {
        var order = PaidOrder();
        order.MarkShipped(Carrier.Usps, "9400111899223456120477", DeliveredSaturday.AddDays(-2));
        order.MarkDelivered(OrderActor.Carrier, DeliveredSaturday);
        return order;
    }

    private static Order PaidOrder()
    {
        var paidAt = DeliveredSaturday.AddDays(-4);
        var checkout = Checkout.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 38m, 7m, paidAt.AddMinutes(-5));
        checkout.SetAddress(Denver, 3.15m);
        var payment = Payment.Create(checkout.Id, Guid.NewGuid(), 48.15m, paidAt);
        return Order.CreatePaid(48140, checkout, Guid.NewGuid(), payment, 0.10m, paidAt);
    }
}
