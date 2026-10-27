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
        for c in ["All"] + [c for c, _, _ in CATEGORIES])
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
    return (f'{cat_group}'
            f'<fieldset class="filters__group"><legend>Brand</legend>{br}<a href="{D("filters")}" class="text-sm">All 10 brands</a></fieldset>'
            f'<fieldset class="filters__group"><legend>Condition</legend>{co}</fieldset>{price_html}')


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
    return Page(main=main, user=user, nav=nav, top=top, phase=phase)


def load_more(shown: int = 16, total: str = "1,284") -> str:
    return (f'<div class="load-more"><span class="text-sm text-muted">Showing {shown} of {total}</span>'
            f'{btn("Load more", attrs="aria-describedby=\"lm-note\"")}<span id="lm-note" class="visually-hidden">Loads the next 24 items</span></div>')


# ---------------------------------------------------------------------------
# Catalog
# ---------------------------------------------------------------------------
s = screen("catalog", "page", "Catalog", route="/", reqs=["L2-022", "L2-023", "L2-024", "L2-059", "L2-061", "L2-062", "L2-093", "L2-094"])


@s.state("default", "Home catalog for a signed-in member: newest listings, filters beside the grid on large screens.")
def _():
    pg = catalog_page(listing_grid(CATALOG), after_grid=load_more())
    pg.notes = note("Pages of 24, newest first. Images below the first screen are lazy loaded with fixed width and height (no layout shift).", "top: 30rem; right: 1rem;")
    return pg


@s.state("guest", "The same catalog for an anonymous visitor: browsing needs no account.")
def _():
    return catalog_page(listing_grid(CATALOG), after_grid=load_more(), user="guest")


@s.state("category", "Clothing only: subcategory chips and the fixed size, reached from the Clothing nav link.")
def _():
    keys = [k for k in CATALOG if ITEMS[k]["cat"] == "Clothing"]
    head = (f'<div class="page-header"><div class="stack stack--sm"><h1>Clothing</h1><div>{size_tag(CATEGORIES[0][2], "size-tag--lg")}</div></div></div>')
    subs = '<div class="cluster" style="margin-bottom: var(--space-8);">' + "".join(
        f'<button class="chip" type="button" aria-pressed="{"true" if i == 0 else "false"}">{t}</button>' for i, t in enumerate(["All clothing"] + CATEGORIES[0][1])) + "</div>"
    return catalog_page(subs + listing_grid(keys), head=head, count="412 items", side={"cat": "Clothing"}, nav="clothing", after_grid=load_more(len(keys), "412"))


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
    grid = empty("tag", "Nothing listed yet", "Be the first to list a dress, a pair of sneakers or anything your daughter has outgrown.", btn("Sell an item", "primary", P("sell")))
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
    return catalog_page(listing_grid(CATALOG), after_grid=load_more(), phase=2)


@s.state("no-results", "Search and filters matched nothing; filters stay visible and clearable.")
def _():
    chips = chip("Dance") + chip("New with tags")
    grid = empty("search", "No items match", "Try fewer filters or a different word.", btn("Clear search and filters", "primary"), cls="empty--compact")
    pg = catalog_page(grid, head='<div class="page-header"><div><p class="eyebrow">Search</p><h1>“sequin cape”</h1></div></div>',
                      count="0 items", chips=chips, side={"cat": "Dance", "conds": ["New with tags"]})
    return pg


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


def gallery(key: str, current: int = 1) -> str:
    it = ITEMS[key]
    n = min(it["photos"], 6)
    thumbs = "".join(
        f'<li><button type="button" aria-label="Show photo {i} of {n}"{" aria-current=\"true\"" if i == current else ""}>{photo(key, tag=False, alt="", tone=((it["tone"] + i - 2) % 6) + 1)}</button></li>'
        for i in range(1, n + 1))
    dots = "".join(f'<span{" aria-current=\"true\"" if i == current else ""}></span>' for i in range(1, n + 1))
    return (f'<section class="gallery" aria-label="Photos"><div class="gallery__main">{photo(key, tag=False, n=current)}</div>'
            f'<div class="gallery__dots" aria-hidden="true">{dots}</div><ul class="gallery__thumbs" role="list">{thumbs}</ul></section>')


def seller_block(handle: str) -> str:
    p = PEOPLE[handle]
    rep = rating(handle) if p["reviews"] else '<span class="badge">New seller</span>'
    sales = f'{p["sales"]} sales · ' if p["sales"] else ""
    return (f'<div class="seller">{avatar(handle)}<div class="stack stack--sm" style="flex: 1; gap: var(--space-1);">'
            f'<a class="seller__name" href="{P("profile")}">{handle}</a><span class="seller__meta">{sales}{p["city"]}</span>{rep}</div></div>')


def detail(key: str, *, actions: str, stamp: str | None = None, user: str = "member", extra_info: str = "", report: bool = True, below: str = "") -> Page:
    it = ITEMS[key]
    total = it["price"] + it["ship"]
    flaws = f'<div class="flaws"><strong>Flaws:</strong> {esc(it["flaws"])}</div>' if it.get("flaws") else ""
    status_line = f'<span class="photo__stamp{" photo__stamp--sold" if stamp == "Sold" else ""}" style="position: static; align-self: flex-start;">{stamp}</span>' if stamp else ""
    rep = ""
    info = (f'<div class="detail__info">'
            f'<div class="stack stack--sm">{status_line}<span class="eyebrow">{esc(it["brand"])}</span><h1 class="detail__title">{esc(it["title"])}</h1>'
            f'<div class="cluster" style="gap: var(--space-3);">{size_tag(it, "size-tag--lg")}<span class="text-muted">{esc(it["cond"])}</span></div></div>'
            f'<div class="detail__price"><span class="price price--lg">{money(it["price"])}</span>'
            f'<span class="text-muted text-sm">{ship_text(it["ship"])} · <span class="num">{money(total)}</span> total before tax</span></div>'
            f'{extra_info}'
            f'{buy_bar(total, actions)}'
            f'{flaws}<p>{esc(DESC.get(key, "Freshly washed, light wear. Smoke-free, pet-free home."))}</p>'
            f'<dl class="facts"><dt>Size</dt><dd>{size_long(it)}</dd><dt>Category</dt><dd>{it["cat"]} · {it["sub"]}</dd>'
            f'<dt>Condition</dt><dd>{esc(it["cond"])}</dd><dt>Listed</dt><dd><time datetime="2026-10-25">{it["listed"]}</time></dd></dl>'
            f'{seller_block(it["seller"])}{rep}</div>')
    crumbs = (f'<nav aria-label="Breadcrumb" style="margin-top: var(--space-6);"><ol class="breadcrumb"><li><a href="{P("catalog")}">Shop</a></li>'
              f'<li><a href="{P("catalog", "category")}">{it["cat"]}</a></li><li aria-current="page">{it["sub"]}</li></ol></nav>')
    return Page(main=f'{crumbs}<div class="detail">{gallery(key)}{info}</div>{below}', user=user, nav=cat_key(it["cat"]))


def buy_bar(total: float, actions: str) -> str:
    if not actions:
        return ""
    return (f'<div class="buy-bar"><div class="buy-bar__price"><span class="price">{money(total)}</span>'
            f'<span class="text-caption">incl. shipping</span></div>{actions}</div>')


def similar(keys) -> str:
    return f'<section class="section" style="margin-top: var(--space-20);"><div class="section-head"><h2>More for girls</h2></div>{listing_grid(keys)}</section>'


d = screen("listing-detail", "page", "Listing detail", route="/items/{id}", reqs=["L2-025", "L2-026", "L2-028", "L2-016", "L2-063", "L2-065"],
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
    return detail("party-dress", stamp="Sold", actions="", report=False, below=similar(["pleated-skirt", "pink-tutu", "velvet-dress", "rainbow-romper"]))


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
            f'<span class="ref">Ref 2b9e-4410</span><div class="actions">{btn("Try again", "primary", icon="refresh")}{btn("Back to shop", href=P("catalog"))}</div></div>')
    return Page(main=main)


@d.state("unavailable", "Removed or draft listing opened by someone else: “This item isn’t available” (HTTP 404).")
def _():
    main = ('<div class="center-state"><span class="code" aria-hidden="true">4<em>0</em>4</span><h1>This item isn’t available</h1>'
            '<p class="text-muted">It may have been sold or taken down.</p>'
            f'<div class="actions">{btn("Shop similar items", "primary", P("catalog"))}</div></div>')
    return Page(main=main)


# ---------------------------------------------------------------------------
# Menu sheet (category links collapse into it under 1280px, L2-062)
# ---------------------------------------------------------------------------
m = screen("nav-menu", "dialog", "Menu", opens_from="catalog", reqs=["L2-062", "L2-065", "L2-093", "L2-094"],
           na={"busy": "Opening the menu makes no request.", "invalid": "The menu has no inputs."})


def menu_sheet(guest: bool, selling: bool = True) -> str:
    cats = "".join(f'<li><a href="{nav_href(k)}">{t}{ic("chevron-right")}</a></li>' for k, t in NAV)
    if guest:
        sell = f'<li><a href="{P("sell")}">Sell an item</a></li>' if selling else ""
        small = f'{sell}<li><a href="{P("rules")}">Marketplace rules</a></li>'
        foot = btn("Sign in", href=P("sign-in")) + btn("Join TuTuMuch", "primary", P("register"))
    else:
        links = [("Sell an item", "sell"), ("Your listings", "my-listings"), ("Purchases", "purchases"), ("Sales", "sales"), ("Settings", "settings")]
        if not selling:
            links = [(t, pid) for t, pid in links if pid not in ("sell", "my-listings", "sales")]
        small = "".join(f'<li><a href="{P(pid)}">{t}</a></li>' for t, pid in links)
        foot = btn("Sign out", icon="log-out")
    return (f'<div class="menu-sheet" id="menu" role="dialog" aria-modal="true" aria-labelledby="dlg-title" data-dismiss-on-escape>'
            f'<div class="menu-sheet__head"><span class="logo">{LOGO_IMG}</span>'
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
