# Team Hub Architecture (AI ops)

## Purpose
- Give an AI agent a fast map of runtime topology, config sources, and integration boundaries.

## Source of truth
- `docker-compose.yml`
- `services/team-hub-gateway/team-hub-gateway/Program.cs`
- `services/team-hub-gateway/team-hub-gateway/reverseproxy.json`
- `services/team-hub-gateway/team-hub-gateway/appsettings.*.json`
- `services/team-hub-auth/team-hub-auth/Program.cs`
- `services/team-hub-auth/team-hub-auth/appsettings*.json`
- `services/team-hub-auth/team-hub-auth/.env.example`
- `frontend/team-hub-web/src/environments/environment.ts`
- `frontend/team-hub-web/proxy.conf.json`

## Do
- Treat flow as: frontend -> gateway -> auth -> postgres.
- Use gateway route `/api/auth/{**catch-all}` as only declared reverse proxy route.
- In local Angular dev, keep `/api` proxied to `https://localhost:7172`.
- For docker runtime, use mapped ports: gateway `7172`, auth `5112`, postgres `5433`.
- Read gateway destination per environment:
  - Development: `http://localhost:5101/`
  - Production/container: `http://auth:8080/`
- Keep auth DB host context-aware:
  - local dotnet run: `localhost:5433`
  - docker auth container: `postgres:5432`

## Don't
- Do not bypass gateway for frontend API calls.
- Do not document routes not present in controllers or proxy config.
- Do not assume extra services (only auth, gateway, postgres are wired).

## Checklist
- Verify route chain frontend `/api/auth/*` -> gateway -> auth.
- Verify all documented ports match `docker-compose.yml` and launch settings.
- Verify env/config references point to existing files only.
