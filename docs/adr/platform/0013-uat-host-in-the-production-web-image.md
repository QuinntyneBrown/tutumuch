# ADR-0013: The Full Application on a UAT Host Served by the Production Web Image

**Date:** 2026-10-26
**Category:** platform
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

Production (`https://tutumuch.kids`) serves the marketing-only build: `/` is the marketing
page and the rest of the application is not reachable there ([ADR platform/0011](0011-marketing-feature-library.md)).
The owner wants the full `tutumuch` application reachable on Azure at
`https://uat.tutumuch.kids` for user acceptance testing, updated on every commit to
`main`. ADR platform/0012 deliberately has no staging environment, no second VM and no
managed hosting, and the budget has not changed.

## Decision

- **One image, two builds.** `deploy/web/Dockerfile` runs `npm run build:web`, which builds
  the libraries once, then the `marketing` configuration to `dist/marketing` and the
  `production` configuration to `dist/app`, and copies them to `/srv/marketing` and
  `/srv/app`. The `tutumuch-web` image is still built, tagged and pushed once per deploy.
- **Caddy chooses by host.** `deploy/web/Caddyfile` serves `/srv/app` for
  `uat.tutumuch.kids` (`UAT_HOST`) and `/srv/marketing` for every other host, through one
  `(spa)` snippet so both keep the `Cache-Control: no-cache` rule for `index.html` and SPA
  routes. `/api/*` and `/photos/*` of both hosts go to the same `api` container.
- **No second environment.** UAT runs on the production API, worker, photo volume and
  Azure SQL database. Anything done on the UAT host is done in production data.
- **The workflow owns `SITE_ADDRESS`.** The deploy script rewrites `SITE_ADDRESS` in
  `/opt/tutumuch/.env` as `<SITE_HOST>, www.<SITE_HOST>, uat.<SITE_HOST>` next to
  `IMAGE_TAG`, so Caddy obtains a Let's Encrypt certificate for each host and adding a host
  needs only a DNS `A` record at Namecheap, not a change on the VM.
- **Smoke test.** After `/api/health`, the run polls `https://uat.<SITE_HOST>/` for up to
  two minutes and fails without a 200.

## Options Considered

### Option 1: Second host on the same stack (chosen)
- **Pros:** No new Azure resource, no new secret, no new workflow; one deploy keeps both
  hosts on the same commit; a certificate for free.
- **Cons:** UAT shares production data; a bad full build takes the marketing page down with
  it (both are in one image).

### Option 2: Azure Static Web Apps for the full build
- **Pros:** Managed hosting and TLS, separate from the VM.
- **Cons:** The app calls `/api` on its own origin, so it would need a CORS and cookie setup
  across origins or a proxy back to the VM, a second workflow and Azure credentials in
  GitHub; diverges from ADR platform/0012.

### Option 3: A second VM or a second compose stack as a real staging environment
- **Pros:** Isolated data.
- **Cons:** Doubles the monthly cost and the moving parts for a product with a few users;
  ruled out by ADR platform/0012.

## Consequences

### Positive
- Every green push to `main` is visible as the full application at `https://uat.tutumuch.kids`
  within the same run that updates `https://tutumuch.kids`.
- Monthly cost is unchanged.

### Negative
- UAT is not a sandbox: accounts, listings and orders created there are real. Turning the
  `uat` host into the production application later is a Caddyfile change, not a migration.
- The web image build is slower (two application builds), mostly recovered by the Buildx cache.

### Risks
- The first deploy after the DNS record may fail its UAT smoke test if Let's Encrypt is slow
  to issue the new certificate. Mitigation: re-run the Deploy workflow; Caddy keeps retrying.

## References

- [ADR platform/0011](0011-marketing-feature-library.md)
- [ADR platform/0012](0012-low-cost-single-vm-continuous-deployment.md)
- [deploy-to-production design](../../detailed-designs/platform/deploy-to-production/README.md)
- [docs/deployment.md](../../deployment.md)
