#!/usr/bin/env bash
set -Eeuo pipefail

mode="${1:-}"
case "$mode" in
  migrate|seed-development-data|initialize-development-data)
    ;;
  *)
    echo "Ús: $0 migrate|seed-development-data|initialize-development-data" >&2
    exit 64
    ;;
esac

repository_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repository_root"

if [[ "${ZUPPETO_DATABASE_MAINTENANCE_ALLOWED:-}" != "true" ]]; then
  echo "Operació bloquejada. Defineix ZUPPETO_DATABASE_MAINTENANCE_ALLOWED=true després d'obtenir autorització." >&2
  exit 77
fi

docker compose build api
docker compose run --rm --no-deps --entrypoint /bin/bash api -lc \
  "dotnet restore Zuppeto.sln --ignore-failed-sources && dotnet build src/Backend/Api/Api.csproj -c Debug --no-restore && cd src/Backend/Api && dotnet \"\${ZUPPETO_BUILD_ROOT}/Api/bin/Debug/net10.0/Api.dll\" --database-maintenance=$mode"
