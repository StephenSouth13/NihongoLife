using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NihongoLife.Core;
using NihongoLife.Audio;

namespace NihongoLife.UI
{
    public class SettingsUI : MonoBehaviour
    {
        private GameObject panelObj;
        private Slider bgmSlider;
        private Slider sfxSlider;

        public void Initialize(TMP_FontAsset font)
        {
            panelObj = new GameObject("SettingsPanel");
            panelObj.transform.SetParent(transform, false);
            var rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            var img = panelObj.AddComponent<Image>();
            img.color = new Color(0.05f, 0.06f, 0.08f, 0.95f);

            var title = CreateText(panelObj.transform, "Cài đặt âm thanh", font, 40, new Vector2(0, 150));
            
            CreateText(panelObj.transform, "Nhạc nền (BGM)", font, 24, new Vector2(0, 60));
            bgmSlider = CreateSlider(panelObj.transform, new Vector2(0, 20));
            
            CreateText(panelObj.transform, "Hiệu ứng (SFX)", font, 24, new Vector2(0, -60));
            sfxSlider = CreateSlider(panelObj.transform, new Vector2(0, -100));

            var closeBtn = CreateButton(panelObj.transform, "Đóng", font, new Vector2(0, -200));
            closeBtn.onClick.AddListener(Hide);

            bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);

            RefreshValues();
            panelObj.SetActive(false);
        }

        public void Show()
        {
            RefreshValues();
            panelObj.SetActive(true);
        }

        public void Hide()
        {
            panelObj.SetActive(false);
        }

        private void RefreshValues()
        {
            bgmSlider.value = PlayerPrefs.GetFloat("Volume_BGM", 0.5f);
            sfxSlider.value = PlayerPrefs.GetFloat("Volume_SFX", 0.8f);
        }

        private void OnBgmChanged(float val)
        {
            if (GameServices.TryGet(out IAudioService audio))
            {
                audio.SetBGMVolume(val);
            }
        }

        private void OnSfxChanged(float val)
        {
            if (GameServices.TryGet(out IAudioService audio))
            {
                audio.SetSFXVolume(val);
            }
        }

        private TextMeshProUGUI CreateText(Transform parent, string text, TMP_FontAsset font, int size, Vector2 pos)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(400, 50);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return tmp;
        }

        private Slider CreateSlider(Transform parent, Vector2 pos)
        {
            var go = new GameObject("Slider");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(300, 20);

            var bg = new GameObject("Background");
            bg.transform.SetParent(go.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            Stretch(bgRect);
            bg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            Stretch(fillArea.AddComponent<RectTransform>());

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            Stretch(fillRect);
            fill.AddComponent<Image>().color = new Color(0.2f, 0.7f, 0.3f);

            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(go.transform, false);
            Stretch(handleArea.AddComponent<RectTransform>());

            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);
            handle.AddComponent<Image>().color = Color.white;

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            
            return slider;
        }

        private Button CreateButton(Transform parent, string text, TMP_FontAsset font, Vector2 pos)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(200, 50);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.8f, 0.2f, 0.2f);

            var btn = go.AddComponent<Button>();
            
            var txt = CreateText(go.transform, text, font, 24, Vector2.zero);
            txt.rectTransform.sizeDelta = new Vector2(200, 50);

            go.AddComponent<UIHoverScale>();
            return btn;
        }

        private void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
