#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
fixture="$repo_root/e2e/fixtures/territorial-phase6-real.sql"
cleanup_fixture="$repo_root/e2e/fixtures/territorial-phase6-real-cleanup.sql"

cleanup() {
  docker compose -f "$repo_root/docker-compose.yml" exec -T db \
    psql -U app -d zuppeto -v ON_ERROR_STOP=1 -f /dev/stdin < "$cleanup_fixture"
}

assert_clean() {
  local residual
  residual="$(docker compose -f "$repo_root/docker-compose.yml" exec -T db \
    psql -U app -d zuppeto -Atc "
      SELECT
        (SELECT count(*) FROM countries WHERE id::text LIKE 'e2e00000-%') +
        (SELECT count(*) FROM territorial_dataset_sources WHERE id::text LIKE 'e2e00000-%') +
        (SELECT count(*) FROM territorial_imports WHERE dataset_source_id::text LIKE 'e2e00000-%') +
        (SELECT count(*) FROM territorial_units WHERE country_id::text LIKE 'e2e00000-%');")"
  [[ "$residual" == "0" ]] || { echo "Queden $residual registres territorials E2E." >&2; return 1; }
}

trap cleanup EXIT
cleanup
docker compose -f "$repo_root/docker-compose.yml" exec -T db \
  psql -U app -d zuppeto -v ON_ERROR_STOP=1 -f /dev/stdin < "$fixture"

(
  cd "$repo_root/e2e"
  E2E_TERRITORIAL_REAL=true \
  E2E_DEFER_EXCEL_SYNC=true \
  E2E_FAIL_ON_NON_PASS=true \
    npm run e2e:chrome -- --scenario=ZUP-160-ADMIN-principal
)

cleanup
trap - EXIT
assert_clean
