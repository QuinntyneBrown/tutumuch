# 0002. My listings: an unpaged list, the payout banner left in the shell, and the draft clean-up deferred

Date: 2026-10-26

## Status

Accepted. The draft clean-up deferral is resolved by
[ADR listings/0004](0004-draft-clean-up-deletes-photo-files-directly.md).

## Context

The manage-my-listings design ([listings/manage-my-listings](../../detailed-designs/listings/manage-my-listings/README.md))
lists a seller's listings by status (L2-021) with a keyset cursor whose page size and paging copy
were left `<TO SUPPLY>`. It also places the payout banner on the page, shows an info toast about
draft expiry through `ToastService`, and adds a daily job that reminds and then deletes drafts not
edited for 90 days (L2-019 criterion 4). The payout banner already sits in the app shell
([ADR orders/0005](../orders/0005-seller-payouts-payout-port-fee-snapshot-and-shell-banner.md)).
`ToastService` does not exist yet, and nothing deletes stored photo files once a listing is gone.

## Decision

- **No paging yet.** `GET /api/me/listings` returns every listing of the seller, ordered by
  `CreatedAt DESC, Id DESC`, with the count per status, as order history does
  ([ADR orders/0008](../orders/0008-order-history-unpaged-lists-and-address-window-on-read.md)).
  The endpoint stays on `ListingsController` under the `CanSell` policy; an unknown status answers
  `400 VALIDATION_FAILED`.
- **The status filter lives in the URL** (`?status=sold`) and is drawn by a new library component,
  `tl-filter-chips`; each change asks the API for that status.
- **The payout banner stays in the app shell.** Moving it into `MyListingsPage` would make
  `@tutumuch/listings` import `@tutumuch/orders`, which the workspace layout forbids; the shell already shows
  it on this page.
- **`@tutumuch/listings` is mounted at `/`** and owns `sell`, `sell/:reference` and `account/listings`,
  as `@tutumuch/orders` owns its account pages.
- **Deferred:** the draft clean-up job, its reminder email and `DraftReminderSentAt`, and the info
  toast. They need a photo-file deletion path and `ToastService`, neither of which exists; they
  follow as one slice of their own.

## Consequences

- A seller with many listings loads them all at once; paging is a backwards-compatible addition.
- L2-019 criterion 4 stays unimplemented until the clean-up slice lands; drafts are kept meanwhile.
