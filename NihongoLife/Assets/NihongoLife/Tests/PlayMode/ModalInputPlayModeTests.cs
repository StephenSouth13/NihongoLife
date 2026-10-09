using System.Collections;
using System.Linq;
using System.Reflection;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.NPC;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Esc / O policy (UiModalStack): Esc closes only the most recently opened overlay and never opens Settings;
    /// with nothing open Esc does nothing; O opens Settings. Keys are pressed through GameInputService.
    /// </summary>
    public class ModalInputPlayModeTests
    {
        [UnityTest]
        public IEnumerator Escape_ClosesTopOverlayOnly_AndNeverOpensSettings()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var dm = DialogueManager.Instance;
            var settings = Object.FindFirstObjectByType<SettingsUI>();
            var hud = Object.FindFirstObjectByType<HUDUI>();
            Assert.NotNull(player); Assert.NotNull(dm); Assert.NotNull(settings); Assert.NotNull(hud);
            while (dm.IsOpen) { dm.CancelDialogue(); yield return null; }
            yield return null;

            // 1) Esc with nothing open: nothing happens (no Settings).
            yield return Press(GameInputId.Pause);
            Assert.IsFalse(settings.IsOpen, "Esc with nothing open must not open Settings.");

            // 2) Esc in an NPC conversation closes it and does not open Settings.
            dm.StartConversation(new System.Collections.Generic.List<NihongoLife.Scenario.ScenarioNode>
            {
                new NihongoLife.Scenario.ScenarioNode { id = "hello", nodeType = NihongoLife.Scenario.ScenarioNodeType.Dialogue, speakerId = "npc_test", speakerName = "Test", textJa = "こんにちは。", textEn = "Xin chào." },
            }, "hello", null);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(dm.IsOpen, "Conversation should open.");
            Assert.AreEqual("Dialogue", UiModalStack.TopName);
            yield return Press(GameInputId.Pause);
            Assert.IsFalse(dm.IsOpen, "Esc closes the conversation.");
            Assert.IsFalse(settings.IsOpen, "…and must not also open Settings.");

            // 3) Bag: Esc closes it.
            Invoke(hud, "SetInventoryVisible", true);
            yield return null; yield return null;
            Assert.AreEqual("Bag", UiModalStack.TopName);
            yield return Press(GameInputId.Pause);
            Assert.IsNull(UiModalStack.TopName, "Esc closes the bag.");

            // 4) O opens Settings, Esc closes it.
            yield return Press(GameInputId.Settings);
            Assert.IsTrue(settings.IsOpen, "O opens Settings.");
            yield return Press(GameInputId.Pause);
            Assert.IsFalse(settings.IsOpen, "Esc closes Settings.");
            Assert.IsFalse(UiModalStack.AnyOpen);

            // 5) Sims-style cursor: never locked in gameplay.
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState);

            // 6) Tab: character profile with the real 3D model; Esc closes it.
            Capture("01_hud");
            yield return Press(GameInputId.Character);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual("Character", UiModalStack.TopName, "Tab opens the character profile.");
            var preview = Object.FindFirstObjectByType<ProfilePreview>();
            Assert.NotNull(preview, "The profile shows a 3D preview.");
            Assert.IsTrue(preview.HasModel, "The preview clones the real player model.");
            Capture("02_profile");
            yield return Press(GameInputId.Pause);
            Assert.IsFalse(UiModalStack.AnyOpen, "Esc closes the profile.");

            // 7) Needs warnings: low hunger / thirst tint the bars.
            var status = PlayerStatus.Instance;
            status.RestoreNeeds(-85f, -55f, 0f);
            yield return new WaitForSecondsRealtime(1.5f);
            Capture("03_needs_low");
            status.RestoreNeeds(100f, 100f, 0f);
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            string folder = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../Bao_Cao/stageA-regression"));
            System.IO.Directory.CreateDirectory(folder);
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i);
            }
            Canvas.ForceUpdateCanvases();
            var target = new RenderTexture(1600, 900, 24);
            var old = RenderTexture.active;
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = old;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Object.Destroy(tex); target.Release(); Object.Destroy(target);
        }

        private static IEnumerator Press(GameInputId id)
        {
            var input = GameInputService.GetOrCreate();
            yield return new WaitForFixedUpdate(); // before Update reads input
            input.SetMobileButton(id, true);
            yield return null;
            input.SetMobileButton(id, false);
            yield return null;
            yield return null;
        }

        private static void Invoke(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(target, args);
    }
}
