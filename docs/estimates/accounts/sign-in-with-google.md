# Sign in with Google — estimate

## Basis

Intermediate Angular and ASP.NET Core developer. [Detailed design](../../detailed-designs/accounts/sign-in-with-google/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| Requirements, mocks (sign-in, register and join-google states), provider button and divider in the design system and the components library, screen composition, design, ADR | 8 |
| `ExternalLogin`, `PendingExternalJoin`, nullable `PasswordHash`, EF configuration and migration | 4 |
| `IGoogleIdentityProvider` port, `GoogleIdentityProvider` adapter (token endpoint, ID token validation), `GoogleOptions` with start-up validation | 6 |
| Start and callback with the signed flow cookie; `GoogleCallbackCommandHandler` (linked, verified-by-email, unverified refused, new person); `SessionIssuer` | 8 |
| Pending and finish; `MemberRegistrationRules` shared with registration; Google-only password sign-in | 5 |
| API tests (`SignInWithGoogleTests`, every path and refusal; cookie allow-list) | 6 |
| `AccountsApi` and `AuthState` additions; sign-in and join buttons; `GoogleJoinPage` | 6 |
| Playwright tests and page objects | 3 |
| Deployment docs, `.env.example`, owner steps | 2 |
| **Total** | **48 (6 days)** |

## Descoping options

- Skip the verified-by-email link: a matching email always goes through the finish step, which then refuses it as taken (saves 2 hours, loses AC5).
- Skip the `google-existing` state: refuse every matching email as `failed` (saves 1 hour, loses the advice).

## Status

Complete: 100% of this design. The per-IP limits, the pending-join sweep and linking from settings are deferred (see the design).
