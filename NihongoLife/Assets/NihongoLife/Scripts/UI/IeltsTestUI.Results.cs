using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NihongoLife.Core;
using NihongoLife.Exam.Ielts;
using NihongoLife.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Partial of IeltsTestUI: results table and confirm dialog. The MonoBehaviour entry point stays IeltsTestUI.cs.</summary>
    public sealed partial class IeltsTestUI : MonoBehaviour
    {
        // ─────────── Results ───────────

        private void BuildResults()
        {
            _results = new GameObject("Results", typeof(RectTransform)).GetComponent<RectTransform>();
            _results.SetParent(_root, false);
            _results.anchorMin = Vector2.zero; _results.anchorMax = Vector2.one;
            _results.offsetMin = new Vector2(0f, 92f); _results.offsetMax = new Vector2(0f, -144f);
            _results.gameObject.SetActive(false);
        }

        private void ShowResults()
        {
            _bodyArea.gameObject.SetActive(false);
            foreach (Transform child in _results) Destroy(child.gameObject);
            _results.gameObject.SetActive(true);
            var scroll = MakeScroll(_results, "ResultScroll", out var content);
            var rect = (RectTransform)scroll.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(0f, 12f); rect.offsetMax = new Vector2(0f, -12f);

            var r = LastResult;
            var summary = Box(content, "Summary");
            PaperText(summary, Loc.T("KẾT QUẢ", "RESULT", "結果"), 15f, FontStyles.Bold).color = Accent;
            PaperText(summary, $"<size=170%><b>{r.Raw}</b></size> / {r.Max}      {Loc.T("Band ước tính", "Estimated band", "推定バンド")} <size=150%><b>{r.Band:0.0}</b></size>", 22f, FontStyles.Normal);
            PaperText(summary, string.Join("      ", r.RawByPart.Select(kv => $"{Loc.F("Phần {0}", "Part {0}", "パート{0}", kv.Key)}: <b>{kv.Value}/{r.MaxByPart[kv.Key]}</b>")), 17f, FontStyles.Normal);
            PaperText(summary, Loc.T("Band là ước tính theo bảng quy đổi điểm thô công khai, không phải kết quả IELTS chính thức.", "The band is an estimate from the public raw-score table, not an official IELTS result.", "バンドは公開されている換算表による推定で、公式のIELTS結果ではありません。"), 14.5f, FontStyles.Italic).color = PaperMuted;

            PaperText(content, Loc.T("Xem lại từng câu", "Question by question", "一問ずつ見直し"), 19f, FontStyles.Bold);
            var header = Row(content, "Head", 10f);
            Cell(header, Loc.T("Câu", "No.", "問"), 60f, FontStyles.Bold); Cell(header, Loc.T("Bạn trả lời", "Your answer", "あなたの答え"), 360f, FontStyles.Bold); Cell(header, Loc.T("Đáp án", "Correct answer", "正解"), 420f, FontStyles.Bold); Cell(header, "", 120f, FontStyles.Bold);
            foreach (var item in r.Items)
            {
                var row = Row(content, "R" + item.Number, 10f);
                row.gameObject.AddComponent<Image>().color = item.IsCorrect ? new Color(0.88f, 0.96f, 0.9f, 1f) : new Color(0.99f, 0.91f, 0.9f, 1f);
                Cell(row, item.Number.ToString(), 60f, FontStyles.Bold);
                Cell(row, string.IsNullOrEmpty(item.Given) ? $"<i>{Loc.T("(bỏ trống)", "(blank)", "（空欄）")}</i>" : item.Given, 360f, FontStyles.Normal);
                Cell(row, IeltsGrader.KeyDisplay(item.Correct) + (item.Correct != null && item.Correct.Length > 1 && item.Correct.All(c => c.Length == 1) ? $"  <i>{Loc.T("(bất kỳ thứ tự)", "(any order)", "（順不同）")}</i>" : ""), 420f, FontStyles.Normal);
                var mark = Cell(row, item.IsCorrect ? Loc.T("✓ Đúng", "✓ Correct", "✓ 正解") : item.OverLimit ? Loc.T("× Quá số từ", "× Too many words", "× 語数オーバー") : Loc.T("× Sai", "× Wrong", "× 不正解"), 120f, FontStyles.Bold);
                mark.color = item.IsCorrect ? Good : Bad;
            }
            var buttons = Row(content, "Buttons", 12f);
            UiKit.Size(UiKit.Button(buttons, "Again", "Làm lại (luyện tập)", "Try again (practice)", "もう一度（練習）", () => Begin(_package, PracticeMode, false), UiKit.Kind.Accent, "replay", 16f), 260f, 50f);
            UiKit.Size(UiKit.Button(buttons, "Exam", "Thi thử lại", "Mock exam again", "もう一度模試", () => Begin(_package, ExamMode, false), UiKit.Kind.Primary, "timer", 16f), 220f, 50f);
            UiKit.Size(UiKit.Button(buttons, "Close", "Đóng", "Close", "閉じる", Close, UiKit.Kind.Secondary, "close", 16f), 160f, 50f);
            _submitButton.interactable = false;
            RefreshAudioBar();
        }

        private TextMeshProUGUI Cell(RectTransform row, string text, float width, FontStyles style)
        {
            var t = PaperText(row, text, 16.5f, style);
            NLUi.Size(t, width);
            return t;
        }

        // ─────────── Confirm dialog ───────────

        private void BuildConfirm()
        {
            _confirm = new GameObject("Confirm", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _confirm.SetParent(_root, false);
            NLUi.Stretch(_confirm);
            _confirm.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.66f);
            var box = NLUi.Panel(_confirm, "Box", UiTheme.Surface, new RectOffset(40, 40, 32, 32), 16f);
            box.GetComponent<Image>().sprite = UiKit.Rounded(22);
            NLUi.Anchor(box, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660f, 0f));
            NLUi.FitContent(box);
            _confirmTitle = NLUi.Label(box, "Title", "", 26f, UiTheme.Text, _font, FontStyles.Bold, TextAlignmentOptions.Center);
            _confirmBody = NLUi.Label(box, "Body", "", 16.5f, UiTheme.TextSoft, _font, FontStyles.Normal, TextAlignmentOptions.Center);
            var row = NLUi.Group(box, "Buttons", false, 14f, TextAnchor.MiddleCenter, false);
            var stay = UiKit.Button(row, "Stay", "Tiếp tục làm bài", "Keep going", "続ける", () => _confirm.gameObject.SetActive(false), UiKit.Kind.Accent, "play_arrow", 16f);
            UiKit.Size(stay, 250f, 50f);
            var yes = UiKit.Button(row, "Yes", null, null, null, () => _confirmYes?.Invoke(), UiKit.Kind.Secondary, null, 16f);
            UiKit.Size(yes, 210f, 50f);
            _confirmYesLabel = UiKit.LabelOf(yes);
            _confirm.gameObject.SetActive(false);
        }
    }
}
