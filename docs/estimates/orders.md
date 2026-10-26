# Estimates: orders

| Design | Effort | Complete |
|---|---|---|
| cancel-order | 3 days | 95% |
| checkout-and-pay | 7 days | 90% |
| complete-order | 3 days | 100% |
| order-history | 3 days | 90% |
| resolve-order-problems | 5 days | 90% |
| seller-payouts | 5 days | 95% |
| ship-order | 4 days | 95% |

Excludes deferred work (see AGENTS.md).

checkout-and-pay is at 90%: the payment provider adapter, its hosted-fields SDK and credentials
are deferred until a provider is chosen ([ADR orders/0001](../adr/orders/0001-payment-provider-behind-a-port-until-chosen.md)).

ship-order is at 95%: the tracking, delivery, cancellation and reminder emails and the reminder
job came with send-email-notifications; the carrier tracking service adapter and the US holiday
list wait until they are chosen and supplied ([ADR orders/0003](../adr/orders/0003-ship-order-status-token-weekday-deadline-and-carrier-port.md)).

complete-order is complete: resolve-order-problems added the open-problem guard of L2-034
criterion 3 and "Report a problem" ([ADR orders/0004](../adr/orders/0004-complete-order-in-process-event-and-fixed-3-day-window.md)).

seller-payouts is at 95%: the payout provider adapter and its credentials wait until a provider is
chosen, and re-sending a failed payout is deferred; resolve-order-problems added the open-problem hold
([ADR orders/0005](../adr/orders/0005-seller-payouts-payout-port-fee-snapshot-and-shell-banner.md)).

cancel-order is at 95%: refunds go through the payment provider port, whose adapter waits for a
provider; "Edit first" in the relist dialog waits for edit-and-withdraw-listing
([ADR orders/0006](../adr/orders/0006-cancel-order-one-path-request-on-the-order-and-relist.md)).

resolve-order-problems is at 90%: a partial refund is deferred until it has a requirement, a status
rule and a mock, and refunds go through the payment provider port, whose adapter waits for a
provider ([ADR orders/0007](../adr/orders/0007-order-problems-administrator-flag-private-photos-and-two-outcomes.md)).

order-history is at 90%: paging and "Show more" wait for a supplied page size and copy
([ADR orders/0008](../adr/orders/0008-order-history-unpaged-lists-and-address-window-on-read.md)).
