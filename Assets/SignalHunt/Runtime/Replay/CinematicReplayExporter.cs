using System;
using System.Collections;
using SignalHunt.Gameplay;
using SignalHunt.Visual;
using UnityEngine;

namespace SignalHunt.Replay
{
    public sealed class CinematicReplayExporter : MonoBehaviour
    {
        private FollowCamera _camera;
        private Transform _gameplayTarget;
        private GamePalette _palette;
        private bool _playing;

        public event Action<string> StatusChanged;
        public event Action<bool> PresentationModeChanged;

        public void Initialize(FollowCamera followCamera, Transform gameplayTarget, GamePalette palette)
        {
            _camera = followCamera;
            _gameplayTarget = gameplayTarget;
            _palette = palette;
        }

        public void Export(ReplayRun run)
        {
            if (_playing || run?.frames == null || run.frames.Count < 2)
            {
                return;
            }

            StartCoroutine(PlaybackRoutine(run));
        }

        private IEnumerator PlaybackRoutine(ReplayRun run)
        {
            _playing = true;
            StatusChanged?.Invoke(NativeReplayKit.IsAvailable ? "Preparing vertical ReplayKit export…" : "Previewing replay — export requires an iOS device build");
            PresentationModeChanged?.Invoke(true);
            Screen.orientation = ScreenOrientation.Portrait;
            NativeReplayKit.Start();
            yield return new WaitForSeconds(0.8f);

            var replayVehicle = HoverVehicleFactory.CreateReplayVisual(_palette, "Cinematic Replay Vehicle", false);
            _camera.SetTarget(replayVehicle.transform, true);
            var startTime = Time.time;
            var lastTimestamp = run.frames[^1].timestamp;
            var frameIndex = 0;
            var activeShot = -1;
            while (Time.time - startTime <= lastTimestamp)
            {
                var elapsed = Time.time - startTime;
                var shotIndex = Mathf.FloorToInt(elapsed / 3.5f) % 4;
                if (shotIndex != activeShot)
                {
                    activeShot = shotIndex;
                    _camera.SetTarget(replayVehicle.transform, true, (ReplayCameraShot)shotIndex);
                }
                while (frameIndex < run.frames.Count - 2 && run.frames[frameIndex + 1].timestamp <= elapsed)
                {
                    frameIndex++;
                }

                var from = run.frames[frameIndex];
                var to = run.frames[Mathf.Min(frameIndex + 1, run.frames.Count - 1)];
                var progress = Mathf.InverseLerp(from.timestamp, Mathf.Max(from.timestamp + 0.001f, to.timestamp), elapsed);
                replayVehicle.transform.position = Vector3.Lerp(from.position, to.position, progress);
                replayVehicle.transform.rotation = Quaternion.Slerp(from.rotation, to.rotation, progress);
                yield return null;
            }

            NativeReplayKit.StopAndPresent();
            _camera.SetTarget(_gameplayTarget);
            Destroy(replayVehicle);
            StatusChanged?.Invoke(NativeReplayKit.IsAvailable ? "Replay ready to save or share" : "Replay preview complete");
            PresentationModeChanged?.Invoke(false);
            _playing = false;
        }
    }
}
