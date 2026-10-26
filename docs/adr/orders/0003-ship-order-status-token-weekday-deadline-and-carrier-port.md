# 0003. Ship order: status as concurrency token, weekday deadline and a carrier tracking port

Date: 2026-10-26

## Status

Accepted

## Context

The ship-order design ([orders/ship-order](../../detailed-designs/orders/ship-order/README.md))
guards transitions with a `RowVersion` column, reads US holidays and per-carrier tracking patterns
from configuration (both `<TO SUPPLY>`), and learns of delivery from a carrier tracking service
that is not chosen yet. The tests run on SQLite in memory, which has no `rowversion` type, and
must not call a real carrier ([AGENTS.md](../../../AGENTS.md)). Emails belong to the
send-email-notifications prompt, and relisting a cancelled item belongs to cancel-order.

## Decision

- **Status is the concurrency token.** `Orders.Status` is an EF Core concurrency token, so an
  update runs `WHERE Status = @original`. The seller adding tracking and the ship deadline job
  cancelling the same Paid order cannot both commit; the loser answers 409
  `ORDER_INVALID_TRANSITION` or is skipped by the job. No `RowVersion` is added.
- **Business days are weekdays, counted in UTC.** `BusinessDays.Add` skips Saturdays and Sundays
  and keeps the time of day of payment, so the ship-by date is 5 weekdays after payment to the
  minute. US holidays are not skipped until the list is supplied. The pages show dates in UTC
  ("Sat 24 Oct") so they match the API's count.
- **Tracking formats live in the domain.** `TrackingNumbers` holds the four carriers of the mock
  (USPS, UPS, FedEx, DHL), their formats after removing spaces and hyphens (USPS 20 to 22 digits
  or `AA123456789US`; UPS `1Z` and 16 letters or digits; FedEx 12, 15, 20 or 22 digits; DHL 10 or
  11 digits), the inline messages and the public tracking URLs. A malformed number is 422
  `TRACKING_INVALID` with a `trackingNumber` field error, the house shape for field errors (the
  design said 400).
- **Carrier tracking stays behind `ICarrierTracking`.** The port verifies and reads a signed
  webhook. `UnconfiguredCarrierTracking` accepts nothing until a service is chosen, so orders stay
  Shipped; tests use `FakeCarrierTracking` with an HMAC signature. A repeated or unrelated event
  is answered 200 and changes nothing, because only a Shipped order with that carrier and number
  moves to Delivered. Polling is not built.
- **Deferred:** the tracking, cancellation and 3-day reminder emails (L2-033 criteria 1 to 3 in
  part) and the reminder job, which only sends email, wait for send-email-notifications. The
  deadline job refunds through `IPaymentProvider.RefundAsync` directly, as the duplicate-payment
  refund does, and leaves a failed refund Pending for the outbox. `UX_Orders_ListingId` stays
  unfiltered until cancel-order adds relisting.

## Consequences

- No migration adds a row version; `OrderStatusChanges` records every transition with its actor.
- A deadline can land on a US holiday until the holiday list is supplied.
- Changing a carrier's format is a code change with a test, not a configuration change.
- Delivery is never reported outside the tests until a carrier tracking service is chosen.
