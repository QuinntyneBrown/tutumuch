# 0001. Public profile without a reputation projection

Date: 2026-10-26

## Status

Accepted

## Context

The view-member-profile design keeps a `MemberReputation` table (published-review count, star total, completed-sale count) up to date from `ReviewPublished` and `OrderCompleted`, exposes it through an `IRatingSummaryReader` port, adds a reviews endpoint, a rebuild command, output caching by profile tag, and shows avatar, bio and public location. Review authoring is deferred ([ADR general/0001](../general/0001-defer-moderation-reviews-and-data-export.md)), so no review is ever published. No feature stores an avatar, bio or public location yet: they belong to edit-profile, which has no journey. Account closure (`L2-005`) has no journey either, yet the profile must hide closed members. The listing detail page showed every seller with 0 sales until a profile stored the figure ([ADR catalog/0002](../catalog/0002-seller-reputation-defaults-until-orders-and-reviews.md)).

## Decision

- `GetPublicProfileQueryHandler` and `GetMemberListingsQueryHandler` read through `ITutuMuchDbContext`, as the catalog does. Completed sales are counted from `Orders` where the member is the seller and the status is `Completed`; Active listings are counted and paged (24 per page, newest first) from `Listings`. `GetListingDetailQueryHandler` uses the same count for `SellerSummaryDto.CompletedSalesCount`.
- The rating is always `{ average: null, count: 0 }`, so every profile and the Reviews section read "No reviews yet". There is no `MemberReputation` table, no reputation handlers, no rebuild command, no reviews endpoint and no `IRatingSummaryReader`.
- `PublicProfileDto` keeps `avatarUrl`, `bio` and `publicLocation` in its shape, always `null` for now; the page shows each only when present.
- `Member` gains a nullable `ClosedAt` and `Close(now)`, with a migration. An unknown or closed display name answers the same 404 `MEMBER_UNAVAILABLE` with `Cache-Control: no-store`. Closing an account from a screen stays with the privacy work.
- 200 responses carry `Cache-Control: public, max-age=60`. No server output cache and no tag eviction.
- The own profile shows no "Edit profile" action until edit-profile exists.

## Consequences

- No projection to keep consistent and no concurrency retries; two counts per profile request, served by the existing seller indexes.
- When review authoring returns, it adds the projection (or a count over published reviews), the reviews endpoint and the rating in `SellerSummaryDto`; the DTO shapes and `tm-rating-summary` do not change.
- When edit-profile arrives it fills avatar, bio and location and adds the own-profile action.
- A profile can be up to 60 seconds stale in shared caches.
