## Purpose
- Shared Azurite blob storage emulator for Aspire local dev and staging Compose (organization avatars).

## Source of truth
- `infrastructure/azurite/docker-compose.azurite.yml`
- `infrastructure/azurite/.env.example`
- Root `.env.staging.example` (`AZURITE_*`, `BlobStorage__*`)
- Root `docker-compose.yml` (includes Azurite for staging)
- `aspire/TeamHub.AppHost/Program.cs` (dev emulator + data volume)
- `building-blocks/TeamHub.BlobStorage/*`

## Why compose fragment, not Dockerfile
- Azurite runs from the official `mcr.microsoft.com/azure-storage/azurite` image with no custom build.
- Use a Dockerfile only when the image needs custom config beyond command args and volumes.
- This folder defines shared compose config included by the root Compose base (DRY).

## Env files
- Root `.env.staging` supplies `AZURITE_CONTAINER_NAME`, volume name, and `BlobStorage__*` for organization.
- `.env.example` — template for standalone Azurite.
- Do not commit `infrastructure/azurite/.env` (gitignored).

## Do
- Run one Azurite container per Aspire or staging stack (blob port `10000` on host).
- Keep connection string templates in `infrastructure/azurite/.env.example`.
- Use `building-blocks/TeamHub.BlobStorage` for blob client bootstrap in services.
- Set `BlobStorage__PublicBlobEndpoint` to a host-reachable URL for browser SAS links (`http://127.0.0.1:10000/devstoreaccount1`).
- Persist blobs: staging Compose volume `AZURITE_VOLUME_NAME`; Aspire dev `RunAsEmulator(...WithDataVolume())` (named Docker volume, typically `teamhub.apphost-blob-storage-data`).

## Don't
- Do not add per-service Azurite instances.
- Do not put avatar upload validation or API logic here (container config only; library lives in `building-blocks/TeamHub.BlobStorage`).
- Do not treat the well-known Azurite account key as a production secret.

## Connection strings
- Host / Aspire public URL: `BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1` (Aspire AppHost pins blob host port `10000`)
- Docker network (staging): `BlobEndpoint=http://blob-storage:10000/devstoreaccount1`
- Browser SAS URLs: use `BlobStorage__PublicBlobEndpoint=http://127.0.0.1:10000/devstoreaccount1`
