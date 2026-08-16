#!/usr/bin/env bash
set -euo pipefail

repo_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_dir"

project_ref="${SIGNAL_HUNT_SUPABASE_PROJECT_REF:-ivhfuhxorppptyuofbgq}"
expected_url="https://${project_ref}.supabase.co"

if [[ -z "${SUPABASE_ACCESS_TOKEN:-}" ]]; then
  echo "SUPABASE_ACCESS_TOKEN is required. Set a fresh token in your terminal; never paste it into chat." >&2
  exit 1
fi
if [[ -z "${SUPABASE_DB_PASSWORD:-}" ]]; then
  echo "SUPABASE_DB_PASSWORD is required for schema backup and database dry-run operations." >&2
  exit 1
fi
if [[ -n "${SUPABASE_URL:-}" && "${SUPABASE_URL%/}" != "$expected_url" ]]; then
  echo "Refusing preflight: SUPABASE_URL does not match the pinned project ref $project_ref." >&2
  exit 1
fi

for command_name in supabase jq docker; do
  if ! command -v "$command_name" >/dev/null 2>&1; then
    echo "Missing required command: $command_name" >&2
    exit 1
  fi
done
if ! docker info >/dev/null 2>&1; then
  echo "Docker must be running for the schema-only backup." >&2
  exit 1
fi

projects_json="$(supabase projects list --output json)"
project_name="$(jq -er --arg ref "$project_ref" '.[] | select(.id == $ref) | .name' <<<"$projects_json")" || {
  echo "The active Supabase access token cannot see project $project_ref. No remote operation was attempted." >&2
  exit 1
}
echo "Verified token scope for project: $project_name ($project_ref)"

# Linking only writes local CLI metadata. It does not change the remote schema.
supabase link --project-ref "$project_ref"
echo "Linked the local CLI to the pinned project ref. No migration has been applied."

echo "Remote migration history:"
supabase migration list --linked

echo "Linting the current remote public schema:"
supabase db lint --linked --schema public --level warning --fail-on error

backup_dir="$repo_dir/artifacts/supabase-backups"
mkdir -p "$backup_dir"
backup_path="$backup_dir/${project_ref}-public-$(date -u +%Y%m%dT%H%M%SZ).sql"
supabase db dump --linked --schema public --file "$backup_path"
if [[ ! -s "$backup_path" ]]; then
  echo "Schema backup was not created; refusing to continue." >&2
  exit 1
fi
echo "Saved a schema-only backup outside Git tracking: $backup_path"

echo "Migration dry run (this command does not apply changes):"
supabase db push --linked --dry-run

echo "Production preflight passed. No remote migration was applied."
