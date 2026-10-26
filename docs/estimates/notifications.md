# Estimates: notifications

| Design | Effort | Complete |
|---|---|---|
| manage-notification-preferences | 3 days | 90% |
| send-email-notifications | 4 days | 85% |

send-email-notifications is at 85%: the outbox, the dispatcher with retry and back-off, the
content guard and the emails for every event built so far are done. The real provider adapter,
HTML layout, retention job and metrics wait for the provider, the final copy and the platform
observability work ([ADR notifications/0001](../adr/notifications/0001-email-outbox-plain-text-templates-and-provider-port.md)).

manage-notification-preferences is at 90%: the settings page, the API with If-Match, the
dispatcher check, the signed one-click unsubscribe link and headers and the unsubscribe page are
done. The unsubscribe rate limit, the token lifetime and signing key rotation remain open
([ADR notifications/0002](../adr/notifications/0002-notification-preferences-defaults-and-unsubscribe-tokens.md)).
