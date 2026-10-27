# ADR-0007: Print Collateral in docs/print

**Date:** 2026-10-26
**Category:** general
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

TuTuMuch needs things to hand out in person: business cards, die-cut stickers and flyers for
parents. The brand kit made in Magic Patterns adds a character, Tilly Tutu, in eight poses, and
the wordmark the app already draws in `tl-logo`. TwinkleTune keeps its stickers as editable HTML
sources plus a renderer under `docs/stickers/`, and Saturdaze keeps its business card under
`docs/business-cards/`. The mandatory folder layout in `AGENTS.md` has no place for print files,
and code may only live in `src/` and `tests/`.

## Decision

- Add `docs/print/` with one folder per kind of print item (`business-cards/`, `stickers/`,
  `flyers/`), plus `characters/` for the Tilly cutouts they share. Those folders hold
  generated, print-ready output only.
- `docs/print/_build/` holds the sources, the same as `docs/mocks/_build/`: one HTML file per
  item, the shared `print.css` and `wordmark.js`, the fonts with their licences, the original
  Magic Patterns images, `cutout.py` (removes Tilly's background with Pillow and numpy) and
  `render.mjs` (Playwright: captures PNGs, builds sticker keylines, prints PDFs and checks them).
  These are documentation build scripts, not product code. Nothing in `src/`, `tests/`, CI or
  deployment uses them.
- The look follows the brand palette in `docs/design-system/tokens/tokens.css`, with Titan One
  (the wordmark face) for display and Nunito (Giddy's rounded sans) for text.

## Options Considered

### Option 1: `docs/print/` with a `_build/` source folder (chosen)
- **Pros:** Matches how mocks are already built; the sources can be edited and re-rendered;
  each kind of item sits in one predictable folder.
- **Cons:** Commits binary output (about 16 MB of PNG and PDF).

### Option 2: Keep the files outside the repository (Magic Patterns, a print shop's portal)
- **Pros:** The repository stays small.
- **Cons:** The copy can drift from the product, and nobody can rebuild the files.

### Option 3: A separate marketing repository
- **Pros:** Keeps binaries out of the product repository.
- **Cons:** One more repository for a handful of files that quote the product's own copy.

## Consequences

### Positive
- Cards, stickers and flyers quote the same copy as `/welcome` and can be rebuilt after it
  changes.

### Negative
- Tilly is raster art at about 1024 px. That is fine for stickers and cards, but on a full-page
  flyer she prints at roughly 150 to 200 DPI. A larger export from Magic Patterns would fix
  that.

### Risks
- The Tilly images come from the Magic Patterns CDN. If the brand kit changes, copy the new
  images into `_build/characters-src/` and run `cutout.py` and `render.mjs` again.
