# Reviews subsystem

The reviews subsystem shows each member's public profile and reputation (`L1-009`, `L1-010`). It reads order state from `orders`, member data from `accounts` and listings from `listings`. Writing reviews after a sale is deferred; see [ADR 0001](../../adr/general/0001-defer-moderation-reviews-and-data-export.md).

| Feature | Design | L2 requirements |
|---------|--------|-----------------|
| View a member profile | [view-member-profile](view-member-profile/README.md) | `L2-040`, `L2-041`, `L2-054` |
