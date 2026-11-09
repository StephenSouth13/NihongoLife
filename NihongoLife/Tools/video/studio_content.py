"""Lessons of the NihongoLife Studio video course (original lessons made for the game, played inside the game).

Each lesson: id, titles (vi / en / ja), the kind of slide ('kana' for a letter with a word, 'phrase' for a sentence) and
items. A kana item: (letter, romaji, word in kana, kanji or '', meaning vi, meaning en). A phrase item: (Japanese, romaji,
meaning vi, meaning en, note vi). Rendered by studio_lessons.py.
"""

KANA_ROWS = [
    ('ja_hira_a', 'Hiragana hàng あ', 'Hiragana: the あ row', 'ひらがな あ行', [
        ('あ', 'a', 'あめ', '雨', 'mưa', 'rain'), ('い', 'i', 'いぬ', '犬', 'con chó', 'dog'),
        ('う', 'u', 'うみ', '海', 'biển', 'sea'), ('え', 'e', 'えき', '駅', 'nhà ga', 'station'),
        ('お', 'o', 'おちゃ', 'お茶', 'trà', 'tea')]),
    ('ja_hira_ka', 'Hiragana hàng か', 'Hiragana: the か row', 'ひらがな か行', [
        ('か', 'ka', 'かさ', '傘', 'cái ô', 'umbrella'), ('き', 'ki', 'きっぷ', '切符', 'vé tàu', 'ticket'),
        ('く', 'ku', 'くつ', '靴', 'đôi giày', 'shoes'), ('け', 'ke', 'けしゴム', '消しゴム', 'cục tẩy', 'eraser'),
        ('こ', 'ko', 'こめ', '米', 'gạo', 'rice')]),
    ('ja_hira_sa', 'Hiragana hàng さ', 'Hiragana: the さ row', 'ひらがな さ行', [
        ('さ', 'sa', 'さかな', '魚', 'con cá', 'fish'), ('し', 'shi', 'しお', '塩', 'muối', 'salt'),
        ('す', 'su', 'すし', '寿司', 'sushi', 'sushi'), ('せ', 'se', 'せんせい', '先生', 'giáo viên', 'teacher'),
        ('そ', 'so', 'そら', '空', 'bầu trời', 'sky')]),
    ('ja_hira_ta', 'Hiragana hàng た', 'Hiragana: the た row', 'ひらがな た行', [
        ('た', 'ta', 'たまご', '卵', 'quả trứng', 'egg'), ('ち', 'chi', 'ちず', '地図', 'bản đồ', 'map'),
        ('つ', 'tsu', 'つくえ', '机', 'cái bàn', 'desk'), ('て', 'te', 'てがみ', '手紙', 'lá thư', 'letter'),
        ('と', 'to', 'とけい', '時計', 'đồng hồ', 'clock')]),
    ('ja_hira_na', 'Hiragana hàng な', 'Hiragana: the な row', 'ひらがな な行', [
        ('な', 'na', 'なつ', '夏', 'mùa hè', 'summer'), ('に', 'ni', 'にく', '肉', 'thịt', 'meat'),
        ('ぬ', 'nu', 'ぬの', '布', 'vải', 'cloth'), ('ね', 'ne', 'ねこ', '猫', 'con mèo', 'cat'),
        ('の', 'no', 'のみもの', '飲み物', 'đồ uống', 'drink')]),
    ('ja_hira_ha', 'Hiragana hàng は', 'Hiragana: the は row', 'ひらがな は行', [
        ('は', 'ha', 'はな', '花', 'bông hoa', 'flower'), ('ひ', 'hi', 'ひと', '人', 'người', 'person'),
        ('ふ', 'fu', 'ふね', '船', 'con thuyền', 'boat'), ('へ', 'he', 'へや', '部屋', 'căn phòng', 'room'),
        ('ほ', 'ho', 'ほん', '本', 'quyển sách', 'book')]),
    ('ja_hira_ma', 'Hiragana hàng ま', 'Hiragana: the ま row', 'ひらがな ま行', [
        ('ま', 'ma', 'まど', '窓', 'cửa sổ', 'window'), ('み', 'mi', 'みず', '水', 'nước', 'water'),
        ('む', 'mu', 'むし', '虫', 'côn trùng', 'insect'), ('め', 'me', 'めがね', '眼鏡', 'kính mắt', 'glasses'),
        ('も', 'mo', 'もり', '森', 'khu rừng', 'forest')]),
    ('ja_hira_ya', 'Hiragana hàng や, わ và ん', 'Hiragana: や, わ and ん', 'ひらがな や行・わ行・ん', [
        ('や', 'ya', 'やま', '山', 'ngọn núi', 'mountain'), ('ゆ', 'yu', 'ゆき', '雪', 'tuyết', 'snow'),
        ('よ', 'yo', 'よる', '夜', 'buổi tối', 'night'), ('わ', 'wa', 'わたし', '私', 'tôi', 'I, me'),
        ('を', 'o', 'ほんをよむ', '本を読む', 'đọc sách (を chỉ tân ngữ)', 'read a book (を marks the object)'),
        ('ん', 'n', 'みかん', '', 'quả quýt', 'mandarin orange')]),
    ('ja_hira_ra', 'Hiragana hàng ら', 'Hiragana: the ら row', 'ひらがな ら行', [
        ('ら', 'ra', 'らいねん', '来年', 'năm sau', 'next year'), ('り', 'ri', 'りんご', '', 'quả táo', 'apple'),
        ('る', 'ru', 'るす', '留守', 'vắng nhà', 'not at home'), ('れ', 're', 'れいぞうこ', '冷蔵庫', 'tủ lạnh', 'fridge'),
        ('ろ', 'ro', 'ろうか', '廊下', 'hành lang', 'corridor')]),
    ('ja_hira_ga', 'Âm đục: が ざ だ ば ぱ', 'Voiced sounds: が ざ だ ば ぱ', '濁音・半濁音', [
        ('が', 'ga', 'がっこう', '学校', 'trường học', 'school'), ('ざ', 'za', 'ざっし', '雑誌', 'tạp chí', 'magazine'),
        ('で', 'de', 'でんしゃ', '電車', 'tàu điện', 'train'), ('ば', 'ba', 'ばんごはん', '晩ご飯', 'bữa tối', 'dinner'),
        ('ぷ', 'pu', 'てんぷら', '天ぷら', 'món tempura', 'tempura')]),
    ('ja_kata_words', 'Katakana trong đời sống', 'Katakana in daily life', '生活のカタカナ', [
        ('コ', 'ko', 'コンビニ', '', 'cửa hàng tiện lợi', 'convenience store'), ('パ', 'pa', 'パン', '', 'bánh mì', 'bread'),
        ('テ', 'te', 'テレビ', '', 'ti vi', 'TV'), ('ホ', 'ho', 'ホテル', '', 'khách sạn', 'hotel'),
        ('タ', 'ta', 'タクシー', '', 'xe taxi', 'taxi'), ('ー', '—', 'コーヒー', '', 'cà phê (ー kéo dài âm)', 'coffee (ー makes a long vowel)')]),
]

PHRASES = [
    ('ja_greetings', 'Chào hỏi mỗi ngày', 'Everyday greetings', '毎日のあいさつ', [
        ('おはようございます', 'ohayō gozaimasu', 'Chào buổi sáng', 'Good morning', 'Dùng đến khoảng 10 giờ sáng.'),
        ('こんにちは', 'konnichiwa', 'Xin chào (ban ngày)', 'Hello', 'Chữ cuối viết は nhưng đọc là "wa".'),
        ('こんばんは', 'konbanwa', 'Chào buổi tối', 'Good evening', 'Khi trời đã tối.'),
        ('ありがとうございます', 'arigatō gozaimasu', 'Cảm ơn ạ', 'Thank you', 'Lịch sự, dùng với mọi người.'),
        ('すみません', 'sumimasen', 'Xin lỗi / cho tôi hỏi', 'Excuse me / sorry', 'Gọi nhân viên cũng dùng câu này.'),
        ('さようなら', 'sayōnara', 'Tạm biệt', 'Goodbye', 'Bạn bè thường nói じゃあね.')]),
    ('ja_self_intro', 'Tự giới thiệu', 'Introducing yourself', '自己紹介', [
        ('はじめまして', 'hajimemashite', 'Rất vui được gặp', 'Nice to meet you', 'Chỉ nói ở lần gặp đầu tiên.'),
        ('わたしは リンです', 'watashi wa Rin desu', 'Tôi là Linh', 'I am Linh', 'Tên + です. は ở đây đọc là "wa".'),
        ('ベトナムから きました', 'Betonamu kara kimashita', 'Tôi đến từ Việt Nam', 'I come from Vietnam', 'から nghĩa là "từ".'),
        ('がくせいです', 'gakusei desu', 'Tôi là học sinh', 'I am a student', 'Nghề nghiệp + です.'),
        ('よろしく おねがいします', 'yoroshiku onegaishimasu', 'Rất mong được giúp đỡ', 'Please treat me kindly', 'Câu kết luôn có.')]),
    ('ja_numbers', 'Số đếm 1 đến 10', 'Numbers 1 to 10', '数字 1〜10', [
        ('いち・に・さん', 'ichi · ni · san', '1 · 2 · 3', '1 · 2 · 3', ''),
        ('よん（し）・ご', 'yon (shi) · go', '4 · 5', '4 · 5', 'Số 4 thường đọc よん.'),
        ('ろく・なな（しち）', 'roku · nana (shichi)', '6 · 7', '6 · 7', 'Số 7 thường đọc なな.'),
        ('はち・きゅう（く）', 'hachi · kyū (ku)', '8 · 9', '8 · 9', ''),
        ('じゅう', 'jū', '10', '10', 'じゅういち là 11, にじゅう là 20.'),
        ('ひゃくえん', 'hyaku en', '100 yên', '100 yen', 'Giá ở konbini đọc như vậy.')]),
    ('ja_konbini', 'Ở cửa hàng tiện lợi', 'At the konbini', 'コンビニで', [
        ('いらっしゃいませ', 'irasshaimase', 'Kính chào quý khách', 'Welcome', 'Nhân viên nói khi khách vào.'),
        ('ふくろは いりますか', 'fukuro wa irimasu ka', 'Anh/chị có cần túi không?', 'Do you need a bag?', 'Trả lời: はい / いいえ、だいじょうぶです.'),
        ('あたためますか', 'atatamemasu ka', 'Có hâm nóng không ạ?', 'Shall I heat it up?', 'Hỏi khi mua cơm hộp.'),
        ('ポイントカードは ありますか', 'pointo kādo wa arimasu ka', 'Có thẻ tích điểm không?', 'Do you have a point card?', ''),
        ('これを ください', 'kore o kudasai', 'Cho tôi cái này', 'This one, please', 'Chỉ vào món hàng và nói.')]),
    ('ja_station', 'Ở nhà ga', 'At the station', '駅で', [
        ('きっぷうりばは どこですか', 'kippu uriba wa doko desu ka', 'Quầy bán vé ở đâu?', 'Where is the ticket office?', 'どこ nghĩa là "ở đâu".'),
        ('ミドリまで いくらですか', 'Midori made ikura desu ka', 'Đến Midori bao nhiêu tiền?', 'How much to Midori?', 'まで: "đến", いくら: "bao nhiêu".'),
        ('つぎは ミドリです', 'tsugi wa Midori desu', 'Ga tiếp theo là Midori', 'The next stop is Midori', 'Câu thông báo trên tàu.'),
        ('なんばんせんですか', 'nanbansen desu ka', 'Sân ga số mấy?', 'Which platform?', ''),
        ('のりかえ', 'norikae', 'Đổi tàu', 'Change trains', 'Thấy chữ 乗り換え là chỗ đổi tàu.')]),
    ('ja_clinic', 'Ở phòng khám', 'At the clinic', 'クリニックで', [
        ('どうしましたか', 'dō shimashita ka', 'Anh/chị bị sao vậy?', 'What seems to be the problem?', 'Bác sĩ hỏi đầu tiên.'),
        ('ねつが あります', 'netsu ga arimasu', 'Tôi bị sốt', 'I have a fever', ''),
        ('のどが いたいです', 'nodo ga itai desu', 'Tôi bị đau họng', 'My throat hurts', 'いたい: đau.'),
        ('ほけんしょうを おねがいします', 'hokenshō o onegaishimasu', 'Cho xem thẻ bảo hiểm', 'Your insurance card, please', 'Lễ tân nói khi đăng ký khám.'),
        ('おだいじに', 'odaiji ni', 'Mau khỏe nhé', 'Take care', 'Nói với người đang ốm.')]),
]
