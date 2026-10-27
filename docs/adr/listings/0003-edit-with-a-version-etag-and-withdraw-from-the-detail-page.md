# 0003. Edit with a version ETag, and withdraw from the listing detail page

Date: 2026-10-26

## Status

Accepted

## Context

The edit-and-withdraw-listing design ([listings/edit-and-withdraw-listing](../../detailed-designs/listings/edit-and-withdraw-listing/README.md))
protects edits with an ETag derived from a SQL Server `rowversion` column, requires `If-Match` on
every `PUT /api/listings/{reference}`, opens the Withdraw dialog from the seller's listing detail
page, and raises an "Undo" toast after a price-only change. Backend tests run on the SQLite
in-memory provider, which does not generate `rowversion` values. The create-listing form already
calls the same `PUT` for drafts without any `If-Match`. The listing detail page lives in
`@tutumuch/catalog`, and feature libraries may not import each other. `ToastService` does not exist yet.

## Decision

- **A version counter, not `rowversion`.** `Listing.Version` is an `int` that every domain change
  bumps; EF Core treats it as the concurrency token. The checkout's conditional reservation update
  bumps it too, so an edit or withdraw racing a reservation fails its save and answers `409
  LISTING_RESERVED` instead of overwriting (L2-020, L2-029). The same code runs on SQL Server and
  SQLite.
- **The ETag is the version as a strong tag**, `"7"`, sent by `GET /api/me/listings/{reference}`
  and every successful `PUT`. `If-Match` is required only when the listing is no longer a draft:
  missing is `428 PRECONDITION_REQUIRED`, stale is `412 LISTING_CHANGED`. Drafts keep the
  last-write-wins behaviour of create-listing, since only their seller can see them.
- **One `PUT` for drafts and published listings.** A published listing takes the publish rules of
  L2-014 to L2-018 and the size confirmation again (`sizeConfirmed` in the body; removed by
  [ADR general/0008](../general/0008-remove-the-for-girls-confirmation.md)). The checks run in
  this order: 404, 409 (Reserved, Sold, withdrawn), 428, 412, 422.
- **Withdraw is final.** `POST /api/listings/{reference}/withdraw` moves `Active` to `Removed` with
  reason `Withdrawn` and answers `200` again on a repeat. A withdrawn listing cannot be edited or
  relisted; only a listing removed by a cancelled order can be relisted, and it can be edited first.
- **The Withdraw dialog lives in `@tutumuch/catalog`** (`src/lib/withdraw-listing/`) beside the listing
  detail page that opens it, as the Relist dialog lives in `@tutumuch/orders` beside the sale page
  ([ADR orders/0006](../orders/0006-cancel-order-one-path-request-on-the-order-and-relist.md)). Both call
  `@tutumuch/api`. The edit page lives in `@tutumuch/listings` at `/items/:reference/edit` and shares the
  sell form through a local `ListingFormComponent`.
- **Deferred:** the price-only "Undo" toast, until `ToastService` exists, and edit history.

## Consequences

- Clients must read the listing before saving a published one; a stale tab shows "This listing
  changed since you opened it" and reloads instead of overwriting newer changes.
- Any future code that changes a listing with `ExecuteUpdate` must bump `Version` as well.
