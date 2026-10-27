# 0004. Match item requests in process when a listing becomes active

Date: 2026-10-27

## Status

Accepted

## Context

The request-items design ([catalog/request-items](../../detailed-designs/catalog/request-items/README.md)) matches a listing against open item requests when it becomes Active, by publishing a draft (`L2-019`) or relisting the listing of a cancelled order (`L2-035`), and emails each member whose request matched (`L2-100`, `L2-101`). Matching could run in the API, right after the listing is saved, or in `TuTuMuch Worker`, which would poll for newly active listings. The repository already raises an in-process MediatR notification after a save for order completion ([ADR orders/0004](../orders/0004-complete-order-in-process-event-and-fixed-3-day-window.md)); it has no domain event dispatcher or message bus. A member opens at most 20 requests (`L2-098`), so one category holds a bounded number of open requests to compare.

## Decision

- **`ListingBecameActive(ListingId)` is a MediatR notification published after the save.** `PublishListingCommandHandler` and `RelistListingCommandHandler` save the listing, then publish it once. Edits of an Active listing and a reservation released back to Active do not publish it.
- **`MatchItemRequestsHandler` runs in the same request.** It re-reads the listing, loads the open requests of the category that have no match with it yet, applies the match rule ([ADR catalog/0005](0005-item-request-match-rule.md)) in memory, adds the `ItemRequestMatch` rows and queues the emails through `IEmailOutbox`, then saves once: the matches and their emails commit together.
- **Matching never fails the publish or relist.** The handler catches and logs any exception (`L2-069`); the seller still gets the success answer, because the listing was saved before the event was published.
- **No worker poll.** No "matched" flag is added to `Listings` and no catch-up job runs.

## Consequences

- A member hears about a match within the 2 minutes of `L2-042`, because the outbox is written at publish time.
- A crash, or a logged failure, between the listing save and the match save loses the matches of that one listing. The member can still find it by searching; a catch-up job can be added later if that proves too lossy.
- Publishing pays for one more query and, at most, one more save. If large categories make this slow, the handler can move behind the outbox or the worker without changing the API.
- Matching is testable on SQLite in-memory through the API, like the rest of the catalog.
