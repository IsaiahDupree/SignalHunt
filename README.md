# Signal Hunt

This repository contains two compact daily challenge games for iOS. Every player receives the same procedurally generated stage for the current UTC date, can retry it without limit, and records reconstructable runs for ghosts, leaderboards, and social replays.

The apps share the daily-seed, identity, cosmetics, backend, leaderboard, replay, and Daily Film engine while shipping as separate Unity scenes and products:

- **Signal Hunt** — find hidden relics in a rotating daily world.
- **Waypoint Rally** — route-optimize through ground checkpoints.
- **Waypoint Wings** — fly through a vertical daily course.

## Play the MVP

Open this directory in Unity `6000.5.2f1`, open `Assets/SignalHunt/Scenes/Main.unity`, and press Play.

Controls:

- Touch: `LEFT`, `RIGHT`, `BRAKE`, and `GO`.
- Keyboard: WASD or arrow keys.
- `STYLE` cycles and persists the launch vehicle color.

The run begins after a three-second countdown. Collect all ten signals; the result panel saves the attempt, shows the improvement against your daily best, syncs the live board when Supabase is configured, and offers `RACE AGAIN`, `WATCH / SHARE`, and `DAILY FILM`.

## Play Waypoint Wings

Open `Assets/WaypointWings/Scenes/Main.unity` and press Play. Fly the aircraft through all 14 gates in order using the on-screen `LEFT`, `RIGHT`, `DOWN`, `UP`, and `BOOST` controls or the keyboard. The UTC date alternates between the bright Sunrise Archipelago and Neon Skyway stages.

Waypoint Wings records each flight at 10 Hz, plays personal-best and daily-leader aircraft ghosts, saves unlimited same-day attempts, and provides `WATCH FLIGHT`, `DAILY FILM`, and `FLY AGAIN` after every round. Its Daily Film reconstructs up to 16 real aircraft runs through cinematic chase, overhead, orbit, and wide cameras.

## What is implemented

- Stable UTC daily challenge IDs and platform-independent xorshift generation.
- Two rotating daily stages: the generated neon city and the low-poly Emerald Isle.
- A fully programmatic island with faceted terrain, beaches, surrounding water, rolling hills, a closed rally road, ramps, pine trees, rocks, lighting, and ten safe/separated relics.
- A generated neon town with roads, alleys, buildings, plazas, ramps, tunnel gates, lighting, and ten safe/separated relics.
- Physics hover-car controller with touch and keyboard input.
- Timer, completion rules, deterministic scoring, player nameplate, and vehicle color selection.
- Unlimited retries on the same daily seed, with durable local attempt history, attempt numbering, personal-best deltas, and best-ghost playback.
- A real post-round top-five board showing each player's best run and total attempts; offline state is labeled honestly.
- 10 Hz replay snapshots with position, rotation, speed, vehicle state, collection markers, and camera events.
- Local personal-best ghost playback and share-caption metadata.
- Cinematic replay playback with orbit, chase, overhead, and wide camera cuts; iOS device builds use ReplayKit's native preview/save/share sheet.
- A Daily Film director that reconstructs up to 16 real racers, time-compresses their routes, switches racers/angles every three seconds, and records a vertical social clip through ReplayKit.
- A separate Waypoint Wings app shell with two deterministic vertical stages, arcade aircraft controls, 14 ordered flight gates, aircraft ghosts, flight scoring, retry flow, and an aircraft-specific Daily Film.
- Shared `appKey`/`modeKey` replay contracts and generic backend RPCs for Signal Hunt, Waypoint Rally, and Waypoint Wings.
- Supabase anonymous authentication, token refresh, challenge claiming, validated run submission, top-ten leaderboard, top ghost, profiles, cosmetics, and inventory schema.
- Portrait safe-area HUD and an automated standalone visual smoke-capture mode.

## Backend setup

Apply the ordered migrations in `supabase/migrations/` to the shared Supabase project, then enable anonymous sign-ins in Supabase Auth. The checked-in client never uses the service-role key.

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

Build the separate Waypoint Wings preview:

```bash
"$SIGNAL_HUNT_UNITY" -batchmode -nographics -noaudio -quit \
  -projectPath "$PWD" \
  -executeMethod WaypointWings.Editor.WaypointWingsProjectBuilder.BuildMacPreview \
  -logFile Logs/mac-wings-build.log
```

Use `--wings-stage archipelago` or `--wings-stage skyway` to inspect either deterministic flight stage in a development build.

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
Assets/WaypointWings/Runtime         flight challenge, world, aircraft, replay, and UI
Assets/WaypointWings/Editor          separate scene and macOS/iOS app builders
Assets/WaypointWings/Tests           flight generator and full-loop smoke tests
supabase/migrations                  shared production backend schema
```

See `docs/SHARED-ENGINE.md` for the three-app boundary and the next build slices.
See `docs/DAILY-FILM.md` for the attempt, leaderboard, ghost, and social montage flow.
