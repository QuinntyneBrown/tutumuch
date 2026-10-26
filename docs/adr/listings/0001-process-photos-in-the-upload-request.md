# ADR-0001: Process Photos in the Upload Request

**Date:** 2026-10-25
**Category:** listings
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

The upload-listing-photos design processed photos in `TuTuMuch Worker`: the upload stored the
original, saved the photo as `Processing` and queued a `ProcessListingPhoto` outbox message,
and the client polled until the photo was `Ready`. Neither the outbox nor any worker job
exists yet; `TuTuMuch.Worker` is an empty host. Building both for one feature is more scope
than the feature needs. A 10 MB photo re-encodes to 6 versions in well under a second.

## Decision

`UploadListingPhotoCommandHandler` checks size, sniffed type, shortest side and pixel count,
then strips metadata and stores the 6 versions and the original in the same request before
it saves the `ListingPhoto` row. A saved photo is therefore always ready: there is no
`Processing` or `Failed` status, no polling and no `PHOTOS_PROCESSING` publish rule. The
public versions are served by `GET /photos/{hash}/{size}.{ext}` on `TuTuMuch API` with
`Cache-Control: public, max-age=31536000, immutable` until a CDN fronts them.

Deferred, each behind the existing ports: the outbox and `PhotoProcessingJob`, the
`PhotoCleanupJob` that deletes unattached photos and unreferenced versions, real object
storage and the CDN.

## Options Considered

### Option 1: Process in the request (chosen)
- **Pros:** No outbox, worker job or polling; one request gives the client a ready photo;
  simple to test with a fake `IPhotoStorage`.
- **Cons:** The request holds a thread for the encode; versions may be stored for an upload
  whose row save then fails (orphans until the clean-up job exists).

### Option 2: Outbox and worker as designed
- **Pros:** Upload latency independent of encoding; retries with back-off.
- **Cons:** Needs the outbox, a worker job, a status machine and client polling first.

## Consequences

### Positive
- Upload, process and attach ship together and are tested end to end in xUnit and Playwright.

### Negative
- Unattached photos and their objects are kept until the clean-up job is built.

### Risks
- Large uploads under load tie up API threads. Mitigation: 10 MB and 50 megapixel limits;
  move processing behind the outbox if upload latency becomes a problem.

## References

- [upload-listing-photos](../../detailed-designs/listings/upload-listing-photos/README.md)
- [ADR platform/0009](../platform/0009-skiasharp-for-photo-processing.md)
- `L2-017`, `L2-059`
