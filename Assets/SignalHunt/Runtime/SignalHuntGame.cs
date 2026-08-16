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
            ConfigureRendering();

            var challenge = DailyChallenge.Today();
            var layout = TownLayoutGenerator.Generate(challenge);
            _palette = new GamePalette();
            _palette.ApplyVehicleColor(PlayerCosmetics.VehicleColorIndex);

            _session = gameObject.AddComponent<HuntSession>();
            TownWorldBuilder.Build(layout, _palette, relicId => _session.Collect(relicId));
            var vehicle = HoverVehicleFactory.CreatePlayer(_palette, layout.playerSpawn, layout.playerHeading);
            WorldNameplate.Create(vehicle.transform, PlayerIdentity.DisplayName);
            _session.Initialize(challenge, vehicle, layout.relics.Count);

            var followCamera = BuildCamera(vehicle.transform);
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

        private static FollowCamera BuildCamera(Transform target)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 260f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.008f, 0.014f, 0.045f);
            var follow = cameraObject.AddComponent<FollowCamera>();
            follow.SetTarget(target);
            return follow;
        }

        private static void ConfigureRendering()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0042f;
            RenderSettings.fogColor = new Color(0.025f, 0.045f, 0.12f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.25f, 0.46f);
            RenderSettings.ambientEquatorColor = new Color(0.08f, 0.10f, 0.19f);
            RenderSettings.ambientGroundColor = new Color(0.025f, 0.032f, 0.065f);

            var lightObject = new GameObject("Synthetic Moon");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.48f, 0.64f, 1f);
            light.intensity = 1.32f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            var rimObject = new GameObject("Neon Rim Light");
            var rim = rimObject.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = new Color(1f, 0.16f, 0.66f);
            rim.intensity = 0.42f;
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
