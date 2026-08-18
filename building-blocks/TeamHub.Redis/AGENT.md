## Purpose
- Shared Redis connection bootstrap for Team Hub microservices.

## Source of truth
- `TeamHub.Redis.csproj`
- `RedisOptions.cs`
- `ServiceCollectionExtensions.cs`

## Do
- Call `AddTeamHubRedis(configuration)` to register `RedisOptions` and `IConnectionMultiplexer`.
- Bind config section `Redis:ConnectionString` (env: `Redis__ConnectionString`).
- Build segmented Redis keys via `IRedisKeySegmenter`.
- Use `RedisDataSegments.Users` for user-related data.
- Use `RedisDataSegments.AuthLoginAttempts/AuthLoginLockout` for auth login lockout counters.
- Add future segments via `RedisDataSegments.Create("segment-name")`.
- Keep domain-specific stores (e.g. session store) in consuming services.
- After `IConnectionMultiplexer` connect, attach OpenTelemetry Redis instrumentation when `StackExchangeRedisInstrumentation` is registered (`includeStackExchangeRedis` on `AddTeamHubOpenTelemetry`).

## Don't
- Do not add business logic or key naming conventions here.
- Do not add Docker or container configuration here (see `infrastructure/redis`).
