## Purpose
- Shared OpenTelemetry, Serilog bootstrap, and cross-service ASP.NET Core observability middleware for Team Hub microservices.

## Source of truth
- `OpenTelemetry/ObservabilityOptions.cs` — `ServiceName`, `OtlpEndpoint` (`Observability` config section).
- `OpenTelemetry/ServiceCollectionExtensions.cs` — `AddTeamHubOpenTelemetry`.
- `OpenTelemetry/ObservabilitySources.cs` — ActivitySource names (`TeamHub.Kafka`).
- `OpenTelemetry/WebApplicationExtensions.cs` — `MapTeamHubObservabilityEndpoints` (`/metrics`).
- `Logging/HostBuilderExtensions.cs` — `AddTeamHubSerilog` (Span enricher, config-driven sinks).
- `Logging/SerilogRequestLoggingExtensions.cs` — `UseSerilogRequestLoggingExcludingHealth`.
- `Problems/ServiceCollectionExceptionExtensions.cs` — `AddTeamHubExceptionMapper<TMapper>`, `AddTeamHubProblemDetails`.
- `Problems/ProblemTypes.cs` — RFC 9457 `type` URI base (`https://teamhub.dev/problems/{suffix}`).
- `Problems/TeamHubProblemDetailsFactory.cs` — builds `ProblemDetails` / `ValidationProblemDetails` with `correlationId`.
- `Problems/IExceptionProblemDetailsMapper.cs`, `Problems/ExceptionMapping.cs` — service-specific exception → ProblemDetails mapping (namespace `TeamHub.Observability.Middleware`).
- `Middleware/` — `ExceptionMiddleware`, `CorrelationIdMiddleware`, `SessionIdMiddleware`, `UserIdLoggingMiddleware`.
- `Middleware/ApplicationBuilderExtensions.cs` — `UseTeamHubExceptionHandling`, `UseTeamHubCorrelationId`, `UseTeamHubSessionId`, `UseTeamHubUserIdLogging`.

## Do
- Register via `AddTeamHubOpenTelemetry(configuration, serviceName, includeEntityFrameworkCore, includeStackExchangeRedis)`.
- Use `includeEntityFrameworkCore: true` only when the service uses EF Core.
- Use `includeStackExchangeRedis: true` only when the service uses `TeamHub.Redis`.
- Push traces via OTLP (`Observability:OtlpEndpoint`, else `OTEL_EXPORTER_OTLP_ENDPOINT`, else `http://localhost:4317`).
- Resource attributes: `service.name`, `service.version` (entry assembly), `deployment.environment`.
- Register custom meters with the same `serviceName` (`AddMeter`).
- Expose Prometheus scrape at `/metrics`.
- Enrich logs with `TraceId`, `SpanId` (`Serilog.Enrichers.Span`); push logs via `Serilog.Sinks.OpenTelemetry` (dev localhost collector, prod `mon-otel`).
- Exclude `/health` and `/metrics` from ASP.NET Core trace instrumentation and Serilog request logging.
- Use shared middleware: Exception (first) → CorrelationId → SessionId → Authentication/Authorization → UserIdLogging → Serilog request logging.
- `SessionIdMiddleware`: first `X-Session-ID` value (comma-separated duplicates collapsed) or new Guid; echo a single value on response (`OnStarting` so YARP/downstream copies do not accumulate); `LogContext` + `HttpContext.Items["SessionId"]`.
- `CorrelationIdMiddleware`: prefer non-empty OpenTelemetry `TraceId`, else first trimmed `X-Correlation-ID`, else new Guid (`N`); set request/response headers + `LogContext`; collapse duplicate response values on `OnStarting`.
- `ExceptionMiddleware`: RFC 9457 `application/problem+json` with stable `type` URIs, `correlationId` (and `sessionId` when `HttpContext.Items["SessionId"]` is set); service-specific mappings via `IExceptionProblemDetailsMapper`.
- Call `AddTeamHubProblemDetails()` so ModelState / FluentValidation returns `ValidationProblemDetails` (`type` = `…/validation-failed`) with `correlationId`.
- Controllers returning intentional errors use `TeamHubProblemDetailsFactory` (same shape as middleware).
- Clients should switch on `type`, not only HTTP status.
- `UserIdLoggingMiddleware`: authenticated JWT `NameIdentifier` / `sub` → `LogContext.UserId`.
- Error type catalog: `docs/errors.mb`.

## Don't
- Do not add domain/business logic here (keep mappers in the owning service).
- Do not duplicate these middlewares inside individual services.
- Do not bypass OTLP collector for production log shipping when monitoring stack is enabled.
- Do not use `https://httpstatuses.com/{code}` as `type` (too coarse for clients).
