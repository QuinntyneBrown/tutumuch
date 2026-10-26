/*
 * The TuTuMuch Gingham wordmark as live vector SVG, for print.
 *
 * A port of ginghamWordmarkScene() + renderSceneToSvg() from the Magic Patterns
 * brand kit (utils/brandArt.ts), with the same letter table as tl-logo
 * (src/frontend/projects/components/src/lib/logo/logo.ts): polka-dot "TuTu",
 * gingham "Much", a plum outline and a raspberry drop shadow. Every letter
 * bounces on its own baseline.
 *
 * Usage: <span data-wordmark></span>, optionally data-shadow="#3A2233" for the
 * reversed (ink-shadow) treatment. The element's CSS width sets the size; the
 * SVG is drawn at a 400px em and scaled, so it stays vector in the PDF.
 *
 * window.printReady resolves once fonts are loaded and every wordmark is drawn.
 * render.mjs waits on it before it captures anything.
 */

const LETTERS = [
  { ch: 'T', dy: -0.02, rot: -7, scale: 1.08, fill: 'dots' },
  { ch: 'u', dy: 0.07, rot: 5, scale: 1, fill: 'dots' },
  { ch: 'T', dy: -0.05, rot: -3, scale: 1.08, fill: 'dots' },
  { ch: 'u', dy: 0.06, rot: 6, scale: 1, fill: 'dots' },
  { ch: 'M', dy: -0.03, rot: -5, scale: 1.08, fill: 'gingham' },
  { ch: 'u', dy: 0.07, rot: 4, scale: 1, fill: 'gingham' },
  { ch: 'c', dy: 0.02, rot: -4, scale: 1, fill: 'gingham' },
  { ch: 'h', dy: -0.04, rot: 6, scale: 1, fill: 'gingham' },
]

const C = {
  ink: '#3A2233',
  raspberry: '#D6336C',
  lilac: '#B9A3E8',
  lavender: '#E3D8FA',
  white: '#FFFFFF',
  dot: '#F29BBC',
}

const FONT = `"Titan One"`
let uid = 0

function wordmarkSvg(shadow) {
  const fs = 400
  const id = `wm${uid++}`
  const measure = document.createElement('canvas').getContext('2d')
  const padX = fs * 0.12
  const baseline = fs * 1.0
  let x = padX
  const letters = LETTERS.map((l) => {
    const size = fs * l.scale
    measure.font = `${size}px ${FONT}`
    const w = measure.measureText(l.ch).width
    const letter = { ...l, x: x + w / 2, baseline: baseline + l.dy * fs, size }
    x += w - fs * 0.024
    return letter
  })
  const W = Math.ceil(x + fs * 0.24)
  const H = Math.ceil(baseline + fs * 0.32)
  const dot = fs * 0.17
  const cell = fs * 0.1

  const defs = `
    <pattern id="${id}d" patternUnits="userSpaceOnUse" width="${dot}" height="${dot}">
      <rect width="${dot}" height="${dot}" fill="${C.white}"/>
      <circle cx="${dot / 2}" cy="${dot / 2}" r="${dot * 0.23}" fill="${C.dot}"/>
    </pattern>
    <pattern id="${id}g" patternUnits="userSpaceOnUse" width="${cell * 2}" height="${cell * 2}">
      <rect width="${cell * 2}" height="${cell * 2}" fill="${C.lavender}"/>
      <rect width="${cell}" height="${cell}" fill="${C.lilac}"/>
      <rect x="${cell}" y="${cell}" width="${cell}" height="${cell}" fill="${C.lilac}"/>
    </pattern>`

  const glyphs = letters
    .map((l) => {
      const face = `url(#${id}${l.fill === 'dots' ? 'd' : 'g'})`
      const common = `font-family="'Titan One'" font-size="${l.size}" text-anchor="middle" stroke="${C.ink}" stroke-width="${l.size * 0.1}" stroke-linejoin="round" paint-order="stroke"`
      return `<g transform="translate(${l.x} ${l.baseline}) rotate(${l.rot})"><text ${common} x="${l.size * 0.07}" y="${l.size * 0.09}" fill="${shadow}">${l.ch}</text><text ${common} fill="${face}">${l.ch}</text></g>`
    })
    .join('')

  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${W} ${H}" role="img" aria-label="TuTuMuch"><defs>${defs}</defs>${glyphs}</svg>`
}

window.printReady = (async () => {
  await document.fonts.load(`400px ${FONT}`)
  await document.fonts.load(`800 40px "Nunito"`)
  await document.fonts.load(`600 40px "Nunito"`)
  await document.fonts.ready
  for (const el of document.querySelectorAll('[data-wordmark]')) {
    el.innerHTML = wordmarkSvg(el.dataset.shadow || C.raspberry)
  }
  const missing = [
    ['Titan One', `400px ${FONT}`],
    ['Nunito', '800 40px "Nunito"'],
  ].filter(([, spec]) => !document.fonts.check(spec))
  if (missing.length) throw new Error(`fonts did not load: ${missing.map(([n]) => n).join(', ')}`)
  await Promise.all([...document.images].map((img) => img.decode().catch(() => {})))
  return true
})()
