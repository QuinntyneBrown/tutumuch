# 0009. Auction winner checkout and a 48-hour payment window

Date: 2026-11-01

## Status

Accepted

## Context

An auction that ends with bids has a winner who must pay the winning bid plus shipping and tax
(`L2-118`). Checkout (`L2-027` to `L2-030`) reserves an `Active` listing for 10 minutes with a
conditional update (ADR orders/0002) and releases it to `Active` when the checkout lapses. An
ended auction must not go back on sale to anyone, and the winner needs more than 10 minutes to
come back and pay.

## Decision

- **Closing reserves the listing for the winner.** `AuctionCloseJob` sets the listing to
  `Reserved` with `WinnerId` = the highest bidder, `PayBy` = end time + 48 hours,
  `ReservedUntil` = `PayBy` and no `ReservedByCheckoutId`.
- **Only the winner starts checkout, before `PayBy`.** The conditional update for an auction
  matches `Status = 'Reserved' AND WinnerId = @buyer AND PayBy > @now AND (ReservedByCheckoutId
  IS NULL OR ReservedUntil <= @now)`. The checkout's item price is `CurrentBid`. Everyone else
  gets 409 `AUCTION_BIDS_ONLY` while the auction runs and 409 `AUCTION_ENDED` after it ends.
- **A lapsed or released winner checkout keeps the hold.** `ReleaseReservation` puts an auction
  back to "reserved for the winner until `PayBy`" instead of `Active`.
- **48 hours.** Long enough for a winner who bid on Friday night to pay over the weekend; short
  enough that the seller is not left waiting. The job ends a window that passed with no
  checkout in progress: the listing becomes `Removed` with `AuctionUnpaid`, relistable, and the
  winner and seller are emailed.
- **The rest is unchanged.** The payment webhook marks the listing `Sold` and creates the order;
  `UX_Orders_ListingId` and `L2-029` still guarantee one order.

## Consequences

- No second checkout path: address, tax, payment and the webhook are shared.
- A winner who does not pay has no penalty yet; non-paying-bidder strikes are deferred.
