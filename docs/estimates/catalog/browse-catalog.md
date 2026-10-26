# Browse the catalog — estimate

## Basis

The hours assume an intermediate Angular/TypeScript and ASP.NET Core/C# developer who is productive in both stacks but new to this codebase. They assume the `Listing` entity, `Listings`, `ListingPhotos` and `ListingPhotoVersions` tables, the problem-details middleware, the rate-limit middleware, the `core` error interceptor and the design-system listing card styles already exist from other designs. This feature introduces `CatalogController`, `ICatalogReadStore` and `ResponsivePhotoComponent`, which search-and-filter-catalog and view-listing-detail then reuse, so their cost sits here. The CDN base URL (`Photos:CdnBaseUrl`) is `<TO SUPPLY>`; the hours assume it is a single configuration value. Estimates cover implementation, unit, integration and acceptance tests, and review fixes. [Detailed design](../../detailed-designs/catalog/browse-catalog/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `CatalogController` with `GET /api/catalog/listings`, `PublicRead` policy and problem-details mapping | 3 |
| `SearchCatalogQuery`, validator, handler with page-size cap and `CatalogOptions` | 4 |
| `CatalogVisibility` specification including the expired-reservation display rule, with unit tests | 2 |
| `ICatalogReadStore` and `CatalogReadStore` count and page queries with cover-photo projection | 6 |
| `IX_Listings_Catalog_Newest` filtered covering index migration and query-plan check | 2 |
| `IPhotoUrlBuilder` and `CdnPhotoUrlBuilder` with DTO mapping | 2 |
| API integration tests for visibility, ordering, paging and the 48 cap (`L2-022` criteria 1, 2, 3, 5) | 5 |
| `CatalogApiService`, DTO types and offline error mapping | 2 |
| `CatalogStore` with load, load more, de-duplication and view states | 5 |
| `CatalogSnapshotStore` and offline banner state | 3 |
| `CatalogPageComponent` with hero, loading, empty, error and offline states | 6 |
| `ListingGridComponent` with 2/3/4-column breakpoints and `ListingCardComponent` with Reserved stamp | 5 |
| `ResponsivePhotoComponent` with `picture`, WebP and JPEG `srcset`, `sizes`, dimensions, lazy and priority loading | 5 |
| `LoadMoreComponent` with busy state, polite announcement and failure toast | 3 |
| Playwright acceptance tests via page objects (paging, empty, Reserved, 375 px thumbnail request, lazy attributes, column counts) | 8 |
| Visual snapshots at 375, 576, 768, 992 and 1280 px | 3 |
| **Total** | **64** |

## Minimal scope

The minimal version ships the API endpoint with visibility, newest-first order, 24-item pages and the 48 cap; the catalog page with loading, loaded, empty and error states; listing cards with the Reserved stamp; "Load more"; and the responsive grid. Photos use a plain `<img>` with the 400 px JPEG, explicit width and height, and `loading="lazy"`. It leaves out the offline snapshot, the WebP `<picture>` sources and the full `srcset`, the expired-reservation display rule, the dedicated covering index and the visual snapshots. It satisfies `L2-022` and `L2-061` acceptance criterion 2, and partially satisfies `L2-059` (acceptance criteria 1 and 2 only on the card size it serves); WebP delivery and full `srcset` under `L2-059` are deferred. Minimal scope totals **42** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Offline snapshot and offline banner state | The catalog shows the error state instead of the last visit's items when offline | 3 |
| WebP sources in `ResponsivePhotoComponent`; keep JPEG `srcset` | Larger photo downloads; `L2-059` WebP clause unmet | 2 |
| Expired-reservation display rule in `CatalogVisibility` | Cards may show "Reserved" for up to one worker interval after expiry | 1 |
| Covering index `IX_Listings_Catalog_Newest` | Read latency at 100,000 listings depends on the base indexes | 2 |
| Visual snapshots at five breakpoints | Layout regressions caught only by manual review | 3 |
| Load-more failure toast; keep silent retry | No feedback when a next page fails | 1 |
