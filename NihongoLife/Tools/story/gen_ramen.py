# -*- coding: utf-8 -*-
import sys

from scen_lib import Scenario

S = Scenario()
C, N = S.C, S.N
TA = ("Tanaka", "npc_neighbor_1")
YA = ("Yamada", "npc_ramen_owner")
NAR = None

N("node_find_tanaka", None, "", "", "", "", "", "n_invite", "obj_meet", type=6, npc="npc_neighbor_1")

# ───────── Tanaka rủ đi ăn ─────────
N("n_invite", TA, "おなかがすきましたね。ラーメンを食べに行きませんか。おいしい店を知っていますよ。", "おなかがすきましたね。ラーメンをたべにいきませんか。おいしいみせをしっていますよ。", "Onaka ga sukimashita ne. Raamen wo tabe ni ikimasen ka. Oishii mise wo shitte imasu yo.",
  "Đói bụng rồi nhỉ. Mình đi ăn ramen nhé? Tôi biết một quán rất ngon đấy.", "talk", choices=[
    C("はい、ぜひ行きましょう。", "Vâng, nhất định phải đi rồi ạ.", "n_walk", "Grammar", 10, "Nhận lời rủ 〜ませんか bằng はい、行きましょう", ["grammar.n5.masen_ka", "grammar.n5.mashou"], ["vocab.n5.raamen"], ["ramen.invited"]),
    C("はい、行きます。", "Vâng, cháu đi ạ.", "n_walk", "ResponseAccuracy", 5, "Nhận lời đúng nhưng nhấn mạnh sự háo hức thì nên dùng 行きましょう", ["grammar.n5.masen_ka"], ["vocab.n5.raamen"], ["ramen.invited"]),
    C("ラーメンは、食べません。", "Cháu không ăn ramen.", "n_invite_wrong", "ResponseAccuracy", -8, "Từ chối cộc lốc và không cho lý do, khiến lời mời trở nên khó xử"),
])
N("n_invite_wrong", TA, "あ、そうですか。でも、うどんも、ありますよ。もう一度、どうですか。", "あ、そうですか。でも、うどんも、ありますよ。もういちど、どうですか。", "A, sou desu ka. Demo, udon mo, arimasu yo. Mou ichido, dou desu ka.",
  "À, vậy à. Nhưng quán còn có cả udon nữa đấy. Bạn nghĩ lại nhé?", "talk", "n_invite")

N("n_walk", NAR, "さくら通りを歩いて、小さな店の前に着きました。赤いのれんに、「やまだ食堂」と書いてあります。", "さくらどおりをあるいて、ちいさなみせのまえにつきました。あかいのれんに、「やまだしょくどう」とかいてあります。", "Sakura-doori wo aruite, chiisana mise no mae ni tsukimashita. Akai noren ni, \"Yamada shokudou\" to kaite arimasu.",
  "Đi dọc phố Sakura, bạn đến trước một quán nhỏ. Trên tấm rèm đỏ có viết 「やまだ食堂」.", "", "n_sign", "obj_arrive")
N("n_sign", TA, "「食堂」は、ごはんを食べる店です。ここは、山田さんの店ですよ。", "「しょくどう」は、ごはんをたべるみせです。ここは、やまださんのみせですよ。", "\"Shokudou\" wa, gohan wo taberu mise desu. Koko wa, Yamada-san no mise desu yo.",
  "「食堂」 là quán để ăn cơm. Đây là quán của chú Yamada đấy.", "point", "n_door")
N("n_door", YA, "いらっしゃい！あれ、田中さん。今日は、お友達と？", "いらっしゃい！あれ、たなかさん。きょうは、おともだちと？", "Irasshai! Are, Tanaka-san. Kyou wa, otomodachi to?",
  "Xin chào! Ơ kìa, bác Tanaka. Hôm nay đi cùng bạn à?", "talk", choices=[
    C("はじめまして。よろしくお願いします。", "Rất vui được gặp chú. Xin chú giúp đỡ ạ.", "n_ticket_intro", "Vocabulary", 10, "Chào người mới gặp đúng nghi thức bằng はじめまして và よろしくお願いします", [], ["vocab.n5.hajimemashite", "vocab.n5.yoroshiku"], ["yamada.introduced"]),
    C("こんにちは。", "Xin chào ạ.", "n_ticket_intro", "ResponseAccuracy", 4, "Chào được nhưng với người mới gặp nên nói はじめまして", [], ["vocab.n5.konnichiwa"]),
    C("ラーメン！", "Ramen!", "n_door_wrong", "ResponseAccuracy", -6, "Gọi món ngay khi vừa bước vào mà chưa chào hỏi, khá bất lịch sự"),
])
N("n_door_wrong", YA, "ははは、元気だね。でも、まず、あいさつだよ。", "ははは、げんきだね。でも、まず、あいさつだよ。", "Hahaha, genki da ne. Demo, mazu, aisatsu da yo.",
  "Haha, khỏe khoắn nhỉ. Nhưng trước hết phải chào hỏi đã chứ.", "talk", "n_door")

# ───────── Máy bán vé (券売機) ─────────
N("n_ticket_intro", TA, "この店は、先に券売機で、券を買います。メニューは、これです。", "このみせは、さきにけんばいきで、けんをかいます。メニューは、これです。", "Kono mise wa, saki ni kenbaiki de, ken wo kaimasu. Menyuu wa, kore desu.",
  "Quán này phải mua vé ở máy bán vé trước. Thực đơn đây này.", "point", "n_menu")
N("n_menu", NAR, "券売機のボタン：しょうゆ 八百円　みそ 九百円　しお 八百円　ぎょうざ 四百円　大盛り 百円", "けんばいきのボタン：しょうゆ はっぴゃくえん　みそ きゅうひゃくえん　しお はっぴゃくえん　ぎょうざ よんひゃくえん　おおもり ひゃくえん", "Kenbaiki no botan: shouyu happyaku en, miso kyuuhyaku en, shio happyaku en, gyouza yonhyaku en, oomori hyaku en",
  "Các nút của máy: shoyu 800 yên, miso 900 yên, shio 800 yên, gyoza 400 yên, thêm mì (oomori) 100 yên.", "", "n_flavor")
N("n_flavor", TA, "どのラーメンにしますか。私は、しょうゆです。", "どのラーメンにしますか。わたしは、しょうゆです。", "Dono raamen ni shimasu ka. Watashi wa, shouyu desu.",
  "Bạn chọn ramen nào? Tôi chọn vị shoyu.", "talk", choices=[
    C("みそラーメンにします。", "Cháu chọn ramen miso ạ.", "n_pay_miso", "Grammar", 10, "Chọn món bằng mẫu 〜にします", ["grammar.n5.ni_shimasu"], ["vocab.n5.miso"], ["ramen.miso"]),
    C("しょうゆラーメンにします。", "Cháu chọn ramen shoyu ạ.", "n_pay_shoyu", "Grammar", 10, "Chọn món bằng mẫu 〜にします", ["grammar.n5.ni_shimasu"], ["vocab.n5.shouyu"], ["ramen.shoyu"]),
    C("しおラーメンにします。", "Cháu chọn ramen shio ạ.", "n_pay_shoyu", "Grammar", 10, "Chọn món bằng mẫu 〜にします", ["grammar.n5.ni_shimasu"], ["vocab.n5.shio"], ["ramen.shio"]),
    C("ラーメンをいただきます。", "Cháu xin phép dùng ramen ạ.", "n_flavor_wrong", "Grammar", -8, "いただきます là lời nói trước khi ăn, không dùng để chọn món"),
])
N("n_flavor_wrong", TA, "まだ、食べませんよ。「〜にします」と言いましょう。どれにしますか。", "まだ、たべませんよ。「〜にします」といいましょう。どれにしますか。", "Mada, tabemasen yo. \"~ ni shimasu\" to iimashou. Dore ni shimasu ka?",
  "Chưa ăn đâu mà. Hãy nói \"~ ni shimasu\" (tôi chọn ~). Bạn chọn món nào?", "point", "n_flavor")

# tiền: miso + gyoza = 1300; shoyu/shio + gyoza = 1200
N("n_pay_miso", TA, "ぎょうざも食べますか。ぎょうざは四百円です。みそラーメンと、ぜんぶで、いくらですか。", "ぎょうざもたべますか。ぎょうざはよんひゃくえんです。みそラーメンと、ぜんぶで、いくらですか。", "Gyouza mo tabemasu ka. Gyouza wa yonhyaku en desu. Miso raamen to, zenbu de, ikura desu ka?",
  "Bạn có ăn cả gyoza không? Gyoza 400 yên. Cùng với ramen miso thì tổng cộng là bao nhiêu?", "talk", choices=[
    C("千三百円です。", "Là 1.300 yên ạ.", "n_buy", "Vocabulary", 10, "Tính và đọc đúng tổng tiền: 九百円 + 四百円 = 千三百円", ["grammar.n5.ikura_desu_ka"], ["vocab.n5.sen", "vocab.n5.hyaku", "vocab.n5.en"]),
    C("千二百円です。", "Là 1.200 yên ạ.", "n_sum_wrong_miso", "Vocabulary", -8, "Tính sai tổng tiền: ramen miso 900 + gyoza 400 = 1.300, không phải 1.200"),
    C("もう一度お願いします。", "Phiền bác nói lại một lần nữa ạ.", "n_pay_miso", "ResponseAccuracy", 6, "Xin nghe lại lịch sự để tính chính xác", [], ["vocab.n5.mou_ichido"]),
])
N("n_sum_wrong_miso", TA, "みそは九百円、ぎょうざは四百円。九百と四百で、千三百円ですよ。", "みそはきゅうひゃくえん、ぎょうざはよんひゃくえん。きゅうひゃくとよんひゃくで、せんさんびゃくえんですよ。", "Miso wa kyuuhyaku en, gyouza wa yonhyaku en. Kyuuhyaku to yonhyaku de, sen sanbyaku en desu yo.",
  "Miso 900 yên, gyoza 400 yên. 900 cộng 400 là 1.300 yên đấy.", "point", "n_pay_miso")
N("n_pay_shoyu", TA, "ぎょうざも食べますか。ぎょうざは四百円です。ラーメンと、ぜんぶで、いくらですか。", "ぎょうざもたべますか。ぎょうざはよんひゃくえんです。ラーメンと、ぜんぶで、いくらですか。", "Gyouza mo tabemasu ka. Gyouza wa yonhyaku en desu. Raamen to, zenbu de, ikura desu ka?",
  "Bạn có ăn cả gyoza không? Gyoza 400 yên. Cùng với ramen thì tổng cộng là bao nhiêu?", "talk", choices=[
    C("千二百円です。", "Là 1.200 yên ạ.", "n_buy", "Vocabulary", 10, "Tính và đọc đúng tổng tiền: 八百円 + 四百円 = 千二百円", ["grammar.n5.ikura_desu_ka"], ["vocab.n5.sen", "vocab.n5.hyaku", "vocab.n5.en"]),
    C("千三百円です。", "Là 1.300 yên ạ.", "n_sum_wrong_shoyu", "Vocabulary", -8, "Tính sai tổng tiền: ramen 800 + gyoza 400 = 1.200, không phải 1.300"),
    C("もう一度お願いします。", "Phiền bác nói lại một lần nữa ạ.", "n_pay_shoyu", "ResponseAccuracy", 6, "Xin nghe lại lịch sự để tính chính xác", [], ["vocab.n5.mou_ichido"]),
])
N("n_sum_wrong_shoyu", TA, "ラーメンは八百円、ぎょうざは四百円。八百と四百で、千二百円ですよ。", "ラーメンははっぴゃくえん、ぎょうざはよんひゃくえん。はっぴゃくとよんひゃくで、せんにひゃくえんですよ。", "Raamen wa happyaku en, gyouza wa yonhyaku en. Happyaku to yonhyaku de, sen nihyaku en desu yo.",
  "Ramen 800 yên, gyoza 400 yên. 800 cộng 400 là 1.200 yên đấy.", "point", "n_pay_shoyu")
N("n_buy", NAR, "お金を入れて、ボタンを押しました。券が二枚、出てきました。", "おかねをいれて、ボタンをおしました。けんがにまい、でてきました。", "Okane wo irete, botan wo oshimashita. Ken ga nimai, dete kimashita.",
  "Bạn bỏ tiền vào rồi bấm nút. Hai tấm vé được nhả ra.", "", "n_counter", "obj_ticket")

# ───────── Quầy: độ cứng của mì ─────────
N("n_counter", YA, "はい、券をお願いします。麺の硬さは、どうする？", "はい、けんをおねがいします。めんのかたさは、どうする？", "Hai, ken wo onegaishimasu. Men no katasa wa, dou suru?",
  "Vâng, cho tôi xin vé nhé. Mì thì để độ cứng thế nào?", "talk", choices=[
    C("ふつうでお願いします。", "Cho cháu độ cứng bình thường ạ.", "n_water", "Grammar", 10, "Chọn bằng mẫu 〜でお願いします (đã học ở nhà hàng sushi)", ["grammar.n5.de_onegaishimasu"], ["vocab.n5.futsuu"]),
    C("かためでお願いします。", "Cho cháu mì cứng vừa ạ.", "n_water", "Grammar", 10, "Chọn bằng mẫu 〜でお願いします", ["grammar.n5.de_onegaishimasu"], ["vocab.n5.katame"]),
    C("かたいです。", "Cứng.", "n_counter_wrong", "Grammar", -6, "かたいです chỉ mô tả tính chất, không phải cách yêu cầu độ cứng khi gọi món"),
])
N("n_counter_wrong", YA, "ははは、「〜でお願いします」だよ。もう一度！", "ははは、「〜でおねがいします」だよ。もういちど！", "Hahaha, \"~ de onegaishimasu\" da yo. Mou ichido!",
  "Haha, phải nói \"~ de onegaishimasu\" chứ. Nói lại đi!", "point", "n_counter")
N("n_water", TA, "お水は、セルフです。あそこにありますよ。", "おみずは、セルフです。あそこにありますよ。", "Omizu wa, serufu desu. Asoko ni arimasu yo.",
  "Nước thì tự phục vụ. Ở đằng kia đấy.", "point", choices=[
    C("ありがとうございます。私が入れます。", "Cảm ơn bác. Để cháu rót ạ.", "n_eat", "Grammar", 10, "Đề nghị tự làm bằng 私が〜ます, phù hợp với quán tự phục vụ", ["grammar.n5.ga_masu"], ["vocab.n5.omizu", "vocab.n5.serufu"]),
    C("お水は、どこですか。", "Nước ở đâu ạ?", "n_water_again", "Grammar", 3, "Đã được chỉ chỗ 「あそこ」 rồi mà vẫn hỏi lại", ["grammar.n5.wa_doko_desu_ka"], ["vocab.n5.omizu"]),
    C("店員さん、お水をください。", "Nhân viên ơi, cho tôi nước.", "n_water_wrong", "ResponseAccuracy", -6, "Nhờ phục vụ trong khi quán này để khách tự lấy nước"),
])
N("n_water_again", TA, "ふふ、あそこですよ。あの青いコップの隣です。", "ふふ、あそこですよ。あのあおいコップのとなりです。", "Fufu, asoko desu yo. Ano aoi koppu no tonari desu.",
  "Hì, ở đằng kia kìa. Bên cạnh chiếc cốc màu xanh.", "point", "n_eat")
N("n_water_wrong", TA, "ここは、セルフの店です。自分で入れましょう。", "ここは、セルフのみせです。じぶんでいれましょう。", "Koko wa, serufu no mise desu. Jibun de iremashou.",
  "Đây là quán tự phục vụ. Mình tự rót nhé.", "point", "n_water")

# ───────── Ăn ramen (được phép húp xì xụp) ─────────
N("n_eat", YA, "はい、おまち！熱いから、気をつけてね。", "はい、おまち！あついから、きをつけてね。", "Hai, omachi! Atsui kara, ki wo tsukete ne.",
  "Đây, xong rồi! Nóng đấy, cẩn thận nhé.", "point", choices=[
    C("いただきます。", "Cháu xin phép dùng ạ.", "n_slurp", "Vocabulary", 10, "Nói いただきます trước khi ăn", [], ["vocab.n5.itadakimasu"]),
    C("ありがとうございます。いただきます。", "Cháu cảm ơn. Cháu xin phép dùng ạ.", "n_slurp", "Vocabulary", 10, "Cảm ơn rồi nói いただきます", [], ["vocab.n5.itadakimasu", "vocab.n5.arigatou_gozaimasu"]),
])
N("n_slurp", TA, "ラーメンは、音を立てて食べてもいいですよ。日本では、おいしいという意味です。", "ラーメンは、おとをたててたべてもいいですよ。にほんでは、おいしいといういみです。", "Raamen wa, oto wo tatete tabete mo ii desu yo. Nihon de wa, oishii to iu imi desu.",
  "Ăn ramen thì phát ra tiếng húp cũng được đấy. Ở Nhật, điều đó có nghĩa là \"ngon\".", "talk", choices=[
    C("音を立ててもいいんですか。", "Phát ra tiếng cũng được ạ?", "n_taste", "Grammar", 10, "Hỏi lại để chắc chắn bằng 〜てもいいんですか (đã học 〜てもいいですか)", ["grammar.n5.te_mo_ii_desu_ka"], ["vocab.n5.oto"]),
    C("音は、だめです。", "Tiếng động thì không được.", "n_slurp_wrong", "Grammar", -6, "Hiểu ngược ý: Tanaka vừa nói phát ra tiếng là được"),
])
N("n_slurp_wrong", TA, "いいえ、ラーメンは、いいんですよ。食べてみてください。", "いいえ、ラーメンは、いいんですよ。たべてみてください。", "Iie, raamen wa, ii n desu yo. Tabete mite kudasai.",
  "Không phải đâu, với ramen thì được đấy. Bạn thử ăn xem.", "point", "n_slurp")
N("n_taste", YA, "どうだい、味は？", "どうだい、あじは？", "Dou dai, aji wa?",
  "Sao nào, vị thế nào?", "talk", choices=[
    C("とてもおいしいです！スープが熱いです。", "Rất ngon ạ! Nước dùng nóng lắm.", "n_kaedama", "Vocabulary", 10, "Khen món ăn kèm nhận xét cụ thể bằng tính từ đuôi い", ["grammar.n5.i_adj"], ["vocab.n5.oishii", "vocab.n5.atsui", "vocab.n5.suupu"], ["yamada.friend"]),
    C("おいしいです。", "Ngon ạ.", "n_kaedama", "Vocabulary", 8, "Khen ngắn gọn nhưng đủ lịch sự", ["grammar.n5.i_adj"], ["vocab.n5.oishii"], ["yamada.friend"]),
    C("ふつうです。", "Bình thường ạ.", "n_taste_wrong", "ResponseAccuracy", -6, "Chê món ăn ngay trước mặt chủ quán: quá thẳng thắn và bất lịch sự"),
])
N("n_taste_wrong", YA, "ふつう！？ははは、正直だね。もう少し、食べてみて。", "ふつう！？ははは、しょうじきだね。もうすこし、たべてみて。", "Futsuu!? Hahaha, shoujiki da ne. Mou sukoshi, tabete mite.",
  "Bình thường ư?! Haha, thật thà quá. Ăn thêm chút nữa xem.", "talk", "n_taste")

# ───────── Kaedama và ra về ─────────
N("n_kaedama", YA, "麺が、もう少しほしいなら、替え玉もあるよ。百円だ。", "めんが、もうすこしほしいなら、かえだまもあるよ。ひゃくえんだ。", "Men ga, mou sukoshi hoshii nara, kaedama mo aru yo. Hyaku en da.",
  "Nếu bạn muốn thêm chút mì thì có kaedama (mì thêm) nữa đấy. 100 yên.", "talk", choices=[
    C("おなかがいっぱいです。だいじょうぶです。", "Cháu no rồi. Không cần đâu ạ.", "n_leave", "Grammar", 10, "Từ chối lịch sự bằng lý do おなかがいっぱいです", ["grammar.n5.ga_ippai"], ["vocab.n5.onaka", "vocab.n5.ippai"]),
    C("はい、もう少し食べたいです。", "Vâng, cháu muốn ăn thêm một chút ạ.", "n_kaedama_yes", "Grammar", 8, "Diễn đạt mong muốn bằng 〜たいです", ["grammar.n5.tai_desu"], ["vocab.n5.taberu"]),
    C("いりません。", "Không cần.", "n_kaedama_blunt", "ResponseAccuracy", 2, "Từ chối được nhưng cộc lốc; nên nêu lý do hoặc nói だいじょうぶです", [], []),
])
N("n_kaedama_yes", YA, "いいね！じゃあ、百円の券を、もう一枚買ってね。", "いいね！じゃあ、ひゃくえんのけんを、もういちまいかってね。", "Ii ne! Jaa, hyaku en no ken wo, mou ichimai katte ne.",
  "Hay lắm! Vậy thì mua thêm một tấm vé 100 yên nhé.", "talk", "n_leave")
N("n_kaedama_blunt", YA, "ははは、はっきりしてるね。じゃあ、いいよ。", "ははは、はっきりしてるね。じゃあ、いいよ。", "Hahaha, hakkiri shiteru ne. Jaa, ii yo.",
  "Haha, thẳng thắn nhỉ. Thôi được rồi.", "talk", "n_leave")
N("n_leave", NAR, "ラーメンを食べ終わりました。おなかも心も、いっぱいです。お店を出ます。", "ラーメンをたべおわりました。おなかもこころも、いっぱいです。おみせをでます。", "Raamen wo tabe owarimashita. Onaka mo kokoro mo, ippai desu. Omise wo demasu.",
  "Bạn ăn xong tô ramen. Bụng no, lòng cũng đầy ắp. Bạn ra khỏi quán.", "", "n_bye", "obj_eat")
N("n_bye", YA, "毎度あり！また来てね。", "まいどあり！またきてね。", "Maido ari! Mata kite ne.",
  "Cảm ơn quý khách! Lại đến nhé.", "talk", choices=[
    C("ごちそうさまでした。おいしかったです。", "Cảm ơn về bữa ăn ạ. Ngon lắm ạ.", "n_end", "Vocabulary", 10, "Kết thúc bữa ăn bằng ごちそうさまでした và nhận xét おいしかったです (quá khứ)", ["grammar.n5.i_adj_past"], ["vocab.n5.gochisousama_deshita", "vocab.n5.oishii"], ["yamada.regular"]),
    C("ありがとう。", "Cảm ơn.", "n_bye_casual", "ResponseAccuracy", 2, "Cảm ơn được nhưng thiếu lời ごちそうさまでした khi rời quán ăn", [], []),
    C("いただきます。", "Cháu xin phép dùng ạ.", "n_bye_wrong", "Vocabulary", -8, "いただきます nói trước khi ăn, không phải khi rời quán"),
])
N("n_bye_casual", YA, "はいよ。「ごちそうさまでした」と言うと、うれしいね。", "はいよ。「ごちそうさまでした」というと、うれしいね。", "Hai yo. \"Gochisousama deshita\" to iu to, ureshii ne.",
  "Ừ. Nếu nói \"gochisousama deshita\" thì tôi vui lắm đấy.", "talk", "n_bye")
N("n_bye_wrong", YA, "ははは、もう食べたよ！「ごちそうさまでした」だ。", "ははは、もうたべたよ！「ごちそうさまでした」だ。", "Hahaha, mou tabeta yo! \"Gochisousama deshita\" da.",
  "Haha, ăn xong rồi mà! Phải nói \"gochisousama deshita\" chứ.", "point", "n_bye")
N("n_end", TA, "ここは、私のお気に入りの店です。これから、いつでも来てくださいね。", "ここは、わたしのおきにいりのみせです。これから、いつでもきてくださいね。", "Koko wa, watashi no okiniiri no mise desu. Korekara, itsu demo kite kudasai ne.",
  "Đây là quán yêu thích của tôi. Từ nay bạn cứ ghé bất cứ lúc nào nhé.", "bow", "n_recap", "obj_pay")
N("n_recap", NAR, "今日のポイント：「〜にします」「〜でお願いします」「ぜんぶで、いくらですか」。", "きょうのポイント：「〜にします」「〜でおねがいします」「ぜんぶで、いくらですか」。", "Kyou no pointo: \"~ ni shimasu\" \"~ de onegaishimasu\" \"zenbu de, ikura desu ka\".",
  "Tóm tắt: \"~ ni shimasu\" (tôi chọn ~), \"~ de onegaishimasu\" (cho tôi ~), \"zenbu de, ikura desu ka\" (tất cả là bao nhiêu?). Ở quán ramen: mua vé ở máy, nước tự phục vụ, húp xì xụp là được!", "", "n_complete")
N("n_complete", NAR, "", "", "", "", "", "", type=4)

objectives = [
    ("obj_meet", "田中さんに会う", "Gặp bác Tanaka", False),
    ("obj_arrive", "やまだ食堂に着く", "Đến quán Yamada", False),
    ("obj_ticket", "券売機で券を買う", "Mua vé ở máy bán vé", False),
    ("obj_eat", "ラーメンを食べる", "Thưởng thức ramen", False),
    ("obj_pay", "お礼を言って店を出る", "Cảm ơn và rời quán", False),
]
targets = ["grammar.n5.ni_shimasu", "grammar.n5.masen_ka", "grammar.n5.tai_desu", "vocab.n5.gochisousama_deshita", "vocab.n5.sen"]

problems = S.validate("node_find_tanaka", objectives)
print("problems:", problems)
if not problems:
    S.write(sys.argv[1], name="scenario_restaurant_order_ramen", sid="scenario.restaurant.order_ramen", title_ja="やまだ食堂でラーメン", title_vi="Ăn ramen ở quán Yamada",
            desc_ja="田中さんと、やまだ食堂へ行きましょう。券売機、注文、水、食べ方を練習します。", desc_vi="Cùng bác Tanaka đến quán ramen của chú Yamada: mua vé ở máy, chọn độ cứng mì, tự lấy nước, cách ăn và cách nói khi ra về.",
            chapter=3, targets=targets, objectives=objectives, start="node_find_tanaka")
    print("nodes", len(S.nodes))
