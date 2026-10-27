"""Item requests: the request form, My requests, delete, toasts and the match email (L1-023)."""
from mk import *

# Tessa's requests, newest first. Saved dates are 2026; a request is open for 90 days after it was saved (L2-098).
# (id, category, keywords, highest price, lowest condition, "Open until ..." or "Expired on ...", matches, match state)
#   matches: item keys still Active; gone: the request matched before, but nothing it matched is still Active.
REQUESTS = [
    {"cat": "Dance", "q": "ballet slippers", "max": "20.00", "cond": "Used - Good", "until": "Jan 18", "matches": ["ballet-slippers"]},
    {"cat": "Cameras and tech", "q": "kids smartwatch", "max": "", "cond": "", "until": "Jan 10", "matches": []},
    {"cat": "Sports", "q": "scooter", "max": "50.00", "cond": "", "until": "Dec 30", "matches": [], "gone": True},
]
EXPIRED = {"cat": "Sports", "q": "swim goggles", "max": "", "cond": "", "expired": "Oct 18", "matches": []}

FORM_REQS = ["L2-097", "L2-098", "L2-099", "L2-104", "L2-105", "L2-063", "L2-065"]


def facts(r: dict) -> str:
    """Category, the one size, the highest price and the lowest condition, in one muted line."""
    bits = [esc(r["cat"]), "Girls’"]
    if r["max"]:
        bits.append(f"Up to {money(float(r['max']))}")
    if r["cond"]:
        bits.append(f"{esc(r['cond'])} or better")
    return " · ".join(bits)


def request_item(r: dict, n: int) -> str:
    """One request: keywords as its title, the facts, open-until or expired, Edit and Delete, then its matches (L2-099, L2-102)."""
    if "expired" in r:
        when = f'{status("Expired")}<span class="text-sm text-muted">Expired on {r["expired"]}</span>'
        acts = btn("Renew", "sm", attrs='type="button"') + btn("Delete", "ghost sm", D("delete-request"))
    else:
        when = f'<span class="text-sm text-muted">Open until {r["until"]}</span>'
        acts = btn("Edit", "sm", P("request-form", "edit")) + btn("Delete", "ghost sm", D("delete-request"))
    if r["matches"]:
        word = "match" if len(r["matches"]) == 1 else "matches"
        found = (f'<div class="request__matches"><h3 class="text-sm" id="m{n}">{len(r["matches"])} {word}</h3>'
                 f'<ul class="listing-grid" role="list" aria-labelledby="m{n}">{"".join(listing_card(k) for k in r["matches"])}</ul></div>')
    elif r.get("gone"):
        found = '<p class="request__note text-sm text-muted">Your match is no longer available. We’ll keep looking.</p>'
    elif "expired" in r:
        found = '<p class="request__note text-sm text-muted">Renew it to keep looking for 90 more days.</p>'
    else:
        found = '<p class="request__note text-sm text-muted">No matches yet. We’ll email you when one is listed.</p>'
    return (f'<li class="request"><div class="request__head"><div class="stack stack--sm">'
            f'<h2 class="request__title">{esc(r["q"])}</h2><span class="text-sm text-muted">{facts(r)}</span>'
            f'<span class="cluster" style="gap: var(--space-2);">{when}</span></div>'
            f'<div class="cluster request__actions">{acts}</div></div>{found}</li>')


def request_list(rows) -> str:
    return f'<ul class="request-list" role="list">{"".join(request_item(r, i) for i, r in enumerate(rows, 1))}</ul>'


def requests_page(body: str, sub: str = "3 open requests", action: bool = True, top: str = "", user: str = "member") -> Page:
    act = btn("Request an item", "primary", P("request-form"), icon="plus") if action else ""
    s = f'<p class="subtitle">{sub}</p>' if sub else ""
    head = f'<div class="page-header"><div><h1>Requests</h1>{s}</div>{act}</div>'
    return Page(main=head + top + body, account="requests", user=user)


# ---------------------------------------------------------------------------
# My requests (L2-098, L2-099, L2-102)
# ---------------------------------------------------------------------------
mr = screen("my-requests", "page", "My requests", route="/account/requests",
            reqs=["L2-098", "L2-099", "L2-102", "L2-105", "L2-106", "L2-061", "L2-065"])


@mr.state("default", "Three open requests, newest first: one with a match still for sale, one with no match yet, one whose match sold.")
def _():
    pg = requests_page(request_list(REQUESTS))
    pg.notes = note("Only the member sees their requests; sellers never see who asked (L2-106). Matches show while the listing is Active.",
                    "top: 12rem; right: 1rem;")
    return pg


@mr.state("loading", "Skeleton requests shaped like the real ones: a title, a line of facts and two card wells.")
def _():
    one = ('<li class="request"><div class="request__head"><div class="stack stack--sm" style="flex: 1;">'
           '<span class="skeleton skeleton--title" style="width: 40%;"></span><span class="skeleton skeleton--text" style="width: 60%;"></span>'
           '<span class="skeleton skeleton--text" style="width: 25%;"></span></div></div></li>')
    return requests_page(f'<ul class="request-list" role="list" aria-busy="true" aria-label="Loading requests">{one * 3}</ul>', sub="")


@mr.state("empty", "No requests yet: Tilly waves hello, one line and one action.")
def _():
    body = empty("bell", "No requests yet", "Ask for something that isn’t in the closet yet. We’ll email you when it’s listed.",
                 btn("Request an item", "primary", P("request-form")))
    return requests_page(body, sub="", action=False)


@mr.state("error", "Requests failed to load: what happened, a reference and Try again.")
def _():
    body = empty("refresh", "Couldn’t load your requests", 'Something went wrong on our side. <span class="ref">Ref 4b2e-90d1</span>',
                 btn("Try again", "primary", icon="refresh"), cls="empty--error empty--compact")
    return requests_page(body, sub="", action=False)


@mr.state("limit-reached", "Twenty open requests: the action gives way to a line saying to delete one first (L2-098).")
def _():
    line = (f'<p class="text-sm text-muted request-list__limit">{ic("info")}You have 20 open requests. Delete one to add another.</p>')
    return requests_page(line + request_list(REQUESTS), sub="20 open requests", action=False)


@mr.state("expired", "An expired request stays for 30 days, marked Expired, with Renew; it no longer matches.")
def _():
    return requests_page(request_list(REQUESTS[:2] + [EXPIRED]), sub="2 open requests")


@mr.state("no-matches", "Requests with no match yet, and one whose only match is no longer available.")
def _():
    return requests_page(request_list(REQUESTS[1:]), sub="2 open requests")


@mr.state("signed-out", "A visitor opening /account/requests goes to sign in and comes back (L2-105).")
def _():
    pg = REGISTRY["sign-in"].states["default"][1]()
    pg.notes = note("Reached from /account/requests: the URL is /sign-in?returnUrl=%2Faccount%2Frequests and a successful sign-in lands back on My requests.",
                    "top: 6rem; right: 1rem;")
    return pg


# ---------------------------------------------------------------------------
# Request form (L2-097, L2-098, L2-104)
# ---------------------------------------------------------------------------
rf = screen("request-form", "page", "Request an item", route="/account/requests/new", reqs=FORM_REQS,
            na={"empty": "The form is the page; a blank form is the default state."})


def select(fid: str, options, chosen: str = "", first: str = "") -> str:
    opts = (f'<option value="">{first}</option>' if first else "") + "".join(
        f'<option{" selected" if o == chosen else ""}>{esc(o)}</option>' for o in options)
    return f'<select class="select" id="{fid}" name="{fid}"{{db}}>{opts}</select>'


def money_input(fid: str, value: str) -> str:
    v = f' value="{value}"' if value else ""
    return f'<div class="input-group"><span class="addon">$</span><input class="input" id="{fid}" name="{fid}" inputmode="decimal"{v}{{db}}></div>'


def request_form(v: dict, *, errors: dict | None = None, busy: bool = False, top: str = "", edit: bool = False) -> str:
    e = errors or {}
    cat = v.get("cat", "")
    size_html = ""
    if cat:
        size_html = (f'<div class="fixed-size" id="size">{size_tag("Girls\'", "size-tag--lg")}<span>Size: Girls’</span>'
                     f'<span class="fixed-size__label">Set by category</span></div>')
    q = v.get("q", "")
    save = btn("Save request", "primary", attrs=('aria-busy="true"' if busy else 'type="submit"'))
    cancel = f'<a class="btn btn--ghost" href="{P("my-requests")}">Cancel</a>'
    return (f'<form class="sell-form" novalidate>{top}'
            f'<section class="form-section" aria-labelledby="s-what"><h2 id="s-what">What she needs</h2>'
            + field("cat", "Category", select("cat", [c for c, _, _ in CATEGORIES], cat, "Choose a category"), error=e.get("cat"))
            + size_html
            + field("keywords", "Keywords", value=q, error=e.get("q"), counter=f"{len(q)} / 100",
                    help="The words a seller would use, like “ballet slippers”. Every word must be in the listing.")
            + '</section>'
            f'<section class="form-section" aria-labelledby="s-limits"><h2 id="s-limits">Limits</h2><div class="form-row">'
            + field("max", "Highest price", money_input("max", v.get("max", "")), optional=True, error=e.get("max"), help="Shipping not included.")
            + field("cond", "Lowest condition", select("cond", CONDITIONS, v.get("cond", ""), "Any condition"), optional=True,
                    help="This condition or better.")
            + '</div></section>'
            f'<p class="text-sm text-muted cluster" style="gap: var(--space-2);">{ic("mail")}<span>We’ll email you when a matching item is listed. A request stays open for 90 days{" from today" if edit else ""}.</span></p>'
            f'<div class="form-actions form-actions--sticky form-actions--split">{cancel}{save}</div></form>')


def form_page(form: str, title: str = "Request an item") -> Page:
    head = f'<div class="page-header"><div><h1>{title}</h1><p class="subtitle">Can’t find it in the closet? Tell us what she needs.</p></div></div>'
    return Page(main=head + form, account="requests", main_class="container page page--form", footer=False)


def form_blocked(icon: str, title: str, text: str, actions: str) -> Page:
    main = f'<div class="center-state"><div class="empty__icon">{ic(icon, "icon icon--lg")}</div><h1>{title}</h1><p class="text-muted">{text}</p><div class="actions">{actions}</div></div>'
    return Page(main=main, account="requests")


@rf.state("default", "A blank request: no size choice; the size appears, fixed, once a category is chosen.")
def _():
    pg = form_page(request_form({}))
    pg.notes = note("No size input exists. Every request is for Girls’, like every listing (L2-097, L2-012).", "top: 18rem; right: 1rem;")
    return pg


@rf.state("prefilled", "Opened from an empty Dance search for “ballet slippers”: category and keywords filled in (L2-104).")
def _():
    pg = form_page(request_form({"cat": "Dance", "q": "ballet slippers"}))
    pg.notes = note("URL: /account/requests/new?category=dance&amp;q=ballet+slippers", "top: 6rem; right: 1rem;")
    return pg


@rf.state("invalid", "Save refused: a summary at the top and each problem inline; focus moves to the summary.")
def _():
    errs = {"cat": "Choose a category", "q": "Describe the item in 3 to 100 characters", "max": "Enter a price from $1.00 to $5,000.00"}
    top = error_summary([("cat", errs["cat"]), ("keywords", errs["q"]), ("max", errs["max"])])
    return form_page(request_form({"q": "sl", "max": "0.99"}, errors=errs, top=top))


@rf.state("submitting", "Saving: the button is busy and nothing shifts.")
def _():
    return form_page(request_form(REQUESTS[0], busy=True))


@rf.state("success", "Saved: the member is back on My requests with the “Request saved” toast.")
def _():
    pg = requests_page(request_list(REQUESTS))
    pg.after = toast_region(toast("success", "Request saved", "We’ll email you when it’s listed."))
    return pg


@rf.state("edit", "Editing a request: its values filled in; saving renews it for 90 days.")
def _():
    return form_page(request_form(REQUESTS[0], edit=True), "Edit request")


@rf.state("loading", "Opening a request to edit: skeleton fields until it loads.")
def _():
    sk = ('<div class="stack stack--lg" aria-busy="true" aria-label="Loading request">'
          + '<div class="stack stack--sm"><span class="skeleton skeleton--text" style="width: 20%;"></span><span class="skeleton skeleton--rect" style="height: 3rem;"></span></div>' * 4
          + '</div>')
    return form_page(sk, "Edit request")


@rf.state("error", "The request could not be loaded: a reference and Try again.")
def _():
    return form_blocked("refresh", "Couldn’t open this request", 'Something went wrong on our side. <span class="ref">Ref 0c7d-55e2</span>',
                        btn("Try again", "primary", icon="refresh") + btn("Your requests", href=P("my-requests")))


@rf.state("not-found", "Another member's request, or one already deleted: answered as missing (404, L2-099).")
def _():
    return form_blocked("search", "This request isn’t here", "It may have been deleted.", btn("Your requests", "primary", P("my-requests")))


@rf.state("limit-reached", "Saving a 21st open request: refused with 409 REQUEST_LIMIT_REACHED, nothing stored (L2-098).")
def _():
    return form_blocked("info", "You have 20 open requests", "Delete one to add another. Expired requests don’t count.",
                        btn("Your requests", "primary", P("my-requests", "limit-reached")) + btn("Shop the closet", href=P("catalog")))


@rf.state("signed-out", "A visitor choosing “Request it” signs in first and comes back to the filled-in form (L2-105).")
def _():
    pg = REGISTRY["sign-in"].states["default"][1]()
    pg.notes = note("URL: /sign-in?returnUrl=%2Faccount%2Frequests%2Fnew%3Fq%3Dballet%2Bslippers. After signing in the form opens with “ballet slippers” filled in.",
                    "top: 6rem; right: 1rem;")
    return pg


# ---------------------------------------------------------------------------
# Delete request (L2-099)
# ---------------------------------------------------------------------------
dr = screen("delete-request", "dialog", "Delete request", opens_from="my-requests", reqs=["L2-099"],
            na={"invalid": "Confirming has no inputs to validate."})


def delete_dialog(primary: str, top: str = "") -> Dialog:
    html = dialog("Delete this request?", "", btn("Keep request", attrs="autofocus") + primary,
                  desc="“ballet slippers” stops looking for new listings. This can’t be undone.", icon="trash", icon_kind="danger", size="sm", top=top)
    return Dialog(bg=("my-requests", "default"), html=html)


@dr.state("default", "Confirm before deleting; the safe action has focus.")
def _():
    return delete_dialog(btn("Delete", "danger"))


@dr.state("busy", "Deleting: the danger button is busy and Keep request is disabled.")
def _():
    d = delete_dialog(btn("Delete", "danger", attrs='aria-busy="true"'))
    d.html = d.html.replace("autofocus>Keep request", "disabled>Keep request")
    return d


@dr.state("failed", "Delete failed: the request is unchanged; try again.")
def _():
    return delete_dialog(btn("Try again", "danger"), top=alert("danger", "Couldn’t delete", "Your request is still open."))


# ---------------------------------------------------------------------------
# Toasts (L2-097, L2-098, L2-099)
# ---------------------------------------------------------------------------
rt = screen("request-toasts", "notification", "Request toasts", opens_from="my-requests", reqs=["L2-097", "L2-098", "L2-099"])


def on_requests(*toasts) -> Note:
    return Note(bg=("my-requests", "default"), toasts=toast_region(*toasts))


@rt.state("info", "A request was deleted.")
def _():
    return on_requests(toast("info", "Request deleted"))


@rt.state("success", "A request was saved, new or edited.")
def _():
    return on_requests(toast("success", "Request saved", "We’ll email you when it’s listed."))


@rt.state("warning", "Renewing a request with 20 already open.")
def _():
    return on_requests(toast("warning", "You have 20 open requests", "Delete one to renew this one."))


@rt.state("danger", "A delete failed: stays until dismissed, offers Try again.")
def _():
    return on_requests(toast("danger", "Couldn’t delete your request", "Check your connection.", "Try again"))


# ---------------------------------------------------------------------------
# Match email (L2-101, L2-103)
# ---------------------------------------------------------------------------
me = screen("request-match-email", "page", "Match email", route="(email)", reqs=["L2-101", "L2-103", "L2-042"],
            na={"loading": "An email is static text.", "empty": "An email is only sent for a match.", "error": "Sending failures are retried by the outbox (L2-042); the member sees nothing."})


def email_page(subject: str, body: str, headers: str) -> Page:
    meta = (f'<dl class="facts"><dt>From</dt><dd>TuTuMuch</dd><dt>To</dt><dd>{esc(PEOPLE[ME]["email"])}</dd>'
            f'<dt>Subject</dt><dd>{esc(subject)}</dd>{headers}</dl>')
    main = (f'<div class="page-header"><div><p class="eyebrow">Email</p><h1>{esc(subject)}</h1></div></div>'
            f'<div class="panel stack stack--lg">{meta}<p style="white-space: pre-line; margin: 0;">{body}</p></div>')
    return Page(main=main, shell="bare", main_class="container page page--form")


@me.state("default", "The plain-text match email: the listing's title and price, links to it and to My requests, and the one-click unsubscribe.")
def _():
    it = ITEMS["ballet-slippers"]
    subject = f"A match for your request: {it['title']}"
    body = (f"Hi TutuTessa,\n\nSomething you asked for was just listed: {esc(it['title'])} by {esc(it['brand'])}, {money(it['price'])}.\n"
            f"See it before it's gone:\nhttps://tutumuch.com/items/{it['id']}\n\nYour requests: https://tutumuch.com/account/requests\n\n"
            "TuTuMuch, the marketplace for all things girls.\nManage your emails: https://tutumuch.com/account/emails\n"
            "Stop item request emails in one click: https://tutumuch.com/emails/unsubscribe?token=…")
    headers = ('<dt>List-Unsubscribe</dt><dd>&lt;https://tutumuch.com/api/notifications/unsubscribe?token=…&gt;</dd>'
               '<dt>List-Unsubscribe-Post</dt><dd>List-Unsubscribe=One-Click</dd>')
    pg = email_page(subject, body, headers)
    pg.notes = note("One email per member per listing, however many of their requests it matched (L2-101). Never names the seller's email or other requesters.",
                    "top: 6rem; right: 1rem;")
    return pg
