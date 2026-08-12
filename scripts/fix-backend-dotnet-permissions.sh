#!/usr/bin/env bash
# Restaura el propietari dels directoris obj/bin del backend (p. ex. després de builds Docker que els creen com a root).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
echo "Zuppeto: arreglant permisos a obj/bin/obj-local/bin-local sota ${ROOT}/src/Backend …"
if [[ "${1:-}" == "--sudo" ]] || [[ "${1:-}" == "-s" ]]; then
  sudo find "${ROOT}/src/Backend" -type d \( -name obj -o -name bin -o -name obj-local -o -name bin-local \) -prune -exec chown -R "$(id -u):$(id -g)" {} \;
else
  find "${ROOT}/src/Backend" -type d \( -name obj -o -name bin -o -name obj-local -o -name bin-local \) -prune -exec chown -R "$(id -u):$(id -g)" {} \; 2>/dev/null \
    || { echo "Ha fallat sense sudo. Prova: bash $0 --sudo"; exit 1; }
fi
echo "Fet. Ara: dotnet build Zuppeto.sln"
