# Daily replay and film pipeline

Every app in the daily challenge network records the same compact replay envelope. The app-specific verb changes, but identity, attempts, ranking, playback, and social-video assembly do not.

## Round loop

```text
shared UTC seed
  → attempt with 10 Hz replay snapshots
  → local attempt history + personal-best comparison
  → authenticated raw-attempt submission
  → one-best-run-per-player leaderboard
  → retry against personal and daily ghosts
  → multi-racer Daily Film reconstruction
```

Every retry keeps the same `challengeId`, generation seed, world template, and collectible/checkpoint layout. Each attempt receives a unique `clientRunId`, making submissions idempotent while preserving every distinct run.

## Shared replay envelope

Schema version 2 includes:

- `appKey` and `modeKey` to route Signal Hunt, Waypoint Rally, or Waypoint Wings;
- immutable challenge, world-template, and generation-seed identity;
- client run ID, timestamp, player identity, vehicle identity, and cosmetic color;
- result and attempt number;
- timestamped position, rotation, speed, state, collection/checkpoint event, and camera event frames.

The game shells provide their own completion and score rules:

| App | Mode | Recorded event | Daily Film emphasis |
|---|---|---|---|
| Signal Hunt | `daily-hunt` | Relic collection | Diverging search routes and final discoveries |
| Waypoint Rally | `daily-race` | Ground checkpoint | Shortcuts, drifts, and side-by-side ghosts |
| Waypoint Wings | `daily-flight` | Air checkpoint | 3D flight paths, close calls, and vertical cuts |

## Daily Film

`daily_challenge_montage` returns a constrained set containing each racer's best validated replay. The client merges those network runs with unsynced local attempts, deduplicates by `clientRunId`, and reconstructs up to 16 racers.

The director time-compresses the source runs into a 12–24 second vertical film. Every three seconds it selects another racer and rotates through orbit, chase, overhead, and wide shots. On iOS, ReplayKit records the rendered sequence and presents the native save/share sheet; in Editor and macOS builds, the same sequence plays as an honest preview without claiming that a file was exported.

Each app shell injects its own replay-visual factory into the director. Signal Hunt currently supplies the hover car; Waypoint Rally can supply its car cosmetics, and Waypoint Wings can supply an aircraft, while timing, camera direction, overlay branding, and ReplayKit capture stay shared.

The database remains the durable source for all submitted attempts. The leaderboard and montage are derived views; neither discards raw runs.
