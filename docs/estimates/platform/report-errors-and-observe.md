# Estimate — Report errors and observe the system

## Basis

The hours assume an intermediate Angular/TypeScript and ASP.NET Core/C# developer
with working knowledge of Serilog and OpenTelemetry, who does not yet know the
Tutu Much codebase. They cover implementation, automated tests and review fixes, and
exclude requirements, design and mock work. The monitoring and log platform, the
on-call channel and the management port are `<TO SUPPLY>`; the hours assume a
Prometheus-compatible scraper and rule format. The toast, banner and form-error
components come from responsive-accessible-shell. Orders and worker features
increment their own counters; this estimate counts the meter and helpers.
[Detailed design](../../detailed-designs/platform/report-errors-and-observe/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `AppError` model and `ApiErrorInterceptor` mapping | 4 |
| `ServerErrorPageComponent`, `NotFoundPageComponent`, `ForbiddenPageComponent`, `OfflinePageComponent` | 5 |
| `LoadErrorComponent` and danger-toast "Try again" replay | 3 |
| `ConnectivityService`, offline banner, "Back online" toast and retry | 3 |
| Service worker and `ngsw-config.json` for the app shell | 3 |
| `GlobalErrorHandler` | 1 |
| `CorrelationIdMiddleware`, `CorrelationIdDelegatingHandler`, outbox correlation column and worker scope | 5 |
| `ProblemDetailsExceptionHandler`, exception types, mapping table, `ErrorCodes` and uniqueness test | 6 |
| Serilog setup, enrichers, request logging settings | 3 |
| `SensitiveDataRedactionPolicy`, `RedactionEnricher`, `[Sensitive]` attribute and redaction tests | 5 |
| OpenTelemetry metrics, Prometheus exporter on management port, `TutuMuch` meter, `JobMetrics` | 5 |
| `alerts.yml` and `promtool` rule tests | 4 |
| Health checks for database and photo storage, minimal response writer, exemptions | 4 |
| Acceptance tests: 500 body, 400 field list, frontend 500 message, offline message, log search by correlation ID, sign-in log content, metrics scrape, health 200/503 | 10 |
| **Total** | **61** |

## Minimal scope

The minimal version ships the interceptor, the server-error, not-found and
offline pages, the danger toast with retry, `ConnectivityService` without the
"Back online" retry, the correlation ID middleware and outbound handler, the
problem details handler and codes, Serilog with redaction, the OpenTelemetry
metrics and exporter, the three `L2-069` alerts without rule tests, and the
health endpoints. It drops the service worker (the offline page then shows only
for in-app navigation), `ForbiddenPageComponent` (403 shows the not-found page),
`GlobalErrorHandler`, the worker correlation scope and `promtool` tests. It still
satisfies the acceptance criteria of `L2-068`, `L2-069` and `L2-070`; log entries
from `Tutu Much Worker` do not join the originating request. Minimal total: **51**
hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Service worker app-shell caching | A reload while offline shows the browser's own error, not the Tutu Much offline page | 3 |
| Outbox correlation column and worker scope | Email and payout logs cannot be found by the request's correlation ID | 2 |
| `promtool` alert rule tests | Alert expressions are verified only in production | 2 |
| `BackgroundJobFailing` alert | Job failures appear on dashboards only | 1 |
| "Back online" automatic retry | The member presses "Try again" after reconnecting | 1 |
