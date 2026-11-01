# Listings subsystem

The listings subsystem covers how a seller creates, photographs, prices, publishes, edits, withdraws and reviews listings, and how `TuTuMuch API` enforces the audience rule that only items for girls are accepted (`L1-003`, `L1-004`), and how a seller offers an item by auction (`L1-025`). Each feature below is a vertical slice from the Angular pages through `TuTuMuch API` and `TuTuMuch Worker` to the `Listings`, `Bids`, `ListingPhotos` and `ListingPhotoVersions` tables, built on the [architecture baseline](../README.md). Pages and dialogs live in the `@tutumuch/listings` library (`src/frontend/projects/listings/`) and call `TuTuMuch API` through `@tutumuch/api`.

| Feature | Design | Estimate | L2 requirements |
|---------|--------|----------|-----------------|
| Create a listing | [create-listing](create-listing/README.md) | [estimate](../../estimates/listings.md) | `L2-003`, `L2-011`, `L2-012`, `L2-013`, `L2-015`, `L2-016`, `L2-018`, `L2-019`, `L2-031`, `L2-051` |
| Upload listing photos | [upload-listing-photos](upload-listing-photos/README.md) | [estimate](../../estimates/listings.md) | `L2-017`, `L2-059` |
| Edit and withdraw a listing | [edit-and-withdraw-listing](edit-and-withdraw-listing/README.md) | [estimate](../../estimates/listings.md) | `L2-020`, `L2-013`, `L2-049`, `L2-035` |
| Manage my listings | [manage-my-listings](manage-my-listings/README.md) | [estimate](../../estimates/listings.md) | `L2-021`, `L2-019` |
| Auction listings | [auction-listings](auction-listings/README.md) | [estimate](../../estimates/listings/auction-listings.md) | `L2-113`, `L2-114`, `L2-115`, `L2-116`, `L2-117`, `L2-118`, `L2-119`, `L2-120` |
