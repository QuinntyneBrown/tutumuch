# Check out and pay — estimate

## Basis

The estimate assumes the listings subsystem's `Listing` aggregate and `RowVersion` exist and the catalog's `Listing.EnsureCanBePurchasedBy` guard is in place. The payment provider is `<TO SUPPLY>`; the hours assume a provider with a .NET SDK, hosted card fields with a browser SDK, idempotency keys, tax quotes and signed webhooks, comparable to the leading card processors. A provider without tax quotes would add a tax service integration. Hours cover implementation, unit and integration tests, acceptance tests and review fixes for an intermediate Angular/TypeScript and ASP.NET Core/C# developer; they exclude requirements, design and mock work. [Detailed design](../../detailed-designs/orders/checkout-and-pay/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `CheckoutPage` with address choice, `AddressFormComponent` and ten states | 14 |
| `HostedCardFieldsComponent` with the provider browser SDK and CSP changes | 8 |
| `ReservationTimerComponent` with warning and danger banners and live-region announcement | 5 |
| `OrderConfirmationPage` with polling, loading and error states and success toast | 5 |
| `CheckoutApiService` with idempotency key handling | 3 |
| `CheckoutsController` endpoints and error mapping | 5 |
| `StartCheckoutCommandHandler` with conditional reservation update and `Listing` reservation methods | 8 |
| `SetCheckoutAddressCommand`, `ShippingAddressValidator` and tax quote | 6 |
| `SubmitPaymentCommandHandler` with idempotent payment creation | 6 |
| `PaymentWebhookController` with signature verification and event deduplication | 6 |
| `ConfirmPaymentCommandHandler`: order creation, listing Sold, payout, duplicate-payment refund | 10 |
| `RecordPaymentFailureCommand` and `ReleaseCheckoutCommand` handlers | 4 |
| `ReservationExpiryJob` | 3 |
| `PaymentProviderAdapter` for payments, tax, refunds and webhook verification | 12 |
| EF Core mapping and migrations for `Checkouts`, `Payments`, `PaymentWebhookEvents`, `Refunds`, `SavedAddresses` | 6 |
| Concurrency acceptance test: 50 parallel checkout requests, one success (`L2-029`) | 5 |
| Unit and integration tests for handlers, webhook and expiry | 12 |
| Playwright acceptance tests with the provider's test mode | 8 |
| **Total** | **126** |

## Minimal scope

Minimal scope keeps the reservation with its database guarantee, a US address form, hosted card fields, the signed webhook with deduplication, idempotent payment submission and duplicate-payment refunds. It drops the saved-address default and shows a fixed countdown without the warning and danger banners. It also uses a single tax rate placeholder in place of provider tax quotes, so `L2-027` sales tax is deferred to `<TO SUPPLY>`. It satisfies `L2-028`, `L2-029` and `L2-030` in full and `L2-027` criteria 1, 2, 3 and 5; it defers criterion 4 and accurate per-state tax.

Minimal scope total: **111 hours**.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Saved address offered by default | Buyer re-enters the address each time; `L2-027` criterion 4 deferred | 3 |
| Reservation timer warning and danger banners | No one-minute warning; the expired page still appears | 3 |
| Provider tax quotes; fixed rate configuration | Tax is approximate per state | 4 |
| Order confirmation polling page; redirect to Purchases | No "Confirming your payment" step | 3 |
