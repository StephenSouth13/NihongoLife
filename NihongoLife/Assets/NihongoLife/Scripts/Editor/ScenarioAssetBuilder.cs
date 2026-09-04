using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using NihongoLife.Scenario;

namespace NihongoLife.Editor
{
    public static class ScenarioAssetBuilder
    {
        private const string ScenarioFolder = "Assets/NihongoLife/Resources/Scenarios";
        private const string ScenarioPath = ScenarioFolder + "/scenario_konbini_buy_onigiri.asset";

        [MenuItem("NihongoLife/Build Scenario Assets")]
        public static void BuildScenarioAssets()
        {
            EnsureFolderExists(ScenarioFolder);

            var scenario = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(ScenarioPath);
            if (scenario == null)
            {
                scenario = ScriptableObject.CreateInstance<ScenarioDefinition>();
                AssetDatabase.CreateAsset(scenario, ScenarioPath);
            }

            FillKonbiniScenario(scenario);
            EditorUtility.SetDirty(scenario);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ScenarioPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[ScenarioAssetBuilder] Rebuilt scenario asset: {ScenarioPath}");
        }

        private static void FillKonbiniScenario(ScenarioDefinition scenario)
        {
            scenario.id = "scenario.konbini.buy_onigiri";
            scenario.version = 2;
            scenario.titleJa = "コンビニで買い物";
            scenario.titleVi = "Mua sắm ở cửa hàng tiện lợi";
            scenario.descriptionJa = "おにぎりを買って、レジで会計を済ませましょう。";
            scenario.descriptionVi = "Mua một chiếc cơm nắm và hoàn thành thanh toán tại quầy thu ngân.";
            scenario.chapterIndex = 2;
            scenario.learningTargets = new List<string>
            {
                "grammar.n5.wo_kudasai",
                "grammar.n5.onegai_shimasu",
                "grammar.n5.daijoubu_desu",
                "vocab.n5.onigiri",
                "vocab.n5.fukuro"
            };

            scenario.objectives = new List<ObjectiveDefinition>
            {
                Obj("obj_enter_store", "コンビニに入る", "Vào cửa hàng tiện lợi"),
                Obj("obj_find_onigiri", "おにぎりを見つける", "Tìm cơm nắm"),
                Obj("obj_go_to_cashier", "レジへ行く", "Đi đến quầy thu ngân"),
                Obj("obj_pay", "お会計をする", "Thanh toán")
            };

            scenario.nodes = new List<ScenarioNode>
            {
                new ScenarioNode
                {
                    id = "node_start",
                    nodeType = ScenarioNodeType.GoToArea,
                    nextNodeId = "node_find_onigiri",
                    objectiveIdToComplete = "obj_enter_store",
                    targetAreaId = "store_entrance"
                },
                new ScenarioNode
                {
                    id = "node_find_onigiri",
                    nodeType = ScenarioNodeType.CollectItem,
                    nextNodeId = "node_go_to_cashier",
                    objectiveIdToComplete = "obj_find_onigiri",
                    targetItemId = "onigiri"
                },
                new ScenarioNode
                {
                    id = "node_go_to_cashier",
                    nodeType = ScenarioNodeType.GoToArea,
                    nextNodeId = "node_cashier_prompt_bag",
                    objectiveIdToComplete = "obj_go_to_cashier",
                    targetAreaId = "cashier"
                },
                Dialogue("node_cashier_prompt_bag", "npc_cashier", "Thu ngân",
                    "袋は要りますか？", "ふくろはいりますか？",
                    "Quý khách có cần túi không?", "Fukuro wa irimasu ka?", "",
                    new List<DialogueChoice>
                    {
                        Choice("はい、お願いします。", "Vâng, xin vui lòng.", "node_bag_yes",
                            Score("Vocabulary", 10, "Hiểu từ túi (fukuro)"),
                            Score("Grammar", 10, "Dùng mẫu ~onegaishimasu")),
                        Choice("いいえ、大丈夫です。", "Không, tôi ổn rồi. Không cần túi.", "node_bag_no",
                            Score("Vocabulary", 10, "Hiểu từ túi (fukuro)"),
                            Score("Grammar", 10, "Dùng mẫu ~daijoubu desu")),
                        Choice("袋を要ります。", "Tôi cần túi. (Sai trợ từ)", "node_bag_wrong_grammar",
                            Score("Grammar", -10, "Sai trợ từ: 要る thường đi với が hoặc は"),
                            Score("ResponseAccuracy", -5, "Phản xạ chưa chính xác"))
                    }),
                Dialogue("node_bag_wrong_grammar", "npc_cashier", "Thu ngân",
                    "すみません、袋は必要ですか？かしこまりました。",
                    "すみません、ふくろはひつようですか？かしこまりました。",
                    "Ý bạn là bạn cần túi phải không? Ở quầy thanh toán, cách nói tự nhiên là “はい、お願いします” hoặc “いいえ、大丈夫です”.",
                    "Sumimasen, fukuro wa hitsuyou desu ka? Kashikomarimashita.",
                    "point",
                    null,
                    "node_bag_yes"),
                Dialogue("node_bag_yes", "npc_cashier", "Thu ngân",
                    "かしこまりました。袋代3円になります。お会計は500円です。",
                    "かしこまりました。ふくろだいさんえんになります。おかいけいはごひゃくえんです。",
                    "Tôi hiểu rồi. Tiền túi là 3 yên. Tổng cộng là 500 yên ạ.",
                    "Kashikomarimashita. Fukurodai san-en ni narimasu. Okaikei wa gohyaku-en desu.",
                    "bow",
                    null,
                    "node_pay_choice"),
                Dialogue("node_bag_no", "npc_cashier", "Thu ngân",
                    "かしこまりました。お会計は497円です。",
                    "かしこまりました。おかいけいはよんひゃくきゅうじゅうななえんです。",
                    "Tôi hiểu rồi. Tổng cộng là 497 yên ạ.",
                    "Kashikomarimashita. Okaikei wa yonhyaku kyujuunana-en desu.",
                    "bow",
                    null,
                    "node_pay_choice"),
                Dialogue("node_pay_choice", "system", "Hệ thống",
                    "支払方法を選択してください。",
                    "しはらいほうほうをせんたくしてください。",
                    "Hãy chọn phương thức thanh toán.",
                    "Shiharai houhou wo sentaku shite kudasai.",
                    "",
                    new List<DialogueChoice>
                    {
                        Choice("これでお願いします。", "Thanh toán bằng cái này ạ.", "node_transaction_done",
                            Score("Grammar", 15, "Dùng mẫu thanh toán “kore de”")),
                        Choice("カードで払います。", "Thanh toán bằng thẻ.", "node_transaction_done",
                            Score("Vocabulary", 10, "Dùng từ thẻ (kaado)"))
                    }),
                Dialogue("node_transaction_done", "npc_cashier", "Thu ngân",
                    "ありがとうございます。またお越しくださいませ。",
                    "ありがとうございます。またおこしくださいませ。",
                    "Xin cảm ơn quý khách. Hẹn gặp lại quý khách lần sau.",
                    "Arigatou gozaimasu. Mata okoshi kudasaimase.",
                    "bow",
                    null,
                    "node_complete",
                    "obj_pay"),
                new ScenarioNode
                {
                    id = "node_complete",
                    nodeType = ScenarioNodeType.Complete
                }
            };

            scenario.startNodeId = "node_start";
        }

        private static ObjectiveDefinition Obj(string id, string titleJa, string titleVi)
        {
            return new ObjectiveDefinition { id = id, titleJa = titleJa, titleVi = titleVi };
        }

        private static ScenarioNode Dialogue(
            string id,
            string speakerId,
            string speakerName,
            string textJa,
            string reading,
            string textVi,
            string romaji,
            string animationCue,
            List<DialogueChoice> choices,
            string nextNodeId = "",
            string objectiveId = "")
        {
            return new ScenarioNode
            {
                id = id,
                nodeType = ScenarioNodeType.Dialogue,
                speakerId = speakerId,
                speakerName = speakerName,
                textJa = textJa,
                textReading = reading,
                textVi = textVi,
                textRomaji = romaji,
                animationCue = animationCue,
                choices = choices ?? new List<DialogueChoice>(),
                nextNodeId = nextNodeId,
                objectiveIdToComplete = objectiveId
            };
        }

        private static DialogueChoice Choice(string textJa, string textVi, string nextNodeId, params ScoreEventModifier[] modifiers)
        {
            var choice = new DialogueChoice
            {
                textJa = textJa,
                textVi = textVi,
                nextNodeId = nextNodeId,
                scoreModifiers = new List<ScoreEventModifier>(modifiers),
                grammarTags = new List<string>(),
                vocabularyTags = new List<string>()
            };
            return choice;
        }

        private static ScoreEventModifier Score(string category, int value, string reason)
        {
            return new ScoreEventModifier { category = category, value = value, reason = reason };
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && parent != "Assets" && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
        }
    }
}
