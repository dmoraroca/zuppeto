#!/usr/bin/env bash
# Després que l'API hagi compilat dins Docker, bin/obj poden quedar com a root al volum muntat.
# Això trenca dotnet restore/build a l'host i el carregament del projecte a Cursor.
# Executa des del directori arrel del repo (demana contrasenya sudo una vegada):
#   bash scripts/fix-backend-perms-after-docker.sh

set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

echo "Esborrant bin/obj/bin-local/obj-local de src/Backend (cal sudo si són de root)..."
sudo rm -rf src/Backend/*/obj src/Backend/*/bin src/Backend/*/obj-local src/Backend/*/bin-local

echo "Restaurant propietari de src/Backend..."
sudo chown -R "$(whoami):$(whoami)" src/Backend

echo "dotnet restore + build..."
dotnet restore Zuppeto.sln
dotnet build Zuppeto.sln -c Debug

echo "Fet. Prova Reload Window a Cursor si cal."
