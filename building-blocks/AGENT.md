## Purpose
- Shared .NET libraries reused by Team Hub microservices.

## Components
- `building-blocks/team-hub-redis` — Redis connection bootstrap (`TeamHub.Redis`).

## Do
- Keep building blocks free of domain/business logic.
- Add a component `AGENT.md` for each library under this folder.
- Reference libraries from services via `ProjectReference`.

## Don't
- Do not add infrastructure runtime config (Docker, nginx, compose) here.
- Do not put service-specific session or API logic in building blocks.
