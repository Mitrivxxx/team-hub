# Team Hub Project Rules

## Purpose
- Keep agent changes small, safe, and consistent with current architecture.

## Project context
- Runtime flow: `frontend -> gateway -> auth -> postgres`.
- Services wired in repo: `team-hub-gateway`, `team-hub-auth`, `postgres`.

## Source of truth
- `docker-compose.yml`
- `services/team-hub-gateway/team-hub-gateway/reverseproxy.json`
- `services/team-hub-gateway/team-hub-gateway/appsettings.*.json`
- `services/team-hub-auth/team-hub-auth/Controllers/AuthController*.cs`
- `services/team-hub-auth/team-hub-auth/.env.example`
- `docs/README.mb` and related `docs/*.mb`

## Do
- Route auth API through gateway path `/api/auth/{**catch-all}`.
- Keep auth endpoints aligned with controllers: `register`, `login`, `refresh`, `logout`.
- Use documented ports/env from config (`7172`, `5112`, `5433`) and verify before edits.
- Update docs in `docs/` when API contract, ports, or runtime flow changes.
- Prefer minimal diffs and preserve existing naming/style in touched files.

## Don't
- Do not bypass gateway in frontend integration.
- Do not invent routes, env vars, or services not present in config/code.
- Do not run destructive git commands (`reset --hard`, forced history rewrites).
- Do not modify unrelated files outside the requested scope.

## Definition of done
- Change solves the requested task with smallest safe diff.
- Affected paths/config references are validated against source files.
- Relevant tests/checks are run when code behavior changes.
- Documentation is updated if behavior or contract changed.
