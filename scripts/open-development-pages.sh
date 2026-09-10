#!/usr/bin/env bash
# Obre les dues pàgines de desenvolupament amb el navegador predeterminat.

set -euo pipefail

open_url() {
  local url="$1"
  nohup xdg-open "${url}" >/dev/null 2>&1 &
}

if ! command -v xdg-open >/dev/null 2>&1; then
  echo "No s'ha trobat xdg-open per obrir el navegador predeterminat." >&2
  exit 1
fi

open_url "http://localhost:4200"
open_url "http://localhost:5211/swagger/index.html"

