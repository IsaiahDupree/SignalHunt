# Waypoint Wings physical-iPhone acceptance

This check closes the one remaining device gap for PR #3: install the existing
signed Waypoint Wings bundle on the paired iPhone, launch it, and complete one
full 12-gate course. It does not rebuild the app or reopen the already-passing
Unity EditMode, Unity PlayMode, or macOS verification.

## Pass boundary

All of the following are required:

1. The existing bundle passes strict code-signature verification.
2. `devicectl` successfully reaches the paired iPhone and installs the bundle.
3. `devicectl` launches `com.isaiahdupree.waypointwings`.
4. The Waypoint Wings start menu appears.
5. `PLAY` starts the course.
6. The player completes all 12 gates and reaches the result screen.
7. No crash, hang, or blocking error occurs during that run.
8. A screenshot or short recording shows the completed result screen.

The earlier `tunnelState: unavailable` condition is considered cleared by a
successful `devicectl` install and launch. It is not necessary to rerun any
desktop or Unity checks to prove device transport recovered.

## Run the check

Use the build-source commit recorded in the PR body. For PR #3's existing
signed bundle, that commit is `544dcc92c6eb3645574b1d23aebeed4f75bc4805`.
Before running it, check out the PR branch with a clean working tree; the helper
also refuses to accept an old bundle over uncommitted product-source changes.

```bash
./scripts/accept-waypoint-wings-device.sh \
  "$IOS_DEVICE_ID" \
  544dcc92c6eb3645574b1d23aebeed4f75bc4805
```

The helper deliberately uses the existing bundle under
`Builds/WaypointWings-Derived/Build/Products/Debug-iphoneos/`. It refuses to
proceed if product source changed after the supplied build commit. Changes to
only this script, this document, and the evidence gitignore rule are permitted.

After launch, complete the full course, save a screenshot or short recording of
the result screen, type `PASS`, and provide the evidence file path. The helper
creates an ignored local evidence directory under `artifacts/device-acceptance/`
containing:

- a PR-ready Markdown comment;
- the bundle-content SHA-256 manifest;
- the completion-evidence SHA-256;
- the raw command transcript and device details.

Do not commit or paste the raw transcript or device-details file; they can
contain the iPhone UDID and other identifiers. Post the generated Markdown
comment and attach only the completion screenshot or recording.

## PR comment template

The helper fills the bracketed values and checklist automatically in
`pr-evidence.md`:

```markdown
### Waypoint Wings iPhone acceptance — PASS

- Acceptance time (UTC): `[timestamp]`
- Device reference: `device-[redacted hash]`
- Built product commit: `544dcc92c6eb3645574b1d23aebeed4f75bc4805`
- PR head checked: `[head SHA]`
- Bundle: `com.isaiahdupree.waypointwings`, version `[version (build)]`
- App manifest SHA-256: `[digest]`
- Completion evidence: `[screenshot-or-recording filename]`
- Evidence SHA-256: `[digest]`

Acceptance performed:

- [x] Existing signed bundle passed `codesign --verify --deep --strict`.
- [x] `devicectl` reached the paired iPhone, installed the bundle, and launched it.
- [x] Waypoint Wings reached its start menu.
- [x] PLAY started the course.
- [x] All 12 gates were completed and the result screen appeared.
- [x] No crash, hang, or blocking error occurred during the run.

Scope note: no Unity tests, macOS builds, or gameplay code were rerun or changed
for this device-only acceptance check. The completion capture is attached.
```

Once the comment and attachment are on PR #3, replace its iPhone-status note
with the passing result, mark the draft ready for review, and merge only if the
PR remains mergeable and no new product-source changes or review blockers have
appeared.
