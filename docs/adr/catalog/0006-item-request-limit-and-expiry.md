# 0006. Item request limit of 20 open, 90-day expiry renewed by saving, and a 30-day clean-up

Date: 2026-10-27

## Status

Accepted

## Context

Open item requests are compared with every listing that becomes Active ([ADR catalog/0004](0004-match-item-requests-in-process-when-a-listing-becomes-active.md)), and a forgotten request keeps sending email. The feature request suggested a limit of 20 open requests per member and an expiry of 90 days with the date shown. The listings subsystem already deletes drafts after 90 days without an edit ([ADR listings/0004](../listings/0004-draft-clean-up-deletes-photo-files-directly.md)). A request is written only by its owner.

## Decision

- **At most 20 open requests per member** (`ItemRequest.MaxOpenPerMember`). Creating a 21st, or renewing an expired one while 20 are open, is refused with `409 REQUEST_LIMIT_REACHED` "You have 20 open requests" (`L2-098`).
- **A request is open for 90 days after it was created or last saved** (`ItemRequest.OpenDays`). Every save renews it, unchanged or not; My requests shows "Open until {Mon d}". "Renew" on an expired request is a save with the same fields, so no separate endpoint exists.
- **Expired requests stay visible for 30 days** (`ItemRequest.KeepExpiredDays`), marked "Expired", are not matched and do not count toward the limit. `ItemRequestCleanupJob` in `TuTuMuch.Worker` deletes them, with their matches, once a day. No reminder email is sent before expiry.
- **No ETag on requests.** Saves are last-write-wins, because only the member edits their own requests; the listing and email settings ETags guard races with other writers that requests do not have.
- **Requests live in `@tutumuch/catalog`.** No new feature library is created, so the mandatory folder structure is unchanged. My requests is at `/account/requests`; the account sub-navigation that would link it is composed with the account overview journey, like Listings and Emails today.

## Consequences

- The cost of matching one listing is bounded by 20 requests times the number of members with requests in its category.
- A member who keeps using a request must save it every 90 days; the date on My requests tells them when.
- The limits are domain constants, not configuration; changing them is a code change and a test change.
