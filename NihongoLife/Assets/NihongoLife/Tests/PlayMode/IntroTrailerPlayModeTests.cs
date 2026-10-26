using System.Collections;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.Save;
using NihongoLife.Trailer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The intro trailer before a guest's first game: it plays on a throw-away save (the player's real save and its
    /// low health are never used, nobody faints), shows a Skip button, and skipping goes straight on into the game
    /// with the real save back. It plays once per device.
    /// </summary>
    public class IntroTrailerPlayModeTests
    {
        [UnityTest]
        public IEnumerator Intro_PlaysOnAThrowAwaySave_SkipGoesIntoTheGame()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2f);
            Assert.IsTrue(GameServices.TryGet(out IProgressRepository realSaves));
            var real = realSaves.GetProgress();
            float realHealth = real.health;
            real.health = 3f; // a nearly fainted save must not reach the trailer
            bool seen = IntroTrailer.Seen;
            bool entered = false;
            IntroTrailer.Bypass = false;
            IntroTrailer.Seen = false;
            try
            {
                Assert.IsTrue(IntroTrailer.PlayBefore(() => entered = true), "A first game plays the intro");
                Assert.IsTrue(IntroTrailer.Playing);
                Assert.IsFalse(IntroTrailer.PlayBefore(() => { }), "Never twice at once");
                TrailerReel reel = null;
                for (float t = 0f; t < 30f && (reel == null || reel.CurrentShot == null || reel.CurrentShot == "title"); t += Time.unscaledDeltaTime)
                {
                    reel = Object.FindFirstObjectByType<TrailerReel>();
                    yield return null;
                }
                Assert.NotNull(reel, "The trailer reel runs");
                Assert.IsTrue(GameServices.TryGet(out IProgressRepository during));
                Assert.AreNotSame(realSaves, during, "The trailer films on its own save");
                yield return new WaitForSecondsRealtime(1f);
                Assert.Greater(PlayerStatus.Instance.CurrentHealth, 90f, "The trailer's player is healthy (not the real, low-health save)");
                Assert.AreEqual(3f, real.health, 0.001f, "The real save is untouched");
                var skip = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "SkipButton");
                Assert.NotNull(skip, "A Skip button");
                Assert.IsTrue(skip.gameObject.activeInHierarchy);

                skip.onClick.Invoke();
                yield return null;
                Assert.IsTrue(entered, "Skip goes on into the game");
                Assert.IsFalse(IntroTrailer.Playing);
                Assert.IsTrue(GameServices.TryGet(out IProgressRepository after));
                Assert.AreSame(realSaves, after, "The real save is back");
                Assert.IsTrue(IntroTrailer.Seen, "It plays once per device");
                Assert.IsFalse(IntroTrailer.PlayBefore(() => { }), "Not again after it was seen");
            }
            finally
            {
                if (IntroTrailer.Playing) IntroTrailer.Finish(continueGame: false);
                IntroTrailer.Seen = seen;
                IntroTrailer.Bypass = true;
                real.health = realHealth;
            }
        }
    }
}
