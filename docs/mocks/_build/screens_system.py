"""Administrator problem detail, error pages, legal pages, cookies and system notices
(L1-008, L1-014, L1-018, L2-036, L2-056, L2-068)."""
from mk import *

# ---------------------------------------------------------------------------
# Shared pieces
# ---------------------------------------------------------------------------
STATIC_NA = {"loading": "Static content ships with the app; nothing is fetched.",
             "empty": "The page always has content.",
             "error": "Static content cannot fail to load once the app is served."}


def admin_page(main: str, notes: str = "") -> Page:
    return Page(main=main, shell="admin", footer=False, notes=notes)


def head(title: str, sub: str = "", actions: str = "", crumb: tuple[str, str] | None = None) -> str:
    c = (f'<nav aria-label="Breadcrumb"><ol class="breadcrumb"><li><a href="{crumb[1]}">{crumb[0]}</a></li>'
         f'<li aria-current="page">{title}</li></ol></nav>') if crumb else ""
    s = f'<p class="subtitle">{sub}</p>' if sub else ""
    return f'<div class="page-header"><div class="stack stack--sm">{c}<h1>{title}</h1>{s}</div>{actions}</div>'


def center(code: str, title: str, text: str, actions: str, ref: str = "", icon: str | None = None) -> str:
    big = f'<span class="code" aria-hidden="true">{code}</span>' if code else ""
    i = f'<div class="empty__icon">{ic(icon, "icon icon--lg")}</div>' if icon else ""
    r = f'<span class="ref">{ref}</span>' if ref else ""
    return f'<div class="center-state">{big}{i}<h1>{title}</h1><p class="text-muted">{text}</p>{r}<div class="actions">{actions}</div></div>'


def skel_rows(n: int = 4) -> str:
    row = ('<li><div class="item-row" style="padding-block: var(--space-5);" aria-hidden="true"><span class="skeleton skeleton--rect" style="width: 3.5rem; height: 3.5rem;"></span>'
           '<div class="item-row__body"><span class="skeleton skeleton--text" style="width: 45%;"></span><span class="skeleton skeleton--text" style="width: 25%;"></span></div></div></li>')
    return f'<ul class="queue" role="list" aria-busy="true" aria-label="Loading">{row * n}</ul>'


def skel_detail() -> str:
    return ('<div class="split" aria-busy="true" aria-label="Loading"><div class="stack stack--lg"><span class="skeleton skeleton--title" style="width: 60%;"></span>'
            '<span class="skeleton skeleton--rect" style="height: 10rem;"></span><div><span class="skeleton skeleton--text"></span><span class="skeleton skeleton--text"></span></div></div>'
            '<div class="panel panel--sunken stack"><span class="skeleton skeleton--text" style="width: 40%;"></span><span class="skeleton skeleton--rect" style="height: 3rem;"></span></div></div>')


def load_error(what: str, ref: str) -> str:
    return empty("refresh", f"Couldn’t load {what}", f'Something went wrong on our side. <span class="ref">Ref {ref}</span>',
                 btn("Try again", "primary", icon="refresh"), cls="empty--error")


# ---------------------------------------------------------------------------
# Problem detail
# ---------------------------------------------------------------------------
def problem_main(resolved: bool) -> str:
    o = ORDERS["TM-48140"]
    photos = '<ul class="uploader" role="list">' + "".join(
        f'<li>{photo("rainbow-sweater", tag=False, alt=f"Buyer photo {i} of the sweater", tone=t)}</li>' for i, t in [(1, 1), (2, 3)]) + "</ul>"
    claim = ('<div class="thread__item"><span class="text-caption">TutuTessa · buyer · Sun 25 Oct</span>'
             '<p>Pilling all over the front and a small hole by the left cuff. Listing said excellent.</p>' + photos + "</div>")
    reply = ('<div class="thread__item"><span class="text-caption">HandMeDownHana · seller · Sun 25 Oct</span>'
             '<p>It left here without the hole. Happy to take it back.</p></div>')
    if resolved:
        top = alert("success", "Refunded in full", f"{money(o['total'])} back to the buyer. No payout. Today 11:05 ET.")
        action = ""
    else:
        top = ""
        action = btn("Resolve", "primary", D("resolve-problem"))
    side = (f'<aside class="panel panel--sunken stack"><span class="eyebrow">Order TM-48140</span>{item_row("rainbow-sweater")}'
            f'<dl class="facts"><dt>Status</dt><dd>{status("Refunded" if resolved else "Delivered")}</dd><dt>Total</dt><dd class="num">{money(o["total"])}</dd>'
            f'<dt>Delivered</dt><dd>{o["delivered"]}</dd><dt>Payout</dt><dd>{"Not paid" if resolved else "Held"}</dd></dl>{action}</aside>')
    body = f'<div class="stack stack--lg">{top}<div class="thread">{claim}{reply}</div></div>'
    return head("Not as described", "Order problem P-0912 · opened yesterday") + f'<div class="split">{body}{side}</div>'


pr = screen("problem-detail", "page", "Problem detail", route="/admin/problems/{id}", reqs=["L2-036", "L2-010"],
            na={"empty": "A problem always has a buyer claim and an order."})


@pr.state("default", "Buyer claim with photos, seller response, payout held; one action to resolve.")
def _():
    return admin_page(problem_main(False))


@pr.state("resolved", "Resolved in the buyer's favour: order refunded, no payout.")
def _():
    return admin_page(problem_main(True))


@pr.state("loading", "Problem detail skeleton.")
def _():
    return admin_page('<h1 class="visually-hidden">Loading problem</h1><div style="margin-top: var(--space-10);">' + skel_detail() + "</div>")


@pr.state("error", "The problem failed to load.")
def _():
    return admin_page(head("Order problem") + load_error("this problem", "5d10-be3c"))


# ---------------------------------------------------------------------------
# Error and status pages
# ---------------------------------------------------------------------------
def simple(sid, title, route, reqs, desc, page_fn, na=None):
    sc = screen(sid, "page", title, route=route, reqs=reqs, na=na or STATIC_NA)
    sc.state("default", desc)(page_fn)
    return sc


simple("not-found", "Not found", "/*", ["L2-068"], "Unknown URL: say so plainly, offer the shop.",
       lambda: Page(main=center("4<em>0</em>4", "Page not found", "The link may be old or mistyped.", btn("Back to shop", "primary", P("catalog")))),
       na={"loading": "Shown after routing; nothing to load.", "empty": "Not a collection.", "error": "This page is itself the error."})

simple("forbidden", "Forbidden", "/403", ["L2-010", "L2-068"], "Signed in but not allowed: who can see this and a way back.",
       lambda: Page(main=center("4<em>0</em>3", "You can’t open this", "It belongs to another member’s account.", btn("Go to your account", "primary", P("account")))),
       na={"loading": "Shown after the API refuses; nothing to load.", "empty": "Not a collection.", "error": "This page is itself the error."})

simple("server-error", "Server error", "/error", ["L2-068"], "Unhandled 500: “Something went wrong”, Try again and the correlation ID.",
       lambda: Page(main=center("5<em>0</em>0", "Something went wrong", "It’s on our side. Your data is safe.",
                                btn("Try again", "primary", icon="refresh") + btn("Back to shop", href=P("catalog")), ref="Ref 3c8e-1d42-a7f0")),
       na={"loading": "Rendered from the error response; nothing to load.", "empty": "Not a collection.", "error": "This page is itself the error."})

simple("offline", "Offline", "(any)", ["L2-068"], "The device lost its connection: “You’re offline”, retry when back.",
       lambda: Page(main=center("", "You’re offline", "Check your connection. We’ll pick up where you left off.", btn("Try again", "primary", icon="refresh"), icon="wifi-off")),
       na={"loading": "Shown from the cached shell without a request.", "empty": "Not a collection.", "error": "This page is itself the error state."})

simple("maintenance", "Maintenance", "(any)", ["L2-066"], "Announced maintenance window with the time we’re back.",
       lambda: Page(main=center("", "Back at 3:00 AM ET", "TuTuMuch is down for planned maintenance. Orders and payments are safe.",
                                btn("Check status", "primary", icon="refresh"), icon="wrench"), user="guest"),
       na={"loading": "Served as a static page during maintenance.", "empty": "Not a collection.", "error": "This page is itself the outage notice."})


# ---------------------------------------------------------------------------
# Legal and rules
# ---------------------------------------------------------------------------
def doc_page(title: str, sub: str, body: str) -> Page:
    return Page(main=f'<div class="page--narrow">{head(title, sub)}{body}</div>', user="guest")


def rules_body() -> str:
    rows = "".join(f'<tr><th scope="row">{c}</th><td>{", ".join(s)}</td><td>{size_tag(z)}</td></tr>' for c, s, z in CATEGORIES)
    sizes = ('<div class="table-wrap"><table class="table"><caption class="visually-hidden">Accepted categories and sizes</caption>'
             f'<thead><tr><th scope="col">Category</th><th scope="col">Includes</th><th scope="col">Size</th></tr></thead><tbody>{rows}</tbody></table></div>')
    rules = [("For girls only", "Clothing, shoes, dance, sports, cameras and tech, toys and crafts. Every item must be for girls."),
             ("18 and over", "One account to buy and sell. No approval step."),
             ("Honest condition", ", ".join(CONDITIONS) + ". Used - Fair needs its flaws described."),
             ("Ship in 5 business days", "Add tracking or the order is cancelled and refunded."),
             ("10% fee", "Taken from item plus shipping when the order completes."),
             ("US only", "Prices in US dollars, shipping within the United States."),
             ("No counterfeits", "Fakes are removed from the catalog.")]
    lst = "".join(f'<li class="settings-list__body" style="padding-block: var(--space-5); border-bottom: 1px solid var(--color-border-default);">'
                  f'<span class="settings-list__title">{t}</span><span class="settings-list__meta">{d}</span></li>' for t, d in rules)
    return f'<div class="stack stack--lg">{sizes}<ol class="rules-list" role="list">{lst}</ol></div>'


simple("rules", "Marketplace rules", "/rules", ["L2-011", "L2-014", "L2-016", "L2-031", "L2-033"],
       "The rules on one short page: sizes, age, condition, shipping, fee, US only, counterfeits.",
       lambda: doc_page("Marketplace rules", "Short, because it only has to cover girls’ clothing.", rules_body()))


def privacy_body() -> str:
    items = [("What we collect", "Email, password hash, display name, shipping addresses, and orders."),
             ("Never public", "Your email, phone and street address never appear on TuTuMuch pages."),
             ("Encrypted", "Shipping addresses are encrypted at rest. Card numbers never reach us."),
             ("Erasure", "Close your account and we erase your data within 30 days."),
             ("Cookies", "Only necessary cookies until you accept analytics.")]
    lst = "".join(f'<li class="settings-list__body" style="padding-block: var(--space-5); border-bottom: 1px solid var(--color-border-default);">'
                  f'<span class="settings-list__title">{t}</span><span class="settings-list__meta">{d}</span></li>' for t, d in items)
    return f'<ul class="rules-list" role="list">{lst}</ul><p class="text-sm" style="margin-top: var(--space-8);"><a href="{D("cookie-settings")}">Cookie settings</a></p>'


simple("privacy", "Privacy", "/privacy", ["L2-054", "L2-056", "L2-005"], "What TuTuMuch keeps, what stays private, and erasure.",
       lambda: doc_page("Privacy", "Updated 1 Oct 2026", privacy_body()))


def terms_body() -> str:
    terms = ["You must be 18 or older to hold an account.",
             "List only girls’ clothing you own.",
             "Describe condition and flaws honestly; photos must be of the actual item.",
             "Payment goes through our payment provider. Sellers are paid when the order completes.",
             "Buyers can report a problem until 3 days after delivery.",
             "We may remove listings that break the marketplace rules.",
             "TuTuMuch operates in the United States only."]
    lst = "".join(f"<li>{t}</li>" for t in terms)
    return (f'<ol class="terms-list">{lst}</ol><p class="text-sm text-muted" style="margin-top: var(--space-8);">See also the '
            f'<a href="{P("rules")}">marketplace rules</a> and <a href="{P("privacy")}">privacy</a>.</p>')


simple("terms", "Terms", "/terms", ["L2-001", "L2-036", "L2-031"], "Terms of service as a short numbered list.",
       lambda: doc_page("Terms of service", "Effective 1 Oct 2026", terms_body()))


# ---------------------------------------------------------------------------
# Dialogs
# ---------------------------------------------------------------------------
def reason_field(error: str | None = None, value: str = "", busy: bool = False) -> str:
    ro = " readonly" if busy else ""
    ctrl = f'<textarea class="textarea" id="reason" name="reason" rows="3"{{db}}{ro}>{esc(value)}</textarea>'
    return field("reason", "Reason", ctrl, help="Sent to both members.", error=error)


def rp_dialog(error=None, value="", busy=False) -> str:
    o = ORDERS["TM-48140"]
    ch = ('<fieldset><legend>Decision</legend>'
          f'<label class="choice"><input type="radio" name="outcome" checked><span class="choice__text">Refund buyer in full<small>{money(o["total"])} to TutuTessa · no payout</small></span></label>'
          f'<label class="choice"><input type="radio" name="outcome"><span class="choice__text">Release payout to seller<small>Order completes · HandMeDownHana is paid</small></span></label></fieldset>')
    prim = btn("Refunding", "primary", attrs='aria-busy="true"') if busy else btn("Refund buyer", "primary")
    return dialog("Resolve problem P-0912", ch + reason_field(error, value, busy), btn("Cancel", attrs="disabled" if busy else "") + prim)


rp = screen("resolve-problem", "dialog", "Resolve problem", opens_from="problem-detail", reqs=["L2-036"])


@rp.state("default", "Refund the buyer or release the payout, with a reason.")
def _():
    return Dialog(bg=("problem-detail", "default"), html=rp_dialog())


@rp.state("invalid", "Reason missing.")
def _():
    return Dialog(bg=("problem-detail", "default"), html=rp_dialog(error="Add a reason. Both members see it."))


@rp.state("busy", "Refund in progress: confirm busy.")
def _():
    return Dialog(bg=("problem-detail", "default"), html=rp_dialog(value="Hole by the left cuff wasn't disclosed.", busy=True))


def cookie_dialog(analytics: bool) -> str:
    body = (f'<label class="switch" style="justify-content: space-between; width: 100%;"><span class="choice__text">Strictly necessary<small>Sign-in and checkout. Always on.</small></span>'
            f'<input type="checkbox" role="switch" checked disabled></label>'
            f'<label class="switch" style="justify-content: space-between; width: 100%;"><span class="choice__text">Analytics<small>Helps us see which pages are slow.</small></span>'
            f'<input type="checkbox" role="switch"{" checked" if analytics else ""} autofocus></label>')
    return dialog("Cookie settings", body, btn("Cancel") + btn("Save", "primary"), size="sm")


cs = screen("cookie-settings", "dialog", "Cookie settings", opens_from="catalog", reqs=["L2-056"],
            na={"busy": "The choice is stored in the browser instantly; no request.", "invalid": "Only switches; nothing can be invalid."})


@cs.state("default", "Strictly necessary always on; analytics off until the visitor opts in.")
def _():
    return Dialog(bg=("catalog", "guest"), html=cookie_dialog(False))


@cs.state("analytics-on", "Analytics accepted; turning it off removes the cookies.")
def _():
    return Dialog(bg=("catalog", "guest"), html=cookie_dialog(True))


# ---------------------------------------------------------------------------
# Notifications
# ---------------------------------------------------------------------------
cc = screen("cookie-consent", "notification", "Cookie consent", opens_from="catalog", reqs=["L2-056"],
            na={"warning": "Consent has nothing to warn about.", "danger": "Choices are stored locally and cannot fail."})


@cc.state("info", "First visit: only necessary cookies are set until the visitor chooses.")
def _():
    html = (f'<section class="consent" aria-labelledby="consent-title"><h2 id="consent-title" style="font: var(--text-h4);">Cookies</h2>'
            f'<p class="text-sm">We use necessary cookies to run TuTuMuch. Analytics only if you say yes.</p>'
            f'<div class="consent__actions">{btn("Accept analytics", "primary")}{btn("Necessary only")}</div>'
            f'<a class="text-sm" href="{D("cookie-settings")}">Cookie settings</a></section>')
    return Note(bg=("catalog", "guest"), after=html)


@cc.state("success", "Choice saved: short toast with a way to change it.")
def _():
    return Note(bg=("catalog", "guest"), toasts=toast_region(toast("success", "Cookie choice saved", "Necessary only. Change it any time in the footer.")))


sb = screen("system-banner", "notification", "System notices", opens_from="catalog", reqs=["L2-066", "L2-068"])


@sb.state("info", "Announced maintenance: persistent banner with the window in ET.")
def _():
    return Note(bg=("catalog", "default"), top=banner("info", "Planned maintenance Tue 27 Oct, 2:00–3:00 AM ET.", icon="wrench"))


@sb.state("success", "Connection restored: brief toast.")
def _():
    return Note(bg=("catalog", "default"), toasts=toast_region(toast("success", "Back online")))


@sb.state("warning", "Offline: persistent banner instead of a generic error.")
def _():
    return Note(bg=("catalog", "default"), top=banner("warning", "You’re offline. We’ll retry when you’re back.", dismiss=False, icon="wifi-off"))


@sb.state("danger", "A request failed with 500: error toast with Try again and the correlation ID; never auto-dismisses.")
def _():
    pg = Note(bg=("catalog", "default"), toasts=toast_region(toast("danger", "Something went wrong", "Ref 3c8e-1d42-a7f0", action="Try again")))
    pg.notes = note("Error toasts stay until dismissed.", "bottom: 9rem; right: 1rem;")
    return pg
