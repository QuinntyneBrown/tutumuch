# Cancel an order — estimate

## Basis

The estimate assumes the `Order` aggregate, `Refund` table and `IssueRefund` outbox message exist from ship-order and checkout-and-pay. It also assumes `Listing.RemoveAfterCancellation` and the relist flow are delivered by the listings subsystem. Hours cover implementation, unit and integration tests, acceptance tests and review fixes for an intermediate Angular/TypeScript and ASP.NET Core/C# developer; they exclude requirements, design and mock work. [Detailed design](../../detailed-designs/orders/cancel-order/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `CancelOrderCommand`, validator, handler and `RefundRequester` | 6 |
| `POST /api/orders/{id}/cancellation` | 2 |
| `CancellationRequest` entity, migration, request and response handlers and endpoints | 6 |
| `OrderModerationPort` (`IOrderModerationPort`) | 4 |
| `CancelOrderDialog` with four states | 4 |
| `RequestCancellationDialog` and `RespondCancellationDialog` | 5 |
| `SaleDetailPage` cancel-requested and cancelled states; `OrderDetailPage` cancelled state | 4 |
| Unit and integration tests for `L2-035` criteria 1 to 3 and the moderation port | 6 |
| Playwright acceptance tests | 4 |
| **Total** | **41** |

## Minimal scope

Minimal scope keeps seller cancellation with a full refund, the shared handler used by the ship deadline job, and the moderation port. It drops the buyer's cancellation request and the respond dialog; a buyer contacts Tutu Much support instead. It satisfies `L2-035` criteria 1, 3 and 4 and `L2-045` criterion 4; it defers criterion 2.

Minimal scope total: **27 hours**.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Buyer cancellation request and seller response | `L2-035` criterion 2 deferred | 13 |
| Optional note to buyer | The buyer receives the reason without a personal note | 1 |
