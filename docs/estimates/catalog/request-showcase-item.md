# Request a showcase item — estimate

## Basis

The hours assume an intermediate Angular/TypeScript and ASP.NET Core/C# developer who is productive in both stacks but new to this codebase. They assume the catalog, listings, orders and accounts features exist as built: the listing detail page and store, `StartCheckoutCommand` with its conditional reservation, the `Administrator` policy, the admin shell, sign in and join, `TuTuMuchApiFactory`, the Playwright fixtures and every `tl-*` component the screens need (only `tl-sold-notice` and the `heart` icon are added). Estimates cover implementation, unit, integration and acceptance tests, and review fixes. [Detailed design](../../detailed-designs/catalog/request-showcase-item/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `Listing.IsShowcase` and `SetShowcase`, `ShowcaseRequest` with its note rule, with unit tests | 3 |
| EF Core configuration, the `IsShowcase` column and the `AddShowcaseRequests` migration | 2 |
| Mark, send, read-one and admin-list handlers with ownership, the daily limit and closed members | 4 |
| `ShowcaseRequestsController`, `AdminShowcaseController` and API acceptance tests | 4 |
| Checkout refusal, the detail flag, item-request matching and My requests skipping showcase listings, with tests | 2 |
| `tl-sold-notice`, the `heart` icon and `ShowcaseApi` | 2 |
| Listing detail: showcase Buy now, the Sold notice, `request=1`, the owner's switch | 4 |
| `RequestShowcaseItemDialog` with update, invalid, failure and the daily limit; toasts | 3 |
| `safeReturnUrl`, sign in and join with the request return URL; the checkout page's showcase state | 2 |
| `ShowcaseRequestsPage` in the admin shell with every state | 2 |
| Playwright acceptance tests via page objects and `page.route` fixtures | 4 |
| **Total** | **32** |

## Minimal scope

The minimal version ships the flag set through the API only (no switch on the page), the checkout refusal, the Sold notice with "Request this item", the dialog for signed-in members and a plain admin list. A visitor is sent to sign in without coming back to the dialog, and there is no daily limit. It satisfies `L2-107`, `L2-108`, `L2-110` and `L2-111`, and part of `L2-109`. Minimal scope totals **24** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| The owner's switch on the detail page | The owner marks listings with an API call | 2 |
| Return to the dialog after sign in or join | A visitor finds the item again after signing in | 2 |
| Daily limit of new requests | Only the general rate limit of `L2-051` applies | 1 |
| Checkout page showcase state | A direct link to checkout shows the generic error | 1 |
