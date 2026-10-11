using System;
using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Looking out of every scene: a camera inside each scene looks at each scene portal (what the player sees when
    /// walking to a door), and Hibari / Midori are shot at five hours of the day/night cycle. The capture folder comes
    /// from NL_VIEWS_OUT (default Bao_Cao/environment-regression).
    /// </summary>
    public class EnvironmentViewsPlayModeTests
    {
        private static string Folder => Environment.GetEnvironmentVariable("NL_VIEWS_OUT")
            ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/environment-regression"));

        private static readonly string[] Scenes =
        {
            WorldLocationCatalog.CityScene, WorldLocationCatalog.StationScene, WorldLocationCatalog.SushiRestaurantScene,
            WorldLocationCatalog.SchoolScene, WorldLocationCatalog.HomeBedroomScene, WorldLocationCatalog.GameCenterScene,
            WorldLocationCatalog.MidoriIslandScene,
        };

        [UnityTest]
        public IEnumerator Portals_LookingOutOfEveryScene()
        {
            foreach (var scene in Scenes)
            {
                yield return Load(scene);
                var player = Object.FindFirstObjectByType<PlayerController>();
                var cam = Camera.main;
                Assert.NotNull(cam, scene + " camera");
                // Zones load over the city: only this scene's own doors.
                var portals = Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.InstanceID).Where(p => p.gameObject.scene.name == scene).ToArray();
                Object.FindFirstObjectByType<DayNightCycle>()?.SetHour(12f);
                yield return null;
                Debug.Log($"[Views] {scene}: {portals.Length} portals");
                int i = 0;
                foreach (var portal in portals)
                {
                    Vector3 door = portal.transform.position;
                    Vector3 from = player != null ? player.transform.position : door + portal.transform.forward * 5f;
                    Vector3 dir = Vector3.ProjectOnPlane(from - door, Vector3.up);
                    if (dir.sqrMagnitude < 0.25f) dir = portal.transform.forward;
                    Vector3 eye = door + dir.normalized * 4.5f + Vector3.up * 1.7f;
                    Shoot(cam, eye, door + Vector3.up * 1.3f, $"{Safe(scene)}_portal{i++}_{Safe(portal.name)}");
                }
            }
        }

        [UnityTest]
        public IEnumerator DayNight_HibariAndMidori()
        {
            foreach (var scene in new[] { WorldLocationCatalog.CityScene, WorldLocationCatalog.MidoriIslandScene })
            {
                yield return Load(scene);
                var cycle = Object.FindFirstObjectByType<DayNightCycle>();
                Assert.NotNull(cycle, "Day/night cycle in " + scene);
                var player = Object.FindFirstObjectByType<PlayerController>();
                var cam = Camera.main;
                Vector3 p = player.transform.position;
                Vector3 f = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
                var outdoors = OutdoorSky.Active;
                Assert.NotNull(outdoors, "OutdoorSky in " + scene);
                foreach (float hour in new[] { 6f, 12f, 17.5f, 20f, 23.5f, 2f })
                {
                    cycle.SetHour(hour);
                    yield return null;
                    yield return null;
                    Debug.Log($"[Views] {scene} {hour:00.0}h daylight={outdoors.Daylight:0.00} lampsOn={outdoors.LampsOn} sun={RenderSettings.sun?.intensity:0.00} amb={RenderSettings.ambientLight} fog={RenderSettings.fogDensity:0.0000}");
                    if (hour >= 23f || hour < 3f) Assert.Greater(outdoors.LampsOn, 0, "Lamps are on at night in " + scene);
                    if (Mathf.Approximately(hour, 12f)) Assert.AreEqual(0, outdoors.LampsOn, "Lamps are off at noon in " + scene);
                    Shoot(cam, p - f * 7f + Vector3.up * 3.2f, p + f * 14f + Vector3.up * 2.5f, $"{Safe(scene)}_hour{hour:00.0}");
                }
            }
        }

        private static IEnumerator Load(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene);
            for (float t = 0f; t < 20f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            yield return new WaitForSecondsRealtime(0.5f);
        }

        private static string Safe(string s) => new string(s.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

        /// <summary>Renders the world only (HUD hidden: the camera does not draw overlay canvases).</summary>
        private static void Shoot(Camera cam, Vector3 eye, Vector3 target, string name)
        {
            var oldPos = cam.transform.position;
            var oldRot = cam.transform.rotation;
            var controller = cam.GetComponent<NihongoLife.Cameras.ThirdPersonCameraController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            cam.transform.position = eye;
            cam.transform.LookAt(target);
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
            cam.transform.SetPositionAndRotation(oldPos, oldRot);
            if (controller != null) controller.enabled = wasEnabled;
        }
    }
}
