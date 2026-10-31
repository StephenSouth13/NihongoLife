using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NihongoLife.Core;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The main menu and its pop-ups at desktop (16:9, 16:10, 21:9, 4:3, a short "free" game view) and phone sizes
    /// (landscape with a notch, small; phones are landscape-only per AGENTS.md) in all four UI languages: no text spills out of its box or off screen,
    /// no two texts overlap and no two buttons overlap. With NL_SHOTS_DIR set it also saves screenshots.
    /// </summary>
    public class MenuLayoutPlayModeTests
    {
        public struct Size
        {
            public string Name; public int W, H; public bool Phone; public Rect? Safe;
            public Size(string name, int w, int h, bool phone, Rect? safe = null) { Name = name; W = w; H = h; Phone = phone; Safe = safe; }
        }

        public static readonly Size[] Sizes =
        {
            new Size("desktop_16x9_1920x1080", 1920, 1080, false),
            new Size("desktop_16x10_1920x1200", 1920, 1200, false),
            new Size("desktop_21x9_2560x1080", 2560, 1080, false),
            new Size("desktop_4x3_1024x768", 1024, 768, false),
            new Size("desktop_free_1280x620", 1280, 620, false),
            new Size("desktop_1366x768", 1366, 768, false),
            new Size("phone_landscape_2400x1080", 2400, 1080, true, new Rect(110f, 0f, 2180f, 1080f)),
            new Size("phone_small_1334x750", 1334, 750, true),
        };

        static readonly GameLanguage[] Languages = { GameLanguage.Vietnamese, GameLanguage.English, GameLanguage.Japanese, GameLanguage.Russian };
        static readonly string[] Views = { "main", "mode", "character", "settings", "guide", "about", "auth" };

        [UnityTest]
        public IEnumerator MainMenu_AndPopups_FitEveryScreenAndLanguage()
        {
            LogAssert.ignoreFailingMessages = true;
            var originalLanguage = Loc.Language;
            yield return SceneManager.LoadSceneAsync("01_MainMenu");
            yield return new WaitForSecondsRealtime(2.5f);
            var menu = UnityEngine.Object.FindFirstObjectByType<MainMenuUI>();
            Assert.IsNotNull(menu, "main menu");
            var canvas = menu.GetComponentInParent<Canvas>().rootCanvas;
            var world = Camera.main;
            var uiCam = new GameObject("LayoutProbeCamera").AddComponent<Camera>();
            uiCam.transform.position = new Vector3(0f, -6000f, 0f);
            uiCam.enabled = false;
            uiCam.cullingMask = 1 << UiLayoutProbe.Layer;
            uiCam.nearClipPlane = 0.1f; uiCam.farClipPlane = 50f;
            if (world != null) world.cullingMask &= ~(1 << UiLayoutProbe.Layer);
            string shots = Environment.GetEnvironmentVariable("NL_SHOTS_DIR");
            var issues = new List<string>();
            int audits = 0;
            try
            {
                foreach (var size in Sizes)
                {
                    UiScreen.SafeAreaOverride = size.Safe;
                    UiLayoutProbe.Fit(canvas, uiCam, size.W, size.H, size.Phone);
                    foreach (var language in Languages)
                    {
                        Loc.Set(language);
                        foreach (var view in Views)
                        {
                            CloseAll();
                            yield return null;
                            Open(menu, view);
                            yield return new WaitForSecondsRealtime(0.45f);
                            yield return null;
                            var layer = UiLayoutProbe.TopLayer((RectTransform)canvas.transform);
                            if (view != "main" && layer == (RectTransform)canvas.transform) { issues.Add($"{size.Name} {language} {view}: did not open"); continue; }
                            audits++;
                            foreach (var issue in UiLayoutProbe.Audit((RectTransform)canvas.transform, layer))
                                issues.Add($"{size.Name} {language} {view}: {issue}");
                            bool shoot = !string.IsNullOrEmpty(shots) && (language == GameLanguage.Vietnamese || view == "main" || view == "mode");
                            if (shoot)
                            {
                                Directory.CreateDirectory(shots);
                                File.WriteAllBytes(Path.Combine(shots, $"{size.Name}_{LangCode(language)}_{view}.png"), UiLayoutProbe.Capture(world, uiCam, size.W, size.H));
                            }
                        }
                    }
                }
            }
            finally
            {
                UiLayoutProbe.Release();
                Loc.Set(originalLanguage);
                CloseAll();
            }
            string report = string.Join("\n", issues.Distinct());
            string dir = !string.IsNullOrEmpty(shots) ? shots : Path.Combine(Application.temporaryCachePath, "menu_layout");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "layout_issues.txt"), $"audits: {audits}  issues: {issues.Distinct().Count()}\n{report}\n");
            Debug.Log($"[MenuLayout] {audits} audits, {issues.Distinct().Count()} issues -> {dir}");
            Assert.Greater(audits, Sizes.Length * Languages.Length * 5, "views opened");
            Assert.IsEmpty(issues.Distinct().ToList(), "layout problems:\n" + report);
        }

        static string LangCode(GameLanguage l) => l switch { GameLanguage.English => "en", GameLanguage.Japanese => "ja", GameLanguage.Russian => "ru", _ => "vi" };

        static void CloseAll()
        {
            for (int i = 0; i < 12 && UiModalStack.CloseTop(); i++) { }
        }

        static void Open(MainMenuUI menu, string view)
        {
            const BindingFlags any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            switch (view)
            {
                case "main": return;
                case "mode": typeof(MainMenuUI).GetMethod("OpenModeSelect", any).Invoke(menu, null); return;
                case "character": typeof(MainMenuUI).GetMethod("OpenCharacterSelect", any).Invoke(menu, null); return;
            }
            string type = view switch { "settings" => "SettingsUI", "guide" => "GuidePopup", "about" => "AboutPopup", _ => "AuthUI" };
            var component = menu.GetComponents<MonoBehaviour>().First(c => c.GetType().Name == type);
            component.GetType().GetMethod("Show", any, null, Type.EmptyTypes, null).Invoke(component, null);
        }
    }
}
