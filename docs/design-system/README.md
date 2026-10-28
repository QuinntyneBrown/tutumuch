# TuTuMuch design system

Extracted from [docs/mocks](../mocks/README.md) on 2026-10-25; redrawn to the page guidelines on 2026-10-27. Open [index.html](index.html) locally; press `t` to toggle dark mode.

TuTuMuch uses the Giddy design system drawn to the TuTuMuch [page guidelines](../page-guidelines.md), as
mapped onto this stack by [ADR general/0009](../adr/general/0009-page-guidelines-on-the-angular-and-giddy-stack.md).
The palette, fonts and type scale come from the TuTuMuch brand kit (Magic Patterns: `tailwind.config.js`,
`index.css`, `data/brand.ts`): **Raspberry** (primary buttons, links, active states), **Lilac** (selected chips
and small accents, never text), **Blush** (soft sections, tags, skeletons), **Sunny** (Like new and deals, with
ink text), **Cream** (the page), **Ink** and **Muted** (text). Fraunces sets headings and prices, DM Sans
everything else, Titan One only the logo; all three are self-hosted under the OFL. Cards are flat (no
shadows), panels are 2rem, cards and photos 1.5rem, inputs 1rem, buttons and chips pills; motion is 150 to
250ms ease-out. There are no gradients, blobs or decorative shapes, and no press lip. Dark mode is a plum
night with a lighter raspberry accent. Brand assets: the gingham wordmark (`tl-logo`, `assets/logo.png`), the
gingham "T" mark (`tl-mark`) and Tilly, the mascot (`tl-tilly`, `assets/tilly/`); the app icon and favicon
are the Tilly face tile.

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
the marketing page added the marketing hero, feature band, steps, video poster and video player, and the
showcase tokens they use. The page guidelines (ADR general/0009) added the mark, Tilly, the condition badge
and the skeleton, redrew every component to the brand kit and retired the decorative shapes. Request-items added the bell icon (the empty My requests page) and uses the removed tone of the
status for an expired request; the request list on My requests is local UI of `@tutumuch/catalog`.

## Foundations

| Page | Covers |
|---|---|
| [Color](foundations/color.html) | Brand palette and roles, ramps, selection, highlight, skeleton and condition tokens, logo-art colours, status, the plum night, contrast pairs; no gradients |
| [Typography](foundations/typography.html) | Fraunces, DM Sans and Titan One roles, the brand-kit type scale, sentence case, self-hosted OFL fonts |
| [Spacing](foundations/spacing.html) | 4px scale, inside/between rules |
| [Layout](foundations/layout.html) | Page skeleton, 72rem content and 80rem product grids, 4/3/2 grid columns, spacing not borders, breakpoints, shells |
| [Elevation](foundations/elevation.html) | No card shadows; soft shadows on menus, toasts, dialogs and sheets only; z-order |
| [Shape](foundations/shape.html) | 2rem panels, 1.5rem cards and images, 1rem inputs, pills |
| [Motion](foundations/motion.html) | 150/200/250ms on cubic-bezier(0.23, 1, 0.32, 1), named properties, no entrance animations, reduced motion |
| [Iconography](foundations/iconography.html) | `tl-icon` set and sizes, labelled icon-only buttons, raspberry error icons |
| [Theming](foundations/theming.html) | Token tiers, light and plum-night roles, adding a theme |
| [Responsive](foundations/responsive.html) | Per-component rules and testing matrix |
| [Accessibility](foundations/accessibility.html) | WCAG 2.2 AA: one h1, landmarks, visible labels, aria-labels, alt text including Tilly's, contrast, focus ring |
| [Content](foundations/content.html) | Voice, buttons as verbs, standard terms (the closet, Sell outgrown gear, condition scale), formulas, microcopy |

## Components

| Component | Variants | States | Source mocks |
|---|---|---|---|
| [Skip link](components/skip-link.html) | one | hidden, focused | every page |
| [Top bar](components/top-bar.html) | guest, member, admin, bare, launch phases; logo `tone` color or reversed; responsive mark | default, current | catalog/guest, catalog/default, problem-detail, maintenance, catalog/phase-one |
| [Footer](components/footer.html) | one: the mark, short links, © TuTuMuch | default, hover | catalog |
| [Mark](components/mark.html) | one; 16 to 64px | static, short wait | catalog/default, verify-email/loading, order-confirmation/loading |
| [Tilly](components/tilly.html) | hero, dance, soccer, volleyball, gymnastics, swimming (tile), show-and-tell, hello | static | welcome, catalog/category-dance, catalog/empty, sell/success |
| [Consent banner](components/consent-banner.html) | one | shown until a choice, then gone | notifications/cookie-consent/info |
| [Toast](components/toast.html) | info, success, warning, danger; optional action; `ToastService` | entering, leaving, paused | notifications/toast |
| [Sheet](components/sheet.html) | navigation, filter | open | dialogs/nav-menu, dialogs/filters |
| [Button](components/button.html) | primary (raspberry, hover ink), secondary (ringed), ghost, link, danger; one primary per view | default, hover, focus, active, disabled, busy | catalog/guest, sell/success, welcome |
| [Provider button](components/provider-button.html) | google; with the "or" divider | default, hover, focus, active, busy, disabled | sign-in/default, sign-in/google-busy, register/default |
| [Link](components/link.html) | inline, nav, footer | default, hover, focus, current | catalog, rules |
| [Form field](components/form-field.html) | input, textarea, select | default, hover, focus, invalid, disabled, read only | sign-in, sell |
| [Alert](components/alert.html) | info, success, warning, danger; banner without title; projected actions; dismissible | static, polite, assertive | sign-in/invalid, sell/draft-saved, verify-email-prompt, payout-banner |
| [Center state](components/center-state.html) | blocked, limited, error, success; icon or status code | static | sell/unverified, sell/limited, sell/error, sell/success, listing-detail/unavailable, listing-detail/error |
| [Chip](components/chip.html) | selected (lilac at 15%, lilac ring), with remove button | default, hover, focus | catalog/filtered, catalog/no-results |
| [Filter chips](components/filter-chips.html) | one pressed toggle chip per value, each with a count; pressed is lilac | default, hover, focus, pressed | my-listings/default, my-listings/filtered |
| [Choice](components/choice.html) | plain, with links | unticked, ticked, invalid | register, sell |
| [Dialog](components/dialog.html) | default, with top alert; info or danger icon tone | default, busy, sent, limited | dialogs/verify-email-prompt, dialogs/cancel-order |
| [Earnings](components/earnings.html) | one | unknown, known | sell/filled |
| [Error summary](components/error-summary.html) | one | shown and focused | sell/invalid, register/invalid |
| [Fixed size](components/fixed-size.html) | Girls' | absent, shown | sell/filled |
| [Icon](components/icon.html) | 19 glyphs (search added by search-and-filter-catalog, flag by resolve-order-problems, bell by request-items) | static | every page |
| [Input group](components/input-group.html) | leading addon | default, invalid | sell price and shipping |
| [Item row](components/item-row.html) | with or without thumbnail | default, truncated | sell/success |
| [Empty state](components/empty-state.html) | Tilly (hello) by default; error (compact, icon well); `[tilly]="false"` | static | catalog/empty, my-listings/empty, catalog/error |
| [Listing card](components/listing-card.html) | active, reserved, sold (greyed photo); photo, title, brand, "Girls · condition", price; grid of 2, 3 or 4 columns | default, hover, focus, loading skeleton | catalog/default, catalog/loading, catalog/category-dance, listing-detail/sold |
| [Load more](components/load-more.html) | one | default, busy, hidden when all shown | catalog/default |
| [Gallery](components/gallery.html) | one photo, several photos | selected thumbnail, swiped | listing-detail/default |
| [Buy bar](components/buy-bar.html) | Buy now, Edit listing | sticky below 992 px, inline from 992 px | listing-detail/default, listing-detail/guest, listing-detail/own |
| [Sold notice](components/sold-notice.html) | first time, already requested; one action | static, announced politely, heading takes focus | listing-detail/showcase-sold, listing-detail/showcase-requested |
| [Seller summary](components/seller-summary.html) | established, new seller | static | listing-detail/default, listing-detail/new-seller |
| [Rating summary](components/rating-summary.html) | rated; "No reviews yet" | static | profile/default, profile/own, profile/empty |
| [Order summary](components/order-summary.html) | priced, not yet priced | static | checkout/default, checkout/new-address |
| [Reservation timer](components/reservation-timer.html) | info, warning, danger | counting, last minute, ended | checkout/default, reservation-timer/warning, reservation-timer/danger |
| [Order timeline](components/order-timeline.html) | paid, shipped, delivered | done, current, to come | sale-detail/shipped, order-detail/default, order-detail/paid |
| [Tracking link](components/tracking-link.html) | USPS, UPS, FedEx, DHL | link states | sale-detail/shipped, order-detail/default |
| [Photo uploader](components/photo-uploader.html) | listing (cover, reorder), evidence (`[cover]="false"`, report-problem) | empty, uploading, filled, rejected, invalid, full | sell/default, sell/filled, sell/invalid |
| [Segmented control](components/segmented.html) | one | empty, chosen, invalid | sell condition |
| [Size tag](components/size-tag.html) | Girls; default, large | static | catalog/category-dance, listing-detail |
| [Condition badge](components/condition-badge.html) | new (raspberry), like new (sunny), gently loved (blush), well loved (white, ink ring) | static | listing-detail/default, listing-detail/sold |
| [Spinner](components/spinner.html) | in place inside a busy control (button, load more, video) | running | sell/submitting, pitch-video/loading |
| [Skeleton](components/skeleton.html) | text, title, photo, rect, circle | loading (`aria-busy` on the container) | catalog/loading, listing-detail/loading |
| [Stat](components/stat.html) | in a `.stats` row | static, loading skeleton | payouts/default, payouts/loading |
| [Switch](components/switch.html) | on, off, locked on; hidden label or label with hint | off, on, focus, disabled | notification-settings/default, notification-settings/submitting |
| [Quote](components/quote.html) | one | static | order-detail/cancelled, sale-detail/cancel-requested, respond-cancellation |
| [Radio group](components/radio-group.html) | one | nothing chosen, chosen, invalid | cancel-order/default, cancel-order/invalid |
| [Thread](components/thread.html) | one | static, with photos | order-detail/problem, sale-detail/problem, problem-detail/default |
| [Facts](components/facts.html) | one | static | problem-detail/default, problem-detail/resolved |
| [Status](components/status.html) | draft, active, reserved, sold, removed, info | static | problem-detail/default, problem-detail/resolved |
| [Marketing hero](components/marketing-hero.html) | hero (solid blush, Tilly above the poster, one primary), closing (secondary only) | static | welcome, pitch-video |
| [Feature band](components/feature-band.html) | blush, surface, default, stacked | static | welcome |
| [Steps](components/steps.html) | one | static | welcome |
| [Video poster](components/video-poster.html) | one | default, hover, focus | welcome, pitch-video |
| [Video player](components/video-player.html) | one | loading, playing, paused, captions, error | welcome, pitch-video |

## Patterns

| Pattern | Covers |
|---|---|
| [Navigation](patterns/navigation.html) | Skip link, top bar, sheet, subnav and footer together |

## Tokens

- `tokens/tokens.css` — source of truth: the brand-kit palette as primitives (the only hex values), the semantic
  roles (colour, Fraunces/DM Sans/Titan One type scale, radii, shadows, motion, condition-badge and selected-chip
  tokens, logo-art tokens), the plum-night dark theme, then the "TuTuMuch layout tokens" block (12L names such
  as `--color-brand` and the ink/paper/signal ramps, each an alias).
- `tokens/tokens.json` — DTCG export generated by `tokens_to_json.py`; don't edit it by hand.
- `tokens/contrast-pairs.json` — Giddy's pairs plus TuTuMuch's (toast status icons, raspberry text on Blush); all pass in both themes.
- `assets/logo.png`, `logo-dark.png` (same image; it reads on both themes), `logo.svg` / `logo-dark.svg` (the PNG embedded),
  `app-icon.png` / `app-icon.svg` and `favicon.ico` (the Tilly face tile).
- `assets/tilly/` — Tilly's cut-out poses as WebP plus the framed swimming tile, with `ATTRIBUTION.md`.
- `assets/fonts/` and `assets/fonts.css` — self-hosted Fraunces, DM Sans and Titan One (SIL OFL).
- `assets/components.css` — component stylesheet extracted from the mocks' `ui.css`.

## Drift found in the mocks

| Mock | Issue | Resolution |
|---|---|---|
| `assets/tokens.css` | Giddy's `--layout-breakpoint-*` tokens (40, 48, 64, 80, 96rem) and its grid switches at 48rem and 64rem differ from the `L2-061` breakpoints (36, 48, 62, 75rem) that `ui.css` uses in its media queries | Documented on the Layout page; media queries keep `L2-061`. The token values are Giddy's and are not changed here |
| `assets/ui.css` | Size tag, photo stamps and the confirmation mark were cut with a 45° clip-path and drawn in the old dark and light primitives | Size tag is an accent pill, stamps are pills on surface or inverse, the mark is round; all through semantic tokens |
| `assets/ui.css` | Square buttons, chips, badges, switches, avatars, status marks and photos; later a press lip, soft card shadows, party-pop photo wells, a gradient showcase with drifting shapes and system rounded fonts | Page guidelines (ADR general/0009): pills for controls, flat cards, white `--radius-card` photo wells, a solid blush showcase, Fraunces and DM Sans; the shapes are retired |
| `assets/ui.css` | Admin tag used white text on the brand colour, which fails in dark mode | `--color-fg-on-accent` on `--color-accent` |
| `assets/ui.css` | The logo was an inline SVG of the old wordmark | The owner's logo image (`assets/logo.png`) inside the same link |

## Verification

- `python3 .claude/skills/extracting-design-systems/scripts/check_contrast.py docs/design-system/tokens/tokens.css`: 138 checks (light and dark); see the latest run.
- `python3 .claude/skills/extracting-design-systems/scripts/tokens_to_json.py docs/design-system/tokens/tokens.css` regenerates `tokens.json`.
- `python3 .claude/skills/extracting-design-systems/scripts/check_design_system.py docs/design-system`: 57 pages, 0 errors, 0 warnings.
