#!/usr/bin/env bash
# Start compose-dev infra (Postgres/Redis/Kafka/Azurite + monitoring), then Aspire AppHost apps.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

"$ROOT/scripts/compose-dev.sh" up -d --wait

cd "$ROOT/aspire/TeamHub.AppHost"
exec dotnet run "$@"
