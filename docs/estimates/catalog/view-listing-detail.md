# View a listing's detail — estimate

## Basis

The hours assume an intermediate Angular/TypeScript and ASP.NET Core/C# developer who is productive in both stacks but new to this codebase. They assume browse-catalog is built first (`CatalogController`, `IPhotoUrlBuilder`, `ResponsivePhotoComponent`, `CatalogVisibility`, `CatalogApiService`); the sign-in page honours `returnUrl` (accounts); `WithdrawListingDialogComponent` and the edit route exist (listings); `ReportListingDialogComponent` exists (moderation); and the checkout-start handler exists (orders) and only needs to call the new guard. Where the seller reputation figures are stored is `<TO SUPPLY>`; the hours assume one view or three columns to read. Returning HTTP 404 on the SPA document for an unavailable listing is `<TO SUPPLY>` and is not included. Estimates cover implementation, unit, integration and acceptance tests, and review fixes. [Detailed design](../../detailed-designs/catalog/view-listing-detail/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `GET /api/catalog/listings/{reference}` with optional viewer resolution and `LISTING_NOT_FOUND` | 3 |
| `GetListingDetailQuery`, handler, viewer relation, total and display status | 4 |
| `ListingDetailVisibility` specification with unit tests | 1 |
| `IListingDetailReadStore` and `ListingDetailReadStore` single-query projection with seller reputation | 5 |
| `Listing.EnsureCanBePurchasedBy` guard, `CANNOT_BUY_OWN_LISTING` to 422 mapping, and the call from the checkout-start handler | 3 |
| API integration tests for `L2-025` criteria 1 to 3 and 5, `L2-026` criterion 2, `L2-028` criterion 1 display | 5 |
| `ListingDetailApiService` and `ListingDetailStore` with `primaryAction` | 3 |
| `ListingDetailPageComponent` with loading, error and unavailable states, breadcrumb and title | 4 |
| `ListingGalleryComponent` with thumbnails, keyboard support and scroll-snap swipe | 7 |
| `ListingInfoComponent` with price line, flaws, facts, Reserved alert and Sold stamp | 4 |
| `SellerSummaryComponent` with rating or "New seller" | 2 |
| `BuyBarComponent` with the four action modes and sticky bottom bar below 992 px | 4 |
| Report entry point and similar items for Sold listings | 2 |
| Responsive two-column layout and 44 px touch targets | 2 |
| Playwright acceptance tests via page objects (active, guest return, own, new seller, reserved, sold, unavailable, gallery, 375 and 1280 px) | 9 |
| **Total** | **58** |

## Minimal scope

The minimal version ships the detail endpoint with visibility and viewer relation; the page with every field of `L2-025`, loading, error and unavailable states; a gallery with thumbnail selection; "New seller"; the Reserved and Sold labels without "Buy now"; "Buy now" with the sign-in return for anonymous visitors; "Edit listing" and "Withdraw" for the seller; and the `CANNOT_BUY_OWN_LISTING` guard. It leaves out swipe gestures (the phone gallery uses thumbnails only), the sticky buy bar, similar items for Sold listings, the Reserved explanatory alert and the report link. It satisfies `L2-025`, `L2-026` and the display side of `L2-028`; the swipe and fixed-bar clauses of `L2-063` are deferred. Minimal scope totals **42** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Swipe gesture in the gallery; keep thumbnails | `L2-063` swipe clause unmet on phones | 3 |
| Sticky buy bar below 992 px | "Buy now" scrolls out of view on phones; `L2-063` clause unmet | 2 |
| Similar items on Sold listings | A Sold page offers no next step beyond navigation | 1 |
| Reserved explanatory alert; keep the stamp | Visitors are not told the item may return in 10 minutes | 1 |
| Keyboard arrow navigation between thumbnails; keep Tab | Slower keyboard browsing of photos | 1 |
