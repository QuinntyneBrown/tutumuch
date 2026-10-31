# Estimate — Marketing page

## Basis

The hours assume an intermediate Angular/TypeScript developer who does not yet know the
TuTuMuch codebase. They cover implementation, automated tests and review fixes, and exclude
requirements, design and mock work. They assume the app shell, the `DialogService` and
`AuthState` exist. The page, components, tests and the narrated video with music are built.
[Detailed design](../../detailed-designs/platform/marketing-page/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `marketing` library skeleton, lazy route, `marketing` shell, title and meta description | 4 |
| `tl-marketing-hero`, `tl-shapes` with reduced motion, showcase tokens | 6 |
| `tl-feature-band`, `tl-steps` and the page copy with CTA targets | 5 |
| `tl-video-poster`, `tl-video-player` (sources, captions, loading, error) and `PitchVideoDialog` | 8 |
| Footer link, `serve.mjs` media types | 1 |
| Playwright page object and acceptance tests | 8 |
| Visual check at phone and desktop, light and dark, and fixes | 4 |
| Video build script, voice and music selection | 4 |
| Phase 1 page ([ADR general/0010](../../adr/general/0010-phase-one-marketing-page.md)): `tl-tulle-field` canvas with reduced motion and theme colours | 6 |
| Phase 1 page: `tl-closet-marquee` and the catalog call with its fallback | 5 |
| Phase 1 page: `tl-closet-worth` with toggles, count-up and live region (built, then taken off the page) | 5 |
| Phase 1 page: top bar without search and Sign in, calculator removed | 1 |
| Phase 1 page: `tl-steps` journey, icons, header logo size | 4 |
| Phase 1 page: copy, phase choice and acceptance tests (`L2-112`) | 6 |
| **Total** | **67** |

## Minimal scope

The minimal version drops the video dialog (the poster links to the MP4), the custom
Captions and Play buttons, the shape motion and the Phase 1 tulle field and journey. Minimal total: **42** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Video dialog and player | Native player only; no clear error state | 8 |
| Shape motion | Static decoration | 1 |
| WebM fallback | Playwright tests cannot play the video | 1 |
| Narration and music | A silent video; captions carry the message | 3 |
| Tulle field | A plain blush Phase 1 hero | 6 |
