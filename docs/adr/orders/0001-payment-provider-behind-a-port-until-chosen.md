# 0001. Payment provider behind a port until a provider is chosen

Date: 2026-10-26

## Status

Accepted

## Context

Checkout takes payment through a PCI DSS compliant provider's hosted fields and confirms it from
the provider's signed webhook (`L2-030`). The detailed design leaves the provider, its browser SDK
and its webhook format `<TO SUPPLY>`, and the prompt defers real credentials. The feature still
has to be built and tested end to end, and tests may not touch an external system (AGENTS.md).

## Decision

- The backend talks to the provider only through `IPaymentProvider` in `TutuMuch.Application`:
  `QuoteTaxAsync`, `CreatePaymentAsync`, `RefundAsync` and `ReadWebhook`, which verifies the
  signature over the raw body and returns the parsed `PaymentEvent` or null.
- `TutuMuch.Infrastructure` registers `UnconfiguredPaymentProvider`: it quotes no tax, charges
  nothing and accepts no webhook. Outside the tests checkout therefore stops safely at the
  address step until the provider adapter replaces it.
- `TutuMuch.Api.Tests` replace the port with `FakePaymentProvider` (8.875% tax, deduplication by
  idempotency key, HMAC-SHA256 webhook signatures in the `Payment-Signature` header).
- The browser mounts card fields through `PAYMENT_FIELDS` in `@tutumuch/api`. Its default,
  `TestModePaymentFields`, offers test cards to pick (`tok_visa`, `tok_chargeDeclined`), so no
  card number is ever typed into a Tutu Much page. Playwright serves every API call from fixtures.

## Consequences

- Choosing the provider means one Infrastructure adapter, one `PAYMENT_FIELDS` factory using the
  provider's SDK, the provider origin in the Content Security Policy, and secrets
  `PaymentProvider:ApiKey`, `PaymentProvider:WebhookSecret` and `PaymentProvider:PublishableKey`.
  No handler, controller or page changes.
- Until then, a deployed Tutu Much cannot take payments; this is intended.
