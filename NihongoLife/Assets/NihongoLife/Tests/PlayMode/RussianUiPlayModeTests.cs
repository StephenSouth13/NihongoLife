using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>Russian as the interface language: Settings and the HUD are drawn in Cyrillic and every glyph exists. Captures: Bao_Cao/russian-ui/.</summary>
    public class RussianUiPlayModeTests
    {
        private static string Out => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/russian-ui"));
        private static bool Cyr(string s) => !string.IsNullOrEmpty(s) && s.Any(c => c >= '\u0400' && c <= '\u04FF');

        [UnityTest, Timeout(180000)]
        public IEnumerator Settings_And_Hud_AreRussian()
        {
            Directory.CreateDirectory(Out);
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var settings = Object.FindFirstObjectByType<SettingsUI>();
            Assert.NotNull(settings);
            try
            {
                Loc.Set(GameLanguage.Russian);
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Capture("01_city_hud_ru");
                settings.Show();
                yield return new WaitForSecondsRealtime(0.8f);
                var texts = settings.GetComponentsInChildren<TMP_Text>(false).Where(t => t.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(t.text)).ToList();
                int cyrillic = texts.Count(t => Cyr(t.text));
                var report = new System.Text.StringBuilder();
                foreach (var t in texts) report.AppendLine((Cyr(t.text) ? "RU  " : "--  ") + t.text.Replace("\n", " "));
                File.WriteAllText(Path.Combine(Out, "settings_texts.txt"), report.ToString());
                Assert.GreaterOrEqual(cyrillic, 6, "the Settings screen shows Russian text (see settings_texts.txt)");
                foreach (var t in texts)
                    foreach (char c in t.text)
                        if (c >= '\u0400' && c <= '\u04FF')
                            Assert.IsTrue(t.font.HasCharacter(c, true, true), "glyph U+" + ((int)c).ToString("X4") + " in " + t.name);
                yield return Capture("02_settings_ru");
                settings.Hide();
            }
            finally { Loc.Set(GameLanguage.Vietnamese); }
        }

        private static IEnumerator Capture(string name)
        {
            var camera = Camera.main;
            if (camera == null) yield break;
            const int width = 1600, height = 900;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            yield return null; yield return null;
            var old = RenderTexture.active;
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(Out, name + ".png"), tex.EncodeToPNG());
            RenderTexture.active = old;
            camera.targetTexture = null;
            target.Release(); Object.Destroy(target); Object.Destroy(tex);
            foreach (var c in canvases) if (c != null) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
        }
    }
}
