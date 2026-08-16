using System;
using NUnit.Framework;
using SignalHunt.Core;
using SignalHunt.Gameplay;
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
            Assert.That(challenge.season, Is.EqualTo("summer"));
            Assert.That(challenge.collectibleCount, Is.EqualTo(10));
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
    }
}
