## Purpose
- Edge reverse proxy and shared infrastructure config for Team Hub.

## Source of truth
- `infrastructure/nginx/nginx.conf`
- `infrastructure/nginx/snippets/proxy_params.conf`
- `infrastructure/nginx/snippets/api_locations.conf`
- `infrastructure/nginx/snippets/error_pages.conf`
- `infrastructure/nginx/Dockerfile`
- `infrastructure/nginx/docker-entrypoint.sh`
- `infrastructure/postgres/docker-compose.postgres.yml`
- `infrastructure/redis/docker-compose.redis.yml`
- `infrastructure/redis/.env.example`
- `infrastructure/azurite/docker-compose.azurite.yml`
- `infrastructure/azurite/.env.example`
- `infrastructure/kafka/docker-compose.kafka.yml`
- `infrastructure/monitoring/docker-compose.monitoring.yml`
- `infrastructure/monitoring/.env.example`
- `building-blocks/TeamHub.Redis/*`
- `building-blocks/TeamHub.BlobStorage/*`
- `building-blocks/TeamHub.Observability/*`
- `docker-compose.yml` / `docker-compose.dev.yml` / `docker-compose.staging.yml`
- `.env.dev.example` / `.env.staging.example`

## Do
- Terminate HTTPS on container port `443` (host `8080`).
- Serve internal HTTP on port `80` for docker network traffic (`ui-web` -> `http://gw-nginx:80`).
- Proxy `/api/*` and `/health` to upstream `gw-api` (see `upstream.conf` generated at container start).
- Keep shared proxy headers/timeouts in `snippets/proxy_params.conf`; keep route + rate-limit locations in `snippets/api_locations.conf` (included by both `:80` and `:443` servers).
- Edge MVP observability/errors:
  - JSON access log + error log under `/var/log/nginx-edge` (volume `nginx_edge_logs`; OTel Collector `filelog/nginx` -> Loki).
  - `X-Correlation-ID`: use inbound header or nginx `$request_id`; forward to upstream. Pass through upstream `X-Correlation-ID` (OTEL TraceId) on success responses; edge error pages still echo `$corr_id`.
  - Access log JSON: `CorrelationId` (inbound UUID / `$request_id`), `TraceId` (`$upstream_http_x_correlation_id`), `SessionId` (`$http_x_session_id`).
  - `limit_req_status 429` with JSON body + `Retry-After` (`snippets/error_pages.conf`); also JSON for edge `413` / `502` / `504` (no `proxy_intercept_errors` — backend ProblemDetails stay intact).
  - Internal `stub_status` on `:8081` scraped by `mon-nginx-exporter` (Prometheus job `gw-nginx`).
- When `GATEWAY_UPSTREAM` is set (Aspire dev), entrypoint writes `upstream gw-api { server $GATEWAY_UPSTREAM; }`.
- When `GATEWAY_UPSTREAM` is unset (Compose staging), default upstream is `gw-api:8080`.
- Forward proxy headers: `Host`, `X-Real-IP`, `X-Forwarded-For`, `X-Forwarded-Proto`, `X-Forwarded-Host`, `X-Correlation-ID`, `X-Session-ID`.
- Keep `client_max_body_size 10m`, HTTP/1.1 keep-alive to upstream, and proxy timeouts aligned with gateway.
- On HTTPS listener (`443`): enable gzip, security headers, and rate limiting on `/api/`.
- Mount TLS certs from `./certs` to `/etc/nginx/certs`.
- Treat flow as: frontend -> infrastructure nginx (`gw-nginx`) -> gateway (`gw-api`, HTTP) -> auth (`srv-auth`).
- Run shared Redis via `infrastructure/redis/docker-compose.redis.yml` (included by Compose base; service `cache-redis`).
- Use `building-blocks/TeamHub.Redis` for service-side Redis connection bootstrap.
- Use `building-blocks/TeamHub.BlobStorage` for Azurite/blob client bootstrap in organization service.
- Use `building-blocks/TeamHub.Observability` for OpenTelemetry and Serilog bootstrap.
- Root Compose env: `.env.dev` (infra + monitoring companion) and `.env.staging` (full stack); never commit secrets.

## Don't
- Do not add business logic or auth validation in nginx.
- Do not expose auth service directly from this container.
- Do not terminate TLS on gateway; TLS ends at nginx (and frontend UI).
- Do not add per-service Redis containers or connection bootstrap outside `building-blocks/TeamHub.Redis`.
- Do not publish `:8081` stub_status to the host.
- Do not commit Grafana passwords or JWT keys in compose fragment env files.

## Listeners
- **Port 80 (internal):** HTTP proxy to gateway for `ui-web` container.
- **Port 443 (public edge):** HTTPS with gzip, security headers, rate limiting.
- **Port 8081 (internal):** `stub_status` for Prometheus exporter only.

## Redis
- Compose-dev: `cache-redis-dev` on `127.0.0.1:6379` (network `team-hub-dev`)
- Staging: `cache-redis-staging`
- Docker network DNS: `cache-redis:6379`

## Azurite
- Compose-dev: `blob-storage-dev` on `127.0.0.1:10000` (volume `azurite_data_dev`)
- Staging: `blob-storage-staging` (volume `AZURITE_VOLUME_NAME`)
- Host blob port: `10000`
- Docker network: `blob-storage:10000`
- Browser SAS URLs: `BlobStorage__PublicBlobEndpoint=http://127.0.0.1:10000/devstoreaccount1`
