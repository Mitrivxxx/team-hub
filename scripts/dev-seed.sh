#!/usr/bin/env bash
# Start compose-dev infra, then Aspire AppHost demo seed (exits when seed finishes).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

"$ROOT/scripts/compose-dev.sh" up -d --wait

cd "$ROOT/aspire/TeamHub.AppHost"
exec dotnet run -- --seed "$@"
