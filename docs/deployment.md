# Deployment

TuTuMuch is designed to run in production on one small Azure Linux VM with Docker Compose,
backed by an Azure SQL Database, at `tutumuch.kids`. The deploy workflow is **manual-only**
until TuTuMuch has its own production host and the `DEPLOY_HOST`, `DEPLOY_SSH_KEY` and
`SITE_HOST` secrets exist; once they do, restore its push-to-`main` trigger.

## Topology

| Piece                  | Where                                   | Notes                                                                 |
| ---------------------- | --------------------------------------- | --------------------------------------------------------------------- |
| `web` (Caddy)          | VM, ports 80/443                        | Serves the Angular build, terminates TLS (Let's Encrypt), proxies `/api` and `/photos` to `api` |
| `api`                  | VM, internal port 8080                  | `TuTuMuch.Api`; photos on the `photos` Docker volume at `/data/photos` |
| `worker`               | VM                                      | `TuTuMuch.Worker`; shares the `photos` volume                         |
| `migrator`             | VM, run once per deploy                 | `TuTuMuch.Migrator`; applies EF Core migrations before `api` restarts |
| Database               | Azure SQL Database, Basic tier          | Firewall allows only the VM's public IP                               |
| DNS                    | Domain registrar                        | `A` records for `@` and `www` of `tutumuch.kids` point at the VM's static public IP |

All of it is defined in [`deploy/`](../deploy):

- `backend.Dockerfile` builds `api`, `worker` and `migrator` from one restore (context `src/backend`).
- `web/Dockerfile` builds the Angular workspace and packages it with Caddy and `web/Caddyfile` (context: repository root).
- `docker-compose.yml` is the production stack; the deploy workflow copies it to `/opt/tutumuch` on the VM.
- `.env.example` lists the settings the stack needs; the real `/opt/tutumuch/.env` lives only on the VM.
- `cloud-init.yaml` is the VM's first-boot script (Docker, swap, `/opt/tutumuch`).

## Deployment workflow

[`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml) runs only when started by hand
(`workflow_dispatch`) for now:

1. Builds the four images `tutumuch-api`, `tutumuch-worker`, `tutumuch-migrator` and
   `tutumuch-web` and pushes them to GitHub Container Registry, tagged with the commit SHA and
   `latest`.
2. Over SSH: copies `docker-compose.yml`, pulls the new images, runs the migrator, restarts the stack, prunes old images.
3. Polls `https://tutumuch.kids/api/health` until it returns 200.

Secrets, in the repository's `production` environment (not set yet):

| Secret           | Value                                                     |
| ---------------- | --------------------------------------------------------- |
| `DEPLOY_HOST`    | Public IP of the VM                                       |
| `DEPLOY_SSH_KEY` | Private key whose public half is in `azureuser`'s `authorized_keys` |
| `SITE_HOST`      | `tutumuch.kids`                                           |

## Operating the server

```bash
ssh azureuser@<vm-ip>
cd /opt/tutumuch
docker compose ps                 # status
docker compose logs -f api        # or worker, web
docker compose run --rm migrator  # apply migrations by hand
docker compose up -d              # restart with the tags in .env
```

Photos live in the `tutumuch_photos` Docker volume. Back it up with
`docker run --rm -v tutumuch_photos:/data -v "$PWD":/backup alpine tar czf /backup/photos.tgz -C /data .`.

## Building from scratch

1. Create a resource group, a Basic Azure SQL server and database, and an Ubuntu VM with a static
   public IP, using `deploy/cloud-init.yaml` as custom data. Open ports 22, 80 and 443.
2. Add the VM's public IP to the SQL server firewall.
3. On the VM, create `/opt/tutumuch/.env` from `deploy/.env.example` with real values.
4. Point `tutumuch.kids` and `www.tutumuch.kids` at the VM's IP.
5. Set the three GitHub secrets and run the Deploy workflow by hand. To deploy on every push,
   add `push: branches: [main]` back to its `on:` block.
