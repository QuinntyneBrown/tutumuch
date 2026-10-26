# Agent instructions for TuTuMuch

TuTuMuch is an open marketplace for new and used girls' (children's) clothing: dresses, tops,
outerwear and bottoms, every one accepted only in girls' sizes. Anyone can join, set up a profile
and sell what their daughter has outgrown; TuTuMuch's owner sells on it as an ordinary member
too. TuTuMuch began as a clone of 12L, so every journey, screen, layout, component (including
the `tl` selector prefix) and rule mirrors 12L; only the domain words and the size rule differ.
The look is the **Giddy design system**: `docs/design-system/tokens/tokens.css` is Giddy's token
file plus a short block of TuTuMuch layout tokens that alias 12L-only names onto Giddy tokens.
Change colour, type, shape or motion in Giddy's tokens, never in components. The product
is defined, before code, by `docs/journeys.md`, `docs/specs/` (L1 and L2 requirements),
`docs/mocks/` (generated HTML mocks), `docs/detailed-designs/` and `docs/estimates/`.
Architecture decisions live in `docs/adr/`.

## Out of scope at this time (CRITICAL)

The following are **deferred and must not be looked at**: do not write specs, user
journeys, mocks, detailed designs, estimates, prompts, code or tests for them, and do
not re-add references to them in other documents. The decision is recorded in
[ADR-0001](docs/adr/general/0001-defer-moderation-reviews-and-data-export.md).

- **Moderation**: reporting listings or members, the moderation queue, removing
  listings by an administrator, member suspension and reinstatement, the administrator
  audit log, and administrator second factor (TOTP) sign-in.
- **Leave review**: writing reviews after an order, review publishing rules and review
  replies. The public rating summary and profile reviews section stay and show
  "No reviews yet".
- **Export personal data**: the JSON data export and its email. Account closure and
  erasure stay.

- **Load and scale tests**: no load-test seeder, k6 or other load scripts, performance
  environment, multi-instance docker-compose, `TuTuMuch.LoadTestSeeder`,
  `TuTuMuch.ScaleTests` or latency-under-load requirement. Statelessness (`L2-060`) is
  checked by an architecture test only. See
  [ADR-0002](docs/adr/general/0002-no-load-or-scale-tests.md).
- **Frontend performance testing**: no Lighthouse or Lighthouse CI, no bundle-size or
  gzip budget script, no lazy-chunk check, no `web-vitals` reporter, no telemetry
  endpoint for Core Web Vitals and no frontend performance requirement. Lazy routes
  stay as a workspace convention, not as a measured budget. See
  [ADR-0003](docs/adr/general/0003-no-frontend-performance-testing.md).

The removed requirement numbers `L2-038`, `L2-039`, `L2-044` to `L2-047`, `L2-055`,
`L2-057` and `L2-058` are retired; never reuse them. `L1-012` no longer exists.

Administrators still resolve order problems (`L2-036`) through the admin shell, guarded
by the `Administrator` role check of `L2-010`. That is the only administrator feature.

## Testing rules (CRITICAL)

- **Backend tests never touch a real database.** Unit, integration and architecture tests
  under `tests/TuTuMuch.*.Tests/` use mocks, fakes or an in-memory substitute (the EF Core
  in-memory or SQLite in-memory provider). No SQL Server, LocalDB, Testcontainers, Docker or
  connection string to a live database in any test, fixture or CI job. The same applies to
  Redis and every other external system: fake the port.
- **Playwright tests never run the backend.** Every e2e test in `src/frontend/e2e/` serves
  the `TuTuMuch API` through `page.route` fixtures or an equivalent fake. No test starts, waits
  for, or calls `TuTuMuch.Api`, `TuTuMuch.Worker` or a database. The frontend under test is
  the built Angular application only.
- These rules are absolute. Do not add an opt-in flag, an "integration" tag or a separate
  CI lane that relaxes them.

## Folder structure (MANDATORY)

This layout is required. Every file you create must land in one of the folders below;
never invent a parallel folder, rename one of these, or put code outside `src/` and
`tests/`. If a design seems to need a folder that is not listed, add it to this section
first and record the change as an ADR.

```
tutumuch/
├── AGENTS.md
├── README.md
├── .github/workflows/          ci.yml runs `npm run verify` on ubuntu-latest
├── docs/
│   ├── journeys.md              user journeys with effort
│   ├── specs/                   L1.md and L2.md requirements
│   ├── mocks/                   generated HTML mocks (_build/ holds the Python sources)
│   ├── design-system/           tokens and components extracted from the mocks
│   ├── detailed-designs/        {subsystem}/{feature}/README.md + diagrams/
│   ├── estimates/               README.md + one file per subsystem
│   ├── prompts/                 ready-to-run implementation prompts, NN-journey.md in build order
│   └── adr/                     {category}/NNNN-title.md
├── src/
│   ├── backend/                 .NET solution, namespace prefix TuTuMuch
│   │   ├── TuTuMuch.Api/         ASP.NET Core Web API: controllers, policies, middleware
│   │   ├── TuTuMuch.Application/ MediatR commands, queries, validators, DTOs, ports
│   │   ├── TuTuMuch.Domain/      entities, value objects, state machines, domain events
│   │   ├── TuTuMuch.Infrastructure/ EF Core DbContext, repositories, outbox, adapters
│   │   ├── TuTuMuch.Worker/      hosted background jobs
│   │   ├── TuTuMuch.Migrator/    applies EF Core migrations
│   │   └── TuTuMuch.Cli/         developer tool: `reset` and `seed` a local demo install
│   │       └── SeedPhotos/      {category}/ demo product photos + ATTRIBUTION.md
│   └── frontend/                Angular workspace (see below)
│       ├── angular.json
│       ├── package.json
│       ├── tsconfig.json        path alias @tutumuch/<library> per library
│       ├── eslint.config.js
│       ├── e2e/                 Playwright: pages/, tests/, fixtures.ts, serve.mjs (static build server)
│       └── projects/
│           ├── tutumuch/         the application (projectType "application")
│           │   ├── src/app/     app.config.ts, app.routes.ts, shells only
│           │   └── public/media/ pitch video, captions, transcript and poster (ADR platform/0011)
│           ├── api/             library: typed HTTP services, DTOs, auth state, interceptors
│           ├── components/      library: design-system components and styles
│           ├── accounts/        library: feature area
│           ├── catalog/         library: feature area
│           ├── listings/        library: feature area
│           ├── orders/          library: feature area (checkout, payouts, admin problems)
│           ├── reviews/         library: feature area (public profile only)
│           ├── marketing/       library: feature area (the /welcome page and pitch video dialog)
│           └── privacy/         library: feature area
└── tests/
    └── TuTuMuch.*.Tests/         unit, integration and architecture tests per backend project
                                 (including TuTuMuch.Cli.Tests)
```

`TuTuMuch.Cli` is a developer tool only: deployments and CI never run it against a database
([ADR platform/0010](docs/adr/platform/0010-tutumuch-cli-for-demo-seeding.md)).

There is no `moderation` library and no `TuTuMuch.Moderation` project.

## Frontend workspace layout (MANDATORY)

The frontend is an Angular workspace organised like
`C:\projects\promise-hub\frontend\projects`: one application project and one Angular
library per area, all registered in `angular.json`. Follow these rules:

- **One application**, `projects/tutumuch/`, with `src/app/` holding only
  `app.config.ts`, `app.routes.ts`, the shell components and `main.ts`/`index.html`/
  `styles.css`. It owns no feature screens.
- **One library per feature area** (`accounts`, `catalog`, `listings`, `orders`,
  `reviews`, `privacy`, `marketing`), each with `ng-package.json`, `package.json`, `README.md`,
  `tsconfig.lib.json` and `src/public-api.ts`. Code lives under `src/lib/` grouped by
  screen (`src/lib/sign-in/`, `src/lib/checkout/`...). The library exports its lazy
  route table (`export { routes } from './lib/<area>.routes';`) and nothing the app does
  not need.
- **`projects/api/`** is the only place that calls `TuTuMuch API`: typed services per area
  (`src/lib/orders/`, `src/lib/listings/`...), DTOs, `provide-api.ts`, base URL,
  interceptors, auth and session state, and shared result types. Feature libraries
  import from it; they never call `HttpClient` directly.
- **`projects/components/`** holds the design-system components extracted from
  `docs/design-system/` (one folder per component under `src/lib/`) plus `src/styles/`
  tokens. Feature libraries compose screens from it and build only screen-specific UI
  locally (see `docs/screen-composition.md` when it exists).
- **Path aliases** in `tsconfig.json`: `@tutumuch/api`, `@tutumuch/components`, `@tutumuch/accounts`,
  and so on, each mapping to that library's `public-api.ts`. Import across libraries only
  through these aliases, never by relative path.
- **Dependency direction**: `tutumuch` → feature libraries → `api` and `components`.
  Feature libraries do not import each other; shared pieces move to `api` or
  `components`.
- Component selector prefix is `tl`. Standalone components, signals and lazy routes
  throughout. Formatting by Prettier, linting by angular-eslint, `npm run build:all`
  compiles every library and the application.

## Components library is mandatory (CRITICAL)

- The `tutumuch` application and every feature library MUST be built from
  `@tutumuch/components` (`src/frontend/projects/components/`). Never hand-write markup, CSS,
  icons, overlays or toasts the library already provides. Use the `tl-*` component,
  `DialogService`, `SheetService` and `ToastService`.
- `docs/screen-composition.md` is binding. Before implementing any page, dialog,
  notification or shell part, read its entry and use exactly the components it lists.
  Build locally only the UI it marks as local.
- If the library lacks something the entry needs, STOP. Update
  `docs/design-system/` and the library first. Do not work around it in the app.
- Keep `docs/screen-composition.md` aligned with the mocks and `docs/design-system/`.

## Working in this repository

- Run `npm run verify` in `src/frontend` before every commit; it is the same command CI runs
  (`L2-071`, [ADR-0002](docs/adr/platform/0002-single-verify-command.md)).
- Change mocks in `docs/mocks/_build/screens_*.py`, then run
  `python docs/mocks/_build/build.py` and
  `python .claude/skills/writing-html-mocks/scripts/check_mocks.py docs/mocks --write`.
  Never hand-edit the generated HTML, `manifest.json`, `index.html` or the coverage
  matrix in `docs/mocks/README.md`.
- Re-render diagrams after editing a `.puml` with
  `python .claude/skills/writing-software-design-documents/scripts/render_puml.py <dir>`.
- Keep `docs/estimates/` consistent with `docs/detailed-designs/`: one subsystem file per
  design folder, and a README whose totals are the sum of the subsystem tables.
- Record any architectural or scope decision as an ADR under `docs/adr/<category>/`.
