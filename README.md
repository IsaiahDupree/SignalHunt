# Signal Hunt

Signal Hunt is a compact daily hunt racer for iOS. Every player receives the same procedurally generated stage for the current UTC date, drives the same hover car, and races to collect ten signal relics. Runs produce a deterministic score, a reconstructable ghost, daily leaderboard data, and an iOS ReplayKit highlight that can be saved or shared.

This repository contains the first playable app and the reusable foundation for two later app shells:

- **Signal Hunt** — find hidden relics in a rotating daily world.
- **Waypoint Rally** — route-optimize through ground checkpoints.
- **Waypoint Wings** — fly through a vertical daily course.

## Play the MVP

Open this directory in Unity `6000.5.2f1`, open `Assets/SignalHunt/Scenes/Main.unity`, and press Play.

Controls:

- Touch: `LEFT`, `RIGHT`, `BRAKE`, and `GO`.
- Keyboard: WASD or arrow keys.
- `STYLE` cycles and persists the launch vehicle color.

The run begins after a three-second countdown. Collect all ten signals; the result panel saves the run locally, syncs it when Supabase is configured, and exposes `EXPORT REPLAY`.

## What is implemented

- Stable UTC daily challenge IDs and platform-independent xorshift generation.
- Two rotating daily stages: the generated neon city and the low-poly Emerald Isle.
- A fully programmatic island with faceted terrain, beaches, surrounding water, rolling hills, a closed rally road, ramps, pine trees, rocks, lighting, and ten safe/separated relics.
- A generated neon town with roads, alleys, buildings, plazas, ramps, tunnel gates, lighting, and ten safe/separated relics.
- Physics hover-car controller with touch and keyboard input.
- Timer, completion rules, deterministic scoring, player nameplate, and vehicle color selection.
- 10 Hz replay snapshots with position, rotation, speed, vehicle state, collection markers, and camera events.
- Local personal-best ghost playback and share-caption metadata.
- Cinematic replay playback; iOS device builds use ReplayKit's native preview/save/share sheet.
- Supabase anonymous authentication, token refresh, challenge claiming, validated run submission, top-ten leaderboard, top ghost, profiles, cosmetics, and inventory schema.
- Portrait safe-area HUD and an automated standalone visual smoke-capture mode.

## Backend setup

The migration is at `supabase/migrations/202608150001_signal_hunt_core.sql`. Apply it to the shared Supabase project, then enable anonymous sign-ins in Supabase Auth. The checked-in client never uses the service-role key.

For Editor network play, export:

```bash
export SUPABASE_URL="https://your-project.supabase.co"
export SIGNAL_HUNT_SUPABASE_ANON_KEY="your-public-anon-key"
```

For an iOS build the editor builder injects those values into a temporary Resources asset, includes it in the player, and deletes the asset after the build. The generated config asset is ignored by Git.

Without client configuration, the complete game loop and local replay/ghost remain functional; the result explicitly reports that the run was saved locally instead of presenting fabricated leaderboard data.

## Commands

Set a local shell variable for the Unity binary:

```bash
SIGNAL_HUNT_UNITY="/Applications/Unity/Hub/Editor/6000.5.2f1/Unity.app/Contents/MacOS/Unity"
```

Set up or refresh the scene and player settings:

```bash
"$SIGNAL_HUNT_UNITY" -batchmode -nographics -noaudio -quit \
  -projectPath "$PWD" \
  -executeMethod SignalHunt.Editor.SignalHuntProjectBuilder.SetupProject \
  -logFile Logs/editor-setup.log
```

Run tests:

```bash
"$SIGNAL_HUNT_UNITY" -batchmode -nographics -noaudio \
  -projectPath "$PWD" -runTests -testPlatform EditMode \
  -testResults Logs/editmode-results.xml -logFile Logs/editmode-tests.log

"$SIGNAL_HUNT_UNITY" -batchmode -nographics -noaudio \
  -projectPath "$PWD" -runTests -testPlatform PlayMode \
  -testResults Logs/playmode-results.xml -logFile Logs/playmode-tests.log
```

Build the local macOS preview:

```bash
"$SIGNAL_HUNT_UNITY" -batchmode -nographics -noaudio -quit \
  -projectPath "$PWD" \
  -executeMethod SignalHunt.Editor.SignalHuntProjectBuilder.BuildMacPreview \
  -logFile Logs/mac-build.log
```

The UTC daily challenge alternates stages automatically. To inspect a specific stage in a development build, add either `--signalhunt-stage city` or `--signalhunt-stage island` to the player command line.

Build the Xcode iOS project after installing Unity's iOS Build Support module:

```bash
"$SIGNAL_HUNT_UNITY" -batchmode -nographics -noaudio -quit \
  -projectPath "$PWD" \
  -executeMethod SignalHunt.Editor.SignalHuntProjectBuilder.BuildIos \
  -logFile Logs/ios-build.log
```

## Daily seed contract

The public challenge identity is derived from:

```text
UTC date + season + theme + difficulty + world template
```

For example, `2026-08-15` produces:

```text
2026-08-15_city_neon_standard_seed_95713
```

The following day produces the shared island challenge:

```text
2026-08-16_island_coastal_standard_seed_62010
```

Do not change `StableHash`, `DeterministicRandom`, or generator ordering inside an existing generation version. Ship intentional changes as a new world-template version, such as `synthetic-town-v2` or `synthetic-island-v2`, so stored ghosts remain reconstructable.

## Repository map

```text
Assets/SignalHunt/Runtime/Core       daily challenge and deterministic RNG
Assets/SignalHunt/Runtime/World      layout model, generator, and renderer
Assets/SignalHunt/Runtime/Gameplay   vehicle, session, camera, and cosmetics
Assets/SignalHunt/Runtime/Replay     recorder, local ghost, and ReplayKit export
Assets/SignalHunt/Runtime/Backend    Supabase auth and API client
Assets/SignalHunt/Runtime/UI         portrait HUD and touch controls
Assets/SignalHunt/Editor             reproducible scene and platform builders
Assets/SignalHunt/Tests              deterministic and play-mode smoke tests
supabase/migrations                  shared production backend schema
```

See `docs/SHARED-ENGINE.md` for the three-app boundary and the next build slices.
