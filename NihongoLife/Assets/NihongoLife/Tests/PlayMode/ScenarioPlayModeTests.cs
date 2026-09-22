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
using NihongoLife.Save;

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

            // Seed only the prerequisite needed by this focused sandbox flow.
            Assert.IsTrue(GameServices.TryGet(out IProgressRepository progressRepository));
            var progress = progressRepository.GetProgress();
            progress.knowledge = 20;
            if (!progress.completedScenarios.Contains("scenario.school.self_intro"))
            {
                progress.completedScenarios.Add("scenario.school.self_intro");
            }
            progressRepository.SaveProgress(progress);
            ScenarioSceneInitializer.QueueLaunch("scenario.konbini.buy_onigiri");

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
            Assert.AreEqual("n_greet", ScenarioManager.Instance.CurrentNode.id);

            // The real cashier is now the next interaction gate in the authored quest chain.
            NPCController cashier = null;
            foreach (var npc in Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None))
            {
                if (npc.NpcId == "npc_cashier")
                {
                    cashier = npc;
                    break;
                }
            }
            Assert.NotNull(cashier, "Cashier NPC should exist in the scene.");
            Assert.AreEqual("npc_cashier", cashier.NpcId);
        }
    }
}
