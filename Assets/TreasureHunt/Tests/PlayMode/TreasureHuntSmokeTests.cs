using System.Collections;
using NUnit.Framework;
using SignalHunt.Replay;
using SignalHunt.UI;
using TreasureHunt.Gameplay;
using TreasureHunt.World;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TreasureHunt.PlayModeTests
{
    public sealed class TreasureHuntSmokeTests
    {
        [UnityTest]
        public IEnumerator BootstrapBuildsAndCompletesATreasureHunt()
        {
            if (Object.FindAnyObjectByType<TreasureHuntGame>() == null)
            {
                new GameObject("Treasure Hunter Test").AddComponent<TreasureHuntGame>();
            }
            yield return new WaitUntil(() => GameObject.Find("START CHALLENGE") != null);

            var game = Object.FindAnyObjectByType<TreasureHuntGame>();
            var explorer = Object.FindAnyObjectByType<ExplorerController>();
            var session = Object.FindAnyObjectByType<TreasureSession>();
            var artifacts = Object.FindObjectsByType<TreasureArtifact>();

            Assert.That(game, Is.Not.Null);
            Assert.That(explorer, Is.Not.Null);
            Assert.That(session, Is.Not.Null);
            Assert.That(session.Challenge.appKey, Is.EqualTo("treasure-hunt"));
            Assert.That(artifacts, Has.Length.EqualTo(12));
            Assert.That(Camera.main, Is.Not.Null);
            var attemptsBefore = ReplayStore.GetAttemptCount(session.Challenge.challengeId);
            Assert.That(GameObject.Find("Daily Start Menu"), Is.Not.Null);
            Assert.That(GameObject.Find("HOW TO PLAY"), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<DailyMenuCameraMotion>(), Is.Not.Null);
            GameObject.Find("START CHALLENGE").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSeconds(3.6f);
            Assert.That(Object.FindAnyObjectByType<DailyMenuCameraMotion>(), Is.Null);
            Assert.That(Object.FindAnyObjectByType<TouchControlPad>(), Is.Not.Null);
            Assert.That(GameObject.Find("Steering Pad"), Is.Not.Null);
            Assert.That(GameObject.Find("SCAN"), Is.Not.Null);
            for (var index = 0; index < session.Challenge.collectibleCount; index++)
            {
                Assert.That(session.FindArtifact($"test-artifact-{index + 1:D2}"), Is.True);
            }
            yield return null;

            Assert.That(session.State, Is.EqualTo(TreasureState.Complete));
            Assert.That(ReplayStore.GetAttemptCount(session.Challenge.challengeId), Is.EqualTo(attemptsBefore + 1));
            Assert.That(GameObject.Find("Hunt Complete"), Is.Not.Null);
            Assert.That(GameObject.Find("HUNT AGAIN"), Is.Not.Null);
            Assert.That(GameObject.Find("DAILY FILM"), Is.Not.Null);
        }
    }
}
