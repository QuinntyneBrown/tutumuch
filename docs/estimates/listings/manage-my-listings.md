# Manage my listings — estimate

## Basis

The estimate assumes an intermediate Angular/TypeScript and ASP.NET Core/C# developer who is productive in both stacks but new to this codebase. It assumes the `Listings` and photo tables, the account shell, `ToastService`, the outbox dispatcher and the email adapter already exist. `PayoutBannerComponent` and the draft reminder email template are estimated in the seller-payouts and notifications features. The open pagination behaviour (`<TO SUPPLY>`) is assumed to be a "Load more" button. Hours cover implementation, unit and acceptance tests, and review fixes. [Detailed design](../../detailed-designs/listings/manage-my-listings/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `GetMyListingsQueryHandler`: projection with cover and sale order, keyset cursor, status counts | 5 |
| `MyListingsController` `GET /api/me/listings` with query validation | 2 |
| Seller indexes and filtered draft index on `Listings`, migration | 1 |
| `MyListingsPage` with loading, empty, error and filtered states, "Load more" | 5 |
| `ListingStatusFilterComponent` chips with counts and URL query state | 2 |
| `MyListingRowComponent` with placeholder cover, untitled draft text, price and date formatting, status-based links | 3 |
| Draft clean-up info toast and payout banner slot | 1 |
| `DraftCleanupJob`: reminder pass, deletion pass, reminder reset on save, options | 5 |
| `DraftExpiryReminderEmail` outbox message contract | 1 |
| API and job acceptance tests for `L2-021` and the clean-up criterion of `L2-019` (with a fake clock) | 5 |
| Playwright acceptance tests for the page via page objects | 3 |
| **Total** | **33** |

## Minimal scope

The minimal version shows every listing of the seller, newest first, with cover, title, price, status and date, a status filter and the empty state with "Sell an item". It returns all rows in one response without pagination, and it includes `DraftCleanupJob` with both passes. It leaves out per-status counts on the chips, the "Load more" control, the info toast and Playwright tests. It still satisfies `L2-021` and the clean-up part of `L2-019`. Minimal scope totals **22** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Keyset pagination and "Load more"; return all rows | Large sellers load every listing at once | 3 |
| Per-status counts on the chips | Chips show no numbers and the subtitle shows no totals | 2 |
| Draft clean-up info toast | Sellers learn of the 90-day rule only from the reminder email | 1 |
| Playwright tests; rely on API acceptance tests | UI regressions surface later | 3 |
