using System.Collections;
using System.IO;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Every body action a Kid does in the game, photographed on Midori with its tool in hand (three frames across the motion) (boy and girl): bow, pick up,
    /// eat/drink, shelf, hoe, plant, harvest, rod cast, fishing wait, water, carry. Also checks the gameplay hooks:
    /// eating plays UseItem, a bow leans the spine. Captures: Bao_Cao/character-regression/action_*.jpg
    /// </summary>
    public class KidActionPosesPlayModeTests
    {
        private static string Out => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/character-regression"));

        private static readonly (string state, string prop, float at)[] Actions =
        {
            ("Pickup", null, 0.45f), ("UseItem", null, 0.5f), ("Work_Shelf", null, 0.45f), ("Work_Hoe", "tool_hoe", 0.35f),
            ("Work_Plant", null, 0.5f), ("Work_Harvest", null, 0.45f), ("Fish_Cast", "tool_fishing_rod", 0.3f),
            ("Fish_Wait", "tool_fishing_rod", 0.5f), ("Work_Water", "tool_watering_can", 0.5f), ("Work_Carry", null, 0.5f),
        };

        [UnityTest, Timeout(300000)]
        public IEnumerator Kids_AllActions_MidMotion()
        {
            // On Midori, where the tools (hoe, rod, watering can) are loaded, so each action is shot with its tool in hand.
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.MidoriIslandScene); // standalone boot through the city
            for (float t = 0f; t < 40f; t += Time.unscaledDeltaTime)
            {
                if (Object.FindFirstObjectByType<PlayerController>() != null && !StandaloneZoneBootstrap.IsBooting && NihongoLife.Island.IslandRuntime.Active != null
                    && SceneManager.GetActiveScene().name == WorldLocationCatalog.MidoriIslandScene && t > 3f) break;
                yield return null;
            }
            Assert.AreEqual(WorldLocationCatalog.MidoriIslandScene, SceneManager.GetActiveScene().name);
            yield return new WaitForSecondsRealtime(1.5f);
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            var player = Object.FindFirstObjectByType<PlayerController>();
            player.InputLocked = true;
            var anim = player.GetComponent<CharacterAnimationController>();
            string original = PlayableCharacterCatalog.GetSelected().Id;
            try
            {
                foreach (var kid in new[] { "kid_haruto", "kid_yui" })
                {
                    PlayableCharacterCatalog.ApplyVisual(player.gameObject, PlayableCharacterCatalog.FindById(kid));
                    yield return new WaitForSecondsRealtime(0.6f);

                    // Bow: the spine visibly leans.
                    var chest = player.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Neck);
                    float upright = Vector3.Angle(chest.up, Vector3.up);
                    anim.TriggerBow();
                    yield return new WaitForSecondsRealtime(0.6f);
                    Assert.IsTrue(anim.IsBowing);
                    float bowed = Vector3.Angle(chest.up, Vector3.up);
                    Debug.Log($"[KidActions] {kid} bow: neck tilt {upright:0}° → {bowed:0}°");
                    Assert.Greater(bowed - upright, 15f, kid + ": the bow is visible");
                    SaveStrip(new[] { Render(player) }, $"action_{kid}_Bow");
                    yield return new WaitForSecondsRealtime(1f);

                    foreach (var (state, prop, at) in Actions)
                    {
                        Assert.IsTrue(anim.PlayWork(state), $"{kid}: state {state}");
                        if (prop != null)
                        {
                            var prefab = NihongoLife.Island.IslandUI.HeldTool(prop); // tool models live on Midori; null in the city
                            Assert.IsNotNull(prefab, $"tool model {prop} on Midori");
                            anim.HoldProp(prefab);
                            Assert.IsNotNull(anim.HeldProp, $"{kid}: {prop} in hand");
                        }
                        var clip = player.GetComponentInChildren<Animator>();
                        yield return new WaitForSecondsRealtime(0.25f); // past the blend into the state
                        float length = Mathf.Max(0.4f, clip.GetCurrentAnimatorStateInfo(0).length);
                        // Three frames across the motion (start, the moment named in Actions, late) in one strip.
                        var frames = new Texture2D[3];
                        float done = 0f;
                        float[] marks = { 0.1f, at, Mathf.Min(0.9f, at + 0.35f) };
                        for (int f = 0; f < 3; f++)
                        {
                            float wait = Mathf.Max(0f, length * marks[f] - done);
                            yield return new WaitForSecondsRealtime(wait);
                            done += wait;
                            frames[f] = Render(player);
                        }
                        SaveStrip(frames, $"action_{kid}_{state}");
                        anim.StopWork();
                        yield return new WaitForSecondsRealtime(0.35f);
                    }
                }

                // Eating from the bag plays the hand-to-mouth action.
                PlayerStatus.Instance.RestoreNeeds(10f, 0f, 0f);
                yield return null;
                Assert.AreEqual("UseItem", anim.CurrentWork, "Eating shows on the body");
                yield return new WaitForSecondsRealtime(1.8f);
                Assert.IsNull(anim.CurrentWork, "…and ends by itself");
            }
            finally
            {
                player.InputLocked = false;
                PlayableCharacterCatalog.ApplyVisual(player.gameObject, PlayableCharacterCatalog.FindById(original));
            }
        }

        /// <summary>One 480×480 frame from the front-left, three-quarter view, the whole Kid and the tool in shot.</summary>
        private static Texture2D Render(PlayerController player)
        {
            var cam = Camera.main;
            var controller = cam.GetComponent<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (controller != null) controller.enabled = false;
            var p = player.transform;
            cam.transform.position = p.position + p.right * 1.9f + p.forward * 1.6f + Vector3.up * 1.1f;
            cam.transform.LookAt(p.position + Vector3.up * 0.6f);
            var rt = new RenderTexture(480, 480, 24);
            cam.targetTexture = rt;
            cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(480, 480, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 480, 480), 0, 0);
            tex.Apply();
            RenderTexture.active = old;
            cam.targetTexture = null;
            rt.Release();
            Object.Destroy(rt);
            if (controller != null) controller.enabled = true;
            return tex;
        }

        private static void SaveStrip(Texture2D[] frames, string name)
        {
            var strip = new Texture2D(480 * frames.Length, 480, TextureFormat.RGB24, false);
            for (int i = 0; i < frames.Length; i++) { strip.SetPixels(i * 480, 0, 480, 480, frames[i].GetPixels()); Object.Destroy(frames[i]); }
            strip.Apply();
            Directory.CreateDirectory(Out);
            File.WriteAllBytes(Path.Combine(Out, name + ".jpg"), strip.EncodeToJPG(86));
            Object.Destroy(strip);
        }
    }
}
