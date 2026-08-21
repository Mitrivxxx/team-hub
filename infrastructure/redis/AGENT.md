## Purpose
- Shared Redis cache for all Team Hub services (single instance).

## Source of truth
- `infrastructure/redis/docker-compose.redis.yml`
- `infrastructure/redis/.env.example`
- Root `.env.staging.example` / `.env.dev.example` (container names via `REDIS_CONTAINER_NAME`)
- Root `docker-compose.yml` / `docker-compose.dev.yml` (includes Redis)

## Why compose fragment, not Dockerfile
- Redis runs from the official `redis:7-alpine` image with no custom build.
- Use a Dockerfile only when the image needs custom config (e.g. `redis.conf`, modules, init scripts).
- This folder defines shared compose config included by the root Compose base (DRY).

## Env files
- Root `.env.staging` / `.env.dev` supply `REDIS_CONTAINER_NAME` / `REDIS_PORT` for Compose interpolation.
- `.env.example` — template for standalone Redis (`docker compose -f infrastructure/redis/docker-compose.redis.yml --env-file infrastructure/redis/.env up -d`).
- Do not commit `infrastructure/redis/.env` (gitignored).

## Do
- Run one Redis container per stack (`6379` on host for Compose).
- Keep connection string template in `infrastructure/redis/.env.example` (`Redis__ConnectionString`).
- Use `building-blocks/TeamHub.Redis` (`TeamHub.Redis`) for `IConnectionMultiplexer` registration in services.
- Use key prefixes per domain (e.g. auth: `auth:session:*`).
- Container names come from root env (`cache-redis-dev` for compose-dev; `cache-redis-staging` for staging).

## Don't
- Do not add per-service Redis instances.
- Do not add a Dockerfile here unless custom Redis image is required.
- Do not put domain/session logic here (container config only; library lives in `building-blocks/TeamHub.Redis`).

## Connection strings
- Aspire apps (compose-dev): `127.0.0.1:6379` via `Aspire:DevInfra`
- Docker network (staging): `cache-redis:6379`
