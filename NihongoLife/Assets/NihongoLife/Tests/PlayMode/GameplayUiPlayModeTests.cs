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
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            var dm = DialogueManager.Instance;
            while (dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
            player.GetComponent<PlayerInventory>().AddYen(1000);

            var portal = GameObject.Find("AdditiveZonePortals/StationPortal").GetComponent<ScenePortal>();
            portal.Interact(player.gameObject);
            float timeout = Time.realtimeSinceStartup + 30f;
            yield return null;
            while (flow.IsLoading && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.AreEqual(WorldLocationCatalog.StationScene, SceneManager.GetActiveScene().name);
            yield return new WaitForSecondsRealtime(1.5f);
            var station = Object.FindFirstObjectByType<StationTravelController>();
            var view = Object.FindFirstObjectByType<DialogueView>();
            Assert.NotNull(station);
            Capture("10_station_hall");

            station.Execute(StationAction.TalkStationStaff, player.gameObject);
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(dm.IsOpen, "Station staff must talk through the shared dialogue box.");
            Capture("11_station_staff");
            view.Pick(0); // ミナトえきへ いきたいです
            yield return new WaitForSecondsRealtime(1.2f);
            Capture("12_station_fare");
            view.Leave();
            yield return null;
            Assert.IsFalse(dm.IsOpen);

            station.Execute(StationAction.BuyTicket, player.gameObject);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsTrue(station.IsTicketMachineOpen);
            Capture("13_ticket_machine");
            Assert.IsTrue(station.PurchaseTicket());
            Assert.IsTrue(station.HasTicket);
            station.Execute(StationAction.PassGate, player.gameObject);
            Assert.IsTrue(station.GatePassed);

            timeout = Time.realtimeSinceStartup + 20f;
            while (!station.IsTrainBoarding && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.IsTrue(station.IsTrainBoarding, "The train must be called in for a passenger who passed the gate.");
            yield return new WaitForSecondsRealtime(20f);
            Assert.IsTrue(station.IsTrainBoarding, "Doors must stay open until the passenger boards.");
            Capture("14_platform_waiting");

            station.Execute(StationAction.BoardTrain, player.gameObject);
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(station.IsOnboard && station.IsRiding, "Boarding must start the ride.");
            Capture("15_on_train");
            station.Execute(StationAction.TalkPassenger, player.gameObject);
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(dm.IsOpen);
            Capture("16_passenger");
            view.Pick(0);
            yield return new WaitForSecondsRealtime(0.8f);
            for (int guard = 0; guard < 10 && dm.IsOpen; guard++) { view.Continue(); yield return new WaitForSecondsRealtime(0.3f); }

            timeout = Time.realtimeSinceStartup + 40f;
            while (station.IsOnboard && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.IsFalse(station.IsOnboard, "The trip must end at Minato with the player back on the platform.");
            yield return new WaitForSecondsRealtime(0.5f);
            Capture("17_arrived");
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

        private static void Capture(string name)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/ui-regression"));
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
