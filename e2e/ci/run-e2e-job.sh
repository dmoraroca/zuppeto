#!/usr/bin/env bash
set -Eeuo pipefail

browser="${1:?Falta el navegador CI}"
profile="${2:?Falta el perfil CI}"
status_file=".runs/ci/services-${browser}-${profile}.txt"
mkdir -p .runs/ci
compose=(docker compose -f ../docker-compose.yml -f ci/docker-compose.ci.yml)

finish() {
  "${compose[@]}" ps --format json > "$status_file" 2>/dev/null || true
  "${compose[@]}" down --volumes --remove-orphans >/dev/null 2>&1 || true
}
trap finish EXIT

"${compose[@]}" build api web
"${compose[@]}" up -d --wait --wait-timeout 300 db rabbitmq
"${compose[@]}" run --rm --no-deps --entrypoint /bin/bash api -lc \
  'dotnet restore Zuppeto.sln --ignore-failed-sources && dotnet build src/Backend/Api/Api.csproj -c Debug --no-restore && cd src/Backend/Api && dotnet "${ZUPPETO_BUILD_ROOT}/Api/bin/Debug/net10.0/Api.dll" --database-maintenance=initialize-development-data'
"${compose[@]}" up -d --wait --wait-timeout 300 api web
npm run ci:accounts
npm run "e2e:${browser}" -- "--profile=${profile}"
