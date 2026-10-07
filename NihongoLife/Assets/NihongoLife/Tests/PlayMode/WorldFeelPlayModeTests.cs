using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Round 7 checks in the city: the horizon backdrop hides the end of the map (day, dusk and night
    /// captures looking off the city edge), sprinting drains stamina until the player is exhausted, and
    /// the posture stabiliser keeps the walking player's back upright. Captures go to Bao_Cao/world-regression.
    /// </summary>
    public class WorldFeelPlayModeTests
    {
        [UnityTest]
        public IEnumerator City_HorizonStaminaPosture()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            Assert.NotNull(player);

            // 1) Horizon: off the city edge there must be scenery and haze, not a black void.
            var backdrop = Object.FindFirstObjectByType<HorizonBackdrop>();
            Assert.NotNull(backdrop, "The city needs its Horizon_Backdrop.");
            var main = Camera.main;
            var follow = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (follow != null) follow.enabled = false;
            var view = new GameObject("HorizonProbe").AddComponent<Camera>();
            view.CopyFrom(main);
            view.enabled = false;
            var cycle = Object.FindFirstObjectByType<DayNightCycle>();
            (float hour, string name)[] times = { (10f, "day"), (18.2f, "dusk"), (22.5f, "night") };
            foreach (var (hour, label) in times)
            {
                if (cycle != null) cycle.SetHour(hour);
                foreach (var (pos, yaw, pitch, shot) in new[]
                {
                    (new Vector3(52f, 9f, 0f), 90f, 4f, "east_edge"),
                    (new Vector3(-52f, 9f, 20f), -90f, 4f, "west_edge"),
                    (new Vector3(0f, 34f, -60f), 35f, 12f, "high_street"),
                })
                {
                    // Move the main camera too: the backdrop follows Camera.main.
                    main.transform.SetPositionAndRotation(pos, Quaternion.Euler(pitch, yaw, 0f));
                    view.transform.SetPositionAndRotation(pos, Quaternion.Euler(pitch, yaw, 0f));
                    yield return null;
                    yield return null;
                    var pixels = Capture($"horizon_{label}_{shot}", view);
                    if (label == "day") Assert.Less(VoidFraction(pixels), 0.02f, $"Black void visible at {shot} in daylight.");
                }
            }
            Object.Destroy(view.gameObject);
            if (follow != null) follow.enabled = true;
            if (cycle != null) cycle.SetHour(10f);

            // 2) Stamina: sprinting costs energy faster than it regenerates, and an empty bar blocks sprint.
            var status = PlayerStatus.Instance;
            Assert.NotNull(status);
            status.RestoreNeeds(0f, 0f, 100f);
            float start = status.CurrentEnergy;
            float elapsed = 0f;
            while (elapsed < 2f)
            {
                bool sprinting = status.CanSprint && status.ConsumeEnergy(14f * Time.deltaTime);
                status.ReportActivity(true, sprinting);
                elapsed += Time.deltaTime;
                yield return null;
            }
            Assert.Less(status.CurrentEnergy, start - 15f, "Two seconds of sprint must visibly drain stamina.");
            for (float t = 0f; t < 15f && !status.IsExhausted; t += Time.deltaTime)
            {
                bool sprinting = status.CanSprint && status.ConsumeEnergy(14f * Time.deltaTime);
                status.ReportActivity(true, sprinting);
                yield return null;
            }
            Assert.IsTrue(status.IsExhausted, "Sprinting until empty must exhaust the player.");
            Assert.IsFalse(status.CanSprint, "No sprinting while exhausted.");
            float empty = status.CurrentEnergy;
            yield return new WaitForSeconds(2.5f);
            status.ReportActivity(false, false);
            Assert.Greater(status.CurrentEnergy, empty + 5f, "Standing still recovers stamina.");

            // 3) Posture: the stabiliser runs on the player and keeps the spine near vertical while walking.
            var posture = player.GetComponent<PostureStabilizer>();
            Assert.NotNull(posture, "PlayerController must add PostureStabilizer.");
            var animator = player.GetComponentInChildren<Animator>();
            var anim = player.GetComponentInChildren<CharacterAnimationController>();
            posture.enabled = false;
            var raw = new Sway();
            yield return WalkAndMeasure(player, animator, raw, "raw");
            posture.enabled = true;
            var fixedPose = new Sway();
            yield return WalkAndMeasure(player, animator, fixedPose, "upright");
            Assert.IsTrue(posture.IsActive, "The player rig must be humanoid so the stabiliser can act.");
            Debug.Log($"[WorldFeel] Walk sway raw → stabilised: hips roll {raw.Hips:0.0}° → {fixedPose.Hips:0.0}°, " +
                      $"shoulder roll {raw.Shoulders:0.0}° → {fixedPose.Shoulders:0.0}°, spine lean {raw.Spine:0.0}° → {fixedPose.Spine:0.0}°, " +
                      $"narrowest feet gap {raw.FeetGap * 100f:0} cm → {fixedPose.FeetGap * 100f:0} cm");
            Assert.Greater(fixedPose.FeetGap, 0.08f, "Feet must not cross onto one line while walking.");
            Assert.LessOrEqual(fixedPose.Shoulders, raw.Shoulders + 0.5f, "The stabiliser must not add shoulder roll.");
            Assert.Less(fixedPose.Shoulders, 7f, "Shoulders stay level while walking.");
            Assert.Less(fixedPose.Spine, 6f, "The back must stay upright (no swaying walk).");
            animator.SetFloat("Speed", 0f);
        }

        private sealed class Sway { public float Hips, Shoulders, Spine, FeetGap = 99f; }

        /// <summary>Walks in place for 3 s at full walk speed, records the worst sideways roll of the pelvis
        /// (hip joints line), the shoulder line and the neck-over-hips lean, and saves a 6-frame front/back
        /// filmstrip. Measured in the coroutine slot after Update, when bones still hold the previous frame's
        /// final pose (after PostureStabilizer).</summary>
        private static IEnumerator WalkAndMeasure(PlayerController player, Animator animator, Sway sway, string label)
        {
            var camera = new GameObject("WalkProbe").AddComponent<Camera>();
            camera.CopyFrom(Camera.main);
            camera.enabled = false;
            Transform t = player.transform;
            float start = Time.time;
            int shot = 0;
            while (Time.time - start < 3f)
            {
                animator.SetFloat("Speed", 1f);
                yield return null;
                if (Time.time - start < 0.8f) continue; // let the Idle → Walk transition finish
                Vector3 hipLine = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position - animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position;
                Vector3 shoulderLine = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position - animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
                Vector3 spine = animator.GetBoneTransform(HumanBodyBones.Neck).position - animator.GetBoneTransform(HumanBodyBones.Hips).position;
                sway.Hips = Mathf.Max(sway.Hips, Roll(hipLine, t));
                sway.Shoulders = Mathf.Max(sway.Shoulders, Roll(shoulderLine, t));
                sway.Spine = Mathf.Max(sway.Spine, Vector3.Angle(Vector3.ProjectOnPlane(spine, t.forward), t.up));
                Vector3 feet = animator.GetBoneTransform(HumanBodyBones.RightFoot).position - animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                sway.FeetGap = Mathf.Min(sway.FeetGap, Vector3.Dot(feet, t.right));
                if (shot < 6 && Time.time - start > 0.8f + shot * 0.18f)
                {
                    Vector3 chest = t.position + Vector3.up * 1.0f;
                    camera.transform.position = chest + t.forward * 2.6f;
                    camera.transform.LookAt(chest);
                    Capture($"walk_{label}_front_{shot}", camera);
                    camera.transform.position = chest - t.forward * 2.6f;
                    camera.transform.LookAt(chest);
                    Capture($"walk_{label}_back_{shot}", camera);
                    shot++;
                }
            }
            Object.Destroy(camera.gameObject);
        }

        /// <summary>Angle of a left→right body line out of the horizontal, seen from the front.</summary>
        private static float Roll(Vector3 line, Transform body)
        {
            Vector3 flat = Vector3.ProjectOnPlane(line, body.forward);
            return Mathf.Abs(90f - Vector3.Angle(flat, body.up));
        }

        private static float VoidFraction(Color32[] pixels)
        {
            int dark = pixels.Count(p => p.r < 10 && p.g < 10 && p.b < 12);
            return dark / (float)pixels.Length;
        }

        private static Color32[] Capture(string name, Camera camera)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/world-regression"));
            Directory.CreateDirectory(folder);
            var target = new RenderTexture(1600, 900, 24);
            var oldActive = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG());
            var pixels = texture.GetPixels32();
            camera.targetTexture = null;
            RenderTexture.active = oldActive;
            Object.Destroy(texture);
            target.Release();
            Object.Destroy(target);
            return pixels;
        }
    }
}
