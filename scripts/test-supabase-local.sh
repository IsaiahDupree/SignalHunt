#!/usr/bin/env bash
set -euo pipefail

repo_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_dir"

for command_name in supabase curl jq node uuidgen; do
  if ! command -v "$command_name" >/dev/null 2>&1; then
    echo "Missing required command: $command_name" >&2
    exit 1
  fi
done

status_json="$(supabase status -o json 2>/dev/null)" || {
  echo "Local Supabase is not running. Start it with: supabase start" >&2
  exit 1
}
api_url="$(jq -er '.API_URL' <<<"$status_json")"
anon_key="$(jq -er '.ANON_KEY' <<<"$status_json")"

tmp_dir="$(mktemp -d "${TMPDIR:-/tmp}/signal-hunt-supabase.XXXXXX")"
trap 'rm -rf "$tmp_dir"' EXIT

request() {
  local method="$1"
  local url="$2"
  local token="$3"
  local body="${4:-}"
  local output_file="$5"
  local curl_args=(
    --silent --show-error
    --output "$output_file"
    --write-out '%{http_code}'
    --request "$method"
    --header "apikey: $anon_key"
    --header "Authorization: Bearer $token"
    --header 'Content-Type: application/json'
  )
  if [[ -n "$body" ]]; then
    curl_args+=(--data "$body")
  fi
  curl "${curl_args[@]}" "$url"
}

assert_status() {
  local actual="$1"
  local expected="$2"
  local label="$3"
  local output_file="$4"
  if [[ "$actual" != "$expected" ]]; then
    echo "$label failed: expected HTTP $expected, received $actual" >&2
    jq -c . "$output_file" 2>/dev/null >&2 || sed -n '1,20p' "$output_file" >&2
    exit 1
  fi
  echo "PASS  $label"
}

auth_file="$tmp_dir/auth.json"
auth_status="$(request POST "$api_url/auth/v1/signup" "$anon_key" '{}' "$auth_file")"
assert_status "$auth_status" 200 "real anonymous authentication" "$auth_file"
access_token="$(jq -er '.access_token' "$auth_file")"

date_key="$(date -u +%F)"
day_of_year="$((10#$(date -u +%j)))"
month="$((10#$(date -u +%m)))"
case "$month" in
  12|1|2) season="winter" ;;
  3|4|5) season="spring" ;;
  6|7|8) season="summer" ;;
  *) season="autumn" ;;
esac

if (( day_of_year % 2 == 0 )); then
  stage="island"
  theme="coastal"
  world_template="synthetic-island-v1"
  generation_version="island-generator-v1"
else
  stage="city"
  theme="neon"
  world_template="synthetic-town-v1"
  generation_version="town-generator-v1"
fi

identity="$date_key|$season|$theme|standard|$world_template"
read -r generation_seed display_seed < <(node -e '
  const value = process.argv[1];
  let hash = 2166136261 >>> 0;
  for (const character of value) {
    hash ^= character.charCodeAt(0);
    hash = Math.imul(hash, 16777619) >>> 0;
  }
  process.stdout.write(`${hash | 0} ${hash % 100000}\n`);
' "$identity")
challenge_id="${date_key}_${stage}_${theme}_standard_seed_$(printf '%05d' "$display_seed")"

ensure_body="$(jq -cn \
  --arg app_key 'signal-hunt' \
  --arg challenge_id "$challenge_id" \
  --arg date_key "$date_key" \
  --argjson generation_seed "$generation_seed" \
  --arg generation_version "$generation_version" \
  --arg world_template "$world_template" \
  '{
    p_app_key: $app_key,
    p_challenge_id: $challenge_id,
    p_date_key: $date_key,
    p_generation_seed: $generation_seed,
    p_generation_version: $generation_version,
    p_world_template: $world_template,
    p_config: {
      appKey: $app_key,
      challengeId: $challenge_id,
      dateKey: $date_key,
      generationSeed: $generation_seed,
      generationVersion: $generation_version,
      worldTemplate: $world_template
    }
  }')"

anon_rpc_file="$tmp_dir/anon-rpc.json"
anon_rpc_status="$(request POST "$api_url/rest/v1/rpc/daily_challenge_ensure" "$anon_key" "$ensure_body" "$anon_rpc_file")"
assert_status "$anon_rpc_status" 401 "anon role cannot invoke protected RPC" "$anon_rpc_file"

ensure_file="$tmp_dir/ensure.json"
ensure_status="$(request POST "$api_url/rest/v1/rpc/daily_challenge_ensure" "$access_token" "$ensure_body" "$ensure_file")"
assert_status "$ensure_status" 204 "authenticated challenge ensure" "$ensure_file"

client_run_id="$(uuidgen | tr '[:upper:]' '[:lower:]')"
install_id="$(uuidgen | tr '[:upper:]' '[:lower:]')"
submit_body="$(jq -cn \
  --arg app_key 'signal-hunt' \
  --arg challenge_id "$challenge_id" \
  --arg client_run_id "$client_run_id" \
  --arg player_name 'Local Integration' \
  --arg install_id "$install_id" \
  '{
    p_app_key: $app_key,
    p_challenge_id: $challenge_id,
    p_client_run_id: $client_run_id,
    p_player_name: $player_name,
    p_install_id: $install_id,
    p_time_ms: 123456,
    p_score: 900,
    p_collected_count: 10,
    p_completed: true,
    p_replay_data: {
      appKey: $app_key,
      challengeId: $challenge_id,
      frames: [
        {timestamp: 0, position: {x: 0, y: 0, z: 0}},
        {timestamp: 1, position: {x: 1, y: 0, z: 1}}
      ]
    }
  }')"

submit_one_file="$tmp_dir/submit-one.json"
submit_one_status="$(request POST "$api_url/rest/v1/rpc/daily_challenge_submit_run" "$access_token" "$submit_body" "$submit_one_file")"
assert_status "$submit_one_status" 200 "first run submission" "$submit_one_file"
run_id_one="$(jq -er '.' "$submit_one_file")"

submit_two_file="$tmp_dir/submit-two.json"
submit_two_status="$(request POST "$api_url/rest/v1/rpc/daily_challenge_submit_run" "$access_token" "$submit_body" "$submit_two_file")"
assert_status "$submit_two_status" 200 "idempotent run resubmission" "$submit_two_file"
run_id_two="$(jq -er '.' "$submit_two_file")"
if [[ "$run_id_one" != "$run_id_two" ]]; then
  echo "Idempotency failed: duplicate client_run_id returned a different run" >&2
  exit 1
fi
echo "PASS  duplicate client_run_id resolves to one run"

leaderboard_body="$(jq -cn --arg app_key 'signal-hunt' --arg challenge_id "$challenge_id" \
  '{p_app_key: $app_key, p_challenge_id: $challenge_id, p_limit: 10}')"
leaderboard_file="$tmp_dir/leaderboard.json"
leaderboard_status="$(request POST "$api_url/rest/v1/rpc/daily_challenge_leaderboard" "$access_token" "$leaderboard_body" "$leaderboard_file")"
assert_status "$leaderboard_status" 200 "leaderboard RPC" "$leaderboard_file"
jq -e 'length >= 1 and .[0].rank >= 1 and .[0].attempt_count >= 1' "$leaderboard_file" >/dev/null
echo "PASS  leaderboard contains ranked real run data"

montage_body="$(jq -cn --arg app_key 'signal-hunt' --arg challenge_id "$challenge_id" \
  '{p_app_key: $app_key, p_challenge_id: $challenge_id, p_limit: 16}')"
montage_file="$tmp_dir/montage.json"
montage_status="$(request POST "$api_url/rest/v1/rpc/daily_challenge_montage" "$access_token" "$montage_body" "$montage_file")"
assert_status "$montage_status" 200 "daily montage RPC" "$montage_file"
jq -e '(length >= 1) and ((.[0].replay_data.frames | length) >= 2)' "$montage_file" >/dev/null
echo "PASS  montage returns reconstructable replay frames"

for readable_table in signal_hunt_cosmetics signal_hunt_inventory; do
  table_file="$tmp_dir/$readable_table.json"
  table_status="$(request GET "$api_url/rest/v1/$readable_table?select=*" "$access_token" '' "$table_file")"
  assert_status "$table_status" 200 "$readable_table RLS read path" "$table_file"
done

runs_file="$tmp_dir/runs.json"
runs_status="$(request GET "$api_url/rest/v1/signal_hunt_runs?select=*" "$access_token" '' "$runs_file")"
assert_status "$runs_status" 403 "direct run-table access remains blocked" "$runs_file"

echo "Local Supabase integration passed without exposing any credential values."
