# ADR-0001: Defer Moderation, Review Authoring and Data Export

**Date:** 2026-10-25
**Category:** general
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

TuTuMuch is a marketplace for new and used girls' clothing. Before any
code exists, the repository carries the full product definition: user journeys
(`docs/journeys.md`), L1 and L2 requirements (`docs/specs/`), HTML mocks
(`docs/mocks/`), detailed designs (`docs/detailed-designs/`) and estimates
(`docs/estimates/`). Every one of those artefacts is generated and cross-checked by
skills, so anything that stays in them is treated as committed scope by the agents that
implement the product.

Six user journeys describe work that is not needed to open the marketplace and that
would pull a disproportionate share of the design and build effort:

| Journey | Former L2 | Former effort |
|---------|-----------|---------------|
| Report listing or member | `L2-044` | 3 days |
| Moderate reports | `L2-045` | 6 days |
| Suspend member | `L2-046` | 4 days |
| Audit log and admin sign-in | `L2-047` | 4 days |
| Leave review | `L2-038`, `L2-039` | 4 days |
| Export personal data | `L2-055` | 5 days |

Together they were 26 of 132 estimated days. They also spread into every other design:
a `NotSuspended` policy on checkout, an `IOrderModerationPort` in cancel-order, an
`IAuditLog` and `AdministratorWithSecondFactor` policy in resolve-order-problems, a
"Report" link on every listing and profile, `MemberSuspended` and `DataExportReady`
emails, a TOTP secret in the platform secret list, and an admin shell with queue and
audit tabs. Keeping them specified but unbuilt would leave agents designing against
ports and policies that nothing provides.

The product owner decided that none of this will be looked at at this time.

## Decision

Moderation, review authoring and data export are removed from the product definition
and will not be designed, mocked, specified, estimated or implemented at this time.
Concretely:

- **Journeys** Report listing or member, Moderate reports, Suspend member, Audit log and
  admin sign-in, Leave review and Export personal data are removed from
  `docs/journeys.md`.
- **Specs** `L1-012` (Trust, safety and moderation) is removed. `L2-038`, `L2-039`,
  `L2-044`, `L2-045`, `L2-046`, `L2-047` and `L2-055` are removed; their numbers are not
  reused. `L1-009` now covers only the public rating summary (`L2-040`), and `L1-014` no
  longer promises export. Mentions of suspension, reporting, review authoring and export
  in other L2 requirements (`L2-002`, `L2-003`, `L2-010`, `L2-034`, `L2-041`, `L2-042`,
  `L2-054`, `L2-057`, `L2-065`) are removed.
- **Mocks** The admin verification, audit log, moderation queue, report detail and member
  admin pages, the report listing, report member, resolve report, suspend member, leave
  review, reply to review and data export dialogs, and every `suspended` page state and
  banner are removed from the generated mocks and their build modules.
- **Detailed designs** `docs/detailed-designs/moderation/`, `reviews/leave-review/` and
  `privacy/export-personal-data/` are deleted. Every other design that depended on them
  is edited so that nothing links to a removed design or cites a removed requirement.
- **Estimates** The moderation subsystem and the two feature estimates are deleted; the
  overall estimate drops from 132 to 106 days.
- **Agent instructions** `AGENTS.md` states the deferral so that no skill or agent
  regenerates the removed artefacts.

What stays:

- Administrators still exist. The `Administrator` role check of `L2-010` guards the
  order-problem endpoints, and `resolve-order-problems` keeps its admin problem detail
  page and resolve dialog under the admin shell. The entry point is the operations alert
  email (`Notifications:OperationsInbox`, formerly `Notifications:ModerationInbox`), not
  a queue.
- The public profile (`view-member-profile`, `L2-040`, `L2-041`) keeps its rating
  summary and reviews section. Until review authoring returns, every profile shows
  "No reviews yet". The `ReviewReceived` email and the review-email preference in
  `L2-042` and `L2-043` are left in place for the same reason.
- Account closure and erasure (`L2-005`) remain in `accounts`; only the export half of
  the former "export and erase" promise is deferred.

## Options Considered

### Option 1: Remove the journeys and everything derived from them (chosen)
- **Pros:** The documentation describes exactly what will be built. No agent can pick up
  a moderation or review task by accident. Cross-subsystem ports that only moderation
  used are gone, so the remaining designs are simpler. Effort drops by 26 days.
- **Cons:** Re-introducing the features later means re-deriving journeys, specs, mocks
  and designs. Git history keeps the removed artefacts (last present at commit
  `8d2f93f`), which softens this.

### Option 2: Keep the artefacts and mark them "deferred"
- **Pros:** Nothing to recreate later. Traceability stays complete.
- **Cons:** Every skill that regenerates mocks, estimates or designs would keep
  including them. Other designs would keep depending on ports and policies that no one
  builds, and agents implementing those designs would have to build stubs.

### Option 3: Keep the specs but delete the designs, mocks and estimates
- **Pros:** Less to delete; requirements remain as a backlog.
- **Cons:** The spec-driven skills treat `docs/specs/` as the source of truth and would
  regenerate the deleted artefacts on the next run. The requirement cross-references
  (suspension gates, audit entries, export contents) would still contaminate the
  remaining designs.

## Consequences

### Positive
- The remaining 73 journey days and 106 estimated days describe a coherent, buildable
  product with no dangling dependencies.
- No admin second factor, audit log, report pipeline or export job to build, test or
  secure before launch.
- Cancel-order, checkout, ship-order and resolve-order-problems lose one external port
  or policy each and become self-contained.

### Negative
- There is no way to report, hide or remove a bad listing or member through the
  product; operators act through the database or support tooling.
- Members cannot rate each other, so the reputation shown on profiles stays empty and
  the "Review" step in the README's five-step story does not exist yet.
- Members cannot download their data; the privacy promise is limited to minimisation,
  protection and erasure.
- Administrator actions on order problems are only structured-logged, not kept in an
  append-only audit table.

### Risks
- Launching a marketplace without reporting or suspension exposes it to abuse that the
  team must handle by hand. Accepted for now; revisit before public launch.
- Depending on jurisdiction, a data-export capability may be required for compliance.
  Accepted for now; the erasure path remains.
- Removed L2 numbers leave gaps (`L2-038`, `L2-039`, `L2-044` to `L2-047`, `L2-055`).
  They are documented in the L2 traceability matrix and must not be reused.

## Implementation Notes

- Do not create `docs/detailed-designs/moderation/`, `reviews/leave-review/`,
  `privacy/export-personal-data/`, mocks for reporting, moderation, suspension, audit,
  review authoring or export, or any spec that revives the removed L2 numbers.
- The admin shell (`AdminTopbarComponent`, `shell: 'admin'`) is kept for the
  order-problem pages only and has no navigation tabs.
- `RemovalReason` on `Listing` has the values `Withdrawn` and `OrderCancelled` only.
- When this decision is revisited, start from the artefacts at commit `8d2f93f` and
  supersede this ADR.

## References

- `docs/journeys.md`, `docs/specs/L1.md`, `docs/specs/L2.md`
- `docs/detailed-designs/README.md` (Deferred section)
- `docs/estimates/README.md`
- `AGENTS.md`
