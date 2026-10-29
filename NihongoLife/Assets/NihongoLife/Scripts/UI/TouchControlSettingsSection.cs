using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Settings block for on-screen controls. Built into the existing options column.</summary>
    public static class TouchControlSettingsSection
    {
        public static void Build(Transform column, TMP_FontAsset font)
        {
            if (column == null) return;
            var root = new GameObject("TouchControlsSettings", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(TouchControlSettingsBinder));
            root.transform.SetParent(column, false);
            var layout = root.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(0, 0, 8, 4);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var element = root.GetComponent<LayoutElement>();
            element.minHeight = 360f;
            element.preferredHeight = 380f;
            element.flexibleWidth = 1f;

            var title = MakeText(root.transform, font, 18f, FontStyles.Bold, 28f);
            var mode = MakeButton(root.transform, font, 40f);
            var sizeLabel = MakeText(root.transform, font, 14f, FontStyles.Normal, 22f);
            var size = MakeSlider(root.transform, 0.7f, 1.45f, TouchControlPrefs.Size);
            var opacityLabel = MakeText(root.transform, font, 14f, FontStyles.Normal, 22f);
            var opacity = MakeSlider(root.transform, 0.35f, 1f, TouchControlPrefs.Opacity);
            var hand = MakeButton(root.transform, font, 40f);
            var hint = MakeText(root.transform, font, 13f, FontStyles.Normal, 72f);
            hint.textWrappingMode = TextWrappingModes.Normal;
            hint.alignment = TextAlignmentOptions.TopLeft;

            var binder = root.GetComponent<TouchControlSettingsBinder>();
            binder.Bind(title, mode, sizeLabel, size, opacityLabel, opacity, hand, hint);
        }

        static TextMeshProUGUI MakeText(Transform parent, TMP_FontAsset font, float size, FontStyles style, float height)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var element = go.GetComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleWidth = 1f;
            var text = go.GetComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = new Color(0.93f, 0.95f, 0.97f, 1f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableAutoSizing = true;
            text.fontSizeMin = 11f;
            text.fontSizeMax = size;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        static Button MakeButton(Transform parent, TMP_FontAsset font, float height)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var element = go.GetComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleWidth = 1f;
            var image = go.GetComponent<Image>();
            image.sprite = UiKit.Rounded(12);
            image.type = Image.Type.Sliced;
            image.color = new Color(0.14f, 0.17f, 0.22f, 1f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var label = MakeText(go.transform, font, 15f, FontStyles.Bold, height);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 0f);
            labelRect.offsetMax = new Vector2(-12f, 0f);
            var labelElement = label.GetComponent<LayoutElement>();
            labelElement.ignoreLayout = true;
            return button;
        }

        static Slider MakeSlider(Transform parent, float min, float max, float value)
        {
            var go = new GameObject("Slider", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var element = go.GetComponent<LayoutElement>();
            element.minHeight = 32f;
            element.preferredHeight = 32f;
            element.flexibleWidth = 1f;

            var background = MakeImage(go.transform, "Background", new Color(0.16f, 0.19f, 0.22f, 1f));
            var backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(-8f, 8f);

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = (RectTransform)fillArea.transform;
            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.sizeDelta = new Vector2(-20f, 8f);
            var fill = MakeImage(fillArea.transform, "Fill", UiTheme.Gold);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.sizeDelta = new Vector2(10f, 0f);

            var handleSlide = new GameObject("HandleSlide", typeof(RectTransform));
            handleSlide.transform.SetParent(go.transform, false);
            var handleArea = (RectTransform)handleSlide.transform;
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(8f, 0f);
            handleArea.offsetMax = new Vector2(-8f, 0f);
            var handle = MakeImage(handleSlide.transform, "Handle", Color.white);
            handle.rectTransform.sizeDelta = new Vector2(18f, 18f);

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.SetValueWithoutNotify(value);
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            return slider;
        }

        static Image MakeImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = UiKit.Rounded(8);
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }
    }

    sealed class TouchControlSettingsBinder : MonoBehaviour
    {
        TextMeshProUGUI _title;
        TextMeshProUGUI _mode;
        TextMeshProUGUI _sizeLabel;
        TextMeshProUGUI _opacityLabel;
        TextMeshProUGUI _hand;
        TextMeshProUGUI _hint;
        Slider _size;
        Slider _opacity;

        public void Bind(TextMeshProUGUI title, Button mode, TextMeshProUGUI sizeLabel, Slider size, TextMeshProUGUI opacityLabel, Slider opacity, Button hand, TextMeshProUGUI hint)
        {
            _title = title;
            _mode = mode.GetComponentInChildren<TextMeshProUGUI>();
            _sizeLabel = sizeLabel;
            _opacityLabel = opacityLabel;
            _hand = hand.GetComponentInChildren<TextMeshProUGUI>();
            _hint = hint;
            _size = size;
            _opacity = opacity;
            mode.onClick.AddListener(() => TouchControlPrefs.CycleMode());
            hand.onClick.AddListener(() => TouchControlPrefs.LeftHanded = !TouchControlPrefs.LeftHanded);
            size.onValueChanged.AddListener(value =>
            {
                if (Mathf.Abs(value - TouchControlPrefs.Size) < 0.001f) return;
                TouchControlPrefs.Size = value;
            });
            opacity.onValueChanged.AddListener(value =>
            {
                if (Mathf.Abs(value - TouchControlPrefs.Opacity) < 0.001f) return;
                TouchControlPrefs.Opacity = value;
            });
            Refresh();
        }

        private void OnEnable()
        {
            TouchControlPrefs.Changed += Refresh;
            Loc.Watch(this, Refresh);
            Refresh();
        }

        private void OnDisable() => TouchControlPrefs.Changed -= Refresh;

        void Refresh()
        {
            if (_title != null) _title.text = Loc.T("Điều khiển cảm ứng", "Touch controls", "タッチ操作");
            if (_mode != null) _mode.text = Loc.T(TouchControlPrefs.ModeVi(), TouchControlPrefs.ModeEn(), TouchControlPrefs.ModeJa());
            if (_hand != null) _hand.text = Loc.T(TouchControlPrefs.HandVi(), TouchControlPrefs.HandEn(), TouchControlPrefs.HandJa());
            if (_sizeLabel != null) _sizeLabel.text = Loc.F("Cỡ nút  {0}", "Button size  {0}", "ボタンサイズ  {0}", TouchControlPrefs.Size.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            if (_opacityLabel != null) _opacityLabel.text = Loc.F("Độ mờ  {0}", "Opacity  {0}", "不透明度  {0}", TouchControlPrefs.Opacity.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            if (_hint != null) _hint.text = Loc.T(
                "Tự động hiện trên điện thoại và máy tính bảng. Bàn phím và chuột vẫn dùng được. Ẩn nhanh bằng nút Ẩn trên màn hình.",
                "Auto appears on phones and tablets. Keyboard and mouse keep working. Hide hides them until you tap Controls.",
                "スマホとタブレットでは自動で表示。キーボードとマウスはそのまま使えます。隠すで一時的に消せます。");
            if (_size != null) _size.SetValueWithoutNotify(TouchControlPrefs.Size);
            if (_opacity != null) _opacity.SetValueWithoutNotify(TouchControlPrefs.Opacity);
        }
    }
}
