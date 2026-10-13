using System.Collections;
using System.IO;
using System.Linq;
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
    /// The player's feet stand on the ground while idle, walking and running (no sinking, no floating): measures the
    /// lowest foot/toe bone against a raycast to the floor under it, for a boy and a girl, and photographs the feet.
    /// Captures: Bao_Cao/character-regression/feet_*.jpg
    /// </summary>
    public class FootGroundingPlayModeTests
    {
        private static string Out => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/character-regression"));

        [UnityTest, Timeout(240000)]
        public IEnumerator Feet_OnTheGround_IdleWalkRun()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            var player = Object.FindFirstObjectByType<PlayerController>();
            var input = GameInputService.GetOrCreate();
            string original = PlayableCharacterCatalog.GetSelected().Id;
            var report = new System.Text.StringBuilder();
            float worst = 0f, worstIdle = 0f, worstIdleHigh = float.MinValue;
            try
            {
                foreach (var kid in new[] { "kid_haruto", "kid_yui" })
                {
                    PlayableCharacterCatalog.ApplyVisual(player.gameObject, PlayableCharacterCatalog.FindById(kid));
                    yield return new WaitForSecondsRealtime(2.5f); // FootGrounder settles while standing
                    foreach (var (mode, move, sprint) in new[] { ("idle", Vector2.zero, false), ("walk", Vector2.up, false), ("run", Vector2.up, true) })
                    {
                        input.SetMobileMove(move);
                        input.SetMobileButton(GameInputId.Sprint, sprint);
                        yield return new WaitForSecondsRealtime(0.8f);
                        float minGap = float.MaxValue, maxGap = float.MinValue, transformGap = 0f;
                        for (int i = 0; i < 40; i++)
                        {
                            yield return null;
                            var (gap, tgap) = Measure(player);
                            if (float.IsNaN(gap)) continue;
                            minGap = Mathf.Min(minGap, gap); maxGap = Mathf.Max(maxGap, gap); transformGap = tgap;
                        }
                        string line = $"{kid} {mode}: lowest foot to ground {minGap * 100:0.0} … {maxGap * 100:0.0} cm, transform to ground {transformGap * 100:0.0} cm";
                        Debug.Log("[Feet] " + line);
                        report.AppendLine(line);
                        worst = Mathf.Min(worst, minGap);
                        if (mode == "idle") { worstIdle = Mathf.Min(worstIdle, minGap); worstIdleHigh = Mathf.Max(worstIdleHigh, minGap); }
                        if (mode != "run") Shoot(player, $"feet_{kid}_{mode}");
                        input.SetMobileButton(GameInputId.Sprint, false);
                        input.SetMobileMove(Vector2.zero);
                    }
                }
            }
            finally
            {
                input.SetMobileMove(Vector2.zero);
                input.SetMobileButton(GameInputId.Sprint, false);
                PlayableCharacterCatalog.ApplyVisual(player.gameObject, PlayableCharacterCatalog.FindById(original));
            }
            Directory.CreateDirectory(Out);
            File.WriteAllText(Path.Combine(Out, "feet_report.txt"), report.ToString());
            // Walking and running legs dip a little at heel strike; standing still must be on the floor.
            Assert.Greater(worst, -0.03f, "Feet sink into the ground:\n" + report);
            Assert.Greater(worstIdle, -0.015f, "Feet sink into the ground while standing:\n" + report);
            Assert.Less(worstIdleHigh, 0.03f, "Feet float above the ground while standing:\n" + report);
        }

        /// <summary>(lowest foot/toe bone above the floor, player transform above the floor), metres.</summary>
        private static (float, float) Measure(PlayerController player)
        {
            var animator = player.GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman) return (float.NaN, 0f);
            float lowest = float.MaxValue;
            foreach (var b in new[] { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, HumanBodyBones.LeftToes, HumanBodyBones.RightToes })
            {
                var t = animator.GetBoneTransform(b);
                if (t != null) lowest = Mathf.Min(lowest, t.position.y);
            }
            // The mesh sole is below the bone: use the skinned mesh bounds' bottom too.
            foreach (var r in player.GetComponentsInChildren<SkinnedMeshRenderer>()) lowest = Mathf.Min(lowest, r.bounds.min.y);
            Vector3 p = player.transform.position;
            if (!Physics.Raycast(p + Vector3.up * 1f, Vector3.down, out var hit, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return (float.NaN, 0f);
            return (lowest - hit.point.y, p.y - hit.point.y);
        }

        private static void Shoot(PlayerController player, string name)
        {
            var cam = Camera.main;
            var controller = cam.GetComponent<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (controller != null) controller.enabled = false;
            var p = player.transform;
            // From the front, a little above the floor, so a curb beside the player does not hide the shoes.
            cam.transform.position = p.position + p.forward * 1.5f + Vector3.up * 0.45f;
            cam.transform.LookAt(p.position + Vector3.up * 0.3f);
            var rt = new RenderTexture(800, 500, 24);
            cam.targetTexture = rt;
            cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(800, 500, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 800, 500), 0, 0);
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
