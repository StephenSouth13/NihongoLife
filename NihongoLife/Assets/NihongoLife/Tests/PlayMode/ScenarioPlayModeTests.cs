using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using NihongoLife.Scenario;
using NihongoLife.Core;
using NihongoLife.Interaction;

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

            // 5. Advance from node_start to node_find_onigiri
            ScenarioManager.Instance.AdvanceNode();
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
            ScenarioManager.Instance.OnItemInteracted("onigiri", targetItem);
            yield return null;
            
            // 7. Verify node advanced to cashier meeting
            Assert.AreEqual("node_go_to_cashier", ScenarioManager.Instance.CurrentNode.id);
        }
    }
}
