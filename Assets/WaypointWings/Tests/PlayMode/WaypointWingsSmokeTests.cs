using System.Collections;
using NUnit.Framework;
using SignalHunt.Replay;
using SignalHunt.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using WaypointWings.Gameplay;
using WaypointWings.World;

namespace WaypointWings.PlayModeTests
{
    public sealed class WaypointWingsSmokeTests
    {
        [UnityTest]
        public IEnumerator BootstrapBuildsAndCompletesAFlightCourse()
        {
            if (Object.FindAnyObjectByType<WaypointWingsGame>() == null)
            {
                new GameObject("Waypoint Wings Test").AddComponent<WaypointWingsGame>();
            }
            yield return new WaitUntil(() => GameObject.Find("START CHALLENGE") != null);

            var game = Object.FindAnyObjectByType<WaypointWingsGame>();
            var aircraft = Object.FindAnyObjectByType<AircraftController>();
            var session = Object.FindAnyObjectByType<FlightSession>();
            var gates = Object.FindObjectsByType<FlightGate>();

            Assert.That(game, Is.Not.Null);
            Assert.That(aircraft, Is.Not.Null);
            Assert.That(session, Is.Not.Null);
            Assert.That(session.Challenge.appKey, Is.EqualTo("waypoint-wings"));
            Assert.That(gates, Has.Length.EqualTo(14));
            Assert.That(Camera.main, Is.Not.Null);
            var attemptsBefore = ReplayStore.GetAttemptCount(session.Challenge.challengeId);
            Assert.That(GameObject.Find("Daily Start Menu"), Is.Not.Null);
            Assert.That(GameObject.Find("HOW TO PLAY"), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<DailyMenuCameraMotion>(), Is.Not.Null);
            GameObject.Find("START CHALLENGE").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSeconds(3.6f);
            Assert.That(Object.FindAnyObjectByType<DailyMenuCameraMotion>(), Is.Null);
            for (var index = 0; index < session.Challenge.collectibleCount; index++)
            {
                Assert.That(session.PassGate(index, $"test-gate-{index + 1:D2}"), Is.True);
            }
            yield return null;

            Assert.That(session.State, Is.EqualTo(FlightState.Complete));
            Assert.That(ReplayStore.GetAttemptCount(session.Challenge.challengeId), Is.EqualTo(attemptsBefore + 1));
            Assert.That(GameObject.Find("Flight Complete"), Is.Not.Null);
            Assert.That(GameObject.Find("FLY AGAIN"), Is.Not.Null);
            Assert.That(GameObject.Find("DAILY FILM"), Is.Not.Null);
        }
    }
}
