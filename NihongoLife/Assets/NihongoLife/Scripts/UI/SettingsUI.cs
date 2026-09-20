using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NihongoLife.Audio;
using NihongoLife.Core;

namespace NihongoLife.UI
{
    public class SettingsUI : MonoBehaviour
    {
        private GameObject panelObj;
        private Slider bgmSlider;
        private Slider sfxSlider;
        private TMP_FontAsset uiFont;
        private GameInputService input;
        private readonly Dictionary<GameInputId, TextMeshProUGUI> bindingLabels = new();

        public void Initialize(TMP_FontAsset font)
        {
            uiFont = font;
            input = GameInputService.GetOrCreate();
            panelObj = new GameObject("SettingsPanel");
            panelObj.transform.SetParent(transform, false);
            var rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panelObj.AddComponent<Image>().color = new Color(0.025f, 0.032f, 0.038f, 0.98f);

            CreateText(panelObj.transform, "Cài đặt", 38, new Vector2(0, 245), new Vector2(500, 55), TextAlignmentOptions.Center);
            CreateText(panelObj.transform, "Âm thanh", 24, new Vector2(-360, 165), new Vector2(360, 45), TextAlignmentOptions.Center);
            CreateText(panelObj.transform, "Nhạc nền", 17, new Vector2(-360, 112), new Vector2(300, 35), TextAlignmentOptions.Center);
            bgmSlider = CreateSlider(panelObj.transform, new Vector2(-360, 75));
            CreateText(panelObj.transform, "Hiệu ứng", 17, new Vector2(-360, 15), new Vector2(300, 35), TextAlignmentOptions.Center);
            sfxSlider = CreateSlider(panelObj.transform, new Vector2(-360, -22));
            CreateText(panelObj.transform, "Điều khiển", 24, new Vector2(220, 190), new Vector2(600, 45), TextAlignmentOptions.Center);
            CreateBindingRows();

            var reset = CreateButton(panelObj.transform, "Mặc định", new Vector2(-120, -270), new Vector2(200, 46));
            reset.onClick.AddListener(input.ResetBindings);
            var close = CreateButton(panelObj.transform, "Đóng", new Vector2(120, -270), new Vector2(200, 46));
            close.onClick.AddListener(Hide);
            bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            input.OnBindingsChanged += RefreshBindingLabels;
            RefreshValues();
            panelObj.SetActive(false);
        }

        public void Show() { RefreshValues(); RefreshBindingLabels(); panelObj.SetActive(true); }
        public void Hide() => panelObj.SetActive(false);

        private void CreateBindingRows()
        {
            var rows = new (GameInputId id, string label)[]
            {
                (GameInputId.UseItem, "Use item"),
                (GameInputId.MoveUp, "Đi tới"), (GameInputId.MoveDown, "Đi lùi"),
                (GameInputId.MoveLeft, "Sang trái"), (GameInputId.MoveRight, "Sang phải"),
                (GameInputId.Sprint, "Chạy"), (GameInputId.Jump, "Nhảy"),
                (GameInputId.Interact, "Tương tác"), (GameInputId.Inventory, "Balo"),
                (GameInputId.Character, "Nhân vật"), (GameInputId.Map, "Bản đồ"), (GameInputId.Chat, "Chat"),
                (GameInputId.Voice, "Ghi âm"), (GameInputId.Attack, "Tấn công"),
                (GameInputId.DropItem, "Vứt đồ"), (GameInputId.Pause, "Tạm dừng")
            };
            for (int i = 0; i < rows.Length; i++)
            {
                int column = i / 8;
                int row = i % 8;
                float x = 65f + column * 315f;
                float y = 150f - row * 45f;
                CreateText(panelObj.transform, rows[i].label, 15, new Vector2(x, y), new Vector2(120, 38), TextAlignmentOptions.MidlineLeft);
                var button = CreateButton(panelObj.transform, string.Empty, new Vector2(x + 125f, y), new Vector2(118, 36));
                var value = button.GetComponentInChildren<TextMeshProUGUI>();
                value.fontSize = 14;
                bindingLabels[rows[i].id] = value;
                GameInputId captured = rows[i].id;
                button.onClick.AddListener(() => BeginRebind(captured));
            }
            RefreshBindingLabels();
        }

        private void BeginRebind(GameInputId id)
        {
            if (!bindingLabels.TryGetValue(id, out var label)) return;
            label.text = "Nhấn phím...";
            input.StartRebind(id, _ => RefreshBindingLabels());
        }

        private void RefreshBindingLabels()
        {
            if (input == null) return;
            foreach (var pair in bindingLabels) pair.Value.text = input.GetBindingLabel(pair.Key);
        }

        private void RefreshValues()
        {
            bgmSlider.value = PlayerPrefs.GetFloat("Volume_BGM", 0.5f);
            sfxSlider.value = PlayerPrefs.GetFloat("Volume_SFX", 0.8f);
        }

        private void OnBgmChanged(float value) { if (GameServices.TryGet(out IAudioService audio)) audio.SetBGMVolume(value); }
        private void OnSfxChanged(float value) { if (GameServices.TryGet(out IAudioService audio)) audio.SetSFXVolume(value); }

        private TextMeshProUGUI CreateText(Transform parent, string value, float size, Vector2 position, Vector2 dimensions, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = uiFont;
            text.text = value;
            text.fontSize = size;
            text.color = new Color(0.92f, 0.95f, 0.97f, 1f);
            text.alignment = alignment;
            text.enableAutoSizing = true;
            text.fontSizeMin = 11f;
            text.fontSizeMax = size;
            return text;
        }

        private Slider CreateSlider(Transform parent, Vector2 position)
        {
            var go = new GameObject("Slider");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300, 20);
            CreateImage(go.transform, "Track", new Color(0.16f, 0.19f, 0.22f), true);
            var fillArea = CreateImage(go.transform, "FillArea", Color.clear, true);
            var fill = CreateImage(fillArea, "Fill", new Color(0.92f, 0.66f, 0.18f), true).GetComponent<RectTransform>();
            var handleArea = CreateImage(go.transform, "HandleArea", Color.clear, true);
            var handle = CreateImage(handleArea, "Handle", Color.white, false);
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 30);
            var slider = go.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<Image>();
            return slider;
        }

        private Transform CreateImage(Transform parent, string name, Color color, bool stretch)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            if (stretch) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
            go.AddComponent<Image>().color = color;
            return go.transform;
        }

        private Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.18f, 1f);
            var button = go.AddComponent<Button>();
            var text = CreateText(go.transform, label, 16, Vector2.zero, size, TextAlignmentOptions.Center);
            text.raycastTarget = false;
            return button;
        }

        private void OnDestroy() { if (input != null) input.OnBindingsChanged -= RefreshBindingLabels; }
    }
}
