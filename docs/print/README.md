# TuTuMuch print collateral

Business cards, die-cut stickers and flyers to hand out to parents. They use the TuTuMuch
wordmark (the same letters as `tl-logo`), Tilly from the Magic Patterns brand kit, the brand
palette in `docs/design-system/tokens/tokens.css` and the copy on `/welcome`, so what people
hold in their hands matches the site. See [ADR general/0007](../adr/general/0007-print-collateral-in-docs-print.md).

| | Print these | For review |
|---|---|---|
| Business card | `business-cards/tutumuch-business-card.pdf` (front, back), or the two 300 DPI PNGs | `*-guides.png` |
| Stickers (6) | `stickers/sticker-N-*.png` | `stickers/contact-sheet.png`, `*-preview.png` |
| Flyers (3) | `flyers/flyer-N-*-print-bleed.pdf` (print shop), `flyer-N-*-letter.pdf` (home printer) | `flyers/*-preview.png` |

## Business card

![Front](business-cards/tutumuch-card-front.png)
![Back](business-cards/tutumuch-card-back.png)

Quinntyne Brown, Founder · hello@tutumuch.kids · www.tutumuch.kids. Tilly waves from her stack
of folded clothes on the front; the back is the wordmark with its reversed ink shadow on
raspberry polka dots and "Twirl it forward."

| | |
|---|---|
| Trim | 3.5 × 2 in (standard North American card) |
| Bleed | 0.125 in on every side: the PDF and PNGs are 3.75 × 2.25 in |
| Safe area | 0.125 in inside the trim (the dashed line in the `-guides` PNGs) |
| PNG | 1125 × 675 px, 300 DPI (written into the file) |
| PDF | Two pages, front then back, live text with Titan One and Nunito embedded |

Order "3.5 × 2 in, with bleed" and upload the PDF. If the printer wants images, upload
`tutumuch-card-front.png` and `tutumuch-card-back.png`.

## Stickers

![All six stickers](stickers/contact-sheet.png)

Every sticker carries the website, **www.tutumuch.kids**, in the same white chip
(`.url` in `_build/stickers/_stage.css`).

1. **Never too much tutu!** Tilly's catchphrase under her twirl.
2. **The wordmark.** Tilly's face mark over the Gingham wordmark and "Twirl it forward."
3. **Hi, I'm Tilly!** Waving from the clothes stack, in a speech bubble.
4. **Twirl it forward.** The dance pose on a polka-dot disc.
5. **Outgrown? Pass it on!** The soccer pose, for sellers.
6. **Gently loved, newly adored.** A round badge with the Show & Tell pose.

### Print spec

| | |
|---|---|
| Canvas | 900 × 900 px = 3 × 3 in at 300 DPI (written into the file) |
| Background | Transparent; the die cut follows the alpha edge |
| Keyline | 60 px (0.2 in) of white beyond the artwork on every edge |
| Cut | One connected piece, checked by the renderer |
| Colour | sRGB; the printer converts to CMYK, and their proof is the check |
| Finish | Matte keeps the pastels truest; gloss also works |

Order **die-cut, custom shape, 3 × 3 in** (Sticker Mule, Jukebox, Vistaprint and others) and
upload the print PNG, not the preview. Their tool traces the cut from the transparent edge,
and the white keyline is sized for that. Check the proof: the cut should run around the outside
of the white border, not through the artwork.

### How the keyline is built

TwinkleTune's stickers are all vector, so their keyline is a fat white stroke behind each
shape. Tilly is a raster illustration, so here the renderer builds the keyline from the pixels
instead:

1. Capture the artwork (`.stage`) with a transparent background.
2. Dilate its alpha by a 60 px disc by drawing the artwork offset around rings of radius 60, 55,
   … 5.
3. Fill any enclosed transparent holes, so the sticker is solid white behind the art.
4. Paint that shape white, put the artwork on top, and check that it is one piece and clears the
   canvas edge.

## Flyers

| Flyer | For | Says |
|---|---|---|
| ![Join](flyers/flyer-1-join-preview.png) | **1. Join**: parents who might buy and sell | "Twirl it forward." The three selling steps (List it, Sell it, Get paid) and why to shop here |
| ![Shop](flyers/flyer-2-shop-preview.png) | **2. Shop**: buyers only | "Her next favourite thing is waiting." The six categories, from clothing to bikes and toys |
| ![Gear](flyers/flyer-3-shop-gear-preview.png) | **3. Shop gear**: sports and dance families | "Gear for girls who never sit still." Tilly's five sports poses plus bikes and scooters |

Every flyer ends with a QR code that opens https://www.tutumuch.kids.

| | |
|---|---|
| Trim | US Letter, 8.5 × 11 in |
| `-print-bleed.pdf` | 8.75 × 11.25 in: 0.125 in bleed for a print shop that trims |
| `-letter.pdf` | 8.5 × 11 in with the bleed cropped off, for a home or office printer ("fit to page" off) |
| Safe area | 0.375 in inside the trim. The renderer fails if any words or the QR code fall outside it |
| Preview PNG | 1680 × 2160 px, for review only |

Tilly's images are about 1024 px, so on a full-page flyer she prints at roughly 150 to 200 DPI.
That is fine for handouts. For posters, export larger images from Magic Patterns first.

## Regenerating

```bash
python -I docs/print/_build/cutout.py         # only when the Tilly originals change
node docs/print/_build/render.mjs             # everything
node docs/print/_build/render.mjs stickers    # or: cards, flyers
node docs/print/_build/render.mjs flyers flyer-2-shop   # one item
```

The renderer uses Playwright from the environment, or `@playwright/test` from
`src/frontend/node_modules` after `npm ci`. `cutout.py` needs Pillow and numpy. Rendering never
touches the network: the fonts and images are all local.

It fails loudly if a font or image did not load, if a sticker is not 900 × 900 RGBA, if its cut is
more than one piece or touches the canvas edge, or if any flyer words leave the safe area.

Edit the HTML sources, never the PNGs or PDFs.

```
docs/print/
  business-cards/   tutumuch-business-card.pdf, tutumuch-card-{front,back}[-guides].png
  stickers/         sticker-N-*.png (print), *-preview.png, contact-sheet.png
  flyers/           flyer-N-*-print-bleed.pdf, *-letter.pdf, *-preview.png
  characters/       Tilly cutouts (transparent PNG) + ATTRIBUTION.md
  _build/
    render.mjs      renders and checks everything
    cutout.py       removes Tilly's background
    print.css       fonts, palette, chip/dots/gingham idioms
    wordmark.js     the Gingham wordmark as live vector SVG
    qr-tutumuch-kids.svg   QR for https://www.tutumuch.kids (segno, error level M)
    business-cards/ stickers/ flyers/   the HTML sources, edit these
    characters-src/ the Magic Patterns originals
    fonts/          Titan One and Nunito with their SIL Open Font Licences
```

## Fonts

Titan One is the wordmark face; the app already ships it in
`src/frontend/projects/tutumuch/public/fonts/`. Nunito is Giddy's rounded sans
(`--font-family-sans`). Both are in `_build/fonts/` with their SIL Open Font Licence texts.
