"""Catalog, search, filters and listing detail (L1-005, L1-006, L1-016)."""
from mk import *

# ---------------------------------------------------------------------------
# Shared pieces
# ---------------------------------------------------------------------------
def filters_form(p: str = "f", *, cat: str = "", brands=(), conds=(), lo: str = "", hi: str = "", invalid: bool = False, categories: bool = True) -> str:
    def cb(name, label, checked):
        return f'<label class="choice"><input type="checkbox" name="{name}"{" checked" if checked else ""}><span>{esc(label)}</span></label>'
    cats = "".join(
        f'<label class="choice"><input type="radio" name="{p}-cat"{" checked" if cat == c else ""}><span>{c}</span></label>'
        for c in ["All"] + [label for _, label in NAV])
    subs = ""
    if cat and cat != "All":
        sub = next(s for c, s, _ in CATEGORIES if c == cat)
        subs = (f'<fieldset class="filters__group"><legend>{cat}</legend>' + "".join(cb(f"{p}-sub", s, False) for s in sub) + "</fieldset>")
    brand_list = ["Carter's", "Gap Kids", "Hanna Andersson", "Janie and Jack", "Mini Boden", "Old Navy"]
    br = "".join(cb(f"{p}-brand", b, b in brands) for b in brand_list)
    co = "".join(cb(f"{p}-cond", c, c in conds) for c in CONDITIONS)
    err = ' aria-invalid="true" aria-describedby="%s-price-error"' % p if invalid else ""
    perr = f'<span class="field__error" id="{p}-price-error">Minimum price must be lower than maximum</span>' if invalid else ""
    price_html = (f'<fieldset class="filters__group"><legend>Price</legend><div class="filters__range">'
                  f'<label class="visually-hidden" for="{p}-min">Minimum price in dollars</label>'
                  f'<div class="input-group"><span class="addon">$</span><input class="input" id="{p}-min" inputmode="decimal" value="{lo}"{err}></div>'
                  f'<span class="text-muted" aria-hidden="true">–</span>'
                  f'<label class="visually-hidden" for="{p}-max">Maximum price in dollars</label>'
                  f'<div class="input-group"><span class="addon">$</span><input class="input" id="{p}-max" inputmode="decimal" value="{hi}"{err}></div>'
                  f'</div>{perr}</fieldset>')
    cat_group = f'<fieldset class="filters__group"><legend>Category</legend>{cats}</fieldset>{subs}' if categories else ""  # none in Phase 1 (L2-093)
    # Page guidelines 8: the three most useful filters first (Category stands for Sport; Size is the one
    # accepted size), then Brand and Price behind "More filters" (ADR general/0009).
    size = ('<div class="filters__group"><span class="field__label">Size</span>'
            '<p class="filters__size">Every item is in Girls\u2019 sizes.</p></div>')
    more = (f'<details class="filters__more-group"{" open" if (brands or lo or hi or invalid) else ""}><summary>More filters</summary><div class="filters__more-body">'
            f'<fieldset class="filters__group"><legend>Brand</legend>{br}<a href="{D("filters")}" class="link text-sm">All 10 brands</a></fieldset>'
            f'{price_html}</div></details>')
    return (f'{cat_group}{size}'
            f'<fieldset class="filters__group"><legend>Condition</legend>{co}</fieldset>{more}')


def sidebar(**kw) -> str:
    return f'<aside class="filters" aria-label="Filters"><form class="stack stack--lg">{filters_form("side", **kw)}</form></aside>'


def toolbar(count: str, chips: str = "", sort: str = "Newest") -> str:
    opts = "".join(f"<option{' selected' if o == sort else ''}>{o}</option>" for o in ["Newest", "Price: low to high", "Price: high to low"])
    return (f'<div class="toolbar"><div class="toolbar__left"><a class="btn filters-toggle" href="{D("filters")}">{ic("filter")}Filters</a>'
            f'<span class="text-muted text-sm" aria-live="polite">{count}</span>{chips}</div>'
            f'<label class="sort"><span class="text-sm text-muted">Sort</span><select class="select input--sm">{opts}</select></label></div>')


def chip(label: str) -> str:
    return f'<span class="chip" aria-pressed="true">{esc(label)}<button class="remove" type="button" aria-label="Remove filter {esc(label)}">{ic("close", "icon icon--sm")}</button></span>'


def hero() -> str:
    return ('<section class="hero" aria-labelledby="hero-title"><h1 class="display" id="hero-title">Find something <em>she&#8217;ll love.</em></h1>'
            '<p class="lead">Clothes, dance and sports gear, cameras, toys and more, from families whose girls have outgrown them.</p></section>')


def catalog_page(grid: str, *, head: str | None = None, count: str = "1,284 items", chips: str = "", side=None, after_grid: str = "",
                 user: str = "member", nav: str = "", top: str = "", sort: str = "Newest", phase: int = 3) -> Page:
    head = hero() if head is None else head
    side_html = sidebar(**(side or {}))
    main = (f'{head}<div class="catalog-layout">{side_html}<section aria-label="Listings">{toolbar(count, chips, sort)}{grid}{after_grid}</section></div>')
    return Page(main=main, user=user, nav=nav, top=top, phase=phase, main_class="container container--wide page")


SPORT_POSE = {"Dance": "dance", "Sports": "soccer", "Soccer": "soccer", "Volleyball": "volleyball", "Gymnastics": "gymnastics", "Swimming": "swimming"}


def category_head(cat: str, sub: str = "") -> str:
    """Category header: H1, the size tag and, on the sport categories, Tilly in the matching pose."""
    pose = SPORT_POSE.get(sub) or SPORT_POSE.get(cat)
    art = tilly(pose) if pose else ""
    return (f'<div class="category-head"><div class="stack stack--sm"><h1>{cat}</h1>'
            f'<div>{size_tag("Girls\'", "size-tag--lg")}</div></div>{art}</div>')


def load_more(shown: int = 16, total: str = "1,284") -> str:
    return (f'<div class="load-more"><span class="text-sm text-muted">Showing {shown} of {total}</span>'
            f'{btn("Load more", attrs="aria-describedby=\"lm-note\"")}<span id="lm-note" class="visually-hidden">Loads the next 24 items</span></div>')


def auction_card(key: str, current: float, left: str, *, label: str = "Current bid", soon: bool = False, href: str | None = None) -> str:
    """Listing card, auction variant (L2-114): the "Auction" stamp, the current price and the time left under it."""
    it = ITEMS[key]
    href = href or P("listing-detail", "auction-bids")
    t = " listing-card__time--soon" if soon else ""
    return (f'<li><a class="listing-card" href="{href}">{photo(key, stamp="Auction")}'
            f'<div class="listing-card__meta"><span class="listing-card__title">{esc(it["title"])}</span>'
            f'<span class="listing-card__brand">{esc(it["brand"])}</span>'
            f'<span class="listing-card__facts">{esc(size_of(it).rstrip("\'"))} · {esc(it["cond"])}</span>'
            f'<span class="listing-card__price"><span class="visually-hidden">{label} </span><span class="price">{price(current)}</span></span>'
            f'<span class="listing-card__time{t}">Ends in {left}</span></div></a></li>')


AUCTION_CARDS = {"swan-tutu": lambda: auction_card("swan-tutu", AUCTION["current"], AUCTION["left"])}
# The shop grid with one running auction among the fixed-price items (16 cards, newest first).
SHOP = CATALOG[:3] + ["swan-tutu"] + CATALOG[3:15]


def shop_grid(keys) -> str:
    """listing_grid, with auction keys drawn as the auction card."""
    return '<ul class="listing-grid" role="list">' + "".join(AUCTION_CARDS[k]() if k in AUCTION_CARDS else listing_card(k) for k in keys) + "</ul>"


# ---------------------------------------------------------------------------
# Catalog
# ---------------------------------------------------------------------------
s = screen("catalog", "page", "Catalog", route="/", reqs=["L2-022", "L2-023", "L2-024", "L2-059", "L2-061", "L2-062", "L2-093", "L2-094", "L2-104", "L2-114"])


@s.state("default", "Home catalog for a signed-in member: newest listings, filters beside the grid on large screens.")
def _():
    pg = catalog_page(shop_grid(SHOP), after_grid=load_more())
    pg.notes = note("Pages of 24, newest first. Images below the first screen are lazy loaded with fixed width and height (no layout shift).", "top: 30rem; right: 1rem;")
    return pg


@s.state("guest", "The same catalog for an anonymous visitor: browsing needs no account.")
def _():
    return catalog_page(shop_grid(SHOP), after_grid=load_more(), user="guest")


@s.state("category", "Clothing only: subcategory chips and the fixed size, reached from the Clothing nav link.")
def _():
    keys = [k for k in CATALOG if ITEMS[k]["cat"] == "Clothing"]
    head = category_head("Clothing")
    subs = '<div class="cluster" style="margin-bottom: var(--space-8);">' + "".join(
        f'<button class="chip" type="button" aria-pressed="{"true" if i == 0 else "false"}">{t}</button>' for i, t in enumerate(["All clothing"] + CATEGORIES[0][1])) + "</div>"
    return catalog_page(subs + listing_grid(keys), head=head, count="412 items", side={"cat": "Clothing"}, nav="clothing", after_grid=load_more(len(keys), "412"))


@s.state("category-dance", "Dance, a sport category: the header shows Tilly dancing (one Tilly on the screen).")
def _():
    keys = [k for k in SHOP if ITEMS[k]["cat"] == "Dance"]
    subs = '<div class="cluster" style="margin-bottom: var(--space-8);">' + "".join(
        f'<button class="chip" type="button" aria-pressed="{"true" if i == 0 else "false"}">{t}</button>' for i, t in enumerate(["All dance"] + CATEGORIES[2][1])) + "</div>"
    pg = catalog_page(subs + shop_grid(keys), head=category_head("Dance"), count="186 items", side={"cat": "Dance"}, nav="dance", after_grid=load_more(len(keys), "186"))
    pg.notes = note("Sports shows the pose of the one selected subcategory (soccer, volleyball, gymnastics, the swimming tile) and soccer otherwise. Empty states on these pages drop Tilly.", "top: 14rem; right: 1rem;")
    return pg


@s.state("auctions", "Auction cards among fixed-price ones: the “Auction” stamp, the current price (the starting bid before any bid) and the time left; under an hour it reads in minutes.")
def _():
    cards = (auction_card("swan-tutu", AUCTION["current"], AUCTION["left"])
             + auction_card("tap-shoes", ITEMS["tap-shoes"]["price"], AUCTION["soon_left"], label="Starting bid", soon=True, href=P("listing-detail", "auction-ending"))
             + auction_card("pink-scooter", AUCTION["own_current"], AUCTION["left"], href=P("listing-detail", "auction-own"))
             + listing_card("pink-tutu"))
    head = '<div class="page-header"><div><p class="eyebrow">Search</p><h1>“tutu”</h1></div></div>'
    pg = catalog_page(f'<ul class="listing-grid" role="list">{cards}</ul>', head=head, count="4 items", side={"cat": "Dance"})
    pg.notes = note("The price filter and the price sort use the current price: the highest bid, or the starting bid before any bid (L2-114). The time left is plain text; it is not announced as it changes.",
                    "top: 6rem; right: 1rem;")
    return pg


@s.state("search", "Keyword results for “carter’s”, title and brand matches first.")
def _():
    head = '<div class="page-header"><div><p class="eyebrow">Search</p><h1>“carter’s”</h1></div></div>'
    return catalog_page(listing_grid(["sundress", "floral-shorts"]), head=head, count="2 items")


@s.state("filtered", "Clothing, new with tags, $20–$200, sorted by price: filters live in the URL and show as removable chips.")
def _():
    chips = chip("Clothing") + chip("New with tags") + chip("$20–$200") + f'<button class="btn btn--link btn--sm" type="button">Clear all</button>'
    pg = catalog_page(listing_grid(["rainbow-romper", "velvet-dress"]), head="", count="2 items", chips=chips,
                      side={"cat": "Clothing", "conds": ["New with tags"], "lo": "20", "hi": "200"}, sort="Price: low to high")
    pg.main = '<div class="page-header"><h1>Shop</h1></div>' + pg.main
    pg.notes = note("URL: /?category=clothing&amp;condition=new-with-tags&amp;min=20&amp;max=200&amp;sort=price-asc — bookmarkable and shareable.", "top: 6rem; right: 1rem;")
    return pg


@s.state("loading", "First load: skeleton cards in the final grid, filters already usable.")
def _():
    pg = catalog_page(skeleton_grid(8), count='<span class="skeleton skeleton--text" style="width: 5rem; display: inline-block;"></span>')
    return pg


@s.state("empty", "No active listings at all (launch day): one action, sell the first item.")
def _():
    grid = empty("tag", "The closet is empty for now", "Be the first to pass on something she has outgrown.", btn("Sell outgrown gear", "primary", P("sell")))
    return catalog_page(grid, count="0 items")


@s.state("phase-one", "Launch Phase 1 for a visitor: no category links or menu button, no Category filter, no Sell link; one flat catalog of the owner's items.")
def _():
    keys = CATALOG[:12]
    pg = catalog_page(listing_grid(keys), count="36 items", user="guest", phase=1, side={"categories": False})
    pg.notes = note("Phase 1 (L2-093, L2-094): at every width the top bar has only the logo, search and Sign in. /clothing, /dance and the other category URLs redirect here.",
                    "top: 6rem; right: 1rem;")
    return pg


@s.state("phase-one-admin", "Launch Phase 1 for the owner signed in as an administrator: the same flat catalog with the Sell link.")
def _():
    keys = CATALOG[:12]
    pg = catalog_page(listing_grid(keys), count="36 items", user="admin", phase=1, side={"categories": False})
    pg.notes = note("Only an administrator sells in Phases 1 and 2 (L2-091); the Sell link and seller screens are offered to them alone.", "top: 6rem; right: 1rem;")
    return pg


@s.state("phase-two", "Launch Phase 2 for a member: all six category links and the menu, but no Sell link (selling opens to members in Phase 3).")
def _():
    return catalog_page(shop_grid(SHOP), after_grid=load_more(), phase=2)


@s.state("no-results", "Search and filters matched nothing; filters stay visible and clearable.")
def _():
    chips = chip("Dance") + chip("New with tags")
    request = f'<a href="{P("request-form", "prefilled")}">Can’t find it? Request it</a>'
    grid = empty("search", "No items match", f"Try fewer filters or a different word. {request}", btn("Clear search and filters", "primary"), cls="empty--compact")
    pg = catalog_page(grid, head='<div class="page-header"><div><p class="eyebrow">Search</p><h1>“sequin cape”</h1></div></div>',
                      count="0 items", chips=chips, side={"cat": "Dance", "conds": ["New with tags"]})
    pg.notes = note("“Request it” opens /account/requests/new?category=dance&amp;q=sequin+cape; a visitor signs in first (L2-104, L2-105).", "top: 22rem; right: 1rem;")
    return pg


@s.state("category-empty", "The Dance page with nothing in it: Tilly already dances in the header, so the empty state keeps its icon and offers “Request it”.")
def _():
    request = f'<a href="{P("request-form", "prefilled")}">Can’t find it? Request it</a>'
    grid = empty("tag", "Nothing in Dance right now", f"New items arrive every day. {request}", btn("Shop the closet", "primary", P("catalog")),
                 cls="empty--compact", tilly_pose=False)
    return catalog_page(grid, head=category_head("Dance"), count="0 items", side={"cat": "Dance"}, nav="dance")


@s.state("error", "The listings request failed: chrome and filters stay, retry with a reference code.")
def _():
    grid = empty("refresh", "Couldn’t load items", 'Something went wrong on our side. <span class="ref">Ref 7f3a-91c2</span>',
                 btn("Try again", "primary", icon="refresh"), cls="empty--error empty--compact")
    return catalog_page(grid, count="")


@s.state("offline", "Device offline: items from the last visit stay browsable under an offline banner.")
def _():
    top = banner("warning", "You’re offline. Showing items from your last visit.", dismiss=False, icon="wifi-off")
    return catalog_page(listing_grid(CATALOG[:8]), top=top, count="8 saved items")


# ---------------------------------------------------------------------------
# Listing detail
# ---------------------------------------------------------------------------
DESC = {
    "pink-tutu": "Worn to one recital. Three layers of tulle, still full and bouncy. Elastic waist, no snags.",
    "raincoat": "Fully waterproof with a soft cotton lining. Hood snaps on, all poppers work. Measured flat: 15 in pit to pit.",
    "sundress": "Light cotton with shoulder ties. Loved for a whole summer.",
    "rainbow-romper": "Bought the wrong colourway. Never worn, tags still on.",
}


def gallery(key: str, current: int = 1, sold: bool = False) -> str:
    it = ITEMS[key]
    n = min(it["photos"], 6)
    thumbs = "".join(
        f'<li><button type="button" aria-label="Show photo {i} of {n}"{" aria-current=\"true\"" if i == current else ""}>{photo(key, tag=False, alt="", tone=((it["tone"] + i - 2) % 6) + 1)}</button></li>'
        for i in range(1, n + 1))
    dots = "".join(f'<span{" aria-current=\"true\"" if i == current else ""}></span>' for i in range(1, n + 1))
    return (f'<section class="gallery" aria-label="Photos"><div class="gallery__main">{photo(key, tag=False, n=current, cls="photo--sold" if sold else "")}</div>'
            f'<div class="gallery__dots" aria-hidden="true">{dots}</div><ul class="gallery__thumbs" role="list">{thumbs}</ul></section>')


def seller_block(handle: str) -> str:
    p = PEOPLE[handle]
    rep = rating(handle) if p["reviews"] else '<span class="badge">New seller</span>'
    sales = f'{p["sales"]} sales · ' if p["sales"] else ""
    return (f'<div class="seller">{avatar(handle)}<div class="stack stack--sm" style="flex: 1; gap: var(--space-1);">'
            f'<a class="seller__name" href="{P("profile")}">{handle}</a><span class="seller__meta">{sales}{p["city"]}</span>{rep}</div></div>')


def detail(key: str, *, actions: str, stamp: str | None = None, user: str = "member", extra_info: str = "", report: bool = True, below: str = "",
           price_html: str | None = None, bar: tuple[str, str] | None = None) -> Page:
    """price_html replaces the price block (an auction's tl-auction-panel); bar is the buy bar's (amount, caption)."""
    it = ITEMS[key]
    total = it["price"] + it["ship"]
    if price_html is None:
        price_html = (f'<div class="detail__price"><span class="price price--lg">{money(it["price"])}</span>'
                      f'<span class="text-muted text-sm">{ship_text(it["ship"])} · <span class="num">{money(total)}</span> total before tax</span></div>')
    flaws = f'<div class="flaws"><strong>Flaws:</strong> {esc(it["flaws"])}</div>' if it.get("flaws") else ""
    status_line = f'<span class="photo__stamp{" photo__stamp--sold" if stamp == "Sold" else ""}" style="position: static; align-self: flex-start;">{stamp}</span>' if stamp else ""
    rep = ""
    info = (f'<div class="detail__info">'
            f'<div class="stack stack--sm">{status_line}<span class="eyebrow">{esc(it["brand"])}</span><h1 class="detail__title">{esc(it["title"])}</h1>'
            f'<div class="cluster" style="gap: var(--space-3);">{size_tag(it, "size-tag--lg")}{condition_badge(it["cond"])}</div></div>'
            f'{price_html}'
            f'{extra_info}'
            f'{buy_bar(total, actions, bar)}'
            f'{flaws}<p>{esc(DESC.get(key, "Freshly washed, light wear. Smoke-free, pet-free home."))}</p>'
            f'<dl class="facts"><dt>Size</dt><dd>{size_long(it)}</dd><dt>Category</dt><dd>{it["cat"]} · {it["sub"]}</dd>'
            f'<dt>Condition</dt><dd>{esc(it["cond"])}</dd><dt>Listed</dt><dd><time datetime="2026-10-25">{it["listed"]}</time></dd></dl>'
            f'{seller_block(it["seller"])}{rep}</div>')
    crumbs = (f'<nav aria-label="Breadcrumb" style="margin-top: var(--space-6);"><ol class="breadcrumb"><li><a href="{P("catalog")}">Shop</a></li>'
              f'<li><a href="{P("catalog", "category")}">{it["cat"]}</a></li><li aria-current="page">{it["sub"]}</li></ol></nav>')
    return Page(main=f'{crumbs}<div class="detail">{gallery(key, sold=stamp == "Sold")}{info}</div>{below}', user=user, nav=cat_key(it["cat"]))


def buy_bar(total: float, actions: str, bar: tuple[str, str] | None = None) -> str:
    if not actions:
        return ""
    amount, caption = bar or (money(total), "incl. shipping")
    return (f'<div class="buy-bar"><div class="buy-bar__price"><span class="price">{amount}</span>'
            f'<span class="text-caption">{caption}</span></div>{actions}</div>')


def similar(keys) -> str:
    return f'<section class="section" id="similar" style="margin-top: var(--space-20);"><div class="section-head"><h2>Similar in the closet</h2></div>{listing_grid(keys)}</section>'


d = screen("listing-detail", "page", "Listing detail", route="/items/{id}", reqs=["L2-025", "L2-026", "L2-028", "L2-016", "L2-063", "L2-065", "L2-107", "L2-108", "L2-109", "L2-111", "L2-114", "L2-115", "L2-117", "L2-118", "L2-120"],
           na={"empty": "A listing always has its required fields; a missing listing is the 'unavailable' state."})


@d.state("default", "An active listing seen by another member: gallery, price with shipping, seller reputation, Buy now.")
def _():
    pg = detail("pink-tutu", actions=btn("Buy now", "primary lg", P("checkout")))
    pg.notes = note("Under 992px: one column, swipeable gallery, Buy now bar fixed to the bottom while scrolling.", "top: 8rem; right: 1rem;")
    return pg


@d.state("guest", "Anonymous visitor: Buy now leads to sign in and returns here afterwards.")
def _():
    return detail("pink-tutu", actions=btn("Buy now", "primary lg", P("sign-in", "return")), user="guest", report=False)


@d.state("new-seller", "A listing from a seller with no completed sales shows “New seller” instead of a rating.")
def _():
    return detail("rainbow-romper", actions=btn("Buy now", "primary lg", P("checkout")))


@d.state("reserved", "Another buyer is in checkout: Reserved label, no Buy now, a hint to check back.")
def _():
    info = alert("warning", "In someone’s checkout", "If they don’t finish within 10 minutes, it’s back on sale.", icon="clock")
    return detail("raincoat", stamp="Reserved", actions="", extra_info=info)


@d.state("sold", "A sold listing keeps its URL with a Sold label and similar items, no Buy now.")
def _():
    find = f'<div class="cluster">{btn("Find similar", "primary", "#similar")}</div>'
    return detail("party-dress", stamp="Sold", actions="", extra_info=find, report=False, below=similar(["pleated-skirt", "pink-tutu", "velvet-dress", "rainbow-romper"]))


@d.state("own", "The seller viewing their own Used - Fair listing: flaws shown, Edit and Withdraw instead of Buy now.")
def _():
    acts = btn("Edit listing", "primary lg", P("edit-listing"), icon="edit") + btn("Withdraw", "lg", D("withdraw-listing"))
    return detail("sundress", actions=acts, report=False)


@d.state("loading", "Detail skeleton matching the gallery and info columns.")
def _():
    sk = ('<div class="detail" aria-busy="true" aria-label="Loading listing"><div class="gallery"><span class="skeleton skeleton--photo"></span></div>'
          '<div class="detail__info"><div class="stack stack--sm"><span class="skeleton skeleton--text" style="width: 20%;"></span>'
          '<span class="skeleton skeleton--title" style="width: 80%;"></span><span class="skeleton skeleton--text" style="width: 35%;"></span></div>'
          '<span class="skeleton skeleton--title" style="width: 30%;"></span><span class="skeleton skeleton--rect" style="height: 3rem;"></span>'
          '<div><span class="skeleton skeleton--text"></span><span class="skeleton skeleton--text"></span><span class="skeleton skeleton--text"></span></div></div></div>')
    return Page(main='<h1 class="visually-hidden">Loading listing</h1>' + sk)


@d.state("error", "The listing failed to load: retry, or go back to the catalog.")
def _():
    main = ('<div class="center-state"><h1>Couldn’t load this item</h1><p class="text-muted">It’s us, not you. Try again in a moment.</p>'
            f'<span class="ref">Ref 2b9e-4410</span><div class="actions">{btn("Try again", "primary", icon="refresh")}{btn("Shop the closet", href=P("catalog"))}</div></div>')
    return Page(main=main)


@d.state("unavailable", "Removed or draft listing opened by someone else: “This item isn’t available” (HTTP 404).")
def _():
    main = ('<div class="center-state"><span class="code" aria-hidden="true">4<em>0</em>4</span><h1>This item isn’t available</h1>'
            '<p class="text-muted">It may have been sold or taken down.</p>'
            f'<div class="actions">{btn("Find similar", "primary", P("catalog"))}</div></div>')
    return Page(main=main)


# ---------------------------------------------------------------------------
# Showcase listing (L2-107 to L2-109): looks Active; Buy now shows Sold and "Request this item"
# ---------------------------------------------------------------------------
def sold_notice(text: str, action: str) -> str:
    """The Sold notice that replaces the buy bar after Buy now on a showcase listing (design system: Sold notice)."""
    return (f'<section class="sold-notice" role="status" aria-labelledby="sold-title">'
            f'<span class="photo__stamp photo__stamp--sold">Sold</span>'
            f'<h2 class="sold-notice__title" id="sold-title" tabindex="-1">This one has already found a home</h2>'
            f'<p class="sold-notice__text">{text}</p><div class="sold-notice__actions">{action}</div></section>')


SHOWCASE_TEXT = "We can look for another one like it. Tell us what you’re after."


@d.state("showcase", "A showcase listing looks like any Active listing: price, shipping and Buy now (L2-107).")
def _():
    pg = detail("gold-tutu", actions=btn("Buy now", "primary lg", P("listing-detail", "showcase-sold"), attrs='role="button"'))
    pg.notes = note("Nothing on the page or in the API response says it is a showcase listing to a shopper, or that anyone requested it (L2-111).", "top: 8rem; right: 1rem;")
    return pg


@d.state("showcase-sold", "Buy now on a showcase listing: no checkout, no address or card. The Sold notice with Request this item takes the buy bar's place (L2-108).")
def _():
    notice = sold_notice(SHOWCASE_TEXT, btn("Request this item", "primary lg", D("request-showcase-item")))
    pg = detail("gold-tutu", actions="", extra_info=notice)
    pg.notes = note("Focus moves to “This one has already found a home”; the notice is announced politely. No call to /api/checkouts is made.", "top: 8rem; right: 1rem;")
    return pg


@d.state("showcase-guest", "A visitor's Buy now shows the same notice; Request this item goes to sign in and comes back with the form open (L2-109).")
def _():
    notice = sold_notice(SHOWCASE_TEXT, btn("Request this item", "primary lg", P("sign-in", "request")))
    return detail("gold-tutu", actions="", extra_info=notice, user="guest", report=False)


@d.state("showcase-requested", "The member already asked for this item: the notice says when and the action updates the request (L2-109).")
def _():
    notice = sold_notice("You asked for this on Oct 24. Add a note or change it any time.", btn("Update your request", "primary lg", D("request-showcase-item", "requested")))
    return detail("gold-tutu", actions="", extra_info=notice)


@d.state("showcase-own", "The owner, an administrator, on their own showcase listing: Edit, Withdraw, the Showcase switch on and See requests (L2-107).")
def _():
    acts = btn("Edit listing", "primary lg", P("edit-listing"), icon="edit") + btn("Withdraw", "lg", D("withdraw-listing"))
    switch = ('<div class="cluster" style="justify-content: space-between; gap: var(--space-4);">'
              '<label class="switch switch--labelled"><span class="choice__text"><span class="switch__label">Showcase</span>'
              '<small>Buy now shows Sold and Request this item</small></span><input type="checkbox" role="switch" checked></label>'
              f'{btn("See requests", "ghost", P("showcase-requests"))}</div>')
    pg = detail("gold-tutu", actions=acts, user="admin", report=False)
    pg.main = pg.main.replace("<p>", switch + "<p>", 1)
    pg.notes = note("The switch calls PUT /api/admin/listings/L-20940/showcase; only the administrator's own Draft or Active listing can be a showcase (L2-107). If the call fails the switch goes back and the danger toast “Couldn’t change the showcase” is shown.", "top: 8rem; right: 1rem;")
    return pg


# ---------------------------------------------------------------------------
# Request this item (L2-109)
# ---------------------------------------------------------------------------
rq = screen("request-showcase-item", "dialog", "Request this item", opens_from="listing-detail", reqs=["L2-109", "L2-049", "L2-051", "L2-065"])

NOTE_HELP = "Anything that helps us find one like it: size, colour, when you need it."


def request_dialog(*, note_text: str = "", error: str | None = None, busy: bool = False, top: str = "", requested: bool = False,
                   primary: str | None = None) -> Dialog:
    ro = " readonly" if busy else ""
    count = len(note_text)
    ctl = f'<textarea class="textarea" id="rq-note" name="note" rows="4" maxlength="600"{{db}}{ro}>{esc(note_text)}</textarea>'
    body = (item_row("gold-tutu", f'{money(48)} · Capezio') +
            (f'<p class="text-sm text-muted">You asked for this on Oct 24. Sending again updates your request.</p>' if requested else "") +
            field("rq-note", "Note", ctl, help=NOTE_HELP, error=error, optional=True, counter=f"{count} / 500"))
    label = primary or ("Update request" if requested else "Send request")
    send = btn(label, "primary", attrs='type="submit" aria-busy="true"' if busy else 'type="submit"')
    cancel = btn("Cancel", attrs='type="button" disabled' if busy else 'type="button"')
    html = dialog("Request this item", body, cancel + send, desc="Tell TuTuMuch you’d like one like it. Only TuTuMuch sees your request.", icon="heart", top=top)
    return Dialog(bg=("listing-detail", "showcase-sold"), html=html)


@rq.state("default", "An empty request: the item, one optional note and Send request.")
def _():
    return request_dialog()


@rq.state("requested", "Already requested: the saved note is filled in and sending again updates it.")
def _():
    d = request_dialog(note_text="Size 7 if you find one, for her spring recital.", requested=True)
    d.bg = ("listing-detail", "showcase-requested")
    return d


@rq.state("invalid", "The note is over 500 characters: refused inline with a summary; nothing is sent.")
def _():
    long_note = ("She dances every Saturday and this is exactly the tutu she has been dreaming of since her first recital. " * 5)[:501]
    summary = error_summary([("rq-note", "Keep the note to 500 characters")])
    return request_dialog(note_text=long_note, error="Keep the note to 500 characters", top=summary)


@rq.state("busy", "Sending: the button is busy, the note read-only and Cancel disabled.")
def _():
    return request_dialog(note_text="Size 7 if you find one, for her spring recital.", busy=True)


@rq.state("failed", "Sending failed: the note is kept; a reference and Try again.")
def _():
    top = alert("danger", "Couldn’t send your request", 'Try again in a moment. <span class="ref">Ref 7e21-90ab</span>')
    return request_dialog(note_text="Size 7 if you find one, for her spring recital.", top=top, primary="Try again")


@rq.state("limited", "A 21st new request today: refused with 429; updating an existing request still works (L2-109, L2-051).")
def _():
    top = alert("warning", "That’s 20 requests today", "Try again tomorrow. You can still change the requests you’ve sent.", icon="clock")
    return request_dialog(note_text="Size 7 if you find one, for her spring recital.", top=top)


# ---------------------------------------------------------------------------
# Showcase request toasts (L2-109)
# ---------------------------------------------------------------------------
rqt = screen("showcase-request-toasts", "notification", "Showcase request toasts", opens_from="listing-detail", reqs=["L2-109"])


def on_showcase(*toasts) -> Note:
    return Note(bg=("listing-detail", "showcase-requested"), toasts=toast_region(*toasts))


@rqt.state("info", "An existing request was sent again with a new note.")
def _():
    return on_showcase(toast("info", "Request updated"))


@rqt.state("success", "A new request was sent.")
def _():
    return on_showcase(toast("success", "Request sent", "Thank you. TuTuMuch sees every request."))


@rqt.state("warning", "The daily limit of new requests was reached.")
def _():
    return on_showcase(toast("warning", "That’s 20 requests today", "Try again tomorrow."))


@rqt.state("danger", "Sending failed after the dialog closed (the connection dropped): stays until dismissed, offers Try again.")
def _():
    return on_showcase(toast("danger", "Couldn’t send your request", "Check your connection.", "Try again"))


# ---------------------------------------------------------------------------
# Menu sheet (category links collapse into it under 1280px, L2-062)
# ---------------------------------------------------------------------------
m = screen("nav-menu", "dialog", "Menu", opens_from="catalog", reqs=["L2-062", "L2-065", "L2-093", "L2-094"],
           na={"busy": "Opening the menu makes no request.", "invalid": "The menu has no inputs."})


def menu_sheet(guest: bool, selling: bool = True) -> str:
    cats = "".join(f'<li><a href="{nav_href(k)}">{t}{ic("chevron-right")}</a></li>' for k, t in NAV)
    if guest:
        sell = f'<li><a href="{P("sell")}">Sell outgrown gear</a></li>' if selling else ""
        small = f'{sell}<li><a href="{P("rules")}">Marketplace rules</a></li>'
        foot = btn("Sign in", href=P("sign-in")) + btn("Join TuTuMuch", "primary", P("register"))
    else:
        links = [("Sell outgrown gear", "sell"), ("Your listings", "my-listings"), ("Purchases", "purchases"), ("Sales", "sales"), ("Settings", "settings")]
        if not selling:
            links = [(t, pid) for t, pid in links if pid not in ("sell", "my-listings", "sales")]
        small = "".join(f'<li><a href="{P(pid)}">{t}</a></li>' for t, pid in links)
        foot = btn("Sign out", icon="log-out")
    return (f'<div class="menu-sheet" id="menu" role="dialog" aria-modal="true" aria-labelledby="dlg-title" data-dismiss-on-escape>'
            f'<div class="menu-sheet__head">{mark()}'
            f'<h2 class="visually-hidden" id="dlg-title">Menu</h2><button class="btn btn--ghost btn--icon" type="button" aria-label="Close menu" autofocus>{ic("close")}</button></div>'
            f'<nav class="menu-sheet__body" aria-label="Menu"><ul class="menu-sheet__links" role="list">{cats}</ul>'
            f'<ul class="menu-sheet__links menu-sheet__links--small" role="list">{small}</ul></nav><div class="menu-sheet__foot">{foot}</div></div>')


@m.state("default", "Full-screen menu for a member on a phone: categories large, account links small.")
def _():
    return Dialog(bg=("catalog", "default"), html=menu_sheet(False))


@m.state("guest", "Menu for an anonymous visitor with Sign in and Join.")
def _():
    return Dialog(bg=("catalog", "guest"), html=menu_sheet(True))


@m.state("phase-two", "Launch Phase 2 for a member: every category, no seller links (L2-094). In Phase 1 the top bar has no menu button at all.")
def _():
    d = Dialog(bg=("catalog", "phase-two"), html=menu_sheet(False, selling=False))
    d.notes = note("Phase 1 offers no menu sheet: it would hold no category (L2-093).", "top: 1rem; left: 1rem;")
    return d


# ---------------------------------------------------------------------------
# Filter panel (full screen under 768px, side panel 768–991px)
# ---------------------------------------------------------------------------
f = screen("filters", "dialog", "Filters", opens_from="catalog", reqs=["L2-024", "L2-062"])


def filter_sheet(form: str, apply: str, top: str = "") -> str:
    return (f'<div class="backdrop"></div><div class="menu-sheet filter-sheet" role="dialog" aria-modal="true" aria-labelledby="dlg-title" data-dismiss-on-escape>'
            f'<div class="menu-sheet__head"><h2 id="dlg-title" style="font: var(--text-h4);">Filters</h2>'
            f'<button class="btn btn--ghost btn--icon" type="button" aria-label="Close filters">{ic("close")}</button></div>'
            f'<form class="menu-sheet__body">{top}{form}</form><div class="menu-sheet__foot">{btn("Clear")}{apply}</div></div>')


@f.state("default", "Filter panel with Clothing chosen; Apply shows the live result count.")
def _():
    return Dialog(bg=("catalog", "default"), html=filter_sheet(filters_form("m", cat="Clothing", conds=["New with tags"]), btn("Show 38 items", "primary", attrs="autofocus")))


@f.state("invalid", "Minimum price above maximum: inline error, Apply blocked until fixed.")
def _():
    form = filters_form("m", cat="Clothing", lo="200", hi="50", invalid=True)
    top = error_summary([("m-min", "Minimum price must be lower than maximum")], title="Check the price range")
    return Dialog(bg=("catalog", "default"), html=filter_sheet(form, btn("Show items", "primary", attrs="disabled"), top=top))


@f.state("busy", "Counting results after a change: Apply is busy, the panel stays usable.")
def _():
    return Dialog(bg=("catalog", "default"), html=filter_sheet(filters_form("m", cat="Clothing", conds=["New with tags", "New without tags"]), btn("Show items", "primary", attrs='aria-busy="true"')))


# ---------------------------------------------------------------------------
# Auctions on the detail page (L2-114 to L2-120): tl-auction-panel takes the price block's place;
# "Place bid" and "Pay now" sit in the buy bar. No bidder is ever named (L2-120).
# ---------------------------------------------------------------------------
STATUS_ICON = {"info": "info", "warning": "alert", "success": "check-circle", "": "clock"}


def bids_text(n: int) -> str:
    return "No bids yet" if n == 0 else f"{n} bid" + ("" if n == 1 else "s")


def auction_panel(key: str, *, label: str = "Current bid", amount: float | None = None, bids: int = AUCTION["bids"], left: str = AUCTION["left"],
                  soon: bool = False, ended: bool = False, status: tuple[str, str, str] | None = None) -> str:
    """The Auction panel (design system: Auction panel). status = (tone, title, text); tone "" is neutral."""
    it = ITEMS[key]
    amount = AUCTION["current"] if amount is None else amount
    if ended:
        time = ""
        end = f'Ended <time datetime="{AUCTION["ended_iso"]}">{AUCTION["ended"]}</time>'
    elif soon:
        time = f'<span class="auction-panel__time auction-panel__time--soon">Ends in {left}</span>'
        end = f'Ends <time datetime="{AUCTION["soon_iso"]}">{AUCTION["soon_ends"]}</time>'
    else:
        time = f'<span class="auction-panel__time">Ends in {left}</span>'
        end = f'Ends <time datetime="{AUCTION["ends_iso"]}">{AUCTION["ends"]}</time>'
    st = ""
    if status:
        tone, title, text = status
        mod = f" auction-panel__status--{tone}" if tone else ""
        st = (f'<p class="auction-panel__status{mod}" role="status">{ic(STATUS_ICON[tone])}'
              f'<span><strong>{title}</strong>{text}</span></p>')
    return (f'<section class="auction-panel" aria-label="Auction"><div class="auction-panel__bid"><span class="auction-panel__label">{label}</span>'
            f'<span class="price price--lg">{money(amount)}</span></div>'
            f'<p class="auction-panel__facts"><span>{bids_text(bids)}</span>{"<span aria-hidden=\"true\">·</span>" + time if time else ""}</p>'
            f'<p class="auction-panel__end">{end} · {ship_text(it["ship"])}</p>{st}</section>')


def bid_bar(amount: float, label: str = "Current bid") -> tuple[str, str]:
    return (money(amount), label)


PLACE_BID = lambda href=None: btn("Place bid", "primary lg", href or D("place-bid"))  # noqa: E731
OUTBID_TEXT = f"Bid {money(AUCTION['minimum'])} or more to get back in."


@d.state("auction", "An auction with no bids, seen by another member: “Starting bid $12.00”, “No bids yet”, the time left and the end time, Place bid instead of Buy now.")
def _():
    panel = auction_panel("swan-tutu", label="Starting bid", amount=AUCTION["start"], bids=0)
    pg = detail("swan-tutu", actions=PLACE_BID(D("place-bid", "first-bid")), price_html=panel, bar=bid_bar(AUCTION["start"], "Starting bid"))
    pg.notes = note("The end time is shown in the viewer's time zone. “Ends in” is plain text that updates each minute; it is not announced.", "top: 8rem; right: 1rem;")
    return pg


@d.state("auction-bids", "An auction with three bids: “Current bid $18.00”, “3 bids”, the time left and Place bid; no Buy now and no bidder names.")
def _():
    panel = auction_panel("swan-tutu")
    pg = detail("swan-tutu", actions=PLACE_BID(), price_html=panel, bar=bid_bar(AUCTION["current"]))
    pg.notes = note("No bid history and no bidder names, for anyone (L2-120). Under 992px Place bid sits in the sticky buy bar with the current bid.", "top: 8rem; right: 1rem;")
    return pg


@d.state("auction-highest", "The signed-in member holds the highest bid: “You’re the highest bidder”; no Place bid until someone bids higher.")
def _():
    panel = auction_panel("swan-tutu", status=("info", "You’re the highest bidder", " We’ll email you if someone bids higher."))
    return detail("swan-tutu", actions="", price_html=panel)


@d.state("auction-outbid", "The member bid and was outbid: “You’ve been outbid” and Place bid with the new lowest bid.")
def _():
    panel = auction_panel("swan-tutu", status=("warning", "You’ve been outbid", f" {OUTBID_TEXT}"))
    return detail("swan-tutu", actions=PLACE_BID(), price_html=panel, bar=bid_bar(AUCTION["current"]))


@d.state("auction-ending", "Less than an hour left: the time left reads in minutes, “Ends in 42 min”, in the warning colour.")
def _():
    it = ITEMS["tap-shoes"]
    panel = auction_panel("tap-shoes", label="Starting bid", amount=it["price"], bids=0, left=AUCTION["soon_left"], soon=True)
    return detail("tap-shoes", actions=PLACE_BID(), price_html=panel, bar=bid_bar(it["price"], "Starting bid"))


@d.state("auction-ended", "An auction that ended with bids, seen by anyone but the winner: “This auction has ended” with the winning bid; no Place bid.")
def _():
    panel = auction_panel("swan-tutu", label="Winning bid", ended=True,
                          status=("", "This auction has ended", f" It ended with a winning bid of {money(AUCTION['current'])}."))
    find = f'<div class="cluster">{btn("Find similar", "primary", "#similar")}</div>'
    return detail("swan-tutu", actions="", price_html=panel, extra_info=find, below=similar(["pink-tutu", "gold-tutu", "ballet-leotard", "velvet-dress"]))


@d.state("auction-won", "The winner after the auction ended: “You won”, the pay-by time and Pay now in the buy bar.")
def _():
    panel = auction_panel("swan-tutu", label="Winning bid", ended=True,
                          status=("success", "You won", f" Pay by <time datetime=\"{AUCTION['pay_by_iso']}\">{AUCTION['pay_by']}</time>: your bid plus shipping and tax."))
    pg = detail("swan-tutu", actions=btn("Pay now", "primary lg", P("checkout", "auction-won")), price_html=panel, bar=bid_bar(AUCTION["current"], "Winning bid"))
    pg.notes = note("The listing is Reserved for the winner until the pay-by time, 48 hours after the end (ADR orders/0009). Pay now opens checkout at the winning bid.",
                    "top: 8rem; right: 1rem;")
    return pg


@d.state("auction-own", "The seller viewing their own auction with bids: the current bid and bids, no Place bid, and no Edit or Withdraw.")
def _():
    panel = auction_panel("pink-scooter", amount=AUCTION["own_current"], bids=AUCTION["own_bids"],
                          status=("", "This auction has bids, so it can’t be changed", " We’ll email you when it ends."))
    pg = detail("pink-scooter", actions="", price_html=panel, report=False)
    pg.notes = note("The seller sees how many bids, never who bid (L2-120). They learn the buyer through the order, as for any sale.", "top: 8rem; right: 1rem;")
    return pg


@d.state("auction-own-no-bids", "The seller's own auction before any bid: Edit listing and Withdraw are still offered.")
def _():
    it = ITEMS["pink-scooter"]
    panel = auction_panel("pink-scooter", label="Starting bid", amount=it["price"], bids=0)
    acts = btn("Edit listing", "primary lg", P("edit-listing", "auction"), icon="edit") + btn("Withdraw", "lg", D("withdraw-listing"))
    return detail("pink-scooter", actions=acts, price_html=panel, report=False)


@d.state("auction-unsold", "The seller's own auction that ended without bids (Removed, AuctionUnsold): out of the closet, with Relist.")
def _():
    it = ITEMS["glitter-boots"]
    panel = auction_panel("glitter-boots", label="Starting bid", amount=it["price"], bids=0, ended=True,
                          status=("", "This auction ended with no bids", " Relist it to run it again, with a new end time from now."))
    pg = detail("glitter-boots", actions=btn("Relist", "primary lg"), price_html=panel, bar=bid_bar(it["price"], "Starting bid"), report=False)
    pg.notes = note("Only the seller sees a Removed listing; anyone else gets “This item isn’t available” (L2-117). The same page follows an unpaid auction (AuctionUnpaid): “The winner didn’t pay in time”.",
                    "top: 8rem; right: 1rem;")
    return pg


@d.state("auction-guest", "A visitor on an auction: Place bid goes to sign in and comes back to the listing.")
def _():
    panel = auction_panel("swan-tutu")
    pg = detail("swan-tutu", actions=PLACE_BID(P("sign-in", "return")), price_html=panel, bar=bid_bar(AUCTION["current"]), user="guest", report=False)
    pg.notes = note("Signed out: /sign-in?returnUrl=/items/L-20944 (L2-109 return rule). An unverified member gets the verify-email prompt instead of the dialog (L2-002).",
                    "top: 8rem; right: 1rem;")
    return pg


# ---------------------------------------------------------------------------
# Place a bid (L2-115, L2-116)
# ---------------------------------------------------------------------------
pb = screen("place-bid", "dialog", "Place a bid", opens_from="listing-detail", reqs=["L2-115", "L2-116", "L2-051", "L2-065"])


def bid_dialog(*, value: str = "19.00", minimum: float = AUCTION["minimum"], current: float = AUCTION["current"], bids: int = AUCTION["bids"],
               error: str | None = None, busy: bool = False, top: str = "", primary: str | None = None, bg=("listing-detail", "auction-bids"),
               focus: bool = True) -> Dialog:
    label = "Current bid" if bids else "Starting bid"
    meta = f'{label} {money(current)} · {bids_text(bids)} · Ends in {AUCTION["left"]}'
    attrs = (" readonly" if busy else "") + (" autofocus" if focus and not busy else "")
    ctl = (f'<div class="input-group"><span class="addon">$</span><input class="input" id="bid" name="amount" inputmode="decimal" '
           f'autocomplete="off" value="{value}"{{db}}{attrs}></div>')
    help_ = None if error else f"Bid {money(minimum)} or more"
    body = item_row("swan-tutu", meta) + field("bid", "Your bid", ctl, help=help_, error=error)
    foot = (btn("Cancel", attrs="disabled" if busy else "")
            + (primary or btn("Place bid", "primary", attrs='type="submit" aria-busy="true"' if busy else 'type="submit"')))
    desc = f"If you win, you pay your bid plus {money(ITEMS['swan-tutu']['ship'])} shipping and tax within 48 hours."
    return Dialog(bg=bg, html=dialog("Place a bid", body, foot, desc=desc, top=top))


@pb.state("default", "The bid dialog on an auction with bids: “Your bid” filled in with the lowest acceptable bid, $19.00, and the hint “Bid $19.00 or more”.")
def _():
    d = bid_dialog()
    d.notes = note("On success the dialog closes, the panel says “You’re the highest bidder” and the toast “Bid placed. You’re the highest bidder” is shown (notifications/auction-toasts: success).",
                   "top: 1rem; left: 1rem;")
    return d


@pb.state("first-bid", "The first bid: filled in with the starting bid, $12.00.")
def _():
    return bid_dialog(value="12.00", minimum=AUCTION["start"], current=AUCTION["start"], bids=0, bg=("listing-detail", "auction"))


@pb.state("invalid", "A bid under the lowest acceptable bid (422 BID_TOO_LOW): the amount is kept and the field says “Bid $19.00 or more”.")
def _():
    return bid_dialog(value="18.50", error=f"Bid {money(AUCTION['minimum'])} or more", focus=False)


@pb.state("conflict", "Another bid won the race (409 OUTBID): “Someone just bid higher”, the current bid refreshed and the new lowest bid filled in.")
def _():
    top = alert("warning", "Someone just bid higher", f"The current bid is now {money(AUCTION['raced'])}.", role="status")
    return bid_dialog(value=f"{AUCTION['raced_minimum']:.2f}", minimum=AUCTION["raced_minimum"], current=AUCTION["raced"], bids=AUCTION["bids"] + 1, top=top)


@pb.state("busy", "Placing the bid: Place bid is busy, the amount read-only and Cancel disabled.")
def _():
    return bid_dialog(busy=True)


@pb.state("ended", "The end time passed while the dialog was open (409 AUCTION_ENDED): no bid is placed.")
def _():
    top = alert("info", "This auction has ended", "Your bid wasn’t placed.", icon="clock", role="status")
    return bid_dialog(top=top, focus=False, primary=btn("Close", "primary", attrs="autofocus"))


@pb.state("failed", "The request failed (network or 5xx): the amount is kept, with Try again.")
def _():
    top = alert("danger", "Couldn’t place your bid", 'Your bid wasn’t placed. Try again. <span class="ref">Ref 4b70-e2c1</span>')
    return bid_dialog(top=top, focus=False, primary=btn("Try again", "primary", attrs='type="submit"'))


# ---------------------------------------------------------------------------
# Auction toasts (L2-115, L2-116)
# ---------------------------------------------------------------------------
at = screen("auction-toasts", "notification", "Auction toasts", opens_from="listing-detail", reqs=["L2-115", "L2-116", "L2-068"])


@at.state("success", "A bid was placed: the dialog closed and the panel says “You’re the highest bidder”.")
def _():
    return Note(bg=("listing-detail", "auction-highest"), toasts=toast_region(toast("success", "Bid placed. You’re the highest bidder")))


@at.state("warning", "Someone bid higher while the member was on the page: the panel now says “You’ve been outbid”.")
def _():
    return Note(bg=("listing-detail", "auction-outbid"), toasts=toast_region(toast("warning", "Someone just bid higher", OUTBID_TEXT, "Place bid")))


@at.state("info", "The auction ended while the member was on the page.")
def _():
    return Note(bg=("listing-detail", "auction-ended"), toasts=toast_region(toast("info", "This auction has ended")))


@at.state("danger", "A bid failed after the dialog closed (the connection dropped): stays until dismissed, offers Try again.")
def _():
    return Note(bg=("listing-detail", "auction-bids"), toasts=toast_region(toast("danger", "Couldn’t place your bid", "Check your connection.", "Try again")))
