// Shared flyer switches: ?trim=1 crops the 0.125in bleed off every side of each
// sheet (and sizes the PDF page to the trim: Letter, Half Letter or 5x7),
// ?guides=1 shows trim and safe lines. A two-sided flyer has two .sheet
// elements, front then back; each becomes one PDF page.
const params = new URLSearchParams(location.search)
const sheets = [...document.querySelectorAll('.sheet')]
if (params.get('trim')) {
  const w = sheets[0].offsetWidth - 24
  const h = sheets[0].offsetHeight - 24
  document.body.classList.add('trim')
  for (const sheet of sheets) {
    const page = document.createElement('div')
    page.className = 'page'
    page.style.width = `${w}px`
    page.style.height = `${h}px`
    sheet.replaceWith(page)
    page.append(sheet)
  }
  const style = document.createElement('style')
  style.textContent = `@page { size: ${w / 96}in ${h / 96}in; margin: 0; }`
  document.head.append(style)
}
if (params.get('guides')) document.body.classList.add('guides')
