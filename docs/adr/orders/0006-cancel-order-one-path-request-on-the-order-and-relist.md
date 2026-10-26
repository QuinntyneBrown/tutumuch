# 0006. Cancel order: one cancellation path, the request on the order, and relisting

Date: 2026-10-26

## Status

Accepted

## Context

The cancel-order design ([orders/cancel-order](../../detailed-designs/orders/cancel-order/README.md))
lets the seller cancel a Paid order, the buyer ask the seller to cancel, and the seller relist the
item (L2-035). ship-order already cancels unshipped orders from `ShipDeadlineJob`, refunding
directly through `IPaymentProvider` ([ADR orders/0001](0001-payment-provider-behind-a-port-until-chosen.md)).
`UX_Orders_ListingId` allowed one order per listing for ever
([ADR orders/0002](0002-reserve-with-a-conditional-update-and-a-filtered-unique-index.md)), so a
relisted listing could never sell again. The design left the note length, a second request after
a decline, and the payout of a cancelled order open, and used dotted error codes.

## Decision

- **One cancellation path.** `OrderCancellation` cancels the order, removes the listing
  (`Sold → Removed`, `RemovalReason = OrderCancelled`), adds a full `Refund` and queues the emails
  in one save, then issues the refund through `IPaymentProvider` with the refund ID as
  idempotency key. The seller, the seller accepting a request and the ship deadline job all use
  it, so a deadline cancellation now also takes the listing off sale and can be relisted. The
  refund keeps the existing "issue after the save, stay Pending on failure" behaviour; a refund
  outbox stays deferred as before.
- **The request lives on the order.** One request per order (L2-035 AC2) is four nullable columns
  on `Orders` (`CancellationRequestStatus`, `…Message`, `…RequestedAt`, `…RespondedAt`) instead of
  a `CancellationRequests` table. A buyer may ask once; after a decline they cannot ask again
  (`CANCELLATION_ALREADY_REQUESTED`). Because `Status` is the concurrency token, a request racing
  with shipping or the deadline job fails with `ORDER_INVALID_TRANSITION`.
- **Lengths.** The seller's note and the buyer's message are at most 500 characters, trimmed;
  blank is null.
- **Relisting.** `POST /api/listings/{reference}/relist` (seller only, 204) moves a listing removed
  by a cancellation back to `Active` with its original details. `UX_Orders_ListingId` is filtered
  to `Status <> 'Cancelled'` and the payment webhook ignores cancelled orders, so a relisted
  listing sells again. The relist dialog lives in `@tutumuch/orders` with the sale page, because
  feature libraries do not import each other; `relist` sits on `OrdersApi`. "Edit first" waits
  for edit-and-withdraw-listing.
- **Payouts.** The Held payout of a cancelled order stays Held and is never released
  ([ADR orders/0005](0005-seller-payouts-payout-port-fee-snapshot-and-shell-banner.md)); no
  `Cancelled` payout status is added.
- **Errors** are UPPER_SNAKE: `ORDER_INVALID_TRANSITION` (409), `CANCELLATION_INVALID` (422 with
  `reason`, `note`, `message` or `decision` field errors), `CANCELLATION_ALREADY_REQUESTED` (409),
  `CANCELLATION_NO_PENDING_REQUEST` (409) and `LISTING_NOT_RELISTABLE` (409).
- **Emails.** The buyer's cancellation email carries the reason and the note; a request emails the
  seller (`CancellationRequested`) with the message. A decline sends no email; the buyer sees it on
  the order page.

## Consequences

- One migration adds the cancellation columns on `Orders`, `RemovalReason` on `Listings`, filters
  `UX_Orders_ListingId`, and backfills earlier deadline cancellations (reason, listing removed).
- There is no toast service yet, so a relisted sale shows "The listing is back in the catalog."
  in its alert instead of a toast.
- Administrator-initiated cancellation stays out of scope.
