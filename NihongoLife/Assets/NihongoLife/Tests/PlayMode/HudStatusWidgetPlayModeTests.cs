using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
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
    /// The player status widget: a portrait photographed from the real player model (re-shot only when the model
    /// changes), the level badge, name/place, wallet and five ring gauges bound to PlayerStatus. Captures at 1080p and
    /// 768p (full screen and a 2× crop of the widget): Bao_Cao/hud-regression.
    /// </summary>
    public class HudStatusWidgetPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/hud-regression"));

        [UnityTest]
        public IEnumerator StatusWidget_RealPortrait_RingsFromStatus_Resolutions()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || PlayerStatus.Instance == null || t < 2f); t += Time.unscaledDeltaTime) yield return null;
            // The arrival dialogue opens on load and the dock steps aside for dialogues: close it first.
            var dm = NihongoLife.Dialogue.DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            yield return new WaitForSecondsRealtime(0.6f);
            var dock = Object.FindFirstObjectByType<StatusDock>();
            Assert.NotNull(dock?.StatusWidget, "Status widget");
            Assert.Greater(dock.StatusWidget.GetComponentInParent<CanvasGroup>().alpha, 0.9f, $"The widget is shown during free play (dialogue open: {NihongoLife.Dialogue.DialogueManager.Instance?.IsOpen}, immersive: {UiModalStack.ImmersiveOpen}, top: {UiModalStack.TopName})");
            var portrait = dock.StatusWidget.GetComponentInChildren<HudPortrait>(true);
            Assert.NotNull(portrait, "Portrait component");
            for (float t = 0f; t < 5f && !portrait.HasPortrait; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(portrait.HasPortrait, "The portrait is photographed from the player model");
            Assert.IsFalse(dock.StatusWidget.GetComponentsInChildren<TextMeshProUGUI>(true).Any(t => t.text == "学"), "The 学 placeholder is gone");

            // The photo shows the model, not an empty backdrop: the centre differs from the corners.
            var pixels = Read(portrait.Texture);
            Color corner = pixels.GetPixel(4, 4), centre = pixels.GetPixel(pixels.width / 2, pixels.height / 2);
            float difference = Mathf.Abs(corner.r - centre.r) + Mathf.Abs(corner.g - centre.g) + Mathf.Abs(corner.b - centre.b);
            Assert.Greater(difference, 0.08f, "The centre of the portrait is the character's face");
            File.WriteAllBytes(Path.Combine(Prepare(), "portrait_raw.png"), pixels.EncodeToPNG());
            Object.Destroy(pixels);

            // No per-frame cost: the stage camera does not render again while the model stays the same.
            int shots = portrait.ShotCount;
            float withHud = 0f;
            for (int i = 0; i < 120; i++) { yield return null; withHud += Time.unscaledDeltaTime; }
            Assert.AreEqual(shots, portrait.ShotCount, "The portrait is not re-rendered every frame");

            // Gauges follow PlayerStatus (no hardcoded values).
            var status = PlayerStatus.Instance;
            yield return new WaitForSecondsRealtime(0.7f);
            var health = dock.StatusWidget.Find("Column/Needs/Need_health");
            Assert.NotNull(health, "Health gauge");
            int expected = Mathf.RoundToInt(Mathf.Clamp01(status.CurrentHealth / Mathf.Max(1f, status.MaxHealth)) * 100f);
            Assert.AreEqual($"{expected}%", health.Find("Value").GetComponent<TextMeshProUGUI>().text);
            Assert.AreEqual(5, dock.StatusWidget.Find("Column/Needs").childCount, "Five needs");
            Assert.IsFalse(dock.StatusWidget.GetComponentsInChildren<TextMeshProUGUI>(true).Any(t => t.text == "Thể lực" || t.text == "Khát"), "No labels under the gauges");
            StringAssert.Contains("¥", dock.StatusWidget.GetComponentsInChildren<TextMeshProUGUI>(true).First(t => t.name == "Wallet").text);

            foreach (var (w, h) in new[] { (1920, 1080), (1366, 768) })
            {
                var issues = Capture($"hud_{w}x{h}", w, h, dock.StatusWidget);
                Assert.IsEmpty(issues, string.Join("; ", issues));
            }

            // Another character: the portrait follows the model in use.
            var player = Object.FindFirstObjectByType<PlayerController>();
            var current = PlayableCharacterCatalog.GetSelected();
            var candidates = Enumerable.Range(0, PlayableCharacterCatalog.Count).Select(PlayableCharacterCatalog.Get).Where(c => c.Id != current.Id && PlayableCharacterCatalog.LoadPrefab(c) != null).ToList();
            if (candidates.Count > 0)
            {
                var other = candidates[0];
                Assert.IsTrue(PlayableCharacterCatalog.ApplyVisual(player.gameObject, other));
                for (float t = 0f; t < 4f && portrait.ShotCount == shots; t += Time.unscaledDeltaTime) yield return null;
                Assert.Greater(portrait.ShotCount, shots, "A new character gets a new portrait");
                yield return null;
                Capture("hud_other_character_1920x1080", 1920, 1080, dock.StatusWidget);
                PlayableCharacterCatalog.ApplyVisual(player.gameObject, current);
            }

            // Frame cost of the widget (measured last so the toggling cannot disturb the captures).
            dock.StatusWidget.gameObject.SetActive(false);
            float withoutHud = 0f;
            for (int i = 0; i < 120; i++) { yield return null; withoutHud += Time.unscaledDeltaTime; }
            dock.StatusWidget.gameObject.SetActive(true);
            Debug.Log($"[HudStatusWidget] frame time with widget {withHud / 120f * 1000f:0.00} ms, without {withoutHud / 120f * 1000f:0.00} ms");
        }

        private static string Prepare()
        {
            Directory.CreateDirectory(Folder);
            return Folder;
        }

        private static Texture2D Read(Texture texture)
        {
            var rt = (RenderTexture)texture;
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var copy = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            copy.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            copy.Apply();
            RenderTexture.active = old;
            return copy;
        }

        /// <summary>Full-screen capture plus a 2× crop of the widget; returns layout problems (clipping, overlaps with
        /// the other HUD pieces).</summary>
        private static List<string> Capture(string name, int width, int height, RectTransform widget)
        {
            var issues = new List<string>();
            var camera = Camera.main;
            if (camera == null) return issues;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            var target = new RenderTexture(width, height, 24);
            var old = RenderTexture.active;
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            foreach (var c in canvases) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)c.transform);
            Canvas.ForceUpdateCanvases();

            Rect ScreenRect(RectTransform rt)
            {
                var corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                Vector2 a = camera.WorldToScreenPoint(corners[0]), b = camera.WorldToScreenPoint(corners[2]);
                return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            }
            var box = ScreenRect(widget);
            if (box.xMin < 0f || box.yMin < 0f || box.xMax > width || box.yMax > height) issues.Add($"widget clipped at {width}x{height}");
            if (box.width > width * 0.36f || box.height > height * 0.24f) issues.Add($"widget too large at {width}x{height}: {box.width:0}x{box.height:0}");
            var dock = widget.GetComponentInParent<StatusDock>();
            if (dock.ObjectiveChip != null && dock.ObjectiveChip.gameObject.activeInHierarchy && ScreenRect(dock.ObjectiveChip).Overlaps(box)) issues.Add("overlaps the objective chip");
            foreach (var t in widget.GetComponentsInChildren<TextMeshProUGUI>(false))
            {
                t.ForceMeshUpdate();
                if (t.isTextOverflowing) issues.Add($"text '{t.name}' overflows");
            }

            camera.Render();
            RenderTexture.active = target;
            var full = new Texture2D(width, height, TextureFormat.RGB24, false);
            full.ReadPixels(new Rect(0, 0, width, height), 0, 0); full.Apply();
            File.WriteAllBytes(Path.Combine(Prepare(), name + ".jpg"), full.EncodeToJPG(90));
            // 2× crop of the widget for review.
            var crop = Rect.MinMaxRect(Mathf.Max(0, box.xMin - 12), Mathf.Max(0, box.yMin - 12), Mathf.Min(width, box.xMax + 12), Mathf.Min(height, box.yMax + 12));
            var cut = new Texture2D((int)crop.width, (int)crop.height, TextureFormat.RGB24, false);
            cut.SetPixels(full.GetPixels((int)crop.x, (int)crop.y, (int)crop.width, (int)crop.height));
            cut.Apply();
            File.WriteAllBytes(Path.Combine(Prepare(), name + "_widget.png"), cut.EncodeToPNG());

            camera.targetTexture = null; RenderTexture.active = old;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Object.Destroy(full); Object.Destroy(cut); target.Release(); Object.Destroy(target);
            return issues;
        }
    }
}
