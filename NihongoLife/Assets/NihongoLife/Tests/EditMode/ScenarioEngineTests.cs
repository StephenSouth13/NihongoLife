using NUnit.Framework;
using UnityEngine;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Scenario;
using NihongoLife.Scoring;
using System.Collections.Generic;

namespace NihongoLife.Tests
{
    public class ScenarioEngineTests
    {
        [Test]
        public void PlayerProgress_SerializationTest()
        {
            // Arrange
            var progress = new PlayerProgressDto
            {
                playerId = "test_player",
                displayName = "Tester",
                xp = 250,
                level = 2,
                completedScenarios = new List<string> { "scenario.test1" }
            };

            // Act
            string json = JsonUtility.ToJson(progress);
            var deserialized = JsonUtility.FromJson<PlayerProgressDto>(json);

            // Assert
            Assert.AreEqual("test_player", deserialized.playerId);
            Assert.AreEqual("Tester", deserialized.displayName);
            Assert.AreEqual(250, deserialized.xp);
            Assert.AreEqual(2, deserialized.level);
            Assert.Contains("scenario.test1", deserialized.completedScenarios);
        }

        [Test]
        public void ScoringCalculation_BreakdownTest()
        {
            // Arrange
            var go = new GameObject("ScoringManager");
            var manager = go.AddComponent<ScoringManager>();
            manager.ResetScore();

            // Act
            manager.AddScore("Vocabulary", 10, "Correct Onigiri word", "node1");
            manager.AddScore("Vocabulary", -5, "Incorrect item select", "node2");
            manager.AddScore("Grammar", 15, "Polite choice", "node3");
            manager.AddScore("TaskCompletion", 100, "Done", "node4");

            var breakdown = manager.GetBreakdown("scenario.test");

            // Cleanup
            Object.DestroyImmediate(go);

            // Assert
            Assert.AreEqual("scenario.test", breakdown.scenarioId);
            
            var vocab = breakdown.categories.Find(c => c.category == "Vocabulary");
            var grammar = breakdown.categories.Find(c => c.category == "Grammar");
            var completion = breakdown.categories.Find(c => c.category == "TaskCompletion");

            Assert.NotNull(vocab);
            Assert.AreEqual(100, vocab.score); // clamped to 100

            Assert.NotNull(grammar);
            Assert.AreEqual(100, grammar.score); // base 100 + 15 clamped to 100

            Assert.NotNull(completion);
            Assert.AreEqual(100, completion.score); // starts at 0, goes up to 100
        }

        [Test]
        public void ScenarioDefinition_GetNodeTest()
        {
            // Arrange
            var scenario = ScriptableObject.CreateInstance<ScenarioDefinition>();
            scenario.id = "test_scenario";
            scenario.nodes = new List<ScenarioNode>
            {
                new ScenarioNode { id = "start", nodeType = ScenarioNodeType.GoToArea },
                new ScenarioNode { id = "end", nodeType = ScenarioNodeType.Complete }
            };

            // Act
            var nodeStart = scenario.GetNode("start");
            var nodeEnd = scenario.GetNode("end");
            var nodeMissing = scenario.GetNode("non_existent");

            // Assert
            Assert.NotNull(nodeStart);
            Assert.AreEqual(ScenarioNodeType.GoToArea, nodeStart.nodeType);
            Assert.NotNull(nodeEnd);
            Assert.AreEqual(ScenarioNodeType.Complete, nodeEnd.nodeType);
            Assert.Null(nodeMissing);
        }

        [Test]
        public void ScenarioValidator_AcceptsValidScenario()
        {
            var scenario = ScriptableObject.CreateInstance<ScenarioDefinition>();
            scenario.id = "scenario.valid";
            scenario.startNodeId = "start";
            scenario.objectives = new List<ObjectiveDefinition>
            {
                new ObjectiveDefinition { id = "obj_pickup", titleJa = "買う", titleEn = "Buy" }
            };
            scenario.nodes = new List<ScenarioNode>
            {
                new ScenarioNode
                {
                    id = "start",
                    nodeType = ScenarioNodeType.CollectItem,
                    targetItemId = "onigiri",
                    objectiveIdToComplete = "obj_pickup",
                    nextNodeId = "done"
                },
                new ScenarioNode { id = "done", nodeType = ScenarioNodeType.Complete }
            };

            var result = ScenarioValidator.Validate(scenario);

            Object.DestroyImmediate(scenario);
            Assert.IsFalse(result.HasErrors, result.ToLogString());
        }

        [Test]
        public void ScenarioValidator_RejectsMissingBranchesAndTargets()
        {
            var scenario = ScriptableObject.CreateInstance<ScenarioDefinition>();
            scenario.id = "scenario.invalid";
            scenario.startNodeId = "missing_start";
            scenario.objectives = new List<ObjectiveDefinition>
            {
                new ObjectiveDefinition { id = "obj_known" }
            };
            scenario.nodes = new List<ScenarioNode>
            {
                new ScenarioNode
                {
                    id = "start",
                    nodeType = ScenarioNodeType.GoToArea,
                    objectiveIdToComplete = "obj_missing",
                    nextNodeId = "missing_next",
                    choices = new List<DialogueChoice>
                    {
                        new DialogueChoice { textJa = "はい", nextNodeId = "missing_choice" }
                    }
                }
            };

            var result = ScenarioValidator.Validate(scenario);

            Object.DestroyImmediate(scenario);
            Assert.IsTrue(result.HasErrors);
            StringAssert.Contains("start node", result.ToLogString());
            StringAssert.Contains("targetAreaId", result.ToLogString());
            StringAssert.Contains("missing nextNodeId", result.ToLogString());
            StringAssert.Contains("missing objective", result.ToLogString());
            StringAssert.Contains("missing node", result.ToLogString());
        }

        [Test]
        public void GameControlService_OrdersCampaignAndFindsNextScenario()
        {
            var controlObject = new GameObject("GameControlService");
            var control = controlObject.AddComponent<GameControlService>();
            var database = ScriptableObject.CreateInstance<GameControlDatabase>();
            database.campaignScenarioIds = new List<string>
            {
                "scenario.street.first_talk",
                "scenario.konbini.buy_onigiri",
                "scenario.house1.greeting"
            };

            typeof(GameControlService)
                .GetField("database", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(control, database);

            Assert.AreEqual("scenario.konbini.buy_onigiri", control.FindNextCampaignScenarioId("scenario.street.first_talk"));
            Assert.AreEqual("scenario.house1.greeting", control.FindNextCampaignScenarioId("scenario.konbini.buy_onigiri"));
            Assert.AreEqual(string.Empty, control.FindNextCampaignScenarioId("scenario.house1.greeting"));

            Object.DestroyImmediate(database);
            Object.DestroyImmediate(controlObject);
        }

        [Test]
        public void LocalOnlineWorldService_TracksPresenceAndChat()
        {
            GameServices.Clear();
            var onlineWorld = new LocalOnlineWorldService();
            onlineWorld.Initialize();

            OnlineChatMessage receivedMessage = null;
            onlineWorld.OnChatMessageReceived += message => receivedMessage = message;

            onlineWorld.ConnectLocalPlayer("player-1", "Aiko");
            onlineWorld.UpdateLocalPlayerPose("90_TestSandbox", new Vector3(1f, 0f, 2f), Quaternion.Euler(0f, 45f, 0f));
            onlineWorld.SendChatMessage("town", "こんにちは");

            Assert.IsTrue(onlineWorld.IsConnected);
            Assert.AreEqual(1, onlineWorld.VisiblePlayers.Count);
            Assert.AreEqual("90_TestSandbox", onlineWorld.VisiblePlayers[0].sceneName);
            Assert.GreaterOrEqual(onlineWorld.ChatHistory.Count, 2);
            Assert.NotNull(receivedMessage);
            Assert.AreEqual("こんにちは", receivedMessage.text);

            onlineWorld.Disconnect();
            Assert.IsFalse(onlineWorld.IsConnected);
            Assert.AreEqual(0, onlineWorld.VisiblePlayers.Count);
        }
    }
}
