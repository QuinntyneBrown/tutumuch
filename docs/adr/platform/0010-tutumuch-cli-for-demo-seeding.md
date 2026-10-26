# ADR-0010: TuTuMuch.Cli for Demo Seeding and Database Reset

**Date:** 2026-10-26
**Category:** platform
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

A developer who wants to demo TuTuMuch has to register members, verify them, upload photos and
publish listings by hand before any catalog page shows anything. There is no way to wipe a
local install either: the only console tool, `TuTuMuch.Migrator`, applies pending migrations
and is the one thing a deployment runs, so it must not learn to drop databases.

Demo listings need photos. They must be real product photographs (dresses, shirts, jackets,
leggings), free to redistribute in this MIT repository, committed so seeding works offline,
and processed by the same pipeline as uploads (`L2-017`, `L2-059`,
[ADR listings/0001](../listings/0001-process-photos-in-the-upload-request.md),
[ADR-0009](0009-skiasharp-for-photo-processing.md)). The pipeline refuses photos under
800 px on the shortest side.

`AGENTS.md` fixes the folder layout, so a new project needs the layout changed first.

## Decision

### A separate developer tool project

Add `src/backend/TuTuMuch.Cli/` (console, `TuTuMuch.Cli.dll`) and `tests/TuTuMuch.Cli.Tests/`
to `TuTuMuch.sln` and to the `AGENTS.md` folder tree. It has two commands:

- `reset [--force]` drops the database, applies every migration and empties the photo store
  (`PhotoStorage:RootPath`).
- `seed [--reset] [--force]` loads the demo catalogue; `--reset` runs `reset` first.

`TuTuMuch.Migrator` stays the only tool deployments run. The CLI references it and calls
the same `MigratorApp.ApplyMigrations` method that `MigratorApp.Run` uses, so migration
logic exists once. Arguments are parsed by hand: two commands and two flags do not justify
a `System.CommandLine` dependency.

`reset` is destructive, so it runs only when the environment (`DOTNET_ENVIRONMENT`, read
as `Production` when unset, as the .NET host does) is `Development`, or when `--force` is
passed. It always prints the target server and database before acting.

### Seeding goes through the application

The seeder sends the same MediatR commands as the API: `RegisterMemberCommand`,
`VerifyEmailCommand` with the token from the queued verification email (which is then
suppressed, because the member is already verified), `UploadListingPhotoCommand`,
`CreateListingDraftCommand` and `PublishListingCommand`. Every domain rule and the photo
pipeline therefore apply unchanged, and no row is written by SQL.

The daily publish limit (`Selling:PublishesPerDay`, `L2-051`) is kept. The seeder replaces
`TimeProvider` with a clock it moves: each listing is created and published at a time
spread over the 30 days before the seed, oldest first, so no member publishes more than
one listing on any day.

`seed` is idempotent by a marker: when the first demo member's email is already
registered, it prints that the demo data exists and exits 0 without writing.

### Demo photos from Open Images (CC BY 2.0)

Unsplash, Pexels, Pixabay and Wikimedia Commons could not be reached from the environment
that built this feature. The photos come instead from the
[Open Images Dataset V4](https://storage.googleapis.com/openimages/web/index.html), whose
images are Flickr photographs under the Creative Commons Attribution 2.0 licence, with the
author and original page listed for every image.

- 24 photos, 6 per category, committed under `src/backend/TuTuMuch.Cli/SeedPhotos/{Dresses,
  Tops, Outerwear, Bottoms}/`, each under 200 KB (about 2.4 MB in total). Listings reuse
  them; stored versions are content-addressed, so a reused photo is stored once.
- Chosen by hand: one item on a plain or simple background, no person, no logo or
  trademark as the subject.
- Each was padded or centre-cropped to a 1024 × 1024 square, so it passes the 800 px
  rule, and re-saved as JPEG. `SeedPhotos/ATTRIBUTION.md` lists every file, its title,
  author, source URL, licence and the change made, as CC BY 2.0 requires.
- The photos keep their CC BY 2.0 licence; the MIT licence covers the code only.
- Brand names on listings come from `Selling:SuggestedBrands` and do not describe the
  photographed item.

## Options Considered

### Option 1: A separate `TuTuMuch.Cli` that reuses the migrator (chosen)
- **Pros:** Deploy-time tooling cannot drop a database; one place for migration logic;
  the demo catalogue lives with the tool that loads it.
- **Cons:** One more project and test project in the solution.

### Option 2: Add `reset` and `seed` to `TuTuMuch.Migrator`
- **Pros:** No new project.
- **Cons:** The binary every deployment runs would carry a drop-database path and the demo
  photos.

### Option 3: Seed with SQL scripts
- **Pros:** Fast.
- **Cons:** Bypasses the domain rules and the photo pipeline; rows could point at files
  `/photos/{hash}/{file}` cannot serve.

### Photo source alternatives
- **Unsplash, Pexels or CC0 images:** the preferred licences, but not reachable from the
  build environment. A later change can swap files and `ATTRIBUTION.md` without code
  changes.
- **Generated or illustrated images:** rejected; the demo must show real product photos.
- **Hotlinking:** rejected; seeding must work offline.

## Consequences

### Positive
- `seed --reset` gives the same fresh demo every time, with listings in every category.
- Seeded data passes the same rules and pipeline as data entered through the UI.

### Negative
- CC BY 2.0 needs attribution kept with the photos; anyone redistributing them must keep
  `ATTRIBUTION.md`.
- `reset` against SQL Server is checked by hand only: tests never touch a real database.

### Risks
- Someone runs `reset --force` against a shared database. Mitigation: the guard is on by
  default, the target server and database are printed first, and the CLI is documented as
  local only.

## References

- [seed-demo-data](../../detailed-designs/platform/seed-demo-data/README.md)
- `L2-072` to `L2-077` in [L2.md](../../specs/L2.md)
- [ADR general/0002](../general/0002-no-load-or-scale-tests.md): the demo set is not a
  load or scale fixture.
