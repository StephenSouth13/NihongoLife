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
    /// "Tham khảo / Model answers": pre-written IELTS Writing (Task 1, Task 2) and Speaking (Part 1–3) samples at
    /// bands 5–9 with examiner-style analysis. Band tabs, side-by-side comparison of two bands, highlighted phrases with
    /// hover/tap tooltips, per-criterion notes and "how to reach the next band". Opens automatically after an IELTS
    /// Writing/Speaking submission and from the Reference buttons. Content only — no AI, no network.
    /// </summary>
    public sealed class ModelAnswersPopup : MenuPopupBase
    {
        private const float W = 1320f, H = 820f;
        private RectTransform _sets, _main, _tooltipLayer;
        private ScrollRect _scroll;

        public IeltsModelSet CurrentSet { get; private set; }
        public float Band { get; private set; } = 7f;
        public float CompareBand { get; private set; } = 8f;
        public bool Comparing { get; private set; }
        public readonly List<HighlightTooltip> Tooltips = new();

        protected override bool LocksGameplay => true;
        protected override Vector2 CardSize => new Vector2(W, H);
        protected override void GetTitle(out string vi, out string en, out string ja)
        { vi = "Tham khảo · Bài mẫu IELTS"; en = "Reference · IELTS model answers"; ja = "参考・IELTSモデル解答"; }

        /// <summary>Opens on the reference canvas (or <paramref name="host"/>), showing the first set for the skill/task.</summary>
        public static ModelAnswersPopup Open(string skill = "writing", int task = 0, GameObject host = null)
        {
            var popup = ReferenceUi.Open<ModelAnswersPopup>(host);
            popup.Show();
            popup.Select(IeltsModelAnswers.For(skill, task).FirstOrDefault() ?? IeltsModelAnswers.All.FirstOrDefault());
            return popup;
        }

        protected override void Build(RectTransform card)
        {
            var sub = UiKit.Label(card, "Subtitle", "", 14.5f, UiTheme.Muted, FontStyles.Italic, TextAlignmentOptions.TopLeft, true);
            UiKit.Place(sub.rectTransform, 36f, 98f, W - 72f, 40f);
            sub.name = "Disclaimer";
            _sets = UiKit.Rect(card, "Sets");
            UiKit.Place(_sets, 32f, 146f, 300f, H - 178f);
            UiKit.Column(_sets, 8f);
            var scrollRoot = UiKit.Rect(card, "MainScroll");
            UiKit.Place(scrollRoot, 352f, 146f, W - 384f, H - 178f);
            _main = UiKit.Scroll(scrollRoot, "Scroll", out _scroll, 14f, new RectOffset(4, 16, 4, 24));
            _tooltipLayer = UiKit.Rect(card, "TooltipLayer");
            UiKit.Fill(_tooltipLayer);
        }

        protected override void OnOpened()
        {
            if (CurrentSet == null) CurrentSet = IeltsModelAnswers.All.FirstOrDefault();
            Rebuild();
        }

        protected override void OnLanguageApplied() { if (IsOpen) Rebuild(); }

        public void Select(IeltsModelSet set)
        {
            if (set == null) return;
            CurrentSet = set;
            if (set.AtBand(Band) == null) Band = set.samples.FirstOrDefault()?.band ?? 7f;
            Rebuild();
        }

        public void SelectBand(float band) { Band = band; if (Mathf.Approximately(CompareBand, band)) CompareBand = band < 9f ? band + 1f : band - 1f; Rebuild(); }
        public void SelectCompareBand(float band) { CompareBand = band; Rebuild(); }
        public void ToggleCompare() { Comparing = !Comparing; if (Mathf.Approximately(CompareBand, Band)) CompareBand = Band < 9f ? Band + 1f : Band - 1f; Rebuild(); }

        private void Rebuild()
        {
            if (_main == null) return;
            var disclaimer = Card.Find("Disclaimer")?.GetComponent<TextMeshProUGUI>();
            if (disclaimer != null)
                disclaimer.text = Loc.T("Bài mẫu do NihongoLife tự viết để minh họa từng band — không phải bài thi thật hay điểm chính thức. Rê chuột (hoặc chạm) vào cụm được tô màu để xem nhận xét.",
                    "Original illustrative band samples written by NihongoLife — not real candidate scripts or official scores. Hover (or tap) a coloured phrase to read the note.",
                    "NihongoLifeが作成したバンド別の例示用オリジナル答案です（実際の答案・公式スコアではありません）。色付きの語句にカーソルを合わせる（タップする）と講評が出ます。");
            BuildSetList();
            Tooltips.Clear();
            UiKit.Clear(_main);
            foreach (Transform child in _tooltipLayer) child.gameObject.SetActive(false);
            UiKit.Clear(_tooltipLayer);
            var set = CurrentSet;
            if (set == null)
            {
                ReferenceUi.Text(_main, "Empty", Loc.T("Chưa có bài mẫu.", "No model answers yet.", "モデル解答はまだありません。"), 18f, UiTheme.TextSoft);
                return;
            }

            var head = ReferenceUi.Panel(_main, "Prompt", UiTheme.Surface);
            ReferenceUi.Text(head, "SetTitle", set.title?.Current, 20f, UiTheme.Gold, FontStyles.Bold);
            ReferenceUi.Text(head, "PromptText", set.prompt, 16f, UiTheme.Text);
            if (set.dataRows != null && set.dataRows.Count > 0)
            {
                ReferenceUi.Text(head, "DataCaption", set.dataCaption, 14f, UiTheme.Muted, FontStyles.Italic);
                var table = new System.Text.StringBuilder();
                table.Append("<mspace=0.6em>");
                foreach (var row in new[] { set.dataColumns }.Concat(set.dataRows.Select(r => r.cells)))
                    table.AppendLine(string.Join("", (row ?? new string[0]).Select((c, i) => (c ?? "").PadRight(i == 0 ? 16 : 8))));
                table.Append("</mspace>");
                ReferenceUi.Text(head, "DataTable", table.ToString(), 14.5f, UiTheme.TextSoft);
            }
            if (set.note != null) ReferenceUi.Text(head, "Note", set.note.Current, 13.5f, UiTheme.Muted, FontStyles.Italic);

            var tabs = ReferenceUi.Row(_main, "BandTabs", 8f);
            UiKit.Label(tabs, "BandLabel", Loc.T("Band:", "Band:", "バンド："), 15f, UiTheme.TextSoft, FontStyles.Bold);
            foreach (var s in set.samples)
            {
                float b = s.band;
                bool on = Mathf.Approximately(b, Band);
                UiKit.Size(UiKit.Button(tabs, "Band_" + ReferenceUi.Band(b), ReferenceUi.Band(b), () => SelectBand(b), on ? UiKit.Kind.Primary : UiKit.Kind.Secondary, null, 16f), 84f, 42f);
            }
            UiKit.Size(UiKit.Button(tabs, "Compare", Comparing ? Loc.T("Tắt so sánh", "Stop comparing", "比較をやめる") : Loc.T("So sánh 2 band", "Compare 2 bands", "2つのバンドを比較"),
                ToggleCompare, Comparing ? UiKit.Kind.Accent : UiKit.Kind.Ghost, "compare_arrows", 15f), 210f, 42f);

            if (Comparing)
            {
                var tabs2 = ReferenceUi.Row(_main, "CompareTabs", 8f);
                UiKit.Label(tabs2, "CompareLabel", Loc.T("So với band:", "Compare with:", "比較するバンド："), 15f, UiTheme.TextSoft, FontStyles.Bold);
                foreach (var s in set.samples)
                {
                    float b = s.band;
                    if (Mathf.Approximately(b, Band)) continue;
                    bool on = Mathf.Approximately(b, CompareBand);
                    UiKit.Size(UiKit.Button(tabs2, "CompareBand_" + ReferenceUi.Band(b), ReferenceUi.Band(b), () => SelectCompareBand(b), on ? UiKit.Kind.Accent : UiKit.Kind.Secondary, null, 16f), 84f, 42f);
                }
                var cols = ReferenceUi.Row(_main, "SideBySide", 16f, TextAnchor.UpperLeft);
                cols.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
                SampleColumn(cols, set, set.AtBand(Band), "Left", true);
                SampleColumn(cols, set, set.AtBand(CompareBand), "Right", true);
            }
            else SampleColumn(_main, set, set.AtBand(Band), "Single", false);

            Canvas.ForceUpdateCanvases();
            if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;
        }

        private void BuildSetList()
        {
            UiKit.Clear(_sets);
            string lastGroup = null;
            foreach (var set in IeltsModelAnswers.All)
            {
                string group = set.skill == "speaking" ? Loc.T("NÓI", "SPEAKING", "スピーキング") : set.task == 1 ? Loc.T("VIẾT · TASK 1", "WRITING · TASK 1", "ライティング・TASK 1") : Loc.T("VIẾT · TASK 2", "WRITING · TASK 2", "ライティング・TASK 2");
                if (group != lastGroup)
                {
                    var g = UiKit.Label(_sets, "Group", group, 13f, UiTheme.Muted, FontStyles.Bold);
                    UiKit.Size(g, -1f, 24f);
                    lastGroup = group;
                }
                var s = set;
                var b = UiKit.Button(_sets, "Set_" + set.id, set.title?.Current ?? set.id, () => Select(s), s == CurrentSet ? UiKit.Kind.Primary : UiKit.Kind.Quiet, set.skill == "speaking" ? "mic" : "edit_note", 14f);
                UiKit.Size(b, -1f, 46f);
            }
        }

        private void SampleColumn(Transform parent, IeltsModelSet set, IeltsModelSample sample, string name, bool narrow)
        {
            var col = ReferenceUi.Panel(parent, "Sample_" + name, UiTheme.Surface, 18, 10f);
            if (narrow) UiKit.Size(col, -1f, -1f, 1f);
            if (sample == null) { ReferenceUi.Text(col, "Missing", "—", 16f, UiTheme.Muted); return; }
            string kind = set.skill == "speaking" ? Loc.T("Câu trả lời mẫu", "Sample answer", "解答例") : Loc.T("Bài mẫu", "Sample essay", "答案例");
            ReferenceUi.Text(col, "Heading", $"<size=135%><b>Band {ReferenceUi.Band(sample.band)}</b></size>  ·  {kind}  ·  {Loc.F("{0} từ", "{0} words", "{0}語", sample.words)}", 16f, UiTheme.Gold);
            ReferenceUi.Text(col, "Summary", sample.summary?.Current, 15f, UiTheme.TextSoft, FontStyles.Italic);

            var crit = new System.Text.StringBuilder();
            foreach (var c in sample.criteria)
                crit.AppendLine($"<b><color={UiTheme.Hex(UiTheme.Gold)}>{c.code} {ReferenceUi.Band(c.band)}</color></b>  {c.note?.Current}");
            ReferenceUi.Text(col, "Criteria", crit.ToString().TrimEnd(), 14.5f, UiTheme.Text);

            var paper = ReferenceUi.Panel(col, "Paper", UiTheme.Sunken, 16, 6f);
            var body = ReferenceUi.Text(paper, "SampleText", ReferenceUi.Highlight(sample.text, sample.highlights), narrow ? 15f : 16.5f, UiTheme.Text);
            body.lineSpacing = 8f;
            var notes = sample.highlights.Select(h => h.note?.Current ?? "").ToList();
            Tooltips.Add(HighlightTooltip.Attach(body, _tooltipLayer, notes));

            var list = new System.Text.StringBuilder();
            for (int i = 0; i < sample.highlights.Count; i++)
            {
                var h = sample.highlights[i];
                string color = h.Good ? "#7FE0AE" : "#FF9A8F";
                string tag = h.Good ? Loc.T("Điểm mạnh", "Strength", "良い点") : Loc.T("Cần sửa", "Fix", "要修正");
                list.AppendLine($"<color={color}><b>{i + 1}. {tag}</b></color> — <i>\u201C{h.phrase}\u201D</i>: {h.note?.Current}");
            }
            ReferenceUi.Text(col, "Notes", list.ToString().TrimEnd(), 14f, UiTheme.TextSoft);

            if (sample.band < 9f)
                ReferenceUi.Text(col, "NextBand", $"<b><color={UiTheme.Hex(UiTheme.Teal)}>{Loc.T("Để lên band tiếp theo", "To reach the next band", "次のバンドへ")}</color></b>\n{sample.nextBand?.Current}", 14.5f, UiTheme.Text);
            else
                ReferenceUi.Text(col, "NextBand", $"<b><color={UiTheme.Hex(UiTheme.Teal)}>{Loc.T("Ghi chú", "Note", "メモ")}</color></b>\n{sample.nextBand?.Current}", 14.5f, UiTheme.Text);
        }
    }
}
