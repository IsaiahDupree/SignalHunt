# Signal Hunt agent instructions

- Preserve deterministic generation across platforms. Any generator behavior change requires a new generation/world-template version.
- Do not introduce mock leaderboards, fake players, placeholder network responses, or hardcoded credentials.
- The game must remain honestly playable offline; clearly label network-unavailable states.
- Never send the Supabase service-role key to a client build. Inject only the public anonymous key at build time.
- Keep Signal Hunt, Waypoint Rally, and Waypoint Wings as separate app shells over the shared daily/replay/backend contracts.
- Verify gameplay changes with EditMode tests, PlayMode smoke tests, and at least one standalone player build.
