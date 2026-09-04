using UnityEditor;
using UnityEngine;
using NihongoLife.Scenario;

namespace NihongoLife.Editor
{
    public static class TranslateScenarioTool
    {
        [MenuItem("NihongoLife/Scenarios/Translate and Generate Scenarios")]
        public static void TranslateAndGenerateScenarios()
        {
            TranslateKonbiniScenario();
            GenerateHouse1Greeting();
            GenerateHouse2LostCat();
            GenerateHouse3Garbage();
            AssetDatabase.SaveAssets();
            Debug.Log("[TranslateScenarioTool] All scenarios successfully translated and generated!");
        }

        private static void TranslateKonbiniScenario()
        {
            string path = "Assets/NihongoLife/Resources/Scenarios/scenario_konbini_buy_onigiri.asset";
            var scenario = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(path);
            if (scenario == null)
            {
                Debug.LogWarning($"[TranslateScenarioTool] Could not find scenario at {path}");
                return;
            }

            scenario.titleEn = "Shopping at the convenience store";
            scenario.descriptionEn = "Buy an onigiri and complete the payment at the cashier.";

            // Translate objectives
            if (scenario.objectives.Count >= 4)
            {
                scenario.objectives[0].titleEn = "Enter the convenience store";
                scenario.objectives[1].titleEn = "Find the onigiri";
                scenario.objectives[2].titleEn = "Go to the cashier";
                scenario.objectives[3].titleEn = "Check out";
            }

            // Translate dialogues
            var cashierNode = scenario.GetNode("node_cashier_talk");
            if (cashierNode != null)
            {
                cashierNode.textEn = "Welcome. Would you like a bag?";
                if (cashierNode.choices.Count >= 2)
                {
                    cashierNode.choices[0].textEn = "Yes, please.";
                    cashierNode.choices[1].textEn = "No, thank you. I'm fine.";
                }
            }
            
            var successNode = scenario.GetNode("node_cashier_success");
            if (successNode != null)
            {
                successNode.textEn = "Thank you very much.";
            }

            var checkoutNoItemNode = scenario.GetNode("node_checkout_no_item");
            if (checkoutNoItemNode != null)
            {
                checkoutNoItemNode.textEn = "This is not the requested item. Please find the onigiri!";
            }

            EditorUtility.SetDirty(scenario);
        }

        private static void GenerateHouse1Greeting()
        {
            var scenario = ScriptableObject.CreateInstance<ScenarioDefinition>();
            scenario.id = "scenario.house1.greeting";
            scenario.titleJa = "ご近所さんに挨拶";
            scenario.titleEn = "Greet a neighbor";
            scenario.descriptionJa = "近所の人に挨拶をして、自己紹介をしましょう。";
            scenario.descriptionEn = "Greet your neighbor and introduce yourself.";
            scenario.chapterIndex = 3;

            scenario.learningTargets.Add("grammar.n5.hajimemashite");
            scenario.learningTargets.Add("grammar.n5.kara_kimashita");

            scenario.objectives.Add(new ObjectiveDefinition { id = "obj_meet_neighbor", titleJa = "近所の人と話す", titleEn = "Talk to the neighbor" });

            var node1 = new ScenarioNode
            {
                id = "node_greeting_start",
                nodeType = ScenarioNodeType.Dialogue,
                speakerName = "Tanaka",
                speakerId = "npc_neighbor_1",
                textJa = "あ、こんにちは。",
                textReading = "あ、こんにちは。",
                textEn = "Ah, hello.",
                textRomaji = "A, konnichiwa."
            };
            
            var choice1 = new DialogueChoice
            {
                textJa = "こんにちは。初めまして。",
                textEn = "Hello. Nice to meet you.",
                nextNodeId = "node_greeting_intro"
            };
            choice1.scoreModifiers.Add(new ScoreEventModifier { category = "TaskCompletion", value = 10, reason = "Correct greeting" });
            node1.choices.Add(choice1);

            var node2 = new ScenarioNode
            {
                id = "node_greeting_intro",
                nodeType = ScenarioNodeType.Dialogue,
                speakerName = "Tanaka",
                speakerId = "npc_neighbor_1",
                textJa = "初めまして。田中です。どこから来ましたか？",
                textReading = "はじめまして。たなかです。どこからきましたか？",
                textEn = "Nice to meet you. I'm Tanaka. Where are you from?",
                textRomaji = "Hajimemashite. Tanaka desu. Doko kara kimashita ka?"
            };

            var choice2 = new DialogueChoice
            {
                textJa = "ベトナムから来ました。",
                textEn = "I came from Vietnam.",
                nextNodeId = "node_greeting_end"
            };
            choice2.scoreModifiers.Add(new ScoreEventModifier { category = "TaskCompletion", value = 10, reason = "Correct intro" });
            node2.choices.Add(choice2);

            var node3 = new ScenarioNode
            {
                id = "node_greeting_end",
                nodeType = ScenarioNodeType.Dialogue,
                speakerName = "Tanaka",
                speakerId = "npc_neighbor_1",
                textJa = "そうですか。よろしくお願いします。",
                textReading = "そうですか。よろしくおねがいします。",
                textEn = "I see. Best regards.",
                textRomaji = "Sou desu ka. Yoroshiku onegaishimasu.",
                animationCue = "bow",
                objectiveIdToComplete = "obj_meet_neighbor",
                nextNodeId = "node_complete"
            };

            var node4 = new ScenarioNode
            {
                id = "node_complete",
                nodeType = ScenarioNodeType.Complete
            };

            scenario.nodes.Add(node1);
            scenario.nodes.Add(node2);
            scenario.nodes.Add(node3);
            scenario.nodes.Add(node4);
            scenario.startNodeId = "node_greeting_start";

            string path = "Assets/NihongoLife/Resources/Scenarios/scenario_house1_greeting.asset";
            AssetDatabase.CreateAsset(scenario, path);
        }

        private static void GenerateHouse2LostCat()
        {
            var scenario = ScriptableObject.CreateInstance<ScenarioDefinition>();
            scenario.id = "scenario.house2.lostcat";
            scenario.titleJa = "迷子の猫";
            scenario.titleEn = "Lost Cat";
            scenario.descriptionJa = "近所の人が猫を探しています。手伝ってあげましょう。";
            scenario.descriptionEn = "A neighbor is looking for their cat. Let's help them.";
            scenario.chapterIndex = 3;

            scenario.objectives.Add(new ObjectiveDefinition { id = "obj_ask_neighbor", titleJa = "近所の人に聞く", titleEn = "Ask the neighbor" });

            var node1 = new ScenarioNode
            {
                id = "node_cat_start",
                nodeType = ScenarioNodeType.Dialogue,
                speakerName = "Suzuki",
                speakerId = "npc_neighbor_2",
                textJa = "すみません、私の猫を見ませんでしたか？",
                textReading = "すみません、わたしのねこをみませんでしたか？",
                textEn = "Excuse me, have you seen my cat?",
                textRomaji = "Sumimasen, watashi no neko o mimasen deshita ka?"
            };
            
            var choice1 = new DialogueChoice
            {
                textJa = "いいえ、見ませんでした。",
                textEn = "No, I haven't seen it.",
                nextNodeId = "node_cat_end"
            };
            node1.choices.Add(choice1);

            var node2 = new ScenarioNode
            {
                id = "node_cat_end",
                nodeType = ScenarioNodeType.Dialogue,
                speakerName = "Suzuki",
                speakerId = "npc_neighbor_2",
                textJa = "そうですか。ありがとうございます。",
                textReading = "そうですか。ありがとうございます。",
                textEn = "I see. Thank you anyway.",
                textRomaji = "Sou desu ka. Arigatou gozaimasu.",
                animationCue = "bow",
                objectiveIdToComplete = "obj_ask_neighbor",
                nextNodeId = "node_complete"
            };

            var node3 = new ScenarioNode
            {
                id = "node_complete",
                nodeType = ScenarioNodeType.Complete
            };

            scenario.nodes.Add(node1);
            scenario.nodes.Add(node2);
            scenario.nodes.Add(node3);
            scenario.startNodeId = "node_cat_start";

            string path = "Assets/NihongoLife/Resources/Scenarios/scenario_house2_lostcat.asset";
            AssetDatabase.CreateAsset(scenario, path);
        }

        private static void GenerateHouse3Garbage()
        {
            var scenario = ScriptableObject.CreateInstance<ScenarioDefinition>();
            scenario.id = "scenario.house3.garbage";
            scenario.titleJa = "ゴミの出し方";
            scenario.titleEn = "Garbage Sorting Rules";
            scenario.descriptionJa = "ゴミの出し方について質問しましょう。";
            scenario.descriptionEn = "Ask about the garbage disposal rules.";
            scenario.chapterIndex = 3;

            scenario.objectives.Add(new ObjectiveDefinition { id = "obj_ask_garbage", titleJa = "ゴミについて聞く", titleEn = "Ask about garbage" });

            var node1 = new ScenarioNode
            {
                id = "node_garbage_start",
                nodeType = ScenarioNodeType.Dialogue,
                speakerName = "Sato",
                speakerId = "npc_neighbor_3",
                textJa = "明日は燃えるゴミの日ですよ。",
                textReading = "あしたはもえるゴミのひですよ。",
                textEn = "Tomorrow is burnable garbage day.",
                textRomaji = "Ashita wa moeru gomi no hi desu yo."
            };
            
            var choice1 = new DialogueChoice
            {
                textJa = "はい、わかりました。ありがとうございます。",
                textEn = "Yes, I understand. Thank you.",
                nextNodeId = "node_garbage_end"
            };
            node1.choices.Add(choice1);

            var node2 = new ScenarioNode
            {
                id = "node_garbage_end",
                nodeType = ScenarioNodeType.Dialogue,
                speakerName = "Sato",
                speakerId = "npc_neighbor_3",
                textJa = "どういたしまして。",
                textReading = "どういたしまして。",
                textEn = "You're welcome.",
                textRomaji = "Douitashimashite.",
                animationCue = "point",
                objectiveIdToComplete = "obj_ask_garbage",
                nextNodeId = "node_complete"
            };

            var node3 = new ScenarioNode
            {
                id = "node_complete",
                nodeType = ScenarioNodeType.Complete
            };

            scenario.nodes.Add(node1);
            scenario.nodes.Add(node2);
            scenario.nodes.Add(node3);
            scenario.startNodeId = "node_garbage_start";

            string path = "Assets/NihongoLife/Resources/Scenarios/scenario_house3_garbage.asset";
            AssetDatabase.CreateAsset(scenario, path);
        }
    }
}
