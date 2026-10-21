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
    /// <summary>
    /// The main menu's "About us" credits the three founders of The Next Generation (and nobody else), with their
    /// roles in the interface language, and the gallery opens on the team's award photo.
    /// Captures: Bao_Cao/about-team.
    /// </summary>
    public class AboutTeamPlayModeTests
    {
        private static readonly string[] Founders = { "Quách Thành Long", "Nguyễn Lê Gia Huy", "Lương Nguyễn Tấn Dũng" };
        private GameLanguage _savedLanguage;

        [TearDown]
        public void TearDown() => Loc.Set(_savedLanguage);

        [UnityTest]
        public IEnumerator About_ShowsTheThreeFounders()
        {
            LogAssert.ignoreFailingMessages = true;
            _savedLanguage = Loc.Language;
            Loc.Set(GameLanguage.Vietnamese);
            SceneManager.LoadScene("01_MainMenu");
            AboutPopup about = null;
            for (float t = 0f; t < 10f && about == null; t += Time.unscaledDeltaTime) { about = Object.FindFirstObjectByType<AboutPopup>(); yield return null; }
            Assert.NotNull(about, "The main menu has the About popup.");
            about.Show();
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.IsTrue(about.IsOpen);

            var texts = about.GetComponentsInChildren<TextMeshProUGUI>(true).Select(t => t.text ?? "").ToArray();
            foreach (string name in Founders) Assert.IsTrue(texts.Contains(name), name + " is credited.");
            Assert.IsFalse(texts.Any(t => t.Contains("Thuận")), "Only the three founders are credited.");
            Assert.IsTrue(texts.Any(t => t.Contains("Nhân vật, animation & gameplay")), "Huy and Dũng: characters, animation, gameplay.");
            Assert.IsTrue(texts.Any(t => t.Contains("Về chúng tôi")), "The popup is titled About us.");
            Assert.IsTrue(texts.Any(t => t.Contains("Giải Triển vọng")), "The gallery opens on the team's award photo.");
            Capture("01_about_vi");

            Loc.Set(GameLanguage.English);
            yield return new WaitForSecondsRealtime(0.4f);
            texts = about.GetComponentsInChildren<TextMeshProUGUI>(true).Select(t => t.text ?? "").ToArray();
            Assert.IsTrue(texts.Any(t => t.Contains("Characters, animation & gameplay")));
            Capture("02_about_en");
            about.Hide();
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
            camera.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/about-team"));
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Object.Destroy(tex); rt.Release();
        }
    }
}
