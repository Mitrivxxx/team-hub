## Purpose
- Shared microservice conventions for anything under `services/`.
- Per-service details stay in that service `AGENT.md`. Domain contracts stay in `docs/*.mb`.
- Verify against code/config; this file is a map, not a substitute.

## Inventory
| folder | runtime | public HTTP | internal |
|--------|---------|-------------|----------|
| `team-hub-gateway` | `gw-api` | YARP only | — |
| `team-hub-auth` | `srv-auth` | `/api/auth/v1/*` | gRPC profiles `:8081` / `:5101` |
| `team-hub-organization` | `srv-organization` | `/api/organizations/v1/*` | gRPC members `:8081` / `:5102`; Kafka producer |
| `team-hub-notification` | `srv-notification` | `/api/notifications/v1/*` | Kafka consumer |
| `team-hub-chat` | `srv-chat` | `/api/chat/v1/*` | gRPC proxy to org members |
| `team-hub-bff` | `srv-bff` | `/api/graphql` | gRPC to auth + org |

Code folders stay `services/team-hub-*`. Compose/Aspire DNS uses `[type]-[module]`.

## Boundaries
- Auth owns users, sessions, JWT, avatars of people.
- Organization owns orgs, teams, membership, RBAC, invitations, org/team avatars.
- Notification owns inbox rows; it does not own org membership.
- Chat owns conversations/messages; membership checks go to org gRPC.
- BFF composes GraphQL reads (members/activity + profiles). Mutations stay on REST.
- Gateway has no domain logic. Nginx terminates TLS; gateway is HTTP.

Do not duplicate another service's data. Resolve cross-service identity via gRPC contracts in `building-blocks/TeamHub.GrpcContracts`, not by copying user tables.

## Communication
- Public path: `ui-web -> gw-nginx -> gw-api -> srv-*`. Frontend never calls a service port.
- Gateway routes (source: `team-hub-gateway/team-hub-gateway/reverseproxy.json`):
  - `/api/auth/{**catch-all}` → `auth-cluster`
  - `/api/organizations/{**catch-all}` → `team-cluster`
  - `/api/notifications/{**catch-all}` → `notification-cluster`
  - `/api/chat/{**catch-all}` → `chat-cluster`
  - `/api/graphql/{**catch-all}` → `bff-cluster`
- Sync internal: gRPC only (not nginx, not gateway, not REST-to-REST).
- Async: Kafka `organization.events` (org outbox → notification). New events go through `TeamHub.Kafka` contracts + transactional outbox on the producer.
- JWT: same `Jwt:Issuer` / `Jwt:Audience` / `Jwt__Key` everywhere. User id = claim `sub`.

## Layout (domain APIs)
Match existing services; do not invent a new shape.

- `Program.cs` — bootstrap only: DotNetEnv (non-Production), `AddServiceDefaults`, Serilog/OTEL, capability DI, `UseApiPipeline`, observability endpoints.
- `Configuration/` — options, DI (`ServiceCollectionExtensions` or `Configuration/Extensions/`), pipeline (`WebApplicationExtensions`), API versions, Swagger.
- `Controllers/{Feature}/` — HTTP; inherit `{Service}ApiController`; XML `<summary>` only on actions.
- `Services/` — application logic. Controllers throw domain exceptions; mappers convert them.
- `Data/` + `Migrations/` — EF Core. Apply on startup except `Testing`.
- `Seeding/` — `--seed` one-shot; Development/Staging + `Seed:Enabled=true`; Production blocked.
- `Dtos/` / `Models/` — keep model namespaces stable (migrations).
- Tests project next to the service (`*.Tests`).

Gateway stays YARP + `reverseproxy.json`. BFF stays `Graph/` + gRPC clients.

DI: infra adapters first (`AddDatabase`, JWT, gRPC, blob, Kafka), then `AddApplicationServices`, then `AddApiInfrastructure`.

## Pipeline order (domain APIs)
1. `UseTeamHubExceptionHandling` (first)
2. `UseTeamHubCorrelationId` (`X-Correlation-ID` = OTEL TraceId; echo)
3. `UseTeamHubSessionId` (`X-Session-ID`)
4. `UseAuthentication` / `UseAuthorization`
5. `UseTeamHubUserIdLogging` (JWT `sub`)
6. `UseSerilogRequestLoggingExcludingHealth` (skip `/health`, `/metrics`)

Swagger UI: Development only. Versioned docs from `IApiVersionDescriptionProvider`.
`IFormFile` uploads: `[Consumes("multipart/form-data")]` without `[FromForm]` on `IFormFile`.

## HTTP contract
- Versioned URL: `/api/{domain}/v1/*`. Source: `{Service}ApiVersions.cs` + `{Service}ApiController`.
- Additive changes stay on `v1`. Breaking change adds `v2` beside deprecated `v1`. Major only in the path.
- Anonymous: `/health`, `/metrics`. Everything else JWT unless a service `AGENT.md` says otherwise.
- Errors: RFC 9457 `application/problem+json` via `AddTeamHubProblemDetails()` + `AddTeamHubExceptionMapper<T>()`. Types in `docs/errors.mb`. Clients switch on `type`.
- Logging: Serilog only (`AddTeamHubSerilog`). No `ILogger` templates that skip Serilog bootstrap.

## Config
- `appsettings.json` — shared defaults (Jwt issuer/audience, Serilog, Observability). No localhost Kestrel/gRPC.
- `appsettings.Development.json` / `appsettings.Staging.json` — localhost ports, seed on.
- `appsettings.Production.json` — Kestrel `+:8080` (REST) / `+:8081` (gRPC), docker DNS (`srv-*`), seed off, compact JSON + OTLP to `mon-otel:4317`.
- gRPC options: `[Required]` + `ValidateOnStart`; no class-level localhost defaults.
- DotNetEnv: `Env.NoClobber().TraversePath().Load()` only when not Production. Aspire/Compose env wins.
- Secrets: Aspire AppHost (dev), root `.env.staging` (staging). Never commit `.env` / `.env.dev` / `.env.staging`.
- Blob: Production requires `BlobStorage:ConnectionString` (fail-fast). Non-Production may return `503` when unset.

## Ports (host / container)
| service | host HTTP | host gRPC | container REST | container gRPC |
|---------|-----------|-----------|----------------|----------------|
| gateway | `5000` | — | `8080` | — |
| auth | `5001` | `5101` | `8080` | `8081` |
| organization | `5002` | `5102` | `8080` | `8081` |
| bff | `5003` | — | `8080` | — |
| notification | `5004` | — | `8080` | — |
| chat | `5005` | — | `8080` | — |

Edge: nginx `8080`. Postgres host `5433`. Do not reuse these ports.

DB names: compose-dev / Aspire `*_db` (`auth_db`, …) on `127.0.0.1:5433`; staging Compose `*db` (`authdb`, …) on `db-postgres`.

## Building blocks
Use via `ProjectReference`; do not copy their bootstrap into a service.

- `TeamHub.Observability` — Serilog, OTEL, exception/correlation/session/user middleware, ProblemDetails
- `TeamHub.GrpcContracts` — protobuf; change contract here, regenerate, update callers
- `TeamHub.Kafka` / `TeamHub.Redis` / `TeamHub.BlobStorage` / `TeamHub.DemoSeed`

No domain logic in building blocks.

## After a service change
Update, if touched: that service `AGENT.md`, gateway `reverseproxy.json` (new public route), nginx if a new public prefix, `docs/*.mb`, Aspire AppHost + compose if a new dependency/port, tests for behavior.

## Don't
- Do not bypass nginx/gateway from the frontend.
- Do not put business logic in gateway.
- Do not expose gRPC through nginx.
- Do not invent routes, ports, env keys, or runtime names.
- Do not call another service's REST for data the gRPC contract already covers.
- Do not skip EF migrations / `AuthDbContext` (or equivalent) registration for model changes.
- Do not log secrets or seed passwords.
- Do not change a public contract without docs + tests.

## Checklist
- Change stays in the owning service.
- Public route still matches gateway catch-all + versioned controller path.
- Pipeline, ProblemDetails, Serilog, health/metrics unchanged in order/shape.
- Config keys bound through options; Production has no localhost.
- `AGENT.md` of the touched service still true.
