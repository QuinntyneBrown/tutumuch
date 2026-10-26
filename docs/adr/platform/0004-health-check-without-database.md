# ADR-0004: Health Check Without a Database; No Database in Any Test

**Date:** 2026-10-25
**Category:** platform
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

The workspace needs `GET /api/health` answering 200 on a fresh clone (`L2-071`
criterion 4). One option was SQL Server LocalDB for local runs. The `AGENTS.md` testing
rules (CRITICAL) then ruled that backend tests never touch a real database and that
Playwright never runs the backend.

## Decision

`GET /api/health` depends on nothing: no connection string, no database, no cache. It is
tested in-process with `WebApplicationFactory`. LocalDB is not used. The readiness check
that does probe the database (`L2-070`) belongs to report-errors-and-observe and will be
tested with a faked dependency, never a live one.

## Options Considered

### Option 1: Dependency-free health endpoint (chosen)
- **Pros:** Works on Windows and Linux CI with nothing installed; complies with the testing rules.
- **Cons:** Says nothing about the database; that is `L2-070`'s job.

### Option 2: LocalDB for local runs
- **Pros:** Exercises EF Core early.
- **Cons:** Windows only; forbidden in tests by `AGENTS.md`; no entities exist yet.

## Consequences

### Positive
- The verify command needs no database on any machine.

### Negative
- Real database behaviour is first exercised outside the automated suite.

### Risks
- Mapping bugs reach later stages. Mitigated by EF Core in-memory or SQLite in-memory tests when entities arrive.

## References

- `AGENTS.md` (Testing rules)
- [set-up-the-workspace](../../detailed-designs/platform/set-up-the-workspace/README.md)
