"""Marketing page at /welcome and its short pitch video dialog (L1-020, L2-078 to L2-083).

Copy rules (ADR general/0005): headlines of 3 to 6 words, one supporting line, scannable
points; everything is for girls and the copy never promises boys' or adult sizes. The copy is
identical to the app's WelcomePage (src/frontend/projects/marketing/src/lib/welcome/).
"""
from mk import *

MEDIA = f"{A}media/"
STATIC_NA = {"loading": "Static content ships with the app; the page calls no TuTuMuch API endpoint.",
             "empty": "The page always has content.",
             "error": "Static content cannot fail to load once the app is served; the video has its own error state."}

SHOPPER_POINTS = [("Every listing is for girls", "Clothes, dance and sports gear, cameras and more. Nothing to filter out."),
                  ("Outgrown, not worn out", "Most things still have plenty of twirl left in them."),
                  ("Honest condition", "Five grades, and flaws named up front.")]
# Phase 1 shopper band: the closet is small and hand-picked by TuTuMuch.
SHOPPER_POINTS_PHASE_ONE = [("Hand-picked by TuTuMuch", "Every piece is chosen, checked and photographed by us."),
                            ("Every listing is for girls", "Clothes, dance and sports gear, cameras and more."),
                            ("Honest condition", "Five grades, and flaws named up front.")]
SELLER_POINTS = [("List in minutes", "Photos, a price and the condition. That’s it."),
                 ("Families are already looking", "Sell to parents who need exactly what she has outgrown.")]
# The seller band in launch Phases 1 and 2 (L2-094): nothing promises that a member can list today.
SELLER_POINTS_SOON = [("Pass it on soon", "Selling opens to every member in the next phase."),
                      ("Join today", "Your account is ready to sell the day selling opens.")]
STEPS = [("List it", "Add photos, a price and the condition."),
         ("Sell it", "A buyer pays. You ship within 5 business days."),
         ("Get paid", "The order completes and your payout is on its way.")]


def seller_href(member: bool) -> str:
    return P("sell") if member else P("register")


def poster() -> str:
    return (f'<a class="video-poster" href="{D("pitch-video")}"><img src="{MEDIA}tutumuch-pitch-poster.png" alt="" width="1920" height="1080">'
            f'<span class="video-poster__play">{ic("play")}Play</span>'
            '<span class="video-poster__meta" aria-hidden="true">0:15</span></a>')


SOON = '<p class="marketing-hero__soon">Selling opens soon.</p>'
# Decorative tulle field behind the hero: petals drift up and twirl around the pointer; a tap throws sparkles.
# One still frame under prefers-reduced-motion; hidden from assistive technology.
TULLE = '<canvas class="tulle-field" data-tulle aria-hidden="true"></canvas>'


def hero(member: bool, selling: bool = True) -> str:
    # The one primary button of the page (L2-096); every other call to action is secondary.
    actions = btn("Shop the closet", "primary lg", P("catalog")) + (btn("Start selling", "lg", seller_href(member)) if selling else SOON)
    return (f'<section class="marketing-hero" aria-labelledby="welcome-title">{TULLE}<div class="marketing-hero__inner">'
            '<div><p class="marketing-hero__eyebrow">Clothes · Dance · Sports · Gear, all for girls</p>'
            '<h1 class="marketing-hero__title" id="welcome-title">Twirl it <em>forward.</em></h1>'
            '<p class="marketing-hero__lede">New and gently loved clothes and gear for girls. Shop her next favourite thing, or pass on what she has outgrown.</p>'
            f'<div class="marketing-hero__actions">{actions}</div></div>'
            f'<div class="marketing-hero__media">{tilly("hero", "tilly--hero")}{poster()}</div></div></section>')


# The closet marquee (inspired by 21st.dev marquee components): two rows of real listings
# gliding in opposite directions, so the first scroll shows what is for sale, not only words.
MARQUEE_ROWS = [["pink-tutu", "glitter-sneakers", "velvet-dress", "instant-camera", "ballet-leotard", "party-dress", "rag-doll", "rainbow-sweater"],
                ["paint-set", "rainbow-romper", "soccer-ball", "cat-headphones", "kick-scooter", "rain-boots", "ballet-slippers", "heart-hoodie"]]


def marquee_tile(key: str, copy: bool) -> str:
    it = ITEMS[key]
    hidden = ' aria-hidden="true" tabindex="-1"' if copy else ""
    return (f'<li class="closet-marquee__tile"><a href="{P("listing-detail")}"{hidden}>{photo(key, alt="" if copy else None)}'
            f'<span class="closet-marquee__meta"><span class="closet-marquee__title">{esc(it["title"])}</span>'
            f'<span class="price">{price(it["price"])}</span></span></a></li>')


def marquee(title: str = "Fresh in the closet", rows_keys=None) -> str:
    rows = "".join(
        f'<div class="closet-marquee__row{" closet-marquee__row--reverse" if i else ""}"><ul class="closet-marquee__track" role="list">'
        + "".join(marquee_tile(k, False) for k in keys) + "".join(marquee_tile(k, True) for k in keys) + "</ul></div>"
        for i, keys in enumerate(rows_keys or MARQUEE_ROWS))
    return (f'<section class="closet-marquee" aria-labelledby="marquee-title"><div class="closet-marquee__head">'
            f'<h2 class="closet-marquee__heading" id="marquee-title"><span class="closet-marquee__live" aria-hidden="true"></span>{title}</h2>'
            f'{btn("See everything", href=P("catalog"), icon_after="arrow-right")}</div>{rows}</section>')


# What's her closet worth? A calculator band: tap what she has outgrown and watch a typical
# total count up, so joining has a number on it. Prices are typical TuTuMuch listing prices.
WORTH_ITEMS = [("tutu", "Tutu", 32), ("party-dress", "Party dress", 42), ("sneakers", "Sneakers", 22), ("leotard", "Leotard", 20),
               ("puffer", "Winter coat", 34), ("scooter", "Scooter", 48), ("camera", "Camera", 45), ("doll", "Doll", 16),
               ("rain-boots", "Rain boots", 18), ("sweater", "Sweater", 32)]
WORTH_START = {"tutu", "party-dress", "sneakers"}


def worth(member: bool, selling: bool) -> str:
    chips = "".join(
        f'<li><button type="button" class="worth__chip" data-worth="{v}" aria-pressed="{"true" if k in WORTH_START else "false"}">'
        f'<img src="{A}items/{k}.svg" alt="" width="40" height="40"><span>{esc(label)}</span><b>${v}</b></button></li>'
        for k, label, v in WORTH_ITEMS)
    total = sum(v for k, _, v in WORTH_ITEMS if k in WORTH_START)
    cta = (btn("Start selling", "lg", seller_href(member)) if selling
           else btn("Join and sell first", "lg", P("register")))  # secondary: the hero keeps the one primary (L2-096)
    small = ("List them in minutes. You keep the payout." if selling
             else "Selling opens soon. Join today and your account is ready the day it does.")
    return (f'<section class="worth" aria-labelledby="worth-title"><div class="worth__inner">'
            f'<div class="worth__intro"><p class="feature-band__eyebrow">Her outgrown pile</p>'
            f'<h2 class="feature-band__title" id="worth-title">What’s her closet <em>worth?</em></h2>'
            f'<p class="worth__lede">Tap what she has outgrown.</p></div>'
            f'<ul class="worth__chips" role="list">{chips}</ul>'
            f'<div class="worth__result"><p class="worth__label" id="worth-label">Typical total on TuTuMuch</p>'
            f'<p class="worth__total" aria-live="polite"><span data-worth-total>${total}</span></p>'
            f'<p class="worth__count"><span data-worth-count>{len(WORTH_START)}</span> items</p>'
            f'{cta}<p class="worth__small">{small}</p></div></div></section>')


def points(items) -> str:
    return '<ul class="feature-band__points" role="list">' + "".join(
        f'<li class="feature-band__point"><strong>{t}</strong><span>{d}</span></li>' for t, d in items) + "</ul>"


def band(bid: str, eyebrow: str, title: str, body: str, action: str = "", mod: str = "") -> str:
    a = f'<div class="feature-band__actions">{action}</div>' if action else ""
    return (f'<section class="feature-band {mod}" aria-labelledby="{bid}"><div class="feature-band__inner">'
            f'<div><p class="feature-band__eyebrow">{eyebrow}</p><h2 class="feature-band__title" id="{bid}">{title}</h2>{a}</div>'
            f'<div>{body}</div></div></section>')


STEP_ICONS = ["camera", "truck", "dollar"]
# Phase 1 (L2-093, L2-094): only TuTuMuch sells, so the journey is the shopper's, not the seller's.
STEPS_PHASE_ONE = [("Find it", "Pick something she will love from the hand-picked closet."),
                   ("Pay at checkout", "Card details go to Stripe, never to TuTuMuch."),
                   ("It ships to you", "TuTuMuch ships within 5 business days.")]
STEP_ICONS_PHASE_ONE = ["search", "card", "truck"]


def steps(items=None, icons=None) -> str:
    items, icons = items or STEPS, icons or STEP_ICONS
    # The journey: a rail fills as the band scrolls through view and each station lights up as
    # the fill reaches it. Without JavaScript or with reduced motion every station shows lit.
    return ('<div class="journey" data-journey><span class="journey__rail" aria-hidden="true"><span class="journey__fill"></span></span>'
            '<ol class="steps steps--journey">' + "".join(
                f'<li class="steps__item"><span class="journey__station" aria-hidden="true">{ic(i, "icon icon--lg")}</span>'
                f'<h3 class="steps__title">{t}</h3><p class="steps__text">{d}</p></li>' for (t, d), i in zip(items, icons))
            + "</ol></div>")


def closing(member: bool, selling: bool = True) -> str:
    actions = btn("Shop the closet", "lg", P("catalog")) + (btn("Start selling", "lg", seller_href(member)) if selling else "")
    return (f'<section class="marketing-hero marketing-hero--closing" aria-labelledby="closing-title">'
            '<div class="marketing-hero__inner"><div><h2 class="marketing-hero__title" id="closing-title">Her next favourite thing is <em>waiting.</em></h2>'
            f'<p class="marketing-hero__lede">New listings every day, every one for girls.</p>'
            f'<div class="marketing-hero__actions">{actions}</div></div></div></section>')


JOIN_EARLY = btn("Join early", "lg", P("register"))


def welcome_phase_one() -> Page:
    """Launch Phase 1, signed out: a small hand-picked closet. Every call to action is shop or join early."""
    hero_html = (f'<section class="marketing-hero" aria-labelledby="welcome-title">{TULLE}<div class="marketing-hero__inner">'
                 '<div><p class="marketing-hero__eyebrow">The first closet is open · All for girls</p>'
                 '<h1 class="marketing-hero__title" id="welcome-title">Twirl it <em>forward.</em></h1>'
                 '<p class="marketing-hero__lede">Clothes and gear for girls, every piece hand-picked by TuTuMuch. Shop her next favourite thing today.</p>'
                 f'<div class="marketing-hero__actions">{btn("Shop the closet", "primary lg", P("catalog"))}{JOIN_EARLY}</div></div>'
                 f'<div class="marketing-hero__media">{tilly("hero", "tilly--hero")}{poster()}</div></div></section>')
    closing_html = ('<section class="marketing-hero marketing-hero--closing" aria-labelledby="closing-title">'
                    '<div class="marketing-hero__inner"><div><h2 class="marketing-hero__title" id="closing-title">Her next favourite thing is <em>waiting.</em></h2>'
                    '<p class="marketing-hero__lede">A small closet, hand-picked for girls. Join early to sell when selling opens.</p>'
                    f'<div class="marketing-hero__actions">{btn("Shop the closet", "lg", P("catalog"))}{JOIN_EARLY}</div></div></div></section>')
    main = (hero_html
            + marquee("Hand-picked this week", [CATALOG[:8], CATALOG[8:16]])
            + band("shoppers-title", "For shoppers", "Hand-picked, every one for girls.", points(SHOPPER_POINTS_PHASE_ONE),
                   btn("Shop the closet", href=P("catalog")), "feature-band--blush")
            + worth(False, False)
            + band("how-title", "How it works", "Three steps to her next favourite.",
                   steps(STEPS_PHASE_ONE, STEP_ICONS_PHASE_ONE), mod="feature-band--stacked")
            + closing_html)
    return Page(main=main, user="guest", main_class="marketing", phase=1)


def welcome(member: bool, selling: bool = True) -> Page:
    sellers = (band("sellers-title", "For sellers", "Pass it on to the next girl.", points(SELLER_POINTS),
                    btn("Start selling", href=seller_href(member)), "feature-band--surface") if selling else
               band("sellers-title", "For sellers", "Selling opens soon.", points(SELLER_POINTS_SOON),
                    '<p class="feature-band__note">For now, every item is hand-picked by TuTuMuch.</p>', "feature-band--surface"))
    main = (hero(member, selling)
            + marquee()
            + band("shoppers-title", "For shoppers", "Everything here is for girls.", points(SHOPPER_POINTS),
                   btn("Shop the closet", href=P("catalog")), "feature-band--blush")
            + sellers
            + worth(member, selling)
            + band("how-title", "How it works", "Three steps. That’s it.", steps(), mod="feature-band--stacked")
            + closing(member, selling))
    return Page(main=main, user="member" if member else "guest", main_class="marketing", phase=3 if selling else 1)


wl = screen("welcome", "page", "Marketing page", route="/welcome",
            reqs=["L2-078", "L2-079", "L2-080", "L2-081", "L2-082", "L2-083", "L2-094"], na=STATIC_NA)


@wl.state("default", "Signed out: hero with the video poster, shoppers, sellers, three steps and a closing CTA; Start selling goes to /join.")
def _():
    pg = welcome(False)
    pg.notes = note("Tilly twirls in the hero (the one Tilly on the page) over a tulle field: canvas petals drift up and twirl around the pointer, and a tap throws sparkles. One still frame under reduced motion. The poster opens the video dialog; nothing autoplays.",
                    "top: 6rem; right: 1rem;")
    return pg


@wl.state("signed-in", "Signed in: the same page; Start selling goes to /sell.")
def _():
    return welcome(True)


@wl.state("selling-opens-soon", "Launch Phases 1 and 2, signed out: no Start selling link; the hero and the seller band say selling opens soon.")
def _():
    pg = welcome(False, selling=False)
    pg.notes = note("An administrator still sees Start selling (L2-094). The shopper band and steps are unchanged; the video is the shopper-only cut in every phase.", "top: 6rem; right: 1rem;")
    return pg


@wl.state("phase-one", "Launch Phase 1, signed out: the hand-picked closet. Marquee of the owner's listings, shopper journey, closet-worth calculator with Join early; no seller steps.")
def _():
    pg = welcome_phase_one()
    pg.notes = note("Phase 1 (L2-093, L2-094): only TuTuMuch sells, so How it works is the shopper's journey and the seller band folds into the closet-worth calculator. "
                    "Join early goes to /join; Shop the closet stays the one primary button.", "top: 6rem; right: 1rem;")
    return pg


# ---------------------------------------------------------------------------
# Pitch video dialog
# ---------------------------------------------------------------------------
def player(state: str) -> str:
    track_default = " default" if state == "captions" else ""
    video = (f'<video controls playsinline preload="metadata" poster="{MEDIA}tutumuch-pitch-poster.png" aria-label="TuTuMuch video">'
             f'<source src="{MEDIA}tutumuch-pitch.mp4" type="video/mp4"><source src="{MEDIA}tutumuch-pitch.webm" type="video/webm">'
             f'<track kind="captions" src="{MEDIA}tutumuch-pitch.vtt" srclang="en" label="English"{track_default}></video>')
    overlay = ""
    if state == "loading":
        overlay = ('<div class="video-player__state" role="status"><span class="spinner spinner--lg" aria-hidden="true"></span>'
                   '<p>Loading video…</p></div>')
    elif state == "error":
        overlay = (f'<div class="video-player__state" role="alert">{ic("alert", "icon icon--lg")}<strong>The video didn’t load</strong>'
                   f'<p>Check your connection and try again.</p>{btn("Try again", "primary", icon="refresh")}</div>')
    playing = state in ("default", "captions")
    play = btn("Pause", "primary", icon="pause") if playing else btn("Play", "primary", icon="play")
    cc_on = state == "captions"
    cc = btn("Captions", "", icon="captions", attrs=f'aria-pressed="{"true" if cc_on else "false"}"' + ("" if state != "error" else " disabled"))
    controls = "" if state == "error" else play + cc
    return (f'<div class="video-player"><div class="video-player__frame">{video}{overlay}</div>'
            f'<div class="video-player__controls">{controls}</div></div>')


pv = screen("pitch-video", "dialog", "Pitch video", opens_from="welcome", reqs=["L2-080", "L2-081", "L2-082"],
            na={"busy": "Playing a video sends nothing to the TuTuMuch API.", "invalid": "Nothing to fill in."})


@pv.state("default", "Playing after the visitor pressed Play: no visible heading, native controls plus Pause and Captions; captions off by default.")
def _():
    d = Dialog(bg=("welcome", "default"), html=dialog("TuTuMuch video", player("default"), size="lg", title_hidden=True))
    d.notes = note("Opens only from the poster button and plays with sound because the visitor asked. Space or K pauses; Esc closes and returns focus to the poster.",
                   "top: 1rem; left: 1rem;")
    return d


@pv.state("loading", "The video is buffering: spinner over the poster; Play and Captions stay usable.")
def _():
    return Dialog(bg=("welcome", "default"), html=dialog("TuTuMuch video", player("loading"), size="lg", title_hidden=True))


@pv.state("error", "The video failed to load: plain message and Try again; the page behind still works once closed.")
def _():
    return Dialog(bg=("welcome", "default"), html=dialog("TuTuMuch video", player("error"), size="lg", title_hidden=True))


@pv.state("captions", "Captions on: the English track shows; the Captions button is pressed.")
def _():
    d = Dialog(bg=("welcome", "default"), html=dialog("TuTuMuch video", player("captions"), size="lg", title_hidden=True))
    d.notes = note("Captions come from tutumuch-pitch.vtt and stay on for the rest of the visit.", "top: 1rem; left: 1rem;")
    return d
