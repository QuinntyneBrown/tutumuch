# Estimate — Responsive, accessible application shell

## Basis

The hours assume an intermediate Angular/TypeScript developer who knows the
Angular CDK and Playwright at a working level and does not yet know the TuTuMuch
codebase. They cover implementation, automated tests and review fixes, and
exclude requirements, design and mock work. The estimate assumes the mock
`tokens.css` and `ui.css` are ported into the Angular styles with the breakpoint
token fix, and that each feature builds its own pages, filter form and listing
detail layout. Fixtures for every route in `e2e/routes.ts` come partly from each
feature's tests; this estimate counts the harness and the shell's own fixtures.
[Detailed design](../../detailed-designs/platform/responsive-accessible-shell/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| Port tokens and component CSS; `_breakpoints.scss`; breakpoint token fix | 6 |
| `AppComponent`, `SkipLinkComponent`, four shell components with route `data.shell` | 5 |
| `TopbarComponent` (member, guest, responsive search and nav) and `AdminTopbarComponent` | 6 |
| `FooterComponent` and `AccountSubnavComponent` | 3 |
| `BreakpointService` | 2 |
| `SheetService` and `SheetComponent` (full and side variants, focus trap) | 6 |
| `NavMenuSheetComponent` (member and guest) | 4 |
| `DialogService` wrapper with focus restore, inert background, bottom-sheet layout | 4 |
| `RouteFocusService` and `TuTuMuchTitleStrategy` | 3 |
| `LiveRegionService` | 1 |
| `FormFieldComponent`, `FormErrorsDirective`, `ErrorSummaryComponent` with server-error mapping | 8 |
| `ToastService`, `ToastRegionComponent`, `ToastComponent` (auto-dismiss, pause, danger rules) | 6 |
| `SystemBannerService` and `SystemBannerComponent` | 2 |
| `ListingPhotoAltPipe`, reduced-motion handling, touch-target sizing | 3 |
| `e2e/routes.ts` harness with fixture API and `a11y.spec.ts` in both themes | 8 |
| `reflow.spec.ts`, `touch-targets.spec.ts`, `responsive-nav.spec.ts` | 5 |
| `visual.spec.ts` at five widths in pinned Docker image, baseline approval | 6 |
| `keyboard-journey.spec.ts` | 5 |
| Accessibility fixes found by axe across shell parts | 4 |
| **Total** | **87** |

## Minimal scope

The minimal version ships the shell components, `BreakpointService`, the
navigation menu and filter sheets, `DialogService`, the form-error directive and
summary, the toast and banner services, reduced-motion handling and touch-target
sizing, plus the axe-core suite in both themes and the 320 px reflow test. It
drops visual snapshots, the keyboard journey test, the touch-target and
responsive-navigation tests, `RouteFocusService` and the title strategy. It
satisfies `L2-062`, `L2-063`, `L2-064` and most of `L2-065`; it defers `L2-061`
AC3 (visual snapshots) and the automated proof of `L2-065` AC1. Minimal total:
**70** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Visual snapshot tests | Layout regressions at breakpoints are found by review only; `L2-061` AC3 unmet | 6 |
| Keyboard journey test | Keyboard-only completion of the main journeys is checked manually | 5 |
| `RouteFocusService` and title strategy | Screen reader users are not told that the page changed after navigation | 3 |
| Side-sheet variant for MD; use full-screen sheet below 992 px | MD filters open full screen instead of in a side panel (`L2-062` deviation) | 2 |
| Toast pause on hover and focus | Toasts can disappear while a user reads them | 1 |
