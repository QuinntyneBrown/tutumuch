# Feature: Showcase listings and item requests

## Goal

The owner of TuTuMuch (a member with the `Administrator` role, `L2-010`) can post **showcase listings**. A
showcase listing looks like any Active listing in the catalog (`L2-020` to `L2-024`) and on the listing detail
page (`docs/detailed-designs/catalog/view-listing-detail/`). It shows a price and a "Buy now" action.

When a shopper presses "Buy now" on a showcase listing, checkout does not start and no payment details are asked
for. Instead the shopper sees a "Sold" indicator (the same "Sold" label `L2-024` criterion 2 uses) with a short
message that this item has already found a home, and a "Request this item" action.

Pressing "Request this item":
- If the shopper is signed out, they are prompted to create an account or sign in (the existing flows in
  `docs/detailed-designs/accounts/sign-up-and-sign-in/` and `sign-in-with-google/`). After they finish, they
  return to the listing and the request form opens.
- If signed in, a short request form opens (for example: an optional note, size wanted). Submitting it records
  an item request against the listing and confirms with a toast.
- The owner can see the requests (a simple list in the admin shell is enough).

What stays unchanged: ordinary listings, checkout and payment (`L2-029` "Each item sold once" and the orders
designs), the launch-phase rules (`L2-089` to `L2-095`) and every member's selling flow. Showcase listings must
work in every launch phase, including Phase 1.

## Finish the whole feature in one run. Do not stop. (CRITICAL)

- You are authorised to take this feature all the way: requirements, mocks, design system and screen
  composition, detailed design and ADR, estimates, test-first implementation, green tests and a PR. Go through
  every phase and every slice in one continuous run. Do not pause between phases to report, ask for approval,
  or wait for "continue".
- Do not end your turn until the "Done means" list at the bottom is fully true. A partial result, a plan, or
  "the next steps would be..." is not done.
- Where the repo's instructions say "stop and ask", treat this prompt as the confirmation in advance for
  everything in scope here. Choose the simplest option that fits the existing architecture, record it (an ADR
  or the design's open-questions section), and keep going.
- If a shared library, component or design asset lacks something, that is not a reason to stop. Update it
  first, as the repo's rules require, then go on.
- When a test, build or command fails, find the root cause and fix it, then go on. Never weaken a test to make
  it pass, and never skip a step quietly.
- Only stop for a real blocker you cannot resolve yourself, such as missing credentials, or an action outside
  this repo that can't be undone, such as production deploys or data changes. If that happens, finish
  everything else first, then report the blocker clearly.
- Keep track of progress (a todo list), so that nothing gets left out after context is summarised.

## Rules (read first, binding)

- Follow `AGENTS.md` exactly, especially:
  - Out of scope (CRITICAL): no moderation, leave review, data export, load/scale tests or frontend performance
    testing. Never reuse retired numbers `L2-038`, `L2-039`, `L2-044` to `L2-047`, `L2-055`, `L2-057`,
    `L2-058`, `L2-014` or `L1-012`. The admin shell may gain the request list only; add no other admin feature
    beyond what this prompt asks.
  - Backend tests never touch a real database (EF Core in-memory or SQLite in-memory, fakes for every port).
    Playwright tests never run the backend; serve the API through `page.route` fixtures in
    `src/frontend/e2e/`.
  - Folder structure and frontend workspace layout are mandatory. API calls only in `projects/api/`. Feature
    libraries do not import each other. Selector prefix `tl`, standalone components, signals, lazy routes.
  - Components library is mandatory: build screens from `@tutumuch/components`, use `DialogService`,
    `SheetService` and `ToastService`, and follow `docs/screen-composition.md`. If the library lacks
    something, update `docs/design-system/` and the library first.
  - Colour, type, shape and motion change only in Giddy's tokens (`docs/design-system/tokens/tokens.css`).
  - Every page follows `docs/page-guidelines.md` (`L1-022`, `L2-096`, ADR general/0009).
  - Mocks change only in `docs/mocks/_build/screens_*.py`; never hand-edit generated HTML.
  - Run `npm run verify` in `src/frontend` before every commit.
- Work through the phases below **in order**, using the repo skill each phase names.
- Copy the house style of each folder. Read the existing neighbours before you write anything new.

## Phase 1: Requirements (`engineering-requirements`)

- Add `L1-023` to `docs/specs/L1.md` for showcase listings and item requests.
- Add new L2 requirements to `docs/specs/L2.md` starting at `L2-097`, with Given-When-Then criteria. Suggested
  split (adjust as needed):
  - `L2-097` Showcase listing: only an Administrator can mark a listing as showcase; it shows in the catalog and
    detail page exactly like an Active listing; it is never reserved, sold or paid for.
  - `L2-098` Buy now on a showcase listing: the checkout endpoint refuses it (HTTP 409 with a problem type the
    web app recognises), and the web app shows the "Sold" indicator and "Request this item" without collecting
    any payment or address data.
  - `L2-099` Request this item: signed-out shoppers are sent to sign up / sign in and returned to the listing
    with the request form open (return URL validated as same-site); signed-in members submit one request per
    listing (a repeat updates it); note length limits; rate limit consistent with existing ones.
  - `L2-100` Owner sees requests: Administrator-only list of requests (member display name, listing, note,
    date), guarded by the `L2-010` role check.
- Cross-reference `L2-010`, `L2-024`, `L2-029`, `L2-089` to `L2-095` instead of copying them. Cover privacy: a
  request exposes the member's email to the Administrator only, and account closure and erasure must remove or
  anonymise their requests (`docs/detailed-designs/privacy/protect-personal-data/`).
- Add or update the journey in `docs/journeys.md` with effort.

## Phase 2: Mocks (`writing-html-mocks`)

- Edit `docs/mocks/_build/screens_catalog.py` (listing detail with the Sold indicator and "Request this item"),
  `screens_accounts.py` (sign-in / sign-up arriving from a request, with the return context) and
  `screens_orders.py` or `screens_system.py` (admin requests list), following their neighbours.
- States: default, Sold-after-Buy-now, request form (empty, invalid, busy, success toast, error), signed-out
  prompt, already requested, admin list (empty, loading, error, populated). Light and dark, responsive,
  keyboard-accessible.
- Run `python docs/mocks/_build/build.py` and
  `python .claude/skills/writing-html-mocks/scripts/check_mocks.py docs/mocks --write`.

## Phase 3: Design system and screen composition (`extracting-design-systems`, `writing-screen-composition`)

- Add any new component or variant to `docs/design-system/` and `src/frontend/projects/components/` first (for
  example a sold notice with an action, if the existing badge and banner do not cover it).
- Update `docs/screen-composition.md` with entries for every new dialog, state and the admin list.

## Phase 4: Detailed design and ADR (`writing-software-design-documents`, `recording-architecture-decisions`)

- Add `docs/detailed-designs/catalog/request-showcase-item/README.md` with diagrams (or the subsystem that fits
  better after reading `docs/detailed-designs/README.md`), and update `view-listing-detail/` and
  `orders/checkout-and-pay/` where showcase changes their behaviour. Render diagrams with
  `python .claude/skills/writing-software-design-documents/scripts/render_puml.py <dir>`.
- Record the decision as an ADR, for example `docs/adr/catalog/0004-showcase-listings-and-item-requests.md`:
  how a listing is flagged as showcase (flag on the listing vs. a status), why it never enters the orders
  state machine, and how the return-after-sign-in works.

## Phase 5: Estimates (`estimating-designs`)

- Add the new design to the matching file in `docs/estimates/` and keep the README totals the sum of the
  subsystem tables.

## Phase 6: Incremental implementation (`implementing-incrementally`, test-first)

- Plan thin vertical slices, for example:
  1. Domain and persistence: showcase flag on the listing, `ItemRequest` entity, EF Core migration via
     `TuTuMuch.Migrator` conventions; Administrator-only command to post or mark a showcase listing.
  2. Checkout refusal: the checkout command rejects showcase listings with HTTP 409 and a distinct problem
     type; nothing is reserved (`L2-029` tests still pass).
  3. Listing detail UI in `projects/catalog/`: "Buy now" on a showcase listing shows the Sold indicator and
     "Request this item".
  4. Request flow: signed-out redirect to sign up / sign in with a safe return URL in `projects/accounts/`,
     then the request form; `POST` endpoint and typed service in `projects/api/`.
  5. Admin requests list in the admin shell (`projects/orders/` holds admin problems today; follow that) with
     its API query.
  6. Privacy: erasure on account closure covers item requests. Seed a showcase listing in `TuTuMuch.Cli`
     `seed` if the seeder has a natural place for it.
- For each slice:
  - Write the Given-When-Then criteria.
  - Write the acceptance test: backend tests in `tests/TuTuMuch.*.Tests/`, e2e in
    `src/frontend/e2e/tests/` (for example `request-showcase-item.spec.ts`) with page objects in
    `src/frontend/e2e/pages/` and `page.route` fixtures.
  - Run it and show that it fails for the expected reason.
  - Implement the minimum that makes it pass, and refactor while the tests are green.
  - Run `dotnet test` and `npm run verify` in `src/frontend`, then commit.
- When all slices are green, bring `docs/estimates/` to its true percent complete and keep the requirements,
  mocks, screen composition and designs aligned with what was built.

## Deferred (do not build)

- Notifying members by email when a showcase item becomes available, or the owner replying to a request.
- Requests on ordinary member listings.
- Anything in the AGENTS.md out-of-scope list.

## Done means

- The owner can post a showcase listing that looks available; "Buy now" on it shows "Sold" and "Request this
  item" without starting checkout; a signed-out shopper is sent to create an account or sign in and comes back
  to the request form; a signed-in member can submit a request; the owner sees it in the admin shell.
- All the new and existing tests pass: `dotnet test` and `npm run verify` in `src/frontend`.
- Requirements, journeys, mocks, design system, screen composition, detailed designs, ADR and estimates agree
  with the code.
- The work is on a branch (for example `feature/showcase-listings-and-item-requests`) with a PR to `main`.
  Fetch and rebase onto `main` first, because another session may have pushed to it.
- Only then end the run, with a short summary of what was built, the decisions made along the way, and
  anything deferred.
