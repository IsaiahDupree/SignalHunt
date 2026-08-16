using System;
using System.Text;
using SignalHunt.Backend;
using SignalHunt.Gameplay;
using SignalHunt.Replay;
using SignalHunt.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WaypointWings.Gameplay;
using WaypointWings.World;

namespace WaypointWings.UI
{
    public sealed class WingsHud : MonoBehaviour
    {
        private static readonly Color Navy = new(0.012f, 0.03f, 0.075f, 0.90f);
        private static readonly Color ResultNavy = new(0.008f, 0.022f, 0.055f, 0.97f);
        private static readonly Color Cyan = new(0.04f, 0.88f, 1f, 1f);
        private static readonly Color Gold = new(1f, 0.70f, 0.12f, 1f);
        private static readonly Color Muted = new(0.69f, 0.78f, 0.90f, 1f);

        private Font _font;
        private GameObject _canvasRoot;
        private GameObject _controls;
        private GameObject _resultPanel;
        private Text _timeText;
        private Text _gateText;
        private Text _statusText;
        private Text _targetText;
        private Text _resultTitle;
        private Text _resultStats;
        private Text _personalBest;
        private Text _leaderboard;
        private Text _network;
        private RectTransform _progressFill;
        private FlightSession _session;
        private AircraftController _aircraft;
        private FlightGate[] _gates;
        private float _nextNavigationUpdate;

        public event Action WatchRequested;
        public event Action FilmRequested;
        public event Action StyleRequested;

        public void Initialize(FlightSession session, SignalHunt.Core.DailyChallenge challenge)
        {
            _session = session;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildCanvas(challenge);
            session.TimeChanged += OnTimeChanged;
            session.GateCountChanged += OnGateCountChanged;
            session.StatusChanged += status => _statusText.text = status;
            session.Completed += OnCompleted;
            OnTimeChanged(0f);
            OnGateCountChanged(0, challenge.collectibleCount);
        }

        public void Track(AircraftController aircraft)
        {
            _aircraft = aircraft;
            _gates = FindObjectsByType<FlightGate>();
        }

        public void SetNetworkStatus(string message)
        {
            if (_network != null)
            {
                _network.text = message;
            }
        }

        public void SetPresentationMode(bool active)
        {
            if (_canvasRoot != null)
            {
                _canvasRoot.SetActive(!active);
            }
        }

        public void SetRunOutcome(RunSaveOutcome outcome, HuntResult result)
        {
            if (outcome == null || result == null)
            {
                return;
            }
            _resultTitle.text = outcome.isPersonalBest
                ? result.completed ? "NEW FLIGHT RECORD" : "BEST FLIGHT SO FAR"
                : $"FLIGHT {outcome.attemptNumber} COMPLETE";
            if (result.completed && outcome.improvementMs > 0)
            {
                _personalBest.text = $"-{FormatMilliseconds(outcome.improvementMs)} FASTER · FLIGHT {outcome.attemptNumber}";
            }
            else if (result.completed && outcome.bestTimeMs >= 0)
            {
                _personalBest.text = $"DAILY BEST {FormatMilliseconds(outcome.bestTimeMs)} · FLIGHT {outcome.attemptNumber}";
            }
            else
            {
                _personalBest.text = $"KEEP FLYING · FLIGHT {outcome.attemptNumber}";
            }
        }

        public void SetLeaderboard(LeaderboardEntry[] entries)
        {
            if (entries == null || entries.Length == 0)
            {
                _leaderboard.text = "NO RANKED FLIGHTS YET\nSET TODAY'S FIRST PATH";
                return;
            }
            var builder = new StringBuilder();
            for (var index = 0; index < Mathf.Min(5, entries.Length); index++)
            {
                var entry = entries[index];
                var name = string.IsNullOrWhiteSpace(entry.playerName) ? "PILOT" : entry.playerName.ToUpperInvariant();
                if (name.Length > 12)
                {
                    name = name.Substring(0, 12);
                }
                builder.Append(entry.rank).Append("  ").Append(name).Append("  ")
                    .Append(FormatMilliseconds(entry.timeMs)).Append("  ×").Append(Mathf.Max(1, entry.attemptCount));
                if (index < Mathf.Min(5, entries.Length) - 1)
                {
                    builder.AppendLine();
                }
            }
            _leaderboard.text = builder.ToString();
        }

        public void SetLeaderboardUnavailable()
        {
            _leaderboard.text = "ONLINE BOARD UNAVAILABLE\nTHIS FLIGHT IS SAVED LOCALLY";
        }

        private void BuildCanvas(SignalHunt.Core.DailyChallenge challenge)
        {
            _canvasRoot = new GameObject("Waypoint Wings HUD");
            _canvasRoot.transform.SetParent(transform, false);
            var canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            var scaler = _canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            _canvasRoot.AddComponent<GraphicRaycaster>();

            var safe = Rect("Safe Area", _canvasRoot.transform);
            safe.anchorMin = Vector2.zero;
            safe.anchorMax = Vector2.one;
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            safe.gameObject.AddComponent<SafeAreaFitter>();

            var header = Panel("Flight Header", safe, new Vector2(0.045f, 0.865f), new Vector2(0.955f, 0.972f), Navy);
            Panel("Header Accent", header, new Vector2(0f, 0.12f), new Vector2(0.012f, 0.88f), Gold);
            Text("WAYPOINT WINGS", header, new Vector2(0.05f, 0.50f), new Vector2(0.62f, 0.91f), 42,
                FontStyle.Bold, Cyan, TextAnchor.MiddleLeft);
            Text($"{challenge.stageDisplayName.ToUpperInvariant()} · {challenge.dateKey} · {challenge.displaySeed:D5}",
                header, new Vector2(0.05f, 0.12f), new Vector2(0.69f, 0.50f), 21, FontStyle.Normal, Muted,
                TextAnchor.MiddleLeft);
            _timeText = Text("00:00.000", header, new Vector2(0.63f, 0.52f), new Vector2(0.94f, 0.90f), 38,
                FontStyle.Bold, Color.white, TextAnchor.MiddleRight);
            _gateText = Text("0 / 14 GATES", header, new Vector2(0.63f, 0.12f), new Vector2(0.94f, 0.48f), 22,
                FontStyle.Bold, Gold, TextAnchor.MiddleRight);

            var progress = Panel("Flight Progress", safe, new Vector2(0.075f, 0.852f), new Vector2(0.925f, 0.858f),
                new Color(0.15f, 0.22f, 0.34f, 0.9f));
            _progressFill = Panel("Flight Progress Fill", progress, Vector2.zero, new Vector2(0f, 1f), Gold);

            _targetText = Text("ACQUIRING GATE…", Panel("Flight Navigator", safe, new Vector2(0.05f, 0.792f),
                    new Vector2(0.71f, 0.837f), Navy), new Vector2(0.05f, 0f), new Vector2(0.95f, 1f), 22,
                FontStyle.Bold, Cyan, TextAnchor.MiddleLeft);
            ActionButton("COLOR", safe, new Vector2(0.74f, 0.792f), new Vector2(0.95f, 0.837f), Gold,
                () => StyleRequested?.Invoke());
            _statusText = Text(string.Empty, safe, new Vector2(0.12f, 0.46f), new Vector2(0.88f, 0.60f), 72,
                FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

            _controls = new GameObject("Flight Controls", typeof(RectTransform));
            var controlsRect = _controls.GetComponent<RectTransform>();
            controlsRect.SetParent(safe, false);
            controlsRect.anchorMin = Vector2.zero;
            controlsRect.anchorMax = new Vector2(1f, 0.255f);
            controlsRect.offsetMin = controlsRect.offsetMax = Vector2.zero;
            ControlButton("LEFT", controlsRect, new Vector2(0.03f, 0.12f), new Vector2(0.19f, 0.68f), FlightControl.TurnLeft, Cyan);
            ControlButton("RIGHT", controlsRect, new Vector2(0.21f, 0.12f), new Vector2(0.37f, 0.68f), FlightControl.TurnRight, Cyan);
            ControlButton("DOWN", controlsRect, new Vector2(0.43f, 0.12f), new Vector2(0.57f, 0.62f), FlightControl.PitchDown, Gold);
            ControlButton("UP", controlsRect, new Vector2(0.59f, 0.12f), new Vector2(0.73f, 0.68f), FlightControl.PitchUp, Gold);
            ControlButton("BOOST", controlsRect, new Vector2(0.77f, 0.12f), new Vector2(0.97f, 0.80f), FlightControl.Boost, Cyan);
            BuildResultPanel(safe);
        }

        private void BuildResultPanel(Transform parent)
        {
            var panel = Panel("Flight Complete", parent, new Vector2(0.045f, 0.10f), new Vector2(0.955f, 0.82f), ResultNavy);
            _resultPanel = panel.gameObject;
            _resultTitle = Text("COURSE CLEARED", panel, new Vector2(0.06f, 0.85f), new Vector2(0.94f, 0.96f),
                43, FontStyle.Bold, Cyan, TextAnchor.MiddleCenter);
            _resultStats = Text(string.Empty, panel, new Vector2(0.08f, 0.71f), new Vector2(0.92f, 0.85f),
                31, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _personalBest = Text("CALCULATING FLIGHT RECORD…", panel, new Vector2(0.08f, 0.63f), new Vector2(0.92f, 0.71f),
                22, FontStyle.Bold, Gold, TextAnchor.MiddleCenter);
            Text("TODAY'S PILOTS", panel, new Vector2(0.08f, 0.56f), new Vector2(0.92f, 0.63f), 22,
                FontStyle.Bold, Cyan, TextAnchor.MiddleLeft);
            _leaderboard = Text("SYNCING DAILY FLIGHTS…", panel, new Vector2(0.08f, 0.31f), new Vector2(0.92f, 0.56f),
                21, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
            _network = Text(string.Empty, panel, new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.31f), 18,
                FontStyle.Normal, Muted, TextAnchor.MiddleCenter);
            ActionButton("WATCH FLIGHT", panel, new Vector2(0.06f, 0.12f), new Vector2(0.48f, 0.23f), Cyan,
                () => WatchRequested?.Invoke());
            ActionButton("DAILY FILM", panel, new Vector2(0.52f, 0.12f), new Vector2(0.94f, 0.23f), Cyan,
                () => FilmRequested?.Invoke());
            ActionButton("FLY AGAIN", panel, new Vector2(0.06f, 0.025f), new Vector2(0.94f, 0.105f), Gold,
                () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
            _resultPanel.SetActive(false);
        }

        private void OnTimeChanged(float seconds) => _timeText.text = FormatMilliseconds(Mathf.RoundToInt(seconds * 1000f));

        private void OnGateCountChanged(int cleared, int total)
        {
            _gateText.text = $"{cleared} / {total} GATES";
            _progressFill.anchorMax = new Vector2(total <= 0 ? 0f : cleared / (float)total, 1f);
        }

        private void OnCompleted(HuntResult result)
        {
            FlightInputState.Clear();
            _controls.SetActive(false);
            _resultPanel.SetActive(true);
            _resultTitle.text = result.completed ? "COURSE CLEARED" : "FLIGHT ENDED";
            _resultStats.text = $"{result.collectedCount}/{result.totalCount} GATES · {result.score:N0} PTS\n{FormatMilliseconds(result.timeMs)}";
            _targetText.text = result.completed ? "ALL GATES CLEARED" : "FLIGHT CLOSED";
        }

        private void Update()
        {
            if (_session == null || _session.State != FlightState.Flying || _aircraft == null ||
                Time.unscaledTime < _nextNavigationUpdate)
            {
                return;
            }
            _nextNavigationUpdate = Time.unscaledTime + 0.12f;
            FlightGate next = null;
            foreach (var gate in _gates ?? Array.Empty<FlightGate>())
            {
                if (gate != null && gate.gameObject.activeInHierarchy && gate.Index == _session.ClearedCount)
                {
                    next = gate;
                    break;
                }
            }
            if (next == null)
            {
                _targetText.text = "ALL GATES CLEARED";
                return;
            }
            var local = _aircraft.transform.InverseTransformPoint(next.transform.position);
            var horizontal = Mathf.Abs(local.x) < 8f ? "CENTER" : local.x > 0f ? "RIGHT" : "LEFT";
            var vertical = Mathf.Abs(local.y) < 5f ? string.Empty : local.y > 0f ? " · ABOVE" : " · BELOW";
            _targetText.text = $"GATE {_session.ClearedCount + 1} · {Mathf.RoundToInt(local.magnitude)}m · {horizontal}{vertical}";
        }

        private void ControlButton(string label, Transform parent, Vector2 min, Vector2 max, FlightControl control, Color color)
        {
            var image = ButtonSurface(label, parent, min, max, color);
            image.gameObject.AddComponent<FlightHoldButton>().Initialize(control, image, color);
        }

        private void ActionButton(string label, Transform parent, Vector2 min, Vector2 max, Color color,
            UnityEngine.Events.UnityAction action)
        {
            var image = ButtonSurface(label, parent, min, max, color);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
        }

        private Image ButtonSurface(string label, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var panel = Panel(label, parent, min, max, new Color(color.r, color.g, color.b, 0.34f));
            var image = panel.GetComponent<Image>();
            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(color.r, color.g, color.b, 0.88f);
            outline.effectDistance = new Vector2(2f, -2f);
            Text(label, panel, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f), 28,
                FontStyle.Bold, Color.white, TextAnchor.MiddleCenter).raycastTarget = false;
            return image;
        }

        private RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var rect = Rect(name, parent);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiSpriteFactory.RoundedRectangle;
            image.type = Image.Type.Sliced;
            image.color = color;
            return rect;
        }

        private Text Text(string value, Transform parent, Vector2 min, Vector2 max, int size, FontStyle style,
            Color color, TextAnchor alignment)
        {
            var rect = Rect("Text", parent);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = rect.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = size;
            return text;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var instance = new GameObject(name, typeof(RectTransform));
            var rect = instance.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }
            var instance = new GameObject("EventSystem");
            instance.AddComponent<EventSystem>();
            instance.AddComponent<StandaloneInputModule>();
        }

        private static string FormatMilliseconds(int milliseconds)
        {
            var span = TimeSpan.FromMilliseconds(milliseconds);
            return $"{(int)span.TotalMinutes:00}:{span.Seconds:00}.{span.Milliseconds:000}";
        }
    }
}
