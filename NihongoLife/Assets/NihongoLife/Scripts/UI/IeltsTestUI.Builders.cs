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
    /// <summary>Partial of IeltsTestUI: small layout builders shared by the paper. The MonoBehaviour entry point stays IeltsTestUI.cs.</summary>
    public sealed partial class IeltsTestUI : MonoBehaviour
    {
        // ─────────── Small builders ───────────

        private RectTransform Strip(RectTransform parent, string name, float offset, float height, bool top, Color color)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rect.anchoredPosition = new Vector2(0f, top ? -offset : offset);
            rect.sizeDelta = new Vector2(0f, height);
            rect.GetComponent<Image>().color = color;
            return rect;
        }

        private static void Place(RectTransform rect, float x, float y, float w, float h, Vector2 anchor)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
        }

        private TextMeshProUGUI Text(RectTransform parent, string name, string value, float size, Color color, TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
        {
            var t = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            t.transform.SetParent(parent, false);
            t.font = _font; t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = style;
            t.text = value; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        private TextMeshProUGUI PaperText(RectTransform parent, string value, float size, FontStyles style)
        {
            var t = NLUi.Label(parent, "T", value, size, PaperInk, _font, style);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.richText = true;
            if (_highlightPaper && !string.IsNullOrWhiteSpace(value))
                TextHighlighter.Bind(t, _highlights, HighlightBook.KeyFor("p" + _viewPart, value));
            return t;
        }

        private RectTransform Row(RectTransform parent, string name, float spacing)
        {
            var row = NLUi.Group(parent, name, false, spacing, TextAnchor.MiddleLeft, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childForceExpandWidth = false;
            layout.childControlWidth = true;
            NLUi.FitContent(row, width: false, height: true);
            return row;
        }

        private RectTransform Column(RectTransform parent, string name, float spacing) => NLUi.Group(parent, name, true, spacing);

        private RectTransform Box(RectTransform parent, string name)
        {
            var box = NLUi.Panel(parent, name, new Color(1f, 1f, 1f, 1f), new RectOffset(26, 26, 18, 20), 8f);
            var outline = box.gameObject.AddComponent<Outline>();
            outline.effectColor = PaperLine;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return box;
        }

        private ScrollRect MakeScroll(RectTransform parent, string name, out RectTransform content)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image)).GetComponent<RectTransform>();
            viewport.SetParent(rect, false);
            NLUi.Stretch(viewport);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var paper = new GameObject("Paper", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            paper.SetParent(viewport, false);
            paper.anchorMin = paper.anchorMax = new Vector2(0.5f, 1f);
            paper.pivot = new Vector2(0.5f, 1f);
            paper.sizeDelta = new Vector2(1180f, 0f);
            paper.GetComponent<Image>().color = Paper;
            var layout = paper.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 34, 44);
            layout.spacing = 10f;
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            paper.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = go.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = paper;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 38f;
            content = paper;
            // Narrow the paper to its column when the passage is shown beside it.
            go.AddComponent<IeltsPaperWidth>().Configure(paper, rect);
            return scroll;
        }

        private TMP_InputField MakeInput(RectTransform parent, float width, float height, bool multiline = false)
        {
            var go = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var element = go.GetComponent<LayoutElement>();
            element.preferredWidth = width; element.minWidth = width; element.preferredHeight = height; element.minHeight = height;
            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 0.97f, 1f);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.55f, 0.6f, 0.68f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);
            var area = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            area.SetParent(go.transform, false);
            NLUi.Stretch(area);
            area.offsetMin = new Vector2(10f, 4f); area.offsetMax = new Vector2(-10f, -4f);
            var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(area, false);
            NLUi.Stretch(text.rectTransform);
            text.font = _font; text.fontSize = 17f; text.color = PaperInk;
            text.alignment = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
            var input = go.GetComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = text;
            input.targetGraphic = image;
            input.fontAsset = _font;
            input.pointSize = 17f;
            input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
            input.richText = false;
            return input;
        }
    }
}
