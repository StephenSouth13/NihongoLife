using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NihongoLife.Audio;
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

        protected static readonly Color Gold = UiTheme.Gold;
        protected static readonly Color Muted = UiTheme.Muted;
        protected static readonly Color Surface = UiTheme.Surface;
        protected static readonly Color DarkText = UiTheme.OnGold;

        private readonly List<LocalizedLabel> _localized = new List<LocalizedLabel>();

        protected GameObject PanelObject { get; private set; }
        protected RectTransform Card { get; private set; }
        protected TMP_FontAsset Font { get; private set; }

        public bool IsOpen => PanelObject != null && PanelObject.activeSelf;

        protected virtual bool LocksGameplay => false;
        private NihongoLife.Player.PlayerController _modalPlayer;
        private NihongoLife.Cameras.ThirdPersonCameraController _modalCamera;
        private bool _playerWasLocked, _cameraWasLocked, _cursorWasVisible;
        private CursorLockMode _previousCursorLock;

        private void LateUpdate()
        {
            if (!LocksGameplay || !IsOpen) return;
            if (_modalPlayer != null) _modalPlayer.InputLocked = true;
            if (_modalCamera != null) _modalCamera.IsLocked = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void RestoreGameplay()
        {
            if (!LocksGameplay) return;
            if (_modalPlayer != null) _modalPlayer.InputLocked = _playerWasLocked;
            if (_modalCamera != null) _modalCamera.IsLocked = _cameraWasLocked;
            Cursor.lockState = _previousCursorLock;
            Cursor.visible = _cursorWasVisible;
        }

        protected abstract Vector2 CardSize { get; }
        protected abstract void GetTitle(out string vi, out string en, out string ja);
        protected abstract void Build(RectTransform card);

        protected virtual void OnOpened() { }
        protected virtual void OnClosed() { }
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

            // Live language switching: re-localize immediately, even while this popup is open.
            Loc.Watch(this, ApplyLanguage);
        }

        /// <summary>Called when the popup is destroyed (e.g. its scene unloads), open or not.</summary>
        protected virtual void OnDestroyed() { }

        private void OnDestroy()
        {
            OnDestroyed();
            if (IsOpen) RestoreGameplay();
        }

        public void Show()
        {
            if (PanelObject == null || IsOpen) return;
            UiModalStack.Register(this, () => IsOpen, () => { if (!HandleEscape()) Hide(); }, GetType().Name, immersive: LocksGameplay);
            if (LocksGameplay)
            {
                _modalPlayer = SceneRefs.Find<NihongoLife.Player.PlayerController>();
                _modalCamera = SceneRefs.Find<NihongoLife.Cameras.ThirdPersonCameraController>();
                _playerWasLocked = _modalPlayer != null && _modalPlayer.InputLocked;
                _cameraWasLocked = _modalCamera != null && _modalCamera.IsLocked;
                _previousCursorLock = Cursor.lockState;
                _cursorWasVisible = Cursor.visible;
            }

            ApplyLanguage();
            OnOpened();
            PanelObject.SetActive(true);
            PanelObject.transform.SetAsLastSibling();
            LateUpdate();
            PlayCue(GameAudioCue.UiOpen, 0.8f);
            UIStyleKit.PlayShowAnimation(Card != null ? Card.gameObject : PanelObject);
        }

        public void Hide()
        {
            if (PanelObject == null || !PanelObject.activeSelf) return;
            PanelObject.SetActive(false);
            RestoreGameplay();
            PlayCue(GameAudioCue.UiClose, 0.8f);
            OnClosed();
        }

        protected static void PlayCue(GameAudioCue cue, float volume)
        {
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(cue, volume);
        }

        protected virtual void Update()
        {
            if (!IsOpen) return;

            OnOpenUpdate();
        }

        private void BuildFrame()
        {
            var rect = (RectTransform)PanelObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var backdrop = PanelObject.GetComponent<Image>();
            backdrop.color = UiTheme.Backdrop;
            var backdropButton = PanelObject.GetComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.targetGraphic = backdrop;
            backdropButton.onClick.AddListener(Hide);

            var cardImage = UiKit.Card(PanelObject.transform, "Card", UiTheme.Ink, 22, shadow: 0.55f);
            var card = cardImage.gameObject;
            Card = (RectTransform)card.transform;
            Card.anchorMin = Card.anchorMax = Card.pivot = new Vector2(0.5f, 0.5f);
            Card.sizeDelta = CardSize;
            cardImage.raycastTarget = true;
            card.AddComponent<HudFitRect>().Configure(0.96f, 0.94f, Vector2.zero, 0.4f);
            // Hairline under the header and a warm glow along the top edge.
            var glow = UiKit.Image(Card, "TopGlow", UiKit.Alpha(UiTheme.Gold, 0.055f), UiKit.VerticalFade);
            UiKit.Strip(glow.rectTransform, true, 0f, 96f, 22f, 22f);
            var hairline = UiKit.Image(Card, "HeaderLine", UiTheme.Line);
            UiKit.Strip(hairline.rectTransform, true, 92f, 1f, 32f, 32f);

            GetTitle(out string vi, out string en, out string ja);
            var title = AddLocalizedText(Card, vi, en, ja, 30f, 36f, 22f, CardSize.x - 150f, 56f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            title.characterSpacing = 1f;

            UiKit.CloseButton(Card, Hide, 26f);
        }

        // ──────────────────────── Localization ────────────────────────

        protected static string Pick(string vi, string en, string ja) => Loc.Pick(vi, en, ja);

        protected void Localize(TextMeshProUGUI text, string vi, string en, string ja)
        {
            _localized.Add(new LocalizedLabel { text = text, vi = vi, en = en, ja = ja });
            LocText.Set(text, vi, en, ja);
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
            if (gold) UIStyleKit.StyleButton(button, UiTheme.Gold, UiKit.Lighten(UiTheme.Gold, 0.12f), UiTheme.GoldDeep);
            else UIStyleKit.StyleButton(button, UiTheme.Raised, UiKit.Lighten(UiTheme.Raised, 0.08f), UiTheme.Sunken);
            var buttonImage = go.GetComponent<Image>();
            buttonImage.sprite = UiKit.Rounded(12);
            buttonImage.type = Image.Type.Sliced;

            var text = AddText(go.transform, label, fontSize, 0f, 0f, width, height, TextAlignmentOptions.Center, FontStyles.Bold, false, gold ? UiTheme.OnGold : UiTheme.Text);
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
