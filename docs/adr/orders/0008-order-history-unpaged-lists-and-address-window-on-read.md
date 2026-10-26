# 0008. Order history: unpaged lists, one list query, and the address window applied on read

Date: 2026-10-26

## Status

Accepted

## Context

The order-history design ([orders/order-history](../../detailed-designs/orders/order-history/README.md))
lists a member's purchases and sales newest first (L2-037) with a keyset cursor whose page size
and "Show more" copy were left `<TO SUPPLY>`. The buyer's order and the seller's sale pages
already existed (ship-order and later features), so this feature adds the two lists and the
30-day address window of L2-037 criteria 2 and 3.

## Decision

- **No paging yet.** `GET /api/purchases` and `GET /api/sales` return every order of the member,
  ordered by `PaidAt DESC, Id DESC`, with the counts the subtitles read. The cursor, page size and
  "Show more" wait for a supplied page size and copy; the row order already matches the keyset.
- **One query, one page.** `ListOrdersQuery` takes the member's role (Buyer or Seller); one
  `OrderHistoryPage` in `@tutumuch/orders` serves `/account/purchases` and `/account/sales`, picked by
  route data.
- **The address window is applied on read.** `GetSaleQueryHandler` checks
  `ShippingAddressVisibility.IsVisibleToSeller` (until `EndedAt` + 30 days) and returns
  `shipTo: null` after it; the sale page shows "Removed 30 days after the order ended.". Action
  endpoints succeed only on orders that have not ended, so they always return the address. Erasing
  the stored cipher remains a privacy-subsystem retention decision.

## Consequences

- A member with many orders loads them all at once; paging is a backwards-compatible addition.
- The address stays stored after the window; only its exposure is removed.
