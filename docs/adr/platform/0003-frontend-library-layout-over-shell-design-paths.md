# ADR-0003: Frontend Library Layout Takes Precedence Over Older Design Paths

**Date:** 2026-10-25
**Category:** platform
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

The responsive-accessible-shell design and other early designs place frontend code under
`src/frontend/src/app/core/...` and `src/frontend/src/app/shared/...`. `AGENTS.md` later
mandated an Angular workspace with one application (`projects/tutumuch`) and one library
per area (`api`, `components` and six feature libraries).

## Decision

The `AGENTS.md` layout wins. Shell components, routes and `AppComponent` live in
`projects/tutumuch/src/app/`. Reusable design-system pieces, including the skip link, top
bar, footer, toast region, sheets, dialogs and form fields, live in
`projects/components/` as `tl-*` components. HTTP access, auth state and interceptors live
in `projects/api/`. A path in an older design is read as the matching library; the shell
design and the designs README were updated to say so.

## Options Considered

### Option 1: Follow AGENTS.md and update the designs (chosen)
- **Pros:** One layout; enforceable library boundaries; matches the promise-hub reference workspace.
- **Cons:** Older designs needed path edits.

### Option 2: Single application with core/ and shared/ folders
- **Pros:** Fewer projects.
- **Cons:** Contradicts a mandatory rule; no compile-time boundary between features.

## Consequences

### Positive
- Feature libraries cannot import each other; shared code has an obvious home.

### Negative
- More `angular.json` and `tsconfig` entries to maintain.

### Risks
- Remaining stale paths in designs not yet touched. Mitigated by the note in the designs README.

## Implementation Notes

- Path aliases `@tutumuch/<library>`; selector prefix `tl`.

## References

- `AGENTS.md` (Frontend workspace layout)
- [responsive-accessible-shell](../../detailed-designs/platform/responsive-accessible-shell/README.md)
