## Purpose
- Shared Azure Blob Storage bootstrap for Team Hub microservices.

## Source of truth
- `TeamHub.BlobStorage.csproj`
- `BlobStorageOptions.cs`
- `IBlobStorageService.cs`
- `BlobStorageService.cs`
- `BlobStoragePaths.cs`
- `ServiceCollectionExtensions.cs`

## Do
- Call `AddTeamHubBlobStorage(configuration)` to register `BlobStorageOptions`, `BlobServiceClient`, and `IBlobStorageService` when a connection string is set.
- Connection string precedence: `ConnectionStrings:blobs` (Aspire `Aspire:DevInfra` / `WithTeamHubDevBlobStorage`) then `BlobStorage:ConnectionString` (`.env` / Compose).
- When `ConnectionStrings:blobs` is set, `PublicBlobEndpoint` is aligned from the connection string `BlobEndpoint=` (SAS URLs match Azurite host/port).
- Bind config section `BlobStorage` (env: `BlobStorage__ConnectionString`, `BlobStorage__ContainerName`, `BlobStorage__PublicBlobEndpoint`, `BlobStorage__SasExpiryMinutes`).
- Use `BlobStoragePaths.OrganizationAvatar(orgId, extension)` for organization avatar blob names.
- Use `BlobStoragePaths.TeamAvatar(orgId, teamId, extension)` for team avatar blob names.
- Use `BlobStoragePaths.UserAvatar(userId, extension)` for user avatar blob names (`users/{userId}/avatar.{ext}`).
- Use `BlobStoragePaths.ImportExportSource/Result/Errors` for import/export job artifacts (export result file uses org slug/name, not `result.*`).
- Use `UploadAsync` (optional `downloadFileName` → Content-Disposition), `OpenReadAsync`, and `GetReadSasUri` for artifact storage and client downloads.
- Keep domain-specific upload validation and authorization in consuming services.
- Reference this project from microservices via `ProjectReference` (organization + auth).
- Aspire AppHost injects Azurite via `Aspire:DevInfra:BlobStorage` (`127.0.0.1:10000` from compose-dev).

## Don't
- Do not add business logic, MIME validation, or authorization here.
- Do not add Docker or container configuration here (see `infrastructure/azurite`).
