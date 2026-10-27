# Estimate — Page guidelines conformance

## Basis

The hours assume an intermediate Angular/TypeScript developer who does not yet know the TuTuMuch
codebase. They cover implementation, automated tests and review fixes, and exclude requirements,
design and mock work. They assume the app shell, the components library and every feature screen
exist. [Detailed design](../../detailed-designs/platform/page-guidelines-conformance/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| Token file, contrast pairs, self-hosted Fraunces and DM Sans | 4 |
| Page-guidelines layer in `components.css` (buttons, chips, badges, photos, skeletons, top bar, filters) | 6 |
| `tl-mark`, `tl-tilly`, `tl-condition-badge`, `tl-skeleton`; `tl-logo` tone and responsive mark | 6 |
| `tl-top-bar` two rows and category order, `tl-footer`, `tl-listing-card` and grid, `tl-gallery` sold | 5 |
| App shell `wide`; catalog headers, filter order, listing detail badge and sold state | 5 |
| Skeletons, empty states, marks and Tilly across the feature libraries; copy and terms | 6 |
| `page-guidelines.spec.ts`, `PageGuidelines` page object and updates to existing specs | 8 |
| Visual check at phone and desktop, light and dark, and fixes | 4 |
| **Total** | **44** |

## Minimal scope

The minimal version keeps the tokens, fonts, buttons, cards and copy and drops Tilly, the mark and
the filter reorder. Minimal total: **28** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Tilly in headers, empty states and confirmations | The character only on the marketing page | 4 |
| Responsive mark | The wordmark shrinks under 640px instead | 2 |
| "More filters" disclosure | Every filter group stays open | 2 |
