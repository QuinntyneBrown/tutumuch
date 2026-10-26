# ADR-0006: Daily Publish Limit Counted in the Database

**Date:** 2026-10-25
**Category:** platform
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

`L2-051` limits listing creation to 20 per member per day. The create-listing mock state
`limited` says "Drafts still save", so the limit counts published listings, not draft
saves. The create-listing and harden-http-and-api designs planned a `listing-publish`
fixed-window counter in `Distributed cache` (Redis), which does not exist yet.

## Decision

`PublishListingCommandHandler` counts the member's listings whose `PublishedAt` is on or
after 00:00 UTC today, using the index `IX_Listings_SellerId_PublishedAt`. At 20 or more it
answers 429 `RATE_LIMITED` with `Retry-After` set to the seconds until the next 00:00 UTC,
before any other publish rule runs. `PublishedAt` is set on the first publish only. The
limit is `Selling:PublishesPerDay` in configuration (20).

## Options Considered

### Option 1: Count published listings in TuTuMuch database (chosen)
- **Pros:** Exact, stateless, survives restarts, no new infrastructure, testable on SQLite in memory.
- **Cons:** One indexed count per publish.

### Option 2: Fixed-window counter in Redis
- **Pros:** Shares the general rate-limiting mechanism.
- **Cons:** Adds Redis for one rule; a counter can drift from the data it limits (a failed
  publish after the increment still counts).

## Consequences

### Positive
- The rule is enforced from the first publish with nothing to deploy.

### Negative
- The general rate-limiting policies (anonymous, member, register) still need the
  harden-http-and-api mechanism.

### Risks
- Two publishes racing at the 20th listing could both pass. Accepted: the limit is an abuse
  guard, not a financial control.

## References

- [create-listing](../../detailed-designs/listings/create-listing/README.md)
- [harden-http-and-api](../../detailed-designs/platform/harden-http-and-api/README.md)
- `L2-051`
