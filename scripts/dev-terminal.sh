#!/usr/bin/env bash
set -euo pipefail

SERVICE_NAME="${1:?service name required}"
shift

load_shell_env() {
  export NVM_DIR="${NVM_DIR:-$HOME/.nvm}"
  if [ -s "$NVM_DIR/nvm.sh" ]; then
    # shellcheck disable=SC1091
    . "$NVM_DIR/nvm.sh"
  fi
}

load_shell_env

echo "[$SERVICE_NAME] starting: $*"
"$@" &
pid=$!

echo ""
echo "[$SERVICE_NAME] server running (pid $pid, job %1)"
echo "[$SERVICE_NAME] stop server: kill %1"
echo "[$SERVICE_NAME] terminal is free for other commands"
echo ""

exec bash -i
