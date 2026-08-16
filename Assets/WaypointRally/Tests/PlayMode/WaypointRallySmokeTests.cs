using System.Collections;
using NUnit.Framework;
using SignalHunt.Gameplay;
using SignalHunt.Replay;
using UnityEngine;
using UnityEngine.TestTools;
using WaypointRally.Gameplay;
using WaypointRally.World;

namespace WaypointRally.PlayModeTests
{
    public sealed class WaypointRallySmokeTests
    {
        [UnityTest]
        public IEnumerator BootstrapBuildsAndCompletesARallyRoute()
        {
            if (Object.FindAnyObjectByType<WaypointRallyGame>() == null)
            {
                new GameObject("Waypoint Rally Test").AddComponent<WaypointRallyGame>();
            }
            yield return null;
            yield return null;

            var game = Object.FindAnyObjectByType<WaypointRallyGame>();
            var vehicleObject = GameObject.Find("Player Rally Car");
            var vehicle = vehicleObject == null ? null : vehicleObject.GetComponent<HoverVehicleController>();
            var session = Object.FindAnyObjectByType<RallySession>();
            var checkpoints = Object.FindObjectsByType<RallyCheckpoint>();

            Assert.That(game, Is.Not.Null);
            Assert.That(vehicle, Is.Not.Null);
            Assert.That(session, Is.Not.Null);
            Assert.That(session.Challenge.appKey, Is.EqualTo("waypoint-rally"));
            Assert.That(checkpoints, Has.Length.EqualTo(10));
            Assert.That(Camera.main, Is.Not.Null);
            var attemptsBefore = ReplayStore.GetAttemptCount(session.Challenge.challengeId);
            yield return new WaitForSeconds(3.2f);
            for (var index = 0; index < session.Challenge.collectibleCount; index++)
            {
                Assert.That(session.PassCheckpoint(index, $"test-checkpoint-{index + 1:D2}"), Is.True);
            }
            yield return null;

            Assert.That(session.State, Is.EqualTo(RallyState.Complete));
            Assert.That(ReplayStore.GetAttemptCount(session.Challenge.challengeId), Is.EqualTo(attemptsBefore + 1));
            Assert.That(GameObject.Find("Race Complete"), Is.Not.Null);
            Assert.That(GameObject.Find("RACE AGAIN"), Is.Not.Null);
            Assert.That(GameObject.Find("DAILY FILM"), Is.Not.Null);
        }
    }
}
