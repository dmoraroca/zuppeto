#!/usr/bin/env bash
# Restaura el propietari dels directoris obj/bin del backend (p. ex. després de builds Docker que els creen com a root).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
echo "YepPet: arreglant permisos a obj/ i bin/ sota ${ROOT}/src/Backend …"
if [[ "${1:-}" == "--sudo" ]] || [[ "${1:-}" == "-s" ]]; then
  sudo find "${ROOT}/src/Backend" -type d \( -name obj -o -name bin \) -prune -exec chown -R "$(id -u):$(id -g)" {} \;
else
  find "${ROOT}/src/Backend" -type d \( -name obj -o -name bin \) -prune -exec chown -R "$(id -u):$(id -g)" {} \; 2>/dev/null \
    || { echo "Ha fallat sense sudo. Prova: bash $0 --sudo"; exit 1; }
fi
echo "Fet. Ara: dotnet build YepPet.sln"
