using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NihongoLife.Exam;

namespace NihongoLife.UI
{
    /// <summary>
    /// The exam-taking screen: renders whatever ExamManager.Instance says is the current section/question,
    /// a per-section timer, a question palette, the right input widget for the question type, and the
    /// results screen once ExamManager finishes grading. Purely a view over ExamManager — it owns no exam
    /// state itself, so closing and reopening it (ExamCenterPopup resumes rather than restarts an attempt
    /// still in progress) never loses answers already submitted to the manager.
    /// </summary>
    public class ExamPlayUI : MenuPopupBase
    {
        private const int RecordSampleRate = 44100;
        private const int MaxRecordSeconds = 120;

        // Header
        private TextMeshProUGUI _sectionTitleText;
        private TextMeshProUGUI _timerText;

        // Palette
        private RectTransform _paletteRoot;

        // Passage (left panel, shown when the current question references one)
        private GameObject _passagePanelRoot;
        private RectTransform _passageContent;

        // Question (right panel)
        private RectTransform _questionPanel;
        private TextMeshProUGUI _promptText;
        private TextMeshProUGUI _promptReadingText;
        private RectTransform _answerRoot;

        // Footer
        private Button _prevButton;
        private Button _nextButton;
        private Button _submitSectionButton;
        private TextMeshProUGUI _statusText;

        // Grading overlay
        private GameObject _gradingOverlay;
        private TextMeshProUGUI _gradingText;

        // Results
        private GameObject _resultsRoot;
        private TextMeshProUGUI _resultsHeadlineText;
        private TextMeshProUGUI _resultsSubText;
        private RectTransform _resultsSectionList;
        private RectTransform _reviewListContent;
        private ExamAttemptResult _lastResult;

        // Essay in-progress text (committed on navigation instead of on every keystroke, so the input
        // field is never rebuilt mid-typing by the OnStateChanged-driven refresh).
        private TMP_InputField _activeEssayInput;

        // Speaking recording
        private bool _isRecording;
        private string _micDevice;
        private AudioClip _recordingClip;
        private Button _recordButton;
        private TextMeshProUGUI _recordStatusText;

        protected override Vector2 CardSize => new Vector2(1300f, 800f);

        public static ExamPlayUI GetOrCreate(Transform host)
        {
            var existing = FindFirstObjectByType<ExamPlayUI>(FindObjectsInactive.Include);
            if (existing != null) return existing;

            var instance = host.gameObject.AddComponent<ExamPlayUI>();
            instance.Initialize(ResolveFont());
            return instance;
        }

        private static TMP_FontAsset ResolveFont()
        {
            var anyText = FindFirstObjectByType<TextMeshProUGUI>();
            return anyText != null ? anyText.font : TMP_Settings.defaultFontAsset;
        }

        protected override void GetTitle(out string vi, out string en, out string ja)
        {
            vi = "Bài thi";
            en = "Exam";
            ja = "試験";
        }

        // ──────────────────────── Build ────────────────────────

        protected override void Build(RectTransform card)
        {
            _sectionTitleText = AddText(card, string.Empty, 22f, 36f, 96f, 700f, 30f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, false, Gold);
            _timerText = AddText(card, string.Empty, 20f, 760f, 96f, 504f, 30f, TextAlignmentOptions.MidlineRight, FontStyles.Bold);

            BuildPaletteRoot(card);
            BuildPassagePanel(card);
            BuildQuestionPanel(card);
            BuildFooter(card);
            BuildGradingOverlay(card);
            BuildResultsPanel(card);
        }

        private void BuildPaletteRoot(RectTransform card)
        {
            var go = new GameObject("Palette", typeof(RectTransform), typeof(GridLayoutGroup));
            go.transform.SetParent(card, false);
            _paletteRoot = (RectTransform)go.transform;
            Place(_paletteRoot, 36f, 136f, 1228f, 34f);
            var grid = go.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(34f, 34f);
            grid.spacing = new Vector2(6f, 6f);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
        }

        private void BuildPassagePanel(RectTransform card)
        {
            _passageContent = BuildScrollScaffold(card, "PassagePanel", 36f, 182f, 560f, 468f, out GameObject root);
            _passagePanelRoot = root;
        }

        private void BuildQuestionPanel(RectTransform card)
        {
            var go = new GameObject("QuestionPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(card, false);
            _questionPanel = (RectTransform)go.transform;
            go.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 0.55f);
            Place(_questionPanel, 612f, 182f, 628f, 468f);

            _promptText = AddText(_questionPanel, string.Empty, 19f, 20f, 16f, 588f, 90f, TextAlignmentOptions.TopLeft, FontStyles.Bold, true);
            _promptReadingText = AddText(_questionPanel, string.Empty, 15f, 20f, 96f, 588f, 26f, TextAlignmentOptions.TopLeft, FontStyles.Italic, false, Muted);

            var answerGo = new GameObject("AnswerRoot", typeof(RectTransform));
            answerGo.transform.SetParent(_questionPanel, false);
            _answerRoot = (RectTransform)answerGo.transform;
            Place(_answerRoot, 20f, 130f, 588f, 320f);
        }

        private void ApplyPassageLayout(bool hasPassage)
        {
            _passagePanelRoot.SetActive(hasPassage);
            float panelWidth = hasPassage ? 628f : 1204f;
            float panelX = hasPassage ? 612f : 36f;
            Place(_questionPanel, panelX, 182f, panelWidth, 468f);

            float innerWidth = panelWidth - 40f;
            Place((RectTransform)_promptText.transform, 20f, 16f, innerWidth, 90f);
            Place((RectTransform)_promptReadingText.transform, 20f, 96f, innerWidth, 26f);
            Place(_answerRoot, 20f, 130f, innerWidth, 320f);
        }

        private void BuildFooter(RectTransform card)
        {
            _prevButton = AddButton(card, string.Empty, 36f, 706f, 140f, 46f, false, 16f);
            _prevButton.onClick.AddListener(() => { CommitActiveEssayIfAny(); ExamManager.Instance?.PreviousQuestion(); });

            _nextButton = AddButton(card, string.Empty, 186f, 706f, 140f, 46f, false, 16f);
            _nextButton.onClick.AddListener(() => { CommitActiveEssayIfAny(); ExamManager.Instance?.NextQuestion(); });

            _statusText = AddText(card, string.Empty, 14f, 340f, 706f, 540f, 56f, TextAlignmentOptions.TopLeft, FontStyles.Italic, true, Muted);

            _submitSectionButton = AddButton(card, string.Empty, 1090f, 706f, 174f, 46f, true, 16f);
            _submitSectionButton.onClick.AddListener(() => { CommitActiveEssayIfAny(); ExamManager.Instance?.SubmitCurrentSection(); });
        }

        private void BuildGradingOverlay(RectTransform card)
        {
            _gradingOverlay = new GameObject("GradingOverlay", typeof(RectTransform), typeof(Image));
            _gradingOverlay.transform.SetParent(card, false);
            var rect = (RectTransform)_gradingOverlay.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _gradingOverlay.GetComponent<Image>().color = new Color(0.01f, 0.014f, 0.02f, 0.9f);

            _gradingText = AddText(_gradingOverlay.transform, string.Empty, 22f, 60f, 0f, CardSize.x - 120f, CardSize.y, TextAlignmentOptions.Center, FontStyles.Bold, true, Gold);
            _gradingOverlay.SetActive(false);
        }

        private void BuildResultsPanel(RectTransform card)
        {
            _resultsRoot = new GameObject("ResultsPanel", typeof(RectTransform), typeof(Image));
            _resultsRoot.transform.SetParent(card, false);
            var rect = (RectTransform)_resultsRoot.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _resultsRoot.GetComponent<Image>().color = new Color(0.045f, 0.055f, 0.07f, 0.99f);

            _resultsHeadlineText = AddText(_resultsRoot.transform, string.Empty, 34f, 40f, 30f, 800f, 50f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, false, Gold);
            _resultsSubText = AddText(_resultsRoot.transform, string.Empty, 16f, 40f, 88f, 1220f, 34f, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, true);

            var sectionListGo = new GameObject("SectionSummary", typeof(RectTransform));
            sectionListGo.transform.SetParent(_resultsRoot.transform, false);
            _resultsSectionList = (RectTransform)sectionListGo.transform;
            Place(_resultsSectionList, 40f, 134f, 1220f, 140f);

            _reviewListContent = BuildScrollScaffold(_resultsRoot.transform, "ReviewList", 40f, 284f, 1220f, 400f, out GameObject _);

            var closeButton = AddButton(_resultsRoot.transform, string.Empty, 1090f, 690f, 174f, 46f, true, 16f);
            closeButton.GetComponentInChildren<TextMeshProUGUI>().text = Pick("Đóng", "Close", "閉じる");
            closeButton.onClick.AddListener(Hide);

            _resultsRoot.SetActive(false);
        }

        /// <summary>Builds a scrollable, top-anchored content area (viewport + auto-growing vertical list),
        /// matching the pattern used by ExamCenterPopup and QuestLogPopup.</summary>
        private RectTransform BuildScrollScaffold(Transform parent, string name, float x, float y, float w, float h, out GameObject root)
        {
            root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            root.transform.SetParent(parent, false);
            Place((RectTransform)root.transform, x, y, w, h);
            root.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 0.7f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = viewportRect.offsetMax = Vector2.zero;

            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportRect, false);
            var content = (RectTransform)contentObject.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            var layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = root.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return content;
        }

        private TMP_InputField CreateInputField(Transform parent, float x, float y, float width, float height, string placeholder, bool multiline)
        {
            var go = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, x, y, width, height);
            go.GetComponent<Image>().color = new Color(0.09f, 0.11f, 0.13f, 1f);

            var input = go.GetComponent<TMP_InputField>();
            input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;

            var viewportGo = new GameObject("TextViewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(go.transform, false);
            var viewportRect = (RectTransform)viewportGo.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(10f, 6f);
            viewportRect.offsetMax = new Vector2(-10f, -6f);
            input.textViewport = viewportRect;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(viewportRect, false);
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<TextMeshProUGUI>();
            if (Font != null) text.font = Font;
            text.fontSize = 16f;
            text.color = new Color(0.92f, 0.96f, 1f, 1f);
            text.alignment = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = multiline ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            input.textComponent = text;

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderGo.transform.SetParent(viewportRect, false);
            var placeholderRect = (RectTransform)placeholderGo.transform;
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = placeholderRect.offsetMax = Vector2.zero;
            var placeholderText = placeholderGo.GetComponent<TextMeshProUGUI>();
            if (Font != null) placeholderText.font = Font;
            placeholderText.fontSize = 16f;
            placeholderText.color = new Color(0.5f, 0.55f, 0.6f, 0.75f);
            placeholderText.alignment = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
            placeholderText.text = placeholder;
            input.placeholder = placeholderText;

            return input;
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
            RefreshAll();
        }

        protected override void OnClosed()
        {
            var manager = ExamManager.Instance;
            if (manager != null)
            {
                manager.OnStateChanged -= HandleStateChanged;
                manager.OnTimerTick -= HandleTimerTick;
                manager.OnExamFinished -= HandleExamFinished;
                manager.OnGradingStatus -= HandleGradingStatus;
            }

            CommitActiveEssayIfAny();
            StopRecordingIfNeeded();
        }

        protected override void OnLanguageApplied()
        {
            if (PanelObject == null || !PanelObject.activeSelf) return;
            RefreshAll();
            if (_resultsRoot.activeSelf && _lastResult != null) ShowResults(_lastResult);
        }

        private void HandleStateChanged() => RefreshAll();

        private void HandleTimerTick(float remaining, float duration)
        {
            if (duration <= 0f)
            {
                _timerText.text = Pick("Không giới hạn thời gian", "No time limit", "時間制限なし");
                _timerText.color = Muted;
                return;
            }

            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            _timerText.text = $"{Pick("Thời gian", "Time", "残り時間")}: {minutes:00}:{seconds:00}";
            _timerText.color = remaining <= 30f ? new Color(0.95f, 0.35f, 0.3f, 1f) : Gold;
        }

        private void HandleGradingStatus(string status)
        {
            bool active = !string.IsNullOrEmpty(status);
            _gradingOverlay.SetActive(active);
            if (active) _gradingText.text = status;
        }

        private void HandleExamFinished(ExamAttemptResult result)
        {
            _lastResult = result;
            _gradingOverlay.SetActive(false);
            ShowResults(result);
        }

        private void CommitActiveEssayIfAny()
        {
            if (_activeEssayInput != null && ExamManager.Instance != null)
            {
                ExamManager.Instance.SubmitEssayText(_activeEssayInput.text);
            }
        }

        // ──────────────────────── Refresh: header / palette / passage ────────────────────────

        private void RefreshAll()
        {
            var manager = ExamManager.Instance;
            if (manager == null || !manager.IsAttemptActive) return;

            var section = manager.CurrentSection;
            var question = manager.CurrentQuestion;
            if (section == null) return;

            _sectionTitleText.text = $"{Pick(section.titleVi, section.titleEn, section.titleJa)}  ({manager.QuestionIndex + 1}/{section.questions.Count})";

            if (section.timeLimitSeconds <= 0)
            {
                _timerText.text = Pick("Không giới hạn thời gian", "No time limit", "時間制限なし");
                _timerText.color = Muted;
            }

            bool locked = manager.IsCurrentSectionLocked;
            _statusText.text = locked
                ? Pick("Phần này đã được nộp.", "This section has been submitted.", "このセクションは提出済みです。")
                : string.Empty;

            _prevButton.GetComponentInChildren<TextMeshProUGUI>().text = Pick("< Trước", "< Prev", "< 前へ");
            _nextButton.GetComponentInChildren<TextMeshProUGUI>().text = Pick("Sau >", "Next >", "次へ >");
            _submitSectionButton.GetComponentInChildren<TextMeshProUGUI>().text = Pick("Nộp phần này", "Submit section", "このセクションを提出");

            _prevButton.interactable = !locked && manager.QuestionIndex > 0;
            _nextButton.interactable = !locked && manager.QuestionIndex < section.questions.Count - 1;
            _submitSectionButton.interactable = !locked;

            RefreshPalette(manager, section);
            RefreshPassage(manager, section, question);
            RefreshQuestion(manager, question, locked);
        }

        private void RefreshPalette(ExamManager manager, ExamSection section)
        {
            for (int i = _paletteRoot.childCount - 1; i >= 0; i--) Destroy(_paletteRoot.GetChild(i).gameObject);

            for (int i = 0; i < section.questions.Count; i++)
            {
                int index = i;
                bool answered = manager.GetAnswer(section.questions[i].id) != null;
                bool current = index == manager.QuestionIndex;

                var button = new GameObject("Q" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                button.transform.SetParent(_paletteRoot, false);
                var image = button.GetComponent<Image>();
                image.color = current ? Gold : (answered ? new Color(0.2f, 0.45f, 0.3f, 1f) : new Color(0.13f, 0.17f, 0.2f, 1f));
                var btn = button.GetComponent<Button>();
                btn.targetGraphic = image;
                btn.onClick.AddListener(() => { CommitActiveEssayIfAny(); ExamManager.Instance?.GoToQuestion(index); });

                var label = AddText(button.transform, (i + 1).ToString(), 13f, 0f, 0f, 34f, 34f, TextAlignmentOptions.Center, FontStyles.Bold, false, current ? DarkText : Color.white);
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
            }
        }

        private void RefreshPassage(ExamManager manager, ExamSection section, ExamQuestion question)
        {
            var passage = question != null ? section.FindPassage(question.passageId) : null;
            bool hasPassage = passage != null;
            ApplyPassageLayout(hasPassage);
            if (!hasPassage) return;

            for (int i = _passageContent.childCount - 1; i >= 0; i--) Destroy(_passageContent.GetChild(i).gameObject);

            var titleText = AddText(_passageContent, Pick(passage.titleVi, passage.titleEn, passage.titleJa), 18f, 0f, 0f, 520f, 30f, TextAlignmentOptions.TopLeft, FontStyles.Bold, false, Gold);
            titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

            bool isListening = section.type == ExamSectionType.JlptListening || section.type == ExamSectionType.IeltsListening;
            if (isListening && passage.maxPlays > 0)
            {
                int remaining = manager.RemainingPlays(passage);
                var playRow = new GameObject("PlayRow", typeof(RectTransform), typeof(LayoutElement));
                playRow.transform.SetParent(_passageContent, false);
                playRow.GetComponent<LayoutElement>().preferredHeight = 44f;
                var playButton = AddButton(playRow.transform, string.Empty, 0f, 0f, 220f, 40f, true, 15f);
                playButton.GetComponentInChildren<TextMeshProUGUI>().text = $"{Pick("Nghe", "Play", "再生")} ({remaining})";
                playButton.interactable = remaining > 0;
                playButton.onClick.AddListener(() =>
                {
                    manager.TryConsumePassagePlay(passage);
                    RefreshPassage(manager, section, question);
                });
            }

            if (!string.IsNullOrWhiteSpace(passage.bodyReading))
            {
                float readingHeight = EstimateTextHeight(passage.bodyReading, 520f, 14f);
                var readingText = AddText(_passageContent, passage.bodyReading, 14f, 0f, 0f, 520f, readingHeight, TextAlignmentOptions.TopLeft, FontStyles.Italic, true, Muted);
                readingText.gameObject.AddComponent<LayoutElement>().preferredHeight = readingHeight;
            }

            string body = Pick(passage.bodyVi, passage.bodyEn, passage.bodyJa);
            float bodyHeight = EstimateTextHeight(body, 520f, 16f);
            var bodyText = AddText(_passageContent, body, 16f, 0f, 0f, 520f, bodyHeight, TextAlignmentOptions.TopLeft, FontStyles.Normal, true);
            bodyText.gameObject.AddComponent<LayoutElement>().preferredHeight = bodyHeight;
        }

        // ──────────────────────── Refresh: question / answer ────────────────────────

        private void RefreshQuestion(ExamManager manager, ExamQuestion question, bool locked)
        {
            StopRecordingIfNeeded();
            _activeEssayInput = null;
            _recordButton = null;
            _recordStatusText = null;

            for (int i = _answerRoot.childCount - 1; i >= 0; i--) Destroy(_answerRoot.GetChild(i).gameObject);

            if (question == null)
            {
                _promptText.text = string.Empty;
                _promptReadingText.gameObject.SetActive(false);
                return;
            }

            _promptText.text = Pick(question.promptVi, question.promptEn, question.promptJa);
            _promptReadingText.text = question.promptReading ?? string.Empty;
            _promptReadingText.gameObject.SetActive(!string.IsNullOrWhiteSpace(question.promptReading));

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

            const float rowHeight = 52f;
            float width = _answerRoot.sizeDelta.x;
            for (int i = 0; i < question.choices.Count; i++)
            {
                int index = i;
                var choice = question.choices[i];
                var row = new GameObject("Choice" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                row.transform.SetParent(_answerRoot, false);
                Place((RectTransform)row.transform, 0f, i * (rowHeight + 10f), width, rowHeight);
                var image = row.GetComponent<Image>();
                image.color = index == selected ? Gold : new Color(0.1f, 0.13f, 0.16f, 1f);
                var button = row.GetComponent<Button>();
                button.targetGraphic = image;
                button.interactable = !locked;
                button.onClick.AddListener(() => manager.SubmitChoice(index));

                AddText(row.transform, Pick(choice.textVi, choice.textEn, choice.textJa), 16f, 16f, 0f, width - 32f, rowHeight, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, true, index == selected ? DarkText : Color.white);
            }
        }

        private void BuildFillBlankAnswer(ExamManager manager, ExamQuestion question, bool locked)
        {
            var existing = manager.GetAnswer(question.id);
            string initial = existing != null ? existing.givenAnswer : string.Empty;

            var input = CreateInputField(_answerRoot, 0f, 0f, Mathf.Min(500f, _answerRoot.sizeDelta.x), 50f, Pick("Nhập câu trả lời", "Type your answer", "答えを入力"), false);
            input.text = initial;
            input.interactable = !locked;
            input.onEndEdit.AddListener(text => manager.SubmitFillBlank(text));
        }

        private void BuildEssayAnswer(ExamManager manager, ExamQuestion question, bool locked)
        {
            float width = _answerRoot.sizeDelta.x;
            float taskOffset = 0f;
            string task = Pick(question.taskInstructionsVi, question.taskInstructionsEn, question.taskInstructionsEn);
            if (!string.IsNullOrWhiteSpace(task))
            {
                float taskHeight = EstimateTextHeight(task, width, 14f);
                AddText(_answerRoot, task, 14f, 0f, 0f, width, taskHeight, TextAlignmentOptions.TopLeft, FontStyles.Italic, true, Muted);
                taskOffset = taskHeight + 12f;
            }

            var existing = manager.GetAnswer(question.id);
            string initial = existing != null ? existing.givenAnswer : string.Empty;

            float inputHeight = Mathf.Max(80f, _answerRoot.sizeDelta.y - taskOffset - 30f);
            var input = CreateInputField(_answerRoot, 0f, taskOffset, width, inputHeight, Pick("Viết câu trả lời của bạn...", "Write your answer...", "回答を書いてください..."), true);
            input.text = initial;
            input.interactable = !locked;
            _activeEssayInput = input;

            var wordCountText = AddText(_answerRoot, string.Empty, 12f, 0f, taskOffset + inputHeight + 4f, width, 20f, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, false, Muted);
            UpdateWordCount(wordCountText, initial, question.minWords);

            input.onValueChanged.AddListener(text => UpdateWordCount(wordCountText, text, question.minWords));
            input.onEndEdit.AddListener(text => manager.SubmitEssayText(text));
        }

        private void UpdateWordCount(TextMeshProUGUI label, string text, int minWords)
        {
            int words = string.IsNullOrWhiteSpace(text) ? 0 : text.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries).Length;
            label.text = minWords > 0 ? $"{words}/{minWords} {Pick("từ", "words", "語")}" : $"{words} {Pick("từ", "words", "語")}";
            label.color = minWords > 0 && words < minWords ? new Color(0.9f, 0.5f, 0.4f, 1f) : Muted;
        }

        private void BuildSpeakingAnswer(ExamManager manager, ExamQuestion question, bool locked)
        {
            float width = _answerRoot.sizeDelta.x;
            float taskOffset = 0f;
            string task = Pick(question.taskInstructionsVi, question.taskInstructionsEn, question.taskInstructionsEn);
            if (!string.IsNullOrWhiteSpace(task))
            {
                float taskHeight = EstimateTextHeight(task, width, 15f);
                AddText(_answerRoot, task, 15f, 0f, 0f, width, taskHeight, TextAlignmentOptions.TopLeft, FontStyles.Italic, true, Muted);
                taskOffset = taskHeight + 16f;
            }

            var existing = manager.GetAnswer(question.id);
            bool alreadyRecorded = existing != null && existing.givenAnswer == "[recording]";

            _recordButton = AddButton(_answerRoot, string.Empty, 0f, taskOffset, 220f, 50f, true, 16f);
            _recordButton.interactable = !locked;
            _recordStatusText = AddText(_answerRoot, string.Empty, 14f, 0f, taskOffset + 60f, width, 40f, TextAlignmentOptions.TopLeft, FontStyles.Normal, true, Muted);

            SetSpeakingUiState(alreadyRecorded ? "recorded" : "idle");

            _recordButton.onClick.AddListener(() =>
            {
                if (_isRecording) StopRecordingAndSubmit(manager);
                else StartRecording();
            });

            AddText(_answerRoot, Pick(
                "Lưu ý: điểm Speaking chỉ dựa trên nội dung câu trả lời — không thể đánh giá phát âm hay ngữ điệu qua bản ghi.",
                "Note: the Speaking score reflects content only — pronunciation and intonation cannot be judged from a recording's transcript.",
                "注意：スピーキングの点数は内容のみに基づきます（発音・イントネーションは評価できません）。"),
                12f, 0f, taskOffset + 110f, width, 44f, TextAlignmentOptions.TopLeft, FontStyles.Italic, true, new Color(0.9f, 0.6f, 0.4f, 1f));
        }

        private void SetSpeakingUiState(string state)
        {
            if (_recordButton == null) return;
            var label = _recordButton.GetComponentInChildren<TextMeshProUGUI>();
            switch (state)
            {
                case "recording":
                    label.text = Pick("Dừng ghi âm", "Stop recording", "録音を停止");
                    _recordStatusText.text = Pick("Đang ghi âm...", "Recording...", "録音中...");
                    break;
                case "recorded":
                    label.text = Pick("Ghi âm lại", "Record again", "録音し直す");
                    _recordStatusText.text = Pick("Đã ghi âm xong.", "Recording captured.", "録音済みです。");
                    break;
                default:
                    label.text = Pick("Bắt đầu ghi âm", "Start recording", "録音を開始");
                    _recordStatusText.text = Pick("Chưa ghi âm.", "Not recorded yet.", "まだ録音していません。");
                    break;
            }
        }

        private void StartRecording()
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                _recordStatusText.text = Pick("Không tìm thấy micro.", "No microphone found.", "マイクが見つかりません。");
                return;
            }

            _micDevice = Microphone.devices[0];
            try
            {
                _recordingClip = Microphone.Start(_micDevice, false, MaxRecordSeconds, RecordSampleRate);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[ExamPlayUI] Microphone start failed: " + ex.Message);
                _recordStatusText.text = Pick("Không thể bắt đầu ghi âm.", "Could not start recording.", "録音を開始できません。");
                return;
            }

            _isRecording = true;
            SetSpeakingUiState("recording");
        }

        private void StopRecordingAndSubmit(ExamManager manager)
        {
            if (!_isRecording) return;

            int position = Microphone.GetPosition(_micDevice);
            try { Microphone.End(_micDevice); }
            catch (System.Exception ex) { Debug.LogWarning("[ExamPlayUI] Microphone stop failed: " + ex.Message); }
            _isRecording = false;

            if (_recordingClip != null && position > 0)
            {
                var samples = new float[position];
                _recordingClip.GetData(samples, 0);
                manager.SubmitSpeakingRecording(samples, _recordingClip.frequency);
            }
            else
            {
                SetSpeakingUiState("idle");
            }
        }

        private void StopRecordingIfNeeded()
        {
            if (!_isRecording) return;
            try { Microphone.End(_micDevice); } catch (System.Exception) { /* device already gone; nothing to clean up */ }
            _isRecording = false;
        }

        // ──────────────────────── Results ────────────────────────

        private void ShowResults(ExamAttemptResult result)
        {
            _resultsRoot.SetActive(true);

            var exam = result.exam;
            var record = result.record;

            if (exam != null && exam.examType == ExamType.Jlpt)
            {
                _resultsHeadlineText.text = record.passed ? Pick("ĐẠT", "PASS", "合格") : Pick("CHƯA ĐẠT", "NOT YET", "不合格");
                _resultsHeadlineText.color = record.passed ? new Color(0.4f, 0.85f, 0.5f, 1f) : new Color(0.9f, 0.45f, 0.4f, 1f);
                _resultsSubText.text = $"{Pick("Tổng điểm", "Total score", "総合点")}: {record.totalScore}/{record.totalMaxScore}";
            }
            else
            {
                _resultsHeadlineText.text = $"Band {record.estimatedBand:0.0}";
                _resultsHeadlineText.color = Gold;
                _resultsSubText.text = Pick(
                    "Đây là band điểm ước tính cho mục đích luyện tập, không phải điểm IELTS chính thức và không liên kết với IELTS/British Council/IDP/Cambridge.",
                    "This is an estimated band for practice purposes only — not an official IELTS score and not affiliated with IELTS/British Council/IDP/Cambridge.",
                    "これは練習用の推定バンドスコアであり、公式のIELTSスコアではありません。");
            }

            for (int i = _resultsSectionList.childCount - 1; i >= 0; i--) Destroy(_resultsSectionList.GetChild(i).gameObject);
            float rowY = 0f;
            if (exam != null)
            {
                foreach (var sectionResult in record.sections)
                {
                    var section = exam.FindSection(sectionResult.sectionId);
                    string title = section != null ? Pick(section.titleVi, section.titleEn, section.titleJa) : sectionResult.sectionId;
                    string scoreText;
                    if (exam.examType == ExamType.Jlpt)
                    {
                        scoreText = $"{sectionResult.scoredPoints}/{sectionResult.maxPoints}";
                    }
                    else
                    {
                        float band = sectionResult.aiGraded ? sectionResult.scoredPoints / 10f : (sectionResult.maxPoints > 0 ? (float)sectionResult.scoredPoints / sectionResult.maxPoints * 9f : 0f);
                        scoreText = $"Band {band:0.0}";
                    }

                    AddText(_resultsSectionList, $"{title}: {scoreText}", 16f, 0f, rowY, 1220f, 26f, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
                    rowY += 28f;
                    if (!string.IsNullOrWhiteSpace(sectionResult.feedback))
                    {
                        float feedbackHeight = EstimateTextHeight(sectionResult.feedback, 1220f, 13f);
                        AddText(_resultsSectionList, sectionResult.feedback, 13f, 20f, rowY, 1180f, feedbackHeight, TextAlignmentOptions.TopLeft, FontStyles.Italic, true, Muted);
                        rowY += feedbackHeight + 6f;
                    }
                }
            }

            BuildReviewList(exam, record);
        }

        private void BuildReviewList(ExamDefinition exam, ExamAttemptRecord record)
        {
            for (int i = _reviewListContent.childCount - 1; i >= 0; i--) Destroy(_reviewListContent.GetChild(i).gameObject);
            if (exam == null) return;

            foreach (var section in exam.sections)
            {
                foreach (var question in section.questions)
                {
                    var answer = record.answers.Find(a => a.questionId == question.id);
                    string prompt = Pick(question.promptVi, question.promptEn, question.promptJa);
                    string given = answer != null
                        ? (string.IsNullOrWhiteSpace(answer.rawAnswerText) ? answer.givenAnswer : answer.rawAnswerText)
                        : Pick("(Không trả lời)", "(No answer)", "(未回答)");
                    string explanation = Pick(question.explanationVi, question.explanationEn, question.explanationEn);

                    bool objective = question.type == ExamQuestionType.MultipleChoice || question.type == ExamQuestionType.TrueFalseNotGiven || question.type == ExamQuestionType.FillBlank;
                    string mark = objective ? (answer != null && answer.correct ? "✓" : "✗") : "•";
                    Color markColor = objective ? (answer != null && answer.correct ? new Color(0.4f, 0.85f, 0.5f, 1f) : new Color(0.9f, 0.45f, 0.4f, 1f)) : Muted;

                    string body = $"<b>{mark} {prompt}</b>\n{Pick("Trả lời", "Answer", "回答")}: {given}" +
                                  (!string.IsNullOrWhiteSpace(explanation) ? $"\n<color=#8FA0AE>{explanation}</color>" : string.Empty);
                    float height = EstimateTextHeight(body, 1180f, 15f) + 16f;
                    var text = AddText(_reviewListContent, body, 15f, 0f, 0f, 1180f, height, TextAlignmentOptions.TopLeft, FontStyles.Normal, true, markColor);
                    text.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
                }
            }
        }

        // ──────────────────────── Helpers ────────────────────────

        private static float EstimateTextHeight(string text, float width, float fontSize)
        {
            if (string.IsNullOrWhiteSpace(text)) return fontSize * 1.6f;
            float charsPerLine = Mathf.Max(10f, width / (fontSize * 0.55f));
            int newlineBreaks = 0;
            foreach (char c in text) if (c == '\n') newlineBreaks++;
            int lines = Mathf.CeilToInt(text.Length / charsPerLine) + newlineBreaks;
            return Mathf.Max(fontSize * 1.8f, lines * fontSize * 1.35f + 20f);
        }
    }
}
