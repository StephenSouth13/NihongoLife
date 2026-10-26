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
    /// <summary>Partial of IeltsTestUI: question paper rendering. The MonoBehaviour entry point stays IeltsTestUI.cs.</summary>
    public sealed partial class IeltsTestUI : MonoBehaviour
    {
        // ─────────── Rendering a part ───────────

        private void RenderPart(IeltsPart part)
        {
            foreach (Transform child in _questionContent) Destroy(child.gameObject);
            foreach (Transform child in _passageContent) Destroy(child.gameObject);
            _anchors.Clear();
            _refreshers.Clear();
            _highlightPaper = true; // every passage / question text below can be highlighted

            bool reading = !string.IsNullOrWhiteSpace(part.passage);
            _passageScroll.gameObject.SetActive(reading);
            var passageRect = (RectTransform)_passageScroll.transform;
            var questionRect = (RectTransform)_questionScroll.transform;
            if (reading)
            {
                passageRect.anchorMin = new Vector2(0f, 0f); passageRect.anchorMax = new Vector2(0.5f, 1f);
                questionRect.anchorMin = new Vector2(0.5f, 0f); questionRect.anchorMax = new Vector2(1f, 1f);
                passageRect.offsetMin = new Vector2(24f, 12f); passageRect.offsetMax = new Vector2(-8f, -12f);
                questionRect.offsetMin = new Vector2(8f, 12f); questionRect.offsetMax = new Vector2(-24f, -12f);
                if (!string.IsNullOrWhiteSpace(part.passageTitle)) PaperText(_passageContent, part.passageTitle, 24f, FontStyles.Bold);
                foreach (string paragraph in part.passage.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries))
                    PaperText(_passageContent, paragraph.Trim(), 18f, FontStyles.Normal).lineSpacing = 6f;
            }
            else
            {
                questionRect.anchorMin = Vector2.zero; questionRect.anchorMax = Vector2.one;
                questionRect.offsetMin = new Vector2(0f, 12f); questionRect.offsetMax = new Vector2(0f, -12f);
            }

            var sheet = _questionContent;
            var title = PaperText(sheet, $"{part.title.ToUpperInvariant()}", 20f, FontStyles.Bold);
            title.color = Accent;
            foreach (var group in part.groups ?? Array.Empty<IeltsGroup>())
            {
                switch (group.type)
                {
                    case "completion": RenderCompletion(sheet, group); break;
                    case "mcq_single": RenderSingleChoice(sheet, group); break;
                    case "mcq_multi": RenderMultiChoice(sheet, group); break;
                    case "matching": RenderMatching(sheet, group); break;
                    case "tfng": RenderFixedChoice(sheet, group, "TRUE", "FALSE", "NOT GIVEN"); break;
                    case "ynng": RenderFixedChoice(sheet, group, "YES", "NO", "NOT GIVEN"); break;
                    case "writing_task": RenderWriting(sheet, group); break;
                    case "speaking_part": RenderSpeaking(sheet, group); break;
                    default: PaperText(sheet, Loc.F("(Dạng câu hỏi chưa hỗ trợ: {0})", "(Question type not supported yet: {0})", "（未対応の問題形式：{0}）", group.type), 16f, FontStyles.Italic).color = Bad; break;
                }
            }
            _highlightPaper = false;
            _questionScroll.verticalNormalizedPosition = 1f;
            if (_passageScroll.gameObject.activeSelf) _passageScroll.verticalNormalizedPosition = 1f;
        }

        // ─────────── Highlights ───────────

        private void ClearHighlights()
        {
            if (_highlights.Count() == 0) { HudFeed.Post(Loc.T("Chưa có đoạn nào được tô sáng.", "Nothing is highlighted yet.", "まだハイライトはありません。"), HudFeed.Kind.Info, 2.5f); return; }
            ShowConfirm(Loc.T("Xoá mọi chỗ tô sáng?", "Clear every highlight?", "ハイライトをすべて消しますか？"),
                Loc.F("Xoá {0} chỗ tô sáng trong cả bài này. Câu trả lời không bị ảnh hưởng.", "Remove {0} highlights from this paper. Your answers are not affected.", "この試験のハイライト{0}件を消します。解答には影響しません。", _highlights.Count()),
                Loc.T("Xoá hết", "Clear all", "すべて消す"),
                () => { _confirm.gameObject.SetActive(false); _highlights.Clear(); });
        }

        /// <summary>Copies what is highlighted in the part on screen into the notebook (one bullet per highlight).</summary>
        private void CopyHighlightsToNotebook()
        {
            var snippets = _highlights.Snippets(k => k.StartsWith("p" + _viewPart + "/"));
            if (snippets.Count == 0) { HudFeed.Post(Loc.T("Tô sáng một đoạn trước, rồi bấm → Sổ để chép vào sổ tay.", "Highlight some text first, then press → Notebook.", "先にテキストをハイライトしてから「→ノート」を押してください。"), HudFeed.Kind.Info, 3f); return; }
            string text = string.Join("\n", snippets.Select(x => "• " + x));
            int left = NihongoLife.Notebook.NotebookService.Append(text, $"{_test.DisplayTitle} · {Loc.F("Phần {0}", "Part {0}", "パート{0}", _test.parts[_viewPart].number)}");
            if (left > 0) HudFeed.Post(Loc.F("Sổ tay hết chỗ — còn {0} ký tự chưa chép. Mua thêm giấy ở Hibari Mart.", "Notebook full — {0} characters not copied. Buy paper at Hibari Mart.", "ノートがいっぱいです — {0}文字コピーできません。ひばりマートで紙を買えます。", left), HudFeed.Kind.Warning, 4f);
            else HudFeed.Post(Loc.F("Đã chép {0} đoạn vào sổ tay (trang {1}).", "Copied {0} highlights to the notebook (page {1}).", "{0}件をノートにコピーしました（{1}ページ）。", snippets.Count, NihongoLife.Notebook.NotebookService.CurrentPage + 1), HudFeed.Kind.Reward, 3f);
            NotebookUI.Show();
        }

        private void GroupHeader(RectTransform parent, IeltsGroup group)
        {
            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(parent, false);
            spacer.GetComponent<LayoutElement>().preferredHeight = 6f;
            string range = group.from == group.to ? $"Question {group.from}" : $"Questions {group.from}–{group.to}";
            PaperText(parent, range, 18f, FontStyles.Bold | FontStyles.Italic);
            if (!string.IsNullOrWhiteSpace(group.instruction)) PaperText(parent, Emphasise(group.instruction), 17f, FontStyles.Italic);
            string limit = LimitText(group.limit);
            if (limit != null) PaperText(parent, limit, 17f, FontStyles.Italic);
            if (!string.IsNullOrWhiteSpace(group.image)) GroupImage(parent, group.image);
        }

        private readonly Dictionary<string, Texture2D> _images = new();

        /// <summary>A plan / map / diagram that belongs to the questions (Listening "label the plan", Reading diagrams).
        /// Read from the package folder at runtime — never imported into the project.</summary>
        private void GroupImage(RectTransform parent, string relative)
        {
            string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(_package.Folder, relative));
            if (!_images.TryGetValue(path, out var texture))
            {
                if (!System.IO.File.Exists(path)) { PaperText(parent, Loc.F("(Thiếu hình: {0})", "(Missing image: {0})", "（画像がありません：{0}）", relative), 15f, FontStyles.Italic); return; }
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.LoadImage(System.IO.File.ReadAllBytes(path));
                _images[path] = texture;
            }
            const float width = 600f;
            float height = width * texture.height / Mathf.Max(1f, texture.width);
            var holder = new GameObject("GroupImage", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            holder.SetParent(parent, false);
            holder.GetComponent<LayoutElement>().preferredHeight = height + 8f;
            var image = new GameObject("Picture", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            image.transform.SetParent(holder, false);
            image.texture = texture;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static string LimitText(string limit) => limit switch
        {
            "ONE_WORD" => "Write <b>ONE WORD ONLY</b> for each answer.",
            "ONE_WORD_AND_OR_NUMBER" => "Write <b>ONE WORD AND/OR A NUMBER</b> for each answer.",
            "TWO_WORDS" => "Write <b>NO MORE THAN TWO WORDS</b> for each answer.",
            "NO_MORE_THAN_TWO_WORDS_AND_OR_A_NUMBER" => "Write <b>NO MORE THAN TWO WORDS AND/OR A NUMBER</b> for each answer.",
            "NO_MORE_THAN_THREE_WORDS" => "Write <b>NO MORE THAN THREE WORDS</b> for each answer.",
            "NO_MORE_THAN_THREE_WORDS_AND_OR_A_NUMBER" => "Write <b>NO MORE THAN THREE WORDS AND/OR A NUMBER</b> for each answer.",
            _ => null,
        };

        private static string Emphasise(string text) =>
            Regex.Replace(text, @"\b(TWO|THREE|FIVE|ONE|NOT GIVEN|TRUE|FALSE|[A-G](?=[,–\-]| or))\b", "<b>$1</b>");

        private void RenderCompletion(RectTransform sheet, IeltsGroup group)
        {
            GroupHeader(sheet, group);
            var box = Box(sheet, "Form");
            if (!string.IsNullOrWhiteSpace(group.heading))
            {
                var heading = PaperText(box, group.heading, 21f, FontStyles.Bold);
                heading.alignment = TextAlignmentOptions.Center;
            }
            foreach (var line in group.lines ?? Array.Empty<IeltsLine>())
            {
                if (line.style == "subheading")
                {
                    PaperText(box, line.label, 17.5f, FontStyles.Bold).margin = new Vector4(0f, 8f, 0f, 0f);
                    continue;
                }
                var row = Row(box, "Line", 10f);
                if (!string.IsNullOrEmpty(line.label) || HasLabels(group))
                {
                    var label = PaperText(row, line.label ?? "", 17f, line.style == "example" ? FontStyles.Italic : FontStyles.Normal);
                    NLUi.Size(label, 290f);
                }
                foreach (string segment in Regex.Split(line.text ?? "", @"(\{\d+\})"))
                {
                    if (segment.Length == 0) continue;
                    var m = Regex.Match(segment, @"^\{(\d+)\}$");
                    if (m.Success) GapInput(row, int.Parse(m.Groups[1].Value), group.limit);
                    else
                    {
                        var t = PaperText(row, segment, 17f, line.style == "example" ? FontStyles.Italic : FontStyles.Normal);
                        t.textWrappingMode = TextWrappingModes.Normal;
                        if (line.style == "example") t.color = PaperMuted;
                        var size = NLUi.Size(t, preferredWidth: Mathf.Min(480f, t.GetPreferredValues(segment, 480f, 0f).x + 4f), flexibleWidth: 1f);
                        size.minWidth = 32f;
                    }
                }
                var filler = new GameObject("Fill", typeof(RectTransform), typeof(LayoutElement));
                filler.transform.SetParent(row, false);
                filler.GetComponent<LayoutElement>().flexibleWidth = 1f;
            }
        }

        private static bool HasLabels(IeltsGroup group) => group.lines != null && group.lines.Any(l => !string.IsNullOrEmpty(l.label) && l.style != "subheading");

        private void GapInput(RectTransform row, int number, string limit)
        {
            var holder = Row(row, "Gap" + number, 6f);
            NLUi.Size(holder, 244f);
            _anchors[number] = holder;
            var chip = NLUi.Pill(holder, "No", number.ToString(), _font, Accent, Color.white, 14f);
            NLUi.Size(chip, 34f, 30f);
            var input = MakeInput(holder, 200f, 34f);
            input.text = GetAnswer(number);
            input.onValueChanged.AddListener(v => SetAnswer(number, v.Trim()));
            input.characterLimit = 40;
        }

        private void RenderSingleChoice(RectTransform sheet, IeltsGroup group)
        {
            GroupHeader(sheet, group);
            if (!string.IsNullOrWhiteSpace(group.heading)) PaperText(sheet, group.heading, 21f, FontStyles.Bold).alignment = TextAlignmentOptions.Center;
            foreach (var item in group.items ?? Array.Empty<IeltsItem>())
            {
                var block = Column(sheet, "Q" + item.number, 6f);
                _anchors[item.number] = block;
                PaperText(block, $"<b>{item.number}</b>   {item.text}", 17.5f, FontStyles.Normal);
                ChoiceList(block, item.number, item.options, multi: false, null);
            }
        }

        private void RenderFixedChoice(RectTransform sheet, IeltsGroup group, params string[] labels)
        {
            GroupHeader(sheet, group);
            var options = labels.Select(l => new IeltsOption { letter = l, text = "" }).ToArray();
            foreach (var item in group.items ?? Array.Empty<IeltsItem>())
            {
                var block = Column(sheet, "Q" + item.number, 6f);
                _anchors[item.number] = block;
                PaperText(block, $"<b>{item.number}</b>   {item.text}", 17.5f, FontStyles.Normal);
                LetterRow(block, item.number, options);
            }
        }

        private void RenderMultiChoice(RectTransform sheet, IeltsGroup group)
        {
            GroupHeader(sheet, group);
            if (!string.IsNullOrWhiteSpace(group.heading)) PaperText(sheet, group.heading, 21f, FontStyles.Bold).alignment = TextAlignmentOptions.Center;
            var block = Column(sheet, $"Q{group.from}-{group.to}", 6f);
            for (int n = group.from; n <= group.to; n++) _anchors[n] = block;
            PaperText(block, group.stem, 17.5f, FontStyles.Normal);
            ChoiceList(block, group.from, group.options, multi: true, group);
        }

        /// <summary>Option rows; for "choose N" the chosen letters are stored in order across from..to.</summary>
        private void ChoiceList(RectTransform parent, int number, IeltsOption[] options, bool multi, IeltsGroup group)
        {
            var rows = new List<(Image bg, string letter)>();
            foreach (var option in options ?? Array.Empty<IeltsOption>())
            {
                string letter = option.letter;
                var button = NLUi.Button(parent, "Opt" + letter, $"<b>{letter}</b>     {option.text}", _font, null, Color.white, 17f, PaperInk, 40f);
                button.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
                var bg = button.GetComponent<Image>();
                rows.Add((bg, letter));
                button.onClick.AddListener(() =>
                {
                    if (IsSubmitted) return;
                    if (!multi) { SetAnswer(number, GetAnswer(number) == letter ? "" : letter); return; }
                    var chosen = Enumerable.Range(group.from, group.to - group.from + 1).Select(GetAnswer).Where(v => v.Length > 0).ToList();
                    if (chosen.Contains(letter)) chosen.Remove(letter);
                    else if (chosen.Count < Mathf.Max(1, group.choose)) chosen.Add(letter);
                    chosen.Sort(string.CompareOrdinal);
                    for (int n = group.from; n <= group.to; n++) SetAnswer(n, n - group.from < chosen.Count ? chosen[n - group.from] : "");
                });
            }
            void Refresh()
            {
                var selected = multi
                    ? new HashSet<string>(Enumerable.Range(group.from, group.to - group.from + 1).Select(GetAnswer))
                    : new HashSet<string> { GetAnswer(number) };
                foreach (var (bg, letter) in rows)
                    bg.color = selected.Contains(letter) ? new Color(0.78f, 0.88f, 0.98f, 1f) : new Color(1f, 1f, 1f, 0.95f);
            }
            if (multi) for (int n = group.from; n <= group.to; n++) _refreshers[n] = Refresh;
            else _refreshers[number] = Refresh;
            Refresh();
        }

        private void RenderMatching(RectTransform sheet, IeltsGroup group)
        {
            GroupHeader(sheet, group);
            var box = Box(sheet, "Options");
            if (!string.IsNullOrWhiteSpace(group.optionsTitle)) PaperText(box, group.optionsTitle, 18f, FontStyles.Bold).alignment = TextAlignmentOptions.Center;
            foreach (var option in group.options ?? Array.Empty<IeltsOption>())
                PaperText(box, $"<b>{option.letter}</b>     {option.text}", 17f, FontStyles.Normal);
            if (!string.IsNullOrWhiteSpace(group.heading)) PaperText(sheet, group.heading, 18f, FontStyles.Bold);
            // A summary completed from the box: show its text with numbered gaps; the letters are picked per gap below.
            foreach (var line in group.lines ?? Array.Empty<IeltsLine>())
                PaperText(sheet, Regex.Replace(line.text ?? "", @"\{(\d+)\}", "<b>$1</b> ________"), 17f, FontStyles.Normal);
            foreach (var item in group.items ?? Array.Empty<IeltsItem>())
            {
                var row = Row(sheet, "Q" + item.number, 10f);
                _anchors[item.number] = row;
                var label = PaperText(row, $"<b>{item.number}</b>   {item.text}", 17f, FontStyles.Normal);
                NLUi.Size(label, 360f);
                LetterRow(row, item.number, group.options);
                var filler = new GameObject("Fill", typeof(RectTransform), typeof(LayoutElement));
                filler.transform.SetParent(row, false);
                filler.GetComponent<LayoutElement>().flexibleWidth = 1f;
            }
        }

        private void LetterRow(RectTransform parent, int number, IeltsOption[] options)
        {
            var row = Row(parent, "Letters", 6f);
            var cells = new List<(Image bg, TextMeshProUGUI label, string letter)>();
            foreach (var option in options ?? Array.Empty<IeltsOption>())
            {
                string letter = option.letter;
                var button = NLUi.Button(row, "L" + letter, letter, _font, () => { if (!IsSubmitted) SetAnswer(number, GetAnswer(number) == letter ? "" : letter); },
                    Color.white, 15f, PaperInk, 36f);
                NLUi.Size(button, letter.Length > 1 ? 118f : 42f, 36f);
                cells.Add((button.GetComponent<Image>(), button.GetComponentInChildren<TextMeshProUGUI>(), letter));
            }
            void Refresh()
            {
                string chosen = GetAnswer(number);
                foreach (var (bg, label, letter) in cells)
                {
                    bool on = chosen == letter;
                    bg.color = on ? Accent : new Color(0.93f, 0.93f, 0.91f, 1f);
                    label.color = on ? Color.white : PaperInk;
                }
            }
            _refreshers[number] = Refresh;
            Refresh();
        }
    }
}
