# Estimate — Marketing page

## Basis

The hours assume an intermediate Angular/TypeScript developer who does not yet know the
TuTuMuch codebase. They cover implementation, automated tests and review fixes, and exclude
requirements, design and mock work. They assume the app shell, the `DialogService` and
`AuthState` exist. The page, components and tests are built; the video is an interim silent
build, so the narrated build with music (part of the last row) remains.
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
| **Total** | **40** |

## Minimal scope

The minimal version drops the video dialog (the poster links to the MP4), the custom
Captions and Play buttons and the shape motion. Minimal total: **26** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Video dialog and player | Native player only; no clear error state | 8 |
| Shape motion | Static decoration | 1 |
| WebM fallback | Playwright tests cannot play the video | 1 |
| Narration and music | The silent interim video stays; captions carry the message | 3 |
