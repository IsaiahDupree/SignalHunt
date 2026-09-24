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

        private IEnumerator Start()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadowDistance = 110f;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;

            var challenge = WingsDailyChallenge.Today();
            var startMenu = gameObject.AddComponent<DailyStartMenu>();
            startMenu.Initialize(challenge);
            if (startMenu.CaptureIfRequested("--wings-capture-loading"))
            {
                yield break;
            }
            yield return null;
            startMenu.SetLoadingProgress(0.24f, "READING TODAY'S SKY SEED");
            ConfigureRendering(challenge);
            _palette = new GamePalette();
            _palette.ApplyVehicleColor(PlayerCosmetics.VehicleColorIndex);
            var layout = FlightCourseGenerator.Generate(challenge);
            _session = gameObject.AddComponent<FlightSession>();
            FlightWorldBuilder.Build(layout, _palette, (index, id) => _session.PassGate(index, id));
            startMenu.SetLoadingProgress(0.66f, "RAISING ISLANDS AND AIR GATES");
            yield return new WaitForSecondsRealtime(0.16f);

            var aircraft = AircraftFactory.CreatePlayer(_palette, layout.playerSpawn, layout.playerRotation);
            var nameplate = WorldNameplate.Create(aircraft.transform, PlayerIdentity.DisplayName);
            _session.Initialize(challenge, aircraft, layout.gates.Count);
            _session.Completed += _ => nameplate.gameObject.SetActive(false);

            var camera = BuildCamera(aircraft, challenge);
            var menuCamera = camera.gameObject.AddComponent<DailyMenuCameraMotion>();
            menuCamera.Initialize(camera, aircraft.transform, challenge);
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
                menuCamera.HideActor(ghost);
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
                if (menuCamera != null)
                {
                    menuCamera.HideActor(ghost);
                }
                ghost.AddComponent<FlightGhostPlayback>().Initialize(topGhost, _session);
            }));

            _filmExporter = gameObject.AddComponent<DailyMontageExporter>();
            _filmExporter.Initialize(camera, aircraft.transform, _palette,
                (run, index) => AircraftFactory.CreateReplayVisual(_palette, run, index, false));
            _filmExporter.StatusChanged += _hud.SetNetworkStatus;
            var gameStarted = false;
            _filmExporter.PresentationModeChanged += active => _hud.SetPresentationMode(active || !gameStarted);
            _filmExporter.PresentationModeChanged += active =>
            {
                startMenu.SetPresentationMode(active);
                if (!active && !gameStarted)
                {
                    menuCamera.ResumeAfterReplay();
                }
            };
            _hud.WatchRequested += () =>
            {
                var run = _latestRun ?? ReplayStore.LoadLatest(challenge.challengeId);
                _filmExporter.Export(run == null ? Array.Empty<ReplayRun>() : new[] { run });
            };
            _hud.FilmRequested += () => StartCoroutine(ExportDailyFilm());

            _hud.SetPresentationMode(true);
            startMenu.StyleRequested += () =>
            {
                _palette.ApplyVehicleColor(PlayerCosmetics.CycleVehicleColor());
                startMenu.NotifyStyleChanged(PlayerCosmetics.VehicleColorIndex);
            };
            startMenu.FilmRequested += () =>
            {
                menuCamera.PauseForReplay();
                StartCoroutine(ExportDailyFilm());
            };
            startMenu.PlayRequested += () =>
            {
                gameStarted = true;
                menuCamera.Finish();
                _hud.SetPresentationMode(false);
                _session.Begin();
                StartCoroutine(CaptureIfRequested(_session, _filmExporter));
            };
            startMenu.SetLoadingProgress(0.92f, "CALCULATING FLIGHT LINES");
            yield return new WaitForSecondsRealtime(0.28f);
            startMenu.SetLoadingProgress(1f, "TODAY'S SKYWAY IS READY");
            yield return new WaitForSecondsRealtime(0.18f);
            startMenu.ShowMenu();
            var menuCapture = startMenu.CaptureIfRequested("--wings-capture-menu");
            if (!menuCapture && HasCaptureRequest("--wings-capture", "--wings-capture-result",
                    "--wings-capture-film"))
            {
                startMenu.BeginForAutomation();
            }
        }

        private static bool HasCaptureRequest(params string[] markers)
        {
            var arguments = Environment.GetCommandLineArgs();
            foreach (var marker in markers)
            {
                if (Array.IndexOf(arguments, marker) >= 0)
                {
                    return true;
                }
            }
            return false;
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

        private static FollowCamera BuildCamera(AircraftController aircraft, DailyChallenge challenge)
        {
            var instance = new GameObject("Main Camera");
            instance.tag = "MainCamera";
            var camera = instance.AddComponent<Camera>();
            camera.fieldOfView = 66f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 900f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.38f, 0.72f, 0.92f);
            var follow = instance.AddComponent<FollowCamera>();
            follow.ConfigureGameplay(10.5f, 14f, 5.2f, 6.5f, 64f, 77f, 46f);
            follow.SetSpeedProvider(() => aircraft.Speed);
            follow.SetTarget(aircraft.transform);
            return follow;
        }

        private static void ConfigureRendering(DailyChallenge challenge)
        {
            _ = challenge;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0013f;
            RenderSettings.fogColor = new Color(0.76f, 0.88f, 0.94f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.86f, 0.98f);
            RenderSettings.ambientEquatorColor = new Color(0.66f, 0.60f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.32f, 0.27f);

            var sunObject = new GameObject("Island Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.86f, 0.62f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(38f, -34f, 0f);

            var rimObject = new GameObject("Flight Rim Light");
            var rim = rimObject.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = new Color(0.22f, 0.78f, 1f);
            rim.intensity = 0.30f;
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
            yield return new WaitForSeconds(3.05f);
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
