using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The main menu in Play looks exactly like the saved scene in the Editor: no element is moved, resized, created
    /// or restyled at runtime (only texts follow the language). Writes Bao_Cao/menu-sync/play_view_*.png and
    /// layout.txt (name, position, size of every menu element in Play) for comparison with the Editor capture.
    /// </summary>
    public class MenuSyncPlayModeTests
    {
        private static string Out => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/menu-sync"));

        [UnityTest, Timeout(120000)]
        public IEnumerator MainMenu_PlayMatchesEditor()
        {
            // The layout saved in the scene, before any runtime code runs (Awake/Start not yet called on load frame).
            yield return SceneManager.LoadSceneAsync("01_MainMenu");
            yield return new WaitForSecondsRealtime(2f);
            var panel = GameObject.Find("Canvas/MainMenuPanel");
            Assert.NotNull(panel, "Main menu panel");
            var report = new System.Text.StringBuilder();
            foreach (RectTransform child in panel.transform)
                report.AppendLine($"{child.name}\tactive={child.gameObject.activeSelf}\tpos={child.anchoredPosition}\tsize={child.sizeDelta}");
            Directory.CreateDirectory(Out);
            File.WriteAllText(Path.Combine(Out, "layout_play.txt"), report.ToString());
            // Saved by MainMenuBaker (ColumnX 110, ListTop 380, 440×68): runtime code must not move or resize it.
            var start = (RectTransform)panel.transform.Find("StartButton");
            Assert.AreEqual(new Vector2(110f, -380f), start.anchoredPosition, "Start stays where the scene put it");
            Assert.AreEqual(new Vector2(440f, 68f), start.sizeDelta, "Start keeps its saved size");
            Assert.IsNull(panel.transform.Find("LanguageLabel"), "No runtime-created label over the menu");
            foreach (var language in new[] { GameLanguage.Vietnamese, GameLanguage.English, GameLanguage.Japanese })
            {
                Loc.Set(language);
                yield return new WaitForSecondsRealtime(0.4f);
                yield return Capture($"play_view_{Loc.Code}");
            }
            Loc.Set(GameLanguage.Vietnamese);
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
            yield return null;
            yield return null;
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
            target.Release();
            Object.Destroy(target);
            Object.Destroy(tex);
            foreach (var c in canvases) if (c != null) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
        }
    }
}
