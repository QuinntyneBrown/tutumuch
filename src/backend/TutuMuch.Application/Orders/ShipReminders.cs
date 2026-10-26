using MediatR;
using Microsoft.EntityFrameworkCore;
using TutuMuch.Application.Abstractions;
using TutuMuch.Application.Notifications;
using TutuMuch.Domain.Notifications;
using TutuMuch.Domain.Orders;

namespace TutuMuch.Application.Orders;

/// <summary>
/// The ship-by reminder job's pass: every Paid order still without tracking 3 business days after
/// payment queues one reminder email to its seller (L2-033 AC3). Returns how many were queued.
/// </summary>
public sealed record SendShipRemindersCommand : IRequest<int>;

public sealed class SendShipRemindersHandler(ITutuMuchDbContext db, IEmailOutbox notifications, TimeProvider clock) :
    IRequestHandler<SendShipRemindersCommand, int>
{
    public async Task<int> Handle(SendShipRemindersCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var ids = await db.Orders
            .Where(o => o.Status == OrderStatus.Paid && o.ShipReminderSentAt == null && o.ShipReminderDue <= now)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);
        var reminded = 0;
        foreach (var id in ids)
        {
            var order = await db.Orders.SingleAsync(o => o.Id == id, cancellationToken);
            if (!order.IsDueForShipReminder(now))
            {
                continue;
            }

            try
            {
                order.MarkShipReminderSent(now);
                notifications.Enqueue(
                    NotificationType.ShipByReminder,
                    order.SellerId,
                    new ShipByReminderEmail(order.Id, order.Reference, await OrderEmails.ItemTitleAsync(db, order, cancellationToken), order.ShipBy));
                await db.SaveChangesAsync(cancellationToken);
                reminded++;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Shipped or cancelled between the select and the update: no reminder.
                db.ChangeTracker.Clear();
            }
        }

        return reminded;
    }
}

/// <summary>Values the order emails share.</summary>
internal static class OrderEmails
{
    public static async Task<string> ItemTitleAsync(ITutuMuchDbContext db, Order order, CancellationToken cancellationToken) =>
        await db.Listings.Where(l => l.Id == order.ListingId).Select(l => l.Title).SingleAsync(cancellationToken) ?? order.Reference;
}
