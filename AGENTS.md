# Team Hub - AGENTS

## Cel
- Dzialaj krotko, pewnie, bez zgadywania.
- Minimalne zmiany, maksymalna zgodnosc z repo.

## Zakres projektu
- `services/team-hub-auth` - API auth (.NET, EF Core, JWT, cookies).
- `services/team-hub-gateway` - reverse proxy (YARP).
- `frontend/team-hub-web` - UI (Angular).
- `docker-compose.yml` - lokalny runtime (gateway, auth, postgres).

## Zasady glowna
- Najpierw sprawdz kod i config, potem zmieniaj.
- Nie wymyslaj endpointow/portow/uslug.
- Frontend gada przez gateway (`/api/auth/*`), nie bezposrednio z auth.
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