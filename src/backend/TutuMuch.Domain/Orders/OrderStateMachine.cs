namespace TutuMuch.Domain.Orders;

/// <summary>Who triggered a transition, as the order history records it (L2-032).</summary>
public enum OrderActor
{
    Buyer,
    Seller,
    Administrator,
    System,
    Carrier,
}

/// <summary>A transition that L2-032 does not allow. The API answers 409 ORDER_INVALID_TRANSITION.</summary>
public sealed class InvalidOrderTransitionException(OrderStatus from, OrderStatus to, OrderActor actor) :
    InvalidOperationException($"An order cannot move from {from} to {to} by {actor}.")
{
    public OrderStatus From { get; } = from;

    public OrderStatus To { get; } = to;
}

/// <summary>The allowed order transitions and who may trigger each (L2-032). Every orders feature uses this table.</summary>
public static class OrderStateMachine
{
    private static readonly HashSet<(OrderStatus From, OrderStatus To, OrderActor Actor)> Allowed =
    [
        (OrderStatus.Paid, OrderStatus.Shipped, OrderActor.Seller),
        (OrderStatus.Paid, OrderStatus.Cancelled, OrderActor.Seller),
        (OrderStatus.Paid, OrderStatus.Cancelled, OrderActor.System),
        (OrderStatus.Shipped, OrderStatus.Delivered, OrderActor.Carrier),
        (OrderStatus.Shipped, OrderStatus.Delivered, OrderActor.Buyer),
        (OrderStatus.Delivered, OrderStatus.Completed, OrderActor.Buyer),
        (OrderStatus.Delivered, OrderStatus.Completed, OrderActor.System),
        (OrderStatus.Delivered, OrderStatus.Completed, OrderActor.Administrator),
        (OrderStatus.Shipped, OrderStatus.Refunded, OrderActor.Administrator),
        (OrderStatus.Delivered, OrderStatus.Refunded, OrderActor.Administrator),
    ];

    public static bool IsAllowed(OrderStatus from, OrderStatus to, OrderActor actor) => Allowed.Contains((from, to, actor));

    public static bool IsTerminal(OrderStatus status) =>
        status is OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Refunded;

    public static void EnsureAllowed(OrderStatus from, OrderStatus to, OrderActor actor)
    {
        if (!IsAllowed(from, to, actor))
        {
            throw new InvalidOrderTransitionException(from, to, actor);
        }
    }
}

/// <summary>One append-only row of an order's history: old status (null at creation), new status, time and actor (L2-032 AC2).</summary>
public sealed class OrderStatusChange
{
    private OrderStatusChange()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public OrderStatus? From { get; private set; }

    public OrderStatus To { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public OrderActor Actor { get; private set; }

    internal static OrderStatusChange Record(Guid orderId, OrderStatus? from, OrderStatus to, OrderActor actor, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        From = from,
        To = to,
        OccurredAt = now,
        Actor = actor,
    };
}

/// <summary>
/// Monday to Friday. US holidays are not skipped yet: the holiday list is to be supplied
/// (ADR orders/0003), so a deadline can fall on a holiday.
/// </summary>
public static class BusinessDays
{
    /// <summary>The same time of day <paramref name="days"/> weekdays later, counting from the day after <paramref name="start"/>.</summary>
    public static DateTime Add(DateTime start, int days)
    {
        var result = start;
        while (days > 0)
        {
            result = result.AddDays(1);
            if (result.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            {
                days--;
            }
        }

        return result;
    }
}
