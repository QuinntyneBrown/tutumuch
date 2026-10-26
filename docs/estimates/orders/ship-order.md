# Ship an order — estimate

## Basis

The estimate assumes the platform baseline already provides `TuTuMuchDbContext`, the outbox dispatcher, `IEmailOutbox` and the problem-details middleware. The `Order` aggregate and state machine are built here and reused by the other six orders features, so their cost sits in this estimate only. The carrier tracking service is `<TO SUPPLY>`; the hours assume one provider with a documented REST API and webhook. The US holiday list is assumed to be a static configuration list. Hours cover implementation, unit and integration tests, acceptance tests and review fixes for an intermediate Angular/TypeScript and ASP.NET Core/C# developer; they exclude requirements, design and mock work. [Detailed design](../../detailed-designs/orders/ship-order/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `Order` aggregate, `OrderStatusChange`, `OrderStateMachine` transition table and domain events | 10 |
| EF Core mapping and migration for `Orders` and `OrderStatusChanges` with indexes, sequence and row version | 5 |
| `UsBusinessCalendar` (`IBusinessCalendar`) with holiday configuration | 3 |
| `AddTrackingCommand`, validator with per-carrier patterns, handler and `POST /api/orders/{id}/shipment` | 6 |
| `ICarrierTrackingService` adapter, `CarrierWebhookController` with signature check | 8 |
| `MarkDeliveredCommandHandler` and `DeliveryPollingJob` | 4 |
| `ShipByReminderJob` and `ShipDeadlineJob` | 5 |
| `AddTrackingDialog` with four states and `OrdersApiService.addTracking` | 5 |
| `SaleDetailPage` Paid and Shipped states, `OrderTimelineComponent`, `TrackingLinkComponent` | 6 |
| Unit tests for every allowed and refused transition (`L2-032`) | 5 |
| Integration tests for jobs and endpoints, Playwright acceptance tests via page objects | 8 |
| **Total** | **65** |

## Minimal scope

Minimal scope keeps the state machine, add tracking, the ship deadline job and manual delivery confirmation by the buyer (from complete-order). It drops the carrier tracking integration, so `Shipped → Delivered` happens only when the buyer confirms receipt, and it drops the 3-day reminder. It satisfies `L2-032` in full and `L2-033` criteria 1, 2 and 5; it defers criteria 3 (reminder) and 4 (carrier delivery).

Minimal scope total: **48 hours**.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Carrier tracking webhook and polling | Delivery comes only from buyer confirmation; `L2-033` criterion 4 deferred | 12 |
| Ship-by reminder job | No 3-business-day reminder; `L2-033` criterion 3 deferred | 2 |
| Holiday calendar; weekdays only | Ship-by dates ignore US holidays | 2 |
| Per-carrier tracking patterns; non-empty check only | Malformed numbers are accepted when non-empty | 2 |
