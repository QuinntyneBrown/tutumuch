# 0002. Seller reputation defaults until orders and reviews exist

Date: 2026-10-25

## Status

Accepted

## Context

The view-listing-detail design read the seller's rating, review count, completed sales, public location and avatar through a `MemberReputation` view and an `IListingDetailReadStore` port. None of those figures is stored yet: completed sales are recorded by `orders/complete-order`, ratings by reviews (review authoring is deferred by [ADR general/0001](../general/0001-defer-moderation-reviews-and-data-export.md)), and the public location and avatar arrive with `reviews/view-member-profile`. The design also put the `Listing.EnsureCanBePurchasedBy` guard in this feature, although the only request it refuses, starting a checkout, is built by `orders/checkout-and-pay`.

## Decision

`GetListingDetailQueryHandler` reads through `ITutuMuchDbContext`, as browse-catalog does ([ADR catalog/0001](0001-catalog-reads-through-the-db-context-port.md)), and returns `SellerSummaryDto` with the display name, 0 completed sales, no rating and 0 reviews. The page therefore shows "New seller" for every seller. No view, read-store port, location or avatar field is added. `tm-seller-summary` already renders sales and a rating when they are present. The own-listing purchase guard (HTTP 422 `CANNOT_BUY_OWN_LISTING`) is built with checkout; this feature only keeps "Buy now" away from the seller.

## Consequences

- No migration and no placeholder schema; the detail read stays in one handler.
- When complete-order and view-member-profile store the figures, they fill `SellerSummaryDto` in this handler; the API shape and the component do not change.
- `L2-026` acceptance criterion 2 stays open until checkout-and-pay.

## Update 2026-10-26

[ADR reviews/0001](../reviews/0001-public-profile-without-a-reputation-projection.md) fills `CompletedSalesCount` from Completed orders where the member is the seller. The rating stays empty while review authoring is deferred.
