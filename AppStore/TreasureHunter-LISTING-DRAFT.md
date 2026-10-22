# Treasure Hunter: Daily Island — App Store listing draft

Status: working draft for iOS 1.0 (`com.isaiahdupree.treasurehunter`). Source reviewed 2026-10-22. This is not yet saved to App Store Connect.

## Proposed metadata

**Name (29/30):** Treasure Hunter: Daily Island  
**Subtitle (28/30):** Scan, Search, Beat Your Best  
**Primary category:** Games  
**Promotional text (112/170):** A new shared island hunt every day. Find ten relics, follow your detector, and try again to beat your best time.  
**Keywords (99/100 ASCII bytes):** artifact,relic,detector,scavenger,hunt,adventure,collect,explore,ghost,leaderboard,offline,speedrun

**Description:**

Every day brings a new island hunt. Explore a sunny trail, find ten hidden artifacts, and see how quickly you can clear the map.

Move in any direction with the on-screen pad and tap SCAN. Each pulse points you toward the nearest unfound cache with its strength, distance, and bearing. Follow the clue and collect all ten.

Everyone receives the same daily island layout. Retry as often as you like, save attempts on your device, and race your personal best. When online play is available, compare daily scores and watch replay ghosts from top runs.

No sign-up is required. The core hunt works offline. If the leaderboard service is unavailable, your attempt stays local and the game tells you.

## Screenshot sequence

Use captures from the actual iPhone build; do not create illustrative or fabricated gameplay. Apple shows up to three screenshots in search results, so make those tell the core loop:

1. Sunny island view and ten-artifact goal — “A New Hunt Every Day”
2. Scanner HUD showing clue values — “Follow Strength, Distance & Bearing”
3. Found-artifact progress — “Collect All Ten”
4. Finish screen and personal best — “Retry. Beat Your Best.”
5. Daily leaderboard / replay ghost, only if production service is live and test data is fictional.

The source checkout contains no App Store gameplay screenshots. A genuine device capture is still required.

## Product and market evidence

Source confirms a deterministic UTC daily island, ten artifact locations, two-axis movement, a scan pulse, nearest-artifact strength/distance/bearing, unlimited local attempts, personal-best replay, and optional online leaderboard/ghosts. Offline mode remains playable and explicitly reports unavailable online results. The title, subtitle, and description describe these behaviors only.

Current U.S. App Store listings show adjacent products taking different approaches: [Daily Treasure Hunt](https://apps.apple.com/us/app/daily-treasure-hunt/id6752296168) offers daily map puzzles, optional boosts, and multiplayer; [Our Treasure Hunt](https://apps.apple.com/us/app/our-treasure-hunt/id1413911129) centers on user-created, real-world location hunts; [Finders Sweepers Treasure Hunt](https://apps.apple.com/us/app/finders-sweepers-treasure-hunt/id1586844563?platform=watch) combines artifact collection with Minesweeper-like clues and level progression. Treasure Hunter's clearest defensible difference is a short 3D movement-and-scanner loop on a shared daily-generated island, with a local-first attempt history.

These listings show category conventions, not keyword volume or proof of demand. The owner strategy packet reports stale market inputs, no current direct-demand evidence, and no verified TubeLab nomination; no market outcome or store conversion data exists for this unreleased app. Treat the proposed keyword set and positioning as an initial hypothesis, then evaluate App Store impressions, product-page views, downloads, first-hunt completion, and return play after release.

## Open readiness gates

- Publish and verify the app-specific support/privacy pages in `sonance-apps` before setting those URLs in ASC.
- Reconcile App Privacy against the anonymous Supabase identity, install UUID, submitted scores/display name, and replay frames. Preserve the current declaration until its exact state and a complete SDK/build review are verified.
- Complete Apple's age-rating questionnaire. Publicly rendered leaderboard names and replay ghosts may affect the user-generated-content answer; do not infer the rating from source alone.
- Decide content-rights declaration after final asset/license and online replay review.
- Decide initial price. The app currently has no implemented IAP, subscription, or ad SDK. Adjacent games are commonly free with optional purchases, but that is not measured willingness-to-pay for this game.
- Upload a full set of real iPhone screenshots. No screenshot files are present in this checkout; capture requires the signed iOS app on a device or simulator.
- Verify that the Supabase production backend and migrations are live before describing online features as currently available.

No ASC write, build upload, review submission, or gameplay-source change is included in this draft.
