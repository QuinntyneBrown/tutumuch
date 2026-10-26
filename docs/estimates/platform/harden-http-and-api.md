# Estimate — Harden HTTP and the API

## Basis

The hours assume an intermediate Angular/TypeScript and ASP.NET Core/C# developer
who does not yet know the TuTuMuch codebase. They cover implementation, automated
tests and review fixes, and exclude requirements, design and mock work. The
estimate assumes the `TuTuMuch API` project skeleton, the `Distributed cache` (Redis)
and the problem details middleware of report-errors-and-observe already exist.
The hosting platform, its load balancer TLS policy and the static host header
mechanism are `<TO SUPPLY>`; the hours assume the platform accepts a declarative
header file and a named TLS policy. Feature-specific validators belong to each
feature's own estimate.
[Detailed design](../../detailed-designs/platform/harden-http-and-api/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| Kestrel TLS 1.2+ settings, `UseHttpsRedirection` with 301, `HstsOptions`, `ForwardedHeadersOptions` with known proxies | 4 |
| `SecurityHeadersMiddleware` and `SecurityHeadersOptions` | 3 |
| `deploy/security-headers.json` for the static host, CSP host list, `inlineCritical` off | 4 |
| Kestrel 1 MB body limit, `request_too_large` mapping, upload opt-out | 2 |
| JSON options (`UnmappedMemberHandling.Skip`, strict numbers, max depth) and `malformed_json` mapping | 2 |
| `ValidationBehavior`, `RequestValidationException`, architecture test for missing validators | 5 |
| Request-record convention and architecture test forbidding domain types as action parameters | 3 |
| `BannedApiAnalyzers` with `BannedSymbols.txt`; ESLint rules against `[innerHTML]` and `bypassSecurityTrust*` | 3 |
| `AntiforgeryController`, antiforgery options and `AntiforgeryValidationMiddleware` with webhook exemption | 5 |
| `AntiforgeryTokenService` and `AntiforgeryInterceptor` with one refresh-and-replay | 5 |
| CORS policy `Frontend` from `Cors:AllowedOrigins` | 2 |
| `RateLimitPolicies`, `RateLimitPolicyAttribute`, `RedisRateLimitingMiddleware` | 6 |
| `RedisFixedWindowRateLimitStore` Lua script, fail-open path and metric | 5 |
| `RateLimitInterceptor` and fallback "Too many requests" alert | 3 |
| Integration tests: headers, CSP build scan, TLS smoke, mass assignment, size, malformed JSON, antiforgery, CORS | 10 |
| Rate-limit tests against a Redis Testcontainer (anonymous, member, register, create-listing) | 5 |
| **Total** | **67** |

## Minimal scope

The minimal version keeps HTTPS redirect, HSTS and the four `L2-048` headers on
the API and the static host, the 1 MB limit, JSON options, request records and
`ValidationBehavior`, the antiforgery cookie, header and middleware with the
interceptor (no refresh-and-replay), the CORS allow-list, and the four rate-limit
policies on Redis with a plain 429 handler in the frontend. It drops the
architecture tests, the banned-API analyzer, the ESLint rules, the fail-open
metric and the fallback alert, and it reduces tests to one per acceptance
criterion. It still satisfies `L2-048`, `L2-049`, `L2-050` and `L2-051`, with
weaker protection against regressions. Minimal total: **46** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Architecture tests for validators and request records | A command without a validator, or an action binding a domain type, is not caught in CI | 5 |
| Banned-API analyzer and ESLint output-encoding rules | Raw SQL or `[innerHTML]` can enter through review only | 3 |
| Antiforgery refresh-and-replay | A stale token after a long idle tab shows an error instead of retrying | 2 |
| Rate-limiter fail-open metric | A Redis outage that disables rate limits is not visible on dashboards | 2 |
| TLS 1.1 smoke test in deployment | A load balancer TLS regression is found only by external scanning | 2 |
