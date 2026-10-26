# Estimate — Deploy to production

## Basis

The hours assume an intermediate developer who knows GitHub Actions and Docker Compose but
not yet the TuTuMuch codebase. They cover the workflow and deploy files, the static tests
and the docs, and exclude requirements, design and creating the Azure resources, secrets
and DNS, which the owner does by hand. They assume the port of 12L's `deploy/` and
workflows from commit `f453ab3`. Tests are static file checks only; no Docker or database.
[Detailed design](../../detailed-designs/platform/deploy-to-production/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `workflow_run` gate on CI, `workflow_dispatch` `ref` input, deploy SHA in every job | 3 |
| Migrate with `-T` and `/dev/null`, token over stdin, `umask 077`, per-job permissions | 2 |
| Caddy `no-cache` rule for `index.html` and SPA routes | 1 |
| Memory limits and log rotation in `docker-compose.yml` | 2 |
| `DeploymentPipelineTests` (trigger, migrate line, permissions, limits, logs, cache header) | 3 |
| `docs/deployment.md`: rollback, cost, building from scratch; README and CHANGELOG | 3 |
| First production deploy and rollback rehearsal, checked by hand | 2 |
| **Total** | **16** |

## Minimal scope

The minimal version restores the push trigger and fixes the migrate-and-restart line and
the cache header, without the CI gate, limits or tests. Minimal total: **5** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| CI gate | A red `main` can still deploy | 2 |
| Memory limits and log rotation | One service can starve the VM or fill its disk | 2 |
| Static tests | A regression in the pipeline files is caught only in production | 3 |
