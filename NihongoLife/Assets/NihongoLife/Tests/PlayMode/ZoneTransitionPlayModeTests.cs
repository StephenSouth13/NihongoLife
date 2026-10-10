using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.UI;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    public class ZoneTransitionPlayModeTests
    {
        [UnityTest]
        public IEnumerator CityStationBedroomRoundTrip()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            Assert.NotNull(player);
            Assert.NotNull(flow);
            var city = SceneManager.GetActiveScene();
            var environments = city.GetRootGameObjects().Where(x => x.GetComponent<SceneZoneVisibility>() != null).ToArray();
            var activeStates = environments.Select(x => x.activeSelf).ToArray();
            Assert.Greater(environments.Length, 2);
            var map = Object.FindFirstObjectByType<WorldMapUI>();
            Assert.NotNull(map);
            var mapInput1 = NihongoLife.Core.GameInputService.GetOrCreate(); yield return new WaitForFixedUpdate(); mapInput1.SetMobileButton(NihongoLife.Core.GameInputId.Map, true); yield return null; mapInput1.SetMobileButton(NihongoLife.Core.GameInputId.Map, false); yield return null;
            Assert.IsTrue(map.IsVisible);
            yield return null;
            Capture("city-map");
            var areaTab_city = Object.FindFirstObjectByType<WorldMapUI>().GetComponentInParent<Canvas>().GetComponentsInChildren<Button>().First(x => x.name.StartsWith("MapTab_"));
            areaTab_city.onClick.Invoke();
            yield return null;
            Capture("city-topdown");
            areaTab_city.transform.parent.GetComponentsInChildren<Button>().Last(x => x.name.StartsWith("MapTab_")).onClick.Invoke();
            map.SetVisible(false);

            // Loading a station additively must not rescue a player still in the city.
            var cityPosition = player.transform.position;
            player.InputLocked = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.StationScene, LoadSceneMode.Additive);
            yield return new WaitForSecondsRealtime(1f);
            var stationScene = SceneManager.GetSceneByName(WorldLocationCatalog.StationScene);
            foreach (var root in stationScene.GetRootGameObjects())
                foreach (var node in root.GetComponentsInChildren<Transform>(true))
                    Assert.IsFalse(node.GetComponents<Component>().Any(x => x == null), "Missing script on " + node.name);
            Assert.Less(Vector3.Distance(cityPosition, player.transform.position), .1f);
            player.InputLocked = false;
            flow.EnterZone(WorldLocationCatalog.StationScene, WorldLocationCatalog.StationEntrance, "Station");
            yield return WaitForFlow(flow);
            Assert.AreEqual(WorldLocationCatalog.StationScene, SceneManager.GetActiveScene().name);
            Assert.Greater(player.transform.position.x, 740f);
            Assert.IsTrue(environments.All(x => !x.activeSelf), "City geometry/collision must be hidden.");
            yield return new WaitForSecondsRealtime(2f);
            Assert.Greater(player.transform.position.x, 740f);
            Capture("station");
            var mapInput2 = NihongoLife.Core.GameInputService.GetOrCreate(); yield return new WaitForFixedUpdate(); mapInput2.SetMobileButton(NihongoLife.Core.GameInputId.Map, true); yield return null; mapInput2.SetMobileButton(NihongoLife.Core.GameInputId.Map, false); yield return null;
            Assert.IsTrue(map.IsVisible);
            yield return null;
            Capture("station-map");
            var areaTab_station = Object.FindFirstObjectByType<WorldMapUI>().GetComponentInParent<Canvas>().GetComponentsInChildren<Button>().First(x => x.name.StartsWith("MapTab_"));
            areaTab_station.onClick.Invoke();
            yield return null;
            Capture("station-topdown");
            areaTab_station.transform.parent.GetComponentsInChildren<Button>().Last(x => x.name.StartsWith("MapTab_")).onClick.Invoke();
            map.SetVisible(false);

            // Sweep the actual character controller against the station perimeter.
            var controller = player.GetComponent<CharacterController>();
            player.InputLocked = true;
            controller.enabled = false;
            player.transform.position = new Vector3(744f, .38f, 6f);
            controller.enabled = true;
            Physics.SyncTransforms();
            controller.Move(Vector3.left * 5f);
            Assert.Greater(player.transform.position.x, 742f, "West wall must stop the player.");
            player.InputLocked = false;
            flow.ExitZone(WorldLocationCatalog.StationScene, WorldLocationCatalog.CityStationReturn);
            yield return WaitForFlow(flow);
            Assert.AreEqual(city, SceneManager.GetActiveScene());
            Assert.Less(player.transform.position.x, 100f);
            for (int i = 0; i < environments.Length; i++) Assert.AreEqual(activeStates[i], environments[i].activeSelf);
            yield return null;
            Assert.IsNull(GameObject.Find("StationTravelHUD"), "Station HUD must be removed on exit.");

            var portal = Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None).First(x => x.name == "HomeBedroomPortal");
            portal.Interact(player.gameObject);
            yield return WaitForFlow(flow);
            Assert.AreEqual(WorldLocationCatalog.HomeBedroomScene, SceneManager.GetActiveScene().name);
            Assert.IsTrue(environments.All(x => !x.activeSelf));
            yield return new WaitForSecondsRealtime(2f);
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var node in root.GetComponentsInChildren<Transform>(true))
                    Assert.IsFalse(node.GetComponents<Component>().Any(x => x == null), "Missing script on " + node.name);
            Assert.IsTrue(Physics.Raycast(player.transform.position + Vector3.up, Vector3.down, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore), "Bedroom floor must support the spawn.");
            Capture("bedroom");
            var mapInput3 = NihongoLife.Core.GameInputService.GetOrCreate(); yield return new WaitForFixedUpdate(); mapInput3.SetMobileButton(NihongoLife.Core.GameInputId.Map, true); yield return null; mapInput3.SetMobileButton(NihongoLife.Core.GameInputId.Map, false); yield return null;
            Assert.IsTrue(map.IsVisible);
            yield return null;
            Capture("bedroom-map");
            var areaTab_bedroom = Object.FindFirstObjectByType<WorldMapUI>().GetComponentInParent<Canvas>().GetComponentsInChildren<Button>().First(x => x.name.StartsWith("MapTab_"));
            areaTab_bedroom.onClick.Invoke();
            yield return null;
            Capture("bedroom-topdown");
            areaTab_bedroom.transform.parent.GetComponentsInChildren<Button>().Last(x => x.name.StartsWith("MapTab_")).onClick.Invoke();
            map.SetVisible(false);
            flow.ExitZone(WorldLocationCatalog.HomeBedroomScene, WorldLocationCatalog.CityHomeBedroomReturn);
            yield return WaitForFlow(flow);
            Assert.AreEqual(city, SceneManager.GetActiveScene());
            for (int i = 0; i < environments.Length; i++) Assert.AreEqual(activeStates[i], environments[i].activeSelf);
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
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/zone-regression"));
            Directory.CreateDirectory(folder);
            var camera = Camera.main;
            Assert.NotNull(camera);
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(x => x.isRootCanvas && x.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            var target = new RenderTexture(1600, 900, 24);
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .5f; }
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
