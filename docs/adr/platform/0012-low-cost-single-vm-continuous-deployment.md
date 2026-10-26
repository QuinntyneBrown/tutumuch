# ADR-0012: Continuous Deployment to One Low-Cost Azure Linux VM

**Date:** 2026-10-26
**Category:** platform
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

TuTuMuch serves only a few users. Commit `f453ab3` ported 12L's production setup
(`.github/workflows/deploy.yml`, `deploy/`, `docs/deployment.md`) but left the deploy
workflow manual-only, kept the bug 12L fixed in `56e2df9` (the migrator read the rest of
the SSH script from stdin, so the restart never ran), let browsers cache `index.html`, did
not gate a deploy on a green `verify`, and set no memory or log limits for a small VM.

## Decision

- **Hosting.** One Azure Linux VM of the B1s or B1ms class (1 to 2 vCPU, 1 to 2 GB RAM, the
  2 GB swap file from `deploy/cloud-init.yaml`) runs `deploy/docker-compose.yml`: one `web`
  (Caddy, TLS by Let's Encrypt), one `api`, one `worker` and a one-off `migrator`. The
  database is Azure SQL Database, Basic tier. Images are on GitHub Container Registry (GHCR)
  as `tutumuch-api|worker|migrator|web`, tagged with the commit SHA and `latest`.
- **Trigger.** `deploy.yml` runs on `workflow_run` of the `CI` workflow, only when that run
  was a `push` to `main` and concluded `success`, and checks out
  `github.event.workflow_run.head_sha`. `verify` runs once, in CI, and stays the single
  gate (`L2-071`, ADR platform/0002). `workflow_dispatch` stays for manual redeploys and
  rollbacks, with an optional `ref` input (a full commit SHA). The `deploy-production`
  concurrency group keeps deploys from overlapping.
- **Deploy.** Over SSH as `azureuser`: copy the compose file, log in to GHCR with the
  job's short-lived token piped through stdin, set `IMAGE_TAG`, pull, run
  `docker compose run --rm -T migrator < /dev/null` under `set -euo pipefail` (a failed
  migration stops the deploy before the restart), `docker compose up -d`, prune images
  older than 72 hours, then poll `https://<SITE_HOST>/api/health`; no 200 within two
  minutes fails the run. The host key is taken with `ssh-keyscan` as 12L does; the
  private key is written with `umask 077`.
- **Secrets.** `DEPLOY_HOST`, `DEPLOY_SSH_KEY` and `SITE_HOST` live only in the GitHub
  `production` environment; application secrets live only in `/opt/tutumuch/.env` on the
  VM. `packages: write` is granted to the build job only.
- **Small-VM limits.** Every service has a `mem_limit`, Docker `json-file` log rotation,
  and one replica. Caddy sends `Cache-Control: no-cache` for `index.html` and SPA routes
  so a deploy shows on the next page load; hashed assets stay cacheable.
- **Folders.** `deploy/` (compose file, Dockerfiles, Caddyfile, cloud-init, `.env.example`)
  and `docs/deployment.md` are added to the mandatory layout in `AGENTS.md`.
- **Rollback.** Run the Deploy workflow by hand with `ref` set to an older SHA, or set
  `IMAGE_TAG` in `/opt/tutumuch/.env` on the VM and run `docker compose up -d`.
  Migrations are forward-only, so a rollback past a schema change needs a compatible
  migration first.

## Options Considered

### Option 1: `workflow_run` after CI (chosen)
- **Pros:** `verify` runs once per push; the deploy can only start from a green CI run.
- **Cons:** The deploy workflow runs from the default branch's copy of `deploy.yml`.

### Option 2: Call `verify` as the first job of `deploy.yml`
- **Pros:** One workflow file per run.
- **Cons:** `verify` runs twice on every push to `main`, doubling CI minutes.

### Option 3: Azure App Service or Container Apps
- **Pros:** Managed TLS, scaling and slots.
- **Cons:** Costs more than one small VM for a few users; diverges from 12L.

## Consequences

### Positive
- A merge to `main` goes live at `https://tutumuch.kids` after CI is green, with no manual step.
- Monthly cost stays near one B1s/B1ms VM plus Azure SQL Basic.

### Negative
- Deliberately left out: a staging environment, blue-green or zero-downtime deploys,
  autoscaling, a second VM, a load balancer, Terraform or Bicep. With few users these cost
  more than they save; brief downtime during `docker compose up -d` is accepted. The
  failover criterion of `L2-066` is therefore not met by this topology.
- No load or performance gate in the pipeline (ADR general/0002, ADR general/0003).

### Risks
- `ssh-keyscan` trusts the host key on first contact each run. Mitigation: the VM has a
  static IP and the deploy key is restricted to `azureuser`.
- One VM is a single point of failure. Mitigation: rollback by SHA and the steps in
  `docs/deployment.md` rebuild the host from scratch.

## References

- [deploy-to-production design](../../detailed-designs/platform/deploy-to-production/README.md)
- [docs/deployment.md](../../deployment.md)
- [ADR general/0002](../general/0002-no-load-or-scale-tests.md)
- [ADR platform/0002](0002-single-verify-command.md)
- [ADR platform/0004](0004-health-check-without-database.md)
