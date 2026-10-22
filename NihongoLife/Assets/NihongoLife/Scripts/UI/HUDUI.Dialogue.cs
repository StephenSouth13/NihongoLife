using System.Linq;
using NihongoLife.Save;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Learning;
using NihongoLife.Player;
using NihongoLife.Scenario;

namespace NihongoLife.UI
{
    /// <summary>Partial of HUDUI: dialogue view, choices, and continue. The MonoBehaviour entry point stays HUDUI.cs.</summary>
    public partial class HUDUI : MonoBehaviour
    {
        /// <summary>The scene-authored dialogue panel stacked fixed-size text boxes that overflowed on long
        /// lines; every scene now uses the layout-driven DialogueView. dialoguePanel points at the new view so
        /// the existing "dialogue open" checks keep working.</summary>
        private void EnsureDialogueView()
        {
            if (_dialogueView != null) return;
            var font = japaneseText != null ? japaneseText.font : (scenarioTitleText != null ? scenarioTitleText.font : null);
            _dialogueView = DialogueView.Create(transform, font);
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            dialoguePanel = _dialogueView.Root;
            _dialogueView.ChoiceSelected += OnChoiceSelected;
            _dialogueView.ContinueRequested += OnContinueClicked;
            _dialogueView.LeaveRequested += () => DialogueManager.Instance?.CancelDialogue();
        }

        /// <summary>The bag (B) uses InventoryWindow (slot grid + detail + vitals) instead of the old text list.</summary>
        private void EnsureInventoryWindow()
        {
            if (_inventoryWindow != null) return;
            var font = japaneseText != null ? japaneseText.font : (scenarioTitleText != null ? scenarioTitleText.font : null);
            _inventoryWindow = InventoryWindow.Create(transform, font);
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            inventoryPanel = _inventoryWindow.Root;
        }

        private void DisplayDialogue(DialogueDisplayData data)
        {
            EnsureDialogueView();
            if (_dialogueView != null)
            {
                _dialogueView.Show(data, DialogueManager.Instance != null && DialogueManager.Instance.CanLeave);
                return;
            }
            if (dialoguePanel == null) return;
            dialoguePanel.SetActive(true);

            if (speakerText != null) speakerText.text = data.speakerName;
            if (japaneseText != null) japaneseText.text = data.textJa;

            ConfigureModeVisibility(data);
            ClearChoiceButtons();

            if (data.choices != null && data.choices.Count > 0)
            {
                if (continueButton != null) continueButton.gameObject.SetActive(false);

                for (int i = 0; i < data.choices.Count; i++)
                {
                    int index = i;
                    var choice = data.choices[i];
                    Button btn = Instantiate(choiceButtonPrefab, choicesContainer);
                    btn.gameObject.SetActive(true);

                    var btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null)
                    {
                        ConfigureText(btnText, 11f, 16f);
                        btnText.text = data.learningMode == LearningMode.GuidedPractice && !string.IsNullOrWhiteSpace(choice.Gloss)
                            ? $"{choice.textJa}\n<size=80%><color=#8fa3b8>({choice.Gloss})</color></size>"
                            : choice.textJa;
                    }

                    btn.onClick.AddListener(() => OnChoiceSelected(index));
                    _activeChoiceButtons.Add(btn);
                }

                SelectChoiceVisual(0);
            }
            else
            {
                if (continueButton != null) continueButton.gameObject.SetActive(true);
            }
        }

        private void ConfigureModeVisibility(DialogueDisplayData data)
        {
            switch (data.learningMode)
            {
                case LearningMode.GuidedPractice:
                    SetHintText(readingText, data.textReading, true);
                    SetHintText(romajiText, data.textRomaji, true);
                    SetHintText(translationText, TranslationLine(data), true);
                    break;
                case LearningMode.Practice:
                    SetHintText(readingText, data.textReading, true);
                    SetHintText(romajiText, data.textEnglishIpa, !string.IsNullOrWhiteSpace(data.textEnglishIpa));
                    SetHintText(translationText, TranslationLine(data), true);
                    break;
                case LearningMode.Assessment:
                    SetHintText(readingText, string.Empty, false);
                    SetHintText(romajiText, string.Empty, false);
                    SetHintText(translationText, string.Empty, false);
                    break;
            }
        }

        private static void SetHintText(TextMeshProUGUI target, string value, bool showWhenNotEmpty)
        {
            if (target == null) return;
            target.text = value;
            target.gameObject.SetActive(showWhenNotEmpty && !string.IsNullOrEmpty(value));
        }

        private static string TranslationLine(DialogueDisplayData data)
        {
            if (GameServices.TryGet(out GameSettingsService settings) && settings.Language == GameLanguage.English)
            {
                return string.IsNullOrWhiteSpace(data.textEnglishIpa)
                    ? data.textEn
                    : $"{data.textEn}\nIPA: {data.textEnglishIpa}";
            }

            return data.textEn;
        }

        private void OnChoiceSelected(int index)
        {
            if (_activeChoiceButtons.Count > 0 && (index < 0 || index >= _activeChoiceButtons.Count)) return;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.SelectChoice(index);
            }
        }

        private void SelectChoiceVisual(int index)
        {
            if (_activeChoiceButtons.Count == 0)
            {
                _selectedChoiceIndex = -1;
                return;
            }

            _selectedChoiceIndex = (index % _activeChoiceButtons.Count + _activeChoiceButtons.Count) % _activeChoiceButtons.Count;
            for (int i = 0; i < _activeChoiceButtons.Count; i++)
            {
                var image = _activeChoiceButtons[i].GetComponent<Image>();
                if (image != null)
                {
                    image.color = i == _selectedChoiceIndex
                        ? new Color(0.95f, 0.55f, 0.12f, 1f)
                        : new Color(0.12f, 0.15f, 0.17f, 1f);
                }
            }
        }

        private void OnContinueClicked()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ContinueDialogue();
            }
        }

        private void HideDialogue()
        {
            _dialogueView?.Hide();
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            ClearChoiceButtons();
        }

        private void ClearChoiceButtons()
        {
            foreach (var btn in _activeChoiceButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            _activeChoiceButtons.Clear();
            _selectedChoiceIndex = -1;
        }
    }
}
