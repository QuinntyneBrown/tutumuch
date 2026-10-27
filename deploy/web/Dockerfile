# Builds the Angular workspace twice from one library build: the marketing-only build (/ is the marketing page) for
# tutumuch.kids and the full application for uat.tutumuch.kids, and serves both with Caddy, which also terminates TLS and
# proxies /api and /photos to the API container. Build context: the repository root.
FROM node:22-alpine AS build
WORKDIR /src
COPY src/frontend/package.json src/frontend/package-lock.json ./
RUN npm ci
COPY src/frontend/ .
RUN npm run build:web

FROM caddy:2-alpine
COPY deploy/web/Caddyfile /etc/caddy/Caddyfile
COPY --from=build /src/dist/marketing/browser /srv/marketing
COPY --from=build /src/dist/app/browser /srv/app
