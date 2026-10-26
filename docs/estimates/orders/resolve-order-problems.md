# Resolve order problems — estimate

## Basis

The estimate assumes the moderation subsystem provides the queue page, `IAuditLog` and the `AdministratorWithSecondFactor` policy, and that the listings subsystem provides `IImageInspector` and `IPhotoStorage`. The partial-refund outcome is `<TO SUPPLY>`; the hours assume it records a refund amount and leaves the order status rule to a later change. Hours cover implementation, unit and integration tests, acceptance tests and review fixes for an intermediate Angular/TypeScript and ASP.NET Core/C# developer; they exclude requirements, design and mock work. [Detailed design](../../detailed-designs/orders/resolve-order-problems/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `OrderProblem`, `ProblemPhoto`, migrations, reference sequence | 5 |
| `ReportProblemCommand`, validator, handler with private photo storage and payout hold | 8 |
| Problem photo streaming endpoint with party authorization | 3 |
| `RespondToProblemCommandHandler` and endpoint | 3 |
| `ResolveProblemCommand`, validator and handler for three outcomes with audit entries | 8 |
| `AdminProblemsController` and `GetProblemDetailQueryHandler` | 4 |
| `OrderProblemQueueSource` (`IOrderProblemQueueSource`) | 2 |
| `ReportProblemDialog` with photo uploader and four states | 7 |
| `ProblemThreadComponent` and problem states on order and sale pages | 5 |
| `ProblemDetailPage` and `ResolveProblemDialog` | 7 |
| Unit and integration tests for `L2-036` criteria 1 to 5 | 7 |
| Playwright acceptance tests for buyer, seller and administrator | 6 |
| **Total** | **65** |

## Minimal scope

Minimal scope keeps reporting with reason and description, the payout hold, the seller reply and administrator resolution with full refund or release. It drops evidence photos and the partial-refund outcome. It satisfies `L2-036` criteria 2 to 5 and the hold rule; it defers the photos in criterion 1 and the partial refund named in the requirement statement.

Minimal scope total: **50 hours**.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| Evidence photos | Administrators decide without photos; photo part of `L2-036` criterion 1 deferred | 11 |
| Partial refund outcome | Only full refund or release is available | 3 |
