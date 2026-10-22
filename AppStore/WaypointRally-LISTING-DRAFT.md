# Waypoint Rally — App Store listing draft

Status: source-backed working draft for iOS 1.0 (`com.isaiahdupree.waypointrally`); not saved to App Store Connect. Reviewed 2026-10-22 against the current repository and public U.S. App Store listings.

## Proposed metadata

**Name (14/30):** Waypoint Rally  
**Subtitle (26/30):** Race the Same Daily Course  
**Candidate category:** Games / Racing; validate the available ASC category taxonomy.  
**Promotional text (93/170):** One shared island loop a day. Clear ten checkpoints, retry your line, and beat your own best.  
**Keywords (90/100 ASCII bytes):** car,driving,checkpoint,arcade,ghost,leaderboard,lap,steering,brake,route,challenge,offline

**Description:**

Race the Palm Coast Loop, a sunny island course shared by every player for the UTC day.

Your car accelerates automatically. Drag the steering pad to follow the road through ten ordered checkpoints, and hold BRAKE when a turn needs it. Complete a run, keep your attempts, and try again to beat your personal best.

When the leaderboard service is configured and reachable, compare daily times and replay ghosts from other runs. If you are offline, the game keeps local attempts and tells you when online results cannot sync.

## Screenshot sequence

Use captures from the actual submitted iPhone build. No gameplay screenshots are present in this checkout; the September 20 build 1 has no exact source-commit attestation.

1. Palm Coast Loop and checkpoint counter — “Ten Checkpoints. One Daily Loop.”
2. Steering through a marked checkpoint — “Choose Your Line”
3. Brake control before a turn — “Brake When the Road Bends”
4. Finish and local best — “Run It Again. Beat Your Best.”
5. Connected daily leaderboard/ghost only after production data and player display are verified.

## Market evidence and positioning

Daily and ghost racing is an active App Store segment. [Race The Sun: Daily Challenge Edition](https://apps.apple.com/us/app/race-the-sun-challenge-edition/id1454414817) positions short sessions around daily challenges. [Fastest Lap: Top Down Racing](https://apps.apple.com/us/app/fastest-lap-top-down-racing/id6795142974) and [The Perfect Race](https://apps.apple.com/us/app/the-perfect-race/id6800795522) emphasize clean laps and ghost racing. The product difference supported by source is an automatically moving arcade car on a shared, deterministic island route, controlled with steering and brake, with ten ordered checkpoints and a local-first attempt history.

These public listings inform category language and positioning only; they do not reveal exact keyword volume or prove a market for Waypoint Rally. The app has no public ratings, reviews, or store conversion data.

## Open release gates

- Pin the September 20 VALID build 1 to its exact source commit before approving listing claims or review.
- Publish and verify app-specific support and privacy pages; the shared policy page does not currently cover this game.
- Complete age-rating and content-rights answers after reviewing player names, leaderboard/ghost content, backend behavior, and asset licenses.
- Decide the app price. No owner-approved price or willingness-to-pay evidence is recorded; do not infer a free schedule from the absence of an IAP in the product README.
- Complete export compliance against the exact binary and included SDKs.
- Capture real gameplay screenshots from the exact submitted build; no gameplay capture is checked in.
- Verify the production leaderboard service before describing connected leaderboards or other players' ghosts as available.
- Review App Privacy against the shipped build and data flow before publishing a declaration.

## Competitor-derived quality risks

Public daily-challenge reviews in adjacent games mention challenges failing to load. Those reports are not Waypoint Rally feedback. Preserve the deterministic local course and attempt history when network access is unavailable; verify that behavior in the release binary before claiming it.

No ASC write, build upload, review submission, or gameplay-source change is included in this draft.
