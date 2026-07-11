## Purpose
- Edge reverse proxy and shared infrastructure config for Team Hub.

## Source of truth
- `infrastructure/nginx/nginx.conf`
- `infrastructure/nginx/Dockerfile`
- `infrastructure/nginx/docker-entrypoint.sh`
- `infrastructure/redis/docker-compose.redis.yml`
- `infrastructure/redis/.env.example`
- `infrastructure/team-hub-redis/*`
- `docker-compose.yml`

## Do
- Terminate HTTPS on container port `443` (host `8080`).
- Serve internal HTTP on port `80` for docker network traffic (`web` -> `http://nginx:80`).
- Proxy `/api/*` and `/health` to `http://gateway:8080`.
- Forward proxy headers: `Host`, `X-Real-IP`, `X-Forwarded-For`, `X-Forwarded-Proto`, `X-Forwarded-Host`.
- Keep `client_max_body_size 10m`, HTTP/1.1 keep-alive to upstream, and proxy timeouts aligned with gateway.
- On HTTPS listener (`443`): enable gzip, security headers, and rate limiting on `/api/`.
- Mount TLS certs from `./certs` to `/etc/nginx/certs`.
- Treat flow as: frontend -> infrastructure nginx -> gateway (HTTP) -> auth.
- Run one shared Redis instance via `infrastructure/redis/docker-compose.redis.yml` (included by root compose files).
- Use `TeamHub.Redis` for service-side Redis connection bootstrap.

## Don't
- Do not add business logic or auth validation in nginx.
- Do not expose auth service directly from this container.
- Do not terminate TLS on gateway; TLS ends at nginx (and frontend UI).
- Do not add per-service Redis containers or connection bootstrap outside `team-hub-redis`.

## Listeners
- **Port 80 (internal):** HTTP proxy to gateway for `web` container.
- **Port 443 (public edge):** HTTPS with gzip, security headers, rate limiting.

## Redis
- Dev container: `team-hub-redis-dev` (`docker-compose.dev.yml`)
- Prod container: `team-hub-redis-prod` (`docker-compose.yml`)
- Host port: `6379`
- Docker network: `redis:6379`
