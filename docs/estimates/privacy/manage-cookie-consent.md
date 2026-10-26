# Manage cookie consent — estimate

## Basis

The estimate assumes an intermediate Angular/TypeScript and ASP.NET Core/C# developer
who knows Angular signals and ASP.NET Core middleware but does not know this codebase.
The hours cover implementation, tests and review fixes for the
[Detailed design](../../detailed-designs/privacy/manage-cookie-consent/README.md). They
assume the shell footer, `ToastService`, the shared dialog and switch components and the
Playwright harness already exist. The analytics provider is `<TO SUPPLY>`; the hours
assume a provider with a script tag, an opt-out call and a documented list of cookie
names.

## Full design

| Work item | Hours |
|-----------|------:|
| `CookieConsentService` with consent cookie read and write and unit tests | 4 |
| `CookieConsentBannerComponent` with saved toast | 4 |
| `CookieSettingsDialogComponent` and `CookieSettingsDialogService` | 4 |
| Entry points: footer link, Settings "Cookies" row, Privacy page link | 2 |
| `AnalyticsService` start, stop, page views, timings and cookie deletion | 6 |
| Analytics origins in the Content Security Policy configuration | 1 |
| `CookieAllowListMiddleware`, `StrictlyNecessaryCookieOptions` and cookie policy | 4 |
| API integration test for `Set-Cookie` names | 2 |
| Playwright tests for the three `L2-056` criteria | 6 |
| Keyboard and axe checks for the banner and dialog | 2 |
| Review fixes | 2 |
| **Total** | **37** |

## Minimal scope

The minimal scope keeps the consent service, the banner, the dialog opened from the
footer, `AnalyticsService`, the CSP change and all tests. It leaves out
`CookieAllowListMiddleware`, relying on the API integration test to catch a
non-essential cookie. It also leaves out the Settings row and Privacy page entry points
and the saved toast. It still satisfies all of `L2-056`, because the footer link reaches
the dialog from every page. Minimal scope totals **32** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| `CookieAllowListMiddleware` | A non-essential API cookie is caught by tests only, not stripped at run time | 4 |
| Settings and Privacy page entry points | "Cookie settings" is reachable from the footer and banner only | 1 |
| Saved toast | No confirmation after a banner choice | 1 |
