# Complete an order — estimate

## Basis

The estimate assumes the `Order` aggregate and state machine from ship-order, the payout handler from seller-payouts, and the reviews subsystem's handling of `OrderCompleted`. Hours cover implementation, unit and integration tests, acceptance tests and review fixes for an intermediate Angular/TypeScript and ASP.NET Core/C# developer; they exclude requirements, design and mock work. [Detailed design](../../detailed-designs/orders/complete-order/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `ConfirmReceiptCommand`, handler and `POST /api/orders/{id}/receipt` | 4 |
| Open-problem guard in `Order.Complete` | 1 |
| `AutoCompleteOrderCommandHandler` and `OrderAutoCompletionJob` | 4 |
| `ConfirmReceiptDialog` with three states and `OrdersApiService.confirmReceipt` | 3 |
| `OrderDetailPage` delivered and completed states | 3 |
| Unit and integration tests for `L2-034` criteria 1 to 3 | 4 |
| Playwright acceptance tests | 3 |
| **Total** | **22** |

## Minimal scope

Minimal scope is close to the full design: receipt confirmation for Delivered orders and the auto-completion job. It drops confirmation directly from Shipped, so a buyer waits for the carrier's delivery event. It satisfies `L2-034` criteria 1 to 3 and defers the Shipped case of the requirement statement.

Minimal scope total: **19 hours**.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Confirming a Shipped order | Buyer can confirm only after delivery is reported | 2 |
| Auto-completion date line on the delivered state | Buyer does not see when the order completes | 1 |
