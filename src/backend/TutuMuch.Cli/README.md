# TutuMuch.Cli

A developer tool that gives a local Tutu Much install that looks real for a demo. It is for a
developer's own machine and local use only: deployments run `TutuMuch.Migrator`, never this
tool, and CI never runs it against a database
([ADR platform/0010](../../../docs/adr/platform/0010-tutumuch-cli-for-demo-seeding.md),
[design](../../../docs/detailed-designs/platform/seed-demo-data/README.md)).

## Commands

| Command | What it does |
|---------|--------------|
| `reset [--force]` | Drops the database, applies every EF Core migration (the same code as `TutuMuch.Migrator`) and empties the photo store. |
| `seed [--reset] [--force]` | Loads 8 demo members and 48 Active listings, 12 in each of Dresses, Tops, Outerwear and Bottoms, each with 1 to 4 photos. `--reset` runs `reset` first. |
| `help` | Prints usage. |

`reset` (and `seed --reset`) only runs when `DOTNET_ENVIRONMENT` is `Development`, unless
`--force` is passed. It prints the target server and database first. When the environment
is not set it counts as `Production`, so the guard is on by default.

`seed` goes through the same application commands as the website: members register and
verify their email, photos go through the upload pipeline (metadata stripped, WebP and JPEG
versions), listings are created as drafts and then published. Running `seed` again when
the demo is already there changes nothing; `seed --reset` always gives the same fresh demo.

## Settings

| Setting | Notes |
|---------|-------|
| `ConnectionStrings__TutuMuch` | Required, the same setting as `TutuMuch.Migrator` and the API. |
| `DOTNET_ENVIRONMENT` | `Development` allows `reset`. |
| `PhotoStorage__RootPath` | Only if you changed it for the API; the default is the same folder the API uses. |

## Usage

From `src/backend`, with the database settings of the root README:

```bash
dotnet build TutuMuch.Cli
export ConnectionStrings__TutuMuch="Server=(localdb)\MSSQLLocalDB;Database=TutuMuch;Trusted_Connection=True;TrustServerCertificate=True"
export DOTNET_ENVIRONMENT=Development
dotnet TutuMuch.Cli/bin/Debug/net10.0/TutuMuch.Cli.dll seed --reset
```

On PowerShell use `$env:ConnectionStrings__TutuMuch = "..."` and
`$env:DOTNET_ENVIRONMENT = "Development"`. On Windows on Arm, LocalDB is x64 only: run the
built DLL with the x64 host, `& "C:\Program Files\dotnet\x64\dotnet.exe" TutuMuch.Cli\bin\Debug\net10.0\TutuMuch.Cli.dll seed --reset`
(the arm64 host fails with error 193).

## Exit codes

| Code | Meaning |
|------|---------|
| 0 | Success, help, or the demo data already exists |
| 1 | Unknown command or option |
| 2 | `ConnectionStrings__TutuMuch` is not set |
| 3 | `reset` refused: not Development and no `--force` |
| 4 | A seed photo is missing |
| 5 | The database cannot be reached, or a migration or seed step failed |

## Demo accounts

Every demo member uses the password **`TutuMuch-demo-only-2026`**. It is for local use only:
never seed a shared or production database.

| Display name | Email |
|--------------|-------|
| TutuTessa | `tessa@demo.tutumuch.test` |
| TwirlyTara | `tara@demo.tutumuch.test` |
| HandMeDownHana | `hana@demo.tutumuch.test` |
| PuddleJumperPia | `pia@demo.tutumuch.test` |
| RainbowRosa | `rosa@demo.tutumuch.test` |
| LeggingsLily | `lily@demo.tutumuch.test` |
| PartyDressPenny | `penny@demo.tutumuch.test` |
| CozyCoatCleo | `cleo@demo.tutumuch.test` |

Seller ratings show "No reviews yet": no orders, reviews, reports or data exports are seeded.

## Photos

The 24 images in [`SeedPhotos/`](SeedPhotos/), six per category, are original illustrations of
girls' clothing generated for Tutu Much and covered by the repository's licence. Each file and
what it shows is listed in [`SeedPhotos/ATTRIBUTION.md`](SeedPhotos/ATTRIBUTION.md); keep that
file with the images.
