# Estimate — Launch phases

## Basis

The hours assume an intermediate .NET and Angular developer who does not yet know the TuTuMuch
codebase. They cover implementation, automated tests and review fixes, and exclude
requirements, design and mock work. They assume the `CanSell` and `Administrator` policies,
`AuthState`, the top bar and the listings, catalog and marketing screens exist.
[Detailed design](../../detailed-designs/platform/launch-phases/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `LaunchPhase`, `LaunchRules`, `LaunchOptions` with the validator and `ValidateOnStart`; domain and unit tests | 4 |
| `GET /api/launch`, `isAdministrator` on `/api/auth/me`; API tests | 3 |
| `SellingOpenRequirement` in `CanSell`, `SELLING_NOT_OPEN`; API tests per action and phase | 4 |
| Phase 1 product limit in create and publish; API tests at limit − 1, the limit and Phase 2 | 3 |
| `LaunchState` and the app initializer; `tl-top-bar` `categoryLinksVisible` and `sellVisible` | 4 |
| Category guard, filter form, `sellingOpen` guard, `SellingNotOpenPage`, product-limit state | 4 |
| Payout banner, empty states, verify-email page and the welcome page follow `canSell()` | 3 |
| e2e fixtures (Phase 3 default, Phases 1 and 2) and `launch-phases.spec.ts` | 4 |
| `appsettings.json`, compose, `docs/deployment.md`, CLI seed phase | 3 |
| **Total** | **32** |

## Minimal scope

The minimal version enforces the phase in the API only (setting, policy, limit) and hides the
top-bar links; the welcome page, empty states and payout banner keep today's copy. Minimal
total: **20** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| `SellingNotOpenPage` | Non-admins see the sell form and get the API's 403 on save | 3 |
| Welcome and empty-state copy | "Start selling" stays and leads to the closed sell page | 3 |
| Product-limit state | The admin sees a generic save error at the limit | 1 |
