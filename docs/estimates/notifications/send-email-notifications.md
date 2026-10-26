# Send email notifications — estimate

## Basis

The hours assume an intermediate ASP.NET Core/C# developer, familiar with EF Core and hosted services, who is new to this codebase. The feature has no Angular work. The estimate also assumes the following: the shared `OutboxMessages` table and `TutuMuch.Worker` host exist; an email provider with an HTTPS API and idempotency keys is chosen (`<TO SUPPLY>`); and final email copy arrives as text, so template work is layout and wiring rather than writing. Each template is counted at 1 hour for subject, HTML and plain-text bodies. Hours cover implementation, tests and review fixes. [Detailed design](../../detailed-designs/notifications/send-email-notifications/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `NotificationType` and `NotificationCategory` with the type-to-category map | 2 |
| `IEmailOutbox`, `EmailOutbox` and `EmailNotificationRequested` payload | 3 |
| `OutboxMessage` state machine, back-off with jitter, 24-hour window, unit tests | 5 |
| `OutboxMessages` columns, `IX_OutboxMessages_Due` index, migration | 2 |
| `OutboxEmailDispatcher`: `READPAST`/`UPDLOCK` claim, lease recovery, parallelism | 6 |
| `SendEmailNotificationHandler` with suppression and outcome recording | 5 |
| `IRecipientResolver` | 2 |
| `FluidEmailTemplateRenderer` and shared layout | 5 |
| 19 templates with typed models | 19 |
| `DeepLinkBuilder` and per-template deep-link tests | 2 |
| `EmailContentGuard` with Luhn and keyword checks, tests | 3 |
| `ProviderEmailSender` adapter with result classification and headers | 5 |
| `OutboxRetentionJob` | 2 |
| Metrics and structured logs | 3 |
| Integration tests: "You made a sale" within 2 minutes, retry against a failing fake provider, no secrets, deep links | 8 |
| **Total** | **72** |

## Minimal scope

The minimal scope ships the outbox publisher, a single-instance dispatcher without the lease-based multi-instance claim, the retry state machine with the 24-hour window, the preference check, the content guard and the provider adapter. It ships the 14 templates the `L2-042` list names. It leaves out the retention job, the metrics, and templates for email types that other requirements add (`PasswordChanged`, `DraftExpiring`, `DataExportReady`, `CancellationRequested`, `Marketing`); the features that need those deliver their own templates. It satisfies `L2-042` in full, while `Tutu Much Worker` runs as one instance. Minimal scope totals **59 hours**.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Multi-instance claim; run one dispatcher instance | `Tutu Much Worker` cannot scale out email dispatch | 3 |
| `OutboxRetentionJob` | `OutboxMessages` grows until cleaned by hand | 2 |
| Metrics; keep logs only | No dashboard or alert on email delay or failures (`L2-069` job-failure metric) | 3 |
| Templates beyond the `L2-042` list (5 types) | Owning features add their own templates later | 5 |
| **Total if all cut** | | **13** |
