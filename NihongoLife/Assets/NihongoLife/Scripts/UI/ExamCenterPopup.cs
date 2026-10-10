using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Exam;
using NihongoLife.Save;

namespace NihongoLife.UI
{
    /// <summary>
    /// Test-prep hub: pick a JLPT level or an IELTS practice test, see your best result, start an attempt.
    /// Exams are entirely data-driven (ExamDefinition assets under Resources/Exams); this popup never needs
    /// to change when a new exam is added. See Docs/EXAM_SYSTEM.md.
    /// </summary>
    public class ExamCenterPopup : MenuPopupBase
    {
        private ExamType _tab = ExamType.Jlpt;
        private Button _jlptTab;
        private Button _ieltsTab;
        private TextMeshProUGUI _jlptTabLabel;
        private TextMeshProUGUI _ieltsTabLabel;
        private RectTransform _listRoot;
        private TextMeshProUGUI _emptyText;

        protected override bool LocksGameplay => true;

        protected override Vector2 CardSize => new Vector2(1180f, 700f);

        protected override void GetTitle(out string vi, out string en, out string ja)
        {
            vi = "Trung tâm luyện thi";
            en = "Test Prep Center";
            ja = "試験対策センター";
        }

        protected override void Build(RectTransform card)
        {
            _jlptTab = AddButton(card, string.Empty, 36f, 100f, 220f, 46f, false, 18f);
            _jlptTabLabel = _jlptTab.GetComponentInChildren<TextMeshProUGUI>();
            _jlptTab.onClick.AddListener(() => SelectTab(ExamType.Jlpt));

            _ieltsTab = AddButton(card, string.Empty, 266f, 100f, 220f, 46f, false, 18f);
            _ieltsTabLabel = _ieltsTab.GetComponentInChildren<TextMeshProUGUI>();
            _ieltsTab.onClick.AddListener(() => SelectTab(ExamType.Ielts));

            var scrollRoot = new GameObject("List", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollRoot.transform.SetParent(card, false);
            Place((RectTransform)scrollRoot.transform, 36f, 160f, 1108f, 500f);
            scrollRoot.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.05f, 0.7f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollRoot.transform, false);
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = viewportRect.offsetMax = Vector2.zero;

            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportRect, false);
            _listRoot = (RectTransform)contentObject.transform;
            _listRoot.anchorMin = new Vector2(0f, 1f);
            _listRoot.anchorMax = new Vector2(1f, 1f);
            _listRoot.pivot = new Vector2(0.5f, 1f);
            _listRoot.sizeDelta = Vector2.zero;
            _listRoot.anchoredPosition = Vector2.zero;
            var layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollRoot.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = _listRoot;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            _emptyText = AddLocalizedText(card, "Chưa có bài luyện thi nào ở mục này.", "No practice test in this category yet.", "このカテゴリーにはまだ試験がありません。", 18f, 36f, 620f, 1108f, 40f, TextAlignmentOptions.Center, FontStyles.Normal, false, Muted);
        }

        protected override void Update()
        {
            base.Update();

            if (Keyboard.current == null || !Keyboard.current.kKey.wasPressedThisFrame) return;
            if (IsOpen)
            {
                Hide();
                return;
            }

            Show();
        }

        protected override void OnOpened()
        {
            SelectTab(_tab);
        }

        protected override void OnLanguageApplied()
        {
            if (PanelObject.activeSelf) SelectTab(_tab);
        }

        private void SelectTab(ExamType tab)
        {
            _tab = tab;
            bool jlpt = tab == ExamType.Jlpt;
            UIStyleKit.StyleButton(_jlptTab, jlpt ? Gold : new Color(0.13f, 0.17f, 0.2f, 1f), jlpt ? UIStyleKit.AccentGoldHover : UIStyleKit.PanelHover, jlpt ? UIStyleKit.AccentGoldPressed : UIStyleKit.PanelPressed);
            UIStyleKit.StyleButton(_ieltsTab, !jlpt ? Gold : new Color(0.13f, 0.17f, 0.2f, 1f), !jlpt ? UIStyleKit.AccentGoldHover : UIStyleKit.PanelHover, !jlpt ? UIStyleKit.AccentGoldPressed : UIStyleKit.PanelPressed);
            _jlptTabLabel.text = "JLPT";
            _jlptTabLabel.color = jlpt ? DarkText : Color.white;
            _ieltsTabLabel.text = "IELTS";
            _ieltsTabLabel.color = !jlpt ? DarkText : Color.white;

            RebuildList();
        }

        private void RebuildList()
        {
            for (int i = _listRoot.childCount - 1; i >= 0; i--)
            {
                var child = _listRoot.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            var exams = new List<ExamDefinition>();
            if (GameServices.TryGet(out ExamRepository repository))
            {
                foreach (var exam in repository.GetAllExams())
                {
                    if (exam != null && exam.examType == _tab) exams.Add(exam);
                }
            }

            exams.Sort((a, b) => string.CompareOrdinal(a.level, b.level));
            var localPackages = _tab == ExamType.Ielts ? NihongoLife.Exam.Ielts.IeltsLibrary.Discover() : new List<NihongoLife.Exam.Ielts.IeltsLibrary.Package>();
            _emptyText.gameObject.SetActive(exams.Count == 0 && localPackages.Count == 0);
            foreach (var package in localPackages) BuildLocalIeltsCard(package);

            PlayerProgressDto progress = GameServices.TryGet(out IProgressRepository progressRepository) ? progressRepository.GetProgress() : null;
            foreach (var exam in exams) BuildCard(exam, progress);
        }

        private void BuildCard(ExamDefinition exam, PlayerProgressDto progress)
        {
            var card = new GameObject("ExamCard", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            card.transform.SetParent(_listRoot, false);
            card.GetComponent<LayoutElement>().preferredHeight = 154f;
            card.GetComponent<Image>().color = Surface;

            AddText(card.transform, $"{exam.level}  ·  {Pick(exam.titleVi, exam.titleEn, exam.titleJa)}", 22f, 22f, 14f, 640f, 32f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, false, Gold);
            AddText(card.transform, Pick(exam.descriptionVi, exam.descriptionEn, exam.descriptionEn), 15f, 22f, 48f, 640f, 44f, TextAlignmentOptions.TopLeft, FontStyles.Normal, true, Muted);

            string levelLabel = BuildLevelLabel(exam);
            AddText(card.transform, levelLabel, 13f, 22f, 94f, 860f, 22f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, false, new Color(0.35f, 0.82f, 0.72f, 1f));

            string bestLabel = BuildBestLabel(exam, progress);
            AddText(card.transform, bestLabel, 14f, 22f, 120f, 760f, 24f, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);

            var start = AddButton(card.transform, string.Empty, 900f, 34f, 180f, 50f, true, 18f);
            start.GetComponentInChildren<TextMeshProUGUI>().text = Pick("Bắt đầu", "Start", "始める");
            var capturedExam = exam;
            start.onClick.AddListener(() => StartExam(capturedExam));
        }

        /// <summary>Full IELTS tests from LocalContent (licensed books kept on this machine only).</summary>
        private void BuildLocalIeltsCard(NihongoLife.Exam.Ielts.IeltsLibrary.Package package)
        {
            var test = package.Test;
            var card = new GameObject("IeltsLocalCard", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            card.transform.SetParent(_listRoot, false);
            card.GetComponent<LayoutElement>().preferredHeight = 154f;
            card.GetComponent<Image>().color = new Color(0.09f, 0.16f, 0.22f, 1f);
            int questions = test.parts?.SelectMany(p => p.groups ?? System.Array.Empty<NihongoLife.Exam.Ielts.IeltsGroup>()).Select(g => g.to).DefaultIfEmpty(0).Max() ?? 0;
            int partCount = test.parts?.Length ?? 0;
            string skill = test.skill switch
            {
                "listening" => $"Listening · {questions} câu · {partCount} phần",
                "reading" => $"Academic Reading · {questions} câu · {partCount} bài đọc · {test.timeLimitMinutes} phút",
                "writing" => "Writing", "speaking" => "Speaking", _ => test.skill,
            };
            AddText(card.transform, test.title, 22f, 22f, 14f, 640f, 32f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, false, Gold);
            AddText(card.transform, $"{skill}   ·   {(test.localOnly ? "Bản local — chỉ để kiểm thử trên máy này, không phát hành" : "")}", 15f, 22f, 50f, 640f, 24f, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, false, Muted);
            var history = NihongoLife.Exam.Ielts.IeltsAttemptStore.LoadHistory().entries.FindAll(e => e.testId == test.id);
            var saved = NihongoLife.Exam.Ielts.IeltsAttemptStore.LoadAttempt(test.id);
            string best = history.Count > 0 ? $"Tốt nhất: {history.Max(h => h.rawScore)}/{history[0].maxScore} · band ước tính {history.Max(h => h.band):0.0}  ({history.Count} lần)" : "Chưa làm lần nào";
            string progress = saved != null && !saved.submitted ? $"   ·   Đang làm dở ({(saved.mode == "exam" ? "thi thử" : "luyện tập")}, {saved.responses.Count} câu)" : "";
            AddText(card.transform, best + progress, 14f, 22f, 86f, 640f, 24f, TextAlignmentOptions.MidlineLeft, FontStyles.Normal, false, new Color(0.35f, 0.82f, 0.72f, 1f));
            string how = test.skill == "listening" ? "Có audio · thi thử: nghe một lần, có 2 phút kiểm tra"
                       : test.skill == "reading" ? "Bài đọc bên cạnh câu hỏi · thi thử: đếm ngược, hết giờ tự nộp" : "Theo cấu trúc đề thật";
            AddText(card.transform, how + " · chấm theo đáp án gốc · xem lại từng câu", 13f, 22f, 116f, 640f, 22f, TextAlignmentOptions.MidlineLeft, FontStyles.Italic, false, Muted);

            // Buttons in a right-hand column, clear of the text (same column as the other exam cards).
            float y = 10f;
            bool resumable = saved != null && !saved.submitted;
            if (resumable)
            {
                var resume = AddButton(card.transform, "Tiếp tục", 900f, y, 180f, 40f, true, 16f);
                resume.onClick.AddListener(() => OpenLocal(package, saved.mode, true));
                y += 46f;
            }
            var practice = AddButton(card.transform, "Luyện tập", 900f, y, 180f, 40f, false, 16f);
            practice.onClick.AddListener(() => OpenLocal(package, NihongoLife.UI.IeltsTestUI.PracticeMode, false));
            var exam = AddButton(card.transform, "Thi thử", 900f, y + 46f, 180f, 40f, !resumable, 16f);
            exam.onClick.AddListener(() => OpenLocal(package, NihongoLife.UI.IeltsTestUI.ExamMode, false));
        }

        private void OpenLocal(NihongoLife.Exam.Ielts.IeltsLibrary.Package package, string mode, bool resume)
        {
            Hide();
            NihongoLife.UI.IeltsTestUI.Open(package, mode, resume);
        }

        private string BuildLevelLabel(ExamDefinition exam)
        {
            if (exam == null) return string.Empty;
            int questions = 0;
            if (exam.sections != null) foreach (var section in exam.sections) if (section?.questions != null) questions += section.questions.Count;
            // Honest scope: these built-in sets are short, written for the project, not official papers.
            string scope = Pick($"Đề rút gọn tự soạn · {questions} câu · không phải đề chính thức", $"Short project-written set · {questions} questions · not an official paper", $"自作の短縮版 · {questions}問 · 公式問題ではありません");
            if (exam.examType != ExamType.Ielts)
                return $"{Pick("Cap do", "Level", "レベル")}: {exam.level}   ·   {scope}";
            scope += Pick("  ·  Writing/Speaking chấm bằng AI (cần mạng, chỉ ước tính)", "  ·  Writing/Speaking scored by AI (online, estimate only)", "  ·  ライティング/スピーキングはAI採点（オンライン・目安）");

            string band = exam.recommendedBandMax > 0f
                ? $"IELTS {exam.recommendedBandMin:0.0}-{exam.recommendedBandMax:0.0}"
                : $"IELTS {exam.recommendedBandMin:0.0}+";
            return $"{Pick("Cap do", "Level", "レベル")}: {exam.learnerLevel}  |  {band}   ·   {scope}";
        }

        private string BuildBestLabel(ExamDefinition exam, PlayerProgressDto progress)
        {
            ExamAttemptRecord best = null;
            if (progress?.examAttempts != null)
            {
                foreach (var attempt in progress.examAttempts)
                {
                    if (attempt.examId != exam.id) continue;
                    if (best == null || (exam.examType == ExamType.Jlpt ? attempt.totalScore > best.totalScore : attempt.estimatedBand > best.estimatedBand)) best = attempt;
                }
            }

            if (best == null) return Pick("Chưa làm lần nào", "Not attempted yet", "まだ受けていません");

            return exam.examType == ExamType.Jlpt
                ? $"{Pick("Điểm cao nhất", "Best score", "最高点")}: {best.totalScore}/{best.totalMaxScore}  ·  {(best.passed ? Pick("Đạt", "Pass", "合格") : Pick("Chưa đạt", "Not yet", "不合格"))}"
                : $"{Pick("Band cao nhất", "Best band", "最高バンド")}: {best.estimatedBand:0.0}";
        }

        private void StartExam(ExamDefinition exam)
        {
            if (!GameServices.TryGet(out ExamManager manager)) return;

            // Resume an attempt still in progress instead of wiping it — the learner may have simply
            // closed this screen mid-exam (ExamManager itself keeps running regardless of this popup).
            bool alreadyInProgress = manager.CurrentExam == exam && manager.IsAttemptActive;
            if (!alreadyInProgress) manager.StartAttempt(exam);

            Hide();
            ExamPlayUI.GetOrCreate(transform).Show();
        }
    }
}
