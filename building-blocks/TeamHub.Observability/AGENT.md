## Purpose
- Shared OpenTelemetry and Serilog bootstrap for Team Hub microservices.

## Source of truth
- `ObservabilityOptions.cs` — `ServiceName`, `OtlpEndpoint` (`Observability` config section).
- `ServiceCollectionExtensions.cs` — `AddTeamHubOpenTelemetry`.
- `HostBuilderExtensions.cs` — `AddTeamHubSerilog` (Span enricher, config-driven sinks).
- `WebApplicationExtensions.cs` — `MapTeamHubObservabilityEndpoints` (`/metrics`).

## Do
- Register via `AddTeamHubOpenTelemetry(configuration, serviceName, includeEntityFrameworkCore)`.
- Use `includeEntityFrameworkCore: true` only when the service uses EF Core.
- Push traces via OTLP (`Observability:OtlpEndpoint` or `OTEL_EXPORTER_OTLP_ENDPOINT`).
- Expose Prometheus scrape at `/metrics`.
- Enrich logs with `TraceId`, `SpanId` (`Serilog.Enrichers.Span`); push prod logs via `Serilog.Sinks.OpenTelemetry`.
- Exclude `/health` and `/metrics` from ASP.NET Core trace instrumentation.

## Don't
- Do not add service-specific middleware or business logic here.
- Do not bypass OTLP collector for production log shipping when monitoring stack is enabled.
