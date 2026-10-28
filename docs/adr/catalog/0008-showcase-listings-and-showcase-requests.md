# 0008. Showcase listings: a flag on the listing, refused before checkout, and showcase requests with a same-site return URL

Date: 2026-10-27

## Status

Accepted

## Context

TuTuMuch's owner, an administrator (`L2-010`), wants to show items that have already gone to a new home so that shoppers can ask for one like them (`L1-024`). Such a listing must look and browse like any Active listing, with its price and "Buy now", but must never be reserved, sold or paid for, and must work in every launch phase. "Buy now" on it shows the Sold label and "Request this item"; a visitor signs in or joins and comes straight back to the request. The listing state machine (Draft, Active, Reserved, Sold, Removed) is shared by the catalog, checkout, payment, cancellation and relist, and `L2-029` guarantees one order per listing with a conditional update. An "item request" already exists (`L1-023`, [ADR catalog/0004](0004-match-item-requests-in-process-when-a-listing-becomes-active.md)): a saved search matched against new listings.

## Decision

- **A flag, not a status.** `Listing.IsShowcase` is a boolean column, default false. A showcase listing keeps the status Active, so every catalog query, filter, sort, card and the detail page treat it like any Active listing with no change (`L2-107`). A new status would have had to be added to every catalog query and visibility rule and would have changed how the listing looks.
- **Only the administrator, only their own Draft or Active listing.** `PUT /api/admin/listings/{reference}/showcase` sits behind the existing `Administrator` policy; another member's listing answers `404`, and a Reserved, Sold or Removed listing `409 SHOWCASE_NOT_ALLOWED`. Marking bumps the listing's version like any change. A draft may be marked so the listing is a showcase from the moment it is published.
- **It never enters the orders state machine.** The web app never sends a checkout for a showcase listing: "Buy now" shows the Sold notice in place. As a guard for stale pages and direct links, `StartCheckoutCommand` refuses it with `409 LISTING_SHOWCASE` before any reservation, and the conditional reservation update requires `IsShowcase = false`, so a checkout racing the switch cannot reserve it. With no reservation there is no checkout, payment, order, payout or relist for it, and `L2-029` is unchanged for ordinary listings. Item request matching skips showcase listings, and My requests hides them as matches.
- **The flag is public, the requests are not.** `ListingDetailDto.showcase` tells the web app which "Buy now" to render. It reveals nothing about who asked or how often; no public response counts or names requests (`L2-111`).
- **Showcase requests are their own table.** `ShowcaseRequest` (member, listing, optional note of 500 characters, dates) is a different thing from an item request and lives in `ShowcaseRequests` with a unique index on (member, listing): sending again updates the one request. It cascades from both members and listings, so erasing a member erases their requests; a closed member's requests leave the admin list at once. New requests are limited to 20 per member per UTC day, counted in the database like the publish limit ([ADR platform/0006](../platform/0006-daily-publish-limit-counted-in-database.md)).
- **Return after sign-in through a same-site `returnUrl`.** "Request this item" sends a visitor to `/sign-in?returnUrl=/items/{reference}?request=1`. Sign in, join and the Google start keep the value; one shared `safeReturnUrl` in `@tutumuch/api` follows only a path that starts with a single `/` (not `//` or `/\`), and the API already keeps only same-site return URLs for Google. Back on the listing, `request=1` shows the Sold notice and opens the request dialog for a signed-in member. Nothing is stored server-side for the return; a member who joins with email signs in after joining, as a request needs no verified email.
- **Screens live in `@tutumuch/catalog`.** The dialog sits beside the listing detail page, and the admin list at `/admin/requests` in the admin shell sits with them, so no feature library imports another. The list is the only administrator feature added.

## Consequences

- Turning the switch off makes the listing an ordinary Active listing again at once; nothing has to be migrated.
- A listing published before it was marked may already have matched item requests and emailed members; marking cannot recall those emails, so the owner marks the draft first.
- The demo seeder has no administrator, so it seeds no showcase listing; a developer marks one with the switch.
- Replying to requests and telling members when a showcase item is available are deferred.
