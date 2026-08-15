## Purpose
- Local development orchestration for the full Team Hub stack via .NET Aspire AppHost.

## Source of truth
- `aspire/TeamHub.AppHost/Program.cs`
- `aspire/TeamHub.AppHost/TeamHub.AppHost.csproj`
- `aspire/TeamHub.AppHost/appsettings.Development.json`
- `aspire/TeamHub.ServiceDefaults/Extensions.cs`
- `TeamHub.sln`

## Do
- Start the full local stack from AppHost:
  - `cd aspire/TeamHub.AppHost && dotnet run`
- Demo seed (Development only; orchestrates existing per-service `--seed` runners — does not live in `TeamHub.DemoSeed`):
  - `cd aspire/TeamHub.AppHost && dotnet run -- --seed`
  - or `dotnet run --launch-profile seed`
  - Order: `seed-auth` → `seed-auth-api` (gRPC `:15101`, HTTP `:15001` — not the live stack `:5001`/`:5101`) → `seed-organization` (1 org, JanWilk Owner) → `seed-notification` (demo inbox for JanWilk).
  - Infra for seed mode: `db-postgres` (`auth_db` / `organization_db` / `notification_db`, `WithDataVolume`), `cache-redis`, `msg-kafka`. No gateway/nginx/web/bff/blobs.
  - Idempotent (same as service seeders). Login: `JanWilk123` / `janwilk123` (Owner of `demo-org-1` / Wilk Technologies).
  - AppHost waits for `seed-notification` Finished then exits (no Ctrl+C). Re-seed: remove Aspire Postgres named volume, then run `--seed` again.
- Prerequisites: .NET 10 SDK (`dotnet --version` should report 10.x), Aspire 13 (`Aspire.AppHost.Sdk` via NuGet — no Aspire workload), Docker, Node.js/npm, TLS certs in `certs/` (`./scripts/setup-certs.sh`).
  - If `dotnet --list-sdks` only shows 8.x, install SDK 10 (`https://aka.ms/dotnet/download` or `curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0`) and put `$HOME/.dotnet` first on `PATH` (`export DOTNET_ROOT=$HOME/.dotnet; export PATH=$HOME/.dotnet:$PATH`). Repo `global.json` pins SDK 10.0.x.
- AppHost runs: Postgres (Aspire resource `db-postgres` + `auth-db` / `organization-db` / `notification-db`, `WithDataVolume`), Redis (`cache-redis`), Kafka (`msg-kafka` host `:9092`), Azurite blob storage (`blob-storage` blob host `:10000` + `blobs`, `WithDataVolume`), `srv-auth`, `srv-organization`, `srv-notification`, `srv-bff`, `gw-api`, infrastructure nginx (`gw-nginx`), Angular (`ui-web` / `npm start`).
- Organization waits for Kafka + Azurite; notification waits for Kafka (`WaitFor`).
- Aspire resource names: only ASCII letters, digits, hyphens (no underscores).
- Keep staging/pre-prod on Compose (`docker-compose.yml` + `docker-compose.staging.yml`) — Aspire is dev-only.
- JWT dev secrets live in `aspire/TeamHub.AppHost/appsettings.Development.json` (`Aspire:Jwt:*`).
- Gateway destinations under Aspire: auth `http://srv-auth`, team `http://srv-organization`, notification `http://srv-notification`, bff `http://srv-bff` (YARP service discovery).
- HTTP ports pinned for companion Prometheus scrapes: auth `5001`, organization `5002`, notification `5004`, bff `5003`, gateway `5000`.
- Internal gRPC ports under Aspire: auth `5101`, organization `5102`; BFF `Grpc__Auth`/`Grpc__Organization` point at `127.0.0.1:5101/5102`.
- Kafka bootstrap is injected as `Kafka__BootstrapServers` on organization (producer) and notification (consumer).
- Aspire Dashboard is one UI for the whole AppHost run (all resources at once: logs, endpoints, traces).
- Nginx under Aspire uses `GATEWAY_UPSTREAM=host.docker.internal:5000` (gateway runs as host process); publishes stub_status on host `8081`; edge logs bind-mounted to `infrastructure/monitoring/.nginx-edge-logs`.
- AppHost sets `OTEL_EXPORTER_OTLP_ENDPOINT` / `Observability__OtlpEndpoint` to `http://127.0.0.1:4317` (compose-dev collector).
- Optional monitoring beside Aspire: `./scripts/compose-dev.sh up -d` (Grafana/Loki/Tempo/Prometheus/OTel; scrapes host ports via `host.docker.internal`).

## Don't
- Do not use AppHost for production deployment.
- Do not use AppHost `--seed` for Production demo data.
- Do not run AppHost and staging Compose on the same host ports at once.
- Do not commit production JWT secrets to AppHost config.
- Do not put orchestrator/seed I/O into `building-blocks/TeamHub.DemoSeed` (identity factory only).

## Ports (Aspire local dev)
| Resource | Host URL |
|----------|----------|
| Aspire Dashboard | printed in console on start (one UI for all resources) |
| Frontend (`ui-web`) | `https://localhost:4200` |
| Infrastructure nginx (`gw-nginx`) | `https://localhost:8080` |
| Gateway (`gw-api`) | `http://localhost:5000` |
| BFF (`srv-bff`) | `http://localhost:5003` |
| Auth (`srv-auth`) | REST `http://localhost:5001` / gRPC `http://localhost:5101` |
| Organization (`srv-organization`) | REST `http://localhost:5002` / gRPC `http://localhost:5102` |
| Notification (`srv-notification`) | `http://localhost:5004` |
| Postgres (`db-postgres`) | dynamic (see dashboard; databases `auth_db`, `organization_db`, `notification_db`) |
| Redis (`cache-redis`) | dynamic (see dashboard) |
| Kafka (`msg-kafka`) | `localhost:9092` (pinned host port) |
| Azurite (`blob-storage`) | blob `localhost:10000` (pinned; queue/table still dynamic) |
| Nginx stub_status | `http://localhost:8081/nginx_status` (companion Prometheus exporter) |
| OTLP (compose-dev) | `http://127.0.0.1:4317` |

## Request flow
- Frontend `/api/auth/*` -> nginx `8080` -> gateway `5000` -> auth (service discovery) -> postgres + redis.
- Frontend `/api/organizations/*` -> nginx `8080` -> gateway `5000` -> team (service discovery) -> postgres + Azurite (avatars).
- Frontend `/api/notifications/*` -> nginx `8080` -> gateway `5000` -> notification (service discovery) -> postgres.
- Async: organization member add -> Kafka `organization.events` -> notification consumer -> `notification_db`.

## Troubleshooting
- Port conflict: stop staging Compose and any running `dotnet`/`npm` processes before `dotnet run` AppHost.
  - Aspire brings its own Postgres/Redis/Kafka/Azurite — do not run staging Compose at the same time.
  - Example: `./scripts/compose-staging.sh down`
  - Monitoring-only (`compose-dev`) is safe alongside Aspire if ports do not collide (Grafana `3000`, OTLP `4317`/`4318`, etc.).
- Nginx 502 to gateway on WSL2: ensure Docker supports `host.docker.internal` (`--add-host=host.docker.internal:host-gateway` is set in AppHost).
- Auth Redis errors: AppHost maps `Redis__ConnectionString` from the Redis resource reference.
- DB connection to `localhost:5433` under Aspire: local service `.env` was overwriting Aspire injection — services use `Env.NoClobber()`; restart AppHost after pull. Aspire Postgres port is dynamic (dashboard), not `5433`.
- Kafka `brokers are down` / Azurite `Connection refused :10000`: AppHost pins Kafka `9092` and Azurite blob `10000`, injects `BlobStorage__ConnectionString` + `WaitFor` kafka/blobs; `TeamHub.BlobStorage` prefers `ConnectionStrings:blobs`. Restart AppHost after pull; do not run staging Compose on the same ports.
- Frontend API errors: confirm `frontend/team-hub-web/proxy.conf.json` still targets `https://localhost:8080`.
