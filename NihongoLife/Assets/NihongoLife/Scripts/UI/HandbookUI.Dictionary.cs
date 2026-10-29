using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Content;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>The 辞書 chapter of the Handbook: a search box and a language pair on the left page, the picked word on the right.</summary>
    public sealed partial class HandbookUI
    {
        private string _dictFrom = "ja", _dictTo = "vi", _dictQuery = "", _dictLangCode;
        private RectTransform _dictResults;
        private TMP_InputField _dictInput;

        public static string DictionaryQuery => _instance != null ? _instance._dictQuery : "";
        public static string DictionaryFrom => _instance != null ? _instance._dictFrom : "ja";
        public static string DictionaryTo => _instance != null ? _instance._dictTo : "vi";

        /// <summary>Types into the dictionary box (tests and deep links): optional language pair, then the query.</summary>
        public static void DictionarySearch(string query, string from = null, string to = null)
        {
            Open(Chapter.Dictionary);
            var ui = _instance;
            if (ui == null) return;
            if (!string.IsNullOrEmpty(from) && DictionaryService.Supports(from, to ?? ui._dictTo)) { ui._dictFrom = from; if (!string.IsNullOrEmpty(to)) ui._dictTo = to; }
            else if (!string.IsNullOrEmpty(to) && DictionaryService.Supports(ui._dictFrom, to)) ui._dictTo = to;
            ui._dictQuery = query ?? "";
            ui._dictLangCode = null; // rebuild the controls so the pair buttons and the box show the new state
            ui.Show(Chapter.Dictionary, null);
        }

        /// <summary>Where the entry rows go. For the dictionary the search controls stay (so typing keeps the focus), only the results are rebuilt.</summary>
        private RectTransform ListHost()
        {
            if (_chapter != Chapter.Dictionary) { UiKit.Clear(_list); _dictResults = null; _dictInput = null; return _list; }
            if (_dictResults == null || _dictLangCode != Loc.Code)
            {
                UiKit.Clear(_list);
                _dictLangCode = Loc.Code;
                BuildDictionaryControls(_list);
            }
            else UiKit.Clear(_dictResults);
            return _dictResults;
        }

        private void BuildDictionaryControls(RectTransform parent)
        {
            var color = ChapterColor(Chapter.Dictionary);
            PairRow(parent, Loc.T("Từ:", "From:", "元："), _dictFrom, code =>
            {
                _dictFrom = code;
                if (_dictTo == code) _dictTo = DictionaryService.Languages.First(l => l != code);
                _dictLangCode = null; Show(Chapter.Dictionary, null);
            }, color);
            PairRow(parent, Loc.T("Sang:", "To:", "先："), _dictTo, code =>
            {
                _dictTo = code;
                if (_dictFrom == code) _dictFrom = DictionaryService.Languages.First(l => l != code);
                _dictLangCode = null; Show(Chapter.Dictionary, null);
            }, color);

            var box = Rounded(parent, "SearchBox", new Color(1f, 1f, 1f, 0.8f), 10);
            NLUi.Size(box, -1f, 48f);
            _dictInput = MakeInput(box, Loc.T("Gõ từ cần tra… (chữ, kana hoặc cách đọc)", "Type a word… (text, kana or reading)", "調べたい言葉…（文字・かな・読み）"));
            _dictInput.SetTextWithoutNotify(_dictQuery);
            _dictInput.onValueChanged.AddListener(text => { _dictQuery = text ?? ""; Show(Chapter.Dictionary, null); });
            _dictResults = NLUi.Group(parent, "Results", true, 6f);
        }

        private void PairRow(RectTransform parent, string label, string selected, Action<string> pick, Color color)
        {
            var row = NLUi.Group(parent, "Pair", false, 8f, TextAnchor.MiddleLeft, false);
            var l = NLUi.Label(row, "Label", label, 15f, InkSoft, _font, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            NLUi.Size(l, 64f, 34f);
            foreach (var code in DictionaryService.Languages)
            {
                string c = code;
                bool on = c == selected;
                var b = NLUi.Button(row, "Lang_" + c, c.ToUpperInvariant(), _font, () => pick(c), on ? color : new Color(0.85f, 0.8f, 0.7f, 1f), 14f, on ? Color.white : Ink, 34f);
                NLUi.Size(b, 64f, 34f);
                // A 34 px button keeps room for its 14 pt code (NLUi.Button's default padding is 10 px top and bottom).
                ((HorizontalOrVerticalLayoutGroup)b.GetComponent<HorizontalOrVerticalLayoutGroup>()).padding = new RectOffset(6, 6, 4, 4);
            }
        }

        private TMP_InputField MakeInput(RectTransform box, string placeholder)
        {
            var go = new GameObject("Input", typeof(RectTransform), typeof(TMP_InputField));
            go.transform.SetParent(box, false);
            NLUi.Stretch((RectTransform)go.transform, 12f);
            var input = go.GetComponent<TMP_InputField>();
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 40;
            input.richText = false;
            input.caretColor = Ink;
            input.customCaretColor = true;
            input.caretWidth = 2;
            input.selectionColor = new Color(0.98f, 0.82f, 0.32f, 0.45f);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            viewport.SetParent(go.transform, false);
            NLUi.Stretch(viewport);
            input.textViewport = viewport;
            TextMeshProUGUI Make(string name, Color color, FontStyles style)
            {
                var t = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                t.transform.SetParent(viewport, false);
                NLUi.Stretch(t.rectTransform);
                if (_font != null) t.font = _font;
                t.fontSize = 19f; t.color = color; t.fontStyle = style;
                t.alignment = TextAlignmentOptions.MidlineLeft;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.richText = false;
                return t;
            }
            input.textComponent = Make("Text", Ink, FontStyles.Normal);
            var ph = Make("Placeholder", new Color(0.43f, 0.4f, 0.36f, 0.7f), FontStyles.Italic);
            ph.text = placeholder;
            input.placeholder = ph;
            return input;
        }

        private List<Entry> DictionaryEntries()
        {
            var color = ChapterColor(Chapter.Dictionary);
            var list = new List<Entry>();
            string tag = _dictFrom.ToUpperInvariant() + " → " + _dictTo.ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(_dictQuery))
            {
                list.Add(new Entry
                {
                    id = "hint", glyph = "辞", color = color, title = Loc.T("Gõ để tra từ", "Type to look up a word", "言葉を入力"),
                    subtitle = Loc.F("{0} từ gốc trong cặp này", "{0} headwords in this pair", "この組み合わせの見出し語：{0}", DictionaryService.HeadwordCount(_dictFrom, _dictTo)),
                    build = p =>
                    {
                        Hero(p, "辞", color, Loc.T("Từ điển bốn thứ tiếng", "A four-language dictionary", "4言語の辞書"), "じしょ", tag);
                        Section(p, Loc.T("Cách dùng", "How to use", "使い方"), Loc.T(
                            "Chọn ngôn ngữ nguồn (Từ) và đích (Sang), rồi gõ vào ô tìm kiếm. Tiếng Nhật tra được bằng chữ Hán, kana hoặc cách đọc; katakana được tìm như hiragana.",
                            "Pick the source language (From) and the target (To), then type in the search box. Japanese can be found by kanji, kana or reading; katakana is matched like hiragana.",
                            "元の言語（元）と先の言語（先）を選び、検索ボックスに入力します。日本語は漢字・かな・読みで探せ、カタカナはひらがなと同じように探せます。"), color);
                        Section(p, Loc.T("Nội dung", "About the content", "内容について"), Loc.T(
                            "Nội dung do NihongoLife tự biên soạn cho người học, không sao chép từ điển nào; có thể còn thiếu từ hoặc nghĩa phụ.",
                            "The entries were written for NihongoLife learners, not copied from any dictionary; some words or secondary senses may be missing.",
                            "収録内容はNihongoLife学習者向けに独自に作成したもので、既存の辞書の複製ではありません。語や副次的な意味が不足している場合があります。"), color);
                    },
                });
                return list;
            }
            var hits = DictionaryService.Search(_dictFrom, _dictTo, _dictQuery, 40);
            if (hits.Count == 0)
            {
                list.Add(new Entry
                {
                    id = "none", glyph = "？", color = color, title = Loc.T("Không tìm thấy", "Nothing found", "見つかりません"),
                    subtitle = "“" + _dictQuery + "”",
                    build = p =>
                    {
                        Hero(p, "？", color, Loc.T("Không tìm thấy", "Nothing found", "見つかりません"), "", tag);
                        Section(p, "“" + _dictQuery + "”", Loc.T("Thử bỏ dấu cách, viết ngắn hơn, hoặc đổi cặp ngôn ngữ ở bên trái.", "Try without spaces, a shorter form, or another language pair on the left.", "スペースを除く、短くする、左で言語の組み合わせを変える、を試してください。"), color);
                    },
                });
                return list;
            }
            for (int i = 0; i < hits.Count; i++)
            {
                var hit = hits[i];
                string head = hit.Headword ?? "?";
                string glyph = head.Length > 1 && char.IsHighSurrogate(head[0]) ? head.Substring(0, 2) : head.Substring(0, 1);
                string reading = string.IsNullOrEmpty(hit.Reading) ? "" : hit.Reading;
                list.Add(new Entry
                {
                    id = "h" + i, glyph = glyph, color = color,
                    title = head + (reading.Length > 0 && reading != head ? "  【" + reading + "】" : ""),
                    subtitle = hit.Meaning, badge = hit.PartOfSpeech,
                    build = p => BuildDictionaryHit(p, hit, hits, color, tag),
                });
            }
            return list;
        }

        private void BuildDictionaryHit(RectTransform p, DictionaryService.Hit hit, List<DictionaryService.Hit> all, Color color, string tag)
        {
            string head = hit.Headword ?? "?";
            string glyph = head.Length > 1 && char.IsHighSurrogate(head[0]) ? head.Substring(0, 2) : head.Substring(0, 1);
            Hero(p, glyph, color, head, string.IsNullOrEmpty(hit.Reading) || hit.Reading == head ? "" : hit.Reading, tag);
            IllustrationRow(p, hit.Id, color);
            Section(p, Loc.T("Nghĩa", "Meaning", "意味"), hit.Meaning, color);
            if (!string.IsNullOrEmpty(hit.PartOfSpeech)) Section(p, Loc.T("Từ loại", "Part of speech", "品詞"), hit.PartOfSpeech, color);
            if (hit.Domains != null && hit.Domains.Length > 0) Section(p, Loc.T("Lĩnh vực", "Field", "分野"), string.Join(" · ", hit.Domains), color);
            var others = all.Where(h => h != hit && h.Headword == hit.Headword).ToList();
            if (others.Count > 0)
                Section(p, Loc.T("Nghĩa khác của từ này", "Other senses of this word", "この語の他の意味"), string.Join("\n", others.Select(o => "• " + o.Meaning + (string.IsNullOrEmpty(o.PartOfSpeech) ? "" : "  (" + o.PartOfSpeech + ")"))), color);
        }
    }
}