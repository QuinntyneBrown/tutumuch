# 0001. Cookie consent kept in the browser, an API cookie allow-list and a placeholder analytics provider

Date: 2026-10-26

## Status

Accepted

## Context

The manage-cookie-consent design
([privacy/manage-cookie-consent](../../detailed-designs/privacy/manage-cookie-consent/README.md))
must set only strictly necessary cookies before consent, load analytics only after the visitor
accepts it, and let the visitor change the choice at any time (L2-056). The analytics provider
is not chosen (deferred), so its script URL, its cookie names and the way Tutu Much Web calls it are
unknown. The design also left the consent cookie lifetime, the toast text after "Accept
analytics" and the shape of the footer entry open. The API sets only the session cookies
`tm_access` and `tm_refresh` (platform ADR 0005); no anti-forgery cookie exists yet. Playwright
never reaches an external system (AGENTS.md).

## Decision

- **Consent lives only in the browser**, in the first-party cookie `tm_consent` with the value
  `v1.necessary` or `v1.analytics`, `Path=/`, `Secure`, `SameSite=Strict`, not `HttpOnly`, and
  `Max-Age` of 180 days. Any other value is no choice, so the banner returns. Nothing is sent to
  Tutu Much API, so saving has no busy or error state.
- **The consent code is in `@tutumuch/privacy`** and is exported to the app shell, as
  `PayoutBannerComponent` is from `@tutumuch/orders`: `CookieConsentService`, `AnalyticsService` with
  `provideAnalytics(config)`, the `tm-cookie-consent` banner and `CookieSettingsDialogService`.
  It is loaded with the shell, not a lazy route, because the banner shows on the first page.
- **The analytics provider is a configuration slot.** `environment.analytics` holds
  `scriptUrl` and `cookieNames`. Until a provider is chosen the script URL is
  `https://analytics.tutumuch.invalid/script.js`, which never resolves, and the cookie name is `_tma`.
  The script, once loaded, may expose `window.tutumuchAnalytics` with `pageView(url)` and
  `optOut()`; `AnalyticsService` is the only code that touches it. An empty `scriptUrl` turns
  analytics off. Playwright serves that origin with a fake script that sets `_tma`.
- **Withdrawing deletes the cookies the configuration names**, with `Max-Age=0` on `Path=/` for
  the host and the parent domain, after calling `optOut()`. An already loaded script stays in
  the page until the next full load; it receives no page views once consent is withdrawn.
- **"Cookie settings" in the footer is a button**, because it opens a dialog and goes nowhere.
  `tm-footer` emits `cookieSettings` and the shell opens the dialog.
- **Tutu Much API strips every cookie not on an allow-list.** `CookieAllowListMiddleware` runs first in
  the pipeline and, before headers are sent, removes each `Set-Cookie` whose name is not in
  `Privacy:Cookies:StrictlyNecessary` (`tm_access`, `tm_refresh`) and logs a warning. The
  anti-forgery cookie joins the list when the platform subsystem adds it.
- **The toast after "Accept analytics"** reads "Analytics on. Change it any time in the footer.",
  matching the mocked "Necessary only" text.

## Consequences

- Choosing a provider is a configuration change plus, at most, a small adapter script that
  exposes `tutumuchAnalytics`; no consent code changes.
- Consent is per browser, not per member: a member signing in on a new device is asked again.
- The Settings "Cookies" row and the Privacy page link are not built: neither page exists yet in
  `@tutumuch/accounts` or `@tutumuch/privacy`. They call `CookieSettingsDialogService.open()` when built.
- No Content Security Policy exists yet (L2-048); when it does, it must allow the provider's
  script and collection origins.
