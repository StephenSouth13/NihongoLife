using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Course;
using NihongoLife.StudyModes.Russian;
using NihongoLife.StudyModes.Video;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// The Russian path: Cyrillic → pronunciation → A1, A2, B1, B2 units → TORFL (ТРКИ) practice sets and original
    /// exam-format mock tests with answer keys. Each step has a video slot for a YouTube link from @NihongoLifeGame
    /// (empty until the video is published; filled slots play in the in-game video player).
    /// </summary>
    public sealed class RussianPathPopup : MenuPopupBase
    {
        private const float W = 1320f, H = 820f;
        private RectTransform _stages, _steps, _main;
        private ScrollRect _stepScroll, _mainScroll;
        private readonly Dictionary<string, int> _answers = new();
        private readonly HashSet<string> _revealed = new();

        public RuPathStage Stage { get; private set; }
        public RuPathStep Step { get; private set; }
        public bool Submitted { get; private set; }
        public int LastScore { get; private set; } = -1;
        public IReadOnlyDictionary<string, int> Answers => _answers;

        protected override bool LocksGameplay => true;
        protected override Vector2 CardSize => new Vector2(W, H);
        protected override void GetTitle(out string vi, out string en, out string ja)
        { vi = "Lộ trình tiếng Nga · đến TORFL"; en = "Russian path · to TORFL"; ja = "ロシア語コース・TORFLまで"; }

        public static RussianPathPopup Open(GameObject host = null)
        {
            var popup = ReferenceUi.Open<RussianPathPopup>(host);
            popup.Show();
            return popup;
        }

        protected override void Build(RectTransform card)
        {
            _stages = ReferenceUi.Row(card, "Stages", 8f);
            UiKit.Place(_stages, 36f, 104f, W - 72f, 46f);
            var left = UiKit.Rect(card, "StepScroll");
            UiKit.Place(left, 32f, 164f, 380f, H - 196f);
            _steps = UiKit.Scroll(left, "Scroll", out _stepScroll, 6f, new RectOffset(2, 12, 2, 12));
            var right = UiKit.Rect(card, "MainScroll");
            UiKit.Place(right, 432f, 164f, W - 464f, H - 196f);
            _main = UiKit.Scroll(right, "Scroll", out _mainScroll, 12f, new RectOffset(4, 16, 4, 24));
        }

        protected override void OnOpened()
        {
            var path = RussianCourse.Path;
            Stage ??= path.stages.FirstOrDefault();
            Step ??= Stage?.steps.FirstOrDefault();
            Rebuild();
        }

        protected override void OnLanguageApplied() { if (IsOpen) Rebuild(); }

        public void SelectStage(string id)
        {
            Stage = RussianCourse.Path.stages.FirstOrDefault(s => s.id == id) ?? Stage;
            SelectStep(Stage?.steps.FirstOrDefault()?.id);
        }

        public void SelectStep(string id)
        {
            var step = Stage?.steps.FirstOrDefault(s => s.id == id) ?? RussianCourse.Path.Steps.FirstOrDefault(s => s.id == id);
            if (step == null) return;
            Stage = RussianCourse.Path.stages.FirstOrDefault(s => s.steps.Contains(step)) ?? Stage;
            Step = step;
            _answers.Clear();
            _revealed.Clear();
            Submitted = false;
            LastScore = -1;
            Rebuild();
            if (_mainScroll != null) _mainScroll.verticalNormalizedPosition = 1f;
        }

        public void Answer(string questionId, int choice) { if (Submitted) return; _answers[questionId] = choice; Rebuild(); }

        public int Submit()
        {
            var mock = Step?.kind == "mock" ? RussianCourse.Mock(Step.@ref) : null;
            var lesson = Step?.kind == "pronunciation" ? RussianCourse.Path.Pronunciation(Step.@ref) : null;
            var items = mock != null ? mock.Questions.ToList() : lesson?.quiz ?? new List<RuQuizItem>();
            LastScore = items.Count(q => _answers.TryGetValue(QuizId(q), out int a) && a == q.answer);
            Submitted = true;
            Rebuild();
            return LastScore;
        }

        public void Retry() { _answers.Clear(); Submitted = false; LastScore = -1; Rebuild(); }
        public void Reveal(string key) { if (!_revealed.Add(key)) _revealed.Remove(key); Rebuild(); }

        public bool PlayVideo()
        {
            if (Step?.video == null || !Step.video.Filled) return false;
            RussianCourse.RegisterVideos();
            var player = gameObject.GetComponent<InGameVideoPlayerUI>();
            if (player == null) player = gameObject.AddComponent<InGameVideoPlayerUI>();
            player.Initialize(Font);
            Hide();
            player.Show();
            player.Select(Step.video.id);
            return true;
        }

        private string QuizId(RuQuizItem q) => string.IsNullOrEmpty(q.id) ? Step.@ref + "_" + q.stem.GetHashCode() : q.id;

        private void Rebuild()
        {
            if (_main == null) return;
            var path = RussianCourse.Path;
            UiKit.Clear(_stages);
            foreach (var s in path.stages)
            {
                string id = s.id;
                UiKit.Size(UiKit.Button(_stages, "Stage_" + s.id, s.label?.Current ?? s.id, () => SelectStage(id), s == Stage ? UiKit.Kind.Primary : UiKit.Kind.Secondary, null, 15f), 190f, 42f);
            }
            UiKit.Clear(_steps);
            int n = 0;
            foreach (var step in Stage?.steps ?? new List<RuPathStep>())
            {
                string id = step.id;
                string icon = step.kind switch { "alphabet" => "abc", "pronunciation" => "record_voice_over", "mock" => "assignment", "cert" => "workspace_premium", _ => "menu_book" };
                var b = UiKit.Button(_steps, "Step_" + step.id, $"{++n}. {step.title?.Current}", () => SelectStep(id), step == Step ? UiKit.Kind.Primary : UiKit.Kind.Quiet, icon, 14f);
                UiKit.Size(b, -1f, 48f);
                var label = UiKit.LabelOf(b);
                if (label != null) label.alignment = TextAlignmentOptions.MidlineLeft;
            }
            UiKit.Clear(_main);
            if (Step == null) { ReferenceUi.Text(_main, "Empty", Loc.T("Chưa có nội dung.", "No content yet.", "内容はまだありません。"), 18f, UiTheme.TextSoft); return; }

            var head = ReferenceUi.Panel(_main, "Head", UiTheme.Surface);
            ReferenceUi.Text(head, "StepTitle", Step.title?.Current, 22f, UiTheme.Gold, FontStyles.Bold);
            VideoSlot(head);

            switch (Step.kind)
            {
                case "pronunciation": Pronunciation(path.Pronunciation(Step.@ref)); break;
                case "mock": Mock(RussianCourse.Mock(Step.@ref)); break;
                default:
                    ReferenceUi.Text(head, "Where", Step.kind == "cert"
                        ? Loc.T("Bộ đề luyện này nằm trong Trường học → Chứng chỉ.", "This practice set is in School → Certificates.", "この練習セットは「学校 → 検定」にあります。")
                        : Loc.T("Bài học này nằm trong Trường học (tiếng Nga): từ vựng, ngữ pháp, bài tập và phát âm.", "This lesson is in School (Russian): vocabulary, grammar, exercises and audio.", "このレッスンは「学校（ロシア語）」にあります：単語・文法・練習・音声。"), 15f, UiTheme.TextSoft);
                    UiKit.Size(UiKit.Button(head, "OpenSchool", "Mở Trường học", "Open School", "学校を開く", OpenSchool, UiKit.Kind.Primary, "cast_for_education", 15f), 240f, 46f);
                    break;
            }
            Canvas.ForceUpdateCanvases();
        }

        private void OpenSchool()
        {
            var school = gameObject.GetComponent<CoursePopup>();
            if (school == null) school = gameObject.AddComponent<CoursePopup>();
            school.Initialize(Font);
            Hide();
            school.Show();
        }

        private void VideoSlot(Transform parent)
        {
            var row = ReferenceUi.Row(parent, "Video", 10f);
            bool filled = Step.video != null && Step.video.Filled;
            var b = UiKit.Button(row, "PlayVideo", "Xem video bài học", "Watch the lesson video", "レッスン動画を見る", () => PlayVideo(), filled ? UiKit.Kind.Danger : UiKit.Kind.Quiet, "smart_display", 15f);
            b.interactable = filled;
            UiKit.Size(b, 260f, 44f);
            var note = UiKit.Label(row, "VideoNote", filled ? "YouTube · @NihongoLifeGame"
                : Loc.T("Video sẽ được thêm từ kênh @NihongoLifeGame.", "The video will be added from the @NihongoLifeGame channel.", "動画は@NihongoLifeGameチャンネルから追加予定です。"), 14f, UiTheme.Muted, FontStyles.Italic);
            UiKit.Size(note, 520f, 44f);
        }

        private void Pronunciation(RuPronunciationLesson lesson)
        {
            if (lesson == null) return;
            var p = ReferenceUi.Panel(_main, "Tip", UiTheme.Surface);
            ReferenceUi.Text(p, "TipText", lesson.tip?.Current, 16f, UiTheme.Text);
            var ex = string.Join("\n", lesson.examples.Select(e => $"<b><size=120%>{e.ru}</size></b>  —  {e.gloss?.Current}"));
            ReferenceUi.Text(p, "Examples", ex, 15f, UiTheme.TextSoft);
            Quiz(_main, "Quiz", Loc.T("Kiểm tra nhanh", "Quick check", "確認クイズ"), lesson.quiz);
            Results(lesson.quiz.Count, 0.66f);
        }

        private void Mock(RuMockTest test)
        {
            if (test == null) return;
            var info = ReferenceUi.Panel(_main, "Info", UiTheme.Surface);
            ReferenceUi.Text(info, "Meta", Loc.F("Thời gian gợi ý: {0} phút · {1} câu trắc nghiệm + Viết + Nói", "Suggested time: {0} min · {1} multiple-choice items + Writing + Speaking", "目安時間：{0}分・選択問題{1}問＋作文＋会話", test.minutes, test.Questions.Count()), 15f, UiTheme.TextSoft);
            ReferenceUi.Text(info, "Disclaimer", test.disclaimer?.Current, 13.5f, UiTheme.Muted, FontStyles.Italic);
            foreach (var s in test.sections)
            {
                var sec = ReferenceUi.Panel(_main, "Section_" + s.id, UiTheme.Surface);
                ReferenceUi.Text(sec, "Label", $"<b>{s.label?.Current}</b>  ·  <i>{s.directions}</i>", 17f, UiTheme.Gold);
                if (!string.IsNullOrEmpty(s.passage)) ReferenceUi.Text(sec, "Passage", s.passage, 15.5f, UiTheme.Text);
                if (!string.IsNullOrEmpty(s.script))
                    ReferenceUi.Text(sec, "Script", $"<color={UiTheme.Hex(UiTheme.Muted)}>{Loc.T("Lời thoại (đọc to hoặc nhờ bạn đọc):", "Script (read aloud or ask a friend to):", "スクリプト（音読するか友人に読んでもらう）：")}</color>\n{s.script}", 15f, UiTheme.TextSoft);
                Quiz(sec, s.id, null, s.questions);
            }
            Results(test.Questions.Count(), test.passRatio);

            var w = ReferenceUi.Panel(_main, "Writing", UiTheme.Surface);
            ReferenceUi.Text(w, "Label", $"<b>{Loc.T("Viết", "Writing", "作文")}</b> · {test.writing?.task?.Current}", 17f, UiTheme.Gold);
            ReferenceUi.Text(w, "Check", string.Join("\n", (test.writing?.checklist ?? new List<NihongoLife.Exam.Ielts.IeltsText>()).Select(c => "\u2610 " + c.Current)), 14.5f, UiTheme.TextSoft);
            UiKit.Size(UiKit.Button(w, "RevealWriting", _revealed.Contains("w") ? Loc.T("Ẩn bài mẫu", "Hide the model", "模範を隠す") : Loc.T("Xem bài mẫu", "Show the model answer", "模範解答を見る"), () => Reveal("w"), UiKit.Kind.Accent, "visibility", 14.5f), 260f, 42f);
            if (_revealed.Contains("w")) ReferenceUi.Text(w, "Model", test.writing?.model, 15.5f, UiTheme.Text);

            var sp = ReferenceUi.Panel(_main, "Speaking", UiTheme.Surface);
            ReferenceUi.Text(sp, "Label", $"<b>{Loc.T("Nói", "Speaking", "会話")}</b>", 17f, UiTheme.Gold);
            for (int i = 0; i < test.speaking.Count; i++)
            {
                string key = "s" + i;
                ReferenceUi.Text(sp, "Prompt_" + i, $"{i + 1}. {test.speaking[i].prompt}", 15.5f, UiTheme.Text);
                UiKit.Size(UiKit.Button(sp, "RevealSpeaking_" + i, _revealed.Contains(key) ? Loc.T("Ẩn câu mẫu", "Hide the sample", "例を隠す") : Loc.T("Xem câu trả lời mẫu", "Show a sample answer", "解答例を見る"), () => Reveal(key), UiKit.Kind.Ghost, "visibility", 14f), 260f, 40f);
                if (_revealed.Contains(key)) ReferenceUi.Text(sp, "Sample_" + i, test.speaking[i].sample, 15f, UiTheme.TextSoft, FontStyles.Italic);
            }
        }

        private void Quiz(Transform parent, string name, string title, List<RuQuizItem> items)
        {
            var box = parent == (Transform)_main ? ReferenceUi.Panel(_main, name, UiTheme.Surface) : (RectTransform)parent;
            if (!string.IsNullOrEmpty(title)) ReferenceUi.Text(box, "QuizTitle", $"<b>{title}</b>", 17f, UiTheme.Gold);
            for (int i = 0; i < items.Count; i++)
            {
                var q = items[i];
                string id = QuizId(q);
                _answers.TryGetValue(id, out int picked);
                bool answered = _answers.ContainsKey(id);
                ReferenceUi.Text(box, "Stem_" + id, $"<b>{i + 1}.</b> {q.stem}", 16f, UiTheme.Text);
                var row = ReferenceUi.Row(box, "Choices_" + id, 8f);
                for (int c = 0; c < q.choices.Length; c++)
                {
                    int choice = c;
                    var kind = UiKit.Kind.Secondary;
                    if (Submitted && c == q.answer) kind = UiKit.Kind.Success;
                    else if (Submitted && answered && c == picked) kind = UiKit.Kind.Danger;
                    else if (answered && c == picked) kind = UiKit.Kind.Primary;
                    var b = UiKit.Button(row, $"Choice_{id}_{c}", $"{(char)('A' + c)}. {q.choices[c]}", () => Answer(id, choice), kind, null, 14.5f);
                    UiKit.Size(b, -1f, 40f, 1f);
                }
                row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
                if (Submitted && q.explanation != null)
                    ReferenceUi.Text(box, "Key_" + id, $"<color={UiTheme.Hex(UiTheme.Teal)}>{Loc.T("Đáp án", "Key", "正解")}: {(char)('A' + q.answer)}</color> — {q.explanation.Current}", 14f, UiTheme.TextSoft);
                else if (Submitted)
                    ReferenceUi.Text(box, "Key_" + id, $"<color={UiTheme.Hex(UiTheme.Teal)}>{Loc.T("Đáp án", "Key", "正解")}: {(char)('A' + q.answer)}</color>", 14f, UiTheme.TextSoft);
            }
        }

        private void Results(int total, float passRatio)
        {
            var row = ReferenceUi.Row(_main, "Submit", 12f);
            if (!Submitted)
                UiKit.Size(UiKit.Button(row, "Check", "Nộp và xem đáp án", "Submit and show the key", "提出して解答を見る", () => Submit(), UiKit.Kind.Primary, "task_alt", 15.5f), 300f, 48f);
            else
            {
                bool pass = total > 0 && LastScore >= Mathf.CeilToInt(passRatio * total);
                UiKit.Label(row, "Score", Loc.F("Điểm: {0}/{1}", "Score: {0}/{1}", "得点：{0}/{1}", LastScore, total) + "  " +
                    (pass ? Loc.T("· Đạt ngưỡng luyện tập", "· Practice pass", "・練習合格") : Loc.T("· Chưa đạt — ôn lại và thử lại", "· Not yet — review and try again", "・不合格—復習して再挑戦")), 18f, pass ? UiTheme.Good : UiTheme.Warn, FontStyles.Bold);
                UiKit.Size(UiKit.Button(row, "Retry", "Làm lại", "Try again", "やり直す", Retry, UiKit.Kind.Secondary, "replay", 15f), 180f, 46f);
            }
        }
    }
}
