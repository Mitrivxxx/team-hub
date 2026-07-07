#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
LOG_DIR="$ROOT/.logs"

WEB_DIR="$ROOT/frontend/team-hub-web"
GATEWAY_DIR="$ROOT/services/team-hub-gateway/team-hub-gateway"
AUTH_DIR="$ROOT/services/team-hub-auth/team-hub-auth"

DEV_TERMINAL="$ROOT/scripts/dev-terminal.sh"

WEB_CMD="cd \"$WEB_DIR\" && \"$DEV_TERMINAL\" web ./node_modules/.bin/ng serve"
GATEWAY_CMD="cd \"$GATEWAY_DIR\" && \"$DEV_TERMINAL\" gateway dotnet run"
AUTH_CMD="cd \"$AUTH_DIR\" && \"$DEV_TERMINAL\" auth dotnet run"

is_vscode_terminal() {
  [ "${TERM_PROGRAM:-}" = "vscode" ] || [ -n "${VSCODE_IPC_HOOK_CLI:-}" ]
}

run_vscode_tasks() {
  echo "Uruchom task 'dev-local' w VS Code/Cursor:"
  echo "  Ctrl+Shift+B"
  echo "  lub: Ctrl+Shift+P -> Tasks: Run Task -> dev-local"
  echo ""
  echo "Powinny pojawic sie 3 terminale: web, auth, gateway."
}

run_tmux() {
  local session="team-hub-dev"

  if [ -n "${TMUX:-}" ]; then
    tmux new-window -n web "bash -lc '$WEB_CMD; exec bash'"
    tmux new-window -n gateway "bash -lc '$GATEWAY_CMD; exec bash'"
    tmux new-window -n auth "bash -lc '$AUTH_CMD; exec bash'"
    tmux select-window -t web
    echo "Started in current tmux session: web, gateway, auth."
    return 0
  fi

  if tmux has-session -t "$session" 2>/dev/null; then
    echo "tmux session '$session' already exists. Attach with: tmux attach -t $session"
    return 0
  fi

  tmux new-session -d -s "$session" -n web "bash -lc '$WEB_CMD; exec bash'"
  tmux new-window -t "$session:" -n gateway "bash -lc '$GATEWAY_CMD; exec bash'"
  tmux new-window -t "$session:" -n auth "bash -lc '$AUTH_CMD; exec bash'"
  tmux select-window -t "$session:web"
  tmux attach -t "$session"
}

run_gnome_terminal() {
  gnome-terminal \
    --tab --title="web" -- bash -lc "$WEB_CMD; exec bash" \
    --tab --title="gateway" -- bash -lc "$GATEWAY_CMD; exec bash" \
    --tab --title="auth" -- bash -lc "$AUTH_CMD; exec bash"
}

run_background() {
  mkdir -p "$LOG_DIR"
  nohup bash -lc "$WEB_CMD" > "$LOG_DIR/web.log" 2>&1 &
  nohup bash -lc "$GATEWAY_CMD" > "$LOG_DIR/gateway.log" 2>&1 &
  nohup bash -lc "$AUTH_CMD" > "$LOG_DIR/auth.log" 2>&1 &

  echo "No terminal multiplexer found. Started in background."
  echo "Logs:"
  echo "  $LOG_DIR/web.log"
  echo "  $LOG_DIR/gateway.log"
  echo "  $LOG_DIR/auth.log"
}

if is_vscode_terminal; then
  run_vscode_tasks
  exit 0
fi

if command -v tmux >/dev/null 2>&1; then
  run_tmux
  exit 0
fi

if command -v gnome-terminal >/dev/null 2>&1; then
  run_gnome_terminal
  exit 0
fi

run_background
