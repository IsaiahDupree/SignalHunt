using System;
using System.Collections.Generic;
using SignalHunt.Core;
using SignalHunt.Gameplay;
using UnityEngine;

namespace SignalHunt.Replay
{
    [Serializable]
    public sealed class ReplayRun
    {
        public int schemaVersion = 2;
        public string appKey = DailyGameCatalog.SignalHuntAppKey;
        public string modeKey = "daily-hunt";
        public string challengeId;
        public string worldTemplate;
        public uint generationSeed;
        public string clientRunId;
        public string recordedAtUtc;
        public int attemptNumber;
        public string playerId;
        public string playerName;
        public string vehicleId = "hover-car-v1";
        public int vehicleColorIndex;
        public HuntResult result;
        public List<ReplayFrame> frames = new();
    }

    [Serializable]
    public struct ReplayFrame
    {
        public float timestamp;
        public Vector3 position;
        public Quaternion rotation;
        public float speed;
        public string vehicleState;
        public string collectedItem;
        public string cameraEvent;
    }

    [Serializable]
    public sealed class ShareRunMetadata
    {
        public string challengeId;
        public string playerName;
        public int found;
        public int total;
        public int timeMs;
        public int score;
        public string caption;
    }

    [Serializable]
    public sealed class ReplayAttemptSummary
    {
        public string clientRunId;
        public string recordedAtUtc;
        public int attemptNumber;
        public int timeMs;
        public int score;
        public int collectedCount;
        public bool completed;
    }

    [Serializable]
    public sealed class ReplayHistoryManifest
    {
        public string challengeId;
        public List<ReplayAttemptSummary> attempts = new();
    }

    [Serializable]
    public sealed class RunSaveOutcome
    {
        public bool isPersonalBest;
        public int attemptNumber;
        public int previousBestTimeMs = -1;
        public int bestTimeMs = -1;
        public int improvementMs;
    }
}
