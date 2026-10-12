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
    /// Every body action a Kid does in the game, photographed mid-motion from the side (boy and girl): bow, pick up,
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
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
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
                    Shoot(player, $"action_{kid}_Bow");
                    yield return new WaitForSecondsRealtime(1f);

                    foreach (var (state, prop, at) in Actions)
                    {
                        Assert.IsTrue(anim.PlayWork(state), $"{kid}: state {state}");
                        if (prop != null)
                        {
                            var prefab = NihongoLife.Island.IslandUI.HeldTool(prop); // tool models live on Midori; null in the city
                            if (prefab != null) anim.HoldProp(prefab);
                        }
                        var clip = player.GetComponentInChildren<Animator>();
                        yield return null;
                        float length = Mathf.Max(0.4f, clip.GetCurrentAnimatorStateInfo(0).length);
                        yield return new WaitForSecondsRealtime(0.25f + length * at);
                        Shoot(player, $"action_{kid}_{state}");
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

        private static void Shoot(PlayerController player, string name)
        {
            var cam = Camera.main;
            var controller = cam.GetComponent<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (controller != null) controller.enabled = false;
            var p = player.transform;
            cam.transform.position = p.position + p.right * 2.4f + p.forward * 1.0f + Vector3.up * 1.0f;
            cam.transform.LookAt(p.position + Vector3.up * 0.65f);
            var rt = new RenderTexture(640, 640, 24);
            cam.targetTexture = rt;
            cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(640, 640, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 640, 640), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Out);
            File.WriteAllBytes(Path.Combine(Out, name + ".jpg"), tex.EncodeToJPG(86));
            RenderTexture.active = old;
            cam.targetTexture = null;
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(tex);
            if (controller != null) controller.enabled = true;
        }
    }
}
