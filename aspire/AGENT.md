## Purpose
- Local development orchestration for Team Hub apps via .NET Aspire AppHost.
- Stateful infra (Postgres, Redis, Kafka, Azurite) + monitoring run in Compose project `team-hub-dev` (network `team-hub-dev`).

## Source of truth
- `aspire/TeamHub.AppHost/Program.cs`
- `aspire/TeamHub.AppHost/DevInfra.cs`
- `aspire/TeamHub.AppHost/TeamHub.AppHost.csproj`
- `aspire/TeamHub.AppHost/appsettings.Development.json` (`Aspire:Jwt:*`, `Aspire:DevInfra:*`)
- `aspire/TeamHub.ServiceDefaults/Extensions.cs`
- `docker-compose.dev.yml` / `.env.dev.example` / `scripts/compose-dev.sh` / `scripts/dev-up.sh` / `scripts/dev-seed.sh`
- `TeamHub.sln`

## Do
- **Preferred daily start:**
  1. `cp .env.dev.example .env.dev` (once)
  2. `./scripts/dev-up.sh` — compose-dev `--wait`, then AppHost apps
  - or two steps: `./scripts/compose-dev.sh up -d --wait` then `cd aspire/TeamHub.AppHost && dotnet run`
- AppHost fail-fast if `127.0.0.1:5433` / `6379` / `9092` / `10000` are down — start compose-dev first.
- Ctrl+C stops AppHost only; compose-dev infra stays up (`restart: unless-stopped`).
- Demo seed (Development only; orchestrates per-service `--seed` runners — not `TeamHub.DemoSeed`):
  - `./scripts/dev-seed.sh` or `cd aspire/TeamHub.AppHost && dotnet run -- --seed`
  - Order: `seed-auth` → `seed-auth-api` (gRPC `:15101`, HTTP `:15001`) → `seed-organization` (1 org) → `seed-notification`
  - Uses compose-dev Postgres/Redis/Kafka (`auth_db` / `organization_db` / `notification_db`). No gateway/nginx/web/bff/Azurite in seed mode.
  - Login: `JanWilk123` / `janwilk123` (Owner of `demo-org-1` / Wilk Technologies).
  - Re-seed: `docker volume rm postgres_data_dev` (or name from `.env.dev`), then `./scripts/dev-seed.sh` again.
- Prerequisites: .NET 10 SDK, Aspire 13 (`Aspire.AppHost.Sdk`), Docker, Node.js/npm, TLS certs in `certs/` (`./scripts/setup-certs.sh`).
- AppHost runs apps only: `srv-auth`, `srv-organization`, `srv-notification`, `srv-chat`, `srv-bff`, `gw-api`, `gw-nginx`, `ui-web`.
- Connection strings from `Aspire:DevInfra` via helpers in `DevInfra.cs` (`127.0.0.1` ports matching compose-dev).
- Keep staging/pre-prod on Compose (`docker-compose.yml` + `docker-compose.staging.yml`) — Aspire is dev-only.
- JWT + DevInfra live in `aspire/TeamHub.AppHost/appsettings.Development.json` (keep Postgres password in sync with `.env.dev`).
- Gateway destinations: `http://srv-auth`, `http://srv-organization`, `http://srv-notification`, `http://srv-chat`, `http://srv-bff`.
- HTTP ports: auth `5001`, organization `5002`, notification `5004`, chat `5005`, bff `5003`, gateway `5000`.
- gRPC: auth `5101`, organization `5102`.
- Nginx: `GATEWAY_UPSTREAM=host.docker.internal:5000`; stub_status `8081`; edge logs → `infrastructure/monitoring/.nginx-edge-logs`.
- OTLP → `http://127.0.0.1:4317` (compose-dev `mon-otel`).

## Don't
- Do not use AppHost for production deployment.
- Do not use AppHost `--seed` for Production demo data.
- Do not run AppHost and staging Compose on the same host ports at once.
- Do not start AppHost without compose-dev infra (fail-fast will abort).
- Do not commit production JWT secrets to AppHost config.
- Do not put orchestrator/seed I/O into `building-blocks/TeamHub.DemoSeed`.

## Ports (local dev)
| Resource | Host URL |
|----------|----------|
| Aspire Dashboard | printed in console on start |
| Frontend (`ui-web`) | `https://localhost:4200` |
| Infrastructure nginx (`gw-nginx`) | `https://localhost:8080` |
| Gateway (`gw-api`) | `http://localhost:5000` |
| BFF (`srv-bff`) | `http://localhost:5003` |
| Auth (`srv-auth`) | REST `http://localhost:5001` / gRPC `http://localhost:5101` |
| Organization (`srv-organization`) | REST `http://localhost:5002` / gRPC `http://localhost:5102` |
| Notification (`srv-notification`) | `http://localhost:5004` |
| Chat (`srv-chat`) | `http://localhost:5005` |
| Postgres (`db-postgres-dev`) | `127.0.0.1:5433` (`auth_db`, `organization_db`, `notification_db`, `chat_db`) |
| PgAdmin (`db-pgadmin-dev`) | `http://127.0.0.1:5050` (login `PGADMIN_DEFAULT_*` from `.env.dev`) |
| Redis (`cache-redis-dev`) | `127.0.0.1:6379` |
| Kafka (`msg-kafka-dev`) | `127.0.0.1:9092` |
| Azurite (`blob-storage-dev`) | `127.0.0.1:10000` |
| Nginx stub_status | `http://localhost:8081/nginx_status` |
| OTLP (compose-dev) | `http://127.0.0.1:4317` |

## Request flow
- Frontend `/api/auth/*` -> nginx `8080` -> gateway `5000` -> auth -> postgres + redis.
- Frontend `/api/organizations/*` -> nginx `8080` -> gateway `5000` -> organization -> postgres + Azurite + Kafka.
- Frontend `/api/notifications/*` -> nginx `8080` -> gateway `5000` -> notification -> postgres + Kafka.
- Frontend `/api/chat/*` -> nginx `8080` -> gateway `5000` -> chat -> postgres.
- Async: organization member add -> Kafka `organization.events` -> notification consumer -> `notification_db`.

## Troubleshooting
- Port conflict: `./scripts/compose-staging.sh down` before compose-dev / AppHost.
- Fail-fast "Compose dev infra is not reachable": `./scripts/compose-dev.sh up -d --wait`.
- Nginx 502 on WSL2: `host.docker.internal` (`--add-host=host.docker.internal:host-gateway` in AppHost).
- Kafka advertised host for Aspire apps must be `127.0.0.1` (`.env.dev` `KAFKA_ADVERTISED_HOST`), not `msg-kafka`.
- Frontend API errors: `frontend/team-hub-web/proxy.conf.json` → `https://localhost:8080`.
