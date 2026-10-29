using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Player;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>H5: spring petals and autumn leaves over the city. Captures: Bao_Cao/seasonal-leaves.</summary>
    public class SeasonalLeavesPlayModeTests
    {
        private static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/seasonal-leaves"));

        [Test]
        public void Rates_FollowSeasonIndoorsAndRain()
        {
            Assert.Greater(SeasonalLeaves.RateFor(Season.Spring, false, false), 0f);
            Assert.Greater(SeasonalLeaves.RateFor(Season.Autumn, false, false), 0f);
            Assert.AreEqual(0f, SeasonalLeaves.RateFor(Season.Summer, false, false));
            Assert.AreEqual(0f, SeasonalLeaves.RateFor(Season.Winter, false, false));
            Assert.AreEqual(0f, SeasonalLeaves.RateFor(Season.Autumn, true, false), "Not indoors");
            Assert.AreEqual(0f, SeasonalLeaves.RateFor(Season.Spring, false, true), "Not in the rain");
        }

        [UnityTest]
        public IEnumerator City_FallsInAutumnAndSpring_NotInSummer()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            WeatherCycle.Instance.Force(WeatherCycle.Kind.Clear);
            var leaves = SeasonalLeaves.Instance;
            Assert.NotNull(leaves, "The effect boots with the game");
            SeasonalLeaves.SeasonOverride = Season.Summer;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(0f, leaves.Rate);
            SeasonalLeaves.SeasonOverride = Season.Autumn;
            yield return new WaitForSecondsRealtime(4f);
            Assert.Greater(leaves.Rate, 0f); Assert.Greater(leaves.AliveParticles, 5, "Leaves are in the air");
            Assert.Greater(leaves.Tint.r, leaves.Tint.b, "Autumn leaves are warm");
            Shot("autumn");
            SeasonalLeaves.SeasonOverride = Season.Spring;
            yield return new WaitForSecondsRealtime(3f);
            Assert.Greater(leaves.Rate, 0f); Assert.Greater(leaves.Tint.b, 150, "Pink petals"); Shot("spring");
            SeasonalLeaves.SeasonOverride = Season.Winter;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(0f, leaves.Rate);
            SeasonalLeaves.SeasonOverride = null; WeatherCycle.Instance.Force(null);
        }

        private static void Shot(string name)
        {
            var cam = Camera.main;
            var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.Render();
            var old = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
            Directory.CreateDirectory(Dir); File.WriteAllBytes(Path.Combine(Dir, name + ".jpg"), tex.EncodeToJPG(88));
            RenderTexture.active = old; cam.targetTexture = null; rt.Release(); Object.Destroy(rt); Object.Destroy(tex);
        }
    }
}