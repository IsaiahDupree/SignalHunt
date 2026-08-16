using System;
using System.IO;
using SignalHunt.Core;
using UnityEngine;

namespace SignalHunt.Replay
{
    public static class ReplayStore
    {
        public static bool SaveIfBest(ReplayRun run)
        {
            if (run?.result == null || string.IsNullOrWhiteSpace(run.challengeId))
            {
                throw new ArgumentException("A completed replay is required.", nameof(run));
            }

            var existing = LoadBest(run.challengeId);
            var isBetter = existing?.result == null ||
                           run.result.completed && !existing.result.completed ||
                           run.result.completed == existing.result.completed && run.result.score > existing.result.score ||
                           run.result.completed && existing.result.completed && run.result.timeMs < existing.result.timeMs;
            SaveJson(LatestPath(run.challengeId), run);
            SaveShareMetadata(run);
            if (isBetter)
            {
                SaveJson(BestPath(run.challengeId), run);
            }

            return isBetter;
        }

        public static ReplayRun LoadBest(string challengeId)
        {
            return Load(BestPath(challengeId));
        }

        public static ReplayRun LoadLatest(string challengeId)
        {
            return Load(LatestPath(challengeId));
        }

        public static string ShareMetadataPath(string challengeId)
        {
            return Path.Combine(RootDirectory(), $"{SafeId(challengeId)}-share.json");
        }

        private static ReplayRun Load(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<ReplayRun>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not load replay at {path}: {exception.Message}");
                return null;
            }
        }

        private static void SaveJson(string path, ReplayRun run)
        {
            Directory.CreateDirectory(RootDirectory());
            File.WriteAllText(path, JsonUtility.ToJson(run, false));
        }

        private static void SaveShareMetadata(ReplayRun run)
        {
            var metadata = new ShareRunMetadata
            {
                challengeId = run.challengeId,
                playerName = run.playerName,
                found = run.result.collectedCount,
                total = run.result.totalCount,
                timeMs = run.result.timeMs,
                score = run.result.score,
                caption = $"Today's Signal Hunt: {run.playerName} found {run.result.collectedCount}/{run.result.totalCount} relics in {TimeSpan.FromMilliseconds(run.result.timeMs):m\\:ss\\.fff}."
            };
            Directory.CreateDirectory(RootDirectory());
            File.WriteAllText(ShareMetadataPath(run.challengeId), JsonUtility.ToJson(metadata, true));
        }

        private static string BestPath(string challengeId) => Path.Combine(RootDirectory(), $"{SafeId(challengeId)}-best.json");
        private static string LatestPath(string challengeId) => Path.Combine(RootDirectory(), $"{SafeId(challengeId)}-latest.json");
        private static string RootDirectory() => Path.Combine(Application.persistentDataPath, "signal-hunt", "replays");
        private static string SafeId(string challengeId) => DailyChallenge.StableHash(challengeId).ToString("x8");
    }
}
