using System;
using System.Collections;
using System.Collections.Generic;
using SignalHunt.Core;
using SignalHunt.Gameplay;
using SignalHunt.UI;
using SignalHunt.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace SignalHunt.Replay
{
    public sealed class DailyMontageExporter : MonoBehaviour
    {
        public delegate GameObject ReplayVisualFactory(ReplayRun run, int racerIndex);

        private const int MaxRacers = 16;
        private const float ShotDuration = 3f;

        private FollowCamera _camera;
        private Transform _gameplayTarget;
        private GamePalette _palette;
        private ReplayVisualFactory _visualFactory;
        private bool _playing;

        public bool IsPlaying => _playing;
        public event Action<string> StatusChanged;
        public event Action<bool> PresentationModeChanged;

        public void Initialize(FollowCamera followCamera, Transform gameplayTarget, GamePalette palette,
            ReplayVisualFactory visualFactory = null)
        {
            _camera = followCamera;
            _gameplayTarget = gameplayTarget;
            _palette = palette;
            _visualFactory = visualFactory;
        }

        public void Export(IReadOnlyList<ReplayRun> runs)
        {
            if (_playing)
            {
                return;
            }

            var playable = new List<ReplayRun>();
            if (runs != null)
            {
                for (var index = 0; index < runs.Count && playable.Count < MaxRacers; index++)
                {
                    if (runs[index]?.frames != null && runs[index].frames.Count > 1 && runs[index].frames[^1].timestamp > 0f)
                    {
                        playable.Add(runs[index]);
                    }
                }
            }

            if (playable.Count == 0)
            {
                StatusChanged?.Invoke("Finish at least one run to create today's film");
                return;
            }
            StartCoroutine(PlaybackRoutine(playable));
        }

        private IEnumerator PlaybackRoutine(IReadOnlyList<ReplayRun> runs)
        {
            _playing = true;
            PresentationModeChanged?.Invoke(true);
            StatusChanged?.Invoke(NativeReplayKit.IsAvailable
                ? $"Recording today's {runs.Count}-racer film…"
                : $"Previewing today's {runs.Count}-racer film — iOS enables sharing");
            Screen.orientation = ScreenOrientation.Portrait;
            NativeReplayKit.Start();
            yield return new WaitForSeconds(0.55f);

            var racers = new List<MontageRacer>(runs.Count);
            var longestRun = 0f;
            for (var index = 0; index < runs.Count; index++)
            {
                var run = runs[index];
                longestRun = Mathf.Max(longestRun, run.frames[^1].timestamp);
                var visual = _visualFactory?.Invoke(run, index) ?? CreateDefaultVisual(run, index);
                racers.Add(new MontageRacer(run, visual));
            }

            var overlay = FilmOverlay.Create(runs[0], runs.Count);

            if (_gameplayTarget != null)
            {
                _gameplayTarget.gameObject.SetActive(false);
            }

            var filmDuration = Mathf.Clamp(longestRun / 6f, 12f, 24f);
            var startTime = Time.time;
            var activeSegment = -1;
            while (Time.time - startTime <= filmDuration)
            {
                var elapsed = Time.time - startTime;
                var sourceTime = Mathf.Clamp01(elapsed / filmDuration) * longestRun;
                foreach (var racer in racers)
                {
                    racer.Sample(sourceTime);
                }

                var segment = Mathf.FloorToInt(elapsed / ShotDuration);
                if (segment != activeSegment)
                {
                    activeSegment = segment;
                    var focus = FindActiveRacer(racers, segment % racers.Count);
                    if (focus != null)
                    {
                        var shot = (ReplayCameraShot)(segment % 4);
                        _camera.SetTarget(focus.Visual.transform, true, shot);
                        overlay.SetShot(shot, focus.Run.playerName);
                    }
                }
                yield return null;
            }

            NativeReplayKit.StopAndPresent();
            foreach (var racer in racers)
            {
                Destroy(racer.Visual);
            }
            Destroy(overlay.Root);
            if (_gameplayTarget != null)
            {
                _gameplayTarget.gameObject.SetActive(true);
                _camera.SetTarget(_gameplayTarget);
            }
            PresentationModeChanged?.Invoke(false);
            StatusChanged?.Invoke(NativeReplayKit.IsAvailable
                ? "Daily film ready to save or share"
                : "Daily film preview complete");
            _playing = false;
        }

        private static MontageRacer FindActiveRacer(IReadOnlyList<MontageRacer> racers, int preferred)
        {
            for (var offset = 0; offset < racers.Count; offset++)
            {
                var racer = racers[(preferred + offset) % racers.Count];
                if (racer.Visual.activeSelf)
                {
                    return racer;
                }
            }
            return racers.Count > 0 ? racers[0] : null;
        }

        private GameObject CreateDefaultVisual(ReplayRun run, int index)
        {
            var visual = HoverVehicleFactory.CreateReplayVisual(_palette,
                $"Daily Film Racer {index + 1}", false, Mathf.Abs(run.vehicleColorIndex) % 4);
            var racerName = string.IsNullOrWhiteSpace(run.playerName) ? $"RACER {index + 1}" : run.playerName;
            if (run.attemptNumber > 0)
            {
                racerName += $" · TRY {run.attemptNumber}";
            }
            WorldNameplate.Create(visual.transform, racerName);
            return visual;
        }

        private sealed class MontageRacer
        {
            private readonly ReplayRun _run;
            private int _frameIndex;

            public GameObject Visual { get; }
            public ReplayRun Run => _run;

            public MontageRacer(ReplayRun run, GameObject visual)
            {
                _run = run;
                Visual = visual;
            }

            public void Sample(float timestamp)
            {
                if (timestamp > _run.frames[^1].timestamp)
                {
                    Visual.SetActive(false);
                    return;
                }
                Visual.SetActive(true);
                while (_frameIndex < _run.frames.Count - 2 && _run.frames[_frameIndex + 1].timestamp <= timestamp)
                {
                    _frameIndex++;
                }

                var from = _run.frames[_frameIndex];
                var to = _run.frames[Mathf.Min(_frameIndex + 1, _run.frames.Count - 1)];
                var progress = Mathf.InverseLerp(from.timestamp, Mathf.Max(from.timestamp + 0.001f, to.timestamp), timestamp);
                Visual.transform.position = Vector3.Lerp(from.position, to.position, progress);
                Visual.transform.rotation = Quaternion.Slerp(from.rotation, to.rotation, progress);
            }
        }

        private sealed class FilmOverlay
        {
            private static readonly Color Cyan = new(0.08f, 0.86f, 1f, 1f);
            private readonly Text _shotText;

            public GameObject Root { get; }

            private FilmOverlay(GameObject root, Text shotText)
            {
                Root = root;
                _shotText = shotText;
            }

            public static FilmOverlay Create(ReplayRun run, int racerCount)
            {
                var root = new GameObject("Daily Film Overlay");
                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 250;
                var scaler = root.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 0.5f;

                var header = Rect("Film Header", root.transform, new Vector2(0.045f, 0.885f), new Vector2(0.955f, 0.972f));
                var image = header.gameObject.AddComponent<Image>();
                image.color = new Color(0.012f, 0.024f, 0.058f, 0.88f);
                var appName = AppDisplayName(run.appKey).ToUpperInvariant();
                TextLabel($"{appName} · DAILY FILM", header, new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.92f),
                    38, FontStyle.Bold, Cyan, TextAnchor.MiddleLeft);
                var date = !string.IsNullOrEmpty(run.challengeId) && run.challengeId.Length >= 10
                    ? run.challengeId.Substring(0, 10)
                    : "SHARED DAILY SEED";
                TextLabel($"{racerCount} RACER{(racerCount == 1 ? string.Empty : "S")} · ONE MAP · {date}", header,
                    new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.48f), 22, FontStyle.Bold, Color.white,
                    TextAnchor.MiddleLeft);

                var shotPanel = Rect("Film Shot", root.transform, new Vector2(0.045f, 0.035f), new Vector2(0.72f, 0.085f));
                shotPanel.gameObject.AddComponent<Image>().color = new Color(0.012f, 0.024f, 0.058f, 0.82f);
                var shotText = TextLabel("ORBIT · RACER", shotPanel, new Vector2(0.06f, 0f), new Vector2(0.94f, 1f),
                    24, FontStyle.Bold, Cyan, TextAnchor.MiddleLeft);
                return new FilmOverlay(root, shotText);
            }

            public void SetShot(ReplayCameraShot shot, string playerName)
            {
                var name = string.IsNullOrWhiteSpace(playerName) ? "RACER" : playerName.ToUpperInvariant();
                _shotText.text = $"{shot.ToString().ToUpperInvariant()} · {name}";
            }

            private static string AppDisplayName(string appKey)
            {
                foreach (var profile in DailyGameCatalog.All)
                {
                    if (string.Equals(profile.appKey, appKey, StringComparison.Ordinal))
                    {
                        return profile.displayName;
                    }
                }
                return "Signal Hunt";
            }

            private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
            {
                var instance = new GameObject(name, typeof(RectTransform));
                var rect = instance.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                rect.anchorMin = min;
                rect.anchorMax = max;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                return rect;
            }

            private static Text TextLabel(string value, Transform parent, Vector2 min, Vector2 max, int size,
                FontStyle style, Color color, TextAnchor alignment)
            {
                var rect = Rect("Text", parent, min, max);
                var text = rect.gameObject.AddComponent<Text>();
                text.text = value;
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = size;
                text.fontStyle = style;
                text.color = color;
                text.alignment = alignment;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 12;
                text.resizeTextMaxSize = size;
                return text;
            }
        }
    }
}
