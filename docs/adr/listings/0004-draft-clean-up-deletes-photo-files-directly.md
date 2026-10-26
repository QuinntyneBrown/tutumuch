# 0004. Draft clean-up deletes photo files directly, and its reminder is always on

Date: 2026-10-26

## Status

Accepted. Resolves the deferral in [ADR listings/0002](0002-my-listings-unpaged-list-and-deferred-draft-clean-up.md).

## Context

L2-019 criterion 4 deletes a draft not edited for 90 days with its photos, after emailing the
seller 7 days before. The manage-my-listings design left the photos to a `PhotoCleanupJob` of
upload-listing-photos, which was never built, and `IPhotoStorage` had no delete operation.
`ToastService` now exists in `@tutumuch/components`.

## Decision

- **One job, one command.** `DraftCleanupJob` in `TuTuMuch.Worker` sends `CleanUpDraftsCommand`
  once a day (`Listings:DraftCleanup:Interval`). The command first reminds about drafts 83 days
  old, then deletes drafts 90 days old whose reminder is at least 7 days old, so a missed run
  never deletes without notice. 90 and 7 are domain constants on `Listing`.
- **`DraftReminderSentAt`** on `Listings`, cleared by every save of the draft. Recording the
  reminder bumps `Version` but not `UpdatedAt`; a racing edit wins and the draft is kept.
- **The command deletes the photos, not a general photo clean-up job.** The listing and photo
  rows go in one save; then the originals, and the versions no other photo shares by content
  hash, go through two new port operations, `IPhotoStorage.DeleteOriginalAsync` and
  `DeletePublicAsync`. A failed file delete is logged and leaves an orphan file. Uploads never
  attached to a listing stay out of scope.
- **The reminder is a transactional listing notice.** `NotificationType.DraftExpiryReminder`
  falls in the always-on category, so it passes the dispatcher's preference check (L2-043) with
  marketing off. No new preference category is added.
- **The info toast** shows once per visit of "Your listings" when the seller has a draft.
- **No batching.** Each pass handles every due draft, as the ship reminder job does.

## Consequences

- L2-019 criterion 4 is implemented and covered by `DraftCleanupTests`.
- Orphan files from a failed delete, and photos uploaded but never attached, still need a sweep;
  that is a later photo-storage concern.
- A seller cannot turn the reminder off.
