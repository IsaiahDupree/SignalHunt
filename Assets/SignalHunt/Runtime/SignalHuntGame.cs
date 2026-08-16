using System;
using System.Collections;
using System.Collections.Generic;
using SignalHunt.Backend;
using SignalHunt.Core;
using SignalHunt.Gameplay;
using SignalHunt.Replay;
using SignalHunt.UI;
using SignalHunt.Visual;
using SignalHunt.World;
using UnityEngine;

namespace SignalHunt
{
    public sealed class SignalHuntGame : MonoBehaviour
    {
        private GamePalette _palette;
        private HuntSession _session;
        private HuntHud _hud;
        private SignalHuntApi _api;
        private ReplayRun _latestRun;
        private CinematicReplayExporter _exporter;
        private DailyMontageExporter _montageExporter;

        private void Start()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadowDistance = 58f;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;

            var challenge = DailyChallenge.Today();
            ConfigureRendering(challenge);
            _palette = new GamePalette();
            _palette.ApplyVehicleColor(PlayerCosmetics.VehicleColorIndex);

            _session = gameObject.AddComponent<HuntSession>();
            Vector3 playerSpawn;
            float playerHeading;
            int relicCount;
            if (challenge.Stage == WorldStage.Island)
            {
                var island = IslandLayoutGenerator.Generate(challenge);
                IslandWorldBuilder.Build(island, _palette, relicId => _session.Collect(relicId));
                playerSpawn = island.playerSpawn;
                playerHeading = island.playerHeading;
                relicCount = island.relics.Count;
            }
            else
            {
                var town = TownLayoutGenerator.Generate(challenge);
                TownWorldBuilder.Build(town, _palette, relicId => _session.Collect(relicId));
                playerSpawn = town.playerSpawn;
                playerHeading = town.playerHeading;
                relicCount = town.relics.Count;
            }

            var vehicle = HoverVehicleFactory.CreatePlayer(_palette, playerSpawn, playerHeading);
            var playerNameplate = WorldNameplate.Create(vehicle.transform, PlayerIdentity.DisplayName);
            _session.Initialize(challenge, vehicle, relicCount);
            _session.Completed += _ => playerNameplate.gameObject.SetActive(false);

            var followCamera = BuildCamera(vehicle.transform, challenge);
            _hud = gameObject.AddComponent<HuntHud>();
            _hud.Initialize(_session, challenge);
            _hud.TrackVehicle(vehicle);
            _hud.StyleRequested += () =>
            {
                _palette.ApplyVehicleColor(PlayerCosmetics.CycleVehicleColor());
                _hud.SetNetworkStatus("Vehicle signal color saved");
            };

            var recorder = gameObject.AddComponent<RunRecorder>();
            recorder.Initialize(_session, vehicle, PlayerIdentity.InstallId, PlayerIdentity.DisplayName);
            recorder.RecordingCompleted += OnRecordingCompleted;

            var bestReplay = ReplayStore.LoadBest(challenge.challengeId);
            if (bestReplay?.frames != null && bestReplay.frames.Count > 1)
            {
                var ghost = HoverVehicleFactory.CreateReplayVisual(_palette, "Personal Best Ghost", true);
                ghost.AddComponent<GhostPlayback>().Initialize(bestReplay, _session);
            }

            _api = gameObject.AddComponent<SignalHuntApi>();
            _api.Initialize();
            StartCoroutine(_api.FetchTopGhost(challenge, (topGhost, error) =>
            {
                if (topGhost?.frames == null || topGhost.frames.Count < 2)
                {
                    return;
                }

                var ghost = HoverVehicleFactory.CreateReplayVisual(_palette,
                    $"Daily Leader Ghost · {topGhost.playerName}", true);
                ghost.AddComponent<GhostPlayback>().Initialize(topGhost, _session);
            }));
            _exporter = gameObject.AddComponent<CinematicReplayExporter>();
            _exporter.Initialize(followCamera, vehicle.transform, _palette);
            _exporter.StatusChanged += _hud.SetNetworkStatus;
            _exporter.PresentationModeChanged += _hud.SetPresentationMode;
            _hud.ExportRequested += () => _exporter.Export(_latestRun ?? ReplayStore.LoadLatest(challenge.challengeId));
            _montageExporter = gameObject.AddComponent<DailyMontageExporter>();
            _montageExporter.Initialize(followCamera, vehicle.transform, _palette);
            _montageExporter.StatusChanged += _hud.SetNetworkStatus;
            _montageExporter.PresentationModeChanged += _hud.SetPresentationMode;
            _hud.MontageRequested += () => StartCoroutine(ExportDailyFilm());

            _session.Begin();
            StartCoroutine(CapturePreviewIfRequested(_session, _montageExporter));
        }

        private void OnRecordingCompleted(ReplayRun run)
        {
            _latestRun = run;
            var outcome = ReplayStore.SaveAttempt(run);
            _hud.SetRunOutcome(outcome, run.result);
            _hud.SetNetworkStatus(outcome.isPersonalBest
                ? $"Attempt {outcome.attemptNumber} is your new best · syncing daily rank…"
                : $"Attempt {outcome.attemptNumber} saved · syncing daily rank…");
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
                    ? $"Daily leaderboard synced · {leaderboard.entries.Length} ranked hunters"
                    : $"Daily rank #{rank.rank} · {leaderboard.entries.Length} ranked hunters");
            }));
        }

        private IEnumerator ExportDailyFilm()
        {
            var challenge = _session.Challenge;
            var localRuns = ReplayStore.LoadHistory(challenge.challengeId, 16);
            if (!_api.IsConfigured)
            {
                _hud.SetNetworkStatus($"Building film from {localRuns.Length} attempt{(localRuns.Length == 1 ? string.Empty : "s")} on this device");
                _montageExporter.Export(localRuns);
                yield break;
            }

            _hud.SetNetworkStatus("Loading today's racers…");
            ReplayRun[] remoteRuns = null;
            string error = null;
            yield return _api.FetchDailyMontage(challenge, 16, (runs, message) =>
            {
                remoteRuns = runs;
                error = message;
            });

            var merged = MergeRuns(remoteRuns, localRuns, 16);
            if (!string.IsNullOrEmpty(error))
            {
                _hud.SetNetworkStatus($"{error} · using {merged.Length} local attempt{(merged.Length == 1 ? string.Empty : "s")}");
            }
            else
            {
                _hud.SetNetworkStatus($"Loaded {merged.Length} racer{(merged.Length == 1 ? string.Empty : "s")} for today's film");
            }
            _montageExporter.Export(merged);
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

        private static FollowCamera BuildCamera(Transform target, DailyChallenge challenge)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = challenge.Stage == WorldStage.Island ? 59f : 62f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = challenge.Stage == WorldStage.Island ? 360f : 260f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = challenge.Stage == WorldStage.Island
                ? new Color(0.55f, 0.75f, 0.82f)
                : new Color(0.008f, 0.014f, 0.045f);
            var follow = cameraObject.AddComponent<FollowCamera>();
            follow.SetTarget(target);
            return follow;
        }

        private static void ConfigureRendering(DailyChallenge challenge)
        {
            var island = challenge.Stage == WorldStage.Island;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = island ? 0.0028f : 0.0042f;
            RenderSettings.fogColor = island ? new Color(0.67f, 0.79f, 0.80f) : new Color(0.025f, 0.045f, 0.12f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = island ? new Color(0.64f, 0.78f, 0.86f) : new Color(0.18f, 0.25f, 0.46f);
            RenderSettings.ambientEquatorColor = island ? new Color(0.45f, 0.57f, 0.48f) : new Color(0.08f, 0.10f, 0.19f);
            RenderSettings.ambientGroundColor = island ? new Color(0.18f, 0.27f, 0.16f) : new Color(0.025f, 0.032f, 0.065f);

            var lightObject = new GameObject(island ? "Island Sun" : "Synthetic Moon");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = island ? new Color(1f, 0.93f, 0.75f) : new Color(0.48f, 0.64f, 1f);
            light.intensity = island ? 1.18f : 1.32f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(island ? 54f : 48f, island ? -38f : -32f, 0f);

            var rimObject = new GameObject("Neon Rim Light");
            var rim = rimObject.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = island ? new Color(0.25f, 0.64f, 0.75f) : new Color(1f, 0.16f, 0.66f);
            rim.intensity = island ? 0.26f : 0.42f;
            rim.shadows = LightShadows.None;
            rimObject.transform.rotation = Quaternion.Euler(35f, 145f, 0f);
        }

        private void OnDestroy()
        {
            VehicleInputState.Clear();
            _palette?.Dispose();
        }

        private static IEnumerator CapturePreviewIfRequested(HuntSession session, DailyMontageExporter montageExporter)
        {
            var arguments = Environment.GetCommandLineArgs();
            var filmMarker = Array.IndexOf(arguments, "--signalhunt-capture-film");
            if (filmMarker >= 0 && filmMarker + 1 < arguments.Length)
            {
                yield return new WaitForSeconds(3.25f);
                VehicleInputState.Set(VehicleControl.Accelerate, true);
                VehicleInputState.Set(VehicleControl.SteerLeft, true);
                yield return new WaitForSeconds(1.4f);
                VehicleInputState.Set(VehicleControl.SteerLeft, false);
                VehicleInputState.Set(VehicleControl.SteerRight, true);
                yield return new WaitForSeconds(1.6f);
                VehicleInputState.Set(VehicleControl.SteerRight, false);
                yield return new WaitForSeconds(1.5f);
                VehicleInputState.Clear();
                CompleteForCapture(session);
                yield return new WaitForSeconds(0.45f);
                montageExporter.Export(ReplayStore.LoadHistory(session.Challenge.challengeId, 16));
                yield return new WaitForSeconds(7.5f);
                ScreenCapture.CaptureScreenshot(arguments[filmMarker + 1], 1);
                yield return new WaitForSeconds(1f);
                Application.Quit(0);
                yield break;
            }

            var resultMarker = Array.IndexOf(arguments, "--signalhunt-capture-result");
            if (resultMarker >= 0 && resultMarker + 1 < arguments.Length)
            {
                yield return new WaitForSeconds(3.25f);
                CompleteForCapture(session);
                yield return new WaitForSeconds(0.8f);
                ScreenCapture.CaptureScreenshot(arguments[resultMarker + 1], 1);
                yield return new WaitForSeconds(1f);
                Application.Quit(0);
                yield break;
            }

            var marker = Array.IndexOf(arguments, "--signalhunt-capture");
            if (marker < 0 || marker + 1 >= arguments.Length)
            {
                yield break;
            }

            yield return new WaitForSeconds(3.2f);
            VehicleInputState.Set(VehicleControl.Accelerate, true);
            yield return new WaitForSeconds(0.8f);
            VehicleInputState.Clear();
            yield return new WaitForSeconds(0.4f);
            ScreenCapture.CaptureScreenshot(arguments[marker + 1], 1);
            yield return new WaitForSeconds(1f);
            Application.Quit(0);
        }

        private static void CompleteForCapture(HuntSession session)
        {
            for (var index = 1; index <= session.Challenge.collectibleCount; index++)
            {
                session.Collect($"relic-{index:D2}");
            }
        }
    }
}
