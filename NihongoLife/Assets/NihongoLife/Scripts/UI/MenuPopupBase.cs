using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NihongoLife.Core;

namespace NihongoLife.UI
{
    /// <summary>
    /// Shared modal for main-menu popups (About, How to play). Provides the dimmed backdrop (click to
    /// close), a card sized to the device (HudFitRect), a localized title, an X button, ESC to close,
    /// pop-in animation and layout helpers that place children by top-left coordinates
    /// (x to the right, y downward). The panel is created hidden first, so a failure while building can
    /// never leave a stuck popup on screen.
    /// </summary>
    public abstract class MenuPopupBase : MonoBehaviour
    {
        private class LocalizedLabel
        {
            public TextMeshProUGUI text;
            public string vi;
            public string en;
            public string ja;
        }

        private static Sprite _circleSprite;

        protected static readonly Color Gold = new Color(0.95f, 0.72f, 0.25f, 1f);
        protected static readonly Color Muted = new Color(0.66f, 0.72f, 0.78f, 1f);
        protected static readonly Color Surface = new Color(0.09f, 0.11f, 0.135f, 1f);
        protected static readonly Color DarkText = new Color(0.08f, 0.06f, 0.02f, 1f);

        private readonly List<LocalizedLabel> _localized = new List<LocalizedLabel>();
        private int _escapeHandledFrame = -1;

        protected GameObject PanelObject { get; private set; }
        protected RectTransform Card { get; private set; }
        protected TMP_FontAsset Font { get; private set; }

        public bool IsOpen => PanelObject != null && PanelObject.activeSelf;

        protected abstract Vector2 CardSize { get; }
        protected abstract void GetTitle(out string vi, out string en, out string ja);
        protected abstract void Build(RectTransform card);

        protected virtual void OnOpened() { }
        protected virtual void OnLanguageApplied() { }
        protected virtual void OnOpenUpdate() { }

        /// <summary>Return true when ESC was consumed by an inner layer (for example a lightbox).</summary>
        protected virtual bool HandleEscape() => false;

        public void Initialize(TMP_FontAsset font)
        {
            if (PanelObject != null) return;

            Font = font;
            PanelObject = new GameObject(GetType().Name + "Panel", typeof(RectTransform), typeof(Image), typeof(Button));
            PanelObject.transform.SetParent(transform, false);
            PanelObject.SetActive(false);

            try
            {
                BuildFrame();
                Build(Card);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public void Show()
        {
            if (PanelObject == null) return;

            ApplyLanguage();
            OnOpened();
            PanelObject.SetActive(true);
            PanelObject.transform.SetAsLastSibling();
            UIStyleKit.PlayShowAnimation(Card != null ? Card.gameObject : PanelObject);
        }

        public void Hide()
        {
            if (PanelObject != null) PanelObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && _escapeHandledFrame != Time.frameCount)
            {
                _escapeHandledFrame = Time.frameCount;
                if (!HandleEscape()) Hide();
                return;
            }

            OnOpenUpdate();
        }

        private void BuildFrame()
        {
            var rect = (RectTransform)PanelObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var backdrop = PanelObject.GetComponent<Image>();
            backdrop.color = new Color(0.01f, 0.014f, 0.02f, 0.86f);
            var backdropButton = PanelObject.GetComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.targetGraphic = backdrop;
            backdropButton.onClick.AddListener(Hide);

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(PanelObject.transform, false);
            Card = (RectTransform)card.transform;
            Card.anchorMin = Card.anchorMax = Card.pivot = new Vector2(0.5f, 0.5f);
            Card.sizeDelta = CardSize;
            UIStyleKit.StylePanel(Card, new Color(0.055f, 0.068f, 0.082f, 1f));
            card.GetComponent<Image>().raycastTarget = true;
            card.AddComponent<HudFitRect>().Configure(0.96f, 0.94f, Vector2.zero, 0.4f);

            GetTitle(out string vi, out string en, out string ja);
            AddLocalizedText(Card, vi, en, ja, 36f, 36f, 30f, CardSize.x - 140f, 56f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);

            var close = AddButton(Card, "X", 0f, 0f, 56f, 46f, true);
            var closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-20f, -26f);
            close.GetComponentInChildren<TextMeshProUGUI>().fontSize = 26f;
            close.onClick.AddListener(Hide);
        }

        // ──────────────────────── Localization ────────────────────────

        protected static string Pick(string vi, string en, string ja)
        {
            GameLanguage language = GameServices.TryGet(out GameSettingsService settings) ? settings.Language : GameLanguage.Vietnamese;
            string value = language == GameLanguage.English ? en : language == GameLanguage.Japanese ? ja : vi;
            if (string.IsNullOrWhiteSpace(value)) value = !string.IsNullOrWhiteSpace(vi) ? vi : !string.IsNullOrWhiteSpace(en) ? en : ja;
            return value ?? string.Empty;
        }

        protected void Localize(TextMeshProUGUI text, string vi, string en, string ja)
        {
            _localized.Add(new LocalizedLabel { text = text, vi = vi, en = en, ja = ja });
            text.text = Pick(vi, en, ja);
        }

        private void ApplyLanguage()
        {
            foreach (var item in _localized)
            {
                if (item.text != null) item.text.text = Pick(item.vi, item.en, item.ja);
            }

            OnLanguageApplied();
        }

        // ──────────────────────── Layout helpers (top-left coordinates) ────────────────────────

        protected static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        protected TextMeshProUGUI AddText(Transform parent, string value, float size, float x, float y, float width, float height, TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal, bool wrap = false, Color? color = null)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, x, y, width, height);
            var text = go.GetComponent<TextMeshProUGUI>();
            if (Font != null) text.font = Font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color ?? new Color(0.92f, 0.95f, 0.97f, 1f);
            text.alignment = alignment;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = wrap ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
            text.richText = true;
            text.raycastTarget = false;
            if (wrap) text.lineSpacing = 6f;
            return text;
        }

        protected TextMeshProUGUI AddLocalizedText(Transform parent, string vi, string en, string ja, float size, float x, float y, float width, float height, TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal, bool wrap = false, Color? color = null)
        {
            var text = AddText(parent, vi, size, x, y, width, height, alignment, style, wrap, color);
            Localize(text, vi, en, ja);
            return text;
        }

        protected Image AddImage(Transform parent, string name, float x, float y, float width, float height, Color color, Sprite sprite = null, bool sliced = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, x, y, width, height);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            }

            return image;
        }

        protected Image AddRounded(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            return AddImage(parent, name, x, y, width, height, color, UIStyleKit.RoundedSprite(), true);
        }

        protected Image AddCircle(Transform parent, string name, float x, float y, float diameter, Color color)
        {
            return AddImage(parent, name, x, y, diameter, diameter, color, CircleSprite(), false);
        }

        protected Button AddButton(Transform parent, string label, float x, float y, float width, float height, bool gold, float fontSize = 18f)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, x, y, width, height);
            var button = go.GetComponent<Button>();
            if (gold) UIStyleKit.StyleButton(button, UIStyleKit.AccentGold, UIStyleKit.AccentGoldHover, UIStyleKit.AccentGoldPressed);
            else UIStyleKit.StyleButton(button, new Color(0.13f, 0.17f, 0.2f, 1f), UIStyleKit.PanelHover, UIStyleKit.PanelPressed);

            var text = AddText(go.transform, label, fontSize, 0f, 0f, width, height, TextAlignmentOptions.Center, FontStyles.Bold, false, gold ? DarkText : Color.white);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 2f);
            textRect.offsetMax = new Vector2(-8f, -2f);
            return button;
        }

        protected static Sprite CircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[size * size];
            float radius = size * 0.5f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - size * 0.5f;
                    float dy = y + 0.5f - size * 0.5f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            _circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _circleSprite;
        }
    }
}
