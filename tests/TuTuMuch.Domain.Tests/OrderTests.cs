// Unit Test
// Traces to: L2-032, L2-033, L2-034
// Description: The order state machine allows only the L2-032 transitions and records each one
// with its time and actor; the ship-by date is 5 business days after payment; tracking numbers
// are checked against the carrier's format. Confirming receipt completes a Shipped or Delivered
// order, and a Delivered order is due for automatic completion 3 days after delivery.

using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Domain.Tests;

public sealed class OrderTests
{
    /// <summary>Saturday 24 October 2026, as in the sale-detail mock.</summary>
    private static readonly DateTime PaidSaturday = new(2026, 10, 24, 15, 0, 0, DateTimeKind.Utc);

    private static readonly ShippingAddress Denver = ShippingAddress.Create("Tom Reyes", "1450 Larimer St", "Apt 6B", "Denver", "CO", "80202");

    private const string UspsNumber = "9400111899223456120477";

    [Fact]
    public void A_new_order_is_paid_with_its_creation_recorded_against_the_system()
    {
        var order = PaidOrder(PaidSaturday);

        Assert.Equal(OrderStatus.Paid, order.Status);
        var change = Assert.Single(order.StatusChanges);
        Assert.Null(change.From);
        Assert.Equal(OrderStatus.Paid, change.To);
        Assert.Equal(PaidSaturday, change.OccurredAt);
        Assert.Equal(OrderActor.System, change.Actor);
    }

    [Theory]
    [InlineData(2026, 10, 24, 2026, 10, 30)]
    [InlineData(2026, 10, 22, 2026, 10, 29)]
    [InlineData(2026, 10, 26, 2026, 11, 2)]
    public void The_ship_by_date_is_5_business_days_after_payment(int y, int m, int d, int sy, int sm, int sd)
    {
        var order = PaidOrder(new DateTime(y, m, d, 15, 0, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(sy, sm, sd, 15, 0, 0, DateTimeKind.Utc), order.ShipBy);
    }

    [Fact]
    public void Adding_tracking_ships_a_paid_order_and_records_the_seller()
    {
        var order = PaidOrder(PaidSaturday);
        var at = PaidSaturday.AddDays(1);

        order.MarkShipped(Carrier.Usps, UspsNumber, at);

        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Equal(Carrier.Usps, order.Carrier);
        Assert.Equal(UspsNumber, order.TrackingNumber);
        Assert.Equal(at, order.ShippedAt);
        var change = order.StatusChanges.Last();
        Assert.Equal((OrderStatus?)OrderStatus.Paid, change.From);
        Assert.Equal(OrderStatus.Shipped, change.To);
        Assert.Equal(at, change.OccurredAt);
        Assert.Equal(OrderActor.Seller, change.Actor);
    }

    [Fact]
    public void Carrier_delivery_moves_a_shipped_order_to_delivered()
    {
        var order = PaidOrder(PaidSaturday);
        order.MarkShipped(Carrier.Usps, UspsNumber, PaidSaturday.AddDays(1));

        order.MarkDelivered(OrderActor.Carrier, PaidSaturday.AddDays(4));

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(PaidSaturday.AddDays(4), order.DeliveredAt);
        Assert.Equal(OrderActor.Carrier, order.StatusChanges.Last().Actor);
    }

    [Fact]
    public void A_missed_deadline_cancels_a_paid_order_as_the_system()
    {
        var order = PaidOrder(PaidSaturday);

        order.Cancel(CancellationReason.ShipDeadlinePassed, null, OrderActor.System, order.ShipBy);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(order.ShipBy, order.EndedAt);
        Assert.Equal(OrderActor.System, order.StatusChanges.Last().Actor);
    }

    [Fact]
    public void A_seller_cancellation_keeps_the_reason_and_note_and_accepts_a_pending_request()
    {
        var order = PaidOrder(PaidSaturday);
        order.RequestCancellation("Wrong colour", PaidSaturday.AddHours(1));

        order.Cancel(CancellationReason.BuyerAskedToCancel, "Sorry", OrderActor.Seller, PaidSaturday.AddHours(2));

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(CancellationReason.BuyerAskedToCancel, order.CancellationReason);
        Assert.Equal("Sorry", order.CancellationNote);
        Assert.Equal(CancellationRequestStatus.Accepted, order.CancellationRequestStatus);
        Assert.Equal(PaidSaturday.AddHours(2), order.CancellationRespondedAt);
    }

    [Fact]
    public void A_buyer_asks_once_and_only_while_paid()
    {
        var order = PaidOrder(PaidSaturday);
        order.RequestCancellation(null, PaidSaturday);
        order.DeclineCancellation(PaidSaturday.AddHours(1));

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(CancellationRequestStatus.Declined, order.CancellationRequestStatus);
        Assert.Throws<InvalidOperationException>(() => order.RequestCancellation("again", PaidSaturday));
        Assert.Throws<InvalidOperationException>(() => order.DeclineCancellation(PaidSaturday));

        var shipped = PaidOrder(PaidSaturday);
        shipped.MarkShipped(Carrier.Usps, "9400111899223456120477", PaidSaturday.AddDays(1));
        Assert.Throws<InvalidOrderTransitionException>(() => shipped.RequestCancellation(null, PaidSaturday.AddDays(2)));
    }

    [Fact]
    public void A_paid_order_cannot_be_completed_directly()
    {
        var order = PaidOrder(PaidSaturday);

        Assert.Throws<InvalidOrderTransitionException>(() => order.TransitionTo(OrderStatus.Completed, OrderActor.Seller, PaidSaturday));
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Single(order.StatusChanges);
    }

    [Fact]
    public void A_cancelled_order_accepts_no_transition()
    {
        var order = PaidOrder(PaidSaturday);
        order.Cancel(CancellationReason.ShipDeadlinePassed, null, OrderActor.System, order.ShipBy);

        foreach (var to in Enum.GetValues<OrderStatus>())
        {
            Assert.Throws<InvalidOrderTransitionException>(() => order.TransitionTo(to, OrderActor.System, order.ShipBy));
        }
    }

    [Fact]
    public void Only_the_listed_actors_may_trigger_a_transition()
    {
        Assert.False(OrderStateMachine.IsAllowed(OrderStatus.Paid, OrderStatus.Shipped, OrderActor.Buyer));
        Assert.False(OrderStateMachine.IsAllowed(OrderStatus.Paid, OrderStatus.Cancelled, OrderActor.Buyer));
        Assert.False(OrderStateMachine.IsAllowed(OrderStatus.Shipped, OrderStatus.Delivered, OrderActor.Seller));
        Assert.True(OrderStateMachine.IsAllowed(OrderStatus.Shipped, OrderStatus.Refunded, OrderActor.Administrator));
        Assert.True(OrderStateMachine.IsAllowed(OrderStatus.Delivered, OrderStatus.Completed, OrderActor.System));
    }

    [Fact]
    public void Shipping_twice_is_refused()
    {
        var order = PaidOrder(PaidSaturday);
        order.MarkShipped(Carrier.Usps, UspsNumber, PaidSaturday.AddDays(1));

        Assert.Throws<InvalidOrderTransitionException>(() => order.MarkShipped(Carrier.Ups, "1Z999AA10123456784", PaidSaturday.AddDays(2)));
    }

    [Theory]
    [InlineData(Carrier.Usps, "9400 1118 9922 3456 1204 77", "9400111899223456120477")]
    [InlineData(Carrier.Ups, "1z999aa10123456784", "1Z999AA10123456784")]
    [InlineData(Carrier.FedEx, "1234 5678 9012", "123456789012")]
    [InlineData(Carrier.Dhl, "12345-67890", "1234567890")]
    public void A_well_formed_tracking_number_is_normalised(Carrier carrier, string entered, string stored)
    {
        Assert.Equal(stored, TrackingNumbers.Normalise(carrier, entered));
    }

    [Theory]
    [InlineData(Carrier.Usps, "")]
    [InlineData(Carrier.Usps, "9400 1118")]
    [InlineData(Carrier.Ups, "9400111899223456120477")]
    [InlineData(Carrier.FedEx, "ABC")]
    [InlineData(Carrier.Dhl, "12345")]
    public void A_malformed_tracking_number_is_refused(Carrier carrier, string entered)
    {
        Assert.Null(TrackingNumbers.Normalise(carrier, entered));
    }

    [Fact]
    public void The_usps_message_says_how_long_the_number_usually_is()
    {
        Assert.Equal("That doesn’t look like a USPS tracking number. It’s usually 22 digits.", TrackingNumbers.InvalidMessage(Carrier.Usps));
    }

    [Fact]
    public void Confirming_receipt_of_a_delivered_order_completes_it_as_the_buyer()
    {
        var order = DeliveredOrder();
        var at = PaidSaturday.AddDays(5);

        order.ConfirmReceipt(at);

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(at, order.CompletedAt);
        Assert.Equal(at, order.EndedAt);
        Assert.Equal(OrderActor.Buyer, order.StatusChanges.Last().Actor);
    }

    [Fact]
    public void Confirming_receipt_of_a_shipped_order_records_delivered_then_completed_against_the_buyer()
    {
        var order = PaidOrder(PaidSaturday);
        order.MarkShipped(Carrier.Usps, UspsNumber, PaidSaturday.AddDays(1));
        var at = PaidSaturday.AddDays(2);

        order.ConfirmReceipt(at);

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(at, order.DeliveredAt);
        var last = order.StatusChanges.TakeLast(2).ToList();
        Assert.Equal([OrderStatus.Delivered, OrderStatus.Completed], last.Select(c => c.To));
        Assert.All(last, c => Assert.Equal(OrderActor.Buyer, c.Actor));
    }

    [Fact]
    public void A_paid_or_completed_order_cannot_be_confirmed()
    {
        var paid = PaidOrder(PaidSaturday);
        Assert.Throws<InvalidOrderTransitionException>(() => paid.ConfirmReceipt(PaidSaturday));

        var completed = DeliveredOrder();
        completed.ConfirmReceipt(PaidSaturday.AddDays(5));
        Assert.Throws<InvalidOrderTransitionException>(() => completed.ConfirmReceipt(PaidSaturday.AddDays(6)));
        Assert.Equal(4, completed.StatusChanges.Count);
    }

    [Fact]
    public void A_delivered_order_completes_automatically_3_days_after_delivery()
    {
        var order = DeliveredOrder();
        var deliveredAt = order.DeliveredAt!.Value;

        Assert.Equal(deliveredAt.AddDays(3), order.AutoCompletesAt);
        Assert.False(order.IsDueForAutoCompletion(deliveredAt.AddDays(3).AddSeconds(-1)));
        Assert.True(order.IsDueForAutoCompletion(deliveredAt.AddDays(3)));

        order.Complete(OrderActor.System, deliveredAt.AddDays(3));

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(OrderActor.System, order.StatusChanges.Last().Actor);
        Assert.Null(order.AutoCompletesAt);
    }

    [Fact]
    public void Only_a_delivered_order_has_an_automatic_completion_time()
    {
        var order = PaidOrder(PaidSaturday);
        order.MarkShipped(Carrier.Usps, UspsNumber, PaidSaturday.AddDays(1));

        Assert.Null(order.AutoCompletesAt);
        Assert.False(order.IsDueForAutoCompletion(PaidSaturday.AddDays(30)));
    }

    [Fact]
    public void A_paid_order_reminds_its_seller_once_3_business_days_after_payment()
    {
        var order = PaidOrder(PaidSaturday);

        Assert.Equal(BusinessDays.Add(PaidSaturday, 3), order.ShipReminderDue);
        Assert.False(order.IsDueForShipReminder(order.ShipReminderDue.AddSeconds(-1)));
        Assert.True(order.IsDueForShipReminder(order.ShipReminderDue));

        order.MarkShipReminderSent(order.ShipReminderDue);

        Assert.False(order.IsDueForShipReminder(order.ShipReminderDue.AddDays(1)));
        Assert.Throws<InvalidOperationException>(() => order.MarkShipReminderSent(order.ShipReminderDue.AddDays(1)));
    }

    [Fact]
    public void A_shipped_order_is_not_due_for_a_reminder()
    {
        var order = PaidOrder(PaidSaturday);
        order.MarkShipped(Carrier.Usps, "9400111899223456120477", PaidSaturday.AddHours(1));

        Assert.False(order.IsDueForShipReminder(PaidSaturday.AddDays(10)));
    }

    private static Order DeliveredOrder()
    {
        var order = PaidOrder(PaidSaturday);
        order.MarkShipped(Carrier.Usps, UspsNumber, PaidSaturday.AddDays(1));
        order.MarkDelivered(OrderActor.Carrier, PaidSaturday.AddDays(3));
        return order;
    }

    private static Order PaidOrder(DateTime paidAt)
    {
        var checkout = Checkout.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 38m, 7m, paidAt.AddMinutes(-5));
        checkout.SetAddress(Denver, 3.15m);
        var payment = Payment.Create(checkout.Id, Guid.NewGuid(), 48.15m, paidAt);
        return Order.CreatePaid(48302, checkout, Guid.NewGuid(), payment, 0.10m, paidAt);
    }
}
