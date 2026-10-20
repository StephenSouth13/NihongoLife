using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Exam;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// The exam-taking screen: renders whatever ExamManager says is the current section/question — an info bar with
    /// the section, progress and a timer pill, a question navigator, the passage and the question on exam paper,
    /// answer cards (keys 1–4), and the results (score ring, pass stamp, division bars, review). Purely a view over
    /// ExamManager: closing and reopening it never loses answers already given.
    /// </summary>
    public class ExamPlayUI : MenuPopupBase
    {
        private const int MaxRecordSeconds = 120;
        private const float W = 1460f, H = 900f, NavW = 236f, Top = 108f, Bottom = 88f;

        // Info bar
        private GameObject _examHeader;
        private TextMeshProUGUI _sectionTitleText, _progressText, _timerText, _familyText;
        private Image _timerRing, _timerPill, _familyChip;
        // Navigator
        private RectTransform _navPanel, _paletteRoot;
        // Passage + question
        private GameObject _passagePanelRoot;
        private RectTransform _passageContent;
        private AudioSource _listeningAudio;
        private RectTransform _questionPanel;
        private TextMeshProUGUI _promptText, _promptReadingText, _questionNumberText;
        private static readonly HighlightBook Highlights = new HighlightBook();
        private RectTransform _highlightToolbar;
        private RectTransform _answerRoot;
        // Footer
        private Button _prevButton, _nextButton, _submitSectionButton;
        private TextMeshProUGUI _statusText;
        // Grading
        private GameObject _gradingOverlay;
        private TextMeshProUGUI _gradingText;
        private RectTransform _gradingSpinner;
        // Results
        private GameObject _resultsRoot;
        private RectTransform _resultsSectionList, _reviewListContent, _resultsHero;
        private TextMeshProUGUI _resultsHeadlineText, _resultsSubText;
        private ExamAttemptResult _lastResult;
        // Essay / speaking
        private TMP_InputField _activeEssayInput;
        private bool _isRecording;
        private readonly MicRecorder _recorder = new MicRecorder();
        private Button _recordButton;
        private TextMeshProUGUI _recordStatusText;
        private readonly List<Button> _choiceButtons = new List<Button>();
        private GameObject _leaveConfirm;

        protected override bool LocksGameplay => true;
        protected override Vector2 CardSize => new Vector2(W, H);

        public static ExamPlayUI GetOrCreate(Transform host)
        {
            var existing = FindFirstObjectByType<ExamPlayUI>(FindObjectsInactive.Include);
            if (existing != null) return existing;
            var instance = host.gameObject.AddComponent<ExamPlayUI>();
            instance.Initialize(UiKit.Font);
            return instance;
        }

        protected override void GetTitle(out string vi, out string en, out string ja)
        {
            vi = "Phòng thi";
            en = "Exam room";
            ja = "試験室";
        }

        private bool IsJlpt => ExamManager.Instance?.CurrentExam != null && ExamManager.Instance.CurrentExam.examType == ExamType.Jlpt;
        private Color Family => IsJlpt ? UiTheme.Jlpt : UiTheme.Ielts;

        /// <summary>The question screen (info bar, navigator, passage, question, footer) — hidden while results show.</summary>
        private void SetExamScreenVisible(bool visible)
        {
            foreach (var go in new[] { _examHeader, _navPanel != null ? _navPanel.gameObject : null, _passagePanelRoot, _questionPanel != null ? _questionPanel.gameObject : null,
                                       _prevButton != null ? _prevButton.gameObject : null, _nextButton != null ? _nextButton.gameObject : null,
                                       _submitSectionButton != null ? _submitSectionButton.gameObject : null, _statusText != null ? _statusText.gameObject : null })
                if (go != null) go.SetActive(visible);
        }

        // ──────────────────────── Build ────────────────────────

        protected override void Build(RectTransform card)
        {
            // Unity's fake-null breaks `??` on components; use an explicit lifetime check.
            _listeningAudio = gameObject.GetComponent<AudioSource>();
            if (_listeningAudio == null) _listeningAudio = gameObject.AddComponent<AudioSource>();
            _listeningAudio.playOnAwake = false;
            _listeningAudio.loop = false;
            _listeningAudio.spatialBlend = 0f;

            BuildInfoBar(card);
            BuildNavigator(card);
            BuildPassagePanel(card);
            BuildQuestionPanel(card);
            BuildFooter(card);
            BuildGradingOverlay(card);
            BuildResultsPanel(card);
            BuildLeaveConfirm(card);
        }

        private void BuildInfoBar(RectTransform card)
        {
            var bar = UiKit.Rect(card, "ExamHeader");
            _examHeader = bar.gameObject;
            UiKit.Place(bar, 240f, 22f, W - 240f - 96f, 58f);

            _familyChip = UiKit.Image(bar, "Family", Family, UiKit.Rounded(12));
            UiKit.Place(_familyChip.rectTransform, 0f, 9f, 92f, 40f);
            _familyText = UiKit.Label(_familyChip.transform, "Text", "JLPT", 15f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Fill(_familyText.rectTransform);
            _sectionTitleText = UiKit.Label(bar, "Section", "", 19f, UiTheme.Text, FontStyles.Bold);
            UiKit.Place(_sectionTitleText.rectTransform, 108f, 4f, 360f, 28f);
            _progressText = UiKit.Label(bar, "Progress", "", 13.5f, UiTheme.Muted);
            UiKit.Place(_progressText.rectTransform, 108f, 32f, 360f, 22f);

            _highlightToolbar = HighlightToolbar.Create(bar, Font, () => Highlights.Clear(), CopyHighlightsToNotebook, 36f);
            HighlightToolbar.SetCompact(_highlightToolbar, true);
            UiKit.Pin(_highlightToolbar, new Vector2(1f, 0.5f), -230f, 0f, 380f, 36f);

            _timerPill = UiKit.Image(bar, "TimerPill", UiTheme.Sunken, UiKit.Rounded(24));
            UiKit.Pin(_timerPill.rectTransform, new Vector2(1f, 0.5f), 0f, 0f, 210f, 50f);
            _timerRing = UiKit.ProgressRing(_timerPill.transform, "TimerRing", 34f, UiTheme.Gold, new Color(1f, 1f, 1f, 0.08f), thin: true);
            UiKit.Place((RectTransform)_timerRing.transform.parent, 10f, 8f, 34f, 34f);
            var clock = UiKit.Icon(_timerRing.transform.parent, "timer", UiTheme.Muted, 16f);
            UiKit.Pin(clock.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, 16f, 16f);
            _timerText = UiKit.Label(_timerPill.transform, "Timer", "", 21f, UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UiKit.Place(_timerText.rectTransform, 54f, 0f, 150f, 50f);
        }

        private void BuildNavigator(RectTransform card)
        {
            _navPanel = UiKit.Card(card, "Navigator", UiTheme.Surface, 18, 0f, false).rectTransform;
            UiKit.Place(_navPanel, 28f, Top, NavW, H - Top - Bottom - 16f);
            var head = UiKit.Label(_navPanel, "Head", "Câu hỏi", "Questions", "問題", 14f, UiTheme.Gold, FontStyles.Bold);
            UiKit.Place(head.rectTransform, 18f, 16f, NavW - 36f, 22f);
            var grid = UiKit.Rect(_navPanel, "Palette");
            _paletteRoot = grid;
            UiKit.Place(grid, 16f, 48f, NavW - 32f, H - Top - Bottom - 16f - 170f);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(34f, 34f);
            layout.spacing = new Vector2(7f, 7f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 5;
            Legend(_navPanel, 0, UiTheme.Gold, "Đang làm", "Current", "解答中");
            Legend(_navPanel, 1, UiTheme.Teal, "Đã trả lời", "Answered", "解答済み");
            Legend(_navPanel, 2, UiTheme.Raised, "Chưa làm", "Not answered", "未解答");
        }

        private void Legend(RectTransform parent, int index, Color color, string vi, string en, string ja)
        {
            float y = -(24f + index * 26f);
            var dot = UiKit.Image(parent, "LegendDot" + index, color, UiKit.Circle);
            UiKit.Pin(dot.rectTransform, new Vector2(0f, 0f), 20f, -y, 14f, 14f);
            var l = UiKit.Label(parent, "Legend" + index, vi, en, ja, 12.5f, UiTheme.Muted);
            UiKit.Pin(l.rectTransform, new Vector2(0f, 0f), 42f, -y - 4f, NavW - 60f, 22f);
        }

        private void BuildPassagePanel(RectTransform card)
        {
            var paper = UiKit.Card(card, "PassagePanel", UiTheme.Paper, 18, 0.35f, false);
            _passagePanelRoot = paper.gameObject;
            _passageContent = UiKit.Scroll(paper.rectTransform, "PassageScroll", out var _, 12f, new RectOffset(30, 30, 26, 26));
            UiKit.Fill((RectTransform)_passageContent.parent.parent);
        }

        private void BuildQuestionPanel(RectTransform card)
        {
            var paper = UiKit.Card(card, "QuestionPanel", UiTheme.Paper, 18, 0.35f, false);
            _questionPanel = paper.rectTransform;
            _questionNumberText = UiKit.Label(_questionPanel, "Number", "", 13f, UiTheme.PaperMuted, FontStyles.Bold);
            _promptText = UiKit.Label(_questionPanel, "Prompt", "", 22f, UiTheme.PaperInk, FontStyles.Bold, TextAlignmentOptions.TopLeft, wrap: true);
            _promptText.raycastTarget = true; // highlighting
            _promptReadingText = UiKit.Label(_questionPanel, "Reading", "", 15f, UiTheme.PaperMuted, FontStyles.Italic);
            _answerRoot = UiKit.Rect(_questionPanel, "AnswerRoot");
        }

        private void ApplyPassageLayout(bool hasPassage)
        {
            _passagePanelRoot.SetActive(hasPassage);
            float x0 = 28f + NavW + 18f, width = W - x0 - 28f, h = H - Top - Bottom - 16f;
            if (hasPassage)
            {
                float half = (width - 18f) * 0.5f;
                UiKit.Place((RectTransform)_passagePanelRoot.transform, x0, Top, half, h);
                UiKit.Place(_questionPanel, x0 + half + 18f, Top, half, h);
            }
            else UiKit.Place(_questionPanel, x0, Top, width, h);
            float inner = _questionPanel.sizeDelta.x - 64f;
            UiKit.Place(_questionNumberText.rectTransform, 32f, 24f, inner, 20f);
            UiKit.Place(_promptText.rectTransform, 32f, 48f, inner, 100f);
            UiKit.Place(_promptReadingText.rectTransform, 32f, 150f, inner, 24f);
            UiKit.Place(_answerRoot, 32f, 186f, inner, h - 210f);
        }

        private void BuildFooter(RectTransform card)
        {
            float y = H - Bottom + 8f;
            _prevButton = UiKit.Button(card, "Prev", "Câu trước", "Previous", "前へ", () => { CommitActiveEssayIfAny(); ExamManager.Instance?.PreviousQuestion(); }, UiKit.Kind.Ghost, "chevron_left", 16f);
            UiKit.Place((RectTransform)_prevButton.transform, 28f + NavW + 18f, y, 170f, 52f);
            _nextButton = UiKit.Button(card, "Next", "Câu sau", "Next", "次へ", () => { CommitActiveEssayIfAny(); ExamManager.Instance?.NextQuestion(); }, UiKit.Kind.Secondary, "chevron_right", 16f);
            UiKit.Place((RectTransform)_nextButton.transform, 28f + NavW + 18f + 182f, y, 170f, 52f);
            _statusText = UiKit.Label(card, "Status", "", 14f, UiTheme.Muted, FontStyles.Italic, TextAlignmentOptions.MidlineLeft, wrap: true);
            UiKit.Place(_statusText.rectTransform, 28f + NavW + 18f + 370f, y, 520f, 52f);
            _submitSectionButton = UiKit.Button(card, "Submit", "Nộp phần này", "Submit section", "このセクションを提出", () => { CommitActiveEssayIfAny(); ExamManager.Instance?.SubmitCurrentSection(); }, UiKit.Kind.Primary, "task_alt", 16f);
            UiKit.Place((RectTransform)_submitSectionButton.transform, W - 28f - 250f, y, 250f, 52f);
        }

        private void BuildGradingOverlay(RectTransform card)
        {
            var overlay = UiKit.Image(card, "GradingOverlay", new Color(0.03f, 0.04f, 0.06f, 0.94f), UiKit.Rounded(22), true);
            _gradingOverlay = overlay.gameObject;
            UiKit.Fill(overlay.rectTransform);
            var ring = UiKit.ProgressRing(overlay.transform, "Spinner", 96f, UiTheme.Gold, new Color(1f, 1f, 1f, 0.08f));
            ring.fillAmount = 0.28f;
            _gradingSpinner = (RectTransform)ring.transform;
            UiKit.Pin((RectTransform)ring.transform.parent, new Vector2(0.5f, 0.5f), 0f, 60f, 96f, 96f);
            _gradingText = UiKit.Label(overlay.transform, "Text", "", 22f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center, wrap: true);
            UiKit.Pin(_gradingText.rectTransform, new Vector2(0.5f, 0.5f), 0f, -40f, 900f, 120f);
            _gradingOverlay.SetActive(false);
        }

        private void BuildResultsPanel(RectTransform card)
        {
            var root = UiKit.Image(card, "ResultsPanel", UiTheme.Ink, UiKit.Rounded(22), true);
            _resultsRoot = root.gameObject;
            UiKit.Fill(root.rectTransform, 0f, 96f, 0f, 0f);

            _resultsHero = UiKit.Card(root.transform, "Hero", UiTheme.Surface, 20, 0f, false).rectTransform;
            UiKit.Place(_resultsHero, 28f, 4f, W - 56f, 190f);
            _resultsHeadlineText = UiKit.Label(_resultsHero, "Headline", "", 40f, UiTheme.Gold, FontStyles.Bold);
            UiKit.Place(_resultsHeadlineText.rectTransform, 220f, 30f, 700f, 56f);
            _resultsSubText = UiKit.Label(_resultsHero, "Sub", "", 16f, UiTheme.TextSoft, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            UiKit.Place(_resultsSubText.rectTransform, 220f, 92f, 640f, 80f);
            _resultsSectionList = UiKit.Rect(_resultsHero, "SectionSummary");
            UiKit.Place(_resultsSectionList, 880f, 26f, W - 56f - 900f, 150f);

            _reviewListContent = UiKit.Scroll(root.transform, "ReviewList", out var _, 10f, new RectOffset(2, 12, 2, 16));
            UiKit.Place((RectTransform)_reviewListContent.parent.parent, 28f, 212f, W - 56f, H - 96f - 212f - 84f);

            var close = UiKit.Button(root.transform, "Close", "Đóng", "Close", "閉じる", Hide, UiKit.Kind.Primary, "done", 16f);
            UiKit.Pin((RectTransform)close.transform, new Vector2(1f, 0f), -28f, 20f, 220f, 52f);
            _resultsRoot.SetActive(false);
        }

        // ─────────── Esc during an attempt: confirm before leaving ───────────

        private void BuildLeaveConfirm(RectTransform card)
        {
            var dim = UiKit.Image(card, "LeaveConfirm", new Color(0.01f, 0.014f, 0.02f, 0.86f), UiKit.Rounded(22), true);
            _leaveConfirm = dim.gameObject;
            UiKit.Fill(dim.rectTransform);
            var box = UiKit.Card(dim.transform, "Box", UiTheme.Surface, 22, 0.5f, true).rectTransform;
            UiKit.Pin(box, new Vector2(0.5f, 0.5f), 0f, 0f, 620f, 280f);
            var icon = UiKit.Icon(box, "warning", UiTheme.Warn, 40f);
            UiKit.Pin(icon.rectTransform, new Vector2(0.5f, 1f), 0f, -30f, 40f, 40f);
            var title = UiKit.Label(box, "Title", "Tạm rời bài thi?", "Leave the exam for now?", "試験を中断しますか？", 24f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Pin(title.rectTransform, new Vector2(0.5f, 1f), 0f, -80f, 560f, 34f);
            var body = UiKit.Label(box, "Body", "Bài làm được giữ lại. Mở lại Trung tâm luyện thi để làm tiếp; đồng hồ vẫn chạy theo luật của đề.",
                "Your answers are kept. Reopen the Exam Center to continue; the timer follows the exam's rules.",
                "解答は保存されます。試験センターから再開できます。タイマーは試験のルールどおりに進みます。", 15.5f, UiTheme.Muted, FontStyles.Normal, TextAlignmentOptions.Top, wrap: true);
            UiKit.Pin(body.rectTransform, new Vector2(0.5f, 1f), 0f, -120f, 540f, 60f);
            var stay = UiKit.Button(box, "Stay", "Tiếp tục làm bài", "Keep going", "続ける", () => _leaveConfirm.SetActive(false), UiKit.Kind.Primary, "play_arrow", 16f);
            UiKit.Pin((RectTransform)stay.transform, new Vector2(0.5f, 0f), -135f, 28f, 250f, 50f);
            var leave = UiKit.Button(box, "Leave", "Tạm rời", "Leave", "中断する", () => { _leaveConfirm.SetActive(false); Hide(); }, UiKit.Kind.Ghost, "logout", 16f);
            UiKit.Pin((RectTransform)leave.transform, new Vector2(0.5f, 0f), 135f, 28f, 250f, 50f);
            _leaveConfirm.SetActive(false);
        }

        /// <summary>Esc while an attempt is running asks first; a second Esc dismisses the question.</summary>
        protected override bool HandleEscape()
        {
            if (_leaveConfirm != null && _leaveConfirm.activeSelf) { _leaveConfirm.SetActive(false); return true; }
            var manager = ExamManager.Instance;
            bool running = manager != null && manager.IsAttemptActive && (_resultsRoot == null || !_resultsRoot.activeSelf);
            if (!running || _leaveConfirm == null) return false;
            _leaveConfirm.SetActive(true);
            _leaveConfirm.transform.SetAsLastSibling();
            return true;
        }

        // ──────────────────────── Lifecycle ────────────────────────

        protected override void OnOpened()
        {
            var manager = ExamManager.Instance;
            if (manager != null)
            {
                manager.OnStateChanged += HandleStateChanged;
                manager.OnTimerTick += HandleTimerTick;
                manager.OnExamFinished += HandleExamFinished;
                manager.OnGradingStatus += HandleGradingStatus;
            }
            _resultsRoot.SetActive(false);
            _gradingOverlay.SetActive(false);
            SetExamScreenVisible(true);
            RefreshAll();
        }

        protected override void OnClosed()
        {
            Unsubscribe();
            CommitActiveEssayIfAny();
            StopRecordingIfNeeded();
            HighlightTool.SetMode(HighlightTool.ToolMode.Off);
        }

        protected override void OnOpenUpdate()
        {
            if (_gradingOverlay != null && _gradingOverlay.activeSelf && _gradingSpinner != null)
                _gradingSpinner.localRotation = Quaternion.Euler(0f, 0f, -Time.unscaledTime * 240f);
            // Keys 1–4 pick an answer (like the computer-based tests), arrows move between questions.
            var kb = Keyboard.current;
            if (kb == null || _resultsRoot.activeSelf || _leaveConfirm.activeSelf || _activeEssayInput != null && _activeEssayInput.isFocused) return;
            for (int i = 0; i < _choiceButtons.Count && i < 4; i++)
            {
                var key = i == 0 ? kb.digit1Key : i == 1 ? kb.digit2Key : i == 2 ? kb.digit3Key : kb.digit4Key;
                if (key.wasPressedThisFrame && _choiceButtons[i] != null && _choiceButtons[i].interactable) _choiceButtons[i].onClick.Invoke();
            }
            if (kb.rightArrowKey.wasPressedThisFrame && _nextButton.interactable) _nextButton.onClick.Invoke();
            if (kb.leftArrowKey.wasPressedThisFrame && _prevButton.interactable) _prevButton.onClick.Invoke();
        }

        private void CopyHighlightsToNotebook()
        {
            var snippets = Highlights.Snippets();
            if (snippets.Count == 0)
            {
                HudFeed.Post(Loc.T("Tô sáng một đoạn trước, rồi bấm → Sổ để chép vào sổ tay.", "Highlight some text first, then press → Notebook.", "先にテキストをハイライトしてから「→ノート」を押してください。"), HudFeed.Kind.Info, 3f);
                return;
            }
            int left = NihongoLife.Notebook.NotebookService.Append(string.Join("\n", snippets.Select(x => "• " + x)), _sectionTitleText != null ? TextHighlighter.StripTags(_sectionTitleText.text) : Loc.T("Bài thi", "Exam", "試験"));
            HudFeed.Post(left > 0
                    ? Loc.F("Sổ tay hết chỗ — còn {0} ký tự chưa chép. Mua thêm giấy ở Hibari Mart.", "Notebook full — {0} characters not copied. Buy paper at Hibari Mart.", "ノートがいっぱいです — {0}文字コピーできません。ひばりマートで紙を買えます。", left)
                    : Loc.F("Đã chép {0} đoạn vào sổ tay.", "Copied {0} highlights to the notebook.", "{0}件をノートにコピーしました。", snippets.Count),
                left > 0 ? HudFeed.Kind.Warning : HudFeed.Kind.Reward, 3.5f);
            NotebookUI.Show();
        }

        protected override void OnLanguageApplied()
        {
            if (PanelObject == null || !PanelObject.activeSelf) return;
            RefreshAll();
            if (_resultsRoot.activeSelf && _lastResult != null) ShowResults(_lastResult);
        }

        /// <summary>ExamManager outlives scenes: a screen destroyed while open must not stay subscribed.</summary>
        protected override void OnDestroyed() => Unsubscribe();

        private void Unsubscribe()
        {
            var manager = ExamManager.Instance;
            if (manager == null) return;
            manager.OnStateChanged -= HandleStateChanged;
            manager.OnTimerTick -= HandleTimerTick;
            manager.OnExamFinished -= HandleExamFinished;
            manager.OnGradingStatus -= HandleGradingStatus;
        }

        private void HandleStateChanged() => RefreshAll();

        private void HandleTimerTick(float remaining, float duration)
        {
            if (duration <= 0f)
            {
                _timerText.text = Loc.T("Không giới hạn", "No limit", "時間制限なし");
                UiKit.SetSize(_timerText, 15f);
                _timerText.color = UiTheme.Muted;
                _timerRing.fillAmount = 1f;
                return;
            }
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            bool urgent = remaining <= 30f;
            UiKit.SetSize(_timerText, 21f);
            _timerText.text = $"{minutes:00}:{seconds:00}";
            _timerText.color = urgent ? UiTheme.Bad : remaining <= 300f ? UiTheme.Warn : UiTheme.Gold;
            _timerRing.color = _timerText.color;
            _timerRing.fillAmount = Mathf.Clamp01(remaining / duration);
            _timerPill.color = urgent ? UiKit.Alpha(UiTheme.Bad, 0.18f + 0.1f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f))) : UiTheme.Sunken;
        }

        private void HandleGradingStatus(string status)
        {
            bool active = !string.IsNullOrEmpty(status);
            _gradingOverlay.SetActive(active);
            if (active) { _gradingText.text = status; _gradingOverlay.transform.SetAsLastSibling(); }
        }

        private void HandleExamFinished(ExamAttemptResult result)
        {
            _lastResult = result;
            _gradingOverlay.SetActive(false);
            ShowResults(result);
        }

        private void CommitActiveEssayIfAny()
        {
            if (_activeEssayInput != null && ExamManager.Instance != null) ExamManager.Instance.SubmitEssayText(_activeEssayInput.text);
        }

        // ──────────────────────── Refresh ────────────────────────

        private void RefreshAll()
        {
            var manager = ExamManager.Instance;
            if (manager == null || !manager.IsAttemptActive) return;
            var section = manager.CurrentSection;
            var question = manager.CurrentQuestion;
            if (section == null) return;

            _familyChip.color = Family;
            _familyText.text = $"{(IsJlpt ? "JLPT" : "IELTS")} {manager.CurrentExam?.level}".Trim();
            _sectionTitleText.text = Loc.Pick(section.titleVi, section.titleEn, section.titleJa);
            int answered = section.questions.Count(item => manager.GetAnswer(item.id) != null);
            _progressText.text = Loc.F("Câu {0}/{1}  ·  đã trả lời {2}", "Question {0} of {1}  ·  {2} answered", "{0}/{1}問  ·  解答済み {2}", manager.QuestionIndex + 1, section.questions.Count, answered);
            if (section.timeLimitSeconds <= 0) HandleTimerTick(0f, 0f);

            bool locked = manager.IsCurrentSectionLocked;
            _statusText.text = locked
                ? Loc.T("Phần này đã được nộp.", "This section has been submitted.", "このセクションは提出済みです。")
                : Loc.T("Phím 1–4 để chọn đáp án · ← → để chuyển câu", "Keys 1–4 pick an answer · ← → move between questions", "1〜4キーで解答 · ←→で移動");
            _prevButton.interactable = !locked && manager.QuestionIndex > 0;
            _nextButton.interactable = !locked && manager.QuestionIndex < section.questions.Count - 1;
            _submitSectionButton.interactable = !locked;

            RefreshPalette(manager, section);
            RefreshPassage(manager, section, question);
            RefreshQuestion(manager, question, locked);
        }

        private void RefreshPalette(ExamManager manager, ExamSection section)
        {
            UiKit.Clear(_paletteRoot);
            for (int i = 0; i < section.questions.Count; i++)
            {
                int index = i;
                bool answered = manager.GetAnswer(section.questions[i].id) != null;
                bool current = index == manager.QuestionIndex;
                var cell = UiKit.Image(_paletteRoot, "Q" + i, current ? UiTheme.Gold : answered ? UiKit.Alpha(UiTheme.Teal, 0.85f) : UiTheme.Raised, UiKit.Circle, true);
                var btn = cell.gameObject.AddComponent<Button>();
                btn.targetGraphic = cell;
                btn.onClick.AddListener(() => { CommitActiveEssayIfAny(); ExamManager.Instance?.GoToQuestion(index); });
                var label = UiKit.Label(cell.transform, "N", (i + 1).ToString(), 13f, current ? UiTheme.OnGold : Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Fill(label.rectTransform);
            }
        }

        private void RefreshPassage(ExamManager manager, ExamSection section, ExamQuestion question)
        {
            var passage = question != null ? section.FindPassage(question.passageId) : null;
            bool hasPassage = passage != null;
            ApplyPassageLayout(hasPassage);
            if (!hasPassage) return;
            UiKit.Clear(_passageContent);

            var title = UiKit.Label(_passageContent, "Title", Loc.Pick(passage.titleVi, passage.titleEn, passage.titleJa), 19f, UiKit.Darken(Family, 0.1f), FontStyles.Bold, TextAlignmentOptions.TopLeft, wrap: true);
            title.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            bool isListening = section.type == ExamSectionType.JlptListening || section.type == ExamSectionType.IeltsListening;
            if (isListening && passage.maxPlays > 0)
            {
                int remaining = manager.RemainingPlays(passage);
                bool hasAudio = passage.audioClip != null || !string.IsNullOrWhiteSpace(passage.audioUrl);
                var row = UiKit.Rect(_passageContent, "PlayRow");
                UiKit.Size(row, -1f, 64f);
                var play = UiKit.Button(row, "Play", null, null, null, () =>
                {
                    if (passage.audioClip != null)
                    {
                        if (!manager.TryConsumePassagePlay(passage)) return;
                        PlayListeningClip(passage.audioClip);
                        RefreshPassage(manager, section, question);
                    }
                    else if (!string.IsNullOrWhiteSpace(passage.audioUrl)) StartCoroutine(DownloadAndPlayListening(manager, passage, section, question));
                }, UiKit.Kind.Accent, "headphones", 17f, 16);
                UiKit.Place((RectTransform)play.transform, 0f, 6f, 300f, 52f);
                UiKit.SetLabel(play, hasAudio ? Loc.F("Nghe (còn {0} lần)", "Play ({0} left)", "再生（残り{0}回）", remaining) : Loc.T("Thiếu audio", "Audio missing", "音声がありません"));
                play.interactable = remaining > 0 && hasAudio;
                var note = UiKit.Label(row, "Note", "Như thi thật: chỉ được nghe số lần cho phép.", "As in the real test: only the allowed number of plays.", "本番と同じく、再生回数は決まっています。", 13f, UiTheme.PaperMuted, FontStyles.Italic, TextAlignmentOptions.MidlineLeft, wrap: true);
                UiKit.Place(note.rectTransform, 316f, 6f, 360f, 52f);
            }

            if (!string.IsNullOrWhiteSpace(passage.bodyReading))
            {
                var reading = UiKit.Label(_passageContent, "Reading", passage.bodyReading, 15f, UiTheme.PaperMuted, FontStyles.Italic, TextAlignmentOptions.TopLeft, wrap: true);
                reading.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            string body = Loc.Pick(passage.bodyVi, passage.bodyEn, passage.bodyJa);
            bool audioListening = isListening && (passage.audioClip != null || !string.IsNullOrWhiteSpace(passage.audioUrl));
            if (audioListening)
                body = Loc.T("Nghe audio rồi chọn đáp án. Lời thoại sẽ hiện ở phần xem lại sau khi nộp bài.",
                             "Listen, then choose the answer. The script is shown in the review after you submit.",
                             "音声を聞いて答えを選んでください。スクリプトは提出後に表示されます。");
            var bodyText = UiKit.Label(_passageContent, "Body", body, 18f, UiTheme.PaperInk, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            bodyText.lineSpacing = 8f;
            bodyText.raycastTarget = true;
            bodyText.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            TextHighlighter.Bind(bodyText, Highlights, HighlightBook.KeyFor("passage/" + passage.id, body));
        }

        private void PlayListeningClip(AudioClip clip)
        {
            if (_listeningAudio == null || clip == null) return;
            _listeningAudio.Stop();
            _listeningAudio.clip = clip;
            _listeningAudio.Play();
        }

        private IEnumerator DownloadAndPlayListening(ExamManager manager, ExamPassage passage, ExamSection section, ExamQuestion question)
        {
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(passage.audioUrl, AudioType.MPEG))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[ExamPlayUI] Listening audio download failed: {request.error}");
                    yield break;
                }
                if (!manager.TryConsumePassagePlay(passage)) yield break;
                PlayListeningClip(DownloadHandlerAudioClip.GetContent(request));
                RefreshPassage(manager, section, question);
            }
        }

        private void RefreshQuestion(ExamManager manager, ExamQuestion question, bool locked)
        {
            StopRecordingIfNeeded();
            _activeEssayInput = null;
            _recordButton = null;
            _recordStatusText = null;
            _choiceButtons.Clear();
            UiKit.Clear(_answerRoot);

            if (question == null)
            {
                _promptText.text = string.Empty;
                _promptReadingText.gameObject.SetActive(false);
                return;
            }
            _questionNumberText.text = Loc.F("CÂU {0}", "QUESTION {0}", "問題 {0}", manager.QuestionIndex + 1);
            _promptText.text = Loc.Pick(question.promptVi, question.promptEn, question.promptJa);
            TextHighlighter.Bind(_promptText, Highlights, HighlightBook.KeyFor("q/" + question.id, _promptText.text));
            _promptText.ForceMeshUpdate();
            float promptHeight = Mathf.Clamp(_promptText.preferredHeight + 6f, 34f, 220f);
            UiKit.Place(_promptText.rectTransform, 32f, 48f, _questionPanel.sizeDelta.x - 64f, promptHeight);
            _promptReadingText.text = question.promptReading ?? string.Empty;
            bool hasReading = !string.IsNullOrWhiteSpace(question.promptReading);
            _promptReadingText.gameObject.SetActive(hasReading);
            float y = 48f + promptHeight + 6f;
            if (hasReading) { UiKit.Place(_promptReadingText.rectTransform, 32f, y, _questionPanel.sizeDelta.x - 64f, 24f); y += 30f; }
            UiKit.Place(_answerRoot, 32f, y + 12f, _questionPanel.sizeDelta.x - 64f, _questionPanel.sizeDelta.y - y - 36f);

            switch (question.type)
            {
                case ExamQuestionType.MultipleChoice:
                case ExamQuestionType.TrueFalseNotGiven:
                    BuildChoiceAnswer(manager, question, locked);
                    break;
                case ExamQuestionType.FillBlank:
                    BuildFillBlankAnswer(manager, question, locked);
                    break;
                case ExamQuestionType.Essay:
                    BuildEssayAnswer(manager, question, locked);
                    break;
                case ExamQuestionType.SpeakingPrompt:
                    BuildSpeakingAnswer(manager, question, locked);
                    break;
            }
        }

        private void BuildChoiceAnswer(ExamManager manager, ExamQuestion question, bool locked)
        {
            var existing = manager.GetAnswer(question.id);
            int selected = existing != null && int.TryParse(existing.givenAnswer, out int parsed) ? parsed : -1;
            float width = _answerRoot.sizeDelta.x;
            const float rowHeight = 60f;
            for (int i = 0; i < question.choices.Count; i++)
            {
                int index = i;
                bool on = index == selected;
                var row = UiKit.Card(_answerRoot, "Choice" + i, on ? UiTheme.PaperSelect : Color.white, 14, 0f, true);
                UiKit.Place(row.rectTransform, 0f, i * (rowHeight + 12f), width, rowHeight);
                UiKit.Outline(row.rectTransform, on ? Family : UiTheme.PaperLine, on ? 2f : 1.2f);
                var button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = row;
                UIStyleKit.StyleButton(button, row.color, on ? row.color : new Color(0.96f, 0.97f, 1f, 1f), new Color(0.88f, 0.92f, 0.98f, 1f));
                row.sprite = UiKit.Rounded(14);
                button.interactable = !locked;
                button.onClick.AddListener(() => manager.SubmitChoice(index));
                _choiceButtons.Add(button);

                var disc = UiKit.Image(row.transform, "Number", on ? Family : new Color(0.92f, 0.93f, 0.95f, 1f), UiKit.Circle);
                UiKit.Place(disc.rectTransform, 14f, 12f, 36f, 36f);
                var n = UiKit.Label(disc.transform, "N", (i + 1).ToString(), 16f, on ? Color.white : UiTheme.PaperMuted, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Fill(n.rectTransform);
                var text = UiKit.Label(row.transform, "Text", Loc.Pick(question.choices[i].textVi, question.choices[i].textEn, question.choices[i].textJa), 18f, UiTheme.PaperInk,
                    on ? FontStyles.Bold : FontStyles.Normal, TextAlignmentOptions.MidlineLeft, wrap: true);
                UiKit.Place(text.rectTransform, 66f, 0f, width - 120f, rowHeight);
                if (on)
                {
                    var check = UiKit.Icon(row.transform, "check_circle", Family, 26f);
                    UiKit.Pin(check.rectTransform, new Vector2(1f, 0.5f), -16f, 0f, 26f, 26f);
                }
            }
        }

        private TMP_InputField CreateInputField(Transform parent, float x, float y, float width, float height, string placeholder, bool multiline)
        {
            var bg = UiKit.Card(parent, "InputField", Color.white, 12, 0f, true);
            UiKit.Place(bg.rectTransform, x, y, width, height);
            UiKit.Outline(bg.rectTransform, UiTheme.PaperLine, 1.4f);
            var input = bg.gameObject.AddComponent<TMP_InputField>();
            input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
            var area = UiKit.Rect(bg.transform, "TextViewport");
            UiKit.Fill(area, 14f, 10f, 14f, 10f);
            area.gameObject.AddComponent<RectMask2D>();
            var text = UiKit.Label(area, "Text", "", 17f, UiTheme.PaperInk, FontStyles.Normal, multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft, multiline);
            UiKit.Fill(text.rectTransform);
            var ph = UiKit.Label(area, "Placeholder", placeholder, 17f, UiTheme.PaperMuted, FontStyles.Italic, multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft, multiline);
            UiKit.Fill(ph.rectTransform);
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = ph;
            input.fontAsset = UiKit.Font;
            input.pointSize = 17f;
            input.targetGraphic = bg;
            return input;
        }

        private void BuildFillBlankAnswer(ExamManager manager, ExamQuestion question, bool locked)
        {
            var existing = manager.GetAnswer(question.id);
            var input = CreateInputField(_answerRoot, 0f, 0f, Mathf.Min(560f, _answerRoot.sizeDelta.x), 56f, Loc.T("Nhập câu trả lời", "Type your answer", "答えを入力"), false);
            input.text = existing != null ? existing.givenAnswer : string.Empty;
            input.interactable = !locked;
            input.onEndEdit.AddListener(text => manager.SubmitFillBlank(text));
        }

        private void BuildEssayAnswer(ExamManager manager, ExamQuestion question, bool locked)
        {
            float width = _answerRoot.sizeDelta.x;
            float offset = 0f;
            string task = Loc.Pick(question.taskInstructionsVi, question.taskInstructionsEn, question.taskInstructionsEn);
            if (!string.IsNullOrWhiteSpace(task))
            {
                var t = UiKit.Label(_answerRoot, "Task", task, 15f, UiTheme.PaperMuted, FontStyles.Italic, TextAlignmentOptions.TopLeft, wrap: true);
                t.ForceMeshUpdate();
                float h = Mathf.Max(24f, t.GetPreferredValues(task, width, 0f).y + 4f);
                UiKit.Place(t.rectTransform, 0f, 0f, width, h);
                offset = h + 12f;
            }
            var existing = manager.GetAnswer(question.id);
            string initial = existing != null ? existing.givenAnswer : string.Empty;
            float inputHeight = Mathf.Max(100f, _answerRoot.sizeDelta.y - offset - 34f);
            var input = CreateInputField(_answerRoot, 0f, offset, width, inputHeight, Loc.T("Viết câu trả lời của bạn...", "Write your answer...", "回答を書いてください..."), true);
            input.text = initial;
            input.interactable = !locked;
            _activeEssayInput = input;
            var count = UiKit.Label(_answerRoot, "WordCount", "", 13f, UiTheme.PaperMuted);
            UiKit.Place(count.rectTransform, 0f, offset + inputHeight + 6f, width, 22f);
            UpdateWordCount(count, initial, question.minWords);
            input.onValueChanged.AddListener(text => UpdateWordCount(count, text, question.minWords));
            input.onEndEdit.AddListener(text => manager.SubmitEssayText(text));
        }

        private static void UpdateWordCount(TextMeshProUGUI label, string text, int minWords)
        {
            int words = string.IsNullOrWhiteSpace(text) ? 0 : text.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries).Length;
            label.text = minWords > 0 ? Loc.F("{0}/{1} từ", "{0}/{1} words", "{0}/{1}語", words, minWords) : Loc.F("{0} từ", "{0} words", "{0}語", words);
            label.color = minWords > 0 && words < minWords ? UiTheme.Bad : UiTheme.Good;
        }

        private void BuildSpeakingAnswer(ExamManager manager, ExamQuestion question, bool locked)
        {
            float width = _answerRoot.sizeDelta.x;
            float offset = 0f;
            string task = Loc.Pick(question.taskInstructionsVi, question.taskInstructionsEn, question.taskInstructionsEn);
            if (!string.IsNullOrWhiteSpace(task))
            {
                var t = UiKit.Label(_answerRoot, "Task", task, 15f, UiTheme.PaperMuted, FontStyles.Italic, TextAlignmentOptions.TopLeft, wrap: true);
                float h = Mathf.Max(24f, t.GetPreferredValues(task, width, 0f).y + 4f);
                UiKit.Place(t.rectTransform, 0f, 0f, width, h);
                offset = h + 16f;
            }
            var existing = manager.GetAnswer(question.id);
            bool alreadyRecorded = existing != null && existing.givenAnswer == "[recording]";
            _recordButton = UiKit.Button(_answerRoot, "Record", null, null, null, () =>
            {
                if (_isRecording) StopRecordingAndSubmit(manager);
                else StartRecording();
            }, UiKit.Kind.Danger, "mic", 17f, 26);
            UiKit.Place((RectTransform)_recordButton.transform, 0f, offset, 260f, 56f);
            _recordButton.interactable = !locked;
            _recordStatusText = UiKit.Label(_answerRoot, "RecordStatus", "", 15f, UiTheme.PaperMuted, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, wrap: true);
            UiKit.Place(_recordStatusText.rectTransform, 280f, offset, width - 280f, 56f);
            SetSpeakingUiState(alreadyRecorded ? "recorded" : "idle");
            var note = UiKit.Label(_answerRoot, "Note",
                "Lưu ý: điểm Speaking ở đây chỉ dựa trên nội dung câu trả lời — không đánh giá được phát âm hay ngữ điệu qua bản ghi. Muốn được chấm đầy đủ, hãy thi trực tiếp với giám khảo.",
                "Note: this Speaking score reflects content only — pronunciation and intonation cannot be judged from a transcript. For full marking, take the live exam with an examiner.",
                "注意：ここでのスピーキング点は内容のみに基づきます（発音・イントネーションは評価できません）。完全な採点は試験官とのライブ試験で。",
                13f, UiTheme.Warn, FontStyles.Italic, TextAlignmentOptions.TopLeft, wrap: true);
            UiKit.Place(note.rectTransform, 0f, offset + 76f, width, 60f);
        }

        private void SetSpeakingUiState(string state)
        {
            if (_recordButton == null) return;
            switch (state)
            {
                case "recording":
                    UiKit.SetLabel(_recordButton, "Dừng ghi âm", "Stop recording", "録音を停止");
                    LocText.Set(_recordStatusText, "Đang ghi âm...", "Recording...", "録音中...");
                    break;
                case "recorded":
                    UiKit.SetLabel(_recordButton, "Ghi âm lại", "Record again", "録音し直す");
                    LocText.Set(_recordStatusText, "Đã ghi âm xong.", "Recording captured.", "録音済みです。");
                    break;
                default:
                    UiKit.SetLabel(_recordButton, "Bắt đầu ghi âm", "Start recording", "録音を開始");
                    LocText.Set(_recordStatusText, "Chưa ghi âm.", "Not recorded yet.", "まだ録音していません。");
                    break;
            }
        }

        private void StartRecording()
        {
            if (!_recorder.Start(MaxRecordSeconds))
            {
                LocText.Clear(_recordStatusText);
                _recordStatusText.text = _recorder.LastError ?? Loc.T("Không thể bắt đầu ghi âm.", "Could not start recording.", "録音を開始できません。");
                return;
            }
            _isRecording = true;
            SetSpeakingUiState("recording");
        }

        private void StopRecordingAndSubmit(ExamManager manager)
        {
            if (!_isRecording) return;
            var samples = _recorder.Stop(out int frequency);
            _isRecording = false;
            if (samples.Length > 0) manager.SubmitSpeakingRecording(samples, frequency);
            else SetSpeakingUiState("idle");
        }

        private void StopRecordingIfNeeded()
        {
            if (!_isRecording) return;
            _recorder.Cancel();
            _isRecording = false;
        }

        // ──────────────────────── Results ────────────────────────

        private void ShowResults(ExamAttemptResult result)
        {
            _resultsRoot.SetActive(true);
            _resultsRoot.transform.SetAsLastSibling();
            SetExamScreenVisible(false);
            var exam = result.exam;
            var record = result.record;
            bool jlpt = exam != null && exam.examType == ExamType.Jlpt;
            Color family = jlpt ? UiTheme.Jlpt : UiTheme.Ielts;

            foreach (Transform child in _resultsHero) if (child.name == "Ring") Destroy(child.gameObject);
            var ring = UiKit.ProgressRing(_resultsHero, "Ring", 150f, jlpt ? (record.passed ? UiTheme.Good : UiTheme.Bad) : UiTheme.Gold, new Color(1f, 1f, 1f, 0.08f));
            var ringRoot = (RectTransform)ring.transform.parent;
            ringRoot.name = "Ring";
            UiKit.Place(ringRoot, 40f, 20f, 150f, 150f);
            var big = UiKit.Label(ringRoot, "Value", jlpt ? record.totalScore.ToString() : record.estimatedBand.ToString("0.0"), 40f, UiTheme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Fill(big.rectTransform, 0f, 0f, 0f, 18f);
            var small = UiKit.Label(ringRoot, "Of", jlpt ? "/ " + record.totalMaxScore : "Band", 13f, UiTheme.Muted, FontStyles.Bold, TextAlignmentOptions.Center);
            UiKit.Fill(small.rectTransform, 0f, 92f, 0f, 0f);
            StartCoroutine(AnimateRing(ring, jlpt ? (record.totalMaxScore > 0 ? (float)record.totalScore / record.totalMaxScore : 0f) : record.estimatedBand / 9f));

            if (jlpt)
            {
                _resultsHeadlineText.text = record.passed ? Loc.T("ĐẠT", "PASS", "合格") : Loc.T("CHƯA ĐẠT", "NOT YET", "不合格");
                _resultsHeadlineText.color = record.passed ? UiTheme.Good : UiTheme.Bad;
                _resultsSubText.text = Loc.F("Tổng điểm {0}/{1} · cần {2} điểm và không phần nào dưới điểm liệt.",
                    "Total {0}/{1} · pass needs {2} points and no division under its minimum.",
                    "総合 {0}/{1}点 · 合格には{2}点以上かつ各区分の基準点が必要です。", record.totalScore, record.totalMaxScore, exam != null ? exam.jlptTotalPassScore : 80);
            }
            else
            {
                _resultsHeadlineText.text = $"Band {record.estimatedBand:0.0}";
                _resultsHeadlineText.color = UiTheme.Gold;
                _resultsSubText.text = Loc.T(
                    "Band ước tính để luyện tập — không phải điểm IELTS chính thức và không liên kết với IELTS/British Council/IDP.",
                    "An estimated band for practice — not an official IELTS score and not affiliated with IELTS/British Council/IDP.",
                    "練習用の推定バンドです。公式のIELTSスコアではなく、IELTS/British Council/IDPとは無関係です。");
            }

            UiKit.Clear(_resultsSectionList);
            float y = 0f;
            if (jlpt && record.groups.Count > 0)
                foreach (var group in record.groups)
                {
                    bool ok = group.scored >= group.passMin;
                    var name = UiKit.Label(_resultsSectionList, "Group_" + group.groupId, ExamAnalysis.GroupTitle(exam, group.groupId), 14f, UiTheme.TextSoft, FontStyles.Bold);
                    UiKit.Place(name.rectTransform, 0f, y, 330f, 22f);
                    var val = UiKit.Label(_resultsSectionList, "Val_" + group.groupId, $"{group.scored}/{group.max}", 14f, ok ? UiTheme.Good : UiTheme.Bad, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
                    UiKit.Place(val.rectTransform, 330f, y, 70f, 22f);
                    var fill = UiKit.ProgressBar(_resultsSectionList, "Bar_" + group.groupId, ok ? UiTheme.Good : UiTheme.Bad, new Color(1f, 1f, 1f, 0.07f));
                    UiKit.Place((RectTransform)fill.parent, 0f, y + 26f, 400f, 9f);
                    UiKit.SetProgress(fill, group.max > 0 ? (float)group.scored / group.max : 0f);
                    var min = UiKit.Label(_resultsSectionList, "Min_" + group.groupId, (System.Func<string>)(() => Loc.F("điểm liệt {0}", "minimum {0}", "基準点 {0}", group.passMin)), 11.5f, UiTheme.Muted);
                    UiKit.Place(min.rectTransform, 0f, y + 38f, 400f, 18f);
                    y += 64f;
                }
            else if (exam != null)
                foreach (var sectionResult in record.sections)
                {
                    var section = exam.FindSection(sectionResult.sectionId);
                    string title = section != null ? Loc.Pick(section.titleVi, section.titleEn, section.titleJa) : sectionResult.sectionId;
                    float band = sectionResult.aiGraded ? sectionResult.scoredPoints / 10f : (sectionResult.maxPoints > 0 ? (float)sectionResult.scoredPoints / sectionResult.maxPoints * 9f : 0f);
                    var line = UiKit.Label(_resultsSectionList, "Section_" + sectionResult.sectionId, $"{title}: <b>Band {band:0.0}</b>", 14f, UiTheme.TextSoft);
                    UiKit.Place(line.rectTransform, 0f, y, 400f, 24f);
                    y += 28f;
                }

            BuildReviewList(exam, record);
        }

        private static IEnumerator AnimateRing(Image ring, float target)
        {
            for (float t = 0f; t < 1f && ring != null; t += Time.unscaledDeltaTime / 0.9f)
            {
                ring.fillAmount = Mathf.Lerp(0f, Mathf.Clamp01(target), 1f - Mathf.Pow(1f - t, 3f));
                yield return null;
            }
            if (ring != null) ring.fillAmount = Mathf.Clamp01(target);
        }

        private void BuildReviewList(ExamDefinition exam, ExamAttemptRecord record)
        {
            UiKit.Clear(_reviewListContent);
            if (exam == null) return;
            if (exam.examType == ExamType.Jlpt) AddAnalysis(exam, record);

            var head = UiKit.Label(_reviewListContent, "ReviewHead", "Xem lại từng câu", "Question by question", "一問ずつ見直し", 17f, UiTheme.Gold, FontStyles.Bold);
            UiKit.Size(head, -1f, 30f);
            foreach (var section in exam.sections)
            {
                var shownPassages = new HashSet<string>();
                bool listening = section.type == ExamSectionType.JlptListening || section.type == ExamSectionType.IeltsListening;
                foreach (var question in section.questions)
                {
                    var passage = listening ? section.FindPassage(question.passageId) : null;
                    if (passage != null && shownPassages.Add(passage.id))
                    {
                        string gloss = Loc.Gloss(passage.bodyVi, passage.bodyEn);
                        string script = $"<color=#C9B57A><b>{Loc.T("Lời thoại", "Script", "スクリプト")} · {Loc.Pick(passage.titleVi, passage.titleEn, passage.titleJa)}</b></color>\n{passage.bodyJa}" +
                                        (string.IsNullOrWhiteSpace(gloss) || gloss == passage.bodyJa ? "" : $"\n<color=#8FA0AE>{gloss}</color>");
                        ReviewCard(script, UiTheme.Muted, "headphones");
                    }
                    var answer = record.answers.Find(a => a.questionId == question.id);
                    string prompt = Loc.Pick(question.promptVi, question.promptEn, question.promptJa);
                    string given = answer != null ? (string.IsNullOrWhiteSpace(answer.rawAnswerText) ? answer.givenAnswer : answer.rawAnswerText) : Loc.T("(Không trả lời)", "(No answer)", "(未回答)");
                    if (answer != null && int.TryParse(answer.givenAnswer, out int gi) && gi >= 0 && gi < question.choices.Count)
                        given = $"{gi + 1}. {Loc.Pick(question.choices[gi].textVi, question.choices[gi].textEn, question.choices[gi].textJa)}";
                    string correct = question.correctChoiceIndex >= 0 && question.correctChoiceIndex < question.choices.Count
                        ? $"{question.correctChoiceIndex + 1}. {Loc.Pick(question.choices[question.correctChoiceIndex].textVi, question.choices[question.correctChoiceIndex].textEn, question.choices[question.correctChoiceIndex].textJa)}" : null;
                    string explanation = Loc.Pick(question.explanationVi, question.explanationEn, question.explanationEn);
                    if (Loc.IsJapanese) explanation = ""; // explanations exist in vi/en only: never mix them into a Japanese screen
                    bool objective = question.type == ExamQuestionType.MultipleChoice || question.type == ExamQuestionType.TrueFalseNotGiven || question.type == ExamQuestionType.FillBlank;
                    bool right = answer != null && answer.correct;
                    string body = $"<b>{prompt}</b>\n{Loc.T("Bạn chọn", "Your answer", "あなたの答え")}: {given}" +
                                  (objective && !right && correct != null ? $"\n<color=#3FB27F>{Loc.T("Đáp án", "Correct", "正解")}: {correct}</color>" : "") +
                                  (!string.IsNullOrWhiteSpace(explanation) ? $"\n<color=#8FA0AE>{explanation}</color>" : string.Empty);
                    ReviewCard(body, objective ? (right ? UiTheme.Good : UiTheme.Bad) : UiTheme.Muted, objective ? (right ? "check_circle" : "cancel") : "info");
                }
            }
        }

        private void ReviewCard(string text, Color accent, string icon)
        {
            var card = UiKit.Card(_reviewListContent, "Review", UiTheme.Surface, 14, 0f, false);
            var rect = card.rectTransform;
            var layout = UiKit.Column(rect, 0f, new RectOffset(62, 20, 14, 14));
            layout.childForceExpandWidth = true;
            var t = UiKit.Label(rect, "Text", text, 15f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            t.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var ic = UiKit.Icon(rect, icon, accent, 26f);
            ic.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiKit.Place(ic.rectTransform, 20f, 14f, 26f, 26f);
            var stripe = UiKit.Image(rect, "Stripe", accent, UiKit.Rounded(3));
            stripe.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            stripe.rectTransform.anchorMin = new Vector2(0f, 0f); stripe.rectTransform.anchorMax = new Vector2(0f, 1f);
            stripe.rectTransform.pivot = new Vector2(0f, 0.5f);
            stripe.rectTransform.offsetMin = new Vector2(0f, 12f); stripe.rectTransform.offsetMax = new Vector2(4f, -12f);
            rect.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>Per-もんだい table and concrete advice (ExamAnalysis), at the top of the scrolling review.</summary>
        private void AddAnalysis(ExamDefinition exam, ExamAttemptRecord record)
        {
            var analysis = ExamAnalysis.Analyze(exam, record);
            if (analysis.mondai.Count == 0) return;
            var card = UiKit.Card(_reviewListContent, "Analysis", UiTheme.Surface, 16, 0f, false);
            var rect = card.rectTransform;
            UiKit.Column(rect, 8f, new RectOffset(24, 24, 18, 20));
            rect.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var title = UiKit.Label(rect, "Title", Loc.T("Phân tích từng dạng bài", "Analysis by question type", "問題タイプ別の分析") + (string.IsNullOrEmpty(analysis.status) ? "" : $"  ·  {analysis.status}"), 17f, UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.TopLeft, wrap: true);
            title.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            foreach (var m in analysis.mondai)
            {
                var row = UiKit.Rect(rect, "Mondai");
                UiKit.Size(row, -1f, 28f);
                var name = UiKit.Label(row, "Name", Loc.IsJapanese ? m.titleJa : $"{m.titleJa}  <color=#8FA0AE>{(Loc.IsEnglish ? "" : m.titleVi)}</color>", 14f, UiTheme.TextSoft);
                UiKit.Place(name.rectTransform, 0f, 2f, 520f, 24f);
                var color = m.Accuracy < 0.5f ? UiTheme.Bad : m.Accuracy < 0.8f ? UiTheme.Warn : UiTheme.Good;
                var fill = UiKit.ProgressBar(row, "Bar", color, new Color(1f, 1f, 1f, 0.07f));
                UiKit.Place((RectTransform)fill.parent, 540f, 10f, 360f, 9f);
                UiKit.SetProgress(fill, m.Accuracy);
                var val = UiKit.Label(row, "Val", $"{m.correct}/{m.total}", 14f, color, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
                UiKit.Place(val.rectTransform, 910f, 2f, 70f, 24f);
            }
            foreach (var line in analysis.advice)
            {
                var a = UiKit.Label(rect, "Advice", "• " + line, 14.5f, new Color(0.95f, 0.92f, 0.8f, 1f), FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
                a.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }
    }
}
