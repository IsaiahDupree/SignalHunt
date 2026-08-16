# Signal Hunt

This public repository contains four compact daily challenge games for iOS. Every player receives the same procedurally generated stage for the current UTC date, can retry it without limit, and records reconstructable runs for ghosts, leaderboards, and social replays.

The app network shares the daily-seed, identity, cosmetics, backend, leaderboard, replay, and Daily Film engine while shipping as separate Unity scenes and products:

- **Signal Hunt** — playable; find hidden relics in a rotating daily world.
- **Waypoint Wings** — playable; fly through a vertical daily course.
- **Waypoint Rally** — playable; choose the fastest route through ground checkpoints.
- **Treasure Hunter** — playable; search on foot with a detector and secure 12 artifacts in any order.

## Play Signal Hunt

Open this directory in Unity `6000.5.2f1`, open `Assets/SignalHunt/Scenes/Main.unity`, and press Play.

Controls:

- Touch: `LEFT`, `RIGHT`, `BRAKE`, and `GO`.
- Keyboard: WASD or arrow keys.
- `STYLE` cycles and persists the launch vehicle color.

The run begins after a three-second countdown. Collect all ten signals; the result panel saves the attempt, shows the improvement against your daily best, syncs the live board when Supabase is configured, and offers `RACE AGAIN`, `WATCH / SHARE`, and `DAILY FILM`.

Before the countdown, every app now opens through the same programmatic startup experience with app-specific art direction. An animated seed loader introduces the shared daily world, followed by a home menu over a live cinematic flyover of that exact generated map. The menu explains today's objective and controls, shows the exact stage/seed/reset time, displays only the player's real local attempts and personal best, offers player color selection and how-to-play, and unlocks Daily Film preview after the first saved run. The moving backdrop is rendered from the daily world in real time, so it needs no bundled video and cannot drift from the playable seed.

## Play Waypoint Wings

Open `Assets/WaypointWings/Scenes/Main.unity` and press Play. Fly the aircraft through all 14 gates in order using the on-screen `LEFT`, `RIGHT`, `DOWN`, `UP`, and `BOOST` controls or the keyboard. The UTC date alternates between the bright Sunrise Archipelago and Neon Skyway stages.

Waypoint Wings records each flight at 10 Hz, plays personal-best and daily-leader aircraft ghosts, saves unlimited same-day attempts, and provides `WATCH FLIGHT`, `DAILY FILM`, and `FLY AGAIN` after every round. Its Daily Film reconstructs up to 16 real aircraft runs through cinematic chase, overhead, orbit, and wide cameras.

## Play Waypoint Rally

Open `Assets/WaypointRally/Scenes/Main.unity` and press Play. Drive through all 10 checkpoints in order using `LEFT`, `RIGHT`, `BRAKE`, and `GO` or the keyboard. The UTC date alternates between Turbo Harbor, with grid roads and diagonal shortcuts, and Dustlands Run, with open off-road terrain, rocks, cacti, and ramps.

Waypoint Rally saves unlimited attempts, reconstructs personal-best and daily-leader car ghosts, and provides `WATCH RACE`, `DAILY FILM`, and `RACE AGAIN` after every round. Players share the checkpoint order while remaining free to discover a faster line between checkpoints.

## Play Treasure Hunter

Open `Assets/TreasureHunt/Scenes/Main.unity` and press Play. Explore on foot with `LEFT`, `RIGHT`, `RUN`, and `SCAN`, or use A/D, W, and Space/E. The detector reports the nearest unfound artifact's strength, distance, and relative bearing; a scan pulse briefly amplifies nearby artifact caches.

The UTC date alternates between the bright, overgrown Sunken Ruins and the nocturnal Crystal Hollow. Each programmatic world contains the same 12 safe, separated hiding locations for every player that day. Treasure Hunter records search routes at 10 Hz, reconstructs personal-best and daily-leader explorer ghosts, saves unlimited attempts, and offers `WATCH SEARCH`, `DAILY FILM`, and `HUNT AGAIN` after every round.

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
- A separate Waypoint Rally app shell with two deterministic ground stages, 10 ordered checkpoints, open route choice, a programmatic rally car, driver ghosts, scoring, retry flow, and a rally-specific Daily Film.
- A separate Treasure Hunter app shell with two deterministic exploration stages, 12 any-order artifacts, an on-foot controller, directional detector, scan pulse, explorer ghosts, scoring, retry flow, and an explorer-specific Daily Film.
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

Use `--wings-stage archipelago` or `--wings-stage skyway` to inspect either deterministic flight stage in a development build.

Build the separate Waypoint Rally preview:

```bash
"$SIGNAL_HUNT_UNITY" -batchmode -nographics -noaudio -quit \
  -projectPath "$PWD" \
  -executeMethod WaypointRally.Editor.WaypointRallyProjectBuilder.BuildMacPreview \
  -logFile Logs/mac-rally-build.log
```

Use `--rally-stage town` or `--rally-stage dustlands` to inspect either deterministic ground-racing stage.

Build the separate Treasure Hunter preview:

```bash
"$SIGNAL_HUNT_UNITY" -batchmode -nographics -noaudio -quit \
  -projectPath "$PWD" \
  -executeMethod TreasureHunt.Editor.TreasureHuntProjectBuilder.BuildMacPreview \
  -logFile Logs/mac-treasure-build.log
```

Use `--treasure-stage ruins` or `--treasure-stage crystals` to inspect either deterministic exploration stage. Automated visual checks can also pin a seed date with `--treasure-date YYYY-MM-DD`.

Programmatic startup visuals can be captured from standalone builds with `--treasure-capture-loading PATH` and `--treasure-capture-menu PATH`. The equivalent prefixes are `--signalhunt`, `--rally`, and `--wings` for the other app shells.

The UTC daily challenge alternates stages automatically. To inspect a specific stage in a development build, add either `--signalhunt-stage city` or `--signalhunt-stage island` to the player command line.

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

Use `signal-hunt`, `waypoint-wings`, or `waypoint-rally` for a single app, or `all` to deploy the complete suite. The phone must be paired, reachable on the local network, and have Developer Mode enabled. The script uses an existing Apple Development certificate and device provisioning profile; it never accepts or stores an Apple password or API key.

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
