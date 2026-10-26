using System;
using System.Collections.Generic;
using NihongoLife.Core;
using NihongoLife.English;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Interactive runner for the original A1-C1 general-English starter lessons.</summary>
    public sealed class EnglishPracticeUI : MenuPopupBase
    {
        private const float W = 1180f, H = 760f;
        private readonly List<Button> _choices = new();
        private EnglishLesson _lesson;
        private int _index, _correct;
        private bool _answered, _completed;
        private TextMeshProUGUI _level, _progress, _objective, _context, _prompt, _feedback, _score;
        private RectTransform _choiceRoot;
        private Button _listen, _transcript, _next;
        private AudioSource _audio;

        protected override bool LocksGameplay => true;
        protected override Vector2 CardSize => new(W, H);

        public static EnglishPracticeUI GetOrCreate(Transform host)
        {
            var existing = SceneRefs.Find<EnglishPracticeUI>(FindObjectsInactive.Include);
            if (existing != null) return existing;
            var ui = host.gameObject.AddComponent<EnglishPracticeUI>();
            ui.Initialize(UiKit.Font);
            return ui;
        }

        public void Begin(EnglishLesson lesson)
        {
            if (lesson == null || lesson.questions == null || lesson.questions.Count == 0) return;
            _lesson = lesson;
            _index = _correct = 0;
            _answered = _completed = false;
            Show();
            Render();
        }

        protected override void GetTitle(out string vi, out string en, out string ja)
        {
            vi = "English Course";
            en = "English Course";
            ja = "英語コース";
        }

        protected override void Build(RectTransform card)
        {
            _audio = gameObject.GetComponent<AudioSource>();
            if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            var accent = UiTheme.Teal;
            var header = UiKit.Card(card, "LessonHeader", UiKit.Alpha(accent, 0.12f), 18, 0f, false).rectTransform;
            UiKit.Place(header, 32f, 104f, W - 64f, 92f);
            _level = UiKit.Label(header, "Level", "A1", 23f, accent, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Place(_level.rectTransform, 18f, 18f, 80f, 54f);
            _objective = UiKit.Label(header, "Objective", "", 16f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(_objective.rectTransform, 112f, 15f, 760f, 28f);
            _progress = UiKit.Label(header, "Progress", "", 13.5f, UiTheme.Muted);
            UiKit.Place(_progress.rectTransform, 112f, 48f, 760f, 24f);
            _score = UiKit.Label(header, "Score", "", 15f, accent, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            UiKit.Place(_score.rectTransform, 860f, 26f, 220f, 32f);

            var paper = UiKit.Card(card, "Exercise", UiTheme.Paper, 20, 0f, false).rectTransform;
            UiKit.Place(paper, 32f, 214f, W - 64f, 458f);
            _context = UiKit.Label(paper, "Context", "", 15f, UiTheme.PaperMuted, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            UiKit.Place(_context.rectTransform, 32f, 24f, 1050f, 96f);
            _prompt = UiKit.Label(paper, "Prompt", "", 22f, UiTheme.PaperInk, FontStyles.Bold, TextAlignmentOptions.TopLeft, wrap: true);
            UiKit.Place(_prompt.rectTransform, 32f, 126f, 760f, 64f);

            _listen = UiKit.Button(paper, "Listen", "Nghe lại", "Play audio", "音声を再生", Speak, UiKit.Kind.Primary, "headphones", 15f);
            UiKit.Place((RectTransform)_listen.transform, 824f, 126f, 220f, 46f);
            _transcript = UiKit.Button(paper, "Transcript", "Xem lời thoại", "Show transcript", "スクリプト", ShowTranscript, UiKit.Kind.Ghost, "subtitles", 14f);
            UiKit.Place((RectTransform)_transcript.transform, 824f, 178f, 220f, 40f);

            _choiceRoot = UiKit.Rect(paper, "Choices");
            UiKit.Place(_choiceRoot, 32f, 206f, 760f, 212f);
            UiKit.Column(_choiceRoot, 8f);
            _feedback = UiKit.Label(paper, "Feedback", "", 14.5f, UiTheme.PaperMuted, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            UiKit.Place(_feedback.rectTransform, 824f, 238f, 250f, 126f);
            _next = UiKit.Button(paper, "Next", "Câu tiếp", "Next", "次へ", Next, UiKit.Kind.Accent, "arrow_forward", 15f);
            UiKit.Place((RectTransform)_next.transform, 824f, 374f, 220f, 48f);

            var note = UiKit.Label(card, "Note",
                "Lộ trình English Foundations dùng nội dung tự soạn. IELTS vẫn là khu luyện thi riêng.",
                "English Foundations uses original content. IELTS remains a separate exam-preparation route.",
                "基礎英語はオリジナル教材です。IELTS対策は別コースです。", 13f, UiTheme.Muted, FontStyles.Italic, TextAlignmentOptions.Center);
            UiKit.Place(note.rectTransform, 60f, 696f, W - 120f, 24f);
        }

        protected override void OnOpened() { if (_lesson != null) Render(); }
        protected override void OnLanguageApplied() { if (IsOpen && _lesson != null) Render(); }

        private void Render()
        {
            if (_lesson == null || _index < 0 || _index >= _lesson.questions.Count) return;
            var q = _lesson.questions[_index];
            _answered = false;
            _completed = false;
            _level.text = _lesson.level;
            _objective.text = Loc.IsVietnamese ? _lesson.titleVi : _lesson.titleEn;
            _progress.text = Loc.F("{0} · {1} · Câu {2}/{3}", "{0} · {1} · Question {2}/{3}", "{0} · {1} · {2}/{3}問",
                EnglishCourseCatalog.SkillNameVi(_lesson.skill), EnglishCourseCatalog.TypeNameVi(_lesson.type), _index + 1, _lesson.questions.Count);
            _score.text = Loc.F("Đúng {0}", "{0} correct", "正解 {0}", _correct);
            _context.text = q.context ?? "";
            _context.gameObject.SetActive(!string.IsNullOrWhiteSpace(q.context));
            _prompt.text = q.prompt;
            _feedback.text = Loc.T("Chọn một đáp án để xem giải thích.", "Choose an answer to see the explanation.", "答えを選ぶと解説が表示されます。");
            _feedback.color = UiTheme.PaperMuted;
            bool listening = _lesson.type == EnglishActivityType.Listening;
            _listen.gameObject.SetActive(listening);
            _transcript.gameObject.SetActive(listening);
            _next.gameObject.SetActive(false);
            _choiceRoot.gameObject.SetActive(true);
            UiKit.Clear(_choiceRoot);
            _choices.Clear();
            for (int i = 0; i < q.choices.Length; i++)
            {
                int selected = i;
                var button = UiKit.Button(_choiceRoot, "Choice_" + i, $"{(char)('A' + i)}. {q.choices[i]}", () => Answer(selected), UiKit.Kind.Secondary, null, 15f, 12);
                UiKit.Size(button, 760f, 46f);
                _choices.Add(button);
            }
            if (listening) Speak();
        }

        private void Answer(int selected)
        {
            if (_answered) return;
            _answered = true;
            var q = _lesson.questions[_index];
            bool right = selected == q.correctIndex;
            if (right) _correct++;
            for (int i = 0; i < _choices.Count; i++)
            {
                _choices[i].interactable = false;
                UiKit.SetKind(_choices[i], i == q.correctIndex ? UiKit.Kind.Success : i == selected ? UiKit.Kind.Danger : UiKit.Kind.Quiet);
            }
            _feedback.text = (right ? Loc.T("✓ Chính xác\n", "✓ Correct\n", "✓ 正解\n") : Loc.T("Chưa đúng. Đáp án: ", "Not quite. Answer: ", "不正解。答え：") + q.choices[q.correctIndex] + "\n") + q.explanationVi;
            _feedback.color = right ? UiTheme.Good : UiTheme.Bad;
            _score.text = Loc.F("Đúng {0}", "{0} correct", "正解 {0}", _correct);
            _next.gameObject.SetActive(true);
            var label = _next.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null && _index == _lesson.questions.Count - 1) label.text = Loc.T("Xem kết quả", "See result", "結果を見る");
        }

        private void Next()
        {
            if (_completed) { Begin(_lesson); return; }
            if (!_answered) return;
            if (_index + 1 < _lesson.questions.Count) { _index++; Render(); return; }
            _prompt.text = Loc.F("Hoàn thành: {0}/{1} câu đúng", "Complete: {0}/{1} correct", "完了：{0}/{1}問正解", _correct, _lesson.questions.Count);
            _context.gameObject.SetActive(false);
            _listen.gameObject.SetActive(false);
            _transcript.gameObject.SetActive(false);
            _choiceRoot.gameObject.SetActive(false);
            _feedback.text = _correct == _lesson.questions.Count
                ? Loc.T("Xuất sắc! Bạn đã nắm chắc bài này.", "Excellent — you have mastered this lesson.", "素晴らしい！このレッスンを習得しました。")
                : Loc.T("Hãy làm lại để củng cố những câu chưa chắc.", "Try again to strengthen the questions you missed.", "もう一度挑戦して復習しましょう。");
            _feedback.color = UiTheme.Teal;
            _completed = true;
            _next.gameObject.SetActive(true);
            var label = _next.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = Loc.T("Làm lại", "Try again", "もう一度");
        }

        private void Speak()
        {
            var q = _lesson?.questions?[_index];
            if (q == null || string.IsNullOrWhiteSpace(q.spokenText)) return;
            StartCoroutine(PlayLocalOrSpeech(q.spokenText));
        }

        private System.Collections.IEnumerator PlayLocalOrSpeech(string text)
        {
            AudioClip clip = null;
            yield return EnglishAudioLibrary.Load(_lesson.id, _index, loaded => clip = loaded);
            if (clip != null)
            {
                _audio.Stop();
                _audio.clip = clip;
                _audio.Play();
                yield break;
            }
            if (!EnglishSpeech.Speak(text))
                HudFeed.Post(Loc.T("Chưa có audio local. Bạn vẫn có thể mở lời thoại để luyện.", "Local audio is not installed. You can still practise with the transcript.", "ローカル音声がありません。スクリプトで練習できます。"), HudFeed.Kind.Info, 4f, "english_speech");
        }

        private void ShowTranscript()
        {
            var q = _lesson?.questions?[_index];
            if (q != null) _context.text = "🎧 “" + q.spokenText + "”";
            _context.gameObject.SetActive(true);
        }
    }
}
