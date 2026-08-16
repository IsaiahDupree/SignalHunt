using System;
using SignalHunt.Core;
using SignalHunt.Gameplay;
using SignalHunt.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SignalHunt.UI
{
    public sealed class HuntHud : MonoBehaviour
    {
        private static readonly Color PanelColor = new(0.018f, 0.03f, 0.075f, 0.88f);
        private static readonly Color Cyan = new(0.08f, 0.86f, 1f, 1f);
        private static readonly Color Magenta = new(1f, 0.12f, 0.67f, 1f);
        private static readonly Color MutedText = new(0.68f, 0.76f, 0.88f, 1f);

        private Font _font;
        private Text _timeText;
        private Text _relicText;
        private Text _statusText;
        private Text _resultTitle;
        private Text _resultStats;
        private Text _networkStatus;
        private Text _targetText;
        private RectTransform _progressFill;
        private GameObject _resultPanel;
        private GameObject _controls;
        private HuntSession _session;
        private HoverVehicleController _vehicle;
        private RelicPickup[] _relics;
        private float _nextTargetUpdate;

        public event Action ExportRequested;
        public event Action StyleRequested;

        public void Initialize(HuntSession session, DailyChallenge challenge)
        {
            _session = session;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildCanvas(challenge);
            session.TimeChanged += OnTimeChanged;
            session.RelicCountChanged += OnRelicCountChanged;
            session.StatusChanged += OnStatusChanged;
            session.Completed += OnCompleted;
            OnTimeChanged(0f);
            OnRelicCountChanged(0, challenge.collectibleCount);
        }

        public void SetNetworkStatus(string message)
        {
            if (_networkStatus != null)
            {
                _networkStatus.text = message;
            }
        }

        public void TrackVehicle(HoverVehicleController vehicle)
        {
            _vehicle = vehicle;
            _relics = FindObjectsByType<RelicPickup>();
        }

        private void BuildCanvas(DailyChallenge challenge)
        {
            var canvasObject = new GameObject("Signal Hunt HUD");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var safe = NewRect("Safe Area", canvasObject.transform);
            safe.anchorMin = Vector2.zero;
            safe.anchorMax = Vector2.one;
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            safe.gameObject.AddComponent<SafeAreaFitter>();

            var topPanel = Panel("Daily Challenge", safe, new Vector2(0.045f, 0.865f), new Vector2(0.955f, 0.972f), PanelColor);
            var accent = Panel("Header Accent", topPanel, new Vector2(0f, 0.12f), new Vector2(0.012f, 0.88f), Cyan);
            accent.GetComponent<Image>().raycastTarget = false;
            Text("SIGNAL HUNT", topPanel, new Vector2(0.05f, 0.50f), new Vector2(0.56f, 0.91f), 44, FontStyle.Bold, Cyan, TextAnchor.MiddleLeft);
            Text($"TODAY · {challenge.dateKey} · SEED {challenge.displaySeed:D5}", topPanel,
                new Vector2(0.05f, 0.13f), new Vector2(0.65f, 0.50f), 22, FontStyle.Normal,
                MutedText, TextAnchor.MiddleLeft);
            _timeText = Text("00:00.000", topPanel, new Vector2(0.59f, 0.52f), new Vector2(0.94f, 0.90f),
                40, FontStyle.Bold, Color.white, TextAnchor.MiddleRight);
            _relicText = Text("0 / 10 SIGNALS", topPanel, new Vector2(0.60f, 0.12f), new Vector2(0.94f, 0.48f),
                23, FontStyle.Bold, Magenta, TextAnchor.MiddleRight);

            var progressTrack = Panel("Signal Progress", safe, new Vector2(0.075f, 0.852f), new Vector2(0.925f, 0.858f),
                new Color(0.15f, 0.20f, 0.31f, 0.9f));
            _progressFill = Panel("Signal Progress Fill", progressTrack, Vector2.zero, new Vector2(0f, 1f), Cyan);

            _statusText = Text(string.Empty, safe, new Vector2(0.12f, 0.46f), new Vector2(0.88f, 0.60f),
                72, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _statusText.horizontalOverflow = HorizontalWrapMode.Overflow;

            var targetPanel = Panel("Target Compass", safe, new Vector2(0.05f, 0.792f), new Vector2(0.71f, 0.837f), PanelColor);
            _targetText = Text("SCANNING FOR SIGNAL…", targetPanel, new Vector2(0.05f, 0f), new Vector2(0.95f, 1f),
                23, FontStyle.Bold, Cyan, TextAnchor.MiddleLeft);
            ActionButton("COLOR", safe, new Vector2(0.74f, 0.792f), new Vector2(0.95f, 0.837f), Magenta,
                () => StyleRequested?.Invoke());

            _controls = new GameObject("Touch Controls");
            var controlsRect = _controls.AddComponent<RectTransform>();
            controlsRect.SetParent(safe, false);
            controlsRect.anchorMin = new Vector2(0f, 0f);
            controlsRect.anchorMax = new Vector2(1f, 0.255f);
            controlsRect.offsetMin = controlsRect.offsetMax = Vector2.zero;
            ControlButton("LEFT", controlsRect, new Vector2(0.04f, 0.12f), new Vector2(0.22f, 0.68f), VehicleControl.SteerLeft, Cyan);
            ControlButton("RIGHT", controlsRect, new Vector2(0.24f, 0.12f), new Vector2(0.42f, 0.68f), VehicleControl.SteerRight, Cyan);
            ControlButton("BRAKE", controlsRect, new Vector2(0.58f, 0.12f), new Vector2(0.76f, 0.68f), VehicleControl.Brake, Magenta);
            ControlButton("GO", controlsRect, new Vector2(0.78f, 0.12f), new Vector2(0.96f, 0.80f), VehicleControl.Accelerate, Cyan);

            BuildResultPanel(safe);
        }

        private void BuildResultPanel(Transform parent)
        {
            var rect = Panel("Run Complete", parent, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.76f), PanelColor);
            _resultPanel = rect.gameObject;
            _resultTitle = Text("SIGNAL LOCKED", rect, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.94f),
                44, FontStyle.Bold, Cyan, TextAnchor.MiddleCenter);
            _resultStats = Text(string.Empty, rect, new Vector2(0.08f, 0.40f), new Vector2(0.92f, 0.76f),
                30, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _networkStatus = Text(string.Empty, rect, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.41f),
                18, FontStyle.Normal, new Color(0.68f, 0.75f, 0.85f), TextAnchor.MiddleCenter);

            ActionButton("EXPORT REPLAY", rect, new Vector2(0.08f, 0.10f), new Vector2(0.58f, 0.27f), Cyan,
                () => ExportRequested?.Invoke());
            ActionButton("RETRY", rect, new Vector2(0.62f, 0.10f), new Vector2(0.92f, 0.27f), Magenta,
                () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
            _resultPanel.SetActive(false);
        }

        private void OnTimeChanged(float seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            _timeText.text = $"{(int)span.TotalMinutes:00}:{span.Seconds:00}.{span.Milliseconds:000}";
        }

        private void OnRelicCountChanged(int collected, int total)
        {
            _relicText.text = $"{collected} / {total} SIGNALS";
            if (_progressFill != null)
            {
                _progressFill.anchorMax = new Vector2(total <= 0 ? 0f : (float)collected / total, 1f);
            }
        }

        private void OnStatusChanged(string status)
        {
            _statusText.text = status;
        }

        private void OnCompleted(HuntResult result)
        {
            VehicleInputState.Clear();
            _controls.SetActive(false);
            _resultPanel.SetActive(true);
            _resultTitle.text = result.completed ? "SIGNAL LOCKED" : "RUN ENDED";
            _resultStats.text =
                $"{result.collectedCount}/{result.totalCount} RELICS\n{FormatMilliseconds(result.timeMs)}\nSCORE {result.score:N0}";
            if (_targetText != null)
            {
                _targetText.text = result.completed ? "ALL SIGNALS SECURED" : "HUNT CLOSED";
            }
        }

        private void Update()
        {
            if (_targetText == null || _vehicle == null || _session == null || _session.State != HuntState.Running ||
                Time.unscaledTime < _nextTargetUpdate)
            {
                return;
            }

            _nextTargetUpdate = Time.unscaledTime + 0.15f;
            RelicPickup nearest = null;
            var nearestDistance = float.MaxValue;
            foreach (var relic in _relics ?? Array.Empty<RelicPickup>())
            {
                if (relic == null || !relic.gameObject.activeInHierarchy)
                {
                    continue;
                }
                var distance = Vector3.Distance(_vehicle.transform.position, relic.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = relic;
                    nearestDistance = distance;
                }
            }

            if (nearest == null)
            {
                _targetText.text = "ALL SIGNALS SECURED";
                return;
            }

            var direction = nearest.transform.position - _vehicle.transform.position;
            direction.y = 0f;
            var angle = Vector3.SignedAngle(_vehicle.transform.forward, direction, Vector3.up);
            var bearing = Mathf.Abs(angle) < 24f ? "AHEAD" :
                Mathf.Abs(angle) > 152f ? "BEHIND" : angle > 0f ? "RIGHT" : "LEFT";
            _targetText.text = $"NEXT SIGNAL · {Mathf.RoundToInt(nearestDistance)}m · {bearing}";
        }

        private void ControlButton(string label, Transform parent, Vector2 min, Vector2 max, VehicleControl control, Color color)
        {
            var image = ButtonSurface(label, parent, min, max, color);
            image.gameObject.AddComponent<HoldControlButton>().Initialize(control, image, color);
        }

        private void ActionButton(string label, Transform parent, Vector2 min, Vector2 max, Color color, UnityEngine.Events.UnityAction action)
        {
            var image = ButtonSurface(label, parent, min, max, color);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
        }

        private Image ButtonSurface(string label, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var rect = Panel(label, parent, min, max, new Color(color.r, color.g, color.b, 0.34f));
            var image = rect.GetComponent<Image>();
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(color.r, color.g, color.b, 0.88f);
            outline.effectDistance = new Vector2(2f, -2f);
            Text(label, rect, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), 31, FontStyle.Bold,
                Color.white, TextAnchor.MiddleCenter).raycastTarget = false;
            return image;
        }

        private RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var rect = NewRect(name, parent);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiSpriteFactory.RoundedRectangle;
            image.type = Image.Type.Sliced;
            image.color = color;
            return rect;
        }

        private Text Text(string value, Transform parent, Vector2 min, Vector2 max, int size,
            FontStyle style, Color color, TextAnchor alignment)
        {
            var rect = NewRect("Text", parent);
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
            if (style == FontStyle.Bold)
            {
                var shadow = rect.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
                shadow.effectDistance = new Vector2(2f, -2f);
            }
            return text;
        }

        private static RectTransform NewRect(string name, Transform parent)
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

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static string FormatMilliseconds(int milliseconds)
        {
            var span = TimeSpan.FromMilliseconds(milliseconds);
            return $"{(int)span.TotalMinutes:00}:{span.Seconds:00}.{span.Milliseconds:000}";
        }
    }
}
