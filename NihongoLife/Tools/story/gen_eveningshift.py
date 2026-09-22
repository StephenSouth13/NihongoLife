# -*- coding: utf-8 -*-
import sys

from scen_lib import Scenario

S = Scenario()
C, N = S.C, S.N
ITO = ("Ito", "npc_cashier")
NAR = None

N("node_find_ito", None, "", "", "", "", "", "n_intro", "obj_help_cashier", type=6, npc="npc_cashier")

N("n_intro", ITO, "あ、いつもありがとうございます。夕方の準備を、手伝ってもらえますか。", "あ、いつもありがとうございます。ゆうがたのじゅんびを、てつだってもらえますか。", "A, itsumo arigatou gozaimasu. Yuugata no junbi wo, tetsudatte moraemasu ka?",
  "À, cảm ơn bạn lúc nào cũng ghé qua. Bạn giúp tôi chuẩn bị cho ca tối được không?", "talk", choices=[
    C("はい、何をすればいいですか。", "Vâng, cháu nên làm gì ạ?", "n_water", "Grammar", 10, "Hỏi việc cần làm bằng mẫu 〜すればいいですか", ["grammar.n5.sureba_ii_desu_ka"], []),
    C("はい、何をしますかでした？", "Vâng, đã làm gì vậy?", "n_offer_hint", "Grammar", -4, "Câu hỏi sai thì: nên hỏi việc sắp làm, không phải hỏi lại việc đã làm"),
])
N("n_offer_hint", ITO, "仕事を聞くときは、「何をすればいいですか」と言うと自然ですよ。", "しごとをきくときは、「なにをすればいいですか」というとしぜんですよ。", "Shigoto wo kiku toki wa, \"nani wo sureba ii desu ka\" to iu to shizen desu yo.",
  "Khi hỏi việc cần làm, nói \"nani wo sureba ii desu ka\" sẽ tự nhiên hơn.", "talk", "n_water")

# ───────── Kiểm tra hàng: nước ─────────
N("n_water", ITO, "水は、まだありますか。", "みずは、まだありますか。", "Mizu wa, mada arimasu ka?",
  "Nước vẫn còn hàng chứ?", "point", choices=[
    C("はい、まだあります。", "Vâng, vẫn còn ạ.", "n_onigiri", "Grammar", 10, "Trả lời đúng bằng mẫu まだ〜あります (vẫn còn)", ["grammar.n5.mada_arimasu"], []),
    C("はい、もうありません。", "Vâng, đã hết rồi ạ.", "n_quantity_hint", "Grammar", -5, "Mâu thuẫn: はい (vâng) nhưng lại nói hết hàng; nếu còn hàng phải dùng まだあります"),
])
N("n_quantity_hint", ITO, "棚にありますから、「まだあります」ですね。よく見てくださいね。", "たなにありますから、「まだあります」ですね。よくみてくださいね。", "Tana ni arimasu kara, \"mada arimasu\" desu ne. Yoku mite kudasai ne.",
  "Vì vẫn còn trên kệ nên phải là \"mada arimasu\" (vẫn còn) nhé. Bạn xem kỹ lại nhé.", "point", "n_water")

# ───────── Onigiri sắp hết: mẫu 〜たほうがいい ─────────
N("n_onigiri", ITO, "おにぎりは少ないですね。どうしますか。", "おにぎりはすくないですね。どうしますか。", "Onigiri wa sukunai desu ne. Dou shimasu ka?",
  "Cơm nắm còn ít nhỉ. Ta nên làm gì?", "point", choices=[
    C("追加したほうがいいです。", "Nên bổ sung thêm ạ.", "n_customer", "Grammar", 10, "Đề xuất bằng mẫu 〜たほうがいいです (nên làm ~)", ["grammar.n5.ta_hou_ga_ii"], ["vocab.n5.tsuika"]),
    C("追加しないです。", "Không cần bổ sung ạ.", "n_restock_hint", "ResponseAccuracy", -5, "Đề xuất không hợp lý: hàng đang ít mà lại nói không cần thêm"),
])
N("n_restock_hint", ITO, "少ないときは、追加したほうがいいですね。お客様が困りますから。", "すくないときは、ついかしたほうがいいですね。おきゃくさまがこまりますから。", "Sukunai toki wa, tsuika shita hou ga ii desu ne. Okyakusama ga komarimasu kara.",
  "Khi hàng còn ít thì nên bổ sung thêm nhé. Vì khách hàng sẽ gặp khó khăn nếu hết hàng.", "point", "n_onigiri")

# ───────── Tình huống khách hỏi (phản xạ dịch vụ, tương tự Chapter 6 roadmap) ─────────
N("n_customer", ITO, "もし、お客様に「おにぎりはありますか」と聞かれたら、何と答えますか。", "もし、おきゃくさまに「おにぎりはありますか」ときかれたら、なんとこたえますか。", "Moshi, okyakusama ni \"onigiri wa arimasu ka\" to kikaretara, nan to kotaemasu ka?",
  "Nếu khách hỏi \"onigiri wa arimasu ka\" (có cơm nắm không), bạn sẽ đáp gì?", "talk", choices=[
    C("はい、あちらにございます。", "Vâng, ở đằng kia ạ.", "n_phrase", "Grammar", 10, "Dùng kính ngữ ございます thay cho あります khi phục vụ khách", ["grammar.n5.gozaimasu"], ["vocab.n5.achira"]),
    C("はい、あります。", "Vâng, có ạ.", "n_customer_hint", "ResponseAccuracy", 4, "Đúng nghĩa nhưng thiếu kính ngữ dành cho khách hàng", ["grammar.n5.arimasu"], []),
])
N("n_customer_hint", ITO, "お客様には、「ございます」を使うと、もっとていねいですよ。", "おきゃくさまには、「ございます」をつかうと、もっとていねいですよ。", "Okyakusama ni wa, \"gozaimasu\" wo tsukau to, motto teinei desu yo.",
  "Với khách hàng, dùng \"gozaimasu\" sẽ lịch sự hơn đấy.", "talk", "n_phrase")

N("n_phrase", ITO, "最後に、お客様を迎える言葉は。", "さいごに、おきゃくさまをむかえることばは。", "Saigo ni, okyakusama wo mukaeru kotoba wa?",
  "Cuối cùng, câu dùng để chào đón khách là gì nhỉ?", "talk", choices=[
    C("いらっしゃいませ。", "Kính chào quý khách.", "n_end", "Vocabulary", 10, "Nhớ đúng câu chào đón khách chuẩn ở cửa hàng: いらっしゃいませ", [], ["vocab.n5.irasshaimase"]),
    C("行ってきます。", "Con đi đây.", "n_greeting_hint", "Vocabulary", -6, "Nhầm câu: 行ってきます là lời chào khi rời khỏi nhà, không phải câu chào đón khách"),
])
N("n_greeting_hint", ITO, "お店では、「いらっしゃいませ」と言いますよ。", "おみせでは、「いらっしゃいませ」といいますよ。", "Omise de wa, \"irasshaimase\" to iimasu yo.",
  "Ở cửa hàng thì ta nói \"irasshaimase\" nhé.", "talk", "n_phrase")

N("n_end", ITO, "よくできました。夕方の準備は完了です。ありがとうございました。", "よくできました。ゆうがたのじゅんびはかんりょうです。ありがとうございました。", "Yoku dekimashita. Yuugata no junbi wa kanryou desu. Arigatou gozaimashita.",
  "Bạn làm tốt lắm. Việc chuẩn bị ca tối đã xong. Cảm ơn bạn nhiều nhé.", "bow", "n_pay", "obj_help_cashier")
N("n_pay", ITO, "お礼に、これをどうぞ。", "おれいに、これをどうぞ。", "Orei ni, kore wo douzo.",
  "Để cảm ơn, mời bạn nhận cái này.", "point", "n_recap")
N("n_recap", NAR, "今日のポイント：「まだあります」「〜たほうがいいです」「いらっしゃいませ」。", "きょうのポイント：「まだあります」「〜たほうがいいです」「いらっしゃいませ」。", "Kyou no pointo: \"mada arimasu\" \"~ ta hou ga ii desu\" \"irasshaimase\".",
  "Tóm tắt: \"mada arimasu\" (vẫn còn), \"~ ta hou ga ii desu\" (nên làm ~), \"irasshaimase\" (câu chào đón khách). Với khách hàng, nhớ dùng \"gozaimasu\" thay cho \"arimasu\".", "", "node_complete")
N("node_complete", NAR, "", "", "", "", "", "", type=4)

objectives = [
    ("obj_help_cashier", "伊藤さんを手伝う", "Giúp chị Ito chuẩn bị ca tối", False),
]
targets = ["grammar.n5.mada_arimasu", "grammar.n5.ta_hou_ga_ii", "grammar.n5.gozaimasu", "vocab.n5.irasshaimase", "vocab.n5.tsuika"]

problems = S.validate("node_find_ito", objectives)
print("problems:", problems)
if not problems:
    S.write(sys.argv[1], name="scenario_konbini_evening_shift", sid="scenario.konbini.evening_shift", title_ja="コンビニの夕方シフト", title_vi="Ca tối ở cửa hàng tiện lợi",
            desc_ja="伊藤さんを手伝って、商品と接客表現を確認しましょう。", desc_vi="Giúp chị Ito kiểm tra hàng hóa và luyện cách nói chuyện với khách hàng vào ca tối.",
            chapter=3, targets=targets, objectives=objectives, start="node_find_ito")
    print("nodes", len(S.nodes))
