# 0002. Reserve with a conditional update and a filtered unique index

Date: 2026-10-26

## Status

Accepted

## Context

`L2-029` asks the database, not the client, to guarantee that concurrent checkouts reserve a
listing once. The detailed design guarded the conditional update with a `RowVersion` column.
`Listings` has no row version, and the tests run on SQLite in memory, which has no `rowversion`
type. The design also stores shipping addresses encrypted by `AesGcmFieldEncryptor` from
protect-personal-data, which is not built yet, and expected `Orders` from ship-order.

## Decision

- Starting checkout runs, in one transaction, `UPDATE Listings SET Status = 'Reserved',
  ReservedByCheckoutId, ReservedUntil WHERE Id = @id AND (Status = 'Active' OR (Status =
  'Reserved' AND ReservedUntil <= @now))` through `ExecuteUpdateAsync`, then inserts the
  checkout. Zero rows, or a violation of the filtered unique index `UX_Checkouts_ListingId_Open`
  (one `Open` or `Processing` checkout per listing), answer 409 `LISTING_RESERVED`. The status
  condition is the concurrency token; no row version is added.
- A lapsed reservation does not block the next buyer even before `ReservationExpiryJob` runs.
- `Orders` is created here with unique `UX_Orders_CheckoutId` and `UX_Orders_ListingId`; with
  only the `Paid` state there is nothing to filter yet. ship-order adds its states and filters
  the listing index on not cancelled.
- Shipping addresses are stored as plain columns on `Checkouts`, `Orders` and
  `SavedAddresses` until protect-personal-data adds the field encryptor and encrypts them.

## Consequences

- The 50-buyer acceptance test runs on a shared-cache SQLite in-memory database with one
  connection per request and gets exactly one 201.
- protect-personal-data must migrate the address columns to ciphertext (`L2-054`).
