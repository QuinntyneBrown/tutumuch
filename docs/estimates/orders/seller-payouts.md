# Seller payouts — estimate

## Basis

The estimate assumes checkout-and-pay creates the `Payout` row and that the payment provider (`<TO SUPPLY>`) supports connected accounts, hosted onboarding links and transfers with idempotency keys. The listing form's estimated-earnings line belongs to the listings estimate; this estimate covers only `GET /api/payouts/fee-schedule`. Hours cover implementation, unit and integration tests, acceptance tests and review fixes for an intermediate Angular/TypeScript and ASP.NET Core/C# developer; they exclude requirements, design and mock work. [Detailed design](../../detailed-designs/orders/seller-payouts/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `Payout` aggregate, `SellerPayoutAccount`, `FeeCalculator` and migrations | 6 |
| `GetPayoutSummaryQueryHandler` and `GET /api/payouts` | 4 |
| `CreateOnboardingLinkCommandHandler` and provider connected-account calls | 5 |
| Webhook handling for `account.updated`, `payout.paid`, `payout.failed` | 5 |
| `OrderCompletedPayoutHandler` and cancel-on-refund handler | 3 |
| `PayoutReleaseJob` with provider transfers | 6 |
| `GET /api/payouts/fee-schedule` | 1 |
| `PayoutsPage` with five states | 6 |
| `PayoutBannerComponent` with four variants | 3 |
| `SalePayoutPanelComponent` on the sale page | 2 |
| Unit tests for fee rounding and release rules (`L2-031` criteria 2 and 3) | 3 |
| Integration and Playwright acceptance tests | 6 |
| **Total** | **50** |

## Minimal scope

Minimal scope keeps onboarding, the fee calculation, the hold until Completed and the release job. It drops the payout banner and the payout history list. The payouts page shows only onboarding status and the two totals. It satisfies `L2-031` criteria 1 to 4 (the fee schedule endpoint stays); it loses the bounced-payout warning.

Minimal scope total: **39 hours**.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| `PayoutBannerComponent` | No banner on My listings; the payouts page still shows status | 3 |
| `payout.failed` handling and danger banner | A bounced payout is visible only in the provider dashboard | 3 |
| Payout history list | Sellers see totals but not per-order payouts | 2 |
