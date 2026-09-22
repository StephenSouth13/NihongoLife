# -*- coding: utf-8 -*-
import sys

from scen_lib import Scenario

S = Scenario()
C, N = S.C, S.N
SA = ("Sato", "npc_neighbor_3")
NAR = None

N("node_find_sato", None, "", "", "", "", "", "b_appt", "obj_meet", type=6, npc="npc_neighbor_3")

# Nếu đã hẹn với Sato ở house3.garbage thì mở lời khác
S.B("b_appt", "sato.appointment", "n_open_appt", "n_open_new")
N("n_open_appt", SA, "おはようございます。約束どおり、来てくれましたね。今日は、資源ごみの日です。", "おはようございます。やくそくどおり、きてくれましたね。きょうは、しげんごみのひです。", "Ohayou gozaimasu. Yakusoku doori, kite kuremashita ne. Kyou wa, shigen gomi no hi desu.",
  "Chào buổi sáng. Bạn đến đúng như đã hẹn nhỉ. Hôm nay là ngày đổ rác tái chế.", "talk", "n_intro")
N("n_open_new", SA, "おはようございます。今日は、資源ごみの日です。少し、手伝ってもらえますか。", "おはようございます。きょうは、しげんごみのひです。すこし、てつだってもらえますか。", "Ohayou gozaimasu. Kyou wa, shigen gomi no hi desu. Sukoshi, tetsudatte moraemasu ka?",
  "Chào buổi sáng. Hôm nay là ngày đổ rác tái chế. Bạn giúp tôi một chút được không?", "talk", "n_intro")

N("n_intro", SA, "このペットボトルは、どう出しますか。", "このペットボトルは、どうだしますか。", "Kono petto botoru wa, dou dashimasu ka?",
  "Chai nhựa này thì phải bỏ như thế nào?", "point", choices=[
    C("ふたを外して、洗ってください。", "Hãy tháo nắp rồi rửa sạch ạ.", "n_label", "Grammar", 10, "Trả lời đúng bằng mẫu 〜てください cho hai việc liên tiếp", ["grammar.n5.te_kudasai"], ["vocab.n5.futa", "vocab.n5.arau"]),
    C("そのまま捨ててください。", "Cứ vứt nguyên như vậy đi ạ.", "n_sort_hint", "ResponseAccuracy", -6, "Trả lời sai quy trình: chai nhựa phải tháo nắp và rửa trước khi vứt"),
])
N("n_sort_hint", SA, "そのままでは出せません。ふたを外して、洗います。", "そのままではだせません。ふたをはずして、あらいます。", "Sono mama dewa dasemasen. Futa wo hazushite, araimasu.",
  "Không thể bỏ nguyên như vậy được. Ta tháo nắp rồi rửa sạch.", "point", "n_intro")

N("n_label", SA, "ラベルも外さなければなりませんか。", "ラベルもはずさなければなりませんか。", "Raberu mo hazusanakereba narimasen ka?",
  "Có bắt buộc phải tháo cả nhãn không nhỉ?", "point", choices=[
    C("はい、外さなければなりません。", "Vâng, bắt buộc phải tháo ạ.", "n_where_sort", "Grammar", 10, "Diễn đạt việc bắt buộc bằng mẫu 〜なければなりません", ["grammar.n5.nakereba_naranai"], ["vocab.n5.raberu"]),
    C("いいえ、外します。", "Không, tháo ạ.", "n_grammar_hint", "Grammar", -4, "Mâu thuẫn: いいえ (không) nhưng lại nói 外します (sẽ tháo); trả lời việc bắt buộc cần dùng なければなりません"),
])
N("n_grammar_hint", SA, "必要なことを聞かれたら、「〜なければなりません」と答えますよ。もう一度、どうぞ。", "ひつようなことをきかれたら、「〜なければなりません」とこたえますよ。もういちど、どうぞ。", "Hitsuyou na koto wo kikaretara, \"~ nakereba narimasen\" to kotaemasu yo. Mou ichido, douzo.",
  "Khi được hỏi về việc bắt buộc, ta trả lời bằng mẫu \"~ nakereba narimasen\". Mời bạn nói lại.", "point", "n_label")

# ───────── Phân biệt các loại rác tái chế (kiểm tra hiểu) ─────────
N("n_where_sort", SA, "缶と、ビンと、古紙は、それぞれ違う袋に入れます。この缶は、どこに入れますか。", "かんと、ビンと、こしは、それぞれちがうふくろにいれます。このかんは、どこにいれますか。", "Kan to, bin to, koshi wa, sorezore chigau fukuro ni iremasu. Kono kan wa, doko ni iremasu ka?",
  "Lon, chai thủy tinh và giấy cũ thì bỏ vào các túi khác nhau. Cái lon này thì bỏ vào đâu?", "point", choices=[
    C("缶の袋に入れます。", "Bỏ vào túi đựng lon ạ.", "n_day", "Vocabulary", 10, "Phân loại đúng: lon (缶) vào túi dành cho lon", [], ["vocab.n5.kan"]),
    C("ビンの袋に入れます。", "Bỏ vào túi đựng chai thủy tinh ạ.", "n_sort_wrong2", "Vocabulary", -6, "Nhầm loại: đây là lon (缶), không phải chai thủy tinh (ビン)"),
])
N("n_sort_wrong2", SA, "それは、缶ですよ。ビンではありません。缶の袋に入れてくださいね。", "それは、かんですよ。ビンではありません。かんのふくろにいれてくださいね。", "Sore wa, kan desu yo. Bin dewa arimasen. Kan no fukuro ni irete kudasai ne.",
  "Đó là lon đấy. Không phải chai thủy tinh. Xin hãy bỏ vào túi đựng lon nhé.", "point", "n_day")

N("n_day", SA, "資源ごみは、何曜日に出せますか。", "しげんごみは、なんようびにだせますか。", "Shigen gomi wa, nan youbi ni dasemasu ka?",
  "Rác tái chế thì có thể bỏ vào thứ mấy?", "talk", "", "obj_learn_recycling", choices=[
    C("水曜日の朝です。", "Sáng thứ Tư ạ.", "n_end", "Vocabulary", 10, "Nhớ đúng ngày thu gom: 水曜日", [], ["vocab.n5.suiyoubi"]),
    C("毎晩です。", "Mỗi tối ạ.", "n_day_hint", "Vocabulary", -5, "Trả lời sai: chỉ thu gom sáng thứ Tư, không phải mỗi tối"),
])
N("n_day_hint", SA, "回収は、水曜日の朝だけですよ。もう一度、どうぞ。", "かいしゅうは、すいようびのあさだけですよ。もういちど、どうぞ。", "Kaishuu wa, suiyoubi no asa dake desu yo. Mou ichido, douzo.",
  "Chỉ thu gom vào sáng thứ Tư thôi đấy. Mời bạn nói lại.", "point", "n_day")

N("n_end", SA, "完璧です。これで町をきれいにできますね。", "かんぺきです。これでまちをきれいにできますね。", "Kanpeki desu. Kore de machi wo kirei ni dekimasu ne.",
  "Hoàn hảo. Vậy là chúng ta có thể giữ khu phố sạch đẹp rồi nhỉ.", "bow", "n_recap")
N("n_recap", NAR, "今日のポイント：「〜てください」「〜なければなりません」「何曜日ですか」。", "きょうのポイント：「〜てください」「〜なければなりません」「なんようびですか」。", "Kyou no pointo: \"~ te kudasai\" \"~ nakereba narimasen\" \"nan youbi desu ka\".",
  "Tóm tắt: \"~ te kudasai\" (hãy ~), \"~ nakereba narimasen\" (bắt buộc phải ~), \"nan youbi desu ka\" (thứ mấy?). Nhớ: lon, chai thủy tinh và giấy cũ bỏ vào túi riêng, thu gom sáng thứ Tư.", "", "node_complete")
N("node_complete", NAR, "", "", "", "", "", "", type=4)

objectives = [
    ("obj_meet", "佐藤さんに会う", "Gặp lại trưởng khu phố Sato", False),
    ("obj_learn_recycling", "分別を確認する", "Học quy tắc phân loại rác tái chế", False),
]
targets = ["grammar.n5.te_kudasai", "grammar.n5.nakereba_naranai", "vocab.n5.kan", "vocab.n5.suiyoubi", "vocab.n5.raberu"]

problems = S.validate("node_find_sato", objectives)
print("problems:", problems)
if not problems:
    S.write(sys.argv[1], name="scenario_neighborhood_recycling_morning", sid="scenario.neighborhood.recycling_morning", title_ja="資源ごみの朝", title_vi="Buổi sáng phân loại rác tái chế",
            desc_ja="佐藤さんと資源ごみの分け方を確認しましょう。", desc_vi="Cùng bác Sato kiểm tra cách phân loại rác tái chế: chai nhựa, lon, chai thủy tinh và giấy cũ.",
            chapter=3, targets=targets, objectives=objectives, start="node_find_sato")
    print("nodes", len(S.nodes))
