using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Scenario;
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
    /// Phones are landscape only (AGENTS.md). In the town with the on-screen controls On, at real phone sizes
    /// (2400x1080 and 2340x1080 with a notch, 1600x720, 1334x750): the joystick and buttons stay inside the safe area,
    /// do not overlap each other or any HUD button, the HUD fits the screen, and a story cutscene with its answers
    /// (dialogue window) fits too. With NL_SHOTS_DIR set it saves a screenshot per size and state.
    /// </summary>
    public class GameplayPhoneLayoutPlayModeTests
    {
        static readonly MenuLayoutPlayModeTests.Size[] Phones =
        {
            new MenuLayoutPlayModeTests.Size("phone_2400x1080", 2400, 1080, true, new Rect(110f, 0f, 2180f, 1080f)),
            new MenuLayoutPlayModeTests.Size("phone_2340x1080", 2340, 1080, true, new Rect(100f, 0f, 2140f, 1080f)),
            new MenuLayoutPlayModeTests.Size("phone_1600x720", 1600, 720, true),
            new MenuLayoutPlayModeTests.Size("phone_1334x750", 1334, 750, true),
        };

        [UnityTest]
        public IEnumerator Town_HudTouchControlsAndDialogue_FitPhoneLandscape()
        {
            LogAssert.ignoreFailingMessages = true;
            int mode = TouchControlPrefs.Mode;
            bool scenes = StoryScenes.Enabled;
            TouchControlPrefs.Mode = 1;
            // Build the HUD as a phone does: phone profile, a phone-sized screen and a phone's pixel density.
            UiScreen.PhoneOverride = true;
            UiScreen.Override = new Vector2Int(Phones[0].W, Phones[0].H);
            UiScreen.DpiOverride = 96f * 2.75f;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(3f);
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            var world = Camera.main;
            var uiCam = new GameObject("PhoneLayoutCamera").AddComponent<Camera>();
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
                foreach (var size in Phones)
                {
                    foreach (string state in new[] { "hud", "dialogue" })
                    {
                        if (state == "dialogue")
                        {
                            var story = NihongoLife.Progression.Storyline.Load();
                            var nodes = StoryScenes.BuildNodes(story.chapters[0].questId, _ => false);
                            var ask = nodes.First(n => n.choices.Count > 0);
                            dm.StartConversation(nodes, ask.id, _ => { });
                        }
                        yield return new WaitForSecondsRealtime(0.6f);
                        var canvases = Canvases();
                        UiScreen.Override = new Vector2Int(size.W, size.H);
                        UiScreen.PhoneOverride = true;
                        UiScreen.SafeAreaOverride = size.Safe;
                        yield return null; yield return null; // fitters pick the canvas reference for this screen
                        int k = 0;
                        foreach (var c in canvases.OrderBy(c => c.sortingOrder))
                            Place(c, uiCam, size.W, size.H, k++);
                        yield return null; yield return null;
                        Canvas.ForceUpdateCanvases();
                        foreach (var c in canvases)
                        {
                            var rt = (RectTransform)c.transform;
                            foreach (var issue in UiLayoutProbe.Audit(rt, UiLayoutProbe.TopLayer(rt)))
                                issues.Add($"{size.Name} {state} [{c.name}]: {issue}");
                        }
                        issues.AddRange(CrossCanvas(canvases, uiCam, size).Select(i => $"{size.Name} {state}: {i}"));
                        audits++;
                        if (!string.IsNullOrEmpty(shots))
                        {
                            Directory.CreateDirectory(shots);
                            File.WriteAllBytes(Path.Combine(shots, $"{size.Name}_{state}.png"), UiLayoutProbe.Capture(world, uiCam, size.W, size.H));
                        }
                        for (int guard = 0; guard < 40 && dm.IsOpen; guard++) dm.CancelDialogue();
                    }
                }
            }
            finally
            {
                UiLayoutProbe.Release();
                UiScreen.DpiOverride = null;
                TouchControlPrefs.Mode = mode;
                StoryScenes.Enabled = scenes;
            }
            string report = string.Join("\n", issues.Distinct());
            string dir = !string.IsNullOrEmpty(shots) ? shots : Path.Combine(Application.temporaryCachePath, "phone_layout");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "phone_layout_issues.txt"), $"audits: {audits} issues: {issues.Distinct().Count()}\n{report}\n");
            Assert.AreEqual(Phones.Length * 2, audits);
            Assert.IsEmpty(issues.Distinct().ToList(), "phone layout problems:\n" + report);
        }

        /// <summary>Lays a canvas out in world space at the scale its own CanvasScaler gives a w x h screen.</summary>
        static void Place(Canvas canvas, Camera cam, int w, int h, int order)
        {
            float scale = 1f;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                Vector2 r = scaler.referenceResolution;
                float sx = w / r.x, sy = h / r.y;
                scale = scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Expand ? Mathf.Min(sx, sy)
                    : scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Shrink ? Mathf.Max(sx, sy)
                    : Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(sx, 2f), Mathf.Log(sy, 2f), scaler.matchWidthOrHeight));
            }
            else if (scaler != null) scale = scaler.scaleFactor;
            if (canvas.renderMode != RenderMode.WorldSpace) { canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = cam; }
            var rt = (RectTransform)canvas.transform;
            rt.sizeDelta = new Vector2(w / scale, h / scale);
            rt.localScale = Vector3.one * scale;
            rt.rotation = cam.transform.rotation;
            rt.position = cam.transform.position + cam.transform.forward * (10f - 0.01f * order); // later canvases nearer
            cam.orthographic = true; cam.orthographicSize = h / 2f; cam.aspect = (float)w / h;
            foreach (var t in canvas.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = UiLayoutProbe.Layer;
        }

        static List<Canvas> Canvases() =>
            UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.isActiveAndEnabled && (c.renderMode != RenderMode.WorldSpace || c.gameObject.layer == UiLayoutProbe.Layer)).ToList();

        static bool Shown(Transform t)
        {
            foreach (var g in t.GetComponentsInParent<CanvasGroup>(false)) { if (g.alpha < 0.05f) return false; if (g.ignoreParentGroups) break; }
            return t.gameObject.activeInHierarchy;
        }

        static Rect ScreenRect(Camera cam, RectTransform r)
        {
            var c = new Vector3[4]; r.GetWorldCorners(c);
            // Viewport, not screen: the batch window is not the size under test.
            Vector3 a = cam.WorldToViewportPoint(c[0]), b = cam.WorldToViewportPoint(c[2]);
            float w = UiScreen.Width, h = UiScreen.Height;
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x) * w, Mathf.Min(a.y, b.y) * h, Mathf.Max(a.x, b.x) * w, Mathf.Max(a.y, b.y) * h);
        }

        /// <summary>Touch controls inside the safe area and clear of every other button on any canvas.</summary>
        static IEnumerable<string> CrossCanvas(List<Canvas> canvases, Camera cam, MenuLayoutPlayModeTests.Size size)
        {
            var overlay = UnityEngine.Object.FindFirstObjectByType<TouchControlOverlay>();
            if (overlay == null) { yield return "no touch controls"; yield break; }
            Rect safe = size.Safe ?? new Rect(0, 0, size.W, size.H);
            var touch = overlay.GetComponentsInChildren<Graphic>(false)
                .Where(g => g.raycastTarget && Shown(g.transform) && g.rectTransform.rect.width > 20f && g.transform.parent != null && g.GetComponentInParent<TouchControlOverlay>() == overlay)
                .Select(g => (name: g.name, r: ScreenRect(cam, g.rectTransform))).Where(x => x.r.width < size.W * 0.6f).ToList();
            if (touch.Count == 0) yield return "touch controls not shown with Mode On";
            foreach (var t in touch)
                if (t.r.xMin < safe.xMin - 2 || t.r.xMax > safe.xMax + 2 || t.r.yMin < safe.yMin - 2 || t.r.yMax > safe.yMax + 2)
                    yield return $"touch control outside the safe area: {t.name} {t.r}";
            var others = new List<(string name, Rect r)>();
            foreach (var c in canvases)
                foreach (var s in c.GetComponentsInChildren<Selectable>(false))
                {
                    if (!s.interactable || !Shown(s.transform) || s.GetComponentInParent<TouchControlOverlay>() != null || s is Scrollbar) continue;
                    if (s.targetGraphic == null || !s.targetGraphic.enabled) continue;
                    var r = ScreenRect(cam, (RectTransform)s.transform);
                    if (r.width < 4f || r.height < 4f) continue;
                    if (r.xMin < -2 || r.yMin < -2 || r.xMax > size.W + 2 || r.yMax > size.H + 2) yield return $"button off screen: {c.name}/{s.name}";
                    others.Add((c.name + "/" + UiLayoutProbe.PathOf(s.transform, c.transform), r));
                }
            foreach (var t in touch)
                foreach (var o in others)
                    if (t.r.Overlaps(o.r) && Mathf.Min(t.r.xMax, o.r.xMax) - Mathf.Max(t.r.xMin, o.r.xMin) > 6f && Mathf.Min(t.r.yMax, o.r.yMax) - Mathf.Max(t.r.yMin, o.r.yMin) > 6f)
                        yield return $"touch control {t.name} covers {o.name}";
        }
    }
}
