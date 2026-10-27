"""Checkout, orders, purchases and sales (L1-007, L1-008, L1-009)."""
from mk import *

# ---------------------------------------------------------------------------
# Shared pieces
# ---------------------------------------------------------------------------
BUY = "pink-tutu"
BUY_TAX = 3.37                       # NY sales tax on item + shipping
BUY_TOTAL = 32 + 6 + BUY_TAX         # $41.37


def page_head(title: str, eyebrow: str = "", sub: str = "", actions: str = "") -> str:
    e = f'<p class="eyebrow">{eyebrow}</p>' if eyebrow else ""
    s_ = f'<p class="subtitle">{sub}</p>' if sub else ""
    a = f'<div class="cluster">{actions}</div>' if actions else ""
    return f'<div class="page-header"><div class="stack stack--sm">{e}<h1>{title}</h1>{s_}</div>{a}</div>'


def address_block(lines) -> str:
    return '<address class="address">' + "<br>".join(esc(l) for l in lines) + "</address>"


def order_summary(key: str, tax: float, total: float, *, heading: str = "Summary", extra: str = "") -> str:
    it = ITEMS[key]
    rows = [("Item", money(it["price"])), ("Shipping", "Free" if it["ship"] == 0 else money(it["ship"])), ("Sales tax", money(tax))]
    return (f'<aside class="panel stack stack--lg" aria-labelledby="sum-title"><h2 id="sum-title" class="panel__title">{heading}</h2>'
            f'{item_row(key, f"{esc(it["brand"])} · {size_long(it)} · {esc(it["cond"])}")}{totals(rows, ("Total", money(total)))}{extra}</aside>')


def skeleton_split(h1: str) -> Page:
    left = ('<div class="stack stack--lg"><span class="skeleton skeleton--title" style="width: 45%;"></span>'
            '<span class="skeleton skeleton--rect" style="height: 6rem;"></span><span class="skeleton skeleton--rect" style="height: 6rem;"></span>'
            '<span class="skeleton skeleton--rect" style="height: 3rem; width: 50%;"></span></div>')
    right = ('<div class="panel stack"><div class="item-row"><span class="skeleton skeleton--rect thumb" style="height: 3.5rem;"></span>'
             '<div class="item-row__body"><span class="skeleton skeleton--text" style="width: 70%;"></span><span class="skeleton skeleton--text" style="width: 40%;"></span></div></div>'
             '<span class="skeleton skeleton--text"></span><span class="skeleton skeleton--text"></span><span class="skeleton skeleton--text" style="width: 50%;"></span></div>')
    return Page(main=f'<h1 class="visually-hidden">{h1}</h1><div class="split" aria-busy="true" style="margin-top: var(--space-10);">{left}{right}</div>')


def error_state(title: str, ref: str, back_label: str, back_href: str, account: str = "") -> Page:
    main = (f'<div class="center-state"><h1>{title}</h1><p class="text-muted">Something went wrong on our side. Try again in a moment.</p>'
            f'<span class="ref">Ref {ref}</span><div class="actions">{btn("Try again", "primary", icon="refresh")}{btn(back_label, href=back_href)}</div></div>')
    return Page(main=main, account=account)


# ---------------------------------------------------------------------------
# Checkout (L2-027 – L2-030)
# ---------------------------------------------------------------------------
def timer(text: str = "9:41", kind: str = "") -> str:
    k = f" timer--{kind}" if kind else ""
    return f'<p class="timer{k}" role="timer" aria-live="off">{ic("clock")}<span>Reserved for you · <span class="num">{text}</span> left</span></p>'


def saved_address(checked: bool = True) -> str:
    return (f'<fieldset class="stack"><legend class="visually-hidden">Shipping address</legend>'
            f'<label class="option"><input type="radio" name="addr"{" checked" if checked else ""}><span class="option__body">{address_block(SAVED_ADDRESS)}</span></label>'
            f'<label class="option"><input type="radio" name="addr"{"" if checked else " checked"}><span class="option__body">New address</span></label></fieldset>')


def address_form(values=None, errors=None) -> str:
    v = values or {}
    e = errors or {}
    states = "".join(f"<option{' selected' if s == v.get('state') else ''}>{s}</option>" for s in ["Select a state", "California", "Colorado", "Georgia", "Illinois", "New York", "Oregon", "Texas"])
    country = v.get("country", "United States")
    countries = "".join(f"<option{' selected' if c == country else ''}>{c}</option>" for c in ["United States", "Canada", "Mexico"])
    inv = lambda k: ' aria-invalid="true" aria-describedby="%s-error"' % k if k in e else ""
    return (field("a-name", "Full name", value=v.get("name", "")) +
            field("a-street", "Street address", value=v.get("street", ""), error=e.get("a-street"), attrs='autocomplete="address-line1"') +
            field("a-apt", "Apartment, suite", optional=True, value=v.get("apt", "")) +
            '<div class="form-row">' + field("a-city", "City", value=v.get("city", ""), error=e.get("a-city")) +
            field("a-state", "State", f'<select class="select" id="a-state"{inv("a-state")}>{states}</select>', error=e.get("a-state")) + "</div>" +
            '<div class="form-row">' + field("a-zip", "ZIP code", value=v.get("zip", ""), error=e.get("a-zip"), attrs='inputmode="numeric" autocomplete="postal-code"') +
            field("a-country", "Country", f'<select class="select" id="a-country"{inv("a-country")}>{countries}</select>', error=e.get("a-country")) + "</div>")


def card_fields() -> str:
    return (f'<div class="stack"><div class="field"><span class="field__label" id="card-label">Card</span>'
            f'<div class="hosted-field" role="group" aria-labelledby="card-label"><span>Card number</span><span>MM / YY · CVC</span></div></div>'
            f'<p class="secure-note">{ic("lock", "icon icon--sm")}Card details go straight to our payment provider. TuTuMuch never sees them.</p></div>')


def checkout(*, address: str | None = None, top: str = "", pay: str | None = None, tmr: str | None = None, pay_note: str = "") -> Page:
    address = address if address is not None else saved_address()
    pay = pay or btn(f"Pay {money(BUY_TOTAL)}", "primary lg block", attrs='type="submit"')
    tmr = tmr if tmr is not None else timer()
    left = (f'<form class="stack stack--lg" novalidate>{top}'
            f'<section class="checkout-step" aria-labelledby="ship-h"><h2 id="ship-h">Ship to</h2>{address}</section>'
            f'<section class="checkout-step" aria-labelledby="pay-h"><h2 id="pay-h">Pay</h2>{card_fields()}</section>'
            f'<div class="stack stack--sm">{pay}{pay_note}<a class="btn btn--ghost btn--block" href="{P("listing-detail")}">Cancel and release item</a></div></form>')
    head = f'<div class="page-header"><div class="stack stack--sm"><h1>Checkout</h1>{tmr}</div></div>'
    return Page(main=f'{head}<div class="split">{left}{order_summary(BUY, BUY_TAX, BUY_TOTAL)}</div>', footer=False)


c = screen("checkout", "page", "Checkout", route="/checkout/{id}", reqs=["L2-027", "L2-028", "L2-029", "L2-030", "L2-063", "L2-065", "L2-108"],
           na={"empty": "Checkout always holds exactly one reserved item."})


@c.state("default", "Checkout with the saved address preselected, card entered in the provider’s hosted fields, total before paying.")
def _():
    pg = checkout()
    pg.notes = note("The item is reserved for 10 minutes from opening checkout. Cancel releases it immediately.", "top: 7rem; right: 1rem;")
    return pg


@c.state("new-address", "Buyer chose a new address: US-only form with numeric ZIP keyboard.")
def _():
    return checkout(address=saved_address(False) + '<div class="stack">' + address_form({"name": "Tessa Bell"}) + "</div>")


@c.state("invalid", "Missing street, city and ZIP are listed at the top and marked on each field; focus moves to the summary.")
def _():
    errs = {"a-street": "Enter a street address", "a-city": "Enter a city", "a-zip": "Enter a 5-digit ZIP code"}
    summary = error_summary(list(errs.items()))
    return checkout(top=summary, address=saved_address(False) + '<div class="stack">' + address_form({"name": "Tessa Bell", "state": "New York"}, errs) + "</div>")


@c.state("non-us", "Address outside the United States is refused with a clear reason.")
def _():
    errs = {"a-country": "TuTuMuch ships within the United States only"}
    top = alert("danger", "TuTuMuch ships within the United States only", "Choose a US address to continue.")
    vals = {"name": "Tessa Bell", "street": "88 Queen St W", "city": "Toronto", "zip": "M5H 2M9", "country": "Canada"}
    return checkout(top=top, address=saved_address(False) + '<div class="stack">' + address_form(vals, errs) + "</div>")


@c.state("submitting", "Payment in flight: Pay is busy and cannot be pressed twice; the charge uses one idempotency key.")
def _():
    pg = checkout(pay=btn(f"Pay {money(BUY_TOTAL)}", "primary lg block", attrs='type="submit" aria-busy="true"'),
                  pay_note='<p class="text-caption" role="status" style="text-align: center;">Confirming with your bank…</p>')
    pg.notes = note("A double-click sends the same idempotency key; the buyer is charged once.", "top: 7rem; right: 1rem;")
    return pg


@c.state("declined", "The card was declined: reason shown, reservation still running, try again.")
def _():
    top = alert("danger", "Your bank declined the card", "Insufficient funds. Try another card — the item is still reserved for you.")
    return checkout(top=top, tmr=timer("6:12"))


@c.state("expired", "The 10-minute reservation ran out: the item is back on sale, nothing was charged.")
def _():
    main = (f'<div class="center-state">{ic("clock", "icon icon--lg")}<h1>Your reservation ended</h1>'
            f'<p class="text-muted">You weren’t charged. The item is back on sale — if it’s still there, you can start again.</p>'
            f'<div class="actions">{btn("Back to the item", "primary", P("listing-detail"))}{btn("Keep shopping", href=P("catalog"))}</div></div>')
    return Page(main=main)


@c.state("conflict", "Another buyer reserved the item first (HTTP 409).")
def _():
    main = (f'<div class="center-state">{ic("lock", "icon icon--lg")}<h1>Someone else is checking out</h1>'
            f'<p class="text-muted">If they don’t finish within 10 minutes, it’s back on sale.</p>'
            f'<div class="actions">{btn("Back to the item", "primary", P("listing-detail", "reserved"))}{btn("Keep shopping", href=P("catalog"))}</div></div>')
    return Page(main=main)


@c.state("showcase", "Checkout opened for a showcase listing: the API refused it (409 LISTING_SHOWCASE); no address or card is asked for (L2-108).")
def _():
    main = (f'<div class="center-state"><span class="photo__stamp photo__stamp--sold" style="position: static;">Sold</span><h1>This one has already found a home</h1>'
            f'<p class="text-muted">We can look for another one like it.</p>'
            f'<div class="actions">{btn("Request this item", "primary", P("listing-detail", "showcase-sold"))}{btn("Keep shopping", href=P("catalog"))}</div></div>')
    return Page(main=main)


@c.state("loading", "Reserving the item and pricing the order: skeletons in the final two-column layout.")
def _():
    pg = skeleton_split("Loading checkout")
    pg.footer = False
    return pg


@c.state("error", "Checkout could not start: retry or go back to the listing.")
def _():
    return error_state("Couldn’t start checkout", "c41d-0e77", "Back to the item", P("listing-detail"))


# ---------------------------------------------------------------------------
# Order confirmation
# ---------------------------------------------------------------------------
oc = screen("order-confirmation", "page", "Order confirmed", route="/orders/{id}/confirmed", reqs=["L2-030", "L2-033"],
            na={"empty": "Only reached after a confirmed payment, which always has an order."})


@oc.state("default", "Payment confirmed: what happens next and where to follow the order.")
def _():
    steps = ('<ol class="next-steps">'
             '<li><b>TwirlyTara ships it</b><span>Within 5 business days. We’ll email the tracking.</span></li>'
             '<li><b>You get it</b><span>Confirm it arrived, or tell us within 3 days if something’s wrong.</span></li>'
             '<li><b>Confirm it arrived</b><span>The order completes and the seller is paid.</span></li></ol>')
    main = (f'<div class="confirm"><div class="confirm__mark" aria-hidden="true">{ic("check")}</div>'
            f'<p class="eyebrow">Order TM-48377</p><h1>It’s yours.</h1>'
            f'<div class="confirm__item">{item_row(BUY, f"Paid {money(BUY_TOTAL)} · {CARD[0]} ending {CARD[1]}")}</div>'
            f'{steps}<div class="cluster">{btn("View order", "primary", P("order-detail", "paid"))}{btn("Keep shopping", href=P("catalog"))}</div></div>')
    return Page(main=main)


@oc.state("loading", "Waiting for the payment provider’s confirmation before showing the order.")
def _():
    main = (f'<div class="center-state" aria-busy="true">{mark()}'
            '<h1>Confirming your payment</h1><p class="text-muted" role="status">This usually takes a few seconds. Don’t close this page.</p></div>')
    return Page(main=main)


@oc.state("error", "The confirmation could not load; the payment is safe and the order is in Purchases.")
def _():
    main = (f'<div class="center-state"><h1>We couldn’t show your order</h1><p class="text-muted">If you were charged, the order is in Purchases.</p>'
            f'<span class="ref">Ref 9a07-c3b1</span><div class="actions">{btn("Try again", "primary", icon="refresh")}{btn("Go to purchases", href=P("purchases"))}</div></div>')
    return Page(main=main)


# ---------------------------------------------------------------------------
# Purchases and sales lists (L2-037)
# ---------------------------------------------------------------------------
def order_li(oid: str, other: str, href: str, status_label: str | None = None) -> str:
    o = ORDERS[oid]
    return (f'<li><a href="{href}">{item_row(o["item"], f"{other} · {o["date"]}")}'
            f'<span class="order-list__end"><span class="price">{money(o["total"])}</span>{status(status_label or o["status"])}</span></a></li>')


def list_skeleton(n: int = 3) -> str:
    row = ('<li><div class="item-row" style="padding-block: var(--space-5);"><span class="skeleton skeleton--rect thumb" style="height: 3.5rem;"></span>'
           '<div class="item-row__body"><span class="skeleton skeleton--text" style="width: 50%;"></span><span class="skeleton skeleton--text" style="width: 30%;"></span></div>'
           '<span class="skeleton skeleton--text" style="width: 4rem;"></span></div></li>')
    return f'<ul class="order-list" aria-busy="true" aria-label="Loading orders">{row * n}</ul>'


pu = screen("purchases", "page", "Purchases", route="/account/purchases", reqs=["L2-037"])


@pu.state("default", "Three purchases, newest first, with seller, total, status and date.")
def _():
    lis = (order_li("TM-48213", "from HandMeDownHana", P("order-detail")) + order_li("TM-48140", "from HandMeDownHana", P("order-detail", "delivered")) +
           order_li("TM-47790", "from RainbowRosa", P("order-detail", "completed")))
    return Page(main=page_head("Purchases", sub="1 on the way · 1 to confirm") + f'<ul class="order-list">{lis}</ul>', account="purchases")


@pu.state("loading", "Skeleton rows in the list layout.")
def _():
    return Page(main=page_head("Purchases") + list_skeleton(), account="purchases")


@pu.state("empty", "No purchases yet: one action, browse the catalog.")
def _():
    return Page(main=page_head("Purchases") + empty("bag", "Nothing bought yet", "Find her next favourite thing in the closet.", btn("Shop the closet", "primary", P("catalog"))), account="purchases")


@pu.state("error", "Purchases failed to load; the account navigation stays.")
def _():
    return Page(main=page_head("Purchases") + empty("refresh", "Couldn’t load purchases", '<span class="ref">Ref 51ce-0a92</span>', btn("Try again", "primary", icon="refresh"), cls="empty--error"), account="purchases")


sa = screen("sales", "page", "Sales", route="/account/sales", reqs=["L2-037", "L2-033"])


@sa.state("default", "Two sales: one to ship by Friday, one completed and paid out.")
def _():
    lis = (order_li("TM-48302", "to PuddleJumperPia · ship by Fri 30 Oct", P("sale-detail"), "Paid") +
           order_li("TM-47511", "to PuddleJumperPia", P("sale-detail", "completed")))
    return Page(main=page_head("Sales", sub="1 to ship") + f'<ul class="order-list">{lis}</ul>', account="sales")


@sa.state("loading", "Skeleton rows in the list layout.")
def _():
    return Page(main=page_head("Sales") + list_skeleton(2), account="sales")


@sa.state("empty", "No sales yet: one action, list an item.")
def _():
    return Page(main=page_head("Sales") + empty("tag", "No sales yet", "Sold items show up here with what to do next.", btn("Sell outgrown gear", "primary", P("sell"))), account="sales")


@sa.state("error", "Sales failed to load; the account navigation stays.")
def _():
    return Page(main=page_head("Sales") + empty("refresh", "Couldn’t load sales", '<span class="ref">Ref 6d10-77fe</span>', btn("Try again", "primary", icon="refresh"), cls="empty--error"), account="sales")


# ---------------------------------------------------------------------------
# Order detail, buyer view (L2-032 – L2-036)
# ---------------------------------------------------------------------------
STEPS = ["Paid", "Shipped", "Delivered", "Completed"]


def timeline(current: str, dates: dict) -> str:
    idx = STEPS.index(current)
    lis = ""
    for i, s_ in enumerate(STEPS):
        attr = " data-done" if i < idx else (' aria-current="step"' if i == idx else "")
        if i == idx and current == "Completed":
            attr = " data-done"
        lis += f'<li{attr}><b>{s_}</b><span>{dates.get(s_, "")}</span></li>'
    return f'<ol class="timeline" aria-label="Order progress">{lis}</ol>'


def tracking(o: dict) -> str:
    return (f'<div class="facts-row"><span class="text-muted text-sm">{o["carrier"]} tracking</span>'
            f'<a href="https://tools.usps.com/go/TrackConfirmAction" class="num">{o["tracking"]}{ic("external", "icon icon--sm")}</a></div>')


def order_page(oid: str, *, head: str, body: str, side_extra: str = "", party: str = "seller", eyebrow: str | None = None, account: str = "purchases") -> Page:
    o = ORDERS[oid]
    other = o["seller"] if party == "seller" else o["buyer"]
    who = (f'<div class="seller" style="padding-top: var(--space-6);">{avatar(other)}<div class="stack" style="gap: var(--space-1);">'
           f'<span class="text-caption">{"Seller" if party == "seller" else "Buyer"}</span><a class="seller__name" href="{P("profile")}">{other}</a></div></div>')
    crumbs = (f'<nav aria-label="Breadcrumb" style="margin-top: var(--space-6);"><ol class="breadcrumb"><li><a href="{P(account)}">{account.title()}</a></li>'
              f'<li aria-current="page">{oid}</li></ol></nav>')
    eb = eyebrow if eyebrow is not None else f"Order {oid} · {o['date']}"
    left = f'<div class="stack stack--lg"><div class="stack stack--sm"><p class="eyebrow">{eb}</p><h1>{head}</h1></div>{body}</div>'
    it = ITEMS[o["item"]]
    rows = [("Item", money(it["price"])), ("Shipping", money(it["ship"])), ("Sales tax", money(o["tax"]))]
    side = (f'<aside class="panel stack stack--lg" aria-label="Order summary">{item_row(o["item"])}{totals(rows, ("Total", money(o["total"])))}'
            f'{side_extra}{who}</aside>')
    return Page(main=f'{crumbs}<div class="split" style="margin-top: var(--space-8);">{left}{side}</div>')


SHIP_DATES = {"Paid": "Thu 22 Oct", "Shipped": "Fri 23 Oct"}

od = screen("order-detail", "page", "Order (buyer)", route="/orders/{id}", reqs=["L2-032", "L2-033", "L2-034", "L2-035", "L2-036"],
            na={"empty": "An order always has an item, a status and a history."})


@od.state("default", "Shipped order: progress, tracking link, confirm receipt once it arrives, and help.")
def _():
    o = ORDERS["TM-48213"]
    body = (timeline("Shipped", SHIP_DATES) + tracking(o) +
            f'<p class="text-muted">Expected Tue 27 Oct.</p>'
            f'<div class="cluster">{btn("I received it", "primary", D("confirm-receipt"))}{btn("Report a problem", "ghost", D("report-problem"))}</div>')
    return order_page("TM-48213", head="On its way", body=body)


@od.state("paid", "Paid, not shipped yet: the buyer can ask the seller to cancel.")
def _():
    body = (timeline("Paid", {"Paid": "Thu 22 Oct"}) + '<p class="text-muted">HandMeDownHana ships by Thu 29 Oct. If not, you’re refunded automatically.</p>'
            f'<div>{btn("Ask to cancel", href=D("request-cancellation"))}</div>')
    return order_page("TM-48213", head="Waiting to ship", body=body)


@od.state("cancel-requested", "Paid, and the buyer asked to cancel: the seller has not answered, so the request cannot be sent again.")
def _():
    body = (timeline("Paid", {"Paid": "Thu 22 Oct"}) + '<p class="text-muted">HandMeDownHana ships by Thu 29 Oct. If not, you’re refunded automatically.</p>'
            '<p>You asked HandMeDownHana to cancel. They haven’t answered yet.</p>')
    return order_page("TM-48213", head="Waiting to ship", body=body)


@od.state("cancel-declined", "Paid, and the seller declined the buyer’s request: the order goes ahead and cannot be asked about again.")
def _():
    body = (timeline("Paid", {"Paid": "Thu 22 Oct"}) + '<p class="text-muted">HandMeDownHana ships by Thu 29 Oct. If not, you’re refunded automatically.</p>'
            '<p>HandMeDownHana declined to cancel. They still ship by Thu 29 Oct.</p>')
    return order_page("TM-48213", head="Waiting to ship", body=body)


@od.state("delivered", "Delivered: confirm receipt, or report a problem within 3 days.")
def _():
    body = (timeline("Delivered", {"Paid": "Mon 19 Oct", "Shipped": "Tue 20 Oct", "Delivered": "Sat 24 Oct"}) +
            '<p class="text-muted">Completes automatically on Tue 27 Oct unless you report a problem.</p>'
            f'<div class="cluster">{btn("I received it", "primary", D("confirm-receipt"))}{btn("Report a problem", "ghost", D("report-problem"))}</div>')
    return order_page("TM-48140", head="Delivered", body=body)


@od.state("completed", "Completed: full timeline; the order is closed.")
def _():
    body = timeline("Completed", {"Paid": "Tue 13 Oct", "Shipped": "Wed 14 Oct", "Delivered": "Fri 16 Oct", "Completed": "Sat 17 Oct"})
    return order_page("TM-47790", head="Completed", body=body)


@od.state("cancelled", "Cancelled by the seller before shipping: full refund and the seller’s reason.")
def _():
    body = (alert("info", "Refunded in full", f"{money(53.35)} back to {CARD[0]} ending {CARD[1]}. Allow 5–10 days.") +
            '<blockquote class="quote"><p>“Found a tear in the lining while packing. Sorry about this.”</p><cite>HandMeDownHana</cite></blockquote>'
            f'<div>{btn("Shop similar items", "primary", P("catalog", "category"))}</div>')
    return order_page("TM-48213", head="Cancelled", body=body)


@od.state("refunded", "Problem resolved in the buyer’s favour: order refunded.")
def _():
    body = (alert("success", "Refunded in full", f"Our team reviewed the problem. {money(53.35)} is on its way back to {CARD[0]} ending {CARD[1]}.") +
            '<p class="text-muted text-sm">Resolved Sat 24 Oct by TuTuMuch support. No need to send the dress back.</p>'
            f'<div>{btn("Back to purchases", href=P("purchases"))}</div>')
    return order_page("TM-48213", head="Refunded", body=body)


@od.state("problem", "Open problem: the reason, the seller’s reply, and the payout held until TuTuMuch resolves it.")
def _():
    body = (alert("warning", "Problem open · Wrong size", "We’re holding the seller’s payout until our team decides. Usually within 2 business days.", icon="flag") +
            '<div class="thread">'
            '<div class="thread__item"><span class="text-caption">You · Sat 24 Oct</span><p>Label inside says boys’ 6, not girls’.</p></div>'
            '<div class="thread__item"><span class="text-caption">HandMeDownHana · Sun 25 Oct</span><p>My mistake, it came from the boys’ rack. Happy to take it back.</p></div></div>')
    return order_page("TM-48213", head="We’re on it", body=body)


@od.state("loading", "Order skeleton in the two-column layout.")
def _():
    return skeleton_split("Loading order")


@od.state("error", "The order failed to load.")
def _():
    return error_state("Couldn’t load this order", "0be2-58a4", "Back to purchases", P("purchases"))


# ---------------------------------------------------------------------------
# Sale detail, seller view
# ---------------------------------------------------------------------------
def ship_to(oid: str = "TM-48302") -> str:
    return (f'<div class="stack stack--sm"><h2 class="panel__title">Ship to</h2>{address_block(ORDERS[oid]["address"])}'
            '<p class="text-caption">Visible until 30 days after the order ends.</p></div>')


def sale_page(oid: str, head: str, body: str, eyebrow: str | None = None) -> Page:
    pg = order_page(oid, head=head, body=body, party="buyer", eyebrow=eyebrow, account="sales")
    pg.account = ""
    return pg


sd = screen("sale-detail", "page", "Sale (seller)", route="/sales/{id}", reqs=["L2-031", "L2-032", "L2-033", "L2-035", "L2-036", "L2-037"],
            na={"empty": "A sale always has an item, a buyer and a status."})


@sd.state("default", "New sale: ship by Friday, buyer’s address, add tracking.")
def _():
    body = (f'<p class="lead">Ship by <strong>Fri 30 Oct</strong>.</p>{ship_to()}'
            f'<div class="cluster">{btn("Add tracking", "primary", D("add-tracking"), icon="truck")}{btn("Cancel order", "ghost", D("cancel-order"))}</div>')
    pg = sale_page("TM-48302", "You made a sale", body)
    pg.notes = note("No tracking after 3 business days: reminder email. After 5: order cancelled and buyer refunded.", "top: 7rem; right: 1rem;")
    return pg


@sd.state("shipped", "Tracking added: the order is on its way, nothing to do.")
def _():
    o = dict(ORDERS["TM-48213"])
    body = timeline("Shipped", {"Paid": "Sat 24 Oct", "Shipped": "Sun 25 Oct"}) + tracking({"carrier": "USPS", "tracking": "9400 1118 9922 3456 1204 77"}) + ship_to()
    return sale_page("TM-48302", "Shipped", body)


@sd.state("cancel-requested", "The buyer asked to cancel before shipping: accept or decline.")
def _():
    body = ('<blockquote class="quote"><p>“Ordered the wrong colour, sorry. Can we cancel?”</p><cite>PuddleJumperPia</cite></blockquote>'
            f'<div>{btn("Respond", "primary", D("respond-cancellation"))}</div>{ship_to()}')
    return sale_page("TM-48302", "Buyer asked to cancel", body)


@sd.state("problem", "The buyer reported a problem: payout held, seller can reply once.")
def _():
    body = (alert("warning", "Problem reported · Wrong size", "Your payout is on hold while our team reviews this.", icon="flag") +
            '<div class="thread"><div class="thread__item"><span class="text-caption">PuddleJumperPia · Sat 24 Oct</span><p>Tag says boys’, not girls’.</p></div></div>' +
            field("reply", "Your reply", '<textarea class="textarea" id="reply" rows="3"{db}></textarea>', help="Visible to the buyer and to TuTuMuch.") +
            f'<div>{btn("Send reply", "primary")}</div>')
    return sale_page("TM-48302", "Problem reported", body)


@sd.state("completed", "Completed sale: payout after the TuTuMuch fee, released to the bank.")
def _():
    o = ORDERS["TM-47511"]
    pay = totals([("Item and shipping", money(37)), ("TuTuMuch fee (10%)", "−" + money(o["fee"]))], ("Your payout", money(o["payout"])))
    body = (timeline("Completed", {"Paid": "Fri 2 Oct", "Shipped": "Sat 3 Oct", "Delivered": "Tue 6 Oct", "Completed": "Wed 7 Oct"}) +
            f'<div class="panel panel--sunken stack"><h2 class="panel__title">Payout</h2>{pay}<p class="text-caption">Sent Wed 7 Oct to Chase ending 6610.</p></div>')
    return sale_page("TM-47511", "Paid out", body)


@sd.state("address-hidden", "More than 30 days after completion the buyer’s address is no longer shown.")
def _():
    body = (timeline("Completed", {"Paid": "Fri 2 Oct", "Shipped": "Sat 3 Oct", "Delivered": "Tue 6 Oct", "Completed": "Wed 7 Oct"}) +
            f'<div class="stack stack--sm"><h2 class="panel__title">Ship to</h2><p class="text-muted text-sm">{ic("eye-off", "icon icon--sm")} Removed 30 days after the order ended.</p></div>')
    return sale_page("TM-47511", "Completed", body, eyebrow="Order TM-47511 · Fri 2 Oct")


@sd.state("cancelled", "Cancelled sale: buyer refunded, listing stays out of the closet until relisted.")
def _():
    body = (alert("info", "Cancelled · Buyer refunded in full", "The listing stays out of the closet until you relist it.") +
            f'<div>{btn("Relist item", "primary", D("relist"))}</div>')
    return sale_page("TM-48302", "Cancelled", body)


@sd.state("loading", "Sale skeleton in the two-column layout.")
def _():
    return skeleton_split("Loading sale")


@sd.state("error", "The sale failed to load.")
def _():
    return error_state("Couldn’t load this sale", "a83f-11d0", "Back to sales", P("sales"))


# ---------------------------------------------------------------------------
# Dialogs
# ---------------------------------------------------------------------------
def foot(primary: str, cancel: str = "Cancel", busy: bool = False, danger: bool = False, disabled_cancel: bool = False) -> str:
    kind = "danger" if danger else "primary"
    b = ' aria-busy="true"' if busy else ""
    dc = " disabled" if disabled_cancel else ""
    return f'<button class="btn" type="button"{dc}>{cancel}</button><button class="btn btn--{kind}" type="submit"{b}>{primary}</button>'


def failed(text: str = "Nothing changed. Check your connection and try again.") -> str:
    return alert("danger", "That didn’t go through", text)


# Add tracking -------------------------------------------------------------
def tracking_body(num: str = "", err: str | None = None, af: bool = True) -> str:
    carriers = "".join(f"<option>{c}</option>" for c in ["USPS", "UPS", "FedEx", "DHL"])
    a = " autofocus" if af and not err else ""
    return (field("carrier", "Carrier", f'<select class="select" id="carrier"{a}>{carriers}</select>') +
            field("track", "Tracking number", value=num, error=err, attrs='autocomplete="off"' + (" autofocus" if err else "")))


at = screen("add-tracking", "dialog", "Add tracking", opens_from="sale-detail", reqs=["L2-033"])


@at.state("default", "Carrier and tracking number; saving marks the order Shipped and emails the buyer.")
def _():
    return Dialog(bg=("sale-detail", "default"), html=dialog("Add tracking", tracking_body(), foot("Mark as shipped"), desc="We’ll email PuddleJumperPia the tracking link."))


@at.state("invalid", "Malformed tracking number: inline error, input kept.")
def _():
    return Dialog(bg=("sale-detail", "default"), html=dialog("Add tracking", tracking_body("9400 1118", "That doesn’t look like a USPS tracking number. It’s usually 22 digits."), foot("Mark as shipped"), desc="We’ll email PuddleJumperPia the tracking link."))


@at.state("busy", "Saving: button busy, inputs kept.")
def _():
    return Dialog(bg=("sale-detail", "default"), html=dialog("Add tracking", tracking_body("9400 1118 9922 3456 1204 77", af=False), foot("Mark as shipped", busy=True, disabled_cancel=True), desc="We’ll email PuddleJumperPia the tracking link."))


@at.state("failed", "Save failed: error at the top, inputs kept, retry.")
def _():
    return Dialog(bg=("sale-detail", "default"), html=dialog("Add tracking", tracking_body("9400 1118 9922 3456 1204 77", af=False), foot("Try again"), desc="We’ll email PuddleJumperPia the tracking link.", top=failed()))


# Cancel order (seller) -----------------------------------------------------
REASONS = ["Item damaged", "No longer have it", "Listed by mistake", "Buyer asked me to cancel"]


def reason_body(err: bool = False, chosen: str = "", af: bool = True) -> str:
    opts = "".join(f'<label class="choice"><input type="radio" name="reason"{" checked" if r == chosen else ""}{" aria-invalid=\"true\"" if err else ""}{" autofocus" if af and i == 0 and not chosen else ""}><span>{r}</span></label>' for i, r in enumerate(REASONS))
    e = '<span class="field__error" id="reason-error">Choose a reason</span>' if err else ""
    db = ' aria-describedby="reason-error"' if err else ""
    return f'<fieldset{db}><legend>Reason</legend>{opts}{e}</fieldset>' + field("cnote", "Note to buyer", '<textarea class="textarea" id="cnote" rows="2"{db}></textarea>', optional=True)


co = screen("cancel-order", "dialog", "Cancel order", opens_from="sale-detail", reqs=["L2-035"])
CO_DESC = "PuddleJumperPia is refunded $48.15 in full. The listing stays out of the closet until you relist."


@co.state("default", "Seller cancels before shipping: reason required, full refund explained.")
def _():
    return Dialog(bg=("sale-detail", "default"), html=dialog("Cancel this sale?", reason_body(), foot("Cancel sale", "Keep sale", danger=True), desc=CO_DESC, icon="alert", icon_kind="danger"))


@co.state("invalid", "No reason chosen: the group is marked required.")
def _():
    return Dialog(bg=("sale-detail", "default"), html=dialog("Cancel this sale?", reason_body(err=True), foot("Cancel sale", "Keep sale", danger=True), desc=CO_DESC, icon="alert", icon_kind="danger"))


@co.state("busy", "Cancelling and refunding.")
def _():
    return Dialog(bg=("sale-detail", "default"), html=dialog("Cancel this sale?", reason_body(chosen="Item damaged"), foot("Cancel sale", "Keep sale", busy=True, danger=True, disabled_cancel=True), desc=CO_DESC, icon="alert", icon_kind="danger"))


@co.state("failed", "Cancellation failed: nothing changed, retry.")
def _():
    return Dialog(bg=("sale-detail", "default"), html=dialog("Cancel this sale?", reason_body(chosen="Item damaged", af=False), foot("Try again", "Keep sale", danger=True), desc=CO_DESC, icon="alert", icon_kind="danger", top=failed()))


# Respond to cancellation request -------------------------------------------
rc = screen("respond-cancellation", "dialog", "Respond to cancellation", opens_from="sale-detail", reqs=["L2-035"],
            na={"invalid": "Two buttons, no inputs to validate."})
RC_BODY = '<blockquote class="quote"><p>“Ordered the wrong colour, sorry. Can we cancel?”</p><cite>PuddleJumperPia</cite></blockquote>'
RC_DESC = "Accepting refunds $48.15 in full. Declining keeps the order — you still ship by Fri 30 Oct."


def rc_foot(busy=False):
    b = ' aria-busy="true"' if busy else ""
    return f'<button class="btn" type="button"{" disabled" if busy else ""}>Decline</button><button class="btn btn--primary" type="button"{b} autofocus>Accept and refund</button>'


@rc.state("default", "Accept (cancel and refund) or decline (order stays Paid).")
def _():
    return Dialog(bg=("sale-detail", "cancel-requested"), html=dialog("PuddleJumperPia asked to cancel", RC_BODY, rc_foot(), desc=RC_DESC))


@rc.state("busy", "Accepting: refund in progress.")
def _():
    return Dialog(bg=("sale-detail", "cancel-requested"), html=dialog("PuddleJumperPia asked to cancel", RC_BODY, rc_foot(True), desc=RC_DESC))


@rc.state("failed", "The response failed to save: retry.")
def _():
    return Dialog(bg=("sale-detail", "cancel-requested"), html=dialog("PuddleJumperPia asked to cancel", RC_BODY, rc_foot(), desc=RC_DESC, top=failed()))


# Relist --------------------------------------------------------------------
rl = screen("relist", "dialog", "Relist item", opens_from="sale-detail", reqs=["L2-035"], na={"invalid": "Relisting reuses the original details; nothing to fill in."})


@rl.state("default", "Relist with the original details.")
def _():
    body = item_row("heart-hoodie", f"{money(38)} · {ship_text(7)}")
    return Dialog(bg=("sale-detail", "cancelled"), html=dialog("Relist this item?", body, f'<button class="btn" type="button">Not now</button><a class="btn" href="{P("edit-listing")}">Edit first</a><button class="btn btn--primary" type="button" autofocus>Relist</button>', desc="It goes back in the closet with its original photos, price and description."))


@rl.state("busy", "Relisting.")
def _():
    body = item_row("heart-hoodie", f"{money(38)} · {ship_text(7)}")
    return Dialog(bg=("sale-detail", "cancelled"), html=dialog("Relist this item?", body, '<button class="btn" type="button" disabled>Not now</button><button class="btn btn--primary" type="button" aria-busy="true">Relist</button>', desc="It goes back in the closet with its original photos, price and description."))


# Request cancellation (buyer) -----------------------------------------------
rq = screen("request-cancellation", "dialog", "Ask to cancel", opens_from="order-detail", reqs=["L2-035"], na={"invalid": "The note is optional; nothing can be invalid."})


def rq_body(af=True):
    a = " autofocus" if af else ""
    return field("rq-note", "Message to HandMeDownHana", f'<textarea class="textarea" id="rq-note" rows="3"{a}{{db}}></textarea>', optional=True)


RQ_DESC = "If HandMeDownHana accepts, you’re refunded in full. Until then the order stays as is."


@rq.state("default", "Buyer asks the seller to cancel before shipping.")
def _():
    return Dialog(bg=("order-detail", "paid"), html=dialog("Ask to cancel?", rq_body(), foot("Send request", "Keep order"), desc=RQ_DESC))


@rq.state("busy", "Sending the request.")
def _():
    return Dialog(bg=("order-detail", "paid"), html=dialog("Ask to cancel?", rq_body(False), foot("Send request", "Keep order", busy=True, disabled_cancel=True), desc=RQ_DESC))


@rq.state("failed", "The request failed: retry, message kept.")
def _():
    return Dialog(bg=("order-detail", "paid"), html=dialog("Ask to cancel?", rq_body(False), foot("Try again", "Keep order"), desc=RQ_DESC, top=failed()))


# Confirm receipt -------------------------------------------------------------
cr = screen("confirm-receipt", "dialog", "Confirm receipt", opens_from="order-detail", reqs=["L2-034"], na={"invalid": "A single confirmation; no inputs."})
CR_DESC = "This completes the order and pays HandMeDownHana. You can’t report a problem afterwards."
CR_BODY = item_row("rainbow-sweater")


@cr.state("default", "Confirming completes the order and releases the payout.")
def _():
    return Dialog(bg=("order-detail", "delivered"), html=dialog("Got it and all good?", CR_BODY, foot("Yes, complete order", "Not yet"), desc=CR_DESC, size="sm"))


@cr.state("busy", "Completing the order.")
def _():
    return Dialog(bg=("order-detail", "delivered"), html=dialog("Got it and all good?", CR_BODY, foot("Yes, complete order", "Not yet", busy=True, disabled_cancel=True), desc=CR_DESC, size="sm"))


@cr.state("failed", "Completion failed: retry.")
def _():
    return Dialog(bg=("order-detail", "delivered"), html=dialog("Got it and all good?", CR_BODY, foot("Try again", "Not yet"), desc=CR_DESC, size="sm", top=failed()))


# Report a problem --------------------------------------------------------------
PROBLEMS = ["Not received", "Not as described", "Wrong size", "Counterfeit"]


def problem_body(chosen: str = "", text: str = "", errs: dict | None = None, photos: int = 0, af: bool = True) -> str:
    e = errs or {}
    rinv = ' aria-invalid="true"' if "reason" in e else ""
    opts = "".join(f'<label><input type="radio" name="prob"{" checked" if p == chosen else ""}{rinv}{" autofocus" if af and i == 0 and not chosen else ""}><span>{p}</span></label>' for i, p in enumerate(PROBLEMS))
    rerr = f'<span class="field__error" id="prob-error">{e["reason"]}</span>' if "reason" in e else ""
    pics = "".join(f'<li>{photo("rainbow-sweater", tag=False, alt=f"Your photo {i + 1}", tone=((i + 2) % 6) + 1, cls="photo--square")}</li>' for i in range(photos))
    add = f'<li><button class="uploader__add" type="button">{ic("camera")}Add photo</button></li>' if photos < 4 else ""
    return (f'<fieldset{" aria-describedby=\"prob-error\"" if "reason" in e else ""}><legend>What’s wrong?</legend><div class="segmented">{opts}</div>{rerr}</fieldset>' +
            field("prob-desc", "Describe it", f'<textarea class="textarea" id="prob-desc" rows="3"{{db}}>{esc(text)}</textarea>', error=e.get("desc")) +
            f'<div class="field"><span class="field__label">Photos <span class="optional">(up to 4)</span></span><ul class="uploader" role="list">{pics}{add}</ul></div>')


rp = screen("report-problem", "dialog", "Report a problem", opens_from="order-detail", reqs=["L2-036"])
RP_DESC = "Open until Tue 27 Oct. We hold the seller’s payout while our team looks into it."


@rp.state("default", "Reason, description and up to 4 photos.")
def _():
    return Dialog(bg=("order-detail", "delivered"), html=dialog("Report a problem", problem_body(), foot("Send to TuTuMuch"), desc=RP_DESC))


@rp.state("invalid", "No reason and no description: both marked inline.")
def _():
    errs = {"reason": "Choose what’s wrong", "desc": "Tell us what happened"}
    return Dialog(bg=("order-detail", "delivered"), html=dialog("Report a problem", problem_body(errs=errs, af=False), foot("Send to TuTuMuch"), desc=RP_DESC,
                                                                top=error_summary([("prob-error", errs["reason"]), ("prob-desc", errs["desc"])])))


@rp.state("busy", "Uploading photos and opening the problem.")
def _():
    return Dialog(bg=("order-detail", "delivered"), html=dialog("Report a problem", problem_body("Wrong size", "Label inside says boys’ 6, not girls’.", photos=2, af=False), foot("Send to TuTuMuch", busy=True, disabled_cancel=True), desc=RP_DESC))


@rp.state("failed", "Sending failed: everything kept, retry.")
def _():
    return Dialog(bg=("order-detail", "delivered"), html=dialog("Report a problem", problem_body("Wrong size", "Label inside says boys’ 6, not girls’.", photos=2, af=False), foot("Try again"), desc=RP_DESC, top=failed("Your photos and description are saved here. Try again.")))


# ---------------------------------------------------------------------------
# Reservation timer notification (L2-028)
# ---------------------------------------------------------------------------
rt = screen("reservation-timer", "notification", "Reservation timer", opens_from="checkout", reqs=["L2-028", "L2-030"])


@rt.state("info", "Banner on checkout: the item is held for this buyer for 10 minutes.")
def _():
    return Note(bg=("checkout", "default"), top=banner("info", "We’re holding the pink tulle tutu skirt for you · <span class=\"num\">9:41</span> left.", dismiss=False, icon="clock"))


@rt.state("warning", "Under a minute left: the banner turns amber and is announced once.")
def _():
    pg = checkout(tmr=timer("0:48", "warning"))
    return Note(bg=pg, top=banner("warning", "Less than a minute left to pay. After that, the item goes back on sale.", dismiss=False, icon="clock"),
                notes=note("Announced once at 60 seconds; the countdown itself is not read aloud.", "top: 6rem; right: 1rem;"))


@rt.state("danger", "Reservation expired mid-checkout: Pay disabled, start again.")
def _():
    pg = checkout(tmr=timer("0:00", "danger"), pay=btn(f"Pay {money(BUY_TOTAL)}", "primary lg block", attrs="disabled"))
    return Note(bg=pg, top=banner("danger", "Your reservation ended. You weren’t charged.", f'<a href="{P("listing-detail")}">Start again</a>', dismiss=False, icon="clock"))


@rt.state("success", "Payment confirmed: success toast over the confirmation page.")
def _():
    return Note(bg=("order-confirmation", "default"), toasts=toast_region(toast("success", "Payment confirmed", f"{money(BUY_TOTAL)} charged to {CARD[0]} ending {CARD[1]}.")))
