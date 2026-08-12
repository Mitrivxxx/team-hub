## Purpose
- Provide observability stack (Grafana, Loki, Tempo, Prometheus, OTel Collector) for Team Hub.

## Source of truth
- `infrastructure/monitoring/docker-compose.monitoring.yml`
- `infrastructure/monitoring/otel-collector-config.yaml`
- `infrastructure/monitoring/tempo.yaml`
- `infrastructure/monitoring/prometheus.yml` (staging / full Compose stack)
- `infrastructure/monitoring/prometheus.dev.yml` (Aspire companion scrapes via `host.docker.internal`)
- `infrastructure/monitoring/grafana/provisioning/datasources/*.yml`
- `infrastructure/monitoring/.env.example`
- `infrastructure/monitoring/.nginx-edge-logs/` (Aspire + compose-dev shared edge access logs; gitignored except `.gitkeep`)
- Root `.env.dev.example` / `.env.staging.example`
- Root `docker-compose.dev.yml` (Aspire companion overrides; merged by `scripts/compose-dev.sh`)
- Root `docker-compose.yml` (includes monitoring for staging)

## Why compose fragment, not Dockerfile
- Grafana, Loki, Tempo, Prometheus, and OTel Collector run from official images with no custom build.
- Use a Dockerfile only when a custom config (e.g. provisioning, datasources, dashboards) is required.

## Env files
- Root `.env.dev` / `.env.staging` supply Grafana credentials, ports, and container names.
- `.env.example` — template for standalone run.
- Do not commit real Grafana passwords; use `CHANGE_ME` placeholders in examples only.

## Ports
- Grafana: `3000` (host) -> `3000` (container)
- Loki: `3100` (host) -> `3100` (container)
- Tempo: `3200` (host) -> `3200` (container)
- Prometheus: `9090` (host) -> `9090` (container)
- OTel Collector OTLP gRPC: `4317` (host) -> `4317` (container)
- OTel Collector OTLP HTTP: `4318` (host) -> `4318` (container)
- Nginx Prometheus exporter: `9113` (host) -> `9113` (container)
  - Staging: scrapes `gw-nginx:8081/nginx_status` (Docker DNS)
  - Aspire companion: scrapes `host.docker.internal:8081/nginx_status` (AppHost publishes stub_status)

## Telemetry ingestion
- **Logs**: services push OTLP logs (Serilog `OpenTelemetry` sink) -> OTel Collector -> Loki native `/otlp` endpoint.
- **Edge logs (staging)**: `gw-nginx` writes JSON access lines to volume `nginx_edge_logs`; OTel Collector `filelog/nginx` tails that file -> Loki (`service.name` / `service_name` = `gw-nginx`). Keep body as raw JSON for LogQL `| json`.
- **Edge logs (Aspire companion)**: AppHost and `mon-otel` share bind mount `infrastructure/monitoring/.nginx-edge-logs`.
- **Traces**: services push OTLP traces -> OTel Collector -> Tempo.
- **Metrics (staging)**: Prometheus (`prometheus.yml`) scrapes Docker DNS: `srv-auth`, `srv-organization`, `srv-notification`, `srv-bff`, `gw-api` on `:8080/metrics`, plus `mon-nginx-exporter:9113`.
- **Metrics (Aspire companion)**: Prometheus (`prometheus.dev.yml`) scrapes `host.docker.internal` on pinned host ports `5000`–`5004`, plus nginx exporter.
- Grafana datasources provisioned: Loki (default), Prometheus, Tempo (with `tracesToLogsV2` -> Loki).
- Query cross-service logs by `CorrelationId` or `TraceId` JSON field:
  - `{service_name="gw-nginx"} | json | CorrelationId="<id>"`
  - `{service_name="team-hub-gateway"} | json | CorrelationId="<trace-id>"`
  - `{service_name="team-hub-auth"} | json | TraceId="<trace-id>"`

## Correlation ID workflow
- `CorrelationId` in app logs equals OpenTelemetry `TraceId` (32 hex chars) when tracing is active.
- Edge (`gw-nginx`): prefer inbound `X-Correlation-ID`, else nginx `$request_id`; forward upstream and echo on the response.
- `X-Correlation-ID` response header echoes the id for browser/API debugging.
- W3C `traceparent` propagates trace context between gateway and auth automatically.

## First telemetry in Grafana
- Aspire + monitoring: `./scripts/compose-dev.sh up -d` then generate traffic via Aspire endpoints.
- Staging: `./scripts/compose-staging.sh up --build -d` then `curl -k https://localhost:8080/health` or `curl http://localhost:5001/health`.
- Open Grafana `http://localhost:3000` (user/password from `.env.dev` / `.env.staging`).
- Explore:
  - **Loki** — `{service_name="gw-nginx"}` or `{service_name="team-hub-auth"}`
  - **Tempo** — search by TraceId from `X-Correlation-ID` header (app services)
  - **Prometheus** — `http_server_request_duration_seconds_count`, `nginx_connections_active`

## Persistence
- Grafana data stored in volume `grafana_data`.
- Loki data stored in volume `loki_data`.
- Tempo data stored in volume `tempo_data`.
- Prometheus data stored in volume `prometheus_data`.
- Nginx edge access logs (staging): volume `nginx_edge_logs` (`gw-nginx` write, `mon-otel` read).
- Nginx edge access logs (Aspire companion): host dir `.nginx-edge-logs`.

## Do
- Prefer `./scripts/compose-dev.sh` for Aspire companion (merges monitoring base + `docker-compose.dev.yml` overrides). Compose `include` cannot override imported services.
- Keep this stack optional and infrastructure-only for Aspire.
- Keep secrets (Grafana admin password) in local env files, not committed.

## Don't
- Do not add business logic here.
- Do not change service ports or routes outside the monitoring compose fragment.
- Do not reintroduce Promtail; logs are OTLP-only for apps, plus Collector `filelog` for nginx edge access logs only.
- Do not expect staging `prometheus.yml` targets (`srv-auth:8080`) to resolve inside the Aspire companion project — use `prometheus.dev.yml`.
