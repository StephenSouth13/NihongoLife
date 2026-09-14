using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace NihongoLife.UI
{
    public class TutorialUI : MonoBehaviour
    {
        private const string TutorialCompletedKey = "TutorialCompleted";

        [SerializeField] private float autoHideSeconds = 5f;

        private GameObject panel;
        private TextMeshProUGUI instructionsText;
        private float _hideAtTime;

        public void Initialize(TMP_FontAsset font)
        {
            if (PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1)
            {
                return;
            }

            panel = new GameObject("TutorialPanel");
            panel.transform.SetParent(transform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.2f);
            rect.anchorMax = new Vector2(0.5f, 0.2f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(600, 120);

            var img = panel.AddComponent<Image>();
            img.color = new Color(0.1f, 0.12f, 0.15f, 0.9f);

            var go = new GameObject("Text");
            go.transform.SetParent(panel.transform, false);
            var textRect = go.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            instructionsText = go.AddComponent<TextMeshProUGUI>();
            instructionsText.font = font;
            instructionsText.fontSize = 24;
            instructionsText.alignment = TextAlignmentOptions.Center;
            instructionsText.color = Color.white;
            instructionsText.text = "S\u1eed d\u1ee5ng ph\u00edm W A S D \u0111\u1ec3 di chuy\u1ec3n";

            panel.SetActive(true);
            _hideAtTime = Time.unscaledTime + autoHideSeconds;
        }

        private void Update()
        {
            if (panel == null || !panel.activeSelf) return;

            if (Time.unscaledTime >= _hideAtTime || HasMovementInput())
            {
                CompleteTutorial();
            }
        }

        private static bool HasMovementInput()
        {
            if (Keyboard.current == null) return false;

            return Keyboard.current.wKey.wasPressedThisFrame
                || Keyboard.current.aKey.wasPressedThisFrame
                || Keyboard.current.sKey.wasPressedThisFrame
                || Keyboard.current.dKey.wasPressedThisFrame
                || Keyboard.current.upArrowKey.wasPressedThisFrame
                || Keyboard.current.downArrowKey.wasPressedThisFrame
                || Keyboard.current.leftArrowKey.wasPressedThisFrame
                || Keyboard.current.rightArrowKey.wasPressedThisFrame;
        }

        private void CompleteTutorial()
        {
            PlayerPrefs.SetInt(TutorialCompletedKey, 1);
            PlayerPrefs.Save();
            panel.SetActive(false);
        }
    }
}
