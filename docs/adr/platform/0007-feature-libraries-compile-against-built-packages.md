# ADR-0007: Feature Libraries Compile Against the Built api and components Packages

**Date:** 2026-10-25
**Category:** platform
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

`npm run build:all` builds every library with ng-packagr and then the application. The
root `tsconfig.json` maps `@tutumuch/api` and `@tutumuch/components` to their source `public-api.ts`.
The first feature library that imported them, `@tutumuch/accounts`, failed with TS6059: the
sources of another library are outside the library's `rootDir`.

## Decision

Each feature library's `tsconfig.lib.json` overrides the two aliases to the built packages,
`../../dist/api` and `../../dist/components`. `build:libs` already builds `api` and
`components` first. The application and the editor keep the root mapping to source.

## Options Considered

### Option 1: Path override to dist in each feature library (chosen)
- **Pros:** The standard Angular workspace arrangement; each library is compiled as its consumers will see it.
- **Cons:** A feature library build needs `api` and `components` built first.

### Option 2: Widen rootDir to the workspace
- **Pros:** No build order.
- **Cons:** ng-packagr would bundle other libraries' sources into each package.

## Consequences

### Positive
- `npm run verify` builds all libraries and the application again.

### Negative
- Building one feature library on its own needs `ng build api && ng build components` first.

## References

- `AGENTS.md` (Frontend workspace layout)
- [ADR platform-0003](0003-frontend-library-layout-over-shell-design-paths.md)
