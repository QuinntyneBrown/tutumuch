# Protect personal data — estimate

## Basis

The estimate assumes an intermediate Angular/TypeScript and ASP.NET Core/C# developer
who knows EF Core and `System.Security.Cryptography` but does not know this codebase.
The hours cover implementation, tests and review fixes for the
[Detailed design](../../detailed-designs/privacy/protect-personal-data/README.md). They
assume the public DTOs themselves are built by the catalog, listings and reviews
features, and that the secret store and hosting platform (`<TO SUPPLY>`) offer key
storage, transparent data encryption and server-side object encryption as settings. A
platform without those settings would move the infrastructure rows.

## Full design

| Work item | Hours |
|-----------|------:|
| `PrivacyPageComponent` at `/privacy` with six rules and the "Cookie settings" link | 3 |
| `IPublicDto` marker, review of public DTO records and serialiser settings | 4 |
| `PublicResponseAttribute` and `PublicResponseGuardFilter` with `privacy.public_dto_required` | 4 |
| `PublicDtoAllowListTests` architecture test over `PersonalDataFieldNames` | 3 |
| `PublicResponsesContainNoPersonalDataTests` contract tests for the public endpoints | 5 |
| `ShippingAddress` value object and `ShippingAddressProtector` | 3 |
| `AesGcmFieldEncryptor` envelope format with unit tests | 6 |
| `FieldEncryptionKeyRing` loading active and retired keys from the secret store | 5 |
| `EncryptedShippingAddressConverter`, EF Core mapping and migration for `Orders` and `SavedAddresses` | 5 |
| Raw-column ciphertext integration test | 3 |
| `ReEncryptShippingAddressesJob` in `Tutu Much Worker` | 6 |
| `DatabaseEncryptionHealthCheck` | 3 |
| `PhotoStorageEncryptionHealthCheck` | 3 |
| Infrastructure-as-code encryption settings and `verify-encryption-at-rest` CI step | 6 |
| `PersonalDataDestructuringPolicy` for log redaction | 2 |
| Review fixes | 4 |
| **Total** | **65** |

## Minimal scope

The minimal scope keeps the public DTO allow-list with its architecture and contract
tests, the Privacy page, and AES-GCM field encryption of shipping addresses with one key
from the secret store. It also keeps the platform encryption settings in the templates.
It leaves out `PublicResponseGuardFilter`, key rotation and `ReEncryptShippingAddressesJob`,
both health checks, the CI verification step and the log redaction policy. It still
satisfies all three acceptance criteria of `L2-054`, verified by tests and by
inspecting configuration. It defers runtime detection of a disabled encryption setting
and key rotation without downtime. Minimal scope totals **42** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| `PublicResponseGuardFilter` | A non-public type on a public endpoint is caught only by tests, not at run time | 4 |
| `ReEncryptShippingAddressesJob` | Key rotation leaves old rows on the retired key until a manual script runs | 6 |
| Multi-key `FieldEncryptionKeyRing`; keep one key | Rotating the key needs a maintenance window | 2 |
| Both encryption health checks | A disabled at-rest setting is not reported by the readiness probe | 6 |
| `verify-encryption-at-rest` CI step | Template drift is caught only by manual review | 3 |
| `PersonalDataDestructuringPolicy` | Address redaction in logs relies on developer care (`L2-069`) | 2 |
