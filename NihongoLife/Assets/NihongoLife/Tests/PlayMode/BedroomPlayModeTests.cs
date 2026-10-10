using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Home;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Plays the player's room the way the game reaches it (city → HomeBedroomPortal) and exercises
    /// every room interaction, the walls, and the standalone zone bootstrap. Captures go to
    /// Bao_Cao/bedroom-regression for visual review.
    /// </summary>
    public class BedroomPlayModeTests
    {
        private const float HalfW = 3.6f, HalfD = 3.0f;

        [UnityTest]
        public IEnumerator BedroomFromCity_AllInteractions()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            Assert.NotNull(player);
            Assert.NotNull(flow);

            var portal = Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None).First(x => x.name == "HomeBedroomPortal");
            portal.Interact(player.gameObject);
            yield return WaitForFlow(flow);
            Assert.AreEqual(WorldLocationCatalog.HomeBedroomScene, SceneManager.GetActiveScene().name);

            var room = HomeBedroomRuntime.Instance;
            Assert.NotNull(room, "Room runtime must be alive.");
            yield return new WaitForSecondsRealtime(1.4f);
            Assert.IsTrue(room.IsTitleVisible, "Entry title card should show.");
            Capture("01_entry_title");
            var cam = Camera.main.transform.position;
            Assert.IsTrue(Mathf.Abs(cam.x) < HalfW && Mathf.Abs(cam.z) < HalfD && cam.y < 2.7f, $"Camera must stay inside the room during the entrance shot: {cam}");
            yield return new WaitForSecondsRealtime(4f);
            Assert.IsFalse(room.IsTitleVisible, "Title card must fade out.");

            // No missing scripts and no stray characters inside the room.
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var node in root.GetComponentsInChildren<Transform>(true))
                    Assert.IsFalse(node.GetComponents<Component>().Any(x => x == null), "Missing script on " + node.name);
            var strays = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None)
                .Where(a => a.gameObject.activeInHierarchy && !a.transform.IsChildOf(player.transform))
                .Where(a => Mathf.Abs(a.transform.position.x) < HalfW + 1f && Mathf.Abs(a.transform.position.z) < HalfD + 1f && Mathf.Abs(a.transform.position.y) < 4f)
                .Select(a => $"{PathOf(a.transform)} [{string.Join(",", a.transform.root.GetComponents<Component>().Select(c => c.GetType().Name))}] ({a.gameObject.scene.name}) @ {a.transform.position}").ToArray();
            Assert.IsEmpty(strays, "Stray characters in the room: " + string.Join("; ", strays));

            // The city HUD is present, so the room must not draw a second prompt.
            Assert.IsFalse(room.IsPromptVisible, "Room prompt duplicates the city HUD.");
            Capture("02_room_overview");

            // Walls: push the real CharacterController into every wall.
            var body = player.GetComponent<CharacterController>();
            player.InputLocked = true;
            foreach (var dir in new[] { Vector3.left, Vector3.right, Vector3.forward, Vector3.back })
            {
                Teleport(player, new Vector3(0.75f, 0.05f, 0.9f));
                body.Move(dir * 8f);
                yield return null;
                Vector3 p = player.transform.position;
                Assert.Less(Mathf.Abs(p.x), HalfW, $"Player crossed the {dir} wall: {p}");
                Assert.Less(Mathf.Abs(p.z), HalfD, $"Player crossed the {dir} wall: {p}");
            }
            player.InputLocked = false;

            var status = PlayerStatus.Instance;
            Assert.NotNull(status);

            // Light switch.
            var lightSwitch = Object.FindFirstObjectByType<RoomLightSwitch>();
            var ceiling = GameObject.Find("CeilingLight").GetComponent<Light>();
            Assert.IsTrue(ceiling.enabled);
            Teleport(player, lightSwitch.transform.position + new Vector3(0f, -1.05f, 0.3f), 180f);
            lightSwitch.Interact(player.gameObject);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsFalse(ceiling.enabled, "Switch must turn the ceiling light off.");
            Capture("03_lights_off");
            lightSwitch.Interact(player.gameObject);
            Assert.IsTrue(ceiling.enabled);

            // Sink and fridge.
            var sink = Object.FindObjectsByType<HomeNeedsStation>(FindObjectsSortMode.None).First(x => x.StationKind == HomeNeedsStation.Kind.Sink);
            var fridge = Object.FindObjectsByType<HomeNeedsStation>(FindObjectsSortMode.None).First(x => x.StationKind == HomeNeedsStation.Kind.Fridge);
            Teleport(player, new Vector3(sink.transform.position.x + 0.2f, 0.05f, sink.transform.position.z), -90f);
            sink.Interact(player.gameObject);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(room.IsToastVisible, "Sink must answer with a toast.");
            Capture("04_sink");

            var inventory = player.GetComponent<PlayerInventory>();
            foreach (var item in inventory.Items.ToArray()) inventory.RemoveItem(item.itemId, item.quantity);
            Teleport(player, new Vector3(fridge.transform.position.x + 0.2f, 0.05f, fridge.transform.position.z), -90f);
            fridge.Interact(player.gameObject);
            yield return new WaitForSecondsRealtime(0.5f);
            StringAssert.Contains("Tủ lạnh trống", room.ToastText);
            inventory.AddItem("onigiri_ume", "うめおにぎり", "Onigiri mơ muối", 150, 1, false, ItemUseType.Food, 30f, 0f, 5f);
            fridge.Interact(player.gameObject);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(0, inventory.GetItemQuantity("onigiri_ume"), "Fridge must use the bought onigiri.");
            StringAssert.Contains("うめおにぎり", room.ToastText);
            Capture("05_fridge");

            // Study desk: answer every question correctly.
            var desk = Object.FindFirstObjectByType<StudyDeskInteractable>();
            Teleport(player, desk.transform.position + new Vector3(0f, -0.75f, -0.2f), 0f);
            int knowledgeBefore = status.Knowledge;
            desk.Interact(player.gameObject);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(room.IsStudyOpen, "Desk must open the study card.");
            Assert.IsTrue(player.InputLocked, "Movement must be locked while studying.");
            Capture("06_study_card");
            string lastWord = null;
            for (int q = 0; q < 5; q++)
            {
                float timeout = Time.realtimeSinceStartup + 6f;
                while (room.IsStudyOpen && room.CurrentStudyJapanese == lastWord && Time.realtimeSinceStartup < timeout) yield return null;
                lastWord = room.CurrentStudyJapanese;
                int option = room.FindOption(Meaning(lastWord));
                Assert.GreaterOrEqual(option, 0, "Correct meaning must be offered for " + lastWord);
                room.PickAnswer(option);
                yield return new WaitForSecondsRealtime(0.3f);
                if (q == 0) Capture("07_study_correct");
            }
            float end = Time.realtimeSinceStartup + 6f;
            while (room.IsStudyOpen && Time.realtimeSinceStartup < end) yield return null;
            Assert.IsFalse(room.IsStudyOpen);
            Assert.IsFalse(player.InputLocked, "Movement must unlock after studying.");
            Assert.AreEqual(knowledgeBefore + 13, status.Knowledge, "5 correct answers = 5×2 + 3 bonus Knowledge.");
            Capture("08_study_result");

            // Bed: full sleep cycle.
            var bed = Object.FindFirstObjectByType<BedroomRestInteractable>();
            Teleport(player, new Vector3(bed.transform.position.x + 1.1f, 0.05f, bed.transform.position.z), -90f);
            yield return null;
            bed.Interact(player.gameObject);
            yield return new WaitForSecondsRealtime(0.35f);
            Capture("09_lying_down");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(player.InputLocked, "Player must be locked while sleeping.");
            Assert.IsFalse(bed.IsInteractionAvailable);
            Bounds bedBounds = RendererBounds(GameObject.Find("HomeBedroom_YourRoom/Room/Furniture/Bed"));
            Bounds pose = SkinBounds(player.gameObject);
            Assert.Less(pose.size.y, 0.95f, $"Player must lie down, pose bounds {pose.size}");
            Assert.Greater(pose.size.z, pose.size.x, $"Body must lie along the bed, pose bounds {pose.size}");
            Assert.IsTrue(pose.center.x > bedBounds.min.x && pose.center.x < bedBounds.max.x && pose.center.z > bedBounds.min.z && pose.center.z < bedBounds.max.z,
                $"Body must be on the mattress: pose {pose.center}, bed {bedBounds.min}-{bedBounds.max}");
            Assert.Greater(pose.min.y, bedBounds.min.y + 0.15f, "Body must lie on the mattress, not inside the frame.");
            var rig = player.GetComponentInChildren<Animator>();
            Transform head = rig.GetBoneTransform(HumanBodyBones.Head), hips = rig.GetBoneTransform(HumanBodyBones.Hips);
            Assert.Greater(head.position.z, hips.position.z + 0.2f, $"Head must point to the headboard (+Z): head {head.position}, hips {hips.position}");
            yield return new WaitForSecondsRealtime(2.2f);
            Capture("10_sleep_night");
            float wake = Time.realtimeSinceStartup + 20f;
            while (!bed.IsInteractionAvailable && Time.realtimeSinceStartup < wake)
            {
                if (room != null && Time.realtimeSinceStartup > wake - 17.5f && Time.realtimeSinceStartup < wake - 17f) Capture("11_morning");
                yield return null;
            }
            Assert.IsTrue(bed.IsInteractionAvailable, "Sleep sequence must finish.");
            Assert.GreaterOrEqual(status.Restfulness, 99f, "Sleeping must remove sleepiness.");
            Assert.AreEqual(status.MaxEnergy, status.CurrentEnergy, 0.5f, "Sleeping must refill energy.");
            Assert.IsFalse(player.InputLocked, "Player must be able to move after waking.");
            Assert.IsTrue(body.enabled);
            Assert.IsTrue(Physics.Raycast(player.transform.position + Vector3.up, Vector3.down, 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore), "Player must wake on the floor.");
            Assert.IsTrue(ceiling.enabled, "Room lights come back on in the morning.");
            yield return new WaitForSecondsRealtime(1f);
            Capture("12_awake");

            // Leave through the entrance door.
            var exit = GameObject.Find("ExitToCity").GetComponent<ScenePortal>();
            Assert.AreEqual("そとに でる", exit.GetPromptJa());
            exit.Interact(player.gameObject);
            yield return WaitForFlow(flow);
            Assert.AreEqual(WorldLocationCatalog.CityScene, SceneManager.GetActiveScene().name);
            Assert.IsNull(HomeBedroomRuntime.Instance, "Room runtime must unload with the room.");
        }

        [UnityTest]
        public IEnumerator BedroomStandalone_BootsThroughCityAndLeaves()
        {
            // Pressing Play inside 45_HomeBedroom must give the full game: city host, HUD, status dock, exits.
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.HomeBedroomScene);
            float timeout = Time.realtimeSinceStartup + 40f;
            while (Time.realtimeSinceStartup < timeout &&
                   (SceneManager.GetActiveScene().name != WorldLocationCatalog.HomeBedroomScene || StandaloneZoneBootstrap.IsBooting
                    || !SceneManager.GetSceneByName(WorldLocationCatalog.CityScene).isLoaded || FlowLoading()))
                yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreEqual(WorldLocationCatalog.HomeBedroomScene, SceneManager.GetActiveScene().name);
            Assert.IsTrue(SceneManager.GetSceneByName(WorldLocationCatalog.CityScene).isLoaded, "The city must host the room.");
            var player = Object.FindFirstObjectByType<PlayerController>();
            Assert.NotNull(player);
            Assert.NotNull(Object.FindFirstObjectByType<NihongoLife.UI.HUDUI>(), "The shared HUD must exist in the room.");
            Assert.NotNull(Object.FindFirstObjectByType<NihongoLife.UI.StatusDock>(), "The status widget must exist in the room.");
            Assert.NotNull(Object.FindFirstObjectByType<NihongoLife.UI.StatusDock>().StatusWidget);
            var dm = NihongoLife.Dialogue.DialogueManager.Instance;
            for (int guard = 0; guard < 60 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }

            var bed = Object.FindFirstObjectByType<BedroomRestInteractable>();
            Teleport(player, new Vector3(bed.transform.position.x + 1.1f, 0.05f, bed.transform.position.z), -90f);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.IsInstanceOf<BedroomRestInteractable>(player.GetComponent<InteractionDetector>().CurrentInteractable);
            Capture("13_standalone_hud");

            // Walk to the genkan: the exit must be the interaction there, and F must reach the street.
            var exit = GameObject.Find("ExitToCity").GetComponent<ScenePortal>();
            Vector3 toRoom = Vector3.ProjectOnPlane(Vector3.zero - exit.transform.position, Vector3.up).normalized;
            float doorYaw = Quaternion.LookRotation(-toRoom).eulerAngles.y;
            Teleport(player, new Vector3(exit.transform.position.x, 0.05f, exit.transform.position.z) + toRoom * 0.7f, doorYaw);
            var orbit = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (orbit != null) orbit.SetOrbit(doorYaw, 20f, 2.4f);
            yield return new WaitForSecondsRealtime(2f); // let the follow camera settle behind the player
            var near = Physics.OverlapSphere(player.transform.position + Vector3.up * 0.9f, 2f, ~0, QueryTriggerInteraction.Collide)
                .Select(c => c.name + "@" + c.gameObject.scene.name).ToArray();
            Assert.AreSame(exit, player.GetComponent<InteractionDetector>().CurrentInteractable as ScenePortal,
                $"Standing at the door must offer そとに でる. player={player.transform.position} exit={exit.transform.position} scene={exit.gameObject.scene.name} near=[{string.Join(", ", near)}] current={player.GetComponent<InteractionDetector>().CurrentInteractable}");
            // Evidence shot from inside the room (the follow camera is still easing in after the teleport).
            var shot = new GameObject("DoorEvidenceCamera").AddComponent<Camera>();
            shot.transform.position = player.transform.position + toRoom * 2.6f + Vector3.up * 1.9f;
            shot.transform.LookAt(exit.transform.position + Vector3.up * 0.1f);
            shot.depth = 50f;
            yield return null;
            Capture("14_standalone_door", shot);
            Object.Destroy(shot.gameObject);
            exit.Interact(player.gameObject);
            timeout = Time.realtimeSinceStartup + 30f;
            yield return null;
            while (FlowLoading() && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.AreEqual(WorldLocationCatalog.CityScene, SceneManager.GetActiveScene().name, "The door must lead to the street.");
            yield return new WaitForSecondsRealtime(1f);
            Capture("15_standalone_street");
        }

        private static bool FlowLoading()
        {
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            return flow != null && flow.IsLoading;
        }

        private static Bounds RendererBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        private static Bounds SkinBounds(GameObject go)
        {
            var skins = go.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.enabled).ToArray();
            Assert.IsNotEmpty(skins, "Player has no skinned mesh.");
            Bounds b = skins[0].bounds;
            foreach (var r in skins) b.Encapsulate(r.bounds);
            return b;
        }

        private static string PathOf(Transform t)
        {
            string p = t.name;
            while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
            return p;
        }

        private static string Meaning(string japanese)
        {
            var word = StudyDeskInteractable.Words.FirstOrDefault(w => w.japanese == japanese);
            return word != null ? word.meaning : "\u0000";
        }

        private static void Teleport(PlayerController player, Vector3 position, float yaw = 0f)
        {
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.SetPositionAndRotation(new Vector3(position.x, Mathf.Max(0.05f, position.y), position.z), Quaternion.Euler(0f, yaw, 0f));
            body.enabled = true;
            Physics.SyncTransforms();
        }

        private static IEnumerator WaitForFlow(SceneFlowController flow)
        {
            float timeout = Time.realtimeSinceStartup + 30f;
            yield return null;
            while (flow.IsLoading && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.IsFalse(flow.IsLoading, "Transition timed out.");
        }

        private static void Capture(string name, Camera source = null)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/bedroom-regression"));
            Directory.CreateDirectory(folder);
            var camera = source != null ? source : Camera.main;
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
