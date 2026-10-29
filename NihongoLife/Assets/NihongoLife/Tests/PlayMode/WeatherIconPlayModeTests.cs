using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>H4: the weather/time icon beside the place name in the status widget. Captures: Bao_Cao/weather-icon.</summary>
    public class WeatherIconPlayModeTests
    {
        private static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/weather-icon"));

        [Test]
        public void Icons_AreDistinctShapes()
        {
            var all = new[] { HudIcons.Sun, HudIcons.Cloud, HudIcons.RainCloud, HudIcons.Moon };
            foreach (var s in all) { Assert.NotNull(s); Assert.NotNull(s.texture); }
            Assert.AreEqual(4, all.Distinct().Count(), "Four different icons");
            Assert.AreSame(HudIcons.Sun, HudIcons.Sun, "Cached");
        }

        [UnityTest]
        public IEnumerator Dock_ShowsSunCloudRainMoon()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            var dock = Object.FindFirstObjectByType<StatusDock>();
            Assert.NotNull(dock);
            DayNightCycle.Active.SetHour(12f);
            WeatherCycle.Instance.Force(WeatherCycle.Kind.Clear);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual("sun", dock.SkyIconName); Shot(dock, "sun");
            WeatherCycle.Instance.Force(WeatherCycle.Kind.Overcast);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual("cloud", dock.SkyIconName); Shot(dock, "cloud");
            WeatherCycle.Instance.Force(WeatherCycle.Kind.Rain);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual("rain", dock.SkyIconName); Shot(dock, "rain");
            WeatherCycle.Instance.Force(WeatherCycle.Kind.Clear);
            DayNightCycle.Active.SetHour(22f);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual("moon", dock.SkyIconName); Shot(dock, "moon");
            WeatherCycle.Instance.Force(null);
            DayNightCycle.Active.SetHour(12f);
        }

        private static void Shot(StatusDock dock, string name)
        {
            var cam = Camera.main;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = cam; canvases[i].planeDistance = cam.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.Render();
            var old = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
            Directory.CreateDirectory(Dir); File.WriteAllBytes(Path.Combine(Dir, name + ".jpg"), tex.EncodeToJPG(88));
            RenderTexture.active = old; cam.targetTexture = null; rt.Release(); Object.Destroy(rt); Object.Destroy(tex);
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
        }
    }
}