# Create a listing — estimate

## Basis

The estimate assumes an intermediate Angular/TypeScript and ASP.NET Core/C# developer who is productive in both stacks but new to this codebase. It assumes the baseline is already in place: the `core` interceptors, `AuthState`, shared design-system components (`ErrorSummaryComponent`, toasts, segmented control), problem-details middleware, MediatR and FluentValidation pipeline, `TuTuMuchDbContext`, Redis rate limiting and `ISellerPayoutAccountReader` (provided by the seller-payouts feature). `PhotoUploaderComponent` and `ListingPhotoAttacher` are estimated in upload-listing-photos. The `<TO SUPPLY>` items (fee percentage, suggested brands, flaws maximum length, rounding rule, reading of the `L2-051` limit) do not change the hours. Hours cover implementation, unit and acceptance tests, and review fixes. [Detailed design](../../detailed-designs/listings/create-listing/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `CategoryCatalog`, `CategoryDefinition`, `ItemCondition` and unit tests for every category, subcategory and size | 3 |
| `Listing` aggregate: fields, `CreateDraft`, `UpdateDetails` for drafts, `Publish`, server-set size, trimming | 6 |
| `Listings` table: EF configuration, `Reference` sequence, check constraints, indexes, migration | 4 |
| `SaveListingDraftValidator` and publish rules with stable error codes and per-field problem details | 6 |
| `CreateListingDraftCommandHandler`, `UpdateListingCommandHandler` draft path and `PublishListingCommandHandler` with payout check | 6 |
| `ListingsController` create, update and publish actions; `CanSell` policy wiring | 3 |
| `CategoriesController` with `GET /api/categories` and `GET /api/selling/options` | 2 |
| `listing-publish` rate-limit policy (20 per member per UTC day, `Retry-After`) | 3 |
| `SellingOptionsService` and `ListingsApiService` with problem-details mapping | 3 |
| `SellPage` with states unverified, suspended, error, draft-saved, success and limited | 6 |
| `ListingFormComponent` typed reactive form, client validation, counters, conditional flaws field, size confirmation label | 10 |
| `FixedSizeComponent` and `EarningsEstimateComponent` | 3 |
| Error summary, focus management and live-region announcements on the form | 3 |
| API acceptance tests for `L2-011`, `L2-013`, `L2-015`, `L2-016`, `L2-018`, `L2-019`, `L2-051` | 8 |
| Playwright acceptance tests for the sell page via page objects (`L2-003`, `L2-012`, `L2-031`) | 6 |
| **Total** | **72** |

## Minimal scope

The minimal version keeps the size rule and the publish path intact. It includes `CategoryCatalog`, the `Listing` aggregate with server-set size, the `Listings` table, the publish rules, `POST /api/listings` that creates and publishes in one call, `GET /api/categories`, a single-state sell form with fixed size, condition, flaws, price, shipping and size confirmation, and API acceptance tests. It leaves out separate draft saving, the earnings estimate, the payout prompt, the rate limit, the dedicated unverified, suspended and limited states, and Playwright tests. It still satisfies `L2-011`, `L2-012`, `L2-013`, `L2-015`, `L2-016` and `L2-018`. It defers the draft part of `L2-019`, the fee display of `L2-031` and the listing limit of `L2-051`. Minimal scope totals **42** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| `EarningsEstimateComponent` | Fee and estimated earnings are not shown before publishing (`L2-031`) | 2 |
| Payout onboarding check after publish | No "Set up payouts" prompt on the success state | 2 |
| `listing-publish` rate-limit policy | No per-member listing limit beyond the general request limits (`L2-051`) | 3 |
| Dedicated unverified, suspended and limited page states; show a generic error instead | Members see a generic message instead of a tailored one | 3 |
| `GET /api/selling/options`; hard-code conditions and brands in the client | Fee and brand list change only with a frontend release | 1 |
| Playwright tests; rely on API acceptance tests | UI regressions surface later | 6 |

## Status

Complete: 90%. Everything in the design is built and tested except the photo rules and Photos section, which arrive with upload-listing-photos ([ADR general-0004](../../adr/general/0004-publish-without-photos-until-upload-listing-photos.md)), and the toast after a failed draft save.
