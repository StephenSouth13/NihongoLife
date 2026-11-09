"""Career milestone stories: retail (Hibari Mart), food (Sushi Hibari), farm (Midori), hospitality (Gánh Ngon)."""
from career_stories import S

# ─────────── Bán lẻ · Hibari Mart (anh Ito) ───────────
YUKI = ('Chị Yuki (đồng nghiệp)', 'Yuki (coworker)', 'ゆきさん')
GUEST = ('Khách hàng', 'Customer', 'お客様')

S('retail', 2, None, ('Một mình đứng quầy', 'Alone at the register', 'はじめての一人レジ'),
  {'regular': ('Ông cụ khách quen', 'Regular customer', '常連のおじいさん')},
  [('boss', '今日から 一人で レジを お願いします。', 'Từ hôm nay em đứng quầy một mình nhé.', 'From today, please run the register on your own.')],
  ('regular', 'この お弁当、温めて もらえますか。', 'Cháu hâm nóng hộp cơm này giúp ông được không?', 'Could you heat up this bento for me?'),
  [('ok', 'はい。', 'Vâng.', 'Yes.', ('boss', '「少々 お待ちください」も 言えると いいね。', 'Nói thêm "xin chờ một chút" thì tốt hơn đấy.', "Adding 'one moment, please' would be even better.")),
   ('best', 'はい、温めますね。少々 お待ちください。', 'Vâng, cháu hâm nóng ạ. Ông chờ một chút nhé.', "Certainly, I'll heat it up. One moment, please.", ('regular', 'ありがとう。ていねいだね。', 'Cảm ơn cháu. Lễ phép quá.', 'Thank you. How polite.')),
   ('poor', '電子レンジは あそこです。', 'Lò vi sóng ở đằng kia.', 'The microwave is over there.', ('regular', 'え……自分で？', 'Hả… tự làm à?', 'Eh… by myself?'))])

S('retail', 3, None, ('Giờ cao điểm', 'Rush hour', '混雑の時間'),
  {'yuki': YUKI, 'guest': GUEST},
  [('narrator', '昼休み。レジに 長い 列が できました。', 'Giờ nghỉ trưa. Hàng người xếp dài trước quầy.', 'Lunchtime. A long queue forms at the register.'),
   ('yuki', 'どうしよう、一人じゃ 間に合わない！', 'Làm sao đây, một mình chị không kịp!', "What do we do? I can't keep up alone!")],
  ('boss', '今日は あなたが リーダーです。どうしますか。', 'Hôm nay em dẫn ca. Em làm gì?', "You're leading the shift today. What will you do?"),
  [('best', 'もう 一台 レジを 開けます。次の お客様、こちらへ どうぞ！', 'Em mở thêm một quầy. Quý khách tiếp theo, mời sang đây ạ!', "I'll open another register. Next customer, this way please!", ('yuki', '助かった！列が すぐ 短く なったね。', 'Cứu chị rồi! Hàng ngắn lại ngay.', 'You saved me! The queue got shorter right away.')),
   ('poor', 'お客様に 待って もらいます。', 'Để khách chờ thôi.', 'The customers will just have to wait.', ('guest', 'まだですか？もう 帰ります。', 'Vẫn chưa xong à? Tôi về đây.', "Still not done? I'm leaving.")),
   ('ok', '急いで やります。', 'Em sẽ làm nhanh hơn.', "I'll hurry.", ('boss', 'がんばったね。でも、レジを もう 一台 開ける 手も あったよ。', 'Em cố gắng rồi. Nhưng còn cách mở thêm một quầy nữa đấy.', 'Good effort. You could also have opened another register.'))])

S('retail', 4, None, ('Kèm người mới', 'Training the new hire', '新人の指導'),
  {'kenta': ('Kenta (nhân viên mới)', 'Kenta (new part-timer)', 'けんたくん')},
  [('boss', '新しい アルバイトの けんたくんを 教えて ください。', 'Em kèm bạn làm thêm mới, Kenta, nhé.', 'Please train our new part-timer, Kenta.'),
   ('kenta', 'すみません……おつりを まちがえました。', 'Em xin lỗi… em đưa nhầm tiền thối.', 'Sorry… I gave the wrong change.')],
  ('narrator', 'けんたくんに 何と 言いますか。', 'Bạn nói gì với Kenta?', 'What do you say to Kenta?'),
  [('ok', '次は 気を つけてね。', 'Lần sau cẩn thận nhé.', 'Be careful next time.', ('boss', '注意だけじゃなくて、やり方も 見せて あげよう。', 'Đừng chỉ nhắc, hãy chỉ cho em ấy cách làm.', "Don't just warn him — show him how.")),
   ('poor', 'どうして こんな 簡単な ことが できないの？', 'Sao việc dễ thế này cũng không làm được?', "Why can't you do something this easy?", ('kenta', '……すみません。', '…Em xin lỗi.', "…I'm sorry.")),
   ('best', '大丈夫。いっしょに 数え方を 確認しよう。', 'Không sao. Mình cùng xem lại cách đếm tiền nhé.', "It's okay. Let's go over how to count it together.", ('kenta', 'はい！ありがとうございます、先輩！', 'Vâng! Em cảm ơn ạ!', 'Yes! Thank you!'))])

S('retail', 5, 'store', ('Ngày bão', 'Typhoon day', '台風の日'),
  {'yuki': YUKI, 'guest': GUEST},
  [('boss', '明日は 台風です。店は あなたに まかせます。', 'Mai có bão. Anh giao cửa hàng cho em.', "A typhoon is coming tomorrow. I'm leaving the store to you."),
   ('yuki', '配送の トラックが 遅れるそうです。', 'Nghe nói xe giao hàng sẽ đến trễ.', 'They say the delivery truck will be late.')],
  ('yuki', 'どう しましょうか。', 'Mình làm sao đây?', 'What should we do?'),
  [('best', '水と パンを 多めに 並べて、入口に お知らせを はります。', 'Bày thêm nước và bánh mì, dán thông báo ở cửa.', "We'll stock extra water and bread and put a notice at the entrance.", ('guest', '水が あって よかった。ありがとう。', 'May mà còn nước. Cảm ơn nhé.', 'Glad you have water. Thank you.')),
   ('ok', 'いつも 通りに します。', 'Cứ làm như mọi ngày.', "We'll do things as usual.", ('yuki', '水が すぐ 売り切れて しまいました……', 'Nước bán hết sạch ngay…', 'The water sold out right away…')),
   ('poor', '今日は 店を 閉めましょう。', 'Hôm nay đóng cửa luôn đi.', "Let's just close the store today.", ('boss', '閉める 前に、私に 相談して ほしかったな。', 'Trước khi đóng cửa, em nên hỏi anh đã.', "I wish you'd asked me before closing."))])

S('retail', 5, 'buyer', ('Rẻ nhưng kém', 'Cheap but poor', '安いけれど……'),
  {'supplier': ('Người chào hàng', 'Supplier', '業者さん'), 'guest': GUEST},
  [('supplier', 'この おにぎり、ほかより 三割 安いですよ。', 'Loại cơm nắm này rẻ hơn chỗ khác 30% đấy.', 'These onigiri are 30% cheaper than anywhere else.'),
   ('narrator', 'でも、ごはんが 少し かたいです。', 'Nhưng cơm hơi cứng.', 'But the rice is a little hard.')],
  ('supplier', 'たくさん 注文しますか。', 'Anh/chị đặt nhiều không?', 'Will you order a lot?'),
  [('poor', '安いので、たくさん 買います。', 'Rẻ thì mua nhiều.', "It's cheap, so I'll buy lots.", ('guest', 'この おにぎり、かたいね……', 'Cơm nắm này cứng quá…', 'This onigiri is hard…')),
   ('best', '品質を 確かめてから 決めます。サンプルを ください。', 'Tôi kiểm tra chất lượng rồi mới quyết. Cho tôi xin mẫu.', "I'll decide after checking the quality. Please give me samples.", ('boss', 'さすが。お客様の ことを 考えて いるね。', 'Đúng là em. Em nghĩ cho khách hàng đấy.', "Well done. You're thinking of the customers.")),
   ('ok', '店長に 聞いて みます。', 'Tôi hỏi cửa hàng trưởng đã.', "I'll ask the manager.", ('boss', '自分で 判断できるように なろう。', 'Em nên tập tự quyết định.', 'Learn to judge for yourself.'))])

S('retail', 6, 'store', ('Quản lý ba cửa hàng', 'Three stores', '三つの店'),
  {'yuki': YUKI},
  [('boss', '来月から 三つの 店を まとめて ほしい。', 'Từ tháng sau anh muốn em quản lý ba cửa hàng.', "From next month, I'd like you to run three stores."),
   ('yuki', 'でも、店ごとに やり方が ちがいますね。', 'Nhưng mỗi cửa hàng làm một kiểu.', 'But each store does things differently.')],
  ('boss', '最初に 何を しますか。', 'Việc đầu tiên em làm gì?', 'What will you do first?'),
  [('ok', 'マニュアルを 配ります。', 'Tôi phát sổ hướng dẫn.', "I'll hand out a manual.", ('boss', 'マニュアルも 大事。でも、人の 声も 聞こう。', 'Sổ hướng dẫn cũng quan trọng, nhưng hãy nghe con người nữa.', 'Manuals matter, but listen to people too.')),
   ('best', 'まず、それぞれの 店の スタッフの 話を 聞きます。', 'Trước tiên tôi nghe nhân viên từng cửa hàng nói.', "First, I'll listen to the staff at each store.", ('yuki', '話を 聞いて もらえて、みんな うれしそうです。', 'Được lắng nghe, ai cũng vui.', 'Everyone looks happy to be heard.')),
   ('poor', '全部 私の やり方に します。', 'Tất cả làm theo cách của tôi.', 'Everything will be done my way.', ('yuki', '……みんな、ちょっと 困って います。', '…Mọi người hơi khó xử.', '…Everyone is a bit troubled.'))])

S('retail', 6, 'buyer', ('Sản phẩm mới', 'A new product', '新商品'),
  {'hana': ('Cô Hana (nông trại Midori)', 'Hana (Midori farm)', 'はなさん'), 'guest': GUEST},
  [('boss', 'ひばりマートだけの 新しい 商品を 作りたい。', 'Anh muốn có sản phẩm mới chỉ Hibari Mart mới có.', 'I want a new product only Hibari Mart sells.'),
   ('hana', 'みどり島の 野菜、使って みませんか。', 'Thử dùng rau đảo Midori không?', 'Why not try vegetables from Midori Island?')],
  ('boss', 'どんな 商品に しますか。', 'Em làm sản phẩm gì?', 'What kind of product will it be?'),
  [('best', 'みどり島の 野菜で、季節の おにぎりを 作りましょう。', 'Làm cơm nắm theo mùa bằng rau đảo Midori.', "Let's make seasonal onigiri with Midori vegetables.", ('hana', 'うれしい！島の 野菜を 町の 人に 食べて もらえる。', 'Vui quá! Người trong phố được ăn rau đảo.', 'Wonderful! Town folk will eat island vegetables.')),
   ('poor', 'いちばん 安い 材料で 作ります。', 'Làm bằng nguyên liệu rẻ nhất.', "We'll make it with the cheapest ingredients.", ('guest', '味が いまいち……もう 買わないかな。', 'Vị không ngon lắm… chắc không mua nữa.', "It doesn't taste great… I won't buy it again.")),
   ('ok', '人気の 商品を まねします。', 'Bắt chước sản phẩm đang được ưa chuộng.', "We'll copy a popular product.", ('boss', 'それでは ひばりマートらしさが ないね。', 'Vậy thì không còn chất Hibari Mart.', "Then it won't feel like Hibari Mart."))])

# ─────────── Ẩm thực · Sushi Hibari (bác Aoki) ───────────
SAKI = ('Chị Saki (phục vụ lâu năm)', 'Saki (senior server)', 'さきさん')

S('food', 2, None, ('Khách đầu tiên', 'First guests', 'はじめての接客'),
  {'guest': ('Hai vị khách', 'Two guests', 'お客様')},
  [('boss', '今日から ホールに 出て もらう。', 'Từ hôm nay cháu ra phục vụ bàn.', "From today you'll work the floor.")],
  ('guest', '二人ですけど、席 ありますか。', 'Chúng tôi hai người, còn chỗ không?', 'There are two of us — any seats?'),
  [('best', 'いらっしゃいませ。二名様ですね。こちらへ どうぞ。', 'Kính chào quý khách. Hai vị ạ. Mời đi lối này.', 'Welcome. Two people? This way, please.', ('guest', 'ありがとう。いい お店だね。', 'Cảm ơn. Quán dễ chịu thật.', 'Thanks. Nice place.')),
   ('ok', 'はい、どうぞ。', 'Vâng, mời vào.', 'Yes, go ahead.', ('boss', '「いらっしゃいませ」を 元気に！', 'Nói "Irasshaimase" to và vui lên nào!', "Say 'Irasshaimase' with energy!")),
   ('poor', 'えっと……わかりません。', 'Ờ… tôi không biết.', "Um… I don't know.", ('guest', '……ほかの 店に 行こうか。', '…Mình sang quán khác nhé.', '…Shall we go somewhere else?'))])

S('food', 3, None, ('Khách bị dị ứng', 'An allergy', 'アレルギー'),
  {'saki': SAKI, 'guest': ('Khách', 'Guest', 'お客様')},
  [('saki', 'あの お客様、注文が あるって。', 'Vị khách kia muốn gọi món.', 'That guest wants to order.')],
  ('guest', 'えびの アレルギーが あるんです。大丈夫ですか。', 'Tôi bị dị ứng tôm. Có sao không?', "I'm allergic to shrimp. Is that okay?"),
  [('ok', 'たぶん 大丈夫です。', 'Chắc không sao đâu.', "It's probably fine.", ('saki', '「たぶん」は だめ。必ず 大将に 聞いてね。', '"Chắc" là không được. Phải hỏi bác chủ quán.', "'Probably' won't do. Always ask the chef.")),
   ('best', '確認して まいります。えびを 使わない メニューを ご案内します。', 'Để tôi hỏi bếp. Tôi sẽ giới thiệu món không có tôm.', "I'll check with the kitchen and show you dishes without shrimp.", ('boss', 'よし。お客様の 安全が 一番だ。', "Tốt. An toàn của khách là trên hết.", "Good. The guest's safety comes first.")),
   ('poor', 'わかりません。好きな ものを どうぞ。', 'Tôi không biết. Cứ gọi món thích đi.', "I don't know. Order what you like.", ('guest', 'それは こわいです……', 'Vậy thì sợ lắm…', "That's scary…"))])

S('food', 4, None, ('Tối bận rộn', 'A busy evening', '忙しい夜'),
  {'saki': SAKI},
  [('boss', '今夜は 団体の 予約が 三つ ある。', 'Tối nay có ba đoàn đặt bàn.', 'We have three group bookings tonight.'),
   ('saki', 'ホールの 人が 足りません！', 'Thiếu người phục vụ rồi!', "We're short of servers!")],
  ('boss', 'リーダーとして、どう 動く？', 'Là trưởng ca, cháu xử lý thế nào?', 'As shift leader, how will you handle it?'),
  [('best', '団体ごとに 担当を 決めて、私は 全体を 見ます。', 'Phân mỗi đoàn một người phụ trách, tôi trông toàn bộ.', "I'll assign someone to each group and watch over everything.", ('saki', 'わかりやすい！みんな 動きやすいです。', 'Rõ ràng quá! Ai cũng dễ làm.', 'Clear! Everyone knows what to do.')),
   ('poor', '予約を 一つ 断りましょう。', 'Huỷ bớt một đoàn đi.', "Let's cancel one booking.", ('boss', '当日に 断るのは 失礼だ。', 'Huỷ ngay trong ngày là thất lễ.', 'Cancelling on the day is rude.')),
   ('ok', '私が 全部の テーブルを 回ります。', 'Tôi tự chạy hết các bàn.', "I'll cover every table myself.", ('boss', '一人で 全部は むりだ。人に 任せる ことも 覚えよう。', 'Một mình không làm hết được. Học cách giao việc đi.', "You can't do it all alone. Learn to delegate."))])

S('food', 5, 'itamae', ('Con cá ngừ', 'The tuna', 'マグロ'),
  {},
  [('boss', '今日の マグロは お前が 切って みろ。', 'Hôm nay cháu thử cắt cá ngừ.', 'Today, you cut the tuna.'),
   ('narrator', '包丁が 少し 切れにくいです。', 'Con dao hơi cùn.', 'The knife is a bit dull.')],
  ('boss', 'どうする？', 'Cháu làm gì?', 'What will you do?'),
  [('ok', '力を 入れて 切ります。', 'Dùng lực cắt mạnh.', "I'll cut with more force.", ('boss', '身が つぶれた。力じゃない、刃だ。', 'Thịt cá bị dập rồi. Không phải sức, mà là lưỡi dao.', "You crushed the flesh. It's the blade, not force.")),
   ('best', '先に 包丁を 研ぎます。それから 切ります。', 'Mài dao trước, rồi mới cắt.', "I'll sharpen the knife first, then cut.", ('boss', 'そうだ。道具を 大事に する 人が いい 職人に なる。', 'Đúng. Người quý dụng cụ mới thành thợ giỏi.', 'Right. Those who care for their tools become good craftsmen.')),
   ('poor', '別の 人に 頼みます。', 'Nhờ người khác cắt.', "I'll ask someone else.", ('boss', '逃げるな。やって みろ。', 'Đừng né. Thử đi.', "Don't run away. Try it."))])

S('food', 5, 'owner', ('Đánh giá xấu', 'A bad review', '悪い口コミ'),
  {'saki': SAKI},
  [('saki', 'ネットに「待ち時間が 長い」って 書かれました。', 'Trên mạng có người viết "chờ lâu quá".', "Someone wrote online that 'the wait is too long.'")],
  ('boss', '副店長、どう する？', 'Phó quản lý, cháu làm gì?', 'Deputy manager, what will you do?'),
  [('best', 'ていねいに 返事を 書いて、予約の 仕組みを 見直します。', 'Viết trả lời lịch sự và xem lại cách nhận đặt bàn.', "I'll reply politely and rethink how we take bookings.", ('saki', '待ち時間が 半分に なりました！', 'Thời gian chờ giảm một nửa!', 'The wait time halved!')),
   ('poor', 'その お客様は もう 来なくて いいです。', 'Vị khách đó đừng đến nữa cũng được.', "That customer doesn't need to come back.", ('saki', 'そんな 言い方は……', 'Nói vậy thì…', "You can't say that…")),
   ('ok', '気に しないで おきます。', 'Kệ nó đi.', "I'll ignore it.", ('boss', 'お客様の 声は 宝だよ。', 'Ý kiến khách là vàng đấy.', 'Customer feedback is a treasure.'))])

S('food', 6, 'itamae', ('Hương vị của quán', "The shop's flavour", '大将の味'),
  {'regular': ('Khách quen lâu năm', 'Old regular', '常連さん')},
  [('boss', 'わしは そろそろ 引退だ。この 店の 味を 頼む。', 'Bác sắp nghỉ hưu. Hương vị quán nhờ cháu giữ.', "I'll retire soon. I'm counting on you to keep this shop's flavour."),
   ('regular', '新しい 大将の すし、楽しみに してるよ。', 'Mong sushi của bếp trưởng mới lắm.', "Can't wait for the new chef's sushi.")],
  ('boss', 'お前の すしは どんな すしだ？', 'Sushi của cháu là sushi thế nào?', 'What kind of sushi is yours?'),
  [('ok', '大将と まったく 同じに します。', 'Làm y hệt bác.', 'Exactly the same as yours.', ('boss', 'まねだけでは 足りない。お前の 味も 見せろ。', 'Bắt chước chưa đủ. Cho bác thấy vị của cháu.', "Copying isn't enough. Show your own flavour.")),
   ('poor', '全部 新しい メニューに 変えます。', 'Đổi hết sang thực đơn mới.', "I'll change the whole menu.", ('regular', 'いつもの すしが ない……', 'Không còn món sushi quen…', 'My usual sushi is gone…')),
   ('best', '大将の 味を 守りながら、季節の 魚で 新しい 一品も 出します。', 'Giữ vị của bác, và thêm một món mới theo cá mùa.', "I'll keep your flavour and add a new dish with seasonal fish.", ('boss', 'それで いい。伝統は 続けながら 変わるものだ。', 'Vậy là được. Truyền thống là vừa giữ vừa đổi mới.', 'Good. Tradition lives on by changing.'))])

S('food', 6, 'owner', ('Chi nhánh thứ hai', 'A second shop', '二号店'),
  {'saki': SAKI},
  [('boss', '二号店を 出さないかと 言われた。', 'Có người mời mở chi nhánh hai.', "We've been asked to open a second shop."),
   ('saki', 'でも、職人が まだ 足りません。', 'Nhưng còn thiếu đầu bếp.', "But we still don't have enough chefs.")],
  ('boss', 'オーナーとして、どう 決める？', 'Là chủ quán, cháu quyết thế nào?', "As owner, what's your decision?"),
  [('best', 'まず 若い 職人を 育てて、一年後に 開きます。', 'Trước hết đào tạo đầu bếp trẻ, một năm sau mới mở.', "We'll train young chefs first and open in a year.", ('boss', '急がば 回れ、だな。', 'Dục tốc bất đạt mà.', 'More haste, less speed.')),
   ('ok', '来月 すぐに 開きます。', 'Mở ngay tháng sau.', "We'll open next month.", ('saki', '人が 足りなくて、どちらの 店も 大変です……', 'Thiếu người, cả hai quán đều vất vả…', 'Short-handed, both shops are struggling…')),
   ('poor', '安い 冷凍の 魚で 開きます。', 'Mở bằng cá đông lạnh giá rẻ.', "We'll open with cheap frozen fish.", ('boss', 'それは もう ひばり寿司じゃない。', 'Thế thì không còn là Sushi Hibari nữa.', "That's not Hibari Sushi anymore."))])

# ─────────── Nông nghiệp · đảo Midori (cô Hana) ───────────
SORA = ('Bé Sora (học sinh)', 'Sora (a pupil)', 'そらくん')

S('farm', 2, None, ('Cây con héo', 'Weak seedlings', 'しおれた苗'),
  {},
  [('boss', 'この 苗、元気が ないね。', 'Cây con này héo quá.', 'These seedlings look weak.')],
  ('boss', 'どう したら いいと 思う？', 'Em nghĩ nên làm gì?', 'What do you think we should do?'),
  [('best', '土を さわって みます。かわいて いたら、水を あげます。', 'Sờ thử đất. Nếu khô thì tưới nước.', "I'll feel the soil. If it's dry, I'll water them.", ('boss', 'そう、まず 見て、さわって。それが 農業の 基本。', 'Đúng, trước tiên nhìn và sờ. Đó là cơ bản của nhà nông.', "Yes — look and touch first. That's farming basics.")),
   ('ok', '肥料を たくさん あげます。', 'Bón thật nhiều phân.', "I'll give lots of fertiliser.", ('boss', '多すぎると 根が いたむよ。まず 土を 見てね。', 'Nhiều quá là hỏng rễ. Xem đất trước đã.', 'Too much hurts the roots. Check the soil first.')),
   ('poor', '抜いて すてます。', 'Nhổ bỏ đi.', "I'll pull them out and throw them away.", ('boss', 'まだ 生きてるのに……', 'Còn sống mà…', "They're still alive…"))])

S('farm', 3, None, ('Mưa đá', 'Hail', 'ひょう'),
  {},
  [('narrator', 'ラジオ：「今夜、ひょうが 降るでしょう。」', 'Đài: "Đêm nay có thể có mưa đá."', "Radio: 'Hail is expected tonight.'")],
  ('boss', 'トマトが もうすぐ とれるのに。どうする？', 'Cà chua sắp thu hoạch rồi. Làm sao đây?', 'The tomatoes are almost ready. What now?'),
  [('ok', '朝 早く 様子を 見に 来ます。', 'Sáng sớm tôi ra xem.', "I'll check early in the morning.", ('boss', '朝では 遅かった……半分 だめに なったね。', 'Sáng thì muộn rồi… hỏng mất một nửa.', 'Morning was too late… half are ruined.')),
   ('best', '暗く なる 前に、ネットを かけて、熟れた トマトは 先に とります。', 'Trước khi tối, phủ lưới và hái trước quả chín.', "Before dark, I'll cover them with nets and pick the ripe ones first.", ('boss', '完璧！ほとんど 無事だったよ。', 'Hoàn hảo! Gần như không sao cả.', 'Perfect! Almost all of them survived.')),
   ('poor', '天気は どう しようも ないです。', 'Thời tiết thì chịu thôi.', 'Nothing can be done about weather.', ('boss', 'できる ことは あったんだよ。', 'Vẫn có việc làm được mà.', 'There were things we could do.'))])

S('farm', 4, None, ('Học sinh tham quan', 'A school visit', '農業体験'),
  {'sora': SORA},
  [('boss', 'ひばり小学校の 子どもたちが 見学に 来るよ。', 'Học sinh tiểu học Hibari đến tham quan đấy.', 'Children from Hibari Elementary are coming to visit.')],
  ('sora', 'にんじんって、どこに なるの？', 'Cà rốt mọc ở đâu ạ?', 'Where do carrots grow?'),
  [('best', '土の 中だよ。いっしょに 抜いて みよう！', 'Trong đất đấy. Cùng nhổ thử nào!', "In the ground! Let's pull one up together!", ('sora', 'わあ、大きい！にんじん 好きに なった！', 'Oa, to quá! Em thích cà rốt rồi!', 'Wow, it\'s big! I like carrots now!')),
   ('poor', '今 いそがしいから、あとでね。', 'Giờ bận, để sau nhé.', "I'm busy now, later.", ('boss', '子どもの 質問は、未来の 農家の たねだよ。', 'Câu hỏi của trẻ con là hạt giống nhà nông tương lai.', "A child's question is the seed of a future farmer.")),
   ('ok', '木に なるんじゃないよ。', 'Không mọc trên cây đâu.', 'Not on trees.', ('sora', 'じゃあ、どこ？', 'Vậy ở đâu ạ?', 'Then where?'))])

S('farm', 5, 'grower', ('Sâu hại', 'Pests', '害虫'),
  {'guest': ('Khách ở chợ', 'Market customer', 'お客さん')},
  [('boss', 'キャベツに 虫が たくさん いる。', 'Bắp cải nhiều sâu quá.', 'The cabbages are full of bugs.')],
  ('boss', '野菜の 専門家として、どう する？', 'Là chuyên gia rau, em làm gì?', 'As the vegetable expert, what will you do?'),
  [('ok', '農薬を まきます。', 'Phun thuốc trừ sâu.', "I'll spray pesticide.", ('boss', '効くけど、使いすぎには 気を つけてね。', 'Có tác dụng, nhưng đừng lạm dụng.', "It works, but don't overuse it.")),
   ('best', 'ネットで 守って、虫を 食べる てんとう虫も 使います。', 'Phủ lưới và dùng bọ rùa ăn sâu.', "I'll protect them with nets and use ladybirds that eat the bugs.", ('boss', '農薬を 減らせたね。お客様も 安心だ。', 'Giảm được thuốc trừ sâu. Khách cũng yên tâm.', 'We cut down pesticides. Customers can relax.')),
   ('poor', '虫の ついた まま 売ります。', 'Bán luôn cả sâu.', "I'll sell them, bugs and all.", ('guest', '虫が いる！返品します。', 'Có sâu! Tôi trả lại.', "There's a bug! I'm returning this."))])

S('farm', 5, 'ranch', ('Con bò ốm', 'A sick cow', '牛の体調'),
  {'vet': ('Bác sĩ thú y', 'Vet', '獣医さん')},
  [('boss', '牛の ハナコが 朝から 何も 食べないの。', 'Bò Hanako từ sáng chưa ăn gì.', "Hanako the cow hasn't eaten since morning.")],
  ('boss', 'どう する？', 'Làm sao đây?', 'What will you do?'),
  [('best', 'すぐ 獣医さんに 電話して、水と 体温を 見ます。', 'Gọi ngay bác sĩ thú y, xem nước uống và nhiệt độ.', "I'll call the vet now and check her water and temperature.", ('vet', '早く 呼んで くれて よかった。もう 大丈夫。', 'May mà gọi sớm. Ổn rồi.', "Good thing you called early. She'll be fine.")),
   ('poor', 'えさを むりに 食べさせます。', 'Ép nó ăn.', "I'll force her to eat.", ('boss', 'それは かわいそう……', 'Tội nó…', 'Poor thing…')),
   ('ok', '明日まで 待って みます。', 'Đợi đến mai xem.', "I'll wait until tomorrow.", ('boss', '動物は がまんするから、早めが いいんだよ。', 'Con vật hay chịu đựng, nên phải sớm.', 'Animals hide pain — sooner is better.'))])

S('farm', 6, 'grower', ('Nông trại hữu cơ', 'The organic farm', '有機農園'),
  {'ito': ('Anh Ito (Hibari Mart)', 'Ito (Hibari Mart)', 'いとう店長')},
  [('boss', 'この 畑を、あなたに ゆずりたい。', 'Chị muốn giao lại mảnh ruộng này cho em.', 'I want to hand this field over to you.'),
   ('ito', '有機野菜を 毎週 ほしいんです。', 'Tôi muốn rau hữu cơ mỗi tuần.', "I'd like organic vegetables every week.")],
  ('boss', '農園の 主として、何を 大切に する？', 'Là chủ nông trại, em coi trọng điều gì?', "As the farm's owner, what will you value?"),
  [('poor', 'いちばん 高く 売れる ものだけ 作ります。', 'Chỉ trồng thứ bán đắt nhất.', "I'll only grow what sells highest.", ('boss', '同じ ものばかりだと、土が やせるよ。', 'Trồng một thứ mãi là đất bạc màu.', 'Growing one thing only wears out the soil.')),
   ('ok', 'たくさん 作って、たくさん 売ります。', 'Trồng nhiều, bán nhiều.', 'Grow lots, sell lots.', ('boss', '量も 大事。でも、土が つかれないようにね。', 'Số lượng cũng quan trọng, nhưng đừng để đất kiệt.', "Quantity matters, but don't exhaust the soil.")),
   ('best', '土を 育てる ことです。毎年 作物を 変えて、土を 休ませます。', 'Nuôi đất. Mỗi năm đổi cây trồng để đất nghỉ.', 'Nurturing the soil. I\'ll rotate crops so it can rest.', ('boss', '安心して まかせられる。', 'Chị yên tâm giao rồi.', 'I can leave it to you with peace of mind.'))])

S('farm', 6, 'ranch', ('Trang trại mở cửa', 'The open ranch', '牧場体験'),
  {'sora': SORA},
  [('boss', '牧場を 町の 人に 開きたいね。', 'Chị muốn mở trang trại cho người trong phố đến.', "I'd like to open the ranch to town folk."),
   ('sora', '馬に 乗りたい！', 'Em muốn cưỡi ngựa!', 'I want to ride a horse!')],
  ('boss', '牧場の 主として、どう する？', 'Là chủ trang trại, em làm gì?', 'As the ranch owner, what will you do?'),
  [('best', 'ヘルメットを つけて、私が 馬を 引きます。動物の 話も します。', 'Đội mũ bảo hiểm, tôi dắt ngựa, kể chuyện về con vật.', "Helmet on, I'll lead the horse and talk about the animals.", ('sora', '楽しかった！また 来たい！', 'Vui quá! Em muốn đến nữa!', 'That was fun! I want to come again!')),
   ('ok', '見るだけに して ください。', 'Chỉ được xem thôi.', 'Please just watch.', ('sora', 'えー、ざんねん……', 'Ơ, tiếc quá…', 'Aww, too bad…')),
   ('poor', '一人で 乗って いいよ。', 'Tự cưỡi đi.', 'Go ahead and ride alone.', ('boss', '子ども 一人は あぶない！', 'Trẻ con một mình nguy hiểm!', 'A child alone is dangerous!'))])

# ─────────── Nhà hàng Phú Quốc · Gánh Ngon (chị Mai) ───────────
TOURIST = ('Khách du lịch Nhật', 'Japanese tourist', '日本人観光客')

S('hospitality', 2, None, ('Khách Nhật đầu tiên', 'First Japanese guests', '日本人のお客様'),
  {'tourist': TOURIST},
  [('boss', '日本人の お客様です。あなたが 案内して。', 'Khách Nhật đấy. Em ra đón nhé.', 'Japanese guests. You show them in.')],
  ('tourist', 'おすすめは 何ですか。', 'Món nào nên thử ạ?', 'What do you recommend?'),
  [('ok', '全部 おいしいです。', 'Món nào cũng ngon.', 'Everything is good.', ('boss', '一つ 選んで、説明して あげて。', 'Chọn một món và giải thích cho khách.', 'Pick one and explain it.')),
   ('best', 'ニシンの サラダ（ゴイ・カー・チック）が おすすめです。島の 名物です。', 'Nên thử gỏi cá trích, đặc sản của đảo.', 'Try the herring salad (gỏi cá trích), an island speciality.', ('tourist', 'おいしそう！それに します。', 'Nghe ngon quá! Lấy món đó.', "Sounds delicious! I'll have that.")),
   ('poor', 'メニューを 見て ください。', 'Xem thực đơn đi.', 'Please look at the menu.', ('tourist', '……読めないんですけど。', '…Tôi không đọc được.', "…I can't read it."))])

S('hospitality', 3, None, ('Món cay', 'Spicy food', '辛いもの'),
  {'tourist': TOURIST},
  [('narrator', 'お客様が 唐辛子を 見て、少し 心配そうです。', 'Khách nhìn ớt, có vẻ hơi lo.', 'The guest looks at the chillies, a little worried.')],
  ('tourist', 'これ、辛いですか。', 'Món này cay không?', 'Is this spicy?'),
  [('best', '少し 辛いです。唐辛子を 別の お皿に しましょうか。', 'Hơi cay ạ. Tôi để ớt ra đĩa riêng nhé?', 'A little. Shall I put the chillies on a separate plate?', ('tourist', 'ありがとう！助かります。', 'Cảm ơn! Đỡ quá.', 'Thank you! That helps.')),
   ('poor', 'ベトナム料理は 辛いのが 普通です。', 'Món Việt cay là bình thường.', 'Vietnamese food is normally spicy.', ('boss', 'お客様に 合わせて あげようね。', 'Mình chiều theo khách nhé.', "Let's adjust to the guest.")),
   ('ok', '大丈夫です。', 'Không sao đâu.', "It's fine.", ('tourist', 'か、辛い！水を ください！', 'Ca… cay quá! Cho tôi nước!', 'S-spicy! Water, please!'))])

S('hospitality', 4, None, ('Mất điện', 'A power cut', '停電'),
  {'tourist': TOURIST},
  [('narrator', '夕食の 時間に、停電に なりました。', 'Giờ ăn tối thì mất điện.', 'The power goes out at dinnertime.'),
   ('boss', 'お客様が ざわざわ して いる！', 'Khách bắt đầu xôn xao!', 'The guests are getting restless!')],
  ('boss', 'シフトリーダー、どう する？', 'Trưởng ca, em làm gì?', 'Shift leader, what will you do?'),
  [('ok', '電気が もどるまで 待ちます。', 'Đợi có điện lại.', "We'll wait for the power to return.", ('boss', '待つ 間も、お客様に 声を かけて。', 'Trong lúc chờ, hỏi han khách đi.', 'While waiting, talk to the guests.')),
   ('poor', '今日は もう 閉店です。', 'Hôm nay đóng cửa luôn.', "We're closing for today.", ('tourist', 'まだ 食べて いないのに……', 'Chúng tôi chưa ăn mà…', "We haven't eaten yet…")),
   ('best', 'ろうそくを 置いて、日本語と ベトナム語で 説明します。', 'Đặt nến, giải thích bằng tiếng Nhật và tiếng Việt.', "I'll set out candles and explain in Japanese and Vietnamese.", ('tourist', 'ろうそくの 夕食、すてき！', 'Bữa tối dưới nến, lãng mạn quá!', 'A candlelit dinner — lovely!'))])

S('hospitality', 5, 'kitchen', ('Nước mắm đảo', "The island's fish sauce", 'ヌックマム'),
  {'tourist': TOURIST},
  [('boss', '日本の お客様に 島の ヌックマムを 紹介したい。', 'Chị muốn giới thiệu nước mắm đảo cho khách Nhật.', "I want to introduce the island's fish sauce to Japanese guests.")],
  ('tourist', 'においが 強いですね……', 'Mùi mạnh nhỉ…', 'It smells strong…'),
  [('best', '少しだけ、ライムと 砂糖で つけだれに します。どうぞ。', 'Pha chút ít với chanh, đường thành nước chấm. Mời anh/chị.', 'Just a little, mixed with lime and sugar as a dip. Please try.', ('tourist', 'あ、おいしい！お土産に 買いたい。', 'Ồ, ngon! Muốn mua làm quà.', 'Oh, tasty! I want to buy some as a souvenir.')),
   ('ok', 'なれれば おいしいです。', 'Quen rồi thì ngon.', "It's tasty once you're used to it.", ('tourist', 'うーん……やめて おきます。', 'Ừm… thôi vậy.', "Hmm… I'll pass.")),
   ('poor', 'いやなら 食べなくて いいです。', 'Không thích thì thôi khỏi ăn.', "If you don't like it, don't eat it.", ('boss', '島の 自慢を そんなふうに……', 'Niềm tự hào của đảo mà em nói vậy…', 'Our island\'s pride, and you say that…'))])

S('hospitality', 5, 'manager', ('Đoàn khách lớn', 'A tour group', 'ツアー客'),
  {'guide': ('Hướng dẫn viên', 'Tour guide', 'ガイドさん')},
  [('guide', '明日、四十人の 日本人ツアーで 行きたいんです。', 'Mai tôi muốn dẫn đoàn 40 khách Nhật đến.', "Tomorrow I'd like to bring a tour of forty Japanese guests.")],
  ('boss', '副マネージャー、受けられる？', 'Phó quản lý, nhận được không?', 'Deputy manager, can we take it?'),
  [('poor', '四十人は むりです。', '40 người thì chịu.', 'Forty is impossible.', ('boss', '少し 考えれば できたのに……', 'Nghĩ một chút là làm được mà…', 'With a little planning we could have.')),
   ('best', 'はい。メニューを 決めて、スタッフを 二人 増やします。', 'Được. Chốt thực đơn trước và thêm hai nhân viên.', "Yes. We'll fix the menu ahead and add two staff.", ('guide', 'スムーズでした！また お願いします。', 'Suôn sẻ quá! Lần sau lại nhờ nhé.', "That went smoothly! We'll come again.")),
   ('ok', 'はい、大丈夫です。', 'Vâng, được ạ.', 'Yes, no problem.', ('boss', '準備なしで、料理が とても 遅れたね。', 'Không chuẩn bị nên món ra rất chậm.', 'Without preparation, the food came out very late.'))])

S('hospitality', 6, 'kitchen', ('Món đặc trưng', 'A signature dish', '看板料理'),
  {'tourist': TOURIST},
  [('boss', '料理長として、新しい 看板料理を 作って。', 'Là bếp trưởng, em làm món đặc trưng mới nhé.', 'As head chef, create a new signature dish.')],
  ('boss', 'どんな 料理に する？', 'Món gì đây?', 'What will it be?'),
  [('best', '島の 海の幸と 日本の だしを 合わせた フォーを 作ります。', 'Làm phở hải sản đảo với nước dùng dashi Nhật.', 'A phở with island seafood and Japanese dashi.', ('tourist', 'ベトナムと 日本が いっしょに なった 味！', 'Vị Việt và Nhật hoà làm một!', 'Vietnam and Japan in one flavour!')),
   ('ok', '有名な 料理を そのまま 出します。', 'Làm y món nổi tiếng.', "We'll serve a famous dish as is.", ('boss', 'おいしいけど、ガインゴンらしさが ほしいね。', 'Ngon, nhưng cần chất Gánh Ngon.', 'Tasty, but it needs a Gánh Ngon touch.')),
   ('poor', '冷凍の 料理を 温めて 出します。', 'Hâm đồ đông lạnh ra bán.', "We'll reheat frozen food.", ('tourist', '……ふつうの 味ですね。', '…Vị bình thường nhỉ.', '…It tastes ordinary.'))])

S('hospitality', 6, 'manager', ('Cầu nối', 'The bridge', 'かけはし'),
  {'agent': ('Nhân viên công ty du lịch', 'Travel agent', '旅行会社の人')},
  [('boss', '日本の 旅行会社が、うちと 契約したいって。', 'Một công ty du lịch Nhật muốn ký hợp đồng với quán.', 'A Japanese travel company wants a contract with us.')],
  ('agent', 'お店の 強みは 何ですか。', 'Điểm mạnh của quán là gì?', "What is your restaurant's strength?"),
  [('ok', '安いです。', 'Rẻ ạ.', "We're cheap.", ('agent', '安いだけでは……', 'Chỉ rẻ thôi thì…', 'Being cheap alone…')),
   ('best', '島の 食材と、日本語で できる おもてなしです。', 'Nguyên liệu của đảo, và sự hiếu khách bằng tiếng Nhật.', 'Island ingredients and hospitality in Japanese.', ('agent', 'ぜひ いっしょに やりましょう。', 'Nhất định hợp tác nhé.', "Let's definitely work together.")),
   ('poor', 'とくに ありません。', 'Không có gì đặc biệt.', 'Nothing in particular.', ('agent', 'では、またの 機会に。', 'Vậy để dịp khác.', 'Another time, then.'))])
