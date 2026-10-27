# TuTuMuch — Page Guidelines for Agents

Follow these rules whenever you create or edit a page for the TuTuMuch web app. The rules came from the TuTuMuch brand kit (a React and Tailwind app in Magic Patterns); [ADR general/0009](adr/general/0009-page-guidelines-on-the-angular-and-giddy-stack.md) maps them onto TuTuMuch's Angular workspace, the Giddy tokens and the `tl-*` components of `@tutumuch/components`, and lists what a domain change must wait for. When something isn't covered here, choose whatever keeps the page **joyful for girls, trustworthy for parents, and easy to shop**.

---

## 1. Who we are

- **What:** An online marketplace for **new and gently used girls' clothing and gear**. Girls only.
- **Who shops:** Mostly parents, especially families raising **Black girls**. Girls often browse alongside them.
- **What the girls love:** **Dance, volleyball, soccer, gymnastics and swimming.** Sports gear is a core category, not a side category.
- **Promise:** Girls grow out of things fast. TuTuMuch passes outgrown pieces on to the next girl, so families save money and good clothes keep getting worn.
- **Feel:** Playful like a sticker sheet, but tidy like a good boutique. Fun shows up in the logo, Tilly and small moments. The shopping itself stays calm and clear.

---

## 2. Brand assets (use these, never recreate them)

| Asset | Where | Rule |
|---|---|---|
| Wordmark | `tl-logo` (`src/frontend/projects/components/src/lib/logo/`) | Always use this component. Never retype "TuTuMuch" in a font to fake the logo. |
| Gingham wordmark | `<tl-logo />` (gingham "Much" is its fill) | **Primary logo** for the site header and marketing. |
| Reversed wordmark | `<tl-logo tone="reversed" />` | Only on Raspberry backgrounds. |
| Mark | `<tl-mark />` (the gingham "T") | Use for the compact header on mobile, the footer and loading states. The favicon and app icon stay the Tilly face tile. |
| Tilly (mascot) | `<tl-tilly pose="…" />`; artwork in `public/tilly/` (copied from `docs/print/characters/`) | See section 6. |
| Colors, type, poses | `docs/design-system/tokens/tokens.css`, `docs/design-system/` (foundations, `tilly.html`) | Read before building. |
| Mocks | `docs/mocks/_build/` (`mk.py`, `data.py`, `screens_*.py`) | Mocks use the same assets as copies under `docs/mocks/assets/`. |

### Logo rules
- **Header:** `<tl-logo />` on desktop, `<tl-mark [size]="36" />` on screens under 640px (`tl-top-bar` switches them).
- **Minimum size:** 28px for the wordmark, 16px for the mark.
- **Clear space:** at least the height of the "u" on every side. Nothing else goes inside that space.
- **Backgrounds:** white, Cream, Blush or Raspberry (reversed) only. Never put it on a photo, a pattern or Lilac.
- **One logo per page:** don't repeat it in the body. The footer may use the mark.
- **Never** stretch, recolor, outline, animate letter by letter, or add ".kids" to it.

---

## 3. Color

Use the semantic tokens of `docs/design-system/tokens/tokens.css` (`--color-*`). **Never hardcode hex values** outside the primitives tier of that file.

| Colour | Hex | Token | Use for |
|---|---|---|---|
| `raspberry` | #D6336C | `--color-accent` (text and links: `--color-fg-link`, raspberry 700) | Primary buttons, links, active states, sale prices. One main action per screen. |
| `lilac` | #9B7FD4 | `--color-selected-border`, `--color-selected-bg` | Small accents, selected filter chips, decoration. **Not for text on light backgrounds.** |
| `blush` | #FDE2EA | `--color-bg-blush`, `--color-skeleton` | Soft section backgrounds, tags, hover backgrounds. |
| `sunny` | #F6C64B | `--color-highlight` | Condition badges ("Like new"), deal highlights. Use sparingly; always with `ink` text. |
| `cream` | #FFF8F5 | `--color-bg-canvas` | Default page background. |
| `ink` | #3A2233 | `--color-fg-default` | All headings and body text. |
| `muted` | #7A6370 | `--color-fg-muted` | Secondary text, metadata, captions. |
| `white` | — | `--color-bg-surface` | Cards, listing photo backgrounds, form fields. |

### Contrast rules
- Body text is always `ink` or `muted` on `cream`, `white` or `blush`.
- White text on `raspberry` only at **16px semibold or larger**, so it stays readable.
- Never put light text (white, blush, sunny) on a light background.
- **No gradients. No blobs, glows or decorative shapes.** The only patterns allowed are the brand's polka dot and gingham, and only inside the logo and mark.

---

## 4. Typography

| Role | Font | Token | Notes |
|---|---|---|---|
| Logo only | Titan One | `--font-family-logo`, used only by `tl-logo` and `tl-mark` | **Never** use Titan One for headings or UI. |
| Headings, prices | Fraunces | `--font-family-display`, semibold, tight tracking | Soft, storybook warmth. |
| Body, buttons, labels, forms | DM Sans | `--font-family-sans` (default) | Clean and easy to scan. |

Fraunces, DM Sans and Titan One are self-hosted under the SIL Open Font License in `src/frontend/projects/tutumuch/public/fonts/`.

### Type scale
| Style | Token | Value |
|---|---|---|
| Page title (H1) | `--text-h1` | Fraunces 600, 36px, 48px from 768px, tight, ink |
| Section heading (H2) | `--text-h2` | Fraunces 600, 30px, tight, ink |
| Subheading (H3) | `--text-h3` | Fraunces 500, 22/28px, ink |
| Body | `--text-body` | DM Sans 400, 16/28px, 18/28px from 768px, ink |
| Secondary | `--text-body-sm` + `--color-fg-muted` | DM Sans 14px, muted |
| Price | `--text-price` | Fraunces 600, 20px, tabular figures, ink |
| Original price | `--text-price-original` | 14px, muted, struck through, tabular figures |

- One H1 per page. Never skip heading levels.
- Use sentence case everywhere: "Shop soccer gear", not "Shop Soccer Gear".
- **No all-caps letterspaced labels above headings.** No emoji.
- Keep body text to about 70 characters per line (`--layout-measure`).

---

## 5. Voice and copy

- **Warm, short and confident.** Write like a friendly mom at the team picnic, not a corporate brand.
- Speak to the parent; let Tilly speak to the girl.
- Celebrate girls' activity and confidence: "built for practice", "ready for the recital", "goals left in them".
- Be honest about condition. Never oversell a used item.
- Don't use "AI", "curated" or "elevate", and don't use exclamation points in UI labels. One exclamation point per page at most, and only in Tilly's voice.
- **Buttons are verbs:** "Shop the closet", "Sell outgrown gear", "Add to bag", "Make an offer".

### Standard terms (always use these exact words)
| Concept | Term |
|---|---|
| Cart | **Bag** |
| Store / catalog | **The closet** |
| Selling | **Pass it on** / **Sell outgrown gear** |
| Condition scale | **New with tags · Like new · Gently loved · Well loved** |
| Sizes | Girls' sizes **2T–16**, then shoe sizes. Always shown as "Size 6" or "Size 2Y". |

---

## 6. Tilly (the mascot)

Tilly is a joyful Black girl, about six, with two afro puffs tied with raspberry bows. She represents our community. Treat her with care.

**Use Tilly for:**
- Home page hero (`<tl-tilly pose="hero" />`, the twirl)
- Sport category headers, with the matching pose (Dance, Volleyball, Soccer, Gymnastics; Swimming only as its framed tile)
- Empty states: the "Hello, friend" pose
- New listing confirmations and "just added" banners: the "Show & Tell" pose
- Welcome emails, onboarding, 404 pages

**Rules:**
- **No more than one Tilly per screen.** She's special; don't use her as wallpaper.
- Never crop off her puffs, recolor her skin or hair, flip her, or stretch her.
- Don't generate new versions of Tilly unless asked. If you must, keep: deep brown skin, two afro puffs, raspberry bows, flat picture-book style, cream background.
- Never put her next to product photos in a way that makes her look like a model wearing the item.
- Her catchphrase is "Never too much tutu!". Use it once at most, in marketing moments only.
- Alt text names her and what she's doing: `alt="Tilly kicking a soccer ball"`.

---

## 7. Layout

- **Page shell:** full height, `--color-bg-canvas`, DM Sans, `--color-fg-default` (the app shell sets these)
- **Content width:** `.container` (72rem, 24px side padding, 40px from 768px). Product grids may go to 80rem.
- **Section spacing:** `--space-16`, `--space-20` from 768px. Separate sections with spacing or a background change (white / blush), not borders.
- **Corners:** `--radius-panel` (2rem) large panels · `--radius-card` (1.5rem) cards and images · `--radius-input` (1rem) swatches and inputs · `--radius-full` buttons, chips and badges.
- **Shadows:** none on cards. A soft shadow is allowed only on floating elements (menus, the bag drawer, modals).
- **Design for desktop first and use the width.** Product grids are 4 columns on desktop, 3 on tablet and 2 on mobile, never 1.
- Don't wrap everything in bordered cards. Group with spacing, headings and thin `ink/10` lines (`--color-border-default`).

### Standard page skeleton
1. **Header:** logo left; search in the middle; Sell, Account and Bag (with a count) on the right. Sport categories in a second row.
2. **Page H1** with an optional one-line description.
3. **Primary content**, which is the thing the user came for.
4. **One supporting section** at most (recently viewed, more from this seller, etc.).
5. **Footer:** mark, short links, "© TuTuMuch".

---

## 8. Marketplace patterns

### Listing card
Photo first. Show only what helps a parent decide at a glance:
1. Photo (4:5, white well, `--radius-card`, cover-fitted)
2. Title (one line, truncated): "Recital tutu, raspberry"
3. Size · Condition: "Size 6 · Like new" (`--text-body-sm`, muted)
4. Price and original price
5. Heart button (save) in the top-right corner of the photo, which needs an `aria-label`

Cards in a row must line up: use `tl-listing-grid` and `tl-listing-card`, which push the price row down with `margin-top: auto`.

### Condition badge
`<tl-condition-badge [condition]="…" />`: a pill, 14px semibold · New with tags = raspberry with white text · Like new = sunny with ink text · Gently loved = blush with ink text · Well loved = white with ink text and a thin `ink/15` ring.

### Buttons
- **Primary:** `tlButton="primary"`: a raspberry pill with a white semibold label, hover ink
- **Secondary:** `tlButton`: a white pill with a raspberry semibold label and a 2px inset raspberry ring
- **Text link:** `tlButton="link"`: semibold raspberry, hover ink with an underline offset 4px
- **Only one primary button per view.** Every button needs a visible focus style: a 2px raspberry ring with a 2px offset (`--shadow-focus`), which `tlButton` provides.

### Filters
- Show the 3 most useful filters inline: **Sport, Size, Condition**. Put everything else (brand, color, price) in a "More filters" drawer.
- Selected chips: lilac at 15% with ink text and a thin lilac ring (`--color-selected-bg`, `--color-selected-border`), as `tl-chip` and `tl-filter-chips` draw them.

### Categories (top navigation)
Dance · Volleyball · Soccer · Gymnastics · Swimming · Everyday · Party & occasion · Shoes · Accessories

---

## 9. States (always design them)

| State | Treatment |
|---|---|
| Loading | `tl-skeleton` blocks in blush, shaped like the real content. No spinners for page loads. |
| Empty | Tilly ("Hello, friend") + one short line + one action. For example: "Your bag is empty. Let's find something to twirl in." → **Shop the closet** |
| Error | Plain language, what happened and what to do. Raspberry icon, never all-red screens. |
| Success | Short toast (`ToastService`), e.g. "Added to your bag". Use Tilly's "Show & Tell" pose only for big moments like a first listing. |
| Sold out | Grey the photo (60% opacity), show a "Sold" badge, and offer "Find similar". |

---

## 10. Motion

- Use CSS transitions with the motion tokens. Keep it quick: 150–250ms (`--duration-fast`, `--duration-base`, `--duration-slow`), `ease-out` (`--ease-out`, `cubic-bezier(0.23, 1, 0.32, 1)`).
- Animate only what helps: adding to bag, drawers opening, a heart filling when saved.
- **No page-load entrance animations on every section.** No bouncing or wiggling UI. The logo already bounces.
- Never use `transition: all`; name the properties. Respect `prefers-reduced-motion` (the tokens drop to zero).

---

## 11. Imagery

- **Product photos:** on white or cream backgrounds, with natural light and the whole item visible.
- **Lifestyle photos:** show **Black girls** first and foremost, being active (on the field, in the studio, at the pool), with natural hairstyles shown with care. Joyful and real, not posed catalog stiffness.
- Every image has meaningful alt text; decorative images use `alt=""`.
- Never use stock photos that don't reflect our girls.

---

## 12. Accessibility

- Semantic HTML: `header`, `nav`, `main`, `section`, `footer`, with one `h1`.
- Every icon-only button has an `aria-label`.
- Full keyboard support: logical tab order and visible focus styles.
- Forms have a visible label for every field (not just placeholder text), and errors appear next to the field.
- Text meets WCAG AA contrast. Check before you finish.

---

## 13. Code conventions

- Angular + TypeScript + the Giddy tokens. Standalone components, signals, lazy routes, selector prefix `tl`, named exports only.
- The workspace layout of `AGENTS.md`: screens in the feature libraries (`src/lib/<screen>/`), reusable UI in `@tutumuch/components`, API types and services in `@tutumuch/api`, shells in the `tutumuch` application.
- Mock listings and people live in `docs/mocks/_build/data.py` for the mocks and in `src/frontend/e2e/fixtures.ts` for the Playwright tests.
- Icons come from `tl-icon` (`<tl-icon name="bag" />`); toasts from `ToastService`.
- Keep every file under 500 lines.

---

## 14. Before you finish (checklist)

- [ ] The gingham `tl-logo` (or `tl-mark` under 640px) is in the header, and there's only one logo on the page
- [ ] All colors are tokens, with no gradients and no hardcoded hex values
- [ ] Fraunces for headings, DM Sans for everything else, Titan One nowhere outside the logo
- [ ] One H1, one primary button per view
- [ ] Tilly appears at most once, in an approved pose, with alt text
- [ ] Listing cards show photo, title, size, condition and price, and line up across the row
- [ ] Loading, empty, error and sold-out states exist
- [ ] Standard terms used: "Bag", "The closet", condition scale
- [ ] Nothing wraps to two lines in buttons or nav at desktop width
- [ ] Contrast checked; focus styles visible; keyboard works
- [ ] The page shows Black girls being active and joyful
