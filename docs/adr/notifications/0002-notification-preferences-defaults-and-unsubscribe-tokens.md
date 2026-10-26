# 0002. Notification preferences as implicit defaults, stateless unsubscribe tokens and a same-origin List-Unsubscribe URL

Date: 2026-10-26

## Status

Accepted

## Context

The manage-notification-preferences design
([notifications/manage-notification-preferences](../../detailed-designs/notifications/manage-notification-preferences/README.md))
lets a member turn review and marketing email off, keeps account security and order email on,
starts marketing off, and puts a one-click unsubscribe in every marketing email (L2-043). The
design assumed a `MemberRegistered` event creating a row per member, a `rowversion` ETag, an
`IUnsubscribeTokenService` with key IDs, anti-forgery on the PUT and a rate limit on the
anonymous endpoint. When it was built, registration published no domain event, listings already
used an integer version as their ETag (edit-and-withdraw-listing), no endpoint had anti-forgery
or a rate limiter, `App:BaseUrl` was the only public URL setting, and no notification type was a
review or marketing email. Backend tests run on SQLite in memory (AGENTS.md).

## Decision

- **No row at registration.** A member without a `NotificationPreferences` row has the
  defaults (`CreateDefault`: reviews on, marketing off). The query, the dispatcher's
  `INotificationPreferenceReader` and the unsubscribe handler all read a missing row as the
  defaults; the first save inserts it. This needs no registration event, no backfill for
  existing members and no change to registration.
- **An integer `Version` is the concurrency token and the ETag**, as for listings, instead of
  `rowversion`, so SQLite in memory and SQL Server behave the same. A save needs `If-Match`
  (428 `PRECONDITION_REQUIRED`), refuses an old one (412 `NOTIFICATION_PREFERENCES_CHANGED`)
  and refuses a lost race the same way.
- **Unsubscribe tokens are stateless HMAC-SHA256** over member ID, category and issue time,
  signed with `Notifications:UnsubscribeSigningKey` by the `UnsubscribeTokens` application
  service. Nothing is stored per email. Tokens do not expire and carry no key ID yet; a
  lifetime and rotation stay open questions.
- **`List-Unsubscribe` points at `{App:BaseUrl}/api/notifications/unsubscribe`**: Tutu Much Web serves
  Tutu Much API under the same origin, so no second public URL setting is added. The endpoint takes
  the token from the query string (RFC 8058 form POST) or a JSON body (the unsubscribe page).
- **`NotificationType.Marketing` exists without a sender.** The dispatcher honours the setting,
  adds the link and headers, and tests queue it directly; marketing campaigns are deferred. No
  review notification type exists, because review authoring is deferred (ADR general/0001).
- **The error toast is built now.** `ToastService` and the rendering `tm-toast-region` are added
  to `@tutumuch/components` for the failed-save and changed-elsewhere states.
- **Not built:** anti-forgery on the PUT and the unsubscribe rate limit (L2-050, L2-051) wait for
  the shared platform work, as for every other endpoint.

## Consequences

- One migration adds `NotificationPreferences` with a cascade delete from `Members`.
- `EmailMessage` gains optional `Headers`, which the future provider adapter must send.
- The API and the worker need the signing key; development settings carry a development-only
  value and production reads it from the secret store (L2-052).
- Changing the signing key invalidates every unsubscribe link already sent until rotation with
  a key ID is built.
