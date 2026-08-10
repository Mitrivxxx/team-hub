# Team Hub - AGENTS

## Cel
- Dzialaj krotko, pewnie, bez zgadywania.
- Minimalne zmiany, maksymalna zgodnosc z repo.

## Zakres projektu
- `aspire/TeamHub.AppHost` - lokalny orchestrator .NET Aspire (dev).
- `aspire/TeamHub.ServiceDefaults` - wspolne service discovery i health dla serwisow .NET.
- `services/team-hub-auth` - API auth (.NET, EF Core, JWT, cookies) + internal gRPC profiles.
- `services/team-hub-gateway` - reverse proxy (YARP).
- `services/team-hub-organization` - API organization (.NET Web API) + internal gRPC members.
- `services/team-hub-notification` - API notifications (.NET Web API; Kafka consumer for org member-added).
- `services/team-hub-bff` - GraphQL BFF (Hot Chocolate) aggregating auth + organization over gRPC.
- `frontend/team-hub-web` - UI (Angular).
- `infrastructure/nginx` - edge reverse proxy (nginx) before gateway.
- `infrastructure/redis` - shared Redis container (compose).
- `infrastructure/kafka` - shared Kafka container (compose, `msg-kafka`).
- `building-blocks/TeamHub.Redis` - shared Redis .NET library.
- `building-blocks/TeamHub.Kafka` - shared Kafka producer/consumer + event contracts.
- `building-blocks/TeamHub.BlobStorage` - shared Azure Blob Storage .NET library.
- `building-blocks/TeamHub.DemoSeed` - shared deterministic demo user identity factory (auth/organization seed).
- `building-blocks/TeamHub.Observability` - shared OTEL/Serilog + Exception/CorrelationId/UserIdLogging middleware.
- `building-blocks/TeamHub.GrpcContracts` - shared gRPC protobuf contracts.
- `docker-compose.yml` - prod/pre-prod runtime (nginx, gateway, auth, organization, notification, bff, postgres, kafka).
- `TeamHub.sln` - solution file; prefer `aspire/TeamHub.AppHost` for local full-stack dev.

## Zasady glowna
- Najpierw sprawdz kod i config, potem zmieniaj.
- Nie wymyslaj endpointow/portow/uslug.
- Frontend gada przez infrastructure nginx i gateway (`/api/auth/*`, `/api/organizations/*`, `/api/notifications/*`), nie bezposrednio z serwisami.
- Nazwy runtime (Docker Compose / Aspire): wzorzec `[typ]-[modul]` — np. `srv-auth`, `srv-organization`, `srv-notification`, `gw-api`, `gw-nginx`, `ui-web`, `db-postgres`, `cache-redis`, `msg-kafka`, `mon-otel`. Foldery kodu pozostaja `services/team-hub-*`.
- Aktualizuj dokumentacje po zmianie kontraktu, flow, configu.

## Obowiazkowe pliki komponentow
- W kazdym mikroserwisie i we frontendzie musi byc `AGENT.md`.
- Jesli `AGENT.md` nie istnieje, utworz go przy pierwszej pracy.
- Po kazdej zmianie funkcjonalnej zaktualizuj `AGENT.md`, aby byl spojny z aktualnym stanem komponentu.

## Styl pracy
- Uzywaj mozliwie najmniej slow.
- Opisuj tylko rzeczy potrzebne do wykonania pracy.
- Bez fuszerki: kod, testy i docs maja byc spojne.

## Dokumentacja kodu
- Wszystkie opisy w projekcie maja byc po angielsku (w tym `AGENT.md`)
- Zawsze używaj Serilog.
- Opisuj każdy endpoint za pomocą XML, używaj tylko <summary> i jak najmniej tekstu.