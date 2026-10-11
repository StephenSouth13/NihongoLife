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
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The HUD portrait and the Tab profile for adult and kid characters at night: the face (eyes) is in the
    /// portrait, the profile keeps the picture's proportions and is bright (studio light, light backdrop) even when
    /// the world is dark. Captures: Bao_Cao/hud-regression (portrait_*, profile_*).
    /// </summary>
    public class PortraitProfilePlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/hud-regression"));

        [UnityTest]
        public IEnumerator PortraitAndProfile_KidsAndAdult_AtNight()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            Object.FindFirstObjectByType<DayNightCycle>()?.SetHour(23f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var dock = Object.FindFirstObjectByType<StatusDock>();
            var portrait = dock.StatusWidget.GetComponentInChildren<HudPortrait>(true);
            var original = PlayableCharacterCatalog.GetSelected();
            var sun = RenderSettings.sun;
            float sunBefore = sun != null ? sun.intensity : 0f;
            try
            {
                foreach (var id in new[] { "kid_haruto", "kid_yui", "student" })
                {
                    int shots = portrait.ShotCount;
                    PlayableCharacterCatalog.ApplyVisual(player.gameObject, PlayableCharacterCatalog.FindById(id));
                    for (float t = 0f; t < 5f && portrait.ShotCount == shots; t += Time.unscaledDeltaTime) yield return null;
                    Assert.Greater(portrait.ShotCount, shots, id + ": new portrait");
                    var pixels = Read(portrait.Texture);
                    File.WriteAllBytes(Path.Combine(Prepare(), $"portrait_{id}.png"), pixels.EncodeToPNG());
                    float brightness = Brightness(pixels);
                    Object.Destroy(pixels);
                    Debug.Log($"[Portrait] {id}: mean brightness {brightness:0.00} at 23:00");
                    Assert.Greater(brightness, 0.3f, id + ": the portrait is lit by the studio light even at night");
                    if (sun != null) Assert.Less(Mathf.Abs(sun.intensity - sunBefore), 0.05f, "The world's sun is restored after the photo (the cycle keeps moving it slightly)");

                    // Tab profile.
                    dock.CharacterRoot.SetActive(true);
                    yield return new WaitForSecondsRealtime(1f);
                    var preview = dock.CharacterRoot.GetComponentInChildren<ProfilePreview>();
                    var image = preview.GetComponent<RawImage>();
                    var rect = ((RectTransform)image.transform).rect;
                    Assert.AreEqual(rect.width / rect.height, image.texture.width / (float)image.texture.height, 0.02f, id + ": profile picture keeps its proportions");
                    var profile = Read(image.texture);
                    float profileBrightness = Brightness(profile);
                    File.WriteAllBytes(Path.Combine(Prepare(), $"profile_{id}.png"), profile.EncodeToPNG());
                    Object.Destroy(profile);
                    Debug.Log($"[Profile] {id}: mean brightness {profileBrightness:0.00}");
                    Assert.Greater(profileBrightness, 0.55f, id + ": bright profile backdrop");
                    CaptureScreen($"profile_screen_{id}");
                    dock.CharacterRoot.SetActive(false);
                    yield return null;
                }
            }
            finally
            {
                PlayableCharacterCatalog.ApplyVisual(player.gameObject, original);
            }
        }

        private static string Prepare() { Directory.CreateDirectory(Folder); return Folder; }

        private static Texture2D Read(Texture texture)
        {
            var rt = RenderTexture.GetTemporary(texture.width, texture.height, 0);
            Graphics.Blit(texture, rt);
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            copy.Apply();
            RenderTexture.active = old;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        private static float Brightness(Texture2D texture) => texture.GetPixels().Average(c => c.grayscale);

        private static void CaptureScreen(string name)
        {
            var cam = Camera.main;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = cam; canvases[i].planeDistance = cam.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            var rt = new RenderTexture(1600, 900, 24);
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Prepare(), name + ".jpg"), tex.EncodeToJPG(88));
            RenderTexture.active = old;
            cam.targetTexture = null;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(tex);
        }
    }
}
