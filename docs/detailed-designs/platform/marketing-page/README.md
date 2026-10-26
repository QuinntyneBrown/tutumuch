# Marketing page

## Overview

TuTuMuch is a marketplace for all things young girls: new and gently loved clothes, dance and
sports gear, cameras and more, every listing for girls. This feature, part of the `platform`
subsystem, adds a public marketing page at `/welcome` that sells TuTuMuch to shoppers and
sellers, with a 30-second pitch video. Anyone can open it, signed in or not. It refines
`L1-020` through `L2-078` to `L2-083`. `/` stays the catalog.

Terms used below:

- **showcase** — the ink (plum) surface with a gradient used by the hero, the closing band and
  the video frame; ink in both themes, with cream text, pink headline emphasis and raspberry,
  lilac and sunny shapes ([design system](../../../design-system/components/marketing-hero.html))
- **pitch video** — `tutumuch-pitch.mp4` (H.264/AAC) and `tutumuch-pitch.webm` (VP9/Opus),
  30.0 s, with `tutumuch-pitch.vtt` captions, `tutumuch-pitch.txt` transcript and
  `tutumuch-pitch-poster.png`

The page calls no TuTuMuch API endpoint; the shell's `GET /api/auth/me` decides the seller
target. Nothing in the size rules, the catalog, sell, accounts or the API changes. The copy
says everything is for girls and never promises boys' or adult sizes
([ADR general/0005](../../../adr/general/0005-marketing-wording.md)).

## Description

### Library, route and shell ([ADR platform/0011](../../../adr/platform/0011-marketing-feature-library.md))

- `src/frontend/projects/marketing/` (alias `@tutumuch/marketing`), built by `build:libs`,
  exports `routes`: `{ path: '', loadComponent: WelcomePage, title: 'Twirl it forward ·
  TuTuMuch', data: { shell: 'marketing' } }`.
- `app.routes.ts` mounts it lazily at `welcome`, before the catalog's empty-path routes.
- `AppShellComponent` gains the `marketing` shell: same payout banner, top bar and footer, but
  `<main id="main" class="marketing" tabindex="-1">` without `.container.page`, so bands are
  full width.

### Components (`@tutumuch/components`)

| Component | Inputs and outputs | Notes |
|---|---|---|
| `tl-marketing-hero` | `eyebrow`, `lede`, `variant: 'hero' \| 'closing'`, `headingId`; slots `[title]`, `[actions]`, `[media]` | h1 in the hero, h2 in the closing band; re-points semantic tokens to the showcase |
| `tl-shapes` | `density: 'full' \| 'quiet'` | Rounded party-pop blobs and a polka-dot band; `aria-hidden`; drift stops under reduced motion |
| `tl-feature-band` | `eyebrow`, `heading`, `headingId`, `points`, `tone: 'default' \| 'surface'`, `stacked`; slots `[actions]`, body | Section labelled by its h2 |
| `tl-steps` | `steps: { title, text }[]` | Ordered list, h3 titles |
| `tl-video-poster` | `poster`, `label`, `duration`; output `watch` | Native button; no `<video>` on the page |
| `tl-video-player` | `label`, `sources`, `captions`, `transcript`, `poster`, `autoplay` | States: loading, ready, error; Play/Pause, Captions (`aria-pressed`), transcript link; error when the last `<source>` fails |

### Feature (`@tutumuch/marketing`)

- **`WelcomePage`** — static copy (headlines of 3 to 6 words): hero "Twirl it *forward.*",
  For shoppers "Everything here is for girls.", For sellers "Pass it on to the next girl.",
  How it works "Three steps. That’s it.", closing "Her next favourite thing is *waiting.*";
  `sellHref = computed(() => auth.member() ? '/sell' : '/join')`, the meta description
  through Angular's `Meta` (removed on destroy), and `openVideo()` that calls
  `DialogService.open(PitchVideoDialog)`.
- **`PitchVideoDialog`** — `tl-dialog` (`lg`, "TuTuMuch in 30 seconds") around
  `tl-video-player` with `autoplay` true: it opens only from the poster press, so playback
  with sound is the visitor's choice. Closing destroys the video, which stops it.

### Media and video build

Media lives in `src/frontend/projects/tutumuch/public/media/` (served at `/media/`) and is
copied to `docs/mocks/assets/media/`. `docs/mocks/_build/video/build_video.py` builds it
(rerun: `python -I docs/mocks/_build/video/build_video.py`; see its
[README](../../../mocks/_build/video/README.md)):

1. Narration: six lines, each with caption text and spoken text, synthesised by **Kokoro-82M**
   from Hugging Face (`onnx-community/Kokoro-82M-v1.0-ONNX`), voice **`af_bella`** at speed
   1.12, bright and upbeat. Model files download to the git-ignored `kokoro/` folder.
2. Timing: gaps stretch so the video is exactly 30.0 s.
3. Slides: six 1920×1080 slides in TuTuMuch tokens (plum gradient, cream text, raspberry,
   lilac and sunny party-pop blobs and polka dots, Giddy's rounded type, the wordmark),
   screenshotted by Playwright Chromium.
4. Music: _Upbeat Ukulele Kids_ by Cold_Fire (Pixabay Content Licence), committed as an
   excerpt, at −9 dB and ducked by a sidechain compressor keyed on the voice.
5. Encode: MP4 (libx264, AAC, faststart) and WebM (VP9, Opus). The WebM exists because
   Playwright's Chromium has no H.264 decoder; browsers pick the first source they can play.
6. Captions from the line timings, transcript, poster (last slide) and `ATTRIBUTION.md`.

The committed media are the full build: narrated by `af_bella`, with the music ducked under the
voice.

### Tests (`src/frontend/e2e/`)

`tests/marketing-page.spec.ts` with `pages/welcome.page.ts`, served by the static build; the
media come from the build, and the error state routes `**/media/tutumuch-pitch.*` to 404.
`serve.mjs` learns the media content types. Covers `L2-078` to `L2-083`, including reduced
motion and the footer link.

## Requirements

| L2 ID | Refines (L1) | Requirement |
|-------|--------------|-------------|
| `L2-078` | `L1-020` | Page sections and CTA targets for visitors and members; copy for girls only |
| `L2-079` | `L1-020` | Footer "About TuTuMuch" link |
| `L2-080` | `L1-020` | Poster first, no autoplay, keyboard play and pause |
| `L2-081` | `L1-020` | Captions toggle and transcript link |
| `L2-082` | `L1-020` | Video error state; page stays usable |
| `L2-083` | `L1-020` | Contrast over gradients, heading order, reduced motion, title and meta description |

Related requirements applied without restating them: `L2-061` to `L2-065` (responsive,
accessible shell), `L2-010` (no role needed), `L2-011` to `L2-014` (the girls' size rule the
copy must stay true to).

## Diagrams

The PlantUML sources are in `diagrams/`, each with its rendered PNG. Re-render after an edit
with
`python -B -I .claude/skills/writing-software-design-documents/scripts/render_puml.py docs/detailed-designs/platform/marketing-page/diagrams`.

### Components

![C4 component view of the marketing page](diagrams/c4-component.png)

### Behaviour — play the pitch video

![Sequence diagram for playing the pitch video](diagrams/sequence-play-video.png)

## Open questions

- Deferred: SEO beyond the title and meta description, analytics, A/B tests, newsletter
  sign-up, making `/welcome` the home page, translations and other video lengths or cuts.
