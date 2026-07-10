#!/usr/bin/env bash
set -euo pipefail

CERTS_DIR="${1:-$(cd "$(dirname "$0")/.." && pwd)/certs}"
CERT_FILE="$CERTS_DIR/localhost.pem"
KEY_FILE="$CERTS_DIR/localhost-key.pem"

mkdir -p "$CERTS_DIR"

if [[ -f "$CERT_FILE" && -f "$KEY_FILE" ]] && [[ -s "$CERT_FILE" && -s "$KEY_FILE" ]]; then
  if openssl x509 -in "$CERT_FILE" -noout >/dev/null 2>&1; then
    echo "Certs already exist in $CERTS_DIR, skipping generation."
    exit 0
  fi
  echo "Existing certs in $CERTS_DIR are invalid, regenerating..."
  rm -f "$CERT_FILE" "$KEY_FILE"
fi

openssl req -x509 -nodes -days 825 -newkey rsa:2048 \
  -keyout "$KEY_FILE" \
  -out "$CERT_FILE" \
  -subj "/CN=localhost" \
  -addext "subjectAltName=DNS:localhost,DNS:*.localhost,IP:127.0.0.1,IP:::1"

chmod 644 "$CERT_FILE"
chmod 600 "$KEY_FILE"

echo "Generated self-signed TLS certs in $CERTS_DIR"
echo "Browsers will show a security warning until you run: ./scripts/setup-certs.sh"
