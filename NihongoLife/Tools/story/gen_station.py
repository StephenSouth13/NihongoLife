# -*- coding: utf-8 -*-
import sys

from scen_lib import Scenario

S = Scenario()
C, N = S.C, S.N
KM = ("Kimura", "npc_station_staff")
NAR = None

N("n_arrive", NAR, "ひばり駅です。今日は、電車に乗って、ミナト駅まで海を見に行きます。", "ひばりえきです。きょうは、でんしゃにのって、ミナトえきまでうみをみにいきます。", "Hibari eki desu. Kyou wa, densha ni notte, Minato eki made umi wo mi ni ikimasu.",
  "Ga Hibari. Hôm nay bạn sẽ đi tàu đến ga Minato để ngắm biển.", "", "n_counter", "obj_arrive")

# ───────── Hỏi nhân viên nhà ga (〜たいです) ─────────
N("n_counter", KM, "いらっしゃいませ。ご案内しましょうか。", "いらっしゃいませ。ごあんないしましょうか。", "Irasshaimase. Goannai shimashou ka.",
  "Xin chào quý khách. Tôi hướng dẫn giúp quý khách nhé?", "talk", choices=[
    C("ミナト駅へ行きたいです。", "Tôi muốn đến ga Minato ạ.", "n_fare", "Grammar", 10, "Nói mong muốn bằng 〜たいです và nơi đến bằng 〜へ", ["grammar.n5.tai_desu", "grammar.n5.e_ikimasu"], ["vocab.n5.eki", "vocab.n5.minato"]),
    C("ミナト駅へ行きます。", "Tôi sẽ đi đến ga Minato.", "n_counter_flat", "ResponseAccuracy", 3, "Nói được kế hoạch nhưng chưa nhờ giúp; muốn được hướng dẫn nên dùng 〜たいです", ["grammar.n5.e_ikimasu"], ["vocab.n5.eki"]),
    C("ミナト駅、どこ？", "Ga Minato, ở đâu?", "n_counter_wrong", "ResponseAccuracy", -6, "Hỏi cộc lốc với nhân viên, thiếu ですか và không nêu rõ điều mình cần"),
])
N("n_counter_flat", KM, "はい、ミナト駅ですね。切符が必要ですか。「行きたいです」と言うと、分かりやすいですよ。", "はい、ミナトえきですね。きっぷがひつようですか。「いきたいです」というと、わかりやすいですよ。", "Hai, Minato eki desu ne. Kippu ga hitsuyou desu ka. \"Ikitai desu\" to iu to, wakariyasui desu yo.",
  "Vâng, ga Minato nhỉ. Quý khách cần vé không ạ? Nói \"ikitai desu\" (tôi muốn đi) thì dễ hiểu hơn đấy ạ.", "talk", "n_fare")
N("n_counter_wrong", KM, "失礼ですが、「ミナト駅へ行きたいです」と、お話しください。", "しつれいですが、「ミナトえきへいきたいです」と、おはなしください。", "Shitsurei desu ga, \"Minato eki e ikitai desu\" to, ohanashi kudasai.",
  "Xin thất lễ, xin quý khách hãy nói \"Minato eki e ikitai desu\" (tôi muốn đến ga Minato) ạ.", "point", "n_counter")

# ───────── Giá vé (số tiền, cách đọc) ─────────
N("n_fare", KM, "ミナト駅までは、三百二十円です。二十分ぐらいです。", "ミナトえきまでは、さんびゃくにじゅうえんです。にじゅっぷんぐらいです。", "Minato eki made wa, sanbyaku nijuu en desu. Nijuppun gurai desu.",
  "Đến ga Minato giá 320 yên. Mất khoảng 20 phút.", "talk", "n_fare_check", "obj_ask")
N("n_fare_check", KM, "おわかりですか。ミナト駅までは、いくらですか。", "おわかりですか。ミナトえきまでは、いくらですか。", "Owakari desu ka. Minato eki made wa, ikura desu ka?",
  "Quý khách đã rõ chưa ạ? Đến ga Minato là bao nhiêu tiền?", "talk", choices=[
    C("三百二十円です。", "Là 320 yên ạ.", "n_machine", "Vocabulary", 10, "Nhắc lại đúng giá vé: 三百二十円 (さんびゃくにじゅうえん)", ["grammar.n5.ikura_desu_ka"], ["vocab.n5.sanbyaku", "vocab.n5.nijuu"]),
    C("二百三十円です。", "Là 230 yên ạ.", "n_fare_wrong", "Vocabulary", -8, "Nhầm thứ tự chữ số: 三百二十 (320) khác 二百三十 (230)"),
    C("もう一度お願いします。", "Phiền anh nói lại một lần nữa ạ.", "n_fare_repeat", "ResponseAccuracy", 8, "Xin nghe lại lịch sự để chắc chắn về số tiền", [], ["vocab.n5.mou_ichido"]),
])
N("n_fare_wrong", KM, "三百二十円です。三、百、二、十。もう一度、お願いします。", "さんびゃくにじゅうえんです。さん、ひゃく、に、じゅう。もういちど、おねがいします。", "Sanbyaku nijuu en desu. San, hyaku, ni, juu. Mou ichido, onegaishimasu.",
  "Là 320 yên. Ba, trăm, hai, mươi. Xin quý khách nói lại ạ.", "point", "n_fare_check")
N("n_fare_repeat", KM, "はい。三百二十円です。三百と、二十です。", "はい。さんびゃくにじゅうえんです。さんびゃくと、にじゅうです。", "Hai. Sanbyaku nijuu en desu. Sanbyaku to, nijuu desu.",
  "Vâng. 320 yên. Ba trăm và hai mươi ạ.", "point", "n_fare_check")

# ───────── Máy bán vé ─────────
N("n_machine", KM, "切符は、あの券売機で買います。まず、お金を入れます。それから、行き先のボタンを押します。", "きっぷは、あのけんばいきでかいます。まず、おかねをいれます。それから、いきさきのボタンをおします。", "Kippu wa, ano kenbaiki de kaimasu. Mazu, okane wo iremasu. Sorekara, ikisaki no botan wo oshimasu.",
  "Vé thì mua ở máy bán vé kia. Trước tiên, bỏ tiền vào. Sau đó, bấm nút điểm đến.", "point", "n_machine_q")
N("n_machine_q", NAR, "券売機の前に来ました。最初に、何をしますか。", "けんばいきのまえにきました。さいしょに、なにをしますか。", "Kenbaiki no mae ni kimashita. Saisho ni, nani wo shimasu ka?",
  "Bạn đứng trước máy bán vé. Việc đầu tiên bạn làm là gì?", "", choices=[
    C("まず、お金を入れます。", "Trước tiên tôi bỏ tiền vào.", "n_machine_ok", "Grammar", 10, "Nói đúng thứ tự thao tác bằng まず〜、それから〜", ["grammar.n5.mazu_sorekara"], ["vocab.n5.okane", "vocab.n5.ireru"]),
    C("まず、ボタンを押します。", "Trước tiên tôi bấm nút.", "n_machine_wrong", "Grammar", -8, "Sai thứ tự: máy này cần bỏ tiền vào trước rồi mới bấm nút điểm đến"),
])
N("n_machine_wrong", KM, "ボタンは、お金を入れてからです。まず、お金を入れてください。", "ボタンは、おかねをいれてからです。まず、おかねをいれてください。", "Botan wa, okane wo irete kara desu. Mazu, okane wo irete kudasai.",
  "Nút chỉ bấm sau khi bỏ tiền. Trước tiên xin hãy bỏ tiền vào ạ.", "point", "n_machine_q")
N("n_machine_ok", NAR, "五百円玉を入れて、「ミナト」のボタンを押しました。切符と、おつりの百八十円が出ました。", "ごひゃくえんだまをいれて、「ミナト」のボタンをおしました。きっぷと、おつりのひゃくはちじゅうえんがでました。", "Gohyaku en dama wo irete, \"Minato\" no botan wo oshimashita. Kippu to, otsuri no hyaku hachijuu en ga demashita.",
  "Bạn bỏ đồng 500 yên vào rồi bấm nút 「ミナト」. Vé và tiền thừa 180 yên được nhả ra.", "", "n_platform_ask", "obj_ticket")

# ───────── Sân ga và giờ tàu ─────────
N("n_platform_ask", KM, "何か、ご質問はありますか。", "なにか、ごしつもんはありますか。", "Nani ka, goshitsumon wa arimasu ka?",
  "Quý khách còn câu hỏi nào không ạ?", "talk", choices=[
    C("何番線ですか。", "Là đường ray số mấy ạ?", "n_platform_answer", "Grammar", 10, "Hỏi số đường ray bằng 何番線ですか", ["grammar.n5.nan_ban_sen"], ["vocab.n5.bansen"]),
    C("電車は、どこですか。", "Tàu ở đâu ạ?", "n_platform_vague", "ResponseAccuracy", 3, "Hiểu được nhưng quá mơ hồ: nên hỏi cụ thể 何番線 (đường ray số mấy)", ["grammar.n5.wa_doko_desu_ka"], ["vocab.n5.densha"]),
    C("トイレは、どこですか。", "Nhà vệ sinh ở đâu ạ?", "n_platform_wrong", "ResponseAccuracy", -4, "Câu hỏi hợp lệ nhưng không phải điều bạn cần lúc này; hãy hỏi đường ray trước để khỏi lỡ tàu"),
])
N("n_platform_vague", KM, "ミナト行きは、二番線です。次は、「何番線ですか」と聞いてくださいね。", "ミナトゆきは、にばんせんです。つぎは、「なんばんせんですか」ときいてくださいね。", "Minato yuki wa, nibansen desu. Tsugi wa, \"nanbansen desu ka\" to kiite kudasai ne.",
  "Tàu đi Minato ở đường ray số 2. Lần sau hãy hỏi \"nanbansen desu ka\" (đường ray số mấy) nhé.", "talk", "n_platform_check")
N("n_platform_wrong", KM, "トイレは、あちらです。ミナト行きは、二番線ですよ。", "トイレは、あちらです。ミナトゆきは、にばんせんですよ。", "Toire wa, achira desu. Minato yuki wa, nibansen desu yo.",
  "Nhà vệ sinh ở phía kia ạ. Tàu đi Minato ở đường ray số 2 đấy ạ.", "point", "n_platform_check")
N("n_platform_answer", KM, "ミナト行きは、二番線です。階段を上がってください。", "ミナトゆきは、にばんせんです。かいだんをあがってください。", "Minato yuki wa, nibansen desu. Kaidan wo agatte kudasai.",
  "Tàu đi Minato ở đường ray số 2. Xin hãy đi lên cầu thang ạ.", "point", "n_platform_check")
N("n_platform_check", KM, "ミナト行きは、何番線ですか。", "ミナトゆきは、なんばんせんですか。", "Minato yuki wa, nanbansen desu ka?",
  "Tàu đi Minato ở đường ray số mấy nhỉ?", "talk", "", "obj_platform", choices=[
    C("二番線です。", "Số 2 ạ.", "n_time", "Vocabulary", 10, "Nhớ đúng số đường ray: 二番線", [], ["vocab.n5.nibansen"]),
    C("三番線です。", "Số 3 ạ.", "n_platform_wrong2", "Vocabulary", -8, "Nhầm số đường ray: tàu đi Minato ở số 2, không phải số 3"),
])
N("n_platform_wrong2", KM, "三番線は、反対の方向です。ミナト行きは、二番線ですよ。", "さんばんせんは、はんたいのほうこうです。ミナトゆきは、にばんせんですよ。", "Sanbansen wa, hantai no houkou desu. Minato yuki wa, nibansen desu yo.",
  "Đường ray số 3 đi hướng ngược lại. Tàu đi Minato là đường ray số 2 ạ.", "point", "n_platform_check")

N("n_time", KM, "次の電車は、十時十五分です。あと五分ですよ。", "つぎのでんしゃは、じゅうじじゅうごふんです。あとごふんですよ。", "Tsugi no densha wa, juuji juugo fun desu. Ato gofun desu yo.",
  "Chuyến tàu tiếp theo là 10 giờ 15 phút. Còn 5 phút nữa thôi ạ.", "talk", choices=[
    C("十時十五分ですね。ありがとうございます。", "10 giờ 15 phút nhỉ. Cảm ơn anh ạ.", "n_announce", "Vocabulary", 10, "Xác nhận giờ tàu bằng cách nhắc lại: 十時十五分", ["grammar.n5.desu_ne"], ["vocab.n5.juuji", "vocab.n5.fun"]),
    C("十一時十五分ですね。", "11 giờ 15 phút nhỉ.", "n_time_wrong", "Vocabulary", -8, "Nghe nhầm giờ: 十時 (10 giờ) khác 十一時 (11 giờ)"),
    C("何時ですか。", "Mấy giờ ạ?", "n_time", "ResponseAccuracy", 3, "Hỏi lại được nhưng vừa nghe rõ giờ rồi, nên nhắc lại để xác nhận", [], ["vocab.n5.nanji"]),
])
N("n_time_wrong", KM, "十時十五分です。十一時ではありません。あと五分ですよ。", "じゅうじじゅうごふんです。じゅういちじではありません。あとごふんですよ。", "Juuji juugo fun desu. Juuichiji dewa arimasen. Ato gofun desu yo.",
  "Là 10 giờ 15 phút, không phải 11 giờ. Còn 5 phút nữa thôi ạ.", "point", "n_time")

# ───────── Thông báo tại sân ga (nghe hiểu) ─────────
N("n_announce", NAR, "ホームに立ちました。アナウンスが流れます。「まもなく、二番線に、ミナト行きが参ります。あぶないですから、白い線の内側まで、お下がりください。」", "ホームにたちました。アナウンスがながれます。「まもなく、にばんせんに、ミナトゆきがまいります。あぶないですから、しろいせんのうちがわまで、おさがりください。」", "Hoomu ni tachimashita. Anaunsu ga nagaremasu. \"Mamonaku, nibansen ni, Minato yuki ga mairimasu. Abunai desu kara, shiroi sen no uchigawa made, osagari kudasai.\"",
  "Bạn đứng trên sân ga. Có thông báo vang lên: \"Chỉ ít phút nữa, tàu đi Minato sẽ vào đường ray số 2. Vì nguy hiểm, xin quý khách lùi vào trong vạch trắng.\"", "", "n_announce_q")
N("n_announce_q", NAR, "アナウンスで、何と言いましたか。どうしますか。", "アナウンスで、なんといいましたか。どうしますか。", "Anaunsu de, nan to iimashita ka. Dou shimasu ka?",
  "Thông báo nói gì? Bạn sẽ làm gì?", "", choices=[
    C("白い線の後ろに下がります。", "Tôi lùi ra phía sau vạch trắng.", "n_train", "Vocabulary", 10, "Hiểu đúng thông báo an toàn: お下がりください = lùi ra sau vạch trắng", ["grammar.n5.te_kudasai"], ["vocab.n5.shiroi_sen", "vocab.n5.sagaru"], ["station.safety_ok"]),
    C("線の前に立ちます。", "Tôi đứng sát trước vạch.", "n_announce_wrong", "Vocabulary", -10, "Hiểu ngược thông báo: đứng sát vạch là nguy hiểm khi tàu vào ga"),
    C("もう一度、聞きます。", "Tôi nghe lại lần nữa.", "n_announce_repeat", "ResponseAccuracy", 4, "Muốn nghe lại thông báo là hợp lý nhưng tàu sắp vào, hãy lùi lại cho an toàn", [], []),
])
N("n_announce_wrong", NAR, "あぶない！電車が来ます。「お下がりください」は、後ろに下がってください、という意味です。", "あぶない！でんしゃがきます。「おさがりください」は、うしろにさがってください、といういみです。", "Abunai! Densha ga kimasu. \"Osagari kudasai\" wa, ushiro ni sagatte kudasai, to iu imi desu.",
  "Nguy hiểm! Tàu sắp vào. 「お下がりください」 nghĩa là hãy lùi ra phía sau.", "", "n_announce_q")
N("n_announce_repeat", NAR, "アナウンスは、もう終わりました。電車が来ます。白い線の後ろに下がりましょう。", "アナウンスは、もうおわりました。でんしゃがきます。しろいせんのうしろにさがりましょう。", "Anaunsu wa, mou owarimashita. Densha ga kimasu. Shiroi sen no ushiro ni sagarimashou.",
  "Thông báo đã kết thúc. Tàu sắp vào rồi. Hãy lùi ra sau vạch trắng nào.", "", "n_announce_q")

# ───────── Trên tàu: nhường ghế (văn hoá) ─────────
N("n_train", NAR, "電車は、すいています。でも、次の駅で、お年寄りが乗ってきました。優先席の前に、立っています。", "でんしゃは、すいています。でも、つぎのえきで、おとしよりがのってきました。ゆうせんせきのまえに、たっています。", "Densha wa, suite imasu. Demo, tsugi no eki de, otoshiyori ga notte kimashita. Yuusenseki no mae ni, tatte imasu.",
  "Tàu khá vắng. Nhưng ở ga tiếp theo, một cụ già bước lên. Cụ đang đứng trước ghế ưu tiên.", "", "n_seat_choice")
N("n_seat_choice", NAR, "あなたは、席に座っています。どうしますか。", "あなたは、せきにすわっています。どうしますか。", "Anata wa, seki ni suwatte imasu. Dou shimasu ka?",
  "Bạn đang ngồi trên ghế. Bạn sẽ làm gì?", "", choices=[
    C("「どうぞ、座ってください」と言います。", "Bạn nói \"douzo, suwatte kudasai\" (mời cụ ngồi).", "n_seat_yes", "Grammar", 10, "Nhường ghế bằng lời mời lịch sự どうぞ、座ってください", ["grammar.n5.te_kudasai"], ["vocab.n5.douzo", "vocab.n5.suwaru"], ["station.gave_seat"]),
    C("寝たふりをします。", "Bạn giả vờ ngủ.", "n_seat_no", "ResponseAccuracy", -6, "Tránh né trong khi có người cần chỗ ngồi hơn mình"),
    C("スマホを見ます。", "Bạn cúi xuống xem điện thoại.", "n_seat_no", "ResponseAccuracy", -4, "Không để ý đến người xung quanh, dù có thể giúp"),
])
N("n_seat_yes", NAR, "お年寄りは、「ありがとう。やさしい人ですね」と言って、ほほえみました。", "おとしよりは、「ありがとう。やさしいひとですね」といって、ほほえみました。", "Otoshiyori wa, \"Arigatou. Yasashii hito desu ne\" to itte, hohoemimashita.",
  "Cụ già nói \"Cảm ơn cháu. Cháu thật là người tốt bụng\" rồi mỉm cười.", "", "n_minato")
N("n_seat_no", NAR, "お年寄りは、少しつらそうです。次の駅で、ほかの人が席をゆずりました。あなたも、次は、声をかけてみましょう。", "おとしよりは、すこしつらそうです。つぎのえきで、ほかのひとがせきをゆずりました。あなたも、つぎは、こえをかけてみましょう。", "Otoshiyori wa, sukoshi tsurasou desu. Tsugi no eki de, hoka no hito ga seki wo yuzurimashita. Anata mo, tsugi wa, koe wo kakete mimashou.",
  "Cụ già trông có vẻ hơi mệt. Ở ga tiếp theo, một người khác đã nhường ghế. Lần sau bạn cũng hãy thử lên tiếng nhé.", "", "n_minato")

N("n_minato", NAR, "ミナト駅に着きました。ドアが開くと、海のにおいがします。", "ミナトえきにつきました。ドアがあくと、うみのにおいがします。", "Minato eki ni tsukimashita. Doa ga aku to, umi no nioi ga shimasu.",
  "Bạn đến ga Minato. Cửa tàu mở ra, mùi biển ùa vào.", "", "n_last", "obj_board")
N("n_last", NAR, "帰りの最終電車は、夜の十一時です。忘れないでくださいね。", "かえりのさいしゅうでんしゃは、よるのじゅういちじです。わすれないでくださいね。", "Kaeri no saishuu densha wa, yoru no juuichiji desu. Wasurenai de kudasai ne.",
  "Chuyến tàu cuối để về là lúc 11 giờ đêm. Đừng quên nhé.", "", "n_recap")
N("n_recap", NAR, "今日のポイント：「〜たいです」「何番線ですか」「まず、それから」。", "きょうのポイント：「〜たいです」「なんばんせんですか」「まず、それから」。", "Kyou no pointo: \"~ tai desu\" \"nanbansen desu ka\" \"mazu, sorekara\".",
  "Tóm tắt: \"~ tai desu\" (tôi muốn ~), \"nanbansen desu ka\" (đường ray số mấy?), \"mazu, sorekara\" (trước tiên..., sau đó...). Ở ga: lùi ra sau vạch trắng, nhường ghế cho người cần.", "", "n_complete")
N("n_complete", NAR, "", "", "", "", "", "", type=4)

objectives = [
    ("obj_arrive", "駅に着く", "Đến nhà ga", False),
    ("obj_ask", "駅員さんに行き先を伝える", "Nói với nhân viên nhà ga nơi bạn muốn đến", False),
    ("obj_ticket", "券売機で切符を買う", "Mua vé ở máy bán vé", False),
    ("obj_platform", "ホームを確認する", "Xác nhận đúng đường ray", False),
    ("obj_board", "電車に乗ってミナト駅に着く", "Lên tàu và đến ga Minato", False),
]
targets = ["grammar.n5.tai_desu", "grammar.n5.nan_ban_sen", "grammar.n5.mazu_sorekara", "vocab.n5.densha", "vocab.n5.kippu"]

problems = S.validate("n_arrive", objectives)
print("problems:", problems)
if not problems:
    S.write(sys.argv[1], name="scenario_station_buy_ticket", sid="scenario.station.buy_ticket", title_ja="駅で切符を買う", title_vi="Mua vé và đi tàu ở ga Hibari",
            desc_ja="ひばり駅で切符を買って、ミナト駅まで行きましょう。駅員さんに聞いて、ホームでアナウンスを聞き取ります。", desc_vi="Mua vé tại ga Hibari để đến Minato: nói nơi muốn đến, đọc giá vé, dùng máy bán vé, hỏi đường ray, nghe thông báo và cư xử đúng trên tàu.",
            chapter=4, targets=targets, objectives=objectives, start="n_arrive")
    print("nodes", len(S.nodes))
