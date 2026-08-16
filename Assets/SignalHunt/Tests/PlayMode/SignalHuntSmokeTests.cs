using System.Collections;
using NUnit.Framework;
using SignalHunt.Gameplay;
using SignalHunt.Replay;
using SignalHunt.World;
using UnityEngine;
using UnityEngine.TestTools;

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

            yield return null;
            yield return null;

            var game = Object.FindAnyObjectByType<SignalHuntGame>();
            var vehicle = Object.FindAnyObjectByType<HoverVehicleController>();
            var session = Object.FindAnyObjectByType<HuntSession>();
            var relics = Object.FindObjectsByType<RelicPickup>();

            Assert.That(game, Is.Not.Null);
            Assert.That(vehicle, Is.Not.Null);
            Assert.That(session, Is.Not.Null);
            Assert.That(session.Challenge, Is.Not.Null);
            Assert.That(relics, Has.Length.EqualTo(10));
            Assert.That(Camera.main, Is.Not.Null);

            var attemptsBefore = ReplayStore.GetAttemptCount(session.Challenge.challengeId);
            yield return new WaitForSeconds(3.2f);
            for (var index = 1; index <= session.Challenge.collectibleCount; index++)
            {
                session.Collect($"playmode-relic-{index:D2}");
            }
            yield return null;

            Assert.That(session.State, Is.EqualTo(HuntState.Complete));
            Assert.That(ReplayStore.GetAttemptCount(session.Challenge.challengeId), Is.EqualTo(attemptsBefore + 1));
            Assert.That(GameObject.Find("Run Complete"), Is.Not.Null);
            Assert.That(GameObject.Find("RACE AGAIN"), Is.Not.Null);
            Assert.That(GameObject.Find("DAILY FILM"), Is.Not.Null);
        }
    }
}
