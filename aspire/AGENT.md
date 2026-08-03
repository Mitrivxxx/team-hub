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
- Prerequisites: .NET 10 SDK (`dotnet --version` should report 10.x), Aspire 13 (`Aspire.AppHost.Sdk` via NuGet — no Aspire workload), Docker, Node.js/npm, TLS certs in `certs/` (`./scripts/setup-certs.sh`).
  - If `dotnet --list-sdks` only shows 8.x, install SDK 10 (`https://aka.ms/dotnet/download` or `curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0`) and put `$HOME/.dotnet` first on `PATH` (`export DOTNET_ROOT=$HOME/.dotnet; export PATH=$HOME/.dotnet:$PATH`). Repo `global.json` pins SDK 10.0.x.
- AppHost runs: Postgres (Aspire resource `auth-db`, database name `auth_db`), Redis, Azurite blob storage (`storage` + `blobs`), `team-hub-auth`, `team-hub-organization`, `team-hub-notification`, `team-hub-bff`, `team-hub-gateway`, infrastructure nginx, Angular (`npm start`).
- Aspire resource names: only ASCII letters, digits, hyphens (no underscores).
- Keep production/pre-prod on `docker-compose.yml` — Aspire is dev-only.
- JWT dev secrets live in `aspire/TeamHub.AppHost/appsettings.Development.json` (`Aspire:Jwt:*`).
- Gateway destinations under Aspire: auth `http://team-hub-auth`, team `http://team-hub-organization`, bff `http://team-hub-bff` (YARP service discovery).
- Internal gRPC ports under Aspire: auth `5101`, organization `5102`; BFF `Grpc__Auth`/`Grpc__Organization` point at `127.0.0.1:5101/5102`.
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
| Frontend (web) | `https://localhost:4200` |
| Infrastructure nginx (API edge) | `https://localhost:8080` |
| Gateway (`team-hub-gateway`) | `http://localhost:5000` |
| BFF (`team-hub-bff`) | `http://localhost:5003` |
| Auth (`team-hub-auth`) | REST dynamic / gRPC `http://localhost:5101` |
| Organization (`team-hub-organization`) | REST dynamic or `http://localhost:5002` / gRPC `http://localhost:5102` |
| Notification (`team-hub-notification`) | `http://localhost:5004` |
| Postgres | dynamic (see dashboard; database `auth_db`) |
| Redis | dynamic (see dashboard) |
| Azurite (blob) | dynamic (see dashboard; host blob port often `10000`) |

## Request flow
- Frontend `/api/auth/*` -> nginx `8080` -> gateway `5000` -> auth (service discovery) -> postgres + redis.
- Frontend `/api/organizations/*` -> nginx `8080` -> gateway `5000` -> team (service discovery) -> postgres + Azurite (avatars).

## Troubleshooting
- Port conflict: stop `docker compose` and any running `dotnet`/`npm` processes before `dotnet run` AppHost.
  - Aspire brings its own Postgres/Redis — do not run `docker-compose.dev.yml` at the same time.
  - Example: `docker compose -f docker-compose.dev.yml down`
- Nginx 502 to gateway on WSL2: ensure Docker supports `host.docker.internal` (`--add-host=host.docker.internal:host-gateway` is set in AppHost).
- Auth Redis errors: AppHost maps `Redis__ConnectionString` from the Redis resource reference.
- Frontend API errors: confirm `frontend/team-hub-web/proxy.conf.json` still targets `https://localhost:8080`.
- After renaming dev DB to `auth_db`, recreate the old volume if postgres still has database `auth`: `docker compose -f docker-compose.dev.yml down -v` then `up -d` (wipes local/dev data).
