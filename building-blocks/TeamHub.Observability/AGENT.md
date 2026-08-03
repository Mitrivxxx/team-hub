## Purpose
- Shared OpenTelemetry, Serilog bootstrap, and cross-service ASP.NET Core observability middleware for Team Hub microservices.

## Source of truth
- `ObservabilityOptions.cs` — `ServiceName`, `OtlpEndpoint` (`Observability` config section).
- `ServiceCollectionExtensions.cs` — `AddTeamHubOpenTelemetry`.
- `HostBuilderExtensions.cs` — `AddTeamHubSerilog` (Span enricher, config-driven sinks).
- `WebApplicationExtensions.cs` — `MapTeamHubObservabilityEndpoints` (`/metrics`).
- `ApplicationBuilderExtensions.cs` — `UseTeamHubExceptionHandling`, `UseTeamHubCorrelationId`, `UseTeamHubUserIdLogging`.
- `SerilogRequestLoggingExtensions.cs` — `UseSerilogRequestLoggingExcludingHealth`.
- `ServiceCollectionExceptionExtensions.cs` — `AddTeamHubExceptionMapper<TMapper>`.
- `Middleware/` — `ExceptionMiddleware`, `CorrelationIdMiddleware`, `UserIdLoggingMiddleware`, `IExceptionProblemDetailsMapper`.

## Do
- Register via `AddTeamHubOpenTelemetry(configuration, serviceName, includeEntityFrameworkCore)`.
- Use `includeEntityFrameworkCore: true` only when the service uses EF Core.
- Push traces via OTLP (`Observability:OtlpEndpoint` or `OTEL_EXPORTER_OTLP_ENDPOINT`).
- Expose Prometheus scrape at `/metrics`.
- Enrich logs with `TraceId`, `SpanId` (`Serilog.Enrichers.Span`); push prod logs via `Serilog.Sinks.OpenTelemetry`.
- Exclude `/health` and `/metrics` from ASP.NET Core trace instrumentation and Serilog request logging.
- Use shared middleware: Exception (first) → CorrelationId → (auth SessionId) → Authentication/Authorization → UserIdLogging → Serilog request logging.
- `CorrelationIdMiddleware`: prefer non-empty OpenTelemetry `TraceId`, else trimmed `X-Correlation-ID`, else new Guid (`N`); set request/response headers + `LogContext`; ignore empty/whitespace TraceId (`000…0`) and headers.
- `ExceptionMiddleware`: RFC 7807 `ProblemDetails` with `correlationId` (and `sessionId` when `HttpContext.Items["SessionId"]` is set); service-specific mappings via `IExceptionProblemDetailsMapper`.
- `UserIdLoggingMiddleware`: authenticated JWT `NameIdentifier` / `sub` → `LogContext.UserId`.

## Don't
- Do not add domain/business logic here (keep mappers in the owning service).
- Do not duplicate these middlewares inside individual services.
- Do not bypass OTLP collector for production log shipping when monitoring stack is enabled.
