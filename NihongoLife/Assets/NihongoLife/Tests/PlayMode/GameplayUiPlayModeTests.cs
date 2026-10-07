using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.NPC;
using NihongoLife.Player;
using NihongoLife.Scenario;
using NihongoLife.Shop;
using NihongoLife.UI;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Plays the reworked gameplay UI end to end: shared dialogue box + Esc, konbini shelf → basket →
    /// clerk checkout → bag, bag window use, map window, and a full Minato train trip. Captures go to
    /// Bao_Cao/ui-regression.
    /// </summary>
    public class GameplayUiPlayModeTests
    {
        [UnityTest]
        public IEnumerator City_DialogueShopBagMap()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var dm = DialogueManager.Instance;
            var view = Object.FindFirstObjectByType<DialogueView>();
            Assert.NotNull(player); Assert.NotNull(dm); Assert.NotNull(view, "HUD must create the shared DialogueView.");
            while (dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }

            // 1) Small talk must not advance the running scenario, and Esc leaves it.
            string nodeBefore = ScenarioManager.Instance?.CurrentNode?.id;
            var guide = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).First(n => n.NpcId == "npc_guide");
            Teleport(player, guide.transform.position + guide.transform.forward * 1.6f, guide.transform.eulerAngles.y + 180f);
            yield return null;
            guide.Interact(player.gameObject);
            yield return new WaitForSecondsRealtime(1.2f);
            if (dm.IsOpen)
            {
                Assert.IsTrue(view.IsOpen, "Dialogue box must open.");
                Capture("01_dialogue_box");
                Assert.IsTrue(dm.CanLeave, "NPC conversations can be left with Esc.");
                view.Leave();
                yield return null;
                Assert.IsFalse(dm.IsOpen, "Esc must close the conversation.");
                Assert.IsFalse(player.InputLocked, "Player must move again after leaving.");
            }
            Assert.AreEqual(nodeBefore, ScenarioManager.Instance?.CurrentNode?.id, "Small talk must not skip scenario nodes.");

            // 2) Konbini: browse → basket → checkout with Ito → bag.
            var inventory = player.GetComponent<PlayerInventory>();
            foreach (var item in inventory.Items.ToArray()) inventory.RemoveItem(item.itemId, item.quantity);
            int yenBefore = inventory.Yen;
            var shelf = Object.FindObjectsByType<KonbiniShelf>(FindObjectsSortMode.None).First(s => s.Section == KonbiniSection.Onigiri);
            Teleport(player, new Vector3(-1.7f, 0.08f, 3.2f), -90f);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsInstanceOf<KonbiniShelf>(player.GetComponent<InteractionDetector>().CurrentInteractable, "Shelf must be the interaction in the aisle.");
            shelf.Interact(player.gameObject);
            yield return null;
            var shop = KonbiniShopUI.GetOrCreate();
            Assert.IsTrue(shop.IsOpen);
            shop.Select(KonbiniCatalog.Find("onigiri_sake"));
            yield return new WaitForSecondsRealtime(0.4f);
            Capture("02_shop_onigiri");
            shop.AddSelectedToBasket();
            shop.OpenSection(KonbiniSection.Drinks);
            shop.Select(KonbiniCatalog.Find("tea"));
            shop.AddSelectedToBasket();
            yield return new WaitForSecondsRealtime(0.3f);
            Capture("03_shop_drinks");
            Assert.AreEqual(2, KonbiniBasket.Count);
            Assert.AreEqual(310, KonbiniBasket.Total);
            shop.Close();
            Assert.IsFalse(player.InputLocked);

            var clerk = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).First(n => n.NpcId == "npc_cashier");
            Teleport(player, new Vector3(0.1f, 0.08f, 7.4f), 0f);
            yield return new WaitForSecondsRealtime(0.6f);
            Capture("04_basket_chip");
            clerk.Interact(player.gameObject);
            yield return null;
            if (!shop.IsCheckout && dm.IsOpen)
            {
                // The konbini scenario owns Ito right now: play its lines, then ring up.
                for (int guard = 0; guard < 40 && dm.IsOpen; guard++) { if (view.ChoiceCount > 0) view.Pick(0); else view.Continue(); yield return new WaitForSecondsRealtime(0.3f); }
                clerk.Interact(player.gameObject);
                yield return null;
            }
            Assert.IsTrue(shop.IsCheckout, "Talking to Ito with a basket must open the register.");
            yield return new WaitForSecondsRealtime(0.4f);
            Capture("05_checkout");
            Assert.IsTrue(shop.Pay());
            Assert.AreEqual(yenBefore - 310, inventory.Yen);
            Assert.AreEqual(1, inventory.GetItemQuantity("onigiri_sake"));
            Assert.AreEqual(1, inventory.GetItemQuantity("tea"));
            Assert.IsTrue(KonbiniBasket.IsEmpty);
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsTrue(dm.IsOpen, "Ito must thank the player.");
            Capture("06_thank_you");
            for (int guard = 0; guard < 20 && dm.IsOpen; guard++) { if (view.ChoiceCount > 0) view.Pick(0); else view.Continue(); yield return new WaitForSecondsRealtime(0.35f); }
            Assert.IsFalse(dm.IsOpen);
            Assert.IsFalse(player.InputLocked);

            // 3) Bag: open, select the rice ball, eat it.
            var hud = Object.FindFirstObjectByType<HUDUI>();
            Invoke(hud, "SetInventoryVisible", true);
            yield return new WaitForSecondsRealtime(0.4f);
            var bag = Object.FindFirstObjectByType<InventoryWindow>();
            Assert.IsTrue(bag.Root.activeInHierarchy, "B must open the bag window.");
            int riceIndex = inventory.Items.ToList().FindIndex(i => i.itemId == "onigiri_sake");
            bag.Select(riceIndex);
            yield return null;
            Capture("07_bag");
            bag.UseSelected();
            Assert.AreEqual(0, inventory.GetItemQuantity("onigiri_sake"), "Using food must consume it.");
            Invoke(hud, "SetInventoryVisible", false);
            yield return null;

            // 4) Map window.
            Teleport(player, new Vector3(0f, 0.08f, -13.5f), 0f);
            GameObject.Find("MapButton").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.4f);
            var map = Object.FindFirstObjectByType<WorldMapUI>();
            Assert.IsTrue(map.IsVisible);
            Capture("08_map_overview");
            var tabs = map.GetComponentInParent<Canvas>().GetComponentsInChildren<Button>().Where(x => x.name.StartsWith("MapTab_")).ToArray();
            tabs.First().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.4f);
            Capture("09_map_area");
            map.SetVisible(false);
        }

        [UnityTest]
        public IEnumerator Station_FullTripToMinato()
        {
            var ctx = new StationContext();
            yield return EnterStation(ctx);
            var (player, flow, dm, view, station) = (ctx.Player, ctx.Flow, ctx.Dialogue, ctx.View, ctx.Station);
            Capture("10_station_hall");

            // Kimura sells the ticket at the counter (F on the NPC runs the real NPC path).
            var kimura = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).First(n => n.NpcId == "npc_station_staff");
            Teleport(player, kimura.transform.position + kimura.transform.forward * 1.5f, kimura.transform.eulerAngles.y + 180f);
            yield return null;
            kimura.Interact(player.gameObject);
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(dm.IsOpen, "Kimura must answer through the shared dialogue box.");
            Capture("11_station_staff");
            view.Pick(0); // Minato e ikitai desu
            yield return new WaitForSecondsRealtime(1.2f);
            Capture("12_station_fare");
            int yenBefore = PlayerInventory.Instance.Yen;
            view.Pick(0); // kippu o ichimai kudasai
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(station.HasTicket, "Asking Kimura for a ticket must sell one.");
            Assert.AreEqual("minato", station.TicketStop.Id);
            Assert.AreEqual(yenBefore - 320, PlayerInventory.Instance.Yen);
            Assert.IsTrue(dm.IsOpen, "Kimura hands the ticket over in dialogue.");
            Capture("13_staff_sold");
            for (int guard = 0; guard < 10 && dm.IsOpen; guard++) { if (view.ChoiceCount > 0) view.Pick(0); else view.Continue(); yield return new WaitForSecondsRealtime(0.3f); }
            Assert.IsFalse(dm.IsOpen);

            station.Execute(StationAction.BuyTicket, player.gameObject);
            yield return null;
            Assert.IsFalse(station.IsTicketMachineOpen, "With a ticket in hand the machine just reminds the player.");

            station.Execute(StationAction.PassGate, player.gameObject);
            Assert.IsTrue(station.GatePassed);
            yield return WaitUntil(() => station.IsTrainBoarding, 25f, "The train must be called in for a passenger who passed the gate.");
            yield return new WaitForSecondsRealtime(18f);
            Assert.IsTrue(station.IsTrainBoarding, "Doors must stay open until the passenger boards.");
            Capture("14_platform_waiting");

            station.Execute(StationAction.BoardTrain, player.gameObject);
            yield return new WaitForSecondsRealtime(2.5f);
            Assert.IsTrue(station.IsOnboard && station.IsRiding, "Boarding must start the ride.");
            var commuters = Object.FindObjectsByType<SeatedPassenger>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(commuters.Length, 3, "Commuters must be seated in the carriage.");
            Capture("15_on_train");
            station.SetWindowView(true);
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsTrue(station.IsWindowViewOpen);
            Capture("16_window_view", station.WindowCamera);
            station.SetWindowView(false);

            var grandma = Object.FindObjectsByType<TrainPassengerTalk>(FindObjectsSortMode.None).First(p => p.PassengerId == "grandma");
            Teleport(player, grandma.transform.position + grandma.transform.forward * 1.1f, grandma.transform.eulerAngles.y + 180f);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsInstanceOf<TrainPassengerTalk>(player.GetComponent<InteractionDetector>().CurrentInteractable, "F next to a commuter must offer a chat.");
            grandma.Interact(player.gameObject);
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(dm.IsOpen);
            Capture("17_passenger");
            for (int guard = 0; guard < 10 && dm.IsOpen; guard++) { if (view.ChoiceCount > 0) view.Pick(0); else view.Continue(); yield return new WaitForSecondsRealtime(0.3f); }

            yield return WaitUntil(() => station != null && station.IsAtStop && station.NextStop.Id == "gakuen", 30f, "The train must stop at Gakuen-mae on the way.");
            Assert.IsTrue(station.IsOnboard, "A Minato ticket rides on past Gakuen-mae.");
            Capture("18_stop_gakuen");

            yield return WaitUntil(() => SceneManager.GetActiveScene().name == WorldLocationCatalog.SushiRestaurantScene && !flow.IsLoading, 60f,
                "Arriving at Minato must take the player to the Minato scene, not back to the platform.");
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsFalse(SceneManager.GetSceneByName(WorldLocationCatalog.StationScene).isLoaded, "The station unloads after the trip.");
            Assert.AreEqual(0, PlayerInventory.Instance.GetItemQuantity(StationTravelController.TicketItemId), "The ticket is collected on arrival.");
            Capture("19_arrived_minato");

            var exit = Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None).First(p => p.gameObject.scene.name == WorldLocationCatalog.SushiRestaurantScene);
            exit.Interact(player.gameObject);
            yield return null;
            yield return WaitUntil(() => !flow.IsLoading && SceneManager.GetActiveScene().name == WorldLocationCatalog.CityScene, 30f, "Leaving Minato must reach the city.");
        }

        [UnityTest]
        public IEnumerator Station_GakuenMaeTicketGoesToSchool()
        {
            var ctx = new StationContext();
            yield return EnterStation(ctx);
            var (player, flow, station) = (ctx.Player, ctx.Flow, ctx.Station);
            station.Execute(StationAction.BuyTicket, player.gameObject);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsTrue(station.IsTicketMachineOpen);
            Capture("20_ticket_machine");
            Assert.IsTrue(station.PurchaseTicket("gakuen"));
            Assert.IsFalse(station.IsTicketMachineOpen);
            station.Execute(StationAction.PassGate, player.gameObject);
            yield return WaitUntil(() => station.IsTrainBoarding, 25f, "Train must come in.");
            station.Execute(StationAction.BoardTrain, player.gameObject);
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == WorldLocationCatalog.SchoolScene && !flow.IsLoading, 40f,
                "A Gakuen-mae ticket must end at Hibari School.");
            yield return new WaitForSecondsRealtime(1.5f);
            Capture("21_arrived_school");
        }

        [UnityTest]
        public IEnumerator Hud_StatusDockButtonsAndPortalSigns()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var dm = DialogueManager.Instance;
            while (dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
            var dock = Object.FindFirstObjectByType<StatusDock>();
            Assert.NotNull(dock, "Vitals + action bar must be on the HUD.");
            foreach (string name in new[] { "BagButton", "CharacterButton", "MapButton", "QuestButton", "ExamButton", "SettingsButton" })
                Assert.NotNull(GameObject.Find(name), name + " missing");

            var portal = GameObject.Find("AdditiveZonePortals/StationPortal").GetComponent<ScenePortal>();
            Vector3 front = portal.transform.position - portal.transform.forward * 6f;
            Teleport(player, new Vector3(front.x, 0.08f, front.z), portal.transform.eulerAngles.y);
            yield return new WaitForSecondsRealtime(1.2f);
            foreach (var p in Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None))
                Assert.NotNull(p.GetComponentInChildren<PortalBeacon>(true), "Portal without a sign: " + p.name);
            Capture("22_portal_sign_and_dock");

            var hud = Object.FindFirstObjectByType<HUDUI>();
            GameObject.Find("CharacterButton").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsTrue(dock.CharacterRoot.activeInHierarchy, "Character button must open the character window.");
            Capture("23_character_window");
            Invoke(hud, "SetCharacterVisible", false);
            yield return null;
            GameObject.Find("BagButton").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(Object.FindFirstObjectByType<InventoryWindow>().Root.activeInHierarchy, "Bag button must open the bag.");
            Invoke(hud, "SetInventoryVisible", false);
            yield return null;
            Assert.IsFalse(player.InputLocked);
        }

        [UnityTest]
        public IEnumerator Emote_WheelAndColouredCharacters()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var dm = DialogueManager.Instance;
            while (dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }

            // Every neighbour-model character must render with its textures (they used to be pure white).
            var neighbourRenderers = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None)
                .Where(r => r.sharedMaterials.Any(m => m != null && m.name.StartsWith("NL_Neighbor_"))).ToArray();
            Assert.IsNotEmpty(neighbourRenderers, "The city should have neighbour-model NPCs.");
            foreach (var r in neighbourRenderers)
                foreach (var m in r.sharedMaterials.Where(m => m != null && m.name.StartsWith("NL_Neighbor_") && !m.name.Contains("Cornea")))
                    Assert.NotNull(m.GetTexture("_BaseMap"), m.name + " has no texture");
            Component npc = neighbourRenderers[0].GetComponentInParent<NPCController>();
            if (npc == null) npc = neighbourRenderers[0];
            Teleport(player, npc.transform.position + npc.transform.forward * 2.2f, npc.transform.eulerAngles.y + 180f);
            var orbit = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            orbit?.SetOrbit(npc.transform.eulerAngles.y + 150f, 12f, 3.2f);
            yield return new WaitForSecondsRealtime(1.5f);
            Capture("26_neighbour_colours");

            // Hold-E wheel: open, hover "thanks", release → bubble + phrase above the head.
            var emotes = player.GetComponent<PlayerEmoteController>();
            Assert.NotNull(emotes);
            Invoke(emotes, "OpenWheel");
            var wheel = Object.FindFirstObjectByType<EmoteWheelUI>();
            Assert.IsTrue(wheel.IsOpen, "Holding E must open the emote wheel.");
            wheel.Select(1);
            yield return new WaitForSecondsRealtime(0.4f);
            Capture("24_emote_wheel");
            Assert.AreEqual(1, wheel.Selected);
            Invoke(emotes, "CloseWheel");
            Assert.IsFalse(wheel.IsOpen);
            Assert.IsFalse(player.InputLocked, "Closing the wheel must give movement back.");
            emotes.Play(wheel.Selected);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.NotNull(player.GetComponentInChildren<EmoteBubble>(), "The emote must pop above the player.");
            Assert.AreEqual(1, emotes.LastEmote, "A quick tap repeats this emote next time.");
            Capture("25_emote_bubble");
        }

        private sealed class StationContext
        {
            public PlayerController Player;
            public SceneFlowController Flow;
            public DialogueManager Dialogue;
            public DialogueView View;
            public StationTravelController Station;
        }

        private static IEnumerator EnterStation(StationContext ctx)
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            ctx.Player = Object.FindFirstObjectByType<PlayerController>();
            ctx.Flow = Object.FindFirstObjectByType<SceneFlowController>();
            ctx.Dialogue = DialogueManager.Instance;
            while (ctx.Dialogue.IsOpen) { ctx.Dialogue.CancelDialogue(); if (ctx.Dialogue.IsOpen) ctx.Dialogue.ContinueDialogue(); yield return null; }
            ctx.Player.GetComponent<PlayerInventory>().AddYen(1000);
            var portal = GameObject.Find("AdditiveZonePortals/StationPortal").GetComponent<ScenePortal>();
            portal.Interact(ctx.Player.gameObject);
            yield return null;
            var flow = ctx.Flow;
            yield return WaitUntil(() => !flow.IsLoading, 30f, "Station transition timed out.");
            Assert.AreEqual(WorldLocationCatalog.StationScene, SceneManager.GetActiveScene().name);
            yield return new WaitForSecondsRealtime(1.5f);
            while (ctx.Dialogue.IsOpen) { ctx.Dialogue.CancelDialogue(); if (ctx.Dialogue.IsOpen) ctx.Dialogue.ContinueDialogue(); yield return null; }
            ctx.View = Object.FindFirstObjectByType<DialogueView>();
            ctx.Station = Object.FindFirstObjectByType<StationTravelController>();
            Assert.NotNull(ctx.Station);
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float seconds, string message)
        {
            float timeout = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.IsTrue(condition(), message);
        }

        [Test]
        public void IntroScenario_GoesHomeBeforeTadaima()
        {
            var intro = Resources.Load<ScenarioDefinition>("Scenarios/scenario_intro_arrival");
            var goHome = intro.nodes.FirstOrDefault(n => n.id == "n_go_home");
            Assert.NotNull(goHome, "Intro must wait for the player to reach the room before the 「ただいま」 lines.");
            Assert.AreEqual(ScenarioNodeType.GoToArea, goHome.nodeType);
            Assert.AreEqual(WorldLocationCatalog.HomeBedroomEntrance, goHome.targetAreaId);
            Assert.AreEqual("n_home", goHome.nextNodeId);
            Assert.IsFalse(intro.nodes.Any(n => n.id != "n_go_home" && n.nextNodeId == "n_home"), "Nothing may jump straight to n_home.");
        }

        private static void Invoke(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(target, args);

        private static void Teleport(PlayerController player, Vector3 position, float yaw)
        {
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            body.enabled = true;
            Physics.SyncTransforms();
            var camera = Object.FindFirstObjectByType<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (camera != null) camera.SetOrbit(yaw, 18f, 3.4f);
        }

        private static void Capture(string name, Camera source = null)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/ui-regression"));
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
