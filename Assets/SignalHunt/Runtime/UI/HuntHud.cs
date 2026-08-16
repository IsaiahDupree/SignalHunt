using System;
using SignalHunt.Core;
using SignalHunt.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SignalHunt.UI
{
    public sealed class HuntHud : MonoBehaviour
    {
        private static readonly Color PanelColor = new(0.025f, 0.04f, 0.085f, 0.90f);
        private static readonly Color Cyan = new(0.08f, 0.86f, 1f, 1f);
        private static readonly Color Magenta = new(1f, 0.12f, 0.67f, 1f);

        private Font _font;
        private Text _timeText;
        private Text _relicText;
        private Text _statusText;
        private Text _resultTitle;
        private Text _resultStats;
        private Text _networkStatus;
        private GameObject _resultPanel;
        private GameObject _controls;
        private HuntSession _session;

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

            var topPanel = Panel("Daily Challenge", safe, new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.97f), PanelColor);
            Text("SIGNAL HUNT", topPanel, new Vector2(0.05f, 0.53f), new Vector2(0.55f, 0.92f), 40, FontStyle.Bold, Cyan, TextAnchor.MiddleLeft);
            Text($"TODAY · {challenge.dateKey} · SEED {challenge.displaySeed:D5}", topPanel,
                new Vector2(0.05f, 0.16f), new Vector2(0.72f, 0.51f), 20, FontStyle.Normal,
                new Color(0.7f, 0.76f, 0.88f), TextAnchor.MiddleLeft);
            _timeText = Text("00:00.000", topPanel, new Vector2(0.62f, 0.50f), new Vector2(0.95f, 0.90f),
                34, FontStyle.Bold, Color.white, TextAnchor.MiddleRight);
            _relicText = Text("0 / 10 SIGNALS", topPanel, new Vector2(0.64f, 0.13f), new Vector2(0.95f, 0.48f),
                20, FontStyle.Bold, Magenta, TextAnchor.MiddleRight);

            _statusText = Text(string.Empty, safe, new Vector2(0.12f, 0.46f), new Vector2(0.88f, 0.60f),
                72, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _statusText.horizontalOverflow = HorizontalWrapMode.Overflow;

            ActionButton("STYLE", safe, new Vector2(0.79f, 0.775f), new Vector2(0.96f, 0.835f), Magenta,
                () => StyleRequested?.Invoke());

            _controls = new GameObject("Touch Controls");
            var controlsRect = _controls.AddComponent<RectTransform>();
            controlsRect.SetParent(safe, false);
            controlsRect.anchorMin = new Vector2(0f, 0f);
            controlsRect.anchorMax = new Vector2(1f, 0.28f);
            controlsRect.offsetMin = controlsRect.offsetMax = Vector2.zero;
            ControlButton("LEFT", controlsRect, new Vector2(0.04f, 0.13f), new Vector2(0.22f, 0.65f), VehicleControl.SteerLeft, Cyan);
            ControlButton("RIGHT", controlsRect, new Vector2(0.24f, 0.13f), new Vector2(0.42f, 0.65f), VehicleControl.SteerRight, Cyan);
            ControlButton("BRAKE", controlsRect, new Vector2(0.58f, 0.13f), new Vector2(0.76f, 0.65f), VehicleControl.Brake, Magenta);
            ControlButton("GO", controlsRect, new Vector2(0.78f, 0.13f), new Vector2(0.96f, 0.78f), VehicleControl.Accelerate, Cyan);

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
        }

        private void ControlButton(string label, Transform parent, Vector2 min, Vector2 max, VehicleControl control, Color color)
        {
            var image = ButtonSurface(label, parent, min, max, color);
            image.gameObject.AddComponent<HoldControlButton>().Initialize(control);
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
            var rect = Panel(label, parent, min, max, new Color(color.r, color.g, color.b, 0.72f));
            var image = rect.GetComponent<Image>();
            Text(label, rect, new Vector2(0f, 0f), new Vector2(1f, 1f), 27, FontStyle.Bold,
                new Color(0.02f, 0.04f, 0.08f), TextAnchor.MiddleCenter).raycastTarget = false;
            return image;
        }

        private RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var rect = NewRect(name, parent);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = rect.gameObject.AddComponent<Image>();
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
