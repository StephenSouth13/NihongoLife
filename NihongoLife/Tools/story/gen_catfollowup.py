# -*- coding: utf-8 -*-
import sys

from scen_lib import Scenario

S = Scenario()
C, N = S.C, S.N
SZ = ("Suzuki", "npc_neighbor_2")
NAR = None

N("node_find_suzuki", None, "", "", "", "", "", "b_promised", "obj_meet", type=6, npc="npc_neighbor_2")

# Nếu bạn đã hứa sẽ báo tin (house2.lostcat) thì Suzuki mở lời khác với nếu chưa từng nói chuyện với cô
S.B("b_promised", "cat.promised", "n_open_promised", "n_open_new")
N("n_open_promised", SZ, "あ、この間、手伝うと言ってくれた方ですね！ミケのこと、何か分かりましたか。", "あ、このあいだ、てつだうといってくれたかたですね！ミケのこと、なにかわかりましたか。", "A, kono aida, tetsudau to itte kureta kata desu ne! Mike no koto, nanika wakarimashita ka.",
  "À, là người hôm trước đã hứa giúp tôi phải không! Bạn có biết được gì về Mike chưa?", "talk", "n_check")
N("n_open_new", SZ, "あの…すみません。うちの猫が、まだ見つからないんです。", "あの…すみません。うちのねこが、まだみつからないんです。", "Ano... sumimasen. Uchi no neko ga, mada mitsukaranai n desu.",
  "À... xin lỗi bạn. Con mèo nhà tôi vẫn chưa tìm thấy.", "talk", "n_check")

N("n_check", SZ, "猫について、何か分かりましたか。", "ねこについて、なにかわかりましたか。", "Neko ni tsuite, nanika wakarimashita ka?",
  "Bạn đã tìm được manh mối gì về chú mèo chưa?", "talk", choices=[
    C("公園の近くで見ました。", "Tôi đã thấy nó gần công viên.", "b_where_hint", "Grammar", 10, "Mô tả vị trí bằng mẫu 〜の近くで見ました", ["grammar.n5.no_chikaku_de", "grammar.n5.mashita"], ["vocab.n5.kouen"]),
    C("猫は公園です。", "Con mèo là công viên.", "n_correction", "Grammar", -8, "Câu vô nghĩa: mèo không thể 'là' công viên; cần dùng mẫu mô tả vị trí"),
])
N("n_correction", SZ, "場所を言うときは、「〜の近くで見ました」と言うと自然ですよ。もう一度、どうぞ。", "ばしょをいうときは、「〜のちかくでみました」というとしぜんですよ。もういちど、どうぞ。", "Basho wo iu toki wa, \"~ no chikaku de mimashita\" to iu to shizen desu yo. Mou ichido, douzo.",
  "Khi nói vị trí, dùng mẫu \"~ no chikaku de mimashita\" (đã thấy gần ~) sẽ tự nhiên hơn. Mời bạn nói lại.", "talk", "n_check")

# ───────── Vị trí cụ thể (đọc lại theo gợi ý ở house2.lostcat) ─────────
S.B("b_where_hint", "cat.shrine_hint", "n_where_shrine", "n_where_park")
N("n_where_shrine", SZ, "神社の近くですか。前に、ミケはそこが好きだと言いましたね。", "じんじゃのちかくですか。まえに、ミケはそこがすきだといいましたね。", "Jinja no chikaku desu ka. Mae ni, Mike wa soko ga suki da to iimashita ne.",
  "Gần đền thờ à. Lúc trước tôi có nói Mike thích chỗ đó nhỉ.", "talk", "n_detail_after")
N("n_where_park", SZ, "公園ですか。そういえば、ミケは公園にもよく行きますね。", "こうえんですか。そういえば、ミケはこうえんにもよくいきますね。", "Kouen desu ka. Souieba, Mike wa kouen ni mo yoku ikimasu ne.",
  "Công viên à. À phải rồi, Mike cũng hay đi công viên nhỉ.", "talk", "n_detail_after")

N("n_detail_after", SZ, "どんな猫でしたか？", "どんなねこでしたか？", "Donna neko deshita ka?",
  "Đó là một chú mèo như thế nào?", "talk", choices=[
    C("小さくて、白い猫でした。", "Đó là một chú mèo nhỏ, màu trắng.", "n_ask_ribbon", "Grammar", 10, "Nối hai tính từ đuôi い bằng dạng 〜くて: 小さくて", ["grammar.n5.te_form_adj"], ["vocab.n5.chiisai", "vocab.n5.shiroi"]),
    C("白いと小さい猫でした。", "Một chú mèo trắng và nhỏ.", "n_adjective_hint", "Grammar", -4, "Nối hai tính từ đuôi い phải đổi thành dạng 〜くて (小さくて), không dùng と"),
])
N("n_adjective_hint", SZ, "い形容詞をつなぐ時は「小さくて」と言いますよ。もう一度、どうぞ。", "いけいようしをつなぐときは「ちいさくて」といいますよ。もういちど、どうぞ。", "I-keiyoushi wo tsunagu toki wa \"chiisakute\" to iimasu yo. Mou ichido, douzo.",
  "Khi nối tính từ đuôi い, hãy nói \"chiisakute\" nhé. Mời bạn nói lại.", "point", "n_detail_after")

N("n_ask_ribbon", SZ, "そうです、ミケです！首に、赤いリボンをしていましたか。", "そうです、ミケです！くびに、あかいリボンをしていましたか。", "Sou desu, Mike desu! Kubi ni, akai ribon wo shite imashita ka?",
  "Đúng rồi, chính là Mike! Nó có đeo chiếc nơ đỏ ở cổ không?", "talk", choices=[
    C("はい、赤いリボンをしていました。", "Vâng, nó có đeo một chiếc nơ đỏ ạ.", "n_direction", "Grammar", 10, "Xác nhận đúng chi tiết bằng thì quá khứ 〜していました", ["grammar.n5.te_imashita"], ["vocab.n5.ribon"], ["cat.described"]),
    C("覚えていません。", "Tôi không nhớ rõ.", "n_direction", "ResponseAccuracy", 3, "Trả lời thành thật nhưng thiếu chi tiết quan trọng", [], ["vocab.n5.oboeru"]),
])

N("n_direction", SZ, "公園のどちら側ですか。", "こうえんのどちらがわですか。", "Kouen no dochira gawa desu ka?",
  "Nó ở phía nào của công viên?", "talk", choices=[
    C("コンビニの向かい側です。", "Ở phía đối diện cửa hàng tiện lợi ạ.", "n_thanks", "Grammar", 10, "Mô tả vị trí tương đối bằng 〜の向かい側です", ["grammar.n5.no_mukaigawa"], ["vocab.n5.konbini"], ["cat.direction_given"]),
    C("コンビニの中です。", "Ở bên trong cửa hàng tiện lợi ạ.", "n_verify", "ResponseAccuracy", -3, "Vị trí không hợp lý: mèo hoang không ở trong cửa hàng, dễ gây hiểu nhầm"),
])
N("n_verify", SZ, "お店の中ではなく、向かい側ですね？", "おみせのなかではなく、むかいがわですね？", "Omise no naka dewa naku, mukaigawa desu ne?",
  "Không phải bên trong cửa hàng, mà là phía đối diện đúng không ạ?", "talk", choices=[
    C("はい、向かい側です。", "Vâng, phía đối diện ạ.", "n_thanks", "ResponseAccuracy", 7, "Xác nhận lại đúng vị trí", [], [], ["cat.direction_given"]),
])

N("n_thanks", SZ, "助かりました。一緒に探してくれて、ありがとうございます。", "たすかりました。いっしょにさがしてくれて、ありがとうございます。", "Tasukarimashita. Issho ni sagashite kurete, arigatou gozaimasu.",
  "Bạn giúp tôi nhiều lắm. Cảm ơn vì đã cùng tôi tìm kiếm.", "bow", "n_go", "obj_report_cat_clue")
N("n_go", SZ, "今から、向かい側を見に行きます。あなたも、良かったら来てください。", "いまから、むかいがわをみにいきます。あなたも、よかったらきてください。", "Ima kara, mukaigawa wo mi ni ikimasu. Anata mo, yokattara kite kudasai.",
  "Bây giờ tôi sẽ đi xem phía đối diện. Nếu tiện thì bạn cùng đi nhé.", "talk", "n_recap")
N("n_recap", NAR, "今日のポイント：「〜の近くで見ました」「小さくて、白い」「〜の向かい側です」。", "きょうのポイント：「〜のちかくでみました」「ちいさくて、しろい」「〜のむかいがわです」。", "Kyou no pointo: \"~ no chikaku de mimashita\" \"chiisakute, shiroi\" \"~ no mukaigawa desu\".",
  "Tóm tắt: \"~ no chikaku de mimashita\" (đã thấy gần ~), nối tính từ đuôi い bằng 〜くて, \"~ no mukaigawa desu\" (ở phía đối diện ~).", "", "node_complete")
N("node_complete", NAR, "", "", "", "", "", "", type=4)

objectives = [
    ("obj_meet", "鈴木さんに会う", "Gặp lại cô Suzuki", False),
    ("obj_report_cat_clue", "手がかりを伝える", "Báo manh mối cho Suzuki", False),
]
targets = ["grammar.n5.no_chikaku_de", "grammar.n5.te_form_adj", "grammar.n5.no_mukaigawa", "vocab.n5.ribon", "vocab.n5.kouen"]

problems = S.validate("node_find_suzuki", objectives)
print("problems:", problems)
if not problems:
    S.write(sys.argv[1], name="scenario_neighborhood_cat_followup", sid="scenario.neighborhood.cat_followup", title_ja="猫の手がかり", title_vi="Manh mối về chú mèo",
            desc_ja="鈴木さんに新しい手がかりを伝えましょう。", desc_vi="Báo cho cô Suzuki manh mối mới về Mike và luyện cách mô tả vị trí, đặc điểm bằng tiếng Nhật.",
            chapter=3, targets=targets, objectives=objectives, start="node_find_suzuki")
    print("nodes", len(S.nodes))
