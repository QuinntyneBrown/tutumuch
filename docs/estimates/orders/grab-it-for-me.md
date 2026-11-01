# Grab it for me — estimate

## Basis

The hours assume an intermediate Angular/TypeScript and ASP.NET Core/C# developer who is productive in both stacks but new to this codebase. They assume [shelf-finds](../catalog/shelf-finds.md) is built and that checkout and pay exists as built: `IPaymentProvider` with `FakePaymentProvider`, the hosted card fields, the address form and rules of `L2-027`, `Payment`, `Refund`, the payment webhook handler, the outbox, the admin shell and `TuTuMuchApiFactory`. The payment provider adapter itself stays deferred ([ADR orders/0001](../../adr/orders/0001-payment-provider-behind-a-port-until-chosen.md)). Estimates cover implementation, unit, integration and acceptance tests, and review fixes. [Detailed design](../../detailed-designs/orders/grab-it-for-me/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `Grab` state machine and `GrabPricing`, with unit tests | 4 |
| `Grabs` configuration, nullable `Payment.CheckoutId`, `Payment.GrabId`, `RefundReason.GrabRefunded` and the migration | 2 |
| Quote and start handlers with the acknowledgement, the total check, the idempotency key and the open-grab race | 4 |
| Webhook branch for grab payments and the `GrabRequested` email | 2 |
| Administrator actions, `GrabRefunds` and the `GrabUpdated` emails | 4 |
| Shopper cancel and received, and `ExpireGrabsCommand` with `GrabDeadlineJob` | 3 |
| `GrabsController`, `AdminGrabsController` and API acceptance tests | 5 |
| `GrabsApi` and its DTOs | 1 |
| `GrabPage`: address, summary, deposit box and acknowledgement, card fields and payment polling | 5 |
| `MyGrabsPage`, `GrabDetailPage` and `CancelGrabDialog` | 4 |
| `AdminGrabsPage` and `GrabUnavailableDialog` | 3 |
| Playwright acceptance tests via page objects and `page.route` fixtures | 3 |
| **Total** | **40** |

## Minimal scope

The minimal version ships the quote, payment with the deposit acknowledgement, the webhook branch, the administrator's accept, decline, bought and couldn't-get-it actions with refunds, the shopper's grab detail with cancel, and the poster's email. My grabs, "I've got it", the deadline job and the shopper's emails are left out, so late grabs are expired by hand. It satisfies `L2-117`, `L2-118` and `L2-121`, and part of `L2-119` and `L2-120`. Minimal scope totals **30** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| `GrabDeadlineJob` | Late grabs are expired by the administrator by hand | 2 |
| Shopper emails on every change | The shopper checks the grab page for news | 2 |
| `MyGrabsPage` | The shopper reaches a grab from its email or the redirect after paying | 2 |
| "I've got it" | Bought grabs stay Bought until the job completes them | 1 |
