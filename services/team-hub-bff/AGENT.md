## Purpose
- GraphQL BFF that aggregates organization membership and activity with auth user profiles over gRPC.

## Source of truth
- `team-hub-bff/Program.cs`
- `team-hub-bff/Graph/`
- `team-hub-bff/Services/`
- `team-hub-bff/appsettings*.json`
- `building-blocks/TeamHub.GrpcContracts`

## Do
- Expose Hot Chocolate at `POST /api/graphql` (JWT required).
- Query `organizationMembers(organizationId, roleId?, teamId?)` returns membership + nested `user` profile.
- Query `organizationActivity(organizationId, type?, q?, from?, to?, page?, pageSize?)` returns paged activity + nested `actor` / `target` profiles.
- Resolve profiles via `UserByIdDataLoader` (batch `auth.GetUsersByIds`).
- Call organization `ListMembers` / `ListActivity` gRPC with `actor_user_id` from JWT `sub`.
- Keep gRPC targets in `Grpc:Auth` / `Grpc:Organization` (dev `5101`/`5102`, docker `auth:8081`/`team:8081`).
- Dev HTTP port `5003`; docker host `5003:8080`.
- DotNetEnv: `Env.NoClobber().TraversePath().Load()` so Aspire-injected Jwt/gRPC win over local `.env`.
- Gateway route: `/api/graphql/{**catch-all}` -> `bff-cluster`.

## Don't
- Do not add GraphQL mutations for members (REST remains for add/update/remove).
- Do not expose internal gRPC through nginx.
- Do not call auth/organization REST for the members/activity composition path.

## Checklist
- JWT validation uses same `Jwt__Key` / Issuer / Audience as `team-hub-auth` (see `.env`; must match auth signer).
- Missing auth profiles return `user` / `actor` / `target`: null.
- Health at `/health`.
