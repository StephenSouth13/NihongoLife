using System.Collections.Generic;
using System.Linq;

namespace NihongoLife.StudyModes
{
    public enum PatternDrillType { FillIn, Choose, Reorder }

    /// <summary>One example sentence: Japanese, kana reading (furigana line), romaji and vi/en meaning.</summary>
    public sealed class PatternExample
    {
        public string ja, kana, romaji, vi, en;
        public PatternExample(string ja, string kana, string romaji, string vi, string en)
        { this.ja = ja; this.kana = kana; this.romaji = romaji; this.vi = vi; this.en = en; }
    }

    /// <summary>
    /// A drill with its answer key. FillIn/Choose: pick <see cref="choices"/>[<see cref="answerIndex"/>].
    /// Reorder: <see cref="tokens"/> is the correct order; the UI shows them shuffled with a fixed seed.
    /// </summary>
    public sealed class PatternDrill
    {
        public PatternDrillType type;
        public string promptJa;        // FillIn: contains "＿＿"; Choose: question; Reorder: meaning to build
        public string hintVi, hintEn;  // meaning of the target sentence
        public string[] choices;
        public int answerIndex;
        public string[] tokens;

        public static PatternDrill Fill(string promptJa, string vi, string en, int answer, params string[] choices) =>
            new PatternDrill { type = PatternDrillType.FillIn, promptJa = promptJa, hintVi = vi, hintEn = en, choices = choices, answerIndex = answer };
        public static PatternDrill Choose(string promptJa, string vi, string en, int answer, params string[] choices) =>
            new PatternDrill { type = PatternDrillType.Choose, promptJa = promptJa, hintVi = vi, hintEn = en, choices = choices, answerIndex = answer };
        public static PatternDrill Order(string vi, string en, params string[] tokens) =>
            new PatternDrill { type = PatternDrillType.Reorder, promptJa = "", hintVi = vi, hintEn = en, tokens = tokens };

        public string CorrectSentence => type == PatternDrillType.Reorder ? string.Concat(tokens) :
            (promptJa ?? "").Replace("＿＿", choices[answerIndex]);
    }

    public sealed class SentencePattern
    {
        public string id, level, pattern, structure, meaningVi, meaningEn, meaningJa, noteVi, noteEn, noteJa;
        public List<PatternExample> examples = new List<PatternExample>();
        public List<PatternDrill> drills = new List<PatternDrill>();
    }

    /// <summary>
    /// Deterministic 文型 (sentence-pattern) course N5 → N3, independent from the JLPT mock exams.
    /// Every drill carries its own answer key, so no AI or network is needed.
    /// </summary>
    public static class JapanesePatternCatalog
    {
        public static readonly string[] Levels = { "N5", "N4", "N3" };
        private static List<SentencePattern> _all;

        public static IReadOnlyList<SentencePattern> All => _all ??= Build();
        public static IEnumerable<SentencePattern> ForLevel(string level) => All.Where(p => p.level == level);
        public static SentencePattern Find(string id) => All.FirstOrDefault(p => p.id == id);

        private static SentencePattern P(string id, string level, string pattern, string structure,
            string vi, string en, string ja, string noteVi, string noteEn, string noteJa) =>
            new SentencePattern { id = id, level = level, pattern = pattern, structure = structure, meaningVi = vi, meaningEn = en, meaningJa = ja, noteVi = noteVi, noteEn = noteEn, noteJa = noteJa };

        private static List<SentencePattern> Build()
        {
            var list = new List<SentencePattern>();
            SentencePattern p;

            // ───────── N5 ─────────
            p = P("n5_wa_desu", "N5", "〜は〜です", "N1 は N2 です", "N1 là N2", "N1 is N2", "N1はN2です（説明・紹介）",
                "Câu giới thiệu cơ bản. Phủ định: じゃありません.", "Basic statement. Negative: じゃありません.", "基本の名詞文。否定は「じゃありません」。");
            p.examples.Add(new PatternExample("わたしはがくせいです。", "わたしはがくせいです。", "watashi wa gakusei desu.", "Tôi là học sinh.", "I am a student."));
            p.examples.Add(new PatternExample("田中さんは先生です。", "たなかさんはせんせいです。", "Tanaka-san wa sensei desu.", "Anh Tanaka là giáo viên.", "Mr Tanaka is a teacher."));
            p.drills.Add(PatternDrill.Fill("わたし＿＿ベトナム人です。", "Tôi là người Việt Nam.", "I am Vietnamese.", 0, "は", "を", "に", "で"));
            p.drills.Add(PatternDrill.Choose("「He is not a doctor.」は？", "Anh ấy không phải bác sĩ.", "He is not a doctor.", 2, "かれはいしゃです。", "かれはいしゃでした。", "かれはいしゃじゃありません。", "かれをいしゃです。"));
            p.drills.Add(PatternDrill.Order("Đây là cuốn sách.", "This is a book.", "これは", "ほん", "です。"));
            list.Add(p);

            p = P("n5_masu", "N5", "〜ます／〜ません", "V-ます / V-ません", "làm / không làm (lịch sự)", "do / don't (polite)", "丁寧な動詞の肯定・否定",
                "Thể lịch sự của động từ ở hiện tại/tương lai.", "Polite non-past verb form.", "丁寧体の非過去形。");
            p.examples.Add(new PatternExample("毎朝コーヒーを飲みます。", "まいあさコーヒーをのみます。", "maiasa koohii o nomimasu.", "Mỗi sáng tôi uống cà phê.", "I drink coffee every morning."));
            p.examples.Add(new PatternExample("日曜日は働きません。", "にちようびははたらきません。", "nichiyoubi wa hatarakimasen.", "Chủ nhật tôi không làm việc.", "I don't work on Sundays."));
            p.drills.Add(PatternDrill.Fill("あした、えいがを＿＿。", "Ngày mai tôi sẽ xem phim.", "I will watch a movie tomorrow.", 1, "みました", "みます", "みません", "みて"));
            p.drills.Add(PatternDrill.Choose("「I don't eat meat.」は？", "Tôi không ăn thịt.", "I don't eat meat.", 0, "にくをたべません。", "にくをたべます。", "にくをたべました。", "にくがたべます。"));
            p.drills.Add(PatternDrill.Order("Tôi đi học bằng tàu điện.", "I go to school by train.", "でんしゃで", "がっこうへ", "いきます。"));
            list.Add(p);

            p = P("n5_wo_kudasai", "N5", "〜をください", "N を ください", "cho tôi N", "please give me N", "Nをお願いする言い方",
                "Dùng khi mua hàng, gọi món.", "Used when shopping or ordering.", "買い物や注文で使う。");
            p.examples.Add(new PatternExample("お水をください。", "おみずをください。", "omizu o kudasai.", "Cho tôi nước.", "Water, please."));
            p.examples.Add(new PatternExample("このパンを二つください。", "このパンをふたつください。", "kono pan o futatsu kudasai.", "Cho tôi hai cái bánh mì này.", "Two of this bread, please."));
            p.drills.Add(PatternDrill.Fill("コーヒー＿＿ください。", "Cho tôi cà phê.", "Coffee, please.", 2, "が", "に", "を", "は"));
            p.drills.Add(PatternDrill.Choose("コンビニで おにぎりが ほしいです。なんと いいますか。", "Ở konbini, bạn muốn onigiri.", "At the konbini you want an onigiri.", 1, "おにぎりをたべます。", "おにぎりをください。", "おにぎりがあります。", "おにぎりです。"));
            p.drills.Add(PatternDrill.Order("Cho tôi ba con tem.", "Three stamps, please.", "きってを", "みっつ", "ください。"));
            list.Add(p);

            p = P("n5_tai", "N5", "〜たいです", "V-ます stem + たいです", "muốn làm V", "want to V", "したいことを言う",
                "Bỏ ます thêm たい. Phủ định: たくないです.", "Drop ます, add たい. Negative: たくないです.", "ます形の語幹＋たい。否定は「たくないです」。");
            p.examples.Add(new PatternExample("日本へ行きたいです。", "にほんへいきたいです。", "nihon e ikitai desu.", "Tôi muốn đi Nhật.", "I want to go to Japan."));
            p.examples.Add(new PatternExample("すしが食べたいです。", "すしがたべたいです。", "sushi ga tabetai desu.", "Tôi muốn ăn sushi.", "I want to eat sushi."));
            p.drills.Add(PatternDrill.Fill("あたらしいくつを＿＿です。", "Tôi muốn mua giày mới.", "I want to buy new shoes.", 3, "かいます", "かって", "かった", "かいたい"));
            p.drills.Add(PatternDrill.Choose("「I don't want to sleep yet.」は？", "Tôi chưa muốn ngủ.", "I don't want to sleep yet.", 1, "まだねたいです。", "まだねたくないです。", "まだねません。", "まだねました。"));
            p.drills.Add(PatternDrill.Order("Tôi muốn học tiếng Nhật.", "I want to study Japanese.", "にほんごを", "べんきょうし", "たいです。"));
            list.Add(p);

            p = P("n5_te_kudasai", "N5", "〜てください", "V-て + ください", "hãy làm V (nhờ, yêu cầu)", "please do V", "丁寧な依頼・指示",
                "Dùng thể て của động từ.", "Uses the て-form of the verb.", "動詞のて形を使う。");
            p.examples.Add(new PatternExample("ちょっと待ってください。", "ちょっとまってください。", "chotto matte kudasai.", "Xin đợi một chút.", "Please wait a moment."));
            p.examples.Add(new PatternExample("ここに名前を書いてください。", "ここになまえをかいてください。", "koko ni namae o kaite kudasai.", "Hãy viết tên vào đây.", "Please write your name here."));
            p.drills.Add(PatternDrill.Fill("もういちど＿＿ください。", "Xin hãy nói lại một lần nữa.", "Please say it once more.", 0, "いって", "いいます", "いった", "いい"));
            p.drills.Add(PatternDrill.Choose("「Please open the window.」は？", "Hãy mở cửa sổ.", "Please open the window.", 2, "まどをあけます。", "まどをあけたいです。", "まどをあけてください。", "まどがあきます。"));
            p.drills.Add(PatternDrill.Order("Hãy nhìn vào bảng.", "Please look at the board.", "こくばんを", "みて", "ください。"));
            list.Add(p);

            p = P("n5_arimasu_imasu", "N5", "〜があります／います", "場所 に N が あります(vật)/います(người, con vật)", "ở đâu có N", "there is N (at a place)", "存在を表す",
                "あります cho đồ vật, います cho người và động vật.", "あります for things, います for people and animals.", "物は「あります」、人・動物は「います」。");
            p.examples.Add(new PatternExample("机の上に本があります。", "つくえのうえにほんがあります。", "tsukue no ue ni hon ga arimasu.", "Trên bàn có quyển sách.", "There is a book on the desk."));
            p.examples.Add(new PatternExample("公園に子どもがいます。", "こうえんにこどもがいます。", "kouen ni kodomo ga imasu.", "Ở công viên có trẻ em.", "There are children in the park."));
            p.drills.Add(PatternDrill.Fill("にわにねこが＿＿。", "Trong vườn có con mèo.", "There is a cat in the garden.", 1, "あります", "います", "です", "ください"));
            p.drills.Add(PatternDrill.Fill("えきのまえにコンビニが＿＿。", "Trước ga có cửa hàng tiện lợi.", "There is a convenience store in front of the station.", 0, "あります", "います", "いきます", "します"));
            p.drills.Add(PatternDrill.Order("Trong phòng có ai không?", "Is anyone in the room?", "へやに", "だれか", "いますか。"));
            list.Add(p);

            // ───────── N4 ─────────
            p = P("n4_ta_koto_ga_aru", "N4", "〜たことがあります", "V-た + ことがあります", "đã từng V", "have done V before", "経験を表す",
                "Nói về kinh nghiệm. Chưa từng: たことがありません.", "Talks about experience. Never: たことがありません.", "経験の有無を言う。");
            p.examples.Add(new PatternExample("富士山に登ったことがあります。", "ふじさんにのぼったことがあります。", "fujisan ni nobotta koto ga arimasu.", "Tôi đã từng leo núi Phú Sĩ.", "I have climbed Mt Fuji."));
            p.examples.Add(new PatternExample("納豆を食べたことがありません。", "なっとうをたべたことがありません。", "nattou o tabeta koto ga arimasen.", "Tôi chưa từng ăn natto.", "I have never eaten natto."));
            p.drills.Add(PatternDrill.Fill("きものを＿＿ことがありますか。", "Bạn đã từng mặc kimono chưa?", "Have you ever worn a kimono?", 2, "きます", "きて", "きた", "きる"));
            p.drills.Add(PatternDrill.Choose("「I have never been to Osaka.」は？", "Tôi chưa từng đến Osaka.", "I have never been to Osaka.", 3, "おおさかへいきます。", "おおさかへいったことがあります。", "おおさかへいきたいです。", "おおさかへいったことがありません。"));
            p.drills.Add(PatternDrill.Order("Tôi đã từng xem sumo.", "I have watched sumo.", "すもうを", "みた", "ことが", "あります。"));
            list.Add(p);

            p = P("n4_temo_ii", "N4", "〜てもいいです", "V-て + もいいです(か)", "được phép V", "may / it's OK to V", "許可を表す・求める",
                "Hỏi xin phép: 〜てもいいですか.", "Asking permission: 〜てもいいですか.", "許可を求めるときは「〜てもいいですか」。");
            p.examples.Add(new PatternExample("写真を撮ってもいいですか。", "しゃしんをとってもいいですか。", "shashin o tottemo ii desu ka.", "Tôi chụp ảnh được không?", "May I take a photo?"));
            p.examples.Add(new PatternExample("ここに座ってもいいですよ。", "ここにすわってもいいですよ。", "koko ni suwattemo ii desu yo.", "Bạn ngồi đây cũng được.", "You can sit here."));
            p.drills.Add(PatternDrill.Fill("まどを＿＿もいいですか。", "Tôi mở cửa sổ được không?", "May I open the window?", 1, "あけます", "あけて", "あけた", "あける"));
            p.drills.Add(PatternDrill.Choose("「May I go home now?」は？", "Bây giờ tôi về được không?", "May I go home now?", 0, "もうかえってもいいですか。", "もうかえりたいです。", "もうかえってください。", "もうかえりました。"));
            p.drills.Add(PatternDrill.Order("Tôi dùng bút chì được không?", "May I use a pencil?", "えんぴつを", "つかっても", "いいですか。"));
            list.Add(p);

            p = P("n4_nakereba", "N4", "〜なければなりません", "V-ない (bỏ い) + ければなりません", "phải V", "must V", "義務を表す",
                "Văn nói thân mật: 〜なきゃ.", "Casual speech: 〜なきゃ.", "話し言葉では「〜なきゃ」。");
            p.examples.Add(new PatternExample("明日早く起きなければなりません。", "あしたはやくおきなければなりません。", "ashita hayaku okinakereba narimasen.", "Ngày mai tôi phải dậy sớm.", "I have to get up early tomorrow."));
            p.examples.Add(new PatternExample("薬を飲まなければなりません。", "くすりをのまなければなりません。", "kusuri o nomanakereba narimasen.", "Tôi phải uống thuốc.", "I must take medicine."));
            p.drills.Add(PatternDrill.Fill("しゅくだいを＿＿なりません。", "Tôi phải làm bài tập.", "I have to do my homework.", 3, "しなくても", "して", "しても", "しなければ"));
            p.drills.Add(PatternDrill.Choose("「I must pay the rent.」は？", "Tôi phải trả tiền nhà.", "I must pay the rent.", 1, "やちんをはらいたいです。", "やちんをはらわなければなりません。", "やちんをはらってもいいです。", "やちんをはらいました。"));
            p.drills.Add(PatternDrill.Order("Tôi phải đi bệnh viện.", "I have to go to the hospital.", "びょういんへ", "いかなければ", "なりません。"));
            list.Add(p);

            p = P("n4_tsumori", "N4", "〜つもりです", "V-る / V-ない + つもりです", "định V", "intend to V", "予定・意志を表す",
                "Diễn tả dự định đã quyết.", "A decided intention or plan.", "前から決めている予定・意志。");
            p.examples.Add(new PatternExample("来年、日本で働くつもりです。", "らいねん、にほんではたらくつもりです。", "rainen, nihon de hataraku tsumori desu.", "Năm sau tôi định làm việc ở Nhật.", "I plan to work in Japan next year."));
            p.examples.Add(new PatternExample("もうたばこは吸わないつもりです。", "もうたばこはすわないつもりです。", "mou tabako wa suwanai tsumori desu.", "Tôi định không hút thuốc nữa.", "I don't intend to smoke anymore."));
            p.drills.Add(PatternDrill.Fill("なつやすみにくにへ＿＿つもりです。", "Hè này tôi định về nước.", "I intend to go back home in the summer holiday.", 0, "かえる", "かえって", "かえります", "かえった"));
            p.drills.Add(PatternDrill.Choose("「I don't intend to buy a car.」は？", "Tôi không định mua xe.", "I don't intend to buy a car.", 2, "くるまをかうつもりです。", "くるまをかいたいです。", "くるまをかわないつもりです。", "くるまをかってください。"));
            p.drills.Add(PatternDrill.Order("Tôi định thi N3.", "I intend to take the N3 exam.", "N3を", "うける", "つもりです。"));
            list.Add(p);

            p = P("n4_hou_ga_ii", "N4", "〜ほうがいいです", "V-た / V-ない + ほうがいいです", "nên V", "you had better V", "助言を表す",
                "Lời khuyên. Không nên: 〜ないほうがいいです.", "Advice. Better not: 〜ないほうがいいです.", "アドバイス。しない方がいいは「〜ないほうがいい」。");
            p.examples.Add(new PatternExample("早く寝たほうがいいですよ。", "はやくねたほうがいいですよ。", "hayaku neta hou ga ii desu yo.", "Bạn nên ngủ sớm.", "You'd better go to bed early."));
            p.examples.Add(new PatternExample("無理しないほうがいいです。", "むりしないほうがいいです。", "muri shinai hou ga ii desu.", "Bạn không nên cố quá.", "You shouldn't overdo it."));
            p.drills.Add(PatternDrill.Fill("かぜのときは、くすりを＿＿ほうがいいです。", "Khi cảm, bạn nên uống thuốc.", "When you have a cold, you should take medicine.", 2, "のみます", "のんで", "のんだ", "のみたい"));
            p.drills.Add(PatternDrill.Choose("「You'd better not go out tonight.」は？", "Tối nay bạn không nên ra ngoài.", "You'd better not go out tonight.", 1, "こんやでかけたほうがいいです。", "こんやでかけないほうがいいです。", "こんやでかけてもいいです。", "こんやでかけるつもりです。"));
            p.drills.Add(PatternDrill.Order("Bạn nên đi khám bác sĩ.", "You should see a doctor.", "いしゃに", "みてもらった", "ほうがいいです。"));
            list.Add(p);

            // ───────── N3 ─────────
            p = P("n3_you_ni_suru", "N3", "〜ようにしています", "V-る / V-ない + ようにしています", "cố gắng (duy trì thói quen) V", "make a point of V-ing", "習慣として心がけていること",
                "Diễn tả nỗ lực duy trì thói quen.", "An effort kept up as a habit.", "努力して習慣にしていること。");
            p.examples.Add(new PatternExample("毎日野菜を食べるようにしています。", "まいにちやさいをたべるようにしています。", "mainichi yasai o taberu you ni shite imasu.", "Tôi cố gắng ăn rau mỗi ngày.", "I make a point of eating vegetables every day."));
            p.examples.Add(new PatternExample("夜遅くまでスマホを見ないようにしています。", "よるおそくまでスマホをみないようにしています。", "yoru osoku made sumaho o minai you ni shite imasu.", "Tôi cố không xem điện thoại đến khuya.", "I try not to look at my phone late at night."));
            p.drills.Add(PatternDrill.Fill("まいあさ、日本語のニュースを＿＿ようにしています。", "Mỗi sáng tôi cố nghe tin tức tiếng Nhật.", "I try to listen to Japanese news every morning.", 0, "きく", "きいて", "きいた", "ききます"));
            p.drills.Add(PatternDrill.Choose("「I try not to be late.」は？", "Tôi cố gắng không đến muộn.", "I try not to be late.", 3, "ちこくするようにしています。", "ちこくしたことがあります。", "ちこくしてもいいです。", "ちこくしないようにしています。"));
            p.drills.Add(PatternDrill.Order("Tôi cố gắng đi bộ mỗi ngày.", "I try to walk every day.", "まいにち", "あるく", "ようにしています。"));
            list.Add(p);

            p = P("n3_bakari", "N3", "〜てばかりいる", "V-て + ばかりいる", "chỉ toàn V (phàn nàn)", "do nothing but V", "同じことばかりして困る気持ち",
                "Thường mang sắc thái phê phán.", "Usually critical in tone.", "非難・不満の気持ちを含むことが多い。");
            p.examples.Add(new PatternExample("弟はゲームをしてばかりいる。", "おとうとはゲームをしてばかりいる。", "otouto wa geemu o shite bakari iru.", "Em trai tôi suốt ngày chơi game.", "My little brother does nothing but play games."));
            p.examples.Add(new PatternExample("彼は文句を言ってばかりいます。", "かれはもんくをいってばかりいます。", "kare wa monku o itte bakari imasu.", "Anh ấy chỉ toàn phàn nàn.", "He does nothing but complain."));
            p.drills.Add(PatternDrill.Fill("あの子はテレビを＿＿ばかりいる。", "Đứa bé đó suốt ngày xem TV.", "That kid does nothing but watch TV.", 1, "みる", "みて", "みた", "みない"));
            p.drills.Add(PatternDrill.Choose("「She does nothing but sleep.」は？", "Cô ấy chỉ toàn ngủ.", "She does nothing but sleep.", 2, "かのじょはねるつもりです。", "かのじょはねたことがある。", "かのじょはねてばかりいる。", "かのじょはねたほうがいい。"));
            p.drills.Add(PatternDrill.Order("Đừng chỉ toàn ăn đồ ngọt.", "Don't eat nothing but sweets.", "あまいものを", "たべて", "ばかりいないで。"));
            list.Add(p);

            p = P("n3_hazu", "N3", "〜はずです", "普通形 + はずです (な-adj な / N の)", "chắc chắn là ~ (suy luận có căn cứ)", "should be / is expected to", "根拠のある推測",
                "Suy luận dựa trên căn cứ. Không thể: はずがない.", "A reasoned expectation. Impossible: はずがない.", "根拠に基づく確信。否定的確信は「はずがない」。");
            p.examples.Add(new PatternExample("彼はもう家に着いたはずです。", "かれはもういえについたはずです。", "kare wa mou ie ni tsuita hazu desu.", "Chắc anh ấy đã về đến nhà rồi.", "He should have arrived home by now."));
            p.examples.Add(new PatternExample("今日は日曜日だから、銀行は休みのはずです。", "きょうはにちようびだから、ぎんこうはやすみのはずです。", "kyou wa nichiyoubi dakara, ginkou wa yasumi no hazu desu.", "Hôm nay chủ nhật nên chắc ngân hàng nghỉ.", "It's Sunday, so the bank should be closed."));
            p.drills.Add(PatternDrill.Fill("かぎはかばんの中に＿＿はずです。", "Chìa khóa chắc là ở trong cặp.", "The key should be in the bag.", 0, "ある", "あって", "あります", "あれ"));
            p.drills.Add(PatternDrill.Fill("あのみせはゆうめいだから、おいしい＿＿です。", "Quán đó nổi tiếng nên chắc là ngon.", "That shop is famous, so it should be tasty.", 3, "ため", "ばかり", "つもり", "はず"));
            p.drills.Add(PatternDrill.Order("Cuộc họp chắc bắt đầu lúc 3 giờ.", "The meeting should start at three.", "かいぎは", "３じに", "はじまる", "はずです。"));
            list.Add(p);

            p = P("n3_koto_ni_suru", "N3", "〜ことにする", "V-る / V-ない + ことにする", "quyết định V", "decide to V", "自分の意志で決める",
                "Quyết định của bản thân. Do người khác quyết: ことになる.", "Your own decision. Decided for you: ことになる.", "自分の決定。他者・状況による決定は「ことになる」。");
            p.examples.Add(new PatternExample("来月から自炊することにしました。", "らいげつからじすいすることにしました。", "raigetsu kara jisui suru koto ni shimashita.", "Tôi đã quyết định tự nấu ăn từ tháng sau.", "I've decided to cook for myself from next month."));
            p.examples.Add(new PatternExample("今日は出かけないことにします。", "きょうはでかけないことにします。", "kyou wa dekakenai koto ni shimasu.", "Hôm nay tôi quyết định không ra ngoài.", "I'll decide not to go out today."));
            p.drills.Add(PatternDrill.Fill("けんこうのために、まいあさ＿＿ことにしました。", "Vì sức khỏe, tôi quyết định chạy bộ mỗi sáng.", "For my health, I decided to jog every morning.", 2, "はしって", "はしった", "はしる", "はしります"));
            p.drills.Add(PatternDrill.Choose("「I decided not to drink alcohol.」は？", "Tôi quyết định không uống rượu.", "I decided not to drink alcohol.", 0, "おさけをのまないことにしました。", "おさけをのむことになりました。", "おさけをのんだことがあります。", "おさけをのんでばかりいます。"));
            p.drills.Add(PatternDrill.Order("Tôi quyết định học ở Nhật.", "I decided to study in Japan.", "にほんで", "べんきょうする", "ことにしました。"));
            list.Add(p);

            return list;
        }
    }

    /// <summary>Pure answer checking and deterministic shuffling for drills (no UnityEngine dependency).</summary>
    public static class PatternDrills
    {
        public static bool IsCorrect(PatternDrill drill, int choice) =>
            drill != null && drill.type != PatternDrillType.Reorder && choice == drill.answerIndex;

        /// <summary>order = indexes into <see cref="Shuffled"/> as the player tapped them.</summary>
        public static bool IsCorrectOrder(PatternDrill drill, IReadOnlyList<string> builtTokens)
        {
            if (drill == null || drill.type != PatternDrillType.Reorder || builtTokens == null) return false;
            return string.Concat(builtTokens) == string.Concat(drill.tokens);
        }

        /// <summary>Tokens shuffled with a seed from the pattern id; never returns the correct order when avoidable.</summary>
        public static string[] Shuffled(PatternDrill drill, string seedKey)
        {
            var arr = (string[])drill.tokens.Clone();
            if (arr.Length < 2) return arr;
            uint h = 2166136261;
            foreach (char c in seedKey ?? "") h = (h ^ c) * 16777619;
            for (int i = arr.Length - 1; i > 0; i--)
            {
                h = h * 1664525 + 1013904223;
                int j = (int)(h % (uint)(i + 1));
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
            if (string.Concat(arr) == string.Concat(drill.tokens)) { (arr[0], arr[arr.Length - 1]) = (arr[arr.Length - 1], arr[0]); }
            return arr;
        }

        /// <summary>Stars for a finished set: 3 = all correct, 2 = ≥ 2/3, 1 = ≥ 1/3, else 0.</summary>
        public static int Stars(int correct, int total)
        {
            if (total <= 0) return 0;
            if (correct >= total) return 3;
            if (correct * 3 >= total * 2) return 2;
            if (correct * 3 >= total) return 1;
            return 0;
        }
    }
}
