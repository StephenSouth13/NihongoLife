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
            var root = UiBind.Make("TouchControlsSettings", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(TouchControlSettingsBinder));
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
            var go = UiBind.Make("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
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
            var go = UiBind.Make("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
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
            var go = UiBind.Make("Slider", typeof(RectTransform), typeof(LayoutElement));
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

            var fillArea = UiBind.Make("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = (RectTransform)fillArea.transform;
            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.sizeDelta = new Vector2(-20f, 8f);
            var fill = MakeImage(fillArea.transform, "Fill", UiTheme.Gold);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.sizeDelta = new Vector2(10f, 0f);

            var handleSlide = UiBind.Make("HandleSlide", typeof(RectTransform));
            handleSlide.transform.SetParent(go.transform, false);
            var handleArea = (RectTransform)handleSlide.transform;
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(8f, 0f);
            handleArea.offsetMax = new Vector2(-8f, 0f);
            var handle = MakeImage(handleSlide.transform, "Handle", Color.white);
            handle.rectTransform.sizeDelta = new Vector2(18f, 18f);

            var slider = go.Need<Slider>();
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
            var go = UiBind.Make(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = UiKit.Rounded(8);
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }
    }

}
