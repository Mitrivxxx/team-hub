## Purpose
- Shared Azurite blob storage emulator for local development (organization avatars).

## Source of truth
- `infrastructure/azurite/docker-compose.azurite.yml`
- `infrastructure/azurite/.env.example`
- `infrastructure/azurite/compose.dev.env`
- Root `docker-compose.dev.yml` (include Azurite service; dev only)
- `building-blocks/TeamHub.BlobStorage/*`

## Why compose fragment, not Dockerfile
- Azurite runs from the official `mcr.microsoft.com/azure-storage/azurite` image with no custom build.
- Use a Dockerfile only when the image needs custom config beyond command args and volumes.
- This folder defines shared compose config included by root dev compose (DRY).

## Env files
- `compose.dev.env` — committed runtime env for root compose `include` (`AZURITE_CONTAINER_NAME`, `AZURITE_BLOB_PORT`).
- `.env.example` — template for standalone Azurite and `BlobStorage__*` connection settings.
- Do not commit `infrastructure/azurite/.env` (gitignored). Root dev compose does not require it.

## Do
- Run one Azurite container per dev stack (blob port `10000` on host).
- Keep connection string templates in `infrastructure/azurite/.env.example`.
- Use `building-blocks/TeamHub.BlobStorage` for blob client bootstrap in services.
- Set `BlobStorage__PublicBlobEndpoint` to a host-reachable URL for browser SAS links (`http://127.0.0.1:10000/devstoreaccount1`).
- Mount dev container name from root compose overrides (`blob-storage-dev`).
- Persist blobs via Docker volume `azurite_data_dev`.

## Don't
- Do not add Azurite to `docker-compose.yml` (prod/pre-prod uses real Azure Storage when enabled).
- Do not add per-service Azurite instances.
- Do not put avatar upload validation or API logic here (container config only; library lives in `building-blocks/TeamHub.BlobStorage`).

## Connection strings
- Local dev (`dotnet run`): `BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1`
- Docker network: `BlobEndpoint=http://blob-storage:10000/devstoreaccount1`
- Browser SAS URLs: use `BlobStorage__PublicBlobEndpoint=http://127.0.0.1:10000/devstoreaccount1`
