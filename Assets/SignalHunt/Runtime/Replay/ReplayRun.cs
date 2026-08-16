using System;
using System.Collections.Generic;
using SignalHunt.Gameplay;
using UnityEngine;

namespace SignalHunt.Replay
{
    [Serializable]
    public sealed class ReplayRun
    {
        public int schemaVersion = 1;
        public string challengeId;
        public string playerId;
        public string playerName;
        public string vehicleId = "hover-car-v1";
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
}
