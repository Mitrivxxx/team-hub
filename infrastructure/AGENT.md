## Purpose
- Edge reverse proxy and shared infrastructure config for Team Hub.

## Source of truth
- `infrastructure/nginx/nginx.conf`
- `infrastructure/nginx/snippets/proxy_params.conf`
- `infrastructure/nginx/snippets/api_locations.conf`
- `infrastructure/nginx/Dockerfile`
- `infrastructure/nginx/docker-entrypoint.sh`
- `infrastructure/redis/docker-compose.redis.yml`
- `infrastructure/redis/.env.example`
- `infrastructure/azurite/docker-compose.azurite.yml`
- `infrastructure/azurite/.env.example`
- `infrastructure/monitoring/docker-compose.monitoring.yml`
- `infrastructure/monitoring/otel-collector-config.yaml`
- `infrastructure/monitoring/tempo.yaml`
- `infrastructure/monitoring/prometheus.yml`
- `infrastructure/monitoring/.env.example`
- `building-blocks/TeamHub.Redis/*`
- `building-blocks/TeamHub.BlobStorage/*`
- `building-blocks/TeamHub.Observability/*`
- `docker-compose.yml`

## Do
- Terminate HTTPS on container port `443` (host `8080`).
- Serve internal HTTP on port `80` for docker network traffic (`ui-web` -> `http://gw-nginx:80`).
- Proxy `/api/*` and `/health` to upstream `gw-api` (see `upstream.conf` generated at container start).
- Keep shared proxy headers/timeouts in `snippets/proxy_params.conf`; keep route + rate-limit locations in `snippets/api_locations.conf` (included by both `:80` and `:443` servers).
- When `GATEWAY_UPSTREAM` is set (Aspire dev), entrypoint writes `upstream gw-api { server $GATEWAY_UPSTREAM; }`.
- When `GATEWAY_UPSTREAM` is unset (docker compose prod), default upstream is `gw-api:8080`.
- Forward proxy headers: `Host`, `X-Real-IP`, `X-Forwarded-For`, `X-Forwarded-Proto`, `X-Forwarded-Host`.
- Keep `client_max_body_size 10m`, HTTP/1.1 keep-alive to upstream, and proxy timeouts aligned with gateway.
- On HTTPS listener (`443`): enable gzip, security headers, and rate limiting on `/api/`.
- Mount TLS certs from `./certs` to `/etc/nginx/certs`.
- Treat flow as: frontend -> infrastructure nginx (`gw-nginx`) -> gateway (`gw-api`, HTTP) -> auth (`srv-auth`).
- Run one shared Redis instance via `infrastructure/redis/docker-compose.redis.yml` (included by root compose files; service `cache-redis`).
- Use `building-blocks/TeamHub.Redis` (`TeamHub.Redis`) for service-side Redis connection bootstrap.
- Use `building-blocks/TeamHub.BlobStorage` (`TeamHub.BlobStorage`) for dev Azurite/blob client bootstrap in organization service.
- Use `building-blocks/TeamHub.Observability` (`TeamHub.Observability`) for OpenTelemetry and Serilog bootstrap.

## Don't
- Do not add business logic or auth validation in nginx.
- Do not expose auth service directly from this container.
- Do not terminate TLS on gateway; TLS ends at nginx (and frontend UI).
- Do not add per-service Redis containers or connection bootstrap outside `building-blocks/TeamHub.Redis`.

## Listeners
- **Port 80 (internal):** HTTP proxy to gateway for `ui-web` container.
- **Port 443 (public edge):** HTTPS with gzip, security headers, rate limiting.

## Redis
- Dev container: `cache-redis-dev` (`docker-compose.dev.yml`)
- Prod container: `cache-redis-prod` (`docker-compose.yml`)
- Host port: `6379`
- Docker network: `cache-redis:6379`

## Azurite (dev only)
- Dev container: `blob-storage-dev` (`docker-compose.dev.yml` only; service `blob-storage`)
- Host blob port: `10000`
- Docker network: `blob-storage:10000`
- Browser SAS URLs: `BlobStorage__PublicBlobEndpoint=http://127.0.0.1:10000/devstoreaccount1`
