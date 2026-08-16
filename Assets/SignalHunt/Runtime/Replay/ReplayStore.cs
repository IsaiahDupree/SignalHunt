using System;
using System.Collections.Generic;
using System.IO;
using SignalHunt.Core;
using UnityEngine;

namespace SignalHunt.Replay
{
    public static class ReplayStore
    {
        public static RunSaveOutcome SaveAttempt(ReplayRun run)
        {
            Validate(run);
            if (string.IsNullOrWhiteSpace(run.clientRunId))
            {
                run.clientRunId = Guid.NewGuid().ToString("N");
            }
            if (string.IsNullOrWhiteSpace(run.recordedAtUtc))
            {
                run.recordedAtUtc = DateTime.UtcNow.ToString("O");
            }

            var previousBest = LoadBest(run.challengeId);
            var history = LoadManifest(run.challengeId);
            run.attemptNumber = history.attempts.Count + 1;
            var isBetter = IsBetter(run, previousBest);

            SaveJson(AttemptPath(run.challengeId, run.clientRunId), run);
            SaveJson(LatestPath(run.challengeId), run);
            SaveShareMetadata(run);
            history.attempts.Add(new ReplayAttemptSummary
            {
                clientRunId = run.clientRunId,
                recordedAtUtc = run.recordedAtUtc,
                attemptNumber = run.attemptNumber,
                timeMs = run.result.timeMs,
                score = run.result.score,
                collectedCount = run.result.collectedCount,
                completed = run.result.completed
            });
            SaveManifest(history);
            if (isBetter)
            {
                SaveJson(BestPath(run.challengeId), run);
            }

            var best = isBetter ? run : previousBest;
            var previousTime = previousBest?.result != null && previousBest.result.completed
                ? previousBest.result.timeMs
                : -1;
            return new RunSaveOutcome
            {
                isPersonalBest = isBetter,
                attemptNumber = run.attemptNumber,
                previousBestTimeMs = previousTime,
                bestTimeMs = best?.result != null && best.result.completed ? best.result.timeMs : -1,
                improvementMs = isBetter && previousTime >= 0 && run.result.completed
                    ? Mathf.Max(0, previousTime - run.result.timeMs)
                    : 0
            };
        }

        public static bool SaveIfBest(ReplayRun run)
        {
            return SaveAttempt(run).isPersonalBest;
        }

        public static ReplayRun LoadBest(string challengeId)
        {
            return Load(BestPath(challengeId));
        }

        public static ReplayRun LoadLatest(string challengeId)
        {
            return Load(LatestPath(challengeId));
        }

        public static ReplayRun[] LoadHistory(string challengeId, int limit = 16)
        {
            if (limit <= 0)
            {
                return Array.Empty<ReplayRun>();
            }

            var history = LoadManifest(challengeId);
            var runs = new List<ReplayRun>();
            for (var index = history.attempts.Count - 1; index >= 0 && runs.Count < limit; index--)
            {
                var summary = history.attempts[index];
                var run = Load(AttemptPath(challengeId, summary.clientRunId));
                if (run?.frames != null && run.frames.Count > 1)
                {
                    runs.Add(run);
                }
            }

            if (runs.Count == 0)
            {
                var latest = LoadLatest(challengeId);
                if (latest?.frames != null && latest.frames.Count > 1)
                {
                    runs.Add(latest);
                }
            }
            return runs.ToArray();
        }

        public static int GetAttemptCount(string challengeId)
        {
            return LoadManifest(challengeId).attempts.Count;
        }

        public static bool IsBetter(ReplayRun candidate, ReplayRun existing)
        {
            Validate(candidate);
            if (existing?.result == null)
            {
                return true;
            }

            return candidate.result.completed && !existing.result.completed ||
                   candidate.result.completed == existing.result.completed && candidate.result.score > existing.result.score ||
                   candidate.result.completed && existing.result.completed &&
                   candidate.result.score == existing.result.score && candidate.result.timeMs < existing.result.timeMs;
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

        private static ReplayHistoryManifest LoadManifest(string challengeId)
        {
            var path = HistoryPath(challengeId);
            if (!File.Exists(path))
            {
                return new ReplayHistoryManifest { challengeId = challengeId };
            }

            try
            {
                var manifest = JsonUtility.FromJson<ReplayHistoryManifest>(File.ReadAllText(path));
                if (manifest == null)
                {
                    return new ReplayHistoryManifest { challengeId = challengeId };
                }
                manifest.attempts ??= new List<ReplayAttemptSummary>();
                return manifest;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not load replay history for {challengeId}: {exception.Message}");
                return new ReplayHistoryManifest { challengeId = challengeId };
            }
        }

        private static void SaveManifest(ReplayHistoryManifest manifest)
        {
            Directory.CreateDirectory(RootDirectory());
            File.WriteAllText(HistoryPath(manifest.challengeId), JsonUtility.ToJson(manifest, true));
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

        private static void Validate(ReplayRun run)
        {
            if (run?.result == null || string.IsNullOrWhiteSpace(run.challengeId))
            {
                throw new ArgumentException("A completed replay is required.", nameof(run));
            }
        }

        private static string BestPath(string challengeId) => Path.Combine(RootDirectory(), $"{SafeId(challengeId)}-best.json");
        private static string LatestPath(string challengeId) => Path.Combine(RootDirectory(), $"{SafeId(challengeId)}-latest.json");
        private static string HistoryPath(string challengeId) => Path.Combine(RootDirectory(), $"{SafeId(challengeId)}-history.json");
        private static string AttemptPath(string challengeId, string clientRunId) =>
            Path.Combine(RootDirectory(), $"{SafeId(challengeId)}-attempt-{SafeId(clientRunId)}.json");
        private static string RootDirectory() => Path.Combine(Application.persistentDataPath, "signal-hunt", "replays");
        private static string SafeId(string challengeId) => DailyChallenge.StableHash(challengeId).ToString("x8");
    }
}
