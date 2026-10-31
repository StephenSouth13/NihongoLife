# -*- coding: utf-8 -*-
# Cutscene content for the story (Tools/story/gen.py): the choice each chapter / career stage ends on, what each answer
# changes (story flag + affinity with the speaker), and the chapter / stage ending lines. Lines: (ja, vi, en, ru);
# ending lines start with their speaker id; a line may carry a 5th item, a StoryFlags condition: it is shown only when the condition holds (consequences of
# earlier choices). Pre-written, no runtime AI.

# Option: (id, choice(ja, vi, en, ru), reply line, affinity delta)
CHAPTER_SCENES = {
 "arrival": dict(
  prompt=("ひばり町で、まず何がしたい？", "Ở Hibari-chō, cháu muốn làm gì trước?", "What would you like to do first in Hibari-chō?", "Что вы хотите сделать в Хибари-тё первым делом?"),
  options=[
   ("study", ("日本語をしっかり勉強したいです。", "Cháu muốn học tiếng Nhật thật chăm chỉ.", "I want to study Japanese properly.", "Я хочу как следует учить японский."),
    ("えらいね。森田先生はきびしいけど、やさしいよ。", "Giỏi lắm. Cô Morita nghiêm nhưng hiền lắm.", "Good for you. Ms Morita is strict but kind.", "Молодец. Морита-сэнсэй строгая, но добрая."), 5),
   ("friends", ("町の人と友だちになりたいです。", "Cháu muốn kết bạn với mọi người trong phố.", "I want to make friends with the people here.", "Я хочу подружиться с местными."),
    ("それなら、まず商店街でおいしいものを食べよう！", "Vậy thì trước hết đi ăn món ngon ở khu phố chợ nhé!", "Then let's start by eating something good on the shopping street!", "Тогда начнём с чего-нибудь вкусного на торговой улице!"), 8),
   ("work", ("アルバイトをさがしたいです。", "Cháu muốn tìm việc làm thêm.", "I want to find a part-time job.", "Я хочу найти подработку."),
    ("コンビニのいとうさんが人をさがしているよ。", "Chị Ito ở konbini đang tìm người đấy.", "Ms Ito at the konbini is looking for help.", "Ито из комбини как раз ищет помощника."), 4)],
  ending=[("narrator", "ひばり町での毎日が、ここから始まる。", "Những ngày ở Hibari-chō bắt đầu từ đây.", "Your days in Hibari-chō begin here.", "Здесь начинаются ваши дни в Хибари-тё.")]),
 "first_class": dict(
  prompt=("自己紹介、どうしますか？", "Em sẽ tự giới thiệu thế nào?", "How will you introduce yourself?", "Как вы представитесь?"),
  options=[
   ("polite", ("はじめまして。どうぞよろしくお願いします。", "Rất vui được gặp mọi người. Mong được giúp đỡ.", "Nice to meet you all. I look forward to working with you.", "Приятно познакомиться. Прошу любить и жаловать."),
    ("ていねいでいいですね。", "Lịch sự lắm, tốt quá.", "Very polite, well done.", "Очень вежливо, хорошо."), 6),
   ("hobby", ("料理が好きです。ベトナム料理を作りたいです。", "Em thích nấu ăn. Em muốn nấu món Việt cho mọi người.", "I like cooking. I want to cook Vietnamese food for everyone.", "Я люблю готовить. Хочу приготовить всем вьетнамскую еду."),
    ("キムさんも料理が好きですよ。あとで話してみて。", "Kim cũng thích nấu ăn đấy. Lát nữa nói chuyện thử nhé.", "Kim likes cooking too. Talk to her later.", "Ким тоже любит готовить. Поговорите потом."), 5),
   ("nervous", ("すみません、日本語がまだ下手です……", "Xin lỗi, tiếng Nhật của em còn kém lắm…", "Sorry, my Japanese is still poor…", "Простите, мой японский пока слабый…"),
    ("大丈夫。みんな同じですよ。いっしょにがんばりましょう。", "Không sao. Ai cũng vậy cả. Cùng cố gắng nhé.", "It's fine. Everyone is the same. Let's do our best together.", "Ничего страшного. Все такие же. Будем стараться вместе."), 8)],
  ending=[("narrator", "キムさんが手をふっている。", "Kim đang vẫy tay với bạn.", "Kim is waving at you.", "Ким машет вам рукой."),
          ("npc_classmate_kim", "料理の話、あとで聞かせてね！", "Kể cho tớ nghe chuyện nấu ăn nhé!", "Tell me about your cooking later!", "Расскажи потом про свою кухню!", "story_choice_first_class_hobby")]),
 "neighbours": dict(
  prompt=("大将がおすすめを聞いている。", "Bác Aoki hỏi bạn muốn ăn gì.", "Aoki asks what you would like.", "Аоки спрашивает, что вы будете."),
  options=[
   ("omakase", ("おまかせでお願いします。", "Cho cháu suất omakase ạ.", "Omakase, please.", "Омакасэ, пожалуйста."),
    ("いい度胸だ！今日のマグロは最高だよ。", "Gan lắm! Cá ngừ hôm nay ngon nhất đấy.", "Brave! Today's tuna is the best.", "Смело! Тунец сегодня лучший."), 8),
   ("ask", ("この魚は何ですか？", "Cá này là cá gì ạ?", "What fish is this?", "Что это за рыба?"),
    ("これはアジ。質問する人、好きだよ。", "Đây là cá aji. Bác thích người hay hỏi.", "That's horse mackerel. I like people who ask.", "Это ставрида. Люблю любознательных."), 6),
   ("share", ("田中さんといっしょに食べたいです。", "Cháu muốn ăn cùng chú Tanaka.", "I'd like to eat with Mr Tanaka.", "Я хочу поесть вместе с Танакой."),
    ("いいね、近所づきあいは大事だ。", "Hay đấy, tình hàng xóm quan trọng lắm.", "Nice. Neighbours matter.", "Хорошо. Соседи — это важно."), 5)],
  ending=[("narrator", "のれんの向こうで、町のみんなが笑っている。", "Sau tấm rèm noren, mọi người trong phố đang cười.", "Behind the noren, the neighbourhood is laughing.", "За занавеской норэн смеётся вся округа.")]),
 "festival": dict(
  prompt=("お祭りで何を手伝う？", "Ở lễ hội cháu sẽ giúp gì?", "How will you help at the festival?", "Чем вы поможете на празднике?"),
  options=[
   ("dance", ("盆踊りをおどります！", "Cháu sẽ nhảy Bon Odori!", "I'll dance the Bon Odori!", "Я станцую бон-одори!"),
    ("いいぞ、輪に入りなさい！", "Hay lắm, vào vòng nhảy đi!", "Great, join the circle!", "Отлично, вставай в круг!"), 6),
   ("stall", ("屋台を手伝います。", "Cháu sẽ phụ quầy hàng.", "I'll help at a stall.", "Я помогу у прилавка."),
    ("たこ焼きの屋台が大変なんだ。助かるよ。", "Quầy takoyaki đang bận lắm. Có cháu đỡ quá.", "The takoyaki stall is swamped. That helps.", "У лотка такояки завал. Спасибо!"), 8),
   ("clean", ("あとかたづけをします。", "Cháu sẽ dọn dẹp sau lễ hội.", "I'll clean up afterwards.", "Я уберу после праздника."),
    ("それが一番ありがたい。町の人はちゃんと見ているよ。", "Đó là việc quý nhất. Người trong phố đều thấy cả.", "That's what we value most. People notice.", "Это ценнее всего. Люди это видят."), 10)],
  ending=[("narrator", "花火が夜空に広がった。", "Pháo hoa nở rộ trên bầu trời đêm.", "Fireworks open across the night sky.", "Фейерверк раскрывается в ночном небе."),
          ("npc_classmate_kim", "きれいだね。来年もいっしょに見よう。", "Đẹp quá. Năm sau lại cùng xem nhé.", "Beautiful. Let's watch together next year too.", "Красиво. Давай смотреть вместе и в следующем году.", "story_bond_npc_classmate_kim")]),
 "career_choice": dict(
  prompt=("どんな仕事で生きていきたい？", "Cháu muốn sống bằng nghề gì?", "What kind of work do you want to live by?", "Какой работой вы хотите жить?"),
  options=[
   ("people", ("人の役に立つ仕事がしたいです。", "Cháu muốn làm việc giúp ích cho người khác.", "I want work that helps people.", "Хочу работу, которая помогает людям."),
    ("クリニックか、お店の仕事が向いているね。", "Phòng khám hoặc cửa hàng hợp với cháu đấy.", "The clinic or a shop would suit you.", "Тебе подойдёт клиника или магазин."), 6),
   ("craft", ("料理や音楽、ものづくりがしたいです。", "Cháu muốn nấu ăn, làm nhạc, tự tay làm ra thứ gì đó.", "I want to cook, make music, create things.", "Хочу готовить, играть музыку, создавать."),
    ("職人の道はきびしいけど、楽しいよ。", "Con đường nghệ nhân vất vả nhưng vui lắm.", "The craftsman's road is hard but fun.", "Путь мастера труден, но интересен."), 6),
   ("bridge", ("日本とベトナムをつなぐ仕事がしたいです。", "Cháu muốn làm việc kết nối Nhật Bản và Việt Nam.", "I want work that links Japan and Vietnam.", "Хочу работу, связывающую Японию и Вьетнам."),
    ("それなら、フーコックのマイさんに会うといい。", "Vậy thì nên gặp chị Mai ở Phú Quốc.", "Then you should meet Mai on Phú Quốc.", "Тогда познакомься с Май на Фукуоке."), 8)],
  ending=[("npc_neighbor_1", "ジャーナルの「仕事」で、道を選べる。", "Bạn có thể chọn con đường trong mục Nghề nghiệp.", "You can pick your path under Careers.", "Свой путь можно выбрать в разделе «Карьера».")]),
 "phu_quoc": dict(
  prompt=("ガンゴンで最初に何をする？", "Ở Gánh Ngon em làm gì trước?", "What will you do first at Gánh Ngon?", "Что вы сделаете первым в «Ганьнгон»?"),
  options=[
   ("menu", ("メニューを日本語に訳します。", "Em sẽ dịch thực đơn sang tiếng Nhật.", "I'll translate the menu into Japanese.", "Я переведу меню на японский."),
    ("ありがとう！日本のお客さまが喜ぶわ。", "Cảm ơn em! Khách Nhật sẽ vui lắm.", "Thank you! Japanese guests will love it.", "Спасибо! Японские гости будут рады."), 10),
   ("kitchen", ("厨房を手伝います。", "Em sẽ phụ bếp.", "I'll help in the kitchen.", "Я помогу на кухне."),
    ("じゃあ、まずは魚のさばき方から。", "Vậy bắt đầu từ cách làm cá nhé.", "Then let's start with filleting fish.", "Тогда начнём с разделки рыбы."), 6),
   ("beach", ("まず島を見て回ります。", "Em đi một vòng đảo trước đã.", "I'll look around the island first.", "Сначала осмотрю остров."),
    ("夕日の時間までには戻ってね。", "Về trước lúc hoàng hôn nhé.", "Be back by sunset.", "Вернись к закату."), 3)],
  ending=[("narrator", "潮風の中、新しい仕事場が待っている。", "Trong gió biển, chỗ làm mới đang chờ bạn.", "In the sea breeze, a new workplace awaits.", "На морском ветру вас ждёт новое место работы."),
          ("npc_ganhngon_manager", "日本語のメニュー、もう評判よ。", "Thực đơn tiếng Nhật được khen rồi đấy.", "The Japanese menu is already a hit.", "Японское меню уже хвалят.", "story_choice_phu_quoc_menu")]),
 "n3_exam": dict(
  prompt=("試験の前の日、どうする？", "Ngày trước kỳ thi em sẽ làm gì?", "What will you do the day before the exam?", "Что вы сделаете накануне экзамена?"),
  options=[
   ("review", ("もう一度、文法を復習します。", "Em ôn lại ngữ pháp một lần nữa.", "Review grammar once more.", "Ещё раз повторю грамматику."),
    ("まじめですね。でも、早く寝てください。", "Chăm quá. Nhưng nhớ ngủ sớm nhé.", "Diligent. But do go to bed early.", "Старательно. Но ложитесь пораньше."), 5),
   ("group", ("キムさんたちと勉強会をします。", "Em học nhóm với Kim và các bạn.", "Hold a study group with Kim and the others.", "Устрою учебную группу с Ким и другими."),
    ("教え合うのは、一番いい勉強です。", "Dạy nhau là cách học tốt nhất.", "Teaching each other is the best study.", "Учить друг друга — лучшая учёба."), 7),
   ("rest", ("ゆっくり休みます。", "Em sẽ nghỉ ngơi thật tốt.", "Rest well.", "Хорошо отдохну."),
    ("それも大事な準備ですよ。", "Đó cũng là chuẩn bị quan trọng đấy.", "That's important preparation too.", "Это тоже важная подготовка."), 4)],
  ending=[("narrator", "合格発表の日、名前を見つけた。", "Ngày công bố kết quả, bạn tìm thấy tên mình.", "On results day, you find your name.", "В день результатов вы находите своё имя."),
          ("narrator", "キムさんも合格！みんなで喜んだ。", "Kim cũng đỗ! Cả nhóm cùng vui mừng.", "Kim passed too! Everyone celebrates.", "Ким тоже сдала! Все радуются.", "story_choice_n3_exam_group")]),
 "mentor": dict(
  prompt=("新しい留学生に、何を伝えたい？", "Em muốn nói gì với du học sinh mới?", "What do you want to tell the new students?", "Что вы хотите сказать новым студентам?"),
  options=[
   ("mistakes", ("まちがえても大丈夫、と伝えます。", "Sai cũng không sao.", "That mistakes are fine.", "Что ошибаться не страшно."),
    ("それは、私が最初にあなたに言った言葉ですね。", "Đó là câu cô nói với em ngày đầu tiên nhỉ.", "Those are the words I said to you on day one.", "Это мои слова вам в первый день."), 10),
   ("town", ("町の人と話すこと。", "Hãy trò chuyện với người trong phố.", "To talk with the townspeople.", "Чтобы говорили с горожанами."),
    ("ひばり町が、あなたを育てましたね。", "Hibari-chō đã nuôi lớn em rồi.", "Hibari-chō raised you.", "Хибари-тё вас вырастил."), 6),
   ("work", ("仕事で日本語は伸びる、と伝えます。", "Đi làm sẽ giỏi tiếng Nhật lên.", "That work makes your Japanese grow.", "Что работа развивает японский."),
    ("経験から出た言葉は強いです。", "Lời từ kinh nghiệm thì có sức nặng.", "Words from experience are strong.", "Слова из опыта сильны."), 6)],
  ending=[("narrator", "教室に、新しい声がひびいている。", "Lớp học vang lên những giọng nói mới.", "New voices ring through the classroom.", "Класс наполняют новые голоса.")]),
 "homecoming": dict(
  prompt=("これから、どこで生きていく？", "Từ giờ, cháu sẽ sống ở đâu?", "Where will you live from now on?", "Где вы будете жить дальше?"),
  options=[
   ("japan", ("ひばり町に残ります。", "Cháu ở lại Hibari-chō.", "I'll stay in Hibari-chō.", "Я останусь в Хибари-тё."),
    ("うれしいなあ。これからも、となり同士だ。", "Vui quá. Vẫn là hàng xóm nhé.", "I'm glad. We stay neighbours.", "Как я рад. Остаёмся соседями."), 10),
   ("vietnam", ("ベトナムに帰って、日本とつなぎます。", "Cháu về Việt Nam và làm cầu nối với Nhật.", "I'll go home to Vietnam and be a bridge to Japan.", "Вернусь во Вьетнам и буду мостом с Японией."),
    ("さびしいけど、誇りに思うよ。", "Buồn nhưng chú tự hào lắm.", "I'll miss you, but I'm proud.", "Грустно, но я горжусь тобой."), 8),
   ("both", ("二つの国を行き来します。", "Cháu sẽ đi lại giữa hai nước.", "I'll live between both countries.", "Буду жить между двумя странами."),
    ("ふるさとが二つ。それがあなたらしい。", "Hai quê hương. Đúng là cháu.", "Two hometowns. That's so you.", "Две родины — это так на тебя похоже."), 8)],
  ending=[("narrator", "森田先生からの手紙：あなたは私の自慢の生徒です。", "Thư của cô Morita: Em là học trò cô tự hào nhất.", "A letter from Ms Morita: you are the student I'm proudest of.", "Письмо от Мориты-сэнсэй: вы — моя гордость."),
          ("npc_classmate_kim", "キムさん：また一緒に料理しようね。", "Kim: Lại nấu ăn cùng nhau nhé.", "Kim: Let's cook together again.", "Ким: Давай снова готовить вместе.", "story_bond_npc_classmate_kim"),
          ("npc_ganhngon_manager", "マイさん：ガンゴンはいつでもあなたの家よ。", "Chị Mai: Gánh Ngon luôn là nhà của em.", "Mai: Gánh Ngon is always your home.", "Май: «Ганьнгон» — всегда твой дом.", "story_bond_npc_ganhngon_manager"),
          ("npc_sushi_staff", "大将：のれんはいつでも開いてるぞ。", "Bác Aoki: Rèm noren lúc nào cũng mở chờ cháu.", "Aoki: The noren is always open for you.", "Аоки: Норэн для тебя всегда открыт.", "story_bond_npc_sushi_staff"),
          ("narrator", "ひばり町の物語は終わらない。新しい一日が始まる。", "Câu chuyện Hibari-chō không kết thúc. Một ngày mới bắt đầu.", "The story of Hibari-chō does not end. A new day begins.", "История Хибари-тё не заканчивается. Начинается новый день.")]),
}

# Career stages: one choice per tier shared by all careers (the boss is the arc's speaker); the answer shapes the
# character's working style, remembered as story_<career>_style_<id>; the owner stage's ending calls it back.
TIER_SCENES = {
 "trainee": dict(prompt=("最初の一週間、どうだった？", "Tuần đầu tiên thế nào?", "How was your first week?", "Как прошла первая неделя?"),
  options=[("careful", ("ミスをしないよう、メモを取りました。", "Em ghi chép để không mắc lỗi.", "I took notes so I wouldn't make mistakes.", "Я делал заметки, чтобы не ошибаться."), ("そのメモは宝物になるよ。", "Cuốn sổ đó sẽ là báu vật đấy.", "Those notes will become a treasure.", "Эти заметки станут сокровищем."), 6),
           ("cheerful", ("お客さまと話すのが楽しかったです。", "Nói chuyện với khách vui lắm ạ.", "I enjoyed talking with customers.", "Мне понравилось говорить с клиентами."), ("その笑顔がいちばんの戦力だ。", "Nụ cười đó là vũ khí mạnh nhất.", "That smile is your best asset.", "Эта улыбка — твоё главное оружие."), 6),
           ("tired", ("正直、つかれました。", "Thật lòng là em mệt ạ.", "Honestly, I'm tired.", "Честно говоря, я устал."), ("正直でいい。休むのも仕事だよ。", "Thật thà là tốt. Nghỉ ngơi cũng là công việc.", "Honesty is good. Resting is part of the job.", "Честность — это хорошо. Отдых — тоже работа."), 4)],
  ending=("見習いの名札に、小さな星がついた。", "Bảng tên thực tập sinh được gắn một ngôi sao nhỏ.", "A small star is added to your trainee badge.", "На бейдж стажёра добавилась маленькая звёздочка.")),
 "staff": dict(prompt=("お客さまからクレームが来た。どうする？", "Có khách phàn nàn. Em làm gì?", "A customer complains. What do you do?", "Клиент жалуется. Что вы сделаете?"),
  options=[("apologize", ("まず心からおわびします。", "Trước hết xin lỗi thật lòng.", "Apologise sincerely first.", "Сначала искренне извинюсь."), ("それが基本だ。よく見ていたね。", "Đó là điều cơ bản. Em quan sát giỏi đấy.", "That's the basics. You've been watching.", "Это основа. Ты внимательно учился."), 6),
           ("listen", ("最後まで話を聞きます。", "Lắng nghe đến hết.", "Listen to the end.", "Выслушаю до конца."), ("聞く力は、日本語の力でもある。", "Biết lắng nghe cũng là giỏi tiếng Nhật.", "Listening is a language skill too.", "Умение слушать — тоже языковой навык."), 7),
           ("boss", ("すぐに先輩を呼びます。", "Gọi ngay tiền bối.", "Call a senior right away.", "Сразу позову старшего."), ("相談するのは正しい判断だ。", "Hỏi ý kiến là quyết định đúng.", "Asking for help was the right call.", "Попросить помощи — верное решение."), 5)],
  ending=("「ありがとう、また来ます」とお客さまが言った。", "Khách nói: 'Cảm ơn, tôi sẽ quay lại'.", "The customer says: thank you, I'll come again.", "Клиент говорит: «Спасибо, приду ещё».")),
 "senior": dict(prompt=("新人が泣きそうになっている。", "Người mới sắp khóc rồi.", "The new trainee is close to tears.", "Новичок вот-вот расплачется."),
  options=[("teach", ("いっしょにやり方を見直します。", "Cùng xem lại cách làm.", "Go over the steps together.", "Вместе разберу порядок работы."), ("昔のあなたを見ているようだ。", "Như thấy lại em ngày trước vậy.", "It's like watching you back then.", "Будто вижу тебя в начале."), 8),
           ("story", ("自分の失敗の話をします。", "Kể chuyện thất bại của mình.", "Tell them about my own mistakes.", "Расскажу о своих ошибках."), ("失敗を話せる人は強い。", "Người dám kể thất bại là người mạnh.", "People who share failures are strong.", "Кто говорит о неудачах — тот силён."), 7),
           ("cover", ("今日は私が代わります。", "Hôm nay để em làm thay.", "I'll cover for them today.", "Сегодня подменю его."), ("やさしいね。でも次は教えてあげて。", "Em tốt bụng. Nhưng lần sau hãy dạy họ nhé.", "Kind. But next time, teach them.", "Добрый поступок. Но в следующий раз научи."), 4)],
  ending=("新人が「先輩」と呼んでくれた。", "Người mới gọi bạn là 'senpai'.", "The trainee calls you senpai.", "Новичок называет вас «сэмпай».")),
 "manager": dict(prompt=("売り上げが落ちた。何を変える？", "Doanh thu giảm. Em thay đổi gì?", "Sales are down. What will you change?", "Продажи упали. Что вы измените?"),
  options=[("team", ("スタッフの意見を聞きます。", "Lắng nghe ý kiến nhân viên.", "Ask the staff for ideas.", "Спрошу мнение сотрудников."), ("チームはあなたを信じている。", "Cả đội tin em.", "The team trusts you.", "Команда тебе доверяет."), 8),
           ("new", ("新しいメニューやサービスを作ります。", "Làm thực đơn, dịch vụ mới.", "Create a new menu or service.", "Создам новое меню или услугу."), ("思い切りがいい。やってみなさい。", "Quyết đoán đấy. Cứ thử đi.", "Bold. Go for it.", "Смело. Попробуй."), 6),
           ("guests", ("常連さんに話を聞きに行きます。", "Đi hỏi chuyện khách quen.", "Go and ask the regulars.", "Поговорю с постоянными клиентами."), ("答えはいつもお客さまの中にある。", "Câu trả lời luôn ở nơi khách hàng.", "The answer is always with the customers.", "Ответ всегда у клиентов."), 7)],
  ending=("三か月後、売り上げは前より上がった。", "Ba tháng sau, doanh thu vượt cả trước kia.", "Three months later, sales are higher than ever.", "Через три месяца продажи выше прежнего.")),
 "owner": dict(prompt=("この仕事で、何を残したい？", "Em muốn để lại gì qua công việc này?", "What do you want to leave behind through this work?", "Что вы хотите оставить после себя в этой работе?"),
  options=[("people", ("人を育てたいです。", "Em muốn đào tạo con người.", "I want to raise the next people.", "Хочу растить людей."), ("あなたが一番の財産を選んだ。", "Em đã chọn tài sản quý nhất.", "You chose the greatest treasure.", "Ты выбрал главное сокровище."), 10),
           ("quality", ("最高の仕事を続けたいです。", "Em muốn giữ chất lượng tốt nhất.", "I want to keep doing the very best work.", "Хочу продолжать делать лучшую работу."), ("その心があれば、看板は守れる。", "Có tấm lòng đó, bảng hiệu sẽ được giữ.", "With that heart, the name will last.", "С таким сердцем имя будет жить."), 8),
           ("bridge", ("日本とベトナムをつなぎたいです。", "Em muốn nối Nhật Bản và Việt Nam.", "I want to link Japan and Vietnam.", "Хочу связать Японию и Вьетнам."), ("あなたにしかできないことだ。", "Đó là việc chỉ em làm được.", "Only you can do that.", "Это можешь только ты."), 8)],
  ending=("あの日の見習いが、今は看板を背負っている。", "Cậu thực tập sinh ngày ấy, giờ gánh cả bảng hiệu.", "The trainee of that first day now carries the whole business.", "Стажёр того первого дня теперь несёт на себе всё дело.")),
}
# Owner-stage callbacks to the trainee answer (character development across the whole arc).
OWNER_CALLBACK = {
 "careful": ("見習いのころのメモ、まだ持っているね。", "Cuốn sổ thời thực tập, em vẫn giữ nhỉ.", "You still keep your trainee notebook.", "Ты всё ещё хранишь тетрадь стажёра."),
 "cheerful": ("あの笑顔は、今もお店の顔だ。", "Nụ cười ấy vẫn là bộ mặt của cửa hàng.", "That smile is still the face of the place.", "Эта улыбка по-прежнему лицо заведения."),
 "tired": ("つかれたと言えた日から、ずいぶん強くなった。", "Từ ngày dám nói 'em mệt', em đã mạnh mẽ hơn nhiều.", "Since the day you admitted you were tired, you've grown strong.", "С того дня, как ты признался в усталости, ты стал сильным."),
}
# Relationship bonds: affinity (SocialStanding) at or above the threshold sets story_bond_<speaker>.
BOND_THRESHOLD = 20
