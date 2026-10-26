# ADR-0001: .NET 10, Angular 22 and Node 22 LTS

**Date:** 2026-10-25
**Category:** platform
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

The repository had documentation only. `docs/detailed-designs/README.md` names ASP.NET
Core and Angular without versions. The workspace must build on the product owner's
Windows machine and on Linux CI, and must not need upgrading before the first feature
ships. Installed on the development machine on the decision date: .NET SDKs 8.0.426 and
10.0.402, Node 22.23.2, npm 11, Angular CLI 22.2.4.

## Decision

- Backend targets `net10.0` with the .NET 10 SDK pinned in `src/backend/global.json`
  (`rollForward: latestFeature`). .NET 10 is the current long-term-support release.
- Frontend is an Angular 22 workspace created with Angular CLI 22, on Node 22 LTS
  (`engines.node >= 22` in `src/frontend/package.json`). CI installs the same majors.
- Playwright runs Chromium only, per `AGENTS.md`.

## Options Considered

### Option 1: .NET 10, Angular 22, Node 22 (chosen)
- **Pros:** Latest LTS for .NET; the CLI's current major; the Node LTS already installed. No upgrade before launch.
- **Cons:** Angular 22 is the newest major; some third-party packages may lag.

### Option 2: .NET 8, Angular 20, Node 20
- **Pros:** Longest track record.
- **Cons:** Both reach end of support earlier; an upgrade would land mid-project.

### Option 3: Node 24
- **Pros:** Newest Node LTS.
- **Cons:** Not installed on the development machine; no benefit for this workspace. Revisit when the machine moves to 24.

## Consequences

### Positive
- One documented toolchain; `global.json` and `engines` stop version drift.

### Negative
- Contributors on older SDKs must upgrade before the verify command passes.

### Risks
- A package without Angular 22 support. Mitigated by keeping the dependency list to the Angular CDK, ESLint, Prettier and Playwright.

## Implementation Notes

- `src/backend/global.json`, `Directory.Build.props` (`TargetFramework` net10.0).
- `src/frontend/package.json` `engines`; `.github/workflows/ci.yml` uses `actions/setup-node` 22 and `actions/setup-dotnet` 10.0.x.

## References

- [set-up-the-workspace](../../detailed-designs/platform/set-up-the-workspace/README.md)
