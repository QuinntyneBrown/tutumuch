# TuTuMuch design system

Extracted from [docs/mocks](../mocks/README.md) on 2026-10-25. Open [index.html](index.html) locally; press `t` to toggle dark mode.

TuTuMuch uses the Giddy design system: Giddy's tokens for type, space, shape, elevation and motion, its colour
structure and its component look (pill buttons with a pressable lip, soft 20px corners, rounded display type,
round avatars and status dots), with TuTuMuch's own palette layered on top: **Raspberry** (primary: logo and
buttons), **Lilac** (secondary accents), **Blush** (soft surfaces and tags), **Sunny** (highlights and deals),
**Cream** (the page) and **Plum** (text and dark surfaces). Dark mode is a plum night with a lighter raspberry
accent. Components and pages keep TuTuMuch's marketplace layout. The logo (`assets/logo.png`, polka-dot and
checked letters with a plum outline) is the owner's artwork; the app icon and favicon are its first T.

The first extraction covers the tokens, every foundation page and the components the
application shell needs. Components for feature screens are added as those screens are
built; until then their mocks remain the reference. Create-listing added the components of
the join, verify-email, sign-in and sell screens and the verify-email-prompt dialog;
upload-listing-photos added the photo uploader and the camera icon; browse-catalog added the
empty state, listing card and load more; view-listing-detail added the gallery, buy bar,
seller summary, the edit and star icons and the status code of the center state;
cancel-order added the quote, the radio group and the danger tone of the dialog;
resolve-order-problems added the thread, facts and status, the flag icon, the admin variant of
the top bar and the no-cover mode of the photo uploader; manage-notification-preferences added
the switch and implemented the toast with `ToastService`; manage-cookie-consent added the
consent banner, the visible label and hint of the switch and the footer's Cookie settings button;
the marketing page added the marketing hero, shapes (party-pop blobs and a polka-dot band),
feature band, steps, video poster and video player, and the showcase tokens they use.

## Foundations

| Page | Covers |
|---|---|
| [Color](foundations/color.html) | The six named colours, ramps, roles, status, pops, both themes, contrast pairs |
| [Typography](foundations/typography.html) | Giddy's rounded display and body stacks, scale, roles |
| [Spacing](foundations/spacing.html) | 4px scale, inside/between rules |
| [Layout](foundations/layout.html) | L2-061 breakpoints, grid, container, page templates |
| [Elevation](foundations/elevation.html) | Shadows, press lip, z-order |
| [Shape](foundations/shape.html) | Pills and soft corners |
| [Motion](foundations/motion.html) | Durations, easings, reduced motion |
| [Iconography](foundations/iconography.html) | Inline SVG set and sizes |
| [Theming](foundations/theming.html) | Token tiers, brand palette layer, light/dark, adding a theme |
| [Responsive](foundations/responsive.html) | Per-component rules and testing matrix |
| [Accessibility](foundations/accessibility.html) | WCAG 2.2 AA commitments |
| [Content](foundations/content.html) | Voice, formulas, microcopy |

## Components

| Component | Variants | States | Source mocks |
|---|---|---|---|
| [Skip link](components/skip-link.html) | one | hidden, focused | every page |
| [Top bar](components/top-bar.html) | guest, member, admin, bare | default, current | catalog/guest, catalog/default, problem-detail, maintenance |
| [Footer](components/footer.html) | one | default, hover | catalog |
| [Consent banner](components/consent-banner.html) | one | shown until a choice, then gone | notifications/cookie-consent/info |
| [Toast](components/toast.html) | info, success, warning, danger; optional action; `ToastService` | entering, leaving, paused | notifications/toast |
| [Sheet](components/sheet.html) | navigation, filter | open | dialogs/nav-menu, dialogs/filters |
| [Button](components/button.html) | primary, secondary, ghost, link, danger | default, hover, focus, active, disabled, loading | catalog/guest, nav-menu, toast |
| [Link](components/link.html) | inline, nav, footer | default, hover, focus, current | catalog, rules |
| [Form field](components/form-field.html) | input, textarea, select | default, hover, focus, invalid, disabled, read only | sign-in, sell |
| [Alert](components/alert.html) | info, success, warning, danger; banner without title; projected actions; dismissible | static, polite, assertive | sign-in/invalid, sell/draft-saved, verify-email-prompt, payout-banner |
| [Center state](components/center-state.html) | blocked, limited, error, success; icon or status code | static | sell/unverified, sell/limited, sell/error, sell/success, listing-detail/unavailable, listing-detail/error |
| [Chip](components/chip.html) | selected, with remove button | default, hover, focus | catalog/filtered, catalog/no-results |
| [Filter chips](components/filter-chips.html) | one pressed toggle chip per value, each with a count | default, hover, focus, pressed | my-listings/default, my-listings/filtered |
| [Choice](components/choice.html) | plain, with links | unticked, ticked, invalid | register, sell |
| [Dialog](components/dialog.html) | default, with top alert; info or danger icon tone | default, busy, sent, limited | dialogs/verify-email-prompt, dialogs/cancel-order |
| [Earnings](components/earnings.html) | one | unknown, known | sell/filled |
| [Error summary](components/error-summary.html) | one | shown and focused | sell/invalid, register/invalid |
| [Fixed size](components/fixed-size.html) | Girls' | absent, shown | sell/filled |
| [Icon](components/icon.html) | 19 glyphs (search added by search-and-filter-catalog, flag by resolve-order-problems) | static | every page |
| [Input group](components/input-group.html) | leading addon | default, invalid | sell price and shipping |
| [Item row](components/item-row.html) | with or without thumbnail | default, truncated | sell/success |
| [Empty state](components/empty-state.html) | default, error (compact) | static | catalog/empty, catalog/error |
| [Listing card](components/listing-card.html) | active, reserved; grid of 2, 3 or 4 columns | default, hover, focus, loading skeleton | catalog/default, catalog/loading, catalog/category |
| [Load more](components/load-more.html) | one | default, busy, hidden when all shown | catalog/default |
| [Gallery](components/gallery.html) | one photo, several photos | selected thumbnail, swiped | listing-detail/default |
| [Buy bar](components/buy-bar.html) | Buy now, Edit listing | sticky below 992 px, inline from 992 px | listing-detail/default, listing-detail/guest, listing-detail/own |
| [Seller summary](components/seller-summary.html) | established, new seller | static | listing-detail/default, listing-detail/new-seller |
| [Rating summary](components/rating-summary.html) | rated; "No reviews yet" | static | profile/default, profile/own, profile/empty |
| [Order summary](components/order-summary.html) | priced, not yet priced | static | checkout/default, checkout/new-address |
| [Reservation timer](components/reservation-timer.html) | info, warning, danger | counting, last minute, ended | checkout/default, reservation-timer/warning, reservation-timer/danger |
| [Order timeline](components/order-timeline.html) | paid, shipped, delivered | done, current, to come | sale-detail/shipped, order-detail/default, order-detail/paid |
| [Tracking link](components/tracking-link.html) | USPS, UPS, FedEx, DHL | link states | sale-detail/shipped, order-detail/default |
| [Photo uploader](components/photo-uploader.html) | listing (cover, reorder), evidence (`[cover]="false"`, report-problem) | empty, uploading, filled, rejected, invalid, full | sell/default, sell/filled, sell/invalid |
| [Segmented control](components/segmented.html) | one | empty, chosen, invalid | sell condition |
| [Size tag](components/size-tag.html) | Girls; default, large | static | sell, catalog |
| [Spinner](components/spinner.html) | with message | running | verify-email/loading |
| [Stat](components/stat.html) | in a `.stats` row | static, loading skeleton | payouts/default, payouts/loading |
| [Switch](components/switch.html) | on, off, locked on; hidden label or label with hint | off, on, focus, disabled | notification-settings/default, notification-settings/submitting |
| [Quote](components/quote.html) | one | static | order-detail/cancelled, sale-detail/cancel-requested, respond-cancellation |
| [Radio group](components/radio-group.html) | one | nothing chosen, chosen, invalid | cancel-order/default, cancel-order/invalid |
| [Thread](components/thread.html) | one | static, with photos | order-detail/problem, sale-detail/problem, problem-detail/default |
| [Facts](components/facts.html) | one | static | problem-detail/default, problem-detail/resolved |
| [Status](components/status.html) | draft, active, reserved, sold, removed, info | static | problem-detail/default, problem-detail/resolved |
| [Marketing hero](components/marketing-hero.html) | hero, closing | static | welcome, pitch-video |
| [Shapes](components/shapes.html) | full, quiet | drifting, reduced motion | welcome, pitch-video |
| [Feature band](components/feature-band.html) | default, surface, stacked | static | welcome, pitch-video |
| [Steps](components/steps.html) | one | static | welcome, pitch-video |
| [Video poster](components/video-poster.html) | one | default, hover, focus | welcome, pitch-video |
| [Video player](components/video-player.html) | one | loading, playing, paused, captions, error | welcome, pitch-video |

## Patterns

| Pattern | Covers |
|---|---|
| [Navigation](patterns/navigation.html) | Skip link, top bar, sheet, subnav and footer together |

## Tokens

- `tokens/tokens.css` — source of truth: Giddy's tokens verbatim, then the "TuTuMuch brand palette" block
  (raspberry, lilac, sunny, cream and plum scales and the semantic overrides), then the "TuTuMuch layout tokens"
  block (older layout names such as `--color-brand`, `--photo-bg-*` and the ink/paper/stone ramps, each an alias).
- `tokens/tokens.json` — DTCG export generated by `tokens_to_json.py`; don't edit it by hand.
- `tokens/contrast-pairs.json` — Giddy's pairs plus TuTuMuch's (toast status icons, raspberry text on Blush); all pass in both themes.
- `assets/logo.png`, `logo-dark.png` (same image; it reads on both themes), `logo.svg` / `logo-dark.svg` (the PNG embedded),
  `app-icon.png` / `app-icon.svg` and `favicon.ico` (the logo's first T).
- `assets/components.css` — component stylesheet extracted from the mocks' `ui.css`.

## Drift found in the mocks

| Mock | Issue | Resolution |
|---|---|---|
| `assets/tokens.css` | Giddy's `--layout-breakpoint-*` tokens (40, 48, 64, 80, 96rem) and its grid switches at 48rem and 64rem differ from the `L2-061` breakpoints (36, 48, 62, 75rem) that `ui.css` uses in its media queries | Documented on the Layout page; media queries keep `L2-061`. The token values are Giddy's and are not changed here |
| `assets/ui.css` | Size tag, photo stamps and the confirmation mark were cut with a 45° clip-path and drawn in the old dark and light primitives | Size tag is an accent pill, stamps are pills on surface or inverse, the mark is round; all through semantic tokens |
| `assets/ui.css` | Square buttons, chips, badges, switches, avatars, status marks and photos | Giddy shapes: pills for controls, `--radius-lg` photos and tiles, round dots |
| `assets/ui.css` | Admin tag used white text on the brand colour, which fails in dark mode | `--color-fg-on-accent` on `--color-accent` |
| `assets/ui.css` | The logo was an inline SVG of the old wordmark | The owner's logo image (`assets/logo.png`) inside the same link |

## Verification

- `python3 .claude/skills/extracting-design-systems/scripts/check_contrast.py docs/design-system/tokens/tokens.css`: 138 checks (light and dark); see the latest run.
- `python3 .claude/skills/extracting-design-systems/scripts/tokens_to_json.py docs/design-system/tokens/tokens.css` regenerates `tokens.json`.
- `python3 .claude/skills/extracting-design-systems/scripts/check_design_system.py docs/design-system`: 57 pages, 0 errors, 0 warnings.
