# Deployment

TuTuMuch runs in production on one small Azure Linux VM with Docker Compose, backed by an
Azure SQL Database, at `tutumuch.kids`. Every push to `main` is deployed automatically once
CI's `npm run verify` has passed on it; there is no manual step. The setup is sized for a
few users and mirrors 12L's ([ADR platform/0012](adr/platform/0012-low-cost-single-vm-continuous-deployment.md),
`L2-084` to `L2-086`, [design](detailed-designs/platform/deploy-to-production/README.md)).

## Topology

| Piece                  | Where                                   | Notes                                                                 |
| ---------------------- | --------------------------------------- | --------------------------------------------------------------------- |
| `web` (Caddy)          | VM, ports 80/443, 128 MB                | Serves the Angular build, terminates TLS (Let's Encrypt), proxies `/api` and `/photos` to `api`; `index.html` and SPA routes are sent with `Cache-Control: no-cache` |
| `api`                  | VM, internal port 8080, 512 MB          | `TuTuMuch.Api`; photos on the `photos` Docker volume at `/data/photos` |
| `worker`               | VM, 384 MB                              | `TuTuMuch.Worker`; shares the `photos` volume                         |
| `migrator`             | VM, run once per deploy, 384 MB         | `TuTuMuch.Migrator`; applies EF Core migrations before the restart    |
| Database               | Azure SQL Database, Basic tier          | Firewall allows only the VM's public IP                               |
| DNS                    | Domain registrar                        | `A` records for `@` and `www` of `tutumuch.kids` point at the VM's static public IP |

One instance of each service, no load balancer, no staging environment. Every service logs
through Docker's `json-file` driver, rotated at 10 MB with 3 files kept. The VM is a B1s
(1 vCPU, 1 GB) or B1ms (1 vCPU, 2 GB) with a 2 GB swap file. A deploy restarts the stack,
so the site is unavailable for a few seconds.

All of it is defined in [`deploy/`](../deploy):

- `backend.Dockerfile` builds `api`, `worker` and `migrator` from one restore (context `src/backend`).
- `web/Dockerfile` builds the Angular workspace and packages it with Caddy and `web/Caddyfile` (context: repository root).
- `docker-compose.yml` is the production stack; the deploy workflow copies it to `/opt/tutumuch` on the VM.
- `.env.example` lists the settings the stack needs; the real `/opt/tutumuch/.env` lives only on the VM.
- `cloud-init.yaml` is the VM's first-boot script (Docker, swap, `/opt/tutumuch`).

## Deployment workflow

[`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml) starts when a `CI` run for a
push to `main` succeeds (`workflow_run`). A failed CI run, or a run for a pull request,
deploys nothing. Deploys queue in the `deploy-production` group and never overlap.

1. **build** (the only job allowed to write packages): builds `tutumuch-api`,
   `tutumuch-worker`, `tutumuch-migrator` and `tutumuch-web` from the commit CI tested and
   pushes them to GitHub Container Registry, tagged with the commit SHA and `latest`.
2. **deploy** (`production` environment): over SSH, copies `docker-compose.yml`, logs in to
   GHCR with the job's token on stdin, sets `IMAGE_TAG` in `.env`, pulls, runs
   `docker compose run --rm -T migrator < /dev/null`, then `docker compose up -d` and
   prunes images older than 72 hours. The script runs under `set -euo pipefail`, so a
   failed migration stops it before the restart and the old containers keep serving.
3. **smoke test**: polls `https://$SITE_HOST/api/health` for up to two minutes; no 200 fails
   the run. The health endpoint does not need the database
   ([ADR platform/0004](adr/platform/0004-health-check-without-database.md)).

Secrets, in the repository's `production` environment only (never committed or printed):

| Secret           | Value                                                     |
| ---------------- | --------------------------------------------------------- |
| `DEPLOY_HOST`    | Public IP of the VM                                       |
| `DEPLOY_SSH_KEY` | Private key whose public half is in `azureuser`'s `authorized_keys` |
| `SITE_HOST`      | `tutumuch.kids`                                           |

Application secrets (connection string, signing keys) live only in `/opt/tutumuch/.env` on
the VM.

## Rolling back

Either:

- **From GitHub**: Actions → Deploy → Run workflow, with `ref` set to the full SHA of an
  earlier commit that passed CI. It rebuilds that commit (mostly from cache), runs the
  migrator and restarts on images tagged with that SHA.
- **On the VM**, without CI:

  ```bash
  ssh azureuser@<vm-ip>
  cd /opt/tutumuch
  sed -i 's/^IMAGE_TAG=.*/IMAGE_TAG=<older-sha>/' .env
  docker compose up -d
  ```

  This works while that SHA's images are still in GHCR.

Migrations are forward-only. Rolling back past a migration that the older code cannot read
needs a new, compatible migration first; restoring the database is covered by its
point-in-time backups (`L2-067`).

## Operating the server

```bash
ssh azureuser@<vm-ip>
cd /opt/tutumuch
docker compose ps                         # status
docker compose logs -f api                # or worker, web
docker compose run --rm -T migrator       # apply migrations by hand
docker compose up -d                      # restart with the tags in .env
```

Photos live in the `tutumuch_photos` Docker volume. Back it up with
`docker run --rm -v tutumuch_photos:/data -v "$PWD":/backup alpine tar czf /backup/photos.tgz -C /data .`.

## Current resources

Created with the Azure CLI on 2026-10-26 in the Pay-As-You-Go subscription, region Canada Central:

| Resource | Name |
| --- | --- |
| Resource group | `tutumuch-prod` |
| VM | `tutumuch-vm`, Ubuntu 24.04, `Standard_B1s`, 30 GB Standard SSD, static IP `20.104.204.145`, NSG `tutumuch-vmNSG` (22, 80, 443) |
| SQL | server `tutumuch-sql` (`tutumuch-sql.database.windows.net`), database `TuTuMuch`, Basic, firewall rule `vm` |
| Deploy key | `~/.ssh/tutumuch_deploy` on the owner's machine; the same key is `DEPLOY_SSH_KEY` |

The `production` environment holds `DEPLOY_HOST`, `DEPLOY_SSH_KEY` and `SITE_HOST`. The SQL
password and signing keys exist only in `/opt/tutumuch/.env` on the VM. DNS for
`tutumuch.kids` is managed at Namecheap.

## Monthly cost

At pay-as-you-go prices, roughly: a B1s VM about US$8 (B1ms about US$15), its 30 GB
standard SSD about US$2 to 5, a static public IP about US$4, and Azure SQL Basic about
US$5, so **about US$20 to 30 a month**. GHCR and GitHub Actions are free for this public
repository's usage. Check the Azure pricing calculator for your region.

## Building from scratch

1. Create a resource group, a Basic Azure SQL server and database, and an Ubuntu VM (B1s or
   B1ms) with a static public IP, using `deploy/cloud-init.yaml` as custom data. Open ports
   22, 80 and 443.
2. Add the VM's public IP to the SQL server firewall.
3. On the VM, create `/opt/tutumuch/.env` from `deploy/.env.example` with real values.
4. Create an SSH key pair for deploys; add the public half to `azureuser`'s
   `~/.ssh/authorized_keys`.
5. Point `tutumuch.kids` and `www.tutumuch.kids` at the VM's IP.
6. In GitHub, create the `production` environment and set `DEPLOY_HOST`, `DEPLOY_SSH_KEY`
   and `SITE_HOST` in it.
7. Run the Deploy workflow by hand once (or push to `main`). From then on every green push
   to `main` deploys itself.
