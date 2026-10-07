using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Layout-driven builders for the runtime UI windows (dialogue, shop, bag, map, station). Every text
    /// element wraps inside its parent and every container sizes itself from its children, so long
    /// Japanese/Vietnamese lines can no longer spill outside a panel. Colours follow UIStyleKit.
    /// </summary>
    public static class NLUi
    {
        public static readonly Color Ink = new Color(0.055f, 0.07f, 0.095f, 0.96f);
        public static readonly Color Card = new Color(0.11f, 0.135f, 0.165f, 1f);
        public static readonly Color CardHover = new Color(0.16f, 0.19f, 0.23f, 1f);
        public static readonly Color Line = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color Gold = UIStyleKit.AccentGold;
        public static readonly Color Text = new Color(0.95f, 0.96f, 0.98f, 1f);
        public static readonly Color Muted = new Color(0.66f, 0.72f, 0.8f, 1f);
        public static readonly Color Soft = new Color(0.95f, 0.84f, 0.6f, 1f);
        public static readonly Color Good = new Color(0.3f, 0.75f, 0.5f, 1f);
        public static readonly Color Bad = new Color(0.9f, 0.4f, 0.36f, 1f);

        public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            // Nested under another canvas (e.g. the HUD) the RectTransform defaults to a 100×100 box in the
            // middle of the screen; stretch it so anchored windows land at the real screen edges.
            if (parent != null && parent.GetComponentInParent<Canvas>() != null) Stretch((RectTransform)go.transform);
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        /// <summary>Rounded panel. Pass padding/spacing to make it a vertical layout that fits its content.</summary>
        public static RectTransform Panel(Transform parent, string name, Color color, RectOffset padding = null, float spacing = -1f, bool vertical = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = UIStyleKit.RoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            if (padding != null)
            {
                HorizontalOrVerticalLayoutGroup layout = vertical ? go.AddComponent<VerticalLayoutGroup>() : go.AddComponent<HorizontalLayoutGroup>();
                layout.padding = padding;
                layout.spacing = Mathf.Max(0f, spacing);
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = vertical;
                layout.childForceExpandHeight = false;
                layout.childAlignment = vertical ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            }
            return (RectTransform)go.transform;
        }

        /// <summary>Invisible layout row/column.</summary>
        public static RectTransform Group(Transform parent, string name, bool vertical, float spacing, TextAnchor alignment = TextAnchor.UpperLeft, bool expandWidth = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            HorizontalOrVerticalLayoutGroup layout = vertical ? go.AddComponent<VerticalLayoutGroup>() : go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = vertical && expandWidth;
            layout.childForceExpandHeight = false;
            layout.childAlignment = alignment;
            return (RectTransform)go.transform;
        }

        public static TextMeshProUGUI Label(Transform parent, string name, string value, float size, Color color, TMP_FontAsset font,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.richText = true;
            return text;
        }

        public static LayoutElement Size(Component target, float preferredWidth = -1f, float preferredHeight = -1f, float flexibleWidth = -1f, float minHeight = -1f)
        {
            var element = target.GetComponent<LayoutElement>();
            if (element == null) element = target.gameObject.AddComponent<LayoutElement>();
            if (preferredWidth >= 0f) element.preferredWidth = preferredWidth;
            if (preferredHeight >= 0f) element.preferredHeight = preferredHeight;
            if (flexibleWidth >= 0f) element.flexibleWidth = flexibleWidth;
            if (minHeight >= 0f) element.minHeight = minHeight;
            return element;
        }

        public static void FitContent(RectTransform rect, bool width = false, bool height = true)
        {
            var fitter = rect.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = width ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = height ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
        }

        public static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>Card-style button whose label wraps; returns the button and its text.</summary>
        public static Button Button(Transform parent, string name, string label, TMP_FontAsset font, Action onClick, Color baseColor, float fontSize = 20f,
            Color? textColor = null, float minHeight = 52f)
        {
            var rect = Panel(parent, name, baseColor, new RectOffset(18, 18, 10, 10), 0f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            UIStyleKit.StyleButton(button, baseColor, Color.Lerp(baseColor, Color.white, 0.12f), Color.Lerp(baseColor, Color.black, 0.2f));
            var text = Label(rect, "Label", label, fontSize, textColor ?? Text, font, FontStyles.Normal, TextAlignmentOptions.Center);
            Size(rect, minHeight: minHeight);
            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        public static RectTransform Divider(Transform parent)
        {
            var go = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Line;
            Size(go.transform, preferredHeight: 1f);
            return (RectTransform)go.transform;
        }

        public static RectTransform Pill(Transform parent, string name, string value, TMP_FontAsset font, Color background, Color textColor, float size = 18f)
        {
            var rect = Panel(parent, name, background, new RectOffset(16, 16, 5, 6), 0f, vertical: false);
            ((HorizontalLayoutGroup)rect.GetComponent<HorizontalOrVerticalLayoutGroup>()).childForceExpandWidth = false;
            var text = Label(rect, "Text", value, size, textColor, font, FontStyles.Bold, TextAlignmentOptions.Center);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            FitContent(rect, width: true, height: true);
            return rect;
        }

        public static TMP_FontAsset ResolveFont()
        {
            foreach (var text in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
                if (text != null && text.font != null && text.font.name.IndexOf("Noto", StringComparison.OrdinalIgnoreCase) >= 0) return text.font;
            return TMP_Settings.defaultFontAsset;
        }
    }
}
