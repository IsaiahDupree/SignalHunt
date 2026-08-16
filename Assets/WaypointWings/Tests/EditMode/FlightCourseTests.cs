using System;
using NUnit.Framework;
using SignalHunt.Core;
using UnityEngine;
using WaypointWings.Core;
using WaypointWings.World;

namespace WaypointWings.Tests
{
    public sealed class FlightCourseTests
    {
        [Test]
        public void DailyChallengeUsesTheWaypointWingsAppContract()
        {
            var challenge = WingsDailyChallenge.ForUtcDate(new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc));

            Assert.That(challenge.appKey, Is.EqualTo(DailyGameCatalog.WaypointWingsAppKey));
            Assert.That(challenge.modeKey, Is.EqualTo("daily-flight"));
            Assert.That(challenge.stageKey, Is.EqualTo("archipelago"));
            Assert.That(challenge.worldTemplate, Is.EqualTo("wings-archipelago-v1"));
            Assert.That(challenge.generationVersion, Is.EqualTo("wings-course-generator-v1"));
            Assert.That(challenge.collectibleCount, Is.EqualTo(14));
        }

        [Test]
        public void FlightCourseIsDeterministicAndThreeDimensional()
        {
            var challenge = WingsDailyChallenge.ForUtcDate(new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc));
            var first = FlightCourseGenerator.Generate(challenge);
            var second = FlightCourseGenerator.Generate(challenge);

            Assert.That(JsonUtility.ToJson(first), Is.EqualTo(JsonUtility.ToJson(second)));
            Assert.That(first.gates, Has.Count.EqualTo(14));
            Assert.That(first.islands, Has.Count.EqualTo(22));
            Assert.That(first.clouds, Has.Count.EqualTo(34));
            Assert.That(first.gates[0].position.y, Is.Not.EqualTo(first.gates[^1].position.y).Within(0.01f));
            for (var index = 1; index < first.gates.Count; index++)
            {
                Assert.That(Vector3.Distance(first.gates[index - 1].position, first.gates[index].position), Is.GreaterThan(15f));
            }
        }

        [Test]
        public void SkywayUsesTowersInsteadOfIslands()
        {
            var challenge = WingsDailyChallenge.ForUtcDate(new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc), WingsStage.Skyway);
            var layout = FlightCourseGenerator.Generate(challenge);

            Assert.That(challenge.stageKey, Is.EqualTo("skyway"));
            Assert.That(layout.towers, Has.Count.EqualTo(22));
            Assert.That(layout.islands, Is.Empty);
        }
    }
}
