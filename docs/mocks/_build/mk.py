"""Mock generator core for TuTuMuch.

Screen modules (screens_*.py) declare screens and their states; build.py
renders every state to docs/mocks/<kind>s/<id>/<state>.html, writes
manifest.json, and validates internal links. All mocks share one shell, one
token file and one component kit, so a layout change happens here once.

    from mk import *
    s = screen("catalog", "page", "Catalog", route="/", reqs=["L2-022"])

    @s.state("default", "Catalog with 24 listings, newest first.")
    def _():
        return Page(main="...", nav="shop")

Return types per kind:
    page          Page(...)
    dialog        Dialog(bg=Page | ("page-id", "state"), html=dialog(...))
    notification  Note(bg=Page | ("page-id", "state"), top="banner html", toasts="toast html")
"""
from __future__ import annotations

import html as _html
from dataclasses import dataclass, field
from typing import Callable

from data import *  # noqa: F401,F403  (cast and catalog for screen modules)
from data import ITEMS, PEOPLE, ME, ADMIN

esc = _html.escape

# ---------------------------------------------------------------------------
# Registry
# ---------------------------------------------------------------------------
KIND_DIR = {"page": "pages", "dialog": "dialogs", "notification": "notifications"}
REGISTRY: dict[str, "Screen"] = {}
LINKS: list[tuple[str, str, str]] = []   # (from, to-kind/id, state) for validation


class Screen:
    def __init__(self, sid, kind, title, route=None, opens_from=None, reqs=(), na=None, module=""):
        self.id, self.kind, self.title = sid, kind, title
        self.route, self.opens_from = route, opens_from
        self.reqs = list(reqs)
        self.na = dict(na or {})
        self.states: dict[str, tuple[str, Callable]] = {}
        self.module = module

    def state(self, name: str, desc: str):
        def deco(fn):
            self.states[name] = (desc, fn)
            return fn
        return deco


def screen(sid, kind, title, *, route=None, opens_from=None, reqs=(), na=None) -> Screen:
    import inspect
    module = inspect.stack()[1].frame.f_globals.get("__name__", "")
    if sid in REGISTRY:
        raise ValueError(f"duplicate screen id {sid}")
    s = Screen(sid, kind, title, route, opens_from, reqs, na, module)
    REGISTRY[sid] = s
    return s


# ---------------------------------------------------------------------------
# Links between mocks. Every mock lives two folders deep, so ../../ is root.
# ---------------------------------------------------------------------------
def P(sid: str, state: str = "default") -> str:
    """Link to a page state."""
    LINKS.append(("page", sid, state))
    return f"../../pages/{sid}/{state}.html"


def D(sid: str, state: str = "default") -> str:
    """Link to a dialog state (the dialog shown open over its page)."""
    LINKS.append(("dialog", sid, state))
    return f"../../dialogs/{sid}/{state}.html"


def N(sid: str, state: str) -> str:
    LINKS.append(("notification", sid, state))
    return f"../../notifications/{sid}/{state}.html"


A = "../../assets/"

# ---------------------------------------------------------------------------
# Icons: 24-unit viewBox, 2px stroke, currentColor.
# ---------------------------------------------------------------------------
ICONS = {
    "menu": '<path d="M4 7h16M4 12h16M4 17h16"/>',
    "close": '<path d="M6 6l12 12M18 6 6 18"/>',
    "search": '<circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/>',
    "plus": '<path d="M12 5v14M5 12h14"/>',
    "user": '<circle cx="12" cy="8" r="4"/><path d="M4 21a8 8 0 0 1 16 0"/>',
    "bag": '<path d="M5 8h14l-1 13H6zM9 8V6a3 3 0 0 1 6 0v2"/>',
    "tag": '<path d="M3 12V4h8l10 10-8 8z"/><circle cx="7.5" cy="8.5" r="1.5"/>',
    "box": '<path d="M3 7l9-4 9 4v10l-9 4-9-4zM3 7l9 4 9-4M12 11v10"/>',
    "truck": '<path d="M3 6h11v10H3zM14 10h4l3 3v3h-7"/><circle cx="7" cy="18" r="2"/><circle cx="17" cy="18" r="2"/>',
    "check": '<path d="M5 12l5 5L20 7"/>',
    "check-circle": '<circle cx="12" cy="12" r="9"/><path d="m8 12 3 3 5-6"/>',
    "info": '<circle cx="12" cy="12" r="9"/><path d="M12 11v5M12 8h.01"/>',
    "alert": '<path d="M12 3 2 20h20zM12 10v4M12 17h.01"/>',
    "x-circle": '<circle cx="12" cy="12" r="9"/><path d="m9 9 6 6M15 9l-6 6"/>',
    "star": '<path d="m12 3 2.8 5.8 6.2.9-4.5 4.4 1 6.3L12 17.5 6.5 20.4l1-6.3L3 9.7l6.2-.9z"/>',
    "camera": '<path d="M4 8h3l2-3h6l2 3h3v11H4z"/><circle cx="12" cy="13" r="3.5"/>',
    "lock": '<rect x="5" y="11" width="14" height="10"/><path d="M8 11V7a4 4 0 0 1 8 0v4"/>',
    "flag": '<path d="M5 21V4h11l-2 4 2 4H5"/>',
    "filter": '<path d="M4 6h16M7 12h10M10 18h4"/>',
    "chevron-right": '<path d="m9 6 6 6-6 6"/>',
    "chevron-left": '<path d="m15 6-6 6 6 6"/>',
    "chevron-down": '<path d="m6 9 6 6 6-6"/>',
    "arrow-left": '<path d="M19 12H5M11 6l-6 6 6 6"/>',
    "arrow-right": '<path d="M5 12h14M13 6l6 6-6 6"/>',
    "mail": '<rect x="3" y="5" width="18" height="14"/><path d="m3 7 9 6 9-6"/>',
    "wifi-off": '<path d="M3 3l18 18M8.5 16.5a5 5 0 0 1 7 0M5 13a10 10 0 0 1 5-2.7M14 10.3A10 10 0 0 1 19 13M2 9.5a15 15 0 0 1 4.5-2.8M10.5 6.1A15 15 0 0 1 22 9.5M12 20h.01"/>',
    "refresh": '<path d="M20 11a8 8 0 1 0-2.3 5.7M20 4v7h-7"/>',
    "edit": '<path d="M4 20h4L19 9l-4-4L4 16zM14 6l4 4"/>',
    "trash": '<path d="M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3"/>',
    "eye-off": '<path d="M3 3l18 18M10.6 6.1A10 10 0 0 1 12 6c5 0 9 6 9 6a17 17 0 0 1-3 3.5M6.6 6.6A17 17 0 0 0 3 12s4 6 9 6a9 9 0 0 0 4.4-1.1M9.9 9.9a3 3 0 0 0 4.2 4.2"/>',
    "shield": '<path d="M12 3 4 6v6c0 5 3.5 8 8 9 4.5-1 8-4 8-9V6z"/>',
    "clock": '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
    "download": '<path d="M12 3v12m0 0-4-4m4 4 4-4M4 17v3h16v-3"/>',
    "upload": '<path d="M12 21V9m0 0-4 4m4-4 4 4M4 7V4h16v3"/>',
    "drag": '<path d="M9 5h.01M15 5h.01M9 12h.01M15 12h.01M9 19h.01M15 19h.01"/>',
    "card": '<rect x="2" y="5" width="20" height="14"/><path d="M2 10h20M6 15h4"/>',
    "bank": '<path d="M3 10 12 4l9 6M5 10v8M9 10v8M15 10v8M19 10v8M3 20h18"/>',
    "settings": '<circle cx="12" cy="12" r="3"/><path d="M19 12a7 7 0 0 0-.1-1l2-1.5-2-3.4-2.3.9a7 7 0 0 0-1.7-1L14.5 3h-5l-.4 2.4a7 7 0 0 0-1.7 1L5.1 5.5l-2 3.4 2 1.5a7 7 0 0 0 0 2l-2 1.5 2 3.4 2.3-.9a7 7 0 0 0 1.7 1l.4 2.4h5l.4-2.4a7 7 0 0 0 1.7-1l2.3.9 2-3.4-2-1.5c.1-.3.1-.7.1-1z"/>',
    "bell": '<path d="M6 16V11a6 6 0 0 1 12 0v5l2 2H4zM10 20a2 2 0 0 0 4 0"/>',
    "list": '<path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01"/>',
    "external": '<path d="M14 4h6v6M20 4l-9 9M18 14v6H4V6h6"/>',
    "map-pin": '<path d="M12 21s7-6.2 7-12a7 7 0 0 0-14 0c0 5.8 7 12 7 12z"/><circle cx="12" cy="9" r="2.5"/>',
    "calendar": '<rect x="3" y="5" width="18" height="16"/><path d="M3 10h18M8 3v4M16 3v4"/>',
    "log-out": '<path d="M15 4h4v16h-4M10 17l5-5-5-5M15 12H3"/>',
    "ruler": '<path d="M3 17 17 3l4 4L7 21zM7 13l2 2M10 10l2 2M13 7l2 2"/>',
    "heart": '<path d="M12 20s-8-5-8-11a4.5 4.5 0 0 1 8-2.8A4.5 4.5 0 0 1 20 9c0 6-8 11-8 11z"/>',
    "share": '<circle cx="6" cy="12" r="2.5"/><circle cx="18" cy="6" r="2.5"/><circle cx="18" cy="18" r="2.5"/><path d="m8.2 10.8 7.6-3.6M8.2 13.2l7.6 3.6"/>',
    "more": '<path d="M5 12h.01M12 12h.01M19 12h.01"/>',
    "image": '<rect x="3" y="4" width="18" height="16"/><circle cx="9" cy="10" r="2"/><path d="m21 17-5-5-9 8"/>',
    "cookie": '<path d="M21 12a9 9 0 1 1-9-9 3 3 0 0 0 3 3 3 3 0 0 0 3 3 3 3 0 0 0 3 3z"/><path d="M8.5 9.5h.01M15.5 14.5h.01M9.5 15h.01"/>',
    "wrench": '<path d="M14.7 6.3a4 4 0 0 0-5.4 5.4L3 18l3 3 6.3-6.3a4 4 0 0 0 5.4-5.4l-2.5 2.5-2.4-.6-.6-2.4z"/>',
    "dollar": '<path d="M12 3v18M17 7H9.5a3 3 0 0 0 0 6h5a3 3 0 0 1 0 6H6"/>',
    "play": '<path d="M7 5v14l12-7z"/>',
    "pause": '<path d="M8 5v14M16 5v14"/>',
    "captions": '<rect x="3" y="5" width="18" height="14"/><path d="M10.5 10a2.5 2.5 0 1 0 0 4M17.5 10a2.5 2.5 0 1 0 0 4"/>',
}


# Google's "G" mark, as Google publishes it for sign-in buttons. Its four colours are Google's
# brand colours and stay fixed in both themes (Google's sign-in branding guidelines; ADR accounts/0001).
GOOGLE_MARK = ('<svg class="icon icon--mark" aria-hidden="true" viewBox="0 0 48 48">'
               '<path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/>'
               '<path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"/>'
               '<path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"/>'
               '<path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"/></svg>')


def provider_btn(label: str, href: str, busy: bool = False) -> str:
    """Third-party sign-in button (design system: Provider button): the provider's mark and a full-width label."""
    a = ' aria-busy="true"' if busy else ""
    return f'<a class="btn btn--provider btn--lg btn--block" href="{href}"{a}>{GOOGLE_MARK}{esc(label)}</a>'


def or_divider() -> str:
    return '<div class="or-divider" role="separator" aria-label="or"><span>or</span></div>'


def ic(name: str, cls: str = "icon", label: str | None = None) -> str:
    aria = f'role="img" aria-label="{esc(label)}"' if label else 'aria-hidden="true"'
    return f'<svg class="{cls}" {aria} viewBox="0 0 24 24">{ICONS[name]}</svg>'


# The TuTuMuch gingham wordmark supplied by the owner (docs/design-system/assets/logo.png, copied to
# docs/mocks/assets/logo.png). Decorative: the surrounding link carries aria-label "TuTuMuch home".
LOGO_IMG = '<img class="logo__wordmark" src="../../assets/logo.png" alt="" width="1200" height="274">'


MARK_IDS = [0]


def mark(cls: str = "") -> str:
    """The mark (brand kit markScene tGingham): a gingham "T" turned -6 degrees on a blush circle, ink
    outline, raspberry shadow. Kit geometry on a 100-unit box: cell 5, letter 76, baseline 46 + 0.35 x 76,
    shadow 0.07/0.09 x 76, outline 0.1 x 76. Decorative; tl-mark draws the same SVG."""
    MARK_IDS[0] += 1
    gid = f"mark-gingham-{MARK_IDS[0]}"   # one id per instance: a pattern inside a hidden SVG does not paint
    return (f'<svg class="mark {cls}" viewBox="0 0 100 100" aria-hidden="true" focusable="false">'
            f'<defs><pattern id="{gid}" width="10" height="10" patternUnits="userSpaceOnUse">'
            '<rect width="10" height="10" style="fill: var(--color-logo-gingham-light)"/>'
            '<rect width="5" height="5" style="fill: var(--color-logo-gingham-dark)"/>'
            '<rect x="5" y="5" width="5" height="5" style="fill: var(--color-logo-gingham-dark)"/></pattern></defs>'
            '<circle cx="50" cy="50" r="50" style="fill: var(--color-mark-bg)"/>'
            '<g transform="rotate(-6 50 50)" style="font-family: var(--font-family-logo); font-size: 76px; paint-order: stroke; stroke: var(--color-logo-outline); stroke-width: 7.6px; stroke-linejoin: round">'
            '<text x="55.32" y="79.44" text-anchor="middle" style="fill: var(--color-logo-shadow)">T</text>'
            f'<text x="50" y="72.6" text-anchor="middle" style="fill: url(#{gid})">T</text></g></svg>')


def logo(href: str | None = None, cls: str = "") -> str:
    """The one logo link of a page: the gingham wordmark, and in the app header (cls logo--responsive)
    the mark instead under 640px, inside the same link."""
    href = href or P("catalog")
    m = mark("logo__mark") if "logo--responsive" in cls else ""
    return f'<a class="logo {cls}" href="{href}" aria-label="TuTuMuch home">{LOGO_IMG}{m}</a>'


# Tilly (docs/print/characters, copied to docs/mocks/assets/tilly). At most one per screen; the alt
# text names her and what she is doing (L2-096).
TILLY = {
    "hero": ("tilly-twirl.webp", "Tilly twirling in a raspberry tutu with a shopping bag", 442, 600),
    "dance": ("tilly-dance.webp", "Tilly dancing an arabesque in her tutu", 418, 600),
    "soccer": ("tilly-soccer.webp", "Tilly kicking a soccer ball", 418, 600),
    "volleyball": ("tilly-volleyball.webp", "Tilly bumping a volleyball", 414, 600),
    "gymnastics": ("tilly-gymnastics.webp", "Tilly landing a gymnastics move on a mat", 465, 600),
    "swimming": ("tilly-swimming-tile.webp", "Tilly ready to dive into the pool", 600, 600),
    "show-and-tell": ("tilly-show-and-tell.webp", "Tilly holding up a yellow dress to pass it on", 391, 600),
    "hello": ("tilly-hello.webp", "Tilly waving hello from a stack of folded clothes", 355, 600),
}


def tilly(pose: str, cls: str = "") -> str:
    f, alt, w, h = TILLY[pose]
    tile = " tilly--tile" if pose == "swimming" else ""
    return f'<img class="tilly{tile} {cls}" src="{A}tilly/{f}" alt="{esc(alt)}" width="{w}" height="{h}">'


# ---------------------------------------------------------------------------
# Small components
# ---------------------------------------------------------------------------
def money(v: float, cents: bool = True) -> str:
    if cents or v != int(v):
        return f"${v:,.2f}"
    return f"${v:,.0f}"


def price(v: float) -> str:
    """Catalog price: whole dollars when there are no cents."""
    return f"${v:,.0f}" if v == int(v) else f"${v:,.2f}"


def ship_text(v: float) -> str:
    return "Free shipping" if v == 0 else f"{money(v)} shipping"


def size_of(item: dict) -> str:
    return "Girls'"


def size_long(item: dict) -> str:
    return "Girls'"


def size_tag(item: dict | str, cls: str = "") -> str:
    s = item if isinstance(item, str) else size_of(item)
    label = s.rstrip("'")          # stored as "Girls'", tagged as "Girls"
    return f'<span class="size-tag {cls}" aria-label="Size: {esc(s)}"><b>{esc(label)}</b></span>'


def photo(key: str, cls: str = "", tag: bool = False, stamp: str | None = None, alt: str | None = None, n: int = 1, tone: int | None = None) -> str:
    """A listing photo in a white 4:5 well. Sold greys the photo and shows the "Sold" badge (L2-096)."""
    it = ITEMS[key]
    alt = alt if alt is not None else (f"Photo {n} of {it['brand']} {it['title']}")
    st = ""
    if stamp:
        st = f'<span class="photo__stamp{" photo__stamp--sold" if stamp == "Sold" else ""}">{stamp}</span>'
    sold = " photo--sold" if stamp == "Sold" else ""
    tg = size_tag(it) if tag else ""
    return (f'<div class="photo{sold} {cls}"><img src="{A}items/{it["img"]}.svg" alt="{esc(alt)}" width="160" height="160" loading="lazy">'
            f'{st}{tg}</div>')


CONDITION_BADGE = {"New with tags": "new", "New without tags": "like-new", "Used - Excellent": "like-new",
                   "Used - Good": "loved", "Used - Fair": "well-loved"}


def condition_badge(cond: str) -> str:
    """Condition badge (design system: Condition badge): colour by the nearest guideline step."""
    return f'<span class="condition-badge condition-badge--{CONDITION_BADGE[cond]}">{esc(cond)}</span>'


def listing_card(key: str, href: str | None = None, stamp: str | None = None) -> str:
    """Listing card anatomy (L2-096): photo, title on one line, brand, "Girls · condition", price pushed down."""
    it = ITEMS[key]
    stamp = stamp if stamp is not None else (it.get("status") if it.get("status") in ("Reserved", "Sold") else None)
    href = href or P("listing-detail")
    return (f'<li><a class="listing-card" href="{href}">{photo(key, stamp=stamp)}'
            f'<div class="listing-card__meta"><span class="listing-card__title">{esc(it["title"])}</span>'
            f'<span class="listing-card__brand">{esc(it["brand"])}</span>'
            f'<span class="listing-card__facts">{esc(size_of(it).rstrip("\'"))} · {esc(it["cond"])}</span>'
            f'<span class="listing-card__price"><span class="price">{price(it["price"])}</span></span></div></a></li>')


def listing_grid(keys, **kw) -> str:
    return '<ul class="listing-grid" role="list">' + "".join(listing_card(k, **kw) for k in keys) + "</ul>"


def skeleton_grid(n: int = 8) -> str:
    """Blush skeleton cards shaped like the real card: photo, title, brand, facts, price."""
    card = ('<li><div class="listing-card" aria-hidden="true"><span class="skeleton skeleton--photo"></span>'
            '<div class="listing-card__meta"><span class="skeleton skeleton--text" style="width: 85%;"></span>'
            '<span class="skeleton skeleton--text" style="width: 45%;"></span><span class="skeleton skeleton--text" style="width: 60%;"></span>'
            '<span class="skeleton skeleton--title" style="width: 30%;"></span></div></div></li>')
    return f'<ul class="listing-grid" role="list" aria-busy="true" aria-label="Loading listings">{card * n}</ul>'


def stars(value: float) -> str:
    full = int(round(value))
    return "".join(ic("star", "icon" if i < full else "icon icon--off") for i in range(5))


def rating(handle_or_value, count: int | None = None, short: bool = False) -> str:
    """Rating summary per L2-040: '4.3 (3 reviews)' or 'No reviews yet'."""
    if isinstance(handle_or_value, str):
        p = PEOPLE[handle_or_value]
        value, count = p["rating"], p["reviews"]
    else:
        value = handle_or_value
    if not count:
        return '<span class="rating text-muted">No reviews yet</span>'
    word = "review" if count == 1 else "reviews"
    label = f"Rated {value:.1f} out of 5 from {count} {word}"
    tail = f"({count:,})" if short else f"({count:,} {word})"
    return (f'<span class="rating" role="img" aria-label="{label}"><span class="rating__stars">{stars(value)}</span>'
            f'<span class="rating__value">{value:.1f}</span><span class="text-muted">{tail}</span></span>')


def avatar(handle: str, cls: str = "") -> str:
    p = PEOPLE.get(handle)
    initials = p["initials"] if p else handle[:2].upper()
    return f'<span class="avatar {cls}" aria-hidden="true">{initials}</span>'


def btn(label: str, kind: str = "", href: str | None = None, icon: str | None = None, attrs: str = "", icon_after: str | None = None) -> str:
    cls = "btn" + "".join(f" btn--{k}" for k in kind.split()) if kind else "btn"
    inner = (ic(icon) if icon else "") + esc(label) + (ic(icon_after) if icon_after else "")
    if href:
        return f'<a class="{cls}" href="{href}" {attrs}>{inner}</a>'
    t = "" if "type=" in attrs else 'type="button" '
    return f'<button class="{cls}" {t}{attrs}>{inner}</button>'


def status(label: str) -> str:
    mod = {"Draft": "draft", "Active": "active", "Reserved": "reserved", "Sold": "sold", "Removed": "removed",
           "Paid": "info", "Shipped": "info", "Delivered": "info", "Completed": "active", "Cancelled": "removed", "Refunded": "removed",
           "Open": "reserved"}.get(label, "draft")
    return f'<span class="status status--{mod}">{esc(label)}</span>'


def alert(kind: str, title: str, text: str = "", actions: str = "", icon: str | None = None, cls: str = "", role: str | None = None) -> str:
    icon = icon or {"info": "info", "success": "check-circle", "warning": "alert", "danger": "x-circle"}[kind]
    r = f' role="{role}"' if role else (' role="alert"' if kind == "danger" else "")
    body = f'<div class="alert__title">{title}</div>' + (f"<div>{text}</div>" if text else "") + (f'<div class="alert__actions">{actions}</div>' if actions else "")
    return f'<div class="alert alert--{kind} {cls}"{r}>{ic(icon)}<div class="alert__body">{body}</div></div>'


def empty(icon: str, title: str, text: str = "", actions: str = "", cls: str = "", heading: str = "h2", tilly_pose: bool | None = None) -> str:
    """Empty state: Tilly's "Hello, friend" pose, one short line and one action (L2-096). Error states
    (cls empty--error) and screens that already show Tilly keep the icon well instead."""
    show = tilly_pose if tilly_pose is not None else "empty--error" not in cls
    t = f'<p class="empty__text">{text}</p>' if text else ""
    a = f'<div class="empty__actions">{actions}</div>' if actions else ""
    art = tilly("hello", "empty__tilly") if show else f'<div class="empty__icon">{ic(icon, "icon icon--lg")}</div>'
    return f'<div class="empty {cls}">{art}<{heading} class="empty__title">{title}</{heading}>{t}{a}</div>'


def error_summary(errors: list[tuple[str, str]], title: str = "Fix these to continue") -> str:
    items = "".join(f'<li><a href="#{fid}">{esc(msg)}</a></li>' for fid, msg in errors)
    return (f'<div class="alert alert--danger form-summary" role="alert" tabindex="-1" id="error-summary">{ic("x-circle")}'
            f'<div class="alert__body"><div class="alert__title">{title}</div><ul style="margin: 0; padding-left: var(--space-5);">{items}</ul></div></div>')


def field(fid: str, label: str, control: str = "", *, help: str | None = None, error: str | None = None, optional: bool = False,
          value: str = "", type: str = "text", attrs: str = "", counter: str | None = None) -> str:
    """Labelled field. Pass `control` to supply custom markup (must use id=fid)."""
    describedby = " ".join(x for x in [f"{fid}-help" if help else "", f"{fid}-error" if error else ""] if x)
    db = f' aria-describedby="{describedby}"' if describedby else ""
    inv = ' aria-invalid="true"' if error else ""
    if not control:
        v = f' value="{esc(value)}"' if value else ""
        control = f'<input class="input" id="{fid}" name="{fid}" type="{type}"{v}{db}{inv} {attrs}>'
    else:
        control = control.replace("{db}", db + inv)
    opt = ' <span class="optional">(optional)</span>' if optional else ""
    h = f'<span class="field__help" id="{fid}-help">{help}</span>' if help else ""
    e = f'<span class="field__error" id="{fid}-error">{esc(error)}</span>' if error else ""
    c = f'<span class="field__counter">{counter}</span>' if counter else ""
    return f'<div class="field"><label class="field__label" for="{fid}">{label}{opt}</label>{control}{h}{e}{c}</div>'


def item_row(key: str, meta: str = "", thumb_cls: str = "thumb") -> str:
    it = ITEMS[key]
    meta = meta or f'{esc(it["brand"])} · {size_long(it)}'
    return (f'<div class="item-row"><div class="{thumb_cls}">{photo(key, tag=False, alt="")}</div>'
            f'<div class="item-row__body"><span class="item-row__title">{esc(it["title"])}</span><span class="item-row__meta">{meta}</span></div></div>')


def totals(rows: list[tuple[str, str]], total: tuple[str, str]) -> str:
    r = "".join(f"<div><dt>{a}</dt><dd>{b}</dd></div>" for a, b in rows)
    return f'<dl class="totals">{r}<div class="totals__total"><dt>{total[0]}</dt><dd>{total[1]}</dd></div></dl>'


def toast(kind: str, title: str, text: str = "", action: str | None = None, progress: bool = True) -> str:
    icon = {"info": "info", "success": "check-circle", "warning": "alert", "danger": "x-circle"}[kind]
    role = ' role="alert"' if kind == "danger" else ""
    a = f'<button class="toast__action" type="button">{action}</button>' if action else ""
    t = f'<div class="toast__text">{text}</div>' if text else ""
    pr = '<span class="toast__progress" aria-hidden="true"></span>' if progress and kind != "danger" else ""
    return (f'<div class="toast toast--{kind}"{role}>{ic(icon)}<div class="toast__body"><div class="toast__title">{title}</div>{t}{a}</div>'
            f'<button class="close" type="button" aria-label="Dismiss">{ic("close", "icon icon--sm")}</button>{pr}</div>')


def toast_region(*toasts: str) -> str:
    return f'<div class="toast-region" role="status" aria-live="polite">{"".join(toasts)}</div>'


def banner(kind: str, text: str, actions: str = "", dismiss: bool = True, icon: str | None = None) -> str:
    icon = icon or {"info": "info", "success": "check-circle", "warning": "alert", "danger": "x-circle"}[kind]
    role = ' role="alert"' if kind == "danger" else ' role="status"'
    d = f'<button class="close" type="button" aria-label="Dismiss">{ic("close", "icon icon--sm")}</button>' if dismiss else ""
    a = f'<span class="alert__actions" style="margin: 0;">{actions}</span>' if actions else ""
    return f'<div class="alert alert--{kind} banner"{role}>{ic(icon)}<div class="alert__body"><span>{text}</span>{a}</div>{d}</div>'


def dialog(title: str, body: str, footer: str = "", *, desc: str | None = None, icon: str | None = None, icon_kind: str = "info",
           size: str = "", top: str = "", title_hidden: bool = False) -> str:
    """Modal dialog markup. `top` goes above the body (failed alerts, summaries); `title_hidden` keeps the
    title as the accessible name only."""
    ic_html = f'<div class="dialog__icon dialog__icon--{icon_kind}" aria-hidden="true">{ic(icon)}</div>' if icon else ""
    d = f'<p id="dlg-desc" class="text-muted" style="margin-top: var(--space-2);">{desc}</p>' if desc else ""
    dd = ' aria-describedby="dlg-desc"' if desc else ""
    f = f'<div class="dialog__footer">{footer}</div>' if footer else ""
    sz = f" dialog__panel--{size}" if size else ""
    t = f'<div style="padding: var(--space-4) var(--space-6) 0;">{top}</div>' if top else ""
    return (f'<div class="backdrop"></div><div class="dialog" role="dialog" aria-modal="true" aria-labelledby="dlg-title"{dd} data-dismiss-on-escape>'
            f'<div class="dialog__panel{sz}"><div class="dialog__header">{ic_html}<div style="flex: 1;"><h2 class="dialog__title{' visually-hidden' if title_hidden else ''}" id="dlg-title">{title}</h2>{d}</div>'
            f'<button class="btn btn--ghost btn--icon" type="button" aria-label="Close">{ic("close")}</button></div>{t}'
            f'<div class="dialog__body stack">{body}</div>{f}</div></div>')


def note(text: str, pos: str = "top: 6rem; right: 1rem;") -> str:
    return f'<p class="mock-note" style="{pos}">{text}</p>'


# ---------------------------------------------------------------------------
# Page model and shell
# ---------------------------------------------------------------------------
@dataclass
class Page:
    main: str
    nav: str = ""            # shop | clothing | shoes | dance | sports | cameras-and-tech | toys-and-crafts | sell
    user: str = "member"     # member | guest | unverified | admin
    shell: str = "app"       # app | auth | admin | bare
    account: str = ""        # active account subnav tab: overview | listings | purchases | requests | sales | payouts | profile | notifications | settings
    top: str = ""            # banners above the header
    after: str = ""          # toasts, sticky bars, overlays after main
    footer: bool = True
    main_class: str = "container page"
    notes: str = ""          # .mock-note annotations
    phase: int = 3           # launch phase (L2-089): 1 hides the category links and menu button; 1 and 2 hide Sell from non-admins


@dataclass
class Dialog:
    bg: object
    html: str
    notes: str = ""


@dataclass
class Note:
    bg: object
    top: str = ""
    toasts: str = ""
    after: str = ""
    notes: str = ""


# The six categories in the guideline order (ADR general/0009): Dance, the sports, everyday clothing, shoes,
# then TuTuMuch's own categories.
NAV = [("dance", "Dance"), ("sports", "Sports"), ("clothing", "Clothing"), ("shoes", "Shoes"),
       ("cameras-and-tech", "Cameras and tech"), ("toys-and-crafts", "Toys and crafts")]


def cat_key(cat: str) -> str:
    """Category name to its route slug: "Cameras and tech" -> "cameras-and-tech"."""
    return cat.lower().replace(" ", "-")


def nav_href(key: str) -> str:
    """Clothing has its own filtered state; the other categories reuse the catalog."""
    return P("catalog", "category") if key == "clothing" else P("catalog")


def topbar(pg: Page) -> str:
    menus = pg.phase != 1                                   # L2-093: no category links or menu button in Phase 1
    can_sell = pg.phase == 3 or pg.user == "admin"          # L2-094: Sell only for administrators in Phases 1 and 2
    nav = "".join(
        f'<a class="nav-link" href="{nav_href(k)}"{" aria-current=\"page\"" if pg.nav == k else ""}>{label}</a>' for k, label in NAV) if menus else ""
    search = (f'<form class="search" role="search" action="{P("catalog", "search")}"><label class="visually-hidden" for="q">Search TuTuMuch</label>'
              f'{ic("search")}<input class="input" id="q" name="q" type="search" placeholder="Search the closet"></form>')
    if pg.user == "guest":
        sell = f'<a class="btn btn--ghost topbar__sell" href="{P("sell")}">Sell</a>' if can_sell else ""
        right = (f'<a class="btn btn--ghost btn--icon topbar__search-btn" href="{P("catalog", "search")}" aria-label="Search">{ic("search")}</a>'
                 f'{sell}<a class="btn" href="{P("sign-in")}">Sign in</a>')
    else:
        who = ADMIN if pg.user == "admin" else PEOPLE[ME]
        sell_cur = ' aria-current="page"' if pg.nav == "sell" else ""
        sell = f'<a class="nav-link topbar__sell" href="{P("sell")}"{sell_cur}>{ic("plus")}Sell</a>' if can_sell else ""
        right = (f'<a class="btn btn--ghost btn--icon topbar__search-btn" href="{P("catalog", "search")}" aria-label="Search">{ic("search")}</a>'
                 f'{sell}'
                 f'<a class="avatar avatar--sm" href="{P("account")}" aria-label="Your account, {esc(who["name"])}" style="text-decoration: none;">{who["initials"]}</a>')
    menu = (f'<button class="btn btn--ghost btn--icon topbar__menu" type="button" aria-label="Open menu" aria-expanded="false" aria-controls="menu">{ic("menu")}</button>'
            if menus else "")
    nav_el = f'<nav class="topbar__nav" aria-label="Primary">{nav}</nav>' if menus else ""
    # Header skeleton (page guidelines 7): logo left, search in the middle, Sell and Account on the right,
    # categories in a second row. TuTuMuch has no bag, so there is no Bag link (ADR general/0009).
    return (f'<header class="topbar"><div class="topbar__row">{menu}'
            f'{logo(cls="logo--responsive")}<span class="topbar__spacer"></span>{search}<span class="topbar__spacer"></span>'
            f'<div class="topbar__actions">{right}</div></div>{nav_el}</header>')


def admin_topbar(pg: Page) -> str:
    return (f'<header class="topbar"><div class="topbar__row">{logo(P("catalog"), "logo--responsive")}<span class="topbar__admin">Admin</span><span class="topbar__spacer"></span>'
            f'<span class="avatar avatar--sm" aria-label="Signed in as {ADMIN["name"]}" role="img">{ADMIN["initials"]}</span></div></header>')


ACCOUNT_TABS = [("overview", "Overview", "account"), ("listings", "Listings", "my-listings"), ("purchases", "Purchases", "purchases"),
                ("requests", "Requests", "my-requests"), ("sales", "Sales", "sales"), ("payouts", "Payouts", "payouts"), ("profile", "Profile", "edit-profile"),
                ("notifications", "Emails", "notification-settings"), ("settings", "Settings", "settings")]


def account_nav(active: str) -> str:
    links = "".join(f'<a class="tab" href="{P(pid)}"{" aria-current=\"page\"" if active == k else ""}>{t}</a>' for k, t, pid in ACCOUNT_TABS)
    return f'<nav class="subnav" aria-label="Account">{links}</nav>'


def footer() -> str:
    """Footer skeleton (page guidelines 7): the mark (not a second logo link), short links, "© TuTuMuch"."""
    return (f'<footer class="footer"><span class="footer__mark">{mark()}</span><nav aria-label="Footer"><a href="{P("rules")}">Marketplace rules</a>'
            f'<a href="{P("privacy")}">Privacy</a><a href="{P("terms")}">Terms</a><a href="{P("welcome")}">About TuTuMuch</a><a href="{D("cookie-settings")}">Cookie settings</a></nav>'
            f'<span>© TuTuMuch</span></footer>')


def page_body(pg: Page) -> str:
    if pg.shell == "auth":
        return (f'{pg.top}<div class="auth-layout">{logo(cls="logo--lg")}<main id="main" class="auth-card">{pg.main}</main></div>{pg.after}')
    if pg.shell == "bare":
        return f'{pg.top}<header class="topbar"><div class="topbar__row">{logo(cls="logo--responsive")}</div></header><main id="main" class="{pg.main_class}">{pg.main}</main>{pg.after}'
    head = admin_topbar(pg) if pg.shell == "admin" else topbar(pg)
    sub = account_nav(pg.account) if pg.account else ""
    main_inner = (f'<div class="container">{sub}</div>' if sub else "")
    foot = footer() if pg.footer and pg.shell != "admin" else ""
    return (f'{pg.top}{head}{main_inner}<main id="main" class="{pg.main_class}">{pg.main}</main>{foot}{pg.after}')


# ---------------------------------------------------------------------------
# Document assembly
# ---------------------------------------------------------------------------
def resolve_bg(bg) -> Page:
    if isinstance(bg, Page):
        return bg
    sid, st = bg
    LINKS.append(("page", sid, st))
    return REGISTRY[sid].states[st][1]()


def mock_bar(s: Screen, current: str) -> str:
    items = "".join(f'<li><a href="{st}.html"{" aria-current=\"page\"" if st == current else ""}>{st}</a></li>' for st in s.states)
    return (f'<nav class="mock-bar" aria-label="Mock navigation"><a class="mock-bar__home" href="../../index.html" aria-label="All mocks">⌂</a>'
            f'<span class="mock-bar__screen">{esc(s.title)}</span><ul class="mock-bar__states">{items}</ul>'
            f'<button type="button" data-theme-toggle aria-pressed="false">☾ Dark</button>'
            f'<button type="button" data-chrome-toggle aria-label="Hide mock chrome">×</button></nav>')


def render(s: Screen, state: str) -> str:
    MARK_IDS[0] = 0
    desc, fn = s.states[state]
    out = fn()
    notes = getattr(out, "notes", "")
    if s.kind == "page":
        body = page_body(out)
    elif s.kind == "dialog":
        bgp = resolve_bg(out.bg)
        body = f'<div inert aria-hidden="true">{page_body(bgp)}</div>{out.html}'
    else:
        bgp = resolve_bg(out.bg)
        pg = Page(**{**bgp.__dict__, "top": out.top + bgp.top, "after": bgp.after + out.after + (out.toasts if out.toasts else "")})
        body = page_body(pg)
        if out.toasts == "" and "toast-region" not in body:
            pass
    if 'class="toast-region"' not in body and s.kind != "dialog":
        body += toast_region()
    reqs = " ".join(s.reqs)
    title = f"{s.title} · {state} — TuTuMuch mocks"
    return f"""<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>{esc(title)}</title>
<meta name="mock:kind" content="{s.kind}">
<meta name="mock:screen" content="{s.id}">
<meta name="mock:state" content="{state}">
<meta name="mock:requirements" content="{reqs}">
<meta name="description" content="{esc(desc)}">
<link rel="icon" href="../../../design-system/assets/favicon.ico">
<link rel="stylesheet" href="../../assets/fonts.css">
<link rel="stylesheet" href="../../assets/tokens.css">
<link rel="stylesheet" href="../../assets/ui.css">
<link rel="stylesheet" href="../../assets/mock.css">
</head>
<body>
<a class="skip-link" href="#main">Skip to content</a>
{body}
{notes}
{mock_bar(s, state)}
<script src="../../assets/mock.js"></script>
</body>
</html>
"""
