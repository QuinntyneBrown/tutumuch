# Builds the marketing-only Angular build (/ is the marketing page) and serves it with Caddy, which also terminates TLS and proxies
# /api and /photos to the API container. Build context: the repository root.
FROM node:22-alpine AS build
WORKDIR /src
COPY src/frontend/package.json src/frontend/package-lock.json ./
RUN npm ci
COPY src/frontend/ .
RUN npm run build:marketing

FROM caddy:2-alpine
COPY deploy/web/Caddyfile /etc/caddy/Caddyfile
COPY --from=build /src/dist/tutumuch/browser /srv
