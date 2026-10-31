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
    /// "Học thêm / Study more" hub (key ` (backquote), or the Study button on touch): Japanese sentence patterns (文型), English Basic
    /// and the in-game video library. Separate from the Exam Center (K), which stays for real JLPT/IELTS tests.
    /// English Basic opens whatever registered <see cref="StudyModeRegistry.EnglishBasic"/>.
    /// </summary>
    public sealed class StudyHubPopup : MenuPopupBase
    {
        private const float W = 1180f, H = 720f;
        private RectTransform _tiles;
        private TextMeshProUGUI _status;

        protected override bool LocksGameplay => true;
        protected override Vector2 CardSize => new Vector2(W, H);

        protected override void GetTitle(out string vi, out string en, out string ja)
        { vi = "Học thêm"; en = "Study more"; ja = "もっと学ぶ"; }

        public static StudyHubPopup GetOrCreate(GameObject host, TMP_FontAsset font)
        {
            var hub = host.GetComponent<StudyHubPopup>();
            if (hub == null) hub = host.AddComponent<StudyHubPopup>();
            hub.Initialize(font);
            return hub;
        }

        protected override void Build(RectTransform card)
        {
            var sub = UiKit.Label(card, "Subtitle",
                "Luyện mẫu câu, tiếng Anh cơ bản và xem video ngay trong game. Thi thật vẫn ở Trung tâm luyện thi (K).",
                "Practise sentence patterns and basic English, and watch videos in-game. Real exams stay in the Exam Center (K).",
                "文型・基礎英語の練習と動画視聴をゲーム内で。本番形式の試験は試験センター（K）へ。", 15f, UiTheme.Muted, wrap: true);
            UiKit.Place(sub.rectTransform, 36f, 68f, W - 120f, 40f);
            _tiles = UiKit.Rect(card, "Tiles");
            UiKit.Place(_tiles, 32f, 114f, W - 64f, 470f);
            _status = UiKit.Label(card, "Status", "", 15f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, true);
            UiKit.Place(_status.rectTransform, 36f, H - 112f, W - 72f - 300f, 52f);
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

        private void Rebuild()
        {
            if (_tiles == null) return;
            UiKit.Clear(_tiles);
            float tw = (W - 64f - 3 * 24f) / 4f;
            var store = LearningProgressStore.Shared;
            int stars = store.TotalStars(JapanesePatternCatalog.All.Select(p => p.id));
            int maxStars = JapanesePatternCatalog.All.Count * 3;
            int watched = VideoCatalog.All.Count(v => store.IsVideoWatched(v.id));
            bool english = StudyModeRegistry.IsAvailable(StudyModeRegistry.EnglishBasic);

            Tile(0, tw, "Mẫu câu tiếng Nhật", "Japanese patterns", "文型練習", "translate", UiTheme.Jlpt,
                Loc.F("N5 → N3 · {0} mẫu câu · ví dụ có furigana, romaji và bài luyện điền/chọn/sắp xếp.",
                      "N5 → N3 · {0} patterns · examples with furigana and romaji, fill-in / choose / reorder drills.",
                      "N5→N3・{0}文型・ふりがな/ローマ字付き例文と穴埋め・選択・並べ替えドリル。", JapanesePatternCatalog.All.Count),
                Loc.F("★ {0}/{1}", "★ {0}/{1}", "★ {0}/{1}", stars, maxStars), true, OpenPatterns);
            Tile(1, tw, "Tiếng Anh cơ bản", "English Basic", "基礎英語", "school", UiTheme.Ielts,
                Loc.F("A1 · {0} chủ đề: chào hỏi, số đếm, mua sắm, hỏi đường… Từ vựng, câu ví dụ và bài kiểm tra nhỏ.",
                      "A1 · {0} topics: greetings, numbers, shopping, directions… Vocabulary, examples and short quizzes.",
                      "A1・{0}テーマ：あいさつ・数字・買い物・道案内…単語・例文・ミニクイズ。", EnglishBasicCatalog.All.Count),
                english ? Loc.F("★ {0}/{1}", "★ {0}/{1}", "★ {0}/{1}", store.TotalStars(EnglishBasicCatalog.All.Select(l => l.id)), EnglishBasicCatalog.All.Count * 3) : Loc.T("Sắp ra mắt", "Coming soon", "近日公開"), english, OpenEnglish);
            Tile(2, tw, "Video bài học", "Video library", "動画ライブラリ", "videocam", UiTheme.Teal,
                Loc.F("{0} video từ kênh NihongoLife — xem trong game, có tóm tắt và phụ đề.",
                      "{0} videos from the NihongoLife channel — watch in-game with summary and transcript.",
                      "NihongoLifeチャンネルの動画{0}本—要約・字幕付きでゲーム内視聴。", VideoCatalog.All.Count),
                Loc.F("Đã xem {0}/{1}", "Watched {0}/{1}", "視聴済み {0}/{1}", watched, VideoCatalog.All.Count), true, OpenVideos);
            var route = LearningPath.CurrentRoute();
            string pathBadge = route != null
                ? Loc.F("Lộ trình: {0}/{1} bước", "Your path: {0}/{1} steps", "コース：{0}/{1}ステップ", LearningPath.DoneCount(route), route.Steps.Count)
                : Loc.T("Chưa chọn lộ trình", "No path chosen yet", "コース未選択");
            Tile(3, tw, "Trường học", "School", "学校", "cast_for_education", UiTheme.Gold,
                Loc.T("40 bài cho mỗi ngôn ngữ (Việt, Anh, Nhật, Nga), bảng chữ cái, ngữ pháp, chứng chỉ N5–N1 / A1–C1 và bài kiểm tra trình độ.",
                      "40 lessons for each language (Vietnamese, English, Japanese, Russian), alphabets, grammar, N5–N1 / A1–C1 certificates and a placement test.",
                      "各言語40レッスン（ベトナム語・英語・日本語・ロシア語）、文字、文法、N5〜N1／A1〜C1の検定、レベルチェック。"),
                pathBadge, true, OpenSchool);
            var tenseKeys = TensesCatalog.Lessons().Select(l => LearningPath.TenseKey(l.TenseId)).ToList();
            Tile(4, tw, "12 thì tiếng Anh", "12 English tenses", "英語の12時制", "schedule", UiTheme.Warn,
                Loc.F("Công thức, cách dùng, từ nhận biết, lỗi hay gặp, bảng chia động từ và {0} bài tập có chấm điểm.",
                      "Formulas, usage, signal words, common mistakes, a verb conjugation table and {0} graded exercises.",
                      "公式・使い方・合図の語・よくある間違い・活用表と、採点つきの練習{0}問。", TensesCatalog.ExerciseCount()),
                Loc.F("★ {0}/{1}", "★ {0}/{1}", "★ {0}/{1}", store.TotalStars(tenseKeys), tenseKeys.Count * 3), true, OpenTenses);
            Tile(5, tw, "Từ điển", "Dictionary", "辞書", "book_2", UiTheme.Teal,
                Loc.T("Tra từ giữa tiếng Việt, Anh, Nhật và Nga (12 hướng) ngay trong Sổ tay: tìm theo từ hoặc cách đọc.",
                      "Look words up between Vietnamese, English, Japanese and Russian (12 directions) right in the Handbook, by word or reading.",
                      "ベトナム語・英語・日本語・ロシア語（12方向）の単語を、ハンドブックで読み方からも引けます。"),
                "VI · EN · JA · RU", true, OpenDictionary);
            Tile(6, tw, "Bài mẫu IELTS", "IELTS model answers", "IELTSモデル解答", "menu_book", UiTheme.Ielts,
                Loc.F("{0} đề Viết/Nói, mỗi đề có bài mẫu band 5–9, phân tích theo tiêu chí và cách lên band.",
                      "{0} Writing/Speaking prompts, each with band 5–9 samples, criterion analysis and how to reach the next band.",
                      "ライティング/スピーキング{0}題、各バンド5〜9の解答例・項目別講評・次のバンドへのコツ。", NihongoLife.Exam.Ielts.IeltsModelAnswers.All.Count),
                Loc.T("Band 5 → 9", "Band 5 → 9", "バンド5→9"), true, OpenModelAnswers);
            Tile(7, tw, "Lộ trình tiếng Nga", "Russian path", "ロシア語コース", "school", UiTheme.Live,
                Loc.T("Bảng chữ cái, phát âm, A1 → B2 và đề thi thử TORFL (ТРКИ) có đáp án.",
                      "Alphabet, pronunciation, A1 → B2 and TORFL (ТРКИ) mock tests with answer keys.",
                      "文字・発音・A1→B2、解答付きTORFL（ТРКИ）模試。"),
                Loc.F("{0} bước", "{0} steps", "{0}ステップ", NihongoLife.StudyModes.Russian.RussianCourse.Path.Steps.Count()), true, OpenRussianPath);
            _status.text = "";
        }

        private void Tile(int index, float width, string vi, string en, string ja, string icon, Color color, string body, string badge, bool enabled, Action open)
        {
            const float TH = 222f;
            var card = UiKit.Card(_tiles, "Tile" + index, UiTheme.Surface, 20, 0.3f);
            UiKit.Place(card.rectTransform, (index % 4) * (width + 24f), (index / 4) * (TH + 26f), width, TH);
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
            UiKit.Place(b.rectTransform, 20f, 84f, width - 40f, 66f);
            var chip = UiKit.Chip(card.transform, "Badge", badge, UiKit.Alpha(color, 0.3f), UiTheme.Text, enabled ? "star" : "hourglass_top", 12.5f);
            UiKit.Place(chip, 20f, 156f, width - 40f - 130f, 28f);
            var go = UiKit.Button(card.transform, "Open", "Mở", "Open", "開く", () => open(), enabled ? UiKit.Kind.Primary : UiKit.Kind.Quiet, "arrow_forward", 15f);
            UiKit.Place((RectTransform)go.transform, width - 20f - 112f, 154f, 112f, 44f);
        }

        private T Child<T>() where T : MenuPopupBase
        {
            var popup = GetComponent<T>();
            if (popup == null) popup = gameObject.AddComponent<T>();
            popup.Initialize(Font);
            return popup;
        }

        public void OpenPatterns() { Hide(); Child<JapanesePatternsUI>().Show(); }
        public void OpenVideos() { Hide(); Child<InGameVideoPlayerUI>().Show(); }
        public void OpenSchool() { Hide(); Child<CoursePopup>().Show(); }
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
