#!/usr/bin/env bash
# Compose DEV companion: stateful infra (Postgres/Redis/Kafka/Azurite) + monitoring.
# Prefer: ./scripts/compose-dev.sh up -d --wait
# Daily apps: ./scripts/dev-up.sh  |  seed: ./scripts/dev-seed.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

ENV_FILE="${ENV_FILE:-.env.dev}"
if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing $ENV_FILE — copy from .env.dev.example and adjust:"
  echo "  cp .env.dev.example .env.dev"
  exit 1
fi

# Order: monitoring base (sets project dir for ./ volume paths) → infra includes → overrides.
# Compose include cannot override imported services in the same file.
exec docker compose \
  -f infrastructure/monitoring/docker-compose.monitoring.yml \
  -f infrastructure/monitoring/docker-compose.dev.infra.yml \
  -f docker-compose.dev.yml \
  --env-file "$ENV_FILE" \
  "$@"
