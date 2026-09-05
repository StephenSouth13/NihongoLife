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

        private readonly List<Button> _activeChoiceButtons = new List<Button>();

        private void Start()
        {
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

            if (GameServices.TryGet(out GameSettingsService settings))
            {
                settings.OnLanguageChanged += HandleLanguageChanged;
            }

            HideDialogue();
            HidePrompt();
            ConfigureResponsiveText();
            SetInventoryVisible(false);
            SetCharacterVisible(false);
            RefreshPlayerPanels();
            UpdateObjectivesDisplay();
        }

        private void ConfigureResponsiveText()
        {
            ConfigureText(scenarioTitleText, 15f, 21f);
            ConfigureText(objectivesText, 12f, 17f);
            ConfigureText(promptText, 13f, 18f);
            ConfigureText(inventoryText, 12f, 17f);
            ConfigureText(characterStatsText, 12f, 17f);
            ConfigureText(japaneseText, 20f, 28f);
            ConfigureText(translationText, 13f, 17f);
        }

        private static void ConfigureText(TextMeshProUGUI text, float min, float max)
        {
            if (text == null) return;
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
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

            if (GameServices.TryGet(out GameSettingsService settings))
            {
                settings.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void HandleLanguageChanged(GameLanguage language)
        {
            RefreshPlayerPanels();
            UpdateObjectivesDisplay();
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
                    inventoryText.text = Text("Balo trống", "Bag empty", "バッグは空です");
                }
                else
                {
                    var builder = new StringBuilder();
                    foreach (var item in inventory.Items)
                    {
                        string ja = string.IsNullOrEmpty(item.displayNameJa) ? item.itemId : item.displayNameJa;
                        string en = string.IsNullOrEmpty(item.displayNameEn) ? item.itemId : item.displayNameEn;
                        builder.AppendLine($"{ja} / {en}");
                        builder.AppendLine($"x{item.quantity}    ¥{item.priceYen}");
                    }
                    inventoryText.text = builder.ToString();
                }
            }

            if (characterStatsText != null)
            {
                characterStatsText.text =
                    Text("Học viên", "Learner", "学習者") + "\n" +
                    Text("Cấp độ: N5 Starter", "Level: N5 Starter", "レベル: N5 Starter") + "\n" +
                    Text("Di chuyển: đi bộ / chạy", "Movement: walk / run", "移動: 歩く / 走る") + "\n" +
                    $"{Text("Tiền mặt", "Cash", "所持金")}: ¥{inventory.Yen}\n" +
                    Text("Mục tiêu: mua hàng bằng tiếng Nhật", "Goal: shop in Japanese", "目標: 日本語で買い物する") + "\n" +
                    "N: Night  |  M: Morning";
            }
        }

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
            promptText.text = $"[E] {interactable.GetPromptJa()} / {interactable.GetpromptEn()}";
        }

        private void HidePrompt()
        {
            if (promptPanel != null) promptPanel.SetActive(false);
        }

        private void UpdateScenarioInfo(ScenarioDefinition scenario)
        {
            if (scenarioTitleText != null)
            {
                scenarioTitleText.text = $"{scenario.titleJa} / {scenario.titleEn}";
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
                objectivesText.text = string.Empty;
                if (scenarioTitleText != null) scenarioTitleText.text = Text("Không có nhiệm vụ", "No active mission", "ミッションなし");
                return;
            }

            var builder = new StringBuilder();
            foreach (var obj in ScenarioManager.Instance.Objectives)
            {
                if (obj.state == ObjectiveState.Inactive) continue;

                string check = obj.state == ObjectiveState.Completed ? "✓" : "☐";
                string color = obj.state == ObjectiveState.Completed ? "#74d680" : "#f5f2e8";
                builder.AppendLine($"<color={color}>{check} {obj.titleJa} ({obj.titleEn})</color>");
            }

            objectivesText.text = builder.ToString();
        }

        private void DisplayDialogue(DialogueDisplayData data)
        {
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
                        btnText.text = data.learningMode == LearningMode.GuidedPractice
                            ? $"{choice.textJa}\n<size=80%><color=#8fa3b8>({choice.textEn})</color></size>"
                            : choice.textJa;
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
                    SetHintText(readingText, data.textReading, true);
                    SetHintText(romajiText, data.textRomaji, true);
                    SetHintText(translationText, data.textEn, true);
                    break;
                case LearningMode.Practice:
                    SetHintText(readingText, data.textReading, true);
                    SetHintText(romajiText, string.Empty, false);
                    SetHintText(translationText, data.textEn, true);
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

        private static string Text(string vi, string en, string ja)
        {
            return GameServices.TryGet(out GameSettingsService settings) ? settings.Text(vi, en, ja) : vi;
        }
    }
}
