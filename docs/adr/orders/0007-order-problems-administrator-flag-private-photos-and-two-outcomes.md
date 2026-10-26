# 0007. Order problems: an administrator flag, private evidence photos and two outcomes

Date: 2026-10-26

## Status

Accepted

## Context

The resolve-order-problems design
([orders/resolve-order-problems](../../detailed-designs/orders/resolve-order-problems/README.md))
lets a buyer report a problem on a Shipped or Delivered order, the seller reply once and an
administrator decide it (L2-036). It is the only administrator feature (AGENTS.md). Nothing in
the code knew about administrators: `L2-010` names an Administrator role, but members had no
roles and the API had no administrator policy. The design left open the problem number's start,
the description length, whether the 800-pixel minimum applies to evidence photos, how the
operations inbox is reached, and the order status and payout effect of a partial refund, which
`L2-032` has no transition for and the mocks have no control for. It used dotted error codes.
complete-order ([ADR orders/0004](0004-complete-order-in-process-event-and-fixed-3-day-window.md))
and seller-payouts ([ADR orders/0005](0005-seller-payouts-payout-port-fee-snapshot-and-shell-banner.md))
deferred "an open problem stops auto-completion and holds the payout" to this feature.

## Decision

- **Administrator flag.** `Members.IsAdministrator` (bit, default 0) is the Administrator role of
  `L2-010`. Operations grant it in the database; there is no screen and no second factor (both
  deferred, [ADR general/0001](../general/0001-defer-moderation-reviews-and-data-export.md)). The
  `Administrator` policy reads the flag from the database on each request, like `VerifiedMember`,
  so a change takes effect at once, and answers 403 `ADMINISTRATOR_REQUIRED`.
- **Operations inbox.** A new problem emails the seller and every administrator through the
  existing outbox (`ProblemReported`), with a link to `/admin/problems/{reference}`. No separate
  `Notifications:OperationsInbox` address is added.
- **Private evidence photos.** Photos go up with the report as `multipart/form-data` (0 to 4).
  Each is checked for size (10 MB), sniffed type and pixel count as listing photos are, without
  the 800-pixel minimum, because evidence is often a close-up. It is re-encoded once as a JPEG of
  at most 1600 pixels, which strips every metadata block, and stored privately through
  `IPhotoStorage.SaveOriginalAsync` under `problems/{problemId}/{photoId}.jpg`. A new
  `ReadOriginalAsync` serves it from `GET /api/orders/{id}/problem/photos/{photoId}` to the buyer,
  the seller and administrators only, with `Cache-Control: private, no-store`.
- **References and lengths.** Problems are numbered from 1001 (`P-1001`) as orders are: the next
  number is the highest plus one, guarded by `UX_OrderProblems_Number`. `UX_OrderProblems_OrderId`
  allows one problem per order. Description, seller reply and resolution reason are trimmed and at
  most 1000 characters.
- **Two outcomes.** An administrator chooses `FullRefund` or `ReleaseToSeller`, with a reason both
  members are emailed. `FullRefund` moves the order to Refunded, adds a full `Refund`
  (`ProblemResolved`) issued through the provider as cancellations are, and cancels the payout
  (new `PayoutStatus.Cancelled`). `ReleaseToSeller` completes a Delivered order as the
  administrator and publishes `OrderCompleted`, which makes the payout releasable; a Shipped
  order answers 409 `ORDER_INVALID_TRANSITION`, because `L2-032` has no Shipped to Completed move.
  **Partial refund is deferred**: `L2-036` is amended to say so, the API refuses it with
  `OUTCOME_INVALID`.
- **Holding and completion.** Opening a problem keeps the Held payout Held with the new
  `HoldReason = OpenProblem`. The completion job skips Delivered orders with an open problem, and
  the buyer cannot confirm receipt while one is open (409 `ORDER_PROBLEM_OPEN`, as complete-order named it). The release
  job's catch-up also repairs a payout held for a problem whose order completed but lost its
  `OrderCompleted` event.
- **Errors** are UPPER_SNAKE: `PROBLEM_INVALID` and `PROBLEM_RESPONSE_INVALID` and
  `PROBLEM_RESOLUTION_INVALID` (422 with field errors), `PROBLEM_WINDOW_CLOSED`,
  `PROBLEM_ALREADY_REPORTED`, `PROBLEM_RESPONSE_NOT_ALLOWED`, `PROBLEM_ALREADY_RESOLVED` (409),
  `ORDER_PROBLEM_OPEN` (409), `PROBLEM_NOT_FOUND` (404) and `ADMINISTRATOR_REQUIRED` (403).
- **Admin shell.** The admin problem page lives in `@tutumuch/orders` with the other order screens and
  renders in the admin shell chosen by route `data.shell = 'admin'`: the top bar's admin variant
  and no footer. The page does not check the role itself; a member without it sees the load
  error, because the API answers 403.

## Consequences

- One migration adds `Members.IsAdministrator`, `OrderProblems` and `ProblemPhotos`.
- `PayoutStatus.Cancelled` exists only for problem refunds; payouts of cancelled orders stay Held,
  as [ADR orders/0006](0006-cancel-order-one-path-request-on-the-order-and-relist.md) decided.
- A partial refund needs a new requirement, a status or payout rule and a mock before it can be
  built.
- An administrator finds a problem only from its email; a problems list or queue stays out of
  scope with moderation.
