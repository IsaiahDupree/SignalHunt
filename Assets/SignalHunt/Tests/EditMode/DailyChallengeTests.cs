using System;
using NUnit.Framework;
using SignalHunt.Core;
using SignalHunt.Gameplay;
using SignalHunt.Replay;
using SignalHunt.World;
using UnityEngine;

namespace SignalHunt.Tests
{
    public sealed class DailyChallengeTests
    {
        [Test]
        public void FixedUtcDateProducesStablePublicSeed()
        {
            var challenge = DailyChallenge.ForUtcDate(new DateTime(2026, 8, 15, 18, 42, 0, DateTimeKind.Utc));

            Assert.That(challenge.challengeId, Is.EqualTo("2026-08-15_city_neon_standard_seed_95713"));
            Assert.That(challenge.generationSeed, Is.EqualTo(3582895713u));
            Assert.That(challenge.Stage, Is.EqualTo(WorldStage.City));
            Assert.That(challenge.worldTemplate, Is.EqualTo("synthetic-town-v1"));
            Assert.That(challenge.generationVersion, Is.EqualTo("town-generator-v1"));
            Assert.That(challenge.appKey, Is.EqualTo("signal-hunt"));
            Assert.That(challenge.season, Is.EqualTo("summer"));
            Assert.That(challenge.collectibleCount, Is.EqualTo(10));
        }

        [Test]
        public void FollowingUtcDateSelectsStableIslandStage()
        {
            var challenge = DailyChallenge.ForUtcDate(new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc));

            Assert.That(challenge.challengeId, Is.EqualTo("2026-08-16_island_coastal_standard_seed_62010"));
            Assert.That(challenge.generationSeed, Is.EqualTo(924762010u));
            Assert.That(challenge.Stage, Is.EqualTo(WorldStage.Island));
            Assert.That(challenge.worldTemplate, Is.EqualTo("synthetic-island-v1"));
            Assert.That(challenge.generationVersion, Is.EqualTo("island-generator-v1"));
            Assert.That(challenge.stageDisplayName, Is.EqualTo("Emerald Isle"));
        }

        [Test]
        public void IslandGeneratorIsDeterministicAndPlayable()
        {
            var challenge = DailyChallenge.ForUtcDate(new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc));
            var first = IslandLayoutGenerator.Generate(challenge);
            var second = IslandLayoutGenerator.Generate(challenge);

            Assert.That(JsonUtility.ToJson(first), Is.EqualTo(JsonUtility.ToJson(second)));
            Assert.That(first.roadPoints, Has.Count.EqualTo(84));
            Assert.That(first.trees, Has.Count.EqualTo(86));
            Assert.That(first.relics, Has.Count.EqualTo(10));
            Assert.That(first.playerSpawn.y, Is.GreaterThan(first.waterLevel));
            foreach (var relic in first.relics)
            {
                Assert.That(relic.position.y, Is.GreaterThan(first.waterLevel + 0.5f));
            }
        }

        [Test]
        public void StageCanBeForcedWithoutChangingOtherChallengeRules()
        {
            var date = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc);
            var automatic = DailyChallenge.ForUtcDate(date);
            var island = DailyChallenge.ForUtcDate(date, WorldStage.Island);

            Assert.That(automatic.Stage, Is.EqualTo(WorldStage.City));
            Assert.That(island.Stage, Is.EqualTo(WorldStage.Island));
            Assert.That(island.dateKey, Is.EqualTo(automatic.dateKey));
            Assert.That(island.collectibleCount, Is.EqualTo(automatic.collectibleCount));
            Assert.That(island.challengeId, Is.Not.EqualTo(automatic.challengeId));
        }

        [Test]
        public void GeneratorIsDeterministicAndPlacesTenSeparatedRelics()
        {
            var challenge = DailyChallenge.ForUtcDate(new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc));
            var first = TownLayoutGenerator.Generate(challenge);
            var second = TownLayoutGenerator.Generate(challenge);

            Assert.That(JsonUtility.ToJson(first), Is.EqualTo(JsonUtility.ToJson(second)));
            Assert.That(first.relics, Has.Count.EqualTo(10));
            for (var left = 0; left < first.relics.Count; left++)
            {
                Assert.That(first.relics[left].id, Is.EqualTo($"relic-{left + 1:D2}"));
                for (var right = left + 1; right < first.relics.Count; right++)
                {
                    Assert.That(Vector3.Distance(first.relics[left].position, first.relics[right].position), Is.GreaterThanOrEqualTo(10f));
                }
            }
        }

        [Test]
        public void PerfectFastRunOutscoresPartialRun()
        {
            var perfect = HuntScore.Calculate(10, 10, 120f, true);
            var partial = HuntScore.Calculate(9, 10, 60f, false);

            Assert.That(perfect, Is.GreaterThan(partial));
            Assert.That(perfect, Is.EqualTo(18800));
        }

        [Test]
        public void RandomGeneratorRepeatsAcrossInstances()
        {
            var first = new DeterministicRandom(123456u);
            var second = new DeterministicRandom(123456u);
            for (var index = 0; index < 100; index++)
            {
                Assert.That(first.NextUInt(), Is.EqualTo(second.NextUInt()));
            }
        }

        [Test]
        public void FasterCompletedReplayBecomesThePersonalBest()
        {
            var existing = ReplayWithResult(true, 112000, 18000);
            var faster = ReplayWithResult(true, 101000, 18110);
            var partial = ReplayWithResult(false, 45000, 9000);

            Assert.That(ReplayStore.IsBetter(faster, existing), Is.True);
            Assert.That(ReplayStore.IsBetter(partial, existing), Is.False);
            Assert.That(ReplayStore.IsBetter(existing, faster), Is.False);
        }

        [Test]
        public void ReplayEnvelopeRoundTripsForEveryAppShell()
        {
            Assert.That(DailyGameCatalog.All.Count, Is.EqualTo(3));
            foreach (var profile in DailyGameCatalog.All)
            {
                var replay = ReplayWithResult(true, 90000, 19000);
                replay.appKey = profile.appKey;
                replay.modeKey = profile.modeKey;
                replay.clientRunId = Guid.NewGuid().ToString("N");
                replay.frames.Add(new ReplayFrame { timestamp = 0f, position = Vector3.zero });
                replay.frames.Add(new ReplayFrame { timestamp = 1f, position = Vector3.one });

                var restored = JsonUtility.FromJson<ReplayRun>(JsonUtility.ToJson(replay));
                Assert.That(restored.appKey, Is.EqualTo(profile.appKey));
                Assert.That(DailyGameCatalog.Get(restored.appKey).replayHook, Is.Not.Empty);
                Assert.That(restored.clientRunId, Is.EqualTo(replay.clientRunId));
                Assert.That(restored.frames, Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void ReplayStorePersistsEveryAttemptAndImprovement()
        {
            var challengeId = $"integration-{Guid.NewGuid():N}";
            var first = ReplayWithResult(true, 112000, 18000);
            first.challengeId = challengeId;
            first.clientRunId = Guid.NewGuid().ToString("N");
            first.frames.Add(new ReplayFrame { timestamp = 0f });
            first.frames.Add(new ReplayFrame { timestamp = 112f });
            var second = ReplayWithResult(true, 101000, 18110);
            second.challengeId = challengeId;
            second.clientRunId = Guid.NewGuid().ToString("N");
            second.frames.Add(new ReplayFrame { timestamp = 0f });
            second.frames.Add(new ReplayFrame { timestamp = 101f });

            var firstOutcome = ReplayStore.SaveAttempt(first);
            var secondOutcome = ReplayStore.SaveAttempt(second);
            var history = ReplayStore.LoadHistory(challengeId, 10);

            Assert.That(firstOutcome.attemptNumber, Is.EqualTo(1));
            Assert.That(secondOutcome.attemptNumber, Is.EqualTo(2));
            Assert.That(secondOutcome.isPersonalBest, Is.True);
            Assert.That(secondOutcome.improvementMs, Is.EqualTo(11000));
            Assert.That(history, Has.Length.EqualTo(2));
            Assert.That(history[0].clientRunId, Is.EqualTo(second.clientRunId));
        }

        private static ReplayRun ReplayWithResult(bool completed, int timeMs, int score)
        {
            return new ReplayRun
            {
                challengeId = "2026-08-16_island_coastal_standard_seed_62010",
                result = new HuntResult
                {
                    completed = completed,
                    timeMs = timeMs,
                    score = score,
                    collectedCount = completed ? 10 : 9,
                    totalCount = 10
                }
            };
        }
    }
}
