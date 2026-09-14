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

"${compose[@]}" up -d --build --wait --wait-timeout 300 db rabbitmq api web
npm run ci:accounts
npm run "e2e:${browser}" -- "--profile=${profile}"
