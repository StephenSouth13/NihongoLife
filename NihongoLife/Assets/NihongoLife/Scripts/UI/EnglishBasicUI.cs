using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.StudyModes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// English Basic (Tiếng Anh cơ bản / 基礎英語) in the Study Hub: pick a beginner topic, read its vocabulary and
    /// example sentences (with Vietnamese/Japanese meaning), then take a short quiz (meaning, fill-in, word order)
    /// with instant feedback, stars and saved best scores. Content: <see cref="EnglishBasicCatalog"/> (pre-written).
    /// </summary>
    public sealed class EnglishBasicUI : MenuPopupBase
    {
        private const float W = 1380f, H = 870f, ListW = 330f;

        private EnglishBasicLesson _lesson;
        private RectTransform _list, _detail;

        private int _quizIndex = -1, _correct;
        private bool _answered, _lastCorrect, _newBest;
        private readonly List<string> _built = new List<string>();
        private readonly List<int> _usedTokens = new List<int>();

        protected override bool LocksGameplay => true;
        protected override Vector2 CardSize => new Vector2(W, H);

        protected override void GetTitle(out string vi, out string en, out string ja)
        { vi = "Tiếng Anh cơ bản"; en = "English Basic"; ja = "基礎英語"; }

        public EnglishBasicLesson CurrentLesson => _lesson;
        public bool InQuiz => _quizIndex >= 0;
        public bool LastAnswerCorrect => _lastCorrect;

        /// <summary>Meaning of a word/sentence in the UI language (English UI shows Vietnamese as the helper gloss).</summary>
        public static string Gloss(string vi, string ja) => Loc.IsJapanese ? ja : vi;

        protected override void Build(RectTransform card)
        {
            var sub = UiKit.Label(card, "Subtitle", "Chủ đề hằng ngày → từ vựng → câu ví dụ → bài kiểm tra nhỏ. Không tính điểm thi IELTS.",
                "Everyday topic → vocabulary → example sentences → short quiz. Separate from IELTS practice.",
                "日常のテーマ→単語→例文→ミニクイズ。IELTS練習とは別です。", 15f, UiTheme.Muted);
            UiKit.Place(sub.rectTransform, 36f, 60f, W - 160f, 24f);

            var listBg = UiKit.Card(card, "ListBg", UiTheme.Sunken, 18, 0f, false);
            UiKit.Place(listBg.rectTransform, 32f, 108f, ListW, H - 140f);
            _list = UiKit.Scroll(listBg.transform, "List", out _, 10f, new RectOffset(10, 10, 10, 10));
            UiKit.Fill((RectTransform)_list.parent.parent);

            var detailBg = UiKit.Card(card, "DetailBg", UiTheme.Surface, 20, 0f, false);
            UiKit.Place(detailBg.rectTransform, 32f + ListW + 24f, 108f, W - ListW - 88f, H - 140f);
            _detail = UiKit.Rect(detailBg.transform, "Detail");
            UiKit.Fill(_detail, 28f, 24f, 28f, 24f);
        }

        protected override void OnOpened()
        {
            _lesson ??= EnglishBasicCatalog.All.FirstOrDefault();
            _quizIndex = -1;
            Redraw();
        }

        protected override void OnLanguageApplied() { if (IsOpen) Redraw(); }

        protected override bool HandleEscape()
        {
            if (!InQuiz) return false;
            _quizIndex = -1; DrawLesson();
            return true;
        }

        public void SelectLesson(string id)
        {
            var l = EnglishBasicCatalog.Find(id);
            if (l == null) return;
            _lesson = l; _quizIndex = -1;
            Redraw();
        }

        private void Redraw()
        {
            DrawList();
            if (InQuiz) DrawQuiz(); else DrawLesson();
        }

        private static string Stars(int n) => new string('★', Mathf.Clamp(n, 0, 3)) + new string('☆', 3 - Mathf.Clamp(n, 0, 3));
        private float DW => W - ListW - 88f - 56f;

        private void DrawList()
        {
            if (_list == null) return;
            UiKit.Clear(_list);
            var store = LearningProgressStore.Shared;
            int n = 0;
            foreach (var l in EnglishBasicCatalog.All)
            {
                var lesson = l;
                bool on = _lesson == l;
                n++;
                var row = UiKit.Card(_list, "Row_" + l.id, on ? UiTheme.Raised : UiTheme.Surface, 14);
                UiKit.Size(row, -1f, 74f);
                var btn = row.gameObject.AddComponent<Button>();
                btn.targetGraphic = row;
                btn.onClick.AddListener(() => SelectLesson(lesson.id));
                if (on) { var bar = UiKit.Image(row.transform, "Sel", UiTheme.Ielts, UiKit.Rounded(3)); UiKit.Place(bar.rectTransform, 0f, 14f, 5f, 46f); }
                var ic = UiKit.Icon(row.transform, l.icon, UiTheme.Ielts, 30f);
                UiKit.Place(ic.rectTransform, 16f, 22f, 30f, 30f);
                var t = UiKit.Label(row.transform, "Title", $"{n}. {l.titleEn}", 18f, UiTheme.Text, FontStyles.Bold);
                UiKit.Place(t.rectTransform, 56f, 8f, ListW - 160f, 30f);
                var m = UiKit.Label(row.transform, "Local", Loc.T(l.titleVi, l.titleEn, l.titleJa), 13.5f, UiTheme.Muted);
                UiKit.Place(m.rectTransform, 56f, 40f, ListW - 90f, 24f);
                var s = UiKit.Label(row.transform, "Stars", Stars(store.StarsFor(l.id)), 15f, UiTheme.Gold, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
                UiKit.Place(s.rectTransform, ListW - 112f, 10f, 76f, 26f);
            }
        }

        private void DrawLesson()
        {
            if (_detail == null) return;
            UiKit.Clear(_detail);
            if (_lesson == null) return;
            var l = _lesson;
            var chip = UiKit.Chip(_detail, "Level", "A1", UiTheme.Ielts, Color.white, "school", 13f);
            UiKit.Place(chip, 0f, 0f, 80f, 26f);
            var title = UiKit.Label(_detail, "Title", l.titleEn, 32f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(title.rectTransform, 0f, 32f, DW, 44f);
            var goal = UiKit.Label(_detail, "Goal", Loc.T(l.goalVi, l.goalEn, l.goalJa), 16f, UiTheme.Gold, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, true);
            UiKit.Place(goal.rectTransform, 0f, 78f, DW, 26f);

            var vTitle = UiKit.Label(_detail, "VocabTitle", Loc.T("Từ vựng", "Vocabulary", "単語"), 17f, UiTheme.Muted, FontStyles.Bold);
            UiKit.Place(vTitle.rectTransform, 0f, 112f, 300f, 26f);
            float cw = (DW - 2 * 12f) / 3f;
            for (int i = 0; i < l.vocab.Count; i++)
            {
                var v = l.vocab[i];
                var c = UiKit.Card(_detail, "Vocab" + i, UiTheme.Raised, 12, 0f, false);
                UiKit.Place(c.rectTransform, (i % 3) * (cw + 12f), 142f + (i / 3) * 72f, cw, 62f);
                var w = UiKit.Label(c.transform, "Word", v.en, 18f, UiTheme.Text, FontStyles.Bold);
                UiKit.Place(w.rectTransform, 14f, 6f, cw - 64f, 28f);
                var g = UiKit.Label(c.transform, "Meaning", Gloss(v.vi, v.ja), 14f, UiTheme.TextSoft);
                UiKit.Place(g.rectTransform, 14f, 34f, cw - 64f, 22f);
                SpeakButton(c.transform, v.en, cw - 50f, 9f);
            }
            float y = 142f + ((l.vocab.Count + 2) / 3) * 72f + 8f;

            var exTitle = UiKit.Label(_detail, "ExamplesTitle", Loc.T("Câu ví dụ", "Example sentences", "例文"), 17f, UiTheme.Muted, FontStyles.Bold);
            UiKit.Place(exTitle.rectTransform, 0f, y, 300f, 26f);
            y += 30f;
            foreach (var ex in l.examples)
            {
                var exCard = UiKit.Card(_detail, "Example", UiTheme.Raised, 14, 0f, false);
                UiKit.Place(exCard.rectTransform, 0f, y, DW, 70f);
                var en = UiKit.Label(exCard.transform, "En", ex.en, 20f, UiTheme.Text, FontStyles.Bold);
                UiKit.Place(en.rectTransform, 18f, 6f, DW - 90f, 30f);
                var gl = UiKit.Label(exCard.transform, "Gloss", Gloss(ex.vi, ex.ja), 15f, UiTheme.Gold);
                UiKit.Place(gl.rectTransform, 18f, 38f, DW - 90f, 24f);
                SpeakButton(exCard.transform, ex.en, DW - 62f, 13f);
                y += 80f;
            }

            var entry = LearningProgressStore.Shared.Get(l.id);
            string best = entry == null ? Loc.T("Chưa làm bài", "Not practised yet", "未練習")
                : Loc.F("Tốt nhất {0}/{1} · {2} lần", "Best {0}/{1} · {2} tries", "ベスト {0}/{1}・{2}回", entry.bestCorrect, entry.total, entry.attempts);
            var bestLabel = UiKit.Label(_detail, "Best", Stars(entry?.stars ?? 0) + "  " + best, 16f, UiTheme.TextSoft);
            UiKit.Place(bestLabel.rectTransform, 0f, y + 10f, DW - 280f, 48f);
            var start = UiKit.Button(_detail, "StartQuiz", "Làm bài", "Take the quiz", "クイズに挑戦", StartQuiz, UiKit.Kind.Primary, "quiz");
            UiKit.Place((RectTransform)start.transform, DW - 240f, y + 6f, 240f, 52f);
        }

        private static void SpeakButton(Transform parent, string text, float x, float y)
        {
            if (!PatternSpeech.Available) return;
            var speak = UiKit.IconButton(parent, "Speak", "volume_up", () => PatternSpeech.Speak(text, "en-US"));
            UiKit.Place((RectTransform)speak.transform, x, y, 44f, 44f);
        }

        public void StartQuiz()
        {
            if (_lesson == null || _lesson.quiz.Count == 0) return;
            _quizIndex = 0; _correct = 0; _newBest = false;
            ResetQuestion();
            DrawQuiz();
        }

        private void ResetQuestion() { _answered = false; _built.Clear(); _usedTokens.Clear(); }

        private void DrawQuiz()
        {
            UiKit.Clear(_detail);
            var l = _lesson;
            if (_quizIndex >= l.quiz.Count) { DrawSummary(); return; }
            var q = l.quiz[_quizIndex];

            var back = UiKit.Button(_detail, "Back", "Quay lại", "Back", "戻る", () => { _quizIndex = -1; DrawLesson(); }, UiKit.Kind.Ghost, "arrow_back", 15f);
            UiKit.Place((RectTransform)back.transform, 0f, 0f, 150f, 40f);
            var progress = UiKit.Label(_detail, "Progress", $"{l.titleEn} · {_quizIndex + 1}/{l.quiz.Count}", 16f, UiTheme.Muted, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            UiKit.Place(progress.rectTransform, DW - 400f, 0f, 400f, 40f);
            var bar = UiKit.ProgressBar(_detail, "Bar", UiTheme.Ielts, UiTheme.Sunken);
            UiKit.Place(bar.parent as RectTransform ?? bar, 0f, 52f, DW, 8f);
            UiKit.SetProgress(bar, (float)_quizIndex / l.quiz.Count);

            string kind = q.type switch
            {
                EnglishQuizType.Meaning => Loc.T("Chọn đáp án đúng", "Choose the right answer", "正しい答えを選ぼう"),
                EnglishQuizType.FillIn => Loc.T("Điền vào chỗ trống", "Fill in the blank", "空欄を埋めよう"),
                _ => Loc.T("Sắp xếp thành câu đúng", "Put the words in order", "正しい順に並べよう"),
            };
            var k = UiKit.Label(_detail, "Kind", kind, 17f, UiTheme.Gold, FontStyles.Bold);
            UiKit.Place(k.rectTransform, 0f, 78f, DW, 28f);
            string hint = Loc.T(q.hintVi, q.hintEn, q.hintJa);
            if (q.type != EnglishQuizType.Reorder)
            {
                var prompt = UiKit.Label(_detail, "Prompt", q.prompt, 28f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, true);
                UiKit.Place(prompt.rectTransform, 0f, 116f, DW, 84f);
            }
            var hintLabel = UiKit.Label(_detail, "Hint", hint, 16f, UiTheme.TextSoft);
            UiKit.Place(hintLabel.rectTransform, 0f, q.type == EnglishQuizType.Reorder ? 116f : 204f, DW, 28f);

            if (q.type == EnglishQuizType.Reorder) DrawReorder(q); else DrawChoices(q);
        }

        private void DrawChoices(EnglishQuiz q)
        {
            float cw = (DW - 16f) / 2f;
            for (int i = 0; i < q.choices.Length; i++)
            {
                int idx = i;
                var kindBtn = _answered && i == q.answerIndex ? UiKit.Kind.Success : UiKit.Kind.Secondary;
                var b = UiKit.Button(_detail, "Choice" + i, q.choices[i], () => Answer(idx), kindBtn, null, 20f);
                UiKit.Place((RectTransform)b.transform, (i % 2) * (cw + 16f), 250f + (i / 2) * 76f, cw, 64f);
                b.interactable = !_answered || i == q.answerIndex;
            }
            if (_answered) DrawFeedback(q, 420f);
        }

        public void Answer(int choice)
        {
            if (!InQuiz || _answered) return;
            var q = _lesson.quiz[_quizIndex];
            if (q.type == EnglishQuizType.Reorder) return;
            _answered = true;
            _lastCorrect = EnglishQuizzes.IsCorrect(q, choice);
            if (_lastCorrect) _correct++;
            PlayCue(_lastCorrect ? NihongoLife.Audio.GameAudioCue.UiOpen : NihongoLife.Audio.GameAudioCue.UiClose, 0.6f);
            DrawQuiz();
        }

        private void DrawReorder(EnglishQuiz q)
        {
            var line = UiKit.Card(_detail, "Built", UiTheme.Sunken, 14, 0f, false);
            UiKit.Place(line.rectTransform, 0f, 156f, DW, 76f);
            var built = UiKit.Label(line.transform, "Text", _built.Count == 0 ? Loc.T("Chạm vào các thẻ bên dưới…", "Tap the cards below…", "下のカードをタップ…") : string.Join(" ", _built),
                24f, _built.Count == 0 ? UiTheme.Muted : UiTheme.Text, FontStyles.Bold);
            UiKit.Fill(built.rectTransform, 18f, 0f, 18f, 0f);

            var shuffled = EnglishQuizzes.Shuffled(q, _lesson.id + _quizIndex);
            float x = 0f, y = 250f;
            for (int i = 0; i < shuffled.Length; i++)
            {
                int idx = i;
                float w = Mathf.Clamp(shuffled[i].Length * 16f + 48f, 90f, DW);
                if (x + w > DW) { x = 0f; y += 66f; }
                var b = UiKit.Button(_detail, "Token" + i, shuffled[i], () => Tap(idx, shuffled[idx]), UiKit.Kind.Secondary, null, 20f);
                UiKit.Place((RectTransform)b.transform, x, y, w, 56f);
                b.interactable = !_answered && !_usedTokens.Contains(i);
                x += w + 12f;
            }
            y += 80f;
            if (!_answered)
            {
                var reset = UiKit.Button(_detail, "Reset", "Làm lại", "Reset", "やり直し", () => { _built.Clear(); _usedTokens.Clear(); DrawQuiz(); }, UiKit.Kind.Ghost, "refresh");
                UiKit.Place((RectTransform)reset.transform, 0f, y, 180f, 50f);
                var check = UiKit.Button(_detail, "Check", "Kiểm tra", "Check", "確認", CheckOrder, UiKit.Kind.Primary, "done");
                UiKit.Place((RectTransform)check.transform, DW - 200f, y, 200f, 50f);
                check.interactable = _built.Count == q.tokens.Length;
            }
            else DrawFeedback(q, y);
        }

        public void Tap(int shuffledIndex, string token)
        {
            if (!InQuiz || _answered || _usedTokens.Contains(shuffledIndex)) return;
            _usedTokens.Add(shuffledIndex);
            _built.Add(token);
            DrawQuiz();
        }

        public void CheckOrder()
        {
            if (!InQuiz || _answered) return;
            var q = _lesson.quiz[_quizIndex];
            if (q.type != EnglishQuizType.Reorder || _built.Count != q.tokens.Length) return;
            _answered = true;
            _lastCorrect = EnglishQuizzes.IsCorrectOrder(q, _built);
            if (_lastCorrect) _correct++;
            DrawQuiz();
        }

        private void DrawFeedback(EnglishQuiz q, float y)
        {
            var fb = UiKit.Card(_detail, "Feedback", UiKit.Alpha(_lastCorrect ? UiTheme.Good : UiTheme.Bad, 0.18f), 14, 0f, false);
            UiKit.Place(fb.rectTransform, 0f, y, DW, 96f);
            var head = UiKit.Label(fb.transform, "Head", _lastCorrect ? Loc.T("Chính xác!", "Correct!", "正解！") : Loc.T("Chưa đúng", "Not quite", "おしい！"),
                20f, _lastCorrect ? UiTheme.Good : UiTheme.Bad, FontStyles.Bold);
            UiKit.Place(head.rectTransform, 18f, 8f, DW - 36f, 30f);
            var ans = UiKit.Label(fb.transform, "Answer", Loc.T("Đáp án: ", "Answer: ", "答え：") + q.CorrectAnswer, 19f, UiTheme.Text);
            UiKit.Place(ans.rectTransform, 18f, 44f, DW - 36f, 40f);
            bool last = _quizIndex + 1 >= _lesson.quiz.Count;
            var next = UiKit.Button(_detail, "Next", last ? "Xem kết quả" : "Câu tiếp", last ? "See results" : "Next", last ? "結果を見る" : "次へ", Next, UiKit.Kind.Primary, "arrow_forward");
            UiKit.Place((RectTransform)next.transform, DW - 220f, y + 112f, 220f, 50f);
        }

        public void Next()
        {
            if (!InQuiz || !_answered) return;
            _quizIndex++;
            ResetQuestion();
            if (_quizIndex >= _lesson.quiz.Count)
            {
                _newBest = LearningProgressStore.Shared.RecordResult(_lesson.id, _correct, _lesson.quiz.Count);
                DrawList();
            }
            DrawQuiz();
        }

        private void DrawSummary()
        {
            int total = _lesson.quiz.Count;
            int stars = PatternDrills.Stars(_correct, total);
            var big = UiKit.Label(_detail, "Stars", Stars(stars), 64f, UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(big.rectTransform, 0f, 90f, DW, 90f);
            var score = UiKit.Label(_detail, "Score", Loc.F("Đúng {0}/{1}", "{0}/{1} correct", "{0}/{1}問正解", _correct, total), 30f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(score.rectTransform, 0f, 190f, DW, 44f);
            string msg = stars == 3 ? Loc.T("Tuyệt vời! Bạn đã nắm vững chủ đề này.", "Excellent! You've mastered this topic.", "すばらしい！このテーマはマスターです。")
                : Loc.T("Ôn lại từ vựng và ví dụ rồi thử lần nữa để đạt 3 sao.", "Review the words and examples, then try again for 3 stars.", "単語と例文を復習して★3を目指そう。");
            if (_newBest) msg += "  " + Loc.T("(Kỷ lục mới!)", "(New best!)", "（自己ベスト！）");
            var m = UiKit.Label(_detail, "Message", msg, 17f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.Center, true);
            UiKit.Place(m.rectTransform, 0f, 244f, DW, 56f);
            var again = UiKit.Button(_detail, "Again", "Làm lại", "Try again", "もう一度", StartQuiz, UiKit.Kind.Secondary, "replay");
            UiKit.Place((RectTransform)again.transform, DW / 2f - 230f, 330f, 220f, 52f);
            var done = UiKit.Button(_detail, "Done", "Về bài học", "Back to lesson", "レッスンに戻る", () => { _quizIndex = -1; DrawLesson(); }, UiKit.Kind.Primary, "done");
            UiKit.Place((RectTransform)done.transform, DW / 2f + 10f, 330f, 220f, 52f);
        }
    }
}
