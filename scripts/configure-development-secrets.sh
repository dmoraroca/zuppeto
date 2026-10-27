#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
api_project="$repo_root/src/Backend/Api/Api.csproj"

store_secret() {
  local key="$1"
  local value="$2"
  local encoded
  encoded="$(printf '%s' "$value" | jq -Rs .)"
  printf '{"%s":%s}\n' "$key" "$encoded" |
    dotnet user-secrets set --project "$api_project" >/dev/null
}

read_secret() {
  local prompt="$1"
  local target_variable="$2"
  local value
  read -r -s -p "$prompt: " value
  printf '\n'
  if [[ -z "$value" ]]; then
    printf 'No es permet un valor buit per a %s.\n' "$prompt" >&2
    exit 1
  fi
  printf -v "$target_variable" '%s' "$value"
}

read_optional_secret() {
  local prompt="$1"
  local target_variable="$2"
  local value
  read -r -s -p "$prompt (opcional, Enter per ometre): " value
  printf '\n'
  printf -v "$target_variable" '%s' "$value"
}

command -v dotnet >/dev/null || { printf 'No s’ha trobat dotnet.\n' >&2; exit 1; }
command -v jq >/dev/null || { printf 'No s’ha trobat jq.\n' >&2; exit 1; }
command -v openssl >/dev/null || { printf 'No s’ha trobat openssl.\n' >&2; exit 1; }

printf 'Els valors s’introduiran ocults i es desaran a .NET User Secrets; no s’escriuran al repositori.\n'
read_secret 'Cadena de connexió local de Zuppeto' database_connection
read_secret 'Contrasenya local de RabbitMQ' rabbit_password
read_optional_secret 'Contrasenya ADMIN del seeder explícit' seed_admin_password
read_optional_secret 'Contrasenya Developer del seeder explícit' seed_developer_password
jwt_signing_key="$(openssl rand -base64 48)"

store_secret 'ConnectionStrings:Zuppeto' "$database_connection"
store_secret 'Auth:Jwt:SigningKey' "$jwt_signing_key"
store_secret 'RabbitMq:Password' "$rabbit_password"
if [[ -n "$seed_admin_password" ]]; then
  store_secret 'DevelopmentIdentitySeed:AdminPassword' "$seed_admin_password"
fi
if [[ -n "$seed_developer_password" ]]; then
  store_secret 'DevelopmentIdentitySeed:DeveloperPassword' "$seed_developer_password"
fi

unset database_connection rabbit_password seed_admin_password seed_developer_password jwt_signing_key
printf 'Configuració local desada. No s’ha mostrat cap secret.\n'
