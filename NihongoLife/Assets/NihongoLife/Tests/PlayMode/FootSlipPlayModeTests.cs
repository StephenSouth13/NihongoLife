using System.Collections;
using System.Collections.Generic;
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
    /// Walk and run cadence must match the ground speed: while a foot is planted it should stay put in the world.
    /// Measures the planted foot's horizontal speed against the body's speed (slip ratio) for both Kids walking and
    /// sprinting in the city. Report: Bao_Cao/character-regression/foot_slip.txt
    /// </summary>
    public class FootSlipPlayModeTests
    {
        private static string Out => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/character-regression"));

        [UnityTest, Timeout(240000)]
        public IEnumerator Feet_DoNotSlide_WalkAndRun()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            var player = Object.FindFirstObjectByType<PlayerController>();
            var input = GameInputService.GetOrCreate();
            string original = PlayableCharacterCatalog.GetSelected().Id;
            var report = new System.Text.StringBuilder();
            float worst = 0f;
            Vector3 home = player.transform.position;
            try
            {
                foreach (var kid in new[] { "kid_haruto", "kid_yui" })
                {
                    PlayableCharacterCatalog.ApplyVisual(player.gameObject, PlayableCharacterCatalog.FindById(kid));
                    yield return new WaitForSecondsRealtime(1.5f);
                    foreach (var (mode, sprint) in new[] { ("walk", false), ("run", true) })
                    {
                        Teleport(player, home);
                        yield return new WaitForSecondsRealtime(0.3f);
                        input.SetMobileButton(GameInputId.Sprint, sprint);
                        input.SetMobileMove(Vector2.up);
                        yield return new WaitForSecondsRealtime(0.7f); // reach speed
                        var animator = player.GetComponentInChildren<Animator>();
                        var feet = new[] { animator.GetBoneTransform(HumanBodyBones.LeftFoot), animator.GetBoneTransform(HumanBodyBones.RightFoot) };
                        var slips = new List<float>();
                        float bodySpeed = 0f; int bodySamples = 0;
                        Vector3 lastBody = player.transform.position;
                        var lastFeet = feet.Select(f => f.position).ToArray();
                        for (int i = 0; i < 70; i++)
                        {
                            yield return null;
                            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
                            Vector3 body = player.transform.position;
                            float v = Flat(body - lastBody).magnitude / dt;
                            if (v > 0.2f) { bodySpeed += v; bodySamples++; }
                            float ground = Mathf.Min(feet[0].position.y, feet[1].position.y);
                            for (int f = 0; f < 2; f++)
                            {
                                // The planted foot: the lower one, and not lifting.
                                if (feet[f].position.y <= ground + 0.012f && Mathf.Abs(feet[f].position.y - lastFeet[f].y) < 0.004f)
                                    slips.Add(Flat(feet[f].position - lastFeet[f]).magnitude / dt);
                                lastFeet[f] = feet[f].position;
                            }
                            lastBody = body;
                        }
                        input.SetMobileMove(Vector2.zero);
                        input.SetMobileButton(GameInputId.Sprint, false);
                        bodySpeed = bodySamples > 0 ? bodySpeed / bodySamples : 0f;
                        slips.Sort();
                        float slip = slips.Count > 0 ? slips[slips.Count / 2] : 0f;
                        float ratio = bodySpeed > 0.1f ? slip / bodySpeed : 0f;
                        string clip = player.GetComponent<KidMoodAnimator>()?.WalkClipName ?? "?";
                        string line = $"{kid} {mode}: body {bodySpeed:0.00} m/s · planted foot {slip:0.00} m/s ({ratio * 100:0}% slip, n={slips.Count}) · clip {clip} · animator speed {animator.speed:0.00}";
                        Debug.Log("[FootSlip] " + line);
                        report.AppendLine(line);
                        worst = Mathf.Max(worst, ratio);
                        yield return new WaitForSecondsRealtime(0.3f);
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
            File.WriteAllText(Path.Combine(Out, "foot_slip.txt"), report.ToString());
            // Chibi legs take ~0.3 m steps while the game moves 2.8–5.2 m/s, so some slide is unavoidable; quicker steps
            // (CharacterAnimationController.CadenceFor) keep it from looking like gliding. Before 2026-10-20: 92% walk, 95% run.
            Assert.Less(worst, 0.88f, "Planted feet slide (cadence does not match ground speed):\n" + report);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static void Teleport(PlayerController player, Vector3 position)
        {
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.position = position;
            body.enabled = true;
            Physics.SyncTransforms();
        }
    }
}
