# Team Hub Architecture (AI ops)

## Purpose
- Give an AI agent a fast map of runtime topology, config sources, and integration boundaries.

## Source of truth
- `TeamHub.sln`
- `aspire/TeamHub.AppHost/Program.cs`
- `docker-compose.yml`
- `services/team-hub-auth/team-hub-auth/.env.example`
- `services/team-hub-gateway/team-hub-gateway/.env.example`
- `services/team-hub-bff/team-hub-bff/.env.example`
- `services/team-hub-gateway/team-hub-gateway/Program.cs`
- `services/team-hub-gateway/team-hub-gateway/reverseproxy.json`
- `services/team-hub-gateway/team-hub-gateway/appsettings.*.json`
- `services/team-hub-auth/team-hub-auth/Program.cs`
- `services/team-hub-auth/team-hub-auth/appsettings*.json`
- `services/team-hub-organization/team-hub-organization/Program.cs`
- `services/team-hub-organization/team-hub-organization/appsettings*.json`
- `services/team-hub-bff/team-hub-bff/Program.cs`
- `services/team-hub-bff/team-hub-bff/appsettings*.json`
- `building-blocks/TeamHub.GrpcContracts/`
- `frontend/team-hub-web/src/environments/environment.ts`
- `frontend/team-hub-web/proxy.conf.json`
- `infrastructure/nginx/nginx.conf`

## Do
- Treat flow as: frontend -> infrastructure nginx -> gateway -> auth|team|bff; BFF uses internal gRPC to auth/organization.
- **Preferred local dev:** Aspire AppHost (`cd aspire/TeamHub.AppHost && dotnet run`) — full stack, dashboard, auto-wired connection strings.
- **Fallback local dev:** `docker-compose.dev.yml` (postgres + redis) + manual `dotnet run` / `npm start`.
- Keep secrets in per-service `.env` files (auth, gateway, bff) for docker compose; Aspire dev JWT in `aspire/TeamHub.AppHost/appsettings.Development.json`.
- Use gateway routes `/api/auth/{**catch-all}`, `/api/organizations/{**catch-all}`, and `/api/graphql/{**catch-all}` as declared reverse proxy routes.
- In local Angular dev, keep `/api` proxied to `https://localhost:8080` (infrastructure nginx HTTPS).
- For docker runtime, use mapped host ports: frontend `4200` (HTTPS), nginx `8080` (HTTPS API edge), gateway `5000` (HTTP debug), auth `5001`, team `5002`, bff `5003`, postgres `5433`.
- Docker compose runs `ASPNETCORE_ENVIRONMENT=Production` with `appsettings.Production.json` (pre-deployment build).
- Read gateway destinations per environment:
  - Development (manual): auth `http://localhost:5001/`, team `http://localhost:5002/`, bff `http://localhost:5003/`
  - Development (Aspire): `http://team-hub-auth` / `http://team-hub-organization` / `http://team-hub-bff` via service discovery (env override from AppHost)
  - Production/container: auth `http://auth:8080/`, team `http://team:8080/`, bff `http://bff:8080/`
- Internal gRPC (not through nginx/gateway):
  - auth `5101` (dev) / `8081` (docker): `GetUsersByIds`
  - organization `5102` (dev) / `8081` (docker): `ListMembers`
  - BFF composes All Members via GraphQL + DataLoader
- Keep auth DB host context-aware:
  - local/dev (`docker-compose.dev.yml` / Aspire): database `auth_db`
  - docker prod compose: database `authdb`
  - local dotnet run: `localhost:5433`
  - docker auth container: `postgres:5432`

- Keep organization DB host context-aware:
  - local/dev (`docker-compose.dev.yml` / Aspire): database `organization_db`
  - docker prod compose: database `organizationdb`
  - local dotnet run: `localhost:5433`
  - docker organization container: `postgres:5432`

## Don't
- Do not bypass gateway for frontend API calls.
- Do not document routes not present in controllers or proxy config.
- Do not assume extra services beyond auth, team, bff, gateway, infrastructure nginx, infrastructure redis, postgres, and web frontend container.
- Do not use docker compose for daily Development workflow when Aspire AppHost is available.
- Do not expose internal gRPC ports publicly through nginx.

## Checklist
- Verify route chain frontend `/api/auth/*` -> infrastructure nginx -> gateway -> auth.
- Verify route chain frontend `/api/organizations/*` -> infrastructure nginx -> gateway -> team.
- Verify route chain frontend `/api/graphql` -> infrastructure nginx -> gateway -> bff -> gRPC auth/organization.
- Verify all documented ports match `docker-compose.yml` and launch settings.
- Verify env/config references point to existing files only.
