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
            float rawTilt = 0f;
            yield return WalkAndMeasure(player, animator, anim, 90, t => rawTilt = Mathf.Max(rawTilt, t));
            posture.enabled = true;
            float worstTilt = 0f;
            yield return WalkAndMeasure(player, animator, anim, 90, t => worstTilt = Mathf.Max(worstTilt, t));
            Assert.IsTrue(posture.IsActive, "The player rig must be humanoid so the stabiliser can act.");
            Debug.Log($"[WorldFeel] Worst sideways neck-over-hips tilt while walking: raw {rawTilt:0.0}° → stabilised {worstTilt:0.0}°");
            Assert.LessOrEqual(worstTilt, rawTilt + 0.1f, "The stabiliser must not add sway.");
            Assert.Less(worstTilt, 6f, "The back must stay upright (no swaying walk).");
            CaptureWalk(player);
            anim?.SetSpeed(0f);
        }

        private static IEnumerator WalkAndMeasure(PlayerController player, Animator animator, CharacterAnimationController anim, int frames, System.Action<float> report)
        {
            for (int frame = 0; frame < frames; frame++)
            {
                anim?.SetSpeed(1f);
                yield return new WaitForEndOfFrame();
                if (animator == null) continue;
                var top = animator.GetBoneTransform(HumanBodyBones.Neck);
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                if (top == null || hips == null) continue;
                Vector3 spine = top.position - hips.position;
                Vector3 side = Vector3.ProjectOnPlane(spine, player.transform.forward);
                if (frame > 20) report(Vector3.Angle(side, player.transform.up));
            }
        }

        private static float VoidFraction(Color32[] pixels)
        {
            int dark = pixels.Count(p => p.r < 10 && p.g < 10 && p.b < 12);
            return dark / (float)pixels.Length;
        }

        private static void CaptureWalk(PlayerController player)
        {
            var camera = new GameObject("PostureProbe").AddComponent<Camera>();
            camera.CopyFrom(Camera.main);
            camera.enabled = false;
            Vector3 chest = player.transform.position + Vector3.up * 1.1f;
            camera.transform.position = chest + player.transform.forward * 3.2f + Vector3.up * 0.2f;
            camera.transform.LookAt(chest);
            Capture("posture_front", camera);
            camera.transform.position = chest + player.transform.right * 3.2f + Vector3.up * 0.2f;
            camera.transform.LookAt(chest);
            Capture("posture_side", camera);
            Object.Destroy(camera.gameObject);
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
