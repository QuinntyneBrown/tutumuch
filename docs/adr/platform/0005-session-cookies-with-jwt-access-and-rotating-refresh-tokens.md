# ADR-0005: Session Cookies with a JWT Access Token and Rotating Refresh Tokens

**Date:** 2026-10-25
**Category:** platform
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

Create-listing needs a signed-in, verified member before any accounts design existed.
`L2-008` asks for a 15-minute access token and a 14-day rotating refresh token in
`HttpOnly`, `Secure`, `SameSite=Strict` cookies, never in web storage, with server-side
revocation on sign-out and reuse detection. `L2-060` asks for a stateless API, and the
`StatelessApiTests` architecture test forbids in-process session state.

## Decision

- The access token is a JWT signed with HMAC-SHA256 (`Auth:SigningKey` from configuration),
  carrying only the member ID in `sub`, valid for 15 minutes, in the cookie `tl_access`
  (path `/`). ASP.NET Core JWT bearer authentication reads it from the cookie.
- The refresh token is 32 random bytes in the cookie `tl_refresh` (path `/api/auth`). Only
  its SHA-256 hash is stored, in the `Sessions` table of `TuTuMuch database`, with a family ID.
  `POST /api/auth/refresh` marks the presented token rotated and issues its successor in the
  same family; presenting a rotated token revokes the whole family. Sign-out revokes the family.
- The `VerifiedMember` and `CanSell` policies read `EmailVerifiedAt` from `TuTuMuch database`, so
  verification takes effect without waiting for a new access token.
- The Angular app learns who is signed in only from `GET /api/auth/me`; the session
  interceptor in `@tutumuch/api` renews an expired access token once through the refresh endpoint.

## Options Considered

### Option 1: JWT access cookie and database refresh tokens (chosen)
- **Pros:** Stateless API instances; refresh rotation and reuse detection are simple rows;
  tests run on SQLite in memory with no external system.
- **Cons:** Every refresh writes a row; the signing key must be shared by all instances.

### Option 2: ASP.NET Core cookie authentication
- **Pros:** Built in.
- **Cons:** No rotating refresh token or reuse detection; data protection keys must be shared.

### Option 3: Refresh-token families in the distributed cache
- **Pros:** Fast; matches the baseline's mention of Redis.
- **Cons:** Adds Redis before anything else needs it; tests would need a fake cache anyway.

## Consequences

### Positive
- `L2-008` criteria 1 to 4 and 6 hold; sessions survive any number of API instances.

### Negative
- "Sign out everywhere" (`L2-008` criterion 5) and listing sessions need more endpoints;
  they belong to manage sessions, deferred in the sign-up-and-sign-in design.

### Risks
- A leaked signing key lets anyone mint access tokens for 15 minutes. Mitigation: the key
  comes from the deployment's secret store and rotates with protect-credentials-and-supply-chain.

## References

- [sign-up-and-sign-in](../../detailed-designs/accounts/sign-up-and-sign-in/README.md)
- `L2-008`, `L2-010`, `L2-060`
