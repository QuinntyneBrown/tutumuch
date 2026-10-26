# Privacy subsystem

The privacy subsystem realizes `L1-014`: TuTuMuch collects only the personal data it needs,
keeps it private, and protects it at rest and in transit. It supplies the public DTO
allow-list and field-level encryption that other subsystems build on, and the cookie
consent banner and dialog. Data export is deferred; see
[ADR 0001](../../adr/general/0001-defer-moderation-reviews-and-data-export.md). Account closure and erasure (`L2-005`) belong to the accounts subsystem.

| Feature | L2 | Estimate |
|---------|----|----------|
| [protect-personal-data](protect-personal-data/README.md) | `L2-054` | [65 hours](../../estimates/privacy/protect-personal-data.md) |
| [manage-cookie-consent](manage-cookie-consent/README.md) | `L2-056` | [37 hours](../../estimates/privacy/manage-cookie-consent.md) |
