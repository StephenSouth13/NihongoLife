using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Exam.Ielts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Self-assessment for IELTS Writing/Speaking when there is no AI or teacher marking: a checklist and the band
    /// descriptors (paraphrased from the public IELTS criteria) for each of the four criteria, a band picker per
    /// criterion and the estimated band with IELTS rounding. Saved with the attempt and always labelled "self-assessed".
    /// </summary>
    public sealed class SelfAssessPopup : MenuPopupBase
    {
        private const float W = 1240f, H = 820f;
        private static readonly int[] Choices = { 4, 5, 6, 7, 8, 9 };

        private RectTransform _main, _tabs;
        private ScrollRect _scroll;
        private TextMeshProUGUI _result;

        public IeltsProductiveRecord Record { get; private set; }
        public string Skill { get; private set; } = "writing";
        /// <summary>Writing task shown (1 or 2); 0 for Speaking.</summary>
        public int Task { get; private set; } = 2;
        public bool Saved { get; private set; }

        protected override bool LocksGameplay => true;
        protected override Vector2 CardSize => new Vector2(W, H);
        protected override void GetTitle(out string vi, out string en, out string ja)
        { vi = "Tự đánh giá theo tiêu chí IELTS"; en = "Self-assessment · IELTS criteria"; ja = "IELTS基準で自己評価"; }

        public static SelfAssessPopup Open(IeltsProductiveRecord record, GameObject host = null)
        {
            var popup = ReferenceUi.Open<SelfAssessPopup>(host);
            popup.Bind(record);
            popup.Show();
            return popup;
        }

        /// <summary>Attach a submitted paper (or null for a practice rubric that is not saved).</summary>
        public void Bind(IeltsProductiveRecord record, string skill = null)
        {
            Record = record;
            Saved = false;
            Skill = record?.skill ?? skill ?? "writing";
            if (Skill == "speaking") Task = 0;
            else Task = record != null && record.writing.Count > 0 && record.writing.All(t => t.task == 1) ? 1 : (record?.writing.Any(t => t.task == 2) ?? true) ? 2 : 1;
            if (IsOpen) Rebuild();
        }

        protected override void Build(RectTransform card)
        {
            _tabs = ReferenceUi.Row(card, "Tabs", 10f);
            UiKit.Place(_tabs, 36f, 104f, W - 72f, 44f);
            var root = UiKit.Rect(card, "MainScroll");
            UiKit.Place(root, 32f, 158f, W - 64f, H - 268f);
            _main = UiKit.Scroll(root, "Scroll", out _scroll, 12f, new RectOffset(4, 16, 4, 16));
            _result = UiKit.Label(card, "Result", "", 18f, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, true);
            UiKit.Place(_result.rectTransform, 36f, H - 100f, W - 560f, 76f);
            var buttons = ReferenceUi.Row(card, "Buttons", 10f, TextAnchor.MiddleRight);
            UiKit.Place(buttons, W - 540f, H - 92f, 504f, 56f);
            UiKit.Size(UiKit.Button(buttons, "Models", "Xem bài mẫu", "Model answers", "モデル解答", () => ModelAnswersPopup.Open(Skill, Task, gameObject), UiKit.Kind.Accent, "menu_book", 15.5f), 220f, 50f);
            UiKit.Size(UiKit.Button(buttons, "Save", "Lưu (tự đánh giá)", "Save (self-assessed)", "保存（自己評価）", Save, UiKit.Kind.Primary, "save", 15.5f), 260f, 50f);
        }

        protected override void OnOpened() => Rebuild();
        protected override void OnLanguageApplied() { if (IsOpen) Rebuild(); }

        /// <summary>The assessment being edited (created on the record on first use).</summary>
        public IeltsSelfAssessment Current
        {
            get
            {
                if (Skill == "speaking")
                {
                    if (Record == null) return _practice ??= new IeltsSelfAssessment();
                    return Record.speakingSelfAssessment ??= new IeltsSelfAssessment();
                }
                var task = Record?.writing.FirstOrDefault(t => t.task == Task);
                if (task == null) { _practiceTasks.TryGetValue(Task, out var p); return _practiceTasks[Task] = p ?? new IeltsSelfAssessment(); }
                return task.selfAssessment ??= new IeltsSelfAssessment();
            }
        }
        private IeltsSelfAssessment _practice;
        private readonly Dictionary<int, IeltsSelfAssessment> _practiceTasks = new();

        public List<IeltsRubricCriterion> Criteria => IeltsRubric.Loaded.For(Skill, Task);

        public void ShowTask(int task) { Task = task; Rebuild(); }

        public void SetBand(string code, int band) { Current.Set(code, band); Saved = false; Rebuild(); }

        public void ToggleCheck(string code, int index)
        {
            string key = code + ":" + index;
            var a = Current;
            if (!a.ticked.Remove(key)) a.ticked.Add(key);
            Saved = false;
            Rebuild();
        }

        public void Save()
        {
            var a = Current;
            a.source = IeltsSelfAssessment.Source;
            a.createdUtc = System.DateTime.UtcNow.ToString("o");
            a.Compute(Skill == "speaking" ? "speaking" : "writing");
            if (Record != null) IeltsSelfAssessmentService.Save(Record);
            Saved = true;
            RefreshResult();
        }

        private void Rebuild()
        {
            if (_main == null) return;
            UiKit.Clear(_tabs);
            if (Skill == "writing")
            {
                foreach (int t in new[] { 1, 2 })
                {
                    if (Record != null && Record.writing.Count > 0 && Record.writing.All(x => x.task != t)) continue;
                    int task = t;
                    UiKit.Size(UiKit.Button(_tabs, "Task" + t, $"Writing Task {t}", () => ShowTask(task), Task == t ? UiKit.Kind.Primary : UiKit.Kind.Secondary, "edit_note", 15f), 200f, 42f);
                }
            }
            else UiKit.Size(UiKit.Button(_tabs, "Speaking", "Speaking", () => { }, UiKit.Kind.Primary, "mic", 15f), 180f, 42f);
            var src = UiKit.Label(_tabs, "Source", IeltsRubric.Loaded.source?.Current ?? "", 13f, UiTheme.Muted, FontStyles.Italic, TextAlignmentOptions.MidlineLeft, true);
            UiKit.Size(src, 700f, 42f);

            UiKit.Clear(_main);
            var a = Current;
            foreach (var c in Criteria)
            {
                var panel = ReferenceUi.Panel(_main, "Criterion_" + c.code, UiTheme.Surface, 18, 8f);
                int chosen = Mathf.RoundToInt(a.Get(c.code));
                ReferenceUi.Text(panel, "Name", $"<b><color={UiTheme.Hex(UiTheme.Gold)}>{c.code}</color>  {c.name?.Current}</b>", 18f, UiTheme.Text);
                ReferenceUi.Text(panel, "Help", c.help?.Current, 14f, UiTheme.Muted, FontStyles.Italic);
                for (int i = 0; i < c.checklist.Count; i++)
                {
                    int index = i;
                    bool on = a.ticked.Contains(c.code + ":" + i);
                    var b = UiKit.Button(panel, $"Check_{c.code}_{i}", (on ? "\u2611  " : "\u2610  ") + c.checklist[i].Current, () => ToggleCheck(c.code, index), on ? UiKit.Kind.Success : UiKit.Kind.Quiet, null, 14.5f);
                    UiKit.Size(b, -1f, 38f);
                    var label = UiKit.LabelOf(b);
                    if (label != null) label.alignment = TextAlignmentOptions.MidlineLeft;
                }
                var row = ReferenceUi.Row(panel, "Bands", 8f);
                UiKit.Label(row, "Pick", Loc.T("Band của bạn:", "Your band:", "あなたのバンド："), 15f, UiTheme.TextSoft, FontStyles.Bold);
                foreach (int band in Choices)
                {
                    int b = band;
                    UiKit.Size(UiKit.Button(row, $"Band_{c.code}_{band}", band.ToString(), () => SetBand(c.code, b), chosen == band ? UiKit.Kind.Primary : UiKit.Kind.Secondary, null, 16f), 60f, 40f);
                }
                var descriptor = c.bands.FirstOrDefault(x => x.band == chosen);
                string text = descriptor != null
                    ? $"<b>Band {chosen}:</b> {descriptor.text?.Current}"
                    : Loc.T("Chọn band gần nhất với bài của bạn. Mô tả từng band hiện ở đây.", "Pick the band closest to your work. Its description appears here.", "自分の答案に最も近いバンドを選ぶと、ここに説明が出ます。");
                ReferenceUi.Text(panel, "Descriptor", text, 14.5f, UiTheme.TextSoft);
            }
            RefreshResult();
            Canvas.ForceUpdateCanvases();
        }

        private void RefreshResult()
        {
            if (_result == null) return;
            var a = Current;
            float band = a.Compute(Skill == "speaking" ? "speaking" : "writing");
            string label = Loc.T("TỰ ĐÁNH GIÁ", "SELF-ASSESSED", "自己評価");
            string head = band > 0f ? $"{label} · {Loc.T("Band ước tính", "Estimated band", "推定バンド")}: <size=140%><b>{ReferenceUi.Band(band)}</b></size>"
                : $"{label} · {Loc.T("Chọn band cho cả 4 tiêu chí", "Pick a band for all 4 criteria", "4項目すべてのバンドを選んでください")}";
            float overall = Record != null ? IeltsSelfAssessmentService.Overall(Record) : 0f;
            if (Record != null && Skill == "writing" && overall > 0f) head += $"   ·   {Loc.T("Band Viết", "Writing band", "ライティング")}: <b>{ReferenceUi.Band(overall)}</b>";
            if (Saved) head += $"   <color={UiTheme.Hex(UiTheme.Good)}>✓ {Loc.T("Đã lưu", "Saved", "保存済み")}</color>";
            _result.text = head + "\n<size=78%><color=" + UiTheme.Hex(UiTheme.Muted) + ">" +
                Loc.T("Chỉ là ước tính của chính bạn, không phải điểm chính thức hay điểm AI.", "Your own estimate — not an official or AI score.", "自分による推定で、公式・AIのスコアではありません。") + "</color></size>";
        }
    }
}
