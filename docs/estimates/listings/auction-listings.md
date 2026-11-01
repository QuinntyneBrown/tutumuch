# Auction listings — estimate

## Basis

The hours assume an intermediate Angular/TypeScript and ASP.NET Core/C# developer who is productive in both stacks but new to this codebase. They assume the listings, catalog, orders and notifications features exist as built: the `Listing` aggregate with its `Version` token, the sell and edit forms, relist, the listing detail page and store, the catalog query with its price filter, `StartCheckoutCommand` with its conditional reservation, `ReservationExpiryJob`, the email outbox and templates, `TuTuMuchApiFactory`, the worker test fakes and the Playwright fixtures. The auction mocks and the `docs/screen-composition.md` entries are done before this work; only `tl-auction-panel` and the card's auction variant are added to `@tutumuch/components`. Hours cover implementation, unit, integration and acceptance tests, and review fixes. [Detailed design](../../detailed-designs/listings/auction-listings/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `ListingFormat`, auction fields on `Listing`, draft and publish rules for the length, end time on publish | 4 |
| `Listing.PlaceBid`, `BidResult`, `Bid`, minimum bid and every refusal, with unit tests | 5 |
| `CloseAuction`, `ExpirePaymentWindow`, auction `ReleaseReservation`, relist of a new run, `EditRefusal` and `CanBeShowcase`, with unit tests | 5 |
| `BidConfiguration`, auction columns, checks, filtered indexes and the `AddAuctions` migration | 3 |
| `PlaceBidCommandHandler` with the outbid email in one `SaveChanges` and `OUTBID` on a concurrency failure | 4 |
| `BidsController` and API acceptance tests, including 20 concurrent bids and an edit racing a bid | 6 |
| Listing commands, withdraw, relist and showcase changes for auctions, with API tests | 4 |
| Catalog and detail queries: current price filter and sort, `AuctionSummaryDto`, viewer flags, privacy tests | 5 |
| `StartCheckoutCommand` winner-only reservation at the winning bid, release keeping the hold, checkout DTO, with API tests | 6 |
| `CloseAuctionsCommandHandler`, `AuctionCloseJob` and worker tests with a fake clock | 5 |
| 4 email models, templates and notification types, with tests | 3 |
| `@tutumuch/api` DTOs and `BidsApi` | 2 |
| Sell and edit form: format choice, starting bid label, length, read-only format, locked state | 5 |
| `tl-auction-panel` with countdown, status line and every viewer state | 5 |
| Listing detail store and page: Place bid, Pay now, seller view, `bid=1` return | 4 |
| `PlaceBidDialog` with success, refusals, `OUTBID` and ended | 4 |
| `tl-listing-card` auction variant and My listings auction rows | 3 |
| Checkout page winning-bid note, pay-by line and the 2 refusal views; email settings line | 2 |
| Playwright acceptance tests in `auction-listings.spec.ts` via page objects and `page.route` fixtures | 8 |
| **Total** | **83** |

## Minimal scope

The minimal version ships the format and length on the sell form, publishing with an end time, the bid endpoint with the `Version` race and `OUTBID`, the detail page panel with a plain "Place bid" dialog, closing by the worker, the winner-only checkout at the winning bid and the 48-hour window. It leaves out the card's auction variant (cards show the current price only), My listings auction details, the countdown under an hour in minutes, the checkout's winning-bid note, the read-only format on edit (the API still ignores it) and the 4 emails. It satisfies `L2-113`, `L2-115`, `L2-116`, `L2-117`, `L2-118` and `L2-120`, and part of `L2-114`; it defers `L2-119`. Minimal scope totals **60** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| The 4 auction emails | Members learn of outbids, wins and endings only on the site (`L2-119`) | 3 |
| Card auction variant and My listings auction rows | Cards and My listings show no tag, bid count or time left | 3 |
| Countdown signal; show the end date and time only | No live "Ends in" text (`L2-114` criterion 7) | 2 |
| Checkout winning-bid note and pay-by line | The winner sees the item price without the note | 1 |
| `bid=1` return to the dialog after sign in | A visitor presses "Place bid" again after signing in | 1 |

## Status

Complete: 0%. Nothing in the design is built and tested yet.
