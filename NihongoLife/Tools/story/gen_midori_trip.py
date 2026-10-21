# -*- coding: utf-8 -*-
"""Chapter 5 — みどり島の畑へ. The quest that opens Midori Island (LocationUnlocks gateway): Tanaka takes you by train
to his grandmother's field. Callbacks: Kim and the Game Center (kim.friend), Kimura and the ticket machine (station
quest). Run: python gen_midori_trip.py <asset path>"""
import sys

from scen_lib import Scenario
from gen_en import V, write_en

S = Scenario()
C, N, B = S.C, S.N, S.B
TA = ("Tanaka", "npc_neighbor_1")
NAR = None

N("node_find_tanaka", None, "", "", "", "", "", "n_hello", "obj_meet", type=6, npc="npc_neighbor_1")
B("n_hello", "kim.friend", "n_hello_kim", "n_hello_plain")

N("n_hello_kim", TA, "こんにちは。キムさんと、ゲームセンターに行ったんですね。たのしかったですか。", "こんにちは。キムさんと、ゲームセンターにいったんですね。たのしかったですか。", "Konnichiwa. Kimu-san to, geemu sentaa ni itta n desu ne. Tanoshikatta desu ka.",
  V("Chào cháu. Nghe nói cháu đi Game Center với Kim à. Có vui không?", "Hello. I hear you went to the Game Center with Kim. Was it fun?"), "talk", choices=[
    C("はい、とてもたのしかったです。", V("Vâng, vui lắm ạ.", "Yes, it was a lot of fun."), "n_invite", "Grammar", 8,
      V("Trả lời bằng quá khứ たのしかったです", "Answering in the past with たのしかったです"), ["grammar.n5.i_adj_past"], ["vocab.n5.tanoshii"]),
])
N("n_hello_plain", TA, "こんにちは。さいきん、日本語が、じょうずになりましたね。", "こんにちは。さいきん、にほんごが、じょうずになりましたね。", "Konnichiwa. Saikin, nihongo ga, jouzu ni narimashita ne.",
  V("Chào cháu. Dạo này tiếng Nhật của cháu giỏi lên nhiều đấy.", "Hello. Your Japanese has really improved lately."), "talk", choices=[
    C("いいえ、まだまだです。", V("Dạ không, cháu còn kém lắm ạ.", "Oh no, I still have a long way to go."), "n_invite", "ResponseAccuracy", 10,
      V("Khiêm tốn kiểu Nhật khi được khen: まだまだです", "Japanese modesty when praised: まだまだです"), [], ["vocab.n5.madamada"]),
    C("はい、じょうずです。", V("Vâng, cháu giỏi ạ.", "Yes, I'm good."), "n_invite_proud", "ResponseAccuracy", 2,
      V("Nhận lời khen thẳng thắn; người Nhật thường khiêm tốn hơn", "Accepting praise outright; Japanese people are usually more modest")),
])
N("n_invite_proud", TA, "ははは、元気ですね。日本では、「まだまだです」と言う人が多いですよ。", "ははは、げんきですね。にほんでは、「まだまだです」というひとがおおいですよ。", "Hahaha, genki desu ne. Nihon de wa, \"madamada desu\" to iu hito ga ooi desu yo.",
  V("Haha, tự tin ghê. Ở Nhật nhiều người hay nói 「まだまだです」 (còn kém lắm) đấy.", "Haha, confident! In Japan many people say 「まだまだです」 (still a long way to go)."), "talk", "n_invite")

# ───────── Lời mời ra đảo ─────────
N("n_invite", TA, "じつは、わたしのおばあちゃんが、みどり島に畑を持っています。週末、いっしょに行きませんか。", "じつは、わたしのおばあちゃんが、みどりじまにはたけをもっています。しゅうまつ、いっしょにいきませんか。", "Jitsu wa, watashi no obaachan ga, Midori-jima ni hatake wo motte imasu. Shuumatsu, issho ni ikimasen ka.",
  V("Thật ra bà của bác có một mảnh ruộng trên đảo Midori. Cuối tuần cháu đi cùng không?", "Actually, my grandmother has a field on Midori Island. Would you like to come with me this weekend?"), "talk", choices=[
    C("ぜひ行きたいです！何で行きますか。", V("Cháu rất muốn đi ạ! Mình đi bằng gì ạ?", "I'd love to go! How do we get there?"), "n_how", "Grammar", 10,
      V("Nhận lời bằng ぜひ〜たいです và hỏi phương tiện bằng 何で", "Accepting with ぜひ〜たいです and asking the transport with 何で"), ["grammar.n5.tai_desu", "grammar.n5.nan_de"], ["vocab.n5.shuumatsu"], ["island.invited"]),
    C("はい、行きます。", V("Vâng, cháu đi ạ.", "Yes, I'll go."), "n_how", "ResponseAccuracy", 6,
      V("Nhận lời được; thêm ぜひ thì thể hiện háo hức hơn", "Fine; adding ぜひ sounds keener"), [], [], ["island.invited"]),
    C("畑は、なんですか。", V("Ruộng là gì ạ?", "What is a 'hatake'?"), "n_field_word", "Vocabulary", 6,
      V("Hỏi nghĩa từ mới bằng 〜は、なんですか", "Asking what a new word means with 〜は、なんですか"), [], ["vocab.n5.hatake"]),
])
N("n_field_word", TA, "畑は、やさいを作るところです。トマトや、にんじんを作りますよ。", "はたけは、やさいをつくるところです。トマトや、にんじんをつくりますよ。", "Hatake wa, yasai wo tsukuru tokoro desu. Tomato ya, ninjin wo tsukurimasu yo.",
  V("Ruộng (hatake) là nơi trồng rau. Trồng cà chua, cà rốt đấy.", "A hatake is a place where you grow vegetables. Tomatoes, carrots and so on."), "point", "n_invite")

N("n_how", TA, "電車で行きます。ひばり駅から、みどり島まで、一時間ぐらいです。切符、一人で買えますか。", "でんしゃでいきます。ひばりえきから、みどりじままで、いちじかんぐらいです。きっぷ、ひとりでかえますか。", "Densha de ikimasu. Hibari eki kara, Midori-jima made, ichijikan gurai desu. Kippu, hitori de kaemasu ka.",
  V("Mình đi tàu. Từ ga Hibari đến đảo Midori mất khoảng một tiếng. Cháu tự mua vé được không?", "We'll take the train. From Hibari Station to Midori Island it's about an hour. Can you buy a ticket by yourself?"), "talk", choices=[
    C("はい、木村さんに、ならいました。", V("Dạ được, cháu đã học với anh Kimura rồi ạ.", "Yes, Mr. Kimura taught me."), "n_station", "Grammar", 10,
      V("Nhắc lại việc đã học bằng 〜に、ならいました", "Saying who taught you with 〜に、ならいました"), ["grammar.n5.ni_naraimashita"], [], ["island.ticket_self"]),
    C("ちょっと、むずかしいです。", V("Hơi khó ạ.", "It's a little difficult."), "n_station_help", "ResponseAccuracy", 6,
      V("Nói thật khó khăn của mình một cách lịch sự", "Being honest about a difficulty, politely"), [], ["vocab.n5.muzukashii"]),
])
N("n_station_help", TA, "だいじょうぶ。いっしょに行きましょう。わたしが、となりにいますよ。", "だいじょうぶ。いっしょにいきましょう。わたしが、となりにいますよ。", "Daijoubu. Issho ni ikimashou. Watashi ga, tonari ni imasu yo.",
  V("Không sao. Mình đi cùng nhau. Bác đứng ngay bên cạnh mà.", "No problem. Let's go together. I'll be right next to you."), "bow", "n_station")

# ───────── Ga Hibari: mua vé ra đảo ─────────
N("n_station", NAR, "週末、ひばり駅。けんばいきで、「みどり島」のボタンをさがします。", "しゅうまつ、ひばりえき。けんばいきで、「みどりじま」のボタンをさがします。", "Shuumatsu, Hibari eki. Kenbaiki de, \"Midori-jima\" no botan wo sagashimasu.",
  V("Cuối tuần, ga Hibari. Bạn tìm nút 「みどり島」 trên máy bán vé.", "The weekend, Hibari Station. You look for the 「みどり島」 button on the ticket machine."), "", "n_ticket", "obj_station")
N("n_ticket", TA, "みどり島までは、六百五十円です。いくら入れますか。", "みどりじままでは、ろっぴゃくごじゅうえんです。いくらいれますか。", "Midori-jima made wa, roppyaku gojuu en desu. Ikura iremasu ka.",
  V("Đến đảo Midori là 650 yên. Cháu bỏ vào bao nhiêu?", "To Midori Island it's 650 yen. How much will you put in?"), "point", choices=[
    C("六百五十円、入れます。", V("Cháu bỏ 650 yên ạ.", "I'll put in 650 yen."), "n_train", "Vocabulary", 10,
      V("Đọc đúng số tiền 六百五十 (ろっぴゃくごじゅう)", "Reading the price right: 六百五十 (ろっぴゃくごじゅう)"), [], ["vocab.n5.roppyaku", "vocab.n5.gojuu"], ["island.ticket_ok"]),
    C("五百六十円、入れます。", V("Cháu bỏ 560 yên ạ.", "I'll put in 560 yen."), "n_ticket_wrong", "Vocabulary", -6,
      V("Nhầm thứ tự chữ số: 650 khác 560", "Digits swapped: 650 is not 560")),
])
N("n_ticket_wrong", TA, "あれ、たりませんよ。ろっぴゃく、ごじゅう。六百五十円です。", "あれ、たりませんよ。ろっぴゃく、ごじゅう。ろっぴゃくごじゅうえんです。", "Are, tarimasen yo. Roppyaku, gojuu. Roppyaku gojuu en desu.",
  V("Ơ, không đủ đâu. Sáu trăm, năm mươi. Là 650 yên.", "Oops, that's not enough. Six hundred, fifty. It's 650 yen."), "point", "n_ticket")

# ───────── Trên tàu: 〜が見えます ─────────
N("n_train", NAR, "電車がうごきだしました。まどの外に、青い海が見えます。", "でんしゃがうごきだしました。まどのそとに、あおいうみがみえます。", "Densha ga ugokidashimashita. Mado no soto ni, aoi umi ga miemasu.",
  V("Tàu bắt đầu chạy. Ngoài cửa sổ có thể thấy biển xanh.", "The train starts moving. Outside the window you can see the blue sea."), "", "n_sea")
N("n_sea", TA, "海が見えますね。何が見えますか。", "うみがみえますね。なにがみえますか。", "Umi ga miemasu ne. Nani ga miemasu ka.",
  V("Thấy biển rồi nhỉ. Cháu thấy gì nào?", "You can see the sea, can't you. What can you see?"), "point", choices=[
    C("海と、小さい島が見えます。", V("Cháu thấy biển và một hòn đảo nhỏ ạ.", "I can see the sea and a small island."), "n_arrive", "Grammar", 10,
      V("Dùng 〜が見えます để nói điều mình nhìn thấy", "Using 〜が見えます for what comes into view"), ["grammar.n5.ga_miemasu"], ["vocab.n5.umi", "vocab.n5.shima"]),
    C("海を見ます。", V("Cháu nhìn biển.", "I look at the sea."), "n_sea_hint", "Grammar", 3,
      V("見ます là chủ động nhìn; điều lọt vào mắt thì dùng 〜が見えます", "見ます is choosing to look; for what you can see, use 〜が見えます")),
])
N("n_sea_hint", TA, "そうですね。でも、ここでは「海が見えます」と言いますよ。", "そうですね。でも、ここでは「うみがみえます」といいますよ。", "Sou desu ne. Demo, koko de wa \"umi ga miemasu\" to iimasu yo.",
  V("Đúng rồi. Nhưng ở đây người ta nói 「海が見えます」 (thấy biển) đấy.", "That's right. But here we say 「海が見えます」 (I can see the sea)."), "talk", "n_arrive")

# ───────── Trên đảo: まず・つぎに・さいごに ─────────
N("n_arrive", NAR, "みどり島に着きました。駅の前に、ひろい畑があります。", "みどりじまにつきました。えきのまえに、ひろいはたけがあります。", "Midori-jima ni tsukimashita. Eki no mae ni, hiroi hatake ga arimasu.",
  V("Đã đến đảo Midori. Trước ga là cánh đồng rộng.", "You've arrived at Midori Island. In front of the station there are wide fields."), "", "n_farm", "obj_arrive")
N("n_farm", TA, "おばあちゃんは、いま、畑で水をやっています。わたしたちも、たねをまきましょう。", "おばあちゃんは、いま、はたけでみずをやっています。わたしたちも、たねをまきましょう。", "Obaachan wa, ima, hatake de mizu wo yatte imasu. Watashitachi mo, tane wo makimashou.",
  V("Bà đang tưới nước ngoài ruộng. Mình cũng gieo hạt nhé.", "Grandma is watering the field right now. Let's sow some seeds too."), "point", choices=[
    C("はい。まず、何をしますか。", V("Vâng. Đầu tiên làm gì ạ?", "Okay. What do we do first?"), "n_steps", "Grammar", 10,
      V("Hỏi thứ tự công việc bằng まず、何をしますか", "Asking the order of work with まず、何をしますか"), [], ["vocab.n5.mazu"]),
    C("たねを食べますか。", V("Mình ăn hạt ạ?", "Do we eat the seeds?"), "n_seed_wrong", "Vocabulary", -4,
      V("たね là hạt giống để gieo (まく), không phải để ăn", "たね are seeds to sow (まく), not to eat")),
])
N("n_seed_wrong", TA, "ははは、たねは食べません。土に「まきます」。", "ははは、たねはたべません。つちに「まきます」。", "Hahaha, tane wa tabemasen. Tsuchi ni \"makimasu\".",
  V("Haha, hạt giống không ăn đâu. Mình 「まきます」 (gieo) xuống đất.", "Haha, we don't eat seeds. We 「まきます」 (sow) them into the soil."), "talk", "n_farm")
N("n_steps", TA, "まず、くわで土をたがやします。つぎに、たねをまいて、さいごに、水をやります。", "まず、くわでつちをたがやします。つぎに、たねをまいて、さいごに、みずをやります。", "Mazu, kuwa de tsuchi wo tagayashimasu. Tsugi ni, tane wo maite, saigo ni, mizu wo yarimasu.",
  V("Đầu tiên dùng cuốc xới đất. Tiếp theo gieo hạt, cuối cùng tưới nước.", "First, you till the soil with a hoe. Next, you sow the seeds, and finally you water them."), "point", "n_steps_check")
N("n_steps_check", TA, "さいごに、何をしますか。", "さいごに、なにをしますか。", "Saigo ni, nani wo shimasu ka.",
  V("Cuối cùng thì làm gì?", "What do we do last?"), "talk", choices=[
    C("水をやります。", V("Tưới nước ạ.", "We water them."), "n_gift", "Vocabulary", 10,
      V("Nhớ đúng thứ tự: たがやす → まく → 水をやる", "Remembering the order: たがやす → まく → 水をやる"), ["grammar.n5.te_form_sequence"], ["vocab.n5.mizu"], ["tanaka.farm"]),
    C("たねをまきます。", V("Gieo hạt ạ.", "We sow the seeds."), "n_steps_wrong", "Vocabulary", -4,
      V("Gieo hạt là bước thứ hai, cuối cùng là tưới nước", "Sowing is the second step; watering comes last")),
])
N("n_steps_wrong", TA, "それは、つぎです。まず、たがやす。つぎに、まく。さいごに…？", "それは、つぎです。まず、たがやす。つぎに、まく。さいごに…？", "Sore wa, tsugi desu. Mazu, tagayasu. Tsugi ni, maku. Saigo ni…?",
  V("Đó là bước tiếp theo. Đầu tiên xới, tiếp theo gieo. Cuối cùng là…?", "That's the next step. First, till. Next, sow. And last…?"), "point", "n_steps_check")

N("n_gift", TA, "おばあちゃんが、「この畑、使っていいよ」と言っています。あなたの畑ですよ！", "おばあちゃんが、「このはたけ、つかっていいよ」といっています。あなたのはたけですよ！", "Obaachan ga, \"kono hatake, tsukatte ii yo\" to itte imasu. Anata no hatake desu yo!",
  V("Bà nói 「ruộng này, cháu cứ dùng đi」. Đây là ruộng của cháu đấy!", "Grandma says, “You can use this field.” It's your field now!"), "bow", choices=[
    C("ほんとうですか。ありがとうございます！大切にします。", V("Thật ạ? Cháu cảm ơn ạ! Cháu sẽ chăm sóc cẩn thận.", "Really? Thank you so much! I'll take good care of it."), "n_recap", "Grammar", 10,
      V("Cảm ơn và hứa bằng 大切にします", "Thanking and promising with 大切にします"), [], ["vocab.n5.taisetsu", "vocab.n5.arigatou_gozaimasu"], ["island.field"]),
    C("ありがとう。", V("Cảm ơn.", "Thanks."), "n_recap", "ResponseAccuracy", 3,
      V("Với người lớn tuổi nên dùng ありがとうございます", "With an elder, ありがとうございます is better"), [], [], ["island.field"]),
])
N("n_recap", NAR, "きょうのポイント：「〜で行きます」「〜が見えます」「まず・つぎに・さいごに」「〜てもいい」。", "きょうのポイント：「〜でいきます」「〜がみえます」「まず・つぎに・さいごに」「〜てもいい」。", "Kyou no pointo: \"~de ikimasu\" \"~ga miemasu\" \"mazu, tsugi ni, saigo ni\" \"~te mo ii\".",
  V("Tóm tắt: 〜で行きます (đi bằng), 〜が見えます (nhìn thấy), まず・つぎに・さいごに (trình tự), 〜てもいい (được phép). Ruộng của bạn trên đảo đã sẵn sàng!",
    "Today's points: ~de ikimasu (go by), ~ga miemasu (can see), mazu / tsugi ni / saigo ni (in order), ~te mo ii (you may). Your field on the island is ready!"), "", "n_complete", "obj_field")
N("n_complete", NAR, "", "", "", "", "", "", type=4)

objectives = [
    ("obj_meet", "田中さんに会う", V("Gặp bác Tanaka", "Meet Mr. Tanaka"), False),
    ("obj_station", "ひばり駅で切符を買う", V("Mua vé ở ga Hibari", "Buy a ticket at Hibari Station"), False),
    ("obj_arrive", "みどり島に着く", V("Đến đảo Midori", "Arrive at Midori Island"), False),
    ("obj_field", "畑をもらう", V("Nhận mảnh ruộng của bà", "Receive Grandma's field"), False),
]
targets = ["grammar.n5.nan_de", "grammar.n5.ga_miemasu", "grammar.n5.te_mo_ii", "vocab.n5.hatake", "vocab.n5.shima"]

problems = S.validate("node_find_tanaka", objectives)
print("problems:", problems)
if not problems:
    S.write(sys.argv[1], name="scenario_island_midori_trip", sid="scenario.island.midori_trip", title_ja="みどり島の畑へ",
            title_vi=V("Ra ruộng trên đảo Midori", "To the Fields of Midori Island"),
            desc_ja="田中さんと電車でみどり島へ。おばあちゃんの畑で、たがやす・まく・水をやるを学びます。",
            desc_vi=V("Cùng bác Tanaka đi tàu ra đảo Midori: mua vé, ngắm biển từ cửa sổ tàu, học cách xới đất, gieo hạt, tưới nước ở ruộng của bà.",
                      "Take the train to Midori Island with Mr. Tanaka: buy the ticket, watch the sea from the window, and learn to till, sow and water in his grandmother's field."),
            chapter=5, targets=targets, objectives=objectives, start="node_find_tanaka")
    write_en(sys.argv[1], "island_midori_trip")
    print("nodes", len(S.nodes))
