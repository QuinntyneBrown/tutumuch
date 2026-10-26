# Edit and withdraw a listing — estimate

## Basis

The estimate assumes an intermediate Angular/TypeScript and ASP.NET Core/C# developer who is productive in both stacks but new to this codebase. It assumes create-listing and upload-listing-photos are done, so `ListingFormComponent`, the validators, `ListingPhotoAttacher` and `ListingsApiService` exist and are reused. It also assumes the cancel-order feature in the orders subsystem calls `Listing.RemoveAfterCancellation` (`<TO SUPPLY>`). Hours cover implementation, unit and acceptance tests, and review fixes. [Detailed design](../../detailed-designs/listings/edit-and-withdraw-listing/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `Listing` editability guards, `Withdraw`, `RemoveAfterCancellation`, `Relist`, `RemovalReason` column and migration | 4 |
| `GetMyListingQuery` handler and `GET /api/me/listings/{reference}` with ETag | 3 |
| `UpdateListingCommandHandler` active path: publish rules, size reset, size re-confirmation, `If-Match`, concurrency mapping to 409 and 412 | 6 |
| `WithdrawListingCommandHandler` and `POST /api/listings/{reference}/withdraw` | 2 |
| `RelistListingCommandHandler` and `POST /api/listings/{reference}/relist` | 2 |
| `UpdateListingRequest` binding that ignores protected fields | 1 |
| `EditListingPage` with loading, error, invalid, submitting, success and locked states | 6 |
| Undo toast for price changes | 2 |
| `WithdrawListingDialog` with busy and failed states | 3 |
| `RelistDialog` with busy state and "Edit first" path | 2 |
| API acceptance tests for `L2-020`, `L2-013` update, `L2-049` mass assignment, `L2-010` and the edit-versus-checkout race | 6 |
| Playwright acceptance tests for edit, withdraw and relist via page objects | 4 |
| **Total** | **41** |

## Minimal scope

The minimal version lets the seller edit and withdraw an `Active` listing and refuses both for `Reserved` and `Sold` listings. It includes the domain guards, the update and withdraw handlers with the `RowVersion` check, `GET /api/me/listings/{reference}`, `EditListingPage` with the locked state, `WithdrawListingDialog`, and API acceptance tests. It leaves out the `If-Match` precondition (last write wins between two tabs), relisting, the undo toast and Playwright tests. It still satisfies `L2-020`, `L2-013` and `L2-049`, and defers the relist part of `L2-035`. Minimal scope totals **27** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Undo toast for price changes | The seller re-edits to revert a price | 2 |
| `If-Match` precondition and 412 handling; keep the `RowVersion` check | Two tabs of the same seller can overwrite each other | 2 |
| Relist endpoint and `RelistDialog` | A cancelled sale's listing cannot return to the catalog (`L2-035`) | 4 |
| Playwright tests; rely on API acceptance tests | UI regressions surface later | 4 |
