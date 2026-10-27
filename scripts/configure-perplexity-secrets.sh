#!/usr/bin/env bash
set -Eeuo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
api_project="$repository_root/src/Backend/Api/Api.csproj"
docker_secret_directory="$repository_root/.secrets"
docker_secret_file="$docker_secret_directory/perplexity_api_key"

command -v dotnet >/dev/null || { printf 'No s’ha trobat dotnet.\n' >&2; exit 1; }
command -v jq >/dev/null || { printf 'No s’ha trobat jq.\n' >&2; exit 1; }

if ! git -C "$repository_root" check-ignore -q .secrets/perplexity_api_key; then
  printf 'S’atura: el fitxer local del secret no està exclòs de Git.\n' >&2
  exit 1
fi

printf 'Enganxa la clau NOVA de Perplexity des de Bitwarden. L’entrada queda oculta.\n'
IFS= read -r -s -p 'Clau nova de Perplexity: ' perplexity_api_key
printf '\n'

if [[ -z "$perplexity_api_key" ]]; then
  printf 'No es permet una clau buida.\n' >&2
  exit 1
fi
if [[ "$perplexity_api_key" == *$'\n'* || "$perplexity_api_key" == *$'\r'* ]]; then
  printf 'La clau no pot contenir salts de línia.\n' >&2
  exit 1
fi

encoded_secret="$(printf '%s' "$perplexity_api_key" | jq -Rs .)"
printf '{"Perplexity:ApiKey":%s}\n' "$encoded_secret" |
  dotnet user-secrets set --project "$api_project" >/dev/null

umask 077
mkdir -p "$docker_secret_directory"
temporary_file="$(mktemp "$docker_secret_directory/.perplexity-api-key.XXXXXX")"
trap 'rm -f -- "$temporary_file"' EXIT
printf '%s' "$perplexity_api_key" >"$temporary_file"
chmod 600 "$temporary_file"
mv -f -- "$temporary_file" "$docker_secret_file"
chmod 600 "$docker_secret_file"

unset perplexity_api_key encoded_secret
printf 'Clau desada a .NET User Secrets i al fitxer local de Docker Secrets.\n'
printf 'Perplexity continua desactivat; no s’ha executat cap petició externa.\n'
