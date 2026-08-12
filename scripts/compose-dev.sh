#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

ENV_FILE="${ENV_FILE:-.env.dev}"
if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing $ENV_FILE — copy from .env.dev.example and adjust:"
  echo "  cp .env.dev.example .env.dev"
  exit 1
fi

# Monitoring base + Aspire companion overrides (include cannot override imported services).
exec docker compose \
  -f infrastructure/monitoring/docker-compose.monitoring.yml \
  -f docker-compose.dev.yml \
  --env-file "$ENV_FILE" \
  "$@"
