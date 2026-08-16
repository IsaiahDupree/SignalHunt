using System;
using System.Collections;
using System.Collections.Generic;
using SignalHunt.Gameplay;
using SignalHunt.Replay;
using SignalHunt.UI;
using SignalHunt.Visual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TreasureHunt.Gameplay
{
    public enum ExplorerControl
    {
        TurnLeft,
        TurnRight,
        Run,
        Scan
    }

    public static class ExplorerInputState
    {
        private static bool _left;
        private static bool _right;
        private static bool _run;
        private static bool _scan;

        public static float Turn => Mathf.Clamp(
            (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || _right ? 1f : 0f) -
            (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || _left ? 1f : 0f), -1f, 1f);
        public static bool Running => Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || _run;
        public static bool Scanning => Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.E) || _scan;

        public static void Set(ExplorerControl control, bool pressed)
        {
            switch (control)
            {
                case ExplorerControl.TurnLeft: _left = pressed; break;
                case ExplorerControl.TurnRight: _right = pressed; break;
                case ExplorerControl.Run: _run = pressed; break;
                case ExplorerControl.Scan: _scan = pressed; break;
            }
        }

        public static void Clear()
        {
            _left = _right = _run = _scan = false;
        }
    }

    public sealed class ExplorerHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private ExplorerControl _control;
        private Image _image;
        private Color _color;

        public void Initialize(ExplorerControl control, Image image, Color color)
        {
            _control = control;
            _image = image;
            _color = color;
        }

        public void OnPointerDown(PointerEventData eventData) => Set(true);
        public void OnPointerUp(PointerEventData eventData) => Set(false);
        public void OnPointerExit(PointerEventData eventData) => Set(false);
        private void OnDisable() => Set(false);

        private void Set(bool pressed)
        {
            ExplorerInputState.Set(_control, pressed);
            if (_image != null)
            {
                _image.color = new Color(_color.r, _color.g, _color.b, pressed ? 0.84f : 0.34f);
                transform.localScale = pressed ? Vector3.one * 0.96f : Vector3.one;
            }
        }
    }

    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class ExplorerController : MonoBehaviour
    {
        private const float RunSpeed = 9.2f;
        private Rigidbody _body;
        private Transform _visual;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private bool _inputEnabled;
        private float _scanCooldown;
        private bool _wasScanning;

        public float Speed => _body == null ? 0f : new Vector2(_body.linearVelocity.x, _body.linearVelocity.z).magnitude;
        public float ScanCooldown01 => Mathf.Clamp01(_scanCooldown / 1.4f);
        public event Action<Vector3> ScanPulsed;

        public void Initialize(Vector3 position, float heading, Transform visual)
        {
            _body = GetComponent<Rigidbody>();
            _body.mass = 75f;
            _body.linearDamping = 4.5f;
            _body.angularDamping = 8f;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _spawnPosition = position;
            _spawnRotation = Quaternion.Euler(0f, heading, 0f);
            _visual = visual;
            ResetToSpawn();
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                ExplorerInputState.Clear();
                if (_body != null)
                {
                    _body.linearVelocity = Vector3.zero;
                    _body.angularVelocity = Vector3.zero;
                }
            }
        }

        public void ResetToSpawn()
        {
            if (_body == null)
            {
                _body = GetComponent<Rigidbody>();
            }
            _body.position = _spawnPosition;
            _body.rotation = _spawnRotation;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
        }

        private void Update()
        {
            _scanCooldown = Mathf.Max(0f, _scanCooldown - Time.deltaTime);
            var scanning = _inputEnabled && ExplorerInputState.Scanning;
            if (scanning && !_wasScanning && _scanCooldown <= 0f)
            {
                _scanCooldown = 1.4f;
                ScanPulsed?.Invoke(transform.position);
            }
            _wasScanning = scanning;
        }

        private void FixedUpdate()
        {
            if (_body == null)
            {
                return;
            }
            if (!_inputEnabled)
            {
                _body.linearVelocity = new Vector3(0f, _body.linearVelocity.y, 0f);
                return;
            }

            var turn = ExplorerInputState.Turn;
            _body.MoveRotation(_body.rotation * Quaternion.Euler(0f, turn * 112f * Time.fixedDeltaTime, 0f));
            var desired = ExplorerInputState.Running ? transform.forward * RunSpeed : Vector3.zero;
            var horizontal = Vector3.MoveTowards(new Vector3(_body.linearVelocity.x, 0f, _body.linearVelocity.z),
                desired, Time.fixedDeltaTime * 28f);
            _body.linearVelocity = new Vector3(horizontal.x, _body.linearVelocity.y, horizontal.z);

            if (_visual != null)
            {
                var bob = ExplorerInputState.Running ? Mathf.Sin(Time.time * 15f) * 0.055f : 0f;
                _visual.localPosition = Vector3.up * bob;
                _visual.localRotation = Quaternion.Slerp(_visual.localRotation,
                    Quaternion.Euler(0f, 0f, -turn * 7f), Time.fixedDeltaTime * 8f);
            }
            if (_body.position.y < -4f || Mathf.Abs(_body.position.x) > 112f || Mathf.Abs(_body.position.z) > 112f)
            {
                ResetToSpawn();
            }
        }
    }

    public static class ExplorerFactory
    {
        public static ExplorerController CreatePlayer(GamePalette palette, Vector3 position, float heading)
        {
            var root = new GameObject("Player Treasure Hunter");
            var collider = root.AddComponent<CapsuleCollider>();
            collider.height = 2.2f;
            collider.radius = 0.48f;
            collider.center = Vector3.up * 1.1f;
            root.AddComponent<Rigidbody>();
            var visual = new GameObject("Explorer Visual").transform;
            visual.SetParent(root.transform, false);
            BuildVisual(visual, palette.HoverBody, palette.HoverGlass, palette.NeonMaterials[3],
                palette.DarkMetal, palette.VehicleTrail, true);
            var controller = root.AddComponent<ExplorerController>();
            controller.Initialize(position, heading, visual);
            return controller;
        }

        public static GameObject CreateReplayVisual(GamePalette palette, ReplayRun run, int index, bool ghost)
        {
            var root = new GameObject($"Treasure Hunter Replay {index + 1}");
            var body = ghost ? palette.Ghost : palette.CreateReplayVehicleMaterial(Mathf.Abs(run.vehicleColorIndex) % 4);
            var accent = ghost ? palette.Ghost : palette.NeonMaterials[(Mathf.Abs(run.vehicleColorIndex) + 3) % 4];
            BuildVisual(root.transform, body, ghost ? palette.Ghost : palette.HoverGlass, accent, palette.DarkMetal,
                palette.VehicleTrail, !ghost);
            var name = string.IsNullOrWhiteSpace(run.playerName) ? $"HUNTER {index + 1}" : run.playerName;
            var nameplate = WorldNameplate.Create(root.transform,
                run.attemptNumber > 0 ? $"{name} · TRY {run.attemptNumber}" : name);
            nameplate.transform.localPosition = new Vector3(0f, 3.25f, 0f);
            nameplate.transform.localScale = Vector3.one * 0.0042f;
            return root;
        }

        private static void BuildVisual(Transform root, Material body, Material glass, Material accent, Material dark,
            Material trailMaterial, bool trail)
        {
            PrimitiveFactory.Capsule("Explorer Body", root, new Vector3(0f, 1.05f, 0f),
                new Vector3(0.78f, 1.1f, 0.78f), body, false);
            PrimitiveFactory.Sphere("Explorer Helmet", root, new Vector3(0f, 2.25f, 0.02f),
                new Vector3(0.82f, 0.76f, 0.82f), body, false);
            PrimitiveFactory.Cube("Explorer Visor", root, new Vector3(0f, 2.27f, 0.54f),
                new Vector3(0.72f, 0.30f, 0.16f), glass, false);
            PrimitiveFactory.Cube("Treasure Pack", root, new Vector3(0f, 1.25f, -0.52f),
                new Vector3(0.68f, 0.74f, 0.30f), dark, false);
            PrimitiveFactory.Cube("Pack Signal", root, new Vector3(0f, 1.32f, -0.70f),
                new Vector3(0.42f, 0.11f, 0.07f), accent, false);
            PrimitiveFactory.Cylinder("Left Leg", root, new Vector3(-0.24f, 0.42f, 0f),
                new Vector3(0.16f, 0.38f, 0.18f), body, false);
            PrimitiveFactory.Cylinder("Right Leg", root, new Vector3(0.24f, 0.42f, 0f),
                new Vector3(0.16f, 0.38f, 0.18f), body, false);
            PrimitiveFactory.Cube("Left Arm", root, new Vector3(-0.52f, 1.28f, 0.06f),
                new Vector3(0.18f, 0.70f, 0.22f), body, false).transform.localRotation =
                Quaternion.Euler(0f, 0f, -10f);
            PrimitiveFactory.Cube("Scanner Arm", root, new Vector3(0.68f, 1.22f, 0.18f),
                new Vector3(0.18f, 0.20f, 0.95f), body, false).transform.localRotation =
                Quaternion.Euler(-22f, 0f, 0f);
            PrimitiveFactory.Sphere("Scanner Orb", root, new Vector3(0.68f, 1.02f, 0.72f),
                new Vector3(0.28f, 0.28f, 0.28f), accent, false);
            PrimitiveFactory.Cube("Chest Signal", root, new Vector3(0f, 1.48f, 0.43f),
                new Vector3(0.46f, 0.12f, 0.10f), accent, false);
            if (trail)
            {
                var trailObject = new GameObject("Search Trail");
                trailObject.transform.SetParent(root, false);
                trailObject.transform.localPosition = new Vector3(0f, 0.08f, -0.28f);
                var trailRenderer = trailObject.AddComponent<TrailRenderer>();
                trailRenderer.sharedMaterial = trailMaterial;
                trailRenderer.time = 0.75f;
                trailRenderer.startWidth = 0.18f;
                trailRenderer.endWidth = 0f;
                trailRenderer.minVertexDistance = 0.12f;
                trailRenderer.startColor = Color.white;
                trailRenderer.endColor = new Color(1f, 1f, 1f, 0f);
            }
        }
    }

    public enum TreasureState
    {
        Countdown,
        Searching,
        Complete
    }

    public sealed class TreasureSession : MonoBehaviour
    {
        private readonly HashSet<string> _found = new();
        private SignalHunt.Core.DailyChallenge _challenge;
        private ExplorerController _explorer;
        private int _artifactCount;
        private float _elapsed;

        public TreasureState State { get; private set; } = TreasureState.Countdown;
        public float Elapsed => _elapsed;
        public int FoundCount => _found.Count;
        public SignalHunt.Core.DailyChallenge Challenge => _challenge;

        public event Action<string> StatusChanged;
        public event Action<float> TimeChanged;
        public event Action<int, int> ArtifactCountChanged;
        public event Action<string> ArtifactFound;
        public event Action<HuntResult> Completed;

        public void Initialize(SignalHunt.Core.DailyChallenge challenge, ExplorerController explorer, int artifactCount)
        {
            _challenge = challenge;
            _explorer = explorer;
            _artifactCount = artifactCount;
            _explorer.SetInputEnabled(false);
        }

        public void Begin() => StartCoroutine(CountdownRoutine());

        public bool FindArtifact(string artifactId)
        {
            if (State != TreasureState.Searching || !_found.Add(artifactId))
            {
                return false;
            }
            ArtifactFound?.Invoke(artifactId);
            ArtifactCountChanged?.Invoke(_found.Count, _artifactCount);
            if (_found.Count >= _artifactCount)
            {
                Finish(true);
            }
            return true;
        }

        private IEnumerator CountdownRoutine()
        {
            State = TreasureState.Countdown;
            for (var count = 3; count > 0; count--)
            {
                StatusChanged?.Invoke(count.ToString());
                yield return new WaitForSeconds(1f);
            }
            StatusChanged?.Invoke("SEARCH!");
            State = TreasureState.Searching;
            _explorer.SetInputEnabled(true);
            yield return new WaitForSeconds(0.65f);
            StatusChanged?.Invoke(string.Empty);
        }

        private void Update()
        {
            if (State != TreasureState.Searching)
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

        private void Finish(bool completed)
        {
            if (State == TreasureState.Complete)
            {
                return;
            }
            State = TreasureState.Complete;
            _explorer.SetInputEnabled(false);
            var result = new HuntResult
            {
                challengeId = _challenge.challengeId,
                timeMs = Mathf.RoundToInt(_elapsed * 1000f),
                score = TreasureScore.Calculate(_found.Count, _artifactCount, _elapsed, completed),
                collectedCount = _found.Count,
                totalCount = _artifactCount,
                completed = completed,
                collectedRelicIds = new List<string>(_found).ToArray()
            };
            StatusChanged?.Invoke(completed ? "VAULT COMPLETE" : "SEARCH ENDED");
            Completed?.Invoke(result);
        }
    }

    public static class TreasureScore
    {
        public static int Calculate(int found, int total, float seconds, bool completed)
        {
            var artifacts = found * 1200;
            var completion = completed ? 7000 : 0;
            var time = completed ? Mathf.Max(0, 300000 - Mathf.RoundToInt(seconds * 1000f)) / 90 : 0;
            var vault = completed && found == total ? 3500 : 0;
            return artifacts + completion + time + vault;
        }
    }

    public sealed class TreasureRunRecorder : MonoBehaviour
    {
        private const float SampleInterval = 0.1f;
        private TreasureSession _session;
        private ExplorerController _explorer;
        private ReplayRun _run;
        private float _nextSample;
        private string _pendingArtifact;

        public event Action<ReplayRun> RecordingCompleted;

        public void Initialize(TreasureSession session, ExplorerController explorer, string playerId,
            string playerName, int colorIndex)
        {
            _session = session;
            _explorer = explorer;
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
                vehicleId = "treasure-explorer-v1",
                vehicleColorIndex = colorIndex
            };
            session.ArtifactFound += OnArtifactFound;
            session.Completed += OnCompleted;
        }

        private void Update()
        {
            if (_session == null || _session.State != TreasureState.Searching || _session.Elapsed < _nextSample)
            {
                return;
            }
            CaptureFrame(string.Empty);
            _nextSample = _session.Elapsed + SampleInterval;
        }

        private void OnArtifactFound(string artifactId)
        {
            _pendingArtifact = artifactId;
            CaptureFrame("artifact-found");
        }

        private void OnCompleted(HuntResult result)
        {
            CaptureFrame("vault-complete");
            _run.result = result;
            RecordingCompleted?.Invoke(_run);
        }

        private void CaptureFrame(string cameraEvent)
        {
            if (_session == null || _explorer == null)
            {
                return;
            }
            _run.frames.Add(new ReplayFrame
            {
                timestamp = _session.Elapsed,
                position = _explorer.transform.position,
                rotation = _explorer.transform.rotation,
                speed = _explorer.Speed,
                vehicleState = ExplorerInputState.Scanning ? "scanning" : _explorer.Speed > 1f ? "searching" : "idle",
                collectedItem = _pendingArtifact,
                cameraEvent = cameraEvent
            });
            _pendingArtifact = string.Empty;
        }
    }

    public sealed class TreasureGhostPlayback : MonoBehaviour
    {
        private ReplayRun _run;
        private TreasureSession _session;
        private int _frameIndex;

        public void Initialize(ReplayRun run, TreasureSession session)
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
