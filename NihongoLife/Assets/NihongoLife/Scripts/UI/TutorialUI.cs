using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NihongoLife.UI
{
    public class TutorialUI : MonoBehaviour
    {
        private GameObject panel;
        private TextMeshProUGUI instructionsText;
        private bool hasMoved = false;
        private bool hasInteracted = false;
        private bool hasBag = false;

        public void Initialize(TMP_FontAsset font)
        {
            // Only show tutorial once per session/save
            if (PlayerPrefs.GetInt("TutorialCompleted", 0) == 1)
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
            instructionsText.text = "Sử dụng phím W A S D để di chuyển";

            panel.SetActive(true);
        }

        private void Update()
        {
            if (panel == null || !panel.activeSelf) return;

            if (!hasMoved)
            {
                if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
                {
                    hasMoved = true;
                    instructionsText.text = "Sử dụng chuột để xoay Camera\nNhấn [E] để tương tác với nhân vật/đồ vật";
                }
            }
            else if (!hasInteracted)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    hasInteracted = true;
                    instructionsText.text = "Nhấn [B] để mở Balo (Inventory)";
                }
            }
            else if (!hasBag)
            {
                if (Input.GetKeyDown(KeyCode.B))
                {
                    hasBag = true;
                    CompleteTutorial();
                }
            }
        }

        private void CompleteTutorial()
        {
            PlayerPrefs.SetInt("TutorialCompleted", 1);
            PlayerPrefs.Save();
            panel.SetActive(false);
        }
    }
}
