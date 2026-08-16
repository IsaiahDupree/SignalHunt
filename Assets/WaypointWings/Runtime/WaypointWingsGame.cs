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
using WaypointWings.Core;
using WaypointWings.Gameplay;
using WaypointWings.UI;
using WaypointWings.World;

namespace WaypointWings
{
    public sealed class WaypointWingsGame : MonoBehaviour
    {
        private GamePalette _palette;
        private FlightSession _session;
        private WingsHud _hud;
        private SignalHuntApi _api;
        private ReplayRun _latestRun;
        private DailyMontageExporter _filmExporter;

        private void Start()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadowDistance = 110f;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;

            var challenge = WingsDailyChallenge.Today();
            ConfigureRendering(challenge);
            _palette = new GamePalette();
            _palette.ApplyVehicleColor(PlayerCosmetics.VehicleColorIndex);
            var layout = FlightCourseGenerator.Generate(challenge);
            _session = gameObject.AddComponent<FlightSession>();
            FlightWorldBuilder.Build(layout, _palette, (index, id) => _session.PassGate(index, id));

            var aircraft = AircraftFactory.CreatePlayer(_palette, layout.playerSpawn, layout.playerRotation);
            var nameplate = WorldNameplate.Create(aircraft.transform, PlayerIdentity.DisplayName);
            _session.Initialize(challenge, aircraft, layout.gates.Count);
            _session.Completed += _ => nameplate.gameObject.SetActive(false);

            var camera = BuildCamera(aircraft);
            _hud = gameObject.AddComponent<WingsHud>();
            _hud.Initialize(_session, challenge);
            _hud.Track(aircraft);
            _hud.StyleRequested += () =>
            {
                _palette.ApplyVehicleColor(PlayerCosmetics.CycleVehicleColor());
                _hud.SetNetworkStatus("Aircraft signal color saved");
            };

            var recorder = gameObject.AddComponent<FlightRunRecorder>();
            recorder.Initialize(_session, aircraft, PlayerIdentity.InstallId, PlayerIdentity.DisplayName,
                PlayerCosmetics.VehicleColorIndex);
            recorder.RecordingCompleted += OnRecordingCompleted;

            var personalBest = ReplayStore.LoadBest(challenge.challengeId);
            if (personalBest?.frames != null && personalBest.frames.Count > 1)
            {
                var ghost = AircraftFactory.CreateReplayVisual(_palette, personalBest, 0, true);
                ghost.name = "Personal Best Flight Ghost";
                ghost.AddComponent<FlightGhostPlayback>().Initialize(personalBest, _session);
            }

            _api = gameObject.AddComponent<SignalHuntApi>();
            _api.Initialize();
            StartCoroutine(_api.FetchTopGhost(challenge, (topGhost, error) =>
            {
                if (topGhost?.frames == null || topGhost.frames.Count < 2)
                {
                    return;
                }
                var ghost = AircraftFactory.CreateReplayVisual(_palette, topGhost, 1, true);
                ghost.name = "Daily Leader Flight Ghost";
                ghost.AddComponent<FlightGhostPlayback>().Initialize(topGhost, _session);
            }));

            _filmExporter = gameObject.AddComponent<DailyMontageExporter>();
            _filmExporter.Initialize(camera, aircraft.transform, _palette,
                (run, index) => AircraftFactory.CreateReplayVisual(_palette, run, index, false));
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
                ? $"Flight {outcome.attemptNumber} is your new record · syncing pilots…"
                : $"Flight {outcome.attemptNumber} saved · syncing pilots…");
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
                    ? $"Daily flight board synced · {leaderboard.entries.Length} pilots"
                    : $"Daily flight rank #{rank.rank} · {leaderboard.entries.Length} pilots");
            }));
        }

        private IEnumerator ExportDailyFilm()
        {
            var local = ReplayStore.LoadHistory(_session.Challenge.challengeId, 16);
            if (!_api.IsConfigured)
            {
                _hud.SetNetworkStatus($"Building film from {local.Length} flight{(local.Length == 1 ? string.Empty : "s")} on this device");
                _filmExporter.Export(local);
                yield break;
            }

            _hud.SetNetworkStatus("Loading today's pilots…");
            ReplayRun[] remote = null;
            string error = null;
            yield return _api.FetchDailyMontage(_session.Challenge, 16, (runs, message) =>
            {
                remote = runs;
                error = message;
            });
            var merged = MergeRuns(remote, local, 16);
            _hud.SetNetworkStatus(string.IsNullOrEmpty(error)
                ? $"Loaded {merged.Length} pilot{(merged.Length == 1 ? string.Empty : "s")} for today's film"
                : $"{error} · using {merged.Length} local flight{(merged.Length == 1 ? string.Empty : "s")}");
            _filmExporter.Export(merged);
        }

        private static ReplayRun[] MergeRuns(IReadOnlyList<ReplayRun> primary, IReadOnlyList<ReplayRun> secondary, int limit)
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

        private static FollowCamera BuildCamera(AircraftController aircraft)
        {
            var instance = new GameObject("Main Camera");
            instance.tag = "MainCamera";
            var camera = instance.AddComponent<Camera>();
            camera.fieldOfView = 66f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 900f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.33f, 0.67f, 0.88f);
            var follow = instance.AddComponent<FollowCamera>();
            follow.ConfigureGameplay(10.5f, 14f, 5.2f, 6.5f, 64f, 77f, 46f);
            follow.SetSpeedProvider(() => aircraft.Speed);
            follow.SetTarget(aircraft.transform);
            return follow;
        }

        private static void ConfigureRendering(DailyChallenge challenge)
        {
            var archipelago = challenge.stageKey == "archipelago";
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = archipelago ? 0.0015f : 0.0025f;
            RenderSettings.fogColor = archipelago
                ? new Color(0.72f, 0.84f, 0.91f)
                : new Color(0.04f, 0.08f, 0.20f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = archipelago
                ? new Color(0.68f, 0.82f, 0.96f)
                : new Color(0.18f, 0.27f, 0.55f);
            RenderSettings.ambientEquatorColor = archipelago
                ? new Color(0.62f, 0.55f, 0.42f)
                : new Color(0.08f, 0.12f, 0.28f);
            RenderSettings.ambientGroundColor = archipelago
                ? new Color(0.20f, 0.28f, 0.25f)
                : new Color(0.02f, 0.025f, 0.08f);

            var sunObject = new GameObject(archipelago ? "Sunrise Key Light" : "Skyway Moon Light");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = archipelago ? new Color(1f, 0.78f, 0.48f) : new Color(0.42f, 0.62f, 1f);
            sun.intensity = archipelago ? 1.35f : 1.15f;
            sun.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(38f, -34f, 0f);

            var rimObject = new GameObject("Flight Rim Light");
            var rim = rimObject.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = archipelago ? new Color(0.22f, 0.78f, 1f) : new Color(1f, 0.18f, 0.72f);
            rim.intensity = 0.38f;
            rim.shadows = LightShadows.None;
            rimObject.transform.rotation = Quaternion.Euler(26f, 145f, 0f);
        }

        private static IEnumerator CaptureIfRequested(FlightSession session, DailyMontageExporter exporter)
        {
            var arguments = Environment.GetCommandLineArgs();
            var film = Array.IndexOf(arguments, "--wings-capture-film");
            if (film >= 0 && film + 1 < arguments.Length)
            {
                yield return new WaitForSeconds(3.25f);
                FlightInputState.Set(FlightControl.Boost, true);
                FlightInputState.Set(FlightControl.TurnRight, true);
                yield return new WaitForSeconds(1.5f);
                FlightInputState.Set(FlightControl.TurnRight, false);
                FlightInputState.Set(FlightControl.PitchUp, true);
                yield return new WaitForSeconds(1.5f);
                FlightInputState.Set(FlightControl.PitchUp, false);
                yield return new WaitForSeconds(1.5f);
                FlightInputState.Clear();
                CompleteCourse(session);
                yield return new WaitForSeconds(0.5f);
                exporter.Export(ReplayStore.LoadHistory(session.Challenge.challengeId, 16));
                yield return new WaitForSeconds(7.5f);
                ScreenCapture.CaptureScreenshot(arguments[film + 1], 1);
                yield return new WaitForSeconds(1f);
                Application.Quit(0);
                yield break;
            }

            var result = Array.IndexOf(arguments, "--wings-capture-result");
            if (result >= 0 && result + 1 < arguments.Length)
            {
                yield return new WaitForSeconds(3.25f);
                CompleteCourse(session);
                yield return new WaitForSeconds(0.8f);
                ScreenCapture.CaptureScreenshot(arguments[result + 1], 1);
                yield return new WaitForSeconds(1f);
                Application.Quit(0);
                yield break;
            }

            var gameplay = Array.IndexOf(arguments, "--wings-capture");
            if (gameplay < 0 || gameplay + 1 >= arguments.Length)
            {
                yield break;
            }
            yield return new WaitForSeconds(3.2f);
            FlightInputState.Set(FlightControl.Boost, true);
            yield return new WaitForSeconds(0.3f);
            FlightInputState.Clear();
            yield return new WaitForSeconds(0.1f);
            ScreenCapture.CaptureScreenshot(arguments[gameplay + 1], 1);
            yield return new WaitForSeconds(1f);
            Application.Quit(0);
        }

        private static void CompleteCourse(FlightSession session)
        {
            for (var index = session.ClearedCount; index < session.Challenge.collectibleCount; index++)
            {
                session.PassGate(index, $"gate-{index + 1:D2}");
            }
        }

        private void OnDestroy()
        {
            FlightInputState.Clear();
            _palette?.Dispose();
        }
    }
}
