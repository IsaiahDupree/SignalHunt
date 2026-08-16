using System;
using SignalHunt.Core;
using SignalHunt.Gameplay;
using UnityEngine;

namespace SignalHunt.Replay
{
    public sealed class RunRecorder : MonoBehaviour
    {
        private const float SampleInterval = 0.1f;

        private HuntSession _session;
        private HoverVehicleController _vehicle;
        private ReplayRun _run;
        private float _nextSample;
        private string _pendingCollectedItem;

        public ReplayRun CurrentRun => _run;
        public event Action<ReplayRun> RecordingCompleted;

        public void Initialize(HuntSession session, HoverVehicleController vehicle, string playerId, string playerName)
        {
            _session = session;
            _vehicle = vehicle;
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
                vehicleColorIndex = PlayerCosmetics.VehicleColorIndex
            };
            session.RelicCollected += OnRelicCollected;
            session.Completed += OnCompleted;
        }

        private void Update()
        {
            if (_session == null || _session.State != HuntState.Running || _session.Elapsed < _nextSample)
            {
                return;
            }

            CaptureFrame(string.Empty);
            _nextSample = _session.Elapsed + SampleInterval;
        }

        private void OnRelicCollected(string relicId)
        {
            _pendingCollectedItem = relicId;
            CaptureFrame("relic-pickup");
        }

        private void OnCompleted(HuntResult result)
        {
            CaptureFrame("finish");
            _run.result = result;
            RecordingCompleted?.Invoke(_run);
        }

        private void CaptureFrame(string cameraEvent)
        {
            if (_vehicle == null || _session == null)
            {
                return;
            }

            _run.frames.Add(new ReplayFrame
            {
                timestamp = _session.Elapsed,
                position = _vehicle.transform.position,
                rotation = _vehicle.transform.rotation,
                speed = _vehicle.Speed,
                vehicleState = _vehicle.Speed > 18f ? "boost" : _vehicle.Speed > 1f ? "moving" : "idle",
                collectedItem = _pendingCollectedItem,
                cameraEvent = cameraEvent
            });
            _pendingCollectedItem = string.Empty;
        }
    }
}
