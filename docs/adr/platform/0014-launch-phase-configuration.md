# ADR-0014: Launch Phases Chosen by One Configuration Setting

**Date:** 2026-10-27
**Category:** platform
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

TuTuMuch opens in three steps. First the owner sells a small, hand-picked catalog alone, with
no category menus. Then the owner fills all six categories, still alone. Finally every verified
member may sell, which is today's behaviour. The owner wants to move between these steps by
changing configuration and restarting, with no code change and no data migration.

The owner already holds the `Administrator` role (`L2-010`, `L2-036`); every other seller rule
(`L2-015` to `L2-021`, the daily publish limit of [ADR-0006](0006-daily-publish-limit-counted-in-database.md))
stays as it is.

| Phase | Who can list | Category menus | Catalog size |
| --- | --- | --- | --- |
| 1 (default) | `Administrator` only | Hidden; one flat catalog at `/` | Fewer than 40 listings |
| 2 | `Administrator` only | All six | No limit |
| 3 | Any verified member | All six | No limit |

## Decision

**The setting.** Section `Launch`, key `Phase`, an integer 1, 2 or 3 (`Launch__Phase` as an
environment variable). A missing value means Phase 1, so a fresh install opens in Phase 1. Any
other value fails API startup with `Launch:Phase must be 1, 2 or 3`. A failed start is caught by
the deploy's health smoke test (`L2-084`) and the previous containers keep running until the
operator fixes the value; falling back to Phase 1 with a warning would instead close selling
for every member silently, which is the larger surprise.

**The Phase 1 limit.** Key `Launch:PhaseOneProductLimit`, default 40, validated as 1 or more. The
catalog must hold **fewer than** the limit: the count is every row of `Listings` in any status
(Draft, Active, Reserved, Sold, Removed). A draft deleted by the clean-up job no longer exists, so
it no longer counts. In Phase 1 the API refuses with 409 `PRODUCT_LIMIT_REACHED`:
- saving a new draft when the count is already limit − 1 or more (the new one would reach the limit);
- publishing a draft when the count is the limit or more (only possible after a move back from Phase 2 or 3).

The count is one query on `ITuTuMuchDbContext` in the create and publish handlers, the same
approach as the daily publish limit. Two saves racing at the 39th listing could both pass;
accepted, because only the owner sells in Phase 1.

**Who may sell.** The `CanSell` policy gains a `SellingOpenRequirement`: in Phases 1 and 2 the
caller must hold the `Administrator` role, read from TuTuMuch database like `AdministratorHandler`.
Everyone else gets 403 `SELLING_NOT_OPEN` ("Selling opens soon"), checked before
`EMAIL_NOT_VERIFIED`. The policy already guards every listings and photo-upload endpoint, so
create, save, edit, publish, withdraw, relist, My listings and photo uploads all follow. Buying,
checkout, orders, payouts, profiles, sign-up and sign-in are unchanged.

**How the owner lists.** With the existing sell, edit and My listings screens, signed in with an
`Administrator` account. There is no separate admin product screen.

**How the frontend learns the phase.** `GET /api/launch`, anonymous, answers
`{ "phase": 1, "categoryMenusVisible": false, "memberSellingOpen": false }` with
`Cache-Control: public, max-age=60`. `GET /api/auth/me` gains `isAdministrator`. The app loads
`/api/launch` once before the first render (an app initializer); if the call fails it assumes
Phase 1, the most closed state. The frontend never reads server configuration directly.

**How operators change it.** Set `Launch__Phase` in the `api` service's `environment` in
`deploy/docker-compose.yml` (default `1`, overridable from `/opt/tutumuch/.env`) and run
`docker compose up -d api`, as described in `docs/deployment.md`. No data changes.

**The developer seed.** `TuTuMuch.Cli seed` always seeds the full demo for Phase 3 (it pins
`Launch:Phase` to 3 for its own run), because the demo holds 48 to 80 listings, and says so in its
output. The API may then run in any phase over that data.

## Options Considered

### Option 1: A public launch endpoint read at app start (chosen)
- **Pros:** One setting for API and SPA; the phase changes with a restart of `api` only; the web
  image stays the same in every phase and on the UAT host; e2e tests fake it with `page.route`.
- **Cons:** One small request before the first render.

### Option 2: Bake the phase into the web image at build time
- **Pros:** No request.
- **Cons:** Two settings that can disagree (API refuses selling while the SPA offers it); a phase
  change needs a rebuild and redeploy of `web`, not just a restart; the UAT host and production
  share one image ([ADR-0013](0013-uat-host-in-the-production-web-image.md)).

### Option 3: Invalid phase falls back to 1 with a warning
- **Pros:** The API always starts.
- **Cons:** A typo silently closes selling for members; the failure is only in a log.

### Option 4: A separate admin product screen
- **Pros:** Owner tools can differ from member tools.
- **Cons:** A second form to build and keep aligned with `L2-015` to `L2-021`; the existing screens already do the job.

## Consequences

### Positive
- No code change and no migration to move between phases; going back is the same setting.
- Existing data is untouched in every phase: listings of members created in Phase 3 stay in the
  catalog if the site returns to Phase 1 or 2; only new selling actions are refused.

### Negative
- A phase change needs a restart; there is no live switch (deferred).
- Tests that assume today's behaviour must state Phase 3 (the API test factory and the e2e
  fixtures default to Phase 3 for that reason; new tests set Phases 1 and 2 explicitly).

### Risks
- An operator forgets the variable and production opens in Phase 1. That is the intended default.

## References

- `docs/specs/L1.md` (`L1-021`), `docs/specs/L2.md` (`L2-089` to `L2-095`)
- [launch-phases](../../detailed-designs/platform/launch-phases/README.md)
- [ADR-0006](0006-daily-publish-limit-counted-in-database.md), [ADR-0012](0012-low-cost-single-vm-continuous-deployment.md)
- `docs/deployment.md`
