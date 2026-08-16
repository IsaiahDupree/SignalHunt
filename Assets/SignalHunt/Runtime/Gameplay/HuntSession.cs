using System;
using System.Collections;
using System.Collections.Generic;
using SignalHunt.Core;
using UnityEngine;

namespace SignalHunt.Gameplay
{
    public enum HuntState
    {
        Countdown,
        Running,
        Complete
    }

    [Serializable]
    public sealed class HuntResult
    {
        public string challengeId;
        public int timeMs;
        public int score;
        public int collectedCount;
        public int totalCount;
        public bool completed;
        public string[] collectedRelicIds;
    }

    public sealed class HuntSession : MonoBehaviour
    {
        private readonly HashSet<string> _collected = new();
        private DailyChallenge _challenge;
        private HoverVehicleController _vehicle;
        private int _totalRelics;
        private float _elapsed;

        public HuntState State { get; private set; } = HuntState.Countdown;
        public float Elapsed => _elapsed;
        public int CollectedCount => _collected.Count;
        public DailyChallenge Challenge => _challenge;

        public event Action<string> StatusChanged;
        public event Action<float> TimeChanged;
        public event Action<int, int> RelicCountChanged;
        public event Action<string> RelicCollected;
        public event Action<HuntResult> Completed;

        public void Initialize(DailyChallenge challenge, HoverVehicleController vehicle, int totalRelics)
        {
            _challenge = challenge;
            _vehicle = vehicle;
            _totalRelics = totalRelics;
            _vehicle.SetInputEnabled(false);
        }

        public void Begin()
        {
            StartCoroutine(CountdownRoutine());
        }

        public void Collect(string relicId)
        {
            if (State != HuntState.Running || !_collected.Add(relicId))
            {
                return;
            }

            RelicCollected?.Invoke(relicId);
            RelicCountChanged?.Invoke(_collected.Count, _totalRelics);
            if (_collected.Count >= _totalRelics)
            {
                Finish(true);
            }
        }

        private IEnumerator CountdownRoutine()
        {
            State = HuntState.Countdown;
            for (var count = 3; count > 0; count--)
            {
                StatusChanged?.Invoke(count.ToString());
                yield return new WaitForSeconds(1f);
            }

            StatusChanged?.Invoke("HUNT!");
            State = HuntState.Running;
            _vehicle.SetInputEnabled(true);
            yield return new WaitForSeconds(0.65f);
            StatusChanged?.Invoke(string.Empty);
        }

        private void Update()
        {
            if (State != HuntState.Running)
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

        private void Finish(bool completed)
        {
            if (State == HuntState.Complete)
            {
                return;
            }

            State = HuntState.Complete;
            _vehicle.SetInputEnabled(false);
            var result = new HuntResult
            {
                challengeId = _challenge.challengeId,
                timeMs = Mathf.RoundToInt(_elapsed * 1000f),
                score = HuntScore.Calculate(_collected.Count, _totalRelics, _elapsed, completed),
                collectedCount = _collected.Count,
                totalCount = _totalRelics,
                completed = completed,
                collectedRelicIds = new List<string>(_collected).ToArray()
            };
            StatusChanged?.Invoke(completed ? "SIGNAL LOCKED" : "TIME EXPIRED");
            Completed?.Invoke(result);
        }
    }

    public static class HuntScore
    {
        public static int Calculate(int collected, int total, float seconds, bool completed)
        {
            var collectionScore = collected * 1000;
            var completionBonus = completed ? 5000 : 0;
            var timeBonus = completed ? Mathf.Max(0, 300000 - Mathf.RoundToInt(seconds * 1000f)) / 100 : 0;
            var perfectBonus = completed && collected == total ? 2000 : 0;
            return collectionScore + completionBonus + timeBonus + perfectBonus;
        }
    }
}
