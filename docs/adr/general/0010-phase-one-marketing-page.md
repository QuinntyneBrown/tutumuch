# ADR-0010: Phase 1 Marketing Page

**Date:** 2026-10-30
**Category:** general
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

In launch Phase 1 only TuTuMuch's owner sells, and the catalog holds at most 40 listings
(`L2-091`, `L2-092`). Before this decision `/welcome` showed visitors the same page in every phase,
apart from the "Selling opens soon" swaps of `L2-094`. A Phase 1 visitor read about seller steps
(List it, Sell it, Get paid) they could not take, saw no items, and the only conversion was "Shop
the closet".

The product owner directed a series of bolder marketing mocks, logged in
`docs/mocks/marketing-changelog.md`, and chose the `welcome` `phase-one` state as the Phase 1 page.
It adds four things the rules did not allow:

1. a **tulle field**: a canvas of translucent petals behind the hero that drift and twirl around
   the pointer;
2. a **closet marquee**: two rows of real listings scrolling under the hero, which needs the
   catalog API;
3. a **closet-worth calculator**: chips for outgrown items, with a typical total that counts up
   beside "Join and sell first";
4. a **journey**: How it works as a rail that fills as the visitor scrolls.

It also changes the copy for Phase 1: the steps are the shopper's, the seller band folds into the
calculator, and "Join early" (to `/join`) sits beside "Shop the closet".

`L2-078` said that the page calls no TuTuMuch API endpoint. `L2-083`, `L2-096` AC2 and the page
guidelines (section 2) forbid decorative shapes, and the page guidelines keep transitions to 150 to
250ms.

## Decision

**In Phase 1, a visitor or a member without the Administrator role gets the Phase 1 marketing page
(`L2-112`).** An administrator, and everyone in Phases 2 and 3, keeps the page of `L2-078` and
`L2-094`.

- **One named exception for the tulle field.** The marketing hero may draw the tulle field, a
  canvas that is `aria-hidden`, coloured only by the `--color-tulle-a` to `--color-tulle-d` tokens,
  and painted once as a still frame under `prefers-reduced-motion`. It is the only decorative
  shape allowed on any page. `L2-083` and `L2-096` AC2 name the exception; there are still no
  gradients, blobs or glows, and the marquee has no fade mask.
- **The marquee reads the public catalog.** The Phase 1 page calls `GET /api/catalog/listings`
  once (page 1, the newest listings) through `CatalogApi`. If the call fails or returns fewer than
  4 listings, the marquee is left out. Each tile links to its listing.
- **The marquee and the count-up are animations, not transitions.** They run longer than 250ms and
  stop under reduced motion: the marquee rows become plain horizontal scrollers, and the total
  changes at once. Hovering or focusing a marquee row pauses it.
- **Calculator prices are static copy.** They are typical prices for the item kinds, written in
  the page, not computed from listings. The label says "Typical total on TuTuMuch".
- **The item art moves into the app.** The ten calculator chips use the mock item illustrations,
  served from `public/items/` in the application project (AGENTS.md folder structure updated).
- **The header logo is bigger on every page.** The top bar wordmark grows from `1.625rem` to
  `2.25rem` and the mark from 36 to 44px, within the 4rem top bar.
- **New design-system components:** `tl-tulle-field`, `tl-closet-marquee`, `tl-closet-worth`, and
  a `journey` mode of `tl-steps` with an icon per step.

## Options Considered

### Option 1: Keep one page for every phase, swapping words only
- **Pros:** no new components or rule exceptions.
- **Cons:** keeps seller steps a Phase 1 visitor cannot take, and shows no items.

### Option 2: A Phase 1 page with the four additions (chosen)
- **Pros:** shows real items first, puts a number on joining, and is honest about who sells.
- **Cons:** one API call on `/welcome`, and named exceptions to the shape and motion rules.

### Option 3: The additions on every phase's page
- **Pros:** one design.
- **Cons:** wider than the brief, and a bigger change to `L2-078` for Phases 2 and 3. It can follow
  later with its own decision.

## Consequences

### Positive
- A Phase 1 visitor sees the actual closet within one scroll and has a second action, "Join early".
- The rule exceptions are named in one place and tied to tokens and reduced motion.

### Negative
- `/welcome` in Phase 1 now depends on the catalog API for the marquee. A failed call removes the
  marquee and nothing else.
- Four more components to keep in step across the mocks, the design system and the library.

### Risks
- Motion on a marketing page can distract. Mitigation: the tulle field and journey only respond to
  the pointer and scroll, the marquee pauses on hover and focus, and reduced motion stops all of
  them.

## Implementation Notes

- Mock: `docs/mocks/_build/screens_marketing.py`, state `phase-one`.
- Requirements: new `L2-112`; amended `L2-078`, `L2-083`, `L2-094`, `L2-096`.
- Code: `WelcomePage` in `@tutumuch/marketing` picks the Phase 1 page from `LaunchState`.
