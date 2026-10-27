# Catalog subsystem

The catalog subsystem is the public, read-only shop window of TuTuMuch. It lets anyone, signed in or not, browse the Active and Reserved listings, narrow them by keyword, category, subcategory, brand, condition and price, and open a single listing with everything needed to decide whether to buy it (`L1-005`, `L1-006`). Apart from item requests (`L1-023`) and showcase requests (`L1-024`), which members write and which are matched against listings as they become Active, the subsystem owns no write model: it reads the `Listing` aggregate that the [listings](../listings/) subsystem writes, the reputation figures that the [reviews](../reviews/) and [orders](../orders/) subsystems maintain, and the photo versions that `upload-listing-photos` produces. All three features share one Angular feature area (`src/frontend/src/app/catalog/`), one controller (`CatalogController` in `TuTuMuch.Api`) and one read port (`ICatalogReadStore` in `TuTuMuch.Application`).

| Feature | Summary | L2 requirements |
|---------|---------|-----------------|
| [browse-catalog](browse-catalog/README.md) | Paged grid of listing cards, newest first, with responsive images and 2/3/4-column layout | `L2-022`, `L2-059`, `L2-061` |
| [search-and-filter-catalog](search-and-filter-catalog/README.md) | Keyword search, filters, sort, URL-held result sets, filter sidebar and filter panel | `L2-023`, `L2-024`, `L2-062` |
| [view-listing-detail](view-listing-detail/README.md) | Listing detail page with gallery, price breakdown, seller reputation and buyer restrictions | `L2-025`, `L2-026`, `L2-063`, `L2-028` |
| [request-items](request-items/README.md) | Item requests: the request form, My requests, matching a listing when it becomes Active, the match email and its switch, "Request it" on an empty search | `L2-097` to `L2-106` |
| [request-showcase-item](request-showcase-item/README.md) | Showcase listings: the owner's switch, Buy now's Sold notice, "Request this item" with sign-in and back, and the admin list of showcase requests | `L2-107` to `L2-111` |
