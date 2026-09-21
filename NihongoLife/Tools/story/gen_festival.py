# -*- coding: utf-8 -*-
import sys

from scen_lib import Scenario

S = Scenario()
C, N = S.C, S.N
SA = ("Sato", "npc_neighbor_3")
TA = ("Tanaka", "npc_neighbor_1")
SZ = ("Suzuki", "npc_neighbor_2")
YA = ("Yamada", "npc_ramen_owner")
MO = ("Morita", "npc_teacher_morita")
KI = ("Kim", "npc_classmate_kim")
NAR = None

# ───────── Mở màn ─────────
N("n_arrive", NAR, "夏祭りの夜。ひばり神社は、ちょうちんの光でいっぱいです。", "なつまつりのよる。ひばりじんじゃは、ちょうちんのひかりでいっぱいです。", "Natsu matsuri no yoru. Hibari jinja wa, chouchin no hikari de ippai desu.",
  "Đêm lễ hội mùa hè. Đền Hibari ngập tràn ánh sáng của những chiếc đèn lồng.", "", "n_sato_open", "obj_arrive")
N("n_sato_open", SA, "皆さん、こんばんは。今年も、夏祭りが始まります。今日は、新しい仲間もいますよ。", "みなさん、こんばんは。ことしも、なつまつりがはじまります。きょうは、あたらしいなかまもいますよ。", "Minasan, konbanwa. Kotoshi mo, natsu matsuri ga hajimarimasu. Kyou wa, atarashii nakama mo imasu yo.",
  "Chào mọi người. Năm nay lễ hội mùa hè lại bắt đầu. Hôm nay cũng có một người bạn mới nữa đấy.", "talk", "n_hub")

# ───────── Trung tâm: chọn nói chuyện với ai ─────────
N("n_hub", NAR, "お祭りには、知っている人がたくさんいます。だれと話しますか。", "おまつりには、しっているひとがたくさんいます。だれとはなしますか。", "Omatsuri ni wa, shitte iru hito ga takusan imasu. Dare to hanashimasu ka?",
  "Ở lễ hội có rất nhiều người bạn quen. Bạn muốn nói chuyện với ai?", "", choices=[
    C("田中さんと話します。", "Nói chuyện với bác Tanaka.", "t_start"),
    C("鈴木さんと話します。", "Nói chuyện với cô Suzuki.", "b_s1"),
    C("山田さんの屋台へ行きます。", "Đến quầy hàng của chú Yamada.", "b_y"),
    C("森田先生とキムさんに会います。", "Gặp cô Morita và bạn Kim.", "b_m"),
])
# (the four choices above carry no score; they only pick the conversation)
S.B("b_hub", "fest.met_tanaka,fest.met_suzuki,fest.met_yamada,fest.met_school", "f_start", "n_hub")

# ───────── Tanaka ─────────
N("t_start", TA, "やあ、来ましたね。今日は、ゆかたを着ましたよ。", "やあ、きましたね。きょうは、ゆかたをきましたよ。", "Yaa, kimashita ne. Kyou wa, yukata wo kimashita yo.",
  "Chào, bạn đến rồi nhỉ. Hôm nay tôi mặc yukata đấy.", "talk", choices=[
    C("ゆかた、似合いますね。", "Bác mặc yukata hợp quá ạ.", "b_t1", "Vocabulary", 10, "Khen trang phục bằng 似合いますね, cách khen tự nhiên với người quen", ["grammar.n5.desu_ne"], ["vocab.n5.yukata", "vocab.n5.niau"]),
    C("ゆかたですね。", "Là yukata nhỉ.", "b_t1", "ResponseAccuracy", 3, "Nhận xét đúng nhưng chưa thành lời khen", [], ["vocab.n5.yukata"]),
    C("ゆかたは暑いです。", "Yukata nóng lắm.", "t_wrong1", "ResponseAccuracy", -6, "Chê trang phục của người vừa khoe, thiếu tế nhị"),
])
N("t_wrong1", TA, "あはは、夏ですから、少し暑いですね。でも、ゆかたは、いいですよ。", "あはは、なつですから、すこしあついですね。でも、ゆかたは、いいですよ。", "Ahaha, natsu desu kara, sukoshi atsui desu ne. Demo, yukata wa, ii desu yo.",
  "Haha, mùa hè nên cũng hơi nóng thật. Nhưng yukata đẹp lắm đấy.", "talk", "t_start")
S.B("b_t1", "tanaka.friend", "t_friend", "t_plain")
N("t_friend", TA, "私たちは、もう友達ですね。今日は、みんなに「私の友達です」と紹介しますよ。", "わたしたちは、もうともだちですね。きょうは、みんなに「わたしのともだちです」としょうかいしますよ。", "Watashitachi wa, mou tomodachi desu ne. Kyou wa, minna ni \"watashi no tomodachi desu\" to shoukai shimasu yo.",
  "Chúng ta đã là bạn rồi nhỉ. Hôm nay tôi sẽ giới thiệu bạn với mọi người là \"bạn của tôi\".", "bow", "t_ask")
N("t_plain", TA, "この町の人は、みんな、あなたを知っていますよ。", "このまちのひとは、みんな、あなたをしっていますよ。", "Kono machi no hito wa, minna, anata wo shitte imasu yo.",
  "Mọi người trong khu phố này ai cũng biết bạn rồi đấy.", "talk", "t_ask")
N("t_ask", TA, "この町は、どうですか。", "このまちは、どうですか。", "Kono machi wa, dou desu ka?",
  "Bạn thấy khu phố này thế nào?", "talk", choices=[
    C("この町が好きです。人がやさしいです。", "Cháu thích khu phố này. Mọi người rất tử tế ạ.", "t_end", "Grammar", 10, "Nói cảm nghĩ bằng 〜が好きです và mô tả bằng tính từ đuôi い: やさしいです", ["grammar.n5.ga_suki_desu"], ["vocab.n5.suki", "vocab.n5.yasashii"], ["fest.met_tanaka", "fest.said_love_town"]),
    C("まだ、ちょっと不安です。でも、がんばります。", "Cháu vẫn hơi lo. Nhưng cháu sẽ cố gắng ạ.", "t_end_honest", "ResponseAccuracy", 8, "Nói thật lòng nhưng vẫn tích cực bằng でも、がんばります", [], ["vocab.n5.fuan", "vocab.n5.ganbaru"], ["fest.met_tanaka"]),
    C("つまらないです。", "Chán lắm ạ.", "t_wrong2", "ResponseAccuracy", -8, "Nói tiêu cực giữa lễ hội, thiếu phù hợp với hoàn cảnh và người nghe"),
])
N("t_wrong2", TA, "えっ！…ふふ、お祭りは、これからですよ。もう一度、聞きますね。", "えっ！…ふふ、おまつりは、これからですよ。もういちど、ききますね。", "Ee! ...Fufu, omatsuri wa, korekara desu yo. Mou ichido, kikimasu ne.",
  "Hả! ...Hì, lễ hội mới bắt đầu thôi mà. Tôi hỏi lại nhé.", "talk", "t_ask")
N("t_end", TA, "うれしいですね。この町も、あなたが好きですよ。", "うれしいですね。このまちも、あなたがすきですよ。", "Ureshii desu ne. Kono machi mo, anata ga suki desu yo.",
  "Vui quá nhỉ. Khu phố này cũng quý mến bạn đấy.", "bow", "b_hub", "obj_tanaka")
N("t_end_honest", TA, "だいじょうぶ。この町には、友達がたくさんいますよ。", "だいじょうぶ。このまちには、ともだちがたくさんいますよ。", "Daijoubu. Kono machi ni wa, tomodachi ga takusan imasu yo.",
  "Không sao đâu. Khu phố này có rất nhiều bạn bè đấy.", "bow", "b_hub", "obj_tanaka")

# ───────── Suzuki (nhớ chuyện con mèo) ─────────
S.B("b_s1", "cat.shrine_hint,cat.promised", "s_found", "b_s2")
S.B("b_s2", "cat.described", "s_returned_asked", "s_returned")
N("s_found", SZ, "ミケが、見つかりました！神社の近くにいました。あなたが教えてくれた場所です！", "ミケが、みつかりました！じんじゃのちかくにいました。あなたがおしえてくれたばしょです！", "Mike ga, mitsukarimashita! Jinja no chikaku ni imashita. Anata ga oshiete kureta basho desu!",
  "Tìm thấy Mike rồi! Nó ở gần đền thờ. Chính là chỗ mà bạn đã nhắc cho tôi đấy!", "bow", "s_react")
N("s_returned_asked", SZ, "ミケは、自分で帰ってきました。心配してくれて、ありがとうございました。", "ミケは、じぶんでかえってきました。しんぱいしてくれて、ありがとうございました。", "Mike wa, jibun de kaette kimashita. Shinpai shite kurete, arigatou gozaimashita.",
  "Mike đã tự về nhà rồi. Cảm ơn bạn đã lo lắng cho tôi.", "bow", "s_react")
N("s_returned", SZ, "ミケは、帰ってきました。今日は、元気です。", "ミケは、かえってきました。きょうは、げんきです。", "Mike wa, kaette kimashita. Kyou wa, genki desu.",
  "Mike đã về rồi. Hôm nay nó khỏe lắm.", "talk", "s_react")
N("s_react", SZ, "ほら、あそこ。ミケも、お祭りに来ましたよ。赤いリボンをしています。", "ほら、あそこ。ミケも、おまつりにきましたよ。あかいリボンをしています。", "Hora, asoko. Mike mo, omatsuri ni kimashita yo. Akai ribon wo shite imasu.",
  "Kìa, ở đằng kia. Mike cũng đến lễ hội rồi. Nó đang đeo chiếc nơ đỏ.", "point", choices=[
    C("よかったですね！", "Thật tốt quá ạ!", "s_end", "Vocabulary", 10, "Chia sẻ niềm vui bằng よかったですね", ["grammar.n5.desu_ne"], ["vocab.n5.yokatta"], ["fest.met_suzuki"]),
    C("そうですか。", "Vậy ạ.", "s_end", "ResponseAccuracy", 3, "Đáp lại được nhưng lạnh nhạt, chưa chia sẻ niềm vui", [], [], ["fest.met_suzuki"]),
    C("猫は、嫌いです。", "Cháu không thích mèo.", "s_wrong", "ResponseAccuracy", -8, "Nói điều tiêu cực đúng lúc người ta đang vui vì tìm được mèo"),
])
N("s_wrong", SZ, "あ…そうですか。でも、ミケは、あなたが好きですよ。", "あ…そうですか。でも、ミケは、あなたがすきですよ。", "A... sou desu ka. Demo, Mike wa, anata ga suki desu yo.",
  "À... vậy à. Nhưng Mike thì quý bạn lắm đấy.", "talk", "s_react")
N("s_end", SZ, "ありがとうございます、ありがとうございます！", "ありがとうございます、ありがとうございます！", "Arigatou gozaimasu, arigatou gozaimasu!",
  "Cảm ơn bạn, cảm ơn bạn nhiều lắm!", "bow", "b_hub", "obj_suzuki")

# ───────── Yamada, quán ramen ─────────
S.B("b_y", "done:scenario.restaurant.order_ramen", "y_regular", "y_new")
N("y_regular", YA, "おっ、また来たね！いつものラーメンかい？", "おっ、またきたね！いつものラーメンかい？", "O, mata kita ne! Itsumo no raamen kai?",
  "Ồ, lại đến rồi à! Vẫn tô ramen quen thuộc chứ?", "talk", "y_order")
N("y_new", YA, "いらっしゃい！新しい学生さんだね。ラーメン、食べていくかい？", "いらっしゃい！あたらしいがくせいさんだね。ラーメン、たべていくかい？", "Irasshai! Atarashii gakusei-san da ne. Raamen, tabete iku kai?",
  "Xin chào! Học viên mới nhỉ. Ăn tô ramen rồi hẵng đi chứ?", "talk", "y_order")
N("y_order", NAR, "ラーメンを注文します。何と言いますか。", "ラーメンをちゅうもんします。なんといいますか。", "Raamen wo chuumon shimasu. Nan to iimasu ka?",
  "Bạn gọi một tô ramen. Bạn sẽ nói gì?", "", choices=[
    C("ラーメンを一つください。", "Cho cháu một tô ramen ạ.", "y_serve", "Grammar", 10, "Gọi món đúng mẫu 〜を〜(số đếm)ください với 一つ", ["grammar.n5.wo_kudasai", "grammar.n5.counter_tsu"], ["vocab.n5.raamen", "vocab.n5.hitotsu"]),
    C("ラーメン、一つ。", "Ramen, một tô.", "y_serve_casual", "ResponseAccuracy", 3, "Hiểu được nhưng cộc lốc, thiếu をください", ["grammar.n5.counter_tsu"], ["vocab.n5.raamen"]),
    C("ラーメンをいただきます。", "Cháu xin phép dùng ramen ạ.", "y_wrong", "Grammar", -8, "いただきます nói khi bắt đầu ăn, không dùng để gọi món"),
])
N("y_serve_casual", YA, "ははは、いいよ。でも、「ください」をつけると、もっといいね。", "ははは、いいよ。でも、「ください」をつけると、もっといいね。", "Hahaha, ii yo. Demo, \"kudasai\" wo tsukeru to, motto ii ne.",
  "Haha, được thôi. Nhưng thêm \"kudasai\" vào thì còn hay hơn nữa.", "talk", "y_serve")
N("y_wrong", YA, "おいおい、まだ食べてないよ。注文は、「ください」だよ。", "おいおい、まだたべてないよ。ちゅうもんは、「ください」だよ。", "Oi oi, mada tabetenai yo. Chuumon wa, \"kudasai\" da yo.",
  "Này này, chưa ăn mà. Gọi món thì phải nói \"kudasai\" chứ.", "point", "y_order")
N("y_serve", YA, "はい、おまち！熱いから、気をつけてね。", "はい、おまち！あついから、きをつけてね。", "Hai, omachi! Atsui kara, ki wo tsukete ne.",
  "Đây, xong rồi! Nóng đấy, cẩn thận nhé.", "point", choices=[
    C("いただきます。", "Cháu xin phép dùng ạ.", "y_eat", "Vocabulary", 10, "Nói いただきます trước khi ăn", [], ["vocab.n5.itadakimasu"]),
    C("ありがとうございます。いただきます。", "Cháu cảm ơn. Cháu xin phép dùng ạ.", "y_eat", "Vocabulary", 10, "Cảm ơn rồi nói いただきます, rất lịch sự", [], ["vocab.n5.itadakimasu", "vocab.n5.arigatou_gozaimasu"]),
])
N("y_eat", YA, "どうだい？おいしいかい？", "どうだい？おいしいかい？", "Dou dai? Oishii kai?",
  "Thế nào? Ngon không?", "talk", choices=[
    C("とてもおいしいです！", "Rất ngon ạ!", "y_end", "Vocabulary", 10, "Khen món ăn bằng とてもおいしいです", ["grammar.n5.i_adj"], ["vocab.n5.oishii"], ["fest.met_yamada"]),
    C("おいしいです。", "Ngon ạ.", "y_end", "Vocabulary", 8, "Khen món ăn ngắn gọn nhưng đủ lịch sự", ["grammar.n5.i_adj"], ["vocab.n5.oishii"], ["fest.met_yamada"]),
    C("ふつうです。", "Bình thường ạ.", "y_plain", "ResponseAccuracy", -6, "Chê món ăn ngay trước mặt chủ quán: thẳng thắn quá mức và bất lịch sự"),
])
N("y_plain", YA, "ふつう！？ははは、正直だね。もう一度、食べてみて。", "ふつう！？ははは、しょうじきだね。もういちど、たべてみて。", "Futsuu!? Hahaha, shoujiki da ne. Mou ichido, tabete mite.",
  "Bình thường ư?! Haha, thật thà quá. Ăn thử lại xem.", "talk", "y_eat")
N("y_end", YA, "そうだろう！また、いつでも来てね。", "そうだろう！また、いつでもきてね。", "Sou darou! Mata, itsu demo kite ne.",
  "Đúng không nào! Lúc nào cũng ghé lại nhé.", "bow", "b_hub", "obj_yamada")

# ───────── Cô Morita và bạn Kim ─────────
S.B("b_m", "feel.nervous", "m_nervous", "m_general")
N("m_nervous", MO, "はじめて会った日、あなたは、少し緊張していましたね。今は、ずいぶん上手に話せますよ。", "はじめてあったひ、あなたは、すこしきんちょうしていましたね。いまは、ずいぶんじょうずにはなせますよ。", "Hajimete atta hi, anata wa, sukoshi kinchou shite imashita ne. Ima wa, zuibun jouzu ni hanasemasu yo.",
  "Hôm gặp nhau lần đầu, bạn đã hơi căng thẳng nhỉ. Bây giờ bạn nói giỏi lên rất nhiều rồi đấy.", "bow", "m_kim")
N("m_general", MO, "日本語が、とても上手になりましたね。がんばりましたね。", "にほんごが、とてもじょうずになりましたね。がんばりましたね。", "Nihongo ga, totemo jouzu ni narimashita ne. Ganbarimashita ne.",
  "Tiếng Nhật của bạn đã giỏi lên nhiều rồi nhỉ. Bạn đã rất cố gắng.", "bow", "m_kim")
N("m_kim", KI, "私も、がんばっています！まちがえても、笑いますよ。あはは！いっしょに、がんばりましょう！", "わたしも、がんばっています！まちがえても、わらいますよ。あはは！いっしょに、がんばりましょう！", "Watashi mo, ganbatte imasu! Machigaete mo, waraimasu yo. Ahaha! Issho ni, ganbarimashou!",
  "Tớ cũng đang cố gắng đây! Nói sai thì tớ cũng chỉ cười thôi. Haha! Cùng cố gắng nhé!", "talk", choices=[
    C("はい、いっしょにがんばりましょう。", "Ừ, cùng cố gắng nhé.", "m_end", "Grammar", 10, "Rủ cùng làm bằng mẫu 〜ましょう", ["grammar.n5.mashou"], ["vocab.n5.issho_ni", "vocab.n5.ganbaru"], ["fest.met_school"]),
    C("はい。", "Ừ.", "m_end", "ResponseAccuracy", 3, "Đáp được nhưng chưa đáp lại sự nhiệt tình của bạn", [], [], ["fest.met_school"]),
    C("私は、もうがんばりません。", "Tớ không cố gắng nữa đâu.", "m_wrong", "ResponseAccuracy", -8, "Nói điều tiêu cực trái ngược với tinh thần lễ hội và với người bạn đang cổ vũ"),
])
N("m_wrong", KI, "えっ！だめですよ！…あはは、じょうだんですよね？", "えっ！だめですよ！…あはは、じょうだんですよね？", "Ee! Dame desu yo! ...Ahaha, joudan desu yo ne?",
  "Hả! Không được đâu! ...Haha, cậu nói đùa đúng không?", "talk", "m_kim")
N("m_end", MO, "みなさん、この学生は、いいクラスメートです。", "みなさん、このがくせいは、いいクラスメートです。", "Minasan, kono gakusei wa, ii kurasumeeto desu.",
  "Mọi người ơi, học viên này là một bạn học rất tốt.", "bow", "b_hub", "obj_school")

# ───────── Bài phát biểu cuối ─────────
N("f_start", SA, "皆さん、ちょっと聞いてください。この学生さんが、あいさつをします。", "みなさん、ちょっときいてください。このがくせいさんが、あいさつをします。", "Minasan, chotto kiite kudasai. Kono gakusei-san ga, aisatsu wo shimasu.",
  "Mọi người ơi, xin hãy lắng nghe. Học viên này sẽ phát biểu đôi lời.", "talk", "f_s1")
N("f_s1", NAR, "たくさんの人が、あなたを見ています。まず、あいさつです。", "たくさんのひとが、あなたをみています。まず、あいさつです。", "Takusan no hito ga, anata wo mite imasu. Mazu, aisatsu desu.",
  "Rất nhiều người đang nhìn bạn. Trước hết là lời chào.", "", choices=[
    C("みなさん、こんばんは。", "Xin chào mọi người, chào buổi tối ạ.", "f_s2", "Vocabulary", 10, "Chào đúng buổi tối với nhiều người bằng みなさん、こんばんは", [], ["vocab.n5.minasan", "vocab.n5.konbanwa"]),
    C("こんにちは、みんな。", "Chào các bạn.", "f_s1_casual", "ResponseAccuracy", 2, "Sai buổi (đang là buổi tối) và quá thân mật cho một lời phát biểu", [], ["vocab.n5.konnichiwa"]),
    C("さようなら。", "Tạm biệt.", "f_s1_wrong", "Vocabulary", -8, "Nói tạm biệt ngay khi mở đầu: không phù hợp"),
])
N("f_s1_casual", NAR, "今は夜ですから、「こんばんは」ですね。もう一度、言いましょう。", "いまはよるですから、「こんばんは」ですね。もういちど、いいましょう。", "Ima wa yoru desu kara, \"konbanwa\" desu ne. Mou ichido, iimashou.",
  "Bây giờ là buổi tối nên phải nói \"konbanwa\". Ta nói lại nhé.", "", "f_s1")
N("f_s1_wrong", NAR, "まだ、始まったばかりですよ。「みなさん、こんばんは」と言いましょう。", "まだ、はじまったばかりですよ。「みなさん、こんばんは」といいましょう。", "Mada, hajimatta bakari desu yo. \"Minasan, konbanwa\" to iimashou.",
  "Mới bắt đầu thôi mà. Hãy nói \"minasan, konbanwa\" nhé.", "", "f_s1")
N("f_s2", NAR, "次に、この町の人に、お礼を言います。", "つぎに、このまちのひとに、おれいをいいます。", "Tsugi ni, kono machi no hito ni, orei wo iimasu.",
  "Tiếp theo, bạn nói lời cảm ơn với người dân khu phố này.", "", choices=[
    C("この町の人は、とてもやさしいです。ありがとうございます。", "Người dân khu phố này rất tử tế. Cảm ơn mọi người ạ.", "f_s3", "Grammar", 10, "Cảm ơn bằng câu đầy đủ: nêu lý do rồi ありがとうございます", ["grammar.n5.i_adj"], ["vocab.n5.yasashii", "vocab.n5.arigatou_gozaimasu"], ["fest.said_thanks"]),
    C("ありがとう。", "Cảm ơn.", "f_s2_casual", "ResponseAccuracy", 3, "Quá ngắn và thân mật cho lời phát biểu trước cả khu phố", [], []),
    C("ごめんなさい。", "Xin lỗi ạ.", "f_s2_wrong", "Vocabulary", -8, "Dùng lời xin lỗi thay cho lời cảm ơn"),
])
N("f_s2_casual", NAR, "もう少し、長く言いましょう。「この町の人は、やさしいです」と。", "もうすこし、ながくいいましょう。「このまちのひとは、やさしいです」と。", "Mou sukoshi, nagaku iimashou. \"Kono machi no hito wa, yasashii desu\" to.",
  "Hãy nói dài hơn một chút, ví dụ \"kono machi no hito wa, yasashii desu\" (người trong khu phố này rất tử tế).", "", "f_s2")
N("f_s2_wrong", NAR, "ここは、お礼の時間です。「ありがとうございます」ですよ。", "ここは、おれいのじかんです。「ありがとうございます」ですよ。", "Koko wa, orei no jikan desu. \"Arigatou gozaimasu\" desu yo.",
  "Đây là lúc nói lời cảm ơn. Câu cần nói là \"arigatou gozaimasu\" nhé.", "", "f_s2")
N("f_s3", NAR, "最後に、これからのことを言います。", "さいごに、これからのことをいいます。", "Saigo ni, korekara no koto wo iimasu.",
  "Cuối cùng, bạn nói về tương lai.", "", choices=[
    C("これからも、日本語をがんばります。よろしくお願いします。", "Từ nay cháu cũng sẽ cố gắng học tiếng Nhật. Xin mọi người tiếp tục giúp đỡ ạ.", "b_end1", "Grammar", 10, "Kết thúc bằng quyết tâm và よろしくお願いします, đúng chuẩn lời phát biểu", ["grammar.n5.mo_korekara"], ["vocab.n5.ganbaru", "vocab.n5.yoroshiku"], ["fest.said_promise"]),
    C("がんばります。", "Cháu sẽ cố gắng ạ.", "f_s3_short", "ResponseAccuracy", 3, "Đúng ý nhưng quá ngắn cho lời kết", [], ["vocab.n5.ganbaru"]),
    C("もう、帰ります。", "Cháu về đây ạ.", "f_s3_wrong", "ResponseAccuracy", -8, "Kết thúc bài phát biểu bằng việc rời đi: lạc điệu và thiếu tôn trọng"),
])
N("f_s3_short", NAR, "みんなが、あなたの言葉を待っています。もう少し、言いましょう。", "みんなが、あなたのことばをまっています。もうすこし、いいましょう。", "Minna ga, anata no kotoba wo matte imasu. Mou sukoshi, iimashou.",
  "Mọi người đang chờ lời của bạn. Hãy nói thêm một chút nhé.", "", "f_s3")
N("f_s3_wrong", NAR, "まだ、花火が始まっていませんよ。もう一度、言いましょう。", "まだ、はなびがはじまっていませんよ。もういちど、いいましょう。", "Mada, hanabi ga hajimatte imasen yo. Mou ichido, iimashou.",
  "Pháo hoa còn chưa bắt đầu mà. Ta nói lại nhé.", "", "f_s3")

# ───────── Sato phản hồi theo những gì bạn đã làm ─────────
S.B("b_end1", "sato.respect", "e_respect", "b_end2")
S.B("b_end2", "sato.stern", "e_stern", "e_plain")
N("e_respect", SA, "あなたは、ゴミのルールも、ちゃんと守りました。みなさん、この人は、町の大切な仲間です。", "あなたは、ゴミのルールも、ちゃんとまもりました。みなさん、このひとは、まちのたいせつななかまです。", "Anata wa, gomi no ruuru mo, chanto mamorimashita. Minasan, kono hito wa, machi no taisetsu na nakama desu.",
  "Bạn đã giữ đúng cả quy tắc đổ rác. Mọi người ơi, người này là một người bạn đồng hành quý giá của khu phố.", "bow", "e_end")
N("e_stern", SA, "最初は、心配しました。でも、今は、だいじょうぶ。あなたは、町の仲間です。", "さいしょは、しんぱいしました。でも、いまは、だいじょうぶ。あなたは、まちのなかまです。", "Saisho wa, shinpai shimashita. Demo, ima wa, daijoubu. Anata wa, machi no nakama desu.",
  "Lúc đầu tôi có lo lắng. Nhưng bây giờ thì ổn rồi. Bạn là một người của khu phố này.", "bow", "e_end")
N("e_plain", SA, "ありがとうございます。あなたは、この町の仲間です。", "ありがとうございます。あなたは、このまちのなかまです。", "Arigatou gozaimasu. Anata wa, kono machi no nakama desu.",
  "Cảm ơn bạn. Bạn là một người của khu phố này.", "bow", "e_end")
N("e_end", NAR, "そのとき、夜空に大きな花火が上がりました。ドン！みんなの笑い声が、ひろがります。", "そのとき、よぞらにおおきなはなびがあがりました。ドン！みんなのわらいごえが、ひろがります。", "Sono toki, yozora ni ookina hanabi ga agarimashita. Don! Minna no waraigoe ga, hirogarimasu.",
  "Đúng lúc đó, một chùm pháo hoa lớn bừng lên trên bầu trời đêm. Đùng! Tiếng cười của mọi người lan ra.", "", "e_last", "obj_speech")
N("e_last", NAR, "はじめて来た日から、三か月。ここは、もう、あなたの町です。", "はじめてきたひから、さんかげつ。ここは、もう、あなたのまちです。", "Hajimete kita hi kara, sankagetsu. Koko wa, mou, anata no machi desu.",
  "Ba tháng kể từ ngày đầu tiên bạn đến. Nơi này giờ đã là thị trấn của bạn.", "", "n_complete")
N("n_complete", NAR, "", "", "", "", "", "", type=4)

objectives = [
    ("obj_arrive", "夏祭りに着く", "Đến lễ hội mùa hè", False),
    ("obj_tanaka", "田中さんと話す", "Trò chuyện với bác Tanaka", False),
    ("obj_suzuki", "鈴木さんと話す", "Trò chuyện với cô Suzuki", False),
    ("obj_yamada", "山田さんのラーメンを食べる", "Ăn ramen của chú Yamada", False),
    ("obj_school", "森田先生とキムさんに会う", "Gặp cô Morita và bạn Kim", False),
    ("obj_speech", "みんなの前であいさつする", "Phát biểu trước mọi người", False),
]
targets = ["grammar.n5.mashou", "grammar.n5.wo_kudasai", "vocab.n5.yokatta", "vocab.n5.oishii", "vocab.n5.arigatou_gozaimasu"]

problems = S.validate("n_arrive", objectives)
print("problems:", problems)
if not problems:
    S.write(sys.argv[1], name="scenario_town_summer_festival", sid="scenario.town.summer_festival", title_ja="ひばり町の夏祭り", title_vi="Lễ hội mùa hè ở Hibari-chō",
            desc_ja="夏祭りの夜。町の人たちと話して、みんなの前であいさつしましょう。", desc_vi="Đêm lễ hội mùa hè. Gặp lại những người bạn bạn đã giúp đỡ, thử ramen của chú Yamada và phát biểu trước cả khu phố; mọi điều bạn từng làm đều được mọi người nhớ.",
            chapter=4, targets=targets, objectives=objectives, start="n_arrive")
    print("nodes", len(S.nodes))
