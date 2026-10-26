# Search and filter the catalog — estimate

## Basis

The hours assume an intermediate Angular/TypeScript and ASP.NET Core/C# developer who is productive in both stacks but new to this codebase. They assume browse-catalog is built first, so `CatalogController`, `SearchCatalogQuery`, `CatalogReadStore`, `CatalogStore`, `CatalogPageComponent` and the listing grid exist; `GET /api/categories` and `ICategoryCatalog` exist from the listings subsystem; and the shell top bar exists from the platform subsystem. The SQL Server edition and hosting are `<TO SUPPLY>`; the hours assume full-text search is available on the chosen hosting. Whether an explicit price sort overrides the keyword match tier is `<TO SUPPLY>`; the hours assume the tier stays primary. Estimates cover implementation, unit, integration and acceptance tests, and review fixes. [Detailed design](../../detailed-designs/catalog/search-and-filter-catalog/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `CatalogCriteria`, `SearchTerm`, `PriceRange` and `CatalogSort` domain types with unit tests | 4 |
| `SearchCatalogQueryValidator` criteria rules and `CATALOG_PRICE_RANGE_INVALID` / `CATALOG_FILTER_INVALID` problem details | 3 |
| Filter composition in `CatalogReadStore` (AND across filters, `IN` within) and sort orders | 4 |
| Full-text migration: `SearchHeadline` and `SearchBody` computed columns, full-text catalog and index with `STOPLIST = OFF` | 4 |
| Keyword condition building, match-tier ordering and stable tie-breakers | 5 |
| Filter indexes `IX_Listings_Catalog_Price`, `IX_Listings_Catalog_Category`, `IX_Listings_Catalog_Brand` | 2 |
| `GET /api/catalog/listings/count` with `CountCatalogListingsQuery` and handler | 2 |
| `GET /api/catalog/brands` with handler and Redis cache key `catalog:brands:v1` | 3 |
| API integration tests for `L2-023` criteria 1 to 5 and `L2-024` criteria 1 to 6 | 6 |
| `CatalogQuery` type and `CatalogQueryParamsCodec` with round-trip and canonicalisation tests | 4 |
| `CatalogPageComponent` URL wiring, search, category and "Shop" headers, subcategory chips, no-results state | 5 |
| `CatalogSearchFormComponent` in the top bar, including the phone search button | 3 |
| `CatalogToolbarComponent` sort select and `ActiveFilterChipsComponent` | 4 |
| `CatalogFiltersFormComponent` with price-range validation and accessible errors | 5 |
| `FiltersSidebarComponent` with immediate apply and debounced price | 3 |
| `FiltersPanelDialogComponent` with full-screen and side-panel layouts, live count, busy and invalid states, focus handling | 7 |
| `CatalogFacetsService` for categories and brands | 2 |
| Playwright acceptance tests via page objects (search, filters, sort, shared URL, panel at 375 and 800 px, sidebar at 1280 px) | 8 |
| **Total** | **74** |

## Minimal scope

The minimal version ships keyword search with case-insensitive `LIKE` matching on title, brand and description and a title-or-brand tier, 100-character truncation, the five filters with AND logic, the three sorts, the URL contract and codec, the no-results state, the sidebar from 992 px and a full-screen filter panel with "Clear" and "Apply" under 992 px. It leaves out the full-text index, the live result count, the count endpoint, the brands endpoint (the brand list comes from a fixed client-side list `<TO SUPPLY>`), the side-panel layout for 768 to 991 px and the phone search button. It satisfies `L2-023`, `L2-024` and the filter clauses of `L2-062` except the medium-viewport side panel, which is deferred. Keyword search is slower on a large catalog without full-text search. Minimal scope totals **50** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Full-text index; use `LIKE` with a tier `CASE` expression | Slower keyword search at scale | 4 |
| Live count in the filter panel and the count endpoint | Apply reads "Show items" with no count | 4 |
| Brands endpoint and cache; fixed brand list | Brand filter does not reflect what is in stock | 3 |
| Side-panel layout for 768 to 991 px; use full screen | `L2-062` medium-viewport clause unmet | 2 |
| Debounced price apply in the sidebar; add an "Apply price" button | One extra tap to apply a price range | 1 |
| Phone search icon button; keep the search field in the menu only | Search one tap further away on phones | 1 |
