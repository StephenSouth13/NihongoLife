using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The M-key map: smooth open/close, legend, compass, scale bar, district names in four languages, a pulsing position marker;
    /// everything fits inside the window. Captures: Bao_Cao/map-ui.
    /// </summary>
    public class MapPremiumPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/map-ui"));

        [UnityTest]
        public IEnumerator Map_Premium_City_And_Island()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var dm = DialogueManager.Instance;
            while (dm != null && dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
            var language = Loc.Language;
            var map = Object.FindFirstObjectByType<WorldMapUI>();
            Assert.NotNull(map, "Map component");
            try
            {
                Loc.Set(GameLanguage.English);

                // Opening fades and scales in (unscaled time), closing fades out; state flips at once.
                var timer = Stopwatch.StartNew();
                map.SetVisible(true);
                timer.Stop();
                Assert.Less(timer.ElapsedMilliseconds, 600, "Opening builds the map quickly (" + timer.ElapsedMilliseconds + " ms)");
                Assert.IsTrue(map.IsVisible);
                Assert.IsTrue(map.IsAnimating, "Open animation runs");
                Assert.Less(map.WindowAlpha, 0.99f);
                float t0 = Time.realtimeSinceStartup;
                while (map.IsAnimating && Time.realtimeSinceStartup - t0 < 2f) yield return null;
                Assert.IsFalse(map.IsAnimating, "Open animation ends");
                Assert.AreEqual(1f, map.WindowAlpha, 0.001f);
                Assert.Less(Time.realtimeSinceStartup - t0, 0.8f, "Open animation is short");
                Assert.AreEqual(1f, Named("MapWindow").transform.localScale.x, 0.001f);

                AssertDecor(city: true);
                Capture("01_city_en");

                // The position marker pulses.
                float min = 9f, max = 0f;
                for (int i = 0; i < 40; i++)
                {
                    var ring = Named("Ring");
                    Assert.NotNull(ring, "Pulse ring");
                    min = Mathf.Min(min, ring.transform.localScale.x); max = Mathf.Max(max, ring.transform.localScale.x);
                    yield return new WaitForSecondsRealtime(0.03f);
                }
                Assert.Greater(max - min, 0.15f, "Marker ring pulses (" + min.ToString("0.00") + ".." + max.ToString("0.00") + ")");

                // Four languages: legend and district names change and fit.
                var youNames = new Dictionary<GameLanguage, string>();
                var districtNames = new Dictionary<GameLanguage, string>();
                foreach (var lang in new[] { GameLanguage.Vietnamese, GameLanguage.Japanese, GameLanguage.Russian, GameLanguage.English })
                {
                    Loc.Set(lang);
                    map.SetVisible(false);
                    yield return new WaitForSecondsRealtime(0.3f);
                    map.SetVisible(true);
                    yield return new WaitForSecondsRealtime(0.4f);
                    AssertDecor(city: true);
                    youNames[lang] = Named("Legend_YOU").GetComponentsInChildren<TMP_Text>().First(t => t.name == "Text").text;
                    districtNames[lang] = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                        .First(r => r.name.StartsWith("District_")).GetComponentInChildren<TMP_Text>().text;
                    Capture("02_city_" + lang.ToString().ToLowerInvariant());
                }
                Assert.AreEqual(4, youNames.Values.Distinct().Count(), "Legend in four languages: " + string.Join(" | ", youNames.Values));
                Assert.AreEqual(4, districtNames.Values.Distinct().Count(), "Districts in four languages: " + string.Join(" | ", districtNames.Values));

                // Top-down tab hides the drawn dressing; the overview brings it back.
                Loc.Set(GameLanguage.English);
                map.SetVisible(false); yield return new WaitForSecondsRealtime(0.3f);
                map.SetVisible(true); yield return new WaitForSecondsRealtime(0.4f);
                var area = Named("MapTab_Area").GetComponent<Button>();
                var overview = Named("MapTab_Overview").GetComponent<Button>();
                area.onClick.Invoke(); yield return null;
                Assert.IsNull(Named("Legend"), "Legend hidden on the live view");
                Assert.IsFalse(Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Any(r => r.name.StartsWith("District_")), "Districts hidden on the live view");
                overview.onClick.Invoke(); yield return null;
                Assert.NotNull(Named("Legend"), "Legend back");

                // Closing: instant state, then fades out; reopening mid-fade cancels it.
                map.SetVisible(false);
                Assert.IsFalse(map.IsVisible, "Closed at once");
                yield return null;
                map.SetVisible(true);
                Assert.IsTrue(map.IsVisible, "Reopened while closing");
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.IsNotNull(Named("WorldMap"), "Still open after the reopen");
                Assert.AreEqual(1f, map.WindowAlpha, 0.001f);
                map.SetVisible(false);
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.IsNull(Named("WorldMap"), "Window gone after the fade");
                Assert.IsFalse(map.IsAnimating);

                // Phu Quoc: sea name, a scale bar in kilometres, more categories.
                yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.PhuQuocIslandScene);
                // Play inside a zone boots the town first and walks in (StandaloneZoneBootstrap): wait for the island
                // to be the active scene rather than a fixed time.
                // (The directly loaded island is active at first; the boot then loads the town and enters the island again.)
                bool Entered()
                {
                    var flow = Object.FindFirstObjectByType<SceneFlowController>();
                    return SceneManager.GetActiveScene().name == WorldLocationCatalog.PhuQuocIslandScene
                        && SceneManager.GetSceneByName(WorldLocationCatalog.CityScene).isLoaded && flow != null && !flow.IsLoading;
                }
                for (float t = 0f; t < 40f && !Entered(); t += Time.unscaledDeltaTime) yield return null;
                Assert.AreEqual(WorldLocationCatalog.PhuQuocIslandScene, SceneManager.GetActiveScene().name, "on the island");
                yield return new WaitForSecondsRealtime(2f);
                dm = DialogueManager.Instance;
                while (dm != null && dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
                map = null;
                for (float t = 0f; t < 15f && (map = Object.FindFirstObjectByType<WorldMapUI>()) == null; t += 0.25f) yield return new WaitForSecondsRealtime(0.25f);
                var hidden = Object.FindObjectsByType<WorldMapUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.NotNull(map, $"Map on the island (inactive maps: {string.Join(", ", hidden.Select(h => h.gameObject.scene.name + "/" + h.name + " active=" + h.gameObject.activeInHierarchy))})");
                Loc.Set(GameLanguage.English);
                map.SetVisible(true);
                yield return new WaitForSecondsRealtime(0.5f);
                AssertDecor(city: false);
                var seaAny = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(r => r.name == "District_Sea");
                var districts = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(r => r.name.StartsWith("District_") || r.name.StartsWith("Place_")).Select(r => r.name + (r.gameObject.activeInHierarchy ? "" : "(off)")).Take(12);
                Assert.NotNull(Named("District_Sea"), $"Sea name (exists: {seaAny != null}, active scene {SceneManager.GetActiveScene().name}; {string.Join(", ", districts)})");
                Assert.AreEqual("Gulf of Thailand", Named("District_Sea").GetComponent<TMP_Text>().text);
                Assert.IsTrue(Named("ScaleBar").GetComponentInChildren<TMP_Text>().text.Contains("km"), "Island scale is in km");
                Assert.GreaterOrEqual(Named("Legend").GetComponentsInChildren<TMP_Text>().Length, 8, "Island legend has many categories");
                Capture("03_island_en");
                Loc.Set(GameLanguage.Russian);
                map.SetVisible(false); yield return new WaitForSecondsRealtime(0.3f);
                map.SetVisible(true); yield return new WaitForSecondsRealtime(0.5f);
                AssertDecor(city: false);
                Capture("04_island_ru");
                map.SetVisible(false);
                yield return new WaitForSecondsRealtime(0.4f);
            }
            finally
            {
                Loc.Set(language);
                if (map != null) map.SetVisible(false);
            }
        }

        private static void AssertDecor(bool city)
        {
            Canvas.ForceUpdateCanvases();
            var window = Named("MapWindow");
            Assert.NotNull(window, "Window");
            Assert.NotNull(Named("YouAreHere"), "Position marker");
            Assert.NotNull(Named("Compass"), "Compass");
            Assert.NotNull(Named("ScaleBar"), "Scale bar");
            var legend = Named("Legend");
            Assert.NotNull(legend, "Legend");
            Assert.NotNull(Named("Legend_YOU"), "Legend: you are here");
            var wc = Corners(window); var lc = Corners(legend); var fc = Corners(Named("MapFrame"));
            Assert.GreaterOrEqual(lc[0].x, wc[0].x - 1f, "Legend inside the window (left)");
            Assert.GreaterOrEqual(lc[0].y, wc[0].y - 1f, "Legend inside the window (bottom)");
            Assert.LessOrEqual(lc[1].y, fc[0].y + 2f, "Legend sits below the map frame");
            var coords = Corners(Named("Coordinates"));
            Assert.GreaterOrEqual(lc[0].y, coords[1].y - 2f, "Legend does not cover the coordinates line");
            foreach (var cell in legend.GetComponentsInChildren<TMP_Text>().Where(t => t.name == "Text"))
            {
                float need = cell.GetPreferredValues(cell.text).x;
                Assert.LessOrEqual(need, cell.rectTransform.rect.width + 1f, "Legend text fits: '" + cell.text + "' needs " + need.ToString("0") + " of " + cell.rectTransform.rect.width.ToString("0"));
            }
            if (city)
            {
                var zones = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(r => r.name.StartsWith("District_")).ToArray();
                Assert.AreEqual(6, zones.Length, "Six districts");
                foreach (var z in zones)
                {
                    var label = z.GetComponentInChildren<TMP_Text>();
                    float need = label.GetPreferredValues(label.text).x;
                    Assert.LessOrEqual(need, z.rect.width + 1f, "District name fits: '" + label.text + "' needs " + need.ToString("0") + " of " + z.rect.width.ToString("0"));
                }
            }
        }

        private static Vector3[] Corners(GameObject g) { var c = new Vector3[4]; ((RectTransform)g.transform).GetWorldCorners(c); return c; }

        private static GameObject Named(string name) =>
            Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Select(r => r.gameObject).FirstOrDefault(g => g.name == name);
        private static void Capture(string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            const int width = 1600, height = 900;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            var rt = new RenderTexture(width, height, 24);
            camera.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Canvas.ForceUpdateCanvases();
            Object.Destroy(tex);
            rt.Release();
        }
    }
}