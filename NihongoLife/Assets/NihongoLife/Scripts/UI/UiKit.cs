using System;
using System.Collections.Generic;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>
    /// Colours of the NihongoLife interface. Dark "night ink" surfaces, warm paper for exam sheets, one gold for the
    /// main action, and a colour per exam family (JLPT vermilion, IELTS blue).
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color Backdrop = new Color(0.02f, 0.03f, 0.05f, 0.82f);
        public static readonly Color Ink = new Color(0.067f, 0.09f, 0.13f, 1f);          // window
        public static readonly Color Surface = new Color(0.10f, 0.13f, 0.18f, 1f);       // card on window
        public static readonly Color Raised = new Color(0.135f, 0.17f, 0.23f, 1f);       // hover / raised card
        public static readonly Color Sunken = new Color(0.045f, 0.06f, 0.09f, 1f);
        public static readonly Color Line = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color Text = new Color(0.95f, 0.96f, 0.98f, 1f);
        public static readonly Color TextSoft = new Color(0.78f, 0.82f, 0.88f, 1f);
        public static readonly Color Muted = new Color(0.58f, 0.64f, 0.72f, 1f);
        public static readonly Color Gold = new Color(0.97f, 0.74f, 0.24f, 1f);
        public static readonly Color GoldDeep = new Color(0.86f, 0.58f, 0.12f, 1f);
        public static readonly Color OnGold = new Color(0.12f, 0.08f, 0.02f, 1f);
        public static readonly Color Teal = new Color(0.16f, 0.72f, 0.6f, 1f);
        public static readonly Color Good = new Color(0.25f, 0.78f, 0.52f, 1f);
        public static readonly Color Warn = new Color(0.98f, 0.66f, 0.22f, 1f);
        public static readonly Color Bad = new Color(0.92f, 0.36f, 0.32f, 1f);
        public static readonly Color Jlpt = new Color(0.86f, 0.27f, 0.25f, 1f);
        public static readonly Color JlptSoft = new Color(0.86f, 0.27f, 0.25f, 0.16f);
        public static readonly Color Ielts = new Color(0.2f, 0.47f, 0.9f, 1f);
        public static readonly Color IeltsSoft = new Color(0.2f, 0.47f, 0.9f, 0.16f);
        public static readonly Color Live = new Color(0.93f, 0.26f, 0.42f, 1f);

        // Exam paper
        public static readonly Color Paper = new Color(0.975f, 0.968f, 0.95f, 1f);
        public static readonly Color PaperCard = Color.white;
        public static readonly Color PaperInk = new Color(0.1f, 0.12f, 0.17f, 1f);
        public static readonly Color PaperMuted = new Color(0.38f, 0.42f, 0.5f, 1f);
        public static readonly Color PaperLine = new Color(0.86f, 0.84f, 0.8f, 1f);
        public static readonly Color PaperSelect = new Color(0.86f, 0.92f, 1f, 1f);

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
    }

    /// <summary>Icons from Resources/UI/Icons (Material Symbols, white, tinted by the Image colour) and the coin art.</summary>
    public static class UiIcons
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>"mic", "headphones", … (without the ic_ prefix) or "coin_yen".</summary>
        public static Sprite Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (Cache.TryGetValue(name, out var cached) && cached != null) return cached;
            var sprite = Resources.Load<Sprite>("UI/Icons/" + (name.StartsWith("coin") ? name : "ic_" + name));
            if (sprite == null)
            {
                // Imported as a plain texture (e.g. before the importer ran): wrap it.
                var tex = Resources.Load<Texture2D>("UI/Icons/" + (name.StartsWith("coin") ? name : "ic_" + name));
                if (tex != null) sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            Cache[name] = sprite;
            return sprite;
        }

        public static Sprite Coin => Get("coin_yen");
        public static Sprite CoinLarge => Get("coin_yen_large");
        public static Sprite CoinStack => Get("coin_stack");
    }

    /// <summary>
    /// Builders for the redesigned screens: rounded cards with soft shadows, buttons by importance, chips, rings,
    /// bars, avatars, icons, and labels that follow the interface language (<see cref="LocText"/>).
    /// Positions use top-left coordinates (x right, y down) like the rest of the UI code.
    /// </summary>
    public static class UiKit
    {
        public enum Kind { Primary, Secondary, Ghost, Danger, Accent, Success, Quiet }

        private static readonly Dictionary<int, Sprite> RoundCache = new Dictionary<int, Sprite>();
        private static readonly Dictionary<int, Sprite> ShadowCache = new Dictionary<int, Sprite>();
        private static Sprite _circle, _ring, _ringThin, _vGradient, _hGradient;
        private static TMP_FontAsset _font;

        public static TMP_FontAsset Font
        {
            get
            {
                if (_font != null) return _font;
                // Noto Sans JP (Vietnamese + Japanese) is the project font; TMP Settings lists it as the fallback.
                var fallbacks = TMP_Settings.fallbackFontAssets;
                if (fallbacks != null)
                    foreach (var f in fallbacks)
                        if (f != null && f.name.IndexOf("Noto", StringComparison.OrdinalIgnoreCase) >= 0) { _font = f; break; }
                if (_font == null) _font = NLUi.ResolveFont();
                return _font;
            }
            set => _font = value;
        }

        // ─────────── Sprites ───────────

        /// <summary>Rounded-rect, 9-sliced, corner radius in pixels (8, 12, 16, 22, 999 = pill).</summary>
        public static Sprite Rounded(int radius)
        {
            radius = Mathf.Clamp(radius, 2, 64);
            if (RoundCache.TryGetValue(radius, out var s) && s != null) return s;
            // Asset sprites (Resources/UI/Art) for the common radii: the same art scenes reference when baked.
            var asset = Resources.Load<Sprite>("UI/Art/round_" + radius);
            if (asset != null) return RoundCache[radius] = asset;
            int size = radius * 2 + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "UiRound" + radius };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - radius));
                    float dy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - radius));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius + 1, radius + 1, radius + 1, radius + 1));
            s.name = tex.name;
            RoundCache[radius] = s;
            return s;
        }

        private static readonly Dictionary<int, Sprite> StrokeCache = new Dictionary<int, Sprite>();

        /// <summary>Hollow rounded-rect border (1.5 px line), 9-sliced — a real stroke, unlike Outline which smears
        /// translucent fills into an opaque blob.</summary>
        public static Sprite Stroke(int radius)
        {
            radius = Mathf.Clamp(radius, 2, 64);
            if (StrokeCache.TryGetValue(radius, out var s) && s != null) return s;
            int size = radius * 2 + 4;
            const float line = 1.6f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "UiStroke" + radius };
            var px = new Color32[size * size];
            float half = size * 0.5f - 1f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Signed distance to a rounded rectangle (negative inside).
                    float qx = Mathf.Abs(x + 0.5f - size * 0.5f) - (half - radius);
                    float qy = Mathf.Abs(y + 0.5f - size * 0.5f) - (half - radius);
                    float dist = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    float outer = Mathf.Clamp01(-dist + 0.5f);
                    float inner = Mathf.Clamp01(dist + line + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(outer * inner * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius + 1, radius + 1, radius + 1, radius + 1));
            s.name = tex.name;
            StrokeCache[radius] = s;
            return s;
        }

        /// <summary>Thin border drawn on top of <paramref name="rect"/> (child, stretched, ignores layout).</summary>
        public static Image Border(RectTransform rect, Color color, int radius)
        {
            var border = Image(rect, "Border", color, Stroke(radius));
            Fill(border.rectTransform);
            border.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return border;
        }

        /// <summary>Soft drop shadow (blurred rounded rect), 9-sliced. Place it behind a card, larger by the blur.</summary>
        public static Sprite Shadow(int radius)
        {
            radius = Mathf.Clamp(radius, 4, 48);
            if (ShadowCache.TryGetValue(radius, out var s) && s != null) return s;
            int blur = 24;
            int size = (radius + blur) * 2 + 8;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "UiShadow" + radius };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - half) - (half - radius - blur));
                    float dy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - half) - (half - radius - blur));
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    float a = d <= 0f ? 1f : Mathf.Exp(-(d * d) / (2f * (blur * 0.42f) * (blur * 0.42f)));
                    px[y * size + x] = new Color32(0, 0, 0, (byte)(Mathf.Clamp01(a) * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            int border = radius + blur + 2;
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            s.name = tex.name;
            ShadowCache[radius] = s;
            return s;
        }

        public static Sprite Circle => _circle != null ? _circle : (_circle = Resources.Load<Sprite>("UI/Art/circle") ?? Disc("UiCircle", 0f));
        /// <summary>Ring for radial progress (Image.Type.Filled, Radial360).</summary>
        public static Sprite Ring => _ring != null ? _ring : (_ring = Disc("UiRing", 0.78f));
        public static Sprite RingThin => _ringThin != null ? _ringThin : (_ringThin = Disc("UiRingThin", 0.88f));

        private static Sprite Disc(string name, float inner)
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = name };
            var px = new Color32[size * size];
            float r = size * 0.5f - 1f, ri = r * inner;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - size * 0.5f) * (x + 0.5f - size * 0.5f) + (y + 0.5f - size * 0.5f) * (y + 0.5f - size * 0.5f));
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    if (inner > 0f) a *= Mathf.Clamp01(d - ri + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            s.name = name;
            return s;
        }

        /// <summary>White → transparent vertical gradient (top opaque), tinted by the Image colour.</summary>
        public static Sprite VerticalFade => _vGradient != null ? _vGradient : (_vGradient = Gradient("UiVFade", true));
        public static Sprite HorizontalFade => _hGradient != null ? _hGradient : (_hGradient = Gradient("UiHFade", false));

        private static Sprite Gradient(string name, bool vertical)
        {
            const int n = 128;
            var tex = new Texture2D(vertical ? 4 : n, vertical ? n : 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = name };
            for (int i = 0; i < n; i++)
                for (int j = 0; j < 4; j++)
                {
                    float a = Mathf.SmoothStep(0f, 1f, i / (n - 1f));
                    var c = new Color(1f, 1f, 1f, a);
                    if (vertical) tex.SetPixel(j, i, c); else tex.SetPixel(i, j, new Color(1f, 1f, 1f, 1f - a));
                }
            tex.Apply(false, true);
            var s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            s.name = name;
            return s;
        }

        // ─────────── Rect helpers (top-left coordinates) ───────────

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = parent != null ? parent.gameObject.layer : 5;
            return (RectTransform)go.transform;
        }

        /// <summary>Top-left placement inside the parent.</summary>
        public static RectTransform Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        /// <summary>Top-left placement of a label.</summary>
        public static RectTransform Place(TMP_Text text, float x, float y, float w, float h) => Place(text.rectTransform, x, y, w, h);

        /// <summary>Anchored placement (anchor = pivot), e.g. (1,1) top-right with x negative to the left.</summary>
        public static RectTransform Pin(RectTransform rect, Vector2 anchor, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        /// <summary>Stretch to the parent with insets (left, top, right, bottom).</summary>
        public static RectTransform Fill(RectTransform rect, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>Full-width strip at the top (y down from the top) or bottom of the parent.</summary>
        public static RectTransform Strip(RectTransform rect, bool top, float offset, float height, float left = 0f, float right = 0f)
        {
            rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rect.offsetMin = new Vector2(left, 0f);
            rect.offsetMax = new Vector2(-right, 0f);
            rect.anchoredPosition = new Vector2((left - right) * 0.5f, top ? -offset : offset);
            rect.sizeDelta = new Vector2(-(left + right), height);
            return rect;
        }

        // ─────────── Images ───────────

        public static Image Image(Transform parent, string name, Color color, Sprite sprite = null, bool raycast = false)
        {
            var rect = Rect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            }
            return image;
        }

        /// <summary>Rounded rectangle (radius px) with an optional soft shadow behind it.</summary>
        public static Image Card(Transform parent, string name, Color color, int radius = 16, float shadow = 0f, bool raycast = true)
        {
            var image = Image(parent, name, color, Rounded(radius), raycast);
            if (shadow > 0f) AddShadow(image.rectTransform, radius, shadow);
            return image;
        }

        /// <summary>Soft shadow behind <paramref name="target"/> (a sibling placed just before it, following its rect).</summary>
        public static Image AddShadow(RectTransform target, int radius, float strength = 0.45f, float drop = 8f)
        {
            if (target.parent == null) return null;
            var shadow = Image(target.parent, target.name + "_Shadow", new Color(0f, 0f, 0f, strength), Shadow(radius));
            shadow.transform.SetSiblingIndex(target.GetSiblingIndex());
            var follow = shadow.gameObject.AddComponent<UiFollowRect>();
            follow.Configure(target, 26f, drop);
            target.gameObject.AddComponent<UiShadowLink>().Shadow = shadow.gameObject;
            return shadow;
        }

        public static Image Icon(Transform parent, string icon, Color color, float size = 24f)
        {
            var image = Image(parent, "Icon_" + icon, color, UiIcons.Get(icon));
            image.preserveAspect = true;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            return image;
        }

        // ─────────── Text ───────────

        public static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, bool wrap = false)
        {
            var rect = Rect(parent, name);
            var t = rect.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = align;
            t.richText = true;
            t.raycastTarget = false;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = wrap ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
            if (!wrap)
            {
                // Noto Sans JP lines are ~1.45 em tall: in Ellipsis mode a line taller than its box is dropped entirely.
                // One-line labels shrink a little to fit their box instead of disappearing.
                t.enableAutoSizing = true;
                t.fontSizeMax = size;
                t.fontSizeMin = Mathf.Max(8f, size * 0.7f);
            }
            t.text = text ?? "";
            return t;
        }

        /// <summary>Changes a label's size (also the auto-size ceiling of one-line labels).</summary>
        public static void SetSize(TMP_Text t, float size)
        {
            if (t == null) return;
            t.fontSize = size;
            if (t.enableAutoSizing) { t.fontSizeMax = size; t.fontSizeMin = Mathf.Max(8f, size * 0.7f); }
        }

        /// <summary>A label in the interface language that follows language switches.</summary>
        public static TextMeshProUGUI Label(Transform parent, string name, string vi, string en, string ja, float size, Color color,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, bool wrap = false)
        {
            var t = Label(parent, name, "", size, color, style, align, wrap);
            LocText.Set(t, vi, en, ja);
            return t;
        }

        /// <summary>A label whose text is rebuilt (in the current language) on every language switch.</summary>
        public static TextMeshProUGUI Label(Transform parent, string name, Func<string> build, float size, Color color,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, bool wrap = false)
        {
            var t = Label(parent, name, "", size, color, style, align, wrap);
            LocText.Set(t, build);
            return t;
        }

        // ─────────── Buttons ───────────

        public static (Color bg, Color fg) Colors(Kind kind) => kind switch
        {
            Kind.Primary => (UiTheme.Gold, UiTheme.OnGold),
            Kind.Accent => (UiTheme.Ielts, Color.white),
            Kind.Danger => (UiTheme.Bad, Color.white),
            Kind.Success => (UiTheme.Good, new Color(0.03f, 0.12f, 0.07f, 1f)),
            Kind.Ghost => (new Color(1f, 1f, 1f, 0.06f), UiTheme.Text),
            Kind.Quiet => (new Color(0f, 0f, 0f, 0f), UiTheme.TextSoft),
            _ => (UiTheme.Raised, UiTheme.Text),
        };

        /// <summary>
        /// Button with an optional leading icon and a label in the interface language. The label object is named
        /// "Label" (its text is what tests and players read).
        /// </summary>
        public static Button Button(Transform parent, string name, string vi, string en, string ja, Action onClick, Kind kind = Kind.Secondary,
            string icon = null, float fontSize = 17f, int radius = 12)
        {
            var (bg, fg) = Colors(kind);
            var image = Image(parent, name, bg, Rounded(radius), true);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            UIStyleKit.StyleButton(button, bg, Lighten(bg, kind == Kind.Ghost || kind == Kind.Quiet ? 0.06f : 0.1f), Darken(bg, 0.12f));
            image.sprite = Rounded(radius); // StyleButton sets its own sprite
            image.type = UnityEngine.UI.Image.Type.Sliced;
            if (kind == Kind.Ghost) Border(image.rectTransform, new Color(1f, 1f, 1f, 0.16f), radius);

            var row = Rect(image.transform, "Content");
            Fill(row, 14f, 0f, 14f, 0f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            if (!string.IsNullOrEmpty(icon))
            {
                var ic = Icon(row, icon, fg, fontSize + 4f);
                var le = ic.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = le.preferredHeight = le.minWidth = fontSize + 4f;
            }
            var label = Label(row, "Label", "", fontSize, fg, FontStyles.Bold, TextAlignmentOptions.Center);
            label.overflowMode = TextOverflowModes.Ellipsis;
            if (vi != null || en != null || ja != null) LocText.Set(label, vi, en, ja);
            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>Button with a fixed (non-translated) label, e.g. "JLPT", "N5", numbers.</summary>
        public static Button Button(Transform parent, string name, string fixedLabel, Action onClick, Kind kind = Kind.Secondary, string icon = null, float fontSize = 17f, int radius = 12)
        {
            var b = Button(parent, name, null, null, null, onClick, kind, icon, fontSize, radius);
            SetLabel(b, fixedLabel);
            return b;
        }

        /// <summary>Round icon-only button (close, mic, camera…).</summary>
        public static Button IconButton(Transform parent, string name, string icon, Action onClick, Kind kind = Kind.Secondary, float size = 44f, float iconSize = 22f)
        {
            var (bg, fg) = Colors(kind);
            var image = Image(parent, name, bg, Circle, true);
            image.rectTransform.sizeDelta = new Vector2(size, size);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            UIStyleKit.StyleButton(button, bg, Lighten(bg, 0.12f), Darken(bg, 0.15f));
            image.sprite = Circle;
            image.type = UnityEngine.UI.Image.Type.Simple;
            if (kind == Kind.Ghost) { var b = Image(image.transform, "Border", new Color(1f, 1f, 1f, 0.18f), RingThin); Fill(b.rectTransform); }
            var ic = Icon(image.transform, icon, fg, iconSize);
            Pin(ic.rectTransform, new Vector2(0.5f, 0.5f), 0f, 0f, iconSize, iconSize);
            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>The standard "close" button of a window, pinned to its top-right corner.</summary>
        public static Button CloseButton(RectTransform window, Action onClick, float inset = 22f)
        {
            var b = IconButton(window, "CloseX", "close", onClick, Kind.Secondary, 44f, 22f);
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            Pin((RectTransform)b.transform, new Vector2(1f, 1f), -inset, -inset, 44f, 44f);
            return b;
        }

        public static TextMeshProUGUI LabelOf(Button button) => button != null ? button.transform.Find("Content/Label")?.GetComponent<TextMeshProUGUI>() : null;

        public static void SetLabel(Button button, string text)
        {
            var label = LabelOf(button);
            if (label == null) return;
            LocText.Clear(label);
            label.text = text;
        }

        public static void SetLabel(Button button, string vi, string en, string ja)
        {
            var label = LabelOf(button);
            if (label != null) LocText.Set(label, vi, en, ja);
        }

        /// <summary>Restyles a button (selected tab, enabled state…).</summary>
        public static void SetKind(Button button, Kind kind)
        {
            if (button == null) return;
            var (bg, fg) = Colors(kind);
            var swap = button.GetComponent<ButtonColorSwap>();
            if (swap != null) swap.Configure(button.targetGraphic as Image, bg, Lighten(bg, 0.1f), Darken(bg, 0.12f));
            else if (button.targetGraphic is Image img) img.color = bg;
            foreach (var t in button.GetComponentsInChildren<TextMeshProUGUI>(true)) t.color = fg;
            foreach (var i in button.GetComponentsInChildren<Image>(true)) if (i.name.StartsWith("Icon_")) i.color = fg;
        }

        public static Outline Outline(RectTransform rect, Color color, float distance = 1.2f)
        {
            var o = rect.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(distance, -distance);
            o.useGraphicAlpha = false;
            return o;
        }

        // ─────────── Small components ───────────

        /// <summary>Pill-shaped tag: optional icon + text (fixed or translated).</summary>
        public static RectTransform Chip(Transform parent, string name, string text, Color bg, Color fg, string icon = null, float size = 13f)
        {
            var image = Image(parent, name, bg, Rounded(14));
            var rect = image.rectTransform;
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 4, 5);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (!string.IsNullOrEmpty(icon))
            {
                var ic = Icon(rect, icon, fg, size + 3f);
                var le = ic.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = le.preferredHeight = size + 3f;
            }
            var label = Label(rect, "Text", text, size, fg, FontStyles.Bold, TextAlignmentOptions.Center);
            label.overflowMode = TextOverflowModes.Overflow;
            return rect;
        }

        public static RectTransform Chip(Transform parent, string name, string vi, string en, string ja, Color bg, Color fg, string icon = null, float size = 13f)
        {
            var rect = Chip(parent, name, "", bg, fg, icon, size);
            LocText.Set(rect.Find("Text").GetComponent<TextMeshProUGUI>(), vi, en, ja);
            return rect;
        }

        /// <summary>Radial progress ring (track + fill) — returns the fill image (fillAmount 0..1).</summary>
        public static Image ProgressRing(Transform parent, string name, float size, Color fill, Color track, bool thin = false)
        {
            var root = Rect(parent, name);
            root.sizeDelta = new Vector2(size, size);
            var back = Image(root, "Track", track, thin ? RingThin : Ring);
            Fill(back.rectTransform);
            var front = Image(root, "Fill", fill, thin ? RingThin : Ring);
            Fill(front.rectTransform);
            front.type = UnityEngine.UI.Image.Type.Filled;
            front.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            front.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
            front.fillClockwise = true;
            front.fillAmount = 0f;
            return front;
        }

        /// <summary>Horizontal progress bar — returns the fill rect (set anchorMax.x = progress).</summary>
        public static RectTransform ProgressBar(Transform parent, string name, Color fill, Color track, int radius = 6)
        {
            var back = Image(parent, name, track, Rounded(radius));
            var front = Image(back.transform, "Fill", fill, Rounded(radius));
            var r = front.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = new Vector2(0f, 1f); r.pivot = new Vector2(0f, 0.5f);
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        public static void SetProgress(RectTransform fill, float value)
        {
            if (fill == null) return;
            fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            fill.gameObject.SetActive(value > 0.001f);
        }

        /// <summary>Round avatar with initials.</summary>
        public static Image Avatar(Transform parent, string name, string displayName, float size, Color bg, Color fg)
        {
            var disc = Image(parent, name, bg, Circle);
            disc.rectTransform.sizeDelta = new Vector2(size, size);
            var t = Label(disc.transform, "Initials", Initials(displayName), size * 0.38f, fg, FontStyles.Bold, TextAlignmentOptions.Center);
            Fill(t.rectTransform);
            return disc;
        }

        public static string Initials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var words = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1) return words[0].Substring(0, Math.Min(2, words[0].Length)).ToUpperInvariant();
            return (char.ToUpperInvariant(words[0][0]).ToString() + char.ToUpperInvariant(words[words.Length - 1][0])).ToString();
        }

        /// <summary>Coin icon + amount, e.g. "¥1,200" (the coin art replaces the ¥ sign).</summary>
        public static RectTransform Money(Transform parent, string name, int amount, float size, Color color, bool showSign = false)
        {
            var row = Rect(parent, name);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = size * 0.25f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fitter = row.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var coin = Image(row, "Coin", Color.white, UiIcons.Coin);
            coin.preserveAspect = true;
            var le = coin.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = size * 1.15f;
            string sign = showSign && amount > 0 ? "+" : amount < 0 ? "−" : "";
            Label(row, "Amount", sign + Mathf.Abs(amount).ToString("N0", System.Globalization.CultureInfo.InvariantCulture), size, color, FontStyles.Bold);
            return row;
        }

        public static void SetMoney(RectTransform money, int amount, bool showSign = false)
        {
            var t = money != null ? money.Find("Amount")?.GetComponent<TextMeshProUGUI>() : null;
            if (t == null) return;
            string sign = showSign && amount > 0 ? "+" : amount < 0 ? "−" : "";
            t.text = sign + Mathf.Abs(amount).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>Inline TMP sprite-free coin for rich text lines: "¥" in gold bold.</summary>
        public static string YenInline(int amount) => $"<color={UiTheme.Hex(UiTheme.Gold)}><b>¥{amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)}</b></color>";

        // ─────────── Layout groups ───────────

        public static VerticalLayoutGroup Column(RectTransform rect, float spacing, RectOffset padding = null, TextAnchor align = TextAnchor.UpperLeft)
        {
            var g = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.padding = padding ?? new RectOffset();
            g.childAlignment = align;
            g.childControlWidth = g.childControlHeight = true;
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = false;
            return g;
        }

        public static HorizontalLayoutGroup RowLayout(RectTransform rect, float spacing, RectOffset padding = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var g = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.padding = padding ?? new RectOffset();
            g.childAlignment = align;
            g.childControlWidth = g.childControlHeight = true;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = false;
            return g;
        }

        public static LayoutElement Size(Component c, float w = -1f, float h = -1f, float flexW = -1f, float flexH = -1f)
        {
            var le = c.GetComponent<LayoutElement>().OrNull() ?? c.gameObject.AddComponent<LayoutElement>();
            if (w >= 0f) { le.preferredWidth = w; le.minWidth = w; }
            if (h >= 0f) { le.preferredHeight = h; le.minHeight = h; }
            if (flexW >= 0f) le.flexibleWidth = flexW;
            if (flexH >= 0f) le.flexibleHeight = flexH;
            return le;
        }

        /// <summary>Vertical scroll view; returns the content (top-anchored, grows with a VerticalLayoutGroup).</summary>
        public static RectTransform Scroll(Transform parent, string name, out ScrollRect scroll, float spacing = 12f, RectOffset padding = null)
        {
            var root = Rect(parent, name);
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0f);
            scroll = root.gameObject.AddComponent<ScrollRect>();
            var viewport = Rect(root, "Viewport");
            Fill(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var vpImage = viewport.gameObject.AddComponent<Image>();
            vpImage.color = new Color(0f, 0f, 0f, 0f);
            var content = Rect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;
            Column(content, spacing, padding ?? new RectOffset(2, 2, 2, 2));
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 36f;
            return content;
        }

        public static void Clear(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }

        // ─────────── Colour maths ───────────

        public static Color Lighten(Color c, float amount) => new Color(Mathf.Lerp(c.r, 1f, amount), Mathf.Lerp(c.g, 1f, amount), Mathf.Lerp(c.b, 1f, amount), Mathf.Max(c.a, c.a + amount * 0.5f));
        public static Color Darken(Color c, float amount) => new Color(c.r * (1f - amount), c.g * (1f - amount), c.b * (1f - amount), c.a);
        public static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }

    /// <summary>
    /// Hover / press feedback that keeps an element's saved resting colour (baked scenes look the same in the Editor
    /// and in Play): lighter on hover, darker on press, a slight scale. <see cref="SetBase"/> changes the resting colour.
    /// </summary>
    public sealed class UiHover : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler,
        UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
    {
        private Graphic _graphic;
        private Color _base;
        private bool _hasBase, _over;
        private Vector3 _scale = Vector3.one;
        private static float _lastHoverSound;

        private void Awake() { _graphic = GetComponent<Button>()?.targetGraphic ?? GetComponent<Graphic>(); Capture(); _scale = transform.localScale; }
        private void Capture() { if (_graphic != null && !_hasBase) { _base = _graphic.color; _hasBase = true; } }

        public void SetBase(Color color)
        {
            if (_graphic == null) _graphic = GetComponent<Button>()?.targetGraphic ?? GetComponent<Graphic>();
            _base = color; _hasBase = true;
            if (_graphic != null) _graphic.color = _over ? Hover(color) : color;
        }

        private static Color Hover(Color c) => c.a < 0.2f ? new Color(1f, 1f, 1f, c.a + 0.1f) : UiKit.Lighten(c, 0.12f);
        private bool Interactable => GetComponent<Button>() == null || GetComponent<Button>().interactable;

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e)
        {
            _over = true;
            if (_graphic == null || !Interactable) return;
            _graphic.color = Hover(_base);
            if (!NihongoLife.Core.MotionSettings.ReduceMotion) transform.localScale = _scale * 1.035f;
            if (Time.unscaledTime - _lastHoverSound < 0.07f) return;
            _lastHoverSound = Time.unscaledTime;
            if (NihongoLife.Core.GameServices.TryGet(out NihongoLife.Audio.IAudioService audio)) audio.PlayCue(NihongoLife.Audio.GameAudioCue.UiHover, 0.35f);
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e)
        {
            _over = false;
            if (_graphic != null) _graphic.color = _base;
            transform.localScale = _scale;
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e)
        {
            if (_graphic == null || !Interactable) return;
            _graphic.color = UiKit.Darken(_base.a < 0.2f ? new Color(1f, 1f, 1f, _base.a + 0.06f) : _base, 0.12f);
            if (!NihongoLife.Core.MotionSettings.ReduceMotion)
                transform.localScale = new Vector3(_scale.x * 1.06f, _scale.y * 0.9f, _scale.z);
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e)
        {
            if (_graphic != null) _graphic.color = _over ? Hover(_base) : _base;
            transform.localScale = _over && !NihongoLife.Core.MotionSettings.ReduceMotion ? _scale * 1.02f : _scale;
        }

        private void OnDisable() { _over = false; if (_graphic != null && _hasBase) _graphic.color = _base; transform.localScale = _scale; }
    }

    /// <summary>Marks text written by people (names, bios, comments, chat): shown as is, in whatever language it was
    /// written — not interface text, so the one-language checks skip it.</summary>
    public sealed class UiUserContent : MonoBehaviour
    {
        public static T Mark<T>(T component) where T : Component
        {
            if (component != null && component.GetComponent<UiUserContent>() == null) component.gameObject.AddComponent<UiUserContent>();
            return component;
        }
    }

    /// <summary>Shows / hides / destroys a card's shadow together with the card.</summary>
    public sealed class UiShadowLink : MonoBehaviour
    {
        public GameObject Shadow;
        private void OnEnable() { if (Shadow != null) Shadow.SetActive(true); }
        private void OnDisable() { if (Shadow != null) Shadow.SetActive(false); }
        private void OnDestroy() { if (Shadow != null) Destroy(Shadow); }
    }

    /// <summary>Keeps a shadow image aligned with its card (same rect grown by the blur, dropped a little).</summary>
    public sealed class UiFollowRect : MonoBehaviour
    {
        private RectTransform _target, _self;
        private float _grow, _drop;

        public void Configure(RectTransform target, float grow, float drop)
        {
            _target = target; _grow = grow; _drop = drop;
            _self = (RectTransform)transform;
            Sync();
        }

        private void LateUpdate() => Sync();

        private void Sync()
        {
            if (_self == null) return;
            if (_target == null) { Destroy(gameObject); return; }
            _self.anchorMin = _target.anchorMin;
            _self.anchorMax = _target.anchorMax;
            _self.pivot = _target.pivot;
            _self.anchoredPosition = _target.anchoredPosition + new Vector2(0f, -_drop);
            _self.sizeDelta = _target.sizeDelta + new Vector2(_grow * 2f, _grow * 2f);
            _self.localScale = _target.localScale;
        }
    }
}
