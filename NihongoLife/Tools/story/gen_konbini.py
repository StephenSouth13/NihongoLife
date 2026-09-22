# -*- coding: utf-8 -*-
# Deepens scenario.konbini.buy_onigiri while preserving the exact gameplay wiring that
# ScenarioManager / the scene rely on: node types (GoToArea/InspectItem/CollectItem) with
# targetAreaId "store_entrance"/"cashier" and targetItemId "water"/"onigiri", and the node id
# "node_transaction_done" (ScenarioManager.cs checks this id literally to resolve checkout).
import sys

from scen_lib import Scenario

S = Scenario()
C, N = S.C, S.N
ITO = ("Ito", "npc_cashier")
NAR = None

# ───────── Vào cửa hàng ─────────
N("node_start", None, "", "", "", "", "", "n_greet", "obj_enter_store", type=3, area="store_entrance")

N("n_greet", ITO, "いらっしゃいませ！", "いらっしゃいませ！", "Irasshaimase!",
  "Xin chào quý khách!", "bow", choices=[
    C("こんにちは。", "Xin chào ạ.", "n_task", "Vocabulary", 8, "Đáp lại lời chào của nhân viên bằng こんにちは", [], ["vocab.n5.konnichiwa"]),
    C("どうも。", "Chào.", "n_greet_casual", "ResponseAccuracy", 2, "Đáp được nhưng hơi suồng sã với nhân viên cửa hàng", [], []),
])
N("n_greet_casual", ITO, "いらっしゃいませ。ごゆっくりどうぞ。", "いらっしゃいませ。ごゆっくりどうぞ。", "Irasshaimase. Goyukkuri douzo.",
  "Xin chào quý khách. Mời quý khách xem thong thả ạ.", "bow", "n_task")

N("n_task", NAR, "今日は、おにぎりと飲み物を買いに来ました。お店の中を見てみましょう。", "きょうは、おにぎりとのみものをかいにきました。おみせのなかをみてみましょう。", "Kyou wa, onigiri to nomimono wo kai ni kimashita. Omise no naka wo mite mimashou.",
  "Hôm nay bạn đến mua cơm nắm và đồ uống. Hãy xem xung quanh cửa hàng.", "", "node_check_drink")

# ───────── Kệ đồ uống ─────────
N("node_check_drink", None, "", "", "", "", "", "n_drink_react", "obj_check_drink", type=2, item="water")

N("n_drink_react", NAR, "冷蔵庫に、水、お茶、ジュースがあります。「水」と書いてあるラベルを見つけました。", "れいぞうこに、みず、おちゃ、ジュースがあります。「みず」とかいてあるラベルをみつけました。", "Reizouko ni, mizu, ocha, juusu ga arimasu. \"Mizu\" to kaite aru raberu wo mitsukemashita.",
  "Trong tủ lạnh có nước, trà và nước ép. Bạn tìm thấy nhãn có ghi 「水」 (nước).", "", "n_shelf")

# ───────── Kệ đồ ăn: quyết định mua gì (nhiều distractor) ─────────
N("n_shelf", ITO, "何かお探しですか。", "なにかおさがしですか。", "Nani ka osagashi desu ka?",
  "Quý khách đang tìm gì ạ?", "talk", "node_confirm_item")

N("node_confirm_item", NAR, "今日買うものはどれですか？", "きょうかうものはどれですか？", "Kyou kau mono wa dore desu ka?",
  "Hôm nay bạn cần mua món nào?", "", choices=[
    C("おにぎりをください。", "Cho tôi cơm nắm ạ.", "node_confirm_item_done", "Grammar", 10, "Chọn đúng vật phẩm bằng mẫu 〜をください", ["grammar.n5.wo_kudasai"], ["vocab.n5.onigiri"]),
    C("パンをください。", "Cho tôi bánh mì ạ.", "node_confirm_item_hint", "Vocabulary", -5, "Nhớ nhầm món: nhiệm vụ hôm nay là mua おにぎり, không phải パン"),
    C("お茶をください。", "Cho tôi trà ạ.", "node_confirm_item_hint", "Vocabulary", -5, "Nhầm món cần mua"),
    C("水をください。", "Cho tôi nước ạ.", "node_confirm_item_hint", "ResponseAccuracy", -5, "Cần đọc lại yêu cầu nhiệm vụ"),
])
N("node_confirm_item_hint", ITO, "今日はおにぎりを買います。棚を見てくださいね。", "きょうはおにぎりをかいます。たなをみてくださいね。", "Kyou wa onigiri wo kaimasu. Tana wo mite kudasai ne.",
  "Hôm nay nhiệm vụ là mua cơm nắm. Mời quý khách xem trên kệ hàng nhé.", "point", "node_confirm_item")
N("node_confirm_item_done", ITO, "はい、おにぎりですね。あちらの棚にありますよ。", "はい、おにぎりですね。あちらのたなにありますよ。", "Hai, onigiri desu ne. Achira no tana ni arimasu yo.",
  "Vâng, cơm nắm nhỉ. Ở kệ đằng kia có đấy ạ.", "point", "n_flavor", "obj_confirm_item")

# ───────── Chọn vị onigiri (từ vựng N5) ─────────
N("n_flavor", NAR, "おにぎりの棚に、うめ、さけ、こんぶ、と書いてあります。", "おにぎりのたなに、うめ、さけ、こんぶ、とかいてあります。", "Onigiri no tana ni, ume, sake, konbu, to kaite arimasu.",
  "Trên kệ có ghi các vị: ume (mơ muối), sake (cá hồi), konbu (rong biển).", "", "node_find_onigiri")
N("node_find_onigiri", None, "", "", "", "", "", "n_flavor_done", "obj_find_onigiri", type=1, item="onigiri")
N("n_flavor_done", NAR, "さけのおにぎりを、かごに入れました。", "さけのおにぎりを、かごにいれました。", "Sake no onigiri wo, kago ni iremashita.",
  "Bạn bỏ một cái onigiri vị cá hồi vào giỏ.", "", "node_go_to_cashier")

# ───────── Đi đến quầy ─────────
N("node_go_to_cashier", None, "", "", "", "", "", "n_cashier_greet", "obj_go_to_cashier", type=3, area="cashier")

N("n_cashier_greet", ITO, "お決まりですか。", "おきまりですか。", "Okimari desu ka?",
  "Quý khách đã chọn xong chưa ạ?", "talk", choices=[
    C("はい、これをお願いします。", "Vâng, cho tôi cái này ạ.", "node_cashier_prompt_bag", "Grammar", 10, "Đưa món ra thanh toán bằng mẫu 〜をお願いします", ["grammar.n5.wo_onegaishimasu"], []),
    C("はい。", "Vâng.", "node_cashier_prompt_bag", "ResponseAccuracy", 4, "Đáp được nhưng ngắn, nên nói kèm これをお願いします khi đưa đồ ra", [], []),
])

# ───────── Túi ─────────
N("node_cashier_prompt_bag", ITO, "袋は要りますか？", "ふくろはいりますか？", "Fukuro wa irimasu ka?",
  "Quý khách có cần túi không?", "talk", choices=[
    C("はい、お願いします。", "Vâng, xin cho tôi ạ.", "n_bag_yes", "Vocabulary", 10, "Hiểu từ túi (fukuro) và dùng mẫu 〜お願いします", ["grammar.n5.wo_onegaishimasu"], ["vocab.n5.fukuro"]),
    C("いいえ、大丈夫です。", "Không, tôi ổn rồi ạ.", "n_bag_no", "Vocabulary", 10, "Hiểu từ túi (fukuro) và từ chối lịch sự bằng 大丈夫です", ["grammar.n5.daijoubu_desu"], ["vocab.n5.fukuro"]),
    C("袋を要ります。", "Tôi cần một túi.", "node_bag_wrong_grammar", "Grammar", -10, "Sai trợ từ: 要る thường đi với が hoặc は, không phải を"),
])
N("node_bag_wrong_grammar", ITO, "すみません、袋は要りますか、ということですね。かしこまりました。", "すみません、ふくろはいりますか、ということですね。かしこまりました。", "Sumimasen, fukuro wa irimasu ka, to iu koto desu ne. Kashikomarimashita.",
  "Xin lỗi, ý quý khách là 「袋は要りますか」 đúng không ạ. Tôi hiểu rồi ạ.", "point", "n_bag_yes")

N("n_bag_yes", ITO, "かしこまりました。袋代3円になります。", "かしこまりました。ふくろだいさんえんになります。", "Kashikomarimashita. Fukurodai san-en ni narimasu.",
  "Vâng ạ. Tiền túi là 3 yên.", "talk", "n_total")
N("n_bag_no", ITO, "かしこまりました。", "かしこまりました。", "Kashikomarimashita.",
  "Vâng, tôi hiểu rồi ạ.", "talk", "n_total")

# ───────── Đọc tổng tiền ─────────
N("n_total", ITO, "お会計は、497円です。", "おかいけいは、よんひゃくきゅうじゅうななえんです。", "Okaikei wa, yonhyaku kyuujuu nana en desu.",
  "Tổng cộng là 497 yên ạ.", "talk", choices=[
    C("497円ですね。分かりました。", "497 yên nhỉ. Cháu hiểu rồi ạ.", "node_pay_choice", "Vocabulary", 10, "Xác nhận đúng số tiền: 四百九十七円", ["grammar.n5.desu_ne"], ["vocab.n5.yonhyaku", "vocab.n5.en"]),
    C("もう一度お願いします。", "Phiền chị nói lại một lần nữa ạ.", "n_total_repeat", "ResponseAccuracy", 6, "Xin nghe lại lịch sự khi chưa chắc số tiền", [], ["vocab.n5.mou_ichido"]),
    C("598円ですね。", "598 yên nhỉ.", "n_total_wrong", "Vocabulary", -8, "Nghe nhầm số tiền: 497 khác 598"),
])
N("n_total_repeat", ITO, "はい。四百、九十七円です。", "はい。よんひゃく、きゅうじゅうななえんです。", "Hai. Yonhyaku, kyuujuu nana en desu.",
  "Vâng ạ. Bốn trăm, chín mươi bảy yên.", "point", "n_total")
N("n_total_wrong", ITO, "いいえ、497円ですよ。もう一度、聞いてくださいね。", "いいえ、よんひゃくきゅうじゅうななえんですよ。もういちど、きいてくださいね。", "Iie, yonhyaku kyuujuu nana en desu yo. Mou ichido, kiite kudasai ne.",
  "Không đâu ạ, là 497 yên. Mời quý khách nghe lại nhé.", "point", "n_total")

# ───────── Thanh toán ─────────
N("node_pay_choice", NAR, "支払方法を選択してください。", "しはらいほうほうをせんたくしてください。", "Shiharai houhou wo sentaku shite kudasai.",
  "Hãy chọn phương thức thanh toán.", "", choices=[
    C("これでお願いします。", "Cho tôi thanh toán bằng cái này ạ.", "n_pay_confirm", "Grammar", 15, "Đưa tiền/thẻ ra bằng mẫu これでお願いします", ["grammar.n5.de_onegaishimasu"], []),
    C("カードで払います。", "Tôi thanh toán bằng thẻ ạ.", "n_pay_confirm", "Vocabulary", 10, "Nói rõ phương thức bằng 〜で払います", ["grammar.n5.de_harau"], ["vocab.n5.kaado"]),
])
N("n_pay_confirm", ITO, "はい、確かに。", "はい、たしかに。", "Hai, tashika ni.",
  "Vâng, tôi đã nhận đủ ạ.", "point", "node_transaction_done")

# ───────── Cảm ơn và tạm biệt (giữ nguyên id node_transaction_done: ScenarioManager kiểm tra id này) ─────────
N("node_transaction_done", ITO, "ありがとうございます。またお越しくださいませ。", "ありがとうございます。またおこしくださいませ。", "Arigatou gozaimasu. Mata okoshi kudasaimase.",
  "Xin cảm ơn quý khách. Hẹn gặp lại quý khách lần sau ạ.", "bow", "n_farewell", "obj_pay")
N("n_farewell", NAR, "レシートを受け取って、お店を出ます。何と言いますか。", "レシートをうけとって、おみせをでます。なんといいますか。", "Reshiito wo uketotte, omise wo demasu. Nan to iimasu ka?",
  "Bạn nhận hóa đơn rồi ra khỏi cửa hàng. Bạn nói gì?", "", choices=[
    C("ありがとうございました。", "Cảm ơn chị ạ.", "n_recap", "Vocabulary", 10, "Cảm ơn nhân viên bằng thì quá khứ ありがとうございました khi rời đi", [], ["vocab.n5.arigatou_gozaimashita"]),
    C("さようなら。", "Tạm biệt.", "n_recap", "ResponseAccuracy", 3, "Chào tạm biệt được nhưng ở cửa hàng nên nói lời cảm ơn hơn là さようなら", [], []),
])
N("n_recap", NAR, "今日のポイント：「〜をください」「〜は要りますか」「〜でお願いします」。", "きょうのポイント：「〜をください」「〜はいりますか」「〜でおねがいします」。", "Kyou no pointo: \"~ wo kudasai\" \"~ wa irimasu ka\" \"~ de onegaishimasu\".",
  "Tóm tắt: \"~ wo kudasai\" (cho tôi ~), \"~ wa irimasu ka\" (bạn có cần ~ không?), \"~ de onegaishimasu\" (thanh toán bằng ~). Nhớ đọc kỹ nhãn hàng trước khi chọn!", "", "node_complete")
N("node_complete", NAR, "", "", "", "", "", "", type=4)

objectives = [
    ("obj_enter_store", "コンビニに入る", "Vào cửa hàng tiện lợi", False),
    ("obj_check_drink", "飲み物のたなを確認する", "Kiểm tra kệ đồ uống", False),
    ("obj_confirm_item", "買うものを選ぶ", "Chọn đúng món cần mua", False),
    ("obj_find_onigiri", "おにぎりを見つける", "Tìm và lấy cơm nắm", False),
    ("obj_go_to_cashier", "レジへ行く", "Đi đến quầy thu ngân", False),
    ("obj_pay", "お金を払う", "Thanh toán", False),
]
targets = ["grammar.n5.wo_kudasai", "grammar.n5.wo_onegaishimasu", "grammar.n5.de_onegaishimasu", "vocab.n5.onigiri", "vocab.n5.fukuro"]

problems = S.validate("node_start", objectives)
print("problems:", problems)
if not problems:
    S.write(sys.argv[1], name="scenario_konbini_buy_onigiri", sid="scenario.konbini.buy_onigiri", title_ja="コンビニでおにぎりを買う", title_vi="Mua cơm nắm ở cửa hàng tiện lợi",
            desc_ja="ひばりコンビニで、おにぎりと飲み物を見て、お会計をしましょう。", desc_vi="Vào cửa hàng Hibari, tìm đúng món trên kệ, mua cơm nắm và thanh toán bằng tiếng Nhật.",
            chapter=2, targets=targets, objectives=objectives, start="node_start")
    print("nodes", len(S.nodes))
