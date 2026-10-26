# Estimate — Protect credentials and the supply chain

## Basis

The hours assume an intermediate Angular/TypeScript and ASP.NET Core/C# developer
with working knowledge of GitHub Actions, who does not yet know the Tutu Much
codebase. They cover implementation, automated tests and review fixes, and
exclude requirements, design and mock work. The secret store product and its
.NET configuration provider are `<TO SUPPLY>`; the hours assume a provider
package exists for it and that the hosting platform supplies a workload
identity. Calling the hasher from registration, sign-in and password reset is
counted in the accounts estimates.
[Detailed design](../../detailed-designs/platform/protect-credentials-and-supply-chain/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `IPasswordHasher`, `PasswordVerification`, `EncodedPasswordHash` | 2 |
| `Pbkdf2PasswordHasher` with constant-time verify and `NeedsRehash`; `PasswordHashingOptions` with start validation | 4 |
| Hasher unit tests (format, salt uniqueness, iteration floor, rehash) and database-record acceptance test | 3 |
| `AddTutuMuchSecretStore` with workload identity and `Development` user-secrets fallback | 4 |
| `SecretKeys` catalogue and `SecretGuard` provider-source check with tests | 5 |
| Remove secret keys from `appsettings*.json`; deployed-configuration inspection test | 2 |
| `secret-scan` job with `.gitleaks.toml` and fixture allow-list | 3 |
| Lock files for .NET projects and `dependency-scan` job with Trivy, SARIF and artifact upload | 4 |
| `.trivyignore` expiry check script | 2 |
| `static-analysis` job: CodeQL for C# and TypeScript; analyzer rule severities in `Directory.Build.props` | 5 |
| Dependabot configuration and required status checks | 1 |
| Acceptance check: pull request with a known critical package fails and names it | 2 |
| **Total** | **37** |

## Minimal scope

The minimal version ships the PBKDF2 hasher without the rehash path, the secret
store provider without `SecretGuard`, the gitleaks job, a Trivy job on the lock
files with SARIF artifacts, and CodeQL with default queries. It drops analyzer
severity tuning, the `.trivyignore` expiry check, Dependabot and the
fail-on-critical acceptance test. It still satisfies `L2-052` and `L2-053`;
protection against a secret placed in a deployed file rests on review rather
than a start-up check. Minimal total: **24** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| `SecretGuard` provider-source check | A secret placed in `appsettings.Production.json` is not stopped at start | 4 |
| Rehash on sign-in | Raising the iteration count does not upgrade existing hashes | 1 |
| `.trivyignore` expiry check | Suppressed vulnerabilities can stay suppressed indefinitely | 2 |
| Analyzer rule severities as errors | Some insecure API uses appear as warnings only | 2 |
| Dependabot | Dependency updates rely on manual effort | 1 |
