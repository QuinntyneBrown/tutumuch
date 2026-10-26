/**
 * Renders the TuTuMuch print collateral: die-cut stickers, business cards and
 * flyers.
 *
 *   node docs/print/_build/render.mjs              # everything
 *   node docs/print/_build/render.mjs stickers     # or: cards, flyers
 *   node docs/print/_build/render.mjs flyers flyer-1-join   # just one
 *
 * Playwright comes from the environment (`require('playwright')`), falling back
 * to the frontend workspace's @playwright/test after `npm ci` in src/frontend.
 *
 * Stickers: each source HTML is captured transparently at 900 x 900 (3in at
 * 300 DPI). The white die-cut keyline is then built from the pixels: the
 * artwork's alpha is dilated by a 60px disc, holes are filled, and the result
 * is checked to be one connected piece that clears the canvas edge. Tilly is
 * raster art, so unlike a vector sticker the keyline cannot be a stroke. The
 * edge URL (.rim) is captured separately, drawn on the white keyline, and
 * checked to stay inside it, as on the TwinkleTune stickers.
 *
 * Business cards and flyers: captured as 300 DPI PNGs and printed to PDF with
 * live text and fonts embedded.
 */
import { createRequire } from 'node:module'
import { fileURLToPath, pathToFileURL } from 'node:url'
import { readFile, writeFile, mkdir } from 'node:fs/promises'
import path from 'node:path'

const here = path.dirname(fileURLToPath(import.meta.url))
const printRoot = path.resolve(here, '..')
const repoRoot = path.resolve(printRoot, '..', '..')

function loadChromium() {
  try {
    return createRequire(import.meta.url)('playwright').chromium
  } catch {
    const req = createRequire(pathToFileURL(path.join(repoRoot, 'src', 'frontend', 'package.json')))
    return req('@playwright/test').chromium
  }
}

const STICKERS = [
  { slug: 'sticker-1-never-too-much-tutu', title: 'Never too much tutu!' },
  { slug: 'sticker-2-wordmark', title: 'The wordmark' },
  { slug: 'sticker-3-hi-im-tilly', title: 'Hi, I’m Tilly!' },
  { slug: 'sticker-4-twirl-it-forward', title: 'Twirl it forward.' },
  { slug: 'sticker-5-pass-it-on', title: 'Outgrown? Pass it on!' },
  { slug: 'sticker-6-gently-loved', title: 'Gently loved, newly adored' },
]

// Trim sizes in inches. Every sheet adds 0.125in bleed on each side; `safe` is
// how far in from the bleed edge (CSS px) words and the QR code must stay.
const LETTER = { w: 8.5, h: 11, safe: 48, name: 'letter' } // 0.375in inside the trim
const CARD_5X7 = { w: 5, h: 7, safe: 36, name: '5x7' } // 0.25in inside the trim

const FLYERS = [
  { slug: 'flyer-1-join', title: 'Join TuTuMuch: buy and sell', size: LETTER },
  { slug: 'flyer-2-shop', title: 'Shop new and gently loved', size: LETTER },
  { slug: 'flyer-3-shop-gear', title: 'Shop bikes, sports and dance gear', size: LETTER },
  { slug: 'flyer-4-shop-5x7', title: 'Shop new and gently loved, 5x7', size: CARD_5X7 },
  { slug: 'flyer-5-shop-gear-5x7', title: 'Shop sports and dance gear, 5x7', size: CARD_5X7 },
]

const KEYLINE = 60 // px of white beyond the artwork: 0.2in at 300 DPI
const DPI = 300
const CSS_DPI = 96

/* ---------- PNG helpers ---------- */

const CRC_TABLE = Array.from({ length: 256 }, (_, n) => {
  let c = n
  for (let k = 0; k < 8; k += 1) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1
  return c >>> 0
})

function crc32(buf) {
  let c = 0xffffffff
  for (const b of buf) c = CRC_TABLE[(c ^ b) & 0xff] ^ (c >>> 8)
  return (c ^ 0xffffffff) >>> 0
}

/** Adds a pHYs chunk so print software reads the PNG at 300 DPI, not 72. */
function withDpi(png, dpi = DPI) {
  const ppm = Math.round(dpi / 0.0254)
  const data = Buffer.alloc(9)
  data.writeUInt32BE(ppm, 0)
  data.writeUInt32BE(ppm, 4)
  data.writeUInt8(1, 8)
  const type = Buffer.from('pHYs')
  const chunk = Buffer.alloc(21)
  chunk.writeUInt32BE(9, 0)
  type.copy(chunk, 4)
  data.copy(chunk, 8)
  chunk.writeUInt32BE(crc32(Buffer.concat([type, data])), 17)
  const ihdrEnd = 8 + 25 // signature + IHDR chunk
  return Buffer.concat([png.subarray(0, ihdrEnd), chunk, png.subarray(ihdrEnd)])
}

function pngHeader(buffer) {
  return { width: buffer.readUInt32BE(16), height: buffer.readUInt32BE(20), colourType: buffer.readUInt8(25) }
}

const fromDataUrl = (url) => Buffer.from(url.slice(url.indexOf(',') + 1), 'base64')

/* ---------- Pages ---------- */

async function openPage(browser, file, { width, height, scale = 1, query = '' }) {
  const context = await browser.newContext({ viewport: { width, height }, deviceScaleFactor: scale })
  const page = await context.newPage()
  const errors = []
  page.on('pageerror', (e) => errors.push(e.message))
  page.on('requestfailed', (r) => errors.push(`failed to load ${r.url()}`))
  await page.goto(pathToFileURL(file).href + query, { waitUntil: 'load' })
  await page.evaluate(() => window.printReady)
  const broken = await page.evaluate(() =>
    [...document.images].filter((i) => !i.complete || i.naturalWidth === 0).map((i) => i.src)
  )
  if (broken.length) errors.push(`images did not load: ${broken.join(', ')}`)
  if (errors.length) throw new Error(`${path.basename(file)}: ${errors.join('; ')}`)
  return { context, page }
}

/* ---------- Stickers ---------- */

/**
 * Runs in the browser. Dilates the artwork's alpha by a disc of radius
 * `keyline` (unions of the artwork shifted around concentric rings), fills
 * enclosed holes, and composites white keyline + artwork + the edge URL. The
 * edge URL (`rim`) is drawn after the keyline, so it never grows the cut; it
 * is checked to sit inside the white, clear of the cut line and the artwork.
 */
async function composeSticker({ art, rim, keyline, preview }) {
  const load = async (src) => {
    const im = new Image()
    im.src = src
    await im.decode()
    return im
  }
  const img = await load(art)
  const rimImg = await load(rim)
  const W = img.width
  const H = img.height

  const mask = document.createElement('canvas')
  mask.width = W
  mask.height = H
  const m = mask.getContext('2d', { willReadFrequently: true })
  for (let r = keyline; r > 0; r -= 5) {
    const steps = Math.ceil((2 * Math.PI * r) / 3)
    for (let i = 0; i < steps; i += 1) {
      const t = (i / steps) * 2 * Math.PI
      m.drawImage(img, Math.cos(t) * r, Math.sin(t) * r)
    }
  }
  m.drawImage(img, 0, 0)

  const data = m.getImageData(0, 0, W, H)
  const px = data.data
  const solid = new Uint8Array(W * H)
  for (let i = 0; i < W * H; i += 1) solid[i] = px[i * 4 + 3] >= 128 ? 1 : 0

  // Flood the outside from the border; anything transparent that is not
  // reached is an enclosed hole and becomes part of the sticker.
  const outside = new Uint8Array(W * H)
  const stack = []
  const push = (i) => {
    if (!outside[i] && !solid[i]) {
      outside[i] = 1
      stack.push(i)
    }
  }
  for (let x = 0; x < W; x += 1) {
    push(x)
    push((H - 1) * W + x)
  }
  for (let y = 0; y < H; y += 1) {
    push(y * W)
    push(y * W + W - 1)
  }
  while (stack.length) {
    const i = stack.pop()
    const x = i % W
    if (x > 0) push(i - 1)
    if (x < W - 1) push(i + 1)
    if (i >= W) push(i - W)
    if (i < W * (H - 1)) push(i + W)
  }

  // Count connected pieces: a die cut must be one piece.
  const seen = new Uint8Array(W * H)
  let pieces = 0
  for (let s = 0; s < W * H; s += 1) {
    if (outside[s] || seen[s]) continue
    pieces += 1
    seen[s] = 1
    const q = [s]
    while (q.length) {
      const i = q.pop()
      const x = i % W
      const next = [x > 0 ? i - 1 : -1, x < W - 1 ? i + 1 : -1, i - W, i + W]
      for (const n of next) {
        if (n >= 0 && n < W * H && !outside[n] && !seen[n]) {
          seen[n] = 1
          q.push(n)
        }
      }
    }
  }

  let minX = W, minY = H, maxX = -1, maxY = -1
  for (let i = 0; i < W * H; i += 1) {
    const a = outside[i] ? (solid[i] ? 255 : px[i * 4 + 3]) : 255
    // Keep the dilation's antialiased rim on the outside edge only.
    const alpha = outside[i] ? Math.min(255, a * 2) : 255
    px[i * 4] = px[i * 4 + 1] = px[i * 4 + 2] = 255
    px[i * 4 + 3] = alpha
    if (alpha > 8) {
      const x = i % W
      const y = (i - x) / W
      if (x < minX) minX = x
      if (x > maxX) maxX = x
      if (y < minY) minY = y
      if (y > maxY) maxY = y
    }
  }
  m.putImageData(data, 0, 0)

  // The edge URL must sit in the white rim: at least 10px inside the cut line
  // and at least 6px clear of the artwork.
  const probe = document.createElement('canvas')
  probe.width = W
  probe.height = H
  const p = probe.getContext('2d', { willReadFrequently: true })
  p.drawImage(img, 0, 0)
  const artA = p.getImageData(0, 0, W, H).data
  p.clearRect(0, 0, W, H)
  p.drawImage(rimImg, 0, 0)
  const rimA = p.getImageData(0, 0, W, H).data
  const cutA = px
  const at = (arr, x, y) => (x < 0 || y < 0 || x >= W || y >= H ? 0 : arr[(y * W + x) * 4 + 3])
  let rimPixels = 0
  const rimFaults = { cut: 0, art: 0, first: null }
  for (let y = 0; y < H; y += 1) {
    for (let x = 0; x < W; x += 1) {
      if (at(rimA, x, y) <= 16) continue
      rimPixels += 1
      const nearCut = [[10, 0], [-10, 0], [0, 10], [0, -10], [7, 7], [-7, 7], [7, -7], [-7, -7]].some(([dx, dy]) => at(cutA, x + dx, y + dy) < 255)
      const onArt = [[0, 0], [6, 0], [-6, 0], [0, 6], [0, -6]].some(([dx, dy]) => at(artA, x + dx, y + dy) > 16)
      if (nearCut) rimFaults.cut += 1
      if (onArt) rimFaults.art += 1
      if ((nearCut || onArt) && !rimFaults.first) rimFaults.first = { x, y }
    }
  }

  const out = document.createElement('canvas')
  const pad = preview ? 60 : 0
  out.width = W + pad * 2
  out.height = H + pad * 2
  const o = out.getContext('2d')
  if (preview) {
    o.fillStyle = '#FFF8F5'
    o.fillRect(0, 0, out.width, out.height)
    o.filter = 'drop-shadow(0 14px 22px rgba(58, 34, 51, 0.25))'
  }
  o.drawImage(mask, pad, pad)
  o.filter = 'none'
  o.drawImage(img, pad, pad)
  o.drawImage(rimImg, pad, pad)
  return { png: out.toDataURL('image/png'), pieces, box: { minX, minY, maxX, maxY }, rimPixels, rimFaults }
}

async function renderStickers(browser) {
  const dir = path.join(printRoot, 'stickers')
  await mkdir(dir, { recursive: true })
  const blank = await (await browser.newContext()).newPage()

  for (const { slug, title } of STICKERS.filter((st) => !pick || st.slug === pick)) {
    const file = path.join(here, 'stickers', `${slug}.html`)
    const { context, page } = await openPage(browser, file, { width: 900, height: 900 })
    const shot = async (mode) => {
      await page.evaluate((m) => {
        document.body.classList.remove('art-only', 'rim-only')
        document.body.classList.add(m)
      }, mode)
      const png = await page.locator('.stage').screenshot({ omitBackground: true, animations: 'disabled' })
      return `data:image/png;base64,${png.toString('base64')}`
    }
    const art = await shot('art-only')
    const rim = await shot('rim-only')
    await context.close()

    const print = await blank.evaluate(composeSticker, { art, rim, keyline: KEYLINE, preview: false })
    const preview = await blank.evaluate(composeSticker, { art, rim, keyline: KEYLINE, preview: true })

    const png = withDpi(fromDataUrl(print.png))
    const { width, height, colourType } = pngHeader(png)
    const { minX, minY, maxX, maxY } = print.box
    if (width !== 900 || height !== 900) throw new Error(`${slug}: expected 900x900, got ${width}x${height}`)
    if (colourType !== 6) throw new Error(`${slug}: expected RGBA, got colour type ${colourType}`)
    if (print.pieces !== 1) throw new Error(`${slug}: the die cut is ${print.pieces} pieces, not one`)
    if (print.rimPixels < 500) throw new Error(`${slug}: no edge URL (.rim) was drawn`)
    const { cut, art: onArt, first } = print.rimFaults
    if (cut || onArt) {
      throw new Error(
        `${slug}: the edge URL leaves the white rim (${cut} px near the cut line, ${onArt} px on the artwork; first at ${first.x},${first.y})`
      )
    }
    if (minX < 4 || minY < 4 || maxX > width - 5 || maxY > height - 5) {
      throw new Error(`${slug}: the keyline touches the canvas edge (x ${minX}-${maxX}, y ${minY}-${maxY})`)
    }

    await writeFile(path.join(dir, `${slug}.png`), png)
    await writeFile(path.join(dir, `${slug}-preview.png`), fromDataUrl(preview.png))

    const inches = (n) => (n / DPI).toFixed(2)
    const w = maxX - minX + 1
    const h = maxY - minY + 1
    console.log(
      `✓ ${title.padEnd(28)} ${`${slug}.png`.padEnd(38)} cut ${w}x${h}px (${inches(w)}" x ${inches(h)}")  margins L${minX} R${width - 1 - maxX} T${minY} B${height - 1 - maxY}`
    )
  }

  // Contact sheet: all six at review size, on paper.
  const tiles = await Promise.all(
    STICKERS.map(async ({ slug, title }) => {
      const data = (await readFile(path.join(dir, `${slug}.png`))).toString('base64')
      return `<figure><img src="data:image/png;base64,${data}" width="380"><figcaption>${title}</figcaption></figure>`
    })
  )
  await blank.setContent(
    `<body style="margin:0;padding:48px;background:#FFF8F5;display:grid;grid-template-columns:repeat(3,380px);gap:40px 48px;font:700 18px system-ui;color:#3A2233;width:max-content">
       <style>figure{margin:0;text-align:center}figcaption{margin-top:10px}img{display:block;filter:drop-shadow(0 8px 14px rgba(58,34,51,.22))}</style>
       ${tiles.join('')}
     </body>`
  )
  await blank.waitForTimeout(300)
  await blank.locator('body').screenshot({ path: path.join(dir, 'contact-sheet.png') })
  console.log(`✓ ${'contact sheet'.padEnd(28)} contact-sheet.png`)
  await blank.context().close()
}

/* ---------- Business cards ---------- */

async function renderCards(browser) {
  const dir = path.join(printRoot, 'business-cards')
  await mkdir(dir, { recursive: true })
  const file = path.join(here, 'business-cards', 'business-card.html')
  const scale = DPI / CSS_DPI

  for (const side of ['front', 'back']) {
    for (const guides of [false, true]) {
      const query = `?side=${side}${guides ? '&guides=1' : ''}`
      const { context, page } = await openPage(browser, file, { width: 360, height: 216, scale, query })
      const png = await page.locator('.card').screenshot()
      const { width, height } = pngHeader(png)
      if (width !== 1125 || height !== 675) throw new Error(`card ${side}: expected 1125x675, got ${width}x${height}`)
      const name = `tutumuch-card-${side}${guides ? '-guides' : ''}.png`
      await writeFile(path.join(dir, name), withDpi(png))
      await context.close()
      console.log(`✓ ${`card ${side}${guides ? ' (guides)' : ''}`.padEnd(28)} ${name}`)
    }
  }

  const { context, page } = await openPage(browser, file, { width: 360, height: 216, query: '?side=both' })
  const pdf = path.join(dir, 'tutumuch-business-card.pdf')
  await page.pdf({ path: pdf, width: '3.75in', height: '2.25in', printBackground: true, pageRanges: '1-2' })
  await context.close()
  console.log(`✓ ${'card PDF (front, back)'.padEnd(28)} tutumuch-business-card.pdf`)
}

/* ---------- Flyers ---------- */

async function renderFlyers(browser) {
  const dir = path.join(printRoot, 'flyers')
  await mkdir(dir, { recursive: true })

  for (const { slug, title, size } of FLYERS.filter((f) => !pick || f.slug === pick)) {
    const file = path.join(here, 'flyers', `${slug}.html`)
    const bleedIn = { w: size.w + 0.25, h: size.h + 0.25 }
    const W = bleedIn.w * CSS_DPI
    const H = bleedIn.h * CSS_DPI

    const { context, page } = await openPage(browser, file, { width: W, height: H, scale: 2 })

    // Words and the QR code must sit inside the safe area.
    const unsafe = await page.evaluate(
      ({ safe, W, H }) => {
        const right = W - safe
        const bottom = H - safe
        return [...document.querySelectorAll('.sheet :is(p, h1, h2, li, .chip, .cta__qr, [data-wordmark])')]
          .map((el) => {
            // Measure the content, not the box: nowrap text can overflow its paragraph.
            const range = document.createRange()
            range.selectNodeContents(el)
            const box = el.getBoundingClientRect()
            // The wordmark's SVG box already includes its padding; its glyph boxes are not ink.
            const ink = el.matches('[data-wordmark], .cta__qr') ? box : range.getBoundingClientRect()
            const r = {
              left: Math.min(box.left, ink.width ? ink.left : box.left),
              top: Math.min(box.top, ink.height ? ink.top : box.top),
              right: Math.max(box.right, ink.right),
              bottom: Math.max(box.bottom, ink.bottom),
            }
            return { el, r }
          })
          .filter(({ r }) => r.left < safe - 0.5 || r.top < safe - 0.5 || r.right > right + 0.5 || r.bottom > bottom + 0.5)
          .map(({ el, r }) => `"${el.textContent.trim().slice(0, 30) || el.className}" (${Math.round(r.left)},${Math.round(r.top)})-(${Math.round(r.right)},${Math.round(r.bottom)})`)
      },
      { safe: size.safe, W, H }
    )
    if (unsafe.length) throw new Error(`${slug}: outside the safe area: ${unsafe.join('; ')}`)

    const preview = await page.locator('.sheet').screenshot()
    await writeFile(path.join(dir, `${slug}-preview.png`), preview)
    await page.pdf({ path: path.join(dir, `${slug}-print-bleed.pdf`), width: `${bleedIn.w}in`, height: `${bleedIn.h}in`, printBackground: true, pageRanges: '1' })
    await context.close()

    // The trimmed PDF: Letter for a home printer, or the 5x7 card without its bleed.
    const trimmed = size === LETTER ? `${slug}-letter.pdf` : `${slug}.pdf`
    const home = await openPage(browser, file, { width: W, height: H, query: '?trim=1' })
    await home.page.pdf({ path: path.join(dir, trimmed), width: `${size.w}in`, height: `${size.h}in`, printBackground: true, pageRanges: '1' })
    await home.context.close()
    console.log(`✓ ${title.padEnd(34)} ${slug}-print-bleed.pdf, ${trimmed}, ${slug}-preview.png`)
  }
}

/* ---------- Main ---------- */

const only = process.argv[2]
const pick = process.argv[3] // optional: one sticker or flyer slug
const browser = await loadChromium().launch({ args: ['--allow-file-access-from-files', '--font-render-hinting=none'] })
try {
  if (!only || only === 'stickers') await renderStickers(browser)
  if (!only || only === 'cards') await renderCards(browser)
  if (!only || only === 'flyers') await renderFlyers(browser)
} finally {
  await browser.close()
}
