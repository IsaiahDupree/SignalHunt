using System;
using System.Collections;
using System.Collections.Generic;
using SignalHunt.Backend;
using SignalHunt.Core;
using SignalHunt.Gameplay;
using SignalHunt.Replay;
using SignalHunt.UI;
using SignalHunt.Visual;
using TreasureHunt.Core;
using TreasureHunt.Gameplay;
using TreasureHunt.UI;
using TreasureHunt.World;
using UnityEngine;

namespace TreasureHunt
{
    public sealed class TreasureHuntGame : MonoBehaviour
    {
        private GamePalette _palette;
        private TreasureSession _session;
        private TreasureHud _hud;
        private SignalHuntApi _api;
        private ReplayRun _latestRun;
        private DailyMontageExporter _filmExporter;

        private IEnumerator Start()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadowDistance = 95f;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;

            var challenge = TreasureDailyChallenge.Today();
            var startMenu = gameObject.AddComponent<DailyStartMenu>();
            startMenu.Initialize(challenge);
            if (startMenu.CaptureIfRequested("--treasure-capture-loading"))
            {
                yield break;
            }
            yield return null;
            startMenu.SetLoadingProgress(0.24f, "READING THE DAILY RELIC MAP");
            ConfigureRendering(challenge);
            _palette = new GamePalette();
            _palette.ApplyVehicleColor(PlayerCosmetics.VehicleColorIndex);
            var layout = TreasureWorldGenerator.Generate(challenge);
            _session = gameObject.AddComponent<TreasureSession>();
            TreasureWorldBuilder.Build(layout, challenge, _palette, id => _session.FindArtifact(id));
            startMenu.SetLoadingProgress(0.66f, "HIDING ARTIFACTS AND CLUES");
            yield return new WaitForSecondsRealtime(0.16f);

            var explorer = ExplorerFactory.CreatePlayer(_palette, layout.playerSpawn, layout.playerHeading);
            var nameplate = WorldNameplate.Create(explorer.transform, PlayerIdentity.DisplayName);
            nameplate.transform.localPosition = new Vector3(0f, 3.25f, 0f);
            nameplate.transform.localScale = Vector3.one * 0.0042f;
            _session.Initialize(challenge, explorer, layout.artifacts.Count);
            _session.Completed += _ => nameplate.gameObject.SetActive(false);
            explorer.ScanPulsed += origin => TreasureArtifact.ScanAll(origin, 34f);

            var camera = BuildCamera(explorer, challenge);
            var menuCamera = camera.gameObject.AddComponent<DailyMenuCameraMotion>();
            menuCamera.Initialize(camera, explorer.transform, challenge);
            _hud = gameObject.AddComponent<TreasureHud>();
            _hud.Initialize(_session, challenge);
            _hud.Track(explorer);
            _hud.StyleRequested += () =>
            {
                _palette.ApplyVehicleColor(PlayerCosmetics.CycleVehicleColor());
                _hud.SetNetworkStatus("Explorer signal color saved");
            };

            var recorder = gameObject.AddComponent<TreasureRunRecorder>();
            recorder.Initialize(_session, explorer, PlayerIdentity.InstallId, PlayerIdentity.DisplayName,
                PlayerCosmetics.VehicleColorIndex);
            recorder.RecordingCompleted += OnRecordingCompleted;

            var personalBest = ReplayStore.LoadBest(challenge.challengeId);
            if (personalBest?.frames != null && personalBest.frames.Count > 1)
            {
                var ghost = ExplorerFactory.CreateReplayVisual(_palette, personalBest, 0, true);
                ghost.name = "Personal Best Search Ghost";
                menuCamera.HideActor(ghost);
                ghost.AddComponent<TreasureGhostPlayback>().Initialize(personalBest, _session);
            }

            _api = gameObject.AddComponent<SignalHuntApi>();
            _api.Initialize();
            StartCoroutine(_api.FetchTopGhost(challenge, (topGhost, error) =>
            {
                if (topGhost?.frames == null || topGhost.frames.Count < 2)
                {
                    return;
                }
                var ghost = ExplorerFactory.CreateReplayVisual(_palette, topGhost, 1, true);
                ghost.name = "Daily Leader Search Ghost";
                if (menuCamera != null)
                {
                    menuCamera.HideActor(ghost);
                }
                ghost.AddComponent<TreasureGhostPlayback>().Initialize(topGhost, _session);
            }));

            _filmExporter = gameObject.AddComponent<DailyMontageExporter>();
            _filmExporter.Initialize(camera, explorer.transform, _palette,
                (run, index) => ExplorerFactory.CreateReplayVisual(_palette, run, index, false));
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
                StartCoroutine(CaptureIfRequested(_session, explorer, _filmExporter));
            };
            startMenu.SetLoadingProgress(0.92f, "CALIBRATING THE DETECTOR");
            yield return new WaitForSecondsRealtime(0.28f);
            startMenu.SetLoadingProgress(1f, "TODAY'S HUNT IS READY");
            yield return new WaitForSecondsRealtime(0.18f);
            startMenu.ShowMenu();
            var menuCapture = startMenu.CaptureIfRequested("--treasure-capture-menu");
            if (!menuCapture && HasCaptureRequest("--treasure-capture", "--treasure-capture-result",
                    "--treasure-capture-film"))
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
                ? $"Hunt {outcome.attemptNumber} is your new record · syncing hunters…"
                : $"Hunt {outcome.attemptNumber} saved · syncing hunters…");
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
                    ? $"Daily hunt board synced · {leaderboard.entries.Length} hunters"
                    : $"Daily hunt rank #{rank.rank} · {leaderboard.entries.Length} hunters");
            }));
        }

        private IEnumerator ExportDailyFilm()
        {
            var local = ReplayStore.LoadHistory(_session.Challenge.challengeId, 16);
            if (!_api.IsConfigured)
            {
                _hud.SetNetworkStatus(
                    $"Building film from {local.Length} search{(local.Length == 1 ? string.Empty : "es")} on this device");
                _filmExporter.Export(local);
                yield break;
            }
            _hud.SetNetworkStatus("Loading today's hunters…");
            ReplayRun[] remote = null;
            string error = null;
            yield return _api.FetchDailyMontage(_session.Challenge, 16, (runs, message) =>
            {
                remote = runs;
                error = message;
            });
            var merged = MergeRuns(remote, local, 16);
            _hud.SetNetworkStatus(string.IsNullOrEmpty(error)
                ? $"Loaded {merged.Length} hunter{(merged.Length == 1 ? string.Empty : "s")} for today's film"
                : $"{error} · using {merged.Length} local search{(merged.Length == 1 ? string.Empty : "es")}");
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

        private static FollowCamera BuildCamera(ExplorerController explorer, DailyChallenge challenge)
        {
            var instance = new GameObject("Main Camera");
            instance.tag = "MainCamera";
            var camera = instance.AddComponent<Camera>();
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 520f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = challenge.stageKey == "sunken-ruins"
                ? new Color(0.31f, 0.67f, 0.82f)
                : new Color(0.025f, 0.025f, 0.10f);
            var follow = instance.AddComponent<FollowCamera>();
            follow.ConfigureGameplay(6.4f, 7.6f, 3.8f, 2.1f, 60f, 67f, 9.2f);
            follow.SetSpeedProvider(() => explorer.Speed);
            follow.SetTarget(explorer.transform);
            return follow;
        }

        private static void ConfigureRendering(DailyChallenge challenge)
        {
            var ruins = challenge.stageKey == "sunken-ruins";
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = ruins ? 0.0032f : 0.0048f;
            RenderSettings.fogColor = ruins
                ? new Color(0.48f, 0.72f, 0.70f)
                : new Color(0.055f, 0.045f, 0.16f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ruins
                ? new Color(0.55f, 0.82f, 0.72f)
                : new Color(0.16f, 0.15f, 0.42f);
            RenderSettings.ambientEquatorColor = ruins
                ? new Color(0.32f, 0.48f, 0.32f)
                : new Color(0.12f, 0.08f, 0.28f);
            RenderSettings.ambientGroundColor = ruins
                ? new Color(0.09f, 0.24f, 0.12f)
                : new Color(0.025f, 0.02f, 0.07f);

            var sunObject = new GameObject(ruins ? "Ruins Sun" : "Crystal Moon");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = ruins ? new Color(1f, 0.84f, 0.58f) : new Color(0.36f, 0.52f, 1f);
            sun.intensity = ruins ? 1.35f : 1.1f;
            sun.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(ruins ? 42f : 31f, -38f, 0f);

            var rimObject = new GameObject("Treasure Rim Light");
            var rim = rimObject.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = ruins ? new Color(0.12f, 0.90f, 1f) : new Color(1f, 0.14f, 0.68f);
            rim.intensity = 0.48f;
            rim.shadows = LightShadows.None;
            rimObject.transform.rotation = Quaternion.Euler(24f, 145f, 0f);
        }

        private static IEnumerator CaptureIfRequested(TreasureSession session, ExplorerController explorer,
            DailyMontageExporter exporter)
        {
            var arguments = Environment.GetCommandLineArgs();
            var film = Array.IndexOf(arguments, "--treasure-capture-film");
            if (film >= 0 && film + 1 < arguments.Length)
            {
                yield return new WaitForSeconds(3.15f);
                ExplorerInputState.Set(ExplorerControl.Run, true);
                ExplorerInputState.Set(ExplorerControl.TurnRight, true);
                yield return new WaitForSeconds(1.5f);
                ExplorerInputState.Set(ExplorerControl.TurnRight, false);
                yield return new WaitForSeconds(2.3f);
                ExplorerInputState.Clear();
                CompleteHunt(session);
                yield return new WaitForSeconds(0.5f);
                exporter.Export(ReplayStore.LoadHistory(session.Challenge.challengeId, 16));
                yield return new WaitForSeconds(7.5f);
                ScreenCapture.CaptureScreenshot(arguments[film + 1], 1);
                yield return new WaitForSeconds(1f);
                Application.Quit(0);
                yield break;
            }

            var result = Array.IndexOf(arguments, "--treasure-capture-result");
            if (result >= 0 && result + 1 < arguments.Length)
            {
                yield return new WaitForSeconds(3.15f);
                Time.timeScale = 12f;
                yield return new WaitForSecondsRealtime(4.4f);
                Time.timeScale = 1f;
                CompleteHunt(session);
                yield return new WaitForSeconds(0.8f);
                ScreenCapture.CaptureScreenshot(arguments[result + 1], 1);
                yield return new WaitForSeconds(1f);
                Application.Quit(0);
                yield break;
            }

            var gameplay = Array.IndexOf(arguments, "--treasure-capture");
            if (gameplay < 0 || gameplay + 1 >= arguments.Length)
            {
                yield break;
            }
            yield return new WaitForSeconds(3.15f);
            ExplorerInputState.Set(ExplorerControl.Run, true);
            yield return new WaitForSeconds(0.5f);
            ExplorerInputState.Set(ExplorerControl.Scan, true);
            yield return new WaitForSeconds(0.1f);
            ExplorerInputState.Clear();
            yield return new WaitForSeconds(0.1f);
            ScreenCapture.CaptureScreenshot(arguments[gameplay + 1], 1);
            yield return new WaitForSeconds(1f);
            Application.Quit(0);
        }

        private static void CompleteHunt(TreasureSession session)
        {
            for (var index = 0; index < session.Challenge.collectibleCount; index++)
            {
                session.FindArtifact($"artifact-{index + 1:D2}");
            }
        }

        private void OnDestroy()
        {
            ExplorerInputState.Clear();
            _palette?.Dispose();
        }
    }
}
