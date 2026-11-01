# 0009. Grab it for me: the total up front, a deposit kept only when the shopper cancels after acceptance, and no order or payout

Date: 2026-11-01

## Status

Accepted

## Context

On a shelf find ([ADR catalog/0009](../catalog/0009-shelf-finds-are-not-listings.md)) TuTuMuch's owner offers to go to the store, buy the item and send it to a shopper "for a fee and a non-refundable deposit" (`L1-025`). The shopper must not be able to make TuTuMuch drive to a store and then walk away for free, and must not lose money when TuTuMuch fails to deliver. Payment already runs through `IPaymentProvider` with the result from its signed webhook ([ADR orders/0001](0001-payment-provider-behind-a-port-until-chosen.md)); orders, shipping, problems and payouts (`L2-031` to `L2-036`) are built around a listing and its seller.

## Decision

- **The shopper pays the whole total up front.** Total = shelf price + grab fee (set by the poster, $1 to $50) + the provider's sales tax quote on both. One payment, one webhook, no balance to chase later. If the price at the store is higher, TuTuMuch does not charge more: it reports "Couldn't get it (Price went up)" and refunds in full.
- **The deposit is a stated part of that total.** Deposit = 20% of the shelf price, at least $5.00, never more than the shelf price, rounded to the cent (`Grabs:DepositPercent`, `Grabs:MinimumDeposit`). The shopper must tick an acknowledgement next to the amount before the card fields work, and `POST /api/grabs` refuses `depositAcknowledged: false` with `422 DEPOSIT_NOT_ACKNOWLEDGED`.
- **When the deposit is kept.** Only when the shopper cancels an Accepted grab: they get the total less the deposit. Every other ending refunds the total in full: the shopper cancels before acceptance, TuTuMuch declines, does not answer within 48 hours, cannot get the item, or does not buy it within 3 days of accepting. A Bought grab cannot be cancelled.
- **Its own state machine.** `Grab` (`GR-{number}`): PaymentPending, then Requested, Accepted, Bought, Received; ended by PaymentFailed, Declined, Unavailable, Expired or Cancelled. Status is a concurrency token, so the administrator and the deadline job cannot both move it. A filtered unique index allows one open grab (PaymentPending, Requested, Accepted, Bought) per find, so the database decides a race (as `L2-029` with [ADR orders/0002](0002-reserve-with-a-conditional-update-and-a-filtered-unique-index.md)).
- **Payments and refunds reuse the existing tables.** `Payment.CheckoutId` becomes nullable and `Payment.GrabId` is added; the webhook handler sends a grab payment to the grab branch. Refunds use `Refund` with the new reason `GrabRefunded` and a stated amount, issued through `IPaymentProvider.RefundAsync` with the refund ID as the key.
- **A grab is not an order and has no payout.** Only administrators offer grabs, so TuTuMuch keeps the money; there is no seller to pay, no tracking, no automatic delivery and no problem report of `L2-036`. The shopper says "I've got it", or the job marks a Bought grab Received after 14 days. Opening grabs to members would need payouts and problems for grabs, and is deferred.
- **Deadlines in one job.** `GrabDeadlineJob` in `TuTuMuch.Worker` expires Requested grabs after 48 hours and Accepted grabs after their buy-by time, with full refunds, and marks Bought grabs Received after 14 days.
- **Emails are transactional.** New `NotificationType` values `GrabRequested` (to the poster) and `GrabUpdated` (to the shopper) go through the outbox and cannot be turned off.

## Consequences

- The shopper sees every amount, the deposit and the refund rule before paying, and in the cancel dialog.
- TuTuMuch carries the risk of a price rise at the store; it can decline the grab or refund in full.
- A refund the provider does not accept stays Pending, as for orders; dispatching pending refunds belongs to the outbox design.
- Grabs by members, paying posters, tracking numbers and problem reports for grabs are deferred.
