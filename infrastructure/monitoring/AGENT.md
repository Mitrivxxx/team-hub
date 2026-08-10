## Purpose
- Provide local observability stack (Grafana, Loki, Tempo, Prometheus, OTel Collector) for Team Hub.

## Source of truth
- `infrastructure/monitoring/docker-compose.monitoring.yml`
- `infrastructure/monitoring/otel-collector-config.yaml`
- `infrastructure/monitoring/tempo.yaml`
- `infrastructure/monitoring/prometheus.yml`
- `infrastructure/monitoring/grafana/provisioning/datasources/*.yml`
- `infrastructure/monitoring/.env.example`
- `infrastructure/monitoring/compose.dev.env`
- `infrastructure/monitoring/compose.prod.env`
- Root `docker-compose.yml` and `docker-compose.dev.yml` (include monitoring services)

## Why compose fragment, not Dockerfile
- Grafana, Loki, Tempo, Prometheus, and OTel Collector run from official images with no custom build.
- Use a Dockerfile only when a custom config (e.g. provisioning, datasources, dashboards) is required.

## Env files
- `compose.dev.env` / `compose.prod.env` — committed runtime env for root compose `include`.
- `.env.example` — template for standalone run (`docker compose -f infrastructure/monitoring/docker-compose.monitoring.yml --env-file infrastructure/monitoring/.env.example up -d`).
- Do not commit `infrastructure/monitoring/.env` if you introduce it later (treat as local secret).

## Ports
- Grafana: `3000` (host) -> `3000` (container)
- Loki: `3100` (host) -> `3100` (container)
- Tempo: `3200` (host) -> `3200` (container)
- Prometheus: `9090` (host) -> `9090` (container)
- OTel Collector OTLP gRPC: `4317` (host) -> `4317` (container)
- OTel Collector OTLP HTTP: `4318` (host) -> `4318` (container)
- Nginx Prometheus exporter: `9113` (host) -> `9113` (container); scrapes `gw-nginx:8081/nginx_status`

## Telemetry ingestion
- **Logs**: services push OTLP logs (Serilog `OpenTelemetry` sink) -> OTel Collector -> Loki native `/otlp` endpoint.
- **Edge logs**: `gw-nginx` writes JSON access lines to volume `nginx_edge_logs` (`/var/log/nginx-edge/access.log`); OTel Collector `filelog/nginx` tails that file -> Loki (`service.name` / `service_name` = `gw-nginx`). Keep body as raw JSON for LogQL `| json`.
- **Traces**: services push OTLP traces -> OTel Collector -> Tempo.
- **Metrics**: Prometheus scrapes `srv-auth:8080/metrics`, `gw-api:8080/metrics`, and `mon-nginx-exporter:9113` (nginx `stub_status` via `gw-nginx:8081`).
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
- Start monitoring + app services: `docker compose up -d mon-otel mon-loki mon-tempo mon-prometheus mon-grafana mon-nginx-exporter srv-auth gw-api gw-nginx`
- Generate traffic: `curl -k https://localhost:8080/health` or `curl http://localhost:5001/health`
- Open Grafana `http://localhost:3000` (user/password from env file).
- Explore:
  - **Loki** — `{service_name="gw-nginx"}` or `{service_name="team-hub-auth"}`
  - **Tempo** — search by TraceId from `X-Correlation-ID` header (app services)
  - **Prometheus** — `http_server_request_duration_seconds_count`, `nginx_connections_active`

## Persistence
- Grafana data stored in volume `grafana_data`.
- Loki data stored in volume `loki_data`.
- Tempo data stored in volume `tempo_data`.
- Prometheus data stored in volume `prometheus_data`.
- Nginx edge access logs shared via volume `nginx_edge_logs` (`gw-nginx` write, `mon-otel` read).

## Do
- Keep this stack optional and infrastructure-only (no app routing changes).
- Keep secrets (Grafana admin password) in env files, not hardcoded in compose.

## Don't
- Do not add business logic here.
- Do not change service ports or routes outside the monitoring compose fragment.
- Do not reintroduce Promtail; logs are OTLP-only for apps, plus Collector `filelog` for nginx edge access logs only.
