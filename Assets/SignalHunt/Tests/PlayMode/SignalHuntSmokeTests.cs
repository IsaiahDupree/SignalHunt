using System.Collections;
using NUnit.Framework;
using SignalHunt.Gameplay;
using SignalHunt.Replay;
using SignalHunt.UI;
using SignalHunt.World;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SignalHunt.PlayModeTests
{
    public sealed class SignalHuntSmokeTests
    {
        [UnityTest]
        public IEnumerator BootstrapBuildsAPlayableDailyWorld()
        {
            if (Object.FindAnyObjectByType<SignalHuntGame>() == null)
            {
                new GameObject("Signal Hunt Test").AddComponent<SignalHuntGame>();
            }

            yield return new WaitUntil(() => Object.FindAnyObjectByType<SignalHuntGame>() != null &&
                                             Object.FindAnyObjectByType<HuntSession>() != null);

            var game = Object.FindAnyObjectByType<SignalHuntGame>();
            var startMenu = game.GetComponent<DailyStartMenu>();
            yield return new WaitUntil(() => startMenu != null &&
                                             startMenu.GetComponentsInChildren<Button>().Length == 1);
            var vehicle = GameObject.Find("Player Hover Car").GetComponent<HoverVehicleController>();
            var session = Object.FindAnyObjectByType<HuntSession>();
            var relics = Object.FindObjectsByType<RelicPickup>();

            Assert.That(game, Is.Not.Null);
            Assert.That(vehicle, Is.Not.Null);
            Assert.That(session, Is.Not.Null);
            Assert.That(session.Challenge, Is.Not.Null);
            Assert.That(relics, Has.Length.EqualTo(10));
            Assert.That(Camera.main, Is.Not.Null);

            var attemptsBefore = ReplayStore.GetAttemptCount(session.Challenge.challengeId);
            Assert.That(startMenu, Is.Not.Null);
            Assert.That(startMenu.GetComponentsInChildren<Button>(), Has.Length.EqualTo(1));
            Assert.That(GameObject.Find("HOW TO PLAY"), Is.Null);
            var menuMotion = System.Array.Find(Object.FindObjectsByType<DailyMenuCameraMotion>(),
                motion => motion.Focus == vehicle.transform);
            Assert.That(menuMotion, Is.Not.Null);
            startMenu.GetComponentInChildren<Button>().onClick.Invoke();
            yield return new WaitForSeconds(3.6f);
            Assert.That(menuMotion == null, Is.True);
            Assert.That(Object.FindAnyObjectByType<TouchControlPad>(), Is.Not.Null);
            Assert.That(GameObject.Find("Steering Pad"), Is.Not.Null);
            Assert.That(GameObject.Find("BRAKE"), Is.Not.Null);
            Assert.That(GameObject.Find("THRUST"), Is.Null);
            for (var index = 1; index <= session.Challenge.collectibleCount; index++)
            {
                session.Collect($"playmode-relic-{index:D2}");
            }
            yield return null;

            Assert.That(session.State, Is.EqualTo(HuntState.Complete));
            Assert.That(ReplayStore.GetAttemptCount(session.Challenge.challengeId), Is.EqualTo(attemptsBefore + 1));
            Assert.That(GameObject.Find("Run Complete"), Is.Not.Null);
            Assert.That(GameObject.Find("PLAY AGAIN"), Is.Not.Null);
            Assert.That(GameObject.Find("DAILY FILM"), Is.Null);
            Object.Destroy(GameObject.Find("Daily Emerald Isle"));
            Object.Destroy(GameObject.Find("Daily Synthetic Town"));
            Object.Destroy(GameObject.Find("Player Hover Car"));
            Object.Destroy(game.gameObject);
            yield return null;
        }
    }
}
