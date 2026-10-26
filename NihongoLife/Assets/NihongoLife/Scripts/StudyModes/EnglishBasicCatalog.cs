using System.Collections.Generic;
using System.Linq;

namespace NihongoLife.StudyModes
{
    public enum EnglishQuizType { Meaning, FillIn, Reorder }

    /// <summary>One vocabulary item: English word/phrase with Vietnamese and Japanese meanings.</summary>
    public sealed class EnglishVocab
    {
        public string en, vi, ja;
        public EnglishVocab(string en, string vi, string ja) { this.en = en; this.vi = vi; this.ja = ja; }
    }

    /// <summary>One example sentence in English with Vietnamese and Japanese translations.</summary>
    public sealed class EnglishExample
    {
        public string en, vi, ja;
        public EnglishExample(string en, string vi, string ja) { this.en = en; this.vi = vi; this.ja = ja; }
    }

    /// <summary>
    /// A quiz question with its own answer key (no AI, no network).
    /// Meaning: <see cref="prompt"/> is a question, pick <see cref="choices"/>[<see cref="answerIndex"/>].
    /// FillIn: <see cref="prompt"/> contains "___". Reorder: <see cref="tokens"/> are the words in the correct order.
    /// </summary>
    public sealed class EnglishQuiz
    {
        public const string Blank = "___";
        public EnglishQuizType type;
        public string prompt;
        public string hintVi, hintEn, hintJa;
        public string[] choices;
        public int answerIndex;
        public string[] tokens;

        public static EnglishQuiz Meaning(string prompt, string vi, string en, string ja, int answer, params string[] choices) =>
            new EnglishQuiz { type = EnglishQuizType.Meaning, prompt = prompt, hintVi = vi, hintEn = en, hintJa = ja, choices = choices, answerIndex = answer };
        public static EnglishQuiz Fill(string prompt, string vi, string en, string ja, int answer, params string[] choices) =>
            new EnglishQuiz { type = EnglishQuizType.FillIn, prompt = prompt, hintVi = vi, hintEn = en, hintJa = ja, choices = choices, answerIndex = answer };
        public static EnglishQuiz Order(string vi, string en, string ja, params string[] tokens) =>
            new EnglishQuiz { type = EnglishQuizType.Reorder, prompt = "", hintVi = vi, hintEn = en, hintJa = ja, tokens = tokens };

        /// <summary>The full correct answer (Reorder: words joined by spaces; FillIn: blank replaced).</summary>
        public string CorrectAnswer => type switch
        {
            EnglishQuizType.Reorder => string.Join(" ", tokens),
            EnglishQuizType.FillIn => (prompt ?? "").Replace(Blank, choices[answerIndex]),
            _ => choices[answerIndex],
        };
    }

    public sealed class EnglishBasicLesson
    {
        public string id, icon, titleVi, titleEn, titleJa, goalVi, goalEn, goalJa;
        public List<EnglishVocab> vocab = new List<EnglishVocab>();
        public List<EnglishExample> examples = new List<EnglishExample>();
        public List<EnglishQuiz> quiz = new List<EnglishQuiz>();
    }

    /// <summary>Answer checking and deterministic shuffling for English Basic quizzes.</summary>
    public static class EnglishQuizzes
    {
        public static bool IsCorrect(EnglishQuiz quiz, int choice) =>
            quiz != null && quiz.type != EnglishQuizType.Reorder && quiz.choices != null && choice == quiz.answerIndex;

        public static bool IsCorrectOrder(EnglishQuiz quiz, IReadOnlyList<string> built)
        {
            if (quiz == null || quiz.type != EnglishQuizType.Reorder || built == null || built.Count != quiz.tokens.Length) return false;
            for (int i = 0; i < built.Count; i++) if (built[i] != quiz.tokens[i]) return false;
            return true;
        }

        /// <summary>Tokens shuffled with a fixed seed (same order every run); never the correct order when avoidable.</summary>
        public static string[] Shuffled(EnglishQuiz quiz, string seedKey)
        {
            var arr = (string[])quiz.tokens.Clone();
            if (arr.Length < 2) return arr;
            uint h = 2166136261;
            foreach (char c in seedKey ?? "") h = (h ^ c) * 16777619;
            for (int i = arr.Length - 1; i > 0; i--)
            {
                h = h * 1664525 + 1013904223;
                int j = (int)(h % (uint)(i + 1));
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
            if (arr.SequenceEqual(quiz.tokens)) (arr[0], arr[arr.Length - 1]) = (arr[arr.Length - 1], arr[0]);
            return arr;
        }
    }

    /// <summary>
    /// "English Basic" (Tiếng Anh cơ bản / 基礎英語): a pre-written beginner course for the Study Hub — everyday topics
    /// with vocabulary, example sentences and short quizzes, translated to Vietnamese and Japanese. Separate from the
    /// CEFR "English Foundations" units and IELTS practice in the Exam Center. All content is original and offline.
    /// Based on Codex's English course draft; progress ids start with "eb_" in <see cref="LearningProgressStore"/>.
    /// </summary>
    public static class EnglishBasicCatalog
    {
        private static List<EnglishBasicLesson> _all;
        public static IReadOnlyList<EnglishBasicLesson> All => _all ??= Build();
        public static EnglishBasicLesson Find(string id) => All.FirstOrDefault(l => l.id == id);

        private static EnglishBasicLesson L(string id, string icon, string vi, string en, string ja, string goalVi, string goalEn, string goalJa) =>
            new EnglishBasicLesson { id = id, icon = icon, titleVi = vi, titleEn = en, titleJa = ja, goalVi = goalVi, goalEn = goalEn, goalJa = goalJa };

        private static List<EnglishBasicLesson> Build()
        {
            var list = new List<EnglishBasicLesson>();
            EnglishBasicLesson l;

            // 1 ───────── Greetings ─────────
            l = L("eb_greetings", "emoji_people", "Chào hỏi & giới thiệu", "Greetings & introductions", "あいさつと自己紹介",
                "Chào hỏi, nói tên và hỏi thăm sức khỏe.", "Say hello, give your name and ask how someone is.", "あいさつ、名前を言う、調子をたずねる。");
            l.vocab.Add(new EnglishVocab("Hello / Hi", "Xin chào", "こんにちは"));
            l.vocab.Add(new EnglishVocab("Good morning", "Chào buổi sáng", "おはようございます"));
            l.vocab.Add(new EnglishVocab("Goodbye / Bye", "Tạm biệt", "さようなら"));
            l.vocab.Add(new EnglishVocab("Thank you", "Cảm ơn", "ありがとう"));
            l.vocab.Add(new EnglishVocab("Nice to meet you", "Rất vui được gặp bạn", "はじめまして"));
            l.vocab.Add(new EnglishVocab("name", "tên", "名前"));
            l.examples.Add(new EnglishExample("Hi, my name is Lan.", "Chào, mình tên là Lan.", "こんにちは、私の名前はランです。"));
            l.examples.Add(new EnglishExample("How are you? — I'm fine, thank you.", "Bạn khỏe không? — Mình khỏe, cảm ơn.", "お元気ですか。— 元気です、ありがとう。"));
            l.examples.Add(new EnglishExample("Nice to meet you, Ken.", "Rất vui được gặp bạn, Ken.", "はじめまして、ケンさん。"));
            l.quiz.Add(EnglishQuiz.Meaning("What does \"Thank you\" mean?", "Chọn nghĩa đúng.", "Choose the meaning.", "意味を選ぼう。", 1, "Xin lỗi / すみません", "Cảm ơn / ありがとう", "Tạm biệt / さようなら", "Xin chào / こんにちは"));
            l.quiz.Add(EnglishQuiz.Fill("My ___ is Minh.", "Tên tôi là Minh.", "My name is Minh.", "私の名前はミンです。", 0, "name", "names", "is", "me"));
            l.quiz.Add(EnglishQuiz.Meaning("Someone says \"How are you?\". What do you answer?", "Ai đó hỏi \"How are you?\".", "Someone asks \"How are you?\".", "「How are you?」と聞かれたら？", 2, "My name is Lan.", "Goodbye.", "I'm fine, thank you.", "Good morning."));
            l.quiz.Add(EnglishQuiz.Order("Rất vui được gặp bạn.", "Nice to meet you.", "はじめまして。", "Nice", "to", "meet", "you."));
            list.Add(l);

            // 2 ───────── Numbers ─────────
            l = L("eb_numbers", "grid_view", "Số đếm & tuổi", "Numbers & age", "数字と年齢",
                "Đếm từ 1 đến 20, nói tuổi và số điện thoại.", "Count 1–20, say your age and phone number.", "1〜20を数え、年齢や電話番号を言う。");
            l.vocab.Add(new EnglishVocab("one, two, three", "một, hai, ba", "いち、に、さん"));
            l.vocab.Add(new EnglishVocab("five", "năm (số 5)", "ご（5）"));
            l.vocab.Add(new EnglishVocab("ten", "mười", "じゅう（10）"));
            l.vocab.Add(new EnglishVocab("twelve", "mười hai", "じゅうに（12）"));
            l.vocab.Add(new EnglishVocab("twenty", "hai mươi", "にじゅう（20）"));
            l.vocab.Add(new EnglishVocab("years old", "tuổi", "〜歳"));
            l.examples.Add(new EnglishExample("I am twenty years old.", "Tôi hai mươi tuổi.", "私は二十歳です。"));
            l.examples.Add(new EnglishExample("How old are you?", "Bạn bao nhiêu tuổi?", "何歳ですか。"));
            l.examples.Add(new EnglishExample("My phone number is 090 123 4567.", "Số điện thoại của tôi là 090 123 4567.", "私の電話番号は090 123 4567です。"));
            l.quiz.Add(EnglishQuiz.Meaning("Which word is 12?", "Số 12 là từ nào?", "Which word means 12?", "12はどれ？", 3, "twenty", "two", "eleven", "twelve"));
            l.quiz.Add(EnglishQuiz.Meaning("three + five = ?", "Ba cộng năm bằng mấy?", "Three plus five is…", "3＋5は？", 0, "eight", "seven", "nine", "six"));
            l.quiz.Add(EnglishQuiz.Fill("I am eighteen years ___.", "Tôi mười tám tuổi.", "I am eighteen years old.", "私は十八歳です。", 1, "age", "old", "year", "older"));
            l.quiz.Add(EnglishQuiz.Order("Bạn bao nhiêu tuổi?", "How old are you?", "何歳ですか。", "How", "old", "are", "you?"));
            list.Add(l);

            // 3 ───────── Family ─────────
            l = L("eb_family", "groups", "Gia đình", "Family", "家族",
                "Giới thiệu các thành viên trong gia đình.", "Talk about the people in your family.", "家族を紹介する。");
            l.vocab.Add(new EnglishVocab("mother", "mẹ", "母"));
            l.vocab.Add(new EnglishVocab("father", "bố", "父"));
            l.vocab.Add(new EnglishVocab("brother", "anh/em trai", "兄弟"));
            l.vocab.Add(new EnglishVocab("sister", "chị/em gái", "姉妹"));
            l.vocab.Add(new EnglishVocab("parents", "bố mẹ", "両親"));
            l.vocab.Add(new EnglishVocab("friend", "bạn", "友達"));
            l.examples.Add(new EnglishExample("This is my mother.", "Đây là mẹ tôi.", "こちらは私の母です。"));
            l.examples.Add(new EnglishExample("I have one brother and two sisters.", "Tôi có một anh trai và hai chị gái.", "兄弟が一人と姉妹が二人います。"));
            l.examples.Add(new EnglishExample("My parents live in Hue.", "Bố mẹ tôi sống ở Huế.", "両親はフエに住んでいます。"));
            l.quiz.Add(EnglishQuiz.Meaning("What does \"father\" mean?", "Chọn nghĩa đúng.", "Choose the meaning.", "意味を選ぼう。", 2, "mẹ / 母", "bạn / 友達", "bố / 父", "chị gái / 姉"));
            l.quiz.Add(EnglishQuiz.Fill("This ___ my sister, Mai.", "Đây là em gái tôi, Mai.", "This is my sister, Mai.", "こちらは妹のマイです。", 1, "are", "is", "am", "be"));
            l.quiz.Add(EnglishQuiz.Fill("I ___ two brothers.", "Tôi có hai anh em trai.", "I have two brothers.", "兄弟が二人います。", 0, "have", "has", "am", "is"));
            l.quiz.Add(EnglishQuiz.Order("Đây là bố tôi.", "This is my father.", "こちらは私の父です。", "This", "is", "my", "father."));
            list.Add(l);

            // 4 ───────── Daily life ─────────
            l = L("eb_daily", "wb_sunny", "Sinh hoạt hằng ngày", "Daily life", "毎日の生活",
                "Kể các việc làm mỗi ngày bằng thì hiện tại đơn.", "Describe your routine with the present simple.", "現在形で毎日の習慣を話す。");
            l.vocab.Add(new EnglishVocab("get up", "thức dậy", "起きる"));
            l.vocab.Add(new EnglishVocab("have breakfast", "ăn sáng", "朝ごはんを食べる"));
            l.vocab.Add(new EnglishVocab("go to work", "đi làm", "仕事に行く"));
            l.vocab.Add(new EnglishVocab("go to school", "đi học", "学校に行く"));
            l.vocab.Add(new EnglishVocab("go to bed", "đi ngủ", "寝る"));
            l.vocab.Add(new EnglishVocab("every day", "mỗi ngày", "毎日"));
            l.examples.Add(new EnglishExample("I get up at six o'clock.", "Tôi thức dậy lúc sáu giờ.", "私は六時に起きます。"));
            l.examples.Add(new EnglishExample("She goes to work by bus.", "Cô ấy đi làm bằng xe buýt.", "彼女はバスで仕事に行きます。"));
            l.examples.Add(new EnglishExample("We have dinner at home every day.", "Chúng tôi ăn tối ở nhà mỗi ngày.", "私たちは毎日家で夕食を食べます。"));
            l.quiz.Add(EnglishQuiz.Meaning("What does \"go to bed\" mean?", "Chọn nghĩa đúng.", "Choose the meaning.", "意味を選ぼう。", 3, "thức dậy / 起きる", "đi làm / 仕事に行く", "ăn sáng / 朝ごはんを食べる", "đi ngủ / 寝る"));
            l.quiz.Add(EnglishQuiz.Fill("He ___ up at seven.", "Anh ấy dậy lúc bảy giờ.", "He gets up at seven.", "彼は七時に起きます。", 2, "get", "getting", "gets", "got up"));
            l.quiz.Add(EnglishQuiz.Fill("I go to school ___ bike.", "Tôi đi học bằng xe đạp.", "I go to school by bike.", "私は自転車で学校に行きます。", 0, "by", "on the", "in", "with"));
            l.quiz.Add(EnglishQuiz.Order("Tôi ăn sáng lúc bảy giờ.", "I have breakfast at seven.", "私は七時に朝ごはんを食べます。", "I", "have", "breakfast", "at", "seven."));
            list.Add(l);

            // 5 ───────── Food & drink ─────────
            l = L("eb_food", "restaurant", "Đồ ăn & gọi món", "Food & ordering", "食べ物と注文",
                "Gọi món lịch sự ở quán ăn, quán cà phê.", "Order politely at a café or restaurant.", "カフェやレストランで丁寧に注文する。");
            l.vocab.Add(new EnglishVocab("water", "nước", "水"));
            l.vocab.Add(new EnglishVocab("coffee", "cà phê", "コーヒー"));
            l.vocab.Add(new EnglishVocab("rice", "cơm", "ご飯"));
            l.vocab.Add(new EnglishVocab("menu", "thực đơn", "メニュー"));
            l.vocab.Add(new EnglishVocab("I'd like…", "Tôi muốn (gọi)…", "〜をお願いします"));
            l.vocab.Add(new EnglishVocab("the bill", "hóa đơn", "お会計"));
            l.examples.Add(new EnglishExample("Can I see the menu, please?", "Cho tôi xem thực đơn được không?", "メニューを見せてください。"));
            l.examples.Add(new EnglishExample("I'd like a coffee, please.", "Cho tôi một ly cà phê.", "コーヒーを一つお願いします。"));
            l.examples.Add(new EnglishExample("Can I have the bill, please?", "Cho tôi xin hóa đơn.", "お会計をお願いします。"));
            l.quiz.Add(EnglishQuiz.Meaning("You want to pay. What do you say?", "Bạn muốn thanh toán.", "You want to pay.", "支払いたいとき。", 1, "I'd like a menu.", "Can I have the bill, please?", "I'm hungry.", "Nice to meet you."));
            l.quiz.Add(EnglishQuiz.Fill("I'd ___ some water, please.", "Cho tôi ít nước.", "I'd like some water, please.", "お水をお願いします。", 3, "want", "likes", "love", "like"));
            l.quiz.Add(EnglishQuiz.Meaning("What does \"rice\" mean?", "Chọn nghĩa đúng.", "Choose the meaning.", "意味を選ぼう。", 0, "cơm / ご飯", "nước / 水", "bánh mì / パン", "thịt / 肉"));
            l.quiz.Add(EnglishQuiz.Order("Cho tôi xem thực đơn được không?", "Can I see the menu, please?", "メニューを見せてください。", "Can", "I", "see", "the", "menu,", "please?"));
            list.Add(l);

            // 6 ───────── Shopping ─────────
            l = L("eb_shopping", "shopping_bag", "Mua sắm", "Shopping", "買い物",
                "Hỏi giá, chọn cỡ và thanh toán ở cửa hàng.", "Ask prices, choose sizes and pay in a shop.", "値段を聞き、サイズを選び、支払う。");
            l.vocab.Add(new EnglishVocab("How much…?", "Bao nhiêu tiền…?", "いくらですか"));
            l.vocab.Add(new EnglishVocab("cheap", "rẻ", "安い"));
            l.vocab.Add(new EnglishVocab("expensive", "đắt", "高い"));
            l.vocab.Add(new EnglishVocab("size", "cỡ, kích thước", "サイズ"));
            l.vocab.Add(new EnglishVocab("cash", "tiền mặt", "現金"));
            l.vocab.Add(new EnglishVocab("card", "thẻ", "カード"));
            l.examples.Add(new EnglishExample("How much is this T-shirt?", "Cái áo phông này bao nhiêu tiền?", "このTシャツはいくらですか。"));
            l.examples.Add(new EnglishExample("Do you have a bigger size?", "Bạn có cỡ lớn hơn không?", "もっと大きいサイズはありますか。"));
            l.examples.Add(new EnglishExample("Can I pay by card?", "Tôi trả bằng thẻ được không?", "カードで払えますか。"));
            l.quiz.Add(EnglishQuiz.Meaning("What is the opposite of \"cheap\"?", "Từ trái nghĩa với \"cheap\".", "The opposite of \"cheap\".", "「cheap」の反対は？", 2, "small", "new", "expensive", "big"));
            l.quiz.Add(EnglishQuiz.Fill("How ___ is this bag?", "Cái túi này bao nhiêu tiền?", "How much is this bag?", "このかばんはいくらですか。", 0, "much", "many", "old", "long"));
            l.quiz.Add(EnglishQuiz.Fill("Can I pay ___ cash?", "Tôi trả bằng tiền mặt được không?", "Can I pay in cash?", "現金で払えますか。", 1, "on", "in", "at", "to"));
            l.quiz.Add(EnglishQuiz.Order("Bạn có cỡ nhỏ hơn không?", "Do you have a smaller size?", "もっと小さいサイズはありますか。", "Do", "you", "have", "a", "smaller", "size?"));
            list.Add(l);

            // 7 ───────── Directions ─────────
            l = L("eb_directions", "explore", "Hỏi đường", "Directions", "道をたずねる",
                "Hỏi và chỉ đường đơn giản.", "Ask for and give simple directions.", "簡単な道案内をする・たずねる。");
            l.vocab.Add(new EnglishVocab("Where is…?", "… ở đâu?", "〜はどこですか"));
            l.vocab.Add(new EnglishVocab("turn left", "rẽ trái", "左に曲がる"));
            l.vocab.Add(new EnglishVocab("turn right", "rẽ phải", "右に曲がる"));
            l.vocab.Add(new EnglishVocab("go straight", "đi thẳng", "まっすぐ行く"));
            l.vocab.Add(new EnglishVocab("next to", "bên cạnh", "〜の隣"));
            l.vocab.Add(new EnglishVocab("station", "nhà ga", "駅"));
            l.examples.Add(new EnglishExample("Excuse me, where is the station?", "Xin lỗi, nhà ga ở đâu ạ?", "すみません、駅はどこですか。"));
            l.examples.Add(new EnglishExample("Go straight and turn left.", "Đi thẳng rồi rẽ trái.", "まっすぐ行って、左に曲がってください。"));
            l.examples.Add(new EnglishExample("The bank is next to the post office.", "Ngân hàng ở bên cạnh bưu điện.", "銀行は郵便局の隣です。"));
            l.quiz.Add(EnglishQuiz.Meaning("What does \"turn right\" mean?", "Chọn nghĩa đúng.", "Choose the meaning.", "意味を選ぼう。", 1, "rẽ trái / 左に曲がる", "rẽ phải / 右に曲がる", "đi thẳng / まっすぐ行く", "quay lại / 戻る"));
            l.quiz.Add(EnglishQuiz.Fill("___ is the hospital?", "Bệnh viện ở đâu?", "Where is the hospital?", "病院はどこですか。", 2, "What", "Who", "Where", "When"));
            l.quiz.Add(EnglishQuiz.Fill("The café is next ___ the bank.", "Quán cà phê ở cạnh ngân hàng.", "The café is next to the bank.", "カフェは銀行の隣です。", 0, "to", "at", "of", "on"));
            l.quiz.Add(EnglishQuiz.Order("Đi thẳng rồi rẽ phải.", "Go straight and turn right.", "まっすぐ行って、右に曲がって。", "Go", "straight", "and", "turn", "right."));
            list.Add(l);

            // 8 ───────── Time & days ─────────
            l = L("eb_time", "schedule", "Giờ giấc & ngày trong tuần", "Time & days", "時間と曜日",
                "Hỏi giờ, nói ngày trong tuần và hẹn gặp.", "Ask the time, name the days and make a plan.", "時刻をたずね、曜日を言い、約束する。");
            l.vocab.Add(new EnglishVocab("What time is it?", "Mấy giờ rồi?", "今何時ですか"));
            l.vocab.Add(new EnglishVocab("o'clock", "giờ đúng", "〜時ちょうど"));
            l.vocab.Add(new EnglishVocab("Monday", "thứ Hai", "月曜日"));
            l.vocab.Add(new EnglishVocab("Sunday", "Chủ nhật", "日曜日"));
            l.vocab.Add(new EnglishVocab("today / tomorrow", "hôm nay / ngày mai", "今日／明日"));
            l.vocab.Add(new EnglishVocab("weekend", "cuối tuần", "週末"));
            l.examples.Add(new EnglishExample("It's nine o'clock.", "Bây giờ là chín giờ.", "九時です。"));
            l.examples.Add(new EnglishExample("See you on Monday.", "Hẹn gặp bạn vào thứ Hai.", "月曜日に会いましょう。"));
            l.examples.Add(new EnglishExample("What do you do at the weekend?", "Cuối tuần bạn làm gì?", "週末は何をしますか。"));
            l.quiz.Add(EnglishQuiz.Meaning("Which day comes after Monday?", "Ngày nào sau thứ Hai?", "Which day is after Monday?", "月曜日の次は？", 3, "Sunday", "Friday", "Saturday", "Tuesday"));
            l.quiz.Add(EnglishQuiz.Fill("It's ten ___.", "Bây giờ là mười giờ.", "It's ten o'clock.", "十時です。", 1, "hours", "o'clock", "time", "clock"));
            l.quiz.Add(EnglishQuiz.Fill("See you ___ Friday.", "Hẹn gặp bạn vào thứ Sáu.", "See you on Friday.", "金曜日に会いましょう。", 2, "in", "at", "on", "to"));
            l.quiz.Add(EnglishQuiz.Order("Mấy giờ rồi?", "What time is it?", "今何時ですか。", "What", "time", "is", "it?"));
            list.Add(l);

            return list;
        }
    }
}
