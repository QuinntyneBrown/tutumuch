# ADR-0003: No Frontend Performance Testing

**Date:** 2026-10-25
**Category:** general
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

`L2-058` set Lighthouse mobile targets (LCP, CLS, INP) for the catalog and listing
detail pages and a 250 KB gzipped limit on the initial JavaScript. The
`meet-performance-and-scale` design turned that into Lighthouse CI with a stub API,
a gzip bundle-budget script, a lazy-chunk script, a Playwright lazy-loading test, a
`web-vitals` reporter and a `TelemetryController` with histograms: 22 of the
feature's 31 estimated hours. The product has not launched, so the numbers were
guesses, and every one of those checks needs a tool like Lighthouse that measures a
production build in CI.

The product owner decided there is to be no frontend performance testing.

## Decision

No frontend performance requirement, budget, measurement or CI gate is specified,
designed, estimated or built.

- `L2-058` is removed and its number retired. `L1-015` stays, refined by `L2-059`
  (image delivery) and `L2-060` (stateless API).
- `meet-performance-and-scale` keeps only the stateless API wiring,
  `EndpointKindAttribute` and `StatelessApiTests`. The frontend sections, the
  Lighthouse, bundle and lazy-chunk checks, `WebVitalsReporter`,
  `TelemetryController` and the two frontend sequence diagrams are removed.
- Lazy-loaded feature routes remain a workspace convention (`AGENTS.md`), not a
  measured budget. No script or test asserts which chunks the catalog downloads.
- `AGENTS.md` lists frontend performance testing as out of scope.

## Options Considered

### Option 1: Remove frontend performance testing entirely (chosen)
- **Pros:** No Lighthouse CI, Chrome dependency or stub-API server in CI. Saves 22
  hours and keeps `npm run verify` fast.
- **Cons:** Regressions in page weight or rendering are caught by review only.

### Option 2: Keep the requirement, drop only Lighthouse
- **Pros:** Bundle budget is cheap to check from `stats.json`.
- **Cons:** `L2-058` would be half verifiable, and spec-driven skills would keep
  regenerating the Lighthouse design to cover the remaining criteria.

### Option 3: Keep field measurement only (`web-vitals` beacon)
- **Pros:** Real-device data after launch.
- **Cons:** Still an endpoint, histograms and a frontend package for a requirement
  that has no agreed target yet.

## Consequences

### Positive
- The feature estimate drops from 31 to 9 hours and is backend only.
- No `lighthouserc.json`, `@lhci/cli`, `web-vitals`, `check-bundle-budget.mjs`,
  `check-lazy-chunks.mjs`, `e2e/lazy-loading.spec.ts` or telemetry endpoint.

### Negative
- Page speed on phones is unmeasured until real traffic arrives.

### Risks
- A heavy dependency can land in the initial bundle unnoticed. Mitigated by the
  lazy-route convention and code review.

## Implementation Notes

- Do not add Lighthouse, Lighthouse CI, `web-vitals`, bundle analysers, `angular.json`
  size budgets as a gate, or any Playwright test that asserts on timing or chunk
  downloads.
- If frontend performance targets are wanted later, supersede this ADR and
  reintroduce a requirement with measured figures.

## References

- [ADR-0002](0002-no-load-or-scale-tests.md)
- `docs/specs/L2.md` (`L2-060`, traceability note)
- `docs/detailed-designs/platform/meet-performance-and-scale/README.md`
- `docs/estimates/platform/meet-performance-and-scale.md`
