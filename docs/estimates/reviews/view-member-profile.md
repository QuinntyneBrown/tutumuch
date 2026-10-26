# View a member profile — estimate

## Basis

The hours assume an intermediate Angular/TypeScript and ASP.NET Core/C# developer who is productive in both stacks but new to this codebase. They also assume the following are already in place: the `accounts` subsystem holds members with `NormalizedDisplayName`, status, bio, public location and avatar; `catalog` provides `ListingCardComponent` and `ListingCardDto`; `orders` raises `OrderCompleted`; and [leave-review](leave-review.md) raises `ReviewPublished`. The reviews page size and the profile cache duration are `<TO SUPPLY>` and are assumed to be configuration only. Hours cover implementation, tests and review fixes. [Detailed design](../../detailed-designs/reviews/view-member-profile/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `MemberReputation` entity and `RatingSummary` value object with rounding tests | 3 |
| `MemberReputations` table and migration | 2 |
| `UpdateReputationOnReviewPublishedHandler` and `UpdateReputationOnOrderCompletedHandler` with concurrency retry | 4 |
| `RebuildMemberReputationCommand` | 2 |
| `IRatingSummaryReader` for `catalog` | 1 |
| `GetPublicProfileQuery` and handler: public-column projection, uniform `member.unavailable` | 4 |
| `GetMemberListingsQuery` and handler, 24 per page | 3 |
| `GetMemberReviewsQuery` and handler, published only, replies, former members | 3 |
| `MembersController`, `PublicReadPolicy`, rate limits, cache headers | 3 |
| Output caching with `profile:{name}` tag and eviction on the 5 change events | 4 |
| `PublicResponsePiiTests` serialization and allow-list tests | 3 |
| `MembersApiService` | 2 |
| `MemberProfilePageComponent` route and 7 states | 6 |
| `ProfileHeaderComponent` and `ProfileStatsComponent`, with the "Report member" entry point | 3 |
| `ProfileListingsComponent` with "Load more" | 3 |
| `ProfileReviewsComponent` with replies and "Reply" link | 3 |
| `RatingSummaryComponent` with full and short forms | 2 |
| API integration tests for `L2-040` and `L2-041` acceptance criteria | 5 |
| Playwright acceptance tests through page objects | 5 |
| **Total** | **61** |

## Minimal scope

The minimal scope calculates the rating summary and completed sales with live aggregate queries over `Reviews` and `Orders` instead of the `MemberReputation` projection. It drops output caching and the rebuild command, and keeps one Playwright path. It still shows every field `L2-041` lists, pages listings 24 at a time, returns 404 for unavailable members, counts published reviews only, and keeps the PII tests. It therefore satisfies `L2-040`, `L2-041` and the profile part of `L2-054`, at the cost of heavier reads under load. Minimal scope totals **45 hours**.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Output caching and tag eviction | Every profile view queries the database | 4 |
| `RebuildMemberReputationCommand` | Fixing a drifted projection needs a manual SQL script | 2 |
| `IRatingSummaryReader` shared with `catalog` | `catalog` computes the seller rating on its own | 1 |
| Reviews paging; show the newest reviews only, count `<TO SUPPLY>` | Older reviews are not reachable from the profile | 2 |
| Playwright suite reduced to one happy path | UI regressions in profile states are caught later | 3 |
| **Total if all cut** | | **12** |

## As built

Built to the scope of [ADR reviews/0001](../../adr/reviews/0001-public-profile-without-a-reputation-projection.md): live counts over `Orders` and `Listings`, no `MemberReputation` projection, no reviews endpoint, "No reviews yet" everywhere, and `Member.ClosedAt` for the unavailable page. The own-profile "Edit profile" action waits for edit-profile, which leaves the feature at 95%.
