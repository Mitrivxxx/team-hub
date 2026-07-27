#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
LOG_DIR="$ROOT/.logs"

WEB_DIR="$ROOT/frontend/team-hub-web"
GATEWAY_DIR="$ROOT/services/team-hub-gateway/team-hub-gateway"
AUTH_DIR="$ROOT/services/team-hub-auth/team-hub-auth"
TEAM_DIR="$ROOT/services/team-hub-organization/team-hub-organization"

WEB_CMD="cd \"$WEB_DIR\" && exec bash -i"
GATEWAY_CMD="cd \"$GATEWAY_DIR\" && exec bash -i"
AUTH_CMD="cd \"$AUTH_DIR\" && exec bash -i"
TEAM_CMD="cd \"$TEAM_DIR\" && exec bash -i"

is_vscode_terminal() {
  [ "${TERM_PROGRAM:-}" = "vscode" ] || [ -n "${VSCODE_IPC_HOOK_CLI:-}" ]
}

run_vscode_tasks() {
  echo "Ten skrypt nie tworzy terminali bezposrednio w panelu VS Code/Cursor."
  echo "Uruchom task 'dev-local' (otwiera czyste terminale w odpowiednich katalogach):"
  echo "  Ctrl+Shift+P -> Tasks: Run Task -> dev-local"
  echo ""
  echo "Powinny pojawic sie 4 terminale: web, auth, gateway, team."
}

run_tmux() {
  local session="team-hub-dev"

  if [ -n "${TMUX:-}" ]; then
    tmux new-window -n web "bash -lc '$WEB_CMD; exec bash'"
    tmux new-window -n gateway "bash -lc '$GATEWAY_CMD; exec bash'"
    tmux new-window -n auth "bash -lc '$AUTH_CMD; exec bash'"
    tmux new-window -n team "bash -lc '$TEAM_CMD; exec bash'"
    tmux select-window -t web
    echo "Started in current tmux session: web, gateway, auth, team."
    return 0
  fi

  if tmux has-session -t "$session" 2>/dev/null; then
    echo "tmux session '$session' already exists. Attach with: tmux attach -t $session"
    return 0
  fi

  tmux new-session -d -s "$session" -n web "bash -lc '$WEB_CMD; exec bash'"
  tmux new-window -t "$session:" -n gateway "bash -lc '$GATEWAY_CMD; exec bash'"
  tmux new-window -t "$session:" -n auth "bash -lc '$AUTH_CMD; exec bash'"
  tmux new-window -t "$session:" -n team "bash -lc '$TEAM_CMD; exec bash'"
  tmux select-window -t "$session:web"
  tmux attach -t "$session"
}

run_gnome_terminal() {
  gnome-terminal \
    --tab --title="web" -- bash -lc "$WEB_CMD; exec bash" \
    --tab --title="gateway" -- bash -lc "$GATEWAY_CMD; exec bash" \
    --tab --title="auth" -- bash -lc "$AUTH_CMD; exec bash" \
    --tab --title="team" -- bash -lc "$TEAM_CMD; exec bash"
}

run_background() {
  echo "Brak tmux/gnome-terminal. Nie moge otworzyc wielu interaktywnych terminali."
  echo "Zainstaluj tmux lub gnome-terminal i uruchom ponownie."
  exit 1
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
