/*
 * Lays each sticker's edge URL along an edge of its artwork, inside the 60px
 * white keyline (the TwinkleTune idiom: curved tracked caps in the rim).
 *
 *   <svg class="rim"><text data-under=".banner">TUTUMUCH.KIDS</text></svg>
 *
 * One of:
 *   data-under="<selector>"   straight, parallel to that element's bottom edge (follows its rotation)
 *   data-over="<selector>"    straight, parallel to its top edge
 *   data-ellipse="cx cy rx ry" plus data-side="top|bottom": around an ellipse or circle
 *                              (stage px, measured to the artwork's outer edge)
 * data-gap is the distance from the artwork edge to the middle of the capitals
 * (default 30, the centre of the 60px rim). data-shadow adds an element's hard
 * box-shadow depth to its bottom edge.
 *
 * Chains onto window.printReady, so it runs once fonts are in and chips have
 * their final size.
 */
const NS = 'http://www.w3.org/2000/svg'

function corners(el) {
  const L = el.offsetLeft
  const T = el.offsetTop
  const W = el.offsetWidth
  const H = el.offsetHeight
  const m = new DOMMatrixReadOnly(getComputedStyle(el).transform === 'none' ? undefined : getComputedStyle(el).transform)
  const [ox, oy] = getComputedStyle(el).transformOrigin.split(' ').map(parseFloat)
  const at = (x, y) => {
    const p = m.transformPoint(new DOMPoint(x - ox, y - oy))
    return { x: p.x + L + ox, y: p.y + T + oy }
  }
  return { tl: at(0, 0), tr: at(W, 0), bl: at(0, H), br: at(W, H) }
}

function layout(text, i) {
  const size = parseFloat(getComputedStyle(text).fontSize)
  const cap = size * 0.72 // Nunito cap height
  const gap = parseFloat(text.dataset.gap ?? '30')
  const shadow = parseFloat(text.dataset.shadow ?? '0')
  let d

  if (text.dataset.under || text.dataset.over) {
    const under = Boolean(text.dataset.under)
    const el = document.querySelector(text.dataset.under || text.dataset.over)
    const c = corners(el)
    const [a, b] = under ? [c.bl, c.br] : [c.tl, c.tr]
    const len = Math.hypot(b.x - a.x, b.y - a.y)
    const u = { x: (b.x - a.x) / len, y: (b.y - a.y) / len }
    const n = under ? { x: -u.y, y: u.x } : { x: u.y, y: -u.x } // outward normal
    // Bottom: capitals hang above the baseline, so the baseline sits further out.
    const off = under ? shadow + gap + cap / 2 : gap - cap / 2
    const mid = { x: (a.x + b.x) / 2 + n.x * off, y: (a.y + b.y) / 2 + n.y * off }
    d = `M ${mid.x - u.x * 450} ${mid.y - u.y * 450} L ${mid.x + u.x * 450} ${mid.y + u.y * 450}`
  } else {
    const [cx, cy, rx, ry] = text.dataset.ellipse.split(/\s+/).map(Number)
    const top = text.dataset.side === 'top'
    const off = top ? gap - cap / 2 : gap + cap / 2
    const Rx = rx + off
    const Ry = ry + off
    // Through the top: clockwise (sweep 1). Through the bottom: anticlockwise (sweep 0),
    // so the letters stay upright with their feet towards the outside.
    d = `M ${cx - Rx} ${cy} A ${Rx} ${Ry} 0 0 ${top ? 1 : 0} ${cx + Rx} ${cy}`
  }

  const svg = text.ownerSVGElement
  svg.setAttribute('viewBox', '0 0 900 900')
  const path = document.createElementNS(NS, 'path')
  path.id = `rim-path-${i}`
  path.setAttribute('d', d)
  path.setAttribute('fill', 'none')
  svg.prepend(path)
  const label = text.textContent.trim()
  text.textContent = ''
  text.setAttribute('text-anchor', 'middle')
  const tp = document.createElementNS(NS, 'textPath')
  tp.setAttribute('href', `#${path.id}`)
  tp.setAttribute('startOffset', '50%')
  tp.textContent = label
  text.append(tp)
}

window.printReady = window.printReady.then(() => {
  document.querySelectorAll('.rim text').forEach(layout)
  return true
})
