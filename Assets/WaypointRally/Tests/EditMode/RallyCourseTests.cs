using System;
using NUnit.Framework;
using SignalHunt.Core;
using UnityEngine;
using WaypointRally.Core;
using WaypointRally.World;

namespace WaypointRally.Tests
{
    public sealed class RallyCourseTests
    {
        [Test]
        public void DailyChallengeUsesTheWaypointRallyContract()
        {
            var challenge = RallyDailyChallenge.ForUtcDate(new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc));

            Assert.That(challenge.appKey, Is.EqualTo(DailyGameCatalog.WaypointRallyAppKey));
            Assert.That(challenge.modeKey, Is.EqualTo("daily-race"));
            Assert.That(challenge.stageKey, Is.EqualTo("harbor-town"));
            Assert.That(challenge.worldTemplate, Is.EqualTo("rally-harbor-town-v1"));
            Assert.That(challenge.generationVersion, Is.EqualTo("rally-course-generator-v1"));
            Assert.That(challenge.collectibleCount, Is.EqualTo(10));
        }

        [Test]
        public void HarborCourseIsDeterministicAndSupportsRouteChoice()
        {
            var challenge = RallyDailyChallenge.ForUtcDate(new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc));
            var first = RallyCourseGenerator.Generate(challenge);
            var second = RallyCourseGenerator.Generate(challenge);

            Assert.That(JsonUtility.ToJson(first), Is.EqualTo(JsonUtility.ToJson(second)));
            Assert.That(first.checkpoints, Has.Count.EqualTo(10));
            Assert.That(first.roads.Count, Is.GreaterThanOrEqualTo(7));
            Assert.That(first.buildings.Count, Is.GreaterThan(0));
            Assert.That(first.ramps, Has.Count.EqualTo(2));
            for (var index = 1; index < first.checkpoints.Count; index++)
            {
                Assert.That(Vector3.Distance(first.checkpoints[index - 1].position, first.checkpoints[index].position),
                    Is.GreaterThan(20f));
            }
        }

        [Test]
        public void DustlandsUsesOpenTerrainObstaclesAndNoBuildings()
        {
            var challenge = RallyDailyChallenge.ForUtcDate(new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc),
                RallyStage.Dustlands);
            var layout = RallyCourseGenerator.Generate(challenge);

            Assert.That(challenge.stageKey, Is.EqualTo("dustlands"));
            Assert.That(layout.checkpoints, Has.Count.EqualTo(10));
            Assert.That(layout.obstacles, Has.Count.EqualTo(42));
            Assert.That(layout.decorations, Has.Count.EqualTo(24));
            Assert.That(layout.buildings, Is.Empty);
        }
    }
}
