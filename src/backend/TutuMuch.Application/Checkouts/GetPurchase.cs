using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TutuMuch.Application.Abstractions;
using TutuMuch.Application.Common;
using TutuMuch.Application.Listings;
using TutuMuch.Application.Orders;
using TutuMuch.Domain.Orders;

namespace TutuMuch.Application.Checkouts;

/// <summary>
/// One order as its buyer's confirmation page shows it: reference, item, the amount paid with the
/// card brand and last four digits, and the seller's display name; the buyer's order page adds
/// the summary, ship-by date, timeline and tracking (ship-order), and when a Delivered order
/// completes by itself (complete-order), and the cancellation and the buyer's request to cancel
/// (cancel-order), and whether a problem can be reported until when, and the problem
/// (resolve-order-problems). order-history extends it.
/// </summary>
public sealed record PurchaseDto(
    Guid Id,
    string Reference,
    string Status,
    CheckoutItemDto Item,
    string Total,
    string? CardBrand,
    string? CardLast4,
    string SellerDisplayName,
    DateTime PaidAt,
    DateTime ShipBy,
    string ItemPrice,
    string ShippingCost,
    string SalesTax,
    IReadOnlyList<TimelineStepDto> Timeline,
    TrackingDto? Tracking,
    DateTime? AutoCompletesAt,
    CancellationDto? Cancellation,
    CancellationRequestDto? CancellationRequest,
    bool CanReportProblem,
    DateTime? ProblemDeadline,
    ProblemDto? Problem);

public sealed record GetPurchaseQuery(Guid BuyerId, Guid OrderId) : IRequest<Result<PurchaseDto>>;

public sealed class GetPurchaseQueryHandler(ITutuMuchDbContext db, IOptions<PhotoOptions> photoOptions, TimeProvider clock) :
    IRequestHandler<GetPurchaseQuery, Result<PurchaseDto>>
{
    public async Task<Result<PurchaseDto>> Handle(GetPurchaseQuery query, CancellationToken cancellationToken)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.StatusChanges).SingleOrDefaultAsync(o => o.Id == query.OrderId && o.BuyerId == query.BuyerId, cancellationToken);
        if (order is null)
        {
            return Error.NotFound("ORDER_NOT_FOUND");
        }

        return await PurchaseViews.PurchaseAsync(db, order, photoOptions.Value, clock, cancellationToken);
    }
}

public static class PurchaseViews
{
    /// <summary>The buyer's view of their order.</summary>
    public static async Task<PurchaseDto> PurchaseAsync(ITutuMuchDbContext db, Order order, PhotoOptions photos, TimeProvider clock, CancellationToken cancellationToken)
    {
        var payment = await db.Payments.AsNoTracking().SingleAsync(p => p.Id == order.PaymentId, cancellationToken);
        var problem = await ProblemViews.ForOrderAsync(db, order.Id, cancellationToken);
        var now = clock.GetUtcNow().UtcDateTime;
        var seller = await db.Members.AsNoTracking().Where(m => m.Id == order.SellerId).Select(m => m.DisplayName).SingleAsync(cancellationToken);
        return new PurchaseDto(
            order.Id,
            order.Reference,
            order.Status.ToString(),
            await OrderViews.ItemAsync(db, order.ListingId, photos, cancellationToken),
            CheckoutMoney.Format(order.Total),
            payment.CardBrand,
            payment.CardLast4,
            seller,
            order.PaidAt,
            order.ShipBy,
            CheckoutMoney.Format(order.ItemPrice),
            CheckoutMoney.Format(order.ShippingCost),
            CheckoutMoney.Format(order.SalesTax),
            OrderViews.Timeline(order),
            OrderViews.Tracking(order),
            order.AutoCompletesAt,
            OrderViews.Cancellation(order),
            OrderViews.CancellationRequest(order),
            problem is null && order.CanReportProblem(now),
            order.ProblemDeadline,
            problem);
    }
}
