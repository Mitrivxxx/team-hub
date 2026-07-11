## Purpose
- Shared Redis connection bootstrap for Team Hub microservices.

## Source of truth
- `TeamHub.Redis.csproj`
- `RedisOptions.cs`
- `ServiceCollectionExtensions.cs`

## Do
- Call `AddTeamHubRedis(configuration)` to register `RedisOptions` and `IConnectionMultiplexer`.
- Bind config section `Redis:ConnectionString` (env: `Redis__ConnectionString`).
- Keep domain-specific stores (e.g. session store) in consuming services.

## Don't
- Do not add business logic or key naming conventions here.
