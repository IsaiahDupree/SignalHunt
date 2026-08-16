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
                    networkLabel = "THE DAILY WORLD HUNT",
                    tagline = "One hidden world. One trail only you will make.",
                    objective = "Explore freely, pulse your detector, and recover every lost artifact before the clock wins.",
                    controls = "RUN to move · LEFT / RIGHT to steer · SCAN to reveal nearby treasure",
                    itemSummary = $"{challenge.collectibleCount} LOST ARTIFACTS",
                    primaryAction = "START TREASURE HUNT",
                    socialHook = "Every search trail can join today's cinematic community film.",
                    loadingVerb = "BURYING TODAY'S ARTIFACTS",
                    accent = new Color(1f, 0.70f, 0.10f),
                    secondary = new Color(0.04f, 0.90f, 1f),
                    backgroundTop = new Color(0.015f, 0.07f, 0.13f),
                    backgroundBottom = new Color(0.005f, 0.015f, 0.04f)
                },
                DailyGameCatalog.WaypointRallyAppKey => new DailyStartMenuContent
                {
                    title = "WAYPOINT RALLY",
                    networkLabel = "THE DAILY ROUTE RACE",
                    tagline = "Same streets. Different instincts. Fastest route wins.",
                    objective = "Drive every waypoint in order, hunt for shortcuts, and replay the route until it is yours.",
                    controls = "GO to accelerate · LEFT / RIGHT to steer · BRAKE to carve tight turns",
                    itemSummary = $"{challenge.collectibleCount} ROUTE WAYPOINTS",
                    primaryAction = "START DAILY RALLY",
                    socialHook = "Ghost routes expose the shortcuts that move the daily leaderboard.",
                    loadingVerb = "DRAWING TODAY'S ROUTE",
                    accent = new Color(1f, 0.38f, 0.08f),
                    secondary = new Color(0.05f, 0.86f, 1f),
                    backgroundTop = new Color(0.15f, 0.045f, 0.025f),
                    backgroundBottom = new Color(0.025f, 0.012f, 0.035f)
                },
                DailyGameCatalog.WaypointWingsAppKey => new DailyStartMenuContent
                {
                    title = "WAYPOINT WINGS",
                    networkLabel = "THE DAILY FLIGHT PATH",
                    tagline = "Own the sky before the rest of the world finds the line.",
                    objective = "Thread every air gate, skim the terrain, and refine the fastest path through today's sky.",
                    controls = "BOOST for speed · LEFT / RIGHT to bank · UP / DOWN to pitch",
                    itemSummary = $"{challenge.collectibleCount} AIR GATES",
                    primaryAction = "TAKE FLIGHT",
                    socialHook = "Closest calls and fastest paths become today's shared flight film.",
                    loadingVerb = "OPENING TODAY'S SKYWAY",
                    accent = new Color(0.12f, 0.78f, 1f),
                    secondary = new Color(1f, 0.18f, 0.72f),
                    backgroundTop = new Color(0.025f, 0.09f, 0.19f),
                    backgroundBottom = new Color(0.008f, 0.012f, 0.055f)
                },
                _ => new DailyStartMenuContent
                {
                    title = "SIGNAL HUNT",
                    networkLabel = "THE DAILY SIGNAL RACE",
                    tagline = "Everybody gets the same world. Nobody takes the same path.",
                    objective = "Race across today's generated world and lock every hidden signal as fast as possible.",
                    controls = "GO to accelerate · LEFT / RIGHT to steer · BRAKE to turn sharply",
                    itemSummary = $"{challenge.collectibleCount} HIDDEN SIGNALS",
                    primaryAction = "START TODAY'S HUNT",
                    socialHook = "Your route, near misses, and best finish can join today's Daily Film.",
                    loadingVerb = "GENERATING TODAY'S SIGNAL GRID",
                    accent = new Color(0.06f, 0.88f, 1f),
                    secondary = new Color(1f, 0.12f, 0.67f),
                    backgroundTop = new Color(0.025f, 0.065f, 0.16f),
                    backgroundBottom = new Color(0.008f, 0.008f, 0.045f)
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
            _backgroundImage.color = new Color(1f, 1f, 1f, 0.64f);
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
            Text("DAILY CHALLENGE NETWORK", safe, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.91f),
                22, FontStyle.Bold, new Color(0.75f, 0.84f, 0.95f), TextAnchor.MiddleCenter, 3f);
            Text(_content.title, safe, new Vector2(0.08f, 0.77f), new Vector2(0.92f, 0.86f),
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
            Text(_challenge.displaySeed.ToString("D5"), orbitContainer, new Vector2(0.35f, 0.43f), new Vector2(0.65f, 0.57f),
                26, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

            _loadingStatus = Text(_content.loadingVerb, safe, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.39f),
                23, FontStyle.Bold, _content.accent, TextAnchor.MiddleCenter, 2f);
            Text($"{_challenge.stageDisplayName.ToUpperInvariant()}  //  {_challenge.dateKey}", safe,
                new Vector2(0.08f, 0.295f), new Vector2(0.92f, 0.34f), 19, FontStyle.Normal,
                new Color(0.64f, 0.73f, 0.86f), TextAnchor.MiddleCenter, 2f);

            var track = Panel("Loading Track", safe, new Vector2(0.12f, 0.25f), new Vector2(0.88f, 0.264f),
                new Color(1f, 1f, 1f, 0.13f));
            _progressFill = Panel("Loading Progress", track, Vector2.zero, new Vector2(0.08f, 1f), _content.accent);
            Text("SAME SEED  ·  SAME WORLD  ·  NEW PATHS", safe, new Vector2(0.08f, 0.17f),
                new Vector2(0.92f, 0.22f), 18, FontStyle.Bold, new Color(0.64f, 0.72f, 0.84f),
                TextAnchor.MiddleCenter, 3f);
        }

        private void BuildMenu(Transform parent)
        {
            var safe = SafeArea(parent);
            var network = Panel("Network Label", safe, new Vector2(0.055f, 0.91f), new Vector2(0.945f, 0.963f),
                new Color(0.02f, 0.04f, 0.09f, 0.72f));
            Panel("Network Accent", network, new Vector2(0f, 0f), new Vector2(0.012f, 1f), _content.accent);
            Text(_content.networkLabel, network, new Vector2(0.04f, 0f), new Vector2(0.72f, 1f), 21,
                FontStyle.Bold, _content.accent, TextAnchor.MiddleLeft, 3f);
            _resetText = Text(string.Empty, network, new Vector2(0.66f, 0f), new Vector2(0.96f, 1f), 18,
                FontStyle.Bold, Color.white, TextAnchor.MiddleRight, 3f);

            Text(_content.title, safe, new Vector2(0.055f, 0.835f), new Vector2(0.945f, 0.91f), 55,
                FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, 8f);
            Text(_content.tagline, safe, new Vector2(0.055f, 0.785f), new Vector2(0.945f, 0.84f), 22,
                FontStyle.Normal, new Color(0.76f, 0.82f, 0.91f), TextAnchor.MiddleLeft, 2f);

            var hero = Panel("Today's World", safe, new Vector2(0.055f, 0.535f), new Vector2(0.945f, 0.775f),
                new Color(0.018f, 0.035f, 0.075f, 0.91f));
            var heroOutline = hero.gameObject.AddComponent<Outline>();
            heroOutline.effectColor = new Color(_content.accent.r, _content.accent.g, _content.accent.b, 0.56f);
            heroOutline.effectDistance = new Vector2(2f, -2f);
            Text("TODAY // " + _challenge.dateKey.Replace("-", "."), hero, new Vector2(0.055f, 0.76f),
                new Vector2(0.67f, 0.94f), 18, FontStyle.Bold, _content.accent, TextAnchor.MiddleLeft, 3f);
            Text(_challenge.stageDisplayName.ToUpperInvariant(), hero, new Vector2(0.055f, 0.43f),
                new Vector2(0.94f, 0.76f), 43, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, 6f);
            Text(_content.objective, hero, new Vector2(0.055f, 0.15f), new Vector2(0.94f, 0.43f), 21,
                FontStyle.Normal, new Color(0.76f, 0.82f, 0.91f), TextAnchor.UpperLeft, 2f);
            var seedPill = Panel("Seed Pill", hero, new Vector2(0.70f, 0.76f), new Vector2(0.94f, 0.92f),
                new Color(_content.secondary.r, _content.secondary.g, _content.secondary.b, 0.18f));
            Text("SEED " + _challenge.displaySeed.ToString("D5"), seedPill, new Vector2(0.05f, 0f),
                new Vector2(0.95f, 1f), 18, FontStyle.Bold, _content.secondary, TextAnchor.MiddleCenter);

            var chips = Rect("Challenge Facts", safe);
            Anchor(chips, new Vector2(0.055f, 0.475f), new Vector2(0.945f, 0.525f));
            FactChip(_content.itemSummary, chips, 0f, 0.49f, _content.accent);
            FactChip("ONE SHARED WORLD", chips, 0.51f, 1f, _content.secondary);

            var player = Panel("Player Card", safe, new Vector2(0.055f, 0.385f), new Vector2(0.945f, 0.465f),
                new Color(0.018f, 0.035f, 0.075f, 0.82f));
            Text("PLAYER", player, new Vector2(0.04f, 0.50f), new Vector2(0.30f, 0.88f), 15,
                FontStyle.Bold, new Color(0.58f, 0.67f, 0.79f), TextAnchor.MiddleLeft);
            Text(PlayerIdentity.DisplayName.ToUpperInvariant(), player, new Vector2(0.04f, 0.08f),
                new Vector2(0.47f, 0.55f), 24, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, 2f);
            var best = ReplayStore.LoadBest(_challenge.challengeId);
            var attempts = ReplayStore.GetAttemptCount(_challenge.challengeId);
            Text(attempts == 0 ? "FIRST RUN" : $"{attempts} ATTEMPT{(attempts == 1 ? string.Empty : "S")}", player,
                new Vector2(0.47f, 0.51f), new Vector2(0.72f, 0.88f), 15, FontStyle.Bold,
                new Color(0.58f, 0.67f, 0.79f), TextAnchor.MiddleRight);
            Text(FormatBest(best), player, new Vector2(0.40f, 0.08f), new Vector2(0.72f, 0.55f), 21,
                FontStyle.Bold, _content.accent, TextAnchor.MiddleRight, 2f);
            var styleButton = ActionButton("PLAYER COLOR", "COLOR", player, new Vector2(0.75f, 0.14f),
                new Vector2(0.96f, 0.86f), _content.secondary, () => StyleRequested?.Invoke());
            _styleText = styleButton.GetComponentInChildren<Text>();
            NotifyStyleChanged(SignalHunt.Gameplay.PlayerCosmetics.VehicleColorIndex);

            _primaryButton = ActionButton("START CHALLENGE", _content.primaryAction, safe,
                new Vector2(0.055f, 0.275f), new Vector2(0.945f, 0.37f), _content.accent,
                () => StartCoroutine(EnterGame()));
            ActionButton("HOW TO PLAY", "HOW TO PLAY", safe, new Vector2(0.055f, 0.195f),
                new Vector2(0.49f, 0.26f), _content.secondary, ShowInstructions);
            var filmButton = ActionButton("DAILY FILM PREVIEW", attempts > 0 ? "WATCH DAILY FILM" : "FILM AFTER FIRST RUN",
                safe, new Vector2(0.51f, 0.195f), new Vector2(0.945f, 0.26f), _content.secondary,
                () => FilmRequested?.Invoke());
            filmButton.interactable = attempts > 0;
            filmButton.targetGraphic.color = new Color(filmButton.targetGraphic.color.r, filmButton.targetGraphic.color.g,
                filmButton.targetGraphic.color.b, attempts > 0 ? 0.36f : 0.14f);

            Text(_content.socialHook, safe, new Vector2(0.075f, 0.105f), new Vector2(0.925f, 0.17f), 19,
                FontStyle.Bold, new Color(0.72f, 0.79f, 0.88f), TextAnchor.MiddleCenter, 3f);
            Text("PLAY  ·  IMPROVE  ·  REPLAY  ·  SHARE", safe, new Vector2(0.075f, 0.055f),
                new Vector2(0.925f, 0.10f), 17, FontStyle.Bold, _content.accent, TextAnchor.MiddleCenter, 4f);

            BuildInstructions(parent);
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
