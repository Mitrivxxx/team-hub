## Purpose
- Shared .NET libraries reused by Team Hub microservices.

## Components
- `building-blocks/TeamHub.Redis` — Redis connection bootstrap (`TeamHub.Redis`).
- `building-blocks/TeamHub.Kafka` — Kafka producer/consumer bootstrap + shared events (`TeamHub.Kafka`).
- `building-blocks/TeamHub.BlobStorage` — Azure Blob Storage bootstrap (`TeamHub.BlobStorage`).
- `building-blocks/TeamHub.Observability` — OpenTelemetry tracing/metrics and Serilog bootstrap (`TeamHub.Observability`).
- `building-blocks/TeamHub.GrpcContracts` — Shared gRPC protobuf contracts (`TeamHub.GrpcContracts`).
- `building-blocks/TeamHub.DemoSeed` — Deterministic demo user identity factory for auth/organization seeders (`TeamHub.DemoSeed`).

## Do
- Keep building blocks free of domain/business logic.
- Add a component `AGENT.md` for each library under this folder.
- Reference libraries from services via `ProjectReference`.

## Don't
- Do not add infrastructure runtime config (Docker, nginx, compose) here.
- Do not put service-specific session or API logic in building blocks.
