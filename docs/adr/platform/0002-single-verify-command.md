# ADR-0002: One Verify Command for Both Stacks

**Date:** 2026-10-25
**Category:** platform
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

`L2-071` requires one command that restores, builds, lints and tests the backend
solution and the frontend workspace, fails on any failure, and passes on Windows and on
Linux CI. Separate per-stack commands drift: CI runs one set, developers another.

## Decision

`npm run verify` in `src/frontend` is the single verify command. It runs, in order and
stopping at the first failure: `dotnet restore`, `dotnet format --verify-no-changes`,
`dotnet build --no-restore -warnaserror` and `dotnet test --no-build` on
`../backend/TutuMuch.sln`, then `lint` (Prettier and ESLint), `build:all` (every library,
then the application) and `test:e2e` (Playwright, Chromium, against the built app with
fixture API responses). CI runs exactly this command.

## Options Considered

### Option 1: npm script in src/frontend (chosen)
- **Pros:** npm is already required; scripts run identically on Windows and Linux; no extra tool.
- **Cons:** The backend steps live in a frontend `package.json`.

### Option 2: build.ps1 at the root
- **Pros:** Natural on Windows.
- **Cons:** Needs PowerShell on Linux CI; a second scripting language.

### Option 3: Makefile
- **Pros:** Familiar on Linux.
- **Cons:** No make on a default Windows install.

## Consequences

### Positive
- CI and developers run the same steps; one place to add a check.

### Negative
- `npm ci` must run once before `verify`; the README says so.

### Risks
- The chain grows slow as tests grow. Revisit by splitting into parallel CI jobs that still call the same scripts.

## Implementation Notes

- Individual steps stay callable (`npm run lint`, `npm run build:all`, `npm run test:e2e`, `npm run backend:test`).
- Under the `AGENTS.md` testing rules neither the backend tests nor Playwright need a database or a running API.

## References

- [set-up-the-workspace](../../detailed-designs/platform/set-up-the-workspace/README.md)
- `L2-071`
