using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Shared panel frame + type scale, so every menu/panel gets the same border, inner highlight, corner accents
    /// and font sizes. Pure uGUI (no animation, no Update), so it is safe in Edit Mode and in batch renders.
    /// Fonts: Noto Sans JP (SIL OFL 1.1). Icons: Material Symbols (Apache 2.0). See Docs/UI_STYLE.md.
    /// </summary>
    public static class UiFrame
    {
        public enum Tone { Neutral, Gold, Teal }

        // ── Type scale (px at 1920×1080 reference) ──
        public const float Display = 40f, Title = 30f, Heading = 22f, Body = 15f, Small = 13f, Caption = 12f;
        public const float TitleSpacing = 1f;

        // ── Frame tokens ──
        public const int Radius = 22;
        public const float BorderWidth = 1.5f, CornerSize = 18f, CornerThickness = 2f;

        public static Color BorderColor(Tone tone) => tone switch
        {
            Tone.Gold => UiKit.Alpha(UiTheme.Gold, 0.38f),
            Tone.Teal => UiKit.Alpha(UiTheme.Teal, 0.40f),
            _ => new Color(1f, 1f, 1f, 0.13f),
        };

        public static Color AccentColor(Tone tone) => tone == Tone.Teal ? UiTheme.Teal : UiTheme.Gold;

        /// <summary>Adds the shared frame (border ring, top inner highlight, four corner accents) to a card. Idempotent.</summary>
        public static RectTransform Apply(RectTransform card, Tone tone = Tone.Gold, int radius = Radius)
        {
            if (card == null) return null;
            var old = card.Find("UiFrame");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = UiKit.Rect(card, "UiFrame");
            UiKit.Fill(root);
            root.SetAsFirstSibling();

            // Border: four hairlines along the edges (stopping short of the rounded corners), drawn behind every other
            // child. Lines, not a filled rect, so translucent or fading cards never pick up a tinted wash.
            float r = radius * 0.8f;
            var bc = BorderColor(tone);
            Edge(root, "EdgeTop", bc, new Vector2(0f, 1f), new Vector2(1f, 1f), r, 0f, BorderWidth);
            Edge(root, "EdgeBottom", bc, new Vector2(0f, 0f), new Vector2(1f, 0f), r, 0f, BorderWidth);
            Edge(root, "EdgeLeft", bc, new Vector2(0f, 0f), new Vector2(0f, 1f), 0f, r, BorderWidth);
            Edge(root, "EdgeRight", bc, new Vector2(1f, 0f), new Vector2(1f, 1f), 0f, r, BorderWidth);

            var hi = UiKit.Image(root, "InnerHighlight", new Color(1f, 1f, 1f, 0.06f));
            UiKit.Strip(hi.rectTransform, true, 1f, 1f, radius, radius);

            var accent = AccentColor(tone);
            Corner(root, "TL", new Vector2(0f, 1f), accent);
            Corner(root, "TR", new Vector2(1f, 1f), accent);
            Corner(root, "BL", new Vector2(0f, 0f), accent);
            Corner(root, "BR", new Vector2(1f, 0f), accent);
            return root;
        }

        private static void Edge(RectTransform root, string name, Color c, Vector2 min, Vector2 max, float insetX, float insetY, float width)
        {
            var img = UiKit.Image(root, name, c);
            var rt = img.rectTransform;
            rt.anchorMin = min; rt.anchorMax = max; rt.pivot = new Vector2(0.5f, 0.5f);
            bool horizontal = min.y == max.y;
            rt.offsetMin = horizontal ? new Vector2(insetX, -width / 2f) : new Vector2(-width / 2f, insetY);
            rt.offsetMax = horizontal ? new Vector2(-insetX, width / 2f) : new Vector2(width / 2f, -insetY);
            // keep the line inside the card
            rt.anchoredPosition += horizontal ? new Vector2(0f, min.y > 0.5f ? -width / 2f : width / 2f) : new Vector2(min.x > 0.5f ? -width / 2f : width / 2f, 0f);
        }

        private static void Corner(RectTransform root, string name, Vector2 anchor, Color c)
        {
            float inset = 10f;
            float sx = anchor.x < 0.5f ? inset : -inset, sy = anchor.y < 0.5f ? inset : -inset;
            var h = UiKit.Image(root, "Corner" + name + "_H", UiKit.Alpha(c, 0.55f));
            UiKit.Pin(h.rectTransform, anchor, sx, sy, CornerSize, CornerThickness);
            var v = UiKit.Image(root, "Corner" + name + "_V", UiKit.Alpha(c, 0.55f));
            UiKit.Pin(v.rectTransform, anchor, sx, sy, CornerThickness, CornerSize);
        }

        /// <summary>Applies the shared title style to a TMP label.</summary>
        public static void StyleTitle(TMP_Text t)
        {
            if (t == null) return;
            t.fontSize = Title; t.fontStyle = FontStyles.Bold; t.characterSpacing = TitleSpacing;
        }
    }
}
