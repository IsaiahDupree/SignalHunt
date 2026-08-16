using System.Collections;
using NUnit.Framework;
using SignalHunt.Gameplay;
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
        }
    }
}
