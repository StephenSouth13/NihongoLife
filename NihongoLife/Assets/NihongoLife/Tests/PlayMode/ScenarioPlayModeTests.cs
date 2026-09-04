using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using NihongoLife.Scenario;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.NPC;
using NihongoLife.Player;

namespace NihongoLife.Tests
{
    public class ScenarioPlayModeTests
    {
        [UnityTest]
        public IEnumerator Scenario_PlayModeSmokeTest()
        {
            // 1. Load Bootstrap scene to register persistent core services
            SceneManager.LoadScene("00_Bootstrap");
            yield return new WaitForSeconds(0.5f); // Wait for load and initialization

            // 2. Load Sandbox scene
            SceneManager.LoadScene("90_TestSandbox");
            yield return new WaitForSeconds(0.5f); // Wait for load

            // 3. Verify ScenarioManager is alive and running the Konbini scenario
            Assert.NotNull(ScenarioManager.Instance, "ScenarioManager should be initialized in Sandbox.");
            Assert.NotNull(ScenarioManager.Instance.CurrentScenario, "Active scenario should be started by ScenarioSceneInitializer.");
            Assert.AreEqual("scenario.konbini.buy_onigiri", ScenarioManager.Instance.CurrentScenario.id);

            // 4. Verify initial node is active
            Assert.NotNull(ScenarioManager.Instance.CurrentNode, "Initial node should be active.");
            Assert.AreEqual("node_start", ScenarioManager.Instance.CurrentNode.id);

            // 5. Enter the store through the real door interactable
            var player = GameObject.FindWithTag("Player");
            Assert.NotNull(player, "Player should exist in the scene.");
            Assert.NotNull(player.GetComponent<PlayerInventory>(), "Player should have a real inventory component.");

            var door = Object.FindFirstObjectByType<DoorInteractable>();
            Assert.NotNull(door, "Store door interactable should exist in the scene.");
            Assert.IsTrue(ScenarioManager.Instance.CanEnterArea("store_entrance"));
            door.Interact(player);
            yield return null;
            Assert.AreEqual("node_find_onigiri", ScenarioManager.Instance.CurrentNode.id);

            // 6. Locate Onigiri item and simulate correct pickup
            var items = Object.FindObjectsByType<InteractiveItem>(FindObjectsSortMode.None);
            InteractiveItem targetItem = null;
            foreach (var item in items)
            {
                if (item.ItemId == "onigiri")
                {
                    targetItem = item;
                    break;
                }
            }
            Assert.NotNull(targetItem, "Onigiri item should exist in the scene.");
            
            // Trigger item interaction
            targetItem.Interact(player);
            yield return null;
            Assert.IsTrue(PlayerInventory.Instance.HasItem("onigiri"), "Picking the rice ball should place it in the backpack.");
            
            // 7. Verify node advanced to cashier meeting
            Assert.AreEqual("node_go_to_cashier", ScenarioManager.Instance.CurrentNode.id);

            // 8. Talk to the cashier before dialogue can start
            var cashier = Object.FindFirstObjectByType<NPCController>();
            Assert.NotNull(cashier, "Cashier NPC should exist in the scene.");
            Assert.IsTrue(ScenarioManager.Instance.CanEnterArea("cashier"));
            cashier.Interact(player);
            yield return null;
            Assert.AreEqual("node_cashier_prompt_bag", ScenarioManager.Instance.CurrentNode.id);
        }
    }
}
