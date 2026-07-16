## Purpose
- Shared .NET libraries reused by Team Hub microservices.

## Components
- `building-blocks/TeamHub.Redis` — Redis connection bootstrap (`TeamHub.Redis`).
- `building-blocks/TeamHub.Observability` — OpenTelemetry tracing/metrics and Serilog bootstrap (`TeamHub.Observability`).

## Do
- Keep building blocks free of domain/business logic.
- Add a component `AGENT.md` for each library under this folder.
- Reference libraries from services via `ProjectReference`.

## Don't
- Do not add infrastructure runtime config (Docker, nginx, compose) here.
- Do not put service-specific session or API logic in building blocks.
