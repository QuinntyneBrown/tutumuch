# TuTuMuch HTML mocks

Static HTML mocks of every TuTuMuch page, dialog and notification, in every state,
light and dark, from 320 px phones to desktop. They are the product on disk
before the Angular app exists: open them, click through them, tab through them.

## Open the mocks

- Open [`index.html`](index.html) from disk (no server needed). It lists every screen and links each state.
- Every mock has a bar at the bottom with its sibling states and a theme toggle (or press `t`).
- `?theme=dark` or `?theme=light` forces a theme; `?chrome=0` hides the mock bar and notes so the page looks exactly like the product.

## Design direction

The mocks follow [`docs/page-guidelines.md`](../page-guidelines.md), mapped onto the
[Giddy design system](../design-system/README.md) by
[ADR general/0009](../adr/general/0009-page-guidelines-on-the-angular-and-giddy-stack.md). Colours
are the brand kit's, from [`assets/tokens.css`](assets/tokens.css) (a copy of
`docs/design-system/tokens/tokens.css`):

| Colour | In the mocks |
| --- | --- |
| Raspberry | The one primary button per view (hover ink), the ring of secondary buttons, links (raspberry 700), the current-page bar, the "Girls" size tag, New with tags badges. |
| Lilac | Selected chips (lilac at 15% with a lilac ring); never text on light. |
| Blush | Category headers, the marketing hero, skeleton blocks, Gently loved badges. |
| Sunny | Like new badges, always with ink text. |
| Cream | The page canvas. |
| Ink and Muted | Headings and body; secondary text. The dark theme is a plum night with a lighter raspberry accent. |

Type: Fraunces (soft, optical sizing) for headings and prices, DM Sans for everything else, Titan
One only in the logo and the mark; all self-hosted from [`assets/fonts/`](assets/fonts/). Sentence
case, no letterspaced capitals. Shape: 2rem panels, 1.5rem cards and photo wells, 1rem inputs,
pill buttons, chips and badges. No card shadows (only menus, toasts, dialogs and sheets float), no
gradients, no decorative shapes; groups are separated by spacing and thin ink/10 lines. Motion is
150–250ms ease-out and stops under reduced motion.

Page skeleton: the header has the gingham wordmark left (the mark under 640px, inside the same
home link), search in the middle, Sell and Account right, and the six categories in a second row
in the guideline order (Dance, Sports, Clothing, Shoes, Cameras and tech, Toys and crafts).
TuTuMuch has no bag, so there is no Bag link. Then one `h1` with an optional line, the content, at
most one supporting section, and the footer with the mark, short links and "© TuTuMuch".

Listing cards show a white 4:5 photo well, the title on one line, the brand, "Girls' · condition"
and the price, with the price rows aligned. Grids are 4, 3 and 2 columns. Loading is blush
skeletons (a spinner only inside a busy control; the mark for a short wait), empty states show
Tilly waving hello with one line and one action, errors say what happened with a raspberry icon,
sold items are greyed with a "Sold" badge and "Find similar".

Tilly ([`assets/tilly/`](assets/tilly/ATTRIBUTION.md)) appears at most once per screen: twirling
in the marketing hero, in the matching pose on the Dance and Sports headers, waving hello in empty
states and holding up a dress on the "It's live" confirmation. Product photos keep a white well in
both themes, the way real photos would.

## Cast and catalog

"Today" is **Sun 25 Oct 2026**. Every mock uses this data (source: [`_build/data.py`](_build/data.py)).
Members are parents and families selling what their daughters have outgrown.

| Member | Real name | City | Role in the story |
| --- | --- | --- | --- |
| `TutuTessa` | Tessa Bell | Brooklyn, NY | The signed-in member. Buys and sells. 4.8 (23 reviews), 19 sales. |
| `TwirlyTara` | Tara Okafor | Atlanta, GA | Top seller of party dresses and tutus. 4.9 (128 reviews), 141 sales. |
| `HandMeDownHana` | Hana Ferreira | Portland, OR | Coats and knits seller. 4.7 (41 reviews). |
| `PuddleJumperPia` | Pia Reyes | Denver, CO | Buyer of Tessa's hoodie. No listings. |
| `RainbowRosa` | Rosa Kim | Chicago, IL | New seller, no completed sales ("New seller"). |
| Ana Ruiz | — | — | Administrator (resolves order problems). |

Fourteen listings, all girls' clothing (size Girls'): Pink tulle tutu skirt, Floral party dress,
Rainbow stripe romper, Velvet holiday dress, Pleated plaid skirt, Yellow gingham sundress
(Used - Fair, with flaws), Rainbow knit sweater, Pink heart hoodie, Peter Pan collar blouse,
Unicorn graphic T-shirt, Purple puffer jacket, Yellow raincoat (Reserved), Denim leggings,
Floral cotton shorts.

Orders: `TM-48213` (Tessa buys the party dress, Shipped), `TM-48140` (rainbow sweater, Delivered),
`TM-47790` (romper, Completed), `TM-48302` (Tessa sells the hoodie to Pia, Paid, ship by Fri 30 Oct),
`TM-47511` (floral shorts, Completed, payout $33.30 after the 10% fee of $3.70).

## How the mocks are built

The HTML files are generated so that ~250 mocks share one shell, one token file
and one component kit:

```
docs/mocks/
  assets/tokens.css    design tokens (Giddy plus the TuTuMuch palette); the only place colours live
  assets/ui.css        component kit in the Giddy look, including the TuTuMuch layer (size tag, listing card, gallery…)
  assets/items/*.svg   product illustrations used as listing photos
  assets/logo.png      the TuTuMuch logo
  _build/mk.py         shell, components and helpers
  _build/data.py       cast and catalog
  _build/screens_*.py  one module per area: catalog, accounts, selling, orders, system
  _build/build.py      renders every state, writes manifest.json, validates links
```

Change a screen in its `screens_*.py`, then rebuild and re-check:

```sh
python3 docs/mocks/_build/build.py
python3 .claude/skills/writing-html-mocks/scripts/check_mocks.py docs/mocks --write
python3 .claude/skills/writing-html-mocks/scripts/screenshot_mocks.py docs/mocks   # PNGs in docs/mocks/.cache (not committed)
```

## Open questions and assumptions

- **Notifications are email only** (L1-011, L2-042). There is no in-app notification centre, so none is mocked; in-app feedback is toasts and banners. Email templates are not mocked here.
- **Home is the catalog.** `/` shows a one-line hero above the catalog rather than a separate landing page.
- **Payment provider UI** is shown as hosted card fields inside checkout; the provider's own pages (payout onboarding, 3-D Secure) are out of scope.
- **Sales tax** is shown as a single line computed at checkout; rates come from the provider.
- **Category pages** share the catalog template; only Dresses is mocked as its own state.
- **Moderation, member suspension, the audit log, reviews and data export are deferred** (see [ADR 0001](../adr/general/0001-defer-moderation-reviews-and-data-export.md)); nothing is mocked for them.
- **Legal copy** on Terms and Privacy summarises the rules in the specs; final wording comes from counsel.
- **Fonts**: Giddy's rounded system stacks (SF Pro Rounded, Nunito when installed, then the system sans). Mocks load no web fonts so they open offline.

## Coverage

<!-- coverage:start -->

Legend: ✅ mock exists · ➖ not applicable (reason in manifest) · ❌ missing

### Pages

| Screen | default | loading | empty | error | unverified | invalid | submitting | success | expired | own | unavailable | long | return | locked | google-busy | google-cancelled | google-existing | google-failed | used | guest | category | category-dance | search | filtered | phase-one | phase-one-admin | phase-two | no-results | offline | new-seller | reserved | sold | signed-in | selling-opens-soon | new-address | non-us | declined | conflict | paid | cancel-requested | cancel-declined | delivered | completed | cancelled | refunded | problem | shipped | address-hidden | changed | withdrawn | not-onboarded | filled | draft-saved | selling-not-open | product-limit | limited | resolved | Requirements |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Account (`account`) | [✅](pages/account/default.html) | [✅](pages/account/loading.html) | [✅](pages/account/empty.html) | [✅](pages/account/error.html) | [✅](pages/account/unverified.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-003`, `L2-002` |
| Edit profile (`edit-profile`) | [✅](pages/edit-profile/default.html) | [✅](pages/edit-profile/loading.html) | ➖ | [✅](pages/edit-profile/error.html) |  | [✅](pages/edit-profile/invalid.html) | [✅](pages/edit-profile/submitting.html) | [✅](pages/edit-profile/success.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-004`, `L2-017` |
| Forgot password (`forgot-password`) | [✅](pages/forgot-password/default.html) | ➖ | ➖ | [✅](pages/forgot-password/error.html) |  | [✅](pages/forgot-password/invalid.html) | [✅](pages/forgot-password/submitting.html) | [✅](pages/forgot-password/success.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-009` |
| Finish joining with Google (`join-google`) | [✅](pages/join-google/default.html) | [✅](pages/join-google/loading.html) | ➖ | [✅](pages/join-google/error.html) |  | [✅](pages/join-google/invalid.html) | [✅](pages/join-google/submitting.html) |  | [✅](pages/join-google/expired.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-088`, `L2-001`, `L2-065` |
| Email settings (`notification-settings`) | [✅](pages/notification-settings/default.html) | [✅](pages/notification-settings/loading.html) | ➖ | [✅](pages/notification-settings/error.html) |  |  | [✅](pages/notification-settings/submitting.html) | [✅](pages/notification-settings/success.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-042`, `L2-043` |
| Member profile (`profile`) | [✅](pages/profile/default.html) | [✅](pages/profile/loading.html) | [✅](pages/profile/empty.html) | [✅](pages/profile/error.html) |  |  |  |  |  | [✅](pages/profile/own.html) | [✅](pages/profile/unavailable.html) | [✅](pages/profile/long.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-041`, `L2-040`, `L2-054` |
| Join (`register`) | [✅](pages/register/default.html) | ➖ | ➖ | [✅](pages/register/error.html) |  | [✅](pages/register/invalid.html) | [✅](pages/register/submitting.html) | [✅](pages/register/success.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-001`, `L2-002`, `L2-051`, `L2-065`, `L2-088` |
| Reset password (`reset-password`) | [✅](pages/reset-password/default.html) | ➖ | ➖ | [✅](pages/reset-password/error.html) |  | [✅](pages/reset-password/invalid.html) | [✅](pages/reset-password/submitting.html) | [✅](pages/reset-password/success.html) | [✅](pages/reset-password/expired.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-009`, `L2-001` |
| Session expired (`session-expired`) | [✅](pages/session-expired/default.html) | ➖ | ➖ | ➖ |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-008` |
| Settings (`settings`) | [✅](pages/settings/default.html) | [✅](pages/settings/loading.html) | ➖ | [✅](pages/settings/error.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-005`, `L2-008`, `L2-009`, `L2-056` |
| Sign in (`sign-in`) | [✅](pages/sign-in/default.html) | ➖ | ➖ | [✅](pages/sign-in/error.html) |  | [✅](pages/sign-in/invalid.html) | [✅](pages/sign-in/submitting.html) |  |  |  |  |  | [✅](pages/sign-in/return.html) | [✅](pages/sign-in/locked.html) | [✅](pages/sign-in/google-busy.html) | [✅](pages/sign-in/google-cancelled.html) | [✅](pages/sign-in/google-existing.html) | [✅](pages/sign-in/google-failed.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-006`, `L2-007`, `L2-026`, `L2-065`, `L2-088` |
| Verify email (`verify-email`) | [✅](pages/verify-email/default.html) | [✅](pages/verify-email/loading.html) | ➖ | [✅](pages/verify-email/error.html) |  |  |  |  | [✅](pages/verify-email/expired.html) |  |  |  |  |  |  |  |  |  | [✅](pages/verify-email/used.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-002` |
| Catalog (`catalog`) | [✅](pages/catalog/default.html) | [✅](pages/catalog/loading.html) | [✅](pages/catalog/empty.html) | [✅](pages/catalog/error.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | [✅](pages/catalog/guest.html) | [✅](pages/catalog/category.html) | [✅](pages/catalog/category-dance.html) | [✅](pages/catalog/search.html) | [✅](pages/catalog/filtered.html) | [✅](pages/catalog/phase-one.html) | [✅](pages/catalog/phase-one-admin.html) | [✅](pages/catalog/phase-two.html) | [✅](pages/catalog/no-results.html) | [✅](pages/catalog/offline.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-022`, `L2-023`, `L2-024`, `L2-059`, `L2-061`, `L2-062`, `L2-093`, `L2-094` |
| Listing detail (`listing-detail`) | [✅](pages/listing-detail/default.html) | [✅](pages/listing-detail/loading.html) | ➖ | [✅](pages/listing-detail/error.html) |  |  |  |  |  | [✅](pages/listing-detail/own.html) | [✅](pages/listing-detail/unavailable.html) |  |  |  |  |  |  |  |  | [✅](pages/listing-detail/guest.html) |  |  |  |  |  |  |  |  |  | [✅](pages/listing-detail/new-seller.html) | [✅](pages/listing-detail/reserved.html) | [✅](pages/listing-detail/sold.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-025`, `L2-026`, `L2-028`, `L2-016`, `L2-063`, `L2-065` |
| Marketing page (`welcome`) | [✅](pages/welcome/default.html) | ➖ | ➖ | ➖ |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | [✅](pages/welcome/signed-in.html) | [✅](pages/welcome/selling-opens-soon.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-078`, `L2-079`, `L2-080`, `L2-081`, `L2-082`, `L2-083`, `L2-094` |
| Checkout (`checkout`) | [✅](pages/checkout/default.html) | [✅](pages/checkout/loading.html) | ➖ | [✅](pages/checkout/error.html) |  | [✅](pages/checkout/invalid.html) | [✅](pages/checkout/submitting.html) |  | [✅](pages/checkout/expired.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | [✅](pages/checkout/new-address.html) | [✅](pages/checkout/non-us.html) | [✅](pages/checkout/declined.html) | [✅](pages/checkout/conflict.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-027`, `L2-028`, `L2-029`, `L2-030`, `L2-063`, `L2-065` |
| Order confirmed (`order-confirmation`) | [✅](pages/order-confirmation/default.html) | [✅](pages/order-confirmation/loading.html) | ➖ | [✅](pages/order-confirmation/error.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-030`, `L2-033` |
| Order (buyer) (`order-detail`) | [✅](pages/order-detail/default.html) | [✅](pages/order-detail/loading.html) | ➖ | [✅](pages/order-detail/error.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | [✅](pages/order-detail/paid.html) | [✅](pages/order-detail/cancel-requested.html) | [✅](pages/order-detail/cancel-declined.html) | [✅](pages/order-detail/delivered.html) | [✅](pages/order-detail/completed.html) | [✅](pages/order-detail/cancelled.html) | [✅](pages/order-detail/refunded.html) | [✅](pages/order-detail/problem.html) |  |  |  |  |  |  |  |  |  |  |  | `L2-032`, `L2-033`, `L2-034`, `L2-035`, `L2-036` |
| Purchases (`purchases`) | [✅](pages/purchases/default.html) | [✅](pages/purchases/loading.html) | [✅](pages/purchases/empty.html) | [✅](pages/purchases/error.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-037` |
| Sale (seller) (`sale-detail`) | [✅](pages/sale-detail/default.html) | [✅](pages/sale-detail/loading.html) | ➖ | [✅](pages/sale-detail/error.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | [✅](pages/sale-detail/cancel-requested.html) |  |  | [✅](pages/sale-detail/completed.html) | [✅](pages/sale-detail/cancelled.html) |  | [✅](pages/sale-detail/problem.html) | [✅](pages/sale-detail/shipped.html) | [✅](pages/sale-detail/address-hidden.html) |  |  |  |  |  |  |  |  |  | `L2-031`, `L2-032`, `L2-033`, `L2-035`, `L2-036`, `L2-037` |
| Sales (`sales`) | [✅](pages/sales/default.html) | [✅](pages/sales/loading.html) | [✅](pages/sales/empty.html) | [✅](pages/sales/error.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-037`, `L2-033` |
| Edit listing (`edit-listing`) | [✅](pages/edit-listing/default.html) | [✅](pages/edit-listing/loading.html) | ➖ | [✅](pages/edit-listing/error.html) |  | [✅](pages/edit-listing/invalid.html) | [✅](pages/edit-listing/submitting.html) | [✅](pages/edit-listing/success.html) |  |  |  |  |  | [✅](pages/edit-listing/locked.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | [✅](pages/edit-listing/sold.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | [✅](pages/edit-listing/changed.html) | [✅](pages/edit-listing/withdrawn.html) |  |  |  |  |  |  |  | `L2-015`, `L2-016`, `L2-017`, `L2-018`, `L2-020`, `L2-013` |
| Your listings (`my-listings`) | [✅](pages/my-listings/default.html) | [✅](pages/my-listings/loading.html) | [✅](pages/my-listings/empty.html) | [✅](pages/my-listings/error.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | [✅](pages/my-listings/filtered.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-019`, `L2-021` |
| Payouts (`payouts`) | [✅](pages/payouts/default.html) | [✅](pages/payouts/loading.html) | [✅](pages/payouts/empty.html) | [✅](pages/payouts/error.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | [✅](pages/payouts/not-onboarded.html) |  |  |  |  |  |  | `L2-031` |
| Sell an item (`sell`) | [✅](pages/sell/default.html) | ➖ | ➖ | [✅](pages/sell/error.html) | [✅](pages/sell/unverified.html) | [✅](pages/sell/invalid.html) | [✅](pages/sell/submitting.html) | [✅](pages/sell/success.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | [✅](pages/sell/filled.html) | [✅](pages/sell/draft-saved.html) | [✅](pages/sell/selling-not-open.html) | [✅](pages/sell/product-limit.html) | [✅](pages/sell/limited.html) |  | `L2-003`, `L2-011`, `L2-012`, `L2-015`, `L2-016`, `L2-017`, `L2-018`, `L2-019`, `L2-031`, `L2-051`, `L2-063`, `L2-091`, `L2-092`, `L2-094` |
| Forbidden (`forbidden`) | [✅](pages/forbidden/default.html) | ➖ | ➖ | ➖ |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-010`, `L2-068` |
| Maintenance (`maintenance`) | [✅](pages/maintenance/default.html) | ➖ | ➖ | ➖ |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-066` |
| Not found (`not-found`) | [✅](pages/not-found/default.html) | ➖ | ➖ | ➖ |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-068` |
| Offline (`offline`) | [✅](pages/offline/default.html) | ➖ | ➖ | ➖ |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-068` |
| Privacy (`privacy`) | [✅](pages/privacy/default.html) | ➖ | ➖ | ➖ |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-054`, `L2-056`, `L2-005` |
| Problem detail (`problem-detail`) | [✅](pages/problem-detail/default.html) | [✅](pages/problem-detail/loading.html) | ➖ | [✅](pages/problem-detail/error.html) |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | [✅](pages/problem-detail/resolved.html) | `L2-036`, `L2-010` |
| Marketplace rules (`rules`) | [✅](pages/rules/default.html) | ➖ | ➖ | ➖ |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-011`, `L2-016`, `L2-031`, `L2-033` |
| Server error (`server-error`) | [✅](pages/server-error/default.html) | ➖ | ➖ | ➖ |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-068` |
| Terms (`terms`) | [✅](pages/terms/default.html) | ➖ | ➖ | ➖ |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | `L2-001`, `L2-036`, `L2-031` |

### Dialogs

| Screen | default | busy | invalid | failed | blocked | sent | limited | guest | phase-two | loading | error | captions | analytics-on | Requirements |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Change password (`change-password`) | [✅](dialogs/change-password/default.html) | [✅](dialogs/change-password/busy.html) | [✅](dialogs/change-password/invalid.html) | [✅](dialogs/change-password/failed.html) |  |  |  |  |  |  |  |  |  | `L2-009`, `L2-001`, `L2-008` |
| Close account (`close-account`) | [✅](dialogs/close-account/default.html) | [✅](dialogs/close-account/busy.html) | [✅](dialogs/close-account/invalid.html) |  | [✅](dialogs/close-account/blocked.html) |  |  |  |  |  |  |  |  | `L2-005` |
| Sign out everywhere (`sign-out-everywhere`) | [✅](dialogs/sign-out-everywhere/default.html) | [✅](dialogs/sign-out-everywhere/busy.html) | ➖ | [✅](dialogs/sign-out-everywhere/failed.html) |  |  |  |  |  |  |  |  |  | `L2-008` |
| Verify email prompt (`verify-email-prompt`) | [✅](dialogs/verify-email-prompt/default.html) | [✅](dialogs/verify-email-prompt/busy.html) | ➖ |  |  | [✅](dialogs/verify-email-prompt/sent.html) | [✅](dialogs/verify-email-prompt/limited.html) |  |  |  |  |  |  | `L2-002`, `L2-003` |
| Filters (`filters`) | [✅](dialogs/filters/default.html) | [✅](dialogs/filters/busy.html) | [✅](dialogs/filters/invalid.html) |  |  |  |  |  |  |  |  |  |  | `L2-024`, `L2-062` |
| Menu (`nav-menu`) | [✅](dialogs/nav-menu/default.html) | ➖ | ➖ |  |  |  |  | [✅](dialogs/nav-menu/guest.html) | [✅](dialogs/nav-menu/phase-two.html) |  |  |  |  | `L2-062`, `L2-065`, `L2-093`, `L2-094` |
| Pitch video (`pitch-video`) | [✅](dialogs/pitch-video/default.html) | ➖ | ➖ |  |  |  |  |  |  | [✅](dialogs/pitch-video/loading.html) | [✅](dialogs/pitch-video/error.html) | [✅](dialogs/pitch-video/captions.html) |  | `L2-080`, `L2-081`, `L2-082` |
| Add tracking (`add-tracking`) | [✅](dialogs/add-tracking/default.html) | [✅](dialogs/add-tracking/busy.html) | [✅](dialogs/add-tracking/invalid.html) | [✅](dialogs/add-tracking/failed.html) |  |  |  |  |  |  |  |  |  | `L2-033` |
| Cancel order (`cancel-order`) | [✅](dialogs/cancel-order/default.html) | [✅](dialogs/cancel-order/busy.html) | [✅](dialogs/cancel-order/invalid.html) | [✅](dialogs/cancel-order/failed.html) |  |  |  |  |  |  |  |  |  | `L2-035` |
| Confirm receipt (`confirm-receipt`) | [✅](dialogs/confirm-receipt/default.html) | [✅](dialogs/confirm-receipt/busy.html) | ➖ | [✅](dialogs/confirm-receipt/failed.html) |  |  |  |  |  |  |  |  |  | `L2-034` |
| Relist item (`relist`) | [✅](dialogs/relist/default.html) | [✅](dialogs/relist/busy.html) | ➖ |  |  |  |  |  |  |  |  |  |  | `L2-035` |
| Report a problem (`report-problem`) | [✅](dialogs/report-problem/default.html) | [✅](dialogs/report-problem/busy.html) | [✅](dialogs/report-problem/invalid.html) | [✅](dialogs/report-problem/failed.html) |  |  |  |  |  |  |  |  |  | `L2-036` |
| Ask to cancel (`request-cancellation`) | [✅](dialogs/request-cancellation/default.html) | [✅](dialogs/request-cancellation/busy.html) | ➖ | [✅](dialogs/request-cancellation/failed.html) |  |  |  |  |  |  |  |  |  | `L2-035` |
| Respond to cancellation (`respond-cancellation`) | [✅](dialogs/respond-cancellation/default.html) | [✅](dialogs/respond-cancellation/busy.html) | ➖ | [✅](dialogs/respond-cancellation/failed.html) |  |  |  |  |  |  |  |  |  | `L2-035` |
| Withdraw listing (`withdraw-listing`) | [✅](dialogs/withdraw-listing/default.html) | [✅](dialogs/withdraw-listing/busy.html) | ➖ | [✅](dialogs/withdraw-listing/failed.html) |  |  |  |  |  |  |  |  |  | `L2-020` |
| Cookie settings (`cookie-settings`) | [✅](dialogs/cookie-settings/default.html) | ➖ | ➖ |  |  |  |  |  |  |  |  |  | [✅](dialogs/cookie-settings/analytics-on.html) | `L2-056` |
| Resolve problem (`resolve-problem`) | [✅](dialogs/resolve-problem/default.html) | [✅](dialogs/resolve-problem/busy.html) | [✅](dialogs/resolve-problem/invalid.html) |  |  |  |  |  |  |  |  |  |  | `L2-036` |

### Notifications

| Screen | info | success | warning | danger | with-action | stacked | Requirements |
|---|---|---|---|---|---|---|---|
| Account banner (`account-banner`) | [✅](notifications/account-banner/info.html) | [✅](notifications/account-banner/success.html) | [✅](notifications/account-banner/warning.html) | ➖ |  |  | `L2-002` |
| Reservation timer (`reservation-timer`) | [✅](notifications/reservation-timer/info.html) | [✅](notifications/reservation-timer/success.html) | [✅](notifications/reservation-timer/warning.html) | [✅](notifications/reservation-timer/danger.html) |  |  | `L2-028`, `L2-030` |
| Payout banner (`payout-banner`) | [✅](notifications/payout-banner/info.html) | [✅](notifications/payout-banner/success.html) | [✅](notifications/payout-banner/warning.html) | [✅](notifications/payout-banner/danger.html) |  |  | `L2-031` |
| Toasts (`toast`) | [✅](notifications/toast/info.html) | [✅](notifications/toast/success.html) | [✅](notifications/toast/warning.html) | [✅](notifications/toast/danger.html) | [✅](notifications/toast/with-action.html) | [✅](notifications/toast/stacked.html) | `L2-019`, `L2-020`, `L2-068` |
| Cookie consent (`cookie-consent`) | [✅](notifications/cookie-consent/info.html) | [✅](notifications/cookie-consent/success.html) | ➖ | ➖ |  |  | `L2-056` |
| System notices (`system-banner`) | [✅](notifications/system-banner/info.html) | [✅](notifications/system-banner/success.html) | [✅](notifications/system-banner/warning.html) | [✅](notifications/system-banner/danger.html) |  |  | `L2-066`, `L2-068` |

<!-- coverage:end -->
