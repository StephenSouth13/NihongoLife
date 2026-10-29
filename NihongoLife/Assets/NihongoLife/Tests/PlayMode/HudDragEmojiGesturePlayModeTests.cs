using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Gestures;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>J2 draggable/collapsible quest tracker, F5 chat emoji, (C) job-action gestures. Captures: Bao_Cao/hud-tracker, chat-emoji, gesture-hooks.</summary>
    public class HudDragEmojiGesturePlayModeTests
    {
        private static string Folder(string name) => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/" + name));

        private static IEnumerator LoadCity()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || PlayerStatus.Instance == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = NihongoLife.Dialogue.DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            yield return new WaitForSecondsRealtime(0.6f);
        }

        private static void Capture(string folder, string name, int w = 1600, int h = 900)
        {
            var cam = Camera.main;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = cam; canvases[i].planeDistance = cam.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render();
            var old = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            Directory.CreateDirectory(Folder(folder));
            File.WriteAllBytes(Path.Combine(Folder(folder), name + ".png"), tex.EncodeToPNG());
            RenderTexture.active = old; cam.targetTexture = null; rt.Release();
            Object.Destroy(rt); Object.Destroy(tex);
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
        }

        [UnityTest]
        public IEnumerator QuestTracker_Drag_Collapse_Persist_Reset()
        {
            PlayerPrefs.DeleteKey("hud_pos_quest_tracker"); PlayerPrefs.DeleteKey("hud_collapsed_quest_tracker");
            yield return LoadCity();
            var dock = Object.FindFirstObjectByType<StatusDock>();
            Assert.NotNull(dock?.ObjectiveChip, "Tracker chip");
            var chip = dock.ObjectiveChip;
            chip.gameObject.SetActive(true);
            var drag = chip.GetComponent<DraggableHud>();
            Assert.NotNull(drag, "The tracker is draggable");
            Vector2 home = drag.DefaultPosition;
            Assert.AreEqual(home, chip.anchoredPosition, "Starts at its default spot");

            // A real drag: begin / drag / end events with a pointer delta.
            var ev = new PointerEventData(EventSystem.current) { delta = new Vector2(260f, -180f) };
            drag.OnBeginDrag(ev); drag.OnDrag(ev); drag.OnEndDrag(ev);
            Assert.Greater(chip.anchoredPosition.x, home.x + 100f, "Moved right");
            Assert.Less(chip.anchoredPosition.y, home.y - 80f, "Moved down");
            StringAssert.Contains(",", PlayerPrefs.GetString("hud_pos_quest_tracker"), "Position saved");

            // Cannot be dragged off screen.
            drag.MoveBy(new Vector2(99999f, 99999f));
            var parent = (RectTransform)chip.parent;
            var corners = new Vector3[4]; chip.GetWorldCorners(corners);
            var pc = new Vector3[4]; parent.GetWorldCorners(pc);
            Assert.LessOrEqual(corners[2].x, pc[2].x + 0.5f, "Right edge stays on screen"); Assert.LessOrEqual(corners[2].y, pc[2].y + 0.5f, "Top edge stays on screen");
            drag.DropAt(home + new Vector2(300f, -200f));

            // Collapse hides the body and is remembered.
            var body = chip.Find("Text").gameObject;
            Assert.IsTrue(body.activeSelf);
            drag.SetCollapsed(true);
            Assert.IsFalse(body.activeSelf, "Collapsed: body hidden");
            Assert.AreEqual(1, PlayerPrefs.GetInt("hud_collapsed_quest_tracker"));
            yield return null;
            Capture("hud-tracker", "01_moved_collapsed");
            drag.SetCollapsed(false);
            Assert.IsTrue(body.activeSelf);

            // Saved position survives re-attaching (as after a reload).
            Vector2 saved = chip.anchoredPosition;
            chip.anchoredPosition = home;
            DraggableHud.Attach(chip, "quest_tracker");
            Assert.AreEqual(saved.x, chip.anchoredPosition.x, 0.2f); Assert.AreEqual(saved.y, chip.anchoredPosition.y, 0.2f);
            yield return null;
            Capture("hud-tracker", "02_moved_expanded");

            // Reset (right-click) returns it home and forgets the saved spot.
            drag.OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Right });
            Assert.AreEqual(home, chip.anchoredPosition);
            Assert.AreEqual("", PlayerPrefs.GetString("hud_pos_quest_tracker", ""));
            PlayerPrefs.DeleteKey("hud_collapsed_quest_tracker");
        }

        [Test]
        public void Emoji_Render_ShortcodesEmoticonsUnicode_AndNoRichTextInjection()
        {
            EmojiService.ResetForTests();
            Assert.GreaterOrEqual(EmojiService.Count, 60);
            string r = EmojiService.Render("Hi :joy: :) <3 \U0001F602 :nonsense: ok");
            StringAssert.Contains("<sprite name=\"joy\">", r);
            StringAssert.Contains("<sprite name=\"smile\">", r);
            StringAssert.Contains("<sprite name=\"heart\">", r);
            Assert.AreEqual(4, EmojiService.CountIn("Hi :joy: :) <3 \U0001F602 :nonsense: ok"), "joy, smile, heart, unicode joy; unknown shortcode stays text");
            StringAssert.Contains(":nonsense:", r);
            string evil = EmojiService.Render("<size=900>BIG</size></noparse><color=red>x");
            Assert.IsFalse(evil.Contains("</noparse><color"), "Players cannot close the noparse");
            Assert.AreEqual(0, EmojiService.CountIn("<sprite name=\"joy\">"), "A typed <sprite> tag is plain text");
            // Every catalogue entry has a real picture in the atlas.
            for (int i = 0; i < EmojiService.Count; i++) Assert.NotNull(EmojiService.GetSprite(EmojiService.NameAt(i)), EmojiService.NameAt(i));
            Assert.AreEqual(0, EmojiService.Render(""). Length);
        }

        [UnityTest]
        public IEnumerator Emoji_DrawnAsSprites_InTmpLabel_Capture()
        {
            EmojiService.ResetForTests();
            var camGo = new GameObject("EmojiCam", typeof(Camera)); var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.1f, 0.12f, 0.15f); cam.orthographic = true; cam.tag = "MainCamera";
            var canvasGo = new GameObject("EmojiCanvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 1f;
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(canvasGo.transform, false);
            var label = labelGo.GetComponent<TextMeshProUGUI>();
            label.font = NLUi.ResolveFont(); label.fontSize = 48; label.color = Color.white;
            var rect = label.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.sizeDelta = new Vector2(900f, 200f);
            EmojiService.Apply(label, "Yoroshiku :joy: :heart: :fire: <b>no bold</b>");
            yield return null; yield return null;
            label.ForceMeshUpdate();
            int sprites = label.textInfo.characterInfo.Take(label.textInfo.characterCount).Count(c => c.elementType == TMP_TextElementType.Sprite);
            Assert.AreEqual(3, sprites, "Three sprites drawn");
            StringAssert.Contains("<b>no bold</b>", label.GetParsedText().Replace("\u200B", ""), "Player tags are shown as text, not applied");

            int w = 1200, h = 400;
            var rt = new RenderTexture(w, h, 24); cam.targetTexture = rt; cam.Render();
            var old = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            Directory.CreateDirectory(Folder("chat-emoji"));
            File.WriteAllBytes(Path.Combine(Folder("chat-emoji"), "01_label_with_emoji.png"), tex.EncodeToPNG());
            int yellow = 0, red = 0;
            foreach (var p in tex.GetPixels32())
            {
                if (p.r > 190 && p.g > 130 && p.b < 130 && p.r - p.b > 70) yellow++;
                if (p.r > 150 && p.r - p.g > 70 && p.r - p.b > 60) red++;
            }
            RenderTexture.active = old; cam.targetTexture = null; rt.Release();
            Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(canvasGo); Object.Destroy(camGo);
            File.WriteAllText(Path.Combine(Folder("chat-emoji"), "pixels.txt"), $"yellow={yellow} red={red} sprites={sprites}");
            Assert.Greater(yellow, 300, "The joy face is drawn in yellow");
            Assert.Greater(red, 150, "The heart / fire is drawn in red");
        }

        [UnityTest]
        public IEnumerator ChatWindow_EmojiPicker_InsertsShortcode()
        {
            yield return LoadCity();
            var window = SocialChatWindow.Ensure();
            SocialChatWindow.Open(SocialChatWindow.Tab.Area);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsFalse(window.EmojiPanelOpen);
            window.ToggleEmojiPanel();
            Assert.IsTrue(window.EmojiPanelOpen, "Picker opens");
            var cells = window.GetComponentsInChildren<Button>(true).Where(b => b.name.StartsWith("E_")).ToArray();
            Assert.AreEqual(EmojiService.Count, cells.Length, "One button per emoji");
            window.ChatInput.text = "";
            cells.First(b => b.name == "E_joy").onClick.Invoke();
            cells.First(b => b.name == "E_heart").onClick.Invoke();
            Assert.AreEqual(":joy::heart:", window.ChatInput.text);
            yield return new WaitForSecondsRealtime(0.3f);
            Capture("chat-emoji", "02_chat_window_picker");
            window.SetEmojiPanel(false);
            Assert.IsFalse(window.EmojiPanelOpen);
            SocialChatWindow.Close();
        }

        [UnityTest]
        public IEnumerator JobActions_PlayHandGestures_AndClearAfterwards()
        {
            yield return LoadCity();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var anim = player.GetComponentInChildren<CharacterAnimationController>();
            Assert.NotNull(anim, "Player animation");
            string[] ids = { "cashier_scan", "serve_tray", "pickup" };
            foreach (var id in ids)
            {
                bool done = false, seen = false;
                Assert.IsTrue(TimedAction.Run("test", "tool", 0.9f, () => done = true, pose: "Work_Hold", gesture: id));
                float t = 0f;
                while (!done && t < 5f) { var l = anim.GetComponent<GestureLayer>(); if (l != null && l.Current == id && l.IsPlaying) seen = true; t += Time.unscaledDeltaTime; yield return null; }
                Assert.IsTrue(done, id + ": action finished");
                Assert.IsTrue(seen, id + ": gesture was playing during the action");
                if (id == "cashier_scan") Capture("gesture-hooks", "01_register_scan_end");
                yield return new WaitForSecondsRealtime(0.8f);
                var layer = anim.GetComponent<GestureLayer>();
                Assert.IsFalse(layer.IsPlaying && layer.Current == id, id + ": cleared after the action");
            }
            // Mid-action capture of the serve gesture.
            bool d2 = false;
            TimedAction.Run("serve", "tray", 1.2f, () => d2 = true, pose: "Work_Hold", gesture: "serve_tray");
            yield return new WaitForSecondsRealtime(0.5f);
            Capture("gesture-hooks", "02_serve_mid");
            while (!d2) yield return null;
        }
    }
}