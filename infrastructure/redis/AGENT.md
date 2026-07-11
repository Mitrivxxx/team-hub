## Purpose
- Shared Redis cache for all Team Hub services (single instance).

## Source of truth
- `infrastructure/redis/docker-compose.redis.yml`
- `infrastructure/redis/.env.example`
- `infrastructure/redis/compose.dev.env`
- `infrastructure/redis/compose.prod.env`
- `infrastructure/team-hub-redis/*`
- Root `docker-compose.yml` and `docker-compose.dev.yml` (include Redis service)

## Do
- Run one Redis container per stack (`6379` on host for local dev).
- Keep connection string in `infrastructure/redis/.env.example` (`Redis__ConnectionString`).
- Use `TeamHub.Redis` library for `IConnectionMultiplexer` registration in services.
- Use key prefixes per domain (e.g. auth: `auth:session:*`).
- Mount dev/prod container names from root compose overrides (`team-hub-redis-dev`, `team-hub-redis-prod`).

## Don't
- Do not add per-service Redis instances or connection config outside infrastructure.
- Do not put domain/session logic in this library (only connection/bootstrap).

## Connection strings
- Local dev (`dotnet run`): `localhost:6379`
- Docker network: `redis:6379`
