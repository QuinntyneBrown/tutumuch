# Orders subsystem

The orders subsystem covers buying and fulfilment (`L1-007`, `L1-008`). A checkout
reserves a listing, the payment provider's signed webhook creates a `Paid` order, and
the seller ships the item. The order then completes, is cancelled or is refunded after
a problem, and the seller's payout follows the outcome. One order state machine, defined
in [ship-order](ship-order/README.md), governs every status change across the seven
features below.

| Feature | L2 requirements | Estimate |
|---------|-----------------|----------|
| [checkout-and-pay](checkout-and-pay/README.md) | `L2-027`, `L2-028`, `L2-029`, `L2-030` | [126 h](../../estimates/orders/checkout-and-pay.md) |
| [seller-payouts](seller-payouts/README.md) | `L2-031` | [50 h](../../estimates/orders/seller-payouts.md) |
| [ship-order](ship-order/README.md) (order state machine) | `L2-032`, `L2-033` | [65 h](../../estimates/orders/ship-order.md) |
| [complete-order](complete-order/README.md) | `L2-034` | [22 h](../../estimates/orders/complete-order.md) |
| [cancel-order](cancel-order/README.md) | `L2-035` | [41 h](../../estimates/orders/cancel-order.md) |
| [resolve-order-problems](resolve-order-problems/README.md) | `L2-036` | [65 h](../../estimates/orders/resolve-order-problems.md) |
| [order-history](order-history/README.md) | `L2-037` | [35 h](../../estimates/orders/order-history.md) |
