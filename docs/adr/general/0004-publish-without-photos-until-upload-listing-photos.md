# ADR-0004: Publish Without Photos Until Upload Listing Photos Is Built

**Date:** 2026-10-25
**Category:** general
**Status:** Superseded (upload-listing-photos landed on 2026-10-25)
**Deciders:** Quinntyne Brown (product owner)

## Context

`L2-015` requires 1 to 8 photos on a published listing. Create-listing is prompt 01 of the
build order and photos are prompt 02 (upload-listing-photos); its implementation prompt
defers photos explicitly. Enforcing the photo rule now would make publishing impossible.

## Decision

Until upload-listing-photos lands, the sell page has no Photos section and the publish
rules do not include `PHOTOS_REQUIRED` or `PHOTOS_PROCESSING`. Upload-listing-photos adds
the `ListingPhotos` table, the uploader and both rules to `Listing.Publish` in the same
change, so a published listing without photos can exist only before that release.

## Options Considered

### Option 1: Defer the photo rule to upload-listing-photos (chosen)
- **Pros:** Each prompt ships a working feature; no dead UI.
- **Cons:** `L2-015` is only partly met between the two releases.

### Option 2: Enforce the rule now
- **Pros:** `L2-015` holds from the start.
- **Cons:** Nobody can publish until photos exist; the publish slice cannot be tested end to end.

## Consequences

### Positive
- Create-listing is complete and testable on its own.

### Negative
- Listings published before upload-listing-photos have no photos.

### Risks
- Release without photos reaches production. Mitigation: production deploy waits for both
  prompts; upload-listing-photos decides what happens to photo-less listings.

## Outcome

Upload-listing-photos added the `ListingPhotos` table, the uploader and `PHOTOS_REQUIRED`
("Add at least one photo") to `Listing.Publish`. `PHOTOS_PROCESSING` is not needed, because
photos are processed in the upload request
([ADR listings/0001](../listings/0001-process-photos-in-the-upload-request.md)). Listings
published before then keep no photos; no production release had happened, so none exist.

## References

- [create-listing](../../detailed-designs/listings/create-listing/README.md)
- [upload-listing-photos](../../detailed-designs/listings/upload-listing-photos/README.md)
- `docs/prompts/01-create-listing.md`, `docs/prompts/02-upload-listing-photos.md`
