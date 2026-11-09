using System;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Course;
using NihongoLife.StudyModes;
using NihongoLife.StudyModes.Video;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// "Lớp học / Classroom" (key ` (backquote), the dock's Classroom button, the desk at the front of the Hibari classroom):
    /// pick a language first, then only that language's lessons, videos and mock tests show. Every study window opens from here,
    /// the exam centre too (K still opens it directly).
    /// </summary>
    public sealed class StudyHubPopup : MenuPopupBase
    {
        private const float W = 1180f, H = 720f;
        private static readonly string[] Languages = { "ja", "en", "ru", "vi" };
        private RectTransform _tiles, _chips;
        private TextMeshProUGUI _status;
        private int _tileCount;

        /// <summary>The language whose tiles show (ja, en, ru, vi).</summary>
        public string Language { get; private set; } = "ja";

        protected override bool LocksGameplay => true;
        protected override Vector2 CardSize => new Vector2(W, H);

        protected override void GetTitle(out string vi, out string en, out string ja)
        { vi = "Lớp học"; en = "Classroom"; ja = "教室"; }

        public static StudyHubPopup GetOrCreate(GameObject host, TMP_FontAsset font)
        {
            var hub = host.GetComponent<StudyHubPopup>();
            if (hub == null) hub = host.Need<StudyHubPopup>();
            hub.Initialize(font);
            return hub;
        }

        protected override void Build(RectTransform card)
        {
            _chips = UiKit.Rect(card, "Languages");
            UiKit.Place(_chips, 32f, 74f, W - 64f, 50f);
            _tiles = UiKit.Rect(card, "Tiles");
            UiKit.Place(_tiles, 32f, 140f, W - 64f, 470f);
            _status = UiKit.Label(card, "Status", "", 15f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, true);
            UiKit.Place(_status.rectTransform, 36f, H - 112f, W - 72f - 520f, 52f);
            var dictionary = UiKit.Button(card, "Dictionary", "Từ điển", "Dictionary", "辞書", () => OpenDictionary(), UiKit.Kind.Secondary, "book_2", 16f);
            UiKit.Place((RectTransform)dictionary.transform, W - 36f - 280f - 16f - 200f, H - 110f, 200f, 48f);
            var plan = UiKit.Button(card, "LearningPlan", "Lộ trình học cá nhân", "My learning plan", "わたしの学習プラン", () => OpenLearningPlan(), UiKit.Kind.Primary, "route", 16f);
            UiKit.Place((RectTransform)plan.transform, W - 36f - 280f, H - 110f, 280f, 48f);
        }

        protected override void OnOpened()
        {
            // English Basic is built in: (re)register its launcher against this hub so the tile opens EnglishBasicUI.
            StudyModeRegistry.Register(StudyModeRegistry.EnglishBasic, OpenEnglishBasic);
            Rebuild();
        }
        protected override void OnLanguageApplied() { if (IsOpen) Rebuild(); }

        protected override void Update()
        {
            base.Update();
            if (Keyboard.current == null || !Keyboard.current.backquoteKey.wasPressedThisFrame) return;
            if (IsOpen) { Hide(); return; }
            if (!UiModalStack.BlocksHotkeys) Show();
        }

        public void ChooseLanguage(string language)
        {
            if (Array.IndexOf(Languages, language) < 0) throw new ArgumentException("Unknown study language " + language);
            Language = language;
            Rebuild();
        }

        private void Rebuild()
        {
            if (_tiles == null) return;
            UiKit.Clear(_chips);
            for (int i = 0; i < Languages.Length; i++)
            {
                string code = Languages[i];
                var chip = UiKit.Button(_chips, "Lang_" + code, LanguageName(code), () => ChooseLanguage(code), code == Language ? UiKit.Kind.Primary : UiKit.Kind.Quiet, null, 16f);
                UiKit.Place((RectTransform)chip.transform, i * 196f, 0f, 184f, 48f);
            }
            UiKit.Clear(_tiles);
            _tileCount = 0;
            var store = LearningProgressStore.Shared;
            var route = LearningPath.CurrentRoute();
            string pathBadge = route != null
                ? Loc.F("Lộ trình: {0}/{1} bước", "Your path: {0}/{1} steps", "コース：{0}/{1}ステップ", LearningPath.DoneCount(route), route.Steps.Count)
                : Loc.T("Chưa chọn lộ trình", "No path chosen yet", "コース未選択");
            string lang = Language;

            if (lang == "ru")
                Tile("Lộ trình tiếng Nga", "Russian path", "ロシア語コース", "route", UiTheme.Live,
                    Loc.T("Bảng chữ cái, phát âm, A1 → B2 và đề thi thử TORFL có đáp án.",
                          "Alphabet, pronunciation, A1 → B2 and TORFL mock tests with answer keys.",
                          "文字・発音・A1→B2、解答付きTORFL模試。"),
                    Loc.F("{0} bước", "{0} steps", "{0}ステップ", NihongoLife.StudyModes.Russian.RussianCourse.Path.Steps.Count()), true, OpenRussianPath);
            Tile("Bài học", "Lessons", "レッスン", "cast_for_education", UiTheme.Gold,
                Loc.T("40 bài, bảng chữ cái, ngữ pháp, chứng chỉ và bài kiểm tra trình độ.",
                      "40 lessons, the alphabet, grammar, certificates and a placement test.",
                      "40レッスン、文字、文法、検定、レベルチェック。"),
                pathBadge, true, () => OpenSchool(lang));
            if (lang == "ja")
            {
                int stars = store.TotalStars(JapanesePatternCatalog.All.Select(p => p.id));
                Tile("Mẫu câu tiếng Nhật", "Japanese patterns", "文型練習", "translate", UiTheme.Jlpt,
                    Loc.F("N5 → N3 · {0} mẫu câu · ví dụ có furigana, romaji và bài luyện điền/chọn/sắp xếp.",
                          "N5 → N3 · {0} patterns · examples with furigana and romaji, fill-in / choose / reorder drills.",
                          "N5→N3・{0}文型・ふりがな/ローマ字付き例文と穴埋め・選択・並べ替えドリル。", JapanesePatternCatalog.All.Count),
                    Loc.F("★ {0}/{1}", "★ {0}/{1}", "★ {0}/{1}", stars, JapanesePatternCatalog.All.Count * 3), true, OpenPatterns);
            }
            if (lang == "en")
            {
                bool english = StudyModeRegistry.IsAvailable(StudyModeRegistry.EnglishBasic);
                Tile("Tiếng Anh cơ bản", "English Basic", "基礎英語", "school", UiTheme.Ielts,
                    Loc.F("A1 · {0} chủ đề: chào hỏi, số đếm, mua sắm, hỏi đường… Từ vựng, câu ví dụ và bài kiểm tra nhỏ.",
                          "A1 · {0} topics: greetings, numbers, shopping, directions… Vocabulary, examples and short quizzes.",
                          "A1・{0}テーマ：あいさつ・数字・買い物・道案内…単語・例文・ミニクイズ。", EnglishBasicCatalog.All.Count),
                    english ? Loc.F("★ {0}/{1}", "★ {0}/{1}", "★ {0}/{1}", store.TotalStars(EnglishBasicCatalog.All.Select(l => l.id)), EnglishBasicCatalog.All.Count * 3) : Loc.T("Sắp ra mắt", "Coming soon", "近日公開"), english, OpenEnglish);
                var tenseKeys = TensesCatalog.Lessons().Select(l => LearningPath.TenseKey(l.TenseId)).ToList();
                Tile("12 thì tiếng Anh", "12 English tenses", "英語の12時制", "schedule", UiTheme.Warn,
                    Loc.F("Công thức, cách dùng, từ nhận biết, lỗi hay gặp, bảng chia động từ và {0} bài tập có chấm điểm.",
                          "Formulas, usage, signal words, common mistakes, a verb conjugation table and {0} graded exercises.",
                          "公式・使い方・合図の語・よくある間違い・活用表と、採点つきの練習{0}問。", TensesCatalog.ExerciseCount()),
                    Loc.F("★ {0}/{1}", "★ {0}/{1}", "★ {0}/{1}", store.TotalStars(tenseKeys), tenseKeys.Count * 3), true, OpenTenses);
                Tile("Bài mẫu IELTS", "IELTS model answers", "IELTSモデル解答", "menu_book", UiTheme.Ielts,
                    Loc.F("{0} đề Viết/Nói, mỗi đề có bài mẫu band 5–9, phân tích theo tiêu chí và cách lên band.",
                          "{0} Writing/Speaking prompts, each with band 5–9 samples, criterion analysis and how to reach the next band.",
                          "ライティング/スピーキング{0}題、各バンド5〜9の解答例・項目別講評・次のバンドへのコツ。", NihongoLife.Exam.Ielts.IeltsModelAnswers.All.Count),
                    Loc.T("Band 5 → 9", "Band 5 → 9", "バンド5→9"), true, OpenModelAnswers);
            }
            var courses = VideoCourseCatalog.File.courses.Where(c => c.lang == lang).ToList();
            if (courses.Count > 0)
                Tile("Video bài học", "Video library", "動画ライブラリ", "smart_display", UiTheme.Teal,
                    Loc.F("{0} bài video có giáo trình, xem ngay trong game hoặc trên YouTube.",
                          "{0} video lessons with a curriculum, in the game or on YouTube.",
                          "カリキュラム付きのビデオ{0}本。ゲーム内でもYouTubeでも。", courses.Sum(c => c.LessonCount)),
                    Loc.F("Đã xem {0}/{1}", "Watched {0}/{1}", "視聴済み {0}/{1}", courses.Sum(c => c.Watched(store)), courses.Sum(c => c.LessonCount)), true, () => OpenVideos(lang));
            if (lang == "ja" || lang == "en")
                Tile(lang == "ja" ? "Thi thử JLPT" : "Thi thử IELTS", lang == "ja" ? "JLPT mock tests" : "IELTS mock tests", lang == "ja" ? "JLPT模試" : "IELTS模試", "quiz", UiTheme.Warn,
                    Loc.T("Đề thi theo đúng định dạng, có chấm điểm và lưu lịch sử.", "Tests in the real format, marked and kept in your history.", "本番形式の試験。採点と履歴つき。"),
                    "K", true, OpenExams);
            _status.text = "";
        }

        private static string LanguageName(string code) => code switch
        {
            "ja" => Loc.T("Tiếng Nhật", "Japanese", "日本語"),
            "en" => Loc.T("Tiếng Anh", "English", "英語"),
            "ru" => Loc.T("Tiếng Nga", "Russian", "ロシア語"),
            _ => Loc.T("Tiếng Việt", "Vietnamese", "ベトナム語"),
        };

        private void Tile(string vi, string en, string ja, string icon, Color color, string body, string badge, bool enabled, Action open)
        {
            const float TH = 222f;
            float width = (W - 64f - 2 * 24f) / 3f;
            int index = _tileCount++;
            var card = UiKit.Card(_tiles, "Tile" + index, UiTheme.Surface, 20, 0.3f);
            UiKit.Place(card.rectTransform, (index % 3) * (width + 24f), (index / 3) * (TH + 26f), width, TH);
            var wash = UiKit.Image(card.transform, "Wash", UiKit.Alpha(color, 0.18f), UiKit.VerticalFade);
            UiKit.Strip(wash.rectTransform, true, 0f, 90f);
            var disc = UiKit.Card(card.transform, "Disc", UiKit.Alpha(color, 0.25f), 30, 0f, false);
            UiKit.Place(disc.rectTransform, 20f, 18f, 60f, 60f);
            var ic = UiKit.Icon(disc.transform, icon, color, 34f);
            UiKit.Pin(ic.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 34f, 34f);
            var title = UiKit.Label(card.transform, "Title", Loc.T(vi, en, ja), 19f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(title.rectTransform, 96f, 18f, width - 116f, 60f);
            title.textWrappingMode = TextWrappingModes.Normal;
            var b = UiKit.Label(card.transform, "Body", body, 12.5f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft, true);
            UiKit.Place(b.rectTransform, 20f, 84f, width - 40f, 74f);
            var chip = UiKit.Chip(card.transform, "Badge", badge, UiKit.Alpha(color, 0.3f), UiTheme.Text, enabled ? "star" : "hourglass_top", 12.5f);
            // A long badge (Russian) shrinks to its place: fitted to the text it ran under the Open button.
            chip.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            var chipText = chip.Find("Text").GetComponent<TextMeshProUGUI>();
            chipText.textWrappingMode = TextWrappingModes.NoWrap;
            chipText.overflowMode = TextOverflowModes.Ellipsis;
            chipText.enableAutoSizing = true;
            chipText.fontSizeMin = 9.5f;
            chipText.fontSizeMax = 12.5f;
            UiKit.Place(chip, 20f, 166f, width - 40f - 130f, 28f);
            var go = UiKit.Button(card.transform, "Open", "Mở", "Open", "開く", () => open(), enabled ? UiKit.Kind.Primary : UiKit.Kind.Quiet, "arrow_forward", 15f);
            UiKit.Place((RectTransform)go.transform, width - 20f - 112f, 162f, 112f, 44f);
        }

        private T Child<T>() where T : MenuPopupBase
        {
            var popup = GetComponent<T>();
            if (popup == null) popup = gameObject.Need<T>();
            popup.Initialize(Font);
            return popup;
        }

        public void OpenPatterns() { Hide(); Child<JapanesePatternsUI>().Show(); }
        public void OpenVideos() { Hide(); Child<VideoCoursesUI>().Show(); }
        public void OpenVideos(string language) { Hide(); Child<VideoCoursesUI>().ShowLanguage(language); }
        public void OpenSchool() { Hide(); Child<CoursePopup>().Show(); }
        public void OpenSchool(string target) { Hide(); var school = Child<CoursePopup>(); school.Show(); school.ChooseTarget(target); }
        public void OpenExams() { Hide(); Child<ExamCenterPopup>().Show(); }
        public void OpenTenses() { Hide(); var school = Child<CoursePopup>(); school.Show(); school.ShowTab(CoursePopup.Tab.Tenses); }
        public void OpenDictionary() { Hide(); HandbookUI.Open(HandbookUI.Chapter.Dictionary); }
        public void OpenEnglishBasic() { Hide(); Child<EnglishBasicUI>().Show(); }
        public void OpenModelAnswers() { Hide(); var p = Child<ModelAnswersPopup>(); p.Show(); }
        public void OpenRussianPath() { Hide(); Child<RussianPathPopup>().Show(); }
        public void OpenLearningPlan() { Hide(); Child<LearningPlanPopup>().Show(); }

        private void OpenEnglish()
        {
            if (StudyModeRegistry.TryLaunch(StudyModeRegistry.EnglishBasic)) { Hide(); return; }
            _status.text = Loc.T("Khóa tiếng Anh cơ bản đang được hoàn thiện — trong lúc chờ, hãy xem video hoặc luyện IELTS ở Trung tâm luyện thi (K).",
                "English Basic is being finished — meanwhile watch videos or practise IELTS in the Exam Center (K).",
                "基礎英語は準備中です。それまで動画視聴や試験センター（K）でIELTS練習をどうぞ。");
        }
    }
}
