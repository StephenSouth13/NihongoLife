using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NihongoLife.Core;
using NihongoLife.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Russian interface: the main-menu Russian button works and is on screen, the HUD place name follows the language,
    /// and every screen opened in Russian shows no Vietnamese-only and no untranslated English interface text, only glyphs
    /// the font has. RuTable records every English string it could not translate (Misses) while the screens are opened.
    /// Captures + text dumps: Bao_Cao/russian-ui/.
    /// </summary>
    public class RussianScreensPlayModeTests
    {
        private static string Out => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/russian-ui"));
        private static bool Cyr(char c) => c >= '\u0400' && c <= '\u04FF';
        // Letters that exist only in Vietnamese (tone marks and ă â ê ô ơ ư đ): such text on a Russian screen is a leak.
        private static bool VietnameseOnly(char c) => (c >= '\u1EA0' && c <= '\u1EF9') || "ăâêôơưđĂÂÊÔƠƯĐ".IndexOf(c) >= 0;
        private static readonly Regex Word = new Regex("[A-Za-z]{3,}", RegexOptions.Compiled);
        // Keyboard key names shown by the Input System in the rebinding list (Left Shift, Right Control ...) are not interface text.
        private static readonly Regex KeyName = new Regex("^(Left|Right) (Shift|Control|Alt)$|^(Left|Right) Mouse Button$", RegexOptions.Compiled);
        private static readonly Regex Tags = new Regex("<[^>]+>", RegexOptions.Compiled);

        [UnityTest, Timeout(120000)]
        public IEnumerator MainMenu_RussianButton_SwitchesLanguage()
        {
            Directory.CreateDirectory(Out);
            Loc.Set(GameLanguage.Vietnamese);
            // The batch-mode Game view is 640x480 (phone layout); measure the desktop layout players get at 1920x1080.
            yield return DesktopResolution();
            yield return SceneManager.LoadSceneAsync("01_MainMenu");
            yield return new WaitForSecondsRealtime(2f);
            try
            {
                var ru = FindByName("LanguageRussianButton");
                Assert.NotNull(ru, "the main menu has the Russian language button");
                Action measure = () =>
                {
                var rect = (RectTransform)ru.transform;
                var canvasRect = (RectTransform)rect.GetComponentInParent<Canvas>().rootCanvas.transform;
                var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                // Everything in canvas space: independent of the batch-mode Game view size and the canvas scale.
                Vector2 ToCanvas(Vector3 world) => canvasRect.InverseTransformPoint(world);
                var screen = canvasRect.rect;
                var scaler = canvasRect.GetComponent<CanvasScaler>();
                var layout = new System.Text.StringBuilder("canvas " + screen + " mode=" + canvasRect.GetComponent<Canvas>().renderMode + " scaler=" + (scaler != null ? scaler.uiScaleMode + " ref=" + scaler.referenceResolution + " screen=" + Screen.width + "x" + Screen.height : "none") + "\n");
                foreach (var name in new[] { "LanguageVietnameseButton", "LanguageEnglishButton", "LanguageJapaneseButton", "LanguageRussianButton" })
                {
                    var o = FindByName(name);
                    if (o == null) { layout.AppendLine(name + " missing"); continue; }
                    var a = new Vector3[4]; ((RectTransform)o.transform).GetWorldCorners(a);
                    layout.AppendLine(name + " " + ToCanvas(a[0]) + " " + ToCanvas(a[2]) + " active=" + o.activeInHierarchy);
                }
                foreach (var n in new[] { "TopCluster", "MainMenuPanel", "LanguageBar" })
                {
                    var o = FindByName(n); if (o == null) continue;
                    var a = new Vector3[4]; ((RectTransform)o.transform).GetWorldCorners(a);
                    layout.AppendLine(n + " " + ToCanvas(a[0]) + " " + ToCanvas(a[2]));
                }
                File.WriteAllText(Path.Combine(Out, "menu_language_layout.txt"), layout.ToString());
                // The top bar (about / login / languages) is pinned to the top-right corner by MenuTopBar (a missing script left it in the middle).
                var cluster = FindByName("TopCluster");
                Assert.NotNull(cluster, "the menu has its top bar");
                Assert.NotNull(cluster.GetComponent<MenuTopBar>(), "the top bar has its MenuTopBar component (not a missing script)");
                var cb = new Vector3[4]; ((RectTransform)cluster.transform).GetWorldCorners(cb);
                Assert.Greater(ToCanvas(cb[0]).y, screen.center.y, "the top bar sits in the upper half of the screen\n" + layout);
                Assert.Greater(ToCanvas(cb[2]).x, screen.xMax - 60f, "the top bar reaches the right edge\n" + layout);
                foreach (var c in corners) Assert.IsTrue(Rect.MinMaxRect(ToCanvas(cb[0]).x, ToCanvas(cb[0]).y, ToCanvas(cb[2]).x, ToCanvas(cb[2]).y).Contains(ToCanvas(c)), "the Russian button is inside the top bar\n" + layout);
                foreach (var c in corners) Assert.IsTrue(screen.Contains(ToCanvas(c)), "the Russian button is fully on screen\n" + layout);
                foreach (var other in new[] { "LanguageVietnameseButton", "LanguageEnglishButton", "LanguageJapaneseButton" })
                {
                    var o = FindByName(other);
                    if (o == null) continue;
                    var a = new Vector3[4]; ((RectTransform)o.transform).GetWorldCorners(a);
                    var ob = Rect.MinMaxRect(ToCanvas(a[0]).x, ToCanvas(a[0]).y, ToCanvas(a[2]).x, ToCanvas(a[2]).y); var rb = Rect.MinMaxRect(ToCanvas(corners[0]).x, ToCanvas(corners[0]).y, ToCanvas(corners[2]).x, ToCanvas(corners[2]).y);
                    Assert.IsFalse(ob.Overlaps(rb), other + " does not overlap the Russian button\n" + layout);
                }
                };
                yield return Capture("03a_main_menu_vi", measure);
                ru.GetComponent<Button>().onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.6f);
                Assert.AreEqual(GameLanguage.Russian, Loc.Language, "pressing the button selects Russian");
                var texts = UiTexts();
                File.WriteAllText(Path.Combine(Out, "menu_texts.txt"), string.Join("\n", texts.Select(t => t.name + ": " + t.text.Replace("\n", " "))));
                Assert.GreaterOrEqual(texts.Count(t => t.text.Any(Cyr)), 5, "the menu shows Russian text");
                // The Vietnamese language button names its own language ("Tiếng Việt") in every interface language.
                var vietnameseButton = FindByName("LanguageVietnameseButton");
                foreach (var t in texts.Where(t => vietnameseButton == null || !t.transform.IsChildOf(vietnameseButton.transform))) Assert.IsFalse(t.text.Any(VietnameseOnly), "no Vietnamese text left in the Russian menu: " + t.name + " = " + t.text);
                foreach (var t in texts) foreach (char c in t.text) if (Cyr(c)) Assert.IsTrue(t.font.HasCharacter(c, true, true), "glyph U+" + ((int)c).ToString("X4") + " in " + t.name);
                yield return Capture("03_main_menu_ru");
                Loc.Set(GameLanguage.English);
                yield return new WaitForSecondsRealtime(0.4f);
                Assert.NotNull(FindByName("LanguageRussianButton"), "the button stays in other languages");
            }
            finally { Loc.Set(GameLanguage.Vietnamese); }
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Hud_PlaceName_And_Screens_FollowRussian()
        {
            Directory.CreateDirectory(Out);
            Loc.Set(GameLanguage.Vietnamese);
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(3f);
            var report = new System.Text.StringBuilder();
            var leaks = new List<string>();
            try
            {
                string Place() => Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(t => t.gameObject.name == "Place" && t.gameObject.activeInHierarchy).Select(t => t.text).FirstOrDefault() ?? "";
                string vi = Place();
                RuTable.ClearMisses();
                Loc.Set(GameLanguage.Russian);
                yield return new WaitForSecondsRealtime(0.5f);
                string ru = Place();
                report.AppendLine("HUD place vi: " + vi); report.AppendLine("HUD place ru: " + ru);
                Assert.IsTrue(ru.Any(Cyr), "the HUD place name changes to Russian at once when the language changes (was: " + vi + " / now: " + ru + ")");
                Assert.AreNotEqual(vi, ru);
                var hud = Object.FindFirstObjectByType<HUDUI>();
                Assert.NotNull(hud);
                var chat = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Placeholder" && t.text.Length > 0 && t.transform.parent != null && t.transform.parent.name.ToLower().Contains("input"));
                if (chat != null) { report.AppendLine("chat placeholder: " + chat.text); Assert.IsTrue(chat.text.Any(Cyr), "the chat hint is Russian"); }
                yield return Capture("04_city_hud_ru_place");
                Scan("hud", report, leaks);

                var steps = new List<(string name, Action open, Action close)>();
                foreach (HandbookUI.Chapter chapter in Enum.GetValues(typeof(HandbookUI.Chapter)))
                {
                    var ch = chapter;
                    // The dictionary keeps the last search (another test may have looked up an English word): start it clean, ja → ru.
                    steps.Add(("handbook_" + ch, () => { if (ch == HandbookUI.Chapter.Dictionary) HandbookUI.DictionarySearch("", "ja", "ru"); else HandbookUI.Open(ch); }, () => HandbookUI.Toggle()));
                }
                steps.Add(("tasks", () => TaskJournalUI.Open(), () => TaskJournalUI.Toggle()));
                steps.Add(("notebook", () => NotebookUI.Show(), () => NotebookUI.Toggle()));
                var map = Object.FindFirstObjectByType<WorldMapUI>();
                if (map != null) steps.Add(("worldmap", () => map.SetVisible(true), () => map.SetVisible(false)));
                var settings = Object.FindFirstObjectByType<SettingsUI>();
                if (settings != null) steps.Add(("settings", () => settings.Show(), () => settings.Hide()));
                foreach (var popup in new MenuPopupBase[] { hud.GetComponent<ExamCenterPopup>(), hud.GetComponent<StudyHubPopup>() })
                {
                    if (popup == null) continue;
                    var p = popup;
                    steps.Add((p.GetType().Name, () => p.Show(), () => p.Hide()));
                }
                int shots = 0;
                foreach (var s in steps)
                {
                    s.open(); yield return new WaitForSecondsRealtime(0.8f);
                    Scan(s.name, report, leaks);
                    yield return Capture("05_" + s.name + "_ru"); shots++;
                    s.close(); yield return new WaitForSecondsRealtime(0.4f);
                }
                Assert.GreaterOrEqual(shots, 6);

                var misses = RuTable.Misses.OrderBy(m => m, StringComparer.Ordinal).ToList();
                File.WriteAllLines(Path.Combine(Out, "runtime_misses.txt"), misses.Select(m => m.Replace("\n", "\\n")));
                report.AppendLine("RuTable misses while opening the screens: " + misses.Count);
                File.WriteAllText(Path.Combine(Out, "screens_texts.txt"), report.ToString());
                File.WriteAllLines(Path.Combine(Out, "screens_leaks.txt"), leaks);
                Assert.AreEqual(0, leaks.Count, "Vietnamese or English interface text left on Russian screens:\n" + string.Join("\n", leaks.Take(30)) + "\n(see screens_leaks.txt)");
            }
            finally
            {
                File.WriteAllText(Path.Combine(Out, "screens_texts.txt"), report.ToString());
                File.WriteAllLines(Path.Combine(Out, "screens_leaks.txt"), leaks);
                Loc.Set(GameLanguage.Vietnamese);
            }
        }

        /// <summary>The interface texts now on screen: not in the 3D world (signs, nameplates), not empty.</summary>
        private static List<TMP_Text> UiTexts() => Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(t => t.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(t.text) && t.canvas != null && t.canvas.rootCanvas.renderMode != RenderMode.WorldSpace).ToList();

        private static void Scan(string screen, System.Text.StringBuilder report, List<string> leaks)
        {
            var texts = UiTexts();
            report.AppendLine("== " + screen + " (" + texts.Count + " texts)");
            foreach (var t in texts)
            {
                string plain = Tags.Replace(t.text, " ").Replace("\n", " ").Trim();
                bool vi = plain.Any(VietnameseOnly);
                bool en = !KeyName.IsMatch(plain) && !plain.Any(Cyr) && !plain.Any(c => c >= '\u3040') && Word.Matches(plain).Count >= 2;
                report.AppendLine((vi ? "VI?  " : en ? "EN?  " : plain.Any(Cyr) ? "RU   " : "--   ") + t.name + ": " + plain);
                if (vi || en) leaks.Add(screen + " | " + t.name + ": " + plain);
                foreach (char c in t.text) if (Cyr(c)) Assert.IsTrue(t.font.HasCharacter(c, true, true), screen + ": glyph U+" + ((int)c).ToString("X4") + " in " + t.name);
            }
        }

        private static GameObject FindByName(string name) =>
            Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.name == name && t.gameObject.scene.IsValid()).Select(t => t.gameObject).FirstOrDefault();

        private static IEnumerator DesktopResolution()
        {
#if UNITY_EDITOR
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "desktop 16:9");
#endif
            yield return null; yield return null; yield return null;
        }

        private static IEnumerator Capture(string name, Action beforeRender = null)
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
            Exception failure = null;
            try { beforeRender?.Invoke(); } catch (Exception e) { failure = e; }
            camera.Render();
            RenderTexture.active = target;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(Out, name + ".png"), tex.EncodeToPNG());
            RenderTexture.active = old;
            camera.targetTexture = null;
            target.Release(); Object.Destroy(target); Object.Destroy(tex);
            foreach (var c in canvases) if (c != null) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            if (failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}