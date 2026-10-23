# Treasure Hunter: Daily Island — App Store listing draft

Status: source-reviewed iOS 1.0 listing for `com.isaiahdupree.treasurehunter`. App Store listing fields, age rating, content-rights declaration, App Review details, price, support URL, and privacy URL are saved for the exact ASC version. The local-only support/privacy pages are live. One API blocker remains: screenshots (0 uploaded). The attached build predates the local-only source change and must be replaced before review.

## Proposed metadata

**Name (29/30):** Treasure Hunter: Daily Island  
**Subtitle (28/30):** Scan, Search, Beat Your Best  
**Primary category:** Games  
**Promotional text (103/170):** A new island hunt every day. Find ten relics, follow your detector, and try to beat your personal best.
**Keywords (90/100 ASCII bytes):** artifact,relic,detector,scavenger,hunt,adventure,collect,explore,offline,speedrun,treasure

**Description:**

Every day brings a new island hunt. Explore a sunny trail, find ten hidden artifacts, and see how quickly you can clear the map.

Move in any direction with the on-screen pad and tap SCAN. Each pulse points you toward the nearest unfound cache with its strength, distance, and bearing. Follow the clue and collect all ten.

The island changes each day. Retry as often as you like, save attempts on your device, and race your personal best. Your scores and replays stay on your device; there are no online leaderboards or player-submitted replays.

No sign-up or internet connection is required. Your daily hunt, attempt history, personal-best replay, and exported hunt film work offline.

## Screenshot sequence

Use captures from the actual iPhone build; do not create illustrative or fabricated gameplay. Apple shows up to three screenshots in search results, so make those tell the core loop:

1. Sunny island view and ten-artifact goal — “A New Hunt Every Day”
2. Scanner HUD showing clue values — “Follow Strength, Distance & Bearing”
3. Found-artifact progress — “Collect All Ten”
4. Finish screen and personal best — “Retry. Beat Your Best.”
5. Local result screen showing the personal-best time and saved attempt count.

The source checkout contains no App Store gameplay screenshots. A genuine device capture is still required.

## Product and market evidence

Source confirms a deterministic UTC daily island, ten artifact locations, two-axis movement, a scan pulse, nearest-artifact strength/distance/bearing, unlimited local attempts, personal-best replay, and locally rendered hunt films. Treasure Hunter does not submit player names, scores, or replays to the shared online leaderboard/replay backend. The title, subtitle, and description describe the local-first behaviors only.

Current U.S. App Store listings show adjacent products taking different approaches: [Daily Treasure Hunt](https://apps.apple.com/us/app/daily-treasure-hunt/id6752296168) offers daily map puzzles, optional boosts, and multiplayer; [Our Treasure Hunt](https://apps.apple.com/us/app/our-treasure-hunt/id1413911129) centers on user-created, real-world location hunts; [Finders Sweepers Treasure Hunt](https://apps.apple.com/us/app/finders-sweepers-treasure-hunt/id1586844563?platform=watch) combines artifact collection with Minesweeper-like clues and level progression. Treasure Hunter's clearest defensible difference is a short 3D movement-and-scanner loop on a deterministic UTC daily island, with a local-only attempt history.

These listings show category conventions, not keyword volume or proof of demand. The owner strategy packet reports stale market inputs, no current direct-demand evidence, and no verified TubeLab nomination; no market outcome or store conversion data exists for this unreleased app. Treat the proposed keyword set and positioning as an initial hypothesis, then evaluate App Store impressions, product-page views, downloads, first-hunt completion, and return play after release.

## Open readiness gates

- The local-only support and privacy pages are live at [support](https://isaiahdupree.github.io/sonance-apps/treasure-hunter/) and [privacy](https://isaiahdupree.github.io/sonance-apps/treasure-hunter/privacy/) after sonance-apps PR #25 merged. The exact ASC support/privacy URLs were already saved; both routes return HTTP 200.
- Reconcile App Privacy against the release binary, Unity packages/SDKs, and any data leaving the device. Gameplay source no longer sends an anonymous identity, score, display name, or replay for this app.
- Re-answer Apple's age-rating questionnaire and content-rights declaration from the replacement iOS build. The current ASC answers still describe the previous online leaderboard/replay features.
- Decide content-rights declaration from the final asset/license inventory. Removing other players' names and replays does not establish ownership of every art/audio asset.
- Permanent U.S. $0.00 base price is set and read back as a first-release validation choice. This does not assert measured willingness to pay or create a paid offer; the source has no IAP, subscription, or ad SDK.
- Upload a full set of real iPhone screenshots. No screenshot files are present in this checkout; capture requires the signed iOS app on a device or simulator.
- Build the next iOS release from this source and confirm the client has no path that initializes or calls the shared social leaderboard/replay APIs.
- App Review contact and notes are now saved on the exact iOS 1.0 version. The notes describe the replacement local-only build; do not submit the currently attached pre-change build.

Fresh exact-version ASC audit on 2026-10-23 confirms every API-visible field except screenshots (0 uploaded). The permanent USA $0.00 schedule and all listing metadata, including URLs, are saved and read back. App Privacy still requires UI review against the replacement binary and SDKs. The attached VALID build predates the local-only code change; a replacement iOS build must be prepared and source parity verified before review. No build upload or review submission is included in this draft. Existing Unity suite and macOS preview results do not replace the required iOS build/device capture.
