using System;
using System.Text;
using SignalHunt.Backend;
using SignalHunt.Replay;
using SignalHunt.UI;
using TreasureHunt.Gameplay;
using TreasureHunt.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreasureHunt.UI
{
    public sealed class TreasureHud : MonoBehaviour
    {
        private static readonly Color Navy = new(0.010f, 0.025f, 0.052f, 0.92f);
        private static readonly Color ResultNavy = new(0.006f, 0.018f, 0.042f, 0.97f);
        private static readonly Color Cyan = new(0.04f, 0.90f, 1f, 1f);
        private static readonly Color Gold = new(1f, 0.70f, 0.10f, 1f);
        private static readonly Color Muted = new(0.70f, 0.79f, 0.89f, 1f);

        private Font _font;
        private GameObject _canvasRoot;
        private GameObject _controls;
        private GameObject _detectorPanel;
        private GameObject _colorButton;
        private GameObject _resultPanel;
        private Text _timeText;
        private Text _artifactText;
        private Text _statusText;
        private Text _detectorText;
        private Text _resultTitle;
        private Text _resultStats;
        private Text _personalBest;
        private Text _leaderboard;
        private Text _network;
        private RectTransform _progressFill;
        private TreasureSession _session;
        private ExplorerController _explorer;
        private TreasureArtifact[] _artifacts;
        private float _nextDetectorUpdate;

        public event Action WatchRequested;
        public event Action FilmRequested;
        public event Action StyleRequested;

        public void Initialize(TreasureSession session, SignalHunt.Core.DailyChallenge challenge)
        {
            _session = session;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildCanvas(challenge);
            session.TimeChanged += OnTimeChanged;
            session.ArtifactCountChanged += OnArtifactCountChanged;
            session.StatusChanged += status => _statusText.text = status;
            session.Completed += OnCompleted;
            OnTimeChanged(0f);
            OnArtifactCountChanged(0, challenge.collectibleCount);
        }

        public void Track(ExplorerController explorer)
        {
            _explorer = explorer;
            _artifacts = FindObjectsByType<TreasureArtifact>();
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

        public void SetRunOutcome(RunSaveOutcome outcome, SignalHunt.Gameplay.HuntResult result)
        {
            if (outcome == null || result == null)
            {
                return;
            }
            _resultTitle.text = outcome.isPersonalBest
                ? result.completed ? "NEW HUNT RECORD" : "BEST SEARCH SO FAR"
                : $"HUNT {outcome.attemptNumber} COMPLETE";
            if (result.completed && outcome.improvementMs > 0)
            {
                _personalBest.text = $"-{FormatMilliseconds(outcome.improvementMs)} FASTER · HUNT {outcome.attemptNumber}";
            }
            else if (result.completed && outcome.bestTimeMs >= 0)
            {
                _personalBest.text = $"DAILY BEST {FormatMilliseconds(outcome.bestTimeMs)} · HUNT {outcome.attemptNumber}";
            }
            else
            {
                _personalBest.text = $"FOLLOW THE SIGNAL · HUNT {outcome.attemptNumber}";
            }
        }

        public void SetLeaderboard(LeaderboardEntry[] entries)
        {
            if (entries == null || entries.Length == 0)
            {
                _leaderboard.text = "NO FULL CLEARS YET\nBE TODAY'S FIRST TREASURE HUNTER";
                return;
            }
            var builder = new StringBuilder();
            for (var index = 0; index < Mathf.Min(3, entries.Length); index++)
            {
                var entry = entries[index];
                var name = string.IsNullOrWhiteSpace(entry.playerName) ? "HUNTER" : entry.playerName.ToUpperInvariant();
                if (name.Length > 12)
                {
                    name = name.Substring(0, 12);
                }
                builder.Append(entry.rank).Append("  ").Append(name).Append("  ")
                    .Append(FormatMilliseconds(entry.timeMs)).Append("  ×").Append(Mathf.Max(1, entry.attemptCount));
                if (index < Mathf.Min(3, entries.Length) - 1)
                {
                    builder.AppendLine();
                }
            }
            _leaderboard.text = builder.ToString();
        }

        public void SetLeaderboardUnavailable()
        {
            _leaderboard.text = "ONLINE BOARD UNAVAILABLE\nTHIS SEARCH IS SAVED LOCALLY";
        }

        private void BuildCanvas(SignalHunt.Core.DailyChallenge challenge)
        {
            _canvasRoot = new GameObject("Treasure Hunter HUD");
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

            var header = Panel("Treasure Header", safe, new Vector2(0.045f, 0.865f), new Vector2(0.955f, 0.972f), Navy);
            Panel("Header Accent", header, new Vector2(0f, 0.12f), new Vector2(0.012f, 0.88f), Gold);
            _artifactText = Text("0 / 10 FOUND", header, new Vector2(0.05f, 0.16f), new Vector2(0.48f, 0.84f), 30,
                FontStyle.Bold, Cyan, TextAnchor.MiddleLeft);
            _timeText = Text("00:00.000", header, new Vector2(0.52f, 0.16f), new Vector2(0.94f, 0.84f), 38,
                FontStyle.Bold, Color.white, TextAnchor.MiddleRight);

            var progress = Panel("Treasure Progress", safe, new Vector2(0.075f, 0.852f), new Vector2(0.925f, 0.858f),
                new Color(0.15f, 0.22f, 0.34f, 0.9f));
            _progressFill = Panel("Treasure Progress Fill", progress, Vector2.zero, new Vector2(0f, 1f), Gold);

            var detectorPanel = Panel("Treasure Detector", safe,
                new Vector2(0.05f, 0.792f), new Vector2(0.95f, 0.837f), Navy);
            _detectorPanel = detectorPanel.gameObject;
            _detectorText = Text("DETECTOR CALIBRATING…", detectorPanel,
                new Vector2(0.05f, 0f), new Vector2(0.95f, 1f), 22, FontStyle.Bold, Cyan, TextAnchor.MiddleLeft);
            _statusText = Text(string.Empty, safe, new Vector2(0.12f, 0.46f), new Vector2(0.88f, 0.60f), 72,
                FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

            _controls = new GameObject("Explorer Controls", typeof(RectTransform));
            var controls = _controls.GetComponent<RectTransform>();
            controls.SetParent(safe, false);
            controls.anchorMin = Vector2.zero;
            controls.anchorMax = new Vector2(1f, 0.285f);
            controls.offsetMin = controls.offsetMax = Vector2.zero;
            SteeringPad(controls, Cyan);
            ControlButton("SCAN", controls, new Vector2(0.72f, 0.12f), new Vector2(0.96f, 0.78f),
                ExplorerControl.Scan, Gold);
            Text("DRAG TO MOVE  ·  TAP SCAN", controls, new Vector2(0.05f, 0.88f),
                new Vector2(0.95f, 0.98f), 19, FontStyle.Bold, Muted, TextAnchor.MiddleCenter).raycastTarget = false;
            BuildResultPanel(safe);
        }

        private void BuildResultPanel(Transform parent)
        {
            var panel = Panel("Hunt Complete", parent, new Vector2(0.045f, 0.10f), new Vector2(0.955f, 0.82f), ResultNavy);
            _resultPanel = panel.gameObject;
            _resultTitle = Text("VAULT COMPLETE", panel, new Vector2(0.06f, 0.85f), new Vector2(0.94f, 0.96f),
                43, FontStyle.Bold, Cyan, TextAnchor.MiddleCenter);
            _resultStats = Text(string.Empty, panel, new Vector2(0.08f, 0.71f), new Vector2(0.92f, 0.85f),
                31, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _personalBest = Text("CALCULATING HUNT RECORD…", panel, new Vector2(0.08f, 0.63f),
                new Vector2(0.92f, 0.71f), 22, FontStyle.Bold, Gold, TextAnchor.MiddleCenter);
            Text("TODAY'S HUNTERS", panel, new Vector2(0.08f, 0.56f), new Vector2(0.92f, 0.63f), 22,
                FontStyle.Bold, Cyan, TextAnchor.MiddleLeft);
            _leaderboard = Text("SYNCING SEARCH ROUTES…", panel, new Vector2(0.08f, 0.31f),
                new Vector2(0.92f, 0.56f), 21, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
            _network = Text(string.Empty, panel, new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.31f), 18,
                FontStyle.Normal, Muted, TextAnchor.MiddleCenter);
            ActionButton("PLAY AGAIN", panel, new Vector2(0.06f, 0.045f), new Vector2(0.94f, 0.19f), Gold,
                () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
            _resultPanel.SetActive(false);
        }

        private void OnTimeChanged(float seconds) =>
            _timeText.text = FormatMilliseconds(Mathf.RoundToInt(seconds * 1000f));

        private void OnArtifactCountChanged(int found, int total)
        {
            _artifactText.text = $"{found} / {total} FOUND";
            _progressFill.anchorMax = new Vector2(total <= 0 ? 0f : found / (float)total, 1f);
        }

        private void OnCompleted(SignalHunt.Gameplay.HuntResult result)
        {
            ExplorerInputState.Clear();
            _controls.SetActive(false);
            _detectorPanel.SetActive(false);
            if (_colorButton != null)
            {
                _colorButton.SetActive(false);
            }
            _statusText.gameObject.SetActive(false);
            _resultPanel.SetActive(true);
            _resultTitle.text = result.completed ? "VAULT COMPLETE" : "SEARCH ENDED";
            _resultStats.text =
                $"{result.collectedCount}/{result.totalCount} ARTIFACTS · {result.score:N0} PTS\n{FormatMilliseconds(result.timeMs)}";
            _detectorText.text = result.completed ? "EVERY ARTIFACT SECURED" : "DETECTOR OFFLINE";
        }

        private void Update()
        {
            if (_session == null || _session.State != TreasureState.Searching || _explorer == null ||
                Time.unscaledTime < _nextDetectorUpdate)
            {
                return;
            }
            _nextDetectorUpdate = Time.unscaledTime + 0.12f;
            TreasureArtifact nearest = null;
            var nearestDistance = float.MaxValue;
            foreach (var artifact in _artifacts ?? Array.Empty<TreasureArtifact>())
            {
                if (artifact == null || !artifact.gameObject.activeInHierarchy)
                {
                    continue;
                }
                var distance = Vector3.Distance(_explorer.transform.position, artifact.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = artifact;
                    nearestDistance = distance;
                }
            }
            if (nearest == null)
            {
                _detectorText.text = "EVERY ARTIFACT SECURED";
                return;
            }
            var direction = nearest.transform.position - _explorer.transform.position;
            direction.y = 0f;
            var angle = Vector3.SignedAngle(_explorer.transform.forward, direction, Vector3.up);
            var bearing = Mathf.Abs(angle) < 22f ? "AHEAD" : Mathf.Abs(angle) > 150f ? "BEHIND" : angle > 0f ? "RIGHT" : "LEFT";
            var signal = nearestDistance < 12f ? "STRONG" : nearestDistance < 30f ? "MEDIUM" : "FAINT";
            var scan = _explorer.ScanCooldown01 <= 0f ? "SCAN READY" : "RECHARGING";
            _detectorText.text = $"{signal} · {Mathf.RoundToInt(nearestDistance)}m · {bearing} · {scan}";
        }

        private void ControlButton(string label, Transform parent, Vector2 min, Vector2 max, ExplorerControl control,
            Color color)
        {
            var image = ButtonSurface(label, parent, min, max, color);
            image.gameObject.AddComponent<ExplorerHoldButton>().Initialize(control, image, color);
        }

        private void SteeringPad(Transform parent, Color color)
        {
            var pad = Panel("Move Pad", parent, new Vector2(0.04f, 0.10f), new Vector2(0.64f, 0.83f),
                new Color(Navy.r, Navy.g, Navy.b, 0.78f));
            var outline = pad.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(color.r, color.g, color.b, 0.72f);
            outline.effectDistance = new Vector2(2f, -2f);
            var guide = Panel("Steering Track", pad, new Vector2(0.12f, 0.48f), new Vector2(0.88f, 0.52f),
                new Color(color.r, color.g, color.b, 0.28f));
            guide.GetComponent<Image>().raycastTarget = false;
            var verticalGuide = Panel("Movement Track", pad, new Vector2(0.49f, 0.14f), new Vector2(0.51f, 0.86f),
                new Color(color.r, color.g, color.b, 0.20f));
            verticalGuide.GetComponent<Image>().raycastTarget = false;
            var thumb = Panel("Steering Thumb", pad, new Vector2(0.38f, 0.28f), new Vector2(0.62f, 0.72f),
                new Color(color.r, color.g, color.b, 0.88f));
            thumb.GetComponent<Image>().raycastTarget = false;
            Text("MOVE", pad, new Vector2(0.30f, 0.03f), new Vector2(0.70f, 0.22f), 20,
                FontStyle.Bold, Color.white, TextAnchor.MiddleCenter).raycastTarget = false;
            pad.gameObject.AddComponent<TouchControlPad>().Initialize(
                pad, thumb, ExplorerInputState.SetMove, false);
        }

        private GameObject ActionButton(string label, Transform parent, Vector2 min, Vector2 max, Color color,
            UnityEngine.Events.UnityAction action)
        {
            var image = ButtonSurface(label, parent, min, max, color);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            return image.gameObject;
        }

        private Image ButtonSurface(string label, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var panel = Panel(label, parent, min, max, new Color(color.r, color.g, color.b, 0.34f));
            var image = panel.GetComponent<Image>();
            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(color.r, color.g, color.b, 0.88f);
            outline.effectDistance = new Vector2(2f, -2f);
            Text(label, panel, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f), 30,
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
