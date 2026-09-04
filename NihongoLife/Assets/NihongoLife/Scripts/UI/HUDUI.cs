using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using NihongoLife.Dialogue;
using NihongoLife.Scenario;
using NihongoLife.Interaction;
using NihongoLife.Learning;
using NihongoLife.Player;

namespace NihongoLife.UI
{
    public class HUDUI : MonoBehaviour
    {
        [Header("Interaction Tooltip")]
        [SerializeField] private GameObject promptPanel;
        [SerializeField] private TextMeshProUGUI promptText;

        [Header("Objectives Overlay")]
        [SerializeField] private TextMeshProUGUI scenarioTitleText;
        [SerializeField] private TextMeshProUGUI objectivesText;

        [Header("Dialogue Panel")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerText;
        [SerializeField] private TextMeshProUGUI japaneseText;
        [SerializeField] private TextMeshProUGUI readingText;
        [SerializeField] private TextMeshProUGUI romajiText;
        [SerializeField] private TextMeshProUGUI translationText;
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private Button choiceButtonPrefab;
        [SerializeField] private Button continueButton;

        [Header("Player Panels")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private TextMeshProUGUI inventoryText;
        [SerializeField] private TextMeshProUGUI walletText;
        [SerializeField] private GameObject characterPanel;
        [SerializeField] private TextMeshProUGUI characterStatsText;

        private List<Button> _activeChoiceButtons = new List<Button>();

        private void Start()
        {
            // Connect to systems
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var detector = player.GetComponent<InteractionDetector>();
                if (detector != null)
                {
                    detector.OnInteractableChanged += HandleInteractableChanged;
                }
            }

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueUpdated += DisplayDialogue;
                DialogueManager.Instance.OnDialogueClosed += HideDialogue;
            }

            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.OnScenarioStarted += UpdateScenarioInfo;
                ScenarioManager.Instance.OnObjectiveStateChanged += HandleObjectiveChanged;
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged += RefreshPlayerPanels;
            }

            HideDialogue();
            HidePrompt();
            SetInventoryVisible(false);
            SetCharacterVisible(false);
            RefreshPlayerPanels();
            UpdateObjectivesDisplay();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                SetInventoryVisible(inventoryPanel != null && !inventoryPanel.activeSelf);
            }

            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                SetCharacterVisible(characterPanel != null && !characterPanel.activeSelf);
            }
        }

        private void OnDestroy()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueUpdated -= DisplayDialogue;
                DialogueManager.Instance.OnDialogueClosed -= HideDialogue;
            }

            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.OnScenarioStarted -= UpdateScenarioInfo;
                ScenarioManager.Instance.OnObjectiveStateChanged -= HandleObjectiveChanged;
            }

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= RefreshPlayerPanels;
            }
        }

        private void SetInventoryVisible(bool visible)
        {
            if (inventoryPanel == null) return;
            inventoryPanel.SetActive(visible);
            if (visible)
            {
                SetCharacterVisible(false);
                RefreshPlayerPanels();
            }
        }

        private void SetCharacterVisible(bool visible)
        {
            if (characterPanel == null) return;
            characterPanel.SetActive(visible);
            if (visible)
            {
                SetInventoryVisible(false);
                RefreshPlayerPanels();
            }
        }

        private void RefreshPlayerPanels()
        {
            var inventory = PlayerInventory.Instance;
            if (inventory == null) return;

            if (walletText != null)
            {
                walletText.text = $"¥ {inventory.Yen}";
            }

            if (inventoryText != null)
            {
                if (inventory.Items.Count == 0)
                {
                    inventoryText.text = "Balo trống";
                }
                else
                {
                    var builder = new StringBuilder();
                    foreach (var item in inventory.Items)
                    {
                        string ja = string.IsNullOrEmpty(item.displayNameJa) ? item.itemId : item.displayNameJa;
                        string vi = string.IsNullOrEmpty(item.displayNameVi) ? item.itemId : item.displayNameVi;
                        builder.AppendLine($"{ja} / {vi}");
                        builder.AppendLine($"x{item.quantity}    ¥{item.priceYen}");
                    }
                    inventoryText.text = builder.ToString();
                }
            }

            if (characterStatsText != null)
            {
                characterStatsText.text =
                    "Học viên\n" +
                    "Cấp độ: N5 Starter\n" +
                    "Tốc độ: Đi bộ / chạy\n" +
                    $"Tiền mặt: ¥{inventory.Yen}\n" +
                    "Mục tiêu: mua hàng bằng tiếng Nhật";
            }
        }

        #region Interaction Prompt
        private void HandleInteractableChanged(IInteractable interactable)
        {
            if (interactable == null)
            {
                HidePrompt();
            }
            else
            {
                ShowPrompt(interactable);
            }
        }

        private void ShowPrompt(IInteractable interactable)
        {
            if (promptPanel == null || promptText == null) return;
            promptPanel.SetActive(true);
            // Display Japanese and Vietnamese hints
            promptText.text = $"[E] {interactable.GetPromptJa()} / {interactable.GetPromptVi()}";
        }

        private void HidePrompt()
        {
            if (promptPanel != null) promptPanel.SetActive(false);
        }
        #endregion

        #region Objectives HUD
        private void UpdateScenarioInfo(ScenarioDefinition scenario)
        {
            if (scenarioTitleText != null)
            {
                scenarioTitleText.text = $"{scenario.titleJa} / {scenario.titleVi}";
            }
            UpdateObjectivesDisplay();
        }

        private void HandleObjectiveChanged(RuntimeObjective objective)
        {
            UpdateObjectivesDisplay();
        }

        private void UpdateObjectivesDisplay()
        {
            if (objectivesText == null) return;

            if (ScenarioManager.Instance == null || ScenarioManager.Instance.CurrentScenario == null)
            {
                objectivesText.text = "";
                if (scenarioTitleText != null) scenarioTitleText.text = "Không có nhiệm vụ";
                return;
            }

            string text = "";
            foreach (var obj in ScenarioManager.Instance.Objectives)
            {
                if (obj.state == ObjectiveState.Inactive) continue;

                string check = obj.state == ObjectiveState.Completed ? "✔" : "☐";
                string color = obj.state == ObjectiveState.Completed ? "#a3e635" : "#e2e8f0"; // Green vs Light Gray
                
                text += $"<color={color}>{check} {obj.titleJa} ({obj.titleVi})</color>\n";
            }

            objectivesText.text = text;
        }
        #endregion

        #region Dialogue System UI
        private void DisplayDialogue(DialogueDisplayData data)
        {
            if (dialoguePanel == null) return;
            dialoguePanel.SetActive(true);

            if (speakerText != null) speakerText.text = data.speakerName;
            if (japaneseText != null) japaneseText.text = data.textJa;

            // Handle Learning Modes for Hints
            ConfigureModeVisibility(data);

            // Clean up old choices
            ClearChoiceButtons();

            // Populate choices or continue button
            if (data.choices != null && data.choices.Count > 0)
            {
                if (continueButton != null) continueButton.gameObject.SetActive(false);
                
                for (int i = 0; i < data.choices.Count; i++)
                {
                    int index = i;
                    var choice = data.choices[i];

                    Button btn = Instantiate(choiceButtonPrefab, choicesContainer);
                    btn.gameObject.SetActive(true);

                    // Text configuration
                    var btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null)
                    {
                        if (data.learningMode == LearningMode.GuidedPractice)
                        {
                            btnText.text = $"{choice.textJa}\n<size=80%><color=#94a3b8>({choice.textVi})</color></size>";
                        }
                        else if (data.learningMode == LearningMode.Practice)
                        {
                            btnText.text = $"{choice.textJa}";
                        }
                        else // Assessment
                        {
                            btnText.text = $"{choice.textJa}";
                        }
                    }

                    btn.onClick.AddListener(() => OnChoiceSelected(index));
                    _activeChoiceButtons.Add(btn);
                }
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
                    if (readingText != null) { readingText.text = data.textReading; readingText.gameObject.SetActive(!string.IsNullOrEmpty(data.textReading)); }
                    if (romajiText != null) { romajiText.text = data.textRomaji; romajiText.gameObject.SetActive(!string.IsNullOrEmpty(data.textRomaji)); }
                    if (translationText != null) { translationText.text = data.textVi; translationText.gameObject.SetActive(!string.IsNullOrEmpty(data.textVi)); }
                    break;

                case LearningMode.Practice:
                    if (readingText != null) { readingText.text = data.textReading; readingText.gameObject.SetActive(!string.IsNullOrEmpty(data.textReading)); }
                    if (romajiText != null) romajiText.gameObject.SetActive(false);
                    // Practice displays translation in muted form or hidden by default, here we display it
                    if (translationText != null) { translationText.text = data.textVi; translationText.gameObject.SetActive(!string.IsNullOrEmpty(data.textVi)); }
                    break;

                case LearningMode.Assessment:
                    if (readingText != null) readingText.gameObject.SetActive(false);
                    if (romajiText != null) romajiText.gameObject.SetActive(false);
                    if (translationText != null) translationText.gameObject.SetActive(false);
                    break;
            }
        }

        private void OnChoiceSelected(int index)
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.SelectChoice(index);
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
        }
        #endregion
    }
}
