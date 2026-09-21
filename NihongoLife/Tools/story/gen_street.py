# -*- coding: utf-8 -*-
import sys

from scen_lib import Scenario

S = Scenario()
C, N = S.C, S.N
T = ("Tanaka", "npc_neighbor_1")

N("node_find_neighbor", None, "", "", "", "", "", "n_greet", "obj_talk_to_neighbor", type=6, npc="npc_neighbor_1")

# ───────── Chào buổi sáng ─────────
N("n_greet", T, "おはようございます！いい朝ですね。", "おはようございます！いいあさですね。", "Ohayou gozaimasu! Ii asa desu ne.",
  "Chào buổi sáng! Sáng nay đẹp trời nhỉ.", "talk", choices=[
    C("おはようございます。", "Chào buổi sáng ạ.", "n_weather", "Vocabulary", 10, "Chào đúng buổi sáng bằng cách nói lịch sự おはようございます", [], ["vocab.n5.ohayou_gozaimasu"]),
    C("おはよう。", "Chào.", "n_greet_casual", "ResponseAccuracy", 2, "Đúng buổi nhưng おはよう là cách nói thân mật, chưa hợp với hàng xóm", [], ["vocab.n5.ohayou_gozaimasu"]),
    C("こんばんは。", "Chào buổi tối.", "n_greet_wrong", "Vocabulary", -8, "Chào sai buổi: こんばんは dùng vào buổi tối, lúc này là buổi sáng"),
])
N("n_greet_casual", T, "ふふ、「おはよう」は友達に言いますね。近所の人には「おはようございます」ですよ。", "ふふ、「おはよう」はともだちにいいますね。きんじょのひとには「おはようございます」ですよ。", "Fufu, \"ohayou\" wa tomodachi ni iimasu ne. Kinjo no hito ni wa \"ohayou gozaimasu\" desu yo.",
  "Hì, \"ohayou\" là để nói với bạn bè. Với hàng xóm thì nói \"ohayou gozaimasu\" nhé.", "talk", "n_weather")
N("n_greet_wrong", T, "「こんばんは」は夜のあいさつですよ。今は朝ですから、もう一度どうぞ。", "「こんばんは」はよるのあいさつですよ。いまはあさですから、もういちどどうぞ。", "\"Konbanwa\" wa yoru no aisatsu desu yo. Ima wa asa desu kara, mou ichido douzo.",
  "\"Konbanwa\" là lời chào buổi tối đấy. Bây giờ là buổi sáng nên bạn thử lại nhé.", "point", "n_greet")

# ───────── Nói chuyện về thời tiết ─────────
N("n_weather", T, "今日は天気がいいですね。", "きょうはてんきがいいですね。", "Kyou wa tenki ga ii desu ne.",
  "Hôm nay thời tiết đẹp nhỉ.", "talk", choices=[
    C("そうですね。いい天気ですね。", "Đúng thế ạ. Thời tiết đẹp thật.", "n_where", "Grammar", 10, "Đồng ý tự nhiên bằng そうですね rồi nhắc lại nhận xét", ["grammar.n5.sou_desu_ne"], ["vocab.n5.tenki"]),
    C("はい、天気です。", "Vâng, là thời tiết.", "n_weather_flat", "ResponseAccuracy", 2, "Trả lời được nhưng nghe như đọc lại từ, không tự nhiên (nên dùng そうですね)", [], ["vocab.n5.tenki"]),
    C("いいえ、雨です。", "Không, trời mưa.", "n_weather_wrong", "ResponseAccuracy", -8, "Trả lời trái với thực tế: trời đang đẹp, không mưa"),
])
N("n_weather_flat", T, "そうですね。「そうですね」と言うと、自然ですよ。", "そうですね。「そうですね」というと、しぜんですよ。", "Sou desu ne. \"Sou desu ne\" to iu to, shizen desu yo.",
  "Đúng vậy. Nói \"sou desu ne\" (đúng thế nhỉ) sẽ tự nhiên hơn đấy.", "talk", "n_where")
N("n_weather_wrong", T, "え？雨は降っていませんよ。空を見てください。", "え？あめはふっていませんよ。そらをみてください。", "E? Ame wa futte imasen yo. Sora wo mite kudasai.",
  "Hả? Trời đâu có mưa. Bạn nhìn lên trời xem.", "point", "n_weather")

# ───────── Nói đi đâu (〜へ行きます) ─────────
N("n_where", T, "どこへ行きますか。", "どこへいきますか。", "Doko e ikimasu ka?",
  "Bạn đi đâu đấy?", "talk", obj="obj_greet", choices=[
    C("ひばりコンビニへ行きます。", "Tôi đi đến cửa hàng tiện lợi Hibari.", "n_konbini_nice", "Grammar", 10, "Dùng đúng mẫu 〜へ行きます để nói nơi mình đi đến", ["grammar.n5.e_ikimasu"], ["vocab.n5.konbini"]),
    C("コンビニです。", "Cửa hàng tiện lợi ạ.", "n_where_short", "ResponseAccuracy", 3, "Hiểu ý nhưng chỉ nói tên nơi chốn, chưa thành câu hoàn chỉnh", [], ["vocab.n5.konbini"]),
    C("コンビニを行きます。", "Tôi đi cửa hàng tiện lợi.", "n_where_wrong", "Grammar", -8, "Dùng trợ từ を sai: nơi đến của động từ 行きます phải đi với へ (hoặc に)"),
])
N("n_where_short", T, "コンビニですか。「コンビニへ行きます」と言うと、分かりやすいですよ。", "コンビニですか。「コンビニへいきます」というと、わかりやすいですよ。", "Konbini desu ka. \"Konbini e ikimasu\" to iu to, wakariyasui desu yo.",
  "Cửa hàng tiện lợi à. Nói \"konbini e ikimasu\" thì dễ hiểu hơn đấy.", "talk", "n_konbini_nice")
N("n_where_wrong", T, "「を」ではなくて、「へ」ですよ。「コンビニへ行きます」。もう一度どうぞ。", "「を」ではなくて、「へ」ですよ。「コンビニへいきます」。もういちどどうぞ。", "\"Wo\" dewa nakute, \"e\" desu yo. \"Konbini e ikimasu\". Mou ichido douzo.",
  "Không phải \"wo\" mà là \"e\" nhé. \"Konbini e ikimasu\". Bạn nói lại nhé.", "point", "n_where")
N("n_konbini_nice", T, "ひばりコンビニですね。でも、この辺ははじめてですよね？", "ひばりコンビニですね。でも、このへんははじめてですよね？", "Hibari konbini desu ne. Demo, kono hen wa hajimete desu yo ne?",
  "Cửa hàng Hibari nhỉ. Nhưng khu này bạn mới đến lần đầu đúng không?", "talk", choices=[
    C("はい、はじめてです。コンビニはどこですか。", "Vâng, lần đầu ạ. Cửa hàng tiện lợi ở đâu ạ?", "n_dir_a", "Grammar", 10, "Hỏi vị trí đúng mẫu 〜はどこですか", ["grammar.n5.wa_doko_desu_ka"], ["vocab.n5.konbini", "vocab.n5.hajimete"]),
    C("コンビニ、どこ？", "Cửa hàng tiện lợi, ở đâu?", "n_ask_casual", "ResponseAccuracy", 2, "Hiểu được nhưng quá cộc lốc với người lớn tuổi hơn, thiếu ですか", ["grammar.n5.wa_doko_desu_ka"], ["vocab.n5.konbini"]),
    C("コンビニですか。", "Là cửa hàng tiện lợi ạ?", "n_ask_wrong", "Grammar", -8, "Câu này chỉ hỏi lại, không hỏi vị trí (muốn hỏi chỗ phải dùng 〜はどこですか)"),
])
N("n_ask_casual", T, "ふふ。「コンビニはどこですか」と言いましょう。ていねいですよ。", "ふふ。「コンビニはどこですか」といいましょう。ていねいですよ。", "Fufu. \"Konbini wa doko desu ka\" to iimashou. Teinei desu yo.",
  "Hì. Mình nói \"konbini wa doko desu ka\" nhé, như vậy lịch sự hơn.", "talk", "n_dir_a")
N("n_ask_wrong", T, "え？私が聞いていますよ。場所を聞くときは、「〜はどこですか」です。もう一度どうぞ。", "え？わたしがきいていますよ。ばしょをきくときは、「〜はどこですか」です。もういちどどうぞ。", "E? Watashi ga kiite imasu yo. Basho wo kiku toki wa, \"~ wa doko desu ka\" desu. Mou ichido douzo.",
  "Hả? Tôi đang hỏi bạn mà. Khi hỏi chỗ thì nói \"~ wa doko desu ka\". Bạn thử lại nhé.", "point", "n_konbini_nice")

# ───────── Chỉ đường ─────────
N("n_dir_a", T, "この道を、まっすぐ行ってください。", "このみちを、まっすぐいってください。", "Kono michi wo, massugu itte kudasai.",
  "Bạn cứ đi thẳng con đường này nhé.", "point", "n_dir_b")
N("n_dir_b", T, "そして、次の角を右です。赤い屋根の店ですよ。", "そして、つぎのかどをみぎです。あかいやねのみせですよ。", "Soshite, tsugi no kado wo migi desu. Akai yane no mise desu yo.",
  "Sau đó, đến góc tiếp theo thì rẽ phải. Đó là cửa hàng có mái nhà màu đỏ.", "point", "n_dir_check")
N("n_dir_check", T, "分かりましたか。ひばりコンビニは、どこですか。", "わかりましたか。ひばりコンビニは、どこですか。", "Wakarimashita ka. Hibari konbini wa, doko desu ka?",
  "Bạn hiểu chưa? Cửa hàng Hibari ở đâu nào?", "talk", choices=[
    C("まっすぐ行って、右ですね。", "Đi thẳng rồi rẽ phải, đúng không ạ.", "n_dir_right", "Vocabulary", 10, "Nhắc lại đúng chỉ đường: まっすぐ (đi thẳng) và 右 (phải)", ["grammar.n5.wa_doko_desu_ka"], ["vocab.n5.massugu", "vocab.n5.migi"]),
    C("すみません、もう一度お願いします。", "Xin lỗi, phiền bạn nói lại một lần nữa ạ.", "n_dir_repeat", "ResponseAccuracy", 8, "Biết xin nghe lại lịch sự khi chưa hiểu rõ", [], ["vocab.n5.sumimasen"]),
    C("分かりません。", "Tôi không hiểu.", "n_dir_repeat", "ResponseAccuracy", 2, "Nói thật là chưa hiểu nhưng thiếu すみません và もう一度 nên hơi cộc", [], ["vocab.n5.wakarimasen"]),
    C("まっすぐ行って、左ですね。", "Đi thẳng rồi rẽ trái, đúng không ạ.", "n_dir_wrong", "Vocabulary", -8, "Nhầm hướng: 左 (trái) trong khi chỉ đường là 右 (phải)", [], ["vocab.n5.hidari"]),
])
N("n_dir_repeat", T, "はい。ゆっくり言いますね。まっすぐ。…そして、右です。", "はい。ゆっくりいいますね。まっすぐ。…そして、みぎです。", "Hai. Yukkuri iimasu ne. Massugu. ...Soshite, migi desu.",
  "Vâng. Tôi nói chậm nhé. Đi thẳng... rồi rẽ phải.", "point", "n_dir_check")
N("n_dir_wrong", T, "左ではありませんよ。右です。お箸を持つ手のほうですね。", "ひだりではありませんよ。みぎです。おはしをもつてのほうですね。", "Hidari dewa arimasen yo. Migi desu. Ohashi wo motsu te no hou desu ne.",
  "Không phải bên trái đâu. Là bên phải nhé, phía tay cầm đũa.", "point", "n_dir_check")
N("n_dir_right", T, "そうです！よくできました。", "そうです！よくできました。", "Sou desu! Yoku dekimashita.",
  "Đúng rồi! Bạn làm tốt lắm.", "bow", "n_thanks", "obj_ask_way")

# ───────── Cảm ơn và chào tạm biệt ─────────
N("n_thanks", T, "ひばりコンビニは、おにぎりがおいしいですよ。", "ひばりコンビニは、おにぎりがおいしいですよ。", "Hibari konbini wa, onigiri ga oishii desu yo.",
  "Cửa hàng Hibari có onigiri rất ngon đấy.", "talk", choices=[
    C("ありがとうございます。", "Cảm ơn bạn ạ.", "n_farewell", "Vocabulary", 10, "Cảm ơn đầy đủ và lịch sự sau khi được chỉ đường", [], ["vocab.n5.arigatou_gozaimasu"]),
    C("どうも。", "Cảm ơn.", "n_thanks_casual", "ResponseAccuracy", 3, "Cảm ơn ngắn gọn, với hàng xóm nên nói ありがとうございます", [], ["vocab.n5.arigatou_gozaimasu"]),
    C("さようなら。", "Tạm biệt.", "n_thanks_wrong", "ResponseAccuracy", -8, "Chào tạm biệt mà quên cảm ơn; さようなら còn nghe như chia tay lâu dài"),
])
N("n_thanks_casual", T, "いえいえ。近所の人には「ありがとうございます」がていねいですよ。", "いえいえ。きんじょのひとには「ありがとうございます」がていねいですよ。", "Ie ie. Kinjo no hito ni wa \"arigatou gozaimasu\" ga teinei desu yo.",
  "Không có gì. Với hàng xóm thì \"arigatou gozaimasu\" sẽ lịch sự hơn.", "talk", "n_farewell")
N("n_thanks_wrong", T, "「さようなら」はちょっとさびしいですよ。まず「ありがとうございます」ですね。", "「さようなら」はちょっとさびしいですよ。まず「ありがとうございます」ですね。", "\"Sayounara\" wa chotto sabishii desu yo. Mazu \"arigatou gozaimasu\" desu ne.",
  "\"Sayounara\" nghe hơi buồn đấy. Trước hết hãy nói \"arigatou gozaimasu\" nhé.", "point", "n_thanks")
N("n_farewell", T, "気をつけて。行ってらっしゃい。", "きをつけて。いってらっしゃい。", "Ki wo tsukete. Itterasshai.",
  "Đi cẩn thận nhé. Chúc bạn đi vui vẻ.", "talk", choices=[
    C("失礼します。", "Tôi xin phép đi ạ.", "n_practice", "Vocabulary", 10, "Chào tạm biệt lịch sự khi rời đi bằng 失礼します", [], ["vocab.n5.shitsurei_shimasu"]),
    C("じゃあね。", "Hẹn gặp lại nhé.", "n_farewell_casual", "ResponseAccuracy", 2, "Cách nói thân mật với bạn bè, chưa hợp với hàng xóm lớn tuổi hơn", [], ["vocab.n5.jaa_ne"]),
    C("こんにちは。", "Xin chào.", "n_farewell_wrong", "Vocabulary", -8, "Chào hỏi khi đang rời đi: こんにちは là lời chào gặp mặt, không phải lời tạm biệt"),
])
N("n_farewell_casual", T, "ふふ、「じゃあね」は友達に言いますね。「失礼します」がていねいですよ。", "ふふ、「じゃあね」はともだちにいいますね。「しつれいします」がていねいですよ。", "Fufu, \"jaa ne\" wa tomodachi ni iimasu ne. \"Shitsurei shimasu\" ga teinei desu yo.",
  "Hì, \"jaa ne\" là nói với bạn bè. \"Shitsurei shimasu\" mới lịch sự.", "talk", "n_practice")
N("n_farewell_wrong", T, "「こんにちは」は会ったときのあいさつですよ。別れるときは、「失礼します」です。", "「こんにちは」はあったときのあいさつですよ。わかれるときは、「しつれいします」です。", "\"Konnichiwa\" wa atta toki no aisatsu desu yo. Wakareru toki wa, \"shitsurei shimasu\" desu.",
  "\"Konnichiwa\" là lời chào khi gặp mặt. Khi chia tay thì nói \"shitsurei shimasu\".", "point", "n_farewell")

# ───────── Luyện nói + tổng kết ─────────
N("n_practice", None, "ひばりコンビニはどこですか。", "ひばりコンビニはどこですか。", "Hibari konbini wa doko desu ka.",
  "Luyện nói (không bắt buộc): nhấn V rồi nói to câu này để luyện phát âm.", "", "n_recap")
N("n_recap", None, "今日のポイント：「〜へ行きます」と「〜はどこですか」。", "きょうのポイント：「〜へいきます」と「〜はどこですか」。", "Kyou no pointo: \"~ e ikimasu\" to \"~ wa doko desu ka\".",
  "Tóm tắt: \"~ e ikimasu\" (đi đến ~) và \"~ wa doko desu ka\" (~ ở đâu?). Từ chỉ đường: massugu (thẳng), migi (phải), hidari (trái). Bây giờ hãy đến cửa hàng Hibari nhé!", "", "n_complete", "obj_thank")
N("n_complete", None, "", "", "", "", "", "", type=4)

objectives = [
    ("obj_talk_to_neighbor", "通りの人に話しかける", "Bắt chuyện với người hàng xóm trên phố", False),
    ("obj_greet", "朝のあいさつと世間話をする", "Chào buổi sáng và trò chuyện về thời tiết", False),
    ("obj_ask_way", "道をたずねて、聞き取る", "Hỏi đường và nghe hiểu chỉ dẫn", False),
    ("obj_thank", "お礼を言って別れる", "Cảm ơn và chào tạm biệt", False),
    ("obj_practice_voice", "声に出して練習する", "Luyện nói bằng micro (không bắt buộc)", True),
]
targets = ["grammar.n5.e_ikimasu", "grammar.n5.wa_doko_desu_ka", "vocab.n5.ohayou_gozaimasu", "vocab.n5.massugu", "vocab.n5.migi", "vocab.n5.hidari"]

problems = S.validate("node_find_neighbor", objectives)
print("problems:", problems)
if not problems:
    S.write(sys.argv[1], name="scenario_street_first_talk", sid="scenario.street.first_talk", title_ja="通りであいさつ", title_vi="Chào hỏi và hỏi đường trên phố",
            desc_ja="朝のあいさつをして、ひばりコンビニへの道を聞きましょう。", desc_vi="Chào buổi sáng, nói mình đi đâu và hỏi đường đến cửa hàng tiện lợi Hibari.",
            chapter=1, targets=targets, objectives=objectives, start="node_find_neighbor")
    print("nodes", len(S.nodes))
