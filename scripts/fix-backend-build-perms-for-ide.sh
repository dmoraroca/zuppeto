#!/usr/bin/env bash
# Soft permission heal for local F5. Never fails the IDE preLaunchTask.
# Prefer a writable host tree; optionally repair via Docker when the socket is usable.
set -u

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BACKEND="${ROOT}/src/Backend"
UID_GID="$(id -u):$(id -g)"

writable() {
  local dir="$1"
  mkdir -p "$dir" 2>/dev/null || return 1
  touch "${dir}/.write-test" 2>/dev/null || return 1
  rm -f "${dir}/.write-test" 2>/dev/null || true
  return 0
}

need_fix=0
for proj in Api Application Domain Infrastructure; do
  for kind in obj-local bin-local; do
    if ! writable "${BACKEND}/${proj}/${kind}"; then
      need_fix=1
      break 2
    fi
  done
done

if [[ "${need_fix}" -eq 0 ]]; then
  exit 0
fi

run_docker_chown() {
  docker run --rm -v "${BACKEND}:/backend" alpine \
    sh -c "chown -R ${UID_GID} /backend/*/obj-local /backend/*/bin-local 2>/dev/null || true"
}

if command -v docker >/dev/null 2>&1; then
  if run_docker_chown 2>/dev/null; then
    exit 0
  fi
  if command -v sg >/dev/null 2>&1; then
    sg docker -c "docker run --rm -v '${BACKEND}:/backend' alpine sh -c 'chown -R ${UID_GID} /backend/*/obj-local /backend/*/bin-local 2>/dev/null || true'" 2>/dev/null || true
  fi
fi

# Never block F5; anonymous Docker volumes + manual scripts remain the durable fix.
exit 0
