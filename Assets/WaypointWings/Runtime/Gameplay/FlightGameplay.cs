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

namespace WaypointWings.Gameplay
{
    public enum FlightControl
    {
        TurnLeft,
        TurnRight,
        PitchUp,
        PitchDown,
        Boost
    }

    public static class FlightInputState
    {
        private static bool _left;
        private static bool _right;
        private static bool _up;
        private static bool _down;
        private static bool _boost;
        private static Vector2 _touchStick;

        public static float Turn => DigitalOrAnalog(
            (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || _right ? 1f : 0f) -
            (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || _left ? 1f : 0f), _touchStick.x);
        public static float Pitch => DigitalOrAnalog(
            (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || _up ? 1f : 0f) -
            (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || _down ? 1f : 0f), _touchStick.y);
        public static bool Boosting => Input.GetKey(KeyCode.Space) || _boost;

        public static void SetStick(Vector2 value) => _touchStick = Vector2.ClampMagnitude(value, 1f);

        public static void Set(FlightControl control, bool pressed)
        {
            switch (control)
            {
                case FlightControl.TurnLeft: _left = pressed; break;
                case FlightControl.TurnRight: _right = pressed; break;
                case FlightControl.PitchUp: _up = pressed; break;
                case FlightControl.PitchDown: _down = pressed; break;
                case FlightControl.Boost: _boost = pressed; break;
            }
        }

        public static void Clear()
        {
            _left = _right = _up = _down = _boost = false;
            _touchStick = Vector2.zero;
        }

        private static float DigitalOrAnalog(float digital, float analog) =>
            Mathf.Abs(digital) > 0.01f ? Mathf.Clamp(digital, -1f, 1f) : Mathf.Clamp(analog, -1f, 1f);
    }

    public sealed class FlightHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private FlightControl _control;
        private Image _image;
        private Color _color;

        public void Initialize(FlightControl control, Image image, Color color)
        {
            _control = control;
            _image = image;
            _color = color;
        }

        public void OnPointerDown(PointerEventData eventData) => Set(true);
        public void OnPointerUp(PointerEventData eventData) => Set(false);
        private void OnDisable() => Set(false);

        private void Set(bool pressed)
        {
            FlightInputState.Set(_control, pressed);
            if (_image != null)
            {
                _image.color = new Color(_color.r, _color.g, _color.b, pressed ? 0.82f : 0.34f);
                transform.localScale = pressed ? Vector3.one * 0.96f : Vector3.one;
            }
        }
    }

    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class AircraftController : MonoBehaviour
    {
        private const float CruiseSpeed = 25f;
        private const float BoostSpeed = 46f;
        private const float TurnRate = 52f;
        private const float PitchRate = 34f;
        private const float AutoLevelRate = 9f;
        private const float MinimumPitch = -27f;
        private const float MaximumPitch = 34f;
        private Rigidbody _body;
        private Transform _visual;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private float _speed = CruiseSpeed;
        private float _smoothedTurn;
        private float _smoothedPitch;
        private bool _inputEnabled;

        public float Speed => _speed;
        public bool IsBoosting => _inputEnabled && FlightInputState.Boosting;

        public void Initialize(Vector3 position, Quaternion rotation, Transform visual)
        {
            _body = GetComponent<Rigidbody>();
            _body.mass = 850f;
            _body.useGravity = false;
            _body.linearDamping = 0.08f;
            _body.angularDamping = 2.5f;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _spawnPosition = position;
            _spawnRotation = rotation;
            _visual = visual;
            ResetToSpawn();
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                FlightInputState.Clear();
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
            _speed = CruiseSpeed;
            _smoothedTurn = 0f;
            _smoothedPitch = 0f;
            _body.position = _spawnPosition;
            _body.rotation = _spawnRotation;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (_body == null)
            {
                return;
            }

            if (!_inputEnabled)
            {
                _body.linearVelocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
                if (_visual != null)
                {
                    _visual.localRotation = Quaternion.Slerp(
                        _visual.localRotation, Quaternion.identity, Time.fixedDeltaTime * 6f);
                }
                return;
            }

            _smoothedTurn = Mathf.MoveTowards(_smoothedTurn, FlightInputState.Turn, Time.fixedDeltaTime * 4.5f);
            _smoothedPitch = Mathf.MoveTowards(_smoothedPitch, FlightInputState.Pitch, Time.fixedDeltaTime * 4.5f);
            var desiredSpeed = FlightInputState.Boosting ? BoostSpeed : CruiseSpeed;
            _speed = Mathf.MoveTowards(_speed, desiredSpeed, Time.fixedDeltaTime * 16f);

            var forward = _body.rotation * Vector3.forward;
            var yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            var currentPitch = Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            yaw += _smoothedTurn * TurnRate * Time.fixedDeltaTime;
            var targetPitch = Mathf.Abs(_smoothedPitch) > 0.015f
                ? currentPitch + _smoothedPitch * PitchRate * Time.fixedDeltaTime
                : Mathf.MoveTowards(currentPitch, 0f, AutoLevelRate * Time.fixedDeltaTime);
            targetPitch = Mathf.Clamp(targetPitch, MinimumPitch, MaximumPitch);
            var yawRadians = yaw * Mathf.Deg2Rad;
            var pitchRadians = targetPitch * Mathf.Deg2Rad;
            var desiredForward = new Vector3(Mathf.Sin(yawRadians) * Mathf.Cos(pitchRadians),
                Mathf.Sin(pitchRadians), Mathf.Cos(yawRadians) * Mathf.Cos(pitchRadians));
            var response = 1f - Mathf.Exp(-8f * Time.fixedDeltaTime);
            var controlledForward = Vector3.Slerp(forward, desiredForward, response).normalized;
            _body.MoveRotation(Quaternion.LookRotation(controlledForward, Vector3.up));
            _body.linearVelocity = controlledForward * _speed;

            if (_visual != null)
            {
                var bank = Quaternion.Euler(_smoothedPitch * -5f, 0f, _smoothedTurn * -30f);
                _visual.localRotation = Quaternion.Slerp(_visual.localRotation, bank, Time.fixedDeltaTime * 6f);
            }

            if (_body.position.y < 3f || _body.position.y > 115f ||
                Mathf.Abs(_body.position.x - _spawnPosition.x) > 230f ||
                Mathf.Abs(_body.position.z - _spawnPosition.z) > 480f)
            {
                ResetToSpawn();
            }
        }
    }

    public static class AircraftFactory
    {
        public static AircraftController CreatePlayer(GamePalette palette, Vector3 position, Quaternion rotation)
        {
            var root = new GameObject("Player Aircraft");
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(4.2f, 1.15f, 5.8f);
            root.AddComponent<Rigidbody>();
            var visual = new GameObject("Aircraft Visual").transform;
            visual.SetParent(root.transform, false);
            BuildVisual(visual, palette.HoverBody, palette.HoverGlass, palette.NeonMaterials[1], palette.VehicleTrail, true);
            var controller = root.AddComponent<AircraftController>();
            controller.Initialize(position, rotation, visual);
            return controller;
        }

        public static GameObject CreateReplayVisual(GamePalette palette, ReplayRun run, int index, bool ghost)
        {
            var root = new GameObject($"Flight Replay {index + 1}");
            var body = ghost ? palette.Ghost : palette.CreateReplayVehicleMaterial(Mathf.Abs(run.vehicleColorIndex) % 4);
            var accent = ghost ? palette.Ghost : palette.NeonMaterials[(Mathf.Abs(run.vehicleColorIndex) + 1) % 4];
            BuildVisual(root.transform, body, ghost ? palette.Ghost : palette.HoverGlass, accent, palette.VehicleTrail, !ghost);
            var name = string.IsNullOrWhiteSpace(run.playerName) ? $"PILOT {index + 1}" : run.playerName;
            WorldNameplate.Create(root.transform, run.attemptNumber > 0 ? $"{name} · TRY {run.attemptNumber}" : name);
            return root;
        }

        private static void BuildVisual(Transform root, Material body, Material glass, Material accent, Material trail, bool trails)
        {
            var fuselage = PrimitiveFactory.Cylinder("Fuselage", root, new Vector3(0f, 0f, 0.1f),
                new Vector3(0.46f, 2.25f, 0.46f), body, false);
            fuselage.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var nose = PrimitiveFactory.Sphere("Nose", root, new Vector3(0f, 0f, 2.25f),
                new Vector3(0.78f, 0.58f, 1.25f), accent, false);
            var cockpit = PrimitiveFactory.Sphere("Cockpit", root, new Vector3(0f, 0.42f, 0.78f),
                new Vector3(0.72f, 0.42f, 1.08f), glass, false);
            cockpit.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);

            var leftWing = PrimitiveFactory.Cube("Left Wing", root, new Vector3(-2.05f, -0.08f, -0.15f),
                new Vector3(3.7f, 0.14f, 1.35f), body, false);
            leftWing.transform.localRotation = Quaternion.Euler(0f, -10f, -2f);
            var rightWing = PrimitiveFactory.Cube("Right Wing", root, new Vector3(2.05f, -0.08f, -0.15f),
                new Vector3(3.7f, 0.14f, 1.35f), body, false);
            rightWing.transform.localRotation = Quaternion.Euler(0f, 10f, 2f);
            PrimitiveFactory.Cube("Left Wing Light", root, new Vector3(-3.72f, -0.06f, -0.28f),
                new Vector3(0.22f, 0.18f, 0.72f), accent, false);
            PrimitiveFactory.Cube("Right Wing Light", root, new Vector3(3.72f, -0.06f, -0.28f),
                new Vector3(0.22f, 0.18f, 0.72f), accent, false);
            PrimitiveFactory.Cube("Tail Plane", root, new Vector3(0f, 0.05f, -1.95f),
                new Vector3(2.25f, 0.12f, 0.75f), body, false);
            PrimitiveFactory.Cube("Tail Fin", root, new Vector3(0f, 0.62f, -1.85f),
                new Vector3(0.14f, 1.1f, 0.85f), accent, false);
            PrimitiveFactory.Cylinder("Left Engine", root, new Vector3(-1.28f, -0.24f, -0.15f),
                new Vector3(0.3f, 0.72f, 0.3f), accent, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            PrimitiveFactory.Cylinder("Right Engine", root, new Vector3(1.28f, -0.24f, -0.15f),
                new Vector3(0.3f, 0.72f, 0.3f), accent, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            if (trails)
            {
                CreateTrail(root, new Vector3(-1.28f, -0.24f, -0.95f), trail);
                CreateTrail(root, new Vector3(1.28f, -0.24f, -0.95f), trail);
            }
        }

        private static void CreateTrail(Transform parent, Vector3 localPosition, Material material)
        {
            var instance = new GameObject("Wing Trail");
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            var trail = instance.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = 0.32f;
            trail.startWidth = 0.12f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.1f;
            trail.startColor = Color.white;
            trail.endColor = new Color(1f, 1f, 1f, 0f);
        }
    }

    public enum FlightState
    {
        Countdown,
        Flying,
        Complete
    }

    public sealed class FlightSession : MonoBehaviour
    {
        private readonly List<string> _cleared = new();
        private SignalHunt.Core.DailyChallenge _challenge;
        private AircraftController _aircraft;
        private int _gateCount;
        private float _elapsed;

        public FlightState State { get; private set; } = FlightState.Countdown;
        public float Elapsed => _elapsed;
        public int ClearedCount => _cleared.Count;
        public SignalHunt.Core.DailyChallenge Challenge => _challenge;

        public event Action<string> StatusChanged;
        public event Action<float> TimeChanged;
        public event Action<int, int> GateCountChanged;
        public event Action<string> GateCleared;
        public event Action<HuntResult> Completed;

        public void Initialize(SignalHunt.Core.DailyChallenge challenge, AircraftController aircraft, int gateCount)
        {
            _challenge = challenge;
            _aircraft = aircraft;
            _gateCount = gateCount;
            _aircraft.SetInputEnabled(false);
        }

        public void Begin() => StartCoroutine(CountdownRoutine());

        public bool PassGate(int index, string gateId)
        {
            if (State != FlightState.Flying || index != _cleared.Count)
            {
                return false;
            }
            _cleared.Add(gateId);
            GateCleared?.Invoke(gateId);
            GateCountChanged?.Invoke(_cleared.Count, _gateCount);
            if (_cleared.Count >= _gateCount)
            {
                Finish(true);
            }
            return true;
        }

        private IEnumerator CountdownRoutine()
        {
            State = FlightState.Countdown;
            for (var count = 3; count > 0; count--)
            {
                StatusChanged?.Invoke(count.ToString());
                yield return new WaitForSeconds(1f);
            }
            StatusChanged?.Invoke("FLY!");
            State = FlightState.Flying;
            _aircraft.SetInputEnabled(true);
            yield return new WaitForSeconds(0.65f);
            StatusChanged?.Invoke(string.Empty);
        }

        private void Update()
        {
            if (State != FlightState.Flying)
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
            if (State == FlightState.Complete)
            {
                return;
            }
            State = FlightState.Complete;
            _aircraft.SetInputEnabled(false);
            var result = new HuntResult
            {
                challengeId = _challenge.challengeId,
                timeMs = Mathf.RoundToInt(_elapsed * 1000f),
                score = FlightScore.Calculate(_cleared.Count, _gateCount, _elapsed, complete),
                collectedCount = _cleared.Count,
                totalCount = _gateCount,
                completed = complete,
                collectedRelicIds = _cleared.ToArray()
            };
            StatusChanged?.Invoke(complete ? "COURSE CLEARED" : "FLIGHT ENDED");
            Completed?.Invoke(result);
        }
    }

    public static class FlightScore
    {
        public static int Calculate(int cleared, int total, float seconds, bool completed)
        {
            var gates = cleared * 1000;
            var completion = completed ? 6000 : 0;
            var time = completed ? Mathf.Max(0, 300000 - Mathf.RoundToInt(seconds * 1000f)) / 100 : 0;
            var perfect = completed && cleared == total ? 2500 : 0;
            return gates + completion + time + perfect;
        }
    }

    public sealed class FlightRunRecorder : MonoBehaviour
    {
        private const float SampleInterval = 0.1f;
        private FlightSession _session;
        private AircraftController _aircraft;
        private ReplayRun _run;
        private float _nextSample;
        private string _pendingGate;

        public event Action<ReplayRun> RecordingCompleted;

        public void Initialize(FlightSession session, AircraftController aircraft, string playerId, string playerName, int colorIndex)
        {
            _session = session;
            _aircraft = aircraft;
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
                vehicleId = "skywing-v1",
                vehicleColorIndex = colorIndex
            };
            session.GateCleared += OnGateCleared;
            session.Completed += OnCompleted;
        }

        private void Update()
        {
            if (_session == null || _session.State != FlightState.Flying || _session.Elapsed < _nextSample)
            {
                return;
            }
            Capture(string.Empty);
            _nextSample = _session.Elapsed + SampleInterval;
        }

        private void OnGateCleared(string id)
        {
            _pendingGate = id;
            Capture("gate-clear");
        }

        private void OnCompleted(HuntResult result)
        {
            Capture("finish");
            _run.result = result;
            RecordingCompleted?.Invoke(_run);
        }

        private void Capture(string cameraEvent)
        {
            _run.frames.Add(new ReplayFrame
            {
                timestamp = _session.Elapsed,
                position = _aircraft.transform.position,
                rotation = _aircraft.transform.rotation,
                speed = _aircraft.Speed,
                vehicleState = _aircraft.IsBoosting ? "boost" : "flight",
                collectedItem = _pendingGate,
                cameraEvent = cameraEvent
            });
            _pendingGate = string.Empty;
        }
    }

    public sealed class FlightGhostPlayback : MonoBehaviour
    {
        private ReplayRun _run;
        private FlightSession _session;
        private int _frameIndex;

        public void Initialize(ReplayRun run, FlightSession session)
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
