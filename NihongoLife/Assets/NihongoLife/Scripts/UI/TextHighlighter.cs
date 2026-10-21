using System;
using NihongoLife.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>One highlighted range of a text, in indices of its source string (rich-text tags included).</summary>
    [Serializable]
    public struct HighlightSpan
    {
        public int start, end, color;
        public HighlightSpan(int start, int end, int color) { this.start = start; this.end = end; this.color = color; }
    }

    /// <summary>The highlights of one exam attempt, per text block. Keys are stable for a block (part + text hash),
    /// so re-rendering a part or reopening the attempt finds them again.</summary>
    public sealed class HighlightBook
    {
        private readonly Dictionary<string, List<HighlightSpan>> _spans = new();
        private readonly Dictionary<string, string> _sources = new();
        public event Action Changed;

        public IReadOnlyDictionary<string, List<HighlightSpan>> All => _spans;

        public List<HighlightSpan> Get(string key) => _spans.TryGetValue(key, out var list) ? list : null;

        public void Set(string key, List<HighlightSpan> spans, bool notify = true)
        {
            if (spans == null || spans.Count == 0) _spans.Remove(key);
            else _spans[key] = spans;
            if (notify) Changed?.Invoke();
        }

        public void Clear(Func<string, bool> where = null)
        {
            foreach (var key in _spans.Keys.Where(k => where == null || where(k)).ToList()) _spans.Remove(key);
            Changed?.Invoke();
            TextHighlighter.RefreshAll(this);
        }

        public int Count(Func<string, bool> where = null) => _spans.Where(kv => where == null || where(kv.Key)).Sum(kv => kv.Value.Count);

        internal void RememberSource(string key, string source) => _sources[key] = source;

        /// <summary>Highlighted passages as plain text, in reading order of the blocks seen so far.</summary>
        public List<string> Snippets(Func<string, bool> where = null)
        {
            var result = new List<string>();
            foreach (var kv in _sources)
            {
                if (where != null && !where(kv.Key) || !_spans.TryGetValue(kv.Key, out var spans)) continue;
                foreach (var s in spans.OrderBy(s => s.start))
                {
                    if (s.start < 0 || s.end > kv.Value.Length || s.end <= s.start) continue;
                    string plain = TextHighlighter.StripTags(kv.Value.Substring(s.start, s.end - s.start)).Trim();
                    if (plain.Length > 0) result.Add(plain);
                }
            }
            return result;
        }

        public static string KeyFor(string scope, string source)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in source ?? string.Empty) hash = (hash ^ c) * 16777619;
                return $"{scope}/{hash:x8}";
            }
        }
    }

    /// <summary>The highlighter pen shared by every exam screen: off, pen (with a colour) or eraser.</summary>
    public static class HighlightTool
    {
        public enum ToolMode { Off, Pen, Eraser }

        public static readonly Color[] Colors =
        {
            new Color(1f, 0.84f, 0.29f, 0.55f),   // yellow
            new Color(0.49f, 0.88f, 0.54f, 0.5f), // green
            new Color(1f, 0.56f, 0.72f, 0.5f),    // pink
        };

        public static ToolMode Mode { get; private set; } = ToolMode.Off;
        public static int ColorIndex { get; private set; }
        public static event Action Changed;

        public static void SetMode(ToolMode mode)
        {
            Mode = mode;
            Changed?.Invoke();
        }

        public static void SetColor(int index)
        {
            ColorIndex = Mathf.Clamp(index, 0, Colors.Length - 1);
            Mode = ToolMode.Pen;
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Mode = ToolMode.Off;
            ColorIndex = 0;
            Changed = null;
        }
    }

    /// <summary>
    /// Lets the candidate mark up a TextMeshPro text block like the computer-delivered exam: with the pen on, drag
    /// across words (or click one word) to highlight them; with the eraser, click or drag over a highlight to remove
    /// it. Highlights are drawn with TMP &lt;mark&gt; tags inserted into the block's own source text, so they wrap with
    /// the text and survive scrolling and resizing. While the tool is off the text ignores the pointer, so the
    /// surrounding scroll view and answer controls behave exactly as before.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class TextHighlighter : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IInitializePotentialDragHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private static readonly List<TextHighlighter> Active = new();
        private static readonly Regex Tags = new Regex("<[^>]*>", RegexOptions.Compiled);

        private TextMeshProUGUI _text;
        private HighlightBook _book;
        private string _key;
        private string _source = string.Empty;
        private string _rendered;
        private int[] _renderedToSource = Array.Empty<int>();
        private int _anchorChar = -1;
        private bool _dragging;
        private List<HighlightSpan> _preview;

        public string Key => _key;
        public HighlightBook Book => _book;

        /// <summary>Attaches (or rebinds) a highlighter to <paramref name="text"/>; call again whenever the text's
        /// content is replaced. Its current text becomes the source the highlights refer to.</summary>
        public static TextHighlighter Bind(TextMeshProUGUI text, HighlightBook book, string key)
        {
            if (text == null || book == null) return null;
            var highlighter = text.GetComponent<TextHighlighter>();
            if (highlighter == null) highlighter = text.gameObject.AddComponent<TextHighlighter>();
            highlighter._text = text;
            highlighter._book = book;
            highlighter._key = key;
            highlighter._source = text.text ?? string.Empty;
            book.RememberSource(key, highlighter._source);
            highlighter.Render();
            highlighter.ApplyTool();
            return highlighter;
        }

        public static void RefreshAll(HighlightBook book)
        {
            foreach (var h in Active) if (h != null && h._book == book) h.Render();
        }

        public static string StripTags(string value) => Tags.Replace(value ?? string.Empty, string.Empty);

        private void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
            HighlightTool.Changed += ApplyTool;
            ApplyTool();
        }

        private void OnDisable()
        {
            Active.Remove(this);
            HighlightTool.Changed -= ApplyTool;
        }

        private void ApplyTool()
        {
            if (_text != null) _text.raycastTarget = HighlightTool.Mode != HighlightTool.ToolMode.Off;
        }

        // ─────────── Rendering ───────────

        private void Render()
        {
            if (_text == null) return;
            var spans = _preview ?? _book?.Get(_key);
            var sb = new StringBuilder(_source.Length + 32);
            var map = new List<int>(_source.Length + 32);
            int at = 0;
            if (spans != null)
            {
                foreach (var span in spans.OrderBy(s => s.start))
                {
                    int start = Mathf.Clamp(span.start, at, _source.Length), end = Mathf.Clamp(span.end, start, _source.Length);
                    if (end <= start) continue;
                    Copy(sb, map, at, start);
                    Insert(sb, map, $"<mark=#{ColorUtility.ToHtmlStringRGBA(HighlightTool.Colors[Mathf.Clamp(span.color, 0, HighlightTool.Colors.Length - 1)])}>", start);
                    Copy(sb, map, start, end);
                    Insert(sb, map, "</mark>", end);
                    at = end;
                }
            }
            Copy(sb, map, at, _source.Length);
            map.Add(_source.Length);
            _rendered = sb.ToString();
            _renderedToSource = map.ToArray();
            if (_text.text != _rendered) _text.text = _rendered;
        }

        private void Copy(StringBuilder sb, List<int> map, int from, int to)
        {
            for (int i = from; i < to; i++) { sb.Append(_source[i]); map.Add(i); }
        }

        private static void Insert(StringBuilder sb, List<int> map, string tag, int sourceIndex)
        {
            sb.Append(tag);
            for (int i = 0; i < tag.Length; i++) map.Add(sourceIndex);
        }

        // ─────────── Pointer ───────────

        public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (HighlightTool.Mode == HighlightTool.ToolMode.Off || eventData.button != PointerEventData.InputButton.Left) return;
            _anchorChar = CharAt(eventData, exact: true);
            _dragging = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_anchorChar < 0) _anchorChar = CharAt(eventData, exact: false);
            _dragging = _anchorChar >= 0;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            int current = CharAt(eventData, exact: false);
            if (current < 0) return;
            var range = SourceRange(_anchorChar, current, expandWords: true);
            if (range.end <= range.start) return;
            _preview = Apply(_book.Get(_key), range.start, range.end);
            Render();
        }

        public void OnEndDrag(PointerEventData eventData) { }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (HighlightTool.Mode == HighlightTool.ToolMode.Off || _anchorChar < 0) { _anchorChar = -1; return; }
            int end = _dragging ? CharAt(eventData, exact: false) : _anchorChar;
            if (end < 0) end = _anchorChar;
            var range = SourceRange(_anchorChar, end, expandWords: true);
            _preview = null;
            _dragging = false;
            _anchorChar = -1;
            if (range.end > range.start) _book.Set(_key, Apply(_book.Get(_key), range.start, range.end));
            Render();
        }

        /// <summary>Pen: paints [start, end) in the current colour over whatever was there. Eraser: removes every
        /// highlight touching [start, end).</summary>
        private static List<HighlightSpan> Apply(List<HighlightSpan> current, int start, int end)
        {
            var result = new List<HighlightSpan>();
            bool erase = HighlightTool.Mode == HighlightTool.ToolMode.Eraser;
            foreach (var s in current ?? new List<HighlightSpan>())
            {
                bool touches = s.start < end && s.end > start;
                if (!touches) { result.Add(s); continue; }
                if (erase) continue;
                if (s.start < start) result.Add(new HighlightSpan(s.start, start, s.color));
                if (s.end > end) result.Add(new HighlightSpan(end, s.end, s.color));
            }
            if (!erase) result.Add(new HighlightSpan(start, end, HighlightTool.ColorIndex));
            // Merge neighbours of the same colour so one stroke reads as one highlight.
            result = result.OrderBy(s => s.start).ToList();
            for (int i = result.Count - 1; i > 0; i--)
            {
                var a = result[i - 1];
                var b = result[i];
                if (a.color == b.color && b.start <= a.end + 1)
                {
                    result[i - 1] = new HighlightSpan(a.start, Mathf.Max(a.end, b.end), a.color);
                    result.RemoveAt(i);
                }
            }
            return result;
        }

        private int CharAt(PointerEventData eventData, bool exact)
        {
            if (_text == null) return -1;
            _text.ForceMeshUpdate();
            var camera = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
            int index = TMP_TextUtilities.FindIntersectingCharacter(_text, eventData.position, camera, true);
            if (index < 0 && !exact) index = TMP_TextUtilities.FindNearestCharacter(_text, eventData.position, camera, true);
            return index < 0 || index >= _text.textInfo.characterCount ? -1 : index;
        }

        /// <summary>Character indices (textInfo) → source string range; Latin text snaps to whole words, Japanese
        /// (no spaces) keeps the exact characters dragged over.</summary>
        private (int start, int end) SourceRange(int a, int b, bool expandWords)
        {
            var info = _text.textInfo;
            if (info == null || info.characterCount == 0 || _renderedToSource.Length == 0) return (0, 0);
            int first = Mathf.Clamp(Mathf.Min(a, b), 0, info.characterCount - 1);
            int last = Mathf.Clamp(Mathf.Max(a, b), 0, info.characterCount - 1);
            if (expandWords)
            {
                while (first > 0 && IsWordChar(info.characterInfo[first].character) && IsWordChar(info.characterInfo[first - 1].character)) first--;
                while (last < info.characterCount - 1 && IsWordChar(info.characterInfo[last].character) && IsWordChar(info.characterInfo[last + 1].character)) last++;
            }
            while (first < last && char.IsWhiteSpace(info.characterInfo[first].character)) first++;
            while (last > first && char.IsWhiteSpace(info.characterInfo[last].character)) last--;
            if (char.IsWhiteSpace(info.characterInfo[first].character)) return (0, 0);
            int start = ToSource(info.characterInfo[first].index);
            var lastInfo = info.characterInfo[last];
            int end = ToSource(lastInfo.index + Mathf.Max(1, lastInfo.stringLength) - 1) + 1;
            return (start, Mathf.Min(end, _source.Length));
        }

        private int ToSource(int renderedIndex) => _renderedToSource[Mathf.Clamp(renderedIndex, 0, _renderedToSource.Length - 1)];

        private static bool IsWordChar(char c) => c < 0x2E80 && (char.IsLetterOrDigit(c) || c == '\'' || c == '’' || c == '-');
    }

    /// <summary>The pen / colours / eraser / clear / notebook strip shown in the exam headers (icons + labels in the
    /// interface language; compact = shorter labels for narrow headers).</summary>
    public static class HighlightToolbar
    {
        private sealed class State : MonoBehaviour { public bool Compact; }

        private static readonly (string name, string icon, string[] full, string[] compact, float fullWidth, float compactWidth)[] Tools =
        {
            ("Pen", "ink_highlighter", new[] { "Tô sáng", "Highlight", "マーカー" }, new[] { "Tô", "Pen", "ペン" }, 118f, 70f),
            ("Eraser", "ink_eraser", new[] { "Xoá tô", "Erase", "消す" }, new[] { "Tẩy", "Erase", "消す" }, 104f, 76f),
            ("ClearAll", "delete_sweep", new[] { "Xoá hết", "Clear all", "全消去" }, new[] { "Xoá", "Clear", "全消" }, 108f, 76f),
            ("CopyToNotebook", "note_add", new[] { "Chép vào sổ", "To notebook", "ノートへ" }, new[] { "→Sổ", "→Note", "→帳" }, 128f, 78f),
            ("Notebook", "book_2", new[] { "Sổ tay", "Notebook", "ノート" }, new[] { "Sổ", "Notes", "帳" }, 106f, 66f),
        };

        /// <summary>Builds the strip under <paramref name="parent"/>. <paramref name="clearAll"/> clears this exam's
        /// highlights; <paramref name="copyToNotebook"/> copies the highlighted passages into the notebook.</summary>
        public static RectTransform Create(Transform parent, TMP_FontAsset font, Action clearAll, Action copyToNotebook, float height = 40f)
        {
            var row = NLUi.Group(parent, "HighlightToolbar", false, 6f, TextAnchor.MiddleLeft, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childForceExpandWidth = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            // Sizes itself from its buttons whenever it is laid out (it may be built while its screen is hidden).
            NLUi.FitContent(row, width: true, height: false);
            var state = row.gameObject.AddComponent<State>();

            var pen = Tool(row, state, 0, height, () =>
                HighlightTool.SetMode(HighlightTool.Mode == HighlightTool.ToolMode.Pen ? HighlightTool.ToolMode.Off : HighlightTool.ToolMode.Pen));
            var swatches = new List<Image>();
            for (int i = 0; i < HighlightTool.Colors.Length; i++)
            {
                int index = i;
                var go = new GameObject("Color" + i, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                go.transform.SetParent(row, false);
                var image = go.GetComponent<Image>();
                image.sprite = UiKit.Circle;
                var c = HighlightTool.Colors[i];
                image.color = new Color(c.r, c.g, c.b, 1f);
                go.GetComponent<Button>().onClick.AddListener(() => HighlightTool.SetColor(index));
                var element = go.GetComponent<LayoutElement>();
                element.preferredWidth = element.minWidth = height * 0.56f;
                element.preferredHeight = height * 0.56f;
                swatches.Add(image);
            }
            var eraser = Tool(row, state, 1, height, () =>
                HighlightTool.SetMode(HighlightTool.Mode == HighlightTool.ToolMode.Eraser ? HighlightTool.ToolMode.Off : HighlightTool.ToolMode.Eraser));
            if (clearAll != null) Tool(row, state, 2, height, clearAll);
            if (copyToNotebook != null) Tool(row, state, 3, height, copyToNotebook);
            Tool(row, state, 4, height, () => NotebookUI.Toggle());

            void Refresh()
            {
                if (pen == null) { HighlightTool.Changed -= Refresh; return; }
                UiKit.SetKind(pen, HighlightTool.Mode == HighlightTool.ToolMode.Pen ? UiKit.Kind.Accent : UiKit.Kind.Ghost);
                UiKit.SetKind(eraser, HighlightTool.Mode == HighlightTool.ToolMode.Eraser ? UiKit.Kind.Danger : UiKit.Kind.Ghost);
                for (int i = 0; i < swatches.Count; i++)
                {
                    bool selected = HighlightTool.Mode == HighlightTool.ToolMode.Pen && HighlightTool.ColorIndex == i;
                    swatches[i].transform.localScale = selected ? Vector3.one * 1.2f : Vector3.one * 0.86f;
                }
            }
            HighlightTool.Changed += Refresh;
            Refresh();
            return row;
        }

        /// <summary>Short labels for narrow screens (the strip shares the exam header with the title and timer).</summary>
        public static void SetCompact(RectTransform toolbar, bool compact)
        {
            if (toolbar == null) return;
            var state = toolbar.GetComponent<State>();
            if (state != null) state.Compact = compact;
            foreach (var tool in Tools)
            {
                var child = toolbar.Find(tool.name);
                if (child == null) continue;
                var label = UiKit.LabelOf(child.GetComponent<Button>());
                if (label != null) LocText.Set(label, () => Pick(state != null && state.Compact ? tool.compact : tool.full));
                NLUi.Size(child, compact ? tool.compactWidth : tool.fullWidth);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(toolbar);
        }

        private static string Pick(string[] labels) => Loc.T(labels[0], labels[1], labels[2]);

        private static Button Tool(Transform row, State state, int index, float height, Action onClick)
        {
            var tool = Tools[index];
            var button = UiKit.Button(row, tool.name, null, null, null, onClick, UiKit.Kind.Ghost, tool.icon, 13.5f, Mathf.RoundToInt(height * 0.5f));
            LocText.Set(UiKit.LabelOf(button), () => Pick(state.Compact ? tool.compact : tool.full));
            UiKit.Size(button, tool.fullWidth, height);
            return button;
        }
    }
}
