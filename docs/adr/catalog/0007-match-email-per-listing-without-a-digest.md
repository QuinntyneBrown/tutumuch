# 0007. Match email per listing without a digest, behind its own preference

Date: 2026-10-27

## Status

Accepted

## Context

When a listing matches open item requests (`L2-100`), the member is emailed (`L2-101`). The feature request allowed either one email per request per listing or at most one daily digest per member, and asked for a new switch in the email settings that honours the existing unsubscribe token ([ADR notifications/0002](../notifications/0002-notification-preferences-defaults-and-unsubscribe-tokens.md)). The outbox, templates and dispatcher of [ADR notifications/0001](../notifications/0001-email-outbox-plain-text-templates-and-provider-port.md) send one message per outbox row and have no batching. Listings are one of a kind: a digest sent a day later can arrive after the item sold.

## Decision

- **One email per member per listing, queued at match time.** When several of a member's requests match the same listing, `MatchItemRequestsHandler` queues one `ItemRequestMatchedEmail(ListingReference, Title, Brand, Price)`; another listing gets its own email. There is no digest and no daily cap.
- **Subject "A match for your request: {title}"**, the price, a link to `/items/{reference}` and a link to `/account/requests`. The email names no seller contact and nothing about other members (`L2-106`).
- **A new category, `NotificationCategory.ItemRequests`, on for every member until turned off.** `NotificationPreferences.ItemRequestEmails` is a new column defaulting to on, shown on the email settings page as "Item requests". The member asked for these emails by making a request, so they start on, unlike marketing. `PUT /api/me/notification-preferences` takes it as an optional `itemRequestEmails`; an absent value keeps the stored one, so older clients cannot turn it off by accident.
- **One-click unsubscribe as for marketing.** The dispatcher adds the unsubscribe link and the RFC 8058 `List-Unsubscribe` and `List-Unsubscribe-Post` headers with a token of the `ItemRequests` category; `POST /api/notifications/unsubscribe` turns that category off for such a token, and still turns marketing off for a marketing token.
- **Matches are recorded whatever the switch says**, so My requests shows them even with the emails off (`L2-102`).

## Consequences

- A member with several requests in a busy category can receive several emails a day; the switch and the one-click unsubscribe stop them, and a digest can be added later as a new template and job without changing the matches.
- The unsubscribe page names the category it turned off.
- Transactional categories (account security, orders) stay always on; the new category is the second one a member may turn off besides reviews and marketing.
