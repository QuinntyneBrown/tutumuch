# Manage notification preferences — estimate

## Basis

The hours assume an intermediate Angular/TypeScript and ASP.NET Core/C# developer who is productive in both stacks but new to this codebase. They also assume the following exist: the account shell and its navigation; the shared switch, alert, skeleton and toast components; anti-forgery and rate-limiting middleware; and the `MemberRegistered` event from `accounts`. The dispatcher in [send-email-notifications](send-email-notifications.md) calls `INotificationPreferenceReader`, which is counted here. Unsubscribe page copy and the token lifetime are `<TO SUPPLY>` and are assumed not to change the hours. Hours cover implementation, tests and review fixes. [Detailed design](../../detailed-designs/notifications/manage-notification-preferences/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `NotificationPreferences` entity with `IsAllowed` and unit tests | 3 |
| `NotificationPreferences` table, migration, `CreateDefaultNotificationPreferencesHandler` | 3 |
| `GetNotificationPreferencesQuery`, `UpdateNotificationPreferencesCommand`, validator and handler with `ETag`/`If-Match` | 4 |
| `NotificationPreferencesController` | 2 |
| `INotificationPreferenceReader` | 1 |
| `HmacUnsubscribeTokenService` with key ID rotation and tests | 4 |
| `UnsubscribeCommand`, handler and `UnsubscribeController` with RFC 8058 support | 4 |
| Marketing template footer link and `List-Unsubscribe` headers | 2 |
| `NotificationSettingsPageComponent` with loading, default, submitting, success, error and save-failed states | 5 |
| `NotificationPreferencesApiService` | 1 |
| `UnsubscribePageComponent` with 4 states | 3 |
| Data export and erasure hookup | 1 |
| API integration tests for `L2-043` acceptance criteria | 5 |
| Playwright acceptance tests through page objects | 4 |
| **Total** | **42** |

## Minimal scope

The minimal scope ships the settings page with the two always-on rows and two toggles, defaults for new members, the dispatcher's preference check and a signed unsubscribe link. It uses last-write-wins saves without `ETag`, a single signing key without rotation, and no RFC 8058 header, and it keeps one Playwright path. It satisfies `L2-043` in full. Minimal scope totals **36 hours**.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| `ETag`/`If-Match` concurrency | A save from a second tab overwrites the first without warning | 2 |
| Signing key rotation by key ID | Rotating the key invalidates every unsubscribe link already sent | 1 |
| RFC 8058 `List-Unsubscribe` headers | Mail clients do not show their built-in unsubscribe button | 1 |
| Playwright suite reduced to one happy path | UI regressions in page states are caught later | 2 |
| **Total if all cut** | | **6** |
