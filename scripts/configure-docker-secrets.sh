#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
env_file="$repo_root/.env"

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
  if [[ "$value" == *$'\n'* || "$value" == *$'\r'* ]]; then
    printf 'El valor de %s no pot contenir salts de línia.\n' "$prompt" >&2
    exit 1
  fi
  if [[ ! "$value" =~ ^[A-Za-z0-9._~!@%+=:,/-]+$ ]]; then
    printf 'El valor de %s conté caràcters no segurs per al format .env.\n' "$prompt" >&2
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
  if [[ "$value" == *$'\n'* || "$value" == *$'\r'* ]]; then
    printf 'El valor de %s no pot contenir salts de línia.\n' "$prompt" >&2
    exit 1
  fi
  if [[ -n "$value" && ! "$value" =~ ^[A-Za-z0-9._~!@%+=:,/-]+$ ]]; then
    printf 'El valor de %s conté caràcters no segurs per al format .env.\n' "$prompt" >&2
    exit 1
  fi
  printf -v "$target_variable" '%s' "$value"
}

set_env_value() {
  local key="$1"
  local value="$2"
  local temporary_file="$3"
  local replaced=false
  : >"$temporary_file"
  if [[ -f "$env_file" ]]; then
    while IFS= read -r line || [[ -n "$line" ]]; do
      if [[ "$line" == "$key="* ]]; then
        printf '%s=%s\n' "$key" "$value" >>"$temporary_file"
        replaced=true
      else
        printf '%s\n' "$line" >>"$temporary_file"
      fi
    done <"$env_file"
  fi
  if [[ "$replaced" == false ]]; then
    printf '%s=%s\n' "$key" "$value" >>"$temporary_file"
  fi
  mv "$temporary_file" "$env_file"
  chmod 600 "$env_file"
}

command -v openssl >/dev/null || { printf 'No s’ha trobat openssl.\n' >&2; exit 1; }
if ! git -C "$repo_root" check-ignore -q .env; then
  printf 'S’atura: .env no està exclòs de Git.\n' >&2
  exit 1
fi

printf 'No es copiarà cap credencial versionada. Introdueix valors locals vàlids i coordinats amb els volums existents.\n'
read_secret 'Contrasenya local de PostgreSQL' postgres_password
read_secret 'Contrasenya local de RabbitMQ' rabbit_password
read_optional_secret 'Contrasenya ADMIN del seeder explícit' seed_admin_password
read_optional_secret 'Contrasenya Developer del seeder explícit' seed_developer_password
jwt_signing_key="$(openssl rand -base64 48)"

temporary_file="$(mktemp "$repo_root/.env.tmp.XXXXXX")"
chmod 600 "$temporary_file"
trap 'rm -f "$temporary_file"' EXIT
set_env_value POSTGRES_PASSWORD "$postgres_password" "$temporary_file"
temporary_file="$(mktemp "$repo_root/.env.tmp.XXXXXX")"; chmod 600 "$temporary_file"
set_env_value RABBITMQ_PASSWORD "$rabbit_password" "$temporary_file"
temporary_file="$(mktemp "$repo_root/.env.tmp.XXXXXX")"; chmod 600 "$temporary_file"
set_env_value JWT_SIGNING_KEY "$jwt_signing_key" "$temporary_file"
temporary_file="$(mktemp "$repo_root/.env.tmp.XXXXXX")"; chmod 600 "$temporary_file"
set_env_value E2E_ADMIN_PASSWORD "$seed_admin_password" "$temporary_file"
temporary_file="$(mktemp "$repo_root/.env.tmp.XXXXXX")"; chmod 600 "$temporary_file"
set_env_value E2E_DEVELOPER_PASSWORD "$seed_developer_password" "$temporary_file"

unset postgres_password rabbit_password seed_admin_password seed_developer_password jwt_signing_key
printf 'Fitxer .env local actualitzat amb permisos 600. No s’ha mostrat cap secret.\n'
