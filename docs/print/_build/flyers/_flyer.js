// Shared flyer switches: ?trim=1 crops the bleed (and sets a Letter page), ?guides=1 shows trim and safe lines.
const params = new URLSearchParams(location.search)
if (params.get('trim')) {
  document.body.classList.add('trim')
  const style = document.createElement('style')
  style.textContent = '@page { size: 8.5in 11in; margin: 0; }'
  document.head.append(style)
}
if (params.get('guides')) document.body.classList.add('guides')
