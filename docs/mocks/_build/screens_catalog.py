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


# ---------------------------------------------------------------------------
# Catalog
# ---------------------------------------------------------------------------
s = screen("catalog", "page", "Catalog", route="/", reqs=["L2-022", "L2-023", "L2-024", "L2-059", "L2-061", "L2-062", "L2-093", "L2-094", "L2-104"])


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
    head = category_head("Clothing")
    subs = '<div class="cluster" style="margin-bottom: var(--space-8);">' + "".join(
        f'<button class="chip" type="button" aria-pressed="{"true" if i == 0 else "false"}">{t}</button>' for i, t in enumerate(["All clothing"] + CATEGORIES[0][1])) + "</div>"
    return catalog_page(subs + listing_grid(keys), head=head, count="412 items", side={"cat": "Clothing"}, nav="clothing", after_grid=load_more(len(keys), "412"))


@s.state("category-dance", "Dance, a sport category: the header shows Tilly dancing (one Tilly on the screen).")
def _():
    keys = [k for k in CATALOG if ITEMS[k]["cat"] == "Dance"]
    subs = '<div class="cluster" style="margin-bottom: var(--space-8);">' + "".join(
        f'<button class="chip" type="button" aria-pressed="{"true" if i == 0 else "false"}">{t}</button>' for i, t in enumerate(["All dance"] + CATEGORIES[2][1])) + "</div>"
    pg = catalog_page(subs + listing_grid(keys), head=category_head("Dance"), count="186 items", side={"cat": "Dance"}, nav="dance", after_grid=load_more(len(keys), "186"))
    pg.notes = note("Sports shows the pose of the one selected subcategory (soccer, volleyball, gymnastics, the swimming tile) and soccer otherwise. Empty states on these pages drop Tilly.", "top: 14rem; right: 1rem;")
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
    return catalog_page(listing_grid(CATALOG), after_grid=load_more(), phase=2)


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


def detail(key: str, *, actions: str, stamp: str | None = None, user: str = "member", extra_info: str = "", report: bool = True, below: str = "") -> Page:
    it = ITEMS[key]
    total = it["price"] + it["ship"]
    flaws = f'<div class="flaws"><strong>Flaws:</strong> {esc(it["flaws"])}</div>' if it.get("flaws") else ""
    status_line = f'<span class="photo__stamp{" photo__stamp--sold" if stamp == "Sold" else ""}" style="position: static; align-self: flex-start;">{stamp}</span>' if stamp else ""
    rep = ""
    info = (f'<div class="detail__info">'
            f'<div class="stack stack--sm">{status_line}<span class="eyebrow">{esc(it["brand"])}</span><h1 class="detail__title">{esc(it["title"])}</h1>'
            f'<div class="cluster" style="gap: var(--space-3);">{size_tag(it, "size-tag--lg")}{condition_badge(it["cond"])}</div></div>'
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
    return Page(main=f'{crumbs}<div class="detail">{gallery(key, sold=stamp == "Sold")}{info}</div>{below}', user=user, nav=cat_key(it["cat"]))


def buy_bar(total: float, actions: str) -> str:
    if not actions:
        return ""
    return (f'<div class="buy-bar"><div class="buy-bar__price"><span class="price">{money(total)}</span>'
            f'<span class="text-caption">incl. shipping</span></div>{actions}</div>')


def similar(keys) -> str:
    return f'<section class="section" id="similar" style="margin-top: var(--space-20);"><div class="section-head"><h2>Similar in the closet</h2></div>{listing_grid(keys)}</section>'


d = screen("listing-detail", "page", "Listing detail", route="/items/{id}", reqs=["L2-025", "L2-026", "L2-028", "L2-016", "L2-063", "L2-065", "L2-107", "L2-108", "L2-109", "L2-111"],
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
