"""Career milestone stories: producer (Hibiki DJ booth), painter (Hibiki easel), fashion (Hibari Mode)."""
from career_stories import S

# ─────────── DJ & nhà sản xuất · bàn DJ Hibiki (DJ Sōta) ───────────
CROWD = ('Khán giả', 'Crowd', '観客')
RINA = ('Ca sĩ Rina', 'Singer Rina', '歌手のリナ')

S('producer', 2, None, ('Phần mở màn', 'The opening set', 'オープニング'),
  {},
  [('boss', '今夜の オープニングを まかせる。', 'Giao cháu phần mở màn tối nay.', "Tonight's opening set is yours."),
   ('narrator', 'まだ お客様は 少ないです。', 'Khách còn ít.', 'Few guests have arrived yet.')],
  ('boss', 'どんな 音に する？', 'Chơi nhạc thế nào?', 'What sound will you go for?'),
  [('ok', '好きな 曲を 順番に かけます。', 'Bật lần lượt bài mình thích.', 'Play my favourites in order.', ('boss', '流れを 考えると、もっと よく なるよ。', 'Nghĩ đến mạch bài thì sẽ hay hơn.', "Think about the flow and it'll get better.")),
   ('best', 'ゆったりした 曲から 始めて、少しずつ もり上げます。', 'Bắt đầu nhạc chậm, rồi nâng dần không khí.', 'Start mellow and build up little by little.', ('boss', '完璧な ウォームアップだ。', 'Khởi động hoàn hảo.', 'A perfect warm-up.')),
   ('poor', '最初から いちばん 激しい 曲を かけます。', 'Bật ngay bài sung nhất.', 'Hit the hardest track straight away.', ('boss', 'まだ 早すぎる。メインの DJが 困るよ。', 'Còn quá sớm. DJ chính sẽ khó xử.', 'Too early. The main DJ will struggle.'))])

S('producer', 3, None, ('Âm lượng', 'Volume', '音量'),
  {'neighbor': ('Hàng xóm', 'Neighbour', '近所の人')},
  [('neighbor', '音が 大きすぎて、赤ちゃんが 起きて しまいました。', 'Nhạc to quá, em bé nhà tôi thức giấc.', 'It is so loud my baby woke up.')],
  ('boss', 'どう 対応する？', 'Xử lý thế nào?', 'How will you respond?'),
  [('best', '申し訳ありません。低音を 下げて、窓を 閉めます。', 'Xin lỗi. Tôi giảm âm trầm và đóng cửa sổ.', "I'm sorry. I'll turn down the bass and close the windows.", ('neighbor', 'ありがとう。これなら 大丈夫です。', 'Cảm ơn. Thế này thì ổn.', "Thank you. That's fine now.")),
   ('poor', 'ここは 音楽の 店です。', 'Đây là tiệm nhạc mà.', 'This is a music shop.', ('boss', 'ご近所あっての 店だよ。', 'Có hàng xóm mới có tiệm.', 'The shop exists thanks to its neighbours.')),
   ('ok', 'あと 十分で 終わります。', 'Mười phút nữa xong ạ.', "We'll finish in ten minutes.", ('neighbor', '十分も……', 'Tận mười phút…', 'Ten whole minutes…'))])

S('producer', 4, None, ('Thiết bị hỏng', 'Gear failure', '機材トラブル'),
  {'crowd': CROWD},
  [('narrator', 'ライブ中、ミキサーの 片方が 動かなく なりました。', 'Đang diễn thì một bên bàn mixer hỏng.', 'Mid-show, one side of the mixer dies.')],
  ('boss', 'レジデントDJ、どう する？', 'DJ thường trú, cháu làm gì?', 'Resident DJ, what will you do?'),
  [('poor', '今日は 終わりに します。', 'Hôm nay kết thúc.', 'Call it a night.', ('crowd', 'えー！まだ 九時だよ！', 'Ơ! Mới chín giờ mà!', "What! It's only nine!")),
   ('ok', '音を 止めて 直します。', 'Dừng nhạc để sửa.', 'Stop the music and fix it.', ('boss', '直せたけど、フロアが 冷えたね。', 'Sửa được nhưng sàn nhảy nguội mất.', 'Fixed, but the floor cooled down.')),
   ('best', '一台の デッキだけで 続けながら、スタッフに 予備を たのみます。', 'Chơi tiếp bằng một đầu đĩa, nhờ nhân viên mang máy dự phòng.', 'Keep going on one deck while asking staff for the spare.', ('crowd', '止まらなかった！すごい！', 'Không hề dừng! Đỉnh!', 'It never stopped! Amazing!'))])

S('producer', 5, 'club_dj', ('Yêu cầu bài hát', 'A request', 'リクエスト'),
  {'fan': ('Khách', 'Guest', 'お客さん')},
  [('fan', '私の 好きな 曲、かけて ください！', 'Bật bài tôi thích đi!', 'Play my favourite song!'),
   ('narrator', 'でも、今の 流れと 合いません。', 'Nhưng bài đó không hợp mạch nhạc lúc này.', "But it doesn't fit the current flow.")],
  ('narrator', 'どう しますか。', 'Bạn làm gì?', 'What do you do?'),
  [('best', 'いいですね！二曲 あとに、つながる ように 入れます。', 'Hay đấy! Hai bài nữa tôi sẽ nối vào cho mượt.', "Great choice! I'll blend it in two tracks from now.", ('fan', 'かかった！最高！', 'Bật rồi! Tuyệt nhất!', 'There it is! The best!')),
   ('ok', '今すぐ かけます。', 'Bật ngay.', 'Right now.', ('boss', '急に 変わって、フロアが とまどったね。', 'Đổi đột ngột, sàn nhảy lúng túng.', 'The sudden switch confused the floor.')),
   ('poor', 'リクエストは 受けません。', 'Không nhận yêu cầu.', 'No requests.', ('fan', 'つめたい……', 'Lạnh lùng thế…', 'So cold…'))])

S('producer', 5, 'producer', ('Đoạn nhạc mẫu', 'The sample', 'サンプル'),
  {'rina': RINA},
  [('narrator', 'スタジオで 新曲を 作って います。', 'Đang làm bài mới trong phòng thu.', "You're making a new song in the studio.")],
  ('rina', 'この 有名な 曲の 一部、使っちゃって いい？', 'Dùng một đoạn bài nổi tiếng này được không?', 'Can we just use part of this famous song?'),
  [('ok', '少しだけなら いいと 思う。', 'Một chút chắc được.', 'A little should be fine.', ('boss', '少しでも 許可は 必要だ。', 'Một chút cũng cần xin phép.', 'Even a little needs permission.')),
   ('best', '権利者に 許可を 取ってからに しよう。だめなら 自分で 作るよ。', 'Xin phép chủ bản quyền trước đã. Không được thì tự làm.', "Let's get permission from the rights holder first. If not, I'll make our own.", ('rina', 'オリジナルの 方が、もっと よく なった！', 'Bản tự làm còn hay hơn!', 'The original turned out even better!')),
   ('poor', 'だれも 気づかないよ。', 'Không ai để ý đâu.', 'Nobody will notice.', ('boss', 'それで 曲を 消された 人を 知ってる。', 'Chú biết người bị gỡ nhạc vì vậy.', 'I know people whose songs got taken down for that.'))])

S('producer', 6, 'club_dj', ('Lễ hội mùa hè', 'The summer festival', '夏フェス'),
  {'staff': ('Nhân viên lễ hội', 'Festival staff', 'スタッフ'), 'crowd': CROWD},
  [('boss', '夏フェスの メインステージだ。', 'Sân khấu chính lễ hội mùa hè đấy.', "It's the summer festival main stage."),
   ('narrator', '急に 雨が 降って きました。', 'Đột nhiên trời mưa.', 'Suddenly it starts to rain.')],
  ('staff', '続けますか。', 'Diễn tiếp không?', 'Shall we continue?'),
  [('poor', '雨でも 気に せず、そのまま 続けます。', 'Kệ mưa, cứ diễn.', 'Ignore the rain and carry on.', ('staff', '機材が ぬれて 危ない！', 'Thiết bị ướt, nguy hiểm!', "The gear is wet — it's dangerous!")),
   ('best', '機材を 守って、安全を 確かめてから 続けます。お客様にも 声を かけます。', 'Che chắn thiết bị, kiểm tra an toàn rồi diễn tiếp, nhắc khán giả nữa.', 'Protect the gear, check safety, continue — and talk to the crowd.', ('crowd', '雨の 中の ダンス、一生 忘れない！', 'Nhảy dưới mưa, nhớ cả đời!', 'Dancing in the rain — unforgettable!')),
   ('ok', '雨が やむまで 待ちます。', 'Đợi tạnh mưa.', 'Wait for the rain to stop.', ('staff', '待って いる 間に、人が 減りました。', 'Trong lúc chờ, khách về bớt.', 'While we waited, people left.'))])

S('producer', 6, 'producer', ('Hãng đĩa riêng', 'My own label', 'レーベル'),
  {'rina': RINA},
  [('boss', '自分の レーベルを 作るんだって？', 'Nghe nói cháu lập hãng đĩa riêng?', "I hear you're starting your own label?"),
   ('rina', '新人を 育てて ほしいです。', 'Mong anh nâng đỡ người mới.', 'Please nurture new artists.')],
  ('boss', 'どんな レーベルに する？', 'Hãng đĩa thế nào?', 'What kind of label?'),
  [('best', '町の 若い アーティストを 見つけて、公平な 契約で 育てます。', 'Tìm nghệ sĩ trẻ trong phố, nâng đỡ bằng hợp đồng công bằng.', 'Find young local artists and grow them with fair contracts.', ('rina', '安心して 音楽が できます。', 'Yên tâm làm nhạc.', 'I can make music with peace of mind.')),
   ('ok', '有名な 人だけと 契約します。', 'Chỉ ký với người nổi tiếng.', 'Sign only famous artists.', ('boss', '安全だけど、新しい 音は 生まれにくいね。', 'An toàn nhưng khó có âm nhạc mới.', 'Safe, but new sounds rarely emerge.')),
   ('poor', 'アーティストの 取り分を 少なく します。', 'Giảm phần chia cho nghệ sĩ.', 'Give artists a smaller share.', ('rina', 'それは ちょっと……', 'Vậy thì hơi…', "That's a bit…"))])

# ─────────── Hoạ sĩ · giá vẽ Hibiki (chú Hibiki) ───────────
TANAKA = ('Bà Tanaka (khách đặt tranh)', 'Mrs Tanaka (client)', '田中さん')

S('painter', 2, None, ('Đơn vẽ đầu tiên', 'First commission', 'はじめての依頼'),
  {'client': TANAKA},
  [('client', 'うちの 犬の 絵を かいて ほしいんです。', 'Tôi muốn nhờ vẽ con chó nhà tôi.', "I'd like a painting of my dog.")],
  ('boss', 'まず 何を 聞く？', 'Trước tiên cháu hỏi gì?', 'What will you ask first?'),
  [('best', '犬の 名前と 性格、絵を かざる 場所を 教えて ください。', 'Cho tôi biết tên, tính cách của cún và chỗ treo tranh.', "Please tell me the dog's name, personality and where it'll hang.", ('client', 'そこまで 聞いて くれるなんて、うれしい。', 'Hỏi kỹ thế, tôi vui quá.', "I'm glad you ask so much.")),
   ('poor', 'いつまでに お金を くれますか。', 'Khi nào bà trả tiền?', 'When will you pay?', ('client', '……ほかの 人に 頼みます。', '…Tôi nhờ người khác.', "…I'll ask someone else.")),
   ('ok', '写真を ください。', 'Cho tôi ảnh.', 'Give me a photo.', ('boss', '写真だけじゃ、気持ちまでは かけないよ。', 'Chỉ ảnh thì không vẽ được tình cảm.', "A photo alone won't capture feelings."))])

S('painter', 3, None, ('Khác hình dung', 'Not quite right', 'イメージとちがう'),
  {'client': TANAKA},
  [('client', 'うーん、イメージと ちょっと ちがう……', 'Ừm, hơi khác hình dung của tôi…', "Hmm, it's a little different from what I imagined…")],
  ('narrator', 'どう こたえますか。', 'Bạn trả lời thế nào?', 'How do you respond?'),
  [('ok', 'これが 私の スタイルです。', 'Đây là phong cách của tôi.', 'This is my style.', ('boss', 'スタイルも 大事。でも、注文の 絵だよ。', 'Phong cách quan trọng, nhưng đây là tranh đặt mà.', 'Style matters, but this is a commission.')),
   ('best', 'どこが ちがいますか。色ですか、表情ですか。直します。', 'Khác ở đâu ạ? Màu hay nét mặt? Tôi sẽ sửa.', "What's different — the colours or the expression? I'll fix it.", ('client', '目の 感じが、まさに うちの 子です！', 'Ánh mắt đúng là bé nhà tôi!', 'The eyes are exactly my dog!')),
   ('poor', 'もう 完成です。', 'Xong rồi đấy.', "It's finished.", ('client', 'そうですか……', 'Vậy à…', 'I see…'))])

S('painter', 4, None, ('Triển lãm nhỏ', 'A small show', '小さな展示'),
  {'visitor': ('Khách xem tranh', 'Visitor', '来場者')},
  [('boss', '店の ギャラリーで、あなたの 絵を 五枚 かざろう。', 'Treo năm bức của cháu trong gallery tiệm nhé.', "Let's hang five of your works in the shop gallery.")],
  ('visitor', 'この 絵、何を かいたんですか。', 'Bức này vẽ gì vậy?', 'What did you paint here?'),
  [('best', '朝の みどり島です。海の 光を 青と 金色で かきました。', 'Đảo Midori buổi sáng. Tôi vẽ ánh biển bằng xanh và vàng.', 'Midori Island at dawn. I painted the sea light in blue and gold.', ('visitor', '見て いると、海の 音が 聞こえそう。', 'Nhìn mà như nghe tiếng biển.', 'Looking at it, I can almost hear the sea.')),
   ('ok', '見た 通りです。', 'Như anh/chị thấy đấy.', 'What you see.', ('boss', '一言 説明が あると、もっと 伝わるよ。', 'Giải thích một câu thì dễ cảm hơn.', 'A word of explanation helps it reach people.')),
   ('poor', 'わかる 人だけ わかれば いいです。', 'Ai hiểu thì hiểu.', 'Those who get it, get it.', ('visitor', '……そうですか。', '…Vậy à.', '…I see.'))])

S('painter', 5, 'illustrator', ('Hạn nộp gấp', 'An early deadline', 'しめきり'),
  {'editor': ('Biên tập viên', 'Editor', '編集者')},
  [('narrator', '出版社から 電話です。', 'Nhà xuất bản gọi điện.', 'The publisher calls.')],
  ('editor', '表紙の しめきりを、三日 早めて ほしいんです。', 'Muốn hạn nộp bìa sớm ba ngày.', 'We need the cover three days earlier.'),
  [('poor', 'むりです。', 'Không được.', 'Impossible.', ('editor', 'では、別の 方に……', 'Vậy tôi nhờ người khác…', 'Then someone else…')),
   ('best', 'ラフを 明日 送ります。それを 見て、細かい 所を 決めましょう。', 'Mai tôi gửi bản phác. Xem rồi mình chốt chi tiết.', "I'll send a rough tomorrow; let's decide details from that.", ('editor', '早くて 助かります！', 'Nhanh quá, đỡ cho tôi!', 'So quick — that helps!')),
   ('ok', 'がんばって みます。', 'Tôi sẽ cố.', "I'll try my best.", ('editor', '間に合ったけど、少し 雑でしたね。', 'Kịp nhưng hơi ẩu.', 'On time, but a little rough.'))])

S('painter', 5, 'fine_art', ('Lời phê bình', 'A harsh critique', 'きびしい批評'),
  {'critic': ('Nhà phê bình', 'Critic', '評論家')},
  [('critic', '技術は あるが、あなたの 声が 聞こえない。', 'Có kỹ thuật, nhưng không nghe được tiếng nói của bạn.', "You have technique, but I can't hear your voice.")],
  ('boss', 'どう 受け止める？', 'Cháu đón nhận thế nào?', 'How will you take it?'),
  [('best', 'ありがとうございます。自分が 本当に かきたい ものを 考えます。', 'Cảm ơn ông. Tôi sẽ nghĩ về điều mình thật sự muốn vẽ.', "Thank you. I'll think about what I truly want to paint.", ('boss', 'その 素直さが、次の 絵を 強く する。', 'Sự cầu thị đó làm bức sau mạnh hơn.', 'That openness will make your next work stronger.')),
   ('poor', 'あなたに 何が わかるんですか。', 'Ông thì biết gì.', 'What would you know?', ('critic', '……失礼する。', '…Tôi xin phép.', '…Excuse me.')),
   ('ok', '気に しません。', 'Kệ thôi.', "I don't care.", ('boss', '全部 聞かなくて いい。でも、一つは 持ち帰ろう。', 'Không cần nghe hết, nhưng giữ lại một điều.', 'No need to take it all, but keep one thing.'))])

S('painter', 6, 'illustrator', ('Giám đốc nghệ thuật', 'Art director', 'アートディレクター'),
  {'junior': ('Hoạ sĩ trẻ', 'Junior artist', '若手イラストレーター')},
  [('boss', 'ゲームの 絵を まとめる 仕事が 来たよ。', 'Có việc chỉ đạo hình ảnh cho một trò chơi.', "There's work directing the art for a game."),
   ('junior', 'みんな、絵の タッチが バラバラです。', 'Nét vẽ mỗi người một kiểu.', "Everyone's drawing styles are all over the place.")],
  ('junior', 'どう そろえますか。', 'Thống nhất thế nào?', 'How do we unify them?'),
  [('ok', '全部 私が かき直します。', 'Tôi vẽ lại tất cả.', "I'll redraw everything myself.", ('boss', 'それでは 間に合わないし、チームも 育たない。', 'Vậy không kịp mà đội cũng không lớn.', "You won't make it, and the team won't grow.")),
   ('best', '色と 線の ルールを 一枚に まとめて、毎週 いっしょに 見ます。', 'Gom quy tắc màu và nét vào một trang, mỗi tuần cùng xem.', 'Put colour and line rules on one sheet and review together weekly.', ('junior', 'わかりやすい！絵が そろって きました。', 'Dễ hiểu quá! Tranh đồng bộ dần rồi.', 'So clear! The art is coming together.')),
   ('poor', '自由に かいて いいよ。', 'Cứ vẽ tự do.', 'Draw however you like.', ('junior', 'ますます バラバラに……', 'Càng lộn xộn hơn…', 'Even more scattered…'))])

S('painter', 6, 'fine_art', ('Bảo tàng sưu tập', 'The museum', '美術館'),
  {'curator': ('Người phụ trách bảo tàng', 'Curator', '学芸員')},
  [('curator', 'あなたの 絵を、美術館に 収めたいのです。', 'Chúng tôi muốn bảo tàng sưu tập tranh của bạn.', "We'd like your painting for the museum's collection.")],
  ('curator', '作品に ついて、一言 お願いします。', 'Xin bạn nói một lời về tác phẩm.', 'A few words about the work, please.'),
  [('best', 'ベトナムと 日本、二つの 海を 一枚に かきました。見る 人に 故郷を 思い出して ほしいです。', 'Tôi vẽ hai biển, Việt Nam và Nhật Bản, trong một bức. Mong người xem nhớ về quê mình.', "I painted two seas, Vietnam's and Japan's, on one canvas. I hope it reminds viewers of home.", ('curator', 'すてきな ことばです。', 'Lời nói thật đẹp.', 'Beautiful words.')),
   ('poor', '高く 買って ください。', 'Mua giá cao vào.', 'Please pay a high price.', ('curator', '……ええと。', '…Ờ.', '…Um.')),
   ('ok', '見て 感じて ください。', 'Mời mọi người cảm nhận.', 'Please just feel it.', ('curator', 'わかりました。', 'Vâng.', 'Understood.'))])

# ─────────── Thời trang · Hibari Mode (anh Ren) ───────────
GUEST = ('Khách', 'Customer', 'お客様')

S('fashion', 2, None, ('Tìm cỡ áo', 'Finding a size', 'サイズ'),
  {'guest': GUEST},
  [('boss', 'まず、シャツの たたみ方を 覚えよう。', 'Trước hết học cách gấp áo sơ mi.', 'First, learn to fold shirts.')],
  ('guest', 'この シャツ、Mサイズ ありますか。', 'Áo này có size M không?', 'Do you have this shirt in M?'),
  [('ok', 'ないと 思います。', 'Chắc là hết.', "I don't think so.", ('boss', '倉庫に あったよ。確認してから 答えよう。', 'Trong kho có đấy. Kiểm tra rồi hãy trả lời.', 'There was one in stock. Check before answering.')),
   ('best', '確認して まいります。少々 お待ちください。', 'Để tôi kiểm tra. Xin chờ một chút.', "I'll go and check. One moment, please.", ('guest', 'ありました！ありがとう。', 'Có rồi! Cảm ơn.', 'You had it! Thanks.')),
   ('poor', '自分で さがして ください。', 'Tự tìm đi.', 'Look for it yourself.', ('guest', '……もう いいです。', '…Thôi khỏi.', '…Never mind.'))])

S('fashion', 3, None, ('Đồ đi phỏng vấn', 'An interview outfit', '面接の服'),
  {'guest': ('Cô gái trẻ', 'Young woman', '若い女性')},
  [('guest', '面接に 着て いく 服を さがして います。', 'Tôi tìm đồ mặc đi phỏng vấn.', "I'm looking for something to wear to an interview.")],
  ('guest', '何が いいですか。', 'Nên chọn gì ạ?', 'What would be good?'),
  [('best', 'どんな 会社ですか。紺の ジャケットなら、きちんと 見えて 動きやすいですよ。', 'Công ty thế nào ạ? Áo khoác xanh navy trông chỉn chu mà dễ vận động.', 'What kind of company? A navy jacket looks neat and is easy to move in.', ('guest', '自信が 出ました！', 'Tự tin hẳn lên!', 'I feel confident now!')),
   ('poor', '派手な 服が いいですよ。', 'Đồ sặc sỡ đi.', 'Something flashy.', ('guest', '面接には ちょっと……', 'Đi phỏng vấn thì hơi…', "For an interview, that's a bit…")),
   ('ok', 'これが 人気です。', 'Mẫu này đang được ưa chuộng.', 'This one is popular.', ('boss', '人気より、その 人に 合うかが 大事だよ。', 'Hợp người hơn là đang hot.', 'What suits the person matters more than popularity.'))])

S('fashion', 4, None, ('Hàng tồn kho', 'Leftover stock', '在庫'),
  {'guest': GUEST},
  [('boss', '冬の コートが たくさん 残って いる。', 'Còn nhiều áo khoác mùa đông.', 'Lots of winter coats are left.')],
  ('boss', 'アドバイザーとして、どう 売る？', 'Là tư vấn thời trang, em bán thế nào?', 'As fashion advisor, how will you sell them?'),
  [('poor', 'すてます。', 'Vứt đi.', 'Throw them away.', ('boss', 'もったいない！', 'Phí quá!', 'What a waste!')),
   ('ok', '半額に します。', 'Giảm nửa giá.', 'Half price.', ('boss', '売れたけど、もうけは 少ないね。', 'Bán được nhưng lãi ít.', 'They sold, but with little profit.')),
   ('best', 'マフラーと 合わせた コーディネートを、マネキンに 着せて 見せます。', 'Phối với khăn quàng, mặc lên ma-nơ-canh cho khách xem.', 'Style them with scarves on the mannequins.', ('guest', 'この 組み合わせ、そのまま ください！', 'Cho tôi nguyên bộ phối này!', "I'll take that whole outfit!"))])

S('fashion', 5, 'stylist', ('Chụp ảnh tạp chí', 'A magazine shoot', '雑誌の撮影'),
  {'model': ('Người mẫu Aya', 'Model Aya', 'モデルのあや'), 'photographer': ('Nhiếp ảnh gia', 'Photographer', 'カメラマン')},
  [('model', 'この 服、ちょっと 大きいです……', 'Bộ này hơi rộng…', 'This outfit is a bit big…')],
  ('photographer', '時間が ありません！', 'Không còn thời gian!', "We're out of time!"),
  [('best', '後ろを クリップで とめます。前から 見えないので 大丈夫です。', 'Kẹp phía sau. Nhìn trước không thấy nên ổn.', "I'll pin it at the back. It won't show from the front.", ('photographer', '完璧だ。いい 写真が とれた。', 'Hoàn hảo. Có ảnh đẹp rồi.', 'Perfect. Great shot.')),
   ('ok', '別の 服を さがします。', 'Tìm bộ khác.', "I'll find another outfit.", ('photographer', '間に合ったけど、ぎりぎりだったね。', 'Kịp nhưng sát giờ.', 'Made it, but barely.')),
   ('poor', 'そのまま とって ください。', 'Cứ chụp vậy đi.', 'Just shoot it as is.', ('model', 'ぶかぶかに 見えます……', 'Trông thùng thình…', 'It looks baggy…'))])

S('fashion', 5, 'designer', ('Mẫu thiết kế đầu tiên', 'My first design', 'はじめてのデザイン'),
  {'guest': GUEST},
  [('boss', 'あなたの デザインで 一着 作ろう。', 'Làm một bộ theo thiết kế của em nhé.', "Let's make one piece from your design.")],
  ('boss', 'テーマは？', 'Chủ đề là gì?', "What's the theme?"),
  [('ok', '去年 流行った 形に します。', 'Theo kiểu thịnh hành năm ngoái.', "Last year's trendy shape.", ('boss', '安全だね。でも、あなたの 色が 見たい。', 'An toàn đấy, nhưng muốn thấy màu riêng của em.', 'Safe. But I want to see your colour.')),
   ('best', 'アオザイの 線と 着物の 帯を 合わせた ワンピースです。', 'Váy kết hợp đường nét áo dài và đai obi kimono.', 'A dress combining áo dài lines with a kimono obi.', ('guest', '見た ことが ない！ほしい！', 'Chưa thấy bao giờ! Muốn mua!', 'Never seen anything like it! I want it!')),
   ('poor', '有名な ブランドの デザインを まねします。', 'Bắt chước thiết kế hãng nổi tiếng.', "Copy a famous brand's design.", ('boss', 'それは コピーだよ。', 'Thế là sao chép.', "That's a copy."))])

S('fashion', 6, 'stylist', ('Áp phích khu phố', 'The street poster', '商店街のポスター'),
  {'shopkeeper': ('Chủ tiệm trong phố', 'Shop owner', '店主')},
  [('boss', '町の 商店街の ポスターを まかせたい。', 'Anh muốn giao em làm áp phích cho khu phố mua sắm.', "I want you to handle the shopping street's posters.")],
  ('shopkeeper', 'どんな ポスターに しますか。', 'Làm áp phích thế nào?', 'What kind of poster?'),
  [('best', '町の 店の 人たちに、自分の 店の 服を 着て もらって 写真を とります。', 'Mời chủ các tiệm mặc đồ của tiệm mình rồi chụp ảnh.', "Have each shop's people wear their own shop's clothes and photograph them.", ('shopkeeper', '町が 一つに なった みたいだ！', 'Như cả phố hoà làm một!', "It's like the whole street came together!")),
   ('poor', 'うちの 店だけ 大きく のせます。', 'Chỉ in to tiệm mình.', 'Feature only our shop, big.', ('shopkeeper', 'それは ずるい……', 'Thế là ăn gian…', "That's not fair…")),
   ('ok', 'モデルを 呼んで とります。', 'Thuê người mẫu chụp.', 'Hire models.', ('boss', 'きれいだけど、町らしさが 少ないね。', 'Đẹp nhưng thiếu chất phố mình.', 'Pretty, but it lacks local feel.'))])

S('fashion', 6, 'designer', ('Thương hiệu mới', 'A new brand', '新ブランド'),
  {'guest': GUEST},
  [('boss', 'ひばりモードの 新しい ブランドを 作って ほしい。', 'Anh muốn em lập thương hiệu mới cho Hibari Mode.', 'I want you to create a new brand for Hibari Mode.'),
   ('narrator', '工場の 人は、少ない 量で 作るのは 大変だと 言います。', 'Xưởng nói làm số lượng ít thì vất vả.', 'The factory says small runs are hard.')],
  ('boss', 'どんな ブランドに する？', 'Thương hiệu thế nào?', 'What kind of brand?'),
  [('ok', '毎週 新しい 服を たくさん 出します。', 'Mỗi tuần tung nhiều mẫu mới.', 'Lots of new clothes every week.', ('boss', '売れるけど、すぐ すてられる 服も 多いね。', 'Bán chạy nhưng nhiều đồ bị vứt nhanh.', 'They sell, but many get thrown away fast.')),
   ('poor', 'いちばん 安い 布で、たくさん 作ります。', 'Làm thật nhiều bằng vải rẻ nhất.', 'Mass-produce with the cheapest fabric.', ('guest', '一回 洗ったら、のびて しまった……', 'Giặt một lần là giãn…', 'It stretched after one wash…')),
   ('best', '長く 着られる 服を、少しずつ 作ります。古い 服の 修理も 受けます。', 'Làm ít một những bộ mặc được lâu, nhận sửa cả đồ cũ.', "Clothes made to last, in small batches — and we'll repair old ones too.", ('guest', '十年 着たい 服に 出会えた。', 'Gặp được bộ muốn mặc mười năm.', 'I found clothes I want to wear for ten years.'))])
