# 0001. Catalog reads through the DbContext port

Date: 2026-10-25

## Status

Accepted

## Context

The browse-catalog design proposed an `ICatalogReadStore` port with an EF Core `CatalogReadStore`, an `IPhotoUrlBuilder` with a `CdnPhotoUrlBuilder`, and a new filtered covering index `IX_Listings_Catalog_Newest`. Every other handler in `TutuMuch.Application` reads and writes through the `ITutuMuchDbContext` port, and photo URLs are already built from `PhotoOptions.PublicBaseUrl` by upload-listing-photos. `Listings` already has `IX_Listings_Status_PublishedAt`. Tests run the API on SQLite in-memory, so either shape is testable without a real database.

## Decision

`BrowseCatalogQueryHandler` queries `ITutuMuchDbContext` directly with `AsNoTracking()`, filters with the domain expression `CatalogVisibility.IsVisible`, and builds `srcset` strings from `PhotoOptions.PublicBaseUrl` and the stored content-addressed keys. No read-store port, URL builder interface, new index or migration is added. The category filter lives on the same endpoint so the top-bar category pages work now; search-and-filter-catalog extends the same handler.

## Consequences

- Fewer types and no migration; the handler matches the rest of the application layer.
- Moving photos behind a CDN is a configuration change of `Photos:PublicBaseUrl`.
- If catalog reads need tuning (a covering filtered index, a raw SQL projection), it is done inside the handler or by introducing the port then, recorded in a new ADR.
