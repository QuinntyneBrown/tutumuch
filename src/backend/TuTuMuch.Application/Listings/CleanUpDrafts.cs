using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Notifications;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Notifications;

namespace TuTuMuch.Application.Listings;

/// <summary>
/// The draft clean-up job's pass (L2-019): reminds the seller of every draft not edited for 83 days,
/// once, then deletes every draft not edited for 90 days whose reminder is at least 7 days old,
/// with its photos and their stored files. Returns how many drafts were reminded or deleted.
/// </summary>
public sealed record CleanUpDraftsCommand : IRequest<int>;

public sealed class CleanUpDraftsHandler(
    ITuTuMuchDbContext db,
    IEmailOutbox notifications,
    IPhotoStorage storage,
    TimeProvider clock,
    ILogger<CleanUpDraftsHandler> logger) : IRequestHandler<CleanUpDraftsCommand, int>
{
    public async Task<int> Handle(CleanUpDraftsCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return await RemindAsync(now, cancellationToken) + await DeleteAsync(now, cancellationToken);
    }

    private async Task<int> RemindAsync(DateTime now, CancellationToken cancellationToken)
    {
        var staleBefore = now.AddDays(-(Listing.DraftRetentionDays - Listing.DraftReminderDaysBefore));
        var ids = await db.Listings
            .Where(l => l.Status == ListingStatus.Draft && l.DraftReminderSentAt == null && l.UpdatedAt <= staleBefore)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);
        var reminded = 0;
        foreach (var id in ids)
        {
            var draft = await db.Listings.SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
            if (draft is null || !draft.IsDueForDraftReminder(now))
            {
                continue;
            }

            try
            {
                draft.MarkDraftReminderSent(now);
                notifications.Enqueue(
                    NotificationType.DraftExpiryReminder,
                    draft.SellerId,
                    new DraftExpiryReminderEmail(draft.Reference, draft.Title ?? $"Untitled draft · {draft.Category}", draft.DraftDeleteOn(now)));
                await db.SaveChangesAsync(cancellationToken);
                reminded++;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Edited or published between the select and the update: its 90 days restarted.
                db.ChangeTracker.Clear();
            }
        }

        return reminded;
    }

    private async Task<int> DeleteAsync(DateTime now, CancellationToken cancellationToken)
    {
        var expiredBefore = now.AddDays(-Listing.DraftRetentionDays);
        var remindedBefore = now.AddDays(-Listing.DraftReminderDaysBefore);
        var ids = await db.Listings
            .Where(l => l.Status == ListingStatus.Draft && l.UpdatedAt <= expiredBefore && l.DraftReminderSentAt <= remindedBefore)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);
        var deleted = 0;
        foreach (var id in ids)
        {
            var draft = await db.Listings.SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
            if (draft is null || !draft.IsExpiredDraft(now))
            {
                continue;
            }

            var photos = await db.ListingPhotos.Where(p => p.ListingId == id).ToListAsync(cancellationToken);
            try
            {
                db.ListingPhotos.RemoveRange(photos);
                db.Listings.Remove(draft);
                await db.SaveChangesAsync(cancellationToken);
                deleted++;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Edited between the select and the delete: the draft is kept.
                db.ChangeTracker.Clear();
                continue;
            }

            await DeleteStoredFilesAsync(photos, cancellationToken);
        }

        return deleted;
    }

    /// <summary>
    /// After the rows are gone: each original, and each version no other photo shares by content
    /// (version keys are content-addressed). A failure leaves an orphan file, never a broken listing.
    /// </summary>
    private async Task DeleteStoredFilesAsync(IReadOnlyList<ListingPhoto> photos, CancellationToken cancellationToken)
    {
        var hashes = photos.Select(p => p.OriginalHash).Distinct().ToList();
        var shared = await db.ListingPhotos
            .Where(p => hashes.Contains(p.OriginalHash))
            .Select(p => p.OriginalHash)
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var photo in photos)
        {
            try
            {
                await storage.DeleteOriginalAsync(photo.OriginalKey, cancellationToken);
                if (!shared.Contains(photo.OriginalHash))
                {
                    foreach (var version in photo.Versions)
                    {
                        await storage.DeletePublicAsync(version.Key, cancellationToken);
                    }
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Deleting the stored files of photo {PhotoId} failed", photo.Id);
            }
        }
    }
}
