<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="docs/design-system/assets/logo-dark.svg">
    <img src="docs/design-system/assets/logo.svg" alt="TuTuMuch" width="240">
  </picture>
</p>

# TuTuMuch

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Frontend: Angular](https://img.shields.io/badge/Frontend-Angular-dd0031.svg)](https://angular.dev)
[![Backend: .NET](https://img.shields.io/badge/Backend-.NET-512bd4.svg)](https://dotnet.microsoft.com)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](CONTRIBUTING.md)
[![Contributor Covenant](https://img.shields.io/badge/Contributor%20Covenant-2.1-4baaaa.svg)](CODE_OF_CONDUCT.md)

TuTuMuch is an open marketplace for new and used **girls' (children's) clothing**: dresses, tops, outerwear and bottoms. Parents and families join, set up a profile, list what their daughters have outgrown and buy what they will wear next.

TuTuMuch is a responsive web application built with [Angular](https://angular.dev) and [ASP.NET Core](https://dotnet.microsoft.com).

> [!NOTE]
> TuTuMuch is in active development. Every journey in `docs/journeys.md` is built end to end; payment, payout, carrier, email and analytics providers are still placeholders. See [Project status](#project-status).

## Contents

- [Overview](#overview)
- [Project status](#project-status)
- [Getting started](#getting-started)
  - [Load demo data](#load-demo-data)
- [Configuration](#configuration)
- [Testing](#testing)
- [API reference](#api-reference)
- [Architecture](#architecture)
- [Repository structure](#repository-structure)
- [Documentation](#documentation)
- [Contributing](#contributing)
- [Security](#security)
- [Support](#support)
- [License](#license)

## Overview

General marketplaces bury girls' clothes among everything else. On TuTuMuch every listing is girls' clothing, so parents only browse what their daughter can wear. The API enforces the size rules whatever the client sends. TuTuMuch's owner also sells on the marketplace as a regular member, with the same rules as everyone else.

| Category                                  | Accepted size | Condition   |
| ----------------------------------------- | ------------- | ----------- |
| Dresses (dresses, skirts, tutus, rompers) | Girls'        | New or used |
| Tops (shirts, t-shirts, sweaters, hoodies) | Girls'        | New or used |
| Outerwear (jackets, coats)                | Girls'        | New or used |
| Bottoms (pants, leggings, shorts)         | Girls'        | New or used |

## Project status

| Area          | Status    | What works today                                                                                                                               |
| ------------- | --------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| Accounts      | Available | Join, email verification, sign in and sign out with session cookies and rotating refresh tokens                                                |
| Listings      | Available | Sell page with drafts, publishing, photo upload and processing, and a daily publish limit                                                      |
| Catalog       | Available | Home and category pages (`/dresses`, `/tops`, `/outerwear`, `/bottoms`), paged newest first, and the listing detail page at `/items/{reference}` |
| Orders        | Available | Checkout, shipping, receipt confirmation, payouts and order problems                                                                           |
| Privacy       | Partial   | Cookie consent; account closure and erasure are planned                                                                                        |
| Reviews       | Available | Public seller profile; ratings show "No reviews yet"                                                                                           |
| Notifications | Available | Transactional email through an outbox, email preferences and one-click unsubscribe                                                             |

Overall progress and effort are tracked in [docs/estimates](docs/estimates/README.md). Moderation, writing reviews and personal data export are deferred by [ADR-0001](docs/adr/general/0001-defer-moderation-reviews-and-data-export.md).

## Getting started

### Prerequisites

- [Git](https://git-scm.com)
- [.NET SDK 10.0.402 or later](https://dotnet.microsoft.com/download) (pinned in `src/backend/global.json`)
- [Node.js 22 LTS](https://nodejs.org) and npm
- [SQL Server Express LocalDB](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb) or another SQL Server instance, to run the API locally

The automated tests don't need a database. They fake every external system.

### Clone and verify

```bash
git clone https://github.com/QuinntyneBrown/tutumuch.git
cd tutumuch/src/frontend
npm ci
npx playwright install chromium
npm run verify
```

`npm run verify` is the same command CI runs. It restores, format-checks, builds and tests the backend, lints the frontend with Prettier and ESLint, builds every Angular library and the application, and runs the Playwright tests. It stops at the first failure.

### Run locally

1. Set the connection string. The API and the migrator read `ConnectionStrings__TuTuMuch`.

   ```powershell
   $env:ConnectionStrings__TuTuMuch = 'Server=(localdb)\MSSQLLocalDB;Database=TuTuMuch;Trusted_Connection=True;TrustServerCertificate=True'
   ```

2. Apply the database migrations.

   ```powershell
   dotnet run --project src/backend/TuTuMuch.Migrator
   ```

3. Start the API on `http://localhost:5045`.

   ```powershell
   dotnet run --project src/backend/TuTuMuch.Api --launch-profile http
   ```

4. In a second terminal, start the Angular app.

   ```powershell
   cd src/frontend
   npm start
   ```

5. Open [http://localhost:4200](http://localhost:4200). The development server proxies `/api` and `/photos` to the API.

To check that the API is up, send `GET http://localhost:5045/api/health`. It returns `200`.

> [!IMPORTANT]
> **Windows on Arm:** LocalDB ships only as x64, so an arm64 process can't load its client and fails with SQL error 193. Build with your usual SDK, then run the built assemblies with the x64 host:
>
> ```powershell
> dotnet build src/backend/TuTuMuch.sln
> & "C:\Program Files\dotnet\x64\dotnet.exe" src/backend/TuTuMuch.Migrator/bin/Debug/net10.0/TuTuMuch.Migrator.dll
> & "C:\Program Files\dotnet\x64\dotnet.exe" src/backend/TuTuMuch.Api/bin/Debug/net10.0/TuTuMuch.Api.dll --urls http://localhost:5045
> ```
>
> This requires the x64 .NET 10 runtime under `C:\Program Files\dotnet\x64`.

### Load demo data

`TuTuMuch.Cli` resets a local database and fills it with 8 demo members and 48 listings with
real product photos in every category, so `/`, `/dresses`, `/tops`, `/outerwear`, `/bottoms`
and the listing pages look real. It is for your own machine only; see
[its README](src/backend/TuTuMuch.Cli/README.md) for the demo accounts and password.

```bash
# from src/backend, with SQL Server LocalDB (or any SQL Server you own)
export ConnectionStrings__TuTuMuch="Server=(localdb)\MSSQLLocalDB;Database=TuTuMuch;Trusted_Connection=True;TrustServerCertificate=True"
export DOTNET_ENVIRONMENT=Development   # reset refuses to run otherwise, unless --force
dotnet build TuTuMuch.Cli
dotnet TuTuMuch.Cli/bin/Debug/net10.0/TuTuMuch.Cli.dll seed --reset
```

Start the API with the same `ConnectionStrings__TuTuMuch` so it reads the seeded database.
On Windows on Arm, LocalDB is x64 only: build with the normal SDK, then run the built DLL
with the x64 host, `"C:\Program Files\dotnet\x64\dotnet.exe" TuTuMuch.Cli\bin\Debug\net10.0\TuTuMuch.Cli.dll seed --reset`
(the arm64 host fails with error 193). Run the API the same way.

## Configuration

The API reads standard ASP.NET Core configuration: `appsettings.json`, `appsettings.{Environment}.json` and environment variables.

| Setting                      | Environment variable          | Description                                                                                                  |
| ---------------------------- | ----------------------------- | ------------------------------------------------------------------------------------------------------------ |
| `ConnectionStrings:TuTuMuch`  | `ConnectionStrings__TuTuMuch`  | SQL Server connection string. Required to run the API and the migrator.                                      |
| `Auth:SigningKey`            | `Auth__SigningKey`            | Key that signs access tokens. The development value in `appsettings.Development.json` is for local use only. |
| `App:BaseUrl`                | `App__BaseUrl`                | Public URL of the frontend, used in links such as email verification.                                        |
| `Selling:PlatformFeePercent` | `Selling__PlatformFeePercent` | Fee shown to sellers when they price an item.                                                                |
| `Selling:PublishesPerDay`    | `Selling__PublishesPerDay`    | Maximum number of listings a member can publish per day.                                                     |

## Testing

Run each check from `src/frontend`.

| Command                  | Runs                                                 |
| ------------------------ | ---------------------------------------------------- |
| `npm run verify`         | Every check below, in order                          |
| `npm run backend:verify` | Backend restore, format check, build and xUnit tests |
| `npm run lint`           | Prettier check and ESLint                            |
| `npm run build:all`      | Every library, then the application                  |
| `npm run test:e2e`       | Playwright (Chromium) against the production build   |

Backend tests use in-memory substitutes and never connect to a real database. Playwright tests serve the API through route fixtures and never start the backend. See [AGENTS.md](AGENTS.md#testing-rules-critical) for the full rules.

## API reference

Endpoints are served under `/api`, except processed photos under `/photos`. Endpoints marked _member_ need a signed-in session; listing and photo upload endpoints also need a verified email.

| Method | Path                                | Description                                            |
| ------ | ----------------------------------- | ------------------------------------------------------ |
| `GET`  | `/api/health`                       | Liveness check                                         |
| `POST` | `/api/auth/register`                | Create an account                                      |
| `POST` | `/api/auth/verify-email`            | Confirm an email address                               |
| `POST` | `/api/auth/verification-email`      | Resend the verification email                          |
| `POST` | `/api/auth/sign-in`                 | Start a session                                        |
| `POST` | `/api/auth/refresh`                 | Rotate the refresh token                               |
| `POST` | `/api/auth/sign-out`                | End the session                                        |
| `GET`  | `/api/auth/me`                      | Current member _(member)_                              |
| `GET`  | `/api/categories`                   | Categories and their accepted sizes                    |
| `GET`  | `/api/selling/options`              | Brands, conditions and fee for the sell page           |
| `POST` | `/api/listings`                     | Create a draft listing _(member)_                      |
| `PUT`  | `/api/listings/{reference}`         | Update a draft _(member)_                              |
| `POST` | `/api/listings/{reference}/publish` | Publish a draft _(member)_                             |
| `GET`  | `/api/me/listings/{reference}`      | One of your own listings _(member)_                    |
| `POST` | `/api/photo-uploads`                | Upload a listing photo _(member)_                      |
| `GET`  | `/api/photo-uploads/{id}`           | Photo upload status _(member)_                         |
| `GET`  | `/api/catalog/listings`             | Active and reserved listings, newest first, paged      |
| `GET`  | `/api/catalog/listings/{reference}` | Listing detail with photos, seller and viewer relation |
| `GET`  | `/photos/{hash}/{file}`             | Processed photo files                                  |

Detailed contracts and acceptance criteria are in [docs/specs/L2.md](docs/specs/L2.md).

## Architecture

| Layer          | Technology                                   | Responsibility                                                          |
| -------------- | -------------------------------------------- | ----------------------------------------------------------------------- |
| Frontend       | Angular 22, standalone components, signals   | Single-page app: one application shell and one library per feature area |
| API            | ASP.NET Core 10 Web API                      | Controllers, authorization policies, middleware                         |
| Application    | MediatR                                      | Commands, queries, validators, DTOs and ports                           |
| Domain         | C#                                           | Entities, value objects, listing state machine and size rules           |
| Infrastructure | Entity Framework Core, SQL Server, SkiaSharp | Persistence, repositories, outbox and photo processing                  |
| Worker         | .NET hosted service                          | Background jobs                                                         |

Architecture decisions are recorded in [docs/adr](docs/adr).

## Repository structure

```text
tutumuch/
├── .github/                 CI workflow, issue and pull request templates
├── docs/                    Journeys, specs, mocks, design system, designs, estimates, ADRs
├── src/
│   ├── backend/             .NET solution: Api, Application, Domain, Infrastructure, Worker, Migrator, Cli
│   └── frontend/            Angular workspace
│       ├── e2e/             Playwright tests and fixtures
│       └── projects/        tutumuch app; api, components, accounts, catalog, listings,
│                            orders, reviews and privacy libraries
└── tests/                   xUnit test project per backend project
```

## Documentation

| Document                                       | Contents                                        |
| ---------------------------------------------- | ----------------------------------------------- |
| [docs/journeys.md](docs/journeys.md)           | User journeys and their effort                  |
| [docs/specs](docs/specs)                       | L1 and L2 requirements with acceptance criteria |
| [docs/mocks](docs/mocks)                       | HTML mocks for every screen and state           |
| [docs/design-system](docs/design-system)       | Tokens and components                           |
| [docs/detailed-designs](docs/detailed-designs) | Per-feature designs and diagrams                |
| [docs/estimates](docs/estimates/README.md)     | Effort, progress and roadmap                    |
| [AGENTS.md](AGENTS.md)                         | Working agreement for AI coding agents          |
| [CHANGELOG.md](CHANGELOG.md)                   | Notable changes                                 |

## Contributing

Contributions are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) before you open an issue or a pull request, and run `npm run verify` before every commit.

This project has adopted the [Contributor Covenant](CODE_OF_CONDUCT.md). By participating, you're expected to uphold it.

## Security

Don't report security vulnerabilities through public issues. See [SECURITY.md](SECURITY.md) for how to report them privately.

## Support

See [SUPPORT.md](SUPPORT.md) for how to file issues and get help.

## License

TuTuMuch is licensed under the [MIT License](LICENSE). The demo photos in
`src/backend/TuTuMuch.Cli/SeedPhotos/` are not: they keep their CC BY 2.0 licence, with authors
and sources in [ATTRIBUTION.md](src/backend/TuTuMuch.Cli/SeedPhotos/ATTRIBUTION.md).
