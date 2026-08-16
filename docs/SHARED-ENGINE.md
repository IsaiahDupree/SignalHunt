# Shared daily challenge engine

The product family should ship as three distinct store apps with one deliberately small shared engine. Players should immediately understand each app's verb, while identity, daily competition, replays, and cosmetics remain network-wide.

## Product boundary

| App | Primary verb | World axis | Completion rule | Social replay hook |
|---|---|---:|---|---|
| Signal Hunt | Search and collect | Ground | Find all relics | Missed-item route and hunt ghosts |
| Waypoint Rally | Drive and route | Ground | Hit ordered checkpoints | Shortcut comparison and ghost race |
| Waypoint Wings | Fly and thread gaps | 3D | Clear airborne checkpoints | Best flight path and closest calls |

The shared runtime owns challenge identity, deterministic random generation, profiles, cosmetics, leaderboards, replay envelopes, local storage, authentication, and share metadata. App-specific packages own vehicle physics, world-template rules, completion scoring, HUD vocabulary, and camera-event detection.

## Current extraction seams

```text
SignalHunt.Core
  DailyChallenge
  DeterministicRandom

SignalHunt.World
  TownLayout data contract
  TownLayoutGenerator
  TownWorldBuilder

SignalHunt.Replay
  ReplayRun / ReplayFrame envelope
  RunRecorder
  GhostPlayback
  CinematicReplayExporter

SignalHunt.Backend
  anonymous session
  daily challenge and leaderboard RPC client
```

When the second app begins, move `Core`, the replay data envelope, backend auth, and common profile/cosmetic types into a Unity Package Manager package such as `com.isaiahdupree.daily-challenge`. Do not extract app vocabulary or world rendering prematurely.

## Backend contract

The database already accepts `signal-hunt`, `waypoint-rally`, and `waypoint-wings` app keys. It stores:

- one immutable config per app/day;
- authenticated player profiles;
- every validated attempt;
- compact replay JSON for reconstruction and ghost delivery;
- shared or app-specific cosmetic catalog entries;
- player inventory.

Leaderboard reads use one personal-best run per player. Raw tables remain protected by row-level security; authenticated clients only execute constrained RPC functions.

Before network launch, add server-side replay verification. Re-run the deterministic layout from the stored generation version, validate snapshot speed/acceleration bounds and collectible intersection order, and only then mark a run leaderboard-eligible.

## Next slices

1. **Signal Hunt closed alpha**
   - Apply the migration and enable Supabase anonymous auth.
   - Add native display-name editing and a results leaderboard panel.
   - Profile the generated town on the oldest supported iPhone.
   - Add App Store privacy strings, icons, launch screen, signing, and TestFlight automation.

2. **Retention instrumentation**
   - Record challenge opened, countdown started, relic collected, run completed, replay watched, replay exported, and next-day return.
   - Measure completion rate, median run duration, retry rate, share rate, and D1/D7 retention.

3. **Waypoint Rally shell**
   - Reuse the town model and hover controller.
   - Replace relic placement with ordered checkpoints and free-route scoring.
   - Reuse the replay envelope for side-by-side ghosts and shortcut heatmaps.

4. **Waypoint Wings shell**
   - Add the flight controller and vertical course template.
   - Reuse identity, daily seed, replay, leaderboard, cosmetics, and export systems.

## Launch test

The MVP is working as a product only if a new player can understand the verb without a tutorial, finish a run in two to five minutes, immediately understand how their route differed, and feel a reason to return for tomorrow's shared seed. Graphics are supporting evidence; the daily ritual is the product.
