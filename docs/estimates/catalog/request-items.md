# Request items — estimate

## Basis

The hours assume an intermediate Angular/TypeScript and ASP.NET Core/C# developer who is productive in both stacks but new to this codebase. They assume the catalog, listings, notifications and accounts features exist as built: `CatalogCards`, `IEmailOutbox`, `DispatchEmailsHandler`, `UnsubscribeTokens`, `NotificationPreferences`, the publish and relist commands, `TuTuMuchApiFactory`, the Playwright fixtures and every `tl-*` component the screens need (only the `bell` icon is added). Estimates cover implementation, unit, integration and acceptance tests, and review fixes. [Detailed design](../../detailed-designs/catalog/request-items/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `ItemRequest` and `ItemRequestMatch` domain types: field rules, expiry, renewal and the match rule, with unit tests | 6 |
| EF Core configurations, the `ItemRequestEmails` column and the `AddItemRequests` migration | 3 |
| Create, update, delete, list and get handlers with the limit, ownership 404 and match cards | 6 |
| `ItemRequestsController` and API acceptance tests | 5 |
| `ListingBecameActive` from publish and relist, `MatchItemRequestsHandler` and matching tests | 6 |
| `ItemRequestMatched` email, the `ItemRequests` category, preference, unsubscribe and dispatcher changes with tests | 5 |
| `CleanUpItemRequestsCommand`, `ItemRequestCleanupJob` and tests | 2 |
| Demo requests in `TuTuMuch.Cli` with a test | 2 |
| `ItemRequestsApi` in `@tutumuch/api` | 2 |
| `RequestFormPage` with prefill, validation, limit, not-found and sign-in redirect | 6 |
| `MyRequestsPage` with matches, expired and renew, limit line, empty, loading and error | 6 |
| `DeleteRequestDialog` and toasts | 2 |
| "Request it" on the catalog's empty states and the email settings switch and unsubscribe copy | 2 |
| Playwright acceptance tests via page objects and `page.route` fixtures | 6 |
| **Total** | **59** |

## Minimal scope

The minimal version ships the request form, My requests with edit and delete, the limit, matching on publish only and the match email with the always-on behaviour of order emails. It leaves out matching on relist, the switch and one-click unsubscribe, the clean-up job (expired requests stay until deleted), the "Request it" link (the form is reached from My requests) and the demo data. It satisfies `L2-097`, `L2-099`, `L2-101`, `L2-102`, `L2-105` and `L2-106`, and part of `L2-098` and `L2-100`; it defers `L2-103` and `L2-104`. Minimal scope totals **44** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Matching on relist | A relisted item never reaches requests saved after its first publish | 1 |
| Item request switch and one-click unsubscribe | Members cannot stop match emails without deleting requests | 5 |
| Clean-up job | Expired requests stay until the member deletes them | 2 |
| "Request it" on empty catalog states | Members find the form only through My requests | 2 |
| Demo requests in `TuTuMuch.Cli` | A local install has an empty My requests page | 2 |
