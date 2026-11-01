# 0005. Auctions as a listing format

Date: 2026-11-01

## Status

Accepted

## Context

`L1-025` lets a seller offer an item by auction instead of at a fixed price (`L2-113` to
`L2-120`). The `Listing` aggregate already carries everything an item needs: the category and
size lock (`L2-013`), photos, condition, shipping, the Draft → Active → Reserved → Sold →
Removed state machine (`L2-019`), the `Version` concurrency token (`L2-020`, `L2-029`), the
launch-phase rules (`L2-091`, `L2-092`), My listings, relist, checkout, orders and payouts. An
auction differs only in how the price is found and who may buy. A separate `Auction` aggregate
would need its own photos, catalog query, detail page, checkout path and order link.

## Decision

- **A format, not a new aggregate.** `Listing` gets `Format` (`FixedPrice` or `Auction`),
  `AuctionDays` (3, 5 or 7), `AuctionEndsAt`, `CurrentBid`, `BidCount`, `HighBidderId`,
  `WinnerId` and `PayBy`. Each bid is a row in a new `Bids` table (`Id`, `ListingId`,
  `BidderId`, `Amount`, `PlacedAt`). No listing status is added: a running auction is
  `Active`, an ended auction awaiting payment is `Reserved` for the winner, a paid one is
  `Sold`, and an unsold or unpaid one is `Removed` with the new reasons `AuctionUnsold` or
  `AuctionUnpaid`, both relistable.
- **The starting bid is the price field.** It keeps the $1.00 to $5,000.00 rule of `L2-018`
  and its check constraint, so the form, validation and draft rules are shared. The catalog's
  current price is `CurrentBid ?? Price`, which the card, the detail page and the price filter
  and sort use.
- **Format and length are fixed at publish.** The end time is the publish (or relist) time plus
  the length. An edit after publish ignores both.
- **Minimum increment of $1.00 and a $5,000.00 ceiling.** One flat step is easy to explain on a
  small marketplace; tiered steps can come later. The ceiling matches the highest price.
- **Bids use the listing's `Version` token.** `PlaceBid` checks the rules on the loaded listing
  and changes `CurrentBid`, `BidCount`, `HighBidderId` and `Version`; `SaveChangesAsync`
  then fails with a concurrency exception for every other bid read against the same version,
  which the API answers 409 `OUTBID`. This is the same token that turns a racing edit into
  412 (`L2-020`), so a bid and an edit cannot both win (`L2-116`).
- **One worker job closes auctions and payment windows.** `AuctionCloseJob` runs every minute
  next to `ReservationExpiryJob`. Bids are refused once `AuctionEndsAt` has passed, so the
  outcome never waits for the job.
- **Auction emails are order emails.** Outbid, won, ended and payment-missed emails go through
  the outbox in the `Orders` category, so they are always on (`L2-043`). No new preference is
  added.
- **Bidders stay private.** No response or email names a bidder; the API gives the viewer only
  `viewerIsHighestBidder`, `viewerHasBid` and `viewerIsWinner`. There is no public bid history.
- **Not matched against item requests, never a showcase.** Publishing an auction does not run
  item request matching, and an auction cannot be marked as a showcase listing.

## Options considered

### Option 1: A format on `Listing` (chosen)
- **Pros:** Reuses photos, catalog, detail, My listings, checkout, orders and payouts; one state
  machine; one concurrency token.
- **Cons:** More nullable columns on `Listings`.

### Option 2: A separate `Auction` aggregate pointing at a listing
- **Pros:** Keeps `Listing` smaller.
- **Cons:** Two sources of truth for status and price, a second catalog path and a second
  checkout path.

## Consequences

- The `Listings` table gets the auction columns and a check that `CurrentBid` is at most
  5,000; a new `Bids` table is indexed on `(ListingId, PlacedAt)`.
- Deferred, recorded in the auction design's open questions: reserve prices, a "Buy it now"
  price on an auction, automatic (proxy) bidding, extending the end time after a late bid,
  retracting a bid, ending an auction early with bids, showcase auctions, matching auctions
  against item requests, a format filter and an "Ending soonest" sort.
