# ADR-0005: Marketing Wording: Everything Is for Girls

**Date:** 2026-10-26
**Category:** general
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

The marketing page (`/welcome`) and its 30-second pitch video tell shoppers and sellers what
TuTuMuch is: a marketplace for all things young girls, new and gently loved: clothes, dance
and sports gear, cameras and more. The size rule (`L1-003`, `L2-011` to `L2-014`) accepts
listings in girls' sizes only, and the API rejects anything else. Copy that hinted at boys'
or adult sizes, or at "kids'" sizing in general, would send visitors to a catalog and a sell
form that refuse them.

## Decision

The marketing copy and narration say plainly that everything here is for girls: "Twirl it
forward.", "Everything here is for girls.", "Pass it on to the next girl.", "Her next
favourite thing is waiting.", "New listings every day, every one for girls." They never
promise, name or hint at boys' or adult sizes, and never say "kids'" or "unisex" about
sizing. The size rules, the domain, the API and the sell page do not change.

## Options Considered

### Option 1: Say everything is for girls (chosen)
- **Pros:** True against the size rule; one clear audience; matches the brand.
- **Cons:** Families shopping for boys learn quickly that TuTuMuch is not for them.

### Option 2: Neutral "kids'" wording
- **Pros:** Sounds broader.
- **Cons:** Implies boys' sizes the API rejects; visitors hit rejections on the sell form.

## Consequences

### Positive
- The page and video are accurate against `L2-012` to `L2-014`.

### Negative
- None for the product; the copy is narrower than a general children's marketplace.

### Risks
- The copy names dance and sports gear and cameras alongside clothes. The accepted categories
  live in `L2-011`; a change to them must re-check the page copy and, if needed, re-record the
  video narration (`docs/mocks/_build/video/build_video.py`). Mitigation: the copy names
  kinds of things, never sizes.

## References

- [marketing-page design](../../detailed-designs/platform/marketing-page/README.md)
- `docs/specs/L2.md` (`L2-011` to `L2-014`, `L2-078` to `L2-083`)
