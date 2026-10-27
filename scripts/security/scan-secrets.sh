#!/usr/bin/env bash
set -Eeuo pipefail

mode="${1:---staged}"
repository_root="$(git rev-parse --show-toplevel)"
cd "$repository_root"

scan_root="$repository_root"
temporary_scan_root=""
cleanup() {
  if [[ -n "$temporary_scan_root" && -d "$temporary_scan_root" ]]; then
    rm -rf -- "$temporary_scan_root"
  fi
}
trap cleanup EXIT

case "$mode" in
  --staged)
    command=(git --pre-commit --redact --staged --verbose)
    ;;
  --history)
    command=(git . --redact --verbose --log-opts=--all)
    ;;
  --working-tree)
    temporary_scan_root="$(mktemp -d)"
    git ls-files --cached --others --exclude-standard -z |
      tar --null --files-from=- --create |
      tar --extract --directory="$temporary_scan_root"
    scan_root="$temporary_scan_root"
    command=(dir . --redact --verbose)
    ;;
  *)
    echo "Ús: $0 [--staged|--history|--working-tree]" >&2
    exit 64
    ;;
esac

if command -v gitleaks >/dev/null 2>&1; then
  (cd "$scan_root" && gitleaks "${command[@]}")
  exit $?
fi

if command -v docker >/dev/null 2>&1; then
  docker run --rm --network none \
    -v "$scan_root:/repo:ro" \
    -w /repo \
    ghcr.io/gitleaks/gitleaks@sha256:b109bc5f8f76a38196a3e413704fc5b9e3c32360bce4e4b603bd6f45b3721dbb \
    "${command[@]}"
  exit $?
fi

echo "No s'ha trobat ni Gitleaks ni Docker; el commit queda bloquejat." >&2
exit 2
