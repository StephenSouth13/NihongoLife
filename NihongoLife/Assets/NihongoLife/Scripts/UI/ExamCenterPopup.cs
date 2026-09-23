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
            _emptyText.gameObject.SetActive(exams.Count == 0);

            PlayerProgressDto progress = GameServices.TryGet(out IProgressRepository progressRepository) ? progressRepository.GetProgress() : null;
            foreach (var exam in exams) BuildCard(exam, progress);
        }

        private void BuildCard(ExamDefinition exam, PlayerProgressDto progress)
        {
            var card = new GameObject("ExamCard", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            card.transform.SetParent(_listRoot, false);
            card.GetComponent<LayoutElement>().preferredHeight = 118f;
            card.GetComponent<Image>().color = Surface;

            AddText(card.transform, $"{exam.level}  ·  {Pick(exam.titleVi, exam.titleEn, exam.titleJa)}", 22f, 22f, 14f, 640f, 32f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, false, Gold);
            AddText(card.transform, Pick(exam.descriptionVi, exam.descriptionEn, exam.descriptionEn), 15f, 22f, 50f, 640f, 56f, TextAlignmentOptions.TopLeft, FontStyles.Normal, true, Muted);

            string bestLabel = BuildBestLabel(exam, progress);
            AddText(card.transform, bestLabel, 15f, 22f, 88f, 640f, 24f, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);

            var start = AddButton(card.transform, string.Empty, 900f, 34f, 180f, 50f, true, 18f);
            start.GetComponentInChildren<TextMeshProUGUI>().text = Pick("Bắt đầu", "Start", "始める");
            var capturedExam = exam;
            start.onClick.AddListener(() => StartExam(capturedExam));
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
