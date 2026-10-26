# Estimate — Meet performance and scale targets

## Basis

The hours assume an intermediate ASP.NET Core/C# developer who does not yet know
the Tutu Much codebase. They cover implementation, automated tests and review fixes, and
exclude requirements, design and mock work. They assume the rate-limit store of
harden-http-and-api and the token store of accounts exist. Tuning queries is
counted in the owning feature's estimate. No load or scale tests
([ADR 0002](../../adr/general/0002-no-load-or-scale-tests.md)) and no frontend
performance testing
([ADR 0003](../../adr/general/0003-no-frontend-performance-testing.md)) are built.
[Detailed design](../../detailed-designs/platform/meet-performance-and-scale/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `EndpointKindAttribute` tagging and its use in metrics | 2 |
| `RedisConnectionFactory` and Data Protection key ring in Redis | 3 |
| `StatelessApiTests` architecture test | 2 |
| `DbContext` pooling, read/write command timeouts, read-path conventions sample and checklist | 2 |
| **Total** | **9** |

## Minimal scope

The minimal version ships the Data Protection key ring in Redis and the stateless
architecture test. It drops `EndpointKindAttribute` and the read-path checklist.
It still satisfies the acceptance criteria of `L2-060`. Minimal total: **5** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| `EndpointKindAttribute` | Latency metrics cannot be split by read, write and payment-bound endpoints | 2 |
| Read-path conventions sample and checklist | Query conventions are passed on by review only | 2 |
