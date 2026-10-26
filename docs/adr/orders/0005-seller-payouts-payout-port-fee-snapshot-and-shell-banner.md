# 0005. Seller payouts: a payout port, a fee snapshot on the order and a banner in the shell

Date: 2026-10-26

## Status

Accepted

## Context

The seller-payouts design ([orders/seller-payouts](../../detailed-designs/orders/seller-payouts/README.md))
needs provider onboarding, transfers and payout webhooks, a configurable fee shown before
publishing and recorded on the order (L2-031), and a payout banner. No payment provider is chosen
([ADR orders/0001](0001-payment-provider-behind-a-port-until-chosen.md)). The fee percentage
already exists as `Selling:PlatformFeePercent`, which `GET /api/selling/options` returns to the
sell form's earnings estimate. complete-order publishes `OrderCompleted` after the save
([ADR orders/0004](0004-complete-order-in-process-event-and-fixed-3-day-window.md)). Order
cancellation and refunds raise no event yet.

## Decision

- **A separate `IPayoutProvider` port.** Connected accounts, onboarding links, transfers and the
  payout webhook sit behind their own port next to `IPaymentProvider`, with
  `UnconfiguredPayoutProvider` until a provider is chosen and `FakePayoutProvider` in tests. The
  webhook has its own endpoint, `POST /api/payouts/webhook` with `Payout-Signature`; event IDs
  share the `PaymentWebhookEvents` deduplication table. One provider adapter may implement both
  ports later.
- **One fee setting.** `Selling:PlatformFeePercent` stays the only fee setting; the design's
  `Payouts:FeePercent` and `GET /api/payouts/fee-schedule` are not added. The payment webhook
  snapshots it on the order as `FeeRate` (0.1000) and `FeeAmount`, rounded to the cent, midpoint
  away from zero, and creates the Held payout in the same save.
- **Cancellation is read from the order.** The payout of a Cancelled or Refunded order is left
  Held: the release job only sends payouts of Completed orders and the summary leaves the others
  out. A `Cancelled` payout status arrives with cancel-order if it needs one.
- **The release job catches up on lost events.** Before sending, it applies `OrderCompleted` to
  payouts still Held for their order whose order is Completed, so a crash between the completion
  save and the publish delays a payout but does not lose it.
- **The banner lives in the app shell.** `PayoutBannerComponent` is exported from `@tutumuch/orders`
  and placed above the top bar for verified members, as the screen composition places every
  notification banner; My listings does not exist yet. The API derives `info`, `warning` and
  `danger`; `success` is shown by the browser on the return from onboarding.
- **Error codes** are UPPER_SNAKE as in the rest of the API: `PAYOUT_PROVIDER_UNAVAILABLE`.

## Consequences

- One migration adds `Payouts`, `SellerPayoutAccounts` and the two fee columns on `Orders`.
- Every signed-in page asks `GET /api/payouts` once per session; e2e fixtures answer it in
  `signInAs`.
- Real credentials, re-sending failed payouts and the open-problem hold stay deferred.
