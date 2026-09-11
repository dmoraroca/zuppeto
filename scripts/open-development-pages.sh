#!/usr/bin/env bash
# Obre les dues pàgines de desenvolupament amb el navegador predeterminat.

set -euo pipefail

readonly urls=(
  "http://localhost:4200"
  "http://localhost:5211/swagger/index.html"
)

# Amb aplicacions Flatpak, xdg-open pot retornar 0 sense arribar a obrir cap
# finestra. gtk-launch executa directament l'aplicació d'escriptori registrada.
if command -v xdg-settings >/dev/null 2>&1 \
  && command -v gtk-launch >/dev/null 2>&1; then
  default_browser="$(xdg-settings get default-web-browser 2>/dev/null || true)"

  if [[ -n "${default_browser}" ]]; then
    gtk-launch "${default_browser%.desktop}" "${urls[@]}"
    exit 0
  fi
fi

if command -v xdg-open >/dev/null 2>&1; then
  for url in "${urls[@]}"; do
    xdg-open "${url}"
  done
  exit 0
fi

echo "No s'ha trobat cap mecanisme per obrir el navegador predeterminat." >&2
exit 1
