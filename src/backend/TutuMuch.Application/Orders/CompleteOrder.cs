using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TutuMuch.Application.Abstractions;
using TutuMuch.Application.Checkouts;
using TutuMuch.Application.Common;
using TutuMuch.Application.Listings;
using TutuMuch.Domain.Orders;

namespace TutuMuch.Application.Orders;

/// <summary>
/// An order became Completed (L2-034), published once after the change is saved. Completion makes
/// the seller's payout releasable: seller-payouts handles it (ADR orders/0004).
/// </summary>
public sealed record OrderCompleted(Guid OrderId, Guid BuyerId, Guid SellerId, DateTime CompletedAt) : INotification
{
    public static OrderCompleted From(Order order) => new(order.Id, order.BuyerId, order.SellerId, order.CompletedAt!.Value);
}

/// <summary>The buyer confirms receipt of a Shipped or Delivered order, which becomes Completed (L2-034 AC1).</summary>
public sealed record ConfirmReceiptCommand(Guid BuyerId, Guid OrderId) : IRequest<Result<PurchaseDto>>;

public sealed class ConfirmReceiptCommandHandler(ITutuMuchDbContext db, IPublisher publisher, TimeProvider clock, IOptions<PhotoOptions> photoOptions) :
    IRequestHandler<ConfirmReceiptCommand, Result<PurchaseDto>>
{
    public async Task<Result<PurchaseDto>> Handle(ConfirmReceiptCommand command, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(o => o.StatusChanges)
            .SingleOrDefaultAsync(o => o.Id == command.OrderId && o.BuyerId == command.BuyerId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.NotFound;
        }

        if (await ProblemViews.IsOpenAsync(db, order.Id, cancellationToken))
        {
            // An open problem is decided by an administrator, not by the buyer (L2-034 AC3, L2-036).
            return ProblemErrors.ProblemOpen;
        }

        try
        {
            order.ConfirmReceipt(clock.GetUtcNow().UtcDateTime);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidOrderTransitionException or DbUpdateConcurrencyException)
        {
            // Not Shipped or Delivered, or the completion job got there between the read and the write.
            return OrderErrors.InvalidTransition;
        }

        await publisher.Publish(OrderCompleted.From(order), cancellationToken);
        return await PurchaseViews.PurchaseAsync(db, order, photoOptions.Value, clock, cancellationToken);
    }
}

/// <summary>
/// The completion job's pass: every Delivered order 3 days after delivery is completed by the
/// system (L2-034 AC2), except an order with an open problem, which waits for an administrator
/// (L2-034 AC3, L2-036). Returns how many were completed.
/// </summary>
public sealed record CompleteDeliveredOrdersCommand : IRequest<int>;

public sealed class CompleteDeliveredOrdersHandler(ITutuMuchDbContext db, IPublisher publisher, TimeProvider clock) :
    IRequestHandler<CompleteDeliveredOrdersCommand, int>
{
    public async Task<int> Handle(CompleteDeliveredOrdersCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var deliveredBy = now - Order.AutoCompleteAfter;
        var ids = await db.Orders
            .Where(o => o.Status == OrderStatus.Delivered && o.DeliveredAt <= deliveredBy)
            .Where(o => !db.OrderProblems.Any(p => p.OrderId == o.Id && p.Status == ProblemStatus.Open))
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);
        var completed = 0;
        foreach (var id in ids)
        {
            var order = await db.Orders.Include(o => o.StatusChanges).SingleAsync(o => o.Id == id, cancellationToken);
            try
            {
                order.Complete(OrderActor.System, now);
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is InvalidOrderTransitionException or DbUpdateConcurrencyException)
            {
                // The buyer confirmed between the select and the update: leave it.
                db.ChangeTracker.Clear();
                continue;
            }

            completed++;
            await publisher.Publish(OrderCompleted.From(order), cancellationToken);
        }

        return completed;
    }
}
