using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.NPC;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Town regression for 90_TestSandbox after the konbini / entrance pass: no duplicated orphan roots,
    /// a real store with its cashier and quest items inside, walkable entrances that load the right zone,
    /// and the map. Captures go to Bao_Cao/city-regression.
    /// </summary>
    public class CityTownPlayModeTests
    {
        private static readonly string[] OrphanNames = { "DoorMat", "FacadeTrim", "WarmWindow_L", "WarmWindow_R", "Collision_Footprint", "WarmRoadLight", "Visual" };

        [UnityTest]
        public IEnumerator Town_KonbiniEntrancesAndMap()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var city = SceneManager.GetActiveScene();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            Assert.NotNull(player);

            foreach (var root in city.GetRootGameObjects())
                Assert.IsFalse(OrphanNames.Contains(root.name), "Orphan root still in the city: " + root.name);
            Capture("01_spawn");

            // Konbini: cashier behind the counter, quest items inside the building.
            var cashier = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).Single(n => n.NpcId == "npc_cashier");
            Assert.That(cashier.transform.position.z, Is.InRange(8.8f, 9.8f), "Cashier must stand behind the counter.");
            foreach (var item in Object.FindObjectsByType<InteractiveItem>(FindObjectsSortMode.None).Where(i => i.name == "Onigiri" || i.name == "Water" || i.name == "Tea"))
            {
                Vector3 p = item.transform.position;
                Assert.IsTrue(Mathf.Abs(p.x) < 4.6f && p.z > 0.6f && p.z < 9.9f, item.name + " must be inside the store: " + p);
            }

            Teleport(player, new Vector3(-1.6f, 0.08f, -6.5f), 10f);
            yield return new WaitForSecondsRealtime(1.2f);
            Capture("02_konbini_front");

            // Walk through the real door: the store door trigger opens and lets the player in.
            var door = Object.FindFirstObjectByType<DoorInteractable>();
            Teleport(player, new Vector3(0f, 0.08f, -0.9f), 0f);
            yield return new WaitForSecondsRealtime(0.3f);
            door.Interact(player.gameObject);
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.Greater(player.transform.position.z, 0.6f, "Door must let the player into the store.");
            Capture("03_konbini_inside");

            // Aisles are free of invisible walls: a sweep down the west aisle must not be blocked.
            var body = player.GetComponent<CharacterController>();
            player.InputLocked = true;
            Teleport(player, new Vector3(-1.9f, 0.08f, 1.6f), 0f);
            body.Move(Vector3.forward * 5.5f);
            yield return null;
            Assert.Greater(player.transform.position.z, 6.5f, "Store aisle must be walkable (old invisible walls removed).");
            // ...but the shell walls stop the player.
            body.Move(Vector3.left * 6f);
            yield return null;
            Assert.Greater(player.transform.position.x, -4.7f, "Store wall must stop the player.");
            player.InputLocked = false;

            Teleport(player, new Vector3(-1.9f, 0.08f, 4.2f), -60f);
            yield return new WaitForSecondsRealtime(0.8f);
            Capture("04_konbini_shelves");
            Teleport(player, new Vector3(0.6f, 0.08f, 6.6f), 0f);
            yield return new WaitForSecondsRealtime(0.8f);
            Capture("05_konbini_cashier");

            // Signposts and entrances.
            Teleport(player, new Vector3(4.6f, 0.08f, -8.3f), 0f);
            yield return new WaitForSecondsRealtime(0.8f);
            Capture("06_signpost");

            string[] portals = { "StationPortal", "SushiPortal", "SchoolPortal", "HomeBedroomPortal" };
            string[] scenes = { WorldLocationCatalog.StationScene, WorldLocationCatalog.SushiRestaurantScene, WorldLocationCatalog.SchoolScene, WorldLocationCatalog.HomeBedroomScene };
            string[] returns = { WorldLocationCatalog.CityStationReturn, WorldLocationCatalog.CitySushiReturn, WorldLocationCatalog.CitySchoolReturn, WorldLocationCatalog.CityHomeBedroomReturn };
            for (int i = 0; i < portals.Length; i++)
            {
                var portal = GameObject.Find("AdditiveZonePortals/" + portals[i]).GetComponent<ScenePortal>();
                var spawn = Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None).First(s => s.Id == returns[i]);
                Assert.IsTrue(Physics.Raycast(spawn.transform.position + Vector3.up, Vector3.down, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore), portals[i] + " return spawn needs ground.");
                // Walk up to the door (spawn → portal): the portal must be the nearest interaction.
                Vector3 doorstep = Vector3.Lerp(spawn.transform.position, new Vector3(portal.transform.position.x, spawn.transform.position.y, portal.transform.position.z), 0.6f);
                Teleport(player, doorstep, spawn.transform.eulerAngles.y + 180f);
                yield return new WaitForSecondsRealtime(1.0f);
                var detector = player.GetComponent<InteractionDetector>();
                Assert.AreSame(portal, detector.CurrentInteractable as ScenePortal, $"{portals[i]} must be the interaction in front of its entrance (got {(detector.CurrentInteractable as MonoBehaviour)?.name}).");
                Capture($"07_entrance_{portals[i].Replace("Portal", "").ToLowerInvariant()}");
                portal.Interact(player.gameObject);
                yield return WaitForFlow(flow);
                Assert.AreEqual(scenes[i], SceneManager.GetActiveScene().name, portals[i] + " must load its zone.");
                yield return new WaitForSecondsRealtime(1.5f);
                Capture($"08_zone_{portals[i].Replace("Portal", "").ToLowerInvariant()}");
                flow.ExitZone(scenes[i], returns[i]);
                yield return WaitForFlow(flow);
                Assert.AreEqual(city, SceneManager.GetActiveScene());
            }

            // Map overview uses real coordinates.
            var map = Object.FindFirstObjectByType<WorldMapUI>();
            Teleport(player, new Vector3(0f, 0.08f, -13.5f), 0f);
            GameObject.Find("MapButton").GetComponent<Button>().onClick.Invoke();
            yield return null;
            var tabs = map.GetComponentInParent<Canvas>().GetComponentsInChildren<Button>().Where(x => x.name.StartsWith("MapTab_")).ToArray();
            tabs.Last().onClick.Invoke();
            yield return null;
            Capture("09_map_overview");
            map.SetVisible(false);
        }

        private static void Teleport(PlayerController player, Vector3 position, float yaw)
        {
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            body.enabled = true;
            Physics.SyncTransforms();
            var camera = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (camera != null) camera.SetOrbit(yaw, 16f, 4.2f);
        }

        private static IEnumerator WaitForFlow(SceneFlowController flow)
        {
            float timeout = Time.realtimeSinceStartup + 30f;
            yield return null;
            while (flow.IsLoading && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.IsFalse(flow.IsLoading, "Transition timed out.");
        }

        private static void Capture(string name)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/city-regression"));
            Directory.CreateDirectory(folder);
            var camera = Camera.main;
            if (camera == null) return;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(x => x.isRootCanvas && x.renderMode == RenderMode.ScreenSpaceOverlay).OrderBy(x => x.sortingOrder).ToArray();
            var target = new RenderTexture(1600, 900, 24);
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i);
            }
            Canvas.ForceUpdateCanvases();
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG());
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
            Object.Destroy(texture);
            target.Release();
            Object.Destroy(target);
        }
    }
}
