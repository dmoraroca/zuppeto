#!/usr/bin/env bash
# Aixeca db, RabbitMQ, API i web (mateix conjunt que la tasca VS Code "docker up all").
#   ./scripts/docker-up-all.sh

set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

docker compose up -d db rabbitmq
api_was_running="$(docker inspect -f '{{.State.Running}}' zuppeto-api 2>/dev/null || echo false)"
docker compose up -d --no-deps api

# Evita recrear l'API a cada F5. Si el contenidor ja estava viu, només el
# reinicia quan algun fitxer de backend és més nou que l'assembly carregat.
# Un contenidor nou ja executa restore/build/migrate des del seu entrypoint.
if [[ "${api_was_running}" == "true" ]]; then
  changed_backend="$({
    docker compose exec -T api bash -lc '
      artifact="${ZUPPETO_BUILD_ROOT}/Api/bin/Debug/net10.0/Api.dll"
      if [[ ! -f "${artifact}" ]]; then
        exit 0
      fi
      find /app/src/Backend /app/Zuppeto.sln /app/dotnet-tools.json \
        \( -path "*/obj" -o -path "*/obj-local" -o -path "*/bin" \
        -o -path "*/bin-local" -o -path "*/logs" -o -path "*/storage" \) \
        -prune -o -type f \
        \( -name "*.cs" -o -name "*.csproj" -o -name "*.props" \
        -o -name "*.targets" -o -name "*.json" -o -name "*.sln" \) \
        -newer "${artifact}" -print -quit
    ' 2>/dev/null
  } || true)"

  if [[ -n "${changed_backend}" ]]; then
    echo "Canvis de backend detectats; reiniciant només l'API..."
    docker compose restart api
  else
    echo "API sense canvis; es reutilitza el procés actiu."
  fi
fi

docker compose up -d --no-deps web
