# Shelf finds — estimate

## Basis

The hours assume an intermediate Angular/TypeScript and ASP.NET Core/C# developer who is productive in both stacks but new to this codebase. They assume the catalog, listings, launch phases and accounts features exist as built: the `CanSell` policy, `LaunchState.canSell()`, the "Selling opens soon" state, `CategoryCatalog`, `TuTuMuchApiFactory`, the Playwright fixtures and every `tl-*` component the screens need. Finds have no photos, so no upload work is counted. The grab screens and endpoints are estimated in [grab-it-for-me](../orders/grab-it-for-me.md). Estimates cover implementation, unit, integration and acceptance tests, and review fixes. [Detailed design](../../detailed-designs/catalog/shelf-finds/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `ShelfFind` with its input rules, expiry, gone and withdraw, with unit tests | 3 |
| EF Core configuration for `ShelfFinds` and its part of the `AddShelfFindsAndGrabs` migration | 1 |
| Post, browse, get, mark gone and withdraw handlers with the daily limit, the grab-offer rule and closed posters | 4 |
| `FindsController` and API acceptance tests | 3 |
| `FindsApi` and its DTOs | 1 |
| `ShelfFindsPage` with every state, and the "Shelf finds" links in the top bar and footer | 3 |
| `ShelfFindPage` with its variants, the maps link and the two dialogs | 3 |
| `PostFindPage` with the phase guard, validation and the administrator's grab fee | 3 |
| Demo finds in `TuTuMuch.Cli` | 1 |
| Playwright acceptance tests via page objects and `page.route` fixtures | 2 |
| **Total** | **24** |

## Minimal scope

The minimal version ships posting (without the daily limit), the list without "Load more", and the detail page with the maps link and the gone and expired states; the poster withdraws through the API only. It satisfies `L2-113` in part, `L2-114` in part, `L2-115` and `L2-121`, and defers `L2-116`'s screens. Minimal scope totals **18** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Mark as gone and Withdraw dialogs | The poster cannot manage a find from the page | 2 |
| "Load more" paging | Only the newest 24 finds show | 1 |
| Daily posting limit | Only the general rate limit of `L2-051` applies | 1 |
| Demo finds | A developer posts finds by hand | 1 |
