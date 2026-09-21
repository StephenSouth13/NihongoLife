# -*- coding: utf-8 -*-
import sys

from scen_lib import Scenario

S = Scenario()
C, N = S.C, S.N
SZ = ("Suzuki", "npc_neighbor_2")
NAR = None

N("node_find_suzuki", None, "", "", "", "", "", "n_open", "obj_meet", type=6, npc="npc_neighbor_2")

# ───────── Suzuki tìm mèo ─────────
N("n_open", SZ, "あの…すみません。私の猫を見ませんでしたか。", "あの…すみません。わたしのねこをみませんでしたか。", "Ano... sumimasen. Watashi no neko wo mimasen deshita ka.",
  "À... xin lỗi. Bạn có thấy con mèo của tôi không?", "talk", choices=[
    C("いいえ、見ませんでした。どんな猫ですか。", "Không, tôi không thấy. Mèo thế nào ạ?", "n_describe", "Grammar", 10, "Trả lời phủ định quá khứ 見ませんでした và hỏi tiếp bằng どんな〜ですか", ["grammar.n5.masen_deshita", "grammar.n5.donna_desu_ka"], ["vocab.n5.neko"], ["cat.asked"]),
    C("見ませんでした。", "Tôi không thấy.", "n_open_short", "ResponseAccuracy", 3, "Trả lời đúng nhưng dừng ở đó, bỏ lỡ cơ hội hỏi giúp đỡ", ["grammar.n5.masen_deshita"], ["vocab.n5.neko"]),
    C("猫は好きです。", "Tôi thích mèo.", "n_open_wrong", "ResponseAccuracy", -8, "Trả lời lạc đề: được hỏi có thấy mèo không, không phải có thích mèo không"),
])
N("n_open_short", SZ, "そうですか…。ミケは、白と茶色と黒の猫です。昨日から、いません。", "そうですか…。ミケは、しろとちゃいろとくろのねこです。きのうから、いません。", "Sou desu ka.... Mike wa, shiro to chairo to kuro no neko desu. Kinou kara, imasen.",
  "Vậy à... Con Mike là mèo ba màu trắng, nâu và đen. Nó mất từ hôm qua rồi.", "talk", "n_ribbon")
N("n_open_wrong", SZ, "あ、ありがとう…。でも、今は、うちの猫がいなくて…。", "あ、ありがとう…。でも、いまは、うちのねこがいなくて…。", "A, arigatou.... Demo, ima wa, uchi no neko ga inakute....",
  "À, cảm ơn... Nhưng bây giờ mèo nhà tôi đi mất rồi...", "talk", "n_open")

N("n_describe", SZ, "ミケです。白と茶色と黒の猫です。", "ミケです。しろとちゃいろとくろのねこです。", "Mike desu. Shiro to chairo to kuro no neko desu.",
  "Tên nó là Mike. Là con mèo ba màu trắng, nâu và đen.", "talk", "n_ribbon")
N("n_ribbon", SZ, "首に、赤いリボンがあります。大きい猫ではありません。小さいです。", "くびに、あかいリボンがあります。おおきいねこではありません。ちいさいです。", "Kubi ni, akai ribon ga arimasu. Ookii neko dewa arimasen. Chiisai desu.",
  "Ở cổ nó có một chiếc nơ đỏ. Nó không phải mèo to. Nó nhỏ thôi.", "point", "n_check_color")

# ───────── Kiểm tra đọc/nghe hiểu ─────────
N("n_check_color", SZ, "分かりましたか。ミケは、何色の猫ですか。", "わかりましたか。ミケは、なにいろのねこですか。", "Wakarimashita ka. Mike wa, nani iro no neko desu ka?",
  "Bạn hiểu chưa? Mike là mèo màu gì nhỉ?", "talk", choices=[
    C("白と茶色と黒の猫です。", "Là mèo màu trắng, nâu và đen ạ.", "n_check_ribbon", "Vocabulary", 10, "Nhắc lại đúng màu lông: 白・茶色・黒", ["grammar.n5.no_neko"], ["vocab.n5.shiro", "vocab.n5.chairo", "vocab.n5.kuro"]),
    C("もう一度お願いします。", "Phiền bạn nói lại một lần nữa ạ.", "n_color_repeat", "ResponseAccuracy", 8, "Xin nghe lại lịch sự khi chưa nghe rõ", [], ["vocab.n5.mou_ichido"]),
    C("白い猫です。", "Là mèo trắng ạ.", "n_color_wrong", "Vocabulary", -8, "Bỏ sót màu nâu và đen: Mike là mèo ba màu, không chỉ có màu trắng"),
])
N("n_color_repeat", SZ, "はい。白、茶色、黒。三つの色です。ミケです。", "はい。しろ、ちゃいろ、くろ。みっつのいろです。ミケです。", "Hai. Shiro, chairo, kuro. Mittsu no iro desu. Mike desu.",
  "Vâng. Trắng, nâu, đen. Ba màu. Đó là Mike.", "point", "n_check_color")
N("n_color_wrong", SZ, "白だけではありませんよ。白と、茶色と、黒です。三つの色です。", "しろだけではありませんよ。しろと、ちゃいろと、くろです。みっつのいろです。", "Shiro dake dewa arimasen yo. Shiro to, chairo to, kuro desu. Mittsu no iro desu.",
  "Không chỉ có màu trắng đâu. Là trắng, nâu và đen. Ba màu.", "point", "n_check_color")

N("n_check_ribbon", SZ, "そうです！ミケの首には、何がありますか。", "そうです！ミケのくびには、なにがありますか。", "Sou desu! Mike no kubi ni wa, nani ga arimasu ka?",
  "Đúng rồi! Ở cổ Mike có gì nào?", "talk", "", "obj_describe", choices=[
    C("赤いリボンがあります。", "Có một chiếc nơ đỏ ạ.", "n_when", "Grammar", 10, "Mô tả vật ở đâu bằng 〜に〜があります (cổ có nơ đỏ)", ["grammar.n5.ni_ga_arimasu"], ["vocab.n5.ribon", "vocab.n5.akai"], ["cat.described"]),
    C("青い鈴があります。", "Có một chiếc chuông xanh ạ.", "n_ribbon_wrong", "Vocabulary", -8, "Nhớ sai chi tiết: Suzuki nói 赤いリボン (nơ đỏ), không phải chuông xanh"),
    C("もう一度お願いします。", "Phiền bạn nói lại một lần nữa ạ.", "n_ribbon_repeat", "ResponseAccuracy", 8, "Xin nghe lại lịch sự", [], ["vocab.n5.mou_ichido"]),
])
N("n_ribbon_wrong", SZ, "ちがいますよ。赤い、リボンです。首に、赤いリボンがあります。", "ちがいますよ。あかい、リボンです。くびに、あかいリボンがあります。", "Chigaimasu yo. Akai, ribon desu. Kubi ni, akai ribon ga arimasu.",
  "Không phải đâu. Là nơ đỏ. Ở cổ có một chiếc nơ đỏ.", "point", "n_check_ribbon")
N("n_ribbon_repeat", SZ, "はい。首に、赤い、リボン。きのう、私がつけました。", "はい。くびに、あかい、リボン。きのう、わたしがつけました。", "Hai. Kubi ni, akai, ribon. Kinou, watashi ga tsukemashita.",
  "Vâng. Ở cổ, một chiếc nơ đỏ. Hôm qua chính tôi đã buộc cho nó.", "point", "n_check_ribbon")

# ───────── Hỏi thêm: từ khi nào, hay đi đâu ─────────
N("n_when", SZ, "昨日の夜、窓を開けました。それから、ミケがいません。", "きのうのよる、まどをあけました。それから、ミケがいません。", "Kinou no yoru, mado wo akemashita. Sorekara, Mike ga imasen.",
  "Tối qua tôi mở cửa sổ. Từ đó, Mike không thấy đâu nữa.", "talk", choices=[
    C("いつからいませんか。", "Nó mất từ khi nào ạ?", "n_when_answer", "Grammar", 10, "Hỏi thời điểm bằng いつから để nắm rõ tình hình", ["grammar.n5.itsu_kara"], ["vocab.n5.itsu"]),
    C("いつですか。", "Khi nào ạ?", "n_when_answer", "ResponseAccuracy", 3, "Hỏi được nhưng thiếu から nên không rõ hỏi 'từ khi nào', dễ gây hiểu nhầm", [], ["vocab.n5.itsu"]),
    C("どこですか。", "Ở đâu ạ?", "n_when_wrong", "Grammar", -8, "Dùng どこ (ở đâu) để hỏi thời gian; phải dùng いつ (khi nào)"),
])
N("n_when_wrong", SZ, "「どこ」は場所です。時間は、「いつ」ですよ。", "「どこ」はばしょです。じかんは、「いつ」ですよ。", "\"Doko\" wa basho desu. Jikan wa, \"itsu\" desu yo.",
  "\"Doko\" là hỏi nơi chốn. Hỏi thời gian thì dùng \"itsu\" nhé.", "point", "n_when")
N("n_when_answer", SZ, "昨日の夜、八時ごろからです。もう、二十時間です。", "きのうのよる、はちじごろからです。もう、にじゅうじかんです。", "Kinou no yoru, hachiji goro kara desu. Mou, nijuu jikan desu.",
  "Từ khoảng tám giờ tối qua. Đã hai mươi tiếng rồi.", "talk", "n_place")
N("n_place", SZ, "ミケは、よく神社の近くに行きます。公園にも行きます。", "ミケは、よくじんじゃのちかくにいきます。こうえんにもいきます。", "Mike wa, yoku jinja no chikaku ni ikimasu. Kouen ni mo ikimasu.",
  "Mike hay đi đến gần đền thờ. Nó cũng hay ra công viên.", "point", choices=[
    C("神社の近くですね。分かりました。", "Gần đền thờ nhỉ. Tôi hiểu rồi ạ.", "n_help", "Vocabulary", 10, "Xác nhận thông tin bằng cách nhắc lại địa điểm: 神社の近く", ["grammar.n5.desu_ne"], ["vocab.n5.jinja", "vocab.n5.chikaku"], ["cat.shrine_hint"]),
    C("公園ですね。分かりました。", "Công viên nhỉ. Tôi hiểu rồi ạ.", "n_help", "Vocabulary", 8, "Xác nhận thông tin bằng cách nhắc lại địa điểm: 公園", ["grammar.n5.desu_ne"], ["vocab.n5.kouen"], ["cat.park_hint"]),
    C("神社は嫌いです。", "Tôi không thích đền thờ.", "n_place_wrong", "ResponseAccuracy", -8, "Nói điều không liên quan và làm người đang lo lắng thêm bối rối"),
])
N("n_place_wrong", SZ, "あ…そうですか。でも、ミケは神社が好きなんです。もう一度、聞いてください。", "あ…そうですか。でも、ミケはじんじゃがすきなんです。もういちど、きいてください。", "A... sou desu ka. Demo, Mike wa jinja ga suki nan desu. Mou ichido, kiite kudasai.",
  "À... vậy à. Nhưng Mike lại thích đền thờ lắm. Bạn nghe lại giúp tôi nhé.", "talk", "n_place")

# ───────── Đề nghị giúp đỡ ─────────
N("n_help", SZ, "ああ、どうしよう…。ミケが、さむくないでしょうか。", "ああ、どうしよう…。ミケが、さむくないでしょうか。", "Aa, dou shiyou.... Mike ga, samukunai deshou ka.",
  "Ôi, biết làm sao đây... Không biết Mike có bị lạnh không nữa.", "talk", choices=[
    C("私も探しましょうか。", "Để tôi cũng đi tìm giúp nhé?", "n_help_yes", "Grammar", 10, "Đề nghị giúp đỡ bằng mẫu 〜ましょうか", ["grammar.n5.mashou_ka"], ["vocab.n5.sagasu"], ["cat.offered_help"]),
    C("がんばってください。", "Cố lên nhé.", "n_help_cheer", "ResponseAccuracy", 3, "Động viên được, nhưng chưa đề nghị giúp trong khi bạn có thể giúp", ["grammar.n5.te_kudasai"], []),
    C("猫は自分で帰りますよ。", "Mèo sẽ tự về thôi.", "n_help_wrong", "ResponseAccuracy", -8, "Nói nhẹ nhõm vô tâm với người đang rất lo, dễ bị coi là thiếu tế nhị"),
])
N("n_help_cheer", SZ, "ありがとう…。でも、私一人では、ちょっと…。", "ありがとう…。でも、わたしひとりでは、ちょっと…。", "Arigatou.... Demo, watashi hitori de wa, chotto....",
  "Cảm ơn bạn... Nhưng một mình tôi thì hơi... khó.", "talk", "n_help")
N("n_help_wrong", SZ, "そう…でしょうか。でも、私は、心配です。", "そう…でしょうか。でも、わたしは、しんぱいです。", "Sou... deshou ka. Demo, watashi wa, shinpai desu.",
  "Vậy... sao. Nhưng tôi thì rất lo.", "talk", "n_help")
N("n_help_yes", SZ, "本当ですか！ありがとうございます、ありがとうございます！", "ほんとうですか！ありがとうございます、ありがとうございます！", "Hontou desu ka! Arigatou gozaimasu, arigatou gozaimasu!",
  "Thật sao! Cảm ơn bạn, cảm ơn bạn nhiều lắm!", "bow", "n_promise")

N("n_promise", SZ, "ミケを見たら、教えてください。私の家は、あの青い屋根の家です。", "ミケをみたら、おしえてください。わたしのいえは、あのあおいやねのいえです。", "Mike wo mitara, oshiete kudasai. Watashi no ie wa, ano aoi yane no ie desu.",
  "Nếu bạn thấy Mike, xin hãy báo cho tôi. Nhà tôi là căn có mái xanh kia.", "point", "", "obj_help", choices=[
    C("はい、見たら、すぐ教えます。", "Vâng, nếu thấy tôi sẽ báo ngay ạ.", "n_bye", "Grammar", 10, "Hứa hẹn bằng mẫu 〜たら (nếu ... thì) và 教えます", ["grammar.n5.tara"], ["vocab.n5.oshieru"], ["cat.promised"]),
    C("はい、たぶん。", "Vâng, chắc vậy.", "n_bye_weak", "ResponseAccuracy", 2, "Trả lời mơ hồ, nghe thiếu chắc chắn khi người ta đang cần giúp", [], []),
    C("いいえ、忙しいです。", "Không, tôi bận.", "n_promise_wrong", "ResponseAccuracy", -8, "Từ chối phũ phàng khi vừa nói sẽ giúp tìm mèo"),
])
N("n_bye_weak", SZ, "はい…。お願いします。", "はい…。おねがいします。", "Hai.... Onegaishimasu.",
  "Vâng... Nhờ bạn nhé.", "talk", "n_recap")
N("n_promise_wrong", SZ, "え…さっき、探しましょうかと言いましたよね…。", "え…さっき、さがしましょうかといいましたよね…。", "E... sakki, sagashimashou ka to iimashita yo ne....",
  "Hả... lúc nãy bạn còn nói \"để tôi tìm giúp\" mà...", "talk", "n_promise")
N("n_bye", SZ, "ありがとうございます。ミケも、きっとうれしいです。", "ありがとうございます。ミケも、きっとうれしいです。", "Arigatou gozaimasu. Mike mo, kitto ureshii desu.",
  "Cảm ơn bạn. Chắc chắn Mike cũng sẽ rất vui.", "bow", "n_recap")

N("n_recap", NAR, "今日のポイント：「どんな〜ですか」「〜ませんでした」「〜ましょうか」。", "きょうのポイント：「どんな〜ですか」「〜ませんでした」「〜ましょうか」。", "Kyou no pointo: \"donna ~ desu ka\" \"~ masen deshita\" \"~ mashou ka\".",
  "Tóm tắt: \"donna ~ desu ka\" (~ như thế nào?), \"~ masen deshita\" (đã không ~), \"~ mashou ka\" (để tôi ~ nhé?). Hãy để ý đến ngôi đền và công viên khi đi dạo!", "", "n_complete")
N("n_complete", NAR, "", "", "", "", "", "", type=4)

objectives = [
    ("obj_meet", "鈴木さんに会う", "Gặp cô Suzuki", False),
    ("obj_describe", "ミケのようすを聞き取る", "Nghe hiểu đặc điểm của Mike", False),
    ("obj_help", "手伝うと言う", "Nhận lời giúp tìm mèo", False),
]
targets = ["grammar.n5.donna_desu_ka", "grammar.n5.masen_deshita", "grammar.n5.mashou_ka", "vocab.n5.neko", "vocab.n5.jinja"]

problems = S.validate("node_find_suzuki", objectives)
print("problems:", problems)
if not problems:
    S.write(sys.argv[1], name="scenario_house2_lostcat", sid="scenario.house2.lostcat", title_ja="鈴木さんの猫を探す", title_vi="Giúp cô Suzuki tìm mèo",
            desc_ja="鈴木さんの猫が、昨日からいません。話を聞いて、手伝いましょう。", desc_vi="Con mèo Mike của cô Suzuki mất từ hôm qua. Hỏi thăm, nghe tả đặc điểm và nhận lời giúp.",
            chapter=3, targets=targets, objectives=objectives, start="node_find_suzuki")
    print("nodes", len(S.nodes))
