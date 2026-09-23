#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "Ús: $0 RUTA_SORTIDA.sql" >&2
  exit 2
fi

output_path="$1"
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

dotnet ef migrations script \
  --idempotent \
  --project "$repo_root/src/Backend/Infrastructure/Infrastructure.csproj" \
  --startup-project "$repo_root/src/Backend/Api/Api.csproj" \
  --context ZuppetoDbContext \
  --output "$output_path"

echo "Script EF Core idempotent generat a $output_path"
