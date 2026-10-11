using System;
using System.Collections;
using System.Collections.Generic;
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
using Object = UnityEngine.Object;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Kids Characters for Survival in Hibari (90_TestSandbox): the player's model is swapped for each kid through the
    /// normal PlayableCharacterCatalog path and every gameplay animation is played on it — locomotion, talking, bow,
    /// point, sit, rest, jump; on Midori, fishing and farm work with the tool in hand. Checks the rig, height, feet on the ground,
    /// the tool in the right hand and the HUD portrait; shoots a pose sheet per character (before = Tanaka/Remy, after =
    /// kids) to Bao_Cao/character-regression.
    /// </summary>
    public class KidsCharactersPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/character-regression"));

        private static readonly (string label, Func<CharacterAnimationController, IEnumerator> pose)[] Poses =
        {
            ("Idle", a => Hold(a, () => a.SetSpeed(0f))),
            ("Walk", a => Hold(a, null, 1f, () => a.SetSpeed(3f))),
            ("Talk", a => Hold(a, () => { a.SetSpeed(0f); a.SetTalking(true); })),
            ("Bow", a => Hold(a, () => { a.SetTalking(false); a.TriggerBow(); }, 0.6f)),
            ("Point", a => Hold(a, () => a.TriggerPoint(), 0.7f)),
            ("Sit", a => Hold(a, () => a.SetSitting(true))),
            ("Rest", a => Hold(a, () => { a.SetSitting(false); a.SetResting(true); })),
            ("Jump", a => Hold(a, () => { a.SetResting(false); a.TriggerJump(); }, 0.35f)),
        };

        private static readonly (string label, string state, string tool)[] ToolPoses =
        {
            ("Fish", "Fish_Cast", "tool_fishing_rod"), ("Hoe", "Work_Hoe", "tool_hoe"), ("Shovel", "Work_Plant", "tool_shovel"),
        };

        [UnityTest]
        public IEnumerator Kids_SwapInHibari_AllGameplayAnimations()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 3f); t += Time.unscaledDeltaTime) yield return null;
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            yield return new WaitForSecondsRealtime(0.5f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            player.InputLocked = true;
            player.enabled = false; // the poses drive the animator directly
            var animation = player.GetComponent<CharacterAnimationController>();
            Assert.NotNull(animation, "Player animation controller");
            var original = PlayableCharacterCatalog.GetSelected();
            Directory.CreateDirectory(Folder);
            try
            {
                foreach (var id in new[] { "student", "kid_haruto", "kid_yui", "kid_sota", "kid_hina", "kid_riku", "kid_mio", "kid_yuto", "kid_sakura" })
                {
                    var character = PlayableCharacterCatalog.FindById(id);
                    Assert.AreEqual(id, character.Id);
                    Assert.IsTrue(character.IsAvailable, id + " is playable");
                    Assert.IsTrue(PlayableCharacterCatalog.ApplyVisual(player.gameObject, character), id + " visual");
                    yield return null;
                    yield return null;
                    var visual = player.transform.Find("Visual");
                    var animator = visual.GetComponentInChildren<Animator>();
                    Assert.IsTrue(animator.isHuman && animator.avatar != null && animator.avatar.isValid, id + ": humanoid Avatar");
                    Assert.NotNull(animator.runtimeAnimatorController, id + ": controller");

                    // Height and feet, standing.
                    animation.SetSpeed(0f);
                    yield return new WaitForSecondsRealtime(0.4f);
                    var bounds = Bounds(visual);
                    float feet = bounds.min.y; // soles (the Foot bone is the ankle)
                    Debug.Log($"[Kids] {id}: height {bounds.size.y:0.00} m, feet {feet - player.transform.position.y:0.00} m above the root, controller {animator.runtimeAnimatorController.name}");
                    Assert.That(bounds.size.y, Is.InRange(id == "student" ? 1.5f : 1.2f, 2f), id + ": height");
                    Assert.That(feet - player.transform.position.y, Is.InRange(-0.08f, 0.08f), id + ": feet on the ground");

                    var tiles = new List<Texture2D>();
                    foreach (var (label, pose) in Poses)
                    {
                        yield return pose(animation);
                        Assert.IsFalse(float.IsNaN(animator.GetBoneTransform(HumanBodyBones.Head).position.y), $"{id} {label}: head");
                        tiles.Add(Shoot(player.transform, bounds.size.y, label));
                        animation.StopWork();
                    }
                    animation.StopWork();
                    animation.SetSitting(false);
                    animation.SetResting(false);
                    SaveSheet(tiles, Path.Combine(Folder, $"poses_{id}.jpg"));

                    // The HUD portrait follows the new model.
                    var portrait = Object.FindFirstObjectByType<HudPortrait>();
                    if (portrait != null)
                    {
                        int shots = portrait.ShotCount;
                        for (float t = 0f; t < 3f && portrait.ShotCount == shots && id != original.Id; t += Time.unscaledDeltaTime) yield return null;
                        Assert.IsTrue(portrait.HasPortrait, id + ": HUD portrait");
                    }
                }
            }
            finally
            {
                PlayableCharacterCatalog.ApplyVisual(player.gameObject, original);
                player.enabled = true;
                player.InputLocked = false;
            }
        }

        /// <summary>Midori: fishing and farm work with the real held tools (the island's model library).</summary>
        [UnityTest]
        public IEnumerator Kids_FarmAndFishingTools_OnMidori()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.MidoriIslandScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || NihongoLife.Island.IslandUI.HeldTool("tool_hoe") == null || t < 3f); t += Time.unscaledDeltaTime) yield return null;
            var player = Object.FindFirstObjectByType<PlayerController>();
            player.InputLocked = true;
            player.enabled = false;
            var animation = player.GetComponent<CharacterAnimationController>();
            var original = PlayableCharacterCatalog.GetSelected();
            try
            {
                foreach (var id in new[] { "student", "kid_haruto", "kid_yui" })
                {
                    Assert.IsTrue(PlayableCharacterCatalog.ApplyVisual(player.gameObject, PlayableCharacterCatalog.FindById(id)));
                    yield return null;
                    yield return null;
                    var visual = player.transform.Find("Visual");
                    var animator = visual.GetComponentInChildren<Animator>();
                    float height = Bounds(visual).size.y;
                    var tiles = new List<Texture2D>();
                    foreach (var (label, state, tool) in ToolPoses)
                    {
                        var prefab = NihongoLife.Island.IslandUI.HeldTool(tool);
                        Assert.NotNull(prefab, tool + " model");
                        Assert.IsTrue(animation.PlayWork(state), $"{id}: {state}");
                        animation.HoldProp(prefab);
                        yield return new WaitForSecondsRealtime(0.9f);
                        Assert.NotNull(animation.HeldProp, $"{id} {label}: tool in hand");
                        float grip = Vector3.Distance(animation.HeldProp.transform.position, animator.GetBoneTransform(HumanBodyBones.RightHand).position);
                        Debug.Log($"[Kids] {id} {label}: grip {grip:0.000} m");
                        Assert.Less(grip, 0.3f, $"{id} {label}: the tool is held by the right hand");
                        tiles.Add(Shoot(player.transform, height, label));
                        animation.StopWork();
                    }
                    SaveSheet(tiles, Path.Combine(Folder, $"tools_{id}.jpg"));
                }
            }
            finally
            {
                PlayableCharacterCatalog.ApplyVisual(player.gameObject, original);
                player.enabled = true;
                player.InputLocked = false;
            }
        }

        /// <summary>The real path: main menu → Start → character select → a kid → Play → Hibari with that kid.</summary>
        [UnityTest]
        public IEnumerator Kids_PickedInCharacterSelect_PlaysInHibari()
        {
            string originalId = PlayerPrefs.GetString(PlayableCharacterCatalog.PlayerPrefsKey, PlayableCharacterCatalog.DefaultId);
            string originalName = PlayerPrefs.GetString(PlayableCharacterCatalog.PlayerNamePrefsKey, PlayableCharacterCatalog.DefaultPlayerName);
            try
            {
                yield return SceneManager.LoadSceneAsync("01_MainMenu");
                for (float t = 0f; t < 15f && Object.FindFirstObjectByType<MainMenuUI>() == null; t += Time.unscaledDeltaTime) yield return null;
                yield return new WaitForSecondsRealtime(1f);
                var menu = Object.FindFirstObjectByType<MainMenuUI>();
                Button(menu.transform, "StartButton").onClick.Invoke();
                yield return null;
                var panel = menu.transform.Find("CharacterSelectPanel") ?? Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t => t.name == "CharacterSelectPanel");
                Assert.IsTrue(panel.gameObject.activeInHierarchy, "Character select opens");
                var next = Button(panel, "NextCharacterButton");
                var title = panel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                for (int i = 0; i < PlayableCharacterCatalog.Count && !title.Any(x => x.text == "Haruto"); i++) { next.onClick.Invoke(); yield return null; }
                Assert.IsTrue(title.Any(x => x.text == "Haruto"), "Haruto in the roster");
                // The 3D preview stage is not created in batch mode (MainMenuUI.CreateCharacterPreviewStage).
                var preview = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "SelectedCharacterPreview");
                if (preview != null) Assert.AreEqual(KidsScale, preview.localScale.x, 0.01f, "Preview keeps the kid's scale");
                var confirm = Button(panel, "ConfirmCharacterButton");
                Assert.IsTrue(confirm.interactable, "Haruto is playable (no COMING SOON)");
                yield return new WaitForSecondsRealtime(0.5f);
                CaptureScreen(Path.Combine(Folder, "menu_character_select_haruto.jpg"));
                confirm.onClick.Invoke();
                for (float t = 0f; t < 30f && (SceneManager.GetActiveScene().name != WorldLocationCatalog.CityScene || Object.FindFirstObjectByType<PlayerController>() == null || t < 3f); t += Time.unscaledDeltaTime) yield return null;
                yield return new WaitForSecondsRealtime(1f);
                var player = Object.FindFirstObjectByType<PlayerController>();
                var animator = player.transform.Find("Visual").GetComponentInChildren<Animator>();
                Assert.AreEqual("NL_Kid_Boy", animator.runtimeAnimatorController.name, "Hibari starts with Haruto");
            }
            finally
            {
                PlayerPrefs.SetString(PlayableCharacterCatalog.PlayerPrefsKey, originalId);
                PlayerPrefs.SetString(PlayableCharacterCatalog.PlayerNamePrefsKey, originalName);
                PlayerPrefs.Save();
            }
        }

        private const float KidsScale = 1.35f;

        private static UnityEngine.UI.Button Button(Transform root, string name) =>
            root.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == name);

        /// <summary>Screen with the overlay UI (canvases moved to the camera for the shot).</summary>
        private static void CaptureScreen(string path)
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
            File.WriteAllBytes(path, tex.EncodeToJPG(88));
            RenderTexture.active = old;
            cam.targetTexture = null;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(tex);
        }

        private static IEnumerator Hold(CharacterAnimationController a, Action start, float seconds = 0.8f, Action perFrame = null)
        {
            start?.Invoke();
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                perFrame?.Invoke();
                yield return null;
            }
        }

        private static Bounds Bounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>3/4 front view of the character, world only.</summary>
        private static Texture2D Shoot(Transform player, float height, string label)
        {
            var cam = Camera.main;
            var controller = cam.GetComponent<NihongoLife.Cameras.ThirdPersonCameraController>();
            bool enabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            var oldPos = cam.transform.position;
            var oldRot = cam.transform.rotation;
            Vector3 target = player.position + Vector3.up * height * 0.5f;
            cam.transform.position = target + (player.forward * 2.6f + player.right * 1.2f) * Mathf.Max(0.8f, height / 1.6f) + Vector3.up * 0.25f;
            cam.transform.LookAt(target);
            var rt = new RenderTexture(360, 480, 24);
            cam.targetTexture = rt;
            cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(360, 480, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 360, 480), 0, 0);
            tex.Apply();
            RenderTexture.active = old;
            cam.targetTexture = null;
            rt.Release();
            Object.Destroy(rt);
            cam.transform.SetPositionAndRotation(oldPos, oldRot);
            if (controller != null) controller.enabled = enabled;
            tex.name = label;
            return tex;
        }

        private static void SaveSheet(List<Texture2D> tiles, string path)
        {
            int cols = 6, w = 360, h = 480, rows = (tiles.Count + cols - 1) / cols;
            var sheet = new Texture2D(cols * w, rows * h, TextureFormat.RGB24, false);
            var fill = Enumerable.Repeat(new Color(0.1f, 0.1f, 0.12f), sheet.width * sheet.height).ToArray();
            sheet.SetPixels(fill);
            for (int i = 0; i < tiles.Count; i++)
            {
                int x = (i % cols) * w, y = (rows - 1 - i / cols) * h;
                sheet.SetPixels(x, y, w, h, tiles[i].GetPixels());
                Object.Destroy(tiles[i]);
            }
            sheet.Apply();
            File.WriteAllBytes(path, sheet.EncodeToJPG(85));
            Object.Destroy(sheet);
        }
    }
}
