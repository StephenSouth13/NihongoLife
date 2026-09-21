# -*- coding: utf-8 -*-
import sys

from scen_lib import Scenario

S = Scenario()
C, N = S.C, S.N
T = ("Tanaka", "npc_neighbor_1")
NAR = None

N("node_find_tanaka", None, "", "", "", "", "", "n_invite", "obj_meet", type=6, npc="npc_neighbor_1")

# ───────── Lời mời ─────────
N("n_invite", T, "あ、さっきの方ですね。ちょうどよかった。うちでお茶でもどうですか。", "あ、さっきのかたですね。ちょうどよかった。うちでおちゃでもどうですか。", "A, sakki no kata desu ne. Choudo yokatta. Uchi de ocha demo dou desu ka?",
  "À, là bạn lúc nãy nhỉ. Đúng lúc quá. Bạn có muốn uống chút trà ở nhà tôi không?", "talk", choices=[
    C("はい、ありがとうございます。", "Vâng, cảm ơn bác ạ.", "b_feel1", "ResponseAccuracy", 10, "Nhận lời mời lịch sự bằng はい、ありがとうございます", [], ["vocab.n5.arigatou_gozaimasu"], ["tanaka.invited"]),
    C("すみません、今日はちょっと…", "Xin lỗi, hôm nay hơi bất tiện ạ...", "n_invite_again", "ResponseAccuracy", 6, "Từ chối nhẹ nhàng bằng ちょっと… (cách từ chối rất Nhật, không nói thẳng いいえ)", [], ["vocab.n5.sumimasen"]),
    C("いいえ。", "Không.", "n_invite_rude", "ResponseAccuracy", -8, "Từ chối cộc lốc bằng いいえ khi được mời, dễ làm người mời mất lòng"),
])
N("n_invite_again", T, "そうですか。でも、五分だけどうですか。おいしいお菓子があります。", "そうですか。でも、ごふんだけどうですか。おいしいおかしがあります。", "Sou desu ka. Demo, gofun dake dou desu ka. Oishii okashi ga arimasu.",
  "Vậy à. Nhưng chỉ năm phút thôi thì sao? Tôi có bánh kẹo ngon lắm.", "talk", choices=[
    C("それでは、少しだけ。", "Vậy thì, chỉ một chút thôi ạ.", "b_feel1", "ResponseAccuracy", 8, "Đổi ý một cách lịch sự với それでは、少しだけ", [], [], ["tanaka.invited"]),
    C("はい、ありがとうございます。", "Vâng, cảm ơn bác ạ.", "b_feel1", "ResponseAccuracy", 8, "Nhận lời mời lần hai bằng はい、ありがとうございます", [], ["vocab.n5.arigatou_gozaimasu"], ["tanaka.invited"]),
])
N("n_invite_rude", T, "あ…そうですか。でも、遠慮しなくてもいいですよ。もう一度、どうですか。", "あ…そうですか。でも、えんりょしなくてもいいですよ。もういちど、どうですか。", "A... sou desu ka. Demo, enryo shinakute mo ii desu yo. Mou ichido, dou desu ka.",
  "À... vậy à. Nhưng bạn không cần ngại đâu. Bạn nghĩ lại một lần nữa nhé?", "talk", "n_invite")

# ───────── Tanaka nhớ cảm xúc của bạn (story flags) ─────────
S.B("b_feel1", "feel.nervous", "n_care_nervous", "b_feel2")
S.B("b_feel2", "feel.tired", "n_care_tired", "n_care_other")
N("n_care_nervous", T, "少し緊張していますね。だいじょうぶ。日本語は、まちがえてもいいですよ。", "すこしきんちょうしていますね。だいじょうぶ。にほんごは、まちがえてもいいですよ。", "Sukoshi kinchou shite imasu ne. Daijoubu. Nihongo wa, machigaete mo ii desu yo.",
  "Trông bạn hơi căng thẳng nhỉ. Không sao đâu. Nói tiếng Nhật sai cũng không sao mà.", "talk", "n_door")
N("n_care_tired", T, "ちょっと疲れていますか。うちで、ゆっくり休んでください。", "ちょっとつかれていますか。うちで、ゆっくりやすんでください。", "Chotto tsukarete imasu ka. Uchi de, yukkuri yasunde kudasai.",
  "Bạn có vẻ hơi mệt à. Hãy nghỉ ngơi thong thả ở nhà tôi nhé.", "talk", "n_door")
N("n_care_other", T, "元気ですね。いいことです。さあ、こちらへ。", "げんきですね。いいことです。さあ、こちらへ。", "Genki desu ne. Ii koto desu. Saa, kochira e.",
  "Bạn khỏe khoắn nhỉ. Thật tốt. Nào, mời đi lối này.", "talk", "n_door")

# ───────── Vào nhà ─────────
N("n_door", NAR, "田中さんの家は、となりの小さな白い家です。玄関の前に来ました。", "たなかさんのいえは、となりのちいさなしろいいえです。げんかんのまえにきました。", "Tanaka-san no ie wa, tonari no chiisana shiroi ie desu. Genkan no mae ni kimashita.",
  "Nhà bác Tanaka là ngôi nhà trắng nhỏ ở bên cạnh. Bạn đứng trước cửa (genkan).", "", "n_enter")
N("n_enter", T, "どうぞ、入ってください。", "どうぞ、はいってください。", "Douzo, haitte kudasai.",
  "Mời bạn vào nhà.", "talk", choices=[
    C("お邪魔します。", "Xin phép cho tôi làm phiền ạ.", "n_shoes", "Vocabulary", 10, "Nói お邪魔します khi bước vào nhà người khác", [], ["vocab.n5.ojamashimasu"], ["house1.polite_entry"]),
    C("入ります。", "Tôi vào đây.", "n_enter_blunt", "ResponseAccuracy", 2, "Ý đúng nhưng cộc lốc, thiếu lời xin phép khi vào nhà người khác", [], []),
    C("失礼しました。", "Tôi xin lỗi vì đã thất lễ.", "n_enter_wrong", "Vocabulary", -8, "Dùng lời nói khi rời đi 失礼しました cho lúc bước vào nhà"),
])
N("n_enter_blunt", T, "はい、どうぞ。人の家に入るときは、「お邪魔します」と言いますよ。", "はい、どうぞ。ひとのいえにはいるときは、「おじゃまします」といいますよ。", "Hai, douzo. Hito no ie ni hairu toki wa, \"ojamashimasu\" to iimasu yo.",
  "Vâng, mời vào. Khi vào nhà người khác thì nói \"ojamashimasu\" nhé.", "talk", "n_shoes")
N("n_enter_wrong", T, "あ、それは帰るときに言います。入るときは、「お邪魔します」ですよ。", "あ、それはかえるときにいいます。はいるときは、「おじゃまします」ですよ。", "A, sore wa kaeru toki ni iimasu. Hairu toki wa, \"ojamashimasu\" desu yo.",
  "À, câu đó nói khi ra về. Khi vào thì nói \"ojamashimasu\" nhé.", "point", "n_enter")

N("n_shoes", T, "ここで、くつをぬいでください。スリッパをどうぞ。", "ここで、くつをぬいでください。スリッパをどうぞ。", "Koko de, kutsu wo nuide kudasai. Surippa wo douzo.",
  "Xin hãy cởi giày ở đây. Đôi dép đi trong nhà đây, mời bạn.", "point", "", "obj_enter", choices=[
    C("はい、ぬぎます。", "Vâng, tôi cởi ạ.", "n_livingroom", "Grammar", 10, "Đáp lại lời nhờ 〜てください bằng câu đồng ý はい、〜ます", ["grammar.n5.te_kudasai"], ["vocab.n5.kutsu", "vocab.n5.genkan"], ["house1.took_off_shoes"]),
    C("くつのまま入ってもいいですか。", "Tôi đi giày vào luôn được không ạ?", "n_shoes_wrong", "Grammar", -8, "Hỏi xin phép đi giày vào nhà: mẫu câu 〜てもいいですか đúng nhưng nội dung trái phong tục Nhật"),
    C("スリッパはどこですか。", "Dép đi trong nhà ở đâu ạ?", "n_shoes_slipper", "Grammar", 5, "Hỏi đúng mẫu 〜はどこですか nhưng đã được đưa dép rồi, nên trước tiên hãy cởi giày", ["grammar.n5.wa_doko_desu_ka"], ["vocab.n5.surippa"]),
])
N("n_shoes_wrong", T, "いいえ、日本では、家の中で、くつをぬぎます。ぬいでください。", "いいえ、にほんでは、いえのなかで、くつをぬぎます。ぬいでください。", "Iie, Nihon de wa, ie no naka de, kutsu wo nugimasu. Nuide kudasai.",
  "Không nhé, ở Nhật người ta cởi giày khi vào trong nhà. Xin bạn cởi ra.", "point", "n_shoes")
N("n_shoes_slipper", T, "ふふ、ここにありますよ。でも、まず、くつをぬいでくださいね。", "ふふ、ここにありますよ。でも、まず、くつをぬいでくださいね。", "Fufu, koko ni arimasu yo. Demo, mazu, kutsu wo nuide kudasai ne.",
  "Hì, đây này. Nhưng trước hết, bạn cởi giày ra nhé.", "point", "n_shoes")

# ───────── Phòng khách, trà ─────────
N("n_livingroom", T, "どうぞ、ここに座ってください。", "どうぞ、ここにすわってください。", "Douzo, koko ni suwatte kudasai.",
  "Mời bạn ngồi ở đây.", "talk", choices=[
    C("失礼します。", "Xin phép ạ.", "n_tea", "Vocabulary", 10, "Nói 失礼します khi ngồi xuống chỗ được mời", [], ["vocab.n5.shitsurei_shimasu"]),
    C("はい。", "Vâng.", "n_tea", "ResponseAccuracy", 3, "Đáp ngắn được nhưng thiếu lời xin phép lịch sự", [], []),
    C("座ってもいいですか。", "Tôi ngồi được không ạ?", "n_livingroom_odd", "Grammar", 3, "Đúng ngữ pháp nhưng thừa: chủ nhà vừa mời ngồi rồi", ["grammar.n5.te_mo_ii_desu_ka"], ["vocab.n5.suwaru"]),
])
N("n_livingroom_odd", T, "ふふ、もちろん。今、どうぞと言いましたよ。", "ふふ、もちろん。いま、どうぞといいましたよ。", "Fufu, mochiron. Ima, douzo to iimashita yo.",
  "Hì, dĩ nhiên rồi. Tôi vừa nói \"douzo\" (mời) mà.", "talk", "n_tea")
N("n_tea", T, "お茶をどうぞ。熱いですから、気をつけてください。", "おちゃをどうぞ。あついですから、きをつけてください。", "Ocha wo douzo. Atsui desu kara, ki wo tsukete kudasai.",
  "Mời bạn dùng trà. Trà nóng nên hãy cẩn thận nhé.", "talk", choices=[
    C("ありがとうございます。いただきます。", "Cảm ơn bác. Cháu xin phép dùng ạ.", "n_photo", "Vocabulary", 10, "Cảm ơn rồi nói いただきます trước khi dùng, đủ lịch sự", [], ["vocab.n5.itadakimasu", "vocab.n5.arigatou_gozaimasu"], ["tanaka.tea_accepted"]),
    C("いただきます。", "Cháu xin phép dùng ạ.", "n_photo", "Vocabulary", 6, "Đúng nhưng nên cảm ơn trước khi nhận trà", [], ["vocab.n5.itadakimasu"], ["tanaka.tea_accepted"]),
    C("けっこうです。", "Không cần đâu ạ.", "n_tea_wrong", "ResponseAccuracy", -8, "けっこうです là từ chối, nhưng trà đã được rót ra rồi"),
])
N("n_tea_wrong", T, "あ…もうお茶を入れましたよ。「いただきます」と言いましょう。", "あ…もうおちゃをいれましたよ。「いただきます」といいましょう。", "A... mou ocha wo iremashita yo. \"Itadakimasu\" to iimashou.",
  "À... tôi đã rót trà rồi mà. Bạn hãy nói \"itadakimasu\" nhé.", "talk", "n_tea")

# ───────── Bức ảnh và câu chuyện của bác Tanaka ─────────
N("n_photo_intro", NAR, "壁に、古い写真があります。学校の前で、たくさんの人が笑っています。", "かべに、ふるいしゃしんがあります。がっこうのまえで、たくさんのひとがわらっています。", "Kabe ni, furui shashin ga arimasu. Gakkou no mae de, takusan no hito ga waratte imasu.",
  "Trên tường có một bức ảnh cũ. Trước cổng trường, rất nhiều người đang mỉm cười.", "", "n_photo_ask")
N("n_photo", NAR, "田中さんは、うれしそうにお茶を飲んでいます。", "たなかさんは、うれしそうにおちゃをのんでいます。", "Tanaka-san wa, ureshisou ni ocha wo nonde imasu.",
  "Bác Tanaka vui vẻ uống trà.", "", "n_photo_intro")
N("n_photo_ask", NAR, "写真を見たいです。田中さんに、何と言いますか。", "しゃしんをみたいです。たなかさんに、なんといいますか。", "Shashin wo mitai desu. Tanaka-san ni, nan to iimasu ka?",
  "Bạn muốn xem bức ảnh. Bạn nói gì với bác Tanaka?", "", choices=[
    C("この写真を見てもいいですか。", "Cháu xem bức ảnh này được không ạ?", "n_teacher", "Grammar", 10, "Xin phép đúng mẫu 〜てもいいですか trước khi xem đồ của người khác", ["grammar.n5.te_mo_ii_desu_ka"], ["vocab.n5.shashin"], ["house1.asked_permission"]),
    C("この写真を見ます。", "Cháu xem bức ảnh này.", "n_photo_blunt", "ResponseAccuracy", 2, "Thông báo chứ không xin phép; với đồ của người khác nên hỏi trước", [], ["vocab.n5.shashin"]),
    C("この写真をください。", "Bác cho cháu bức ảnh này.", "n_photo_wrong", "Grammar", -8, "Dùng をください (xin vật) trong khi chỉ muốn xem"),
])
N("n_photo_blunt", T, "ふふ、どうぞ。でも、「見てもいいですか」と聞くと、もっとていねいですよ。", "ふふ、どうぞ。でも、「みてもいいですか」ときくと、もっとていねいですよ。", "Fufu, douzo. Demo, \"mite mo ii desu ka\" to kiku to, motto teinei desu yo.",
  "Hì, cứ xem đi. Nhưng hỏi \"mite mo ii desu ka\" sẽ lịch sự hơn đấy.", "talk", "n_teacher")
N("n_photo_wrong", T, "え？あげられませんよ。見るだけです。「見てもいいですか」ですね。", "え？あげられませんよ。みるだけです。「みてもいいですか」ですね。", "E? Agerarimasen yo. Miru dake desu. \"Mite mo ii desu ka\" desu ne.",
  "Hả? Tôi không cho được đâu. Chỉ để xem thôi. Bạn nói \"mite mo ii desu ka\" nhé.", "point", "n_photo_ask")
N("n_teacher", T, "どうぞ。昔、学校で先生をしていました。これは、私の生徒たちです。", "どうぞ。むかし、がっこうでせんせいをしていました。これは、わたしのせいとたちです。", "Douzo. Mukashi, gakkou de sensei wo shite imashita. Kore wa, watashi no seito-tachi desu.",
  "Xin mời. Ngày xưa tôi từng làm giáo viên ở trường. Đây là các học trò của tôi.", "talk", choices=[
    C("先生でしたか。すごいですね。", "Bác từng là thầy giáo ạ? Giỏi quá.", "n_story", "Grammar", 10, "Dùng 〜でしたか (quá khứ của です) để bày tỏ ngạc nhiên và khen", ["grammar.n5.deshita_ka"], ["vocab.n5.sensei"], ["tanaka.knows_teacher"]),
    C("先生ですか。", "Bác là thầy giáo ạ?", "n_teacher_present", "Grammar", 3, "Dùng thì hiện tại ですか trong khi bác nói chuyện đã qua (でしたか mới đúng)", ["grammar.n5.deshita_ka"], ["vocab.n5.sensei"]),
    C("先生はきらいです。", "Cháu không thích thầy giáo.", "n_teacher_wrong", "ResponseAccuracy", -8, "Câu nói bất lịch sự, không phù hợp: bác đang tự hào kể về nghề cũ"),
])
N("n_teacher_present", T, "はい、昔ですよ。「先生でしたか」と言うと、いいですね。", "はい、むかしですよ。「せんせいでしたか」というと、いいですね。", "Hai, mukashi desu yo. \"Sensei deshita ka\" to iu to, ii desu ne.",
  "Vâng, chuyện ngày xưa thôi. Nói \"sensei deshita ka\" sẽ hay hơn nhé.", "talk", "n_story")
N("n_teacher_wrong", T, "あ…そうですか。でも、私は先生の仕事が大好きでしたよ。", "あ…そうですか。でも、わたしはせんせいのしごとがだいすきでしたよ。", "A... sou desu ka. Demo, watashi wa sensei no shigoto ga daisuki deshita yo.",
  "À... vậy à. Nhưng tôi từng rất yêu nghề giáo viên đấy.", "talk", "n_teacher")

# ───────── Khoảnh khắc đồng cảm ─────────
N("n_story", T, "若いとき、外国に住んでいました。一人で、さびしかったです。でも、近所の人が、やさしかったです。", "わかいとき、がいこくにすんでいました。ひとりで、さびしかったです。でも、きんじょのひとが、やさしかったです。", "Wakai toki, gaikoku ni sunde imashita. Hitori de, sabishikatta desu. Demo, kinjo no hito ga, yasashikatta desu.",
  "Hồi trẻ tôi từng sống ở nước ngoài. Một mình, tôi rất cô đơn. Nhưng những người hàng xóm rất tử tế.", "talk", choices=[
    C("私も、少しさびしいです。でも、田中さんに会えて、うれしいです。", "Cháu cũng hơi cô đơn. Nhưng được gặp bác Tanaka, cháu rất vui.", "n_story_warm", "ResponseAccuracy", 10, "Chia sẻ cảm xúc thật bằng 〜て、うれしいです (vui vì ...) — đây là cách tạo tình bạn", ["grammar.n5.te_form_reason"], ["vocab.n5.sabishii", "vocab.n5.ureshii"], ["tanaka.friend"]),
    C("そうですか。", "Vậy ạ.", "n_story_flat", "ResponseAccuracy", 2, "Đáp được nhưng quá ngắn, bỏ lỡ dịp chia sẻ cảm xúc", [], []),
    C("外国はきらいです。", "Cháu không thích nước ngoài.", "n_story_wrong", "ResponseAccuracy", -8, "Nói điều tiêu cực và chẳng liên quan đến câu chuyện của bác, còn mâu thuẫn với việc bạn đang ở Nhật"),
])
N("n_story_warm", T, "ありがとう。うれしいです。私たちは、もう友達ですね。", "ありがとう。うれしいです。わたしたちは、もうともだちですね。", "Arigatou. Ureshii desu. Watashitachi wa, mou tomodachi desu ne.",
  "Cảm ơn bạn. Tôi mừng lắm. Chúng ta đã là bạn rồi nhé.", "bow", "n_school")
N("n_story_flat", T, "ふふ。いつでも話してくださいね。私は、となりに住んでいます。", "ふふ。いつでもはなしてくださいね。わたしは、となりにすんでいます。", "Fufu. Itsu demo hanashite kudasai ne. Watashi wa, tonari ni sunde imasu.",
  "Hì. Bạn cứ nói chuyện với tôi bất cứ lúc nào nhé. Tôi sống ngay bên cạnh.", "talk", "n_school")
N("n_story_wrong", T, "え？…ふふ、そうですか。でも、この町は、いい町ですよ。もう一度、話しましょう。", "え？…ふふ、そうですか。でも、このまちは、いいまちですよ。もういちど、はなしましょう。", "E? ...Fufu, sou desu ka. Demo, kono machi wa, ii machi desu yo. Mou ichido, hanashimashou.",
  "Hả? ...Hì, vậy à. Nhưng thị trấn này là một nơi tốt đấy. Ta nói chuyện lại nhé.", "talk", "n_story")

# ───────── Giới thiệu trường ─────────
N("n_school", T, "あなたは、ひばり日本語学院の学生さんですね。明日、学校へ行きますか。", "あなたは、ひばりにほんごがくいんのがくせいさんですね。あした、がっこうへいきますか。", "Anata wa, Hibari Nihongo Gakuin no gakusei-san desu ne. Ashita, gakkou e ikimasu ka?",
  "Bạn là học viên của Học viện Hibari đúng không. Ngày mai bạn có đến trường chứ?", "talk", "", "obj_talk", choices=[
    C("はい、明日の朝九時に行きます。", "Vâng, sáng mai lúc 9 giờ cháu sẽ đến.", "n_school_ok", "Grammar", 10, "Nói kế hoạch tương lai bằng 〜に行きます kèm giờ giấc", ["grammar.n5.e_ikimasu", "grammar.n5.ni_time"], ["vocab.n5.ashita", "vocab.n5.kuji"]),
    C("はい、昨日行きました。", "Vâng, hôm qua cháu đã đến.", "n_school_wrong", "Grammar", -8, "Dùng quá khứ 昨日行きました trong khi câu hỏi nói về ngày mai"),
    C("はい、行きます。", "Vâng, cháu đi ạ.", "n_school_ok", "ResponseAccuracy", 5, "Đúng ý nhưng chưa nói rõ thời gian", ["grammar.n5.e_ikimasu"], []),
])
N("n_school_wrong", T, "え？「きのう」ではなくて、「あした」ですよ。明日のことを聞いています。もう一度どうぞ。", "え？「きのう」ではなくて、「あした」ですよ。あしたのことをきいています。もういちどどうぞ。", "E? \"Kinou\" dewa nakute, \"ashita\" desu yo. Ashita no koto wo kiite imasu. Mou ichido douzo.",
  "Hả? Không phải 「昨日」 (hôm qua) mà là 「明日」 (ngày mai). Tôi đang hỏi về ngày mai. Bạn thử lại nhé.", "point", "n_school")
N("n_school_ok", T, "いい先生がいますよ。森田先生です。やさしくて、ゆっくり話してくれます。", "いいせんせいがいますよ。もりたせんせいです。やさしくて、ゆっくりはなしてくれます。", "Ii sensei ga imasu yo. Morita-sensei desu. Yasashikute, yukkuri hanashite kuremasu.",
  "Ở đó có một cô giáo rất tốt, cô Morita. Cô hiền và luôn nói chậm cho học viên nghe.", "talk", "n_leave")

# ───────── Ra về ─────────
N("n_leave", T, "もう、こんな時間ですね。今日は、ありがとう。またいつでも来てくださいね。", "もう、こんなじかんですね。きょうは、ありがとう。またいつでもきてくださいね。", "Mou, konna jikan desu ne. Kyou wa, arigatou. Mata itsu demo kite kudasai ne.",
  "Đã muộn thế này rồi nhỉ. Hôm nay cảm ơn bạn. Lúc nào cũng hãy ghé chơi nhé.", "talk", choices=[
    C("お茶、ごちそうさまでした。お邪魔しました。", "Cảm ơn bác về trà ạ. Cháu xin phép về ạ.", "n_bye", "Vocabulary", 10, "Kết thúc buổi thăm bằng ごちそうさまでした và お邪魔しました", [], ["vocab.n5.gochisousama_deshita", "vocab.n5.ojamashimashita"], ["house1.polite_exit"]),
    C("さようなら。", "Tạm biệt.", "n_leave_casual", "ResponseAccuracy", 2, "Chào tạm biệt được nhưng thiếu lời cảm ơn và お邪魔しました", [], []),
    C("いただきます。", "Cháu xin phép dùng ạ.", "n_leave_wrong", "Vocabulary", -8, "いただきます nói trước khi ăn, không phải lời chào khi ra về"),
])
N("n_leave_casual", T, "はい、さようなら。帰るときは、「お邪魔しました」と言うと、いいですよ。", "はい、さようなら。かえるときは、「おじゃましました」というと、いいですよ。", "Hai, sayounara. Kaeru toki wa, \"ojamashimashita\" to iu to, ii desu yo.",
  "Vâng, tạm biệt. Khi ra về thì nói \"ojamashimashita\" sẽ hay hơn đấy.", "talk", "n_bye")
N("n_leave_wrong", T, "あ、それは食べる前に言いますよ。帰るときは、「お邪魔しました」ですね。", "あ、それはたべるまえにいいますよ。かえるときは、「おじゃましました」ですね。", "A, sore wa taberu mae ni iimasu yo. Kaeru toki wa, \"ojamashimashita\" desu ne.",
  "À, câu đó nói trước khi ăn nhé. Khi ra về là \"ojamashimashita\".", "point", "n_leave")
N("n_bye", T, "はい、気をつけて。また明日、学校で！", "はい、きをつけて。またあした、がっこうで！", "Hai, ki wo tsukete. Mata ashita, gakkou de!",
  "Vâng, đi cẩn thận nhé. Mai gặp lại, ở trường nhé!", "bow", "n_recap", "obj_leave")

N("n_recap", NAR, "今日のポイント：「お邪魔します」「〜てもいいですか」「〜てください」。", "きょうのポイント：「おじゃまします」「〜てもいいですか」「〜てください」。", "Kyou no pointo: \"ojamashimasu\" \"~ te mo ii desu ka\" \"~ te kudasai\".",
  "Tóm tắt: \"ojamashimasu\" (khi vào nhà), \"~ te mo ii desu ka\" (xin phép làm gì), \"~ te kudasai\" (xin hãy làm ...).", "", "n_complete")
N("n_complete", NAR, "", "", "", "", "", "", type=4)

objectives = [
    ("obj_meet", "田中さんに会う", "Gặp bác Tanaka", False),
    ("obj_enter", "礼儀正しく家に入る", "Vào nhà một cách lịch sự", False),
    ("obj_talk", "お茶を飲みながら話す", "Vừa uống trà vừa trò chuyện", False),
    ("obj_leave", "お礼を言って帰る", "Cảm ơn và ra về", False),
]
targets = ["grammar.n5.te_mo_ii_desu_ka", "grammar.n5.te_kudasai", "vocab.n5.ojamashimasu", "vocab.n5.itadakimasu", "vocab.n5.gochisousama_deshita"]

problems = S.validate("node_find_tanaka", objectives)
print("problems:", problems)
if not problems:
    S.write(sys.argv[1], name="scenario_house1_greeting", sid="scenario.house1.greeting", title_ja="田中さんの家でお茶", title_vi="Uống trà ở nhà bác Tanaka",
            desc_ja="田中さんの家に招かれました。あいさつ、くつ、お茶、写真のことを、日本語でていねいに話しましょう。", desc_vi="Được bác Tanaka mời sang nhà. Học cách vào nhà, cởi giày, nhận trà, xin phép xem đồ và chào ra về.",
            chapter=1, targets=targets, objectives=objectives, start="node_find_tanaka")
    print("nodes", len(S.nodes))
