# Signal Hunt

This public repository contains four compact daily challenge games for iOS. Every player receives the same procedurally generated stage for the current UTC date, can retry it without limit, and records reconstructable runs for ghosts, leaderboards, and social replays.

The app network shares the daily-seed, identity, cosmetics, backend, leaderboard, replay, and Daily Film engine while shipping as separate Unity scenes and products:

- **Signal Hunt** — find hidden signals while driving a compact daily island.
- **Waypoint Wings** — fly a smooth gate course over a sunny archipelago.
- **Waypoint Rally** — race the readable Palm Coast island loop.
- **Treasure Hunter** — search a bright island trail for 10 artifacts in any order.

## Play Signal Hunt

Open this directory in Unity `6000.5.2f1`, open `Assets/SignalHunt/Scenes/Main.unity`, and press Play.

Controls:

- Touch: the vehicle moves automatically; drag the steering pad and hold `BRAKE` when needed.
- Keyboard: A/D or left/right steers, W accelerates, and S/reverse remains available.

The run begins after a three-second countdown. Collect all ten signals; the result panel saves the attempt, shows the improvement against your daily best, syncs the live board when Supabase is configured, and presents one clear action: `PLAY AGAIN`.

Every app opens with the same deliberately sparse flow: a short animated island loader, then a single `PLAY` button over a live cinematic flyover of that day's exact generated world. One compact card states the goal, item count, control gesture, and local best. Seed details, capture tools, replay production, and social-film assembly remain automated infrastructure and are not exposed as player-facing menu buttons. The backdrop is rendered from the playable daily world in real time, so it needs no bundled video and cannot drift from the active seed.

## Play Waypoint Wings

Open `Assets/WaypointWings/Scenes/Main.unity` and press Play. Fly through 12 large gates in order with the two-axis flight stick: drag left/right to turn and up/down to climb or descend, then release to auto-level. Hold `BOOST` with the other thumb. Keyboard controls remain WASD/arrows plus Space. Every daily course is a bright Sunny Archipelago with a smoother line, gentler altitude changes, and islands kept clear of the intended route.

Waypoint Wings uses slower cruise speed, responsive proportional steering, bounded pitch, and strong release-to-level assistance. Contact with terrain or leaving the course now triggers a short `CRASHED · RESTARTING` transition and automatically restarts the same attempt instead of leaving the plane stopped. Flights are still recorded at 10 Hz for personal-best ghosts and automated Daily Film production; the player sees only `PLAY AGAIN` after the round.

## Play Waypoint Rally

Open `Assets/WaypointRally/Scenes/Main.unity` and press Play. The car moves automatically; drag the steering pad and hold `BRAKE` when needed. Race through 10 wide checkpoints on the sunny Palm Coast Loop. The deterministic course uses a smooth paved island ring, generous road width, clear checkpoint order, and decorative palms outside the racing line.

Waypoint Rally saves unlimited attempts and reconstructs personal-best and daily-leader car ghosts behind the scenes. The finish screen has one `PLAY AGAIN` action; automated replay and Daily Film systems can still assemble the day's community runs without adding menu complexity.

## Play Treasure Hunter

Open `Assets/TreasureHunt/Scenes/Main.unity` and press Play. Drag the two-axis move pad to walk or run in any direction and tap `SCAN` to pulse nearby caches. Keyboard controls remain WASD/arrows and Space/E. The detector reports the nearest unfound artifact's strength, distance, and relative bearing.

Each programmatic Treasure Island contains the same 10 safe hiding locations for every player that day, arranged around a legible trail ring connected to the spawn point. Treasure Hunter records search routes at 10 Hz, reconstructs personal-best and daily-leader explorer ghosts, and saves unlimited attempts while exposing only `PLAY AGAIN` at the finish.

## What is implemented

- Stable UTC daily challenge IDs and platform-independent xorshift generation.
- Bright island-only production stages across all four apps; no night stage is selected by the daily challenge contract.
- Fully programmatic archipelagos with beaches, surrounding water, readable loops and trails, restrained scenery, and deterministic safe collectible positions.
- Physics hover-car controller with touch and keyboard input.
- Minimal touch controls with two player actions per app: steering plus brake, flight stick plus boost, or move pad plus scan.
- A one-action startup menu and one-action result screen over the live daily-world flyover.
- Timer, completion rules, deterministic scoring, and player nameplate.
- Unlimited retries on the same daily seed, with durable local attempt history, attempt numbering, personal-best deltas, and best-ghost playback.
- A compact post-round top-three board showing each player's best run and total attempts; offline state is labeled honestly.
- 10 Hz replay snapshots with position, rotation, speed, vehicle state, collection markers, and camera events.
- Local personal-best ghost playback and share-caption metadata.
- Cinematic replay playback with orbit, chase, overhead, and wide camera cuts; iOS device builds use ReplayKit's native preview/save/share sheet.
- A Daily Film director that reconstructs up to 16 real racers, time-compresses their routes, switches racers/angles every three seconds, and records a vertical social clip through ReplayKit.
- A Waypoint Wings shell with a sunny archipelago, analog flight stick, assisted controls, 12 large ordered gates, automatic crash restart, aircraft ghosts, scoring, retry flow, and aircraft-specific Daily Film automation.
- A Waypoint Rally shell with a bright island loop, 10 ordered checkpoints, auto-drive, a programmatic rally car, driver ghosts, scoring, retry flow, and rally-specific Daily Film automation.
- A Treasure Hunter shell with a readable island trail, 10 any-order artifacts, two-axis movement, directional detector, scan pulse, explorer ghosts, scoring, retry flow, and explorer-specific Daily Film automation.
- Shared `appKey`/`modeKey` replay contracts and generic backend RPCs for Signal Hunt, Waypoint Rally, Waypoint Wings, and Treasure Hunter.
- Supabase anonymous authentication, token refresh, challenge claiming, validated run submission, top-ten leaderboard, top ghost, profiles, cosmetics, and inventory schema.
- Portrait safe-area HUD and an automated standalone visual smoke-capture mode.

## Backend setup

Apply the ordered migrations in `supabase/migrations/` to the shared Supabase project, then enable anonymous sign-ins in Supabase Auth. The checked-in client never uses the service-role key.

Before a paid-project migration, verify the entire stack against a real local Supabase instance:

```bash
supabase start
supabase db reset --local
./scripts/test-supabase-local.sh
```

Then run the production preflight with a fresh personal access token and database password injected only through your terminal environment. The preflight pins the expected project ref, verifies token scope, lints the existing remote schema, saves a schema-only backup under ignored `artifacts/`, and runs `db push --dry-run`. It never applies a migration.

```bash
export SUPABASE_ACCESS_TOKEN="..."
export SUPABASE_DB_PASSWORD="..."
./scripts/preflight-supabase-production.sh
```

Do not paste either secret into chat or commit it. Review the preflight output and backup before running any real `supabase db push --linked` operation.

For Editor network play, export:

```bash
export SUPABASE_URL="https://your-project.supabase.co"
export SIGNAL_HUNT_SUPABASE_ANON_KEY="your-public-anon-key"
```

For an iOS build the editor builder injects those values into a temporary Resources asset, includes it in the player, and deletes the asset after the build. The generated config asset is ignored by Git. If the public URL and anonymous key are absent, every app still builds in honest offline mode without a runtime config or fabricated network data.

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

Use `--wings-stage archipelago` to pin the production island stage in a development build. Legacy stage names are accepted only as compatibility aliases and resolve to the archipelago.

Build the separate Waypoint Rally preview:

```bash
"$SIGNAL_HUNT_UNITY" -batchmode -nographics -noaudio -quit \
  -projectPath "$PWD" \
  -executeMethod WaypointRally.Editor.WaypointRallyProjectBuilder.BuildMacPreview \
  -logFile Logs/mac-rally-build.log
```

Use `--rally-stage island` to pin the production Palm Coast Loop in a development build. Legacy ground-stage names resolve to this island course.

Build the separate Treasure Hunter preview:

```bash
"$SIGNAL_HUNT_UNITY" -batchmode -nographics -noaudio -quit \
  -projectPath "$PWD" \
  -executeMethod TreasureHunt.Editor.TreasureHuntProjectBuilder.BuildMacPreview \
  -logFile Logs/mac-treasure-build.log
```

Use `--treasure-stage island` to pin the production Treasure Island stage. Legacy exploration-stage names resolve to the island. Automated visual checks can also pin a seed date with `--treasure-date YYYY-MM-DD`.

Programmatic startup visuals can be captured from standalone builds with `--treasure-capture-loading PATH` and `--treasure-capture-menu PATH`. The equivalent prefixes are `--signalhunt`, `--rally`, and `--wings` for the other app shells.

Production UTC challenges select the island stage automatically. Add `--signalhunt-stage island` to make that selection explicit in a development build; the legacy `city` alias also resolves to the island.

Build the Xcode iOS project after installing Unity's iOS Build Support module:

```bash
"$SIGNAL_HUNT_UNITY" -batchmode -nographics -noaudio -quit \
  -projectPath "$PWD" \
  -executeMethod SignalHunt.Editor.SignalHuntProjectBuilder.BuildIos \
  -logFile Logs/ios-build.log
```

To export, sign, install, and launch one app on a paired iPhone over Wi-Fi, supply the identifier shown by `xcrun devicectl list devices` and the Apple development team shown by Xcode:

```bash
./scripts/build-ios-wireless.sh treasure-hunter "$IOS_DEVICE_ID" "$IOS_DEVELOPMENT_TEAM"
```

Use `signal-hunt`, `waypoint-wings`, or `waypoint-rally` for a single app, or `all` to deploy the complete suite. The phone must be paired, reachable on the local network, and have Developer Mode enabled. The script uses Xcode automatic signing and permits Xcode to refresh the development profile for the supplied team after a restart; it never accepts or stores an Apple password or API key.

## Daily seed contract

The public challenge identity is derived from:

```text
UTC date + season + theme + difficulty + world template
```

For example, `2026-08-15` produces:

```text
2026-08-15_signal-island_sunny-island_standard_seed_95713
```

The following day produces a new shared island layout:

```text
2026-08-16_signal-island_sunny-island_standard_seed_62010
```

Do not change `StableHash`, `DeterministicRandom`, or generator ordering inside an existing generation version. Ship intentional changes as a new world-template and generator version, such as `signal-island-adventure-v2` and `island-generator-v2`, so stored ghosts remain reconstructable.

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
Assets/WaypointRally/Runtime         rally challenge, world, car, replay, and UI
Assets/WaypointRally/Editor          separate scene and macOS/iOS app builders
Assets/WaypointRally/Tests           rally generator and full-loop smoke tests
Assets/TreasureHunt/Runtime          exploration challenge, world, detector, replay, and UI
Assets/TreasureHunt/Editor           separate scene and macOS/iOS app builders
Assets/TreasureHunt/Tests            treasure generator and full-loop smoke tests
supabase/migrations                  shared production backend schema
```

See `docs/SHARED-ENGINE.md` for the four-app boundary and the next build slices.
See `docs/DAILY-FILM.md` for the attempt, leaderboard, ghost, and social montage flow.
