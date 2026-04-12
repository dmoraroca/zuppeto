#!/usr/bin/env bash
# Atura i elimina contenidors de la stack (docker compose down).
#   ./scripts/docker-down.sh

set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

docker compose down "$@"
