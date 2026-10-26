# Notifications subsystem

The notifications subsystem tells members by email about events that affect their account, listings and orders (`L1-011`). Every other subsystem enqueues email through one outbox port. `Tutu Much Worker` renders, guards and sends each email with retry and back-off. Members choose which optional categories they receive, and anyone can leave marketing email in one click without signing in.

| Feature | Design | L2 requirements |
|---------|--------|-----------------|
| Send email notifications | [send-email-notifications](send-email-notifications/README.md) | `L2-042` |
| Manage notification preferences | [manage-notification-preferences](manage-notification-preferences/README.md) | `L2-043` |
