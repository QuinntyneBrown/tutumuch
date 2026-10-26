# ADR-0008: MediatR 12 and Validation in the Domain and Handlers

**Date:** 2026-10-25
**Category:** platform
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

The architecture baseline names MediatR commands and FluentValidation validators in
`TutuMuch.Application`. MediatR moved to a commercial licence from version 13. The listing
rules (draft rules, publish rules, the size rule) must report every failing field at once
with stable codes and the exact messages the form shows (`L2-015`, `L2-068`).

## Decision

- Use MediatR 12.5.0, the last Apache-2.0 release, for commands and queries.
- Listing rules live in the `Listing` aggregate (`Listing.CheckDraft`, `Listing.Publish`)
  and return `RuleViolation(Field, Code, Message)` lists; registration rules live in
  `RegisterMemberCommandHandler`. Handlers map violations to a 422 problem with an `errors`
  object keyed by request field. FluentValidation is not added.
- Handlers return `Result<T>` with an `Error` (status, code, title, field errors,
  Retry-After) instead of throwing; controllers write it as problem details.

## Options Considered

### Option 1: MediatR 12, rules in the domain (chosen)
- **Pros:** No licence cost; the rules sit next to the state they protect and are unit
  tested without a host; one place produces codes and messages.
- **Cons:** The harden-http-and-api convention of one validator per command is not followed.

### Option 2: MediatR 13 and FluentValidation
- **Pros:** Matches the baseline wording.
- **Cons:** Licence cost; publish rules depend on stored state, which validators reach awkwardly.

## Consequences

### Positive
- `ListingTests` cover every rule in milliseconds.

### Negative
- harden-http-and-api must restate its "validator per command" architecture test as
  "every command handler validates its input".

## References

- [create-listing](../../detailed-designs/listings/create-listing/README.md)
- [sign-up-and-sign-in](../../detailed-designs/accounts/sign-up-and-sign-in/README.md)
