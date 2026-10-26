# Listings subsystem

The listings subsystem covers how a seller creates, photographs, prices, publishes, edits, withdraws and reviews listings, and how `Tutu Much API` enforces the size rule that only girls' clothing is accepted (`L1-003`, `L1-004`). Each feature below is a vertical slice from the Angular pages through `Tutu Much API` and `Tutu Much Worker` to the `Listings`, `ListingPhotos` and `ListingPhotoVersions` tables, built on the [architecture baseline](../README.md). Pages and dialogs live in the `@tutumuch/listings` library (`src/frontend/projects/listings/`) and call `Tutu Much API` through `@tutumuch/api`.

| Feature | Design | Estimate | L2 requirements |
|---------|--------|----------|-----------------|
| Create a listing | [create-listing](create-listing/README.md) | [estimate](../../estimates/listings.md) | `L2-003`, `L2-011`, `L2-012`, `L2-013`, `L2-014`, `L2-015`, `L2-016`, `L2-018`, `L2-019`, `L2-031`, `L2-051` |
| Upload listing photos | [upload-listing-photos](upload-listing-photos/README.md) | [estimate](../../estimates/listings.md) | `L2-017`, `L2-059` |
| Edit and withdraw a listing | [edit-and-withdraw-listing](edit-and-withdraw-listing/README.md) | [estimate](../../estimates/listings.md) | `L2-020`, `L2-013`, `L2-049`, `L2-035` |
| Manage my listings | [manage-my-listings](manage-my-listings/README.md) | [estimate](../../estimates/listings.md) | `L2-021`, `L2-019` |
