using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Scenario
{
    public static class BuiltInStoryScenarioCatalog
    {
        /// <summary>
        /// Superseded (2026-09-22): scenario.neighborhood.cat_followup, scenario.neighborhood.recycling_morning
        /// and scenario.konbini.evening_shift are now written as full ScenarioDefinition assets under
        /// Resources/Scenarios (with story-flag callbacks into house2.lostcat / house3.garbage /
        /// konbini.buy_onigiri — see StoryFlags.cs and Tools/story/). LocalScenarioRepository loads
        /// Resources/Scenarios first, so an asset with the same id always wins over these; this method is
        /// kept only so the private Create* builders below remain available as a reference/fallback.
        /// </summary>
        public static IEnumerable<ScenarioDefinition> CreateAll()
        {
            yield break;
        }

        private static ScenarioDefinition CreateCatFollowUp()
        {
            var scenario = Create("scenario.neighborhood.cat_followup", 3,
                "猫の手がかり", "Manh mối về chú mèo",
                "鈴木さんに新しい手がかりを伝えましょう。", "Báo cho Suzuki manh mối mới và luyện cách mô tả vị trí.");
            scenario.learningTargets.AddRange(new[] { "grammar.n5.te_imashita", "grammar.n5.no_soba", "vocab.n5.direction" });
            scenario.objectives.Add(Objective("obj_report_cat_clue", "手がかりを伝える", "Báo manh mối cho Suzuki"));
            scenario.nodes.Add(Talk("node_find_suzuki", "npc_neighbor_2", "obj_report_cat_clue", "node_cat_question"));
            scenario.nodes.Add(Dialogue("node_cat_question", "npc_neighbor_2", "Suzuki",
                "猫について、何か分かりましたか？", "Bạn đã tìm được manh mối gì về chú mèo chưa?", "Neko ni tsuite, nanika wakarimashita ka?",
                Choice("公園の近くで見ました。", "Tôi thấy nó gần công viên.", "node_cat_detail", 10),
                Choice("猫は公園です。", "Con mèo là công viên.", "node_cat_correction", -5)));
            scenario.nodes.Add(Dialogue("node_cat_correction", "npc_neighbor_2", "Suzuki",
                "場所には『の近くで見ました』を使うと自然ですよ。", "Khi nói địa điểm, dùng mẫu 'đã thấy ở gần...' sẽ tự nhiên hơn.", "Basho ni wa no chikaku de mimashita o tsukau to shizen desu yo.",
                Choice("公園の近くで見ました。", "Tôi thấy nó gần công viên.", "node_cat_detail", 8)));
            scenario.nodes.Add(Dialogue("node_cat_detail", "npc_neighbor_2", "Suzuki",
                "どんな猫でしたか？", "Đó là một chú mèo như thế nào?", "Donna neko deshita ka?",
                Choice("小さくて、白い猫でした。", "Đó là một chú mèo nhỏ màu trắng.", "node_cat_direction", 10),
                Choice("白いと小さい猫でした。", "Một chú mèo trắng và nhỏ.", "node_cat_adjective_hint", -4)));
            scenario.nodes.Add(Dialogue("node_cat_adjective_hint", "npc_neighbor_2", "Suzuki",
                "い形容詞をつなぐ時は『小さくて』と言います。", "Khi nối tính từ đuôi i, hãy dùng dạng 'chiisakute'.", "I-keiyoushi o tsunagu toki wa chiisakute to iimasu.",
                Choice("小さくて、白い猫でした。", "Đó là một chú mèo nhỏ màu trắng.", "node_cat_direction", 8)));
            scenario.nodes.Add(Dialogue("node_cat_direction", "npc_neighbor_2", "Suzuki",
                "公園のどちら側ですか？", "Nó ở phía nào của công viên?", "Kouen no dochira gawa desu ka?",
                Choice("コンビニの向かい側です。", "Ở phía đối diện cửa hàng tiện lợi.", "node_cat_thanks", 10),
                Choice("コンビニの中です。", "Ở bên trong cửa hàng tiện lợi.", "node_cat_verify", -3)));
            scenario.nodes.Add(Dialogue("node_cat_verify", "npc_neighbor_2", "Suzuki",
                "店の中ではなく、向かい側ですね？", "Không phải bên trong, mà là phía đối diện đúng không?", "Mise no naka dewa naku, mukaigawa desu ne?",
                Choice("はい、向かい側です。", "Vâng, ở phía đối diện.", "node_cat_thanks", 7)));
            scenario.nodes.Add(Dialogue("node_cat_thanks", "npc_neighbor_2", "Suzuki",
                "助かりました。一緒に探してくれて、ありがとうございます。", "Bạn giúp tôi nhiều lắm. Cảm ơn vì đã cùng tìm.", "Tasukarimashita. Issho ni sagashite kurete, arigatou gozaimasu.",
                "node_complete", "obj_report_cat_clue"));
            scenario.nodes.Add(Complete());
            ApplyQuest(scenario, "community", "npc_neighbor_2", 400, 50, new[] { "scenario.house2.lostcat" }, new[] { "scenario.town.summer_festival" },
                "Bạn thấy manh mối về chú mèo. Báo cho Suzuki và tả vị trí bằng tiếng Nhật.", "You spotted a clue about the cat. Tell Suzuki and describe the place in Japanese.", "猫の手がかりを見つけました。鈴木さんに伝え、場所を日本語で説明しましょう。",
                "Nhà Suzuki", "Suzuki's house", "鈴木さんの家");
            scenario.startNodeId = "node_find_suzuki";
            return scenario;
        }

        private static ScenarioDefinition CreateRecyclingMorning()
        {
            var scenario = Create("scenario.neighborhood.recycling_morning", 3,
                "資源ごみの朝", "Buổi sáng phân loại rác",
                "佐藤さんと資源ごみの分け方を確認しましょう。", "Cùng Sato kiểm tra cách phân loại rác tái chế trong khu phố.");
            scenario.learningTargets.AddRange(new[] { "grammar.n5.te_kudasai", "grammar.n5.nakereba_naranai", "vocab.n5.recycling" });
            scenario.objectives.Add(Objective("obj_learn_recycling", "分別を確認する", "Học quy tắc phân loại"));
            scenario.nodes.Add(Talk("node_find_sato", "npc_neighbor_3", "obj_learn_recycling", "node_sort_question"));
            scenario.nodes.Add(Dialogue("node_sort_question", "npc_neighbor_3", "Sato",
                "このペットボトルは、どう出しますか？", "Chai nhựa này phải bỏ như thế nào?", "Kono petto botoru wa, dou dashimasu ka?",
                Choice("ふたを外して、洗ってください。", "Hãy tháo nắp rồi rửa sạch.", "node_label_question", 10),
                Choice("そのまま捨ててください。", "Hãy vứt nguyên như vậy.", "node_sort_hint", -6)));
            scenario.nodes.Add(Dialogue("node_sort_hint", "npc_neighbor_3", "Sato",
                "そのままでは出せません。ふたを外して洗います。", "Không thể bỏ nguyên như vậy. Ta phải tháo nắp và rửa sạch.", "Sono mama dewa dasemasen. Futa o hazushite araimasu.",
                Choice("ふたを外して、洗ってください。", "Hãy tháo nắp rồi rửa sạch.", "node_label_question", 7)));
            scenario.nodes.Add(Dialogue("node_label_question", "npc_neighbor_3", "Sato",
                "ラベルも外さなければなりませんか？", "Có bắt buộc phải tháo nhãn không?", "Raberu mo hazusanakereba narimasen ka?",
                Choice("はい、外さなければなりません。", "Vâng, phải tháo nhãn.", "node_collection_day", 10),
                Choice("いいえ、外します。", "Không, tháo nhãn.", "node_grammar_hint", -4)));
            scenario.nodes.Add(Dialogue("node_grammar_hint", "npc_neighbor_3", "Sato",
                "必要なことは『なければなりません』で答えます。", "Việc bắt buộc được trả lời bằng mẫu 'nakereba narimasen'.", "Hitsuyou na koto wa nakereba narimasen de kotaemasu.",
                Choice("外さなければなりません。", "Phải tháo nhãn.", "node_collection_day", 7)));
            scenario.nodes.Add(Dialogue("node_collection_day", "npc_neighbor_3", "Sato",
                "資源ごみは何曜日に出せますか？", "Có thể bỏ rác tái chế vào thứ mấy?", "Shigen gomi wa nan youbi ni dasemasu ka?",
                Choice("水曜日の朝です。", "Sáng thứ Tư.", "node_recycling_end", 10),
                Choice("毎晩です。", "Mỗi tối.", "node_day_hint", -5)));
            scenario.nodes.Add(Dialogue("node_day_hint", "npc_neighbor_3", "Sato",
                "回収は水曜日の朝だけです。", "Chỉ thu gom vào sáng thứ Tư.", "Kaishuu wa suiyoubi no asa dake desu.",
                Choice("水曜日の朝に出します。", "Tôi sẽ mang ra vào sáng thứ Tư.", "node_recycling_end", 7)));
            scenario.nodes.Add(Dialogue("node_recycling_end", "npc_neighbor_3", "Sato",
                "完璧です。これで町をきれいにできますね。", "Hoàn hảo. Vậy là chúng ta có thể giữ khu phố sạch đẹp.", "Kanpeki desu. Kore de machi o kirei ni dekimasu ne.",
                "node_complete", "obj_learn_recycling"));
            scenario.nodes.Add(Complete());
            ApplyQuest(scenario, "community", "npc_neighbor_3", 300, 50, new[] { "scenario.house3.garbage" }, new[] { "scenario.town.summer_festival" },
                "Sáng thứ hai là ngày thu gom rác tái chế. Phân loại đúng và hỏi Sato khi chưa chắc.", "Monday morning is recycling day. Sort correctly and ask Sato when you are unsure.", "月曜の朝はリサイクルの日。正しく分別し、迷ったら佐藤さんに聞きましょう。",
                "Điểm tập kết rác gần nhà Sato", "The collection point near Sato's house", "佐藤さんの家の近くのゴミ置き場");
            scenario.startNodeId = "node_find_sato";
            return scenario;
        }

        private static ScenarioDefinition CreateStoreEveningShift()
        {
            var scenario = Create("scenario.konbini.evening_shift", 3,
                "コンビニの夕方シフト", "Ca tối ở cửa hàng tiện lợi",
                "伊藤さんを手伝って、商品と接客表現を確認しましょう。", "Giúp Ito kiểm tra hàng hóa và luyện cách phục vụ khách vào ca tối.");
            scenario.learningTargets.AddRange(new[] { "grammar.n5.mada_arimasu", "grammar.n5.hou_ga_ii", "vocab.n5.customer_service" });
            scenario.objectives.Add(Objective("obj_help_cashier", "伊藤さんを手伝う", "Giúp nhân viên cửa hàng"));
            scenario.nodes.Add(Talk("node_find_ito", "npc_cashier", "obj_help_cashier", "node_stock_question"));
            scenario.nodes.Add(Dialogue("node_stock_question", "npc_cashier", "Ito",
                "夕方の準備を手伝ってもらえますか？", "Bạn có thể giúp tôi chuẩn bị cho ca tối không?", "Yuugata no junbi o tetsudatte moraemasu ka?",
                Choice("はい、何をすればいいですか？", "Vâng, tôi nên làm gì?", "node_water_check", 10),
                Choice("はい、何をしますかでした？", "Vâng, đã làm gì vậy?", "node_offer_hint", -4)));
            scenario.nodes.Add(Dialogue("node_offer_hint", "npc_cashier", "Ito",
                "仕事を聞く時は『何をすればいいですか』が自然です。", "Khi hỏi việc cần làm, 'nani o sureba ii desu ka' sẽ tự nhiên hơn.", "Shigoto o kiku toki wa nani o sureba ii desu ka ga shizen desu.",
                Choice("何をすればいいですか？", "Tôi nên làm gì?", "node_water_check", 7)));
            scenario.nodes.Add(Dialogue("node_water_check", "npc_cashier", "Ito",
                "水はまだありますか？", "Nước vẫn còn hàng chứ?", "Mizu wa mada arimasu ka?",
                Choice("はい、まだあります。", "Vâng, vẫn còn.", "node_onigiri_check", 10),
                Choice("はい、もうありません。", "Vâng, đã hết rồi.", "node_quantity_hint", -5)));
            scenario.nodes.Add(Dialogue("node_quantity_hint", "npc_cashier", "Ito",
                "棚にありますから、『まだあります』ですね。", "Vì vẫn có trên kệ nên hãy nói 'mada arimasu'.", "Tana ni arimasu kara, mada arimasu desu ne.",
                Choice("はい、まだあります。", "Vâng, vẫn còn.", "node_onigiri_check", 7)));
            scenario.nodes.Add(Dialogue("node_onigiri_check", "npc_cashier", "Ito",
                "おにぎりは少ないですね。どうしますか？", "Cơm nắm còn ít. Chúng ta nên làm gì?", "Onigiri wa sukunai desu ne. Dou shimasu ka?",
                Choice("追加したほうがいいです。", "Nên bổ sung thêm.", "node_customer_phrase", 10),
                Choice("追加しないです。", "Không bổ sung.", "node_restock_hint", -5)));
            scenario.nodes.Add(Dialogue("node_restock_hint", "npc_cashier", "Ito",
                "少ない時は、追加したほうがいいですね。", "Khi còn ít thì nên bổ sung thêm.", "Sukunai toki wa, tsuika shita hou ga ii desu ne.",
                Choice("追加したほうがいいです。", "Nên bổ sung thêm.", "node_customer_phrase", 7)));
            scenario.nodes.Add(Dialogue("node_customer_phrase", "npc_cashier", "Ito",
                "最後に、お客様を迎える言葉は？", "Cuối cùng, câu dùng để chào đón khách là gì?", "Saigo ni, okyakusama o mukaeru kotoba wa?",
                Choice("いらっしゃいませ。", "Kính chào quý khách.", "node_shift_end", 10),
                Choice("行ってきます。", "Tôi đi rồi sẽ về.", "node_greeting_hint", -6)));
            scenario.nodes.Add(Dialogue("node_greeting_hint", "npc_cashier", "Ito",
                "お店では『いらっしゃいませ』と言います。", "Trong cửa hàng, ta nói 'irasshaimase'.", "Omise dewa irasshaimase to iimasu.",
                Choice("いらっしゃいませ。", "Kính chào quý khách.", "node_shift_end", 7)));
            scenario.nodes.Add(Dialogue("node_shift_end", "npc_cashier", "Ito",
                "よくできました。夕方の準備は完了です。", "Làm tốt lắm. Việc chuẩn bị ca tối đã hoàn tất.", "Yoku dekimashita. Yuugata no junbi wa kanryou desu.",
                "node_complete", "obj_help_cashier"));
            scenario.nodes.Add(Complete());
            ApplyQuest(scenario, "career", "npc_cashier", 1200, 50, new[] { "scenario.konbini.buy_onigiri" }, new string[0],
                "Ito nhờ bạn phụ ca tối ở cửa hàng. Học cách nói với khách khi hết hàng và gợi ý món khác.", "Ito asks you to help with the evening shift. Learn what to say when an item is out of stock and how to suggest another.", "伊藤さんに夜のシフトを頼まれました。品切れのときの言い方と、別の商品のすすめ方を学びます。",
                "ひばりコンビニ", "Hibari Konbini", "ひばりコンビニ");
            scenario.startNodeId = "node_find_ito";
            return scenario;
        }

        private static void ApplyQuest(ScenarioDefinition scenario, string questType, string giverNpcId, int rewardYen, int requiredKnowledge,
            string[] requiredScenarioIds, string[] unlockScenarioIds, string briefingVi, string briefingEn, string briefingJa,
            string locationVi, string locationEn, string locationJa)
        {
            scenario.questType = questType;
            scenario.giverNpcId = giverNpcId;
            scenario.rewardYen = rewardYen;
            scenario.requiredKnowledge = requiredKnowledge;
            scenario.requiredScenarioIds = new List<string>(requiredScenarioIds);
            scenario.unlockScenarioIds = new List<string>(unlockScenarioIds);
            scenario.branchId = questType == "career" ? "career.retail" : questType;
            scenario.briefingVi = briefingVi;
            scenario.briefingEn = briefingEn;
            scenario.briefingJa = briefingJa;
            scenario.locationHintVi = locationVi;
            scenario.locationHintEn = locationEn;
            scenario.locationHintJa = locationJa;
        }

        private static ScenarioDefinition Create(string id, int chapter, string titleJa, string titleEn, string descriptionJa, string descriptionEn)
        {
            var scenario = ScriptableObject.CreateInstance<ScenarioDefinition>();
            scenario.hideFlags = HideFlags.DontSave;
            scenario.id = id;
            scenario.version = 1;
            scenario.chapterIndex = chapter;
            scenario.titleJa = titleJa;
            scenario.titleEn = titleEn;
            scenario.descriptionJa = descriptionJa;
            scenario.descriptionEn = descriptionEn;
            return scenario;
        }

        private static ObjectiveDefinition Objective(string id, string ja, string en) =>
            new ObjectiveDefinition { id = id, titleJa = ja, titleEn = en };

        private static ScenarioNode Talk(string id, string npcId, string objectiveId, string next) =>
            new ScenarioNode { id = id, nodeType = ScenarioNodeType.TalkToNPC, targetNpcId = npcId, objectiveIdToComplete = objectiveId, nextNodeId = next };

        private static ScenarioNode Dialogue(string id, string speakerId, string speakerName, string ja, string en, string romaji, params DialogueChoice[] choices) =>
            new ScenarioNode { id = id, nodeType = ScenarioNodeType.Dialogue, speakerId = speakerId, speakerName = speakerName, textJa = ja, textReading = ja, textEn = en, textRomaji = romaji, choices = new List<DialogueChoice>(choices) };

        private static ScenarioNode Dialogue(string id, string speakerId, string speakerName, string ja, string en, string romaji, string next, string objectiveId) =>
            new ScenarioNode { id = id, nodeType = ScenarioNodeType.Dialogue, speakerId = speakerId, speakerName = speakerName, textJa = ja, textReading = ja, textEn = en, textRomaji = romaji, nextNodeId = next, objectiveIdToComplete = objectiveId };

        private static DialogueChoice Choice(string ja, string en, string next, int score)
        {
            var choice = new DialogueChoice { textJa = ja, textEn = en, nextNodeId = next };
            choice.scoreModifiers.Add(new ScoreEventModifier
            {
                category = score >= 0 ? "ResponseAccuracy" : "Grammar",
                value = score,
                reason = score >= 0 ? "Câu trả lời phù hợp ngữ cảnh" : "Cần sửa mẫu câu"
            });
            return choice;
        }

        private static ScenarioNode Complete() => new ScenarioNode { id = "node_complete", nodeType = ScenarioNodeType.Complete };
    }
}
