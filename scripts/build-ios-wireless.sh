#!/bin/zsh
set -euo pipefail

if (( $# != 3 )); then
  echo "Usage: $0 signal-hunt|waypoint-wings|waypoint-rally|treasure-hunter|all DEVICE_ID DEVELOPMENT_TEAM" >&2
  exit 64
fi

game_key=$1
device_id=$2
development_team=$3
script_dir=${0:A:h}
repo_root=${script_dir:h}
unity_binary=${SIGNAL_HUNT_UNITY:-/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity}
xcode_device_id=$(xcrun devicectl device info details --device "$device_id" |
  sed -n 's/^[[:space:]]*• udid: //p' | head -1)
if [[ -z $xcode_device_id ]]; then
  echo "Could not resolve the Xcode UDID for device $device_id" >&2
  exit 1
fi

if [[ $game_key == all ]]; then
  for game in signal-hunt waypoint-wings waypoint-rally treasure-hunter; do
    "$0" "$game" "$device_id" "$development_team"
  done
  exit 0
fi

case $game_key in
  signal-hunt)
    builder=SignalHunt.Editor.SignalHuntProjectBuilder.BuildIos
    xcode_dir=Builds/iOS
    derived_dir=Builds/SignalHunt-Derived
    ;;
  waypoint-wings)
    builder=WaypointWings.Editor.WaypointWingsProjectBuilder.BuildIos
    xcode_dir=Builds/WaypointWings-iOS
    derived_dir=Builds/WaypointWings-Derived
    ;;
  waypoint-rally)
    builder=WaypointRally.Editor.WaypointRallyProjectBuilder.BuildIos
    xcode_dir=Builds/WaypointRally-iOS
    derived_dir=Builds/WaypointRally-Derived
    ;;
  treasure-hunter)
    builder=TreasureHunt.Editor.TreasureHuntProjectBuilder.BuildIos
    xcode_dir=Builds/TreasureHunter-iOS
    derived_dir=Builds/TreasureHunter-Derived
    ;;
  *)
    echo "Unknown game: $game_key" >&2
    exit 64
    ;;
esac

cd "$repo_root"
mkdir -p Logs

"$unity_binary" -batchmode -nographics -noaudio -quit \
  -projectPath "$repo_root" \
  -executeMethod "$builder" \
  -logFile "Logs/${game_key}-ios-export.log"

if ! grep -q 'ReplayKit.framework' "$xcode_dir/Unity-iPhone.xcodeproj/project.pbxproj"; then
  echo "The iOS export is missing ReplayKit.framework for $game_key" >&2
  exit 1
fi

xcodebuild -quiet \
  -project "$xcode_dir/Unity-iPhone.xcodeproj" \
  -scheme Unity-iPhone \
  -configuration Debug \
  -destination "platform=iOS,id=$xcode_device_id" \
  -derivedDataPath "$derived_dir" \
  -allowProvisioningUpdates \
  DEVELOPMENT_TEAM="$development_team" \
  CODE_SIGN_STYLE=Automatic \
  build

app_path=$(find "$derived_dir/Build/Products/Debug-iphoneos" -maxdepth 1 -type d -name '*.app' -print -quit)
if [[ -z $app_path ]]; then
  echo "Signed app bundle was not produced for $game_key" >&2
  exit 1
fi

codesign --verify --deep --strict "$app_path"
bundle_id=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$app_path/Info.plist")
xcrun devicectl device install app --device "$device_id" "$app_path"
xcrun devicectl device process launch --device "$device_id" --terminate-existing "$bundle_id"
echo "Installed and launched $game_key ($bundle_id) on $device_id"
