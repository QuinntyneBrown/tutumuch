# TuTuMuch — Page Guidelines for Agents

Follow these rules whenever you create or edit a page for the TuTuMuch web app. When something isn't covered here, choose whatever keeps the page **joyful for girls, trustworthy for parents, and easy to shop**.

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
| Wordmark | `components/Logo.tsx` → `<Logo />` | Always use this component. Never retype "TuTuMuch" in a font to fake the logo. |
| Gingham wordmark | `<Logo muchFill="checker" />` | **Primary logo** for the site header and marketing. |
| Reversed wordmark | `<Logo tone="reversed" />` | Only on Raspberry backgrounds. |
| Mark / favicon | `components/Mark.tsx` → `<Mark variant="tGingham" />` | Use for avatars, the compact header on mobile, and loading states. |
| Tilly (mascot) | `data/brand.ts` → `brandAssets.characterHero`, `sportsPoses`, `characterPoses` | See section 6. |
| Colors, type, poses | `data/brand.ts`, `tailwind.config.js` | Read before building. |

### Logo rules
- **Header:** `<Logo size={32} muchFill="checker" />` on desktop, `<Mark size={36} />` on screens under 640px.
- **Minimum size:** 28px for the wordmark, 16px for the mark.
- **Clear space:** at least the height of the "u" on every side. Nothing else goes inside that space.
- **Backgrounds:** white, Cream, Blush or Raspberry (reversed) only. Never put it on a photo, a pattern or Lilac.
- **One logo per page:** don't repeat it in the body. The footer may use the mark.
- **Never** stretch, recolor, outline, animate letter by letter, or add ".kids" to it.

---

## 3. Color

Use the Tailwind tokens. **Never hardcode hex values or Tailwind defaults like `pink-500` or `slate-600`.**

| Token | Hex | Use for |
|---|---|---|
| `raspberry` | #D6336C | Primary buttons, links, active states, sale prices. One main action per screen. |
| `lilac` | #9B7FD4 | Small accents, selected filter chips, decoration. **Not for text on light backgrounds.** |
| `blush` | #FDE2EA | Soft section backgrounds, tags, hover backgrounds. |
| `sunny` | #F6C64B | Condition badges ("Like new"), deal highlights. Use sparingly; always with `ink` text. |
| `cream` | #FFF8F5 | Default page background. |
| `ink` | #3A2233 | All headings and body text. |
| `muted` | #7A6370 | Secondary text, metadata, captions. |
| `white` | — | Cards, listing photo backgrounds, form fields. |

### Contrast rules
- Body text is always `ink` or `muted` on `cream`, `white` or `blush`.
- White text on `raspberry` only at **16px semibold or larger**, so it stays readable.
- Never put light text (white, blush, sunny) on a light background.
- **No gradients. No blobs, glows or decorative shapes.** The only patterns allowed are the brand's polka dot and gingham, and only inside the logo and mark.

---

## 4. Typography

| Role | Font | Tailwind | Notes |
|---|---|---|---|
| Logo only | Titan One | handled by `<Logo />` | **Never** use Titan One for headings or UI. |
| Headings, prices | Fraunces | `font-display font-semibold tracking-tight` | Soft, storybook warmth. |
| Body, buttons, labels, forms | DM Sans | `font-body` (default) | Clean and easy to scan. |

### Type scale
| Style | Classes |
|---|---|
| Page title (H1) | `font-display text-4xl md:text-5xl font-semibold tracking-tight text-ink` |
| Section heading (H2) | `font-display text-3xl font-semibold tracking-tight text-ink` |
| Subheading (H3) | `font-display text-[1.375rem] font-medium leading-7 text-ink` |
| Body | `text-base md:text-lg leading-7 text-ink` |
| Secondary | `text-sm text-muted` |
| Price | `font-display text-xl font-semibold tabular-nums text-ink` |
| Original price | `text-sm text-muted line-through tabular-nums` |

- One H1 per page. Never skip heading levels.
- Use sentence case everywhere: "Shop soccer gear", not "Shop Soccer Gear".
- **No all-caps letterspaced labels above headings.** No emoji.
- Keep body text to about 70 characters per line (`max-w-2xl`).

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
- Home page hero (use `brandAssets.characterHero`)
- Sport category headers, with the matching pose from `sportsPoses` (Dance, Volleyball, Soccer, Gymnastics, Swimming)
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

- **Page shell:** `min-h-screen w-full bg-cream font-body text-ink`
- **Content width:** `mx-auto max-w-6xl` with `px-6 md:px-10`. Product grids may go to `max-w-7xl`.
- **Section spacing:** `py-16 md:py-20`. Separate sections with spacing or a background change (`bg-white` / `bg-blush`), not borders.
- **Corners:** `rounded-[2rem]` large panels · `rounded-[1.5rem]` cards and images · `rounded-2xl` swatches and inputs · `rounded-full` buttons, chips and badges.
- **Shadows:** none on cards. A soft shadow is allowed only on floating elements (menus, the bag drawer, modals).
- **Design for desktop first and use the width.** Product grids are 4 columns on desktop, 3 on tablet and 2 on mobile, never 1.
- Don't wrap everything in bordered cards. Group with spacing, headings and thin `border-ink/10` lines.

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
1. Photo (`aspect-[4/5]`, `bg-white`, `rounded-[1.5rem]`, `object-cover`)
2. Title (one line, truncated): "Recital tutu, raspberry"
3. Size · Condition: "Size 6 · Like new" (`text-sm text-muted`)
4. Price and original price
5. Heart button (save) in the top-right corner of the photo, which needs an `aria-label`

Cards in a row must line up: use a shared grid and `mt-auto` on the price row.

### Condition badge
`rounded-full px-3 py-1 text-sm font-semibold` · New with tags = `bg-raspberry text-white` · Like new = `bg-sunny text-ink` · Gently loved = `bg-blush text-ink` · Well loved = `bg-white text-ink ring-1 ring-ink/15`.

### Buttons
- **Primary:** `rounded-full bg-raspberry px-6 py-3 font-semibold text-white` with hover `bg-ink`
- **Secondary:** `rounded-full bg-white px-6 py-3 font-semibold text-raspberry ring-2 ring-inset ring-raspberry`
- **Text link:** `font-semibold text-raspberry hover:text-ink underline-offset-4 hover:underline`
- **Only one primary button per view.** Every button needs a visible focus style (`focus-visible:ring-2 focus-visible:ring-raspberry focus-visible:ring-offset-2`).

### Filters
- Show the 3 most useful filters inline: **Sport, Size, Condition**. Put everything else (brand, color, price) in a "More filters" drawer.
- Selected chips: `bg-lilac/15 text-ink ring-1 ring-lilac`.

### Categories (top navigation)
Dance · Volleyball · Soccer · Gymnastics · Swimming · Everyday · Party & occasion · Shoes · Accessories

---

## 9. States (always design them)

| State | Treatment |
|---|---|
| Loading | Skeleton blocks in `bg-blush`, shaped like the real content. No spinners for page loads. |
| Empty | Tilly ("Hello, friend") + one short line + one action. For example: "Your bag is empty. Let's find something to twirl in." → **Shop the closet** |
| Error | Plain language, what happened and what to do. `text-raspberry` icon, never all-red screens. |
| Success | Short toast (use `sonner`), e.g. "Added to your bag". Use Tilly's "Show & Tell" pose only for big moments like a first listing. |
| Sold out | Grey the photo (`opacity-60`), show a "Sold" badge, and offer "Find similar". |

---

## 10. Motion

- Use `framer-motion`. Keep it quick: 150–250ms, `ease-out` (`cubic-bezier(0.23, 1, 0.32, 1)`).
- Animate only what helps: adding to bag, drawers opening, a heart filling when saved.
- **No page-load entrance animations on every section.** No bouncing or wiggling UI. The logo already bounces.
- Never use `transition-all`. Respect `prefers-reduced-motion`.

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

- React + TypeScript + Tailwind. Named exports only.
- `pages/` one file per route · `components/` reusable UI · `data/` mock listings and content · `types/` shared types · `utils/` pure helpers.
- Mock listings live in `data/listings.ts` as an array and are rendered with one `.map()`.
- Icons come from `lucide-react` with the `Icon` suffix (`HeartIcon`, `ShoppingBagIcon`).
- Keep every file under 500 lines.

---

## 14. Before you finish (checklist)

- [ ] The gingham `<Logo />` is in the header, and there's only one logo on the page
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
