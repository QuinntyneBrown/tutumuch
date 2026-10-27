# TuTuMuch detailed designs

This folder holds the detailed design of every TuTuMuch feature. Each design refines the
level-2 (L2) requirements in [`docs/specs/L2.md`](../specs/L2.md) and draws its screens
and states from the HTML mocks in [`docs/mocks/`](../mocks/README.md). Each feature
folder holds a `README.md` with four sections (Overview, Description, Requirements,
Diagrams) and a `diagrams/` folder of PlantUML sources and rendered PNGs. Each design
has a matching estimate in [`docs/estimates/`](../estimates/).

## Architecture baseline

Every feature design builds on the same baseline, so the names below recur across all
designs.

### Containers

| Container | Technology | Responsibility |
|-----------|------------|----------------|
| `TuTuMuch Web` | Angular (TypeScript), standalone components, signals, lazy-loaded routes, in `src/frontend` | Responsive single-page application for visitors, members and administrators |
| `TuTuMuch API` | ASP.NET Core Web API (C#), in `src/backend` | REST API over HTTPS; source of truth for every business rule, including the size rules |
| `TuTuMuch Worker` | .NET worker service, in `src/backend` | Background jobs: outbox email dispatch, reservation expiry, order auto-completion, payouts, photo processing and erasure |
| `TuTuMuch database` | SQL Server through EF Core | Members, listings, orders, reviews, outbox |
| `Distributed cache` | Redis | Rate-limit counters, refresh-token families and other state shared across API instances (`L2-060`) |
| `Photo storage` | Object storage behind a CDN | Original uploads (private) and processed, content-addressed photo versions (public through the CDN) |

### External systems

| System | Used for |
|--------|----------|
| `Payment provider` | PCI DSS compliant hosted card fields, payments, refunds, seller payout onboarding and payouts, signed webhooks (`L2-030`, `L2-031`) |
| `Email provider` | Transactional and marketing email (`L2-042`) |
| `Breached-password service` | k-anonymity lookup of known breached passwords (`L2-001`) |

The specific payment provider, email provider and hosting platform are `<TO SUPPLY>`.

### Backend solution layout

The C# namespace prefix is `TuTuMuch`, because a C# identifier cannot start with a digit.

| Project | Layer | Holds |
|---------|-------|-------|
| `TuTuMuch.Api` | API | Controllers, endpoint policies, middleware (problem details, correlation ID, security headers, rate limiting, anti-forgery) |
| `TuTuMuch.Application` | Application | MediatR commands, queries and handlers; FluentValidation validators; DTOs; interfaces for infrastructure ports |
| `TuTuMuch.Domain` | Domain | Entities, value objects, enumerations, domain events and state machines (`Member`, `Listing`, `Order`, `Review`) |
| `TuTuMuch.Infrastructure` | Infrastructure | `TuTuMuchDbContext` (EF Core), repositories, outbox, adapters for the payment provider, email provider, photo storage and cache |
| `TuTuMuch.Worker` | Worker | Hosted background services that run the jobs listed above |

### Frontend layout

The frontend is an Angular workspace (`src/frontend`) laid out as `AGENTS.md` mandates:
the application `projects/tutumuch/` (shell and routes only) and one library per area under
`projects/`: `api` (typed HTTP services, DTOs, auth state, interceptors), `components`
(design-system components and styles) and the lazy-loaded feature libraries `accounts`,
`catalog`, `listings`, `orders`, `reviews` and `privacy`. Each feature library exports its
route table from `public-api.ts`, holds its pages and dialogs under `src/lib/`, and calls
`TuTuMuch API` only through `@tutumuch/api`. Where an older design names `src/frontend/src/app/...`,
read it as the matching library.

### Cross-cutting conventions

- **Sessions.** `TuTuMuch API` issues a 15-minute access token and a rotating 14-day refresh token in `HttpOnly`, `Secure`, `SameSite=Strict` cookies (`L2-008`). The frontend never stores a token in web storage.
- **Authorisation.** Every endpoint applies a policy; resources the caller may not see return HTTP 404 (`L2-010`).
- **Errors.** Every API error is an RFC 9457 problem details response with a stable error code and a correlation ID (`L2-068`).
- **Outbox.** Handlers that must send email or call an external system write an outbox message in the same database transaction as the state change; `TuTuMuch Worker` dispatches it with retry and back-off (`L2-042`).

## Subsystems

| Subsystem | Scope (L1) | Features |
|-----------|------------|----------|
| [accounts](accounts/README.md) | `L1-001`, `L1-002` | sign-up-and-sign-in (registration, email verification, sign-in, sessions, authorisation); still to design: reset-password, lockout, manage-sessions, edit-profile, close-account |
| [listings](listings/) | `L1-003`, `L1-004` | create-listing, upload-listing-photos, edit-and-withdraw-listing, manage-my-listings |
| [catalog](catalog/) | `L1-005`, `L1-006`, `L1-023` | browse-catalog, search-and-filter-catalog, view-listing-detail, request-items |
| [orders](orders/) | `L1-007`, `L1-008` | checkout-and-pay, seller-payouts, ship-order, complete-order, cancel-order, resolve-order-problems, order-history |
| [reviews](reviews/) | `L1-009`, `L1-010` | view-member-profile |
| [notifications](notifications/) | `L1-011` | send-email-notifications, manage-notification-preferences |
| [privacy](privacy/) | `L1-014` | protect-personal-data, manage-cookie-consent |
| [platform](platform/) | `L1-013`, `L1-015` to `L1-018`, `L1-022` | harden-http-and-api, protect-credentials-and-supply-chain, meet-performance-and-scale, responsive-accessible-shell, report-errors-and-observe, keep-available-and-recoverable, page-guidelines-conformance |

## Deferred

Moderation (reporting, the moderation queue, member suspension, the administrator audit log and administrator second factor), review authoring and data export have no detailed design, mocks, specs or estimates at this time. See [ADR 0001](../adr/general/0001-defer-moderation-reviews-and-data-export.md). Administrators still resolve order problems through [resolve-order-problems](orders/resolve-order-problems/README.md), guarded by the `Administrator` policy of `L2-010`.
