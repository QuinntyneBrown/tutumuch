using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Checkouts;
using TuTuMuch.Application.Listings;
using TuTuMuch.Domain.Orders;

namespace TuTuMuch.Application.Orders;

/// <summary>
/// One row of "Purchases" or "Sales" (L2-037): the item, the other party's display name, the total,
/// the status and the date paid, plus the ship-by date the sales row shows while the order is Paid.
/// </summary>
public sealed record OrderRowDto(Guid Id, string Reference, string Status, CheckoutItemDto Item, string Total, string OtherParty, DateTime PaidAt, DateTime? ShipBy);

/// <summary>The counts the page subtitles read: on the way (Shipped), to confirm (Delivered), to ship (Paid).</summary>
public sealed record OrderCountsDto(int OnTheWay, int ToConfirm, int ToShip);

public sealed record OrderListDto(IReadOnlyList<OrderRowDto> Items, OrderCountsDto Counts);

/// <summary>
/// The signed-in member's purchases (<see cref="Role"/> Buyer) or sales (Seller), newest first
/// (PaidAt, then Id). Every order is returned: paging waits until a member can have more orders
/// than one page shows (detailed design order-history, open questions).
/// </summary>
public sealed record ListOrdersQuery(Guid MemberId, OrderRole Role) : IRequest<OrderListDto>;

public enum OrderRole
{
    Buyer,
    Seller,
}

public sealed class ListOrdersQueryHandler(ITuTuMuchDbContext db, IOptions<PhotoOptions> photoOptions) : IRequestHandler<ListOrdersQuery, OrderListDto>
{
    public async Task<OrderListDto> Handle(ListOrdersQuery query, CancellationToken cancellationToken)
    {
        var buyer = query.Role == OrderRole.Buyer;
        var orders = db.Orders.AsNoTracking().Where(o => buyer ? o.BuyerId == query.MemberId : o.SellerId == query.MemberId);
        var rows = await (
            from order in orders
            join member in db.Members.AsNoTracking() on (buyer ? order.SellerId : order.BuyerId) equals member.Id
            orderby order.PaidAt descending, order.Id descending
            select new { order.Id, order.Reference, order.Status, order.ListingId, order.Total, order.PaidAt, order.ShipBy, member.DisplayName })
            .ToListAsync(cancellationToken);

        var items = new List<OrderRowDto>(rows.Count);
        foreach (var row in rows)
        {
            items.Add(new OrderRowDto(
                row.Id,
                row.Reference,
                row.Status.ToString(),
                await OrderViews.ItemAsync(db, row.ListingId, photoOptions.Value, cancellationToken),
                CheckoutMoney.Format(row.Total),
                row.DisplayName,
                row.PaidAt,
                buyer || row.Status != OrderStatus.Paid ? null : row.ShipBy));
        }

        return new OrderListDto(items, new OrderCountsDto(
            rows.Count(r => r.Status == OrderStatus.Shipped),
            rows.Count(r => r.Status == OrderStatus.Delivered),
            rows.Count(r => r.Status == OrderStatus.Paid)));
    }
}

/// <summary>
/// The buyer's shipping address is visible to the seller from payment until 30 days after the order
/// ends (L2-037 criteria 2 and 3); afterwards the sale shows "Removed 30 days after the order ended."
/// </summary>
public static class ShippingAddressVisibility
{
    public static readonly TimeSpan AfterEnd = TimeSpan.FromDays(30);

    public static bool IsVisibleToSeller(Order order, DateTime now) =>
        order.EndedAt is not { } endedAt || now <= endedAt + AfterEnd;
}
