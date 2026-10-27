using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Listings;
using TuTuMuch.Application.Notifications;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Notifications;
using TuTuMuch.Domain.Requests;

namespace TuTuMuch.Application.Requests;

/// <summary>
/// Matches a listing that became Active against the open item requests of its category (L2-100,
/// ADR catalog/0004): never the seller's own, a closed member's or an already matched request
/// (L2-106). It records each match and queues one email per member (L2-101), and saves both
/// together. A failure is logged and never fails the publish or relist that raised the event.
/// </summary>
public sealed partial class MatchItemRequestsHandler(
    ITuTuMuchDbContext db, IEmailOutbox outbox, TimeProvider clock, ILogger<MatchItemRequestsHandler> logger) : INotificationHandler<ListingBecameActive>
{
    public async Task Handle(ListingBecameActive notification, CancellationToken cancellationToken)
    {
        try
        {
            await MatchAsync(notification.ListingId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            db.ChangeTracker.Clear();
            LogMatchingFailed(logger, exception, notification.ListingId);
        }
    }

    private async Task MatchAsync(Guid listingId, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.AsNoTracking().SingleOrDefaultAsync(l => l.Id == listingId, cancellationToken);
        if (listing is not { Status: ListingStatus.Active })
        {
            return;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var candidates = await db.ItemRequests
            .Where(r => r.Category == listing.Category && r.ExpiresAt > now && r.MemberId != listing.SellerId)
            .Where(r => db.Members.Any(m => m.Id == r.MemberId && m.ClosedAt == null))
            .Where(r => !db.ItemRequestMatches.Any(m => m.ItemRequestId == r.Id && m.ListingId == listing.Id))
            .ToListAsync(cancellationToken);
        var matched = candidates.Where(r => r.Matches(listing)).ToList();
        if (matched.Count == 0)
        {
            return;
        }

        foreach (var request in matched)
        {
            db.ItemRequestMatches.Add(ItemRequestMatch.Record(request.Id, listing.Id, now));
        }

        var email = new ItemRequestMatchedEmail(listing.Reference, listing.Title ?? string.Empty, listing.Brand ?? string.Empty, listing.Price ?? 0m);
        foreach (var memberId in matched.Select(r => r.MemberId).Distinct())
        {
            outbox.Enqueue(NotificationType.ItemRequestMatched, memberId, email);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Matching item requests against listing {ListingId} failed")]
    private static partial void LogMatchingFailed(ILogger logger, Exception exception, Guid listingId);
}

/// <summary>Deletes item requests that expired at least 30 days ago, with their matches (L2-098 AC8). Returns how many.</summary>
public sealed record CleanUpItemRequestsCommand : IRequest<int>;

public sealed class CleanUpItemRequestsHandler(ITuTuMuchDbContext db, TimeProvider clock) : IRequestHandler<CleanUpItemRequestsCommand, int>
{
    public async Task<int> Handle(CleanUpItemRequestsCommand command, CancellationToken cancellationToken)
    {
        var cutoff = clock.GetUtcNow().UtcDateTime.AddDays(-ItemRequest.KeepExpiredDays);
        var due = await db.ItemRequests.Where(r => r.ExpiresAt <= cutoff).ToListAsync(cancellationToken);
        if (due.Count == 0)
        {
            return 0;
        }

        var ids = due.Select(r => r.Id).ToList();
        db.ItemRequestMatches.RemoveRange(await db.ItemRequestMatches.Where(m => ids.Contains(m.ItemRequestId)).ToListAsync(cancellationToken));
        db.ItemRequests.RemoveRange(due);
        await db.SaveChangesAsync(cancellationToken);
        return due.Count;
    }
}
