# 0009. Shelf finds: their own table, no photos yet, posting under the selling rule and a maps link

Date: 2026-11-01

## Status

Accepted

## Context

A member who spots an item for girls in a store wants to tell others where to buy it: the store, its address, the shelf price and the day it was seen (`L1-025`). TuTuMuch's owner also wants to offer to buy such an item and send it to a shopper for a fee ([ADR orders/0009](../orders/0009-grab-it-for-me-fee-and-non-refundable-deposit.md)). Listings (`L1-004`) are things a seller has in hand and ships: their state machine (Draft, Active, Reserved, Sold, Removed) drives the catalog, checkout, payment, orders, payouts, item-request matching and the Phase 1 product limit. A find is none of these: nobody owns the item, TuTuMuch never ships it from the poster, and it goes stale in days.

## Decision

- **Its own table, not a listing.** `ShelfFind` (reference `F-{number}`, poster, title, category, shelf price, store name, store address, seen-on date, optional note, optional grab fee, status Active, Gone or Withdrawn, created and changed dates) lives in `ShelfFinds`. No catalog query, search, filter, item-request match, product limit or checkout sees it. A flag on `Listing`, as for showcase listings ([ADR catalog/0008](0008-showcase-listings-and-showcase-requests.md)), would have had to be excluded from every one of those.
- **Expiry is computed.** A find is shown until 14 days after the day it was seen (`ExpiresOn`); there is no job. The seen-on date may be at most 7 days old when posted, so a find lives 7 to 14 days.
- **Posting follows the selling rule.** `POST /api/finds` sits behind the existing `CanSell` policy (verified member, and an administrator in Phases 1 and 2, `L2-091`), so a find appears exactly where selling does. 20 finds per member per UTC day, counted in the database ([ADR platform/0006](../platform/0006-daily-publish-limit-counted-in-database.md)).
- **Only administrators offer grabs.** A grab fee from a member without the Administrator role is refused with `422 GRAB_OFFER_NOT_ALLOWED`. This keeps grabs to TuTuMuch itself, so no member is paid for a grab (see ADR orders/0009).
- **No photos in this release.** The card (`tl-find-card`) and the page show the `map-pin` icon of `tl-icon` in a sunken well, so no new icons are needed. Photos would need the upload pipeline of `L2-017` and its clean-up for a second owner type; they follow later, as listings once shipped without photos ([ADR general/0004](../general/0004-publish-without-photos-until-upload-listing-photos.md)).
- **A maps link, no map.** "Open in maps" is a plain link to `https://www.google.com/maps/search/?api=1&query={store name, store address}` in a new tab. No map SDK, key, geocoding or stored coordinates; the store address is free text the poster types, and the form says it is the store's address, never their home (`L2-121`).
- **No editing.** A poster marks a find gone or withdraws it and posts again. Neither is allowed while a grab is open (`409 GRAB_IN_PROGRESS`).
- **Screens.** The list and detail pages live in `@tutumuch/catalog`, the post form in `@tutumuch/listings`, the grab screens in `@tutumuch/orders`; they talk through routes only, so no feature library imports another. HTTP lives in `@tutumuch/api` under `src/lib/finds/`.

## Consequences

- Finds cost nothing to store and need no clean-up job; old rows stay as history and drop out of every list by date.
- A closed member's finds leave the list at once and are erased with the member by cascade.
- The demo seeder adds three finds so the page is not empty locally.
- Photos, editing, reporting a find (moderation is out of scope, ADR general/0001) and finds by location are deferred.
