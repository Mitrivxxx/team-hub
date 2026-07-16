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
- Prerequisites: .NET 8 SDK, Aspire workload (`dotnet workload install aspire`), Docker, Node.js/npm.
- AppHost runs: Postgres (Aspire resource `auth-db`, database name `auth_db`), Redis, `team-hub-auth`, `team-hub-gateway`, infrastructure nginx, Angular (`npm start`).
- Aspire resource names: only ASCII letters, digits, hyphens (no underscores).
- Keep production/pre-prod on `docker-compose.yml` — Aspire is dev-only.
- JWT dev secrets live in `aspire/TeamHub.AppHost/appsettings.Development.json` (`Aspire:Jwt:*`).
- Gateway auth destination under Aspire: env override `http://team-hub-auth` (YARP service discovery).
- Aspire Dashboard is one UI for the whole AppHost run (all resources at once: logs, endpoints, traces).
- Nginx under Aspire uses `GATEWAY_UPSTREAM=host.docker.internal:5000` (gateway runs as host process).
- Manual dev without Aspire remains available via `docker-compose.dev.yml` + `dotnet run` / `npm start`.

## Don't
- Do not use AppHost for production deployment.
- Do not run AppHost and `docker compose` on the same host ports at once.
- Do not commit production JWT secrets to AppHost config.

## Ports (Aspire local dev)
| Resource | Host URL |
|----------|----------|
| Aspire Dashboard | printed in console on start (one UI for all resources) |
| Frontend (web) | `http://localhost:4200` |
| Infrastructure nginx (API edge) | `https://localhost:8080` |
| Gateway (`team-hub-gateway`) | `http://localhost:5000` |
| Auth (`team-hub-auth`) | dynamic (see dashboard) |
| Postgres | dynamic (see dashboard; database `auth_db`) |
| Redis | dynamic (see dashboard) |

## Request flow
- Frontend `/api/auth/*` -> nginx `8080` -> gateway `5000` -> auth (service discovery) -> postgres + redis.

## Troubleshooting
- Port conflict: stop `docker compose` and any running `dotnet`/`npm` processes before `dotnet run` AppHost.
  - Aspire brings its own Postgres/Redis — do not run `docker-compose.dev.yml` at the same time.
  - Example: `docker compose -f docker-compose.dev.yml down`
- Nginx 502 to gateway on WSL2: ensure Docker supports `host.docker.internal` (`--add-host=host.docker.internal:host-gateway` is set in AppHost).
- Auth Redis errors: AppHost maps `Redis__ConnectionString` from the Redis resource reference.
- Frontend API errors: confirm `frontend/team-hub-web/proxy.conf.json` still targets `https://localhost:8080`.
- After renaming dev DB to `auth_db`, recreate the old volume if postgres still has database `auth`: `docker compose -f docker-compose.dev.yml down -v` then `up -d` (wipes local/dev data).
