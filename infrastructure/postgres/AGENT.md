## Purpose
- Shared PostgreSQL for Team Hub Compose (dev companion + staging).

## Source of truth
- `infrastructure/postgres/docker-compose.postgres.yml`
- `infrastructure/postgres/init/01-create-dbs-dev.sql` → `auth_db`, `organization_db`, `notification_db`, `chat_db` (compose-dev / Aspire apps)
- `infrastructure/postgres/init/01-create-dbs-prod.sql` → `authdb`, `organizationdb`, `notificationdb`, `chatdb` (staging)
- `infrastructure/postgres/pgadmin/servers.json` (compose-dev PgAdmin pre-registered server)
- Root `.env.dev.example` / `.env.staging.example` (`POSTGRES_*`, `PGADMIN_*`)
- Root `docker-compose.dev.yml` (includes this fragment + `db-pgadmin` for Aspire companion)
- Root `docker-compose.yml` (includes this fragment for staging)

## Why compose fragment, not Dockerfile
- Postgres runs from the official `postgres:16` image with init SQL bind-mount.
- Use a Dockerfile only when custom extensions or image layers are required.

## Env
- Dev: `POSTGRES_*` from root `.env.dev`; `POSTGRES_INIT_SCRIPT=01-create-dbs-dev.sql`; host `127.0.0.1:5433`; volume `postgres_data_dev`.
- Dev PgAdmin: `PGADMIN_*` — UI `http://127.0.0.1:5050`, container `db-pgadmin-dev`.
- Staging: from `.env.staging`; `01-create-dbs-prod.sql`; container `db-postgres-staging` (no PgAdmin).

## Do
- Prefer compose-dev for local daily databases (`./scripts/compose-dev.sh up -d --wait`).
- PgAdmin (compose-dev only): login with `PGADMIN_DEFAULT_EMAIL` / `PGADMIN_DEFAULT_PASSWORD`; server `db-postgres` is pre-registered (Docker DNS). Enter `POSTGRES_PASSWORD` when connecting. Keep `servers.json` Username in sync with `POSTGRES_USER`.
- Keep passwords in `.env.dev` / `.env.staging` (gitignored); keep AppHost `Aspire:DevInfra:Postgres:Password` in sync with `.env.dev`.

## Don't
- Do not commit real `POSTGRES_PASSWORD` or `PGADMIN_DEFAULT_PASSWORD`.
- Do not start compose-dev and staging Compose on the same host port (`5433`).
- Do not add PgAdmin to staging (dev UI only).
