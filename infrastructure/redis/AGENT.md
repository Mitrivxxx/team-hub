## Purpose
- Shared Redis cache for all Team Hub services (single instance).

## Source of truth
- `infrastructure/redis/docker-compose.redis.yml`
- `infrastructure/redis/.env.example`
- `infrastructure/redis/compose.dev.env`
- `infrastructure/redis/compose.prod.env`
- Root `docker-compose.yml` and `docker-compose.dev.yml` (include Redis service)

## Why compose fragment, not Dockerfile
- Redis runs from the official `redis:7-alpine` image with no custom build.
- Use a Dockerfile only when the image needs custom config (e.g. `redis.conf`, modules, init scripts).
- This folder defines shared compose config included by root compose files (DRY, separate dev/prod container names).

## Env files
- `compose.dev.env` / `compose.prod.env` — committed runtime env for root compose `include` (`REDIS_CONTAINER_NAME`).
- `.env.example` — template for standalone Redis (`docker compose -f infrastructure/redis/docker-compose.redis.yml --env-file infrastructure/redis/.env up -d`).
- Do not commit `infrastructure/redis/.env` (gitignored). Root compose does not require it.

## Do
- Run one Redis container per stack (`6379` on host for local dev).
- Keep connection string template in `infrastructure/redis/.env.example` (`Redis__ConnectionString`).
- Use `building-blocks/TeamHub.Redis` (`TeamHub.Redis`) for `IConnectionMultiplexer` registration in services.
- Use key prefixes per domain (e.g. auth: `auth:session:*`).
- Mount dev/prod container names from root compose overrides (`cache-redis-dev`, `cache-redis-prod`).

## Don't
- Do not add per-service Redis instances.
- Do not add a Dockerfile here unless custom Redis image is required.
- Do not put domain/session logic here (container config only; library lives in `building-blocks/TeamHub.Redis`).

## Connection strings
- Local dev (`dotnet run`): `localhost:6379`
- Docker network: `cache-redis:6379`
