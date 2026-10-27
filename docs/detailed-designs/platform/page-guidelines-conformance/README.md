# Page guidelines conformance

## Overview

Every TuTuMuch screen follows [`docs/page-guidelines.md`](../../../page-guidelines.md), the brand
and page rulebook that came from the TuTuMuch brand kit. This feature, part of the `platform`
subsystem, redraws the existing screens to those rules. It changes look, copy and accessibility
only: routes, journeys, the API, the backend and the domain rules stay as they are.
[ADR general/0009](../../../adr/general/0009-page-guidelines-on-the-angular-and-giddy-stack.md)
maps each guideline onto the Angular workspace and the Giddy tokens and lists what waits for a
domain change.

Terms used below:

- **token** — a CSS custom property in `docs/design-system/tokens/tokens.css`, mirrored to the mocks and the components library; the only place a colour, font, radius, shadow or duration is written
- **mark** — the gingham "T" on a blush circle, the compact form of the logo
- **Tilly** — TuTuMuch's character, shown in one approved pose, at most once per screen

## Description

### Tokens and fonts

The token file takes the brand kit's palette (raspberry, lilac, blush, sunny, cream, ink, muted),
Fraunces for headings and prices, DM Sans for everything else and Titan One for the logo only, the
kit's type scale, 2rem panels, 1.5rem cards and images, 1rem inputs, no card shadows and 150 to
250ms ease-out motion; the dark theme stays a plum night. The three fonts are self-hosted under the
OFL in `projects/tutumuch/public/fonts/` and declared in the app's `styles.css`.

### Components library

`components.css` gains the page-guidelines layer (buttons, selected chips, condition badges, white
photo wells, blush skeletons, the two-row top bar, the category header, "More filters"). New
components: `tl-mark`, `tl-tilly`, `tl-condition-badge`, `tl-skeleton`. Changed: `tl-logo`
(`tone`, `responsive`, a 28px minimum), `tl-top-bar` (page skeleton, guideline category order,
"Search the closet"), `tl-footer` (mark and "© TuTuMuch"), `tl-listing-card` and `tl-listing-grid`
(anatomy, aligned price rows, `sold`, skeleton grid), `tl-gallery` (`sold`), `tl-empty-state` and
`tl-center-state` (`tilly`), `tl-feature-band` (`blush`), `tl-marketing-hero` (no shapes).
`tl-shapes` is retired.

### Shell and feature pages

The app shell adds a `wide` shell for the catalog's product grids. The catalog shows Tilly on the
Dance and Sports headers, the filters put Category, Size and Condition first with Brand and Price
under "More filters", and the listing detail shows a condition badge and a sold treatment with
"Find similar". Every loading state is built from `tl-skeleton`; short waits show the mark; empty
states show Tilly waving hello; the "It's live" confirmation shows her Show & Tell pose; the
marketing hero shows her twirling. Copy uses the standard terms ("Shop the closet", "Sell outgrown
gear") and one primary button per view.

### Tests

`e2e/tests/page-guidelines.spec.ts` with the `PageGuidelines` page object checks every routed page
for one `h1`, one logo link, labelled icon-only controls, alt text, at most one Tilly with alt text
starting "Tilly", no inline hex or gradient, at most one primary button, Fraunces headings, DM Sans
text and Titan One only in the logo; it also checks the header skeleton, the compact mark, card
anatomy and alignment, the sold treatment, condition badges, blush skeletons, the filter order and
reduced motion. The design-system and mock checkers verify tokens, contrast and pages.

## Requirements

- `L1-022` Page guidelines
- `L2-096` Pages follow the page guidelines
- Copy updates in `L2-003`, `L2-021`, `L2-078`, `L2-083` and `L2-094`

## Diagrams

None: the feature adds no component boundary, class or flow beyond the presentation components
listed above.

## Open questions

- Numeric sizes, the four-step condition scale and the guideline's extra top-level categories need
  domain changes (deferred in ADR general/0009).
