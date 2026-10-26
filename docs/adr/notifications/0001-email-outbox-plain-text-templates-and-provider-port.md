# 0001. Email outbox with a claim token, plain-text templates and the provider behind a port

Date: 2026-10-26

## Status

Accepted

## Context

The send-email-notifications design
([notifications/send-email-notifications](../../detailed-designs/notifications/send-email-notifications/README.md))
writes each email request to an outbox in the same transaction as the change that caused it,
and has `Tutu Much Worker` send it with retry and back-off for 24 hours (L2-042). When it was built,
the features that raise email events were email verification, checkout (a new sale), ship-order
(tracking, delivery, the ship deadline cancellation and refund, and the 3-day reminder that
ADR orders/0003 deferred) and complete-order. Domain events are in-process MediatR
notifications published after the save (ADR orders/0004). No email provider is chosen, final
email copy is not supplied, and the backend tests run on SQLite in memory, never SQL Server
(AGENTS.md).

## Decision

- **The outbox is written by the handler, not by an event.** Handlers call
  `IEmailOutbox.Enqueue(type, recipientMemberId, model)` before `SaveChangesAsync`, so the email
  commits or rolls back with the change. An event published after the save could be lost in a
  crash; the outbox row cannot. The port is `IEmailOutbox`, not the design's
  `INotificationPublisher`, which clashes with MediatR's interface in every handler file.
- **`OutboxMessages` holds email requests only**, with typed columns (`NotificationType`,
  `RecipientMemberId`, `Payload`) in place of the design's generic `Type` and
  `EmailNotificationRequested` envelope. Nothing else uses an outbox yet; a second message
  kind can add its own table or generalise this one.
- **Claims use a concurrency token, not `READPAST`/`UPDLOCK` SQL.** The dispatcher selects due
  IDs, then claims each message by setting `Processing`, `LockedUntil` and a new `ClaimToken`
  and saving with a check on the old token. The loser of a race gets a concurrency conflict and
  skips the message. This is portable LINQ that runs the same on SQLite in the tests and on SQL
  Server, at the cost of one round trip per message. One pass sends one message at a time;
  several worker instances share the work through the claim.
- **Templates are plain text in C#.** `EmailTemplates` renders each type's subject and body
  with string interpolation, its deep link from `App:BaseUrl` and a footer link to
  `/account/emails`. Fluid (Liquid) templates, HTML bodies and the logo layout wait for the
  final copy, which is still `<TO SUPPLY>`.
- **The provider stays behind `IEmailSender`.** A send throws `HttpRequestException`,
  `TimeoutException` or a timeout `TaskCanceledException` when worth retrying, and
  `EmailRejectedException` when not. `EmailMessage` carries the outbox message ID as its
  idempotency key. Until a provider is chosen, `LoggingEmailSender` writes each email to the
  log, as before, so a developer can follow a verification link; tests use a capturing fake.
- **Only the types whose events exist are built**: `EmailVerification`, `SaleMade`,
  `OrderShipped`, `OrderDelivered`, `ShipByReminder`, `OrderCancelled` and `OrderRefunded`.
  The verification email moves from a direct send to the outbox. The buyer who confirms
  receipt gets no delivery email. The duplicate-payment refund of L2-029 sends no email yet.
- **The ship-by reminder is stored on the order.** `Orders.ShipReminderDue` (3 US business days
  after payment) and `Orders.ShipReminderSentAt` let `ShipReminderJob` remind once; the order's
  status token stops a reminder for an order shipped meanwhile. The migration marks existing
  orders as reminded.
- **Deferred:** the preference check and unsubscribe headers (manage-notification-preferences,
  since built, see ADR notifications/0002; every type built here is always sent under L2-043), the retention job, the dispatch metrics
  and the delay alert (L2-069), and parallel dispatch within one pass.

## Consequences

- One migration adds `OutboxMessages` with `IX_OutboxMessages_Due`, and the two reminder
  columns with `IX_Orders_Status_ShipReminderDue` on `Orders`.
- Tests that read an email run one dispatcher pass first (`TutuMuchApiFactory.DispatchEmailsAsync`).
- Each later feature adds its notification type, model and template, and calls `IEmailOutbox`.
- `OutboxMessages` grows until the retention job is built.
