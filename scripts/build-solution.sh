#!/usr/bin/env bash
# Restaura paquets i compila tota la solució (host).
#   Des de l'arrel del repo: ./scripts/build-solution.sh
#   Des de qualsevol lloc:   bash /ruta/al/repo/scripts/build-solution.sh

set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

dotnet restore YepPet.sln
dotnet build YepPet.sln -c Debug "$@"
