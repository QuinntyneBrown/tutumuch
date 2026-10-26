# 0004. Complete order: in-process completion event and a fixed 3-day window

Date: 2026-10-26

## Status

Accepted

## Context

The complete-order design ([orders/complete-order](../../detailed-designs/orders/complete-order/README.md))
completes an order when the buyer confirms receipt or 3 days after delivery, refuses while a
problem is open, and raises `OrderCompleted` so that seller-payouts marks the payout releasable.
At the time of building, no `Payout` aggregate, no `OrderProblem` and no domain event dispatcher
exist: they belong to the seller-payouts and resolve-order-problems prompts. The order state
machine and the status concurrency token come from ship-order
([ADR orders/0003](0003-ship-order-status-token-weekday-deadline-and-carrier-port.md)).

## Decision

- **`OrderCompleted` is a MediatR notification published after the save.** The confirm-receipt
  and completion-job handlers save the order, then publish
  `OrderCompleted(OrderId, BuyerId, SellerId, CompletedAt)` once. seller-payouts adds the
  `INotificationHandler` that marks the payout releasable; until then nothing handles it. A crash
  between the save and the publish loses the event; the payout release job of seller-payouts
  selects by `Orders.Status = 'Completed'` as well, so a lost event delays the payout and does not
  lose it.
- **"Ready for payout" means `Status = Completed` with `CompletedAt` set.** A new nullable
  `Orders.CompletedAt` column records when; `EndedAt` is set by the state machine as before.
- **The 3-day window is a domain constant.** `Order.AutoCompleteAfter` is 3 days, as L2-034
  states; it is not configuration. `Order.AutoCompletesAt` is `DeliveredAt` plus 3 days while
  Delivered and null otherwise, and the buyer's order (`GET /api/purchases/{id}`) returns it as
  `autoCompletesAt`. The job interval is `Orders:CompletionJobInterval`, 5 minutes.
- **Confirming a Shipped order records two changes.** `Order.ConfirmReceipt` moves Shipped to
  Delivered and then Delivered to Completed, both by the buyer, because L2-032 has no direct move.
- **Races use the status token.** The buyer confirming and the job completing the same order
  cannot both commit: the buyer gets 409 `ORDER_INVALID_TRANSITION`, the job skips the order.
  Error codes are UPPER_SNAKE as in the rest of the API, not the design's dotted codes.
- **Deferred:** the open-problem guard (L2-034 criterion 3 and 409 `ORDER_PROBLEM_OPEN`) arrives
  with resolve-order-problems, which introduces `OrderProblem`; "Report a problem" is not shown
  until then. The seller's completed-sale payout panel belongs to seller-payouts.

## Consequences

- One migration adds `Orders.CompletedAt` and `IX_Orders_Status_DeliveredAt` for the job.
- Until resolve-order-problems lands, every Delivered order completes 3 days after delivery.
- seller-payouts plugs in by adding a handler, without touching complete-order.
