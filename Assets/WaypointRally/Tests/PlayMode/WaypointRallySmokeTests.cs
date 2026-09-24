using System.Collections;
using NUnit.Framework;
using SignalHunt.Gameplay;
using SignalHunt.Replay;
using SignalHunt.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
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
            yield return new WaitUntil(() => Object.FindAnyObjectByType<WaypointRallyGame>() != null &&
                                             Object.FindAnyObjectByType<RallySession>() != null);

            var game = Object.FindAnyObjectByType<WaypointRallyGame>();
            var startMenu = game.GetComponent<DailyStartMenu>();
            yield return new WaitUntil(() => startMenu != null &&
                                             startMenu.GetComponentsInChildren<Button>().Length == 1);
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
            Assert.That(startMenu, Is.Not.Null);
            Assert.That(startMenu.GetComponentsInChildren<Button>(), Has.Length.EqualTo(1));
            Assert.That(GameObject.Find("HOW TO PLAY"), Is.Null);
            var menuMotion = System.Array.Find(Object.FindObjectsByType<DailyMenuCameraMotion>(),
                motion => motion.Focus == vehicle.transform);
            Assert.That(menuMotion, Is.Not.Null);
            var rallyCamera = menuMotion.GetComponent<Camera>();
            Assert.That(rallyCamera, Is.Not.Null);
            vehicle.ResetToSpawn();
            vehicle.SetInputEnabled(false);
            var spawnPosition = vehicle.transform.position;
            startMenu.GetComponentInChildren<Button>().onClick.Invoke();
            yield return new WaitForSeconds(3.6f);
            Assert.That(menuMotion == null, Is.True);
            Assert.That(Object.FindAnyObjectByType<TouchControlPad>(), Is.Not.Null);
            Assert.That(GameObject.Find("Steering Pad"), Is.Not.Null);
            Assert.That(GameObject.Find("BRAKE"), Is.Not.Null);
            Assert.That(GameObject.Find("THRUST"), Is.Null);
            Assert.That(Vector3.Distance(spawnPosition, vehicle.transform.position), Is.LessThan(10f),
                $"spawn={spawnPosition}, current={vehicle.transform.position}, speed={vehicle.Speed:F2}");
            var vehicleViewport = rallyCamera.WorldToViewportPoint(vehicle.transform.position + Vector3.up * 0.5f);
            Assert.That(vehicleViewport.z, Is.GreaterThan(0f));
            Assert.That(vehicleViewport.x, Is.InRange(0.12f, 0.88f));
            Assert.That(vehicleViewport.y, Is.InRange(0.18f, 0.82f));
            Assert.That(vehicleObject.GetComponentsInChildren<Renderer>(),
                Has.Some.Matches<Renderer>(renderer => renderer.enabled));
            for (var index = 0; index < session.Challenge.collectibleCount; index++)
            {
                Assert.That(session.PassCheckpoint(index, $"test-checkpoint-{index + 1:D2}"), Is.True);
            }
            yield return null;

            Assert.That(session.State, Is.EqualTo(RallyState.Complete));
            Assert.That(ReplayStore.GetAttemptCount(session.Challenge.challengeId), Is.EqualTo(attemptsBefore + 1));
            Assert.That(GameObject.Find("Race Complete"), Is.Not.Null);
            Assert.That(GameObject.Find("PLAY AGAIN"), Is.Not.Null);
            Assert.That(GameObject.Find("DAILY FILM"), Is.Null);
        }
    }
}
