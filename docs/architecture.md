# Team Hub Architecture (AI ops)

## Purpose
- Give an AI agent a fast map of runtime topology, config sources, and integration boundaries.

## Source of truth
- `TeamHub.sln`
- `aspire/TeamHub.AppHost/Program.cs`
- `docker-compose.yml` (shared base)
- `docker-compose.dev.yml` (Aspire companion: Postgres/Redis/Kafka/Azurite + monitoring; use `scripts/compose-dev.sh`)
- `docker-compose.staging.yml` (full-stack staging overlay)
- `.env.dev.example` / `.env.staging.example`
- `services/team-hub-gateway/team-hub-gateway/Program.cs`
- `services/team-hub-gateway/team-hub-gateway/reverseproxy.json`
- `services/team-hub-gateway/team-hub-gateway/appsettings.*.json`
- `services/team-hub-auth/team-hub-auth/Program.cs`
- `services/team-hub-auth/team-hub-auth/appsettings*.json`
- `services/team-hub-organization/team-hub-organization/Program.cs`
- `services/team-hub-organization/team-hub-organization/appsettings*.json`
- `services/team-hub-chat/team-hub-chat/Program.cs`
- `services/team-hub-chat/team-hub-chat/appsettings*.json`
- `services/team-hub-bff/team-hub-bff/Program.cs`
- `services/team-hub-bff/team-hub-bff/appsettings*.json`
- `building-blocks/TeamHub.GrpcContracts/`
- `frontend/team-hub-web/src/environments/environment.ts`
- `frontend/team-hub-web/proxy.conf.json`
- `infrastructure/nginx/nginx.conf`

## Do
- Treat flow as: frontend -> infrastructure nginx -> gateway -> auth|team|notification|chat|bff; BFF uses internal gRPC to auth/organization.
- **Preferred local dev:** Aspire AppHost (`cd aspire/TeamHub.AppHost && dotnet run`) — full stack, dashboard, auto-wired connection strings.
- **Aspire companion infra:** `./scripts/compose-dev.sh up -d --wait` (or `./scripts/dev-up.sh`) — Postgres/Redis/Kafka/Azurite + monitoring on network `team-hub-dev`; Aspire AppHost runs apps only and scrapes via `host.docker.internal`.
- **Staging:** `./scripts/compose-staging.sh up --build -d` — full Docker stack; secrets in root `.env.staging`.
- Aspire JWT in `aspire/TeamHub.AppHost/appsettings.Development.json`; staging JWT/DB/Grafana in `.env.staging` (from `.env.staging.example`).
- Use gateway routes `/api/auth/{**catch-all}`, `/api/organizations/{**catch-all}`, `/api/notifications/{**catch-all}`, `/api/chat/{**catch-all}`, and `/api/graphql/{**catch-all}` as declared reverse proxy routes.
- In local Angular dev, keep `/api` proxied to `https://localhost:8080` (infrastructure nginx HTTPS).
- For staging Docker runtime, use mapped host ports: frontend `4200` (HTTPS), nginx `8080` (HTTPS API edge), gateway `5000` (HTTP debug), auth `5001`, team `5002`, bff `5003`, notification `5004`, chat `5005`, postgres `5433`, Azurite `10000`.
- Staging Compose runs `ASPNETCORE_ENVIRONMENT=Production` with `appsettings.Production.json`.
- Read gateway destinations per environment:
  - Development (Aspire): `http://srv-auth` / `http://srv-organization` / `http://srv-notification` / `http://srv-chat` / `http://srv-bff` via service discovery
  - Staging/container: auth `http://srv-auth:8080/`, team `http://srv-organization:8080/`, notification `http://srv-notification:8080/`, chat `http://srv-chat:8080/`, bff `http://srv-bff:8080/`
- Internal gRPC (not through nginx/gateway):
  - auth `5101` (Aspire) / `8081` (Docker): `GetUsersByIds`
  - organization `5102` (Aspire) / `8081` (Docker): `ListMembers`
  - BFF composes All Members via GraphQL + DataLoader
- Keep auth DB host context-aware:
  - Aspire + compose-dev: database `auth_db` on `127.0.0.1:5433`
  - staging Compose: database `authdb` on `db-postgres:5432`
- Keep organization DB host context-aware:
  - Aspire + compose-dev: database `organization_db` on `127.0.0.1:5433`
  - staging Compose: database `organizationdb` on `db-postgres:5432`
- Keep notification DB host context-aware:
  - Aspire + compose-dev: database `notification_db` on `127.0.0.1:5433`
  - staging Compose: database `notificationdb` on `db-postgres:5432`
- Keep chat DB host context-aware:
  - Aspire + compose-dev: database `chat_db` on `127.0.0.1:5433`
  - staging Compose: database `chatdb` on `db-postgres:5432`
- Keep blob storage (Azurite in compose-dev / staging) for avatars: Aspire injects `Aspire:DevInfra:BlobStorage` (`127.0.0.1:10000`); staging Compose `BlobStorage__*` on auth/organization/chat.

## Don't
- Do not bypass gateway for frontend API calls.
- Do not document routes not present in controllers or proxy config.
- Do not assume extra services beyond those in Aspire AppHost / Compose base.
- Do not use staging Compose for daily Development workflow when Aspire AppHost is available.
- Do not expose internal gRPC ports publicly through nginx.
- Do not commit `.env.dev` / `.env.staging`.

## Checklist
- Verify route chain frontend `/api/auth/*` -> infrastructure nginx -> gateway -> auth.
- Verify route chain frontend `/api/organizations/*` -> infrastructure nginx -> gateway -> team.
- Verify route chain frontend `/api/graphql` -> infrastructure nginx -> gateway -> bff -> gRPC auth/organization.
- Verify all documented ports match Compose base and Aspire launch settings.
- Verify env/config references point to existing files only.
