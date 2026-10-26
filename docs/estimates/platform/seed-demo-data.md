# Estimate — Seed demo data

## Basis

The hours assume an intermediate ASP.NET Core/C# developer who does not yet know
the Tutu Much codebase. They cover implementation, automated tests and review fixes, and
exclude requirements, design and photo curation. They assume the account, listing
and photo commands and `TutuMuch.Migrator` exist. Tests use SQLite in-memory and a
fake photo store only; `reset` against SQL Server is checked by hand.
[Detailed design](../../detailed-designs/platform/seed-demo-data/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `TutuMuch.Cli` and `TutuMuch.Cli.Tests` projects, `CliArguments`, help and exit codes | 3 |
| `reset`: `ResetGuard`, `IDatabaseSchema` with `MigratorApp.ApplyMigrations`, photo store clean-up | 4 |
| `seed` members: register, verify from the queued link, suppress the email | 3 |
| `seed` listings: `DemoCatalogue`, `SeedClock`, drafts and publish across four categories | 5 |
| `seed` photos: upload through the pipeline, missing-file check | 3 |
| Idempotency, `seed --reset`, CLI README and root README step | 3 |
| Manual check against LocalDB and the running app | 3 |
| **Total** | **24** |

## Minimal scope

The minimal version ships `seed` without `reset` and without the missing-file check:
the developer drops the database by hand and runs `TutuMuch.Migrator`. It still loads
the full demo catalogue through the application. Minimal total: **15** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| `reset` command | Developer drops the database and empties the photo folder by hand | 6 |
| Missing-file check | A missing photo fails mid-seed with exit 5 instead of before any write | 1 |
| Verification through the queued link | Members are verified through the domain method instead of `VerifyEmailCommand` | 2 |
