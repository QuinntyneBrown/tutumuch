# Estimate — Keep TuTuMuch available and recoverable

## Basis

The hours assume an intermediate Angular/TypeScript and ASP.NET Core/C# developer
with working knowledge of GitHub Actions and one infrastructure-as-code tool, who
does not yet know the TuTuMuch codebase. They cover implementation, automated tests
and review fixes, and exclude requirements, design and mock work. The hosting
platform, managed database product, infrastructure-as-code tool and uptime
monitor are `<TO SUPPLY>`; the hours assume each is configured declaratively and
supports PITR, health probes and point-in-time restore through a command-line
interface. The health endpoints come from report-errors-and-observe and the
banner service from responsive-accessible-shell.
[Detailed design](../../detailed-designs/platform/keep-available-and-recoverable/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| Load balancer probe, draining, instance counts and zones in infrastructure as code | 5 |
| `ShutdownReadinessHealthCheck` and shutdown timeout | 2 |
| `TuTuMuch.Migrator` job and rolling deployment step | 4 |
| Uptime monitor checks and monthly availability report query | 3 |
| `MaintenanceOptions`, `MaintenanceOptionsValidator` (Eastern time rules, monthly total) with tests | 5 |
| `SystemStatusController` and `MaintenanceModeMiddleware` with tests | 4 |
| `SystemStatusService`, maintenance banner, `MaintenanceInterceptor`, `MaintenancePageComponent` | 5 |
| Static maintenance page and `maintenance.yml` workflow | 4 |
| PITR and 35-day retention in infrastructure as code; `infra-policy` CI check | 3 |
| `TuTuMuch.RestoreVerifier` with four checks and report | 8 |
| `restore-test.yml` workflow (restore, verify, time, report, clean up) | 6 |
| Restore runbook | 3 |
| Instance-failure acceptance test (stop one of three instances, assert no errors after 30 s) | 3 |
| **Total** | **55** |

## Minimal scope

The minimal version ships the load balancer probe settings, graceful shutdown,
the uptime monitor checks, PITR with 35-day retention and its CI check, the
static maintenance page switched by `maintenance.yml`, and a restore-test
workflow that restores, runs migration and `DBCC CHECKDB` checks only, and
records the elapsed time. It drops the in-app maintenance banner and API
maintenance mode (visitors see no advance notice in the app), the options
validator, the business-invariant and recency checks, the migrator job and the
runbook. It satisfies `L2-067` and the measurable parts of `L2-066`; advance
announcement of maintenance then relies on channels outside the app. Minimal
total: **30** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| In-app maintenance banner and `SystemStatusService` | No advance notice inside TuTuMuch | 3 |
| `MaintenanceOptionsValidator` | A window breaking the `L2-066` limits is caught only by review | 3 |
| Business-invariant and recency checks in the verifier | A restore with lost or inconsistent orders passes the test | 4 |
| `TuTuMuch.Migrator` as a separate job; run migrations at API start | Concurrent starts race on migrations; rollback is harder | 2 |
| Monthly availability report query; use the monitor's built-in report | Maintenance windows are not excluded automatically | 2 |
