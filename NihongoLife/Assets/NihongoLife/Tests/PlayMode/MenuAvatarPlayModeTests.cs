using System.Collections;
using System.IO;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>The main menu shows the chosen character as a living 3D figure (moves over time). Captures: Bao_Cao/menu-avatar/.</summary>
    public class MenuAvatarPlayModeTests
    {
        private static string Out => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/menu-avatar"));

        [UnityTest, Timeout(120000)]
        public IEnumerator MainMenu_ShowsALivingCharacter()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync("01_MainMenu");
            yield return new WaitForSecondsRealtime(2f);
            var stage = Object.FindFirstObjectByType<MenuAvatarStage>();
            Assert.NotNull(stage, "the menu has its avatar stage");
            Assert.IsTrue(stage.HasFigure, "a character model stands in the menu");
            var camera = Camera.main;
            Assert.NotNull(camera);
            var vp = camera.WorldToViewportPoint(stage.Figure.position + Vector3.up * 0.9f);
            Assert.That(vp.x, Is.InRange(0.55f, 0.95f), "on the right of the buttons");
            Assert.That(vp.y, Is.InRange(0.2f, 0.8f), "inside the screen vertically");
            var a = Grab(camera, "01_menu_avatar_a");
            yield return new WaitForSecondsRealtime(1.6f);
            var b = Grab(camera, "02_menu_avatar_b");
            // Right-hand third of the screen: the figure is there and it moved (pose / turn) between the two frames.
            int w = a.width, h = a.height, x0 = (int)(w * 0.55f), moved = 0, total = 0;
            float lumMin = 1f, lumMax = 0f;
            for (int y = 0; y < h; y += 3)
                for (int x = x0; x < w; x += 3)
                {
                    var ca = a.GetPixel(x, y); var cb = b.GetPixel(x, y);
                    if (Mathf.Abs(ca.grayscale - cb.grayscale) > 0.04f) moved++;
                    lumMin = Mathf.Min(lumMin, ca.grayscale); lumMax = Mathf.Max(lumMax, ca.grayscale);
                    total++;
                }
            File.WriteAllText(Path.Combine(Out, "motion.txt"), $"moved pixels {moved}/{total}; luminance range {lumMin:0.00}-{lumMax:0.00}; shown={stage.ShownId}");
            Assert.Greater(moved, 40, "the character moves between frames");
            Assert.Greater(lumMax - lumMin, 0.25f, "the character is visible, not a flat background");
        }

        private static Texture2D Grab(Camera camera, string name)
        {
            const int width = 1600, height = 900;
            var rt = new RenderTexture(width, height, 24);
            var previous = camera.targetTexture;
            camera.targetTexture = rt;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            for (int i = 0; i < canvases.Length; i++) { modes[i] = canvases[i].renderMode; if (canvases[i].isRootCanvas && modes[i] == RenderMode.ScreenSpaceOverlay) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f; } }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            Directory.CreateDirectory(Out);
            File.WriteAllBytes(Path.Combine(Out, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = previous;
            RenderTexture.active = null;
            for (int i = 0; i < canvases.Length; i++) if (canvases[i] != null && modes[i] == RenderMode.ScreenSpaceOverlay && canvases[i].isRootCanvas) { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = null; }
            rt.Release();
            return tex;
        }
    }
}
