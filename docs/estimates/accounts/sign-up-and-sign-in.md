# Sign up and sign in — estimate

## Basis

Intermediate Angular and ASP.NET Core developer. Built inside the create-listing run as its prerequisite. [Detailed design](../../detailed-designs/accounts/sign-up-and-sign-in/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `Member`, `EmailVerificationToken`, `Session`, EF configuration and migration | 4 |
| Register, verify, resend (3 an hour) handlers and API tests | 6 |
| Sign in, refresh with rotation and reuse detection, sign out, me; cookies and JWT; API tests | 7 |
| `VerifiedMember` and `CanSell` policies with problem details | 2 |
| `AuthState`, session interceptor, `AccountsApi` | 3 |
| Join, verify-email and sign-in pages, auth shell, member top bar | 6 |
| Library components (alert, button, choice, error summary, form field, icon, spinner) | 3 |
| Playwright tests and page objects | 1 |
| **Total** | **32 (4 days)** |

## Status

Complete: 100% of this design. Password reset, lockout, manage sessions, edit profile and close account are separate designs, not estimated yet.
