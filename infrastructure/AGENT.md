## Purpose
- Edge reverse proxy for API traffic before the gateway.

## Source of truth
- `infrastructure/nginx/nginx.conf`
- `infrastructure/nginx/Dockerfile`
- `infrastructure/nginx/docker-entrypoint.sh`
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

## Don't
- Do not add business logic or auth validation in nginx.
- Do not expose auth service directly from this container.
- Do not terminate TLS on gateway; TLS ends at nginx (and frontend UI).

## Listeners
- **Port 80 (internal):** HTTP proxy to gateway for `web` container.
- **Port 443 (public edge):** HTTPS with gzip, security headers, rate limiting.
