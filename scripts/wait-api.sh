#!/usr/bin/env bash
# Espera que l'API respongui a http://127.0.0.1:5211/health/db (mateixa lògica que la tasca VS Code).
#   ./scripts/wait-api.sh

set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
exec bash "$ROOT/.vscode/wait-api-ready.sh"
