## Purpose
- Shared PostgreSQL for Team Hub Compose staging (auth / organization / notification databases).

## Source of truth
- `infrastructure/postgres/docker-compose.postgres.yml`
- `infrastructure/postgres/init/01-create-dbs-prod.sql`
- `infrastructure/postgres/init/01-create-dbs-dev.sql` (legacy Aspire-aligned names; Aspire creates DBs itself)
- Root `.env.staging.example` (`POSTGRES_*`)
- Root `docker-compose.yml` (includes this fragment)

## Why compose fragment, not Dockerfile
- Postgres runs from the official `postgres:16` image with init SQL bind-mount.
- Use a Dockerfile only when custom extensions or image layers are required.

## Env
- `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_CONTAINER_NAME` / `POSTGRES_VOLUME_NAME` / `POSTGRES_INIT_SCRIPT` / `POSTGRES_PORT` from root `.env.staging`.
- Staging init script: `01-create-dbs-prod.sql` → `authdb`, `organizationdb`, `notificationdb`.

## Do
- Prefer Aspire AppHost for local daily databases (`auth_db` / `organization_db` / `notification_db`).
- Use this fragment for staging Compose only.
- Keep passwords in `.env.staging` (gitignored).

## Don't
- Do not commit real `POSTGRES_PASSWORD`.
- Do not start this fragment alongside Aspire on the same host port (`5433`).
