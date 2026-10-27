# ADR-0009: Page Guidelines on the Angular and Giddy Stack

**Date:** 2026-10-27
**Category:** general
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

[`docs/page-guidelines.md`](../../page-guidelines.md) is the brand and page rulebook for TuTuMuch.
It came from the TuTuMuch brand kit built in Magic Patterns and published at
`https://project-tutumuch-kids-marketplace-223.magicpatterns.app` (editor
`49fz5xj9zssbvvrv2unpph`, source read on 2026-10-27). The kit is a React, Tailwind,
`lucide-react`, `framer-motion` and `sonner` app, so the guidelines name things that do not exist
here: `components/Logo.tsx`, `components/Mark.tsx`, `data/brand.ts`, `tailwind.config.js`,
Tailwind classes, `pages/`, `data/listings.ts`.

TuTuMuch is an Angular workspace drawn with the Giddy design system: one token file
(`docs/design-system/tokens/tokens.css`, mirrored to `docs/mocks/assets/tokens.css` and
`src/frontend/projects/components/src/styles/tokens.css`), one component stylesheet and the `tl-*`
components of `@tutumuch/components` (AGENTS.md: "Components library is mandatory"). Before this
decision the tokens already pointed Giddy's colour roles at the raspberry palette, but kept Giddy's
system rounded fonts, soft card shadows, the "pressable lip" under primary buttons, a gradient
showcase with drifting blobs, and party-pop photo wells.

The product also differs from the kit's assumptions: there is no cart (an order is one listing
bought with "Buy now", `L2-027`), the size is always `Girls'` (`L2-011` to `L2-013`), the condition
scale has five steps (`L2-016`), the categories are the six of `L2-011`, there are no saved hearts
or original prices, and launch phases hide category links and Sell (`L2-093`, `L2-094`).

## Decision

**Port the rules, not the stack.** Every guideline rule applies; each stack-specific instruction
is carried out with the TuTuMuch equivalent below. `docs/page-guidelines.md` is amended to name
these equivalents (sections 2, 3, 4, 8, 9, 10 and 13) without loosening any rule. Requirements
`L1-022` and `L2-096` make conformance testable.

### Colour: kit palette to Giddy tokens

The kit's exact hex values (`tailwind.config.js`) live only in the primitives tier of the token
file. Components use the semantic tokens.

| Guideline colour | Kit hex | Primitive | Semantic token(s) |
| --- | --- | --- | --- |
| `raspberry` | `#D6336C` | `--palette-raspberry-600` | `--color-accent` (primary buttons, active states, focus ring), `--color-sale` |
| raspberry as text | — | `--palette-raspberry-700` (`#C2255C`) | `--color-fg-link`, `--color-fg-accent`, `--color-error-icon` (raspberry 600 is 4.4:1 on cream, below AA for body text; white on raspberry 600 is 4.6:1) |
| `lilac` | `#9B7FD4` | `--palette-lilac-500` | `--color-selected-border`; `--color-selected-bg` is lilac at 15% (`--palette-lilac-tint-15`, lilac at 15% over cream made opaque). Never text on light. |
| `blush` | `#FDE2EA` | `--palette-raspberry-100` | `--color-bg-blush`, `--color-accent-subtle`, `--color-skeleton` |
| `sunny` | `#F6C64B` | `--palette-sunny-400` | `--color-highlight` (always with `--color-fg-default`) |
| `cream` | `#FFF8F5` | `--palette-cream-50` | `--color-bg-canvas` |
| `ink` | `#3A2233` | `--palette-plum-800` | `--color-fg-default`, `--color-bg-inverse` |
| `muted` | `#7A6370` | `--palette-plum-500` | `--color-fg-muted` |
| `white` | — | `--palette-white` | `--color-bg-surface` (cards, photo wells, fields) |
| `ink/10` lines | — | `--palette-ink-alpha-10` | `--color-border-default` (thin separators) |
| `ink/15` ring | — | `--palette-ink-alpha-15` | `--color-border-ring` (Well loved badge) |

The logo art keeps its own colours, used only inside `tl-logo` and `tl-mark` through the
`--color-logo-*` tokens: lilac `#B9A3E8`, lavender `#E3D8FA`, grape `#8A5CD6`, polka-dot pink
`#F29BBC`, reversed dot `#F7B8CF`.

Giddy's grape, gray, blue and party-pop primitives are removed. Status colours (info, success,
warning, danger) stay, because order and form states need them, and are drawn from the palette:
danger uses raspberry 700/800, warning uses sunny with ink text, success a green kept from
Giddy, info lilac 700. "No gradients" removes `--gradient-showcase`, the hero gradient tokens and
the drifting shapes; the showcase becomes a solid blush or ink panel.

### Type

The kit (`index.css`, `data/brand.ts` `typeScale`) uses Fraunces (variable, opsz 9–144, wght
400–900, `font-variation-settings: 'SOFT' 100, 'WONK' 0`, `font-optical-sizing: auto`), DM Sans
(opsz 9–40, wght 400–700) and Titan One for the logo only.

| Guideline style | Kit value | Token |
| --- | --- | --- |
| Display (marketing hero) | Fraunces 600 64/64 | `--text-display` |
| Page title (H1) | Fraunces 600 36/40, 48 at md | `--text-h1` (`--font-size-h1` grows at 48rem) |
| Section heading (H2) | Fraunces 600 30 | `--text-h2` |
| Subheading (H3) | Fraunces 500 22/28 | `--text-h3` |
| Body | DM Sans 400 16/28, 18/28 at md | `--text-body` (grows at 48rem) |
| Label | DM Sans 600 14/20 muted | `--text-label` |
| Secondary | DM Sans 14 muted | `--text-body-sm` with `--color-fg-muted` |
| Price | Fraunces 600 20, tabular | `--text-price` |
| Original price | 14 muted, struck | `--text-price-original` (no domain field yet; see Deferred) |
| `tracking-tight` | −0.025em | `--letter-spacing-tight` |
| Logo | Titan One | `--font-family-logo`, only in `tl-logo` and `tl-mark` |

`--font-family-display` is Fraunces, `--font-family-sans` is DM Sans. No `--text-overline`
uppercase label is used above headings (the token stays, sentence case).

**Fonts are self-hosted** under the SIL Open Font License, next to Titan One:
`src/frontend/projects/tutumuch/public/fonts/fraunces-latin-full.woff2` (Fontsource 5.3.0,
latin subset, axes opsz, wght, SOFT, WONK; 121 KB) and `dm-sans-latin-opsz.woff2` (latin, opsz
and wght; 63 KB), with `Fraunces-OFL.txt` and `DMSans-OFL.txt`. The same files are copied to
`docs/mocks/assets/fonts/` and `docs/design-system/assets/fonts/` and declared in a `fonts.css`
beside each (`@font-face` with `font-display: swap`). Variable files are chosen over static
weights because the kit's `SOFT 100` and optical sizing need the variable axes.

### Shape, elevation and motion

| Guideline | Token |
| --- | --- |
| `rounded-[2rem]` large panels | `--radius-panel` = `--radius-xl` = 2rem (dialogs, sheets, hero panels) |
| `rounded-[1.5rem]` cards and images | `--radius-card` = `--radius-lg` = 1.5rem |
| `rounded-2xl` swatches and inputs | `--radius-input` = `--radius-md` = 1rem |
| `rounded-full` buttons, chips, badges | `--radius-full` |
| No card shadows | `--shadow-1` and `--shadow-2` are `none`; `--shadow-press` is removed |
| Soft shadow on floating elements | `--shadow-3` (menus, toasts), `--shadow-4` (dialogs, sheets) |
| 150–250ms, `cubic-bezier(0.23, 1, 0.32, 1)` | `--duration-fast` 150ms, `--duration-base` 200ms, `--duration-slow` 250ms, `--ease-out`; `--ease-standard` and `--ease-enter` alias it |
| No `transition-all`, reduced motion | components list the properties they animate; the reduced-motion block zeroes the durations |

### Components

| Guideline / kit | TuTuMuch |
| --- | --- |
| Tailwind utility classes | Giddy semantic tokens and the `tl-*` components; never a hard-coded hex outside the primitives |
| `<Logo />`, `<Logo muchFill="checker" />` | `tl-logo`: the existing eight-letter wordmark ("TuTu" polka dot, "Much" gingham, 0.05em ink outline, raspberry shadow at 0.07em, 0.09em). Gingham is its only fill, so it needs no `muchFill` input. |
| `<Logo tone="reversed" />` | `tl-logo` input `tone`: `color` (default) or `reversed` (ink outline, ink shadow, `#F7B8CF` dots, sunny "Much"), only on raspberry |
| Logo size and clear space | `tl-logo` sizes sm, md, lg, never below 28px tall; padding equal to the "u" height (`0.5em`) is reserved around it |
| `<Mark variant="tGingham" />` | New `tl-mark`: an inline SVG gingham "T" on a blush circle using the kit's `markScene` geometry (cell 0.05 × size, letter 0.76 × size, baseline 0.46 × size + 0.35 × letter, shadow 0.07/0.09 × letter, outline 0.1 × letter with round joins, rotation −6°), coloured by tokens. Minimum 16px. The other kit variants (`tuBlush`, `ginghamTile`, `raspberryT`) are not built. |
| Header mark under 640px | `tl-top-bar` shows `tl-logo` from 40rem and `tl-mark` (36px) below, inside the same home link, so the page still has one logo link |
| Favicon and app icon | Keep the Tilly face tile (`docs/design-system/assets/app-icon.svg`, `favicon.ico`); it is the app icon, not the mark |
| Primary button | `tlButton="primary"`: raspberry, white 16px semibold label, hover ink, no lip |
| Secondary button | `tlButton` (default): white, raspberry label, 2px inset raspberry ring, hover raspberry with white text over 150ms (kit convention) |
| Text link | `tlButton="link"` or `a.link`: semibold raspberry 700, hover ink and underline with 4px offset |
| Focus style | every control: 2px raspberry ring with 2px offset (`--shadow-focus`) |
| Condition badge | New `tl-condition-badge` (input `condition`). Not `tl-status`, whose colours mean order and listing states, nor `tl-size-tag`, which is the size. |
| Skeleton blocks in blush | New `tl-skeleton` (shapes `text`, `photo`, `block`), drawn in `--color-skeleton`; `tl-listing-grid` uses it. `tl-spinner` stays only for in-place busy states (a button, load more). |
| Tilly | New `tl-tilly` (input `pose`), which also writes the alt text: "Tilly twirling in a raspberry tutu", "Tilly dancing an arabesque", "Tilly kicking a soccer ball", "Tilly bumping a volleyball", "Tilly landing a gymnastics move", "Tilly holding up a yellow dress", "Tilly waving hello". `tl-empty-state` takes `[tilly]="true"` to show the "Hello, friend" pose. |
| Selected chips | `--color-selected-bg` with a 1px `--color-selected-border` ring and ink text |
| `sonner` toasts | `ToastService` and `tl-toast-region` |
| `lucide-react` icons | `tl-icon` (24-unit, 2px stroke, `currentColor`) |
| `framer-motion` | the motion tokens and CSS transitions of named properties |
| `pages/`, `components/`, `data/`, `types/`, `utils/` | the Angular workspace layout of AGENTS.md; mock data is `docs/mocks/_build/data.py` and the e2e fixtures |

### Tilly artwork

The cut-out poses from `docs/print/characters/` (twirl, dance, soccer, volleyball, gymnastics,
show-and-tell, hello) are converted to WebP (600px tall, alpha kept) and copied to
`docs/mocks/assets/tilly/`, `docs/design-system/assets/tilly/` and
`src/frontend/projects/tutumuch/public/tilly/`, each with an `ATTRIBUTION.md` that points back to
`docs/print/characters/ATTRIBUTION.md`. The swimming pose exists only framed
(`docs/print/_build/characters-src/tilly-swimming.jpg`); it is copied as a framed tile and used
only as a tile. The face mark stays the app icon only, so it never adds a second Tilly to a
screen. AGENTS.md's folder list gains `public/fonts/` and `public/tilly/`.

Placement: the twirl pose in the marketing hero; the dance pose on the Dance category header; on
the Sports header the pose matching the one selected subcategory (Soccer, Volleyball, Gymnastics;
the swimming tile for Swimming) and the soccer pose otherwise; the hello pose in empty states;
the show-and-tell pose on the "Your listing is live" confirmation. At most one per screen: a
page that already shows Tilly in its header uses a plain empty state.

### Product facts that bend a guideline

- **Bag.** TuTuMuch has no cart; each order is one listing bought with "Buy now". There is no Bag
  link, count or "Add to bag" in this run, and none is invented. The header right side is Sell
  and Account (or Sign in). Wherever a bag appears in future, it is called "Bag".
- **The closet.** The catalog is called "the closet" in copy: "Shop the closet", "Search the
  closet". Route and page titles do not change.
- **Pass it on.** Seller calls to action read "Sell outgrown gear" or "Pass it on". The top bar
  link stays "Sell", as the guideline header names it.
- **Condition scale.** The domain has five conditions (`New with tags`, `New without tags`,
  `Used - Excellent`, `Used - Good`, `Used - Fair`) that are also API values. Labels stay as they
  are; badge colours follow the guideline by nearest step: New with tags raspberry; New without
  tags and Used - Excellent sunny (Like new); Used - Good blush (Gently loved); Used - Fair white
  with an ink ring (Well loved). Renaming to the four-step scale needs a domain change (Deferred).
- **Size.** Every listing is `Girls'` (`L2-013`). Cards show it as "Girls'" at the start of the
  "Size · Condition" line ("Size Girls'" would read oddly); the size tag stays on category
  headers and the detail page. Numeric sizes need a domain change (Deferred).
- **Listing card.** Photo (4:5, white well, 1.5rem corners, cover), title on one line, brand (kept
  because `L2-022` requires it), "Girls' · {condition}" muted, price row pushed to the bottom with
  `margin-top: auto`. No heart: saved hearts do not exist (Deferred). Reserved and Sold use a
  status badge; Sold greys the photo to 60% and offers "Find similar".
- **Filters.** `L2-062` keeps the sidebar on large screens and the panel on small ones. Inside
  both, the three most useful groups show first and open: Category (the "Sport" filter, since not
  every category is a sport), Size (shown as the fixed "Girls'" note, since there is one size),
  Condition. Brand and Price move behind a "More filters" disclosure.
- **Categories.** The guideline order is Dance · Volleyball · Soccer · Gymnastics · Swimming ·
  Everyday · Party & occasion · Shoes · Accessories. The domain has Clothing, Shoes, Dance, Sports
  (subcategories Soccer, Volleyball, Gymnastics, Swimming, Bikes and scooters), Cameras and tech,
  Toys and crafts. The top bar shows the six in guideline order: **Dance · Sports · Clothing ·
  Shoes · Cameras and tech · Toys and crafts** (Sports stands for the four sports, Clothing for
  Everyday; the two TuTuMuch-only categories of ADR general/0006 come last).
- **Launch phases.** ADR platform/0014 still hides the category row and Sell; the header skeleton
  describes the fully launched state (Phase 3).
- **Dark theme.** The guidelines define light only. The dark theme stays: plum night canvas
  (`--palette-plum-950`), lighter raspberry accent (raspberry 400 with plum text), blush text
  roles inverted, every pair checked by `check_contrast.py`.
- **Admin shell and notifications.** Not covered by the guidelines; they keep their layout and
  take the new tokens.

## Options Considered

### Option 1: Map the guidelines onto tokens and `tl-*` components (chosen)
- **Pros:** One source of the look stays (tokens), the components library stays mandatory, no new
  dependency, and the checkers already in the repo can prove conformance.
- **Cons:** The guidelines must be amended so their stack references match the repo.

### Option 2: Add Tailwind and port the kit's React components
- **Pros:** The guideline classes would work verbatim.
- **Cons:** A second styling system beside Giddy, a React dependency in an Angular workspace, and
  hex values scattered through templates; breaks AGENTS.md's token and component rules.

### Condition badge: new component vs `tl-status` vs `tl-size-tag`
`tl-status` already means order and listing states (Paid, Reserved, Sold) and `tl-size-tag` the
size; overloading either would give one colour two meanings. A small `tl-condition-badge` keeps
the mapping in one place. Chosen.

### Mark: Tilly face vs gingham "T"
The face tile would count as a Tilly on every page and break "one Tilly per screen". The kit's
`tGingham` mark is chosen; the face stays the app icon.

### Fonts: static weights vs variable files
Static files (about 90 KB for six weights) lose the kit's SOFT axis and optical sizing. Variable
files (184 KB) are chosen.

## Consequences

- Tokens, the three stylesheet copies, every mock, the design-system pages, the components and the
  feature pages change look and some copy; routes, API, behaviour and the backend do not.
- `L1-022` and `L2-096` are added; Playwright checks every routed page for one `h1`, one logo,
  labelled icon buttons, alt text, at most one Tilly and no inline hex or gradient.
- AGENTS.md lists `public/fonts/` and `public/tilly/`.
- The decorative `tl-shapes` component and its design-system page are retired: "no blobs, glows or
  decorative shapes". The marketing hero and closing band become solid blush panels.
- Copy that requirements quote changes with the standard terms: "Shop the catalog" and "Browse the
  catalog" become "Shop the closet" (`L2-078`, `L2-094`), "Sell an item" actions become "Sell
  outgrown gear" (`L2-003`, `L2-019`, `L2-094`), and `L2-083` no longer mentions shapes.
- One primary button per view: the marketing page keeps its primary in the hero; the bands and the
  closing call to action use secondary buttons.

### Deferred (needs a domain or product change)
- Top-level Volleyball, Soccer, Gymnastics, Swimming, Party & occasion and Accessories
  categories, and renaming Clothing to Everyday.
- The four-step condition scale (New with tags, Like new, Gently loved, Well loved).
- Numeric girls' sizes 2T–16 and shoe sizes ("Size 6", "Size 2Y"), and with them a real Size
  filter.
- A bag, saved hearts, original prices, "recently viewed", "more from this seller", 404 art.
- A cut-out swimming pose (only the framed tile exists).

## References

- [`docs/page-guidelines.md`](../../page-guidelines.md)
- TuTuMuch brand kit, `https://project-tutumuch-kids-marketplace-223.magicpatterns.app`
  (Magic Patterns editor `49fz5xj9zssbvvrv2unpph`)
- [ADR general/0005](0005-marketing-wording.md), [ADR general/0006](0006-all-things-young-girls.md),
  [ADR general/0007](0007-print-collateral-in-docs-print.md),
  [ADR platform/0014](../platform/0014-launch-phase-configuration.md)
- `docs/specs/L1.md` (`L1-022`), `docs/specs/L2.md` (`L2-096`, `L2-011` to `L2-013`, `L2-016`,
  `L2-022`, `L2-062`, `L2-064`, `L2-065`)
