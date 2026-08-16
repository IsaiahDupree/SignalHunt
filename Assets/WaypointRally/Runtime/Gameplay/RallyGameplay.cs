using System;
using System.Collections;
using System.Collections.Generic;
using SignalHunt.Gameplay;
using SignalHunt.Replay;
using SignalHunt.UI;
using SignalHunt.Visual;
using UnityEngine;

namespace WaypointRally.Gameplay
{
    public static class RallyCarFactory
    {
        public static HoverVehicleController CreatePlayer(GamePalette palette, Vector3 position, float heading)
        {
            var root = new GameObject("Player Rally Car");
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(2.15f, 0.9f, 4.25f);
            root.AddComponent<Rigidbody>();
            var visual = new GameObject("Rally Car Visual").transform;
            visual.SetParent(root.transform, false);
            BuildVisual(visual, palette.HoverBody, palette.HoverGlass, palette.NeonMaterials[3], palette.DarkMetal,
                palette.VehicleTrail, true);
            var controller = root.AddComponent<HoverVehicleController>();
            controller.Initialize(position, heading);
            root.AddComponent<HoverVehicleVisualFx>().Initialize(visual, controller);
            return controller;
        }

        public static GameObject CreateReplayVisual(GamePalette palette, ReplayRun run, int index, bool ghost)
        {
            var root = new GameObject($"Rally Replay {index + 1}");
            var body = ghost ? palette.Ghost : palette.CreateReplayVehicleMaterial(Mathf.Abs(run.vehicleColorIndex) % 4);
            var accent = ghost ? palette.Ghost : palette.NeonMaterials[(Mathf.Abs(run.vehicleColorIndex) + 3) % 4];
            BuildVisual(root.transform, body, ghost ? palette.Ghost : palette.HoverGlass, accent, palette.DarkMetal,
                palette.VehicleTrail, !ghost);
            var name = string.IsNullOrWhiteSpace(run.playerName) ? $"DRIVER {index + 1}" : run.playerName;
            WorldNameplate.Create(root.transform, run.attemptNumber > 0 ? $"{name} · TRY {run.attemptNumber}" : name);
            return root;
        }

        private static void BuildVisual(Transform root, Material body, Material glass, Material accent, Material tire,
            Material trailMaterial, bool trails)
        {
            PrimitiveFactory.Cube("Rally Chassis", root, new Vector3(0f, 0.02f, 0f),
                new Vector3(2.05f, 0.46f, 4.05f), body, false);
            var hood = PrimitiveFactory.Cube("Rally Hood", root, new Vector3(0f, 0.30f, 1.27f),
                new Vector3(1.82f, 0.28f, 1.32f), body, false);
            hood.transform.localRotation = Quaternion.Euler(-5f, 0f, 0f);
            var cabin = PrimitiveFactory.Cube("Rally Cabin", root, new Vector3(0f, 0.60f, -0.25f),
                new Vector3(1.62f, 0.82f, 1.75f), glass, false);
            cabin.transform.localRotation = Quaternion.Euler(-2f, 0f, 0f);
            PrimitiveFactory.Cube("Front Light Bar", root, new Vector3(0f, 0.18f, 2.07f),
                new Vector3(1.44f, 0.18f, 0.10f), accent, false);
            PrimitiveFactory.Cube("Rear Light Bar", root, new Vector3(0f, 0.26f, -2.07f),
                new Vector3(1.62f, 0.16f, 0.10f), accent, false);
            PrimitiveFactory.Cube("Rear Spoiler", root, new Vector3(0f, 0.92f, -1.72f),
                new Vector3(2.15f, 0.12f, 0.42f), body, false);
            PrimitiveFactory.Cube("Left Spoiler Mount", root, new Vector3(-0.72f, 0.61f, -1.72f),
                new Vector3(0.10f, 0.62f, 0.12f), accent, false);
            PrimitiveFactory.Cube("Right Spoiler Mount", root, new Vector3(0.72f, 0.61f, -1.72f),
                new Vector3(0.10f, 0.62f, 0.12f), accent, false);
            AddWheel(root, new Vector3(-1.05f, -0.18f, 1.27f), tire, accent);
            AddWheel(root, new Vector3(1.05f, -0.18f, 1.27f), tire, accent);
            AddWheel(root, new Vector3(-1.05f, -0.18f, -1.25f), tire, accent);
            AddWheel(root, new Vector3(1.05f, -0.18f, -1.25f), tire, accent);
            if (trails)
            {
                CreateTrail(root, new Vector3(-0.82f, -0.28f, -2.0f), trailMaterial);
                CreateTrail(root, new Vector3(0.82f, -0.28f, -2.0f), trailMaterial);
            }
        }

        private static void AddWheel(Transform root, Vector3 position, Material tire, Material accent)
        {
            var wheel = PrimitiveFactory.Cylinder("Rally Tire", root, position, new Vector3(0.47f, 0.22f, 0.47f),
                tire, false);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var hubPosition = position + new Vector3(position.x < 0f ? -0.23f : 0.23f, 0f, 0f);
            var hub = PrimitiveFactory.Cylinder("Neon Wheel Hub", root, hubPosition,
                new Vector3(0.24f, 0.24f, 0.24f), accent, false);
            hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        private static void CreateTrail(Transform root, Vector3 position, Material material)
        {
            var instance = new GameObject("Tire Trail");
            instance.transform.SetParent(root, false);
            instance.transform.localPosition = position;
            var trail = instance.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = 0.42f;
            trail.startWidth = 0.15f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.08f;
            trail.startColor = Color.white;
            trail.endColor = new Color(1f, 1f, 1f, 0f);
        }
    }

    public enum RallyState
    {
        Countdown,
        Racing,
        Complete
    }

    public sealed class RallySession : MonoBehaviour
    {
        private readonly List<string> _cleared = new();
        private SignalHunt.Core.DailyChallenge _challenge;
        private HoverVehicleController _vehicle;
        private int _checkpointCount;
        private float _elapsed;

        public RallyState State { get; private set; } = RallyState.Countdown;
        public float Elapsed => _elapsed;
        public int ClearedCount => _cleared.Count;
        public SignalHunt.Core.DailyChallenge Challenge => _challenge;

        public event Action<string> StatusChanged;
        public event Action<float> TimeChanged;
        public event Action<int, int> CheckpointCountChanged;
        public event Action<string> CheckpointCleared;
        public event Action<HuntResult> Completed;

        public void Initialize(SignalHunt.Core.DailyChallenge challenge, HoverVehicleController vehicle, int checkpointCount)
        {
            _challenge = challenge;
            _vehicle = vehicle;
            _checkpointCount = checkpointCount;
            _vehicle.SetInputEnabled(false);
        }

        public void Begin() => StartCoroutine(CountdownRoutine());

        public bool PassCheckpoint(int index, string checkpointId)
        {
            if (State != RallyState.Racing || index != _cleared.Count)
            {
                return false;
            }
            _cleared.Add(checkpointId);
            CheckpointCleared?.Invoke(checkpointId);
            CheckpointCountChanged?.Invoke(_cleared.Count, _checkpointCount);
            if (_cleared.Count >= _checkpointCount)
            {
                Finish(true);
            }
            return true;
        }

        private IEnumerator CountdownRoutine()
        {
            State = RallyState.Countdown;
            for (var count = 3; count > 0; count--)
            {
                StatusChanged?.Invoke(count.ToString());
                yield return new WaitForSeconds(1f);
            }
            StatusChanged?.Invoke("RACE!");
            State = RallyState.Racing;
            _vehicle.SetInputEnabled(true);
            yield return new WaitForSeconds(0.65f);
            StatusChanged?.Invoke(string.Empty);
        }

        private void Update()
        {
            if (State != RallyState.Racing)
            {
                return;
            }
            _elapsed += Time.deltaTime;
            TimeChanged?.Invoke(_elapsed);
            if (_elapsed >= 300f)
            {
                Finish(false);
            }
        }

        private void Finish(bool complete)
        {
            if (State == RallyState.Complete)
            {
                return;
            }
            State = RallyState.Complete;
            _vehicle.SetInputEnabled(false);
            if (_vehicle.Body != null)
            {
                _vehicle.Body.linearVelocity = Vector3.zero;
                _vehicle.Body.angularVelocity = Vector3.zero;
            }
            var result = new HuntResult
            {
                challengeId = _challenge.challengeId,
                timeMs = Mathf.RoundToInt(_elapsed * 1000f),
                score = RallyScore.Calculate(_cleared.Count, _checkpointCount, _elapsed, complete),
                collectedCount = _cleared.Count,
                totalCount = _checkpointCount,
                completed = complete,
                collectedRelicIds = _cleared.ToArray()
            };
            StatusChanged?.Invoke(complete ? "FINISH!" : "RALLY ENDED");
            Completed?.Invoke(result);
        }
    }

    public static class RallyScore
    {
        public static int Calculate(int cleared, int total, float seconds, bool completed)
        {
            var checkpoints = cleared * 1100;
            var finish = completed ? 6500 : 0;
            var time = completed ? Mathf.Max(0, 300000 - Mathf.RoundToInt(seconds * 1000f)) / 90 : 0;
            var fullRoute = completed && cleared == total ? 3000 : 0;
            return checkpoints + finish + time + fullRoute;
        }
    }

    public sealed class RallyRunRecorder : MonoBehaviour
    {
        private const float SampleInterval = 0.1f;
        private RallySession _session;
        private HoverVehicleController _vehicle;
        private ReplayRun _run;
        private float _nextSample;
        private string _pendingCheckpoint;

        public event Action<ReplayRun> RecordingCompleted;

        public void Initialize(RallySession session, HoverVehicleController vehicle, string playerId, string playerName,
            int colorIndex)
        {
            _session = session;
            _vehicle = vehicle;
            _run = new ReplayRun
            {
                appKey = session.Challenge.appKey,
                modeKey = session.Challenge.modeKey,
                challengeId = session.Challenge.challengeId,
                worldTemplate = session.Challenge.worldTemplate,
                generationSeed = session.Challenge.generationSeed,
                clientRunId = Guid.NewGuid().ToString("N"),
                recordedAtUtc = DateTime.UtcNow.ToString("O"),
                playerId = playerId,
                playerName = playerName,
                vehicleId = "rally-car-v1",
                vehicleColorIndex = colorIndex
            };
            session.CheckpointCleared += OnCheckpointCleared;
            session.Completed += OnCompleted;
        }

        private void Update()
        {
            if (_session == null || _session.State != RallyState.Racing || _session.Elapsed < _nextSample)
            {
                return;
            }
            CaptureFrame(string.Empty);
            _nextSample = _session.Elapsed + SampleInterval;
        }

        private void OnCheckpointCleared(string checkpointId)
        {
            _pendingCheckpoint = checkpointId;
            CaptureFrame("checkpoint");
        }

        private void OnCompleted(HuntResult result)
        {
            CaptureFrame("finish");
            _run.result = result;
            RecordingCompleted?.Invoke(_run);
        }

        private void CaptureFrame(string cameraEvent)
        {
            if (_session == null || _vehicle == null)
            {
                return;
            }
            _run.frames.Add(new ReplayFrame
            {
                timestamp = _session.Elapsed,
                position = _vehicle.transform.position,
                rotation = _vehicle.transform.rotation,
                speed = _vehicle.Speed,
                vehicleState = _vehicle.Speed > 20f ? "drift" : _vehicle.Speed > 1f ? "racing" : "idle",
                collectedItem = _pendingCheckpoint,
                cameraEvent = cameraEvent
            });
            _pendingCheckpoint = string.Empty;
        }
    }

    public sealed class RallyGhostPlayback : MonoBehaviour
    {
        private ReplayRun _run;
        private RallySession _session;
        private int _frameIndex;

        public void Initialize(ReplayRun run, RallySession session)
        {
            _run = run;
            _session = session;
            gameObject.SetActive(run?.frames != null && run.frames.Count > 1);
        }

        private void Update()
        {
            if (_run?.frames == null || _run.frames.Count < 2 || _session == null)
            {
                return;
            }
            var time = _session.Elapsed;
            while (_frameIndex < _run.frames.Count - 2 && _run.frames[_frameIndex + 1].timestamp <= time)
            {
                _frameIndex++;
            }
            if (time > _run.frames[^1].timestamp)
            {
                gameObject.SetActive(false);
                return;
            }
            var from = _run.frames[_frameIndex];
            var to = _run.frames[Mathf.Min(_frameIndex + 1, _run.frames.Count - 1)];
            var progress = Mathf.InverseLerp(from.timestamp, Mathf.Max(from.timestamp + 0.001f, to.timestamp), time);
            transform.position = Vector3.Lerp(from.position, to.position, progress);
            transform.rotation = Quaternion.Slerp(from.rotation, to.rotation, progress);
        }
    }
}
