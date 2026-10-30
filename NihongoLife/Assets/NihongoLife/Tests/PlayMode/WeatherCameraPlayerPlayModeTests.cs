using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Cameras;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.NPC;
using NihongoLife.Player;
using NihongoLife.Scenario;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Hibari weather, the Sims-style overview camera and the kids as player characters, in the real city scene.
    /// Captures: Bao_Cao/environment-regression (weather_*, overview_*).
    /// </summary>
    public class WeatherCameraPlayerPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/environment-regression"));

        [UnityTest]
        public IEnumerator Rain_HidesOutdoorNpcs_SlowsRecovery_CloudsDrift()
        {
            yield return LoadCity();
            var weather = WeatherCycle.Instance;
            Assert.NotNull(weather, "WeatherCycle on AppRoot");
            var cycle = Object.FindFirstObjectByType<DayNightCycle>();
            cycle.SetHour(13f);
            var sky = OutdoorSky.Active;
            Assert.NotNull(sky);
            var skyMaterial = RenderSettings.skybox;
            float rotation0 = skyMaterial.GetFloat("_Rotation");
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreNotEqual(rotation0, skyMaterial.GetFloat("_Rotation"), "Clouds drift");

            var player = Object.FindFirstObjectByType<PlayerController>();
            var status = PlayerStatus.Instance;
            int npcsOutdoorsBefore = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).Length;
            Shoot("weather_clear_13h");
            float clearGain = 0f;
            yield return MeasureRecovery(status, g => clearGain = g);

            weather.Force(WeatherCycle.Kind.Rain);
            yield return new WaitForSecondsRealtime(2.2f);
            Assert.IsTrue(weather.IsRaining);
            Assert.AreEqual(1f, sky.Rain, 0.01f, "Sky sees the rain");
            Assert.Greater(sky.RainParticlesPerSecond, 100f, "Rain falls around the camera");
            Assert.Greater(skyMaterial.GetFloat("_Overcast"), 0.9f, "Overcast sky");
            Assert.IsTrue(WeatherCycle.PlayerInRain, "The player stands in the rain on the plaza");
            Assert.Greater(weather.HiddenNpcCount, 0, "Outdoor NPCs went in");
            var node = ScenarioManager.Instance != null ? ScenarioManager.Instance.CurrentNode : null;
            string needed = node == null ? null : node.nodeType == ScenarioNodeType.TalkToNPC ? node.targetNpcId : node.speakerId;
            var visible = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None);
            Debug.Log($"[Weather] rain: hidden {weather.HiddenNpcCount}, visible {string.Join(", ", visible.Select(n => n.NpcId))}, story needs '{needed}'");
            if (!string.IsNullOrEmpty(needed) && npcsOutdoorsBefore > 0)
                Assert.IsTrue(visible.Any(n => n.NpcId == needed) || !Object.FindObjectsByType<NPCController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(n => n.NpcId == needed),
                    "The NPC the story needs is never hidden");
            Assert.IsTrue(visible.Any(n => n.NpcId == "npc_cashier"), "The cashier inside the konbini stays");
            Shoot("weather_rain_13h");
            float rainGain = 0f;
            yield return MeasureRecovery(status, g => rainGain = g);
            Debug.Log($"[Weather] energy gained in 2 s: clear {clearGain:0.0}, rain {rainGain:0.0}");
            Assert.Less(rainGain, clearGain * 0.75f, "Recovery is slower in the rain");

            weather.Force(WeatherCycle.Kind.Clear);
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsFalse(weather.IsRaining);
            Assert.AreEqual(0, weather.HiddenNpcCount, "Everyone is back after the rain");
            Assert.GreaterOrEqual(Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).Length, npcsOutdoorsBefore, "Nobody is still missing after the rain (town folk may finish spawning meanwhile)");
            cycle.SetHour(20.5f);
            weather.Force(WeatherCycle.Kind.Rain);
            yield return new WaitForSecondsRealtime(1f);
            Shoot("weather_rain_20h");
            weather.Force(null);
        }

        private static IEnumerator MeasureRecovery(PlayerStatus status, System.Action<float> done)
        {
            status.ConsumeEnergy(status.CurrentEnergy - 30f);
            yield return new WaitForSecondsRealtime(1.2f); // past the post-sprint delay
            float start = status.CurrentEnergy;
            yield return new WaitForSecondsRealtime(2f);
            done(status.CurrentEnergy - start);
        }

        [UnityTest]
        public IEnumerator OverviewCamera_SnapRotationZoomLevels_FreeCursor()
        {
            yield return LoadCity();
            CursorDirector.AssumeFocusForTests = true;
            try
            {
                var camera = Object.FindFirstObjectByType<ThirdPersonCameraController>();
                Assert.IsTrue(camera.SetOverview(true), "Overview outdoors");
                yield return new WaitForSecondsRealtime(1.5f);
                var player = Object.FindFirstObjectByType<PlayerController>();
                Vector3 toCamera = camera.transform.position - (player.transform.position + Vector3.up * 1.6f);
                float pitch = Mathf.Asin(toCamera.normalized.y) * Mathf.Rad2Deg;
                Debug.Log($"[Overview] pitch {pitch:0}°, distance {toCamera.magnitude:0.0} m, yaw {camera.Yaw:0}");
                Assert.That(pitch, Is.InRange(45f, 58f), "High three-quarter view");
                Assert.That(toCamera.magnitude, Is.InRange(13f, 17f), "Medium zoom level");
                Assert.AreEqual(45f, Mathf.Repeat(camera.Yaw, 90f), 1f, "On a diagonal");
                Assert.IsFalse(CursorDirector.GameplayLook, "Free cursor in the overview (Sims-style)");
                Assert.IsTrue(Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).Any(r => r.name == "OverviewControls" && r.gameObject.activeInHierarchy), "Rotate/zoom controls on the HUD (open overlay: " + UiModalStack.TopName + ")");
                Shoot("overview_medium", useGameCamera: true);
                float yaw = camera.Yaw;
                camera.RotateOverview(1);
                camera.ZoomOverview(1);
                yield return new WaitForSecondsRealtime(1.5f);
                Assert.AreEqual(90f, Mathf.DeltaAngle(yaw, camera.Yaw), 1f, "Rotates by a quarter turn");
                Assert.That((camera.transform.position - (player.transform.position + Vector3.up * 1.6f)).magnitude, Is.InRange(21f, 26f), "Far zoom level");
                Shoot("overview_far_rotated", useGameCamera: true);
                camera.SetOverview(false);
                yield return new WaitForSecondsRealtime(0.8f);
                Assert.IsFalse(camera.OverviewMode);
                Assert.IsTrue(CursorDirector.GameplayLook, "Back to mouse-look");
            }
            finally { CursorDirector.AssumeFocusForTests = false; }
        }

        [UnityTest]
        public IEnumerator Kids_DefaultCharacter_PlayerRingOnly()
        {
            string key = PlayableCharacterCatalog.PlayerPrefsKey;
            string original = PlayerPrefs.GetString(key, "");
            int migrated = PlayerPrefs.GetInt("NihongoLife.KidsDefaultMigrated", 0);
            try
            {
                // An old save that stored the previous default moves to the new one once.
                PlayerPrefs.SetString(key, "student");
                PlayerPrefs.SetInt("NihongoLife.KidsDefaultMigrated", 0);
                Assert.AreEqual("kid_haruto", PlayableCharacterCatalog.GetSelected().Id, "Old default → Haruto");
                PlayerPrefs.SetString(key, "student");
                Assert.AreEqual("kid_haruto", PlayableCharacterCatalog.GetSelected().Id, "The adult Tanaka is gone: always a Kid");
                PlayerPrefs.SetString(key, "kid_haruto");

                yield return LoadCity();
                var player = Object.FindFirstObjectByType<PlayerController>();
                var animator = player.transform.Find("Visual").GetComponentInChildren<Animator>();
                Assert.AreEqual("NL_Kid_Boy", animator.runtimeAnimatorController.name, "The city starts with Haruto");
                var marker = player.GetComponent<PlayerMarker>();
                Assert.NotNull(marker, "The player wears the ground ring");
                Assert.IsTrue(marker.Ring.gameObject.activeInHierarchy);
                Assert.AreEqual(PlayableCharacterCatalog.FindById("kid_haruto").AccentColor, marker.Color, "Ring in the character's colour");
                Assert.IsFalse(Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).Any(n => n.GetComponent<PlayerMarker>() != null), "NPCs never get the ring");
                Shoot("player_ring_haruto");
            }
            finally
            {
                if (string.IsNullOrEmpty(original)) PlayerPrefs.DeleteKey(key); else PlayerPrefs.SetString(key, original);
                PlayerPrefs.SetInt("NihongoLife.KidsDefaultMigrated", migrated);
                PlayerPrefs.Save();
            }
        }

        // ─────────── helpers ───────────

        private static IEnumerator LoadCity()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            yield return new WaitForSecondsRealtime(0.5f);
        }

        /// <summary>World shot: behind the player along the plaza (or the game camera as it is, for the overview).</summary>
        private static void Shoot(string name, bool useGameCamera = false)
        {
            var cam = Camera.main;
            var player = Object.FindFirstObjectByType<PlayerController>();
            var controller = cam.GetComponent<ThirdPersonCameraController>();
            var oldPos = cam.transform.position;
            var oldRot = cam.transform.rotation;
            bool enabled = controller != null && controller.enabled;
            if (!useGameCamera)
            {
                if (controller != null) controller.enabled = false;
                Vector3 f = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
                cam.transform.position = player.transform.position - f * 6f + Vector3.up * 2.6f;
                cam.transform.LookAt(player.transform.position + f * 10f + Vector3.up * 2.2f);
            }
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Path.Combine(Folder, name + ".jpg"), tex.EncodeToJPG(88));
            RenderTexture.active = old;
            cam.targetTexture = null;
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(tex);
            if (!useGameCamera)
            {
                cam.transform.SetPositionAndRotation(oldPos, oldRot);
                if (controller != null) controller.enabled = enabled;
            }
        }
    }
}
