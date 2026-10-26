"""Selling: listing form, edit, my listings, payouts (L1-003, L1-004, L1-007 payouts)."""
from mk import *

FEE = FEE_PERCENT / 100

# A new listing Marcus is creating in the sell form (not yet in the catalog).
NEW = {"cat": "Shoes", "sub": "Sneakers", "title": "Air Force 1 '07, white", "brand": "Nike", "cond": "Used - Good",
       "desc": "Light creasing, soles have plenty left. Cleaned and ready to wear.", "price": "60.00", "ship": "8.00"}


def earn(price: float, ship: float) -> str:
    return money(round((price + ship) * (1 - FEE), 2))


# ---------------------------------------------------------------------------
# Listing form pieces
# ---------------------------------------------------------------------------
def uploader(photos, *, rejected: bool = False, error: str | None = None) -> str:
    """photos: list of (item key, tone)."""
    tiles = ""
    for i, (key, tone) in enumerate(photos, 1):
        cover = '<span class="badge uploader__cover">Cover</span>' if i == 1 else ""
        tiles += (f'<li>{photo(key, tag=False, alt=f"Photo {i}", tone=tone)}{cover}'
                  f'<button class="btn btn--icon btn--sm uploader__remove" type="button" aria-label="Remove photo {i}">{ic("close", "icon icon--sm")}</button></li>')
    if rejected:
        tiles += (f'<li><div class="uploader__rejected">{ic("x-circle")}<span>IMG_4471.jpg</span><span>11.4 MB</span></div>'
                  f'<button class="btn btn--icon btn--sm uploader__remove" type="button" aria-label="Remove IMG_4471.jpg">{ic("close", "icon icon--sm")}</button></li>')
    n = len(photos)
    inv = ' aria-invalid="true" aria-describedby="photos-error"' if error else ' aria-describedby="photos-help"'
    add = (f'<li><label class="uploader__add"{" aria-invalid=\"true\"" if error else ""}>{ic("camera", "icon icon--lg")}<span>Add photos</span>'
           f'<span class="text-caption">{n} of 8</span>'
           f'<input class="visually-hidden" id="photos" type="file" accept="image/jpeg,image/png,image/webp" multiple{inv}></label></li>')
    err = f'<span class="field__error" id="photos-error">{esc(error)}</span>' if error else ""
    return (f'<ul class="uploader" role="list">{tiles}{add}</ul>'
            f'<span class="field__help" id="photos-help">JPEG, PNG or WebP up to 10 MB. Drag to reorder; the first is the cover.</span>{err}')


def select(fid: str, options, chosen: str = "", first: str = "", disabled: bool = False) -> str:
    opts = (f'<option value="">{first}</option>' if first else "") + "".join(
        f'<option{" selected" if o == chosen else ""}>{esc(o)}</option>' for o in options)
    return f'<select class="select" id="{fid}" name="{fid}"{{db}}{" disabled" if disabled else ""}>{opts}</select>'


def condition_group(chosen: str = "") -> str:
    opts = "".join(
        f'<label><input type="radio" name="cond" value="{esc(c)}"{" checked" if c == chosen else ""}><span>{esc(c)}</span></label>'
        for c in CONDITIONS)
    return f'<fieldset><legend>Condition</legend><div class="segmented">{opts}</div></fieldset>'


def money_input(fid: str, value: str) -> str:
    v = f' value="{value}"' if value else ""
    return f'<div class="input-group"><span class="addon">$</span><input class="input" id="{fid}" name="{fid}" inputmode="decimal"{v}{{db}}></div>'


def sell_form(v: dict, *, photos=(), errors: dict | None = None, rejected: bool = False, busy: bool = False, mode: str = "new", top: str = "") -> str:
    e = errors or {}
    cat = v.get("cat", "")
    sizes = {c: s for c, _, s in CATEGORIES}
    subs = next((s for c, s, _ in CATEGORIES if c == cat), [])
    size_html = ""
    if cat:
        long = "Men's US 12" if cat == "Shoes" else "Men's L"
        size_html = (f'<div class="fixed-size" id="size">{size_tag(sizes[cat], "size-tag--lg")}<span>Size: {long}</span>'
                     f'<span class="fixed-size__label">Set by category</span></div>')
    cond = v.get("cond", "")
    flaws = ""
    if cond == "Used - Fair":
        flaws = field("flaws", "Describe the flaws", f'<textarea class="textarea" id="flaws" rows="2"{{db}}>{esc(v.get("flaws", ""))}</textarea>',
                      help="Shown on the listing. At least 10 characters.", error=e.get("flaws"))
    p = float(v["price"]) if v.get("price") and not e.get("price") else None
    s = float(v["ship"]) if v.get("ship") else 0.0
    earnings = earn(p, s) if p is not None else "—"
    label_text = {"Shoes": "This item is labelled men's US 12"}.get(cat, "This item is labelled men's L" if cat else "The label shows men's US 12 (shoes) or men's L (clothing)")
    conf_err = ' aria-invalid="true" aria-describedby="confirm-error"' if e.get("confirm") else ""
    confirm = (f'<label class="choice"><input type="checkbox" id="confirm" name="confirm"{" checked" if v.get("confirm") else ""}{conf_err}>'
               f'<span>{label_text}</span></label>'
               + (f'<span class="field__error" id="confirm-error">{esc(e["confirm"])}</span>' if e.get("confirm") else ""))
    tlen = len(v.get("title", ""))
    dlen = len(v.get("desc", ""))
    brands = "".join(f'<option value="{esc(b)}"></option>' for b in BRANDS)
    dis = " disabled" if busy else ""
    if mode == "edit":
        actions = (f'<a class="btn btn--ghost" href="{P("listing-detail", "own")}">Cancel</a>'
                   f'{btn("Save changes", "primary", attrs=("aria-busy=\"true\"" if busy else "type=\"submit\""))}')
    else:
        actions = (f'{btn("Save draft", attrs=dis.strip())}'
                   f'{btn("Publish", "primary", attrs=("aria-busy=\"true\"" if busy else "type=\"submit\""))}')
    return (f'<form class="sell-form" novalidate>{top}'
            f'<section class="form-section" aria-labelledby="s-photos"><h2 id="s-photos">Photos</h2>'
            f'{uploader(photos, rejected=rejected, error=e.get("photos"))}</section>'
            f'<section class="form-section" aria-labelledby="s-item"><h2 id="s-item">Item</h2><div class="form-row">'
            + field("cat", "Category", select("cat", [c for c, _, _ in CATEGORIES], cat, "Choose a category"))
            + field("sub", "Type", select("sub", subs, v.get("sub", ""), "Choose a type", disabled=not cat))
            + f'</div>{size_html}'
            + field("title", "Title", value=v.get("title", ""), error=e.get("title"), counter=f"{tlen} / 80")
            + field("brand", "Brand", f'<input class="input" id="brand" name="brand" list="brands" value="{esc(v.get("brand", ""))}"{{db}}><datalist id="brands">{brands}</datalist>')
            + f'{condition_group(cond)}{flaws}'
            + field("desc", "Description", f'<textarea class="textarea" id="desc" rows="4"{{db}}>{esc(v.get("desc", ""))}</textarea>',
                    error=e.get("desc"), counter=f"{dlen} / 2,000")
            + '</section>'
            f'<section class="form-section" aria-labelledby="s-price"><h2 id="s-price">Price</h2><div class="form-row">'
            + field("price", "Price", money_input("price", v.get("price", "")), error=e.get("price"))
            + field("ship", "Shipping", money_input("ship", v.get("ship", "")), help="Flat rate. $0 shows as free shipping.")
            + f'</div><p class="earnings"><span>You earn after the {FEE_PERCENT}% fee</span><strong>{earnings}</strong></p></section>'
            f'<section class="form-section" aria-labelledby="s-confirm"><h2 id="s-confirm" class="visually-hidden">Size confirmation</h2>{confirm}</section>'
            f'<div class="form-actions form-actions--sticky form-actions--split">{actions}</div></form>')


def sell_page(form: str, title: str = "Sell an item") -> Page:
    head = f'<div class="page-header"><h1>{title}</h1></div>'
    return Page(main=head + form, nav="sell", main_class="container page page--form", footer=False)


def blocked(icon: str, title: str, text: str, actions: str, code: str | None = None) -> Page:
    c = f'<span class="code" aria-hidden="true">{code}</span>' if code else f'<div class="empty__icon">{ic(icon, "icon icon--lg")}</div>'
    main = f'<div class="center-state">{c}<h1>{title}</h1><p class="text-muted">{text}</p><div class="actions">{actions}</div></div>'
    return Page(main=main, nav="sell")


FILLED_PHOTOS = [("air-max-90", 6), ("air-max-90", 1), ("air-max-90", 4)]

# ---------------------------------------------------------------------------
# Sell
# ---------------------------------------------------------------------------
s = screen("sell", "page", "Sell an item", route="/sell",
           reqs=["L2-003", "L2-011", "L2-012", "L2-014", "L2-015", "L2-016", "L2-017", "L2-018", "L2-019", "L2-031", "L2-051", "L2-063"],
           na={"loading": "A new listing form needs no data; categories ship with the app shell.",
               "empty": "The form is the page; an empty form is the default state."})


@s.state("default", "A blank listing form: no size choice until a category is picked.")
def _():
    pg = sell_page(sell_form({}))
    pg.notes = note("No size input exists. Picking a category shows its fixed size (L2-012).", "top: 22rem; right: 1rem;")
    return pg


@s.state("filled", "Shoes chosen: size fixed at men's US 12, three photos with a cover, earnings after the fee.")
def _():
    return sell_page(sell_form({**NEW, "confirm": True}, photos=FILLED_PHOTOS))


@s.state("invalid", "Publish refused: summary at the top, each problem inline, focus moved to the summary.")
def _():
    errs = {"photos": "Add at least one photo. Photos must be 10 MB or smaller.",
            "title": "Title must be 5 to 80 characters", "price": "Enter a price from $1.00 to $5,000.00",
            "flaws": "Describe the item's flaws", "confirm": "Confirm the label shows men's US 12"}
    top = error_summary([("photos", "Add at least one photo"), ("title", "Title must be 5 to 80 characters"),
                         ("flaws", "Describe the item's flaws"), ("price", "Enter a price from $1.00 to $5,000.00"),
                         ("confirm", "Confirm the label shows men's US 12")])
    v = {**NEW, "title": "AF1", "cond": "Used - Fair", "price": "0.99", "confirm": False}
    pg = sell_page(sell_form(v, rejected=True, errors=errs, top=top))
    pg.notes = note("Errors are announced in a live region; focus moves to the summary (L2-065).", "top: 9rem; right: 1rem;")
    return pg


@s.state("submitting", "Publishing: the button is busy, Save draft is disabled, nothing shifts.")
def _():
    return sell_page(sell_form({**NEW, "confirm": True}, photos=FILLED_PHOTOS, busy=True))


@s.state("draft-saved", "Draft saved: kept out of the catalog, finish it later from Your listings.")
def _():
    top = alert("success", "Draft saved", f'Finish it any time from <a href="{P("my-listings")}">your listings</a>.', role="status")
    return sell_page(sell_form({**NEW}, photos=FILLED_PHOTOS, top=top))


@s.state("success", "Published: the item is live, with a nudge to set up payouts.")
def _():
    main = (f'<div class="center-state"><div class="empty__icon">{ic("check", "icon icon--lg")}</div><h1>It’s live</h1>'
            f'<div class="panel" style="width: 100%;">{item_row("air-max-90", "Nike · Men’s US 12 · $60.00").replace("Air Max 90, white", NEW["title"])}</div>'
            f'<p class="text-muted"><a href="{P("payouts", "not-onboarded")}">Set up payouts</a> so you’re paid when it sells.</p>'
            f'<div class="actions">{btn("View listing", "primary", P("listing-detail", "own"))}{btn("Sell another", href=P("sell"))}</div></div>')
    return Page(main=main, nav="sell")


@s.state("unverified", "Unverified member: listing is blocked until the email is verified (HTTP 403).")
def _():
    return blocked("mail", "Verify your email to sell", "We sent a link to marcus.bell@example.com.",
                   btn("Resend link", "primary", D("verify-email-prompt")) + btn("Back to shop", href=P("catalog")))


@s.state("limited", "Daily limit reached: 20 new listings per day (HTTP 429).")
def _():
    return blocked("clock", "That’s 20 listings today", "You can list more tomorrow. Drafts still save.",
                   btn("Your listings", "primary", P("my-listings")))


@s.state("error", "Categories failed to load: the form can’t start, retry with a reference.")
def _():
    return blocked("refresh", "Couldn’t open the form", 'Something went wrong on our side. <span class="ref">Ref 5c1d-0e7a</span>',
                   btn("Try again", "primary", icon="refresh") + btn("Back to shop", href=P("catalog")))


# ---------------------------------------------------------------------------
# Edit listing
# ---------------------------------------------------------------------------
ARIZONA = {"cat": "Shoes", "sub": "Sandals", "title": "Arizona sandals", "brand": "Birkenstock", "cond": "Used - Fair",
           "flaws": ITEMS["arizona"]["flaws"], "desc": "Comfortable and broken in. Regular width.", "price": "45.00", "ship": "8.00", "confirm": True}
ARIZONA_PHOTOS = [("arizona", 5), ("arizona", 1), ("arizona", 3)]

e = screen("edit-listing", "page", "Edit listing", route="/items/{id}/edit", reqs=["L2-015", "L2-016", "L2-017", "L2-018", "L2-020", "L2-013"],
           na={"empty": "Only existing listings can be edited; a missing one is the listing's 'unavailable' page."})


def edit_page(form: str) -> Page:
    head = (f'<div class="page-header"><div class="stack stack--sm"><a class="text-sm text-muted" href="{P("listing-detail", "own")}">Arizona sandals</a>'
            f'<h1>Edit listing</h1></div>{status("Active")}</div>')
    return Page(main=head + form, main_class="container page page--form", footer=False)


@e.state("default", "Editing an active Used - Fair listing: every field prefilled, flaws visible.")
def _():
    return edit_page(sell_form(ARIZONA, photos=ARIZONA_PHOTOS, mode="edit"))


@e.state("loading", "Loading the listing into the form: skeleton fields, no layout shift.")
def _():
    sk = ('<div class="stack stack--lg" aria-busy="true" aria-label="Loading listing"><div class="uploader">'
          + '<span class="skeleton skeleton--rect" style="aspect-ratio: 1;"></span>' * 3 + '</div>'
          + ('<div class="stack stack--sm"><span class="skeleton skeleton--text" style="width: 20%;"></span>'
             '<span class="skeleton skeleton--rect" style="height: var(--control-height-md);"></span></div>') * 4 + '</div>')
    return Page(main='<div class="page-header"><h1>Edit listing</h1></div>' + sk, main_class="container page page--form", footer=False)


@e.state("error", "The listing failed to load for editing.")
def _():
    main = (f'<div class="center-state"><h1>Couldn’t load this listing</h1><p class="text-muted">Your listing is safe. Try again in a moment.</p>'
            f'<span class="ref">Ref 9d02-77b1</span><div class="actions">{btn("Try again", "primary", icon="refresh")}{btn("Your listings", href=P("my-listings"))}</div></div>')
    return Page(main=main)


@e.state("invalid", "Save refused: flaws emptied and price out of range.")
def _():
    errs = {"flaws": "Describe the item's flaws", "price": "Enter a price from $1.00 to $5,000.00"}
    top = error_summary([("flaws", "Describe the item's flaws"), ("price", "Enter a price from $1.00 to $5,000.00")])
    return edit_page(sell_form({**ARIZONA, "flaws": "", "price": "5200"}, photos=ARIZONA_PHOTOS, errors=errs, mode="edit", top=top))


@e.state("submitting", "Saving changes: button busy.")
def _():
    return edit_page(sell_form(ARIZONA, photos=ARIZONA_PHOTOS, mode="edit", busy=True))


@e.state("success", "Saved: the new price shows in the catalog and on the listing.")
def _():
    top = alert("success", "Changes saved", f'<a href="{P("listing-detail", "own")}">View listing</a>', role="status")
    return edit_page(sell_form({**ARIZONA, "price": "40.00"}, photos=ARIZONA_PHOTOS, mode="edit", top=top))


@e.state("locked", "Reserved listing: editing is blocked while a buyer is in checkout.")
def _():
    main = (f'<div class="center-state"><div class="empty__icon">{ic("clock", "icon icon--lg")}</div><h1>This item is in someone’s checkout</h1>'
            f'<p class="text-muted">You can edit it again if they don’t finish within 10 minutes.</p>'
            f'<div class="actions">{btn("Back to listing", "primary", P("listing-detail", "own"))}</div></div>')
    return Page(main=main)


@e.state("changed", "Save refused because the listing changed since the form opened (412): reload, nothing overwritten.")
def _():
    top = alert("warning", "This listing changed since you opened it", "Reload to see the latest version, then make your changes again.",
                actions=btn("Reload", "sm", icon="refresh"))
    return edit_page(sell_form({**ARIZONA, "price": "40.00"}, photos=ARIZONA_PHOTOS, mode="edit", top=top))


@e.state("sold", "Sold listing: it can no longer be edited; the seller goes to their sales.")
def _():
    main = (f'<div class="center-state"><div class="empty__icon">{ic("check", "icon icon--lg")}</div><h1>This item has sold</h1>'
            f'<p class="text-muted">Sold listings can’t be edited.</p>'
            f'<div class="actions">{btn("Your sales", "primary", P("sales"))}{btn("Your listings", href=P("my-listings"))}</div></div>')
    return Page(main=main)


@e.state("withdrawn", "Withdrawn listing: it is gone for good and can no longer be edited.")
def _():
    main = (f'<div class="center-state"><div class="empty__icon">{ic("trash", "icon icon--lg")}</div><h1>This listing was withdrawn</h1>'
            f'<p class="text-muted">Withdrawn listings can’t be edited or relisted.</p>'
            f'<div class="actions">{btn("Your listings", "primary", P("my-listings"))}</div></div>')
    return Page(main=main)


# ---------------------------------------------------------------------------
# My listings
# ---------------------------------------------------------------------------
ROWS = [  # newest first: (title, photo key or None, price, status, date)
    ("Air Force 1 '07, white", "air-max-90", "$60", "Draft", "Today"),
    ("Untitled draft · Tops", None, "—", "Draft", "Yesterday"),
    ("Reverse Weave hoodie, grey", "reverse-weave", "$38", "Sold", "Sat 24 Oct"),
    ("Arizona sandals", "arizona", "$45", "Active", "Wed 21 Oct"),
    ("Tech Fleece shorts", "tech-shorts", "$30", "Sold", "Fri 2 Oct"),
]


def listing_row(title, key, pr, st, date) -> str:
    th = photo(key, tag=False, alt="") if key else f'<div class="photo photo--none">{ic("image")}</div>'
    href = {"Active": P("listing-detail", "own"), "Draft": P("sell", "draft-saved"), "Sold": P("sale-detail")}[st]
    return (f'<li><a href="{href}"><div class="item-row"><div class="thumb thumb--lg">{th}</div>'
            f'<div class="item-row__body"><span class="item-row__title">{esc(title)}</span>{status(st)}</div></div>'
            f'<div class="order-list__end"><span class="price">{pr}</span><span class="text-caption">{date}</span></div></a></li>')


def filter_chips(active: str = "All") -> str:
    counts = [("All", 5), ("Draft", 2), ("Active", 1), ("Reserved", 0), ("Sold", 2), ("Removed", 0)]
    return ('<div class="cluster" role="group" aria-label="Filter by status" style="margin-bottom: var(--space-8);">' + "".join(
        f'<button class="chip" type="button" aria-pressed="{"true" if n == active else "false"}">{n} <span class="num">{c}</span></button>'
        for n, c in counts) + "</div>")


def listings_page(body: str, sub: str = "5 listings · 1 active") -> Page:
    head = (f'<div class="page-header"><div><h1>Your listings</h1>' + (f'<p class="subtitle">{sub}</p>' if sub else "")
            + f'</div>{btn("Sell an item", "primary", P("sell"), icon="plus")}</div>')
    return Page(main=head + body, account="listings")


ml = screen("my-listings", "page", "Your listings", route="/account/listings", reqs=["L2-019", "L2-021"])


@ml.state("default", "All of Marcus's listings, newest first, filterable by status.")
def _():
    rows = "".join(listing_row(*r) for r in ROWS)
    return listings_page(filter_chips() + f'<ul class="order-list" role="list">{rows}</ul>')


@ml.state("filtered", "Filtered to Sold: only the two sold listings.")
def _():
    rows = "".join(listing_row(*r) for r in ROWS if r[3] == "Sold")
    return listings_page(filter_chips("Sold") + f'<ul class="order-list" role="list">{rows}</ul>')


@ml.state("loading", "Skeleton rows matching thumbnails, titles and prices.")
def _():
    row = ('<li><div class="item-row" style="padding-block: var(--space-5);"><span class="skeleton skeleton--rect thumb thumb--lg" style="aspect-ratio: 1;"></span>'
           '<div class="item-row__body"><span class="skeleton skeleton--text" style="width: 50%;"></span><span class="skeleton skeleton--text" style="width: 20%;"></span></div></div></li>')
    return listings_page(filter_chips() + f'<ul class="order-list" role="list" aria-busy="true" aria-label="Loading listings">{row * 4}</ul>', sub="")


@ml.state("empty", "No listings yet: one action, sell an item.")
def _():
    return listings_page(empty("tag", "Nothing listed yet", "List a pair of 12s or something in L.", btn("Sell an item", "primary", P("sell"))), sub="")


@ml.state("error", "Listings failed to load: retry with a reference.")
def _():
    return listings_page(empty("refresh", "Couldn’t load your listings", 'Try again in a moment. <span class="ref">Ref 3e88-1a40</span>',
                               btn("Try again", icon="refresh"), cls="empty--error empty--compact"), sub="")


# ---------------------------------------------------------------------------
# Payouts
# ---------------------------------------------------------------------------
pay = screen("payouts", "page", "Payouts", route="/account/payouts", reqs=["L2-031"])

PAYOUT_ROWS = [
    ("TM-48302", "reverse-weave", "Held until the order completes", earn(38, 7), "Pending"),
    ("TM-47511", "tech-shorts", f"Fee {money(3.70)} · Mon 5 Oct", money(33.30), "Paid"),
]


def payout_list(rows) -> str:
    out = "".join(
        f'<li><a href="{P("sale-detail")}">{item_row(k, f"{oid} · {meta}")}<div class="order-list__end"><span class="price">{amt}</span>'
        f'<span class="text-caption">{st}</span></div></a></li>' for oid, k, meta, amt, st in rows)
    return f'<ul class="order-list" role="list">{out}</ul>'


def payouts_page(body: str) -> Page:
    return Page(main=f'<div class="page-header"><h1>Payouts</h1></div>{body}', account="payouts")


@pay.state("default", "Payouts connected: totals, the bank on file, each payout with its fee.")
def _():
    stats = (f'<div class="stats" style="margin-bottom: var(--space-12);"><div class="stat"><span class="stat__label">Paid out</span><span class="stat__value">{money(33.30)}</span></div>'
             f'<div class="stat"><span class="stat__label">Pending</span><span class="stat__value">{earn(38, 7)}</span></div></div>')
    method = (f'<ul class="settings-list" role="list" style="margin-bottom: var(--space-12);"><li><div class="settings-list__body"><span class="settings-list__title">Chase checking •••• 6021</span>'
              f'<span class="settings-list__meta">Managed by our payment provider</span></div>{btn("Change", href="https://payments.example.com/onboarding", icon_after="external")}</li></ul>')
    return payouts_page(stats + method + '<div class="section-head"><h2>History</h2></div>' + payout_list(PAYOUT_ROWS))


@pay.state("not-onboarded", "No payout method yet: earnings are held until setup is done.")
def _():
    a = alert("warning", "Set up payouts to get paid", f"{earn(38, 7)} is waiting for you.",
              btn("Set up payouts", "primary sm", "https://payments.example.com/onboarding", icon_after="external"))
    return payouts_page(a + '<div class="section-head" style="margin-top: var(--space-12);"><h2>Held</h2></div>' + payout_list(PAYOUT_ROWS[:1]))


@pay.state("loading", "Skeleton totals and rows.")
def _():
    sk = ('<div class="stats" style="margin-bottom: var(--space-12);" aria-busy="true">'
          + '<div class="stat"><span class="skeleton skeleton--text" style="width: 5rem;"></span><span class="skeleton skeleton--title" style="width: 7rem;"></span></div>' * 2
          + '</div><span class="skeleton skeleton--rect" style="height: 4rem;"></span>')
    return payouts_page(sk)


@pay.state("empty", "No sales yet: payouts appear after the first one.")
def _():
    return payouts_page(empty("dollar", "No payouts yet", "Your earnings show up here after your first sale.", btn("Sell an item", "primary", P("sell"))))


@pay.state("error", "Payout details failed to load.")
def _():
    return payouts_page(empty("refresh", "Couldn’t load payouts", 'Try again in a moment. <span class="ref">Ref 61fa-c3d9</span>',
                              btn("Try again", icon="refresh"), cls="empty--error empty--compact"))


# ---------------------------------------------------------------------------
# Withdraw listing
# ---------------------------------------------------------------------------
w = screen("withdraw-listing", "dialog", "Withdraw listing", opens_from="listing-detail", reqs=["L2-020"],
           na={"invalid": "Confirming has no inputs to validate."})


def withdraw(footer_primary: str, top: str = "") -> Dialog:
    html = dialog("Withdraw “Arizona sandals”?", "", btn("Keep listing", attrs="autofocus") + footer_primary,
                  desc="It leaves the shop right away. This can’t be undone.", icon="trash", icon_kind="danger", size="sm", top=top)
    return Dialog(bg=("listing-detail", "own"), html=html)


@w.state("default", "Confirm before withdrawing; the safe action has focus.")
def _():
    return withdraw(btn("Withdraw", "danger"))


@w.state("busy", "Withdrawing: danger button busy, Keep listing disabled.")
def _():
    d = withdraw(btn("Withdraw", "danger", attrs='aria-busy="true"'))
    d.html = d.html.replace('autofocus>Keep listing', 'disabled>Keep listing')
    return d


@w.state("failed", "Withdraw failed: the listing is unchanged, retry.")
def _():
    return withdraw(btn("Try again", "danger"), top=alert("danger", "Couldn’t withdraw", "Your listing is still live."))


# ---------------------------------------------------------------------------
# Toasts
# ---------------------------------------------------------------------------
t = screen("toast", "notification", "Toasts", opens_from="my-listings", reqs=["L2-019", "L2-020", "L2-068"])


def on_listings(*toasts, notes: str = "") -> Note:
    return Note(bg=("my-listings", "default"), toasts=toast_region(*toasts), notes=notes)


@t.state("info", "Neutral information: a draft clean-up reminder.")
def _():
    return on_listings(toast("info", "Drafts older than 90 days are deleted", "We’ll email you a week before."))


@t.state("success", "Listing published, with a link to view it.")
def _():
    return on_listings(toast("success", "Listing published", "Air Force 1 '07 is live.", "View listing"),
                       notes=note("Auto-dismisses after 8 s; pauses on hover and focus.", "bottom: 10rem; right: 1rem;"))


@t.state("warning", "Something needs attention: the ship-by deadline is close.")
def _():
    return on_listings(toast("warning", "Ship by Fri 30 Oct", "Reverse Weave hoodie for tall_tom.", "Add tracking"))


@t.state("danger", "A save failed: stays until dismissed, offers retry.")
def _():
    return on_listings(toast("danger", "Couldn’t save your draft", "Check your connection.", "Try again"),
                       notes=note("Errors never auto-dismiss.", "bottom: 10rem; right: 1rem;"))


@t.state("with-action", "Reversible change with Undo.")
def _():
    return on_listings(toast("success", "Price updated to $40", "Arizona sandals", "Undo"))


@t.state("stacked", "Three toasts at once: newest at the bottom, each dismissible.")
def _():
    return on_listings(toast("info", "Draft saved", ""), toast("success", "Listing published", "Air Force 1 '07 is live.", "View listing"),
                       toast("warning", "Ship by Fri 30 Oct", "Reverse Weave hoodie", "Add tracking"))


# ---------------------------------------------------------------------------
# Payout banner
# ---------------------------------------------------------------------------
pb = screen("payout-banner", "notification", "Payout banner", opens_from="my-listings", reqs=["L2-031"])


def on_listings_banner(b: str) -> Note:
    return Note(bg=("my-listings", "default"), top=b)


@pb.state("info", "A payout is on its way.")
def _():
    return on_listings_banner(banner("info", f"{money(33.30)} is on its way to Chase •••• 6021.", f'<a href="{P("payouts")}">Payouts</a>'))


@pb.state("success", "Payout setup finished.")
def _():
    return on_listings_banner(banner("success", "Payouts are set up. You’ll be paid when orders complete."))


@pb.state("warning", "Payout setup unfinished: held earnings, persistent until done.")
def _():
    return on_listings_banner(banner("warning", f"Finish payout setup to receive {earn(38, 7)}.", f'<a href="{P("payouts", "not-onboarded")}">Set up payouts</a>', dismiss=False))


@pb.state("danger", "A payout bounced: update bank details.")
def _():
    return on_listings_banner(banner("danger", "Your last payout was returned by the bank.", f'<a href="{P("payouts")}">Update bank details</a>', dismiss=False))
