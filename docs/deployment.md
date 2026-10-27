# Deployment

TuTuMuch runs in production on one small Azure Linux VM with Docker Compose, backed by an
Azure SQL Database, at `tutumuch.kids` (the marketing page) and `uat.tutumuch.kids` (the full
application, for user acceptance testing, on the same API and database). Every push to
`main` is deployed automatically to both hosts once CI's `npm run verify` has passed on it;
there is no manual step. The setup is sized for a
few users and mirrors 12L's ([ADR platform/0012](adr/platform/0012-low-cost-single-vm-continuous-deployment.md),
`L2-084` to `L2-086`, [design](detailed-designs/platform/deploy-to-production/README.md)).

## Topology

| Piece                  | Where                                   | Notes                                                                 |
| ---------------------- | --------------------------------------- | --------------------------------------------------------------------- |
| `web` (Caddy)          | VM, ports 80/443, 128 MB                | Serves the marketing-only build (`/srv/marketing`) on `www`, and the full application (`/srv/app`) on `uat` ([ADR platform/0013](adr/platform/0013-uat-host-in-the-production-web-image.md)); terminates TLS (Let's Encrypt), proxies `/api` and `/photos` of both hosts to `api`; `index.html` and SPA routes are sent with `Cache-Control: no-cache` |
| `api`                  | VM, internal port 8080, 512 MB          | `TuTuMuch.Api`; photos on the `photos` Docker volume at `/data/photos` |
| `worker`               | VM, 384 MB                              | `TuTuMuch.Worker`; shares the `photos` volume                         |
| `migrator`             | VM, run once per deploy, 384 MB         | `TuTuMuch.Migrator`; applies EF Core migrations before the restart    |
| Database               | Azure SQL Database, Basic tier          | Firewall allows only the VM's public IP                               |
| DNS                    | Namecheap                               | `A` records for `@`, `www` and `uat` of `tutumuch.kids` point at the VM's static public IP; Caddy redirects the bare domain to `www.tutumuch.kids` |

One instance of each service, no load balancer, no staging environment: `uat` is a second
host on the same stack, not a second environment, so whatever is done there (sign-ups,
listings, orders) happens in the production database. Every service logs
through Docker's `json-file` driver, rotated at 10 MB with 3 files kept. The VM is a B1s
(1 vCPU, 1 GB) or B1ms (1 vCPU, 2 GB) with a 2 GB swap file. A deploy restarts the stack,
so the site is unavailable for a few seconds.

All of it is defined in [`deploy/`](../deploy):

- `backend.Dockerfile` builds `api`, `worker` and `migrator` from one restore (context `src/backend`).
- `web/Dockerfile` builds the Angular workspace twice from one library build (`npm run build:web`: the `marketing` configuration to `dist/marketing`, the `production` configuration to `dist/app`) and packages both with Caddy and `web/Caddyfile` (context: repository root).
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
   prunes images older than 72 hours. It also rewrites `SITE_ADDRESS` in `.env` as
   `<SITE_HOST>, www.<SITE_HOST>, uat.<SITE_HOST>`, so Caddy requests a certificate for
   each host and a new host needs only a DNS record. The script runs under `set -euo pipefail`, so a
   failed migration stops it before the restart and the old containers keep serving.
3. **smoke test**: polls `https://$SITE_HOST/api/health` for up to two minutes; no 200 fails
   the run. The health endpoint does not need the database
   ([ADR platform/0004](adr/platform/0004-health-check-without-database.md)).
4. **smoke test UAT**: polls `https://uat.$SITE_HOST/` for up to two minutes; no 200 fails
   the run (Caddy may still be obtaining the certificate for a brand-new host).

Secrets, in the repository's `production` environment only (never committed or printed):

| Secret           | Value                                                     |
| ---------------- | --------------------------------------------------------- |
| `DEPLOY_HOST`    | Public IP of the VM                                       |
| `DEPLOY_SSH_KEY` | Private key whose public half is in `azureuser`'s `authorized_keys` |
| `SITE_HOST`      | `tutumuch.kids` (the `www` and `uat` hosts are derived from it) |

Application secrets (connection string, signing keys, the Google OAuth client) live only in
`/opt/tutumuch/.env` on the VM.

## Sign in with Google

"Continue with Google" on `https://uat.tutumuch.kids/sign-in` and `/join` ([ADR accounts/0001](adr/accounts/0001-sign-in-with-google.md),
`L2-088`) is off until the API has a Google OAuth client. Nothing in the repository changes to
turn it on; the steps below are the owner's, outside the repository.

1. In the [Google Cloud Console](https://console.cloud.google.com/), create or pick a project and
   open **APIs & Services → OAuth consent screen**. Choose *External*, give the app the name
   TuTuMuch, a support email and the `tutumuch.kids` domain, and add the `email` and `openid`
   scopes. While the app is in *Testing*, only the test users you list can sign in; publish it to
   let anyone sign in (Google may ask to verify the domain).
2. Open **APIs & Services → Credentials → Create credentials → OAuth client ID**, type
   *Web application*, name `TuTuMuch UAT`. Add:
   - Authorised JavaScript origins: `https://uat.tutumuch.kids`
   - Authorised redirect URIs: `https://uat.tutumuch.kids/api/auth/google/callback`

   For local development add `http://localhost:4200` as an origin and
   `http://localhost:4200/api/auth/google/callback` as a redirect URI (the Angular dev server
   proxies `/api` to the API, so the browser only ever sees port 4200). The marketing host
   `www.tutumuch.kids` has no sign-in and gets no entry.
3. Copy the client ID and secret into `/opt/tutumuch/.env` on the VM:

   ```bash
   ssh azureuser@20.104.204.145
   cd /opt/tutumuch
   printf 'Auth__Google__ClientId=%s\n' '<client id>' >> .env
   printf 'Auth__Google__ClientSecret=%s\n' '<client secret>' >> .env
   docker compose up -d api
   ```

   `env_file: .env` already passes every variable to the `api` container, so nothing else
   changes. The API refuses to start with only one of the two values set.
4. Check it: `curl -s https://uat.tutumuch.kids/api/auth/providers` answers `{"google":true}`,
   `/sign-in` shows "Continue with Google", and pressing it reaches Google's consent page
   (the API builds the redirect URI from the request host and `X-Forwarded-Proto`; set
   `Auth__Google__RedirectUri=https://uat.tutumuch.kids/api/auth/google/callback` in `.env` if
   Google reports a redirect URI mismatch).

To turn the feature off, remove both lines and restart `api`; existing Google-linked members then
need a password reset (`L2-009`, not built yet) to sign in.

Locally, keep the values out of `appsettings*.json` and use user secrets from `src/backend/TuTuMuch.Api`:

```bash
dotnet user-secrets init
dotnet user-secrets set "Auth:Google:ClientId" "<client id>"
dotnet user-secrets set "Auth:Google:ClientSecret" "<client secret>"
```

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


## Changing the launch phase

TuTuMuch opens in three launch phases ([ADR platform/0014](adr/platform/0014-launch-phase-configuration.md),
`L2-089` to `L2-095`). `deploy/docker-compose.yml` passes `Launch__Phase` to `api` and `worker`
with default `1`, so a VM whose `.env` does not set it runs in Phase 1.

| Phase | Who can list | Category menus | Catalog size |
| --- | --- | --- | --- |
| 1 (default) | The owner (an `Administrator`) only | Hidden; one flat catalog at `/` | Fewer than 40 listings |
| 2 | The owner only | All six | No limit |
| 3 | Every verified member | All six | No limit |

To move to another phase (or back), on the VM:

```bash
cd /opt/tutumuch
grep -q '^Launch__Phase=' .env && sed -i 's/^Launch__Phase=.*/Launch__Phase=2/' .env || echo 'Launch__Phase=2' >> .env
docker compose up -d api worker           # recreate both with the new value
curl -fsS https://<SITE_HOST>/api/launch  # {"phase":2,...}
```

No image, migration or data changes: listings made in any phase stay as they are. A value
other than 1, 2 or 3 stops `api` at startup with `Launch:Phase must be 1, 2 or 3`; set a valid
value and run `docker compose up -d api worker` again. The browser may keep the previous answer
of `/api/launch` for up to 60 seconds. The owner sells with the usual sell and edit screens,
signed in with an account that holds the `Administrator` role.

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
5. Point `tutumuch.kids`, `www.tutumuch.kids` and `uat.tutumuch.kids` at the VM's IP.
6. In GitHub, create the `production` environment and set `DEPLOY_HOST`, `DEPLOY_SSH_KEY`
   and `SITE_HOST` in it.
7. Run the Deploy workflow by hand once (or push to `main`). From then on every green push
   to `main` deploys itself.
