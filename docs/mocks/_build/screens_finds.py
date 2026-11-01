"""Shelf finds and grabs: the finds list and page, posting, marking gone and withdrawing, "Grab it for me",
the shopper's grabs, the administrator's grabs, toasts and emails (L1-025, L2-113 to L2-121,
ADR catalog/0009, ADR orders/0009)."""
from mk import *
from screens_orders import address_block, address_form, card_fields, saved_address
from screens_system import admin_page, head

POSTER_HANDLE = "AnasCloset"
MINE = "find-leotard"          # the find most screens follow; the owner offers to grab it for $8.00
GRAB = "GR-1007"               # Tessa's grab on it


def find(key: str) -> dict:
    return FINDS[key]


def maps_url(f: dict) -> str:
    q = f"{f['store']}, {f['address']}".replace("’", "'")
    return "https://www.google.com/maps/search/?api=1&amp;query=" + esc(q).replace(" ", "+").replace(",", "%2C")


def grab_amounts(key: str) -> dict:
    """Shelf price, grab fee, tax, total, deposit and the refund after acceptance (ADR orders/0009)."""
    f = find(key)
    g = next((g for g in GRABS.values() if g["find"] == key), None)
    tax = g["tax"] if g else round((f["price"] + f["grab"]) * 0.08875, 2)
    total = round(f["price"] + f["grab"] + tax, 2)
    deposit = min(max(round(f["price"] * 0.2, 2), 5.00), f["price"])
    return {"price": f["price"], "fee": f["grab"], "tax": tax, "total": total, "deposit": deposit, "refund": round(total - deposit, 2)}


# ---------------------------------------------------------------------------
# Shared pieces
# ---------------------------------------------------------------------------
def find_card(key: str, href: str | None = None) -> str:
    """Find card (design system: Find card): map-pin well, title, category, store, seen date, price, Grab available."""
    f = find(key)
    href = href or P("find-detail", "grab-open" if f.get("open") else ("default" if f["grab"] else "no-grab"))
    badge = '<span class="badge badge--info">Grab available</span>' if f["grab"] and not f.get("open") else ""
    return (f'<li><a class="find-card" href="{href}"><span class="find-card__well" aria-hidden="true">{ic("map-pin")}</span>'
            f'<div class="find-card__meta"><span class="find-card__title">{esc(f["title"])}</span>'
            f'<span class="find-card__seen">{esc(f["cat"])}</span>'
            f'<span class="find-card__store">{ic("map-pin")}{esc(f["store"])}</span>'
            f'<span class="find-card__seen">Seen <time datetime="{f["seen_iso"]}">{f["seen"]}</time></span>'
            f'<span class="find-card__foot"><span class="price">{money(f["price"])}</span>{badge}</span></div></a></li>')


def find_grid(keys) -> str:
    return '<ul class="listing-grid find-grid" role="list">' + "".join(find_card(k) for k in keys) + "</ul>"


def finds_head(can_post: bool = True) -> str:
    act = btn("Post a shelf find", "primary", P("post-find"), icon="plus") if can_post else ""
    return (f'<div class="page-header"><div class="stack stack--sm"><h1>Shelf finds</h1>'
            f'<p class="subtitle">Spotted in a store by our community. Go get it, or let TuTuMuch grab it for you.</p></div>{act}</div>')


def finds_page(body: str, *, can_post: bool = True, user: str = "member", phase: int = 3, top: str = "", after: str = "") -> Page:
    return Page(main=finds_head(can_post) + body, nav="finds", user=user, phase=phase, top=top, after=after,
                main_class="container container--wide page")


def find_skeleton(n: int = 8) -> str:
    card = ('<li><div class="find-card" aria-hidden="true"><span class="skeleton skeleton--rect find-card__well"></span>'
            '<div class="find-card__meta"><span class="skeleton skeleton--text" style="width: 85%;"></span>'
            '<span class="skeleton skeleton--text" style="width: 40%;"></span><span class="skeleton skeleton--text" style="width: 60%;"></span>'
            '<span class="skeleton skeleton--title" style="width: 30%;"></span></div></div></li>')
    return f'<ul class="listing-grid find-grid" role="list" aria-busy="true" aria-label="Loading shelf finds">{card * n}</ul>'


# ---------------------------------------------------------------------------
# Shelf finds list (L2-114)
# ---------------------------------------------------------------------------
sf = screen("shelf-finds", "page", "Shelf finds", route="/finds", reqs=["L2-114", "L2-113", "L2-121", "L2-061", "L2-065", "L2-096"])


@sf.state("default", "Six finds, newest first: title, category, store, seen date, shelf price, and “Grab available” where TuTuMuch offers a grab and none is open.")
def _():
    more = f'<div class="load-more"><p class="text-sm text-muted">Showing 6 of 6 finds</p></div>'
    pg = finds_page(find_grid(FINDS_ORDER) + more)
    pg.notes = note("24 finds per page with Load more (L2-022). Finds are not listings: they never appear in the catalog, search or category pages. "
                    "The camera bundle has an open grab, so it shows no badge.", "top: 12rem; right: 1rem;")
    return pg


@sf.state("guest", "A visitor browses the same list; posting needs selling rights, so there is no “Post a shelf find”.")
def _():
    return finds_page(find_grid(FINDS_ORDER), can_post=False, user="guest")


@sf.state("phase-one", "Launch Phase 1 for a shopper: no category row, so Shelf finds is reached from the footer; no Post action (L2-091).")
def _():
    pg = finds_page(find_grid(FINDS_ORDER), can_post=False, phase=1)
    pg.notes = note("In Phases 1 and 2 only an administrator posts finds; in Phase 1 the link lives in the footer.", "top: 10rem; right: 1rem;")
    return pg


@sf.state("loading", "Skeleton cards shaped like find cards.")
def _():
    return finds_page(find_skeleton())


@sf.state("empty", "No finds yet: Tilly, one line and the one action to post.")
def _():
    body = empty("map-pin", "No shelf finds yet", "When someone spots something great in a store, it shows up here.",
                 btn("Post a shelf find", "primary", P("post-find")))
    return finds_page(body, can_post=False)


@sf.state("error", "Finds failed to load: a reference and Try again.")
def _():
    body = empty("refresh", "Couldn’t load shelf finds", 'Something went wrong on our side. <span class="ref">Ref 6d1f-02ab</span>',
                 btn("Try again", "primary", icon="refresh"), cls="empty--error empty--compact")
    return finds_page(body)


# ---------------------------------------------------------------------------
# Find detail (L2-115, L2-116)
# ---------------------------------------------------------------------------
fd = screen("find-detail", "page", "Shelf find", route="/finds/{reference}",
            reqs=["L2-115", "L2-116", "L2-117", "L2-121", "L2-049", "L2-063", "L2-065"],
            na={"empty": "A find always has its required fields; a missing one is the not-found state."})

NOTICE = "Prices and stock change. Check with the store before you go."


def grab_box(key: str, state: str = "offer", user: str = "member") -> str:
    """The "Grab it for me" box: the offer with the fee and the primary button, or the open-grab line."""
    f = find(key)
    if state == "open":
        return (f'<section class="panel panel--sunken stack" aria-labelledby="grab-h"><h2 id="grab-h" class="panel__title">Grab it for me</h2>'
                f'<p class="text-muted" style="margin: 0;">Someone already asked TuTuMuch to grab this one</p></section>')
    href = P("sign-in", "return") if user == "guest" else P("grab")
    return (f'<section class="panel panel--sunken stack" aria-labelledby="grab-h"><h2 id="grab-h" class="panel__title">Grab it for me</h2>'
            f'<p style="margin: 0;">TuTuMuch buys it at the store and sends it to you for a {money(f["grab"])} grab fee.</p>'
            f'<div>{btn("Grab it for me", "primary lg", href)}</div></section>')


def find_detail(key: str, *, box: str = "", actions: str = "", user: str = "member", gone: str = "", top: str = "", after: str = "") -> Page:
    f = find(key)
    note_html = f'<p style="margin: 0; white-space: pre-line;">{esc(f["note"])}</p>' if f["note"] else ""
    well = f'<div class="find-card__well" aria-hidden="true" style="align-self: start;">{ic("map-pin")}</div>'
    if gone:
        stamp = f'<span class="photo__stamp photo__stamp--sold" style="position: static; align-self: flex-start;">Gone</span>'
        where = (f'<section class="sold-notice" role="status" aria-labelledby="gone-h">{stamp}'
                 f'<h2 class="sold-notice__title" id="gone-h" tabindex="-1">{gone}</h2>'
                 f'<p class="sold-notice__text">Seen at {esc(f["store"])} on {f["seen"]}.</p>'
                 f'<div class="sold-notice__actions">{btn("See more shelf finds", "primary lg", P("shelf-finds"))}</div></section>')
        box = ""
        actions = ""
    else:
        where = (f'<dl class="facts"><dt>Store</dt><dd>{esc(f["store"])}</dd><dt>Address</dt><dd>{esc(f["address"])}<br>'
                 f'<a href="{maps_url(f)}" target="_blank" rel="noopener noreferrer" style="display: inline-flex; align-items: center; gap: var(--space-1);">'
                 f'Open in maps{ic("external", "icon icon--sm")}<span class="visually-hidden"> (opens in a new tab)</span></a></dd>'
                 f'<dt>Seen</dt><dd>Seen in store on <time datetime="{f["seen_iso"]}">{f["seen"]}</time></dd></dl>'
                 f'{alert("info", NOTICE, icon="info")}')
    poster = (f'<div class="seller">{avatar(f["poster"])}<div class="stack stack--sm" style="flex: 1; gap: var(--space-1);">'
              f'<span class="text-caption">Posted by</span><a class="seller__name" href="{P("profile")}">{f["poster"]}</a></div></div>')
    acts = f'<div class="cluster">{actions}</div>' if actions else ""
    info = (f'<div class="detail__info">{top}'
            f'<div class="stack stack--sm"><span class="eyebrow">Shelf find · {esc(f["cat"])}</span><h1 class="detail__title">{esc(f["title"])}</h1></div>'
            f'<div class="detail__price"><span class="price price--lg">{money(f["price"])}</span><span class="text-muted text-sm">Shelf price · Girls’</span></div>'
            f'{where}{box}{acts}{note_html}{poster}</div>')
    crumbs = (f'<nav aria-label="Breadcrumb" style="margin-top: var(--space-6);"><ol class="breadcrumb"><li><a href="{P("shelf-finds")}">Shelf finds</a></li>'
              f'<li aria-current="page">{esc(f["title"])}</li></ol></nav>')
    return Page(main=f'{crumbs}<div class="detail">{well}{info}</div>', user=user, nav="finds", after=after)


@fd.state("default", "An Active find seen by a member, with the store, address, Open in maps, the notice and the “Grab it for me” box.")
def _():
    pg = find_detail(MINE, box=grab_box(MINE))
    pg.notes = note("Open in maps opens the maps search for “{store}, {address}” in a new tab (rel=\"noopener noreferrer\"). "
                    "The address is the store’s, never the poster’s home (L2-121).", "top: 8rem; right: 1rem;")
    return pg


@fd.state("guest", "A visitor: “Grab it for me” goes to sign in with returnUrl=/finds/F-1042/grab.")
def _():
    pg = find_detail(MINE, box=grab_box(MINE, user="guest"), user="guest")
    pg.notes = note("URL after the button: /sign-in?returnUrl=%2Ffinds%2FF-1042%2Fgrab (L2-117 AC9).", "top: 8rem; right: 1rem;")
    return pg


@fd.state("no-grab", "A member’s find with no grab offer: the store details and Open in maps only.")
def _():
    return find_detail("find-sneakers")


@fd.state("grab-open", "Another shopper’s grab is open: the box says so and has no button; no shopper is named (L2-121).")
def _():
    return find_detail("find-camera", box=grab_box("find-camera", "open"))


@fd.state("own", "The poster views their own Active find: “Mark as gone” and “Withdraw” instead of the grab button (L2-115 AC6).")
def _():
    acts = btn("Mark as gone", "lg", D("mark-find-gone")) + btn("Withdraw", "ghost lg", D("withdraw-find"))
    return find_detail(MINE, actions=acts, user="admin")


@fd.state("gone", "Marked gone: “This find is gone” with “See more shelf finds”, no store call to action and no grab box.")
def _():
    return find_detail(MINE, gone="This find is gone")


@fd.state("expired", "Fourteen days after it was seen: “This find has expired”.")
def _():
    return find_detail("find-dress", gone="This find has expired")


@fd.state("success", "Just posted: the poster lands on the find with the toast “Your find is live” (L2-113 AC1).")
def _():
    acts = btn("Mark as gone", "lg", D("mark-find-gone")) + btn("Withdraw", "ghost lg", D("withdraw-find"))
    return find_detail(MINE, actions=acts, user="admin", after=toast_region(toast("success", "Your find is live", "Shoppers can see it on Shelf finds.")))


@fd.state("loading", "Skeleton in the detail layout.")
def _():
    sk = ('<div class="detail" aria-busy="true" aria-label="Loading find"><span class="skeleton skeleton--rect" style="aspect-ratio: 4 / 3;"></span>'
          '<div class="detail__info"><div class="stack stack--sm"><span class="skeleton skeleton--text" style="width: 30%;"></span>'
          '<span class="skeleton skeleton--title" style="width: 80%;"></span></div><span class="skeleton skeleton--title" style="width: 25%;"></span>'
          '<span class="skeleton skeleton--rect" style="height: 6rem;"></span><span class="skeleton skeleton--rect" style="height: 8rem;"></span></div></div>')
    return Page(main='<h1 class="visually-hidden">Loading find</h1>' + sk, nav="finds")


@fd.state("error", "The find failed to load: a reference, Try again and the way back.")
def _():
    main = ('<div class="center-state"><h1>Couldn’t load this find</h1><p class="text-muted">It’s us, not you. Try again in a moment.</p>'
            f'<span class="ref">Ref 8e40-c2d7</span><div class="actions">{btn("Try again", "primary", icon="refresh")}{btn("See shelf finds", href=P("shelf-finds"))}</div></div>')
    return Page(main=main, nav="finds")


@fd.state("not-found", "Unknown or withdrawn reference, or a closed poster: 404 FIND_NOT_FOUND, the not-found state of L2-025.")
def _():
    main = ('<div class="center-state"><span class="code" aria-hidden="true">4<em>0</em>4</span><h1>We couldn’t find that</h1>'
            '<p class="text-muted">The find may have been withdrawn.</p>'
            f'<div class="actions">{btn("See more shelf finds", "primary", P("shelf-finds"))}</div></div>')
    return Page(main=main, nav="finds")


# ---------------------------------------------------------------------------
# Post a shelf find (L2-113)
# ---------------------------------------------------------------------------
pf = screen("post-find", "page", "Post a shelf find", route="/finds/new",
            reqs=["L2-113", "L2-091", "L2-049", "L2-051", "L2-003", "L2-063", "L2-065"],
            na={"empty": "The form is the page; a blank form is the default state.",
                "loading": "Nothing loads before the form; the launch phase and the role come with the session."})


def cat_select(chosen: str = "") -> str:
    opts = '<option value="">Choose a category</option>' + "".join(
        f'<option{" selected" if c == chosen else ""}>{esc(c)}</option>' for c, _, _ in CATEGORIES)
    return f'<select class="select" id="f-cat" name="f-cat"{{db}}>{opts}</select>'


def money_in(fid: str, value: str) -> str:
    v = f' value="{value}"' if value else ""
    return f'<div class="input-group"><span class="addon">$</span><input class="input" id="{fid}" name="{fid}" inputmode="decimal"{v}{{db}}></div>'


def post_form(v: dict, *, admin: bool = False, grab_on: bool = False, errors: dict | None = None, busy: bool = False, top: str = "") -> str:
    e = errors or {}
    note_v = v.get("note", "")
    size = ""
    if v.get("cat"):
        size = (f'<div class="fixed-size">{size_tag("Girls\'", "size-tag--lg")}<span>Size: Girls’</span>'
                f'<span class="fixed-size__label">Every find is for girls</span></div>')
    grab = ""
    if admin:
        sw = (f'<label class="switch switch--labelled"><input type="checkbox" role="switch" id="f-grab"{" checked" if grab_on else ""}>'
              f'<span>Offer to grab it</span></label>'
              f'<p class="field__help" style="margin: 0;">TuTuMuch buys it at the store and sends it to the shopper.</p>')
        fee = field("f-fee", "Grab fee", money_in("f-fee", v.get("fee", "")), error=e.get("f-fee"), help="$1.00 to $50.00, paid by the shopper.") if grab_on else ""
        grab = f'<section class="form-section" aria-labelledby="s-grab"><h2 id="s-grab">Grab it for me</h2>{sw}{fee}</section>'
    post = btn("Post find", "primary", attrs=('aria-busy="true"' if busy else 'type="submit"'))
    cancel = f'<a class="btn btn--ghost" href="{P("shelf-finds")}">Cancel</a>'
    return (f'<form class="sell-form" novalidate>{top}'
            f'<section class="form-section" aria-labelledby="s-item"><h2 id="s-item">The item</h2>'
            + field("f-title", "Title", value=v.get("title", ""), error=e.get("f-title"), counter=f'{len(v.get("title", ""))} / 80')
            + field("f-cat", "Category", cat_select(v.get("cat", "")), error=e.get("f-cat"))
            + size
            + field("f-price", "Shelf price", money_in("f-price", v.get("price", "")), error=e.get("f-price"), help="What the store’s tag says.")
            + '</section>'
            f'<section class="form-section" aria-labelledby="s-store"><h2 id="s-store">Where you saw it</h2>'
            + field("f-store", "Store name", value=v.get("store", ""), error=e.get("f-store"))
            + field("f-address", "Store address", f'<textarea class="textarea" id="f-address" name="f-address" rows="2"{{db}}>{esc(v.get("address", ""))}</textarea>',
                    error=e.get("f-address"), help="The store’s street address or mall and city. Never your home.")
            + field("f-seen", "Seen on", value=v.get("seen", ""), type="date", error=e.get("f-seen"), help="Today or up to 7 days ago.")
            + field("f-note", "Note", f'<textarea class="textarea" id="f-note" name="f-note" rows="3"{{db}}>{esc(note_v)}</textarea>', optional=True,
                    error=e.get("f-note"), counter=f"{len(note_v)} / 500", help="Where in the store, sizes left, anything that helps.")
            + '</section>'
            + grab
            + f'<div class="form-actions form-actions--sticky form-actions--split">{cancel}{post}</div></form>')


def post_page(form: str, user: str = "member") -> Page:
    h = ('<div class="page-header"><div class="stack stack--sm"><h1>Post a shelf find</h1>'
         '<p class="subtitle">Saw something great for girls in a store? Tell everyone where to get it.</p></div></div>')
    return Page(main=h + form, nav="finds", user=user, main_class="container page page--form", footer=False)


def blocked(icon: str, title: str, text: str, actions: str, user: str = "member", phase: int = 3) -> Page:
    main = f'<div class="center-state"><div class="empty__icon">{ic(icon, "icon icon--lg")}</div><h1>{title}</h1><p class="text-muted">{text}</p><div class="actions">{actions}</div></div>'
    return Page(main=main, nav="finds", user=user, phase=phase)


FILLED = {"title": "Lilac ballet leotard", "cat": "Dance", "price": "40.00", "store": "Dancewear Corner",
          "address": "214 Smith St, Brooklyn, NY 11201", "seen": "2026-10-24", "note": "Rack at the back, sizes 6 to 10. Three left when I was there."}


@pf.state("default", "A blank form for a member in Phase 3: the item, where it was seen, an optional note. No grab offer for members (ADR catalog/0009).")
def _():
    return post_page(post_form({}))


@pf.state("filled", "Every field filled in; the size shows as Girls’ once a category is chosen.")
def _():
    return post_page(post_form({**FILLED, "price": "24.99", "title": "Light-up glitter sneakers", "cat": "Shoes", "store": "Target",
                                "address": "139 Flatbush Ave, Brooklyn, NY 11217", "seen": "2026-10-23", "note": ""}))


@pf.state("admin", "An administrator turns on “Offer to grab it” and sets an $8.00 grab fee.")
def _():
    pg = post_page(post_form({**FILLED, "fee": "8.00"}, admin=True, grab_on=True), user="admin")
    pg.notes = note("Only administrators see “Offer to grab it”; a member who sends a grab fee gets 422 GRAB_OFFER_NOT_ALLOWED (L2-113 AC6).", "top: 8rem; right: 1rem;")
    return pg


@pf.state("invalid", "Post refused: the summary on top, each problem on its field; focus moves to the summary.")
def _():
    errs = {"f-title": "Title must be 5 to 80 characters", "f-price": "Enter a price from $1.00 to $5,000.00",
            "f-address": "Enter the store address in 5 to 200 characters", "f-seen": "Choose a date from the last 7 days"}
    top = error_summary([("f-title", errs["f-title"]), ("f-price", errs["f-price"]), ("f-address", errs["f-address"]), ("f-seen", errs["f-seen"])])
    return post_page(post_form({"title": "Tutu", "cat": "Dance", "price": "0.50", "store": "Dancewear Corner", "address": "", "seen": "2026-10-17"},
                               errors=errs, top=top))


@pf.state("invalid-fee", "An administrator’s grab fee outside $1.00 to $50.00 (422 GRAB_FEE_OUT_OF_RANGE).")
def _():
    errs = {"f-fee": "Enter a grab fee from $1.00 to $50.00"}
    return post_page(post_form({**FILLED, "fee": "50.01"}, admin=True, grab_on=True, errors=errs, top=error_summary([("f-fee", errs["f-fee"])])), user="admin")


@pf.state("submitting", "Posting: the button is busy and nothing shifts.")
def _():
    return post_page(post_form(FILLED, busy=True))


@pf.state("error", "Posting failed on our side: the form keeps every value and offers Try again.")
def _():
    top = alert("danger", "Couldn’t post your find", 'Your details are still here. Try again. <span class="ref">Ref 1a77-f0c3</span>')
    return post_page(post_form(FILLED, top=top))


@pf.state("limited", "A 21st find today: 429 with “That’s 20 finds today” (L2-113 AC8).")
def _():
    return blocked("clock", "That’s 20 finds today", "Thanks for spotting so much. You can post again tomorrow.",
                   btn("See shelf finds", "primary", P("shelf-finds")))


@pf.state("selling-not-open", "A member without the Administrator role in Phase 1 or 2: the “Selling opens soon” state of L2-091.")
def _():
    return blocked("clock", "Selling opens soon", "For now TuTuMuch posts every find. You can still shop the closet.",
                   btn("Shop the closet", "primary", P("catalog")), phase=2)


@pf.state("unverified", "An unverified member in Phase 3: 403 EMAIL_NOT_VERIFIED; verify first.")
def _():
    return blocked("mail", "Verify your email to post finds", f'We sent a link to {esc(PEOPLE[ME]["email"])}.',
                   btn("Resend link", "primary", D("verify-email-prompt")) + btn("See shelf finds", href=P("shelf-finds")), user="unverified")


@pf.state("signed-out", "A visitor opening /finds/new goes to sign in and comes back.")
def _():
    pg = REGISTRY["sign-in"].states["default"][1]()
    pg.notes = note("URL: /sign-in?returnUrl=%2Ffinds%2Fnew (L2-113 AC3).", "top: 6rem; right: 1rem;")
    return pg


# ---------------------------------------------------------------------------
# Grab it for me (L2-117)
# ---------------------------------------------------------------------------
gp = screen("grab", "page", "Grab it for me", route="/finds/{reference}/grab",
            reqs=["L2-117", "L2-027", "L2-030", "L2-029", "L2-003", "L2-063", "L2-065"],
            na={"empty": "The page always has one find to grab."})


def grab_summary(key: str = MINE) -> str:
    a = grab_amounts(key)
    f = find(key)
    rows = [("Shelf price", money(a["price"])), ("Grab fee", money(a["fee"])), ("Sales tax", money(a["tax"]))]
    item = (f'<div class="item-row"><div class="thumb"><div class="photo photo--none">{ic("map-pin")}</div></div>'
            f'<div class="item-row__body"><span class="item-row__title">{esc(f["title"])}</span>'
            f'<span class="item-row__meta">{esc(f["store"])} · {f["id"]}</span></div></div>')
    return (f'<aside class="panel stack stack--lg" aria-labelledby="sum-title"><h2 id="sum-title" class="panel__title">Summary</h2>'
            f'{item}{totals(rows, ("Total", money(a["total"])))}'
            f'<p class="text-sm" style="margin: 0;"><strong>Non-refundable deposit: {money(a["deposit"])}</strong></p></aside>')


def deposit_box(key: str = MINE) -> str:
    a = grab_amounts(key)
    return alert("info", "How the deposit works",
                 f"You pay the total now. If TuTuMuch can’t grab it, you get everything back. If you cancel after TuTuMuch accepts, "
                 f"you get everything back except the {money(a['deposit'])} deposit.", icon="info")


def ack(checked: bool = False, error: str = "") -> str:
    a = grab_amounts(MINE)
    inv = ' aria-invalid="true" aria-describedby="ack-error"' if error else ""
    e = f'<span class="field__error" id="ack-error">{esc(error)}</span>' if error else ""
    return (f'<div class="field"><label class="choice"><input type="checkbox" id="ack"{" checked" if checked else ""}{inv}>'
            f'<span class="choice__text">I understand the {money(a["deposit"])} deposit is non-refundable once TuTuMuch accepts</span></label>{e}</div>')


def grab_page(*, address: str | None = None, ticked: bool = False, top: str = "", ack_error: str = "", pay: str | None = None, pay_note: str = "") -> Page:
    a = grab_amounts(MINE)
    address = address if address is not None else saved_address()
    disabled = "" if ticked else " disabled"
    pay = pay or btn(f"Pay {money(a['total'])}", "primary lg block", attrs=f'type="submit"{disabled}')
    cards = card_fields() if ticked else ('<div class="stack" aria-disabled="true" style="opacity: 0.55;">' + card_fields() + "</div>")
    left = (f'<form class="stack stack--lg" novalidate>{top}'
            f'<section class="checkout-step" aria-labelledby="ship-h"><h2 id="ship-h">Send it to</h2>{address}</section>'
            f'<section class="checkout-step stack" aria-labelledby="dep-h"><h2 id="dep-h">The deposit</h2>{deposit_box()}{ack(ticked, ack_error)}</section>'
            f'<section class="checkout-step" aria-labelledby="pay-h"><h2 id="pay-h">Pay</h2>{cards}</section>'
            f'<div class="stack stack--sm">{pay}{pay_note}<a class="btn btn--ghost btn--block" href="{P("find-detail")}">Back to the find</a></div></form>')
    h = (f'<div class="page-header"><div class="stack stack--sm"><p class="eyebrow">Shelf find {find(MINE)["id"]}</p><h1>Grab it for me</h1>'
         f'<p class="subtitle">TuTuMuch goes to {esc(find(MINE)["store"])}, buys it and sends it to you.</p></div></div>')
    return Page(main=f'{h}<div class="split">{left}{grab_summary()}</div>', nav="finds", footer=False)


@gp.state("default", "Saved address chosen and priced: the summary with the deposit line, the deposit box, the unticked box; card fields and Pay wait for the tick.")
def _():
    pg = grab_page()
    pg.notes = note("POST /api/grabs/quote prices the address. The card fields and “Pay” stay disabled until the box is ticked (L2-117).", "top: 7rem; right: 1rem;")
    return pg


@gp.state("acknowledged", "The box is ticked: the hosted card fields and “Pay $52.26” are enabled.")
def _():
    return grab_page(ticked=True)


@gp.state("new-address", "A new US address: the address form of checkout; the quote follows when it is complete.")
def _():
    return grab_page(address=saved_address(False) + '<div class="stack">' + address_form({"name": "Tessa Bell"}) + "</div>")


@gp.state("invalid", "Pay pressed without the tick: nothing is sent, the error sits on the box and in the summary (L2-117 AC3).")
def _():
    msg = "Tick the box to confirm you understand the deposit"
    return grab_page(top=error_summary([("ack", msg)]), ack_error=msg,
                     pay=btn(f"Pay {money(grab_amounts(MINE)['total'])}", "primary lg block", attrs='type="submit"'))


@gp.state("non-us", "A non-US address: 422 NON_US_ADDRESS, as checkout.")
def _():
    errs = {"a-country": "TuTuMuch ships within the United States only"}
    top = alert("danger", "TuTuMuch ships within the United States only", "Choose a US address to continue.")
    vals = {"name": "Tessa Bell", "street": "88 Queen St W", "city": "Toronto", "zip": "M5H 2M9", "country": "Canada"}
    return grab_page(top=top, address=saved_address(False) + '<div class="stack">' + address_form(vals, errs) + "</div>")


@gp.state("submitting", "Paying: Pay is busy; one idempotency key; the result comes from the provider’s webhook.")
def _():
    total = money(grab_amounts(MINE)["total"])
    return grab_page(ticked=True, pay=btn(f"Pay {total}", "primary lg block", attrs='type="submit" aria-busy="true"'),
                     pay_note='<p class="text-caption" role="status" style="text-align: center;">Confirming with your bank…</p>')


@gp.state("declined", "The card was declined: the reason, the address and the tick are kept; the find has no open grab (L2-117 AC5).")
def _():
    top = alert("danger", "Your bank declined the card", "Insufficient funds. Try another card.")
    return grab_page(ticked=True, top=top)


@gp.state("in-progress", "Another shopper’s grab is pending or open: 409 GRAB_IN_PROGRESS.")
def _():
    main = (f'<div class="center-state">{ic("clock", "icon icon--lg")}<h1>Someone already asked TuTuMuch to grab this one</h1>'
            f'<p class="text-muted">If that grab falls through, the button comes back on the find.</p>'
            f'<div class="actions">{btn("See more shelf finds", "primary", P("shelf-finds"))}{btn("Back to the find", href=P("find-detail", "grab-open"))}</div></div>')
    return Page(main=main, nav="finds")


@gp.state("unverified", "An unverified member: “Verify your email to ask for a grab” (403 EMAIL_NOT_VERIFIED).")
def _():
    main = (f'<div class="center-state"><div class="empty__icon">{ic("mail", "icon icon--lg")}</div><h1>Verify your email to ask for a grab</h1>'
            f'<p class="text-muted">We sent a link to {esc(PEOPLE[ME]["email"])}.</p>'
            f'<div class="actions">{btn("Resend link", "primary", D("verify-email-prompt"))}{btn("Back to the find", href=P("find-detail"))}</div></div>')
    return Page(main=main, nav="finds", user="unverified")


@gp.state("signed-out", "A visitor is sent to sign in and comes straight back to this page.")
def _():
    pg = REGISTRY["sign-in"].states["default"][1]()
    pg.notes = note("URL: /sign-in?returnUrl=%2Ffinds%2FF-1042%2Fgrab. Only a same-site path is followed (L2-109).", "top: 6rem; right: 1rem;")
    return pg


@gp.state("loading", "Pricing the grab: skeletons in the final two-column layout.")
def _():
    left = ('<div class="stack stack--lg"><span class="skeleton skeleton--title" style="width: 45%;"></span>'
            '<span class="skeleton skeleton--rect" style="height: 6rem;"></span><span class="skeleton skeleton--rect" style="height: 6rem;"></span></div>')
    right = ('<div class="panel stack"><span class="skeleton skeleton--text" style="width: 70%;"></span><span class="skeleton skeleton--text"></span>'
             '<span class="skeleton skeleton--text"></span><span class="skeleton skeleton--text" style="width: 50%;"></span></div>')
    return Page(main=f'<h1 class="visually-hidden">Loading grab</h1><div class="split" aria-busy="true" style="margin-top: var(--space-10);">{left}{right}</div>',
                nav="finds", footer=False)


@gp.state("error", "The quote failed: a reference, Try again and the way back.")
def _():
    main = (f'<div class="center-state"><h1>Couldn’t price your grab</h1><p class="text-muted">Something went wrong on our side. Try again in a moment.</p>'
            f'<span class="ref">Ref 5f02-9b1e</span><div class="actions">{btn("Try again", "primary", icon="refresh")}{btn("Back to the find", href=P("find-detail"))}</div></div>')
    return Page(main=main, nav="finds")


# ---------------------------------------------------------------------------
# The shopper's grabs (L2-119)
# ---------------------------------------------------------------------------
STATUS_WORDS = {
    "Requested": "Waiting for TuTuMuch to accept (by {deadline})",
    "Accepted": "TuTuMuch is going to the store (by {deadline})",
    "Bought": "TuTuMuch has it and is sending it to you",
    "Received": "You’ve got it",
    "Declined": "TuTuMuch couldn’t take this grab. You’ve been refunded {total}.",
    "Couldn’t get it": "TuTuMuch couldn’t get it (Price went up). You’ve been refunded {total}.",
    "Expired": "This grab ran out of time. You’ve been refunded {total}.",
    "Cancelled": "You cancelled. You’ve been refunded {refund}.",
}


def words(st: str, g: dict) -> str:
    a = grab_amounts(g["find"])
    return STATUS_WORDS[st].format(deadline=g.get("deadline", ""), total=money(g["total"]), refund=money(a["refund"]))


def my_grab_row(gid: str, st: str | None = None) -> str:
    g = GRABS[gid]
    f = find(g["find"])
    st = st or g["status"]
    return (f'<li><a class="item-row" href="{P("grab-detail", GRAB_STATE.get(st, "default"))}" style="padding-block: var(--space-4);">'
            f'<div class="thumb"><div class="photo photo--none">{ic("map-pin")}</div></div>'
            f'<div class="item-row__body"><span class="item-row__title">{esc(f["title"])}</span>'
            f'<span class="item-row__meta">{gid} · {esc(f["store"])} · {money(g["total"])}</span>'
            f'<span class="cluster" style="gap: var(--space-2);">{status(st)}<span class="text-sm text-muted">{words(st, g)}</span></span></div></a></li>')


GRAB_STATE = {"Requested": "default", "Accepted": "accepted", "Bought": "bought", "Received": "received", "Declined": "declined",
              "Couldn’t get it": "unavailable", "Expired": "expired", "Cancelled": "cancelled"}


def grabs_page(body: str, sub: str = "") -> Page:
    s = f'<p class="subtitle">{sub}</p>' if sub else ""
    h = f'<div class="page-header"><div><h1>Your grabs</h1>{s}</div></div>'
    return Page(main=h + body, account="grabs")


mg = screen("my-grabs", "page", "Your grabs", route="/account/grabs", reqs=["L2-119", "L2-121", "L2-061", "L2-065"])


@mg.state("default", "Tessa’s grabs, newest first, each with its status in words.")
def _():
    rows = my_grab_row("GR-1007") + my_grab_row("GR-1004")
    return grabs_page(f'<ul class="order-list" role="list">{rows}</ul>', "2 grabs")


@mg.state("loading", "Skeleton rows.")
def _():
    row = ('<li><div class="item-row" style="padding-block: var(--space-4);" aria-hidden="true"><span class="skeleton skeleton--rect thumb" style="height: 3.5rem;"></span>'
           '<div class="item-row__body"><span class="skeleton skeleton--text" style="width: 45%;"></span><span class="skeleton skeleton--text" style="width: 30%;"></span></div></div></li>')
    return grabs_page(f'<ul class="order-list" role="list" aria-busy="true" aria-label="Loading grabs">{row * 3}</ul>')


@mg.state("empty", "No grabs yet: Tilly and “See shelf finds” (L2-119 AC6).")
def _():
    return grabs_page(empty("bag", "No grabs yet", "Ask TuTuMuch to grab a shelf find and it shows up here.", btn("See shelf finds", "primary", P("shelf-finds"))))


@mg.state("error", "The list failed to load: a reference and Try again.")
def _():
    return grabs_page(empty("refresh", "Couldn’t load your grabs", 'Something went wrong on our side. <span class="ref">Ref 2c90-71ad</span>',
                            btn("Try again", "primary", icon="refresh"), cls="empty--error empty--compact"))


gd = screen("grab-detail", "page", "Grab", route="/account/grabs/{id}", reqs=["L2-119", "L2-117", "L2-118", "L2-121"],
            na={"empty": "A grab always has its find and its amounts."})


def grab_detail(st: str, gid: str = GRAB, *, top: str = "", actions: str = "", after: str = "") -> Page:
    g = GRABS[gid]
    f = find(g["find"])
    a = grab_amounts(g["find"])
    rows = [("Shelf price", money(a["price"])), ("Grab fee", money(a["fee"])), ("Sales tax", money(g["tax"]))]
    refund = ""
    if st in ("Declined", "Couldn’t get it", "Expired"):
        refund = f'<dt>Refunded</dt><dd class="num">{money(g["total"])}</dd>'
    elif st == "Cancelled":
        refund = f'<dt>Refunded</dt><dd class="num">{money(a["refund"])}</dd><dt>Deposit kept</dt><dd class="num">{money(a["deposit"])}</dd>'
    kind = "success" if st in ("Bought", "Received") else ("info" if st in ("Requested", "Accepted") else "warning")
    icon = {"Requested": "clock", "Accepted": "map-pin", "Bought": "truck", "Received": "check-circle"}.get(st)
    banner_html = alert(kind, words(st, g), icon=icon, role="status")
    acts = f'<div class="cluster">{actions}</div>' if actions else ""
    left = (f'<div class="stack stack--lg">{top}{banner_html}{acts}'
            f'<section class="stack" aria-labelledby="find-h"><h2 id="find-h">The find</h2>'
            f'<dl class="facts"><dt>Item</dt><dd><a href="{P("find-detail", "gone" if st in ("Bought", "Received", "Couldn’t get it") else "grab-open")}">{esc(f["title"])}</a></dd>'
            f'<dt>Store</dt><dd>{esc(f["store"])}<br><span class="text-sm text-muted">{esc(f["address"])}</span></dd>'
            f'<dt>Status</dt><dd>{status(st)}</dd>{refund}</dl></section>'
            f'<section class="stack" aria-labelledby="ship-h"><h2 id="ship-h">Sending it to</h2>{address_block(SAVED_ADDRESS)}</section></div>')
    side = (f'<aside class="panel stack stack--lg" aria-labelledby="sum-title"><h2 id="sum-title" class="panel__title">Summary</h2>'
            f'{totals(rows, ("Total paid", money(g["total"])))}'
            f'<p class="text-sm" style="margin: 0;">Non-refundable deposit: {money(a["deposit"])}</p></aside>')
    h = (f'<div class="page-header"><div class="stack stack--sm"><nav aria-label="Breadcrumb"><ol class="breadcrumb"><li><a href="{P("my-grabs")}">Your grabs</a></li>'
         f'<li aria-current="page">{gid}</li></ol></nav><h1>Grab {gid}</h1><p class="subtitle">Paid {g["paid"]}</p></div></div>')
    return Page(main=f'{h}<div class="split">{left}{side}</div>', account="grabs", after=after)


@gd.state("default", "Requested: waiting for TuTuMuch to accept by the 48-hour deadline; the shopper may cancel with a full refund.")
def _():
    return grab_detail("Requested", actions=btn("Cancel grab", "", D("cancel-grab", "requested")))


@gd.state("accepted", "Accepted: TuTuMuch is going to the store; cancelling now keeps the deposit.")
def _():
    g = GRABS[GRAB]
    saved = g["deadline"]
    g["deadline"] = "Wed 28 Oct, 11:02"
    try:
        return grab_detail("Accepted", actions=btn("Cancel grab", "", D("cancel-grab")))
    finally:
        g["deadline"] = saved


@gd.state("bought", "Bought: TuTuMuch has it and is sending it; “I’ve got it” when it arrives. No cancel.")
def _():
    return grab_detail("Bought", actions=btn("I’ve got it", "primary", D("grab-received")))


@gd.state("received", "Received: the shopper said they got it, or 14 days passed after it was bought.")
def _():
    return grab_detail("Received", after=toast_region(toast("success", "Enjoy it!", "Thanks for using Grab it for me.")))


@gd.state("declined", "Declined by TuTuMuch: the total refunded in full.")
def _():
    return grab_detail("Declined")


@gd.state("unavailable", "Couldn’t get it (Price went up): the total refunded in full and the find is gone.")
def _():
    return grab_detail("Couldn’t get it")


@gd.state("expired", "Ran out of time (not accepted in 48 hours, or not bought in 3 days): the total refunded in full.")
def _():
    return grab_detail("Expired")


@gd.state("cancelled", "Cancelled after acceptance: the total less the deposit refunded; TuTuMuch keeps the deposit.")
def _():
    return grab_detail("Cancelled")


@gd.state("loading", "Skeleton in the two-column layout.")
def _():
    left = ('<div class="stack stack--lg"><span class="skeleton skeleton--rect" style="height: 4rem;"></span>'
            '<span class="skeleton skeleton--rect" style="height: 8rem;"></span><span class="skeleton skeleton--rect" style="height: 5rem;"></span></div>')
    right = '<div class="panel stack"><span class="skeleton skeleton--text"></span><span class="skeleton skeleton--text"></span><span class="skeleton skeleton--text" style="width: 50%;"></span></div>'
    return Page(main=f'<h1 class="visually-hidden">Loading grab</h1><div class="split" aria-busy="true" style="margin-top: var(--space-10);">{left}{right}</div>', account="grabs")


@gd.state("error", "The grab failed to load.")
def _():
    main = (f'<div class="center-state"><h1>Couldn’t load this grab</h1><p class="text-muted">Something went wrong on our side.</p>'
            f'<span class="ref">Ref 9d14-3e60</span><div class="actions">{btn("Try again", "primary", icon="refresh")}{btn("Your grabs", href=P("my-grabs"))}</div></div>')
    return Page(main=main, account="grabs")


@gd.state("not-found", "Another member’s grab, or an unknown one: 404 GRAB_NOT_FOUND (L2-119 AC7).")
def _():
    main = ('<div class="center-state"><span class="code" aria-hidden="true">4<em>0</em>4</span><h1>We couldn’t find that grab</h1>'
            f'<p class="text-muted">It may belong to another account.</p><div class="actions">{btn("Your grabs", "primary", P("my-grabs"))}</div></div>')
    return Page(main=main, account="grabs")


# ---------------------------------------------------------------------------
# Administrator's grabs (L2-118)
# ---------------------------------------------------------------------------
ADMIN_ORDER = [("GR-1007", "Requested"), ("GR-1006", "Accepted"), ("GR-1004", "Bought"), ("GR-1002", "Declined")]


def admin_grab(gid: str, st: str) -> str:
    g = GRABS[gid]
    f = find(g["find"])
    who = g["shopper"]
    p = PEOPLE[who]
    email = p.get("email", f'{p["name"].split()[0].lower()}.{p["name"].split()[1].lower()}@example.com')
    if st == "Requested":
        when = f'Answer by {g["deadline"]}'
        acts = btn("Accept", "primary sm") + btn("Decline", "sm", D("decline-grab"))
    elif st == "Accepted":
        when = f'Buy by {g["deadline"]}'
        acts = btn("Bought it", "sm") + btn("Couldn’t get it", "ghost sm", D("grab-unavailable"))
    else:
        when = f'Paid {g["paid"]}'
        acts = ""
    addr = ""
    if st in ("Accepted", "Bought"):
        lines = ["Pia Reyes", "1450 Larimer St, Apt 6B", "Denver, CO 80202"] if who == "PuddleJumperPia" else SAVED_ADDRESS
        addr = f'<div class="stack stack--sm"><span class="text-caption">Ship to</span>{address_block(lines)}</div>'
    return (f'<li class="request"><div class="request__head"><div class="stack stack--sm">'
            f'<h2 class="request__title"><a href="{P("find-detail", "grab-open")}">{esc(f["title"])}</a></h2>'
            f'<span class="text-sm text-muted">{gid} · {esc(f["store"])} · {f["id"]}</span>'
            f'<span class="cluster" style="gap: var(--space-2);">{status(st)}<span class="text-sm text-muted">{when}</span></span></div>'
            f'<div class="stack stack--sm request__actions"><span class="text-label">{who}</span><a class="text-sm" href="mailto:{email}">{email}</a>'
            f'<span class="text-sm">Total <span class="num">{money(g["total"])}</span> · deposit <span class="num">{money(g["deposit"])}</span></span></div></div>'
            f'{addr}{f"<div class=\"cluster\">{acts}</div>" if acts else ""}</li>')


ag = screen("admin-grabs", "page", "Grabs", route="/admin/grabs", reqs=["L2-118", "L2-119", "L2-121", "L2-010", "L2-061", "L2-065"])


@ag.state("default", "Open grabs first by their next deadline, then ended ones: reference, find, store, shopper and email, total, deposit, status, deadline; the address for Accepted and Bought.")
def _():
    body = f'<ul class="request-list" role="list">{"".join(admin_grab(g, s) for g, s in ADMIN_ORDER)}</ul>'
    return admin_page(head("Grabs", "2 open · 2 ended") + body,
                      note("Only administrators see this page and the shopper emails and addresses on it (L2-121). Accept and Bought it act at once with a toast; "
                           "Decline and Couldn’t get it ask first because they refund.", "top: 10rem; right: 1rem;"))


@ag.state("accepted", "After Accept: the grab moves to Accepted with its buy-by deadline and the toast “Grab accepted”.")
def _():
    rows = [("GR-1007", "Accepted"), ("GR-1006", "Accepted"), ("GR-1004", "Bought"), ("GR-1002", "Declined")]
    g = GRABS["GR-1007"]
    saved = g["deadline"]
    g["deadline"] = "Wed 28 Oct, 11:02"
    try:
        body = f'<ul class="request-list" role="list">{"".join(admin_grab(x, s) for x, s in rows)}</ul>'
    finally:
        g["deadline"] = saved
    return Page(main=head("Grabs", "2 open · 2 ended") + body, shell="admin", footer=False,
                after=toast_region(toast("success", "Grab accepted", "We emailed TutuTessa. Buy it by Wed 28 Oct.")))


@ag.state("loading", "Skeleton blocks shaped like the grabs.")
def _():
    one = ('<li class="request"><div class="request__head"><div class="stack stack--sm" style="flex: 1;">'
           '<span class="skeleton skeleton--title" style="width: 40%;"></span><span class="skeleton skeleton--text" style="width: 30%;"></span></div>'
           '<div class="stack stack--sm" style="width: 12rem;"><span class="skeleton skeleton--text"></span><span class="skeleton skeleton--text"></span></div></div></li>')
    return admin_page(head("Grabs") + f'<ul class="request-list" role="list" aria-busy="true" aria-label="Loading grabs">{one * 3}</ul>')


@ag.state("empty", "No grabs yet (L2-118 AC7).")
def _():
    return admin_page(head("Grabs") + empty("bag", "No grabs yet", "When a shopper asks TuTuMuch to grab a find, it shows up here."))


@ag.state("error", "The list failed to load.")
def _():
    return admin_page(head("Grabs") + empty("refresh", "Couldn’t load grabs", 'Something went wrong on our side. <span class="ref">Ref 3b7e-0a54</span>',
                                            btn("Try again", "primary", icon="refresh"), cls="empty--error"))


@ag.state("forbidden", "A member without the Administrator role: 403 ADMINISTRATOR_REQUIRED and “You can’t open this” (L2-118 AC6).")
def _():
    main = (f'<div class="center-state"><span class="code" aria-hidden="true">4<em>0</em>3</span><h1>You can’t open this</h1>'
            f'<p class="text-muted">Only TuTuMuch’s administrators can see grabs.</p><div class="actions">{btn("Shop the closet", "primary", P("catalog"))}</div></div>')
    return admin_page(main)


# ---------------------------------------------------------------------------
# Dialogs
# ---------------------------------------------------------------------------
mfg = screen("mark-find-gone", "dialog", "Mark as gone", opens_from="find-detail", reqs=["L2-116"],
             na={"invalid": "Confirming has no inputs to validate."})


def gone_dialog(primary: str, top: str = "") -> Dialog:
    html = dialog("Mark as gone?", "", btn("Keep it up", attrs="autofocus") + primary,
                  desc="Gone from the shelf? Shoppers will see it’s gone.", icon="map-pin", size="sm", top=top)
    return Dialog(bg=("find-detail", "own"), html=html)


@mfg.state("default", "Confirm first; the safe action has focus.")
def _():
    return gone_dialog(btn("Mark as gone", "primary"))


@mfg.state("busy", "Marking: the button is busy and Keep it up is disabled.")
def _():
    d = gone_dialog(btn("Mark as gone", "primary", attrs='aria-busy="true"'))
    d.html = d.html.replace("autofocus>Keep it up", "disabled>Keep it up")
    return d


@mfg.state("in-progress", "A grab is open on the find: 409 GRAB_IN_PROGRESS (L2-116 AC3).")
def _():
    return gone_dialog(btn("Mark as gone", "primary", attrs="disabled"),
                       top=alert("warning", "TuTuMuch is grabbing this one for a shopper", "You can mark it gone once the grab ends.", icon="clock"))


@mfg.state("failed", "Marking failed: the find is unchanged; try again.")
def _():
    return gone_dialog(btn("Try again", "primary"), top=alert("danger", "Couldn’t mark it as gone", "Your find is still live."))


wf = screen("withdraw-find", "dialog", "Withdraw find", opens_from="find-detail", reqs=["L2-116"],
            na={"invalid": "Confirming has no inputs to validate."})


def withdraw_dialog(primary: str, top: str = "") -> Dialog:
    html = dialog("Withdraw this find?", "", btn("Keep find", attrs="autofocus") + primary,
                  desc="It disappears from Shelf finds.", icon="trash", icon_kind="danger", size="sm", top=top)
    return Dialog(bg=("find-detail", "own"), html=html)


@wf.state("default", "Confirm first; the safe action has focus.")
def _():
    return withdraw_dialog(btn("Withdraw", "danger"))


@wf.state("busy", "Withdrawing: the danger button is busy.")
def _():
    d = withdraw_dialog(btn("Withdraw", "danger", attrs='aria-busy="true"'))
    d.html = d.html.replace("autofocus>Keep find", "disabled>Keep find")
    return d


@wf.state("in-progress", "A grab is open on the find: 409 GRAB_IN_PROGRESS.")
def _():
    return withdraw_dialog(btn("Withdraw", "danger", attrs="disabled"),
                           top=alert("warning", "TuTuMuch is grabbing this one for a shopper", "You can withdraw it once the grab ends.", icon="clock"))


@wf.state("failed", "Withdrawing failed: the find stays live.")
def _():
    return withdraw_dialog(btn("Try again", "danger"), top=alert("danger", "Couldn’t withdraw", "Your find is still live."))


cg = screen("cancel-grab", "dialog", "Cancel grab", opens_from="grab-detail", reqs=["L2-119"],
            na={"invalid": "Confirming has no inputs to validate."})


def cancel_dialog(accepted: bool, primary: str, top: str = "") -> Dialog:
    a = grab_amounts(MINE)
    g = GRABS[GRAB]
    desc = (f"You get {money(a['refund'])} back. TuTuMuch keeps the {money(a['deposit'])} deposit." if accepted
            else f"TuTuMuch hasn’t accepted yet, so you get all {money(g['total'])} back.")
    html = dialog("Cancel this grab?", "", btn("Keep grab", attrs="autofocus") + primary, desc=desc,
                  icon="alert", icon_kind="danger" if accepted else "info", size="sm", top=top)
    return Dialog(bg=("grab-detail", "accepted" if accepted else "default"), html=html)


@cg.state("default", "After acceptance: the refund is the total less the deposit, stated before confirming.")
def _():
    return cancel_dialog(True, btn("Cancel grab", "danger"))


@cg.state("requested", "Before acceptance: a full refund.")
def _():
    return cancel_dialog(False, btn("Cancel grab", "danger"))


@cg.state("busy", "Cancelling: the danger button is busy.")
def _():
    d = cancel_dialog(True, btn("Cancel grab", "danger", attrs='aria-busy="true"'))
    d.html = d.html.replace("autofocus>Keep grab", "disabled>Keep grab")
    return d


@cg.state("bought", "TuTuMuch bought it in the meantime: 409 GRAB_STATE_CHANGED; nothing is refunded.")
def _():
    return cancel_dialog(True, btn("Cancel grab", "danger", attrs="disabled"),
                         top=alert("warning", "TuTuMuch already bought it", "It’s on its way to you, so it can’t be cancelled now."))


@cg.state("failed", "Cancelling failed: the grab is unchanged.")
def _():
    return cancel_dialog(True, btn("Try again", "danger"), top=alert("danger", "That didn’t go through", "Your grab is unchanged."))


gr = screen("grab-received", "dialog", "I’ve got it", opens_from="grab-detail", reqs=["L2-119"],
            na={"invalid": "A single confirmation; no inputs."})


def received_dialog(primary: str, top: str = "") -> Dialog:
    html = dialog("Got it?", "", btn("Not yet", attrs="autofocus") + primary,
                  desc="Tell TuTuMuch it arrived. This closes the grab.", icon="check-circle", size="sm", top=top)
    return Dialog(bg=("grab-detail", "bought"), html=html)


@gr.state("default", "Confirm the item arrived.")
def _():
    return received_dialog(btn("Yes, I’ve got it", "primary"))


@gr.state("busy", "Saving: the button is busy.")
def _():
    d = received_dialog(btn("Yes, I’ve got it", "primary", attrs='aria-busy="true"'))
    d.html = d.html.replace("autofocus>Not yet", "disabled>Not yet")
    return d


@gr.state("failed", "Saving failed: try again.")
def _():
    return received_dialog(btn("Try again", "primary"), top=alert("danger", "That didn’t go through", "Your grab is unchanged."))


dg = screen("decline-grab", "dialog", "Decline grab", opens_from="admin-grabs", reqs=["L2-118"],
            na={"invalid": "Confirming has no inputs to validate."})


def decline_dialog(primary: str, top: str = "") -> Dialog:
    g = GRABS[GRAB]
    html = dialog(f"Decline grab {GRAB}?", "", btn("Keep grab", attrs="autofocus") + primary,
                  desc=f"TutuTessa is refunded {money(g['total'])} in full and emailed.", icon="alert", icon_kind="danger", size="sm", top=top)
    return Dialog(bg=("admin-grabs", "default"), html=html)


@dg.state("default", "Confirm first: the refund is stated.")
def _():
    return decline_dialog(btn("Decline and refund", "danger"))


@dg.state("busy", "Declining: the danger button is busy.")
def _():
    d = decline_dialog(btn("Decline and refund", "danger", attrs='aria-busy="true"'))
    d.html = d.html.replace("autofocus>Keep grab", "disabled>Keep grab")
    return d


@dg.state("changed", "The grab moved meanwhile (the shopper cancelled or the deadline passed): 409 GRAB_STATE_CHANGED.")
def _():
    return decline_dialog(btn("Decline and refund", "danger", attrs="disabled"),
                          top=alert("warning", "This grab has changed", "Reload the list to see where it is."))


gu = screen("grab-unavailable", "dialog", "Couldn’t get it", opens_from="admin-grabs", reqs=["L2-118"])


def unavailable_dialog(primary: str, *, chosen: str = "", error: str = "", top: str = "") -> Dialog:
    g = GRABS["GR-1006"]
    opts = [("Gone", "Gone from the shelf"), ("PriceWentUp", "Price went up")]
    inv = ' aria-invalid="true" aria-describedby="why-error"' if error else ""
    radios = "".join(f'<label class="choice"><input type="radio" name="why" value="{v}"{" checked" if v == chosen else ""}{inv}>'
                     f'<span class="choice__text">{t}</span></label>' for v, t in opts)
    e = f'<span class="field__error" id="why-error">{esc(error)}</span>' if error else ""
    body = f'<fieldset class="stack stack--sm"><legend class="field__label">Why?</legend>{radios}{e}</fieldset>'
    html = dialog("Couldn’t get it?", body, btn("Cancel") + primary,
                  desc=f"PuddleJumperPia is refunded {money(g['total'])} in full and the find is marked gone.", icon="map-pin", top=top)
    return Dialog(bg=("admin-grabs", "default"), html=html)


@gu.state("default", "Choose the reason; the refund is stated.")
def _():
    return unavailable_dialog(btn("Refund in full", "danger"), chosen="PriceWentUp")


@gu.state("invalid", "No reason chosen: “Choose a reason”.")
def _():
    return unavailable_dialog(btn("Refund in full", "danger"), error="Choose a reason")


@gu.state("busy", "Refunding: the button is busy.")
def _():
    return unavailable_dialog(btn("Refund in full", "danger", attrs='aria-busy="true"'), chosen="Gone")


@gu.state("failed", "The refund could not be started: nothing changed.")
def _():
    return unavailable_dialog(btn("Try again", "danger"), chosen="Gone", top=alert("danger", "That didn’t go through", "The grab is unchanged."))


# ---------------------------------------------------------------------------
# Toasts
# ---------------------------------------------------------------------------
ft = screen("find-toasts", "notification", "Shelf find toasts", opens_from="shelf-finds", reqs=["L2-113", "L2-116"])


def on_finds(*toasts) -> Note:
    return Note(bg=("shelf-finds", "default"), toasts=toast_region(*toasts))


@ft.state("info", "A find was withdrawn (the poster is back on Shelf finds), or marked as gone.")
def _():
    return on_finds(toast("info", "Find withdrawn"), toast("info", "Marked as gone"))


@ft.state("success", "A find was posted.")
def _():
    return on_finds(toast("success", "Your find is live", "Shoppers can see it on Shelf finds."))


@ft.state("warning", "Posting a 21st find today.")
def _():
    return on_finds(toast("warning", "That’s 20 finds today", "You can post again tomorrow."))


@ft.state("danger", "Withdrawing failed: stays until dismissed, offers Try again.")
def _():
    return on_finds(toast("danger", "Couldn’t withdraw your find", "Check your connection.", "Try again"))


gt = screen("grab-toasts", "notification", "Grab toasts", opens_from="grab-detail", reqs=["L2-117", "L2-118", "L2-119"])


def on_grab(state: str, *toasts) -> Note:
    return Note(bg=("grab-detail", state), toasts=toast_region(*toasts))


@gt.state("info", "The shopper cancelled: the refund amount.")
def _():
    a = grab_amounts(MINE)
    return on_grab("cancelled", toast("info", "Grab cancelled", f"{money(a['refund'])} is on its way back to your card."))


@gt.state("success", "The grab is paid and Requested; the shopper lands here.")
def _():
    return on_grab("default", toast("success", "TuTuMuch has your grab", "We’ll email you when it’s accepted."))


@gt.state("warning", "Someone else got there first while the shopper was paying (409 GRAB_IN_PROGRESS).")
def _():
    return Note(bg=("find-detail", "grab-open"), toasts=toast_region(toast("warning", "Someone already asked TuTuMuch to grab this one")))


@gt.state("danger", "An action failed: stays until dismissed, offers Try again.")
def _():
    return on_grab("default", toast("danger", "That didn’t go through", "Your grab is unchanged.", "Try again"))


# ---------------------------------------------------------------------------
# Emails (L2-120)
# ---------------------------------------------------------------------------
ge = screen("grab-email", "page", "Grab emails", route="(email)", reqs=["L2-120", "L2-042"],
            na={"loading": "An email is static text.", "empty": "An email is only sent for an event.",
                "error": "Sending failures are retried by the outbox (L2-042); the member sees nothing."})


def email_page(to: str, subject: str, body: str) -> Page:
    meta = (f'<dl class="facts"><dt>From</dt><dd>TuTuMuch</dd><dt>To</dt><dd>{esc(to)}</dd><dt>Subject</dt><dd>{esc(subject)}</dd></dl>')
    main = (f'<div class="page-header"><div><p class="eyebrow">Email</p><h1>{esc(subject)}</h1></div></div>'
            f'<div class="panel stack stack--lg">{meta}<p style="white-space: pre-line; margin: 0;">{body}</p></div>')
    return Page(main=main, shell="bare", main_class="container page page--form")


FOOT = "\n\nTuTuMuch, the marketplace for all things girls.\nThis is a transactional email about your grab; it can’t be turned off."


@ge.state("default", "To the poster when a grab is paid: who asked and a link to the admin list.")
def _():
    f = find(MINE)
    subject = f"TutuTessa asked TuTuMuch to grab {f['title']}"
    body = (f"Hi {POSTER_HANDLE},\n\nTutuTessa paid {money(GRABS[GRAB]['total'])} for TuTuMuch to grab {esc(f['title'])} at {esc(f['store'])}.\n"
            f"Accept or decline by {GRABS[GRAB]['deadline']}:\nhttps://tutumuch.com/admin/grabs" + FOOT)
    pg = email_page("ana.ruiz@example.com", subject, body)
    pg.notes = note("Queued in the same transaction as the webhook that confirmed the payment; a repeated webhook queues nothing (L2-120 AC3).", "top: 6rem; right: 1rem;")
    return pg


@ge.state("accepted", "To the shopper when TuTuMuch accepts.")
def _():
    f = find(MINE)
    subject = f"TuTuMuch is grabbing {f['title']}"
    body = (f"Hi TutuTessa,\n\nTuTuMuch is going to the store (by Wed 28 Oct, 11:02).\n"
            f"Your grab: https://tutumuch.com/account/grabs/{GRAB}\n\nIf you cancel now, you get everything back except the {money(GRABS[GRAB]['deposit'])} deposit." + FOOT)
    return email_page(PEOPLE[ME]["email"], subject, body)


@ge.state("refunded", "To the shopper when TuTuMuch couldn’t get it: the amount refunded.")
def _():
    f = find(MINE)
    subject = f"TuTuMuch couldn’t get {f['title']}"
    body = (f"Hi TutuTessa,\n\nTuTuMuch couldn’t get it (Price went up). You’ve been refunded {money(GRABS[GRAB]['total'])}.\n"
            f"Your grab: https://tutumuch.com/account/grabs/{GRAB}" + FOOT)
    return email_page(PEOPLE[ME]["email"], subject, body)


@ge.state("cancelled", "To the shopper after cancelling an Accepted grab: the refund and the deposit kept.")
def _():
    a = grab_amounts(MINE)
    f = find(MINE)
    subject = f"You cancelled your grab of {f['title']}"
    body = (f"Hi TutuTessa,\n\nYou cancelled. You’ve been refunded {money(a['refund'])}. TuTuMuch keeps the {money(a['deposit'])} deposit "
            f"because the grab had been accepted.\nYour grab: https://tutumuch.com/account/grabs/{GRAB}" + FOOT)
    return email_page(PEOPLE[ME]["email"], subject, body)
