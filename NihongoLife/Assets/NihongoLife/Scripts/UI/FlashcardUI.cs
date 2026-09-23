using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace NihongoLife.UI
{
    public class FlashcardUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text wordText;
        [SerializeField] private TMP_Text meaningText;
        [SerializeField] private Button flipButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button previousButton;
        
        private List<FlashcardData> currentDeck = new List<FlashcardData>();
        private int currentIndex = 0;
        private bool isFlipped = false;

        private void Awake()
        {
            if (flipButton) flipButton.onClick.AddListener(FlipCard);
            if (nextButton) nextButton.onClick.AddListener(NextCard);
            if (previousButton) previousButton.onClick.AddListener(PreviousCard);
        }

        public void LoadDeck(List<FlashcardData> deck)
        {
            currentDeck = deck;
            currentIndex = 0;
            isFlipped = false;
            UpdateDisplay();
            gameObject.SetActive(true);
        }

        private void FlipCard()
        {
            isFlipped = !isFlipped;
            UpdateDisplay();
        }

        private void NextCard()
        {
            if (currentIndex < currentDeck.Count - 1)
            {
                currentIndex++;
                isFlipped = false;
                UpdateDisplay();
            }
        }

        private void PreviousCard()
        {
            if (currentIndex > 0)
            {
                currentIndex--;
                isFlipped = false;
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            if (currentDeck.Count == 0) return;

            var card = currentDeck[currentIndex];
            if (!isFlipped)
            {
                wordText.text = card.Word;
                meaningText.text = ""; // Hide meaning
            }
            else
            {
                wordText.text = card.Word;
                meaningText.text = card.Meaning;
            }

            previousButton.interactable = currentIndex > 0;
            nextButton.interactable = currentIndex < currentDeck.Count - 1;
        }
    }

    public class FlashcardData
    {
        public string Word;
        public string Meaning;
        public string TargetId;
    }
}
