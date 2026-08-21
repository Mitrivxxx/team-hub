## Purpose
- Shared Azurite blob storage emulator for compose-dev (Aspire companion) and staging Compose (avatars / import artifacts).

## Source of truth
- `infrastructure/azurite/docker-compose.azurite.yml`
- `infrastructure/azurite/.env.example`
- Root `.env.dev.example` / `.env.staging.example` (`AZURITE_*`, `BlobStorage__*`)
- Root `docker-compose.dev.yml` (includes Azurite for Aspire companion)
- Root `docker-compose.yml` (includes Azurite for staging)
- `aspire/TeamHub.AppHost/DevInfra.cs` / `appsettings.Development.json` (`Aspire:DevInfra:BlobStorage`)
- `building-blocks/TeamHub.BlobStorage/*`

## Why compose fragment, not Dockerfile
- Azurite runs from the official `mcr.microsoft.com/azure-storage/azurite` image with no custom build.
- Use a Dockerfile only when the image needs custom config beyond command args and volumes.

## Env files
- Root `.env.dev` / `.env.staging` supply `AZURITE_*` and `BlobStorage__*`.
- `.env.example` — template for standalone Azurite.
- Do not commit `infrastructure/azurite/.env` (gitignored).

## Do
- Run one Azurite per stack (blob port `10000` on host).
- Aspire apps: `BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1` (host processes).
- Staging containers: `BlobEndpoint=http://blob-storage:10000/devstoreaccount1` (Docker DNS).
- Persist: compose-dev volume `azurite_data_dev`; staging `AZURITE_VOLUME_NAME`.

## Don't
- Do not add per-service Azurite instances.
- Do not put avatar upload validation or API logic here.
- Do not treat the well-known Azurite account key as a production secret.

## Connection strings
- Host / Aspire: `BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1`
- Docker network (staging): `BlobEndpoint=http://blob-storage:10000/devstoreaccount1`
- Browser SAS: `BlobStorage__PublicBlobEndpoint=http://127.0.0.1:10000/devstoreaccount1`
