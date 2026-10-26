# Tutu Much design system

Extracted from [docs/mocks](../mocks/README.md) on 2026-10-25. Open [index.html](index.html) locally.

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
consent banner, the visible label and hint of the switch and the footer's Cookie settings button.

## Foundations

| Page | Covers |
|---|---|
| [Color](foundations/color.html) | Ramps, roles, status colours, both themes, contrast pairs |
| [Typography](foundations/typography.html) | Families, scale, roles, measure |
| [Spacing](foundations/spacing.html) | 4px scale, inside/between rules |
| [Layout](foundations/layout.html) | L2-061 breakpoints, grid, container, page templates |
| [Elevation](foundations/elevation.html) | Shadows, surfaces, z-order |
| [Shape](foundations/shape.html) | Radii, borders, chamfer |
| [Motion](foundations/motion.html) | Durations, easings, reduced motion |
| [Iconography](foundations/iconography.html) | Inline SVG set and sizes |
| [Theming](foundations/theming.html) | Token tiers, light/dark, adding a theme |
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
| [Fixed size](components/fixed-size.html) | men's US 12, men's L | absent, shown | sell/filled |
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
| [Size tag](components/size-tag.html) | US 12, L; default, large | static | sell, catalog |
| [Spinner](components/spinner.html) | with message | running | verify-email/loading |
| [Stat](components/stat.html) | in a `.stats` row | static, loading skeleton | payouts/default, payouts/loading |
| [Switch](components/switch.html) | on, off, locked on; hidden label or label with hint | off, on, focus, disabled | notification-settings/default, notification-settings/submitting |
| [Quote](components/quote.html) | one | static | order-detail/cancelled, sale-detail/cancel-requested, respond-cancellation |
| [Radio group](components/radio-group.html) | one | nothing chosen, chosen, invalid | cancel-order/default, cancel-order/invalid |
| [Thread](components/thread.html) | one | static, with photos | order-detail/problem, sale-detail/problem, problem-detail/default |
| [Facts](components/facts.html) | one | static | problem-detail/default, problem-detail/resolved |
| [Status](components/status.html) | draft, active, reserved, sold, removed, info | static | problem-detail/default, problem-detail/resolved |

## Patterns

| Pattern | Covers |
|---|---|
| [Navigation](patterns/navigation.html) | Skip link, top bar, sheet, subnav and footer together |

## Tokens

- `tokens/tokens.css` — source of truth; `tokens/tokens.json` — DTCG export; `tokens/contrast-pairs.json` — checked pairs.
- `assets/components.css` — component stylesheet extracted from the mocks' `ui.css`.

## Drift found in the mocks

| Mock | Issue | Resolution |
|---|---|---|
| `assets/tokens.css` | `--layout-breakpoint-sm` 40rem, `-lg` 64rem, `-xl` 80rem, plus a 2xl value, disagree with `L2-061` | Tokens set to 36rem, 48rem, 62rem, 75rem; 2xl removed; grid media queries moved to 48rem and 62rem |
| `assets/tokens.css` | Four `--color-inverse-*` lines duplicated inside the dark media block | Duplicates removed |
| `assets/ui.css` | Already uses the `L2-061` values in its media queries | None |

## Verification

- `python .claude/skills/extracting-design-systems/scripts/check_contrast.py docs/design-system/tokens/tokens.css` — see the latest run in the pull request.
- `python .claude/skills/extracting-design-systems/scripts/tokens_to_json.py docs/design-system/tokens/tokens.css`
- `python .claude/skills/extracting-design-systems/scripts/check_design_system.py docs/design-system`
