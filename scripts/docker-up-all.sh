#!/usr/bin/env bash
# Aixeca db, RabbitMQ, API i web (mateix conjunt que la tasca VS Code "docker up all").
#   ./scripts/docker-up-all.sh

set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

docker compose up -d db rabbitmq api web
