using MediatR;
using Microsoft.EntityFrameworkCore;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Notifications;
using TuTuMuch.Domain.Finds;

namespace TuTuMuch.Application.Grabs;

/// <summary>What one pass of the grab deadline job did.</summary>
public sealed record GrabDeadlineResult(int Expired, int Received);

/// <summary>
/// Expires Requested grabs not accepted within 48 hours and Accepted grabs not bought by their
/// buy-by time, each with a full refund and an email, and marks Bought grabs Received 14 days after
/// they were bought (L2-119, ADR orders/0009).
/// </summary>
public sealed record RunGrabDeadlinesCommand : IRequest<GrabDeadlineResult>;

public sealed class RunGrabDeadlinesHandler(ITuTuMuchDbContext db, IPaymentProvider provider, IEmailOutbox outbox, TimeProvider clock) :
    IRequestHandler<RunGrabDeadlinesCommand, GrabDeadlineResult>
{
    public async Task<GrabDeadlineResult> Handle(RunGrabDeadlinesCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var receiveBefore = now - Grab.ReceivedAfter;
        var due = await db.Grabs
            .Where(g => (g.Status == GrabStatus.Requested && g.AnswerBy <= now)
                || (g.Status == GrabStatus.Accepted && g.BuyBy <= now)
                || (g.Status == GrabStatus.Bought && g.BoughtAt <= receiveBefore))
            .Select(g => g.Id)
            .ToListAsync(cancellationToken);
        int expired = 0, received = 0;
        foreach (var id in due)
        {
            db.ChangeTracker.Clear();
            var grab = await db.Grabs.SingleAsync(g => g.Id == id, cancellationToken);
            if (grab.IsDueToReceive(now))
            {
                grab.ConfirmReceived(now);
                if (await TrySaveAsync(cancellationToken))
                {
                    received++;
                }

                continue;
            }

            if (!grab.IsOverdue(now))
            {
                continue;
            }

            var title = await db.ShelfFinds.Where(f => f.Id == grab.FindId).Select(f => f.Title).SingleAsync(cancellationToken);
            var refund = await GrabSupport.AddRefundAsync(db, grab, grab.Expire(now), now, cancellationToken);
            GrabSupport.EmailShopper(outbox, grab, title);
            if (await TrySaveAsync(cancellationToken))
            {
                expired++;
                await GrabSupport.IssueAsync(db, provider, refund, cancellationToken);
            }
        }

        return new GrabDeadlineResult(expired, received);
    }

    private async Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // An administrator or the shopper moved it first.
            return false;
        }
    }
}
