# Signal Hunt — App Store listing draft

Status: source-backed working draft for iOS 1.0 (`com.isaiahdupree.signalhunt`); not saved to App Store Connect. Reviewed 2026-10-22 against the current repository and public U.S. App Store category examples.

## Proposed metadata

**Name (11/30):** Signal Hunt  
**Subtitle (26/30):** Find Ten Signals Every Day  
**Candidate category:** Games; validate the current App Store Connect category/subcategory options.  
**Promotional text (96/170):** One shared island challenge each day: steer, brake, find ten signals, and chase your best route.  
**Keywords (87/100 ASCII bytes):** driving,arcade,island,scavenger,collect,detect,explore,ghost,leaderboard,replay,offline

**Description:**

Find ten hidden signals on a compact island course.

Each UTC day, players receive the same generated island and signal layout. Your car moves forward automatically. Steer with the on-screen pad, brake when the route calls for it, and collect all ten signals.

Finish a run, keep your local attempt history, and try again to beat your best. When the leaderboard service is configured and reachable, compare daily runs and view replay ghosts. The core game remains playable offline; the app reports when a result cannot sync.

## Screenshot sequence

Capture the actual submitted iPhone build. The repository has no gameplay screenshot assets, and the September 20 build has not been source-pinned to this checkout. Do not use the app icons or rendered mock screens as store screenshots.

1. Daily island and ten-signal goal — “Find Ten Signals”
2. Steering pad in the playable course — “Steer the Island Route”
3. Signal collection state — “Track Your Progress”
4. Finish and personal-best comparison — “Retry. Beat Your Best.”
5. Daily leaderboard/ghost only after the production service and displayed player data are verified.

## Market evidence and positioning

Signal Hunt sits beside established hidden-object and scavenger-hunt games such as [Scavenger Hunt!](https://apps.apple.com/us/app/scavenger-hunt/id1613687713), which uses map-based object collection and progression, and [Hidmont](https://apps.apple.com/us/app/hidmont-hidden-objects-games/id1509453362), which includes a daily challenge. Public listings show strong category activity, but do not provide keyword search volume or prove demand for Signal Hunt. Its defensible difference is a short 3D driving-and-detection loop on one shared, generated daily island, with local-first retries.

Search terms are relevance hypotheses drawn from product behavior and public competitor vocabulary, not measured ranking or volume data. The app has no App Store reviews or conversion history yet.

## Open release gates

- Resolve the exact attached-build/source relationship. The VALID build 1 was uploaded September 20; current source facts are not a binary attestation.
- Publish and verify app-specific support and privacy pages before setting either URL. The shared `sonance-apps/privacy/` page does not currently cover this game.
- Complete Apple's age-rating questionnaire and content-rights declaration after checking player names, leaderboard/replay display, backend behavior, and asset licenses. Do not infer these answers from the game genre.
- Decide app price; no approved price hypothesis or measured willingness-to-pay is recorded. No in-app purchase or ad implementation is described by the current product README.
- Complete export-compliance answer against the exact submitted binary and bundled SDKs.
- Capture genuine iPhone screenshots from the exact release build. No gameplay capture is checked in.
- Verify the production Supabase configuration and live leaderboard before making unconditional online-feature claims.
- Keep App Privacy unpublished until the actual binary, SDKs, backend flow, and retention are reviewed.

## Competitor-derived quality risks

Public reviews of daily challenge games mention unavailable challenges and lost progress. These are competitor reports, not feedback on Signal Hunt. Preserve local attempts and make offline/network status explicit; verify both against the exact App Store build before claiming them.

No ASC write, build upload, review submission, or gameplay-source change is included in this draft.
