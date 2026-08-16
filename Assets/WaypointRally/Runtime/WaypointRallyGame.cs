using System;
using System.Collections;
using System.Collections.Generic;
using SignalHunt.Backend;
using SignalHunt.Core;
using SignalHunt.Gameplay;
using SignalHunt.Replay;
using SignalHunt.UI;
using SignalHunt.Visual;
using UnityEngine;
using WaypointRally.Core;
using WaypointRally.Gameplay;
using WaypointRally.UI;
using WaypointRally.World;

namespace WaypointRally
{
    public sealed class WaypointRallyGame : MonoBehaviour
    {
        private GamePalette _palette;
        private RallySession _session;
        private RallyHud _hud;
        private SignalHuntApi _api;
        private ReplayRun _latestRun;
        private DailyMontageExporter _filmExporter;

        private void Start()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadowDistance = 95f;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;

            var challenge = RallyDailyChallenge.Today();
            ConfigureRendering(challenge);
            _palette = new GamePalette();
            _palette.ApplyVehicleColor(PlayerCosmetics.VehicleColorIndex);
            var layout = RallyCourseGenerator.Generate(challenge);
            _session = gameObject.AddComponent<RallySession>();
            RallyWorldBuilder.Build(layout, challenge, _palette,
                (index, id) => _session.PassCheckpoint(index, id));

            var vehicle = RallyCarFactory.CreatePlayer(_palette, layout.playerSpawn, layout.playerHeading);
            var nameplate = WorldNameplate.Create(vehicle.transform, PlayerIdentity.DisplayName);
            _session.Initialize(challenge, vehicle, layout.checkpoints.Count);
            _session.Completed += _ => nameplate.gameObject.SetActive(false);

            var camera = BuildCamera(vehicle, challenge);
            _hud = gameObject.AddComponent<RallyHud>();
            _hud.Initialize(_session, challenge);
            _hud.Track(vehicle);
            _hud.StyleRequested += () =>
            {
                _palette.ApplyVehicleColor(PlayerCosmetics.CycleVehicleColor());
                _hud.SetNetworkStatus("Rally color saved");
            };

            var recorder = gameObject.AddComponent<RallyRunRecorder>();
            recorder.Initialize(_session, vehicle, PlayerIdentity.InstallId, PlayerIdentity.DisplayName,
                PlayerCosmetics.VehicleColorIndex);
            recorder.RecordingCompleted += OnRecordingCompleted;

            var personalBest = ReplayStore.LoadBest(challenge.challengeId);
            if (personalBest?.frames != null && personalBest.frames.Count > 1)
            {
                var ghost = RallyCarFactory.CreateReplayVisual(_palette, personalBest, 0, true);
                ghost.name = "Personal Best Rally Ghost";
                ghost.AddComponent<RallyGhostPlayback>().Initialize(personalBest, _session);
            }

            _api = gameObject.AddComponent<SignalHuntApi>();
            _api.Initialize();
            StartCoroutine(_api.FetchTopGhost(challenge, (topGhost, error) =>
            {
                if (topGhost?.frames == null || topGhost.frames.Count < 2)
                {
                    return;
                }
                var ghost = RallyCarFactory.CreateReplayVisual(_palette, topGhost, 1, true);
                ghost.name = "Daily Leader Rally Ghost";
                ghost.AddComponent<RallyGhostPlayback>().Initialize(topGhost, _session);
            }));

            _filmExporter = gameObject.AddComponent<DailyMontageExporter>();
            _filmExporter.Initialize(camera, vehicle.transform, _palette,
                (run, index) => RallyCarFactory.CreateReplayVisual(_palette, run, index, false));
            _filmExporter.StatusChanged += _hud.SetNetworkStatus;
            _filmExporter.PresentationModeChanged += _hud.SetPresentationMode;
            _hud.WatchRequested += () =>
            {
                var run = _latestRun ?? ReplayStore.LoadLatest(challenge.challengeId);
                _filmExporter.Export(run == null ? Array.Empty<ReplayRun>() : new[] { run });
            };
            _hud.FilmRequested += () => StartCoroutine(ExportDailyFilm());

            _session.Begin();
            StartCoroutine(CaptureIfRequested(_session, _filmExporter));
        }

        private void OnRecordingCompleted(ReplayRun run)
        {
            _latestRun = run;
            var outcome = ReplayStore.SaveAttempt(run);
            _hud.SetRunOutcome(outcome, run.result);
            _hud.SetNetworkStatus(outcome.isPersonalBest
                ? $"Race {outcome.attemptNumber} is your new record · syncing drivers…"
                : $"Race {outcome.attemptNumber} saved · syncing drivers…");
            StartCoroutine(_api.SubmitAndFetchLeaderboard(_session.Challenge, run, leaderboard =>
            {
                if (!string.IsNullOrEmpty(leaderboard.error))
                {
                    _hud.SetNetworkStatus(leaderboard.error);
                    _hud.SetLeaderboardUnavailable();
                    return;
                }
                _hud.SetLeaderboard(leaderboard.entries);
                var rank = Array.Find(leaderboard.entries,
                    entry => string.Equals(entry.playerName, PlayerIdentity.DisplayName, StringComparison.Ordinal));
                _hud.SetNetworkStatus(rank == null
                    ? $"Daily route board synced · {leaderboard.entries.Length} drivers"
                    : $"Daily route rank #{rank.rank} · {leaderboard.entries.Length} drivers");
            }));
        }

        private IEnumerator ExportDailyFilm()
        {
            var local = ReplayStore.LoadHistory(_session.Challenge.challengeId, 16);
            if (!_api.IsConfigured)
            {
                _hud.SetNetworkStatus(
                    $"Building film from {local.Length} race{(local.Length == 1 ? string.Empty : "s")} on this device");
                _filmExporter.Export(local);
                yield break;
            }

            _hud.SetNetworkStatus("Loading today's drivers…");
            ReplayRun[] remote = null;
            string error = null;
            yield return _api.FetchDailyMontage(_session.Challenge, 16, (runs, message) =>
            {
                remote = runs;
                error = message;
            });
            var merged = MergeRuns(remote, local, 16);
            _hud.SetNetworkStatus(string.IsNullOrEmpty(error)
                ? $"Loaded {merged.Length} driver{(merged.Length == 1 ? string.Empty : "s")} for today's film"
                : $"{error} · using {merged.Length} local race{(merged.Length == 1 ? string.Empty : "s")}");
            _filmExporter.Export(merged);
        }

        private static ReplayRun[] MergeRuns(IReadOnlyList<ReplayRun> primary, IReadOnlyList<ReplayRun> secondary,
            int limit)
        {
            var merged = new List<ReplayRun>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            AddRuns(primary, merged, ids, limit);
            AddRuns(secondary, merged, ids, limit);
            return merged.ToArray();
        }

        private static void AddRuns(IReadOnlyList<ReplayRun> source, ICollection<ReplayRun> destination,
            ISet<string> ids, int limit)
        {
            if (source == null)
            {
                return;
            }
            for (var index = 0; index < source.Count && destination.Count < limit; index++)
            {
                var run = source[index];
                if (run?.frames == null || run.frames.Count < 2)
                {
                    continue;
                }
                var id = string.IsNullOrWhiteSpace(run.clientRunId)
                    ? $"legacy-{DailyChallenge.StableHash(run.playerId + run.challengeId + run.result?.timeMs)}"
                    : run.clientRunId;
                if (ids.Add(id))
                {
                    destination.Add(run);
                }
            }
        }

        private static FollowCamera BuildCamera(HoverVehicleController vehicle, DailyChallenge challenge)
        {
            var instance = new GameObject("Main Camera");
            instance.tag = "MainCamera";
            var camera = instance.AddComponent<Camera>();
            camera.fieldOfView = 64f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 520f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = challenge.stageKey == "harbor-town"
                ? new Color(0.35f, 0.72f, 0.91f)
                : new Color(0.20f, 0.045f, 0.17f);
            var follow = instance.AddComponent<FollowCamera>();
            follow.ConfigureGameplay(7.2f, 9.6f, 4.2f, 3.0f, 61f, 74f, 30f);
            follow.SetTarget(vehicle.transform);
            return follow;
        }

        private static void ConfigureRendering(DailyChallenge challenge)
        {
            var town = challenge.stageKey == "harbor-town";
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = town ? 0.0032f : 0.0042f;
            RenderSettings.fogColor = town
                ? new Color(0.62f, 0.79f, 0.89f)
                : new Color(0.72f, 0.31f, 0.16f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = town
                ? new Color(0.64f, 0.82f, 0.98f)
                : new Color(0.98f, 0.46f, 0.20f);
            RenderSettings.ambientEquatorColor = town
                ? new Color(0.43f, 0.58f, 0.65f)
                : new Color(0.62f, 0.30f, 0.16f);
            RenderSettings.ambientGroundColor = town
                ? new Color(0.16f, 0.25f, 0.25f)
                : new Color(0.31f, 0.18f, 0.10f);

            var sunObject = new GameObject(town ? "Harbor Sun" : "Dustlands Sunset");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = town ? new Color(1f, 0.88f, 0.70f) : new Color(1f, 0.48f, 0.22f);
            sun.intensity = town ? 1.35f : 1.5f;
            sun.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(town ? 42f : 24f, -35f, 0f);

            var rimObject = new GameObject("Rally Rim Light");
            var rim = rimObject.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = town ? new Color(0.12f, 0.82f, 1f) : new Color(1f, 0.16f, 0.54f);
            rim.intensity = 0.42f;
            rim.shadows = LightShadows.None;
            rimObject.transform.rotation = Quaternion.Euler(28f, 142f, 0f);
        }

        private static IEnumerator CaptureIfRequested(RallySession session, DailyMontageExporter exporter)
        {
            var arguments = Environment.GetCommandLineArgs();
            var film = Array.IndexOf(arguments, "--rally-capture-film");
            if (film >= 0 && film + 1 < arguments.Length)
            {
                yield return new WaitForSeconds(3.15f);
                VehicleInputState.Set(VehicleControl.Accelerate, true);
                VehicleInputState.Set(VehicleControl.SteerRight, true);
                yield return new WaitForSeconds(1.25f);
                VehicleInputState.Set(VehicleControl.SteerRight, false);
                yield return new WaitForSeconds(2.5f);
                VehicleInputState.Clear();
                CompleteCourse(session);
                yield return new WaitForSeconds(0.5f);
                exporter.Export(ReplayStore.LoadHistory(session.Challenge.challengeId, 16));
                yield return new WaitForSeconds(7.5f);
                ScreenCapture.CaptureScreenshot(arguments[film + 1], 1);
                yield return new WaitForSeconds(1f);
                Application.Quit(0);
                yield break;
            }

            var result = Array.IndexOf(arguments, "--rally-capture-result");
            if (result >= 0 && result + 1 < arguments.Length)
            {
                yield return new WaitForSeconds(3.15f);
                CompleteCourse(session);
                yield return new WaitForSeconds(0.8f);
                ScreenCapture.CaptureScreenshot(arguments[result + 1], 1);
                yield return new WaitForSeconds(1f);
                Application.Quit(0);
                yield break;
            }

            var gameplay = Array.IndexOf(arguments, "--rally-capture");
            if (gameplay < 0 || gameplay + 1 >= arguments.Length)
            {
                yield break;
            }
            yield return new WaitForSeconds(3.15f);
            VehicleInputState.Set(VehicleControl.Accelerate, true);
            yield return new WaitForSeconds(0.38f);
            VehicleInputState.Clear();
            ScreenCapture.CaptureScreenshot(arguments[gameplay + 1], 1);
            yield return new WaitForSeconds(1f);
            Application.Quit(0);
        }

        private static void CompleteCourse(RallySession session)
        {
            for (var index = session.ClearedCount; index < session.Challenge.collectibleCount; index++)
            {
                session.PassCheckpoint(index, $"checkpoint-{index + 1:D2}");
            }
        }

        private void OnDestroy()
        {
            VehicleInputState.Clear();
            _palette?.Dispose();
        }
    }
}
