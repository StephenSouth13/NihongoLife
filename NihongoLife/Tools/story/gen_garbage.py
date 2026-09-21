# -*- coding: utf-8 -*-
import sys

from scen_lib import Scenario

S = Scenario()
C, N = S.C, S.N
SA = ("Sato", "npc_neighbor_3")
NAR = None

N("node_find_sato", None, "", "", "", "", "", "b_tanaka", "obj_meet", type=6, npc="npc_neighbor_3")

# Sato biết bạn qua Tanaka nếu bạn đã kết bạn với bác ấy (callback)
S.B("b_tanaka", "tanaka.friend", "n_tanaka_praise", "n_open")
N("n_tanaka_praise", SA, "田中さんから聞きましたよ。礼儀正しい学生さんが来た、と。", "たなかさんからききましたよ。れいぎただしいがくせいさんがきた、と。", "Tanaka-san kara kikimashita yo. Reigi tadashii gakusei-san ga kita, to.",
  "Tôi nghe bác Tanaka kể rồi. Bác nói có một học viên rất lễ phép mới đến.", "talk", "n_open")

N("n_open", SA, "あなたが、新しい学生さんですね。町のゴミのルールを、説明します。", "あなたが、あたらしいがくせいさんですね。まちのゴミのルールを、せつめいします。", "Anata ga, atarashii gakusei-san desu ne. Machi no gomi no ruuru wo, setsumei shimasu.",
  "Bạn là học viên mới nhỉ. Tôi sẽ giải thích quy tắc đổ rác của khu phố.", "talk", choices=[
    C("はい、よろしくお願いします。", "Vâng, xin nhờ bác chỉ bảo ạ.", "n_q_when", "Vocabulary", 10, "Mở đầu lịch sự với người phụ trách khu phố bằng よろしくお願いします", [], ["vocab.n5.yoroshiku"]),
    C("はい。", "Vâng.", "n_q_when", "ResponseAccuracy", 3, "Đáp được nhưng hơi cộc với một người lớn tuổi có vai trò quan trọng", [], []),
    C("ルールは嫌いです。", "Cháu không thích quy tắc.", "n_open_wrong", "ResponseAccuracy", -8, "Nói thẳng điều tiêu cực ngay lúc đầu, gây ấn tượng xấu với trưởng khu phố"),
])
N("n_open_wrong", SA, "…そうですか。でも、ルールは、みんなのためです。もう一度、聞いてください。", "…そうですか。でも、ルールは、みんなのためです。もういちど、きいてください。", "...Sou desu ka. Demo, ruuru wa, minna no tame desu. Mou ichido, kiite kudasai.",
  "...Vậy à. Nhưng quy tắc là vì mọi người. Mời bạn nghe lại cho.", "talk", "n_open", "", choices=None)
# the retry above must not stay a dead node: patch flag for the stern reaction through a choice on the parent instead
S.nodes[-1]["nxt"] = "n_open"

# ───────── Hỏi ngày đổ rác ─────────
N("n_q_when", SA, "ゴミについて、質問がありますか。", "ゴミについて、しつもんがありますか。", "Gomi ni tsuite, shitsumon ga arimasu ka?",
  "Bạn có câu hỏi nào về việc đổ rác không?", "talk", choices=[
    C("燃えるゴミは、いつですか。", "Rác cháy được thì đổ khi nào ạ?", "n_when_answer", "Grammar", 10, "Hỏi lịch bằng mẫu 〜はいつですか (khi nào)", ["grammar.n5.wa_itsu_desu_ka"], ["vocab.n5.moeru_gomi", "vocab.n5.itsu"]),
    C("ゴミ、いつ？", "Rác, khi nào?", "n_when_casual", "ResponseAccuracy", 2, "Hiểu được nhưng quá cộc lốc với trưởng khu phố, và chưa nói rõ loại rác", ["grammar.n5.wa_itsu_desu_ka"], ["vocab.n5.gomi"]),
    C("ゴミはどこですか。", "Rác ở đâu ạ?", "n_when_wrong", "Grammar", -8, "Dùng どこ (ở đâu) trong khi muốn hỏi ngày; ngày phải dùng いつ"),
])
N("n_when_casual", SA, "「燃えるゴミは、いつですか」と聞きましょう。種類も言ってくださいね。", "「もえるゴミは、いつですか」とききましょう。しゅるいもいってくださいね。", "\"Moeru gomi wa, itsu desu ka\" to kikimashou. Shurui mo itte kudasai ne.",
  "Hãy hỏi là \"moeru gomi wa, itsu desu ka\". Nhớ nói rõ cả loại rác nữa nhé.", "talk", "n_q_when")
N("n_when_wrong", SA, "場所ではなくて、日です。「いつ」ですよ。もう一度。", "ばしょではなくて、ひです。「いつ」ですよ。もういちど。", "Basho dewa nakute, hi desu. \"Itsu\" desu yo. Mou ichido.",
  "Không phải hỏi nơi chốn mà là hỏi ngày. Dùng \"itsu\" nhé. Bạn hỏi lại đi.", "point", "n_q_when")
N("n_when_answer", SA, "燃えるゴミは、月曜日と木曜日です。ペットボトルは、金曜日です。", "もえるゴミは、げつようびともくようびです。ペットボトルは、きんようびです。", "Moeru gomi wa, getsuyoubi to mokuyoubi desu. Pettobotoru wa, kinyoubi desu.",
  "Rác cháy được là thứ Hai và thứ Năm. Chai nhựa là thứ Sáu.", "talk", "n_check_day")

N("n_check_day", SA, "では、ペットボトルは、何曜日ですか。", "では、ペットボトルは、なんようびですか。", "Dewa, pettobotoru wa, nan youbi desu ka?",
  "Vậy chai nhựa thì đổ vào thứ mấy?", "talk", "", "obj_rules", choices=[
    C("金曜日です。", "Là thứ Sáu ạ.", "n_q_time", "Vocabulary", 10, "Nhớ đúng thứ trong tuần: 金曜日 (thứ Sáu)", [], ["vocab.n5.kinyoubi", "vocab.n5.pettobotoru"], ["garbage.rules_known"]),
    C("木曜日です。", "Là thứ Năm ạ.", "n_day_wrong", "Vocabulary", -8, "Nhầm 木曜日 (thứ Năm, ngày đổ rác cháy được) với 金曜日 (thứ Sáu)"),
    C("もう一度お願いします。", "Phiền bác nói lại một lần nữa ạ.", "n_day_repeat", "ResponseAccuracy", 8, "Xin nghe lại lịch sự khi chưa nhớ rõ", [], ["vocab.n5.mou_ichido"]),
])
N("n_day_wrong", SA, "木曜日は、燃えるゴミです。ペットボトルは、金曜日ですよ。", "もくようびは、もえるゴミです。ペットボトルは、きんようびですよ。", "Mokuyoubi wa, moeru gomi desu. Pettobotoru wa, kinyoubi desu yo.",
  "Thứ Năm là rác cháy được. Chai nhựa là thứ Sáu nhé.", "point", "n_check_day")
N("n_day_repeat", SA, "はい。燃えるゴミは月曜日と木曜日。ペットボトルは金曜日です。", "はい。もえるゴミはげつようびともくようび。ペットボトルはきんようびです。", "Hai. Moeru gomi wa getsuyoubi to mokuyoubi. Pettobotoru wa kinyoubi desu.",
  "Vâng. Rác cháy được thứ Hai và thứ Năm. Chai nhựa thứ Sáu.", "point", "n_check_day")

# ───────── Giờ giấc và điều cấm (〜ないでください) ─────────
N("n_q_time", SA, "ゴミは、朝八時までに出してください。", "ゴミは、あさはちじまでにだしてください。", "Gomi wa, asa hachiji made ni dashite kudasai.",
  "Rác thì hãy đem ra trước tám giờ sáng.", "talk", choices=[
    C("朝八時までですね。分かりました。", "Trước tám giờ sáng nhỉ. Cháu hiểu rồi ạ.", "n_place", "Vocabulary", 10, "Xác nhận thời hạn bằng 〜までですね", ["grammar.n5.made"], ["vocab.n5.made", "vocab.n5.hachiji"]),
    C("夜に出してもいいですか。", "Cháu đem ra vào ban đêm có được không ạ?", "n_night_no", "Grammar", 4, "Hỏi xin phép đúng mẫu 〜てもいいですか; đây là câu hỏi hợp lý dù câu trả lời sẽ là không", ["grammar.n5.te_mo_ii_desu_ka"], ["vocab.n5.yoru"], ["sato.asked_night"]),
    C("夜に出します。大丈夫です。", "Cháu sẽ đem ra ban đêm. Không sao đâu ạ.", "n_night_stern", "ResponseAccuracy", -10, "Tự quyết định phá quy tắc thay vì hỏi ý kiến người phụ trách", [], [], ["sato.stern"]),
])
N("n_night_no", SA, "いいえ。夜に出さないでください。カラスが来ますから。", "いいえ。よるにださないでください。カラスがきますから。", "Iie. Yoru ni dasanai de kudasai. Karasu ga kimasu kara.",
  "Không được đâu. Xin đừng đem rác ra vào ban đêm. Vì quạ sẽ đến.", "talk", "n_q_time_ok")
N("n_night_stern", SA, "大丈夫ではありません！夜に出さないでください。カラスが来て、町がよごれます。", "だいじょうぶではありません！よるにださないでください。カラスがきて、まちがよごれます。", "Daijoubu dewa arimasen! Yoru ni dasanai de kudasai. Karasu ga kite, machi ga yogoremasu.",
  "Không ổn đâu! Xin đừng đem rác ra ban đêm. Quạ sẽ đến làm bẩn cả khu phố.", "point", "n_q_time_ok")
N("n_q_time_ok", SA, "もう一度、聞きます。ゴミは、何時までに出しますか。", "もういちど、ききます。ゴミは、なんじまでにだしますか。", "Mou ichido, kikimasu. Gomi wa, nanji made ni dashimasu ka?",
  "Tôi hỏi lại nhé. Rác thì phải đem ra trước mấy giờ?", "talk", choices=[
    C("朝八時までです。", "Trước tám giờ sáng ạ.", "n_place", "Vocabulary", 10, "Nhắc lại đúng giờ hạn chót: 朝八時まで", ["grammar.n5.made"], ["vocab.n5.made", "vocab.n5.hachiji"]),
    C("朝八時からです。", "Từ tám giờ sáng ạ.", "n_time_wrong", "Grammar", -8, "Nhầm から (từ) với まで (đến/trước): hạn chót là trước 8 giờ, không phải sau 8 giờ"),
])
N("n_time_wrong", SA, "「から」ではありません。「まで」です。八時までに、出してください。", "「から」ではありません。「まで」です。はちじまでに、だしてください。", "\"Kara\" dewa arimasen. \"Made\" desu. Hachiji made ni, dashite kudasai.",
  "Không phải \"kara\" mà là \"made\". Hãy đem ra trước tám giờ.", "point", "n_q_time_ok")

# ───────── Điểm tập kết ─────────
N("n_place", SA, "ゴミは、あの角の、ゴミ置き場に出してください。ネットをかけるのも、忘れないでくださいね。", "ゴミは、あのかどの、ゴミおきばにだしてください。ネットをかけるのも、わすれないでくださいね。", "Gomi wa, ano kado no, gomi okiba ni dashite kudasai. Netto wo kakeru no mo, wasurenai de kudasai ne.",
  "Rác thì hãy đem ra điểm tập kết ở góc đường kia. Đừng quên phủ tấm lưới lên nhé.", "point", choices=[
    C("はい、忘れません。", "Vâng, cháu sẽ không quên ạ.", "n_why", "Grammar", 10, "Đáp lại 〜ないでください bằng câu hứa 忘れません", ["grammar.n5.nai_de_kudasai"], ["vocab.n5.wasureru"]),
    C("ネットはどこですか。", "Tấm lưới ở đâu ạ?", "n_net_answer", "Grammar", 8, "Hỏi đúng mẫu 〜はどこですか để biết chỗ lấy lưới", ["grammar.n5.wa_doko_desu_ka"], ["vocab.n5.netto"]),
    C("ネットは嫌いです。", "Cháu không thích tấm lưới.", "n_place_wrong", "ResponseAccuracy", -8, "Nói điều tiêu cực không cần thiết về quy tắc được nhờ"),
])
N("n_net_answer", SA, "ゴミ置き場の横に、緑のネットがあります。それを使ってください。", "ゴミおきばのよこに、みどりのネットがあります。それをつかってください。", "Gomi okiba no yoko ni, midori no netto ga arimasu. Sore wo tsukatte kudasai.",
  "Bên cạnh điểm tập kết có tấm lưới màu xanh lá. Hãy dùng nó nhé.", "point", "n_why")
N("n_place_wrong", SA, "ネットがないと、カラスがゴミを食べます。だいじな物です。", "ネットがないと、カラスがゴミをたべます。だいじなものです。", "Netto ga nai to, karasu ga gomi wo tabemasu. Daiji na mono desu.",
  "Nếu không có lưới, quạ sẽ bới rác ra ăn. Nó quan trọng lắm.", "talk", "n_place")

# ───────── Lý do và thái độ ─────────
N("n_why", SA, "ルールは、みんなのためです。町がきれいだと、みんなが気持ちいいでしょう？", "ルールは、みんなのためです。まちがきれいだと、みんながきもちいいでしょう？", "Ruuru wa, minna no tame desu. Machi ga kirei da to, minna ga kimochi ii deshou?",
  "Quy tắc là vì mọi người. Khi khu phố sạch sẽ, ai cũng thấy dễ chịu, phải không?", "talk", choices=[
    C("はい、きれいな町は、気持ちいいです。", "Vâng, khu phố sạch thì thấy dễ chịu ạ.", "n_promise", "ResponseAccuracy", 10, "Đồng tình và diễn đạt lại bằng lời của mình: きれいな町は、気持ちいいです", ["grammar.n5.na_adj"], ["vocab.n5.kirei", "vocab.n5.kimochi_ii"], ["sato.respect"]),
    C("分かりました。", "Cháu hiểu rồi ạ.", "n_promise", "ResponseAccuracy", 3, "Đáp được nhưng chưa thể hiện sự đồng cảm với lý do của bác", [], []),
    C("面倒くさいです。", "Phiền phức quá ạ.", "n_why_wrong", "ResponseAccuracy", -8, "Than phiền thẳng với trưởng khu phố, rất bất lịch sự", [], [], ["sato.stern"]),
])
N("n_why_wrong", SA, "…面倒でも、大切です。もう一度、考えてください。", "…めんどうでも、たいせつです。もういちど、かんがえてください。", "...Mendou demo, taisetsu desu. Mou ichido, kangaete kudasai.",
  "...Dù phiền phức thì nó vẫn quan trọng. Xin bạn suy nghĩ lại.", "talk", "n_why")

# ───────── Hẹn buổi sáng thứ Hai ─────────
N("n_promise", SA, "では、来週の月曜日の朝、ゴミ置き場で会いましょう。いっしょにゴミを出しましょう。", "では、らいしゅうのげつようびのあさ、ゴミおきばであいましょう。いっしょにゴミをだしましょう。", "Dewa, raishuu no getsuyoubi no asa, gomi okiba de aimashou. Issho ni gomi wo dashimashou.",
  "Vậy thì sáng thứ Hai tuần sau, chúng ta gặp nhau ở điểm tập kết rác. Cùng nhau đem rác ra nhé.", "talk", "", "obj_promise", choices=[
    C("はい、月曜日の朝ですね。よろしくお願いします。", "Vâng, sáng thứ Hai nhỉ. Xin nhờ bác chỉ bảo ạ.", "n_bye", "Grammar", 10, "Xác nhận hẹn bằng 〜ですね và kết thúc lịch sự", ["grammar.n5.desu_ne"], ["vocab.n5.getsuyoubi", "vocab.n5.yoroshiku"], ["sato.appointment"]),
    C("はい、行きます。", "Vâng, cháu sẽ đến ạ.", "n_bye", "ResponseAccuracy", 4, "Đúng ý nhưng chưa nhắc lại thời gian để xác nhận", [], []),
    C("月曜日は忙しいです。", "Thứ Hai cháu bận ạ.", "n_promise_wrong", "ResponseAccuracy", -6, "Từ chối thẳng mà không đề xuất thời gian khác, hơi thiếu tế nhị"),
])
N("n_promise_wrong", SA, "そうですか。では、いつがいいですか。…でも、月曜日の朝は、ゴミの日です。がんばりましょう。", "そうですか。では、いつがいいですか。…でも、げつようびのあさは、ゴミのひです。がんばりましょう。", "Sou desu ka. Dewa, itsu ga ii desu ka. ...Demo, getsuyoubi no asa wa, gomi no hi desu. Ganbarimashou.",
  "Vậy à. Thế khi nào thì được? ...Nhưng sáng thứ Hai là ngày đổ rác. Mình cố gắng nhé.", "talk", "n_promise")
N("n_bye", SA, "はい、お願いします。町のこと、何でも聞いてくださいね。", "はい、おねがいします。まちのこと、なんでもきいてくださいね。", "Hai, onegaishimasu. Machi no koto, nan demo kiite kudasai ne.",
  "Vâng, nhờ bạn nhé. Về khu phố này, cứ hỏi tôi bất cứ điều gì.", "bow", "n_recap")

N("n_recap", NAR, "今日のポイント：「〜はいつですか」「〜ないでください」「〜まで」。", "きょうのポイント：「〜はいつですか」「〜ないでください」「〜まで」。", "Kyou no pointo: \"~ wa itsu desu ka\" \"~ nai de kudasai\" \"~ made\".",
  "Tóm tắt: \"~ wa itsu desu ka\" (~ khi nào?), \"~ nai de kudasai\" (xin đừng ~), \"~ made\" (đến/trước ~). Nhớ: rác cháy được thứ Hai và thứ Năm, chai nhựa thứ Sáu, đem ra trước 8 giờ sáng!", "", "n_complete")
N("n_complete", NAR, "", "", "", "", "", "", type=4)

objectives = [
    ("obj_meet", "佐藤さんに会う", "Gặp trưởng khu phố Sato", False),
    ("obj_rules", "ゴミの日を聞いて覚える", "Hỏi và ghi nhớ ngày đổ rác", False),
    ("obj_promise", "月曜日の朝に会う約束をする", "Hẹn gặp vào sáng thứ Hai", False),
]
targets = ["grammar.n5.wa_itsu_desu_ka", "grammar.n5.nai_de_kudasai", "grammar.n5.made", "vocab.n5.moeru_gomi", "vocab.n5.kinyoubi"]

problems = S.validate("node_find_sato", objectives)
print("problems:", problems)
if not problems:
    S.write(sys.argv[1], name="scenario_house3_garbage", sid="scenario.house3.garbage", title_ja="ゴミの出し方", title_vi="Quy tắc đổ rác của khu phố",
            desc_ja="町内会長の佐藤さんに、ゴミの日と出し方を聞きましょう。", desc_vi="Hỏi trưởng khu phố Sato về ngày đổ rác, giờ giấc và điểm tập kết; học cách hỏi và hiểu điều cấm.",
            chapter=3, targets=targets, objectives=objectives, start="node_find_sato")
    print("nodes", len(S.nodes))
