# TuTuMuch.Cli

A developer tool that gives a local TuTuMuch install that looks real for a demo. It is for a
developer's own machine and local use only: deployments run `TuTuMuch.Migrator`, never this
tool, and CI never runs it against a database
([ADR platform/0010](../../../docs/adr/platform/0010-tutumuch-cli-for-demo-seeding.md),
[design](../../../docs/detailed-designs/platform/seed-demo-data/README.md)).

## Commands

| Command | What it does |
|---------|--------------|
| `reset [--force]` | Drops the database, applies every EF Core migration (the same code as `TuTuMuch.Migrator`) and empties the photo store. |
| `seed [--reset] [--force]` | Loads 8 demo members and 72 Active listings, 12 in each of Clothing, Shoes, Dance, Sports, Cameras and tech, and Toys and crafts, each with 1 to 4 photos from its category's folder. `--reset` runs `reset` first. |
| `help` | Prints usage. |

`reset` (and `seed --reset`) only runs when `DOTNET_ENVIRONMENT` is `Development`, unless
`--force` is passed. It prints the target server and database first. When the environment
is not set it counts as `Production`, so the guard is on by default.

`seed` goes through the same application commands as the website: members register and
verify their email, photos go through the upload pipeline (metadata stripped, WebP and JPEG
versions), listings are created as drafts and then published. Running `seed` again when
the demo is already there changes nothing; `seed --reset` always gives the same fresh demo.
The demo targets launch phase 3 (every member sells): `seed` ignores `Launch__Phase` and says
so in its output; the API may run in any phase over the demo
([ADR platform/0014](../../../docs/adr/platform/0014-launch-phase-configuration.md)).

## Settings

| Setting | Notes |
|---------|-------|
| `ConnectionStrings__TuTuMuch` | Required, the same setting as `TuTuMuch.Migrator` and the API. |
| `DOTNET_ENVIRONMENT` | `Development` allows `reset`. |
| `PhotoStorage__RootPath` | Only if you changed it for the API; the default is the same folder the API uses. |

## Usage

From `src/backend`, with the database settings of the root README:

```bash
dotnet build TuTuMuch.Cli
export ConnectionStrings__TuTuMuch="Server=(localdb)\MSSQLLocalDB;Database=TuTuMuch;Trusted_Connection=True;TrustServerCertificate=True"
export DOTNET_ENVIRONMENT=Development
dotnet TuTuMuch.Cli/bin/Debug/net10.0/TuTuMuch.Cli.dll seed --reset
```

On PowerShell use `$env:ConnectionStrings__TuTuMuch = "..."` and
`$env:DOTNET_ENVIRONMENT = "Development"`. On Windows on Arm, LocalDB is x64 only: run the
built DLL with the x64 host, `& "C:\Program Files\dotnet\x64\dotnet.exe" TuTuMuch.Cli\bin\Debug\net10.0\TuTuMuch.Cli.dll seed --reset`
(the arm64 host fails with error 193).

## Exit codes

| Code | Meaning |
|------|---------|
| 0 | Success, help, or the demo data already exists |
| 1 | Unknown command or option |
| 2 | `ConnectionStrings__TuTuMuch` is not set |
| 3 | `reset` refused: not Development and no `--force` |
| 4 | A seed photo is missing |
| 5 | The database cannot be reached, or a migration or seed step failed |

## Demo accounts

Every demo member uses the password **`TuTuMuch-demo-only-2026`**. It is for local use only:
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

The 54 images in [`SeedPhotos/`](SeedPhotos/) are original illustrations of girls' items
generated for TuTuMuch and covered by the repository's licence: 24 in `Clothing/` and 6 in each of
`Shoes/`, `Dance/`, `Sports/`, `Cameras and tech/` and `Toys and crafts/`. A folder is named
exactly as its category, and every demo listing uses photos from its own category's folder. Each file and
what it shows is listed in [`SeedPhotos/ATTRIBUTION.md`](SeedPhotos/ATTRIBUTION.md); keep that
file with the images.
