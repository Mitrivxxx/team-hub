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
- Call `AddTeamHubBlobStorage(configuration)` to register `BlobStorageOptions`, `BlobServiceClient`, and `IBlobStorageService` when `BlobStorage:ConnectionString` is set.
- Bind config section `BlobStorage` (env: `BlobStorage__ConnectionString`, `BlobStorage__ContainerName`, `BlobStorage__PublicBlobEndpoint`, `BlobStorage__SasExpiryMinutes`).
- Use `BlobStoragePaths.OrganizationAvatar(orgId, extension)` for organization avatar blob names.
- Use `BlobStoragePaths.TeamAvatar(orgId, teamId, extension)` for team avatar blob names.
- Use `BlobStoragePaths.ImportExportSource/Result/Errors` for import/export job artifacts (export result file uses org slug/name, not `result.*`).
- Use `UploadAsync` (optional `downloadFileName` → Content-Disposition), `OpenReadAsync`, and `GetReadSasUri` for artifact storage and client downloads.
- Keep domain-specific upload validation and authorization in consuming services.
- Reference this project from microservices via `ProjectReference`.

## Don't
- Do not add business logic, MIME validation, or authorization here.
- Do not add Docker or container configuration here (see `infrastructure/azurite`).
