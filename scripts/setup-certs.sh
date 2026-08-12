#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
CERTS_DIR="$ROOT/certs"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

mkdir -p "$CERTS_DIR"

if command -v mkcert >/dev/null 2>&1; then
  mkcert -install
  mkcert -cert-file "$CERTS_DIR/localhost.pem" -key-file "$CERTS_DIR/localhost-key.pem" localhost 127.0.0.1 ::1
  chmod 644 "$CERTS_DIR/localhost.pem"
  chmod 600 "$CERTS_DIR/localhost-key.pem"
  echo "Browser-trusted certs written to $CERTS_DIR (mkcert)."
  echo "Then restart Aspire AppHost or staging: ./scripts/compose-staging.sh up --build gw-nginx ui-web"
  exit 0
fi

echo "mkcert not found — generating self-signed certs."
echo "Install mkcert for browser-trusted local HTTPS: https://github.com/FiloSottile/mkcert"
"$SCRIPT_DIR/generate-certs.sh" "$CERTS_DIR"
