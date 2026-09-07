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
        private const string StreetScenarioPath = ScenarioFolder + "/scenario_street_first_talk.asset";

        [MenuItem("NihongoLife/Build Scenario Assets")]
        public static void BuildScenarioAssets()
        {
            EnsureFolderExists(ScenarioFolder);

            var streetScenario = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(StreetScenarioPath);
            if (streetScenario == null)
            {
                streetScenario = ScriptableObject.CreateInstance<ScenarioDefinition>();
                AssetDatabase.CreateAsset(streetScenario, StreetScenarioPath);
            }

            FillStreetFirstTalkScenario(streetScenario);
            EditorUtility.SetDirty(streetScenario);

            var scenario = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(ScenarioPath);
            if (scenario == null)
            {
                scenario = ScriptableObject.CreateInstance<ScenarioDefinition>();
                AssetDatabase.CreateAsset(scenario, ScenarioPath);
            }

            FillKonbiniScenario(scenario);
            EditorUtility.SetDirty(scenario);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(StreetScenarioPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(ScenarioPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[ScenarioAssetBuilder] Rebuilt scenario asset: {ScenarioPath}");
        }

        private static void FillStreetFirstTalkScenario(ScenarioDefinition scenario)
        {
            scenario.id = "scenario.street.first_talk";
            scenario.version = 1;
            scenario.titleJa = "街であいさつ";
            scenario.titleEn = "Talk to someone on the street";
            scenario.descriptionJa = "通りにいる人に話しかけて、あいさつと行き先を練習しましょう。";
            scenario.descriptionEn = "Walk to a person on the street, start a conversation, then practice the line with your mic.";
            scenario.chapterIndex = 1;
            scenario.learningTargets = new List<string>
            {
                "english.ipa.greeting",
                "japanese.greeting.konnichiwa",
                "japanese.pattern.doko_e_ikimasu_ka"
            };

            scenario.objectives = new List<ObjectiveDefinition>
            {
                Obj("obj_talk_to_neighbor", "通りの人に話しかける", "Talk to the person on the street"),
                Obj("obj_practice_voice", "声に出して練習する", "Practice the line with the microphone")
            };

            scenario.nodes = new List<ScenarioNode>
            {
                new ScenarioNode
                {
                    id = "node_find_neighbor",
                    nodeType = ScenarioNodeType.TalkToNPC,
                    nextNodeId = "node_neighbor_greeting",
                    objectiveIdToComplete = "obj_talk_to_neighbor",
                    targetNpcId = "npc_neighbor_1"
                },
                Dialogue("node_neighbor_greeting", "npc_neighbor_1", "Tanaka",
                    "こんにちは。どこへ行きますか。",
                    "こんにちは。どこへいきますか。",
                    "Hello. Where are you going?",
                    "Konnichiwa. Doko e ikimasu ka.",
                    "talk",
                    new List<DialogueChoice>
                    {
                        Choice("コンビニへ行きます。", "I am going to the convenience store.", "node_neighbor_reply",
                            Score("ResponseAccuracy", 10, "Answered the street greeting naturally")),
                        Choice("駅へ行きます。", "I am going to the station.", "node_neighbor_reply",
                            Score("Vocabulary", 8, "Practiced destination vocabulary"))
                    }),
                Dialogue("node_neighbor_reply", "npc_neighbor_1", "Tanaka",
                    "いいですね。気をつけて。",
                    "いいですね。きをつけて。",
                    "Nice. Take care.",
                    "Ii desu ne. Ki o tsukete.",
                    "bow",
                    null,
                    "node_street_complete"),
                new ScenarioNode
                {
                    id = "node_street_complete",
                    nodeType = ScenarioNodeType.Complete
                }
            };

            scenario.startNodeId = "node_find_neighbor";
        }

        private static void FillKonbiniScenario(ScenarioDefinition scenario)
        {
            scenario.id = "scenario.konbini.buy_onigiri";
            scenario.version = 3;
            scenario.titleJa = "コンビニで買い物";
            scenario.titleEn = "Mua sắm ở cửa hàng tiện lợi";
            scenario.descriptionJa = "おにぎりを買って、レジで会計を済ませましょう。";
            scenario.descriptionEn = "Vào cửa hàng, kiểm tra kệ đồ uống, chọn đúng onigiri và hoàn thành thanh toán tại quầy.";
            scenario.chapterIndex = 2;
            scenario.learningTargets = new List<string>
            {
                "grammar.n5.wo_kudasai",
                "grammar.n5.onegai_shimasu",
                "grammar.n5.daijoubu_desu",
                "vocab.n5.onigiri",
                "vocab.n5.mizu",
                "vocab.n5.ocha",
                "vocab.n5.fukuro"
            };

            scenario.objectives = new List<ObjectiveDefinition>
            {
                Obj("obj_enter_store", "コンビニに入る", "Vào cửa hàng tiện lợi"),
                Obj("obj_check_drink", "飲み物を確認する", "Kiểm tra kệ đồ uống"),
                Obj("obj_confirm_item", "買うものを選ぶ", "Chọn đúng món cần mua"),
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
                    nextNodeId = "node_check_drink",
                    objectiveIdToComplete = "obj_enter_store",
                    targetAreaId = "store_entrance"
                },
                new ScenarioNode
                {
                    id = "node_check_drink",
                    nodeType = ScenarioNodeType.InspectItem,
                    nextNodeId = "node_confirm_item",
                    objectiveIdToComplete = "obj_check_drink",
                    targetItemId = "water"
                },
                Dialogue("node_confirm_item", "system", "Hệ thống",
                    "今日買うものはどれですか？",
                    "きょうかうものはどれですか？",
                    "Hôm nay bạn cần mua món nào?", "Kyou kau mono wa dore desu ka?", "",
                    new List<DialogueChoice>
                    {
                        Choice("おにぎりをください。", "Tôi muốn cơm nắm.", "node_confirm_item_done",
                            Score("Vocabulary", 10, "Chọn đúng từ onigiri"),
                            Score("Grammar", 10, "Dùng mẫu をください")),
                        Choice("お茶をください。", "Tôi muốn trà xanh.", "node_confirm_item_hint",
                            Score("Vocabulary", -5, "Nhầm món cần mua")),
                        Choice("水をください。", "Tôi muốn nước.", "node_confirm_item_hint",
                            Score("ResponseAccuracy", -5, "Cần đọc lại yêu cầu nhiệm vụ"))
                    }),
                Dialogue("node_confirm_item_done", "system", "Hệ thống",
                    "はい、おにぎりですね。",
                    "はい、おにぎりですね。",
                    "Đúng rồi, hãy lấy onigiri trên kệ hàng.", "Hai, onigiri desu ne.", "",
                    null,
                    "node_find_onigiri",
                    "obj_confirm_item"),
                Dialogue("node_confirm_item_hint", "system", "Hệ thống",
                    "今日はおにぎりを買います。",
                    "きょうはおにぎりをかいます。",
                    "Hôm nay nhiệm vụ là mua cơm nắm. Hãy chọn onigiri.", "Kyou wa onigiri wo kaimasu.", "",
                    null,
                    "node_confirm_item"),
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

        private static ObjectiveDefinition Obj(string id, string titleJa, string titleEn)
        {
            return new ObjectiveDefinition { id = id, titleJa = titleJa, titleEn = titleEn };
        }

        private static ScenarioNode Dialogue(
            string id,
            string speakerId,
            string speakerName,
            string textJa,
            string reading,
            string textEn,
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
                textEn = textEn,
                textRomaji = romaji,
                textEnglishIpa = EnglishIpaFor(id),
                animationCue = animationCue,
                choices = choices ?? new List<DialogueChoice>(),
                nextNodeId = nextNodeId,
                objectiveIdToComplete = objectiveId
            };
        }

        private static DialogueChoice Choice(string textJa, string textEn, string nextNodeId, params ScoreEventModifier[] modifiers)
        {
            var choice = new DialogueChoice
            {
                textJa = textJa,
                textEn = textEn,
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

        private static string EnglishIpaFor(string nodeId)
        {
            switch (nodeId)
            {
                case "node_cashier_prompt_bag":
                    return "/du\u02d0 ju\u02d0 ni\u02d0d \u0259 b\u00e6\u0261/";
                case "node_bag_wrong_grammar":
                    return "/du\u02d0 ju\u02d0 mi\u02d0n ju\u02d0 ni\u02d0d \u0259 b\u00e6\u0261/";
                case "node_bag_yes":
                    return "/a\u026a \u028cnd\u0259r\u02c8st\u00e6nd \u00f0\u0259 b\u00e6\u0261 fi\u02d0 \u026az \u03b8ri\u02d0 jen/";
                case "node_bag_no":
                    return "/a\u026a \u028cnd\u0259r\u02c8st\u00e6nd \u00f0\u0259 to\u028atl \u026az f\u0254\u02d0r h\u028cndr\u0259d na\u026anti sev\u0259n jen/";
                case "node_pay_choice":
                    return "/pli\u02d0z t\u0283u\u02d0z \u0259 pe\u026am\u0259nt me\u03b8\u0259d/";
                case "node_transaction_done":
                    return "/\u03b8\u00e6\u014bk ju\u02d0 \u02c8v\u025bri m\u028ct\u0283 si\u02d0 ju\u02d0 \u0259\u02c8\u0261en/";
                case "node_neighbor_greeting":
                    return "/h\u0259\u02c8lo\u028a we\u0259r \u0251\u02d0r ju\u02d0 \u02c8\u0261o\u028a\u026a\u014b/";
                case "node_neighbor_reply":
                    return "/na\u026as te\u026ak ke\u0259r/";
                default:
                    return string.Empty;
            }
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
