"""Career milestone stories: medical (Hibari Clinic), chef (Sushi Hibari kitchen), musician (Hibiki stage).
Medical answers follow real practice (Docs/HEALTH_SOURCES.md): no antibiotics for colds, blood pressure after rest,
medicine notebook (お薬手帳) for interactions, chest pain is an emergency."""
from career_stories import S

# ─────────── Y tế · Phòng khám Hibari (bác sĩ Mori) ───────────
KANA = ('Chị Kana (điều dưỡng)', 'Kana (nurse)', 'かなさん')

S('medical', 2, None, ('Quên thẻ bảo hiểm', 'A forgotten insurance card', '保険証を忘れた'),
  {'patient': ('Bà cụ bệnh nhân', 'Elderly patient', 'おばあさん')},
  [('boss', '受付を お願いします。', 'Em trực quầy tiếp nhận nhé.', 'Please take the reception desk.')],
  ('patient', '保険証を 忘れて しまいました……', 'Tôi quên mang thẻ bảo hiểm…', 'I forgot my insurance card…'),
  [('best', '今日は 全額 お支払いいただいて、保険証を お持ちいただければ 返金します。', 'Hôm nay bà trả toàn bộ, mang thẻ đến thì được hoàn lại phần chênh.', "Pay in full today; bring the card and we'll refund the difference.", ('patient', 'よかった……ありがとう。', 'May quá… cảm ơn cháu.', 'What a relief… thank you.')),
   ('ok', '保険証が ないと だめです。', 'Không có thẻ thì không được.', "Without the card, we can't.", ('boss', '全額で 受けて、あとで 返金できるよ。', 'Có thể trả toàn bộ rồi hoàn lại sau mà.', 'She can pay in full and be refunded later.')),
   ('poor', 'また 明日 来て ください。', 'Mai bà quay lại nhé.', 'Please come back tomorrow.', ('patient', 'でも、熱が あって つらいのに……', 'Nhưng tôi sốt mệt lắm…', 'But I have a fever and feel awful…'))])

S('medical', 3, None, ('Đo huyết áp', 'Blood pressure', '血圧'),
  {'kana': KANA, 'patient': ('Bệnh nhân', 'Patient', '患者さん')},
  [('kana', '血圧を はかって もらえる？', 'Em đo huyết áp giúp chị nhé?', 'Could you take the blood pressure?'),
   ('narrator', '患者さんは 走って 来た ばかりです。', 'Bệnh nhân vừa chạy đến.', 'The patient has just come running in.')],
  ('patient', 'すぐ はかりますか。', 'Đo ngay luôn à?', 'Are you measuring right away?'),
  [('ok', 'はい、すぐ はかります。', 'Vâng, đo ngay.', 'Yes, right away.', ('kana', '少し 休んで、もう 一度 はかって みて。', 'Cho nghỉ chút rồi đo lại nhé.', 'Let them rest and measure again.')),
   ('best', '五分ほど 座って 休んでから はかりましょう。', 'Mình ngồi nghỉ khoảng 5 phút rồi đo nhé.', "Let's rest for about five minutes, then measure.", ('kana', '正しいね。走った あとは 高く 出るから。', 'Đúng rồi. Vừa chạy thì huyết áp lên cao.', 'Correct. It reads high right after running.')),
   ('poor', '高いですね。病気かも しれません。', 'Cao quá. Có thể bị bệnh.', "That's high. You might be ill.", ('patient', 'え……こわい……', 'Hả… sợ quá…', "What… that's scary…"))])

S('medical', 4, None, ('Xin kháng sinh', 'Asking for antibiotics', '抗生物質'),
  {'patient': ('Bệnh nhân bị cảm', 'Patient with a cold', 'かぜの患者さん')},
  [('patient', 'かぜなので、抗生物質を ください。', 'Tôi bị cảm, cho tôi kháng sinh.', 'I have a cold — give me antibiotics.')],
  ('boss', 'あなたなら、どう 説明する？', 'Nếu là em, em giải thích thế nào?', 'How would you explain it?'),
  [('poor', 'はい、出して もらいますね。', 'Vâng, tôi sẽ xin cho.', "Sure, I'll get you some.", ('boss', '必要ない 薬は 出さないよ。', 'Thuốc không cần thì không kê.', "We don't give unnecessary medicine.")),
   ('best', 'かぜの 多くは ウイルスなので、抗生物質は 効きません。休んで、水分を とりましょう。', 'Cảm phần lớn do virus, kháng sinh không có tác dụng. Nghỉ ngơi và uống nhiều nước nhé.', "Most colds are viral, so antibiotics don't work. Rest and drink fluids.", ('boss', 'その通り。わかりやすい 説明だ。', 'Chính xác. Giải thích dễ hiểu.', 'Exactly. A clear explanation.')),
   ('ok', '先生に 聞いて ください。', 'Hỏi bác sĩ đi ạ.', 'Please ask the doctor.', ('boss', '君も 説明できるように なろう。', 'Em cũng nên giải thích được.', 'You should be able to explain it too.'))])

S('medical', 5, 'doctor', ('Đau ngực', 'Chest pain', '胸の痛み'),
  {'kana': KANA, 'patient': ('Bệnh nhân lớn tuổi', 'Older patient', '患者さん')},
  [('kana', '先生、急患です！', 'Bác sĩ ơi, có ca cấp cứu!', 'Doctor, an emergency!'),
   ('patient', '胸が しめつけられるように 痛い……', 'Ngực tôi đau như bị bóp chặt…', "My chest hurts like it's being squeezed…")],
  ('kana', 'どう しますか。', 'Làm gì đây ạ?', 'What do we do?'),
  [('best', 'すぐ 心電図を とって、院長と 救急車を 呼びます。', 'Đo điện tim ngay, gọi viện trưởng và xe cấp cứu.', 'ECG now, and call the director and an ambulance.', ('boss', '正しい 判断だ。大きい 病院に 間に合った。', 'Quyết định đúng. Kịp chuyển viện lớn.', 'Right call. We got them to the big hospital in time.')),
   ('ok', '痛み止めを 出して、様子を 見ます。', 'Cho thuốc giảm đau, theo dõi.', 'Give painkillers and observe.', ('boss', '胸の 痛みは 待っては だめだ。まず 心臓を 調べよう。', 'Đau ngực thì không được chờ. Kiểm tra tim trước.', "Chest pain can't wait. Check the heart first.")),
   ('poor', '疲れでしょう。帰って 寝て ください。', 'Chắc do mệt. Về ngủ đi.', 'Probably tiredness. Go home and sleep.', ('boss', 'それは 危ない！', 'Thế là nguy hiểm!', "That's dangerous!"))])

S('medical', 5, 'pharmacist', ('Uống chung thuốc', 'Taking medicines together', '飲み合わせ'),
  {'patient': ('Bệnh nhân', 'Patient', '患者さん')},
  [('narrator', '薬局の カウンターで。', 'Tại quầy thuốc.', 'At the pharmacy counter.')],
  ('patient', 'この 薬と、ほかの 病院の 薬、いっしょに 飲んで いいですか。', 'Thuốc này với thuốc của bệnh viện khác uống chung được không?', "Can I take this with medicine from another hospital?"),
  [('ok', 'たぶん 大丈夫です。', 'Chắc không sao.', 'Probably fine.', ('boss', '薬に「たぶん」は ないよ。', 'Thuốc không có chuyện "chắc".', "There's no 'probably' with medicine.")),
   ('poor', '全部 いっしょに 飲んで ください。', 'Uống chung hết đi.', 'Take them all together.', ('boss', '確認しないのは 危険だ。', 'Không kiểm tra là nguy hiểm.', 'Not checking is dangerous.')),
   ('best', 'お薬手帳を 見せて いただけますか。飲み合わせを 確認します。', 'Cho tôi xem sổ thuốc được không? Tôi kiểm tra tương tác thuốc.', "May I see your medicine notebook? I'll check for interactions.", ('patient', '手帳、持って いて よかった。', 'May mà có mang sổ.', 'Glad I brought my notebook.'))])

S('medical', 6, 'doctor', ('Phòng khám của em', 'Your clinic', 'あなたのクリニック'),
  {'kana': KANA, 'patient': ('Bệnh nhân người Việt', 'Vietnamese patient', 'ベトナム人の患者さん')},
  [('boss', '来年から、この クリニックを 君に まかせたい。', 'Từ năm sau bác muốn giao phòng khám cho em.', 'From next year I want to leave this clinic to you.'),
   ('kana', '町の 外国人の 患者さんも 増えて います。', 'Bệnh nhân người nước ngoài trong phố cũng tăng.', 'We have more foreign patients in town too.')],
  ('boss', '院長として、何を 始める？', 'Là giám đốc, em bắt đầu gì?', 'As director, what will you start?'),
  [('best', 'やさしい 日本語と 英語、ベトナム語の 問診票を 作ります。', 'Làm phiếu hỏi bệnh bằng tiếng Nhật dễ hiểu, tiếng Anh và tiếng Việt.', 'Questionnaires in easy Japanese, English and Vietnamese.', ('patient', '自分の ことばで 書けて、安心しました。', 'Viết được bằng tiếng mình, yên tâm quá.', 'Writing in my own language put me at ease.')),
   ('poor', '日本語が できない 人は ほかへ どうぞ。', 'Ai không biết tiếng Nhật thì đi chỗ khác.', 'Those without Japanese can go elsewhere.', ('boss', '医者が 患者を えらぶのか？', 'Bác sĩ mà lại chọn bệnh nhân sao?', 'A doctor choosing patients?')),
   ('ok', '今まで 通りに します。', 'Giữ như trước.', 'Keep things as they are.', ('kana', '外国の 方は 問診票で 困って いますよ。', 'Người nước ngoài khổ với phiếu hỏi bệnh lắm.', 'Foreign patients struggle with the form.'))])

S('medical', 6, 'pharmacist', ('Nhà thuốc của phố', 'The town pharmacy', '町の薬局'),
  {'patient': ('Ông cụ', 'Elderly man', 'おじいさん')},
  [('boss', '薬局の 責任者は 君だ。', 'Em là trưởng nhà thuốc.', 'You head the pharmacy now.')],
  ('patient', '薬が 多すぎて、どれを いつ 飲むか わからない……', 'Thuốc nhiều quá, không biết uống cái nào lúc nào…', "So many pills, I don't know which to take when…"),
  [('best', '朝・昼・夜で 一回分ずつ まとめる 一包化を、先生に 相談しましょう。', 'Mình hỏi bác sĩ để gói thuốc theo từng lần uống sáng, trưa, tối nhé.', "Let's ask the doctor about packing doses by morning, noon and night.", ('patient', 'これなら まちがえない。ありがとう。', 'Thế này thì không nhầm nữa. Cảm ơn.', "Now I won't mix them up. Thank you.")),
   ('ok', '紙に 書いて おきます。', 'Tôi ghi ra giấy cho ông.', "I'll write it down.", ('patient', '字が 小さくて……', 'Chữ nhỏ quá…', 'The writing is so small…')),
   ('poor', '説明書を 読んで ください。', 'Ông đọc hướng dẫn đi.', 'Please read the leaflet.', ('boss', '読むのが 難しい 人も いるよ。', 'Có người đọc khó lắm.', 'Some people find reading hard.'))])

# ─────────── Đầu bếp · bếp Sushi Hibari (bác Ota) ───────────
KEN = ('Anh Ken (bếp phụ)', 'Ken (sous-chef)', 'けんさん')
MOTHER = ('Mẹ bé Yui', "Yui's mother", 'ゆいちゃんの お母さん')

S('chef', 2, None, ('Vo gạo', 'Washing rice', '米とぎ'),
  {'ken': KEN},
  [('boss', 'まずは 米を といで みろ。', 'Trước hết vo gạo xem.', 'First, wash the rice.')],
  ('ken', '何回 とぐか、わかる？', 'Biết vo mấy lần không?', 'Know how many times to wash it?'),
  [('ok', '一回で いいと 思います。', 'Một lần là được.', 'Once is enough, I think.', ('ken', 'それだと ごはんが べたつくよ。', 'Vậy cơm sẽ dính.', 'Then the rice gets sticky.')),
   ('best', '水が すきとおるまで、やさしく 何回か とぎます。', 'Vo nhẹ vài lần đến khi nước trong.', 'Gently, several times, until the water runs clear.', ('boss', 'そうだ。米を つぶさないように。', 'Đúng. Đừng làm nát hạt gạo.', "Right. Don't crush the grains.")),
   ('poor', '洗剤で 洗います。', 'Rửa bằng nước rửa chén.', "I'll wash it with detergent.", ('boss', 'ばかもん！', 'Đồ ngốc!', 'You fool!'))])

S('chef', 3, None, ('Cầm dao trong bếp', 'Knife safety', '包丁'),
  {'ken': KEN},
  [('ken', '包丁の 持ち方、気を つけて。', 'Chú ý cách cầm dao.', 'Mind how you hold the knife.')],
  ('boss', '包丁を 持って 人の 後ろを 通る とき、何と 言う？', 'Cầm dao đi sau lưng người khác thì nói gì?', 'What do you say when passing behind someone with a knife?'),
  [('best', '「後ろ、通ります。包丁です」と 声を かけます。', 'Nói: "Đi sau lưng đây, có dao."', "I call out, 'Behind you — knife.'", ('boss', 'それで いい。厨房は 声で 守る。', 'Đúng. Bếp được giữ an toàn bằng lời nói.', 'Good. A kitchen stays safe by calling out.')),
   ('poor', '急いで 走ります。', 'Chạy thật nhanh.', "I'll run.", ('ken', 'あぶない！', 'Nguy hiểm!', 'Dangerous!')),
   ('ok', '何も 言わずに、ゆっくり 通ります。', 'Không nói gì, đi chậm.', 'I pass slowly without a word.', ('ken', 'ゆっくりでも、声は 出そう。', 'Chậm cũng phải lên tiếng.', 'Even slowly, call out.'))])

S('chef', 4, None, ('Độ tươi của cá', 'Freshness', '鮮度'),
  {'ken': KEN},
  [('ken', 'この 魚、ちょっと においが 気に なる。', 'Con cá này hơi có mùi.', 'This fish smells a bit off.')],
  ('boss', 'どう する？', 'Làm sao?', 'What will you do?'),
  [('poor', '安い まかないに します。', 'Làm cơm nhân viên.', "We'll use it for the staff meal.", ('ken', 'おなかを こわしたら どう するの……', 'Lỡ đau bụng thì sao…', 'What if someone gets sick…')),
   ('ok', 'においを 消して 使います。', 'Khử mùi rồi dùng.', 'Mask the smell and use it.', ('boss', 'においを 消しても、悪い 魚は 悪い。', 'Khử mùi rồi thì cá hỏng vẫn là cá hỏng.', 'Masked or not, bad fish is bad.')),
   ('best', '使いません。仕入れ先に 伝えて、今日の メニューを 変えます。', 'Không dùng. Báo nơi nhập và đổi thực đơn hôm nay.', "We won't use it. I'll tell the supplier and change today's menu.", ('boss', '店の 信用は 一皿で 決まる。正しい。', 'Uy tín của quán nằm ở từng đĩa. Đúng lắm.', "A shop's trust rests on each plate. Correct."))])

S('chef', 5, 'sushi', ('Khách quen khó tính', 'A demanding regular', 'うるさい常連'),
  {'sato': ('Ông Sato (khách quen)', 'Mr Sato (regular)', '佐藤さん')},
  [('sato', '今日の シャリは 少し かたいね。', 'Cơm sushi hôm nay hơi cứng.', "Today's sushi rice is a bit firm.")],
  ('boss', '二番手、どう 答える？', 'Bếp phó, cháu trả lời sao?', 'Number two, how do you answer?'),
  [('best', '申し訳ありません。すぐ 新しく 握り直します。', 'Xin lỗi ông. Tôi nắm lại món mới ngay.', "I'm sorry. I'll make you a fresh one right away.", ('sato', 'うん、これだ。また 来るよ。', 'Ừ, đúng vị rồi. Lần sau lại đến.', "Yes, that's it. I'll come again.")),
   ('ok', '今日は こういう シャリです。', 'Hôm nay cơm như vậy ạ.', "That's today's rice.", ('sato', 'そうか……', 'Vậy à…', 'I see…')),
   ('poor', 'ほかの お客様は 何も 言いません。', 'Khách khác có ai nói gì đâu.', "Other customers don't complain.", ('boss', 'お客様と 争うな。', 'Đừng cãi khách.', "Don't argue with guests."))])

S('chef', 5, 'pastry', ('Bánh sinh nhật', 'A birthday cake', '誕生日ケーキ'),
  {'mother': MOTHER},
  [('mother', '娘の 誕生日なんですが、卵の アレルギーが あって……', 'Sinh nhật con gái tôi, nhưng bé dị ứng trứng…', "It's my daughter's birthday, but she's allergic to eggs…")],
  ('mother', 'ケーキは 作れますか。', 'Có làm bánh được không?', 'Can you make a cake?'),
  [('ok', 'アレルギーの ケーキは むずかしいです。', 'Bánh cho người dị ứng thì khó lắm.', 'Allergy cakes are difficult.', ('mother', 'そうですか……', 'Vậy à…', 'I see…')),
   ('best', 'はい。卵を 使わない レシピで、道具も 分けて 作ります。', 'Được ạ. Tôi làm theo công thức không trứng, dùng dụng cụ riêng.', 'Yes. An egg-free recipe, with separate utensils.', ('mother', '娘が「おいしい！」って 笑いました。', 'Con bé cười nói "Ngon quá!"', "My daughter smiled and said 'Yummy!'")),
   ('poor', '卵を 少しだけ 使います。', 'Chỉ dùng chút trứng thôi.', 'Just a little egg.', ('boss', 'アレルギーに「少し」は ない！', 'Dị ứng thì không có chuyện "một chút"!', "With allergies there's no 'a little'!"))])

S('chef', 6, 'sushi', ('Bếp trưởng', 'Head chef', '板長'),
  {'ken': KEN},
  [('boss', '今日から お前が 板長だ。', 'Từ hôm nay cháu là bếp trưởng.', 'From today you are head chef.'),
   ('ken', '新人が 三人 入りました。', 'Có ba người mới vào.', 'Three newcomers have joined.')],
  ('boss', '板長として、何を 教える？', 'Là bếp trưởng, cháu dạy gì?', 'As head chef, what will you teach?'),
  [('ok', '見て 覚えろ、と 言います。', 'Bảo họ nhìn mà học.', "I'll tell them to learn by watching.", ('boss', '昔は そうだった。今は 言葉でも 教えよう。', 'Ngày xưa thế. Giờ hãy dạy cả bằng lời.', 'That was the old way. Teach with words too.')),
   ('best', 'まず 掃除と 道具の 手入れ。それから 一つずつ 技を 見せます。', 'Trước là dọn dẹp và giữ dụng cụ. Rồi chỉ từng kỹ thuật.', "Cleaning and tool care first. Then I'll show each skill one by one.", ('ken', '新人たち、楽しそうに がんばって います。', 'Người mới hăng hái lắm.', 'The newcomers are working with joy.')),
   ('poor', '最初から 握らせます。', 'Cho nắm sushi ngay từ đầu.', "I'll have them make sushi from day one.", ('ken', '基本が ないと、けがを しますよ。', 'Không có căn bản dễ bị thương.', "Without basics they'll get hurt."))])

S('chef', 6, 'pastry', ('Tiệm bánh', 'The patisserie', 'パティスリー'),
  {'mother': MOTHER},
  [('boss', 'ひばり町に 小さな ケーキ屋を 開かないか。', 'Mở một tiệm bánh nhỏ ở phố Hibari không?', 'How about opening a small cake shop in Hibari?')],
  ('boss', 'どんな 店に する？', 'Tiệm thế nào?', 'What kind of shop?'),
  [('best', '季節の 果物と、アレルギーの 人も 食べられる ケーキの 店です。', 'Tiệm bánh trái cây theo mùa, có bánh cho cả người dị ứng.', 'Seasonal fruit cakes, and cakes people with allergies can eat too.', ('mother', 'うちの 子も 安心して 買えます！', 'Con tôi cũng yên tâm mua được!', 'My child can buy safely too!')),
   ('poor', 'いちばん 安い 材料で たくさん 作ります。', 'Làm thật nhiều bằng nguyên liệu rẻ nhất.', 'Mass-produce with the cheapest ingredients.', ('mother', '前の ほうが おいしかった……', 'Hồi trước ngon hơn…', 'It used to taste better…')),
   ('ok', 'パリの 店と 同じに します。', 'Làm y tiệm ở Paris.', 'Just like a Paris shop.', ('boss', 'すてきだけど、町の 人の 好みも 考えよう。', 'Đẹp đấy, nhưng nghĩ cả khẩu vị người trong phố.', "Lovely, but think of the town's tastes too."))])

# ─────────── Nhạc công · sân khấu Hibiki (chú Hibiki) ───────────
AUDIENCE = ('Khán giả', 'Audience', '観客')
SORA = ('Bé Sora', 'Sora', 'そらくん')

S('musician', 2, None, ('Đàn ở quán cà phê', 'A café gig', 'カフェライブ'),
  {'owner': ('Chủ quán cà phê', 'Café owner', 'カフェの マスター')},
  [('boss', 'カフェで ピアノを ひいて みないか。', 'Thử đàn piano ở quán cà phê không?', 'How about playing piano at the café?'),
   ('narrator', 'お客様は 静かに 話を して います。', 'Khách đang trò chuyện nhỏ nhẹ.', 'The guests are chatting quietly.')],
  ('owner', 'どんな 曲に する？', 'Đàn bài gì đây?', 'What will you play?'),
  [('poor', 'いちばん 大きな 音で ひきます。', 'Đàn to hết cỡ.', "I'll play as loud as possible.", ('owner', 'お客様が 帰って しまった……', 'Khách về hết rồi…', 'The guests have left…')),
   ('best', '話の じゃまに ならない、静かな ジャズに します。', 'Đàn jazz nhẹ, không làm phiền cuộc trò chuyện.', "Quiet jazz that won't disturb the conversations.", ('owner', 'いい 雰囲気だ。また 来て ほしい。', 'Không khí dễ chịu. Mong cậu đến nữa.', 'Lovely mood. Please come again.')),
   ('ok', '自分の 好きな 曲を ひきます。', 'Đàn bài mình thích.', "I'll play what I like.", ('boss', '場所に 合わせる ことも 大事だよ。', 'Hợp với nơi chốn cũng quan trọng.', 'Fitting the place matters too.'))])

S('musician', 3, None, ('Đàn sai nốt', 'A wrong note', 'ミスタッチ'),
  {'audience': AUDIENCE},
  [('narrator', 'ライブの 途中で、まちがえて しまいました。', 'Giữa buổi diễn, bạn đàn sai.', 'Mid-performance, you hit a wrong note.')],
  ('narrator', 'どう しますか。', 'Bạn làm gì?', 'What do you do?'),
  [('best', '止まらずに、そのまま 曲を 続けます。', 'Không dừng, đàn tiếp bài.', 'I keep playing without stopping.', ('boss', 'プロは 止まらない。よく やった。', "Dân chuyên nghiệp không dừng. Giỏi lắm.", "Pros don't stop. Well done.")),
   ('ok', '最初から ひき直します。', 'Đàn lại từ đầu.', 'I start over from the beginning.', ('boss', 'ステージでは、続ける 方が いいよ。', 'Trên sân khấu, đàn tiếp thì hơn.', "On stage, it's better to carry on.")),
   ('poor', 'ステージから おります。', 'Bỏ xuống sân khấu.', 'I leave the stage.', ('audience', 'え、終わり？', 'Hả, hết rồi à?', 'Huh, is it over?'))])

S('musician', 4, None, ('Phòng thu', 'The studio', 'スタジオ'),
  {'producer': ('Nhà sản xuất Kudō', 'Producer Kudō', '工藤プロデューサー')},
  [('narrator', 'レコーディングスタジオで。', 'Trong phòng thu âm.', 'In the recording studio.')],
  ('producer', 'テンポを 少し 速く して くれる？', 'Đàn nhanh hơn chút được không?', 'Could you play a little faster?'),
  [('ok', 'このままの 方が いいと 思います。', 'Tôi nghĩ giữ nguyên thì hơn.', "I think it's better as it is.", ('producer', '意見は いいけど、まず 一回 ためして みよう。', 'Có ý kiến tốt, nhưng thử một lần đã.', "Opinions are fine, but let's try once first.")),
   ('best', 'わかりました。メトロノームに 合わせて、もう 一度 ひきます。', 'Vâng. Tôi đàn lại theo máy đếm nhịp.', "Understood. I'll play again with the metronome.", ('producer', 'ぴったりだ。いい テイクが とれた。', 'Chuẩn luôn. Được bản thu tốt.', 'Spot on. We got a great take.')),
   ('poor', '疲れたので 帰ります。', 'Mệt rồi, tôi về.', "I'm tired, I'm going home.", ('producer', '……次は ほかの 人に 頼むよ。', '…Lần sau tôi nhờ người khác.', "…Next time I'll ask someone else."))])

S('musician', 5, 'pianist', ('Buổi hoà nhạc', 'The concert', '演奏会'),
  {'audience': AUDIENCE},
  [('boss', 'ホールで ソロの 演奏会だ。', 'Buổi hoà nhạc độc tấu ở nhà hát đấy.', "It's a solo concert in the hall."),
   ('narrator', '本番の 前、手が ふるえて います。', 'Trước giờ diễn, tay bạn run.', 'Before the show, your hands are shaking.')],
  ('boss', 'どう する？', 'Cháu làm gì?', 'What will you do?'),
  [('best', 'ゆっくり 深呼吸して、最初の 四小節を 心の 中で 歌います。', 'Hít thở sâu, hát thầm bốn ô nhịp đầu.', 'Breathe deeply and sing the first four bars in my head.', ('audience', 'すばらしい 演奏だった！', 'Màn trình diễn tuyệt vời!', 'A wonderful performance!')),
   ('ok', '楽譜を 見ながら ひきます。', 'Đàn nhìn bản nhạc.', "I'll play reading the score.", ('boss', 'それも いい。でも、心を 落ち着ける ことも 大切だ。', 'Cũng được, nhưng giữ bình tĩnh cũng quan trọng.', 'That works. But calming yourself matters too.')),
   ('poor', '演奏会を やめます。', 'Huỷ buổi diễn.', 'Cancel the concert.', ('boss', 'お客様が 待って いるのに……', 'Khán giả đang đợi mà…', 'The audience is waiting…'))])

S('musician', 5, 'songwriter', ('Bài hát đầu tay', 'My first song', 'はじめての曲'),
  {'audience': AUDIENCE},
  [('boss', '自分の 曲を 作って みないか。', 'Thử viết bài hát của riêng mình không?', 'How about writing your own song?')],
  ('boss', '何に ついて 歌う？', 'Hát về điều gì?', 'What will you sing about?'),
  [('ok', '流行って いる 曲を まねします。', 'Bắt chước bài đang thịnh hành.', "I'll copy a trending song.", ('boss', 'あなたらしさを 入れて ごらん。', 'Thêm chất riêng của cháu vào.', 'Put some of yourself into it.')),
   ('poor', 'ほかの 人の 歌詞を そのまま 使います。', 'Dùng nguyên lời bài hát của người khác.', "I'll use someone else's lyrics as is.", ('boss', 'それは 盗作だよ。', 'Thế là đạo nhạc.', "That's plagiarism.")),
   ('best', 'ひばり町に 来た 日の 気持ちを、やさしい 日本語で 歌います。', 'Hát về cảm xúc ngày đầu đến phố Hibari, bằng tiếng Nhật dễ hiểu.', 'The feelings of my first day in Hibari, in simple Japanese.', ('audience', '心に しみる 歌だね。', 'Bài hát thấm vào lòng quá.', 'That song touches the heart.'))])

S('musician', 6, 'pianist', ('Giám đốc âm nhạc', 'Music director', '音楽監督'),
  {'sora': SORA},
  [('boss', '町の 音楽祭の 監督を 頼みたい。', 'Chú muốn nhờ cháu làm giám đốc lễ hội âm nhạc của phố.', "I'd like you to direct the town's music festival."),
   ('sora', 'ぼくも ステージに 出たい！', 'Em cũng muốn lên sân khấu!', 'I want to be on stage too!')],
  ('boss', 'どんな 音楽祭に する？', 'Lễ hội thế nào?', 'What kind of festival?'),
  [('best', 'プロも 子どもも 出られる、町の みんなの 音楽祭に します。', 'Lễ hội của cả phố, dân chuyên lẫn trẻ em đều lên được.', 'A festival for everyone — pros and children alike.', ('sora', 'ぼく、ピアノ 練習する！', 'Em sẽ tập piano!', "I'll practise piano!")),
   ('poor', 'チケットを 高く して、もうけます。', 'Bán vé thật đắt để lời.', 'Expensive tickets to make money.', ('boss', '町の 人が 来られないね。', 'Người trong phố không đến được rồi.', "The townspeople can't come.")),
   ('ok', 'プロだけの 音楽祭に します。', 'Chỉ mời dân chuyên nghiệp.', 'Professionals only.', ('sora', 'そっか……', 'Vậy à…', 'Oh…'))])

S('musician', 6, 'songwriter', ('Lưu diễn', 'On tour', 'ツアー'),
  {'manager': ('Quản lý tour', 'Tour manager', 'マネージャー'), 'fan': ('Người hâm mộ', 'Fan', 'ファン')},
  [('manager', '全国ツアーが 決まりました！', 'Đã chốt tour lưu diễn toàn quốc!', 'A national tour is confirmed!'),
   ('fan', '最初の カフェライブから、ずっと 応援して います。', 'Tôi ủng hộ từ buổi diễn ở quán cà phê đầu tiên.', "I've supported you since your first café gig.")],
  ('manager', '最後の 曲は どう しますか。', 'Bài cuối chọn gì?', 'What about the final song?'),
  [('best', 'はじめて 作った、ひばり町の 歌に します。みんなと 歌います。', 'Bài hát về phố Hibari đầu tay, hát cùng mọi người.', "My first song, about Hibari. We'll sing it together.", ('fan', '泣いちゃった……ありがとう！', 'Khóc mất rồi… cảm ơn!', 'I cried… thank you!')),
   ('ok', 'いちばん 売れた 曲に します。', 'Bài bán chạy nhất.', 'My best-selling song.', ('manager', 'もり上がりました！', 'Không khí bùng nổ!', 'The crowd went wild!')),
   ('poor', 'アンコールは しません。', 'Không hát encore.', 'No encore.', ('fan', 'えー……', 'Ơ…', 'Aww…'))])
