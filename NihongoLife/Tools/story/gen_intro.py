# -*- coding: utf-8 -*-
import sys

from scen_lib import Scenario

S = Scenario()
C, N = S.C, S.N
NAR = None  # narration (no speaker)

# ───────── Ga Hibari ─────────
N("n_open", NAR, "八月の夕方。ひばり駅に着きました。", "はちがつのゆうがた。ひばりえきにつきました。", "Hachigatsu no yuugata. Hibari eki ni tsukimashita.",
  "Chiều tháng Tám. Bạn xuống tàu ở ga Hibari.", "", "n_open2", "obj_arrive")
N("n_open2", NAR, "大きなかばんを持って、はじめて一人で日本に来ました。ここが、これから住む町です。", "おおきなかばんをもって、はじめてひとりでにほんにきました。ここが、これからすむまちです。", "Ookina kaban wo motte, hajimete hitori de Nihon ni kimashita. Koko ga, korekara sumu machi desu.",
  "Kéo theo chiếc vali lớn, lần đầu tiên bạn một mình đến Nhật. Đây là nơi bạn sẽ sống từ nay.", "", "n_feel")

# ───────── Cảm xúc của bạn (lưu lại để NPC nhớ) ─────────
N("n_feel", NAR, "むねがどきどきします。今、どんな気持ちですか。", "むねがどきどきします。いま、どんなきもちですか。", "Mune ga dokidoki shimasu. Ima, donna kimochi desu ka?",
  "Tim bạn đập thình thịch. Lúc này bạn thấy thế nào?", "", choices=[
    C("わくわくします。", "Tôi thấy háo hức.", "n_feel_excited", "Vocabulary", 8, "Nói cảm xúc bằng động từ わくわくします (háo hức)", ["grammar.n5.masu_form"], ["vocab.n5.wakuwaku"], ["feel.excited"]),
    C("ちょっと不安です。", "Tôi hơi lo lắng.", "n_feel_nervous", "Vocabulary", 8, "Nói cảm xúc bằng tính từ 不安です (lo lắng) và ちょっと làm nhẹ ý", ["grammar.n5.desu_adj"], ["vocab.n5.fuan"], ["feel.nervous"]),
    C("疲れました。", "Tôi mệt rồi.", "n_feel_tired", "Vocabulary", 8, "Nói cảm xúc bằng 疲れました (đã mệt) sau chuyến đi dài", ["grammar.n5.masu_form"], ["vocab.n5.tsukareta"], ["feel.tired"]),
])
N("n_feel_excited", NAR, "新しい町。新しい生活。早く歩いてみたいです。", "あたらしいまち。あたらしいせいかつ。はやくあるいてみたいです。", "Atarashii machi. Atarashii seikatsu. Hayaku aruite mitai desu.",
  "Thị trấn mới. Cuộc sống mới. Bạn muốn đi thử ngay.", "", "n_exit")
N("n_feel_nervous", NAR, "日本語がうまく話せるでしょうか。でも、だいじょうぶ。まず、一歩ずつ。", "にほんごがうまくはなせるでしょうか。でも、だいじょうぶ。まず、いっぽずつ。", "Nihongo ga umaku hanaseru deshou ka. Demo, daijoubu. Mazu, ippo zutsu.",
  "Liệu mình có nói tiếng Nhật giỏi không nhỉ? Nhưng không sao. Cứ từng bước một.", "", "n_exit")
N("n_feel_tired", NAR, "今日は早く休みたいです。でも、その前にやることがあります。", "きょうははやくやすみたいです。でも、そのまえにやることがあります。", "Kyou wa hayaku yasumitai desu. Demo, sono mae ni yaru koto ga arimasu.",
  "Hôm nay bạn muốn nghỉ sớm. Nhưng trước đó còn việc phải làm.", "", "n_exit")

# ───────── Tìm lối ra (đọc biển) ─────────
N("n_exit", NAR, "駅の中に、大きな看板がいくつかあります。外に出たいです。どれを見ますか。", "えきのなかに、おおきなかんばんがいくつかあります。そとにでたいです。どれをみますか。", "Eki no naka ni, ookina kanban ga ikutsu ka arimasu. Soto ni detai desu. Dore wo mimasu ka?",
  "Trong ga có vài tấm biển lớn. Bạn muốn ra ngoài. Bạn nhìn tấm nào?", "", choices=[
    C("「出口」を見ます。", "Tôi nhìn biển \"deguchi\" (lối ra).", "n_exit_ok", "Vocabulary", 10, "Đọc đúng biển 出口 (lối ra) bằng chữ Hán N5", [], ["vocab.n5.deguchi"]),
    C("「入口」を見ます。", "Tôi nhìn biển \"iriguchi\" (lối vào).", "n_exit_wrong", "Vocabulary", -8, "Nhầm 入口 (lối vào) với 出口 (lối ra)"),
    C("駅員さんに聞きます。", "Tôi hỏi nhân viên nhà ga.", "n_exit_ask", "ResponseAccuracy", 4, "Hỏi người khác cũng được, nhưng nên tập đọc biển trước", [], ["vocab.n5.ekiin"]),
])
N("n_exit_wrong", NAR, "「入」は中に入る、「出」は外に出る。「入口」は入るところです。もう一度見ましょう。", "「にゅう」はなかにはいる、「しゅつ」はそとにでる。「いりぐち」ははいるところです。もういちどみましょう。", "\"Nyuu\" wa naka ni hairu, \"shutsu\" wa soto ni deru. \"Iriguchi\" wa hairu tokoro desu. Mou ichido mimashou.",
  "「入」là vào trong, 「出」là ra ngoài. 「入口」là chỗ để đi vào. Ta xem lại nhé.", "", "n_exit")
N("n_exit_ask", NAR, "駅員さんは、あの緑の看板を指さしました。「出口」と書いてあります。", "えきいんさんは、あのみどりのかんばんをゆびさしました。「でぐち」とかいてあります。", "Ekiin-san wa, ano midori no kanban wo yubisashimashita. \"Deguchi\" to kaite arimasu.",
  "Nhân viên nhà ga chỉ vào tấm biển xanh lá kia. Trên đó viết 「出口」 (lối ra).", "", "n_out")
N("n_exit_ok", NAR, "あの緑の看板に「出口」と書いてあります。そちらへ行きましょう。", "あのみどりのかんばんに「でぐち」とかいてあります。そちらへいきましょう。", "Ano midori no kanban ni \"deguchi\" to kaite arimasu. Sochira e ikimashou.",
  "Trên tấm biển xanh lá kia có viết 「出口」. Đi về phía đó nào.", "", "n_out")
N("n_out", NAR, "外は、ゆうやけがきれいな町でした。さくらの並木道が長くのびています。", "そとは、ゆうやけがきれいなまちでした。さくらのなみきみちがながくのびています。", "Soto wa, yuuyake ga kirei na machi deshita. Sakura no namikimichi ga nagaku nobite imasu.",
  "Bên ngoài là một thị trấn có hoàng hôn rất đẹp. Con đường trồng anh đào kéo dài phía trước.", "", "n_phone")

# ───────── Tin nhắn của trường (đọc hiểu) ─────────
N("n_phone", NAR, "スマホが鳴りました。学校からのメッセージです。", "スマホがなりました。がっこうからのメッセージです。", "Sumaho ga narimashita. Gakkou kara no messeeji desu.",
  "Điện thoại kêu lên. Là tin nhắn từ trường.", "", "n_message", "obj_arrive")
N("n_message", NAR, "「ひばり日本語学院です。ようこそ！明日の朝九時に、学校へ来てください。」", "「ひばりにほんごがくいんです。ようこそ！あしたのあさくじに、がっこうへきてください。」", "\"Hibari Nihongo Gakuin desu. Youkoso! Ashita no asa kuji ni, gakkou e kite kudasai.\"",
  "\"Đây là Học viện tiếng Nhật Hibari. Chào mừng bạn! Sáng mai lúc 9 giờ, xin hãy đến trường.\"", "", "n_message_q")
N("n_message_q", NAR, "学校へ行くのは、いつですか。", "がっこうへいくのは、いつですか。", "Gakkou e iku no wa, itsu desu ka?",
  "Khi nào bạn phải đến trường?", "", "obj_read" if False else "", choices=[
    C("明日の朝九時です。", "Là 9 giờ sáng mai.", "n_message_ok", "Vocabulary", 10, "Hiểu đúng thời gian trong tin nhắn: 明日の朝九時", ["grammar.n5.ni_time"], ["vocab.n5.ashita", "vocab.n5.kuji"]),
    C("今日の夜九時です。", "Là 9 giờ tối nay.", "n_message_wrong", "Vocabulary", -8, "Nhầm 明日の朝 (sáng mai) với 今日の夜 (tối nay)"),
    C("来週の月曜日です。", "Là thứ Hai tuần sau.", "n_message_wrong", "Vocabulary", -8, "Nhầm 明日 (ngày mai) với 来週 (tuần sau)"),
])
N("n_message_wrong", NAR, "もう一度、メッセージを読みましょう。「明日の朝九時に」と書いてあります。", "もういちど、メッセージをよみましょう。「あしたのあさくじに」とかいてあります。", "Mou ichido, messeeji wo yomimashou. \"Ashita no asa kuji ni\" to kaite arimasu.",
  "Đọc lại tin nhắn nào. Trong đó viết 「明日の朝九時に」 (9 giờ sáng mai).", "", "n_message_q")
N("n_message_ok", NAR, "そうです。明日の朝九時。忘れないように、スマホにメモしましょう。", "そうです。あしたのあさくじ。わすれないように、スマホにメモしましょう。", "Sou desu. Ashita no asa kuji. Wasurenai you ni, sumaho ni memo shimashou.",
  "Đúng rồi. 9 giờ sáng mai. Để khỏi quên, hãy ghi vào điện thoại nhé.", "", "n_reply", "obj_read")

# ───────── Trả lời tin nhắn ─────────
N("n_reply", NAR, "学校に返事を送ります。何と書きますか。", "がっこうにへんじをおくります。なんとかきますか。", "Gakkou ni henji wo okurimasu. Nan to kakimasu ka?",
  "Bạn gửi tin trả lời cho trường. Bạn sẽ viết gì?", "", choices=[
    C("分かりました。よろしくお願いします。", "Tôi đã hiểu. Xin nhờ thầy cô giúp đỡ ạ.", "n_reply_ok", "Grammar", 10, "Trả lời lịch sự bằng 分かりました và よろしくお願いします", ["grammar.n5.masu_past", "grammar.n5.onegaishimasu"], ["vocab.n5.wakarimashita", "vocab.n5.yoroshiku"], ["intro.polite_reply"]),
    C("OK。", "OK.", "n_reply_casual", "ResponseAccuracy", 2, "Ý đúng nhưng quá suồng sã khi nhắn cho nhà trường", [], []),
    C("分かりません。", "Tôi không hiểu.", "n_reply_wrong", "ResponseAccuracy", -8, "Trả lời không hiểu trong khi bạn đã đọc và hiểu tin nhắn"),
])
N("n_reply_casual", NAR, "友達には「OK」でいいですが、学校には「分かりました」と書きましょう。", "ともだちには「オーケー」でいいですが、がっこうには「わかりました」とかきましょう。", "Tomodachi ni wa \"OK\" de ii desu ga, gakkou ni wa \"wakarimashita\" to kakimashou.",
  "Với bạn bè thì \"OK\" là được, nhưng với nhà trường hãy viết 「分かりました」 nhé.", "", "n_walk")
N("n_reply_wrong", NAR, "でも、もう分かりましたよね。「分かりました」と書きましょう。", "でも、もうわかりましたよね。「わかりました」とかきましょう。", "Demo, mou wakarimashita yo ne. \"Wakarimashita\" to kakimashou.",
  "Nhưng bạn đã hiểu rồi mà. Hãy viết 「分かりました」 nhé.", "", "n_reply")
N("n_reply_ok", NAR, "すぐに返事が来ました。「ありがとうございます。あしたお会いしましょう。」", "すぐにへんじがきました。「ありがとうございます。あしたおあいしましょう。」", "Sugu ni henji ga kimashita. \"Arigatou gozaimasu. Ashita oai shimashou.\"",
  "Tin trả lời đến ngay. \"Cảm ơn bạn. Ngày mai chúng ta gặp nhau nhé.\"", "", "n_walk")

# ───────── Đường về nhà, tiếng vọng của Sakura-dōri ─────────
N("n_walk", NAR, "さくら通りを歩きます。パン屋さんのいいにおい。犬の散歩をする人。", "さくらどおりをあるきます。パンやさんのいいにおい。いぬのさんぽをするひと。", "Sakura-doori wo arukimasu. Panya-san no ii nioi. Inu no sanpo wo suru hito.",
  "Bạn đi bộ trên phố Sakura. Mùi bánh mì thơm từ tiệm bánh. Có người dắt chó đi dạo.", "", "n_greet_choice")
N("n_greet_choice", NAR, "犬を連れたおばあさんと目が合いました。ほほえんでいます。どうしますか。", "いぬをつれたおばあさんとめがあいました。ほほえんでいます。どうしますか。", "Inu wo tsureta obaasan to me ga aimashita. Hohoende imasu. Dou shimasu ka?",
  "Ánh mắt bạn chạm vào một bà cụ dắt chó. Bà đang mỉm cười. Bạn sẽ làm gì?", "", choices=[
    C("「こんばんは」と言います。", "Bạn nói \"konbanwa\".", "n_greet_ok", "Vocabulary", 10, "Chào đúng buổi tối bằng こんばんは", [], ["vocab.n5.konbanwa"], ["intro.greeted_stranger"]),
    C("頭を下げます。", "Bạn cúi đầu chào.", "n_greet_bow", "ResponseAccuracy", 6, "Cúi chào là lời chào không lời rất tự nhiên ở Nhật", [], []),
    C("見ないで、通ります。", "Bạn lờ đi rồi đi qua.", "n_greet_pass", "ResponseAccuracy", -4, "Bỏ qua một lời chào thân thiện: ở khu phố nhỏ người ta thường chào nhau"),
])
N("n_greet_ok", NAR, "おばあさんは「こんばんは。いい夕方ですね」と言って、笑いました。", "おばあさんは「こんばんは。いいゆうがたですね」といって、わらいました。", "Obaasan wa \"Konbanwa. Ii yuugata desu ne\" to itte, waraimashita.",
  "Bà cụ nói \"Chào buổi tối. Chiều nay thật đẹp nhỉ\" rồi mỉm cười.", "", "n_home")
N("n_greet_bow", NAR, "おばあさんも頭を下げました。言葉がなくても、気持ちは伝わります。", "おばあさんもあたまをさげました。ことばがなくても、きもちはつたわります。", "Obaasan mo atama wo sagemashita. Kotoba ga nakute mo, kimochi wa tsutawarimasu.",
  "Bà cụ cũng cúi đầu. Dù không có lời nào, tấm lòng vẫn được truyền đến.", "", "n_home")
N("n_greet_pass", NAR, "おばあさんは少しさびしそうでした。明日は、自分からあいさつしてみましょう。", "おばあさんはすこしさびしそうでした。あしたは、じぶんからあいさつしてみましょう。", "Obaasan wa sukoshi sabishisou deshita. Ashita wa, jibun kara aisatsu shite mimashou.",
  "Bà cụ có vẻ hơi buồn. Ngày mai hãy thử chủ động chào trước nhé.", "", "n_home")

# ───────── Về nhà, đêm đầu tiên ─────────
N("n_home", NAR, "小さな部屋のドアを開けました。電気をつけると、白い机とベッドがあります。", "ちいさなへやのドアをあけました。でんきをつけると、しろいつくえとベッドがあります。", "Chiisana heya no doa wo akemashita. Denki wo tsukeru to, shiroi tsukue to beddo ga arimasu.",
  "Bạn mở cửa căn phòng nhỏ. Bật đèn lên, có một chiếc bàn trắng và một chiếc giường.", "", "n_home_q")
N("n_home_q", NAR, "「ただいま」と言ってみますか。", "「ただいま」といってみますか。", "\"Tadaima\" to itte mimasu ka?",
  "Bạn có thử nói \"tadaima\" (con/tôi về rồi) không?", "", choices=[
    C("「ただいま」と言います。", "Bạn nói \"tadaima\".", "n_home_tadaima", "Vocabulary", 8, "ただいま là lời nói khi về đến nhà, kể cả nhà trống", [], ["vocab.n5.tadaima"], ["intro.said_tadaima"]),
    C("何も言いません。", "Bạn không nói gì.", "n_home_silent", "ResponseAccuracy", 0, "Không sao, nhưng ở Nhật người ta hay nói ただいま khi về nhà", [], []),
])
N("n_home_tadaima", NAR, "だれもいません。でも、小さな声で言うと、少し、ほっとしました。", "だれもいません。でも、ちいさなこえでいうと、すこし、ほっとしました。", "Dare mo imasen. Demo, chiisana koe de iu to, sukoshi, hotto shimashita.",
  "Không có ai cả. Nhưng nói bằng giọng nhỏ, bạn thấy yên lòng hơn một chút.", "", "n_end")
N("n_home_silent", NAR, "部屋は静かです。でも、明日から、この町がにぎやかになります。", "へやはしずかです。でも、あしたから、このまちがにぎやかになります。", "Heya wa shizuka desu. Demo, ashita kara, kono machi ga nigiyaka ni narimasu.",
  "Căn phòng yên tĩnh. Nhưng từ ngày mai, thị trấn này sẽ trở nên náo nhiệt.", "", "n_end")

# ───────── Kết ─────────
N("n_end", NAR, "おやすみなさい。明日から、ひばり町の生活が始まります。", "おやすみなさい。あしたから、ひばりちょうのせいかつがはじまります。", "Oyasuminasai. Ashita kara, Hibari-chou no seikatsu ga hajimarimasu.",
  "Chúc ngủ ngon. Từ ngày mai, cuộc sống ở Hibari-chō bắt đầu.", "", "n_complete", "obj_home")
N("n_complete", NAR, "", "", "", "", "", "", type=4)

# n_message_q has an objective placeholder; keep it clean
for n in S.nodes:
    if n["id"] == "n_message_q":
        n["obj"] = ""

objectives = [
    ("obj_arrive", "駅を出て、町に着く", "Ra khỏi ga và đến thị trấn", False),
    ("obj_read", "学校のメッセージを読む", "Đọc tin nhắn của trường", False),
    ("obj_home", "部屋に着く", "Về đến căn phòng của mình", False),
]
targets = ["vocab.n5.deguchi", "vocab.n5.ashita", "vocab.n5.konbanwa", "vocab.n5.wakarimashita", "vocab.n5.yoroshiku"]

problems = S.validate("n_open", objectives)
print("problems:", problems)
if not problems:
    S.write(sys.argv[1], name="scenario_intro_arrival", sid="scenario.intro.arrival", title_ja="ひばり町へようこそ", title_vi="Chào mừng đến Hibari-chō",
            desc_ja="ひばり駅に着いた夜。看板を読み、学校のメッセージに返事をして、部屋に帰りましょう。", desc_vi="Đêm đầu tiên ở Hibari: đọc biển ga, hiểu tin nhắn của trường, chào người dân và về nhà.",
            chapter=0, targets=targets, objectives=objectives, start="n_open")
    print("nodes", len(S.nodes))
