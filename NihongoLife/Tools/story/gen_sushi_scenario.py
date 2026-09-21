# -*- coding: utf-8 -*-
import json, sys

def q(s): return json.dumps(s, ensure_ascii=False)

AOKI = ("Aoki", "npc_sushi_staff")
OTA = ("Ota", "npc_sushi_chef")

nodes = []

def C(ja, vi, nxt, cat=None, val=0, reason="", g=(), v=()):
    return dict(ja=ja, vi=vi, nxt=nxt, mods=([(cat, val, reason)] if cat else []), g=list(g), v=list(v))

def N(id, sp, ja, rd, rom, vi, cue="", nxt="", obj="", choices=None):
    nodes.append(dict(id=id, sp=sp, ja=ja, rd=rd, rom=rom, vi=vi, cue=cue, nxt=nxt, obj=obj, choices=choices or [], type=0))

# ───────────── Vào quán, chào hỏi, chọn chỗ ─────────────
N("n_arrive", None, "夕方、ひばり寿司に着きました。", "ゆうがた、ひばりずしにつきました。", "Yuugata, Hibari zushi ni tsukimashita.",
  "Chiều tối, bạn đến quán Hibari Sushi.", "", "n_welcome")

N("n_welcome", AOKI, "いらっしゃいませ！何名様ですか。", "いらっしゃいませ！なんめいさまですか。", "Irasshaimase! Nanmeisama desu ka?",
  "Xin chào quý khách! Quý khách đi mấy người ạ?", "bow", choices=[
    C("一人です。", "Một người ạ.", "n_seat_offer", "Grammar", 10, "Dùng đúng cách đếm người 一人 (ひとり) và câu 〜です", ["grammar.n5.counter_people", "grammar.n5.wa_desu"], ["vocab.n5.hitori"]),
    C("一人。", "Một người.", "n_welcome_short", "ResponseAccuracy", 2, "Đúng ý nhưng thiếu です nên nghe hơi cộc", ["grammar.n5.counter_people"], ["vocab.n5.hitori"]),
    C("一つです。", "Một cái ạ.", "n_welcome_wrong", "Grammar", -8, "Dùng bộ đếm đồ vật 一つ cho người (phải dùng 一人)"),
])
N("n_welcome_short", AOKI, "一名様ですね。「一人です」と言うと、もっとていねいですよ。", "いちめいさまですね。「ひとりです」というと、もっとていねいですよ。", "Ichimeisama desu ne. \"Hitori desu\" to iu to, motto teinei desu yo.",
  "Một người ạ. Nói \"hitori desu\" sẽ lịch sự hơn đấy.", "talk", "n_seat_offer")
N("n_welcome_wrong", AOKI, "あ、人は「一人、二人」と数えますよ。もう一度どうぞ。", "あ、ひとは「ひとり、ふたり」とかぞえますよ。もういちどどうぞ。", "A, hito wa \"hitori, futari\" to kazoemasu yo. Mou ichido douzo.",
  "À, khi đếm người thì nói \"hitori, futari\" nhé. Mời quý khách nói lại.", "point", "n_welcome")

N("n_seat_offer", AOKI, "カウンターとテーブルがあります。どちらがいいですか。", "カウンターとテーブルがあります。どちらがいいですか。", "Kauntaa to teeburu ga arimasu. Dochira ga ii desu ka?",
  "Quán có quầy và bàn. Quý khách muốn ngồi chỗ nào ạ?", "talk", obj="obj_enter", choices=[
    C("カウンターでお願いします。", "Cho tôi ngồi ở quầy nhé.", "n_counter", "Grammar", 10, "Dùng đúng mẫu 〜でお願いします để chọn chỗ ngồi", ["grammar.n5.de_onegaishimasu"], ["vocab.n5.kauntaa"]),
    C("テーブルでお願いします。", "Cho tôi ngồi ở bàn nhé.", "n_table", "Grammar", 10, "Dùng đúng mẫu 〜でお願いします để chọn chỗ ngồi", ["grammar.n5.de_onegaishimasu"], ["vocab.n5.teeburu"]),
    C("どちらでもいいです。", "Chỗ nào cũng được ạ.", "n_seat_any", "ResponseAccuracy", 4, "Trả lời được nhưng nhường hết quyết định cho nhân viên", ["grammar.n5.dochira"], []),
    C("カウンターをください。", "Cho tôi cái quầy.", "n_seat_wrong", "Grammar", -8, "Dùng をください (xin một vật) cho chỗ ngồi; phải dùng 〜でお願いします"),
])
N("n_counter", OTA, "いらっしゃい！今日はいい魚がありますよ。どうぞ、ここへ。", "いらっしゃい！きょうはいいさかながありますよ。どうぞ、ここへ。", "Irasshai! Kyou wa ii sakana ga arimasu yo. Douzo, koko e.",
  "Chào mừng! Hôm nay có cá ngon lắm đấy. Mời ngồi đây.", "talk", "n_thanks_towel", "obj_seat")
N("n_table", AOKI, "こちらのテーブルへどうぞ。", "こちらのテーブルへどうぞ。", "Kochira no teeburu e douzo.",
  "Mời quý khách ngồi bàn này ạ.", "point", "n_thanks_towel", "obj_seat")
N("n_seat_any", AOKI, "では、カウンターへどうぞ。大将の前です。", "では、カウンターへどうぞ。たいしょうのまえです。", "Dewa, kauntaa e douzo. Taishou no mae desu.",
  "Vậy mời quý khách ngồi quầy ạ. Ngay trước mặt đầu bếp (taishou, cách gọi kính trọng dành cho bếp trưởng).", "point", "n_thanks_towel", "obj_seat")
N("n_seat_wrong", AOKI, "「カウンターをください」ではありませんよ。席は「〜でお願いします」と言います。もう一度どうぞ。", "「カウンターをください」ではありませんよ。せきは「〜でおねがいします」といいます。もういちどどうぞ。", "\"Kauntaa wo kudasai\" dewa arimasen yo. Seki wa \"~ de onegaishimasu\" to iimasu. Mou ichido douzo.",
  "Không phải \"kauntaa wo kudasai\" đâu ạ. Khi chọn chỗ ngồi, hãy nói \"~ de onegaishimasu\". Mời quý khách nói lại.", "point", "n_seat_offer")

N("n_thanks_towel", AOKI, "おしぼりとお茶です。どうぞ。", "おしぼりとおちゃです。どうぞ。", "Oshibori to ocha desu. Douzo.",
  "Khăn ướt và trà ạ. Mời quý khách.", "bow", choices=[
    C("ありがとうございます。", "Cảm ơn ạ.", "n_towel_ok", "Vocabulary", 8, "Cảm ơn đúng mức lịch sự khi nhận khăn và trà", [], ["vocab.n5.arigatou_gozaimasu"]),
    C("どうも。", "Cảm ơn.", "n_towel_casual", "ResponseAccuracy", 3, "Cảm ơn ngắn gọn, ổn với nhân viên quán nhưng kém lịch sự hơn", [], ["vocab.n5.arigatou_gozaimasu"]),
    C("いただきます。", "Xin phép được ăn ạ.", "n_towel_wrong", "ResponseAccuracy", -8, "いただきます chỉ nói trước khi ăn, không dùng khi nhận khăn"),
])
HINT = " (Gợi ý: lại gần bảng thực đơn trong quán và nhấn E để xem chi tiết từng món.)"
N("n_towel_ok", AOKI, "いえいえ。メニューはそちらのボードにあります。ゆっくり見てくださいね。", "いえいえ。メニューはそちらのボードにあります。ゆっくりみてくださいね。", "Ie ie. Menyuu wa sochira no boodo ni arimasu. Yukkuri mite kudasai ne.",
  "Không có gì ạ. Thực đơn ở bảng bên kia, quý khách cứ xem thong thả nhé." + HINT, "bow", "n_ask_order")
N("n_towel_casual", AOKI, "いえいえ。「ありがとうございます」と言うと、もっとていねいですよ。メニューはそちらのボードにあります。", "いえいえ。「ありがとうございます」というと、もっとていねいですよ。メニューはそちらのボードにあります。", "Ie ie. \"Arigatou gozaimasu\" to iu to, motto teinei desu yo. Menyuu wa sochira no boodo ni arimasu.",
  "Không có gì ạ. Nói \"arigatou gozaimasu\" sẽ lịch sự hơn. Thực đơn ở bảng bên kia ạ." + HINT, "talk", "n_ask_order")
N("n_towel_wrong", AOKI, "あ、「いただきます」は食べるときに言いますよ。ここは「ありがとうございます」ですね。", "あ、「いただきます」はたべるときにいいますよ。ここは「ありがとうございます」ですね。", "A, \"itadakimasu\" wa taberu toki ni iimasu yo. Koko wa \"arigatou gozaimasu\" desu ne.",
  "À, \"itadakimasu\" chỉ nói khi bắt đầu ăn thôi ạ. Ở đây phải nói \"arigatou gozaimasu\".", "point", "n_thanks_towel")

# ───────────── Gọi món ─────────────
N("n_ask_order", AOKI, "ご注文はお決まりですか。", "ごちゅうもんはおきまりですか。", "Gochuumon wa okimari desu ka?",
  "Quý khách đã chọn món chưa ạ?", "talk", choices=[
    C("おすすめは何ですか。", "Món nào được gợi ý ạ?", "n_recommend", "Vocabulary", 8, "Hỏi món gợi ý đúng cách: おすすめは何ですか", ["grammar.n5.nan_desu_ka"], ["vocab.n5.osusume"]),
    C("まぐろとサーモンをください。", "Cho tôi cá ngừ và cá hồi.", "n_order_nigiri", "Grammar", 10, "Dùng đúng mẫu 〜と〜をください để gọi hai món", ["grammar.n5.wo_kudasai", "grammar.n5.to_and"], ["vocab.n5.maguro", "vocab.n5.sake"]),
    C("まぐろとサーモンください。", "Cho tôi cá ngừ và cá hồi.", "n_order_casual", "Grammar", 3, "Đúng ý nhưng bỏ trợ từ を nên hơi cộc, hợp với người quen hơn là nhân viên", ["grammar.n5.wo_kudasai"], ["vocab.n5.maguro", "vocab.n5.sake"]),
    C("すみません、まだです。", "Xin lỗi, tôi chưa chọn xong.", "n_not_yet", "ResponseAccuracy", 5, "Nói lịch sự rằng mình chưa quyết định xong", [], ["vocab.n5.sumimasen"]),
    C("まぐろは食べます。", "Tôi ăn cá ngừ.", "n_order_wrong", "Grammar", -8, "Câu này chỉ nói mình ăn cá ngừ, không phải gọi món (gọi món dùng 〜をください/〜をお願いします)"),
])
N("n_not_yet", AOKI, "はい、ゆっくりどうぞ。決まったらお呼びください。", "はい、ゆっくりどうぞ。きまったらおよびください。", "Hai, yukkuri douzo. Kimattara oyobi kudasai.",
  "Vâng, cứ thong thả ạ. Chọn xong thì gọi tôi nhé.", "bow", "n_ask_order")
N("n_order_wrong", AOKI, "注文のときは「〜をください」と言いますよ。もう一度どうぞ。", "ちゅうもんのときは「〜をください」といいますよ。もういちどどうぞ。", "Chuumon no toki wa \"~ wo kudasai\" to iimasu yo. Mou ichido douzo.",
  "Khi gọi món thì nói \"~ wo kudasai\" nhé. Mời quý khách gọi lại.", "point", "n_ask_order")

N("n_recommend", OTA, "今日のおすすめは、うな丼とちらし寿司です。うな丼は、あまいたれがおいしいですよ。ちらし寿司は、いろいろな魚が入っています。",
  "きょうのおすすめは、うなどんとちらしずしです。うなどんは、あまいたれがおいしいですよ。ちらしずしは、いろいろなさかながはいっています。",
  "Kyou no osusume wa, unadon to chirashi zushi desu. Unadon wa, amai tare ga oishii desu yo. Chirashi zushi wa, iroiro na sakana ga haitte imasu.",
  "Món gợi ý hôm nay là unadon và chirashi zushi. Unadon có sốt ngọt rất ngon. Chirashi có nhiều loại cá bên trong.", "talk", choices=[
    C("うな丼をお願いします。", "Cho tôi unadon nhé.", "n_order_unadon", "Grammar", 10, "Dùng đúng mẫu 〜をお願いします để gọi món", ["grammar.n5.wo_onegaishimasu"], ["vocab.n5.unagi"]),
    C("ちらし寿司をお願いします。", "Cho tôi chirashi zushi nhé.", "n_order_chirashi", "Grammar", 10, "Dùng đúng mẫu 〜をお願いします để gọi món", ["grammar.n5.wo_onegaishimasu"], ["vocab.n5.chirashi"]),
    C("アレルギーがあります。", "Tôi bị dị ứng.", "n_allergy_ask", "ResponseAccuracy", 8, "Báo dị ứng trước khi gọi món, đúng thói quen an toàn", ["grammar.n5.ga_arimasu"], ["vocab.n5.arerugii"]),
    C("おいしいです。", "Ngon quá ạ.", "n_recommend_wrong", "ResponseAccuracy", -8, "Khen ngon khi chưa gọi và chưa ăn món nào, không khớp tình huống"),
])
N("n_recommend_wrong", OTA, "まだ食べていませんよ。どちらにしますか。", "まだたべていませんよ。どちらにしますか。", "Mada tabete imasen yo. Dochira ni shimasu ka?",
  "Quý khách chưa ăn mà. Quý khách chọn món nào ạ?", "talk", "n_recommend")
N("n_allergy_ask", OTA, "アレルギーですか。何のアレルギーですか。", "アレルギーですか。なんのアレルギーですか。", "Arerugii desu ka. Nan no arerugii desu ka?",
  "Dị ứng à? Quý khách dị ứng với gì ạ?", "point", choices=[
    C("えびです。", "Tôm ạ.", "n_allergy_shrimp", "Vocabulary", 8, "Nêu đúng tên chất gây dị ứng: えび (tôm)", [], ["vocab.n5.ebi"]),
    C("たまごです。", "Trứng ạ.", "n_allergy_egg", "Vocabulary", 8, "Nêu đúng tên chất gây dị ứng: たまご (trứng)", [], ["vocab.n5.tamago"]),
])
N("n_allergy_shrimp", OTA, "えびですね。ちらし寿司にはえびが入っています。うな丼は大丈夫ですよ。", "えびですね。ちらしずしにはえびがはいっています。うなどんはだいじょうぶですよ。", "Ebi desu ne. Chirashi zushi ni wa ebi ga haitte imasu. Unadon wa daijoubu desu yo.",
  "Tôm nhỉ. Chirashi có tôm bên trong. Unadon thì không sao đâu ạ.", "point", "n_choose_after_allergy")
N("n_allergy_egg", OTA, "たまごですね。ちらし寿司にはたまごやきが入っています。うな丼は大丈夫ですよ。", "たまごですね。ちらしずしにはたまごやきがはいっています。うなどんはだいじょうぶですよ。", "Tamago desu ne. Chirashi zushi ni wa tamagoyaki ga haitte imasu. Unadon wa daijoubu desu yo.",
  "Trứng nhỉ. Chirashi có trứng cuộn bên trong. Unadon thì không sao đâu ạ.", "point", "n_choose_after_allergy")
N("n_choose_after_allergy", OTA, "では、どれにしますか。", "では、どれにしますか。", "Dewa, dore ni shimasu ka?",
  "Vậy quý khách chọn món nào ạ?", "talk", choices=[
    C("うな丼をお願いします。", "Cho tôi unadon nhé.", "n_order_unadon", "Grammar", 10, "Chọn đúng món an toàn và dùng đúng mẫu 〜をお願いします", ["grammar.n5.wo_onegaishimasu"], ["vocab.n5.unagi"]),
    C("まぐろとサーモンをください。", "Cho tôi cá ngừ và cá hồi.", "n_order_nigiri", "Grammar", 10, "Chọn đúng món an toàn và dùng đúng mẫu 〜をください", ["grammar.n5.wo_kudasai", "grammar.n5.to_and"], ["vocab.n5.maguro", "vocab.n5.sake"]),
    C("ちらし寿司をお願いします。", "Cho tôi chirashi zushi nhé.", "n_allergy_warn", "ResponseAccuracy", -10, "Chọn món có chất gây dị ứng vừa nêu"),
])
N("n_allergy_warn", OTA, "それにはえびとたまごが入っていますよ。あぶないですから、ほかのものにしましょう。", "それにはえびとたまごがはいっていますよ。あぶないですから、ほかのものにしましょう。", "Sore ni wa ebi to tamago ga haitte imasu yo. Abunai desu kara, hoka no mono ni shimashou.",
  "Món đó có tôm và trứng đấy ạ. Nguy hiểm nên mình chọn món khác nhé.", "point", "n_choose_after_allergy")

N("n_order_unadon", AOKI, "かしこまりました。うな丼ですね。少々お待ちください。", "かしこまりました。うなどんですね。しょうしょうおまちください。", "Kashikomarimashita. Unadon desu ne. Shoushou omachi kudasai.",
  "Vâng ạ. Quý khách gọi unadon nhỉ. Xin chờ một chút.", "bow", "n_served_unadon", "obj_order")
N("n_order_chirashi", AOKI, "かしこまりました。ちらし寿司ですね。少々お待ちください。", "かしこまりました。ちらしずしですね。しょうしょうおまちください。", "Kashikomarimashita. Chirashi zushi desu ne. Shoushou omachi kudasai.",
  "Vâng ạ. Quý khách gọi chirashi zushi nhỉ. Xin chờ một chút.", "bow", "n_served_chirashi", "obj_order")
N("n_order_nigiri", AOKI, "かしこまりました。まぐろとサーモンですね。", "かしこまりました。まぐろとサーモンですね。", "Kashikomarimashita. Maguro to saamon desu ne.",
  "Vâng ạ. Cá ngừ và cá hồi nhỉ.", "bow", "n_wasabi", "obj_order")
N("n_order_casual", AOKI, "かしこまりました。「まぐろとサーモンをください」と言うと、もっとていねいですよ。", "かしこまりました。「まぐろとサーモンをください」というと、もっとていねいですよ。", "Kashikomarimashita. \"Maguro to saamon wo kudasai\" to iu to, motto teinei desu yo.",
  "Vâng ạ. Nói \"maguro to saamon wo kudasai\" sẽ lịch sự hơn đấy.", "talk", "n_wasabi", "obj_order")

N("n_wasabi", OTA, "わさびは大丈夫ですか。", "わさびはだいじょうぶですか。", "Wasabi wa daijoubu desu ka?",
  "Quý khách dùng wasabi được không ạ?", "talk", choices=[
    C("大丈夫です。", "Được ạ.", "n_wasabi_ok", "Grammar", 8, "Dùng 大丈夫です để đồng ý một cách tự nhiên", ["grammar.n5.daijoubu_desu"], ["vocab.n5.wasabi"]),
    C("わさびなしでお願いします。", "Cho tôi không wasabi nhé.", "n_wasabi_none", "Grammar", 10, "Dùng đúng mẫu 〜なしでお願いします để bỏ một thành phần", ["grammar.n5.nashi_de"], ["vocab.n5.wasabi"]),
    C("わさびをください。", "Cho tôi wasabi.", "n_wasabi_wrong", "ResponseAccuracy", -8, "Trả lời không khớp câu hỏi (nhân viên hỏi có dùng được không, không phải xin thêm)"),
])
N("n_wasabi_ok", OTA, "はい、ふつうにしますね。", "はい、ふつうにしますね。", "Hai, futsuu ni shimasu ne.", "Vâng, tôi làm như bình thường nhé.", "talk", "n_served_nigiri")
N("n_wasabi_none", OTA, "わかりました。わさびなしですね。", "わかりました。わさびなしですね。", "Wakarimashita. Wasabi nashi desu ne.", "Tôi hiểu rồi ạ. Không wasabi nhé.", "bow", "n_served_nigiri")
N("n_wasabi_wrong", OTA, "あ、たくさんですか？いえ、「大丈夫です」か「なしでお願いします」ですよ。", "あ、たくさんですか？いえ、「だいじょうぶです」か「なしでおねがいします」ですよ。", "A, takusan desu ka? Ie, \"daijoubu desu\" ka \"nashi de onegaishimasu\" desu yo.",
  "À, quý khách muốn nhiều à? Không, phải trả lời \"daijoubu desu\" hoặc \"nashi de onegaishimasu\" cơ ạ.", "point", "n_wasabi")

N("n_served_nigiri", OTA, "お待たせしました。まぐろとサーモンです。しょうゆは、さかなにつけてください。どうぞ。", "おまたせしました。まぐろとサーモンです。しょうゆは、さかなにつけてください。どうぞ。", "Omatase shimashita. Maguro to saamon desu. Shouyu wa, sakana ni tsukete kudasai. Douzo.",
  "Xin lỗi đã để quý khách chờ. Cá ngừ và cá hồi ạ. Nước tương thì chấm vào phần cá nhé. Mời quý khách.", "point", "nn_eat")
N("n_served_unadon", AOKI, "お待たせしました。うな丼です。お味噌汁もどうぞ。", "おまたせしました。うなどんです。おみそしるもどうぞ。", "Omatase shimashita. Unadon desu. Omisoshiru mo douzo.",
  "Xin lỗi đã để quý khách chờ. Unadon ạ. Có cả súp miso, mời quý khách.", "bow", "nd_eat")
N("n_served_chirashi", AOKI, "お待たせしました。ちらし寿司です。お味噌汁もどうぞ。", "おまたせしました。ちらしずしです。おみそしるもどうぞ。", "Omatase shimashita. Chirashi zushi desu. Omisoshiru mo douzo.",
  "Xin lỗi đã để quý khách chờ. Chirashi zushi ạ. Có cả súp miso, mời quý khách.", "bow", "nd_eat")

# ───────────── Ăn, thanh toán, ra về (2 nhánh vì tổng tiền khác nhau) ─────────────
def tail(p, staff, total_ja, total_rd, total_rom, total_vi):
    S = staff
    N(f"{p}_eat", S, "ごゆっくりどうぞ。", "ごゆっくりどうぞ。", "Goyukkuri douzo.", "Xin mời quý khách dùng bữa thong thả ạ.", "bow", choices=[
        C("いただきます。", "Xin phép được ăn ạ.", f"{p}_taste", "Vocabulary", 10, "Nói いただきます đúng lúc trước khi ăn", [], ["vocab.n5.itadakimasu"]),
        C("ありがとうございます。", "Cảm ơn ạ.", f"{p}_eat_thanks", "ResponseAccuracy", 3, "Cảm ơn cũng lịch sự nhưng trước khi ăn nên nói いただきます", [], ["vocab.n5.arigatou_gozaimasu"]),
        C("いただきました。", "Tôi đã ăn xong rồi ạ.", f"{p}_eat_wrong", "Grammar", -8, "Dùng quá khứ いただきました khi chưa ăn (trước khi ăn phải là いただきます)"),
    ])
    N(f"{p}_eat_thanks", S, "いえいえ。食べるときは「いただきます」と言いましょう。どうぞ。", "いえいえ。たべるときは「いただきます」といいましょう。どうぞ。", "Ie ie. Taberu toki wa \"itadakimasu\" to iimashou. Douzo.",
      "Không có gì ạ. Khi bắt đầu ăn hãy nói \"itadakimasu\" nhé. Mời quý khách.", "talk", f"{p}_taste")
    N(f"{p}_eat_wrong", S, "あ、まだ食べていませんよ。食べる前は「いただきます」です。", "あ、まだたべていませんよ。たべるまえは「いただきます」です。", "A, mada tabete imasen yo. Taberu mae wa \"itadakimasu\" desu.",
      "À, quý khách chưa ăn mà. Trước khi ăn thì nói \"itadakimasu\" ạ.", "point", f"{p}_eat")
    N(f"{p}_taste", OTA, "どうですか。おいしいですか。", "どうですか。おいしいですか。", "Dou desu ka. Oishii desu ka?", "Thế nào ạ? Có ngon không ạ?", "talk", choices=[
        C("おいしいです。", "Ngon ạ.", f"{p}_finish", "Vocabulary", 8, "Khen món ăn tự nhiên bằng おいしいです", [], ["vocab.n5.oishii"]),
        C("おいしかったです。", "Đã ngon ạ.", f"{p}_taste_past", "ResponseAccuracy", 3, "Dùng quá khứ khi đang ăn nghe hơi lạ; quá khứ dùng khi đã ăn xong", [], ["vocab.n5.oishii"]),
        C("まずいです。", "Dở ạ.", f"{p}_taste_wrong", "ResponseAccuracy", -10, "まずい là chê món ăn rất thô lỗ, không dùng với người nấu"),
    ])
    N(f"{p}_taste_past", OTA, "ありがとうございます。食べているときは「おいしいです」、食べたあとは「おいしかったです」ですよ。", "ありがとうございます。たべているときは「おいしいです」、たべたあとは「おいしかったです」ですよ。", "Arigatou gozaimasu. Tabete iru toki wa \"oishii desu\", tabeta ato wa \"oishikatta desu\" desu yo.",
      "Cảm ơn quý khách. Khi đang ăn thì nói \"oishii desu\", ăn xong thì nói \"oishikatta desu\" nhé.", "bow", f"{p}_finish")
    N(f"{p}_taste_wrong", OTA, "えっ！それは失礼ですよ。「おいしいです」と言いましょう。", "えっ！それはしつれいですよ。「おいしいです」といいましょう。", "E! Sore wa shitsurei desu yo. \"Oishii desu\" to iimashou.",
      "Ơ! Thế là thất lễ đấy ạ. Hãy nói \"oishii desu\" nhé.", "point", f"{p}_taste")
    N(f"{p}_finish", None, "食べ終わりました。店員さんを呼びましょう。", "たべおわりました。てんいんさんをよびましょう。", "Tabeowarimashita. Tenin-san wo yobimashou.",
      "Bạn đã ăn xong. Hãy gọi nhân viên để thanh toán.", "", obj="obj_eat", choices=[
        C("すみません、お会計をお願いします。", "Xin lỗi, cho tôi thanh toán ạ.", f"{p}_total", "Grammar", 10, "Gọi nhân viên bằng すみません rồi xin thanh toán đúng mẫu お会計をお願いします", ["grammar.n5.wo_onegaishimasu"], ["vocab.n5.okaikei", "vocab.n5.sumimasen"]),
        C("いくらですか。", "Bao nhiêu tiền ạ?", f"{p}_total_asked", "ResponseAccuracy", 4, "Hỏi giá được nhưng khi thanh toán thường dùng お会計をお願いします", [], ["vocab.n5.ikura"]),
        C("お金をください。", "Cho tôi tiền.", f"{p}_bill_wrong", "Grammar", -10, "お金をください nghĩa là xin nhân viên đưa tiền cho mình, ngược với ý muốn thanh toán"),
    ])
    N(f"{p}_total", AOKI, f"はい。お会計は{total_ja}です。", f"はい。おかいけいは{total_rd}です。", f"Hai. Okaikei wa {total_rom} desu.", f"Vâng. {total_vi}", "talk", f"{p}_pay")
    N(f"{p}_total_asked", AOKI, f"「お会計をお願いします」と言うと、もっといいですよ。{total_ja}です。", f"「おかいけいをおねがいします」というと、もっといいですよ。{total_rd}です。", f"\"Okaikei wo onegaishimasu\" to iu to, motto ii desu yo. {total_rom} desu.",
      f"Nói \"okaikei wo onegaishimasu\" sẽ hay hơn đấy ạ. {total_vi}", "talk", f"{p}_pay")
    N(f"{p}_bill_wrong", AOKI, "えっ？お金をもらうんですか？お会計は「お会計をお願いします」ですよ。", "えっ？おかねをもらうんですか？おかいけいは「おかいけいをおねがいします」ですよ。", "E? Okane wo morau n desu ka? Okaikei wa \"okaikei wo onegaishimasu\" desu yo.",
      "Ơ? Quý khách muốn lấy tiền của tôi sao? Xin thanh toán thì nói \"okaikei wo onegaishimasu\" nhé.", "point", f"{p}_finish")
    N(f"{p}_pay", AOKI, "現金ですか、カードですか。", "げんきんですか、カードですか。", "Genkin desu ka, kaado desu ka?", "Quý khách trả bằng tiền mặt hay thẻ ạ?", "talk", choices=[
        C("現金でお願いします。", "Tôi trả bằng tiền mặt ạ.", f"{p}_change", "Grammar", 10, "Dùng đúng mẫu 〜でお願いします để chọn cách thanh toán", ["grammar.n5.de_onegaishimasu"], ["vocab.n5.genkin"]),
        C("カードで払えますか。", "Tôi trả bằng thẻ được không ạ?", f"{p}_card_no", "ResponseAccuracy", 4, "Hỏi trước xem có trả bằng thẻ được không là thói quen tốt ở quán nhỏ", [], ["vocab.n5.kaado"]),
        C("払います。", "Tôi trả.", f"{p}_pay_short", "Grammar", 2, "Nói được ý trả tiền nhưng chưa chọn cách thanh toán", [], ["vocab.n5.harau"]),
    ])
    N(f"{p}_pay_short", AOKI, "はい。現金ですね。", "はい。げんきんですね。", "Hai. Genkin desu ne.", "Vâng. Tiền mặt nhỉ ạ.", "talk", f"{p}_change")
    N(f"{p}_card_no", AOKI, "すみません、カードは使えません。現金だけです。", "すみません、カードはつかえません。げんきんだけです。", "Sumimasen, kaado wa tsukaemasen. Genkin dake desu.",
      "Xin lỗi, quán không dùng thẻ được ạ. Chỉ nhận tiền mặt.", "bow", choices=[
        C("はい、現金で払います。", "Vâng, tôi trả bằng tiền mặt.", f"{p}_change", "Grammar", 8, "Đổi sang tiền mặt bằng mẫu 〜で払います", ["grammar.n5.de_harau"], ["vocab.n5.genkin"]),
        C("いいえ、カードです。", "Không, tôi dùng thẻ.", f"{p}_card_again", "ResponseAccuracy", -6, "Nhân viên vừa nói không dùng được thẻ nhưng vẫn khăng khăng dùng thẻ"),
    ])
    N(f"{p}_card_again", AOKI, "すみません、本当にカードは使えません。現金でお願いします。", "すみません、ほんとうにカードはつかえません。げんきんでおねがいします。", "Sumimasen, hontou ni kaado wa tsukaemasen. Genkin de onegaishimasu.",
      "Xin lỗi, thật sự quán không dùng được thẻ ạ. Xin quý khách trả bằng tiền mặt.", "point", f"{p}_card_no")
    N(f"{p}_change", AOKI, f"はい、{total_ja}ですね。ありがとうございます。", f"はい、{total_rd}ですね。ありがとうございます。", f"Hai, {total_rom} desu ne. Arigatou gozaimasu.", f"Vâng, {total_vi[:-1] if total_vi.endswith('.') else total_vi} nhỉ. Xin cảm ơn quý khách.", "bow", f"{p}_leave", "obj_pay")
    N(f"{p}_leave", None, "店を出ます。何と言いますか。", "みせをでます。なんといいますか。", "Mise wo demasu. Nan to iimasu ka?", "Bạn rời quán. Bạn sẽ nói gì?", "", choices=[
        C("ごちそうさまでした。", "Cảm ơn vì bữa ăn ngon ạ.", f"{p}_leave_ok", "Vocabulary", 10, "Cảm ơn bữa ăn đúng lúc khi rời quán", [], ["vocab.n5.gochisousama_deshita"]),
        C("さようなら。", "Tạm biệt ạ.", f"{p}_leave_casual", "ResponseAccuracy", 3, "Chào tạm biệt được, nhưng ở quán ăn người ta thường nói ごちそうさまでした", [], ["vocab.n5.sayounara"]),
        C("いただきます。", "Xin phép được ăn ạ.", f"{p}_leave_wrong", "ResponseAccuracy", -8, "いただきます là lời trước bữa ăn, không dùng khi ra về"),
    ])
    N(f"{p}_leave_ok", OTA, "ありがとうございました。またどうぞ。", "ありがとうございました。またどうぞ。", "Arigatou gozaimashita. Mata douzo.", "Xin cảm ơn quý khách. Mời quý khách quay lại ạ.", "bow", "n_summary", "obj_leave")
    N(f"{p}_leave_casual", AOKI, "ありがとうございました。店を出るときは「ごちそうさまでした」と言うと、うれしいです。", "ありがとうございました。みせをでるときは「ごちそうさまでした」というと、うれしいです。", "Arigatou gozaimashita. Mise wo deru toki wa \"gochisousama deshita\" to iu to, ureshii desu.",
      "Xin cảm ơn quý khách. Khi rời quán, nếu quý khách nói \"gochisousama deshita\" chúng tôi sẽ rất vui ạ.", "talk", "n_summary", "obj_leave")
    N(f"{p}_leave_wrong", AOKI, "あ、「いただきます」は食べる前ですよ。「ごちそうさまでした」ですね。", "あ、「いただきます」はたべるまえですよ。「ごちそうさまでした」ですね。", "A, \"itadakimasu\" wa taberu mae desu yo. \"Gochisousama deshita\" desu ne.",
      "À, \"itadakimasu\" là lời trước khi ăn ạ. Khi ra về thì nói \"gochisousama deshita\" nhé.", "point", f"{p}_leave")

tail("nn", OTA, "530円", "ごひゃくさんじゅうえん", "gohyaku sanjuu-en", "Tổng cộng là 530 yên ạ.")
tail("nd", AOKI, "1,100円", "せんひゃくえん", "sen hyaku-en", "Tổng cộng là 1.100 yên ạ.")

N("n_summary", OTA, "今日は「一人です」「〜でお願いします」「〜をください」「お会計をお願いします」「ごちそうさまでした」を使いましたね。上手でした。また来てください。",
  "きょうは「ひとりです」「〜でおねがいします」「〜をください」「おかいけいをおねがいします」「ごちそうさまでした」をつかいましたね。じょうずでした。またきてください。",
  "Kyou wa \"hitori desu\" \"~ de onegaishimasu\" \"~ wo kudasai\" \"okaikei wo onegaishimasu\" \"gochisousama deshita\" wo tsukaimashita ne. Jouzu deshita. Mata kite kudasai.",
  "Hôm nay bạn đã dùng \"hitori desu\", \"~ de onegaishimasu\", \"~ wo kudasai\", \"okaikei wo onegaishimasu\" và \"gochisousama deshita\". Bạn làm rất tốt. Hãy quay lại nhé.", "bow", "n_complete")
nodes.append(dict(id="n_complete", sp=None, ja="", rd="", rom="", vi="", cue="", nxt="", obj="", choices=[], type=4))

# ───────────── Xuất YAML ─────────────
objectives = [
 ("obj_enter", "店に入ってあいさつする", "Vào quán và đáp lời chào"),
 ("obj_seat", "席を決める", "Chọn chỗ ngồi"),
 ("obj_order", "注文する", "Gọi món"),
 ("obj_eat", "食事をする", "Thưởng thức bữa ăn"),
 ("obj_pay", "お会計をする", "Thanh toán"),
 ("obj_leave", "あいさつして店を出る", "Chào và rời quán"),
]
targets = ["grammar.n5.counter_people", "grammar.n5.de_onegaishimasu", "grammar.n5.wo_kudasai", "grammar.n5.wo_onegaishimasu",
           "grammar.n5.nashi_de", "grammar.n5.daijoubu_desu", "vocab.n5.itadakimasu", "vocab.n5.gochisousama_deshita", "vocab.n5.okaikei", "vocab.n5.osusume"]

out = []
w = out.append
w("%YAML 1.1"); w("%TAG !u! tag:unity3d.com,2011:"); w("--- !u!114 &11400000"); w("MonoBehaviour:")
for l in ["  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}", "  m_PrefabAsset: {fileID: 0}", "  m_GameObject: {fileID: 0}", "  m_Enabled: 1", "  m_EditorHideFlags: 0",
          "  m_Script: {fileID: 11500000, guid: 3ba8f435ab5427145b206cd2a4f4ea7c, type: 3}", "  m_Name: scenario_restaurant_sushi_dining", "  m_EditorClassIdentifier: NihongoLife::NihongoLife.Scenario.ScenarioDefinition",
          "  id: scenario.restaurant.sushi_dining", "  version: 1"]:
    w(l)
w("  titleJa: " + q("すし店で食事")); w("  titleEn: " + q("Dùng bữa ở quán sushi"))
w("  descriptionJa: " + q("ひばり寿司で、あいさつ、注文、会計まで練習しましょう。"))
w("  descriptionEn: " + q("Luyện toàn bộ quy trình ở quán sushi Hibari: chào hỏi, chọn chỗ, hỏi món đặc sản, báo dị ứng, gọi món, ăn, thanh toán và ra về."))
w("  chapterIndex: 3")
w("  learningTargets:")
for t in targets: w("  - " + t)
w("  objectives:")
for o in objectives:
    w("  - id: " + o[0]); w("    titleJa: " + q(o[1])); w("    titleEn: " + q(o[2])); w("    isOptional: 0")
w("  nodes:")
for n in nodes:
    w("  - id: " + n["id"]); w("    nodeType: %d" % n["type"]); w("    nextNodeId: " + n["nxt"]); w("    objectiveIdToComplete: " + n["obj"])
    sp = n["sp"]
    w("    speakerName: " + (sp[0] if sp else "")); w("    speakerId: " + (sp[1] if sp else ""))
    w("    textJa: " + (q(n["ja"]) if n["ja"] else "")); w("    textReading: " + (q(n["rd"]) if n["rd"] else ""))
    w("    textEn: " + (q(n["vi"]) if n["vi"] else "")); w("    textRomaji: " + (q(n["rom"]) if n["rom"] else ""))
    w("    textEnglishIpa: "); w("    animationCue: " + n["cue"]); w("    voiceClip: {fileID: 0}")
    if n["choices"]:
        w("    choices:")
        for c in n["choices"]:
            w("    - textJa: " + q(c["ja"])); w("      textEn: " + q(c["vi"])); w("      nextNodeId: " + c["nxt"])
            if c["mods"]:
                w("      scoreModifiers:")
                for m in c["mods"]:
                    w("      - category: " + m[0]); w("        value: %d" % m[1]); w("        reason: " + q(m[2]))
            else:
                w("      scoreModifiers: []")
            if c["g"]:
                w("      grammarTags:")
                for t in c["g"]: w("      - " + t)
            else: w("      grammarTags: []")
            if c["v"]:
                w("      vocabularyTags:")
                for t in c["v"]: w("      - " + t)
            else: w("      vocabularyTags: []")
    else:
        w("    choices: []")
    w("    targetItemId: "); w("    targetNpcId: "); w("    targetAreaId: ")
w("  startNodeId: n_arrive")
open(sys.argv[1], "w", encoding="utf-8", newline="\n").write("\n".join(out) + "\n")
print("nodes", len(nodes))
