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
    /// 文型 practice: browse N5–N3 sentence patterns (structure, meaning, examples with kana reading, romaji and a
    /// gloss) and run their drills (fill-in, choose, reorder) with instant feedback, stars and saved best scores.
    /// Separate from the JLPT mock exams in the Exam Center.
    /// </summary>
    public sealed class JapanesePatternsUI : MenuPopupBase
    {
        private const float W = 1380f, H = 870f, ListW = 330f;

        private string _level = "N5";
        private SentencePattern _pattern;
        private RectTransform _levelRow, _list, _detail;
        private readonly Dictionary<string, Button> _levelButtons = new Dictionary<string, Button>();

        // drill session
        private int _drillIndex = -1, _correct;
        private bool _answered;
        private readonly List<string> _built = new List<string>();
        private readonly List<int> _usedTokens = new List<int>();

        protected override bool LocksGameplay => true;
        protected override Vector2 CardSize => new Vector2(W, H);

        protected override void GetTitle(out string vi, out string en, out string ja)
        { vi = "Mẫu câu tiếng Nhật"; en = "Japanese sentence patterns"; ja = "文型練習"; }

        public SentencePattern CurrentPattern => _pattern;
        public bool InDrill => _drillIndex >= 0;

        protected override void Build(RectTransform card)
        {
            var sub = UiKit.Label(card, "Subtitle", "Học cấu trúc → xem ví dụ → luyện tập. Không tính điểm thi JLPT.",
                "Learn the structure → read examples → drill. Separate from JLPT mock exams.",
                "構造を学ぶ→例文→ドリル。JLPT模試とは別です。", 15f, UiTheme.Muted);
            UiKit.Place(sub.rectTransform, 36f, 60f, W - 160f, 24f);

            _levelRow = UiKit.Rect(card, "Levels");
            UiKit.Place(_levelRow, 32f, 108f, ListW, 44f);
            float x = 0f, bw = (ListW - 16f) / 3f;
            foreach (var level in JapanesePatternCatalog.Levels)
            {
                string l = level;
                var b = UiKit.Button(_levelRow, "Level" + l, l, () => SelectLevel(l), UiKit.Kind.Secondary, null, 17f);
                UiKit.Place((RectTransform)b.transform, x, 0f, bw, 44f);
                _levelButtons[l] = b;
                x += bw + 8f;
            }

            var listBg = UiKit.Card(card, "ListBg", UiTheme.Sunken, 18, 0f, false);
            UiKit.Place(listBg.rectTransform, 32f, 164f, ListW, H - 196f);
            _list = UiKit.Scroll(listBg.transform, "List", out _, 10f, new RectOffset(10, 10, 10, 10));
            UiKit.Fill((RectTransform)_list.parent.parent);

            var detailBg = UiKit.Card(card, "DetailBg", UiTheme.Surface, 20, 0f, false);
            UiKit.Place(detailBg.rectTransform, 32f + ListW + 24f, 108f, W - ListW - 88f, H - 140f);
            _detail = UiKit.Rect(detailBg.transform, "Detail");
            UiKit.Fill(_detail, 28f, 24f, 28f, 24f);
        }

        protected override void OnOpened() => SelectLevel(_level);
        protected override void OnLanguageApplied() { if (IsOpen) Redraw(); }

        protected override bool HandleEscape()
        {
            if (!InDrill) return false;
            _drillIndex = -1; DrawPattern();
            return true;
        }

        public void SelectLevel(string level)
        {
            _level = level;
            foreach (var kv in _levelButtons) UiKit.SetKind(kv.Value, kv.Key == level ? UiKit.Kind.Primary : UiKit.Kind.Secondary);
            var first = JapanesePatternCatalog.ForLevel(level).FirstOrDefault();
            if (_pattern == null || _pattern.level != level) _pattern = first;
            _drillIndex = -1;
            Redraw();
        }

        public void SelectPattern(string id)
        {
            var p = JapanesePatternCatalog.Find(id);
            if (p == null) return;
            _pattern = p; _level = p.level; _drillIndex = -1;
            Redraw();
        }

        private void Redraw()
        {
            DrawList();
            if (InDrill) DrawDrill(); else DrawPattern();
        }

        private void DrawList()
        {
            if (_list == null) return;
            UiKit.Clear(_list);
            var store = LearningProgressStore.Shared;
            foreach (var p in JapanesePatternCatalog.ForLevel(_level))
            {
                var pat = p;
                bool on = _pattern == p;
                var row = UiKit.Card(_list, "Row_" + p.id, on ? UiTheme.Raised : UiTheme.Surface, 14);
                UiKit.Size(row, -1f, 74f);
                var btn = row.gameObject.AddComponent<Button>();
                btn.targetGraphic = row;
                btn.onClick.AddListener(() => SelectPattern(pat.id));
                if (on) { var bar = UiKit.Image(row.transform, "Sel", UiTheme.Jlpt, UiKit.Rounded(3)); UiKit.Place(bar.rectTransform, 0f, 14f, 5f, 46f); }
                var t = UiKit.Label(row.transform, "Pattern", p.pattern, 20f, UiTheme.Text, FontStyles.Bold);
                UiKit.Place(t.rectTransform, 16f, 8f, ListW - 120f, 30f);
                var m = UiKit.Label(row.transform, "Meaning", Loc.T(p.meaningVi, p.meaningEn, p.meaningJa), 13.5f, UiTheme.Muted);
                UiKit.Place(m.rectTransform, 16f, 40f, ListW - 60f, 24f);
                var s = UiKit.Label(row.transform, "Stars", Stars(store.StarsFor(p.id)), 16f, UiTheme.Gold, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
                UiKit.Place(s.rectTransform, ListW - 120f, 10f, 80f, 26f);
            }
        }

        private static string Stars(int n) => new string('★', Mathf.Clamp(n, 0, 3)) + new string('☆', 3 - Mathf.Clamp(n, 0, 3));

        private float DW => W - ListW - 88f - 56f;

        private void DrawPattern()
        {
            if (_detail == null) return;
            UiKit.Clear(_detail);
            if (_pattern == null) return;
            var p = _pattern;
            var chip = UiKit.Chip(_detail, "Level", p.level, UiTheme.Jlpt, Color.white, "translate", 13f);
            UiKit.Place(chip, 0f, 0f, 80f, 26f);
            var title = UiKit.Label(_detail, "Title", p.pattern, 34f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(title.rectTransform, 0f, 34f, DW - 220f, 46f);
            var mean = UiKit.Label(_detail, "Meaning", Loc.T(p.meaningVi, p.meaningEn, p.meaningJa), 18f, UiTheme.Gold);
            UiKit.Place(mean.rectTransform, 0f, 82f, DW, 28f);

            var box = UiKit.Card(_detail, "Structure", UiTheme.Sunken, 14, 0f, false);
            UiKit.Place(box.rectTransform, 0f, 120f, DW, 84f);
            var st = UiKit.Label(box.transform, "Text", Loc.T("Cấu trúc: ", "Structure: ", "構造：") + p.structure, 18f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(st.rectTransform, 18f, 8f, DW - 36f, 32f);
            var note = UiKit.Label(box.transform, "Note", Loc.T(p.noteVi, p.noteEn, p.noteJa), 14.5f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft, true);
            UiKit.Place(note.rectTransform, 18f, 42f, DW - 36f, 40f);

            var exTitle = UiKit.Label(_detail, "ExamplesTitle", Loc.T("Ví dụ", "Examples", "例文"), 17f, UiTheme.Muted, FontStyles.Bold);
            UiKit.Place(exTitle.rectTransform, 0f, 218f, 300f, 26f);
            float y = 250f;
            foreach (var ex in p.examples)
            {
                var exCard = UiKit.Card(_detail, "Example", UiTheme.Raised, 14, 0f, false);
                UiKit.Place(exCard.rectTransform, 0f, y, DW, 128f);
                var kana = UiKit.Label(exCard.transform, "Kana", ex.kana, 13.5f, UiTheme.Muted);
                UiKit.Place(kana.rectTransform, 18f, 8f, DW - 90f, 22f);
                var ja = UiKit.Label(exCard.transform, "Ja", ex.ja, 23f, UiTheme.Text, FontStyles.Bold);
                UiKit.Place(ja.rectTransform, 18f, 30f, DW - 90f, 34f);
                var ro = UiKit.Label(exCard.transform, "Romaji", ex.romaji, 14f, UiTheme.TextSoft, FontStyles.Italic);
                UiKit.Place(ro.rectTransform, 18f, 66f, DW - 90f, 22f);
                string gloss = Loc.IsJapanese ? ex.en : Loc.T(ex.vi, ex.en, ex.en);
                var gl = UiKit.Label(exCard.transform, "Gloss", gloss, 15f, UiTheme.Gold);
                UiKit.Place(gl.rectTransform, 18f, 92f, DW - 90f, 26f);
                if (PatternSpeech.Available)
                {
                    string text = ex.ja;
                    var speak = UiKit.IconButton(exCard.transform, "Speak", "volume_up", () => PatternSpeech.Speak(text));
                    UiKit.Place((RectTransform)speak.transform, DW - 62f, 40f, 44f, 44f);
                }
                y += 140f;
            }

            var entry = LearningProgressStore.Shared.Get(p.id);
            string best = entry == null ? Loc.T("Chưa luyện", "Not practised yet", "未練習")
                : Loc.F("Tốt nhất {0}/{1} · {2} lần", "Best {0}/{1} · {2} tries", "ベスト {0}/{1}・{2}回", entry.bestCorrect, entry.total, entry.attempts);
            var bestLabel = UiKit.Label(_detail, "Best", Stars(entry?.stars ?? 0) + "  " + best, 16f, UiTheme.TextSoft);
            UiKit.Place(bestLabel.rectTransform, 0f, y + 14f, DW - 280f, 48f);
            var start = UiKit.Button(_detail, "StartDrill", "Luyện tập", "Practise", "練習する", StartDrill, UiKit.Kind.Primary, "quiz");
            UiKit.Place((RectTransform)start.transform, DW - 240f, y + 10f, 240f, 52f);
        }

        public void StartDrill()
        {
            if (_pattern == null || _pattern.drills.Count == 0) return;
            _drillIndex = 0; _correct = 0;
            ResetQuestion();
            DrawDrill();
        }

        private void ResetQuestion() { _answered = false; _built.Clear(); _usedTokens.Clear(); }

        private void DrawDrill()
        {
            UiKit.Clear(_detail);
            var p = _pattern;
            if (_drillIndex >= p.drills.Count) { DrawSummary(); return; }
            var d = p.drills[_drillIndex];

            var back = UiKit.Button(_detail, "Back", "Quay lại", "Back", "戻る", () => { _drillIndex = -1; DrawPattern(); }, UiKit.Kind.Ghost, "arrow_back", 15f);
            UiKit.Place((RectTransform)back.transform, 0f, 0f, 150f, 40f);
            var progress = UiKit.Label(_detail, "Progress", $"{p.pattern} · {_drillIndex + 1}/{p.drills.Count}", 16f, UiTheme.Muted, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            UiKit.Place(progress.rectTransform, DW - 400f, 0f, 400f, 40f);
            var bar = UiKit.ProgressBar(_detail, "Bar", UiTheme.Jlpt, UiTheme.Sunken);
            UiKit.Place(bar.parent as RectTransform ?? bar, 0f, 52f, DW, 8f);
            UiKit.SetProgress(bar, (float)_drillIndex / p.drills.Count);

            string kind = d.type switch
            {
                PatternDrillType.FillIn => Loc.T("Điền vào chỗ trống", "Fill in the blank", "空欄を埋めよう"),
                PatternDrillType.Choose => Loc.T("Chọn câu đúng", "Choose the right sentence", "正しい文を選ぼう"),
                _ => Loc.T("Sắp xếp thành câu đúng", "Put the words in order", "正しい順に並べよう"),
            };
            var k = UiKit.Label(_detail, "Kind", kind, 17f, UiTheme.Gold, FontStyles.Bold);
            UiKit.Place(k.rectTransform, 0f, 78f, DW, 28f);
            string hint = Loc.IsJapanese ? d.hintEn : Loc.T(d.hintVi, d.hintEn, d.hintEn);
            if (d.type != PatternDrillType.Reorder)
            {
                var prompt = UiKit.Label(_detail, "Prompt", d.promptJa, 28f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, true);
                UiKit.Place(prompt.rectTransform, 0f, 116f, DW, 84f);
            }
            var hintLabel = UiKit.Label(_detail, "Hint", hint, 16f, UiTheme.TextSoft);
            UiKit.Place(hintLabel.rectTransform, 0f, d.type == PatternDrillType.Reorder ? 116f : 204f, DW, 28f);

            if (d.type == PatternDrillType.Reorder) DrawReorder(d); else DrawChoices(d);
        }

        private void DrawChoices(PatternDrill d)
        {
            float cw = (DW - 16f) / 2f;
            for (int i = 0; i < d.choices.Length; i++)
            {
                int idx = i;
                var kindBtn = UiKit.Kind.Secondary;
                if (_answered && i == d.answerIndex) kindBtn = UiKit.Kind.Success;
                var b = UiKit.Button(_detail, "Choice" + i, d.choices[i], () => Answer(idx), kindBtn, null, 20f);
                UiKit.Place((RectTransform)b.transform, (i % 2) * (cw + 16f), 250f + (i / 2) * 76f, cw, 64f);
                b.interactable = !_answered || i == d.answerIndex;
            }
            if (_answered) DrawFeedback(d, 420f);
        }

        public void Answer(int choice)
        {
            if (!InDrill || _answered) return;
            var d = _pattern.drills[_drillIndex];
            _answered = true;
            bool ok = PatternDrills.IsCorrect(d, choice);
            if (ok) _correct++;
            _lastCorrect = ok;
            PlayCue(ok ? NihongoLife.Audio.GameAudioCue.UiOpen : NihongoLife.Audio.GameAudioCue.UiClose, 0.6f);
            DrawDrill();
        }

        private bool _lastCorrect;

        private void DrawReorder(PatternDrill d)
        {
            var line = UiKit.Card(_detail, "Built", UiTheme.Sunken, 14, 0f, false);
            UiKit.Place(line.rectTransform, 0f, 156f, DW, 76f);
            var built = UiKit.Label(line.transform, "Text", _built.Count == 0 ? Loc.T("Chạm vào các thẻ bên dưới…", "Tap the cards below…", "下のカードをタップ…") : string.Concat(_built),
                26f, _built.Count == 0 ? UiTheme.Muted : UiTheme.Text, FontStyles.Bold);
            UiKit.Fill(built.rectTransform, 18f, 0f, 18f, 0f);

            var shuffled = PatternDrills.Shuffled(d, _pattern.id + _drillIndex);
            float x = 0f, y = 250f;
            for (int i = 0; i < shuffled.Length; i++)
            {
                int idx = i;
                float w = Mathf.Clamp(shuffled[i].Length * 26f + 40f, 90f, DW);
                if (x + w > DW) { x = 0f; y += 66f; }
                var b = UiKit.Button(_detail, "Token" + i, shuffled[i], () => Tap(idx, shuffled[idx]), UiKit.Kind.Secondary, null, 20f);
                UiKit.Place((RectTransform)b.transform, x, y, w, 56f);
                b.interactable = !_answered && !_usedTokens.Contains(i);
                x += w + 12f;
            }
            y += 80f;
            if (!_answered)
            {
                var reset = UiKit.Button(_detail, "Reset", "Làm lại", "Reset", "やり直し", () => { _built.Clear(); _usedTokens.Clear(); DrawDrill(); }, UiKit.Kind.Ghost, "refresh");
                UiKit.Place((RectTransform)reset.transform, 0f, y, 180f, 50f);
                var check = UiKit.Button(_detail, "Check", "Kiểm tra", "Check", "確認", CheckOrder, UiKit.Kind.Primary, "done");
                UiKit.Place((RectTransform)check.transform, DW - 200f, y, 200f, 50f);
                check.interactable = _built.Count == d.tokens.Length;
            }
            else DrawFeedback(d, y);
        }

        public void Tap(int shuffledIndex, string token)
        {
            if (!InDrill || _answered || _usedTokens.Contains(shuffledIndex)) return;
            _usedTokens.Add(shuffledIndex);
            _built.Add(token);
            DrawDrill();
        }

        public void CheckOrder()
        {
            if (!InDrill || _answered) return;
            var d = _pattern.drills[_drillIndex];
            _answered = true;
            _lastCorrect = PatternDrills.IsCorrectOrder(d, _built);
            if (_lastCorrect) _correct++;
            DrawDrill();
        }

        private void DrawFeedback(PatternDrill d, float y)
        {
            var fb = UiKit.Card(_detail, "Feedback", UiKit.Alpha(_lastCorrect ? UiTheme.Good : UiTheme.Bad, 0.18f), 14, 0f, false);
            UiKit.Place(fb.rectTransform, 0f, y, DW, 96f);
            var head = UiKit.Label(fb.transform, "Head", _lastCorrect ? Loc.T("Chính xác!", "Correct!", "正解！") : Loc.T("Chưa đúng", "Not quite", "おしい！"),
                20f, _lastCorrect ? UiTheme.Good : UiTheme.Bad, FontStyles.Bold);
            UiKit.Place(head.rectTransform, 18f, 8f, DW - 36f, 30f);
            var ans = UiKit.Label(fb.transform, "Answer", Loc.T("Đáp án: ", "Answer: ", "答え：") + d.CorrectSentence, 19f, UiTheme.Text);
            UiKit.Place(ans.rectTransform, 18f, 44f, DW - 36f, 40f);
            bool last = _drillIndex + 1 >= _pattern.drills.Count;
            var next = UiKit.Button(_detail, "Next", last ? "Xem kết quả" : "Câu tiếp", last ? "See results" : "Next", last ? "結果を見る" : "次へ", Next, UiKit.Kind.Primary, "arrow_forward");
            UiKit.Place((RectTransform)next.transform, DW - 220f, y + 112f, 220f, 50f);
        }

        public void Next()
        {
            if (!InDrill || !_answered) return;
            _drillIndex++;
            ResetQuestion();
            if (_drillIndex >= _pattern.drills.Count)
            {
                _newBest = LearningProgressStore.Shared.RecordResult(_pattern.id, _correct, _pattern.drills.Count);
                DrawList();
            }
            DrawDrill();
        }

        private bool _newBest;

        private void DrawSummary()
        {
            int total = _pattern.drills.Count;
            int stars = PatternDrills.Stars(_correct, total);
            var big = UiKit.Label(_detail, "Stars", Stars(stars), 64f, UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(big.rectTransform, 0f, 90f, DW, 90f);
            var score = UiKit.Label(_detail, "Score", Loc.F("Đúng {0}/{1}", "{0}/{1} correct", "{0}/{1}問正解", _correct, total), 30f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(score.rectTransform, 0f, 190f, DW, 44f);
            string msg = stars == 3 ? Loc.T("Tuyệt vời! Bạn đã nắm vững mẫu câu này.", "Excellent! You've mastered this pattern.", "すばらしい！この文型はマスターです。")
                : Loc.T("Ôn lại ví dụ rồi thử lần nữa để đạt 3 sao.", "Review the examples and try again for 3 stars.", "例文を復習して★3を目指そう。");
            if (_newBest) msg += "  " + Loc.T("(Kỷ lục mới!)", "(New best!)", "（自己ベスト！）");
            var m = UiKit.Label(_detail, "Message", msg, 17f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.Center, true);
            UiKit.Place(m.rectTransform, 0f, 244f, DW, 56f);
            var again = UiKit.Button(_detail, "Again", "Luyện lại", "Try again", "もう一度", StartDrill, UiKit.Kind.Secondary, "replay");
            UiKit.Place((RectTransform)again.transform, DW / 2f - 230f, 330f, 220f, 52f);
            var done = UiKit.Button(_detail, "Done", "Về mẫu câu", "Back to pattern", "文型に戻る", () => { _drillIndex = -1; DrawPattern(); }, UiKit.Kind.Primary, "done");
            UiKit.Place((RectTransform)done.transform, DW / 2f + 10f, 330f, 220f, 52f);
        }
    }

    /// <summary>Japanese read-aloud: browser speech synthesis on WebGL (Plugins/WebGL/NihongoPatternSpeech.jslib); hidden elsewhere.</summary>
    public static class PatternSpeech
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern int NL_PatternSpeak(string text, string lang);
        public static bool Available => true;
        public static void Speak(string text) { try { NL_PatternSpeak(text ?? "", "ja-JP"); } catch (System.Exception) { } }
#else
        public static bool Available => false;
        public static void Speak(string text) { }
#endif
    }
}
