# 0003. Keyword search by case-insensitive match, not a full-text index

Date: 2026-10-25

## Status

Accepted

## Context

The search-and-filter-catalog design proposed SQL Server full-text search: two persisted computed columns, a full-text index with `CHANGE_TRACKING AUTO`, `EF.Functions.Contains` prefix conditions, an `ICatalogReadStore` port, `SearchCatalogQuery`, a `GetCatalogBrandsQuery` cached in `IDistributedCache`, three new filtered indexes and a migration. ADR [catalog/0001](0001-catalog-reads-through-the-db-context-port.md) already moved catalog reads onto `ITutuMuchDbContext` with no read-store port. Backend tests run on SQLite in-memory, where `EF.Functions.Contains` and full-text indexes do not exist, so the full-text path could not be tested at all under the testing rules. The design also left open whether full-text change tracking makes a new listing searchable within the 5 seconds of `L2-019`.

## Decision

- `BrowseCatalogQuery` takes the raw URL values as `CatalogFilters`; `CatalogCriteria.From` validates them and `CatalogCriteria.Apply` and `Order` compose one `IQueryable<Listing>` over `ITutuMuchDbContext`.
- A keyword is collapsed, truncated to 100 characters, lower-cased and split into words. Every word must appear (`LOWER(column) LIKE %word%`) in the title, brand or description. Listings where every word is in the title or brand come first (match tier 0), then the rest; the chosen sort orders listings inside each tier, and `Id DESC` is the last key. An explicit price sort therefore does not override the match tier.
- The API takes the same parameter names and slugs as the page URL (`q`, `category`, `subcategory`, `brand`, `condition`, `min`, `max`, `sort`), so the frontend sends its canonical URL parameters unchanged. Category is matched ignoring case, so the existing `category=Shoes` keeps working.
- `GET /api/catalog/brands` groups visible listings by brand on every request; it is not cached.
- No full-text index, computed column, new index or migration.

## Consequences

- Search is testable on SQLite in-memory and works the same on SQL Server; a published listing is searchable immediately (`L2-019`).
- `LIKE '%word%'` scans the visible listings. If search latency becomes a problem, a full-text index (or a covering index plus a raw SQL projection) is introduced inside the handler, recorded in a new ADR, as ADR 0001 allows.
- Words match inside other words ("boot" matches "boots"), which is close to the prefix matching the design asked for.
- The brands facet costs one grouped query per catalog load; caching it is a later change if needed.
