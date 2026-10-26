# Order history — estimate

## Basis

The estimate assumes the `Orders` table and indexes from ship-order and the `AesGcmFieldEncryptor` from the privacy subsystem. The detail pages' status-specific blocks are estimated in the feature that owns each action; this estimate covers the page shells, lists and projections. Hours cover implementation, unit and integration tests, acceptance tests and review fixes for an intermediate Angular/TypeScript and ASP.NET Core/C# developer; they exclude requirements, design and mock work. [Detailed design](../../detailed-designs/orders/order-history/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `ListOrdersQueryHandler` with keyset paging and counts | 4 |
| `GET /api/purchases`, `GET /api/sales` and detail endpoints | 3 |
| `GetPurchaseDetailQueryHandler` and `GetSaleDetailQueryHandler` with `OrderActionsDto` | 6 |
| `ShippingAddressVisibility` policy | 2 |
| `PurchasesPage` and `SalesPage` with loading, empty and error states | 6 |
| `OrderDetailPage` and `SaleDetailPage` shells, `OrderSummaryPanelComponent`, `OrderStatusBadgeComponent` | 6 |
| Unit and integration tests for `L2-037` criteria 1 to 4 and `L2-010` 404 rule | 5 |
| Playwright acceptance tests | 3 |
| **Total** | **35** |

## Minimal scope

Minimal scope keeps both lists without paging (all orders in one response) and the detail pages with the address rule. It drops the summary counts in the page subtitles. It satisfies `L2-037` criteria 1 to 4.

Minimal scope total: **33 hours**.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Keyset paging | Long histories load in one response | 2 |
| Subtitle counts | No "1 to ship" summary | 1 |
