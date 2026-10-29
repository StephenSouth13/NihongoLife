using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Progression;
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
    /// The career screen groups careers by industry and shows each career's person in uniform, animated: cards fit, every avatar is
    /// drawn (not blank), a hovered avatar works (moves) and an idle one rests, the stage is released on close, languages follow.
    /// Captures: Bao_Cao/careers-ui.
    /// </summary>
    public class CareerAvatarPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/careers-ui"));

        [UnityTest]
        public IEnumerator CareerScreen_IndustriesAvatarsAndAnimation()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var dm = DialogueManager.Instance;
            while (dm != null && dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
            var language = Loc.Language;
            string careerSnapshot = CareerService.State != null ? JsonUtility.ToJson(CareerService.State) : null; // choosing a career here must not leak into other tests
            try
            {
                Loc.Set(GameLanguage.English);
                CareerIndustries.Reset();
                CareerService.Bypass = true;
                CareerUI.Open(CareerUI.Tab.Careers);
                for (int i = 0; i < 6; i++) yield return null;
                var catalog = CareerService.Catalog;
                var stage = CareerAvatarStage.Instance;

                // Industry headers and one card with a picture per career.
                foreach (var (industry, members) in CareerIndustries.Group(catalog.careers))
                {
                    Assert.NotNull(Named("IndustryHeader_" + industry.id), "Header " + industry.id);
                    Assert.NotNull(Named("Industry_" + industry.id), "Grid " + industry.id);
                }
                Assert.AreEqual(catalog.careers.Length, stage.Avatars.Count, "One avatar per career");
                foreach (var career in catalog.careers)
                {
                    var card = Named("Career_" + career.id);
                    Assert.NotNull(card, "Card " + career.id);
                    var raw = card.GetComponentInChildren<RawImage>();
                    Assert.NotNull(raw, career.id + " picture");
                    Assert.NotNull(raw.texture, career.id + " texture");
                    AssertFits(card, career.id);
                }

                // Every avatar draws something (not blank, not everything) and the person is in uniform colours of its own.
                var looks = new List<string>();
                foreach (var a in stage.Avatars)
                {
                    stage.Step(0.2f);
                    stage.RenderAvatar(a);
                    var (coverage, mean) = Read(a.Texture);
                    Assert.Greater(coverage, 0.06f, a.careerId + " is drawn (coverage " + coverage.ToString("0.00") + ")");
                    Assert.Less(coverage, 0.85f, a.careerId + " does not fill the whole picture");
                    looks.Add(a.careerId + " " + a.variantId + " coverage=" + coverage.ToString("0.00") + " mean=" + mean.ToString("0.00"));
                }
                Assert.AreEqual(catalog.careers.Length, looks.Select(l => l.Split(' ')[1]).Distinct().Count(), "Different people");

                // A hovered person works: it moves between frames; a resting one stands (breathing only) and its gesture stops.
                var hot = stage.Avatars.First(a => a.careerId == "retail");
                hot.Hot = true;
                float moved = 0f;
                Color32[] last = null;
                for (int i = 0; i < 90; i++)
                {
                    stage.Step(1f / 30f);
                    if (i % 6 == 5)
                    {
                        stage.RenderAvatar(hot);
                        var px = Pixels(hot.Texture);
                        if (last != null) moved = Mathf.Max(moved, Diff(last, px));
                        last = px;
                    }
                }
                Assert.IsTrue(hot.Working && hot.Layer.IsPlaying, "Hot avatar works");
                Assert.IsTrue(hot.Layer.Current == hot.gesture || hot.Layer.Current == hot.alt, "Plays its own gesture, got " + hot.Layer.Current);
                Assert.Greater(moved, 0.004f, "The working person moves (frame difference " + moved.ToString("0.0000") + ")");
                hot.Hot = false;
                hot.Clock = 0f; // start of the idle part of the cycle
                for (int i = 0; i < 90; i++) stage.Step(1f / 60f);
                Assert.IsFalse(hot.Working, "Resting after the hover");
                Assert.IsFalse(hot.Layer.IsPlaying, "Gesture stopped");
                Assert.Less(hot.Layer.Influence, 0.02f, "Gesture influence fades");

                // The live loop draws only what is on screen and at a capped rate.
                yield return new WaitForSecondsRealtime(1.2f);
                Assert.LessOrEqual(stage.RendersLastFrame, 3);
                int visible = stage.Avatars.Count(CareerAvatarStage.IsVisible);
                Assert.Greater(visible, 0, "Some cards are in view");
                int rendersBefore = stage.Avatars.Sum(a => a.Renders);
                yield return new WaitForSecondsRealtime(1f);
                int rendered = stage.Avatars.Sum(a => a.Renders) - rendersBefore;
                Assert.Greater(rendered, 0, "Visible avatars keep rendering");
                Assert.LessOrEqual(rendered, Mathf.CeilToInt(visible * 22f * 1.6f), "At most about 20 draws a second per visible avatar");
                Capture("01_careers_top");

                // Scroll to the bottom: other cards in view now.
                var scroll = Object.FindFirstObjectByType<CareerUI>().GetComponentInChildren<ScrollRect>(true);
                scroll.verticalNormalizedPosition = 0f;
                for (int i = 0; i < 10; i++) yield return null;
                stage.Avatars.First(a => a.careerId == "medical").Hot = true;
                yield return new WaitForSecondsRealtime(1.0f);
                Capture("02_careers_bottom");

                // Frame time with the screen open (informational, written to the report).
                float sum = 0f, worst = 0f; int frames = 0;
                for (int i = 0; i < 90; i++) { yield return null; float dt = Time.unscaledDeltaTime; sum += dt; worst = Mathf.Max(worst, dt); frames++; }
                File.WriteAllText(Path.Combine(Folder, "_perf.txt"), "careers screen open, " + stage.Avatars.Count + " avatars, " + visible + " visible\n" +
                    "average frame " + (sum / frames * 1000f).ToString("0.0") + " ms, worst " + (worst * 1000f).ToString("0.0") + " ms over " + frames + " frames\n" + string.Join("\n", looks) + "\n");

                // The overview shows the person of the current career.
                Assert.IsNull(CareerService.Choose("food"));
                CareerUI.Show(CareerUI.Tab.Overview);
                for (int i = 0; i < 6; i++) yield return null;
                Assert.NotNull(Named("Avatar_food"), "Overview picture");
                yield return new WaitForSecondsRealtime(1.2f);
                Capture("03_overview_food");

                // Languages: industry names change with the language.
                CareerUI.Show(CareerUI.Tab.Careers);
                var names = new Dictionary<GameLanguage, string>();
                foreach (var lang in new[] { GameLanguage.Vietnamese, GameLanguage.English, GameLanguage.Japanese, GameLanguage.Russian })
                {
                    Loc.Set(lang);
                    for (int i = 0; i < 4; i++) yield return null;
                    var header = Named("IndustryHeader_foodservice");
                    Assert.NotNull(header, "Header in " + lang);
                    var text = header.transform.Find("Name").GetComponent<TMP_Text>().text;
                    names[lang] = text;
                    foreach (var career in catalog.careers) AssertFits(Named("Career_" + career.id), career.id + " in " + lang);
                    if (lang == GameLanguage.Russian || lang == GameLanguage.Japanese) { yield return new WaitForSecondsRealtime(0.6f); Capture("04_careers_" + Loc.Code); }
                }
                Assert.AreEqual(4, names.Values.Distinct().Count(), "Four languages, four names: " + string.Join(" | ", names.Values));

                // Closing releases the stage.
                CareerUI.Close();
                yield return null;
                Assert.AreEqual(0, CareerAvatarStage.Instance.Avatars.Count, "Avatars released on close");
            }
            finally
            {
                Loc.Set(language);
                if (careerSnapshot != null && CareerService.State != null) JsonUtility.FromJsonOverwrite(careerSnapshot, CareerService.State);
                CareerService.Bypass = false;
                CareerUI.Close();
                CareerAvatarStage.ClearIfAny();
            }
        }

        private static GameObject Named(string name) =>
            Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Select(r => r.gameObject).FirstOrDefault(g => g.name == name);

        private static void AssertFits(GameObject card, string what)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)card.transform);
            var c = new Vector3[4]; ((RectTransform)card.transform).GetWorldCorners(c);
            float bottom = c[0].y, top = c[1].y, left = c[0].x, right = c[2].x;
            foreach (var t in card.GetComponentsInChildren<TMP_Text>())
            {
                var w = new Vector3[4]; t.rectTransform.GetWorldCorners(w);
                Assert.GreaterOrEqual(w[0].y, bottom - 1.5f, what + ": '" + t.name + "' stays inside the card (bottom)");
                Assert.LessOrEqual(w[2].x, right + 1.5f, what + ": '" + t.name + "' stays inside the card (right)");
                Assert.GreaterOrEqual(w[0].x, left - 1.5f, what + ": '" + t.name + "' stays inside the card (left)");
                Assert.LessOrEqual(w[1].y, top + 1.5f, what + ": '" + t.name + "' stays inside the card (top)");
            }
        }

        private static Color32[] Pixels(RenderTexture rt)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            var px = tex.GetPixels32();
            Object.Destroy(tex);
            return px;
        }

        /// <summary>Share of pixels the person covers (alpha) and the mean brightness of those pixels.</summary>
        private static (float coverage, float mean) Read(RenderTexture rt)
        {
            var px = Pixels(rt);
            int n = 0; float sum = 0f;
            foreach (var p in px) if (p.a > 20) { n++; sum += (p.r + p.g + p.b) / (3f * 255f); }
            return (n / (float)px.Length, n > 0 ? sum / n : 0f);
        }

        private static float Diff(Color32[] a, Color32[] b)
        {
            int changed = 0;
            for (int i = 0; i < a.Length; i++)
                if (Mathf.Abs(a[i].r - b[i].r) + Mathf.Abs(a[i].g - b[i].g) + Mathf.Abs(a[i].b - b[i].b) + Mathf.Abs(a[i].a - b[i].a) > 24) changed++;
            return changed / (float)a.Length;
        }

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