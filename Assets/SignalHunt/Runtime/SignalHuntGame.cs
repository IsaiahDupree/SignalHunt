using System;
using System.Collections;
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (FindAnyObjectByType<SignalHuntGame>() == null)
            {
                new GameObject("Signal Hunt").AddComponent<SignalHuntGame>();
            }
        }

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
            WorldNameplate.Create(vehicle.transform, PlayerIdentity.DisplayName);
            _session.Initialize(challenge, vehicle, relicCount);

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
            _hud.ExportRequested += () => _exporter.Export(_latestRun ?? ReplayStore.LoadLatest(challenge.challengeId));

            _session.Begin();
            StartCoroutine(CapturePreviewIfRequested());
        }

        private void OnRecordingCompleted(ReplayRun run)
        {
            _latestRun = run;
            var personalBest = ReplayStore.SaveIfBest(run);
            _hud.SetNetworkStatus(personalBest ? "New personal best · syncing daily rank…" : "Run saved · syncing daily rank…");
            StartCoroutine(_api.SubmitAndFetchLeaderboard(_session.Challenge, run, leaderboard =>
            {
                if (!string.IsNullOrEmpty(leaderboard.error))
                {
                    _hud.SetNetworkStatus(leaderboard.error);
                    return;
                }

                var rank = Array.Find(leaderboard.entries,
                    entry => string.Equals(entry.playerName, PlayerIdentity.DisplayName, StringComparison.Ordinal));
                _hud.SetNetworkStatus(rank == null
                    ? $"Daily leaderboard synced · {leaderboard.entries.Length} ranked hunters"
                    : $"Daily rank #{rank.rank} · {leaderboard.entries.Length} ranked hunters");
            }));
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

        private static IEnumerator CapturePreviewIfRequested()
        {
            var arguments = Environment.GetCommandLineArgs();
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
    }
}
