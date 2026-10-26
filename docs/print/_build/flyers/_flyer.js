// Shared flyer switches: ?trim=1 crops the 0.125in bleed off every side (and sizes
// the PDF page to the trim: Letter, or 5x7), ?guides=1 shows trim and safe lines.
const params = new URLSearchParams(location.search)
if (params.get('trim')) {
  const sheet = document.querySelector('.sheet')
  const w = sheet.offsetWidth - 24
  const h = sheet.offsetHeight - 24
  document.body.classList.add('trim')
  document.body.style.width = `${w}px`
  document.body.style.height = `${h}px`
  const style = document.createElement('style')
  style.textContent = `@page { size: ${w / 96}in ${h / 96}in; margin: 0; }`
  document.head.append(style)
}
if (params.get('guides')) document.body.classList.add('guides')
