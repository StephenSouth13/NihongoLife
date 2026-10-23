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
        private TextMeshProUGUI webGlGraphicsLabel;
        private TextMeshProUGUI reduceMotionLabel;
        private Button closeButton;
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
            if (closeButton != null && UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
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

            var backdrop = panelObj.GetComponent<Image>();
            backdrop.color = new Color(0.01f, 0.014f, 0.02f, 0.86f);
            var backdropButton = panelObj.GetComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.targetGraphic = backdrop;
            backdropButton.onClick.AddListener(Hide);

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            card.transform.SetParent(panelObj.transform, false);
            cardRect = (RectTransform)card.transform;
            UIStyleKit.StylePanel(cardRect, new Color(0.055f, 0.068f, 0.082f, 1f));
            card.GetComponent<Image>().raycastTarget = true;
            var frame = card.AddComponent<MenuSafeFrame>();
            frame.Configure(new Vector2(1080f, 720f), new Vector2(640f, 440f), new Vector2(40f, 28f));
            var cardLayout = card.GetComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(22, 22, 18, 16);
            cardLayout.spacing = 10f;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            var header = Row(card.transform, "Header", 56f, false);
            var title = Label(header, "Cài đặt", "Settings", "設定", 28, Vector2.zero, new Vector2(400f, 40f), TextAlignmentOptions.MidlineLeft, true);
            StretchInLayout(title.rectTransform, 1f, 40f);
            var x = CreateButton(header, "X", Vector2.zero, new Vector2(44f, 40f), true);
            x.name = "CloseX";
            StretchInLayout(x.GetComponent<RectTransform>(), 0f, 40f, 44f);
            x.GetComponentInChildren<TextMeshProUGUI>().fontSize = 22f;
            x.onClick.AddListener(Hide);

            var body = Row(card.transform, "Body", 200f, true);
            var left = ScrollColumn(body, "Options", 340f);
            var right = ScrollColumn(body, "Bindings", 0f);

            BuildOptions(left);
            BuildBindings(right);

            var footer = Row(card.transform, "Footer", 52f, false);
            var reset = CreateButton(footer, string.Empty, Vector2.zero, new Vector2(200f, 44f), false);
            Localize(reset.GetComponentInChildren<TextMeshProUGUI>(), "Mặc định", "Reset keys", "初期化");
            StretchInLayout(reset.GetComponent<RectTransform>(), 1f, 44f);
            reset.onClick.AddListener(() => input.ResetBindings());
            closeButton = CreateButton(footer, string.Empty, Vector2.zero, new Vector2(200f, 44f), true);
            Localize(closeButton.GetComponentInChildren<TextMeshProUGUI>(), "Đóng", "Close", "閉じる");
            StretchInLayout(closeButton.GetComponent<RectTransform>(), 1f, 44f);
            closeButton.onClick.AddListener(Hide);
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                var graphics = CreateButton(footer, string.Empty, Vector2.zero, new Vector2(220f, 44f), false);
                StretchInLayout(graphics.GetComponent<RectTransform>(), 1.2f, 44f);
                webGlGraphicsLabel = graphics.GetComponentInChildren<TextMeshProUGUI>();
                webGlGraphicsLabel.fontSize = 14f;
                webGlGraphicsLabel.enableAutoSizing = true;
                webGlGraphicsLabel.fontSizeMin = 10f;
                webGlGraphicsLabel.fontSizeMax = 14f;
                graphics.onClick.AddListener(() =>
                {
                    WebGLGraphicsSettings.Cycle();
                    RefreshWebGlGraphics();
                    HudFeed.Post(WebGLGraphicsSettings.ReloadHint(), HudFeed.Kind.Info, 4f, "webgl-graphics");
                });
            }

            RefreshValues();
        }

        private void BuildOptions(RectTransform column)
        {
            Label(column, "Âm thanh", "Audio", "音", 20, Vector2.zero, new Vector2(300f, 28f), TextAlignmentOptions.MidlineLeft, true);
            Label(column, "Nhạc nền", "Music", "BGM", 15, Vector2.zero, new Vector2(300f, 22f), TextAlignmentOptions.MidlineLeft, false);
            bgmSlider = CreateSlider(column, Vector2.zero, out bgmValue);
            ParkInLayout(bgmSlider.transform, 36f);
            Label(column, "Hiệu ứng", "Sound effects", "効果音", 15, Vector2.zero, new Vector2(300f, 22f), TextAlignmentOptions.MidlineLeft, false);
            sfxSlider = CreateSlider(column, Vector2.zero, out sfxValue);
            ParkInLayout(sfxSlider.transform, 36f);
            bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);

            Label(column, "Ngôn ngữ", "Language", "言語", 18, Vector2.zero, new Vector2(300f, 26f), TextAlignmentOptions.MidlineLeft, true);
            var languages = Row(column, "Languages", 40f, false);
            CreateLanguageButton(languages, "VI", GameLanguage.Vietnamese, Vector2.zero);
            CreateLanguageButton(languages, "EN", GameLanguage.English, Vector2.zero);
            CreateLanguageButton(languages, "JP", GameLanguage.Japanese, Vector2.zero);
            foreach (RectTransform child in languages)
                StretchInLayout(child, 1f, 36f);

            Label(column, "Camera & chuột", "Camera & mouse", "カメラ・マウス", 18, Vector2.zero, new Vector2(300f, 26f), TextAlignmentOptions.MidlineLeft, true);
            Label(column, "Độ nhạy chuột", "Mouse sensitivity", "マウス感度", 15, Vector2.zero, new Vector2(300f, 22f), TextAlignmentOptions.MidlineLeft, false);
            sensitivitySlider = CreateSlider(column, Vector2.zero, out sensitivityValue);
            ParkInLayout(sensitivitySlider.transform, 36f);
            sensitivitySlider.minValue = ControlSettings.MinSensitivity;
            sensitivitySlider.maxValue = ControlSettings.MaxSensitivity;
            sensitivitySlider.onValueChanged.AddListener(v => { ControlSettings.MouseSensitivity = v; sensitivityValue.text = $"{v:0.0}x"; });

            var toggles = Row(column, "Toggles", 42f, false);
            invertButton = CreateButton(toggles, string.Empty, Vector2.zero, new Vector2(160f, 40f), false);
            invertButton.name = "InvertY";
            StretchInLayout(invertButton.GetComponent<RectTransform>(), 1f, 40f);
            invertButton.onClick.AddListener(() => { ControlSettings.InvertY = !ControlSettings.InvertY; RefreshControlButtons(); });
            schemeButton = CreateButton(toggles, string.Empty, Vector2.zero, new Vector2(160f, 40f), false);
            schemeButton.name = "ControlScheme";
            StretchInLayout(schemeButton.GetComponent<RectTransform>(), 1f, 40f);
            schemeButton.onClick.AddListener(() =>
            {
                ControlSettings.Scheme = ControlSettings.Scheme == ControlScheme.MouseLook ? ControlScheme.ClickToMove : ControlScheme.MouseLook;
                RefreshControlButtons();
            });

            schemeHint = CreateText(column, string.Empty, 14, Vector2.zero, new Vector2(300f, 64f), TextAlignmentOptions.TopLeft);
            schemeHint.textWrappingMode = TextWrappingModes.Normal;
            schemeHint.enableAutoSizing = false;
            schemeHint.color = new Color(0.66f, 0.72f, 0.8f, 1f);
            ParkInLayout(schemeHint.transform, 72f);

            var motion = CreateButton(column, string.Empty, Vector2.zero, new Vector2(300f, 40f), false);
            motion.name = "ReduceMotion";
            reduceMotionLabel = motion.GetComponentInChildren<TextMeshProUGUI>();
            reduceMotionLabel.fontSize = 15f;
            reduceMotionLabel.enableAutoSizing = true;
            reduceMotionLabel.fontSizeMin = 11f;
            reduceMotionLabel.fontSizeMax = 15f;
            ParkInLayout(motion.transform, 40f);
            motion.onClick.AddListener(() => { MotionSettings.Toggle(); RefreshControlButtons(); });
            TouchControlSettingsSection.Build(column, uiFont);
        }

        private void BuildBindings(RectTransform column)
        {
            Label(column, "Phím điều khiển", "Key bindings", "キー操作", 20, Vector2.zero, new Vector2(400f, 28f), TextAlignmentOptions.MidlineLeft, true);
            var hint = Label(column, "Bấm vào ô phím để đổi. Giữ Ctrl để hiện con trỏ khi đang chơi.", "Click a key to rebind. Hold Ctrl for a cursor during play.", "キーをクリックして変更。Ctrlでカーソル表示。", 14, Vector2.zero, new Vector2(400f, 40f), TextAlignmentOptions.TopLeft, false);
            hint.textWrappingMode = TextWrappingModes.Normal;
            ParkInLayout(hint.transform, 44f);

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
                (GameInputId.Settings, "Cài đặt", "Settings", "設定"), (GameInputId.Pause, "Đóng / quay lại", "Close / back", "閉じる・戻る"),
                (GameInputId.CameraMode, "Góc nhìn trên cao", "Overview camera", "俯瞰カメラ"), (GameInputId.CameraRotateLeft, "Xoay góc nhìn trái", "Rotate view left", "左回転"),
                (GameInputId.CameraRotateRight, "Xoay góc nhìn phải", "Rotate view right", "右回転"),
                (GameInputId.CycleTarget, "Đổi mục tiêu tương tác", "Next interaction target", "対象を切り替え"),
                (GameInputId.Social, "Bạn bè & tin nhắn", "Friends & messages", "フレンド・メッセージ"),
                (GameInputId.PushToTalk, "Giữ để nói (voice)", "Push to talk", "押して話す")
            };

            for (int i = 0; i < rows.Length; i++)
            {
                var line = Row(column, "BindRow", 40f, false);
                var name = Label(line, rows[i].vi, rows[i].en, rows[i].ja, 15, Vector2.zero, new Vector2(180f, 36f), TextAlignmentOptions.MidlineLeft, false);
                name.textWrappingMode = TextWrappingModes.Normal;
                name.overflowMode = TextOverflowModes.Ellipsis;
                StretchInLayout(name.rectTransform, 1f, 36f);
                var button = CreateButton(line, string.Empty, Vector2.zero, new Vector2(118f, 34f), false);
                button.name = "Bind_" + rows[i].id;
                StretchInLayout(button.GetComponent<RectTransform>(), 0f, 34f, 124f);
                var value = button.GetComponentInChildren<TextMeshProUGUI>();
                value.fontSize = 14f;
                value.enableAutoSizing = true;
                value.fontSizeMin = 10f;
                value.fontSizeMax = 14f;
                bindingLabels[rows[i].id] = value;
                GameInputId captured = rows[i].id;
                button.onClick.AddListener(() => BeginRebind(captured));
            }

            var fixedKeys = CreateText(column, string.Empty, 13, Vector2.zero, new Vector2(400f, 56f), TextAlignmentOptions.TopLeft);
            fixedKeys.textWrappingMode = TextWrappingModes.Normal;
            fixedKeys.color = new Color(0.66f, 0.72f, 0.8f, 1f);
            Localize(fixedKeys, "Chuột: xoay camera · Lăn chuột: zoom · Esc: đóng cửa sổ trên cùng · K: luyện thi · L: sổ tay", "Mouse: rotate camera · Wheel: zoom · Esc: close the top window · K: exams · L: notebook", "マウス：カメラ回転 · ホイール：ズーム · Esc：いちばん上の画面を閉じる · K：試験 · L：ノート");
            ParkInLayout(fixedKeys.transform, 64f);
            RefreshBindingLabels();
        }

        private static RectTransform Row(Transform parent, string name, float height, bool flexible)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(2, 2, 0, 0);
            var element = go.GetComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = flexible ? 1f : 0f;
            element.flexibleWidth = 1f;
            return (RectTransform)go.transform;
        }

        private RectTransform ScrollColumn(Transform parent, string name, float preferredWidth)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.03f);
            image.raycastTarget = true;
            var element = go.GetComponent<LayoutElement>();
            element.flexibleWidth = preferredWidth > 0f ? 0f : 1f;
            element.preferredWidth = preferredWidth > 0f ? preferredWidth : 420f;
            element.minWidth = preferredWidth > 0f ? 260f : 220f;
            element.flexibleHeight = 1f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(go.transform, false);
            var viewport = (RectTransform)viewportGo.transform;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(8f, 8f);
            viewport.offsetMax = new Vector2(-8f, -8f);
            viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewport, false);
            var content = (RectTransform)contentGo.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);
            var stack = contentGo.GetComponent<VerticalLayoutGroup>();
            stack.spacing = 6f;
            stack.padding = new RectOffset(4, 8, 4, 8);
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            var fit = contentGo.GetComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = go.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            return content;
        }

        private static void StretchInLayout(RectTransform rect, float flexibleWidth, float height, float preferredWidth = -1f)
        {
            if (rect == null) return;
            var element = rect.GetComponent<LayoutElement>().OrNull() ?? rect.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = flexibleWidth;
            element.minHeight = height;
            element.preferredHeight = height;
            if (preferredWidth > 0f) element.preferredWidth = preferredWidth;
            if (rect.GetComponent<TextMeshProUGUI>() != null) return;
            var text = rect.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                var textRect = text.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(8f, 2f);
                textRect.offsetMax = new Vector2(-8f, -2f);
            }
        }

        private static void ParkInLayout(Transform target, float height)
        {
            if (target == null) return;
            var element = target.GetComponent<LayoutElement>().OrNull() ?? target.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleWidth = 1f;
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
            RefreshWebGlGraphics();
        }

        private void RefreshWebGlGraphics()
        {
            if (webGlGraphicsLabel == null) return;
            webGlGraphicsLabel.text = Pick(WebGLGraphicsSettings.LabelVi(), WebGLGraphicsSettings.LabelEn(), WebGLGraphicsSettings.LabelJa());
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
            if (reduceMotionLabel != null)
                reduceMotionLabel.text = Pick(MotionSettings.LabelVi(), MotionSettings.LabelEn(), MotionSettings.LabelJa());
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
            // Mouse only: with keyboard navigation, walking keys (A / ←) pressed while the panel is open slid
            // the volume down to 0 without anyone noticing.
            slider.navigation = new Navigation { mode = Navigation.Mode.None };

            valueText = CreateText(go.transform, "0%", 16f, Vector2.zero, new Vector2(64f, 28f), TextAlignmentOptions.MidlineRight);
            var valueRect = valueText.rectTransform;
            valueRect.anchorMin = valueRect.anchorMax = valueRect.pivot = new Vector2(1f, 0.5f);
            valueRect.anchoredPosition = new Vector2(-2f, 0f);
            valueRect.sizeDelta = new Vector2(64f, 28f);
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
