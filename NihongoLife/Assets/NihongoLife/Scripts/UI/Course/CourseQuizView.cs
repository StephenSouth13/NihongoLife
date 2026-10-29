using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Course;
using NihongoLife.StudyModes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    public sealed class CourseQuizOptions
    {
        public string Title = "";
        /// <summary>LearningProgressStore key the best score is saved under (null = not saved).</summary>
        public string StoreKey;
        /// <summary>Learning language of the questions (for glosses).</summary>
        public string Target = "en";
        public Color Accent = UiTheme.Ielts;
        /// <summary>The host draws its own result page (placement).</summary>
        public bool SuppressSummary;
        /// <summary>Shows an "I don't know" button that counts as wrong (placement).</summary>
        public bool AllowDontKnow;
        /// <summary>0 = no pass mark.</summary>
        public double PassRatio;
        public int TimeLimitSeconds;
        public Action<CourseQuizResult> OnFinished;
        public Action OnExit;
    }

    public sealed class CourseQuizResult
    {
        public int Correct, Total;
        public bool TimedOut, NewBest;
        public List<CourseQuestion> Mistakes = new List<CourseQuestion>();
        public Dictionary<string, bool> ById = new Dictionary<string, bool>();
        public int Stars => PatternDrills.Stars(Correct, Total);
        public double Ratio => Total <= 0 ? 0 : (double)Correct / Total;
    }

    /// <summary>
    /// One question at a time for every CourseQuestion kind (choice, typing, ordering, matching, true/false), with instant feedback,
    /// an optional countdown, a result page (stars, pass mark, retry mistakes) and a review of the mistakes. A plain class: the host popup owns the rectangle.
    /// </summary>
    public sealed class CourseQuizView
    {
        private enum CardState { Normal, Right, Wrong, Dim }

        private readonly RectTransform _root;
        private readonly float _w, _h;
        private readonly CourseQuizOptions _o;
        private List<CourseQuestion> _qs;
        private int _index, _correct;
        private bool _answered, _lastCorrect, _showScript, _record = true, _started;
        private string _lastIllu;
        private string _chosen, _typed;
        private readonly List<CourseQuestion> _mistakes = new List<CourseQuestion>();
        private readonly Dictionary<string, bool> _byId = new Dictionary<string, bool>();
        private readonly List<string> _built = new List<string>();
        private readonly List<int> _used = new List<int>();
        private readonly HashSet<string> _matched = new HashSet<string>();
        private int _matchSel = -1;
        private bool _matchMistake;
        private float _timeLeft;
        private int _shownSeconds = -1;
        private TextMeshProUGUI _timer;
        private TMP_InputField _input;

        public CourseQuizResult Result { get; private set; }
        public bool Finished { get; private set; }
        public int Index => _index;
        public int Count => _qs.Count;
        public bool Answered => _answered;
        /// <summary>Concept id of the picture unlocked by the last correct answer, or null.</summary>
        public string LastIllustration => _lastIllu;
        public bool LastCorrect => _lastCorrect;
        public int CorrectCount => _correct;
        public CourseQuestion Current => !Finished && _index >= 0 && _index < _qs.Count ? _qs[_index] : null;
        public float TimeLeft => _timeLeft;

        public CourseQuizView(RectTransform root, float width, float height, List<CourseQuestion> questions, CourseQuizOptions options)
        {
            _root = root; _w = width; _h = height; _o = options ?? new CourseQuizOptions();
            _qs = questions ?? new List<CourseQuestion>();
            _timeLeft = _o.TimeLimitSeconds;
        }

        public void Start() { _started = true; BeginQuestion(); Draw(); }

        private void BeginQuestion()
        {
            _answered = false; _chosen = null; _typed = null; _showScript = false; _matchSel = -1; _matchMistake = false;
            _built.Clear(); _used.Clear(); _matched.Clear(); _input = null;
        }

        // ---------------- frame ----------------

        public void Draw()
        {
            UiKit.Clear(_root);
            _timer = null; _input = null;
            if (Finished) { if (!_o.SuppressSummary) DrawSummary(); return; }
            if (Current == null) return;
            DrawHeader();
            var host = UiKit.Rect(_root, "BodyHost");
            UiKit.Place(host, 0f, 72f, _w, _h - 72f);
            var content = CourseUi.ScrollIn(host);
            var body = CourseUi.Body(content);
            float y = DrawQuestion(body, _w - 4f);
            CourseUi.EndBody(body, y + 24f);
        }

        private void DrawHeader()
        {
            var back = UiKit.Button(_root, "Back", "Quay lại", "Back", "戻る", () => _o.OnExit?.Invoke(), UiKit.Kind.Ghost, "arrow_back", 15f);
            UiKit.Place((RectTransform)back.transform, 0f, 0f, 150f, 40f);
            var title = UiKit.Label(_root, "Title", _o.Title, 17f, UiTheme.TextSoft, FontStyles.Bold);
            UiKit.Place(title.rectTransform, 164f, 0f, _w - 164f - 300f, 40f);
            var progress = UiKit.Label(_root, "Progress", (_index + 1) + "/" + _qs.Count, 17f, UiTheme.Muted, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            UiKit.Place(progress.rectTransform, _w - 120f, 0f, 120f, 40f);
            if (_o.TimeLimitSeconds > 0)
            {
                _timer = UiKit.Label(_root, "Timer", "", 17f, _timeLeft < 60f ? UiTheme.Bad : UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
                UiKit.Place(_timer.rectTransform, _w - 260f, 0f, 130f, 40f);
                _shownSeconds = -1; UpdateTimer();
            }
            var bar = UiKit.ProgressBar(_root, "Bar", _o.Accent, UiTheme.Sunken);
            UiKit.Place(bar.parent as RectTransform ?? bar, 0f, 50f, _w, 8f);
            UiKit.SetProgress(bar, (float)_index / Mathf.Max(1, _qs.Count));
        }

        private void UpdateTimer()
        {
            if (_timer == null) return;
            int s = Mathf.CeilToInt(Mathf.Max(0f, _timeLeft));
            if (s == _shownSeconds) return;
            _shownSeconds = s;
            _timer.text = "⏱ " + CourseUi.Mmss(_timeLeft);
            _timer.color = _timeLeft < 60f ? UiTheme.Bad : UiTheme.Gold;
        }

        /// <summary>Counts the exam clock down (the host calls this every frame while the quiz is open).</summary>
        public void Tick(float dt)
        {
            if (!_started || Finished || _o.TimeLimitSeconds <= 0) return;
            _timeLeft -= dt;
            if (_timeLeft <= 0f) { ForceTimeout(); return; }
            UpdateTimer();
        }

        public void ForceTimeout()
        {
            if (Finished) return;
            _timeLeft = 0f;
            Finish(true);
        }

        // ---------------- question ----------------

        private float DrawQuestion(RectTransform body, float bw)
        {
            var q = Current;
            float y = 8f;
            if (!string.IsNullOrEmpty(q.Context) || q.ContextIsListening) y = DrawContext(body, q, bw, y);
            if (!q.Prompt.IsEmpty)
            {
                CourseUi.Para(body, "Prompt", q.Prompt.Resolve(), 16f, UiTheme.Gold, 0f, y, bw, out float ph, FontStyles.Bold);
                y += ph + 8f;
            }
            if (!q.Stem.IsEmpty)
            {
                float sw = bw - (CanSpeakStem(q) ? 56f : 0f);
                CourseUi.Para(body, "Stem", q.Stem.Resolve(), 26f, UiTheme.Text, 0f, y, sw, out float sh, FontStyles.Bold);
                if (CanSpeakStem(q)) CourseUi.SpeakButton(body, q.SpeechText, q.SpeechLocale ?? CourseUi.LocaleOf(q.Target), bw - 44f, y);
                y += Mathf.Max(sh, 40f) + 14f;
            }
            switch (q.Kind)
            {
                case CourseQuestionKind.Choice: y = DrawChoices(body, q, bw, y); break;
                case CourseQuestionKind.Typing: y = DrawTyping(body, q, bw, y); break;
                case CourseQuestionKind.Ordering: y = DrawOrdering(body, q, bw, y); break;
                case CourseQuestionKind.Matching: y = DrawMatching(body, q, bw, y); break;
                case CourseQuestionKind.TrueFalse: y = DrawTrueFalse(body, q, bw, y); break;
            }
            if (!_answered && _o.AllowDontKnow)
            {
                var skip = UiKit.Button(body, "DontKnow", "Tôi không biết", "I don't know", "わからない", DontKnow, UiKit.Kind.Quiet, "help", 16f);
                UiKit.Place((RectTransform)skip.transform, 0f, y + 10f, 240f, 46f);
                y += 64f;
            }
            if (_answered) y = DrawFeedback(body, q, bw, y + 6f);
            return y;
        }

        private static bool CanSpeakStem(CourseQuestion q) => PatternSpeech.Available && !string.IsNullOrEmpty(q.StemAudio);

        private float DrawContext(RectTransform body, CourseQuestion q, float bw, float y)
        {
            var card = UiKit.Card(body, "Context", UiTheme.Surface, 16, 0f, false);
            bool listening = q.ContextIsListening;
            bool speech = PatternSpeech.Available;
            bool showText = !listening || _showScript || !speech;
            float inner = bw - 40f, cy = 16f;
            if (!string.IsNullOrEmpty(q.ContextTitle) && !listening)
            {
                CourseUi.Para(card.transform, "CtxTitle", q.ContextTitle, 18f, UiTheme.Gold, 20f, cy, inner, out float th, FontStyles.Bold);
                cy += th + 6f;
            }
            if (listening)
            {
                var head = UiKit.Label(card.transform, "Listen", "Nghe và trả lời", "Listen and answer", "聞いて答えましょう", 17f, UiTheme.Gold, FontStyles.Bold);
                UiKit.Place(head.rectTransform, 20f, cy, inner - 340f, 40f);
                if (speech)
                {
                    var play = UiKit.Button(card.transform, "Play", "Nghe", "Play audio", "再生", () => PatternSpeech.Speak(q.SpeechText, q.SpeechLocale ?? CourseUi.LocaleOf(q.Target)), UiKit.Kind.Primary, "volume_up", 15f);
                    UiKit.Place((RectTransform)play.transform, bw - 20f - 320f, cy, 150f, 40f);
                    var show = UiKit.Button(card.transform, "Script", _showScript ? "Ẩn bản ghi" : "Hiện bản ghi", _showScript ? "Hide script" : "Show script", _showScript ? "スクリプトを隠す" : "スクリプトを表示",
                        () => { _showScript = !_showScript; Draw(); }, UiKit.Kind.Ghost, null, 14f);
                    UiKit.Place((RectTransform)show.transform, bw - 20f - 160f, cy, 160f, 40f);
                }
                cy += 46f;
                if (!speech)
                {
                    CourseUi.Para(card.transform, "NoAudio", Loc.T("Giọng đọc chỉ có trong bản web (trình duyệt đọc to). Ở đây bạn đọc bản ghi bên dưới.",
                        "Spoken audio plays in the web build (the browser reads it aloud). Here you read the script below.",
                        "音声はWeb版（ブラウザの読み上げ）で再生されます。ここでは下のスクリプトを読んでください。"), 14f, UiTheme.Muted, 20f, cy, inner, out float nh);
                    cy += nh + 6f;
                }
            }
            if (showText && !string.IsNullOrEmpty(q.Context))
            {
                CourseUi.Para(card.transform, "CtxText", q.Context, 19f, UiTheme.Text, 20f, cy, inner, out float ch);
                cy += ch + 6f;
            }
            UiKit.Place(card.rectTransform, 0f, y, bw, cy + 14f);
            return y + cy + 14f + 14f;
        }

        private static void StyleCard(Image card, CardState state)
        {
            switch (state)
            {
                case CardState.Right: card.color = UiKit.Alpha(UiTheme.Good, 0.38f); break;
                case CardState.Wrong: card.color = UiKit.Alpha(UiTheme.Bad, 0.38f); break;
                case CardState.Dim: card.color = UiTheme.Surface; break;
                default: card.color = UiTheme.Raised; break;
            }
        }

        private float TextCard(RectTransform parent, string name, string text, float x, float y, float w, CardState state, bool interactable, Action click, float size = 20f, float minH = 58f)
        {
            var card = UiKit.Card(parent, name, UiTheme.Raised, 14, 0f, true);
            StyleCard(card, state);
            CourseUi.Para(card.transform, "Text", text, size, state == CardState.Dim ? UiTheme.Muted : UiTheme.Text, 18f, 12f, w - 36f, out float th, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            float h = Mathf.Max(minH, th + 26f);
            UiKit.Place(card.rectTransform, x, y, w, h);
            if (interactable && click != null)
            {
                var b = card.gameObject.AddComponent<Button>();
                b.targetGraphic = card;
                var colors = b.colors; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f); colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f); b.colors = colors;
                b.onClick.AddListener(() => click());
            }
            return h;
        }

        private float DrawChoices(RectTransform body, CourseQuestion q, float bw, float y)
        {
            bool twoCols = q.Choices.All(c => c.Text.Resolve().Length <= 26);
            float cw = twoCols ? (bw - 16f) / 2f : bw;
            float rowH = 0f, x = 0f;
            for (int i = 0; i < q.Choices.Count; i++)
            {
                var c = q.Choices[i];
                var state = CardState.Normal;
                if (_answered)
                {
                    if (c.Id == q.AnswerId) state = CardState.Right;
                    else if (c.Id == _chosen) state = CardState.Wrong;
                    else state = CardState.Dim;
                }
                string id = c.Id;
                float h = TextCard(body, "Choice" + i, c.Text.Resolve(), x, y, cw, state, !_answered, () => AnswerChoice(id));
                rowH = Mathf.Max(rowH, h);
                if (twoCols && i % 2 == 0 && i + 1 < q.Choices.Count) { x = cw + 16f; }
                else { y += rowH + 12f; rowH = 0f; x = 0f; }
            }
            if (rowH > 0f) y += rowH + 12f;
            return y;
        }

        private float DrawTyping(RectTransform body, CourseQuestion q, float bw, float y)
        {
            var bg = UiKit.Card(body, "InputBg", _answered ? UiKit.Alpha(_lastCorrect ? UiTheme.Good : UiTheme.Bad, 0.35f) : UiTheme.Paper, 14, 0f, true);
            UiKit.Place(bg.rectTransform, 0f, y, bw, 60f);
            _input = BuildInput(bg.rectTransform);
            _input.text = _typed ?? "";
            _input.interactable = !_answered;
            _input.onSubmit.AddListener(_ => CheckTyping());
            y += 74f;
            if (!_answered)
            {
                var check = UiKit.Button(body, "Check", "Kiểm tra", "Check", "確認", CheckTyping, UiKit.Kind.Primary, "done");
                UiKit.Place((RectTransform)check.transform, bw - 200f, y, 200f, 50f);
                y += 62f;
            }
            return y;
        }

        private TMP_InputField BuildInput(RectTransform parent)
        {
            var go = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            UiKit.Fill((RectTransform)go.transform, 16f, 0f, 16f, 0f);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            var input = go.GetComponent<TMP_InputField>();
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 200;
            input.richText = false;
            input.caretColor = UiTheme.PaperInk;
            input.customCaretColor = true;
            input.caretWidth = 2;
            input.selectionColor = new Color(0.98f, 0.82f, 0.32f, 0.45f);
            var viewport = UiKit.Rect(go.transform, "Viewport");
            UiKit.Fill(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            input.textViewport = viewport;
            TextMeshProUGUI Make(string name, Color color, FontStyles style)
            {
                var t = UiKit.Label(viewport, name, "", 24f, color, style, TextAlignmentOptions.MidlineLeft, false);
                UiKit.Fill(t.rectTransform);
                t.richText = false;
                return t;
            }
            input.textComponent = Make("Text", UiTheme.PaperInk, FontStyles.Bold);
            var ph = Make("Placeholder", UiTheme.PaperMuted, FontStyles.Italic);
            ph.text = Loc.T("Gõ câu trả lời…", "Type your answer…", "答えを入力…");
            input.placeholder = ph;
            return input;
        }

        private float DrawOrdering(RectTransform body, CourseQuestion q, float bw, float y)
        {
            var line = UiKit.Card(body, "Built", UiTheme.Sunken, 14, 0f, false);
            string sep = q.TokenSeparator ?? " ";
            string built = _built.Count == 0 ? Loc.T("Chạm vào các thẻ bên dưới.", "Tap the cards below.", "下のカードをタップ。") : string.Join(sep, _built);
            CourseUi.Para(line.transform, "Text", built, 24f, _built.Count == 0 ? UiTheme.Muted : UiTheme.Text, 18f, 14f, bw - 36f, out float bh, FontStyles.Bold);
            float lh = Mathf.Max(72f, bh + 28f);
            UiKit.Place(line.rectTransform, 0f, y, bw, lh);
            y += lh + 16f;
            var shuffled = CourseUi.Shuffled(q.Tokens, q.Id);
            if (shuffled.SequenceEqual(q.AnswerTokens) && shuffled.Count > 1) { shuffled.Reverse(); }
            float x = 0f, rowY = y;
            for (int i = 0; i < shuffled.Count; i++)
            {
                int idx = i; string tok = shuffled[i];
                float w = Mathf.Clamp(tok.Length * 15f + 50f, 84f, bw);
                if (x + w > bw) { x = 0f; rowY += 64f; }
                var b = UiKit.Button(body, "Token" + i, tok, () => TapToken(idx, tok), UiKit.Kind.Secondary, null, 20f);
                UiKit.Place((RectTransform)b.transform, x, rowY, w, 54f);
                b.interactable = !_answered && !_used.Contains(i);
                x += w + 10f;
            }
            y = rowY + 74f;
            if (!_answered)
            {
                var undo = UiKit.Button(body, "Undo", "Xóa từ cuối", "Undo", "1つ戻す", UndoToken, UiKit.Kind.Ghost, "replay", 15f);
                UiKit.Place((RectTransform)undo.transform, 0f, y, 180f, 48f);
                var reset = UiKit.Button(body, "Reset", "Làm lại", "Reset", "やり直し", () => { _built.Clear(); _used.Clear(); Draw(); }, UiKit.Kind.Ghost, "refresh", 15f);
                UiKit.Place((RectTransform)reset.transform, 192f, y, 160f, 48f);
                var check = UiKit.Button(body, "Check", "Kiểm tra", "Check", "確認", CheckOrder, UiKit.Kind.Primary, "done");
                UiKit.Place((RectTransform)check.transform, bw - 200f, y, 200f, 48f);
                check.interactable = _built.Count == q.Tokens.Count;
                y += 60f;
            }
            return y;
        }

        private float DrawMatching(RectTransform body, CourseQuestion q, float bw, float y)
        {
            float cw = (bw - 24f) / 2f;
            var left = CourseUi.Shuffled(q.Pairs, q.Id + "L");
            var right = CourseUi.Shuffled(q.Pairs, q.Id + "R");
            float ly = y, ry = y;
            for (int i = 0; i < left.Count; i++)
            {
                int idx = i; var p = left[i];
                bool done = _matched.Contains(p.Id);
                var st = done ? CardState.Right : (_matchSel == i ? CardState.Wrong : CardState.Normal);
                if (_matchSel == i && !done) st = CardState.Normal;
                float h = TextCard(body, "Left" + i, p.Left.Resolve(), 0f, ly, cw, done ? CardState.Right : st, !_answered && !done, () => MatchTap(true, idx), 19f, 52f);
                if (_matchSel == i && !done) { var ring = UiKit.Border((RectTransform)body.Find("Left" + i), _o.Accent, 14); }
                ly += h + 10f;
            }
            for (int i = 0; i < right.Count; i++)
            {
                int idx = i; var p = right[i];
                bool done = _matched.Contains(p.Id);
                float h = TextCard(body, "Right" + i, p.Right.Resolve(), cw + 24f, ry, cw, done ? CardState.Right : CardState.Normal, !_answered && !done, () => MatchTap(false, idx), 19f, 52f);
                ry += h + 10f;
            }
            y = Mathf.Max(ly, ry) + 4f;
            if (_matchMistake && !_answered)
            {
                CourseUi.Para(body, "MatchHint", Loc.T("Có cặp chưa đúng — hãy thử lại.", "A pair did not match — try again.", "合わないペアがありました。もう一度。"), 15f, UiTheme.Warn, 0f, y, bw, out float mh);
                y += mh + 8f;
            }
            return y;
        }

        private float DrawTrueFalse(RectTransform body, CourseQuestion q, float bw, float y)
        {
            float cw = (bw - 16f) / 2f;
            var tState = !_answered ? CardState.Normal : (q.AnswerBool ? CardState.Right : (_chosen == "true" ? CardState.Wrong : CardState.Dim));
            var fState = !_answered ? CardState.Normal : (!q.AnswerBool ? CardState.Right : (_chosen == "false" ? CardState.Wrong : CardState.Dim));
            float h1 = TextCard(body, "True", Loc.T("Đúng", "True", "正しい"), 0f, y, cw, tState, !_answered, () => AnswerBool(true), 22f, 64f);
            float h2 = TextCard(body, "False", Loc.T("Sai", "False", "誤り"), cw + 16f, y, cw, fState, !_answered, () => AnswerBool(false), 22f, 64f);
            return y + Mathf.Max(h1, h2) + 14f;
        }

        private float DrawFeedback(RectTransform body, CourseQuestion q, float bw, float y)
        {
            var color = _lastCorrect ? UiTheme.Good : UiTheme.Bad;
            var fb = UiKit.Card(body, "Feedback", UiKit.Alpha(color, 0.16f), 16, 0f, false);
            float inner = bw - 40f, cy = 14f;
            var head = UiKit.Label(fb.transform, "Head", _lastCorrect ? Loc.T("Chính xác!", "Correct!", "正解！") : Loc.T("Chưa đúng", "Not quite", "おしい！"), 20f, color, FontStyles.Bold);
            UiKit.Place(head.rectTransform, 20f, cy, inner, 30f);
            cy += 34f;
            if (!_lastCorrect && q.Kind != CourseQuestionKind.Matching)
            {
                string ans = q.AnswerDisplay;
                if (!string.IsNullOrEmpty(ans))
                {
                    CourseUi.Para(fb.transform, "Answer", Loc.T("Đáp án: ", "Answer: ", "答え：") + ans, 19f, UiTheme.Text, 20f, cy, inner, out float ah, FontStyles.Bold);
                    cy += ah + 6f;
                }
                if (q.Kind == CourseQuestionKind.Typing && !string.IsNullOrEmpty(_typed))
                {
                    CourseUi.Para(fb.transform, "Typed", Loc.T("Bạn gõ: ", "You wrote: ", "あなたの答え：") + _typed, 16f, UiTheme.TextSoft, 20f, cy, inner, out float th);
                    cy += th + 6f;
                }
            }
            if (_lastCorrect && !string.IsNullOrEmpty(_lastIllu))
            {
                var sp = NihongoLife.Content.IllustrationService.GetSprite(_lastIllu);
                if (sp != null)
                {
                    var pic = new GameObject("Illustration", typeof(RectTransform), typeof(Image));
                    pic.transform.SetParent(fb.transform, false);
                    var pimg = pic.GetComponent<Image>(); pimg.sprite = sp; pimg.preserveAspect = true; pimg.raycastTarget = false;
                    UiKit.Place((RectTransform)pic.transform, 20f, cy, 84f, 84f);
                    var cap = UiKit.Label(fb.transform, "IlluCaption", Loc.T("Hình minh họa đã mở khóa trong từ điển", "Picture unlocked in the dictionary", "辞書のイラストが解放されました"), 15f, UiTheme.TextSoft);
                    UiKit.Place(cap.rectTransform, 118f, cy + 26f, inner - 100f, 30f);
                    cy += 92f;
                }
            }
            string gloss = q.Translation.IsEmpty ? null : q.Translation.Gloss(q.Target);
            if (!string.IsNullOrEmpty(gloss))
            {
                CourseUi.Para(fb.transform, "Gloss", gloss, 17f, UiTheme.Gold, 20f, cy, inner, out float gh);
                cy += gh + 6f;
            }
            string why = q.Explanation.IsEmpty ? null : q.Explanation.Resolve();
            if (!string.IsNullOrEmpty(why))
            {
                CourseUi.Para(fb.transform, "Why", why, 16f, UiTheme.TextSoft, 20f, cy, inner, out float wh);
                cy += wh + 6f;
            }
            UiKit.Place(fb.rectTransform, 0f, y, bw, cy + 54f);
            bool last = _index + 1 >= _qs.Count;
            var next = UiKit.Button(fb.transform, "Next", last ? "Xem kết quả" : "Câu tiếp", last ? "See results" : "Next", last ? "結果を見る" : "次へ", Next, UiKit.Kind.Primary, "arrow_forward");
            UiKit.Place((RectTransform)next.transform, bw - 20f - 220f, cy, 220f, 46f);
            return y + cy + 54f + 8f;
        }

        // ---------------- answering ----------------

        private void Resolve(bool ok)
        {
            if (_answered || Finished) return;
            _answered = true; _lastCorrect = ok;
            var q = Current;
            _lastIllu = null;
            if (ok && q.Kind != CourseQuestionKind.Matching)
            {
                try { _lastIllu = NihongoLife.Content.IllustrationService.UnlockFor(q.AnswerDisplay, q.Stem.Resolve(), q.Translation.IsEmpty ? null : q.Translation.Gloss(q.Target)); }
                catch (Exception e) { Debug.LogWarning("Illustration unlock failed: " + e.Message); }
            }
            if (ok) _correct++; else _mistakes.Add(q);
            _byId[q.Id] = ok;
            Draw();
        }

        public void AnswerChoice(string id)
        {
            var q = Current;
            if (q == null || _answered || q.Kind != CourseQuestionKind.Choice) return;
            _chosen = id;
            Resolve(CourseGrader.Choice(q, id));
        }

        public void AnswerTyping(string text)
        {
            var q = Current;
            if (q == null || _answered || q.Kind != CourseQuestionKind.Typing) return;
            _typed = text ?? "";
            Resolve(CourseGrader.Typing(q, _typed));
        }

        private void CheckTyping()
        {
            if (_input == null || _answered) return;
            if (string.IsNullOrWhiteSpace(_input.text)) return;
            AnswerTyping(_input.text);
        }

        public void AnswerBool(bool value)
        {
            var q = Current;
            if (q == null || _answered || q.Kind != CourseQuestionKind.TrueFalse) return;
            _chosen = value ? "true" : "false";
            Resolve(CourseGrader.TrueFalse(q, value));
        }

        public void TapToken(int index, string token)
        {
            var q = Current;
            if (q == null || _answered || q.Kind != CourseQuestionKind.Ordering || _used.Contains(index)) return;
            _used.Add(index); _built.Add(token);
            Draw();
        }

        private void UndoToken()
        {
            if (_built.Count == 0 || _answered) return;
            _built.RemoveAt(_built.Count - 1); _used.RemoveAt(_used.Count - 1);
            Draw();
        }

        public void CheckOrder()
        {
            var q = Current;
            if (q == null || _answered || q.Kind != CourseQuestionKind.Ordering || _built.Count != q.Tokens.Count) return;
            Resolve(CourseGrader.Ordering(q, _built));
        }

        /// <summary>Matching: tap a left card, then a right card.</summary>
        public void MatchTap(bool left, int index)
        {
            var q = Current;
            if (q == null || _answered || q.Kind != CourseQuestionKind.Matching) return;
            var l = CourseUi.Shuffled(q.Pairs, q.Id + "L");
            var r = CourseUi.Shuffled(q.Pairs, q.Id + "R");
            if (left)
            {
                if (_matched.Contains(l[index].Id)) return;
                _matchSel = _matchSel == index ? -1 : index;
                Draw();
                return;
            }
            if (_matchSel < 0 || _matched.Contains(r[index].Id)) return;
            if (l[_matchSel].Id == r[index].Id) _matched.Add(r[index].Id); else _matchMistake = true;
            _matchSel = -1;
            if (_matched.Count == q.Pairs.Count) Resolve(!_matchMistake); else Draw();
        }

        public void DontKnow()
        {
            var q = Current;
            if (q == null || _answered) return;
            Resolve(false);
        }

        public void Next()
        {
            if (!_answered || Finished) return;
            _index++;
            if (_index >= _qs.Count) { _index = _qs.Count - 1; Finish(false); return; }
            BeginQuestion();
            Draw();
        }

        /// <summary>Test hook: answers the current question right or wrong the way a player would.</summary>
        public void AnswerForTest(bool correct)
        {
            var q = Current;
            if (q == null || _answered) return;
            switch (q.Kind)
            {
                case CourseQuestionKind.Choice:
                    AnswerChoice(correct ? q.AnswerId : q.Choices.First(c => c.Id != q.AnswerId).Id); break;
                case CourseQuestionKind.Typing:
                    AnswerTyping(correct ? q.Accepted[0] : "zzzz qqqq"); break;
                case CourseQuestionKind.TrueFalse:
                    AnswerBool(correct ? q.AnswerBool : !q.AnswerBool); break;
                case CourseQuestionKind.Ordering:
                    {
                        var order = new List<string>(q.AnswerTokens);
                        if (!correct && order.Count > 1) order.Reverse();
                        var pool = CourseUi.Shuffled(q.Tokens, q.Id);
                        if (pool.SequenceEqual(q.AnswerTokens) && pool.Count > 1) pool.Reverse();
                        var used = new List<int>();
                        foreach (var tok in order)
                        {
                            int at = -1;
                            for (int i = 0; i < pool.Count; i++) if (!used.Contains(i) && pool[i] == tok) { at = i; break; }
                            if (at < 0) continue;
                            used.Add(at); TapToken(at, tok);
                        }
                        CheckOrder();
                        if (!_answered) Resolve(false);
                        break;
                    }
                case CourseQuestionKind.Matching:
                    {
                        var l = CourseUi.Shuffled(q.Pairs, q.Id + "L");
                        var r = CourseUi.Shuffled(q.Pairs, q.Id + "R");
                        if (!correct && l.Count > 1)
                        {
                            int wrongLeft = 0, wrongRight = r.FindIndex(p => p.Id != l[0].Id);
                            MatchTap(true, wrongLeft); MatchTap(false, wrongRight);
                        }
                        for (int i = 0; i < l.Count && !_answered; i++)
                        {
                            if (_matched.Contains(l[i].Id)) continue;
                            MatchTap(true, i);
                            MatchTap(false, r.FindIndex(p => p.Id == l[i].Id));
                        }
                        break;
                    }
            }
        }

        // ---------------- result ----------------

        private void Finish(bool timedOut)
        {
            if (Finished) return;
            if (!_answered && _index < _qs.Count && !_byId.ContainsKey(_qs[_index].Id) && timedOut) { _mistakes.Add(_qs[_index]); _byId[_qs[_index].Id] = false; }
            for (int i = _index + 1; i < _qs.Count && timedOut; i++) if (!_byId.ContainsKey(_qs[i].Id)) { _mistakes.Add(_qs[i]); _byId[_qs[i].Id] = false; }
            var res = new CourseQuizResult { Correct = _correct, Total = _qs.Count, TimedOut = timedOut, Mistakes = new List<CourseQuestion>(_mistakes), ById = new Dictionary<string, bool>(_byId) };
            if (_record && !string.IsNullOrEmpty(_o.StoreKey)) res.NewBest = LearningProgressStore.Shared.RecordResult(_o.StoreKey, res.Correct, res.Total);
            Result = res; Finished = true;
            _o.OnFinished?.Invoke(res);
            if (!_o.SuppressSummary) Draw();
        }

        public void Retry(bool mistakesOnly)
        {
            var list = mistakesOnly ? new List<CourseQuestion>(_mistakes) : new List<CourseQuestion>(_qs);
            if (list.Count == 0) return;
            _qs = list; _record = !mistakesOnly && _record;
            _index = 0; _correct = 0; _mistakes.Clear(); _byId.Clear(); Finished = false; Result = null;
            _timeLeft = _o.TimeLimitSeconds;
            BeginQuestion();
            Draw();
        }

        private void DrawSummary()
        {
            var r = Result;
            var host = UiKit.Rect(_root, "SummaryHost");
            UiKit.Place(host, 0f, 0f, _w, _h);
            var content = CourseUi.ScrollIn(host);
            var body = CourseUi.Body(content);
            float bw = _w - 4f, y = 10f;
            var stars = UiKit.Label(body, "Stars", CourseUi.Stars(r.Stars), 54f, UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(stars.rectTransform, 0f, y, bw, 70f); y += 74f;
            var score = UiKit.Label(body, "Score", r.Correct + " / " + r.Total + "  ·  " + Mathf.RoundToInt((float)r.Ratio * 100f) + "%", 34f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(score.rectTransform, 0f, y, bw, 46f); y += 52f;
            string line;
            Color lineColor = UiTheme.TextSoft;
            if (r.TimedOut) { line = Loc.T("Hết giờ — các câu chưa làm tính là sai.", "Time is up — unanswered questions count as wrong.", "時間切れ—未回答は不正解になります。"); lineColor = UiTheme.Warn; }
            else if (_o.PassRatio > 0)
            {
                bool pass = r.Ratio >= _o.PassRatio;
                line = pass ? Loc.F("Đạt! Mốc đạt là {0}%.", "Passed! The pass mark is {0}%.", "合格！合格ラインは{0}％です。", Mathf.RoundToInt((float)_o.PassRatio * 100f))
                            : Loc.F("Chưa đạt — mốc đạt là {0}%. Xem lại lỗi rồi thử lại.", "Not yet — the pass mark is {0}%. Review the mistakes and try again.", "もう少し—合格ラインは{0}％です。間違いを見直して再挑戦。", Mathf.RoundToInt((float)_o.PassRatio * 100f));
                lineColor = pass ? UiTheme.Good : UiTheme.Warn;
            }
            else line = r.Stars >= 3 ? Loc.T("Hoàn hảo!", "Perfect!", "パーフェクト！") : r.Stars == 2 ? Loc.T("Tốt lắm! Luyện thêm một chút nữa.", "Well done! A little more practice.", "よくできました！もう少し練習しましょう。") : Loc.T("Cố lên — làm lại sẽ tốt hơn.", "Keep going — another try will go better.", "がんばって—もう一度やればできます。");
            CourseUi.Para(body, "Line", line, 19f, lineColor, 0f, y, bw, out float lh, FontStyles.Bold, TextAlignmentOptions.Top); y += lh + 6f;
            if (r.NewBest)
            {
                var chip = UiKit.Chip(body, "Best", "Kỷ lục mới", "New best", "自己ベスト", UiKit.Alpha(UiTheme.Gold, 0.3f), UiTheme.Text, "star", 14f);
                UiKit.Place(chip, bw / 2f - 70f, y, 140f, 28f); y += 36f;
            }
            y += 8f;
            float bx = bw / 2f - (r.Mistakes.Count > 0 ? 340f : 190f);
            var retry = UiKit.Button(body, "Retry", "Làm lại", "Try again", "もう一度", () => Retry(false), UiKit.Kind.Secondary, "refresh");
            UiKit.Place((RectTransform)retry.transform, bx, y, 180f, 50f);
            if (r.Mistakes.Count > 0)
            {
                var retryM = UiKit.Button(body, "RetryMistakes", "Làm lại câu sai", "Retry mistakes", "間違いだけ再挑戦", () => Retry(true), UiKit.Kind.Accent, "replay", 16f);
                UiKit.Place((RectTransform)retryM.transform, bx + 192f, y, 280f, 50f);
                bx += 292f;
            }
            var done = UiKit.Button(body, "Done", "Xong", "Done", "完了", () => _o.OnExit?.Invoke(), UiKit.Kind.Primary, "check");
            UiKit.Place((RectTransform)done.transform, bx + 192f, y, 180f, 50f);
            y += 74f;
            if (r.Mistakes.Count > 0)
            {
                var head = UiKit.Label(body, "ReviewHead", "Các câu cần xem lại", "Questions to review", "見直す問題", 18f, UiTheme.Muted, FontStyles.Bold);
                UiKit.Place(head.rectTransform, 0f, y, bw, 28f); y += 34f;
                foreach (var q in r.Mistakes.Take(12))
                {
                    var card = UiKit.Card(body, "Review", UiTheme.Surface, 14, 0f, false);
                    string stem = q.Stem.IsEmpty ? q.Prompt.Resolve() : q.Stem.Resolve();
                    CourseUi.Para(card.transform, "Stem", stem, 17f, UiTheme.TextSoft, 18f, 10f, bw - 36f, out float sh);
                    float ch = 10f + sh + 4f;
                    string ans = q.AnswerDisplay;
                    if (!string.IsNullOrEmpty(ans))
                    {
                        CourseUi.Para(card.transform, "Ans", "✓ " + ans, 18f, UiTheme.Good, 18f, ch, bw - 36f, out float ah, FontStyles.Bold);
                        ch += ah + 4f;
                    }
                    UiKit.Place(card.rectTransform, 0f, y, bw, ch + 8f);
                    y += ch + 8f + 10f;
                }
            }
            CourseUi.EndBody(body, y + 20f);
        }
    }
}