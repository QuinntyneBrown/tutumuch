# ADR-0002: No Load Tests or Scale Tests

**Date:** 2026-10-25
**Category:** general
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

`L2-057` required 95th-percentile latencies at 200 requests per second against 100,000
Active listings, and the acceptance criteria of `L2-060` were written as tests against
three API instances. The `meet-performance-and-scale` design turned those into a
`TuTuMuch.LoadTestSeeder` console app, k6 scenarios under `tests/load/`, a weekly
`load-test.yml` workflow, a performance environment, `docker-compose.scale.yml` and a
`TuTuMuch.ScaleTests` xUnit project. That was 22 of the feature's 53 estimated hours
plus an unpriced environment, for a marketplace that has not launched and has no
traffic data to size against.

The product owner judged this scope creep: not needed now and not to be implemented.

## Decision

No load tests, load-test seeders, performance environments or multi-instance scale
tests are specified, designed, estimated or built.

- `L2-057` is removed and its number retired.
- `L2-060` keeps its statelessness statement. Its acceptance criteria now check design
  (an architecture test and where state is stored), not behaviour under a three-instance
  deployment.
- `meet-performance-and-scale` keeps the stateless wiring and `StatelessApiTests`
  (the frontend budgets it also kept at the time were later removed by
  [ADR-0003](0003-no-frontend-performance-testing.md)). The load and scale
  test sections, diagrams and estimate rows are removed.
- `AGENTS.md` lists load and scale testing as out of scope, and `tests/` holds only the
  per-project unit, integration and architecture test projects.

## Options Considered

### Option 1: Remove load and scale testing entirely (chosen)
- **Pros:** No performance environment to provision or pay for. Saves 22 hours of build
  work and recurring CI time. The performance feature becomes a set of cheap, fast CI
  checks.
- **Cons:** Latency under load is unmeasured until real traffic arrives. Multi-instance
  bugs are caught by the architecture test and code review only.

### Option 2: Keep the requirements but defer the harnesses
- **Pros:** Targets stay written down.
- **Cons:** Spec-driven skills would keep regenerating the harness designs and
  estimates, and `L2-057` would be an unverifiable requirement.

### Option 3: Keep scale tests, drop load tests
- **Pros:** Multi-instance correctness (sessions, reservations, rate limits) is the
  riskier class of bug.
- **Cons:** Still needs docker-compose orchestration in CI and the extra project. The
  same properties are covered by the architecture test plus the single-instance
  integration tests of accounts, orders and harden-http-and-api.

## Consequences

### Positive
- The performance feature estimate drops from 53 to 31 hours.
- No `tests/load/`, `TuTuMuch.LoadTestSeeder` or `TuTuMuch.ScaleTests`, and no
  performance environment.

### Negative
- No measured latency budget for the API. Slow queries surface through the
  observability metrics of report-errors-and-observe after launch.

### Risks
- A multi-instance regression (for example in-memory state) can reach production.
  Mitigated by `StatelessApiTests` and the Redis-backed stores being the only option
  in the designs.

## Implementation Notes

- Do not add k6, Artillery, NBomber, Locust or similar tooling, a seeder project, a
  `docker-compose.scale.yml`, or a load-test workflow.
- Keep `EndpointKindAttribute`; it feeds the latency metrics, not a load test.
- If load testing is wanted later, supersede this ADR and reintroduce a requirement
  with the then-known traffic figures.

## References

- [ADR-0001](0001-defer-moderation-reviews-and-data-export.md)
- `docs/specs/L2.md` (`L2-060`, traceability note)
- `docs/detailed-designs/platform/meet-performance-and-scale/README.md`
- `docs/estimates/platform/meet-performance-and-scale.md`
