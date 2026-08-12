#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

ENV_FILE="${ENV_FILE:-.env.staging}"
if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing $ENV_FILE — copy from .env.staging.example and replace CHANGE_ME values:"
  echo "  cp .env.staging.example .env.staging"
  exit 1
fi

exec docker compose \
  -f docker-compose.yml \
  -f docker-compose.staging.yml \
  --env-file "$ENV_FILE" \
  "$@"
