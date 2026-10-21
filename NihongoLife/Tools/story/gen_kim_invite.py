# -*- coding: utf-8 -*-
"""Chapter 4 — キムさんとゲームセンター. The quest that opens the Game Center (LocationUnlocks gateway).
Theme 「間違えてもいい」: Kim gets things wrong, laughs, and keeps going. Run: python gen_kim_invite.py <asset path>"""
import sys

from scen_lib import Scenario
from gen_en import V, write_en

S = Scenario()
C, N = S.C, S.N
KIM = ("Kim", "npc_classmate_kim")
NAR = None

N("node_find_kim", None, "", "", "", "", "", "n_greet", "obj_meet", type=6, npc="npc_classmate_kim")

# ───────── Sau giờ học ─────────
N("n_greet", KIM, "あ、おつかれさまです！じゅぎょう、むずかしかったですね。", "あ、おつかれさまです！じゅぎょう、むずかしかったですね。", "A, otsukaresama desu! Jugyou, muzukashikatta desu ne.",
  V("A, cậu vất vả rồi! Tiết học khó nhỉ.", "Oh, good work today! Class was tough, wasn't it?"), "talk", choices=[
    C("はい、むずかしかったです。でも、おもしろかったです。", V("Ừ, khó thật. Nhưng thú vị.", "Yes, it was hard. But it was fun."), "n_invite", "Grammar", 10,
      V("Dùng quá khứ của tính từ đuôi い: むずかしかった, おもしろかった", "Past tense of i-adjectives: むずかしかった, おもしろかった"), ["grammar.n5.i_adj_past"], ["vocab.n5.muzukashii", "vocab.n5.omoshiroi"]),
    C("おつかれさまです。", V("Cậu cũng vất vả rồi.", "Good work to you too."), "n_invite", "ResponseAccuracy", 6,
      V("Đáp lại おつかれさまです đúng phép", "Answering おつかれさまです properly"), [], ["vocab.n5.otsukaresama"]),
    C("むずかしいでした。", V("Đã khó. (sai ngữ pháp)", "It was difficult. (wrong grammar)"), "n_greet_mistake", "Grammar", -6,
      V("Tính từ đuôi い không chia quá khứ bằng でした: phải là むずかしかったです", "An i-adjective does not take でした in the past: it is むずかしかったです")),
])
N("n_greet_mistake", KIM, "あはは、わたしも、よくまちがえます！「むずかしかったです」ですよ。いっしょにがんばりましょう。", "あはは、わたしも、よくまちがえます！「むずかしかったです」ですよ。いっしょにがんばりましょう。", "Ahaha, watashi mo, yoku machigaemasu! \"Muzukashikatta desu\" desu yo. Issho ni ganbarimashou.",
  V("Haha, tớ cũng hay sai lắm! Phải là 「むずかしかったです」 đấy. Cùng cố gắng nhé.", "Haha, I make that mistake all the time! It's 「むずかしかったです」. Let's do our best together."), "talk", "n_invite")

# ───────── Lời mời (〜ませんか / 〜ましょう) ─────────
N("n_invite", KIM, "ねえ、このあと、ひまですか。ゲームセンターに行きませんか。", "ねえ、このあと、ひまですか。ゲームセンターにいきませんか。", "Nee, kono ato, hima desu ka. Geemu sentaa ni ikimasen ka.",
  V("Này, lát nữa cậu rảnh không? Đi Game Center không?", "Hey, are you free after this? Want to go to the Game Center?"), "talk", choices=[
    C("いいですね。行きましょう！", V("Hay đấy. Đi thôi!", "Sounds good. Let's go!"), "n_walk", "Grammar", 10,
      V("Nhận lời 〜ませんか bằng 〜ましょう", "Accepting 〜ませんか with 〜ましょう"), ["grammar.n5.masen_ka", "grammar.n5.mashou"], ["vocab.n5.geemusentaa"], ["kim.arcade"]),
    C("すみません、きょうは、ちょっと…。", V("Xin lỗi, hôm nay thì hơi…", "Sorry, today is a bit…"), "n_invite_later", "ResponseAccuracy", 6,
      V("Từ chối khéo kiểu Nhật bằng ちょっと…", "Declining gently the Japanese way with ちょっと…"), ["grammar.n5.chotto"], ["vocab.n5.sumimasen"]),
    C("ゲームセンターに行きませんか。", V("Đi Game Center không? (nhắc lại câu hỏi)", "Want to go to the Game Center? (repeating the question)"), "n_invite_echo", "ResponseAccuracy", -4,
      V("Lặp lại câu mời thay vì trả lời", "Repeating the invitation instead of answering it")),
])
N("n_invite_echo", KIM, "あはは、それは、わたしのしつもんですよ！「はい、行きましょう」か「すみません」ですね。", "あはは、それは、わたしのしつもんですよ！「はい、いきましょう」か「すみません」ですね。", "Ahaha, sore wa, watashi no shitsumon desu yo! \"Hai, ikimashou\" ka \"sumimasen\" desu ne.",
  V("Haha, đó là câu tớ hỏi mà! Phải trả lời 「はい、行きましょう」 hoặc 「すみません」 chứ.", "Haha, that's my question! You answer with 「はい、行きましょう」 or 「すみません」."), "talk", "n_invite")
N("n_invite_later", KIM, "そうですか。じゃあ、あしたはどうですか。ゲームで、かずのれんしゅうができますよ！", "そうですか。じゃあ、あしたはどうですか。ゲームで、かずのれんしゅうができますよ！", "Sou desu ka. Jaa, ashita wa dou desu ka. Geemu de, kazu no renshuu ga dekimasu yo!",
  V("Vậy à. Thế ngày mai thì sao? Chơi game mà luyện được số đếm đấy!", "I see. How about tomorrow then? You can practise numbers with the games!"), "talk", choices=[
    C("あしたなら、だいじょうぶです。行きましょう。", V("Ngày mai thì được. Đi nhé.", "Tomorrow is fine. Let's go."), "n_walk_tomorrow", "Grammar", 8,
      V("Dùng なら để nêu điều kiện, nhận lời bằng 〜ましょう", "なら for a condition, accepting with 〜ましょう"), ["grammar.n5.nara", "grammar.n5.mashou"], ["vocab.n5.ashita"], ["kim.arcade"]),
])
N("n_walk_tomorrow", NAR, "つぎの日、じゅぎょうのあと、キムさんとゲームセンターへ行きました。", "つぎのひ、じゅぎょうのあと、キムさんとゲームセンターへいきました。", "Tsugi no hi, jugyou no ato, Kimu-san to geemu sentaa e ikimashita.",
  V("Hôm sau, tan học, bạn cùng Kim đến Game Center.", "The next day, after class, you went to the Game Center with Kim."), "", "n_arrive")
N("n_walk", NAR, "キムさんと、さくら通りをあるいて、ゲームセンターへ行きました。", "キムさんと、さくらどおりをあるいて、ゲームセンターへいきました。", "Kimu-san to, Sakura-doori wo aruite, geemu sentaa e ikimashita.",
  V("Bạn cùng Kim đi dọc phố Sakura đến Game Center.", "You walked along Sakura-dori with Kim to the Game Center."), "", "n_arrive")
N("n_arrive", NAR, "ゲームセンターは、音と光で、いっぱいです。", "ゲームセンターは、おととひかりで、いっぱいです。", "Geemu sentaa wa, oto to hikari de, ippai desu.",
  V("Game Center đầy âm thanh và ánh sáng.", "The Game Center is full of sound and light."), "", "n_choose", "obj_arrive")

# ───────── Chọn trò chơi ─────────
N("n_choose", KIM, "どれをしますか。UFOキャッチャーと、バスケと、かなレインがあります。", "どれをしますか。ユーフォーキャッチャーと、バスケと、かなレインがあります。", "Dore wo shimasu ka. Yuufoo kyacchaa to, basuke to, kana rein ga arimasu.",
  V("Chơi cái nào đây? Có máy gắp UFO, bóng rổ và Kana Rain.", "Which one shall we play? There's the UFO catcher, basketball and Kana Rain."), "point", choices=[
    C("バスケをしましょう。", V("Chơi bóng rổ đi.", "Let's play basketball."), "n_basket", "Grammar", 10,
      V("Đề nghị cùng làm bằng 〜ましょう", "Suggesting something with 〜ましょう"), ["grammar.n5.mashou"], ["vocab.n5.basuke"], ["kim.basket"]),
    C("UFOキャッチャーがいいです。", V("Máy gắp UFO được đấy.", "The UFO catcher sounds good."), "n_ufo", "Grammar", 10,
      V("Chọn bằng 〜がいいです", "Choosing with 〜がいいです"), ["grammar.n5.ga_ii_desu"], [], ["kim.ufo"]),
    C("ゲームを食べます。", V("Ăn trò chơi. (sai động từ)", "I eat the game. (wrong verb)"), "n_choose_wrong", "Vocabulary", -6,
      V("Dùng sai động từ: trò chơi thì dùng する, không phải 食べる", "Wrong verb: games take する, not 食べる")),
])
N("n_choose_wrong", KIM, "あはは、ゲームは食べません！ゲームは「します」ですよ。", "あはは、ゲームはたべません！ゲームは「します」ですよ。", "Ahaha, geemu wa tabemasen! Geemu wa \"shimasu\" desu yo.",
  V("Haha, trò chơi thì không ăn được! Trò chơi thì dùng 「します」 đấy.", "Haha, you don't eat games! For games you say 「します」."), "talk", "n_choose")

N("n_basket", KIM, "バスケは、ボールを三回なげます。いっしょにかぞえましょう。いっかい、にかい…", "バスケは、ボールをさんかいなげます。いっしょにかぞえましょう。いっかい、にかい…", "Basuke wa, booru wo sankai nagemasu. Issho ni kazoemashou. Ikkai, nikai…",
  V("Bóng rổ thì ném bóng 3 lần. Cùng đếm nhé. Một lần, hai lần…", "In basketball you throw the ball three times. Let's count together. Once, twice…"), "point", choices=[
    C("さんかい！", V("Ba lần!", "Three times!"), "n_basket_score", "Vocabulary", 10,
      V("Đếm số lần đúng với 〜回: いっかい、にかい、さんかい", "Counting times with 〜回: いっかい、にかい、さんかい"), [], ["vocab.n5.kai", "vocab.n5.san"]),
    C("みっつ！", V("Ba cái!", "Three (things)!"), "n_basket_counter", "Vocabulary", 2,
      V("みっつ dùng đếm đồ vật; đếm số lần thì dùng 〜回: さんかい", "みっつ counts things; for times use 〜回: さんかい"), [], ["vocab.n5.mittsu"]),
])
N("n_basket_counter", KIM, "おしい！ものは「みっつ」、でも、なげるのは「さんかい」です。", "おしい！ものは「みっつ」、でも、なげるのは「さんかい」です。", "Oshii! Mono wa \"mittsu\", demo, nageru no wa \"sankai\" desu.",
  V("Suýt đúng! Đồ vật thì 「みっつ」, còn ném thì 「さんかい」.", "Close! Objects are 「みっつ」, but throws are 「さんかい」."), "point", "n_basket_score")
N("n_basket_score", NAR, "ボールが、二回、はいりました！チケットが、五まい出てきました。", "ボールが、にかい、はいりました！チケットが、ごまいでてきました。", "Booru ga, nikai, hairimashita! Chiketto ga, gomai dete kimashita.",
  V("Bóng vào rổ 2 lần! Máy nhả ra 5 tấm vé.", "The ball went in twice! Five tickets came out."), "", "n_kim_turn", "obj_play")

N("n_ufo", KIM, "UFOキャッチャーは、ボタンを二回おします。どのぬいぐるみが、ほしいですか。", "ユーフォーキャッチャーは、ボタンをにかいおします。どのぬいぐるみが、ほしいですか。", "Yuufoo kyacchaa wa, botan wo nikai oshimasu. Dono nuigurumi ga, hoshii desu ka.",
  V("Máy gắp UFO thì bấm nút 2 lần. Cậu muốn con thú bông nào?", "With the UFO catcher you press the button twice. Which plush do you want?"), "point", choices=[
    C("あのパンダが、ほしいです。", V("Tớ muốn con gấu trúc kia.", "I want that panda."), "n_ufo_try", "Grammar", 10,
      V("Nói muốn có đồ vật bằng 〜がほしいです", "Saying you want something with 〜がほしいです"), ["grammar.n5.ga_hoshii"], ["vocab.n5.nuigurumi"], ["kim.panda"]),
    C("あのパンダを、ほしいです。", V("Tớ muốn con gấu trúc kia. (sai trợ từ)", "I want that panda. (wrong particle)"), "n_ufo_particle", "Grammar", -4,
      V("ほしい đi với が, không phải を", "ほしい takes が, not を")),
])
N("n_ufo_particle", KIM, "わたしも、まえは「を」と言いました。でも、「ほしい」は「が」ですよ。", "わたしも、まえは「を」といいました。でも、「ほしい」は「が」ですよ。", "Watashi mo, mae wa \"wo\" to iimashita. Demo, \"hoshii\" wa \"ga\" desu yo.",
  V("Trước tớ cũng nói 「を」. Nhưng 「ほしい」 thì đi với 「が」 đấy.", "I used to say 「を」 too. But 「ほしい」 goes with 「が」."), "talk", "n_ufo_try")
N("n_ufo_try", NAR, "いっかい、にかい…ボタンをおしました。パンダが、とれました！", "いっかい、にかい…ボタンをおしました。パンダが、とれました！", "Ikkai, nikai… botan wo oshimashita. Panda ga, toremashita!",
  V("Một lần, hai lần… bạn bấm nút. Gắp được gấu trúc rồi!", "Once, twice… you pressed the button. You caught the panda!"), "", "n_kim_turn", "obj_play")

# ───────── Lượt của Kim: nói sai cũng không sao ─────────
N("n_kim_turn", KIM, "つぎは、わたしのばんです。…あっ、ぜんぜん、だめでした！あはは。", "つぎは、わたしのばんです。…あっ、ぜんぜん、だめでした！あはは。", "Tsugi wa, watashi no ban desu. …A, zenzen, dame deshita! Ahaha.",
  V("Tiếp theo đến lượt tớ. …Á, chẳng được gì cả! Haha.", "Now it's my turn. …Oh, total failure! Haha."), "talk", choices=[
    C("だいじょうぶですよ。もう一回、やりましょう。", V("Không sao đâu. Chơi lại một lần nữa đi.", "It's okay. Let's try one more time."), "n_counter", "Grammar", 10,
      V("Động viên bằng だいじょうぶ và rủ もう一回〜ましょう", "Cheering up with だいじょうぶ and もう一回〜ましょう"), ["grammar.n5.mashou"], ["vocab.n5.daijoubu", "vocab.n5.mou_ikkai"], ["kim.friend"]),
    C("ざんねんですね。", V("Tiếc nhỉ.", "That's a shame."), "n_counter", "ResponseAccuracy", 6,
      V("Chia sẻ bằng ざんねんですね", "Sympathising with ざんねんですね"), [], ["vocab.n5.zannen"]),
    C("へたですね。", V("Cậu dở nhỉ.", "You're bad at it."), "n_kim_hurt", "ResponseAccuracy", -8,
      V("Chê thẳng bạn là へた: bất lịch sự", "Calling your friend へた to their face is rude")),
])
N("n_kim_hurt", KIM, "うう…。でも、まちがえても、だいじょうぶ！先生も、そう言いました。", "うう…。でも、まちがえても、だいじょうぶ！せんせいも、そういいました。", "Uu…. Demo, machigaete mo, daijoubu! Sensei mo, sou iimashita.",
  V("Ư… Nhưng sai cũng không sao! Cô giáo cũng nói vậy mà.", "Ugh… But it's okay to make mistakes! Our teacher said so too."), "talk", "n_counter")

# ───────── Quầy đổi quà (〜まい, 〜をください) ─────────
N("n_counter", NAR, "チケットを持って、けいひんのカウンターへ行きました。", "チケットをもって、けいひんのカウンターへいきました。", "Chiketto wo motte, keihin no kauntaa e ikimashita.",
  V("Bạn cầm vé đến quầy đổi quà.", "You took the tickets to the prize counter."), "", "n_prize")
N("n_prize", KIM, "このステッカーは、チケット五まいです。何まい、ありますか。", "このステッカーは、チケットごまいです。なんまい、ありますか。", "Kono sutekkaa wa, chiketto gomai desu. Nanmai, arimasu ka.",
  V("Hình dán này đổi 5 vé. Cậu có mấy vé?", "This sticker costs five tickets. How many do you have?"), "point", choices=[
    C("五まい、あります。ステッカーをください。", V("Tớ có 5 tấm. Cho tớ hình dán ạ.", "I have five. The sticker, please."), "n_prize_ok", "Vocabulary", 10,
      V("Đếm vật mỏng bằng 〜まい và xin bằng 〜をください", "Counting flat things with 〜まい and asking with 〜をください"), ["grammar.n5.wo_kudasai"], ["vocab.n5.mai", "vocab.n5.go"], ["kim.sticker"]),
    C("五つ、あります。", V("Tớ có 5 cái.", "I have five (things)."), "n_prize_counter", "Vocabulary", 2,
      V("Vé là vật mỏng nên đếm bằng 〜まい (ごまい)", "Tickets are flat, so they are counted with 〜まい (ごまい)")),
])
N("n_prize_counter", KIM, "チケットは、うすいですから、「まい」でかぞえます。「五まい」ですよ。", "チケットは、うすいですから、「まい」でかぞえます。「ごまい」ですよ。", "Chiketto wa, usui desu kara, \"mai\" de kazoemasu. \"Gomai\" desu yo.",
  V("Vé mỏng nên đếm bằng 「まい」. Là 「五まい」 đấy.", "Tickets are thin, so you count them with 「まい」. It's 「五まい」."), "point", "n_prize")
N("n_prize_ok", NAR, "ステッカーを、もらいました。キムさんと、おそろいです。", "ステッカーを、もらいました。キムさんと、おそろいです。", "Sutekkaa wo, moraimashita. Kimu-san to, osoroi desu.",
  V("Bạn nhận được hình dán, giống y như của Kim.", "You got the sticker — a matching one with Kim's."), "", "n_bye", "obj_prize")

N("n_bye", KIM, "きょうは、たのしかったです！また、いっしょに来ましょうね。", "きょうは、たのしかったです！また、いっしょにきましょうね。", "Kyou wa, tanoshikatta desu! Mata, issho ni kimashou ne.",
  V("Hôm nay vui quá! Lần sau lại cùng đến nhé.", "Today was fun! Let's come again together."), "bow", choices=[
    C("はい、また来ましょう。ありがとうございました。", V("Ừ, lần sau lại đến nhé. Cảm ơn cậu.", "Yes, let's come again. Thank you."), "n_recap", "Grammar", 10,
      V("Hẹn lần sau bằng また〜ましょう và cảm ơn ở quá khứ", "Making plans with また〜ましょう and thanking in the past tense"), ["grammar.n5.mashou"], ["vocab.n5.mata", "vocab.n5.arigatou_gozaimasu"], ["kim.promise"]),
    C("じゃあね。", V("Bye nhé.", "See you."), "n_recap", "ResponseAccuracy", 4,
      V("Chào thân mật じゃあね được với bạn bè", "The casual じゃあね is fine with friends")),
])
N("n_recap", NAR, "きょうのポイント：「〜ませんか」「〜ましょう」「〜回」「〜まい」「〜がほしいです」。", "きょうのポイント：「〜ませんか」「〜ましょう」「〜かい」「〜まい」「〜がほしいです」。", "Kyou no pointo: \"~masen ka\" \"~mashou\" \"~kai\" \"~mai\" \"~ga hoshii desu\".",
  V("Tóm tắt: 〜ませんか (rủ), 〜ましょう (cùng làm), 〜回 (số lần), 〜まい (vật mỏng), 〜がほしいです (muốn có). Game Center giờ là chỗ quen của bạn!",
    "Today's points: ~masen ka (inviting), ~mashou (let's), ~kai (times), ~mai (flat things), ~ga hoshii desu (I want). The Game Center is your place now!"), "", "n_complete", "obj_bye")
N("n_complete", NAR, "", "", "", "", "", "", type=4)

objectives = [
    ("obj_meet", "キムさんに会う", V("Gặp Kim sau giờ học", "Meet Kim after class"), False),
    ("obj_arrive", "ゲームセンターに着く", V("Đến Game Center", "Arrive at the Game Center"), False),
    ("obj_play", "いっしょにゲームをする", V("Chơi game cùng Kim", "Play a game with Kim"), False),
    ("obj_prize", "けいひんをもらう", V("Đổi vé lấy quà", "Swap tickets for a prize"), False),
    ("obj_bye", "またのやくそくをする", V("Hẹn lần sau", "Plan to come again"), False),
]
targets = ["grammar.n5.masen_ka", "grammar.n5.mashou", "grammar.n5.ga_hoshii", "vocab.n5.kai", "vocab.n5.mai"]

problems = S.validate("node_find_kim", objectives)
print("problems:", problems)
if not problems:
    S.write(sys.argv[1], name="scenario_arcade_kim_invite", sid="scenario.arcade.kim_invite", title_ja="キムさんとゲームセンター",
            title_vi=V("Đi Game Center cùng Kim", "Game Center with Kim"),
            desc_ja="放課後、キムさんとゲームセンターへ。さそい方、数え方（〜回・〜まい）、ほしいものの言い方を練習します。",
            desc_vi=V("Sau giờ học, cùng Kim đến Game Center: rủ rê, đếm số lần (〜回), đếm vé (〜まい), nói điều mình muốn (〜がほしい).",
                      "After class, go to the Game Center with Kim: inviting, counting times (~kai) and tickets (~mai), and saying what you want (~ga hoshii)."),
            chapter=4, targets=targets, objectives=objectives, start="node_find_kim")
    write_en(sys.argv[1], "arcade_kim_invite")
    print("nodes", len(S.nodes))
