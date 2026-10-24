#!/bin/zsh
emulate -L zsh
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: ./scripts/accept-waypoint-wings-device.sh DEVICE_ID BUILD_COMMIT

Installs the existing signed Waypoint Wings .app, launches it on a paired
iPhone, and records a human-confirmed full-course acceptance result. This
script intentionally does not invoke Unity, xcodebuild, or the macOS tests.

Arguments:
  DEVICE_ID     Identifier accepted by `xcrun devicectl --device`.
  BUILD_COMMIT  Commit from which the existing signed .app was built.

The only source changes permitted after BUILD_COMMIT are this acceptance
script, its documentation, and the device-evidence gitignore rule.
USAGE
}

if (( $# == 1 )) && [[ $1 == --help ]]; then
  usage
  exit 0
fi

if (( $# != 2 )); then
  usage >&2
  exit 64
fi

device_id=$1
build_commit=$2
script_dir=${0:A:h}
repo_root=${script_dir:h}
derived_products="$repo_root/Builds/WaypointWings-Derived/Build/Products/Debug-iphoneos"

cd "$repo_root"

git cat-file -e "${build_commit}^{commit}" 2>/dev/null || {
  echo "BUILD_COMMIT is not a commit in this repository: $build_commit" >&2
  exit 1
}

current_head=$(git rev-parse HEAD)
allowed_delta=(
  .gitignore
  docs/WAYPOINT-WINGS-DEVICE-ACCEPTANCE.md
  scripts/accept-waypoint-wings-device.sh
)

unexpected_delta=()
while IFS= read -r changed_path; do
  [[ -z $changed_path ]] && continue
  if (( ${allowed_delta[(Ie)$changed_path]} == 0 )); then
    unexpected_delta+=("$changed_path")
  fi
done < <(git diff --name-only "$build_commit..$current_head")

if (( ${#unexpected_delta} > 0 )); then
  echo "Refusing acceptance: product source changed after BUILD_COMMIT:" >&2
  printf '  %s\n' "${unexpected_delta[@]}" >&2
  echo "Rebuild the iPhone app from the current product source before accepting it." >&2
  exit 1
fi

working_tree_paths=(
  "${(@f)$(git diff --name-only)}"
  "${(@f)$(git diff --cached --name-only)}"
  "${(@f)$(git ls-files --others --exclude-standard)}"
)
unexpected_working_tree=()
for changed_path in "${working_tree_paths[@]}"; do
  [[ -z $changed_path ]] && continue
  if (( ${allowed_delta[(Ie)$changed_path]} == 0 )); then
    unexpected_working_tree+=("$changed_path")
  fi
done

if (( ${#unexpected_working_tree} > 0 )); then
  echo "Refusing acceptance: the working tree contains product-source changes:" >&2
  printf '  %s\n' "${(u)unexpected_working_tree[@]}" >&2
  echo "Restore or rebuild from those changes before accepting the existing app." >&2
  exit 1
fi

app_path=$(find "$derived_products" -maxdepth 1 -type d -name '*.app' -print -quit 2>/dev/null || true)
if [[ -z $app_path ]]; then
  echo "Existing signed Waypoint Wings .app not found under: $derived_products" >&2
  echo "This acceptance check does not rebuild it." >&2
  exit 1
fi

plist="$app_path/Info.plist"
if [[ ! -f $plist ]]; then
  echo "App bundle has no Info.plist: $app_path" >&2
  exit 1
fi

bundle_id=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$plist")
if [[ $bundle_id != com.isaiahdupree.waypointwings ]]; then
  echo "Unexpected bundle identifier: $bundle_id" >&2
  exit 1
fi

timestamp=$(date -u +%Y%m%dT%H%M%SZ)
evidence_dir="$repo_root/artifacts/device-acceptance/waypoint-wings-$timestamp"
mkdir -p "$evidence_dir"
raw_log="$evidence_dir/device-acceptance.log"
summary_file="$evidence_dir/pr-evidence.md"
manifest_file="$evidence_dir/app-manifest.sha256"
device_info_file="$evidence_dir/device-info.txt"

exec > >(tee "$raw_log") 2>&1

echo "Waypoint Wings physical-device acceptance"
echo "UTC timestamp: $timestamp"
echo "Build commit: $build_commit"
echo "Current repository head: $current_head"
echo "App path: $app_path"
echo

echo "== Tool versions =="
xcodebuild -version
xcrun devicectl version
echo

echo "== Source-scope guard =="
echo "PASS: no product source changed between BUILD_COMMIT and current HEAD"
git diff --name-only "$build_commit..$current_head"
echo

echo "== Bundle identity and signature =="
short_version=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$plist")
build_version=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleVersion' "$plist")
echo "Bundle identifier: $bundle_id"
echo "Version: $short_version ($build_version)"
codesign --verify --deep --strict --verbose=2 "$app_path"
codesign -d --verbose=4 "$app_path"
echo

echo "== Bundle content digest =="
while IFS= read -r bundle_file; do
  relative_path=${bundle_file#$app_path/}
  file_digest=$(shasum -a 256 "$bundle_file" | awk '{print $1}')
  printf '%s  %s\n' "$file_digest" "$relative_path"
done < <(find "$app_path" -type f -print | LC_ALL=C sort) > "$manifest_file"
app_digest=$(shasum -a 256 "$manifest_file" | awk '{print $1}')
echo "App manifest SHA-256: $app_digest"
echo

echo "== Paired-device reachability =="
xcrun devicectl device info details --device "$device_id" | tee "$device_info_file"
echo

echo "== Install and launch =="
xcrun devicectl device install app --device "$device_id" "$app_path"
xcrun devicectl device process launch \
  --device "$device_id" \
  --terminate-existing \
  "$bundle_id"
echo "PASS: devicectl installed and launched $bundle_id"
echo

cat <<'INSTRUCTIONS'
On the iPhone:
  1. Confirm the Waypoint Wings start menu appears.
  2. Tap PLAY.
  3. Fly through all 12 gates and finish the course.
  4. Confirm the result screen appears without a crash, hang, or blocking error.
  5. Capture a screenshot or short recording showing the completed result.
INSTRUCTIONS
echo

read -r "course_result?Type PASS only after the full course and result screen complete: "
if [[ $course_result != PASS ]]; then
  echo "FAIL: full-course acceptance was not confirmed." >&2
  exit 1
fi

read -r "course_evidence?Path to the completion screenshot or recording: "
course_evidence=${course_evidence/#\~/$HOME}
if [[ ! -f $course_evidence ]]; then
  echo "Evidence file not found: $course_evidence" >&2
  exit 1
fi

evidence_digest=$(shasum -a 256 "$course_evidence" | awk '{print $1}')
evidence_name=${course_evidence:t}
device_ref=$(printf '%s' "$device_id" | shasum -a 256 | awk '{print substr($1, 1, 12)}')

{
  echo "### Waypoint Wings iPhone acceptance — PASS"
  echo
  echo "- Acceptance time (UTC): \`$timestamp\`"
  echo "- Device reference: \`device-$device_ref\` (identifier redacted)"
  echo "- Built product commit: \`$build_commit\`"
  echo "- PR head checked: \`$current_head\`"
  echo "- Bundle: \`$bundle_id\`, version \`$short_version ($build_version)\`"
  echo "- App manifest SHA-256: \`$app_digest\`"
  echo "- Completion evidence: \`$evidence_name\`"
  echo "- Evidence SHA-256: \`$evidence_digest\`"
  echo
  echo "Acceptance performed:"
  echo
  echo "- [x] Existing signed bundle passed \`codesign --verify --deep --strict\`."
  echo "- [x] \`devicectl\` reached the paired iPhone, installed the bundle, and launched it."
  echo "- [x] Waypoint Wings reached its start menu."
  echo "- [x] PLAY started the course."
  echo "- [x] All 12 gates were completed and the result screen appeared."
  echo "- [x] No crash, hang, or blocking error occurred during the run."
  echo
  echo "Scope note: no Unity tests, macOS builds, or gameplay code were rerun or changed for this device-only acceptance check. Attach \`$evidence_name\` to this comment. Keep the raw device log local because it can contain device identifiers."
} > "$summary_file"

echo
echo "PASS: Waypoint Wings completed one full course on the physical iPhone."
echo "PR-ready evidence: $summary_file"
echo "Raw local log (do not post unredacted): $raw_log"
