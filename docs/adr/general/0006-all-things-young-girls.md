# ADR-0006: All Things Young Girls

**Date:** 2026-10-26
**Category:** general
**Status:** Accepted (the seller confirmation is superseded by [ADR-0008](0008-remove-the-for-girls-confirmation.md))
**Deciders:** Quinntyne Brown (product owner)

## Context

TuTuMuch began as a clone of 12L with four girls' clothing categories (Dresses, Tops, Outerwear,
Bottoms) and one accepted size, `Girls'`, enforced by the API and the `CK_Listings_Size` check
constraint (`L1-003`, `L2-011` to `L2-014`). The marketing page already says TuTuMuch is for
clothes, dance and sports gear, cameras and more ([ADR-0005](0005-marketing-wording.md)), but the
sell form refused everything that was not clothing. Families want to pass on more than clothes:
shoes, ballet kit, soccer gear, an instant camera, dolls and craft sets. Most of those have no
clothing size label, so "the label shows girls' sizing" cannot be confirmed for them.

## Decision

TuTuMuch accepts new and gently loved **clothes, shoes, dance gear, sports equipment, cameras and
tech, toys and crafts: all things young girls**. The catalogue (`CategoryCatalog`, still code,
not configuration) becomes six categories with 26 subcategories:

| Category (route) | Subcategories |
|---|---|
| Clothing (`/clothing`) | Dresses, Tops, Bottoms, Outerwear, Pajamas, Swimwear |
| Shoes (`/shoes`) | Sneakers, Boots, Sandals, Dress shoes |
| Dance (`/dance`) | Tutus, Leotards, Dance shoes, Dance bags |
| Sports (`/sports`) | Soccer, Volleyball, Gymnastics, Swimming, Bikes and scooters |
| Cameras and tech (`/cameras-and-tech`) | Cameras, Headphones, Tablet accessories |
| Toys and crafts (`/toys-and-crafts`) | Dolls, Arts and crafts, Games and puzzles, Books |

The old Dresses, Tops, Outerwear and Bottoms become subcategories of Clothing; skirts and rompers
move into Clothing → Dresses, tutus into Dance → Tutus.

The size lock keeps its mechanism and becomes an **audience lock**: every listing is for girls.
`CategoryCatalog.Girls` (`Girls'`) stays the one accepted value, every card keeps its `Girls`
size tag, and `CK_Listings_Size` is unchanged, so no migration is needed. The seller's
confirmation changes from "the label shows girls' sizing" to **"This item is for girls"**
(error: "Confirm this item is for girls"), because cameras and bikes have no clothing label.
Category and subcategory slugs in URLs are the lower-cased names with spaces turned into
hyphens (`cameras-and-tech`, `bikes-and-scooters`).

## Options Considered

### Option 1: Six categories under one audience lock (chosen)
- **Pros:** Matches the marketing promise; no schema change; the lock, its tests and the sell
  form keep their shape; every listing is still plainly for girls.
- **Cons:** `Girls'` no longer tells a shopper the fit; that waits for real sizes.

### Option 2: Add real sizes (2T to 16, shoe sizes) now
- **Pros:** Shoppers filter by fit.
- **Cons:** A new attribute, per-category size tables, a migration, form and filter work, and
  no size at all for most new categories. Too much for this change.

### Option 3: Keep clothing only
- **Pros:** No work.
- **Cons:** The marketing page and pitch video promise more than the catalogue accepts.

## Consequences

### Positive
- The catalogue, sell form, seed data and marketing copy agree.
- No persisted shape changes; existing listings keep `Girls'`.

### Negative
- Six category links do not fit the top bar on a phone; they move into the menu sheet below the
  small-screen breakpoint (desktop keeps inline links).
- Seed data, seed photos, mocks and tests all change.

### Risks
- Sellers may list boys' or adult items under a broad category. Mitigation: the "This item is
  for girls" confirmation and the copy rule of [ADR-0005](0005-marketing-wording.md).

## Open questions

- Do real sizes (2T to 16, shoe sizes) come later as a separate listing attribute and filter?
  **Deferred**, not built here.

## References

- `docs/specs/L1.md` (`L1-003`), `docs/specs/L2.md` (`L2-011` to `L2-014`, `L2-022` to `L2-024`,
  `L2-075`)
- `src/backend/TuTuMuch.Domain/Listings/CategoryCatalog.cs`
- [create-listing design](../../detailed-designs/listings/create-listing/README.md)
