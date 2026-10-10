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
    /// Settings modal opened from the main menu: audio volumes and key bindings.
    /// Robustness rules: the panel is created hidden first, so a failure while building or refreshing
    /// can never leave it stuck on screen; it always has an X button, a Close button, ESC and
    /// click-outside to close; it is brought to the front every time it opens.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        private class LocalizedLabel
        {
            public TextMeshProUGUI text;
            public string vi;
            public string en;
            public string ja;
        }

        private GameObject panelObj;
        private RectTransform cardRect;
        private Slider bgmSlider;
        private Slider sfxSlider;
        private TextMeshProUGUI bgmValue;
        private TextMeshProUGUI sfxValue;
        private TMP_FontAsset uiFont;
        private GameInputService input;
        private bool rebinding;
        private int escapeBlockedFrame = -1;
        private Slider sensitivitySlider;
        private TextMeshProUGUI sensitivityValue;
        private Button invertButton, schemeButton;
        private TextMeshProUGUI schemeHint;
        private readonly Dictionary<GameInputId, TextMeshProUGUI> bindingLabels = new Dictionary<GameInputId, TextMeshProUGUI>();
        private readonly List<LocalizedLabel> localized = new List<LocalizedLabel>();

        public bool IsOpen => panelObj != null && panelObj.activeSelf;

        public void Initialize(TMP_FontAsset font)
        {
            UiModalStack.Register(this, () => IsOpen && !rebinding, Hide, "Settings");
            if (panelObj != null) return;

            uiFont = font;

            // Hidden first: whatever happens below, the panel can never stay stuck open.
            panelObj = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image), typeof(Button));
            panelObj.transform.SetParent(transform, false);
            panelObj.SetActive(false);

            try
            {
                input = GameInputService.GetOrCreate();
                BuildPanel();
                input.OnBindingsChanged += RefreshBindingLabels;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            // Live language switching, even while Settings is open.
            if (GameServices.TryGet(out GameSettingsService settings)) settings.OnLanguageChanged += HandleGlobalLanguageChanged;
        }

        private void HandleGlobalLanguageChanged(GameLanguage _) => ApplyLanguage();

        public void Show()
        {
            if (panelObj == null) return;

            ApplyLanguage();
            RefreshValues();
            RefreshBindingLabels();
            panelObj.SetActive(true);
            panelObj.transform.SetAsLastSibling();
            PlayCue(GameAudioCue.UiOpen, 0.8f);
            UIStyleKit.PlayShowAnimation(cardRect != null ? cardRect.gameObject : panelObj);
        }

        public void Hide()
        {
            if (panelObj == null || !panelObj.activeSelf) return;
            panelObj.SetActive(false);
            PlayCue(GameAudioCue.UiClose, 0.8f);
        }

        private static void PlayCue(GameAudioCue cue, float volume)
        {
            if (GameServices.TryGet(out IAudioService audio)) audio.PlayCue(cue, volume);
        }

        public void ToggleFromEscape()
        {
            escapeBlockedFrame = Time.frameCount;
            if (IsOpen) Hide();
            else Show();
        }

        private void Update()
        {
            // Esc is routed by UiModalStack; Settings opens with O (GameInputId.Settings).
        }

        // ──────────────────────── Build ────────────────────────

        private void BuildPanel()
        {
            var rect = (RectTransform)panelObj.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            // Backdrop: dims the menu and closes the modal when clicked outside the card.
            var backdrop = panelObj.GetComponent<Image>();
            backdrop.color = new Color(0.01f, 0.014f, 0.02f, 0.86f);
            var backdropButton = panelObj.GetComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.targetGraphic = backdrop;
            backdropButton.onClick.AddListener(Hide);

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(panelObj.transform, false);
            cardRect = (RectTransform)card.transform;
            cardRect.anchorMin = cardRect.anchorMax = cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(1180f, 740f);
            UIStyleKit.StylePanel(cardRect, new Color(0.055f, 0.068f, 0.082f, 1f));
            card.GetComponent<Image>().raycastTarget = true;
            card.AddComponent<HudFitRect>().Configure(0.96f, 0.94f, Vector2.zero, 0.4f);

            Label(card.transform, "Cài đặt", "Settings", "設定", 36, new Vector2(0f, 325f), new Vector2(700f, 52f), TextAlignmentOptions.Center, true);
            // Left column: audio, language, camera.
            Label(card.transform, "Âm thanh", "Audio", "音", 22, new Vector2(-360f, 262f), new Vector2(360f, 40f), TextAlignmentOptions.Center, true);
            Label(card.transform, "Nhạc nền", "Music", "BGM", 16, new Vector2(-360f, 222f), new Vector2(300f, 32f), TextAlignmentOptions.Center, false);
            bgmSlider = CreateSlider(card.transform, new Vector2(-380f, 190f), out bgmValue);
            Label(card.transform, "Hiệu ứng", "Sound effects", "効果音", 16, new Vector2(-360f, 152f), new Vector2(300f, 32f), TextAlignmentOptions.Center, false);
            sfxSlider = CreateSlider(card.transform, new Vector2(-380f, 120f), out sfxValue);
            Label(card.transform, "Ngôn ngữ", "Language", "言語", 18, new Vector2(-360f, 74f), new Vector2(300f, 32f), TextAlignmentOptions.Center, true);
            CreateLanguageButton(card.transform, "VI", GameLanguage.Vietnamese, new Vector2(-465f, 34f));
            CreateLanguageButton(card.transform, "EN", GameLanguage.English, new Vector2(-360f, 34f));
            CreateLanguageButton(card.transform, "JP", GameLanguage.Japanese, new Vector2(-255f, 34f));
            Label(card.transform, "Camera & chuột", "Camera & mouse", "カメラ・マウス", 22, new Vector2(-360f, -22f), new Vector2(360f, 40f), TextAlignmentOptions.Center, true);
            Label(card.transform, "Độ nhạy chuột", "Mouse sensitivity", "マウス感度", 16, new Vector2(-360f, -62f), new Vector2(300f, 32f), TextAlignmentOptions.Center, false);
            sensitivitySlider = CreateSlider(card.transform, new Vector2(-380f, -94f), out sensitivityValue);
            sensitivitySlider.minValue = ControlSettings.MinSensitivity;
            sensitivitySlider.maxValue = ControlSettings.MaxSensitivity;
            sensitivitySlider.onValueChanged.AddListener(v => { ControlSettings.MouseSensitivity = v; sensitivityValue.text = $"{v:0.0}x"; });
            invertButton = CreateButton(card.transform, string.Empty, new Vector2(-465f, -146f), new Vector2(210f, 40f), false);
            invertButton.name = "InvertY";
            invertButton.onClick.AddListener(() => { ControlSettings.InvertY = !ControlSettings.InvertY; RefreshControlButtons(); });
            schemeButton = CreateButton(card.transform, string.Empty, new Vector2(-245f, -146f), new Vector2(210f, 40f), false);
            schemeButton.name = "ControlScheme";
            schemeButton.onClick.AddListener(() =>
            {
                ControlSettings.Scheme = ControlSettings.Scheme == ControlScheme.MouseLook ? ControlScheme.ClickToMove : ControlScheme.MouseLook;
                RefreshControlButtons();
            });
            schemeHint = CreateText(card.transform, string.Empty, 14, new Vector2(-355f, -210f), new Vector2(440f, 64f), TextAlignmentOptions.Center);
            schemeHint.textWrappingMode = TextWrappingModes.Normal;
            schemeHint.enableAutoSizing = false;
            schemeHint.color = new Color(0.66f, 0.72f, 0.8f, 1f);
            // Right column: every key, rebindable.
            Label(card.transform, "Phím điều khiển", "Key bindings", "キー操作", 22, new Vector2(240f, 262f), new Vector2(600f, 40f), TextAlignmentOptions.Center, true);
            Label(card.transform, "Bấm vào ô phím để đổi. Giữ Ctrl để hiện con trỏ khi đang chơi.", "Click a key to rebind. Hold Ctrl for a cursor during play.", "キーをクリックして変更。Ctrlでカーソル表示。", 14, new Vector2(240f, 230f), new Vector2(600f, 28f), TextAlignmentOptions.Center, false);

            bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);

            // Footer and close controls first, so they exist even if binding rows fail below.
            var reset = CreateButton(card.transform, string.Empty, new Vector2(-130f, -318f), new Vector2(220f, 48f), false);
            Localize(reset.GetComponentInChildren<TextMeshProUGUI>(), "Mặc định", "Reset keys", "初期化");
            reset.onClick.AddListener(() => input.ResetBindings());
            var close = CreateButton(card.transform, string.Empty, new Vector2(130f, -318f), new Vector2(220f, 48f), true);
            Localize(close.GetComponentInChildren<TextMeshProUGUI>(), "Đóng", "Close", "閉じる");
            close.onClick.AddListener(Hide);

            var x = CreateButton(card.transform, "X", Vector2.zero, new Vector2(56f, 46f), true);
            var xRect = (RectTransform)x.transform;
            xRect.anchorMin = xRect.anchorMax = xRect.pivot = new Vector2(1f, 1f);
            xRect.anchoredPosition = new Vector2(-18f, -18f);
            x.GetComponentInChildren<TextMeshProUGUI>().fontSize = 26f;
            x.onClick.AddListener(Hide);

            CreateBindingRows(card.transform);
            RefreshValues();
        }

        private void CreateLanguageButton(Transform parent, string label, GameLanguage language, Vector2 position)
        {
            Button button = CreateButton(parent, label, position, new Vector2(88f, 38f), false);
            button.onClick.AddListener(() =>
            {
                if (GameServices.TryGet(out GameSettingsService settings)) settings.SetLanguage(language);
                ApplyLanguage();
            });
        }

        private void CreateBindingRows(Transform parent)
        {
            var rows = new (GameInputId id, string vi, string en, string ja)[]
            {
                (GameInputId.MoveUp, "Đi tới", "Forward", "前進"), (GameInputId.MoveDown, "Đi lùi", "Backward", "後退"),
                (GameInputId.MoveLeft, "Sang trái", "Left", "左"), (GameInputId.MoveRight, "Sang phải", "Right", "右"),
                (GameInputId.Sprint, "Chạy", "Sprint", "走る"), (GameInputId.Jump, "Nhảy", "Jump", "ジャンプ"),
                (GameInputId.Interact, "Tương tác", "Interact", "調べる"), (GameInputId.FreeCursor, "Hiện con trỏ (giữ)", "Cursor (hold)", "カーソル"),
                (GameInputId.UseItem, "Dùng vật phẩm", "Use item", "アイテム使用"), (GameInputId.DropItem, "Vứt đồ", "Drop item", "捨てる"),
                (GameInputId.Emote, "Biểu cảm", "Emotes", "エモート"), (GameInputId.Voice, "Ghi âm", "Record", "録音"),
                (GameInputId.Inventory, "Balo", "Bag", "バッグ"), (GameInputId.Character, "Nhân vật", "Profile", "プロフィール"),
                (GameInputId.Journal, "Sổ nhiệm vụ", "Task journal", "タスク"), (GameInputId.Map, "Bản đồ", "Map", "地図"),
                (GameInputId.Shop, "Cửa hàng (đảo)", "Shop (island)", "ショップ"), (GameInputId.Chat, "Chat", "Chat", "チャット"),
                (GameInputId.Settings, "Cài đặt", "Settings", "設定"), (GameInputId.Pause, "Đóng / quay lại", "Close / back", "閉じる・戻る")
            };

            for (int i = 0; i < rows.Length; i++)
            {
                int column = i / 10;
                int row = i % 10;
                float x = 0f + column * 300f;
                float y = 190f - row * 44f;
                Label(parent, rows[i].vi, rows[i].en, rows[i].ja, 15, new Vector2(x, y), new Vector2(150f, 38f), TextAlignmentOptions.MidlineLeft, false);
                var button = CreateButton(parent, string.Empty, new Vector2(x + 145f, y), new Vector2(118f, 36f), false);
                button.name = "Bind_" + rows[i].id;
                var value = button.GetComponentInChildren<TextMeshProUGUI>();
                value.fontSize = 14f;
                value.enableAutoSizing = true;
                value.fontSizeMin = 10f;
                value.fontSizeMax = 14f;
                bindingLabels[rows[i].id] = value;
                GameInputId captured = rows[i].id;
                button.onClick.AddListener(() => BeginRebind(captured));
            }

            // Keys that are not rebindable, listed so every control is discoverable in one place.
            var fixedKeys = CreateText(parent, "Chuột: xoay camera  ·  Lăn chuột: zoom  ·  Esc: đóng cửa sổ trên cùng  ·  K: luyện thi", 14, new Vector2(240f, -262f), new Vector2(620f, 28f), TextAlignmentOptions.Center);
            fixedKeys.color = new Color(0.66f, 0.72f, 0.8f, 1f);

            RefreshBindingLabels();
        }

        // ──────────────────────── Behaviour ────────────────────────

        private void BeginRebind(GameInputId id)
        {
            if (input == null || !bindingLabels.TryGetValue(id, out var label)) return;

            rebinding = true;
            label.text = Pick("Nhấn phím...", "Press a key...", "キーを押して");
            try
            {
                input.StartRebind(id, _ =>
                {
                    rebinding = false;
                    escapeBlockedFrame = Time.frameCount; // ESC that cancels a rebind must not also close the modal
                    RefreshBindingLabels();
                });
            }
            catch (Exception exception)
            {
                rebinding = false;
                Debug.LogException(exception);
                RefreshBindingLabels();
            }
        }

        private void RefreshBindingLabels()
        {
            if (input == null) return;

            foreach (var pair in bindingLabels)
            {
                string label;
                try
                {
                    label = input.GetBindingLabel(pair.Key);
                }
                catch (Exception)
                {
                    label = "-";
                }

                if (pair.Value != null) pair.Value.text = string.IsNullOrWhiteSpace(label) ? "-" : label;
            }
        }

        private void RefreshValues()
        {
            if (bgmSlider == null || sfxSlider == null) return;

            if (sensitivitySlider != null)
            {
                sensitivitySlider.SetValueWithoutNotify(ControlSettings.MouseSensitivity);
                sensitivityValue.text = $"{ControlSettings.MouseSensitivity:0.0}x";
            }
            RefreshControlButtons();
            bgmSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("Volume_BGM", 0.5f));
            sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("Volume_SFX", 0.8f));
            UpdateValueText(bgmValue, bgmSlider.value);
            UpdateValueText(sfxValue, sfxSlider.value);
        }

        private void RefreshControlButtons()
        {
            if (invertButton != null)
                invertButton.GetComponentInChildren<TextMeshProUGUI>().text = ControlSettings.InvertY ? Pick("Đảo trục Y: Bật", "Invert Y: On", "上下反転: オン") : Pick("Đảo trục Y: Tắt", "Invert Y: Off", "上下反転: オフ");
            if (schemeButton != null)
                schemeButton.GetComponentInChildren<TextMeshProUGUI>().text = ControlSettings.Scheme == ControlScheme.MouseLook ? Pick("Kiểu: Khoá chuột", "Mode: Mouse look", "モード: マウス視点") : Pick("Kiểu: Click để đi", "Mode: Click to move", "モード: クリック移動");
            if (schemeHint != null)
                schemeHint.text = ControlSettings.Scheme == ControlScheme.MouseLook
                    ? Pick("Chuột xoay camera, con trỏ ẩn. Giữ Ctrl để dùng chuột; mở cửa sổ (Tab, N, B…) cũng hiện con trỏ.", "Mouse turns the camera. Hold Ctrl or open a window for the cursor.", "マウスで視点操作。Ctrlでカーソル。")
                    : Pick("Con trỏ luôn hiện: click chuột trái để đi tới, giữ chuột phải/giữa để xoay camera.", "Cursor always visible: left click to walk, right/middle drag to turn.", "左クリックで移動、右ドラッグで視点。");
        }

        private float _lastTickTime;

        private void OnBgmChanged(float value)
        {
            UpdateValueText(bgmValue, value);
            if (GameServices.TryGet(out IAudioService audio)) audio.SetBGMVolume(value);
        }

        private void OnSfxChanged(float value)
        {
            UpdateValueText(sfxValue, value);
            if (!GameServices.TryGet(out IAudioService audio)) return;

            audio.SetSFXVolume(value);
            // Audible preview of the new effects volume while dragging.
            if (Time.unscaledTime - _lastTickTime > 0.12f)
            {
                _lastTickTime = Time.unscaledTime;
                audio.PlayCue(GameAudioCue.UiTick, 1f);
            }
        }

        private static void UpdateValueText(TextMeshProUGUI text, float value)
        {
            if (text != null) text.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }

        private void ApplyLanguage()
        {
            foreach (var item in localized)
            {
                if (item.text != null) item.text.text = Pick(item.vi, item.en, item.ja);
            }
        }

        private static string Pick(string vi, string en, string ja)
        {
            GameLanguage language = GameServices.TryGet(out GameSettingsService settings) ? settings.Language : GameLanguage.Vietnamese;
            string value = language == GameLanguage.English ? en : language == GameLanguage.Japanese ? ja : vi;
            return string.IsNullOrWhiteSpace(value) ? vi : value;
        }

        private void Localize(TextMeshProUGUI text, string vi, string en, string ja)
        {
            localized.Add(new LocalizedLabel { text = text, vi = vi, en = en, ja = ja });
            text.text = vi;
        }

        // ──────────────────────── Widgets ────────────────────────

        private TextMeshProUGUI Label(Transform parent, string vi, string en, string ja, float size, Vector2 position, Vector2 dimensions, TextAlignmentOptions alignment, bool bold)
        {
            var text = CreateText(parent, vi, size, position, dimensions, alignment);
            if (bold) text.fontStyle = FontStyles.Bold;
            Localize(text, vi, en, ja);
            return text;
        }

        private TextMeshProUGUI CreateText(Transform parent, string value, float size, Vector2 position, Vector2 dimensions, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = go.GetComponent<TextMeshProUGUI>();
            if (uiFont != null) text.font = uiFont;
            text.text = value;
            text.fontSize = size;
            text.color = new Color(0.92f, 0.95f, 0.97f, 1f);
            text.alignment = alignment;
            text.enableAutoSizing = true;
            text.fontSizeMin = 11f;
            text.fontSizeMax = size;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private Slider CreateSlider(Transform parent, Vector2 position, out TextMeshProUGUI valueText)
        {
            var go = new GameObject("Slider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position + new Vector2(20f, 0f);
            rect.sizeDelta = new Vector2(300f, 28f);

            Sprite rounded = UIStyleKit.RoundedSprite();

            var background = CreateSliderImage(go.transform, "Background", new Color(0.16f, 0.19f, 0.22f, 1f), rounded);
            var backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(0f, 10f);

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = (RectTransform)fillArea.transform;
            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.sizeDelta = new Vector2(-16f, 10f);
            var fill = CreateSliderImage(fillArea.transform, "Fill", UIStyleKit.AccentGold, rounded);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.sizeDelta = new Vector2(16f, 0f);

            var handleArea = new GameObject("HandleArea", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRect = (RectTransform)handleArea.transform;
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);
            var handle = CreateSliderImage(handleArea.transform, "Handle", Color.white, rounded);
            handle.rectTransform.sizeDelta = new Vector2(22f, 0f); // stretched to the 28 px row by the Slider; a y size here would add to it

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;

            valueText = CreateText(parent, "0%", 17f, position + new Vector2(230f, 0f), new Vector2(70f, 30f), TextAlignmentOptions.MidlineRight);
            valueText.color = UIStyleKit.AccentGold;
            return slider;
        }

        private static Image CreateSliderImage(Transform parent, string name, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }

            return image;
        }

        private Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size, bool gold)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var button = go.GetComponent<Button>();
            if (gold) UIStyleKit.StyleButton(button, UIStyleKit.AccentGold, UIStyleKit.AccentGoldHover, UIStyleKit.AccentGoldPressed);
            else UIStyleKit.StyleButton(button, new Color(0.13f, 0.17f, 0.2f, 1f), UIStyleKit.PanelHover, UIStyleKit.PanelPressed);

            var text = CreateText(go.transform, label, 18f, Vector2.zero, size, TextAlignmentOptions.Center);
            text.fontStyle = FontStyles.Bold;
            text.color = gold ? new Color(0.08f, 0.06f, 0.02f, 1f) : Color.white;
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 2f);
            textRect.offsetMax = new Vector2(-6f, -2f);
            return button;
        }

        private void OnDestroy()
        {
            if (input != null) input.OnBindingsChanged -= RefreshBindingLabels;
            if (GameServices.TryGet(out GameSettingsService settings)) settings.OnLanguageChanged -= HandleGlobalLanguageChanged;
        }
    }
}
