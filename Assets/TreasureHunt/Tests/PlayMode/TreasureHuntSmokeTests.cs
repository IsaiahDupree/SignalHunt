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
            yield return new WaitUntil(() => Object.FindAnyObjectByType<TreasureHuntGame>() != null &&
                                             Object.FindAnyObjectByType<TreasureSession>() != null);

            var game = Object.FindAnyObjectByType<TreasureHuntGame>();
            var startMenu = game.GetComponent<DailyStartMenu>();
            yield return new WaitUntil(() => startMenu != null &&
                                             startMenu.GetComponentsInChildren<Button>().Length == 1);
            var explorer = Object.FindAnyObjectByType<ExplorerController>();
            var session = Object.FindAnyObjectByType<TreasureSession>();
            var artifacts = Object.FindObjectsByType<TreasureArtifact>();

            Assert.That(game, Is.Not.Null);
            Assert.That(explorer, Is.Not.Null);
            Assert.That(session, Is.Not.Null);
            Assert.That(session.Challenge.appKey, Is.EqualTo("treasure-hunt"));
            Assert.That(artifacts, Has.Length.EqualTo(10));
            Assert.That(Camera.main, Is.Not.Null);
            var attemptsBefore = ReplayStore.GetAttemptCount(session.Challenge.challengeId);
            Assert.That(startMenu, Is.Not.Null);
            Assert.That(startMenu.GetComponentsInChildren<Button>(), Has.Length.EqualTo(1));
            Assert.That(GameObject.Find("HOW TO PLAY"), Is.Null);
            var menuMotion = System.Array.Find(Object.FindObjectsByType<DailyMenuCameraMotion>(),
                motion => motion.Focus == explorer.transform);
            Assert.That(menuMotion, Is.Not.Null);
            startMenu.GetComponentInChildren<Button>().onClick.Invoke();
            yield return new WaitForSeconds(3.6f);
            Assert.That(menuMotion == null, Is.True);
            Assert.That(Object.FindAnyObjectByType<TouchControlPad>(), Is.Not.Null);
            Assert.That(GameObject.Find("Move Pad"), Is.Not.Null);
            Assert.That(GameObject.Find("SCAN"), Is.Not.Null);
            Assert.That(GameObject.Find("RUN"), Is.Null);
            for (var index = 0; index < session.Challenge.collectibleCount; index++)
            {
                Assert.That(session.FindArtifact($"test-artifact-{index + 1:D2}"), Is.True);
            }
            yield return null;

            Assert.That(session.State, Is.EqualTo(TreasureState.Complete));
            Assert.That(ReplayStore.GetAttemptCount(session.Challenge.challengeId), Is.EqualTo(attemptsBefore + 1));
            Assert.That(GameObject.Find("Hunt Complete"), Is.Not.Null);
            Assert.That(GameObject.Find("PLAY AGAIN"), Is.Not.Null);
            Assert.That(GameObject.Find("DAILY FILM"), Is.Null);
            Object.Destroy(GameObject.Find("Daily Treasure World"));
            Object.Destroy(GameObject.Find("Player Treasure Hunter"));
            Object.Destroy(game.gameObject);
            yield return null;
        }
    }
}
