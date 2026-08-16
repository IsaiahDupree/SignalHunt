using System;
using NUnit.Framework;
using SignalHunt.Core;
using UnityEngine;
using TreasureHunt.Core;
using TreasureHunt.World;

namespace TreasureHunt.Tests
{
    public sealed class TreasureWorldTests
    {
        [Test]
        public void DailyChallengeUsesTheTreasureHuntContract()
        {
            var challenge = TreasureDailyChallenge.ForUtcDate(
                new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc));

            Assert.That(challenge.appKey, Is.EqualTo(DailyGameCatalog.TreasureHuntAppKey));
            Assert.That(challenge.modeKey, Is.EqualTo("daily-treasure-hunt"));
            Assert.That(challenge.stageKey, Is.EqualTo("sunken-ruins"));
            Assert.That(challenge.worldTemplate, Is.EqualTo("treasure-sunken-ruins-v1"));
            Assert.That(challenge.generationVersion, Is.EqualTo("treasure-world-generator-v1"));
            Assert.That(challenge.collectibleCount, Is.EqualTo(12));
        }

        [Test]
        public void RuinsAreDeterministicAndArtifactsAreSeparated()
        {
            var challenge = TreasureDailyChallenge.ForUtcDate(
                new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc));
            var first = TreasureWorldGenerator.Generate(challenge);
            var second = TreasureWorldGenerator.Generate(challenge);

            Assert.That(JsonUtility.ToJson(first), Is.EqualTo(JsonUtility.ToJson(second)));
            Assert.That(first.artifacts, Has.Count.EqualTo(12));
            Assert.That(first.paths, Has.Count.EqualTo(8));
            Assert.That(first.patches, Has.Count.EqualTo(22));
            for (var index = 0; index < first.artifacts.Count; index++)
            {
                for (var other = index + 1; other < first.artifacts.Count; other++)
                {
                    Assert.That(Vector3.Distance(first.artifacts[index].position, first.artifacts[other].position),
                        Is.GreaterThanOrEqualTo(15.9f));
                }
            }
        }

        [Test]
        public void CrystalHollowUsesCrystalsRocksAndPillars()
        {
            var challenge = TreasureDailyChallenge.ForUtcDate(
                new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc), TreasureStage.CrystalHollow);
            var layout = TreasureWorldGenerator.Generate(challenge);

            Assert.That(challenge.stageKey, Is.EqualTo("crystal-hollow"));
            Assert.That(layout.artifacts, Has.Count.EqualTo(12));
            Assert.That(layout.paths, Has.Count.EqualTo(5));
            Assert.That(layout.props.FindAll(prop => prop.kind == TreasurePropKind.Crystal).Count,
                Is.GreaterThanOrEqualTo(50));
            Assert.That(layout.props.FindAll(prop => prop.kind == TreasurePropKind.Pillar), Has.Count.EqualTo(18));
        }
    }
}
