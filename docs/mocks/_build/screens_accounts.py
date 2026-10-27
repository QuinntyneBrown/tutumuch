"""Membership, authentication, profiles and account settings (L1-001, L1-002, L1-010, L1-011, L1-014)."""
from mk import *

ME_P = PEOPLE[ME]

# ---------------------------------------------------------------------------
# Shared pieces
# ---------------------------------------------------------------------------
def auth(title: str, body: str, lead: str = "", foot: str = "", top: str = "") -> Page:
    l = f'<p class="text-muted" style="margin-top: var(--space-2);">{lead}</p>' if lead else ""
    f = f'<p class="text-sm text-muted">{foot}</p>' if foot else ""
    return Page(main=f'<div><h1>{title}</h1>{l}</div>{top}{body}{f}', shell="auth", user="guest")


def pw(fid: str, label: str, *, error: str | None = None, help: str | None = None, value: str = "", auto: str = "current-password") -> str:
    v = ' value="••••••••••••"' if value else ""
    ctl = f'<input class="input" id="{fid}" name="{fid}" type="password" autocomplete="{auto}"{v}{{db}}>'
    return field(fid, label, ctl, error=error, help=help)


def form(inner: str, submit: str, attrs: str = "") -> str:
    return f'<form class="stack stack--lg" novalidate {attrs}>{inner}<div class="stack">{submit}</div></form>'


def submit(label: str, busy: bool = False, disabled: bool = False) -> str:
    a = 'type="submit"' + (' aria-busy="true"' if busy else "") + (" disabled" if disabled else "")
    return btn(label, "primary lg block", attrs=a)


def ro(busy: bool) -> str:
    return " readonly" if busy else ""


def center(code: str, title: str, text: str, actions: str, icon: str | None = None) -> str:
    c = f'<span class="code" aria-hidden="true">{code}</span>' if code else ""
    i = f'<div class="empty__icon" style="margin: 0;">{ic(icon, "icon icon--lg")}</div>' if icon else ""
    return f'<div class="center-state">{c}{i}<h1>{title}</h1><p class="text-muted">{text}</p><div class="actions">{actions}</div></div>'


def account_page(main: str, tab: str, user: str = "member", top: str = "") -> Page:
    return Page(main=main, account=tab, user=user, top=top)


def head(title: str, sub: str = "", action: str = "") -> str:
    s = f'<p class="subtitle">{sub}</p>' if sub else ""
    return f'<div class="page-header"><div><h1>{title}</h1>{s}</div>{action}</div>'


def sk_rows(n: int = 4) -> str:
    row = ('<li><div class="settings-list__body"><span class="skeleton skeleton--text" style="width: 30%;"></span>'
           '<span class="skeleton skeleton--text" style="width: 55%;"></span></div><span class="skeleton skeleton--rect" style="width: 6rem; height: 2.5rem;"></span></li>')
    return f'<ul class="settings-list" aria-busy="true" aria-label="Loading">{row * n}</ul>'


def load_error(title: str, ref: str) -> str:
    return empty("refresh", title, f'Something went wrong on our side. <span class="ref">Ref {ref}</span>',
                 btn("Try again", "primary", icon="refresh"), cls="empty--error empty--compact")


# ---------------------------------------------------------------------------
# Sign in (L2-006, L2-007, L2-026)
# ---------------------------------------------------------------------------
s = screen("sign-in", "page", "Sign in", route="/sign-in", reqs=["L2-006", "L2-007", "L2-026", "L2-065", "L2-088", "L2-109"],
           na={"loading": "The form renders without fetching data.", "empty": "A form, not a collection."})

GOOGLE_START = P("sign-in", "google-busy")


def google_option(return_url: str = "/", busy: bool = False) -> str:
    """"Continue with Google" above an "or" divider; first in keyboard order (L2-088)."""
    return f'<div class="stack">{provider_btn("Continue with Google", GOOGLE_START, busy)}{or_divider()}</div>'


def sign_in(*, email: str = "", err: str | None = None, busy: bool = False, top: str = "", title: str = "Sign in", lead: str = "",
            google_busy: bool = False) -> Page:
    e = field("email", "Email", value=email, type="email", attrs=f'autocomplete="email"{ro(busy or google_busy)}')
    p = pw("password", "Password", value=email)
    forgot = f'<a class="text-sm" href="{P("forgot-password")}" style="align-self: flex-start;">Forgot password?</a>'
    body = google_option(busy=google_busy) + form(e + f'<div class="stack stack--sm">{p}{forgot}</div>', submit("Sign in", busy, disabled=google_busy))
    return auth(title, body, lead, f'New to TuTuMuch? <a href="{P("register")}">Join</a>', top)


@s.state("default", "Continue with Google first, then email and password.")
def _():
    pg = sign_in()
    pg.notes = note("Keyboard order: Continue with Google, email, password, Forgot password?, Sign in. The Google button only renders when a Google client is configured (L2-088).", "top: 6rem; right: 1rem;")
    return pg


@s.state("return", "Reached from Buy now as a visitor: after signing in the member returns to the listing.")
def _():
    pg = sign_in(title="Sign in to buy", lead="You’ll go straight back to the pink tulle tutu skirt.")
    pg.notes = note("Return URL /items/L-20931 is kept through sign in (L2-026).", "top: 6rem; right: 1rem;")
    return pg


@s.state("request", "Reached from “Request this item” on a showcase listing: after signing in the member is back on it with the request form open (L2-109).")
def _():
    pg = sign_in(title="Sign in to request this item", lead="You’ll go straight back to the gold sequin recital tutu and your request.")
    pg.main = pg.main.replace(f'href="{P("register")}">Join', f'href="{P("register", "request")}">Join', 1)
    pg.notes = note("URL: /sign-in?returnUrl=%2Fitems%2FL-20940%3Frequest%3D1. Join and Continue with Google keep the same returnUrl. Only a same-site path is followed; anything else goes to / (L2-109).",
                    "top: 6rem; right: 1rem;")
    return pg


@s.state("invalid", "Wrong email or password: one generic message, the email is kept.")
def _():
    return sign_in(email=ME_P["email"], top=alert("danger", "Email or password is incorrect", "Check both and try again."))


@s.state("submitting", "Signing in: button busy, fields read-only.")
def _():
    return sign_in(email=ME_P["email"], busy=True)


@s.state("locked", "Five failed attempts in 15 minutes: the account is locked and an email is sent.")
def _():
    top = alert("warning", "Too many attempts", "Try again in 15 minutes. We’ve emailed you about this.", icon="lock")
    pg = sign_in(email=ME_P["email"], top=top)
    pg.main = pg.main.replace('type="submit"', 'type="submit" disabled', 1)
    return pg


@s.state("error", "The sign-in service failed: reference code and retry, input kept.")
def _():
    top = alert("danger", "Something went wrong", 'Try again in a moment. <span class="ref">Ref 5d1c-0a77</span>')
    return sign_in(email=ME_P["email"], top=top)


@s.state("google-busy", "Continue with Google chosen: the button is busy while the browser leaves for Google; the form waits.")
def _():
    pg = sign_in(google_busy=True)
    pg.notes = note("The browser is navigating to /api/auth/google/start; Google's consent page opens next. Nothing from Google runs in this page (L2-088).", "top: 6rem; right: 1rem;")
    return pg


@s.state("google-cancelled", "Back from Google without consent: a calm notice, no session, both ways to sign in still offered.")
def _():
    top = alert("info", "Google sign-in was cancelled", "Nothing was changed. Try again, or sign in with your email and password.", role="status")
    return sign_in(top=top)


@s.state("google-existing", "Google's email matches a member whose email is not verified: refused, so an account registered with someone else's email cannot be taken over.")
def _():
    top = alert("warning", "An account with this email already exists",
                "Sign in with your password and verify your email first. After that, Google will sign you in to the same account.", icon="lock")
    pg = sign_in(email=ME_P["email"], top=top)
    pg.notes = note("A verified matching email links and signs in instead (L2-088 AC5, AC6).", "top: 6rem; right: 1rem;")
    return pg


@s.state("google-failed", "Google answered with something TuTuMuch could not accept: reference code, no session.")
def _():
    top = alert("danger", "Couldn’t sign in with Google", 'Try again in a moment, or sign in with your email and password. <span class="ref">Ref 7e3b-41c9</span>')
    return sign_in(top=top)


# ---------------------------------------------------------------------------
# Register (L2-001)
# ---------------------------------------------------------------------------
r = screen("register", "page", "Join", route="/join", reqs=["L2-001", "L2-002", "L2-051", "L2-065", "L2-088", "L2-109"],
           na={"loading": "The form renders without fetching data.", "empty": "A form, not a collection."})


def register(*, filled: bool = False, errs: dict | None = None, busy: bool = False, top: str = "") -> Page:
    errs = errs or {}
    v = (lambda x: x) if filled else (lambda x: "")
    name = field("display-name", "Display name", value=v("TutuTessa"), error=errs.get("display-name"),
                 help="3–30 letters, numbers, _ or -. Shown publicly.", attrs=f'autocomplete="username" autofocus{ro(busy)}')
    email = field("email", "Email", value=v(ME_P["email"]), type="email", error=errs.get("email"), attrs=f'autocomplete="email"{ro(busy)}')
    pwd = pw("password", "Password", error=errs.get("password"), help="At least 12 characters.", value="x" if filled else "", auto="new-password")

    def tick(fid, label):
        e = errs.get(fid)
        chk = "" if e or not filled else " checked"
        inv = f' aria-invalid="true" aria-describedby="{fid}-error"' if e else ""
        msg = f'<span class="field__error" id="{fid}-error">{e}</span>' if e else ""
        return f'<div class="field"><label class="choice"><input type="checkbox" id="{fid}"{chk}{inv}><span>{label}</span></label>{msg}</div>'
    ticks = tick("age", "I’m 18 or older") + tick("terms", f'I accept the <a href="{P("terms")}">terms</a> and <a href="{P("rules")}">marketplace rules</a>')
    body = google_option() + form(name + email + pwd + f'<div class="stack stack--sm">{ticks}</div>', submit("Join TuTuMuch", busy))
    return auth("Join TuTuMuch", body, "Buy and sell with one account.", f'Already a member? <a href="{P("sign-in")}">Sign in</a>', top)


@r.state("default", "Continue with Google, or join with display name, email and password; 18+ and terms ticks.")
def _():
    pg = register()
    pg.notes = note("A Google join skips the password and the verification email: Google's verified email counts (L2-088). The display name, 18+ and terms are still asked on /join/google.", "top: 6rem; right: 1rem;")
    return pg


@r.state("invalid", "Every error at once: taken name, existing email, short password, unticked boxes; summary first.")
def _():
    errs = {"display-name": "That display name is taken", "email": "An account with this email already exists",
            "password": "Password must be at least 12 characters", "age": "Confirm you’re 18 or older", "terms": "Accept the terms to continue"}
    summ = error_summary([(k, v) for k, v in errs.items()])
    return register(filled=True, errs=errs, top=summ)


@r.state("submitting", "Creating the account: button busy, fields read-only.")
def _():
    return register(filled=True, busy=True)


@r.state("success", "Account created: check your email for the verification link.")
def _():
    body = (f'<div class="empty__icon" style="margin: 0;">{ic("mail", "icon icon--lg")}</div>'
            f'<p>We sent a link to <strong>{ME_P["email"]}</strong>. It works for 24 hours.</p>'
            f'<div class="stack">{btn("Shop the closet while you wait", "primary lg block", P("catalog"))}'
            f'<button class="btn btn--link" type="button">Resend email</button></div>')
    return auth("Check your email", body)


@r.state("request", "Reached from “Sign in to request this item”: the same form, a lead that says where the member goes next (L2-109).")
def _():
    pg = register()
    pg.main = pg.main.replace("Buy and sell with one account.", "Join, verify your email, then sign in and we’ll take you back to your request.", 1)
    pg.main = pg.main.replace(f'href="{P("sign-in")}">Sign in', f'href="{P("sign-in", "request")}">Sign in', 1)
    pg.notes = note("URL: /join?returnUrl=%2Fitems%2FL-20940%3Frequest%3D1. Continue with Google carries the returnUrl and lands straight on the request form.", "top: 6rem; right: 1rem;")
    return pg


@r.state("request-success", "Joined from a request: the verification email is on its way and the next step is signing in to send the request (L2-109).")
def _():
    body = (f'<div class="empty__icon" style="margin: 0;">{ic("mail", "icon icon--lg")}</div>'
            f'<p>We sent a link to <strong>{ME_P["email"]}</strong>. It works for 24 hours. You can send your request now and verify later.</p>'
            f'<div class="stack">{btn("Sign in to send your request", "primary lg block", P("sign-in", "request"))}'
            f'<button class="btn btn--link" type="button">Resend email</button></div>')
    return auth("Check your email", body)


@r.state("error", "Registration failed on the server: input kept, retry with a reference.")
def _():
    return register(filled=True, top=alert("danger", "Couldn’t create your account", 'Try again in a moment. <span class="ref">Ref 0c4e-7b21</span>'))


# ---------------------------------------------------------------------------
# Finish joining with Google (L2-088)
# ---------------------------------------------------------------------------
jg = screen("join-google", "page", "Finish joining with Google", route="/join/google", reqs=["L2-088", "L2-001", "L2-065"],
            na={"empty": "A form, not a collection."})


def join_google(*, errs: dict | None = None, busy: bool = False, top: str = "", filled: bool = False) -> Page:
    errs = errs or {}
    email = field("email", "Google email", value=ME_P["email"], type="email", attrs='readonly aria-describedby="email-help"',
                  help="From your Google account. It counts as verified.")
    name = field("display-name", "Display name", value="TutuTessa" if filled else "", error=errs.get("display-name"),
                 help="3–30 letters, numbers, _ or -. Shown publicly.", attrs=f'autocomplete="username" autofocus{ro(busy)}')

    def tick(fid, label):
        e = errs.get(fid)
        chk = "" if e or not filled else " checked"
        inv = f' aria-invalid="true" aria-describedby="{fid}-error"' if e else ""
        msg = f'<span class="field__error" id="{fid}-error">{e}</span>' if e else ""
        return f'<div class="field"><label class="choice"><input type="checkbox" id="{fid}"{chk}{inv}><span>{label}</span></label>{msg}</div>'
    ticks = tick("age", "I’m 18 or older") + tick("terms", f'I accept the <a href="{P("terms")}">terms</a> and <a href="{P("rules")}">marketplace rules</a>')
    body = form(email + name + f'<div class="stack stack--sm">{ticks}</div>', submit("Join TuTuMuch", busy))
    foot = f'Not you? <a href="{P("sign-in")}">Back to sign in</a>'
    return auth("Almost there", body, "Pick a display name to finish joining with Google.", foot, top)


@jg.state("loading", "Reading the pending Google sign-in: skeleton in the form's shape.")
def _():
    body = ('<div class="stack stack--lg" aria-busy="true" aria-label="Loading">'
            '<div><span class="skeleton skeleton--text" style="width: 30%;"></span><span class="skeleton skeleton--rect" style="height: 2.75rem;"></span></div>'
            '<div><span class="skeleton skeleton--text" style="width: 35%;"></span><span class="skeleton skeleton--rect" style="height: 2.75rem;"></span></div>'
            '<span class="skeleton skeleton--text" style="width: 40%;"></span><span class="skeleton skeleton--text" style="width: 70%;"></span>'
            '<span class="skeleton skeleton--rect" style="height: 3rem;"></span></div>')
    return auth("Almost there", body, "Pick a display name to finish joining with Google.")


@jg.state("default", "Email read-only from Google; display name, 18+ and terms to fill; focus on the display name.")
def _():
    pg = join_google()
    pg.notes = note("The pending Google sign-in is held server-side for 10 minutes and used once; this page never sees a Google token (L2-088).", "top: 6rem; right: 1rem;")
    return pg


@jg.state("invalid", "Taken display name and unticked boxes: summary first, then inline.")
def _():
    errs = {"display-name": "That display name is taken", "age": "Confirm you’re 18 or older", "terms": "Accept the terms to continue"}
    return join_google(filled=True, errs=errs, top=error_summary([(k, v) for k, v in errs.items()]))


@jg.state("submitting", "Creating the account: button busy, fields read-only.")
def _():
    return join_google(filled=True, busy=True)


@jg.state("expired", "The pending Google sign-in timed out or was already used: start again from sign in.")
def _():
    body = f'<div class="stack">{btn("Continue with Google again", "primary lg block", P("sign-in"))}{btn("Back to sign in", "lg block", P("sign-in"))}</div>'
    return auth("That took a little too long", body, "Google sign-ins have to be finished within 10 minutes. Nothing was created.")


@jg.state("error", "Finishing failed on the server: input kept, retry with a reference.")
def _():
    return join_google(filled=True, top=alert("danger", "Couldn’t create your account", 'Try again in a moment. <span class="ref">Ref 2b7f-90d4</span>'))


# ---------------------------------------------------------------------------
# Verify email (L2-002)
# ---------------------------------------------------------------------------
v = screen("verify-email", "page", "Verify email", route="/verify", reqs=["L2-002"],
           na={"empty": "A single confirmation, not a collection."})


@v.state("default", "Link opened within 24 hours: email verified, ready to buy and sell.")
def _():
    body = f'<div class="stack">{btn("Start shopping", "primary lg block", P("catalog"))}{btn("Sell outgrown gear", "lg block", P("sell"))}</div>'
    return auth("You’re verified", body, "Buying and selling are open.")


@v.state("loading", "Checking the link.")
def _():
    body = f'<div class="cluster">{mark()}<span class="text-muted" role="status">Checking your link…</span></div>'
    return auth("Verifying", body)


@v.state("expired", "Link older than 24 hours: expired, resend a new one.")
def _():
    body = f'<div class="stack">{btn("Send a new link", "primary lg block")}</div>'
    return auth("This link has expired", body, "Links work for 24 hours. A new one replaces it.")


@v.state("used", "Link already used: nothing changes, sign in instead.")
def _():
    body = f'<div class="stack">{btn("Sign in", "primary lg block", P("sign-in"))}</div>'
    return auth("Link already used", body, "This email is already verified.")


@v.state("error", "Verification failed on the server: retry.")
def _():
    body = f'<span class="ref">Ref 9a02-c3d8</span><div class="stack">{btn("Try again", "primary lg block", icon="refresh")}</div>'
    return auth("Something went wrong", body, "We couldn’t check your link.")


# ---------------------------------------------------------------------------
# Forgot / reset password (L2-009)
# ---------------------------------------------------------------------------
fp = screen("forgot-password", "page", "Forgot password", route="/forgot-password", reqs=["L2-009"],
            na={"loading": "The form renders without fetching data.", "empty": "A form, not a collection."})


def forgot(*, email: str = "", err: str | None = None, busy: bool = False, top: str = "") -> Page:
    e = field("email", "Email", value=email, type="email", error=err, attrs=f'autocomplete="email" autofocus{ro(busy)}')
    return auth("Reset your password", form(e, submit("Send reset link", busy)), "We’ll email you a link that works for 1 hour.",
                f'<a href="{P("sign-in")}">Back to sign in</a>', top)


@fp.state("default", "Request a reset link by email.")
def _():
    return forgot()


@fp.state("invalid", "Malformed email: inline error.")
def _():
    return forgot(email="tessa.bell@", err="Enter an email like name@example.com")


@fp.state("submitting", "Sending: button busy.")
def _():
    return forgot(email=ME_P["email"], busy=True)


@fp.state("success", "Same message whether or not the email is registered.")
def _():
    body = f'<div class="empty__icon" style="margin: 0;">{ic("mail", "icon icon--lg")}</div><div class="stack">{btn("Back to sign in", "lg block", P("sign-in"))}</div>'
    return auth("Check your email", body, "If an account exists for this email, we’ve sent a link.")


@fp.state("error", "Request failed: retry with reference, email kept.")
def _():
    return forgot(email=ME_P["email"], top=alert("danger", "Couldn’t send the link", 'Try again in a moment. <span class="ref">Ref 33f0-1e9b</span>'))


rp = screen("reset-password", "page", "Reset password", route="/reset-password", reqs=["L2-009", "L2-001"],
            na={"loading": "The token is checked when the form is submitted.", "empty": "A form, not a collection."})


def reset(*, err: str | None = None, busy: bool = False, top: str = "", filled: bool = False) -> Page:
    a = pw("new-password", "New password", error=err, help="At least 12 characters.", value="x" if filled else "", auto="new-password")
    b = pw("confirm-password", "Confirm password", value="x" if filled else "", auto="new-password")
    return auth("Choose a new password", form(a + b, submit("Save password", busy)), "You’ll be signed out on every device.", top=top)


@rp.state("default", "Set a new password from the emailed link.")
def _():
    return reset()


@rp.state("invalid", "Password on the breached list: inline error.")
def _():
    return reset(err="This password has appeared in a data breach. Choose another.", filled=True)


@rp.state("submitting", "Saving: button busy.")
def _():
    return reset(busy=True, filled=True)


@rp.state("success", "Password changed, all sessions revoked, confirmation emailed.")
def _():
    return auth("Password saved", f'<div class="stack">{btn("Sign in", "primary lg block", P("sign-in"))}</div>', "You’re signed out everywhere. Sign in with your new password.")


@rp.state("expired", "Link older than 1 hour or already used.")
def _():
    return auth("This link has expired", f'<div class="stack">{btn("Request a new link", "primary lg block", P("forgot-password"))}</div>', "Reset links work once, for 1 hour.")


@rp.state("error", "Save failed: retry with a reference.")
def _():
    return reset(filled=True, top=alert("danger", "Couldn’t save your password", 'Try again in a moment. <span class="ref">Ref 6b7d-2a40</span>'))


# ---------------------------------------------------------------------------
# Session expired (L2-008)
# ---------------------------------------------------------------------------
se = screen("session-expired", "page", "Session expired", route="/sign-in?expired=1", reqs=["L2-008"],
            na={"loading": "Static message; no data is fetched.", "empty": "Not a collection.", "error": "This page is itself the recovery path for an ended session."})


@se.state("default", "No activity for 14 days: sign in again, then return where you were.")
def _():
    body = f'<div class="stack">{btn("Sign in", "primary lg block", P("sign-in"))}</div>'
    return auth("Welcome back", body, "For your security, sign in again after 14 days away.")


# ---------------------------------------------------------------------------
# Account overview
# ---------------------------------------------------------------------------
ac = screen("account", "page", "Account", route="/account", reqs=["L2-003", "L2-002"])


def stat(value: str, label: str, href: str) -> str:
    return (f'<a class="panel stat" href="{href}" style="text-decoration: none; color: inherit;">'
            f'<span class="stat__value">{value}</span><span class="stat__label">{label}</span></a>')


def overview(stats: str, top: str = "", user: str = "member", extra: str = "") -> Page:
    h = head(f'Hi, {ME}', f'Member since {ME_P["since"]}', btn("Sell outgrown gear", "primary", P("sell"), icon="plus") if user == "member" else "")
    grid = f'<div class="grid grid--auto" style="--layout-gutter: var(--space-4);">{stats}</div>' if stats else ""
    return account_page(h + extra + grid, "overview", user, top)


@ac.state("default", "Three numbers that matter: listings live, sales to ship, purchases on the way.")
def _():
    return overview(stat("3", "Active listings", P("my-listings")) + stat("1", "To ship by Fri 30 Oct", P("sales")) + stat("1", "On the way", P("purchases")))


@ac.state("loading", "Overview skeleton.")
def _():
    sk = '<div class="panel stat" aria-hidden="true"><span class="skeleton skeleton--title" style="width: 3rem;"></span><span class="skeleton skeleton--text" style="width: 60%;"></span></div>'
    return overview(sk * 3)


@ac.state("empty", "New member, nothing yet: one nudge to list or shop.")
def _():
    e = empty("tag", "Nothing here yet", "Pass on something she’s outgrown, and it shows up here.",
              btn("Sell outgrown gear", "primary", P("sell")))
    pg = overview("", extra=e)
    pg.main = pg.main.replace(btn("Sell outgrown gear", "primary", P("sell"), icon="plus"), "")
    return pg


@ac.state("error", "Overview failed to load; tabs still work.")
def _():
    pg = overview("", extra=load_error("Couldn’t load your account", "1f8e-77c0"))
    pg.main = pg.main.replace(btn("Sell outgrown gear", "primary", P("sell"), icon="plus"), "")  # Try again is the one primary
    return pg


@ac.state("unverified", "Unverified member: verify to buy and sell.")
def _():
    a = alert("warning", "Verify your email to buy and sell", f'We sent a link to {ME_P["email"]}.', btn("Resend link", "sm"), icon="mail")
    return overview("", extra=a, user="unverified")


# ---------------------------------------------------------------------------
# Public profile (L2-041, L2-040)
# ---------------------------------------------------------------------------
pr = screen("profile", "page", "Member profile", route="/members/{displayName}", reqs=["L2-041", "L2-040", "L2-054"])

REVIEWS = {
    "TwirlyTara": [("TutuTessa", 5, "Fri 9 Oct", "Shipped next day, exactly as pictured.", "Thanks Tessa, hope she loves it."),
                 ("PuddleJumperPia", 5, "Mon 28 Sep", "Packed with tissue paper. Would buy again.", None),
                 ("HandMeDownHana", 4, "Tue 15 Sep", "Lovely dress, took a week to arrive.", None)],
    "TutuTessa": [("PuddleJumperPia", 5, "Mon 5 Oct", "Shorts were spotless. Fast shipping.", None),
                  ("RainbowRosa", 4, "Thu 17 Sep", "Good seller, a little slow to reply.", None)],
}


def review_html(author, stars_n, date, text, reply, reply_btn=False) -> str:
    rp_ = f'<div class="review__reply"><strong>Reply:</strong> {esc(reply)}</div>' if reply else ""
    rb = ""
    return (f'<article class="review"><div class="review__head">{avatar(author, "avatar--sm")}<a href="{P("profile")}">{author}</a>'
            f'{rating(float(stars_n), 1, short=True).replace("(1)", "")}<span class="text-caption"><time>{date}</time></span></div>'
            f'<p>{esc(text)}</p>{rp_}{rb}</article>')


def profile(handle: str, keys, *, own: bool = False, reviews=None, listings_count: int | None = None, more: str = "") -> Page:
    p = PEOPLE[handle]
    bio = f'<p class="text-muted">{esc(p["bio"])}</p>' if p["bio"] else ""
    if own:
        act = btn("Edit profile", href=P("edit-profile"), icon="edit")
    else:
        act = ""
    sales = p["sales"]
    stats = (f'<div class="stats"><div class="stat"><span class="stat__value">{sales}</span><span class="stat__label">Sales</span></div>'
             f'<div class="stat"><span class="stat__value">{p["since"]}</span><span class="stat__label">Member since</span></div></div>')
    headh = (f'<section class="profile-head">{avatar(handle, "avatar--xl")}<div class="profile-head__body"><h1>{handle}</h1>'
             f'<span class="text-muted" style="display: inline-flex; gap: var(--space-1); align-items: center;">{ic("map-pin", "icon icon--sm")}{p["city"]}</span>{rating(handle)}{bio}</div>'
             f'<div class="stack" style="align-items: flex-end;">{act}</div></section>{stats}')
    n = listings_count if listings_count is not None else len(keys)
    if keys:
        lst = listing_grid(keys) + more
    else:
        lst = empty("tag", "No items for sale", f"{handle} hasn’t listed anything yet.", cls="empty--compact", heading="h3")
    reviews = reviews if reviews is not None else REVIEWS.get(handle, [])
    if reviews:
        rv = "".join(review_html(*r, reply_btn=own) for r in reviews)
    else:
        rv = '<p class="text-muted">No reviews yet</p>'
    main = (f'{headh}<section class="section" style="margin-top: var(--space-16);" aria-labelledby="l-h"><div class="section-head"><h2 id="l-h">For sale</h2><span class="text-muted text-sm">{n}</span></div>{lst}</section>'
            f'<section class="section" style="margin-top: var(--space-16); max-width: var(--layout-measure);" aria-labelledby="r-h"><div class="section-head"><h2 id="r-h">Reviews</h2><span class="text-muted text-sm">{p["reviews"]}</span></div>{rv}</section>')
    return Page(main=main)


TARA = ["pink-tutu", "velvet-dress", "pleated-skirt", "denim-leggings"]


@pr.state("default", "TwirlyTara’s public profile: rating, sales, items for sale and reviews; no contact details.")
def _():
    pg = profile("TwirlyTara", TARA)
    pg.notes = note("No email, phone or street address appears on the page or in its API response (L2-054).", "top: 6rem; right: 1rem;")
    return pg


@pr.state("own", "Tessa viewing her own profile: Edit profile.")
def _():
    return profile("TutuTessa", ["sundress", "heart-hoodie"], own=True)


@pr.state("empty", "Member with nothing for sale and only buyer reviews (PuddleJumperPia).")
def _():
    return profile("PuddleJumperPia", [], reviews=[("TutuTessa", 5, "Wed 7 Oct", "Paid fast, easy to deal with.", None),
                                            ("TutuTessa", 5, "Sat 3 Oct", "Smooth sale.", None)])


@pr.state("loading", "Profile skeleton.")
def _():
    main = ('<section class="profile-head" aria-busy="true" aria-label="Loading profile"><span class="skeleton avatar avatar--xl"></span>'
            '<div class="profile-head__body"><span class="skeleton skeleton--title" style="width: 12rem;"></span><span class="skeleton skeleton--text" style="width: 8rem;"></span>'
            '<span class="skeleton skeleton--text" style="width: 10rem;"></span></div></section>'
            '<h1 class="visually-hidden">Loading profile</h1>' + skeleton_grid(4))
    return Page(main=main)


@pr.state("error", "Profile failed to load: retry.")
def _():
    return Page(main=center("", "Couldn’t load this profile", 'It’s us, not you. <span class="ref">Ref 4c11-8e02</span>',
                            btn("Try again", "primary", icon="refresh") + btn("Shop the closet", href=P("catalog"))))


@pr.state("unavailable", "Closed member: “This member isn’t available” (HTTP 404).")
def _():
    return Page(main=center('4<em>0</em>4', "This member isn’t available", "The account may have been closed.",
                            btn("Shop the closet", "primary", P("catalog"))))


@pr.state("long", "Busy seller: listings paged 24 at a time.")
def _():
    keys = TARA + ["rainbow-romper", "party-dress", "unicorn-tee", "rainbow-sweater"]
    more = ('<div class="load-more"><span class="text-sm text-muted">Showing 24 of 141</span>'
            + btn("Load more") + '</div>')
    pg = profile("TwirlyTara", keys, listings_count=141, more=more)
    pg.notes = note("The mock shows 8 cards; pages hold 24.", "top: 30rem; right: 1rem;")
    return pg


# ---------------------------------------------------------------------------
# Edit profile (L2-004)
# ---------------------------------------------------------------------------
ep = screen("edit-profile", "page", "Edit profile", route="/account/profile", reqs=["L2-004", "L2-017"],
            na={"empty": "A profile always has a display name."})


def edit_profile(*, errs: dict | None = None, busy: bool = False, top: str = "", bio: str | None = None) -> Page:
    errs = errs or {}
    bio = ME_P["bio"] if bio is None else bio
    av = (f'<div class="cluster" style="gap: var(--space-6);">{avatar(ME, "avatar--xl")}<div class="stack stack--sm">'
          f'{btn("Change photo", icon="camera")}<span class="field__help">JPEG, PNG or WebP, up to 10 MB.</span></div></div>')
    name = field("display-name", "Display name", value=errs.get("_name", ME), error=errs.get("display-name"), attrs=ro(busy))
    count = len(bio)
    btxt = field("bio", "Bio", f'<textarea class="textarea" id="bio" rows="4"{{db}}{ro(busy)}>{esc(bio)}</textarea>', optional=True,
                 error=errs.get("bio"), counter=f"{count} / 500")
    city = field("city", "City", value="Brooklyn", attrs=ro(busy), optional=True)
    region = field("region", "State", f'<select class="select" id="region"{{db}}><option>New York</option></select>', optional=True)
    loc = f'<fieldset class="stack"><legend>Public location</legend><div class="form-row">{city}{region}</div><span class="field__help">Only city and state are shown.</span></fieldset>'
    fm = (f'<form class="stack stack--lg" novalidate>{av}{name}{btxt}{loc}'
          f'<div class="form-actions" style="justify-content: flex-start;">{btn("Save profile", "primary", attrs="type=\"submit\"" + (" aria-busy=\"true\"" if busy else ""))}'
          f'{btn("View public profile", "ghost", P("profile", "own"))}</div></form>')
    return account_page(head("Profile") + top + f'<div class="page--form">{fm}</div>', "profile")


@ep.state("default", "Edit display name, photo, bio and city; no street address.")
def _():
    return edit_profile()


@ep.state("invalid", "Name taken and bio over 500 characters: summary plus inline errors.")
def _():
    long_bio = ("Two girls, one size apart. Mostly dresses, the odd raincoat. " * 9)[:501]
    errs = {"_name": "TWIRLYTARA", "display-name": "That display name is taken", "bio": "Bio must be 500 characters or fewer"}
    return edit_profile(errs=errs, bio=long_bio, top=error_summary([("display-name", errs["display-name"]), ("bio", errs["bio"])]))


@ep.state("submitting", "Saving: button busy, fields read-only.")
def _():
    return edit_profile(busy=True)


@ep.state("success", "Saved: inline confirmation with a link to the public profile.")
def _():
    return edit_profile(top=alert("success", "Profile saved", "", f'<a href="{P("profile", "own")}">View public profile</a>', role="status") + '<div style="height: var(--space-8);"></div>')


@ep.state("loading", "Form skeleton.")
def _():
    sk = ('<div class="page--form stack stack--lg" aria-busy="true" aria-label="Loading profile"><span class="skeleton avatar avatar--xl"></span>'
          + '<span class="skeleton skeleton--rect" style="height: 2.5rem;"></span>' * 2 + '<span class="skeleton skeleton--rect" style="height: 7rem;"></span></div>')
    return account_page(head("Profile") + sk, "profile")


@ep.state("error", "Profile failed to load: retry.")
def _():
    return account_page(head("Profile") + load_error("Couldn’t load your profile", "a7c3-55e1"), "profile")


# ---------------------------------------------------------------------------
# Settings (L2-005, L2-008, L2-056)
# ---------------------------------------------------------------------------
st = screen("settings", "page", "Settings", route="/account/settings", reqs=["L2-005", "L2-008", "L2-009", "L2-056"],
            na={"empty": "Every member has an email, password and session."})


def row(title: str, meta: str, action: str) -> str:
    return f'<li><div class="settings-list__body"><span class="settings-list__title">{title}</span><span class="settings-list__meta">{meta}</span></div>{action}</li>'


def settings_rows() -> str:
    rows = [
        row("Email", ME_P["email"], ""),
        row("Password", "Changed Tue 2 Jun", btn("Change", href=D("change-password"))),
        row("Sessions", "Signed in on 2 devices", btn("Sign out everywhere", href=D("sign-out-everywhere"))),
        row("Cookies", "Strictly necessary only", btn("Cookie settings", href=D("cookie-settings"))),
        row("Close account", "Removes your listings and erases your data within 30 days", btn("Close account", "ghost", D("close-account"), attrs='style="color: var(--color-danger-fg);"')),
    ]
    return f'<ul class="settings-list page--form">{"".join(rows)}</ul>'


@st.state("default", "Account settings as a short list: email, password, sessions, cookies, close account.")
def _():
    return account_page(head("Settings") + settings_rows(), "settings")


@st.state("loading", "Settings rows as skeletons.")
def _():
    return account_page(head("Settings") + f'<div class="page--form">{sk_rows(5)}</div>', "settings")


@st.state("error", "Settings failed to load: retry.")
def _():
    return account_page(head("Settings") + load_error("Couldn’t load settings", "e210-4f9a"), "settings")


# ---------------------------------------------------------------------------
# Email notification settings (L2-043)
# ---------------------------------------------------------------------------
ns = screen("notification-settings", "page", "Email settings", route="/account/emails", reqs=["L2-042", "L2-043", "L2-103"],
            na={"empty": "The list of email types is fixed."})


def sw(fid: str, checked: bool, disabled: bool = False, label: str = "") -> str:
    return (f'<label class="switch"><input type="checkbox" role="switch" id="{fid}"{" checked" if checked else ""}{" disabled" if disabled else ""}>'
            f'<span class="visually-hidden">{label}</span></label>')


def emails(*, reviews: bool = True, marketing: bool = False, item_requests: bool = True, busy: bool = False, top: str = "") -> Page:
    rows = [
        row("Account security", "Sign-in alerts, password resets", sw("sec", True, True, "Account security emails, always on")),
        row("Orders", "Sales, shipping, delivery, refunds", sw("ord", True, True, "Order emails, always on")),
        row("Item requests", "When something you asked for is listed", sw("req", item_requests, busy, "Item request emails")),
        row("Reviews", "When someone reviews you", sw("rev", reviews, busy, "Review emails")),
        row("News from TuTuMuch", "New arrivals for girls, now and then", sw("mkt", marketing, busy, "Marketing emails")),
    ]
    save = btn("Save", "primary", attrs='type="submit"' + (' aria-busy="true"' if busy else ""))
    main = head("Emails", "Security and order emails are always on.") + top + f'<form class="page--form"><ul class="settings-list">{"".join(rows)}</ul><div class="form-actions" style="justify-content: flex-start;">{save}</div></form>'
    return account_page(main, "notifications")


@ns.state("default", "Security and order emails locked on; item requests and reviews on; marketing off until opted in.")
def _():
    return emails()


@ns.state("submitting", "Saving after turning reviews off.")
def _():
    return emails(reviews=False, busy=True)


@ns.state("success", "Saved: reviews off.")
def _():
    return emails(reviews=False, top=alert("success", "Email settings saved", role="status") + '<div style="height: var(--space-6);"></div>')


@ns.state("loading", "Rows as skeletons.")
def _():
    return account_page(head("Emails") + f'<div class="page--form">{sk_rows(5)}</div>', "notifications")


@ns.state("error", "Settings failed to load: retry.")
def _():
    return account_page(head("Emails") + load_error("Couldn’t load email settings", "77ab-0d13"), "notifications")


# ---------------------------------------------------------------------------
# Dialogs
# ---------------------------------------------------------------------------
def cancel(label: str = "Cancel", disabled: bool = False) -> str:
    return btn(label, attrs="disabled" if disabled else "")


def failed(title: str, ref: str) -> str:
    return alert("danger", title, f'Try again. <span class="ref">Ref {ref}</span>')


vp = screen("verify-email-prompt", "dialog", "Verify email prompt", opens_from="listing-detail", reqs=["L2-002", "L2-003"],
            na={"invalid": "No inputs; the email is the account’s."})


def verify_prompt(footer: str, top: str = "", desc: str | None = None) -> Dialog:
    desc = desc or f'Open the link we sent to <strong>{ME_P["email"]}</strong> to buy and sell.'
    return Dialog(bg=("listing-detail", "default"), html=dialog("Verify your email first", "", footer, desc=desc, icon="mail", top=top))


@vp.state("default", "Unverified member chose Buy now: verify first, resend available.")
def _():
    return verify_prompt(cancel("Not now") + btn("Resend link", "primary", attrs="autofocus"))


@vp.state("busy", "Resending: button busy.")
def _():
    return verify_prompt(cancel("Not now") + btn("Resend link", "primary", attrs='aria-busy="true"'))


@vp.state("sent", "New link sent; earlier links stop working.")
def _():
    return verify_prompt(btn("Done", "primary", attrs="autofocus"),
                         top=alert("success", "Link sent", "Earlier links no longer work.", role="status"))


@vp.state("limited", "Fourth request within an hour: refused (HTTP 429).")
def _():
    return verify_prompt(btn("Done", "primary", attrs="autofocus"),
                         top=alert("warning", "Too many requests", "You can request 3 links an hour. Try again later.", icon="clock"))


cp = screen("change-password", "dialog", "Change password", opens_from="settings", reqs=["L2-009", "L2-001", "L2-008"])


def change_pw(*, err: dict | None = None, busy: bool = False, top: str = "", filled: bool = False) -> Dialog:
    err = err or {}
    body = (pw("current", "Current password", error=err.get("current"), value="x" if filled else "")
            + pw("new", "New password", error=err.get("new"), help="At least 12 characters.", value="x" if filled else "", auto="new-password"))
    foot = cancel(disabled=busy) + btn("Change password", "primary", attrs='type="submit"' + (' aria-busy="true"' if busy else ""))
    return Dialog(bg=("settings", "default"), html=dialog("Change password", body, foot, desc="Other devices will be signed out.", top=top))


@cp.state("default", "Current and new password.")
def _():
    return change_pw()


@cp.state("invalid", "Wrong current password and a too-short new one.")
def _():
    return change_pw(err={"current": "That’s not your current password", "new": "Password must be at least 12 characters"}, filled=True)


@cp.state("busy", "Saving: button busy, cancel disabled.")
def _():
    return change_pw(busy=True, filled=True)


@cp.state("failed", "Server error: inputs kept, retry.")
def _():
    return change_pw(filled=True, top=failed("Couldn’t change your password", "c0d2-9911"))


so = screen("sign-out-everywhere", "dialog", "Sign out everywhere", opens_from="settings", reqs=["L2-008"],
            na={"invalid": "Confirmation only; no inputs."})


def signout(footer: str, top: str = "") -> Dialog:
    return Dialog(bg=("settings", "default"), html=dialog("Sign out everywhere?", "", footer, desc="Both devices, including this one, will need to sign in again.", icon="log-out", top=top))


@so.state("default", "Confirm signing out of every device.")
def _():
    return signout(cancel() + btn("Sign out everywhere", "primary"))


@so.state("busy", "Revoking sessions.")
def _():
    return signout(cancel(disabled=True) + btn("Sign out everywhere", "primary", attrs='aria-busy="true"'))


@so.state("failed", "Revocation failed: retry.")
def _():
    return signout(cancel() + btn("Try again", "primary"), failed("Couldn’t sign you out", "8e44-1b7c"))


ca = screen("close-account", "dialog", "Close account", opens_from="settings", reqs=["L2-005"])


def close_acc(*, err: str | None = None, busy: bool = False, top: str = "", blocked: bool = False) -> Dialog:
    if blocked:
        orders = "".join(f'<li><a href="{P("order-detail" if o == "TM-48213" else "sale-detail")}">{o}</a> · {status(ORDERS[o]["status"])}</li>' for o in ["TM-48213", "TM-48302"])
        body = f'<ul class="stack stack--sm" style="padding-left: var(--space-5);">{orders}</ul>'
        foot = btn("Close", "primary", attrs="autofocus")
        top = alert("warning", "Finish your open orders first", "You can close your account once these are completed, cancelled or refunded.")
        return Dialog(bg=("settings", "default"), html=dialog("Close your account?", body, foot, icon="trash", icon_kind="danger", top=top))
    body = pw("confirm-pw", "Enter your password to confirm", error=err, value="x" if (err or busy) else "")
    foot = cancel(disabled=busy) + btn("Close account", "danger", attrs='type="submit"' + (' aria-busy="true"' if busy else ""))
    return Dialog(bg=("settings", "default"), html=dialog("Close your account?", body, foot, icon="trash", icon_kind="danger", top=top,
                                                          desc="Your 2 listings come down now. Your data is erased within 30 days. This can’t be undone."))


@ca.state("default", "Re-enter password; red button, never the default focus.")
def _():
    return close_acc()


@ca.state("invalid", "Wrong password.")
def _():
    return close_acc(err="That password isn’t right")


@ca.state("busy", "Closing.")
def _():
    return close_acc(busy=True)


@ca.state("blocked", "Open orders block closure and are listed.")
def _():
    return close_acc(blocked=True)


# ---------------------------------------------------------------------------
# Account banner (L2-002)
# ---------------------------------------------------------------------------
ab = screen("account-banner", "notification", "Account banner", opens_from="catalog", reqs=["L2-002"],
            na={"danger": "Account suspension is deferred (see docs/adr/general/0001-defer-moderation-reviews-and-data-export.md); no danger banner exists."})


@ab.state("info", "Verification email sent after joining.")
def _():
    return Note(bg=("catalog", "default"), top=banner("info", f'We sent a verification link to {ME_P["email"]}.', '<button type="button">Resend</button>', icon="mail"))


@ab.state("success", "Email verified.")
def _():
    return Note(bg=("catalog", "default"), top=banner("success", "Email verified. Buying and selling are open."))


@ab.state("warning", "Persistent until verified: verify to buy and sell.")
def _():
    n = Note(bg=("catalog", "default"), top=banner("warning", "Verify your email to buy and sell.", '<button type="button">Resend link</button>', dismiss=False, icon="mail"))
    n.notes = note("Stays on every page until the email is verified; cannot be dismissed.", "top: 8rem; right: 1rem;")
    return n
