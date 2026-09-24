using System;
using System.Collections;
using SignalHunt.Backend;
using SignalHunt.Core;
using SignalHunt.Replay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SignalHunt.UI
{
    public sealed class DailyStartMenuContent
    {
        public string title;
        public string networkLabel;
        public string tagline;
        public string objective;
        public string controls;
        public string itemSummary;
        public string primaryAction;
        public string socialHook;
        public string loadingVerb;
        public Color accent;
        public Color secondary;
        public Color backgroundTop;
        public Color backgroundBottom;

        public static DailyStartMenuContent For(DailyChallenge challenge)
        {
            return challenge.appKey switch
            {
                DailyGameCatalog.TreasureHuntAppKey => new DailyStartMenuContent
                {
                    title = "TREASURE HUNTER",
                    networkLabel = "TODAY'S ISLAND",
                    tagline = "A bright island. A fresh trail every day.",
                    objective = "Follow the island paths and recover every hidden artifact.",
                    controls = "DRAG TO MOVE  ·  TAP SCAN",
                    itemSummary = $"{challenge.collectibleCount} LOST ARTIFACTS",
                    primaryAction = "PLAY",
                    socialHook = "Same island for everyone. Retry whenever you want.",
                    loadingVerb = "BURYING TODAY'S ARTIFACTS",
                    accent = new Color(1f, 0.70f, 0.10f),
                    secondary = new Color(0.04f, 0.90f, 1f),
                    backgroundTop = new Color(0.18f, 0.62f, 0.82f),
                    backgroundBottom = new Color(0.04f, 0.34f, 0.48f)
                },
                DailyGameCatalog.WaypointRallyAppKey => new DailyStartMenuContent
                {
                    title = "WAYPOINT RALLY",
                    networkLabel = "TODAY'S ISLAND",
                    tagline = "One sunny island loop. Find your fastest line.",
                    objective = "Follow the island road and clear every gate in order.",
                    controls = "AUTO-DRIVE  ·  DRAG TO STEER  ·  HOLD BRAKE",
                    itemSummary = $"{challenge.collectibleCount} ROUTE WAYPOINTS",
                    primaryAction = "PLAY",
                    socialHook = "Same island for everyone. Retry whenever you want.",
                    loadingVerb = "DRAWING TODAY'S ROUTE",
                    accent = new Color(1f, 0.38f, 0.08f),
                    secondary = new Color(0.05f, 0.86f, 1f),
                    backgroundTop = new Color(0.24f, 0.68f, 0.88f),
                    backgroundBottom = new Color(0.06f, 0.40f, 0.52f)
                },
                DailyGameCatalog.WaypointWingsAppKey => new DailyStartMenuContent
                {
                    title = "WAYPOINT WINGS",
                    networkLabel = "TODAY'S ISLANDS",
                    tagline = "A calm island flight with a new path every day.",
                    objective = "Guide the plane through each wide air gate in order.",
                    controls = "DRAG TO FLY  ·  HOLD BOOST",
                    itemSummary = $"{challenge.collectibleCount} AIR GATES",
                    primaryAction = "PLAY",
                    socialHook = "Crashes respawn automatically. Retry whenever you want.",
                    loadingVerb = "OPENING TODAY'S SKYWAY",
                    accent = new Color(0.12f, 0.78f, 1f),
                    secondary = new Color(1f, 0.72f, 0.18f),
                    backgroundTop = new Color(0.30f, 0.72f, 0.94f),
                    backgroundBottom = new Color(0.08f, 0.46f, 0.68f)
                },
                _ => new DailyStartMenuContent
                {
                    title = "SIGNAL HUNT",
                    networkLabel = "TODAY'S ISLAND",
                    tagline = "A simple island hunt with a fresh route every day.",
                    objective = "Follow the island road and collect every signal.",
                    controls = "AUTO-DRIVE  ·  DRAG TO STEER  ·  HOLD BRAKE",
                    itemSummary = $"{challenge.collectibleCount} HIDDEN SIGNALS",
                    primaryAction = "PLAY",
                    socialHook = "Same island for everyone. Retry whenever you want.",
                    loadingVerb = "GENERATING TODAY'S SIGNAL GRID",
                    accent = new Color(0.06f, 0.88f, 1f),
                    secondary = new Color(1f, 0.72f, 0.18f),
                    backgroundTop = new Color(0.24f, 0.70f, 0.90f),
                    backgroundBottom = new Color(0.05f, 0.42f, 0.56f)
                }
            };
        }
    }

    public sealed class DailyStartMenu : MonoBehaviour
    {
        private Font _font;
        private DailyChallenge _challenge;
        private DailyStartMenuContent _content;
        private GameObject _canvasRoot;
        private GameObject _loadingRoot;
        private GameObject _menuRoot;
        private GameObject _instructionsRoot;
        private CanvasGroup _canvasGroup;
        private Image _backgroundImage;
        private RectTransform _progressFill;
        private RectTransform _orbit;
        private Image _loadingCore;
        private Text _loadingStatus;
        private Text _resetText;
        private Text _styleText;
        private Button _primaryButton;
        private float _targetProgress;
        private float _shownProgress;
        private bool _transitioning;

        public event Action PlayRequested;
        public event Action FilmRequested;
        public event Action StyleRequested;

        public void Initialize(DailyChallenge challenge)
        {
            _challenge = challenge ?? throw new ArgumentNullException(nameof(challenge));
            _content = DailyStartMenuContent.For(challenge);
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildCanvas();
            SetLoadingProgress(0.08f, "LOCKING DAILY SEED");
        }

        public void SetLoadingProgress(float progress, string status)
        {
            _targetProgress = Mathf.Clamp01(progress);
            if (_loadingStatus != null)
            {
                _loadingStatus.text = status;
            }
        }

        public void ShowMenu()
        {
            _targetProgress = 1f;
            _shownProgress = 1f;
            ApplyProgress();
            _loadingRoot.SetActive(false);
            _menuRoot.SetActive(true);
            _backgroundImage.color = new Color(1f, 1f, 1f, 0.28f);
            UpdateResetText();
        }

        public void SetPresentationMode(bool active)
        {
            if (_canvasRoot != null)
            {
                _canvasRoot.SetActive(!active);
            }
        }

        public void NotifyStyleChanged(int styleIndex)
        {
            if (_styleText != null)
            {
                _styleText.text = $"COLOR {Mathf.Abs(styleIndex) % 4 + 1:00} / 04";
            }
        }

        public void BeginForAutomation()
        {
            if (!_transitioning)
            {
                StartCoroutine(EnterGame());
            }
        }

        public bool CaptureIfRequested(string argument)
        {
            var arguments = Environment.GetCommandLineArgs();
            var marker = Array.IndexOf(arguments, argument);
            if (marker < 0 || marker + 1 >= arguments.Length)
            {
                return false;
            }
            StartCoroutine(CaptureMenu(arguments[marker + 1]));
            return true;
        }

        private static IEnumerator CaptureMenu(string path)
        {
            yield return new WaitForSecondsRealtime(0.65f);
            ScreenCapture.CaptureScreenshot(path, 1);
            yield return new WaitForSecondsRealtime(0.8f);
            Application.Quit(0);
        }

        private void BuildCanvas()
        {
            _canvasRoot = new GameObject("Daily Start Experience");
            _canvasRoot.transform.SetParent(transform, false);
            var canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = _canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            _canvasRoot.AddComponent<GraphicRaycaster>();
            _canvasGroup = _canvasRoot.AddComponent<CanvasGroup>();

            var background = Rect("Programmatic Gradient", _canvasRoot.transform);
            Stretch(background);
            _backgroundImage = background.gameObject.AddComponent<Image>();
            _backgroundImage.sprite = CreateGradientSprite(_content.backgroundTop, _content.backgroundBottom);
            _backgroundImage.type = UnityEngine.UI.Image.Type.Simple;
            _backgroundImage.raycastTarget = true;

            BuildDecoration(background);
            _loadingRoot = Rect("Daily Loading Screen", _canvasRoot.transform).gameObject;
            Stretch((RectTransform)_loadingRoot.transform);
            BuildLoading(_loadingRoot.transform);

            _menuRoot = Rect("Daily Start Menu", _canvasRoot.transform).gameObject;
            Stretch((RectTransform)_menuRoot.transform);
            BuildMenu(_menuRoot.transform);
            _menuRoot.SetActive(false);
        }

        private void BuildDecoration(Transform parent)
        {
            var glowA = Image("Ambient Glow A", parent, UiSpriteFactory.Circle,
                new Color(_content.accent.r, _content.accent.g, _content.accent.b, 0.065f));
            Anchor(glowA.rectTransform, new Vector2(-0.15f, 0.68f), new Vector2(0.33f, 1.01f));
            glowA.raycastTarget = false;
            var glowB = Image("Ambient Glow B", parent, UiSpriteFactory.Circle,
                new Color(_content.secondary.r, _content.secondary.g, _content.secondary.b, 0.05f));
            Anchor(glowB.rectTransform, new Vector2(0.70f, -0.04f), new Vector2(1.12f, 0.24f));
            glowB.raycastTarget = false;

            for (var index = 0; index < 9; index++)
            {
                var node = Image($"Signal Node {index + 1}", parent, UiSpriteFactory.Circle,
                    new Color(1f, 1f, 1f, index % 2 == 0 ? 0.17f : 0.09f));
                var x = 0.08f + (index * 0.137f % 0.84f);
                var y = 0.08f + (index * 0.213f % 0.84f);
                node.rectTransform.anchorMin = node.rectTransform.anchorMax = new Vector2(x, y);
                node.rectTransform.sizeDelta = Vector2.one * (index % 3 == 0 ? 10f : 6f);
                node.raycastTarget = false;
            }
        }

        private void BuildLoading(Transform parent)
        {
            var safe = SafeArea(parent);
            Text(_content.title, safe, new Vector2(0.08f, 0.79f), new Vector2(0.92f, 0.88f),
                58, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 8f);

            var orbitContainer = Rect("Loading Orbit", safe);
            orbitContainer.anchorMin = orbitContainer.anchorMax = new Vector2(0.5f, 0.56f);
            orbitContainer.sizeDelta = new Vector2(340f, 340f);
            _orbit = Rect("Loading Orbit Spinner", orbitContainer);
            Stretch(_orbit);
            var outerRing = Image("Outer Ring", _orbit, UiSpriteFactory.Ring,
                new Color(_content.accent.r, _content.accent.g, _content.accent.b, 0.30f));
            Stretch(outerRing.rectTransform);
            var innerRing = Image("Inner Ring", orbitContainer, UiSpriteFactory.Ring,
                new Color(_content.secondary.r, _content.secondary.g, _content.secondary.b, 0.55f));
            innerRing.rectTransform.anchorMin = innerRing.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            innerRing.rectTransform.sizeDelta = new Vector2(218f, 218f);
            var satellite = Image("Orbit Signal", _orbit, UiSpriteFactory.Circle, _content.accent);
            satellite.rectTransform.anchorMin = satellite.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            satellite.rectTransform.anchoredPosition = new Vector2(0f, -12f);
            satellite.rectTransform.sizeDelta = new Vector2(28f, 28f);
            _loadingCore = Image("Daily Seed Core", orbitContainer, UiSpriteFactory.Circle, _content.secondary);
            _loadingCore.rectTransform.anchorMin = _loadingCore.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _loadingCore.rectTransform.sizeDelta = new Vector2(104f, 104f);

            _loadingStatus = Text(_content.loadingVerb, safe, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.39f),
                23, FontStyle.Bold, _content.accent, TextAnchor.MiddleCenter, 2f);
            Text(_challenge.stageDisplayName.ToUpperInvariant(), safe,
                new Vector2(0.08f, 0.295f), new Vector2(0.92f, 0.34f), 19, FontStyle.Normal,
                new Color(0.64f, 0.73f, 0.86f), TextAnchor.MiddleCenter, 2f);

            var track = Panel("Loading Track", safe, new Vector2(0.12f, 0.25f), new Vector2(0.88f, 0.264f),
                new Color(1f, 1f, 1f, 0.13f));
            _progressFill = Panel("Loading Progress", track, Vector2.zero, new Vector2(0.08f, 1f), _content.accent);
            Text("A NEW ISLAND EVERY DAY", safe, new Vector2(0.08f, 0.17f),
                new Vector2(0.92f, 0.22f), 18, FontStyle.Bold, new Color(0.64f, 0.72f, 0.84f),
                TextAnchor.MiddleCenter, 3f);
        }

        private void BuildMenu(Transform parent)
        {
            var safe = SafeArea(parent);
            var network = Panel("Island Day", safe, new Vector2(0.055f, 0.91f), new Vector2(0.945f, 0.963f),
                new Color(0.02f, 0.16f, 0.22f, 0.76f));
            Panel("Network Accent", network, new Vector2(0f, 0f), new Vector2(0.012f, 1f), _content.accent);
            Text(_content.networkLabel, network, new Vector2(0.04f, 0f), new Vector2(0.72f, 1f), 21,
                FontStyle.Bold, _content.accent, TextAnchor.MiddleLeft, 3f);
            _resetText = Text(string.Empty, network, new Vector2(0.66f, 0f), new Vector2(0.96f, 1f), 18,
                FontStyle.Bold, Color.white, TextAnchor.MiddleRight, 3f);

            Text(_content.title, safe, new Vector2(0.055f, 0.825f), new Vector2(0.945f, 0.91f), 58,
                FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, 8f);
            Text(_content.tagline, safe, new Vector2(0.055f, 0.765f), new Vector2(0.945f, 0.825f), 23,
                FontStyle.Normal, new Color(0.76f, 0.82f, 0.91f), TextAnchor.MiddleLeft, 2f);

            var hero = Panel("Today's Island", safe, new Vector2(0.055f, 0.485f), new Vector2(0.945f, 0.745f),
                new Color(0.018f, 0.11f, 0.15f, 0.86f));
            var heroOutline = hero.gameObject.AddComponent<Outline>();
            heroOutline.effectColor = new Color(_content.accent.r, _content.accent.g, _content.accent.b, 0.56f);
            heroOutline.effectDistance = new Vector2(2f, -2f);
            Text("TODAY  ·  " + _challenge.dateKey, hero, new Vector2(0.055f, 0.78f),
                new Vector2(0.94f, 0.94f), 18, FontStyle.Bold, _content.accent, TextAnchor.MiddleLeft, 3f);
            Text(_challenge.stageDisplayName.ToUpperInvariant(), hero, new Vector2(0.055f, 0.48f),
                new Vector2(0.94f, 0.78f), 45, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, 6f);
            Text(_content.objective, hero, new Vector2(0.055f, 0.23f), new Vector2(0.94f, 0.49f), 22,
                FontStyle.Normal, new Color(0.76f, 0.82f, 0.91f), TextAnchor.UpperLeft, 2f);
            var best = ReplayStore.LoadBest(_challenge.challengeId);
            var attempts = ReplayStore.GetAttemptCount(_challenge.challengeId);
            Text(_content.itemSummary, hero, new Vector2(0.055f, 0.055f), new Vector2(0.40f, 0.22f), 18,
                FontStyle.Bold, _content.secondary, TextAnchor.MiddleLeft, 2f);
            Text(attempts == 0 ? "FIRST RUN" : FormatBest(best), hero, new Vector2(0.52f, 0.055f),
                new Vector2(0.94f, 0.22f), 18, FontStyle.Bold, _content.accent, TextAnchor.MiddleRight, 2f);

            var controls = Panel("Simple Controls", safe, new Vector2(0.055f, 0.39f), new Vector2(0.945f, 0.465f),
                new Color(1f, 1f, 1f, 0.12f));
            Text(_content.controls, controls, new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), 20,
                FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 2f);

            _primaryButton = ActionButton("START CHALLENGE", _content.primaryAction, safe,
                new Vector2(0.055f, 0.255f), new Vector2(0.945f, 0.37f), _content.accent,
                () => StartCoroutine(EnterGame()));
            Text(_content.socialHook, safe, new Vector2(0.075f, 0.145f), new Vector2(0.925f, 0.225f), 19,
                FontStyle.Bold, new Color(0.72f, 0.79f, 0.88f), TextAnchor.MiddleCenter, 3f);
        }

        private void BuildInstructions(Transform parent)
        {
            _instructionsRoot = Rect("How To Play Sheet", parent).gameObject;
            Stretch((RectTransform)_instructionsRoot.transform);
            var blocker = _instructionsRoot.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.76f);
            var safe = SafeArea(_instructionsRoot.transform);
            var sheet = Panel("Instructions", safe, new Vector2(0.055f, 0.17f), new Vector2(0.945f, 0.83f),
                new Color(0.012f, 0.025f, 0.06f, 0.99f));
            var outline = sheet.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(_content.accent.r, _content.accent.g, _content.accent.b, 0.65f);
            outline.effectDistance = new Vector2(2f, -2f);
            Text("HOW TO PLAY", sheet, new Vector2(0.07f, 0.84f), new Vector2(0.93f, 0.95f), 40,
                FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, 5f);
            InstructionRow("01", "LEARN THE CONTROLS", _content.controls, sheet, 0.61f, 0.82f);
            InstructionRow("02", "COMPLETE TODAY'S WORLD", _content.objective, sheet, 0.39f, 0.60f);
            InstructionRow("03", "IMPROVE AND SHARE", _content.socialHook, sheet, 0.17f, 0.38f);
            ActionButton("CLOSE HOW TO PLAY", "GOT IT", sheet, new Vector2(0.07f, 0.045f),
                new Vector2(0.93f, 0.14f), _content.accent, HideInstructions);
            _instructionsRoot.SetActive(false);
        }

        private void InstructionRow(string number, string title, string body, Transform parent, float minY, float maxY)
        {
            var row = Panel("Step " + number, parent, new Vector2(0.07f, minY), new Vector2(0.93f, maxY),
                new Color(1f, 1f, 1f, 0.055f));
            Text(number, row, new Vector2(0.035f, 0.18f), new Vector2(0.16f, 0.82f), 32,
                FontStyle.Bold, _content.accent, TextAnchor.MiddleCenter);
            Text(title, row, new Vector2(0.18f, 0.51f), new Vector2(0.96f, 0.87f), 20,
                FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, 2f);
            Text(body, row, new Vector2(0.18f, 0.12f), new Vector2(0.96f, 0.55f), 17,
                FontStyle.Normal, new Color(0.69f, 0.77f, 0.87f), TextAnchor.UpperLeft, 1f);
        }

        private void FactChip(string label, Transform parent, float minX, float maxX, Color color)
        {
            var chip = Panel(label, parent, new Vector2(minX, 0f), new Vector2(maxX, 1f),
                new Color(color.r, color.g, color.b, 0.15f));
            Text(label, chip, new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), 16, FontStyle.Bold,
                color, TextAnchor.MiddleCenter, 3f);
        }

        private IEnumerator EnterGame()
        {
            if (_transitioning)
            {
                yield break;
            }
            _transitioning = true;
            if (_primaryButton != null)
            {
                _primaryButton.interactable = false;
                var label = _primaryButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = "ENTERING TODAY'S WORLD…";
                }
            }
            var elapsed = 0f;
            while (elapsed < 0.26f)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / 0.26f);
                yield return null;
            }
            PlayRequested?.Invoke();
            Destroy(_canvasRoot);
            Destroy(this);
        }

        private void ShowInstructions() => _instructionsRoot.SetActive(true);
        private void HideInstructions() => _instructionsRoot.SetActive(false);

        private void Update()
        {
            if (_progressFill != null && _shownProgress < _targetProgress)
            {
                _shownProgress = Mathf.MoveTowards(_shownProgress, _targetProgress, Time.unscaledDeltaTime * 0.72f);
                ApplyProgress();
            }
            if (_orbit != null && _loadingRoot != null && _loadingRoot.activeSelf)
            {
                _orbit.Rotate(0f, 0f, -32f * Time.unscaledDeltaTime);
                var pulse = 0.84f + Mathf.Sin(Time.unscaledTime * 4.4f) * 0.12f;
                _loadingCore.transform.localScale = Vector3.one * pulse;
            }
            if (_resetText != null && Time.frameCount % 30 == 0)
            {
                UpdateResetText();
            }
        }

        private void ApplyProgress()
        {
            if (_progressFill != null)
            {
                _progressFill.anchorMax = new Vector2(Mathf.Clamp01(_shownProgress), 1f);
            }
        }

        private void UpdateResetText()
        {
            var now = DateTime.UtcNow;
            var remaining = now.Date.AddDays(1) - now;
            _resetText.text = $"RESET {remaining.Hours:00}H {remaining.Minutes:00}M";
        }

        public static string FormatBest(ReplayRun run)
        {
            if (run?.result == null)
            {
                return "NO BEST YET";
            }
            if (!run.result.completed)
            {
                return $"BEST {run.result.collectedCount}/{run.result.totalCount}";
            }
            var span = TimeSpan.FromMilliseconds(run.result.timeMs);
            return $"BEST {(int)span.TotalMinutes:00}:{span.Seconds:00}.{span.Milliseconds:000}";
        }

        private RectTransform SafeArea(Transform parent)
        {
            var safe = Rect("Safe Area", parent);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            return safe;
        }

        private Button ActionButton(string objectName, string label, Transform parent, Vector2 min, Vector2 max,
            Color color, UnityEngine.Events.UnityAction action)
        {
            var rect = Panel(objectName, parent, min, max, new Color(color.r, color.g, color.b, 0.34f));
            var image = rect.GetComponent<Image>();
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(color.r, color.g, color.b, 0.88f);
            outline.effectDistance = new Vector2(2f, -2f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            Text(label, rect, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.94f), 27,
                FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 3f).raycastTarget = false;
            return button;
        }

        private RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var rect = Rect(name, parent);
            Anchor(rect, min, max);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiSpriteFactory.RoundedRectangle;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = color;
            return rect;
        }

        private Image Image(string name, Transform parent, Sprite sprite, Color color)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private Text Text(string value, Transform parent, Vector2 min, Vector2 max, int size, FontStyle style,
            Color color, TextAnchor alignment, float spacing = 0f)
        {
            var rect = Rect("Text", parent);
            Anchor(rect, min, max);
            var text = rect.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 11;
            text.resizeTextMaxSize = size;
            text.lineSpacing = 1f;
            if (spacing > 0f)
            {
                var shadow = rect.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.52f);
                shadow.effectDistance = new Vector2(spacing * 0.35f, -spacing * 0.35f);
            }
            return text;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var instance = new GameObject(name, typeof(RectTransform));
            var rect = instance.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect) => Anchor(rect, Vector2.zero, Vector2.one);

        private static Sprite CreateGradientSprite(Color top, Color bottom)
        {
            const int height = 256;
            var texture = new Texture2D(2, height, TextureFormat.RGBA32, false)
            {
                name = "Daily Start Programmatic Gradient",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            for (var y = 0; y < height; y++)
            {
                var color = Color.Lerp(bottom, top, y / (height - 1f));
                texture.SetPixel(0, y, color);
                texture.SetPixel(1, y, color);
            }
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, 2f, height), new Vector2(0.5f, 0.5f), 100f);
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
    }
}
