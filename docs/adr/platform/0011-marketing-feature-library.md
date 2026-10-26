# ADR-0011: A Marketing Feature Library and a Marketing Shell

**Date:** 2026-10-26
**Category:** platform
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

The public marketing page (`/welcome`, `L1-020`) sells TuTuMuch to shoppers and sellers and
plays a 30-second pitch video. It belongs to no existing feature area, and the app project may
not own feature screens (`AGENTS.md`, Frontend workspace layout). Its full-bleed bands cannot
sit in the app shell's `main.container.page`. The video files must be served by the static host.

## Decision

- Add the `marketing` library: `src/frontend/projects/marketing/`, alias `@tutumuch/marketing`,
  registered in `angular.json`, `tsconfig.json` and `build:libs`. It exports its lazy routes and
  holds `WelcomePage` and `PitchVideoDialog`. The app mounts it at `welcome`.
- Reusable pieces go to `@tutumuch/components`: `tl-marketing-hero`, `tl-shapes`,
  `tl-feature-band`, `tl-steps`, `tl-video-poster` and `tl-video-player`.
- Add a `marketing` shell value: routes with `data.shell = 'marketing'` get the same top bar
  and footer but `<main id="main" class="marketing">` without the container.
- Video media lives in `src/frontend/projects/tutumuch/public/media/` (served at `/media/`),
  built by `docs/mocks/_build/video/build_video.py`.

## Options Considered

### Option 1: New `marketing` library and shell value (chosen)
- **Pros:** Follows the one-library-per-area rule; the app stays shell-only; bands are full width.
- **Cons:** One more library to build.

### Option 2: Put the page in `catalog`
- **Pros:** No new library.
- **Cons:** Mixes a static sales page into the catalog area.

## Consequences

### Positive
- The page is lazy-loaded and calls no TuTuMuch API endpoint of its own.

### Negative
- `public/media/` adds about 2.2 MB of committed media.

### Risks
- Media grows with more cuts. Mitigation: other lengths and cuts are deferred.

## References

- [marketing-page design](../../detailed-designs/platform/marketing-page/README.md)
- [ADR general/0005](../general/0005-marketing-wording.md)
