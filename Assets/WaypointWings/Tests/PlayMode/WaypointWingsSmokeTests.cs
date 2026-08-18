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
            yield return new WaitUntil(() => Object.FindAnyObjectByType<WaypointWingsGame>() != null &&
                                             Object.FindAnyObjectByType<FlightSession>() != null);

            var game = Object.FindAnyObjectByType<WaypointWingsGame>();
            var startMenu = game.GetComponent<DailyStartMenu>();
            yield return new WaitUntil(() => startMenu != null &&
                                             startMenu.GetComponentsInChildren<Button>().Length == 1);
            var aircraft = Object.FindAnyObjectByType<AircraftController>();
            var session = Object.FindAnyObjectByType<FlightSession>();
            var gates = Object.FindObjectsByType<FlightGate>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.That(game, Is.Not.Null);
            Assert.That(aircraft, Is.Not.Null);
            Assert.That(session, Is.Not.Null);
            Assert.That(session.Challenge.appKey, Is.EqualTo("waypoint-wings"));
            Assert.That(gates, Has.Length.EqualTo(12));
            Assert.That(System.Array.FindAll(gates, gate => gate.gameObject.activeSelf), Has.Length.EqualTo(1));
            Assert.That(Camera.main, Is.Not.Null);
            var attemptsBefore = ReplayStore.GetAttemptCount(session.Challenge.challengeId);
            Assert.That(startMenu, Is.Not.Null);
            Assert.That(startMenu.GetComponentsInChildren<Button>(), Has.Length.EqualTo(1));
            Assert.That(GameObject.Find("HOW TO PLAY"), Is.Null);
            var menuMotion = System.Array.Find(Object.FindObjectsByType<DailyMenuCameraMotion>(),
                motion => motion.Focus == aircraft.transform);
            Assert.That(menuMotion, Is.Not.Null);
            startMenu.GetComponentInChildren<Button>().onClick.Invoke();
            yield return new WaitForSeconds(3.6f);
            Assert.That(menuMotion == null, Is.True);
            Assert.That(Object.FindAnyObjectByType<TouchControlPad>(), Is.Not.Null);
            Assert.That(GameObject.Find("Flight Stick"), Is.Not.Null);
            Assert.That(GameObject.Find("BOOST"), Is.Not.Null);
            var forwardBeforeStickInput = aircraft.transform.forward;
            FlightInputState.SetStick(new Vector2(0.65f, 0.45f));
            yield return new WaitForSeconds(0.45f);
            FlightInputState.Clear();
            Assert.That(Vector3.Angle(forwardBeforeStickInput, aircraft.transform.forward), Is.GreaterThan(1f));
            var flyingDeadline = Time.realtimeSinceStartup + 4.5f;
            while (session.State != FlightState.Flying && Time.realtimeSinceStartup < flyingDeadline)
            {
                yield return null;
            }
            Assert.That(session.State, Is.EqualTo(FlightState.Flying));
            // The wide first gate can be reached naturally while the steering response is exercised.
            // If it has not been reached yet, advance the currently expected gate explicitly.
            if (session.ClearedCount == 0)
            {
                Assert.That(session.PassGate(0, "test-gate-01"), Is.True);
            }
            else
            {
                Assert.That(session.ClearedCount, Is.GreaterThanOrEqualTo(1));
            }
            var restartsBeforeCrash = session.RestartCount;
            aircraft.TriggerCrash();
            var restartDeadline = Time.realtimeSinceStartup + 1.5f;
            while (session.RestartCount == restartsBeforeCrash && Time.realtimeSinceStartup < restartDeadline)
            {
                yield return null;
            }
            Assert.That(session.RestartCount, Is.EqualTo(restartsBeforeCrash + 1));
            Assert.That(session.ClearedCount, Is.Zero);
            Assert.That(System.Array.Find(gates, gate => gate.Index == 0).gameObject.activeSelf, Is.True);
            Assert.That(System.Array.FindAll(gates, gate => gate.gameObject.activeSelf), Has.Length.EqualTo(1));
            flyingDeadline = Time.realtimeSinceStartup + 4.5f;
            while (session.State != FlightState.Flying && Time.realtimeSinceStartup < flyingDeadline)
            {
                yield return null;
            }
            Assert.That(session.State, Is.EqualTo(FlightState.Flying));
            for (var index = 0; index < session.Challenge.collectibleCount; index++)
            {
                Assert.That(session.PassGate(index, $"test-gate-{index + 1:D2}"), Is.True);
            }
            yield return null;

            Assert.That(session.State, Is.EqualTo(FlightState.Complete));
            Assert.That(ReplayStore.GetAttemptCount(session.Challenge.challengeId), Is.EqualTo(attemptsBefore + 1));
            Assert.That(GameObject.Find("Flight Complete"), Is.Not.Null);
            Assert.That(GameObject.Find("PLAY AGAIN"), Is.Not.Null);
            Assert.That(GameObject.Find("DAILY FILM"), Is.Null);
        }
    }
}
