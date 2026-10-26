# Feature: Create listing

## Goal

A verified member opens `/sell`, describes an item in one of the accepted categories, and publishes it. The server sets the size (men's US 12 or men's L) whatever the client sends; drafts save; publishing is limited to 20 a day. This is the first journey that produces real data, so every later journey reads what it writes.

Order: prompt 01 of 18 (see `docs/prompts/README.md`). Effort in `docs/journeys.md`: 4 days.
Detailed design: `docs/detailed-designs/listings/create-listing/README.md`. Requirements: L2-003, L2-011, L2-012, L2-013, L2-014, L2-015, L2-016, L2-018, L2-019, L2-031, L2-051.
Unchanged: every journey, screen and design outside this feature.

## Prerequisite inside this run: minimum member authentication

No accounts design or journey exists yet (`docs/detailed-designs/accounts/` is missing), but selling needs a
signed-in, verified member. Build only the minimum, as its own first slices:

- A new design `docs/detailed-designs/accounts/sign-up-and-sign-in/` covering `L2-001` (registration),
  `L2-002` (email verification), `L2-006` (sign-in), `L2-008` (sessions) and `L2-010` (authorisation), using
  the mocks `pages/register`, `pages/verify-email` and `pages/sign-in`.
- `AuthState` in `@tutumuch/api`, the member state of `tm-top-bar`, and the `VerifiedMember` / `CanSell` policies.
- Defer password reset, lockout, manage sessions, edit profile and close account; list them in the design's
  open questions.

## Finish the whole feature in one run. Do not stop. (CRITICAL)

- You are authorised to take this feature all the way: mocks check, design system, requirements, detailed
  design, estimate, incremental implementation, green tests and a PR. Go through every phase and every slice
  in one continuous run. Do not pause between phases to report, ask for approval, or wait for "continue".
- Do not end your turn until the "Done means" list at the bottom is fully true. A partial result, a plan, or
  "the next steps would be..." is not done.
- Where `AGENTS.md` says "stop and ask", treat this prompt as the confirmation in advance for everything in
  scope here. Choose the simplest option that fits the existing architecture, record it (an ADR or the
  detailed design's open-questions section), and keep going.
- If the components library lacks something, that is not a reason to stop. Update the design and the library
  first, as the rules require, then go on.
- When a test, build or command fails, find the root cause and fix it, then go on. Never weaken a test to make
  it pass, and never skip a step quietly.
- Only stop for a real blocker you cannot resolve yourself, such as missing credentials, or an action outside
  this repo that can't be undone, such as production deploys or data changes. If that happens, finish
  everything else first, then report the blocker clearly.
- Keep track of progress (a todo list), so that nothing gets left out after context is summarised.

## Rules (read first, binding)

- Follow `AGENTS.md` exactly, especially:
  - **Out of scope (CRITICAL).** No moderation, review authoring, data export, load or scale tests.
  - **Testing rules (CRITICAL).** Backend tests never touch a real database or external system: use fakes or
    the EF Core in-memory or SQLite in-memory provider. Playwright never runs the backend: serve every
    `Tutu Much API` call through `page.route` fixtures (`src/frontend/e2e/fixtures.ts`).
  - **Folder structure and frontend workspace layout (MANDATORY).** Pages and dialogs live in
    `src/frontend/projects/listings/src/lib/`; HTTP calls only through
    `@tutumuch/api`; shared UI only in `@tutumuch/components`.
  - **Scope lock and zero-waste engineering.** Build only what is described here. If something seems to need
    more scope, leave it out, note it as deferred, and continue. Do not stop.
  - **The components library is mandatory.** Build all UI from `tm-*` components in `@tutumuch/components`;
    follow `docs/screen-composition.md`.
  - **ATDD in small slices** with Playwright page objects (`src/frontend/e2e/pages/`), Chromium only, and xUnit
    tests in `tests/TutuMuch.*.Tests/`. The only architecture test is the existing `StatelessApiTests`.
  - No single-file components: each component gets `.ts`, `.html` and `.css`.
- Work through the phases below **in order**, using the repo skill each phase names.
- Copy the house style of each folder. Read the existing neighbours before you write anything new.
- Run `npm run verify` in `src/frontend` before every commit.
- Another session may be editing docs in this repo at the same time. Fetch and rebase before you push.

## Phase 1: Mocks (`writing-html-mocks`)
- The mocks already exist: `pages/sell` (default, filled, invalid, submitting, draft-saved, success, unverified, limited, error) and `dialogs/verify-email-prompt`. Check every state, light and dark, at phone, tablet and desktop
  widths. Add a missing state only if the design or requirements need it, in `docs/mocks/_build/screens_*.py`,
  then run `python docs/mocks/_build/build.py` and
  `python .claude/skills/writing-html-mocks/scripts/check_mocks.py docs/mocks --write`.

## Phase 2: Design system (`extracting-design-systems`)
- Add a page in `docs/design-system/components/` for every component this feature needs that the catalogue
  lacks, and the matching `tm-*` component in `src/frontend/projects/components/`. Update tokens only if new
  values are needed.
- Compose this feature's screens in `docs/screen-composition.md` (`writing-screen-composition`), replacing
  "not yet composed".

## Phase 3: Requirements (`engineering-requirements`)
- The requirements exist: L2-003, L2-011, L2-012, L2-013, L2-014, L2-015, L2-016, L2-018, L2-019, L2-031, L2-051. Tighten their Given/When/Then criteria only where the design or the
  implementation shows a gap. Any new requirement takes the next free number after the highest in
  `docs/specs/L2.md` (L2-071 at the time of writing).

## Phase 4: Detailed design (`writing-software-design-documents`)
- Update `docs/detailed-designs/listings/create-listing/README.md` and its diagrams so paths match the
  `AGENTS.md` layout (feature library `@tutumuch/listings`, API services in `@tutumuch/api`) and the testing rules.
  Record real decisions as ADRs in `docs/adr/` (`recording-architecture-decisions`).
- Touch related designs only where needed: `platform/harden-http-and-api` (rate limiting for the 20-a-day limit).

## Phase 5: Estimate update (`estimating-designs`)
- Keep `docs/estimates/listings.md` and `docs/estimates/README.md` in step: in progress while you work, the
  true percent complete when done.

## Phase 6: Incremental implementation (`implementing-incrementally`, ATDD)
- Plan thin vertical slices, for example:
  1. Register, verify and sign in a member (API with fakes for email; Playwright with fixtures).
  2. Category and size catalogue: `GET /api/categories` and the server-side size rule (`L2-011` to `L2-014`).
  3. Save a draft from `/sell`.
  4. Publish a listing, with validation errors shown through `tm-form-field` and the error summary.
  5. Unverified and daily-limit states.
- For each slice:
  - Write the Given-When-Then criteria.
  - Write the acceptance test first: Playwright in `src/frontend/e2e/tests/` with a `Traces to: L2-xxx` header
    and page objects in `src/frontend/e2e/pages/`, or xUnit in `tests/TutuMuch.*.Tests/`.
  - Run it and show that it fails for the expected reason.
  - Implement the minimum that makes it pass, and refactor while the tests are green.
  - Run `npm run verify`, then commit.
- Use Node 22 LTS and the .NET 10 SDK, as pinned in the repo.
- When all slices are green, update the estimate to its true percent complete and keep the mocks, design
  system, screen composition and designs aligned with what was built.

## Deferred (do not build)
- Photos (next prompt), editing and withdrawing, my-listings, payouts onboarding prompts beyond what `L2-031` requires on this page.

## Done means
- A new member can join, verify, sign in, open `/sell`, save a draft and publish a listing whose size the server sets; unverified members and members over the daily limit see the mocked states.
- All the new and existing xUnit and Playwright (Chromium) tests pass, and `npm run verify` exits 0.
- The docs (mocks, design system, screen composition, L1/L2, detailed design, ADRs, estimates) agree with the
  code.
- The work is on a branch with a PR to `main`, and the checks are green. Fetch and rebase onto `main` first,
  because another session may have pushed to it.
- Only then end the run, with a short summary of what was built, the decisions made along the way, and
  anything deferred.
