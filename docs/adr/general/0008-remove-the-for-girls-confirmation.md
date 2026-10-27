# ADR-0008: Remove the "This item is for girls" Confirmation

**Date:** 2026-10-26
**Category:** general
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

[ADR-0006](0006-all-things-young-girls.md) kept a seller confirmation on the sell and edit
listing form, reworded to "This item is for girls" (requirement `L2-014`). A seller had to tick it
to publish, the API refused a publish without it (`SIZE_CONFIRMATION_REQUIRED`) and the
listing stored the time as `SizeConfirmedAt`. In practice it is one more checkbox that every
seller ticks. TuTuMuch is a marketplace for all things young girls, and the audience lock
(`L2-013`: size is always `Girls'`, set by the server) already says so on every listing.

## Decision

Remove the confirmation everywhere: the checkbox on the sell and edit listing form, the
`sizeConfirmed` field of the publish and update requests, the `SIZE_CONFIRMATION_REQUIRED`
violation, the `Listing.SizeConfirmedAt` property and its `SizeConfirmedAt` column (dropped by
the `RemoveSizeConfirmation` migration). `POST /api/listings/{reference}/publish` takes no body.
`L2-014` is retired and its number is never reused.

This supersedes only the confirmation part of ADR-0006. Everything else there stands: the six
categories, the audience lock, the `Girls'` size and `CK_Listings_Size`. Every listing is
still for girls; the marketplace rules page and the copy rule of
[ADR-0005](0005-marketing-wording.md) remain the way sellers learn that.

## Options Considered

### Option 1: Remove the confirmation (chosen)
- **Pros:** A shorter form and publish path; no field, flag, timestamp or violation to maintain.
- **Cons:** No per-listing record that the seller attested the item is for girls.

### Option 2: Keep it
- **Pros:** An explicit attestation per listing.
- **Cons:** Friction for every seller with little protection; the audience rule is the rules page's job.

## Consequences

- Existing `SizeConfirmedAt` values are discarded when the migration runs; `Down` restores a
  nullable, empty column.
- Clients that still send `sizeConfirmed` are unaffected: unknown members are ignored.
- Specs, designs, mocks, estimates and tests that mention the confirmation change with this ADR.

## References

- [ADR-0006](0006-all-things-young-girls.md) (confirmation part superseded)
- `docs/specs/L2.md` (`L2-014` retired)
