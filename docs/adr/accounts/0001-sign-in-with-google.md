# ADR-0001: Sign in with Google through a Server-Side Authorization Code Flow

**Date:** 2026-10-27
**Category:** accounts
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

Members join and sign in with an email and a password (`L2-001`, `L2-006`). The owner wants
"Continue with Google" on `/sign-in` and `/join` of the full application (`L2-088`), on
`https://uat.tutumuch.kids` ([ADR platform/0013](../platform/0013-uat-host-in-the-production-web-image.md))
and locally, without weakening the session model of [ADR platform/0005](../platform/0005-session-cookies-with-jwt-access-and-rotating-refresh-tokens.md),
the planned `script-src 'self'` of `L2-048`, the cookie rules of `L2-056` or the statelessness
of `L2-060`. Google is an external system, so tests must never reach it (`AGENTS.md`).

## Decision

- **The flow.** The server-side OpenID Connect authorization code flow with PKCE.
  `GET /api/auth/google/start?returnUrl=…` creates a random `state`, nonce and PKCE verifier,
  keeps them with the safe `returnUrl` in the cookie `tl_google_flow` (`HttpOnly`, `Secure`,
  `SameSite=Lax`, path `/api/auth/google`, 10 minutes, HMAC-signed with `Auth:SigningKey` under
  a `google-flow` label so it cannot be forged or reused for a session token) and redirects the
  browser to Google. Google sends the browser back to `GET /api/auth/google/callback` on the
  same origin. The cookie is `Lax`, not `Strict`, because a `Strict` cookie is not sent on the
  cross-site redirect back from Google. No Google script, frame or request runs in the page, so
  the CSP of `harden-http-and-api` does not change and cookie consent is not involved
  ([ADR privacy/0001](../privacy/0001-cookie-consent-in-the-browser-and-a-placeholder-analytics-provider.md)).
- **Validation.** The callback checks `state` against the cookie, then
  `IGoogleIdentityProvider` exchanges the code with the PKCE verifier and validates the ID
  token's signature against Google's published keys (discovery document
  `https://accounts.google.com/.well-known/openid-configuration`), issuer
  (`https://accounts.google.com` or `accounts.google.com`), audience (the client ID) and
  expiry. The handler then checks the nonce and `email_verified`. Any failure answers the same
  way: no session, nothing stored, back to `/sign-in?google=failed&ref=…`.
- **Where the adapter lives.** The port `IGoogleIdentityProvider` is in
  `TuTuMuch.Application.Abstractions`; the adapter `GoogleIdentityProvider` sits in
  `TuTuMuch.Api/Auth/` next to `SessionCookies`, because it reuses the JWT and OpenID Connect
  libraries the API already carries for its own bearer tokens and `IHttpClientFactory` from the
  web host; this adds no package. Tests replace it with `FakeGoogleIdentityProvider`.
- **Identity storage.** `ExternalLogins` (`Id`, `MemberId`, `Provider`, `Subject`,
  `CreatedAt`) with a unique index on `Provider` + `Subject`, keyed by Google's `sub`, never by
  email, and deleted with the member (cascade). `Members.PasswordHash` becomes nullable;
  `SignInCommandHandler` treats a missing hash as a wrong password, so a Google-only member
  gets exactly "Email or password is incorrect" (`L2-006` criterion 2). Only the subject and
  the email are kept from Google; no name or photo.
- **Linking an existing email.** A Google account whose verified email matches a member whose
  own email is verified links automatically and signs in: both sides have proved control of the
  same address. One that matches a member whose email is **not** verified is refused with
  `/sign-in?google=existing`, because that member may have been registered by someone else
  with this address (a pre-registered account takeover); the person signs in with their
  password and verifies the email first, after which Google links on its next sign-in. A closed
  member is refused as a failure. There is no settings page to link or unlink; linking happens
  only through a sign-in.
- **The pending new-person step.** When no member matches, the callback stores a
  `PendingExternalJoin` (`Provider`, `Subject`, `Email`, SHA-256 `TokenHash`, `CreatedAt`,
  `ExpiresAt` = 10 minutes, `UsedAt`) in the database, puts the raw token in the cookie
  `tl_google_join` (`HttpOnly`, `Secure`, `SameSite=Strict`, path `/api/auth/google`,
  10 minutes) and sends the browser to `/join/google?returnUrl=…`. The page reads the email
  with `GET /api/auth/google/pending` and finishes with `POST /api/auth/google/finish`, which
  applies the display name, age and terms rules of `L2-001` through the same
  `MemberRegistrationRules` the password registration uses, creates the member with
  `EmailVerifiedAt` set and no password, links the Google account, marks the pending join
  used and starts the session. The step is single use and holds no in-process state
  (`L2-060`).
- **Configuration and hosts.** `Auth:Google:ClientId` and `Auth:Google:ClientSecret` from the
  environment (`Auth__Google__ClientId`, `Auth__Google__ClientSecret` in `/opt/tutumuch/.env`;
  `dotnet user-secrets` locally). Both or neither: the API refuses to start with one of them.
  With neither, `GET /api/auth/providers` answers `{ "google": false }`, the button does not
  render and every Google endpoint answers `404 GOOGLE_NOT_CONFIGURED`. The redirect URI is
  `{scheme}://{host}/api/auth/google/callback` built from the request (the scheme from
  `X-Forwarded-Proto` behind Caddy), or `Auth:Google:RedirectUri` when set; it is never built
  from `App:BaseUrl`, which is the marketing host. The Google client's authorised origin and
  redirect URI are `https://uat.tutumuch.kids` and the local development origin only.
- **Abuse limits.** The Google endpoints take the per-IP sign-in limits of `L2-007` and
  `L2-051` when those are built; no sign-in endpoint is limited today, and the Google ones
  join the same deferred item rather than getting a limiter of their own.
- **The mark and the label.** The button is the design system's secondary pill (`lg`, `block`)
  with Google's published "G" mark and the label "Continue with Google", one of the three
  labels Google allows. The mark's four colours are Google's and are fixed in both themes; this
  is the one element outside the Giddy tokens, allowed because a third party's mark may not be
  recoloured, and recorded on the Provider button page of the design system.

## Options Considered

### Option 1: Server-side authorization code flow with PKCE (chosen)
- **Pros:** No third-party script in the page, so `script-src 'self'` holds and consent is untouched; the code never reaches the browser's JavaScript; one place validates the token; the fake port makes every path testable.
- **Cons:** Two redirects; a `Lax` flow cookie; the API must know its public origin.

### Option 2: Google Identity Services button posting an ID token to the API
- **Pros:** Google's own button and One Tap; no code exchange.
- **Cons:** Loads `accounts.google.com` script, needs `script-src`, `frame-src` and `connect-src` entries, sets Google cookies in the page that cookie consent would have to govern, and the ID token passes through page JavaScript.

### Option 3: Link by email automatically, verified or not
- **Pros:** Fewer refusals.
- **Cons:** An account registered with someone else's address, never verified, would be handed to whoever signs in with Google; rejected.

## Consequences

### Positive
- A member can join or sign in with one press and come back with the same cookies and the same `returnUrl` handling as a password sign-in.
- Password sign-in, registration, verification, refresh and sign-out are unchanged.

### Negative
- A member whose email is unverified cannot use Google until they verify it.
- `PendingExternalJoins` rows older than 10 minutes stay until a cleanup is added (deferred; they hold only a subject, an email and a hash).
- Google's keys are fetched over the network at first use and refreshed by the configuration manager; the first Google sign-in after a start takes one extra round trip.

### Risks
- The redirect URI depends on the proxy's `X-Forwarded-Proto`. Mitigation: `Auth:Google:RedirectUri` overrides it, and the smoke test of the owner steps in `docs/deployment.md` checks the start endpoint.
- A code or token must never be logged. Mitigation: the adapter logs only outcomes; `TuTuMuch.Api.Tests` assert no code or token in any response body.

## References

- [sign-in-with-google design](../../detailed-designs/accounts/sign-in-with-google/README.md)
- [ADR platform/0005](../platform/0005-session-cookies-with-jwt-access-and-rotating-refresh-tokens.md)
- [ADR platform/0013](../platform/0013-uat-host-in-the-production-web-image.md)
- [ADR privacy/0001](../privacy/0001-cookie-consent-in-the-browser-and-a-placeholder-analytics-provider.md)
- Google, "Sign in with Google branding guidelines"
