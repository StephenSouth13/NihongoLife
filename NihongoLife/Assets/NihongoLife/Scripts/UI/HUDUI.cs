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
        private int _selectedChoiceIndex = -1;

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
            RepairRuntimeLayout();
            ConfigureResponsiveText();
            SetInventoryVisible(false);
            SetCharacterVisible(false);
            RefreshPlayerPanels();
            UpdateObjectivesDisplay();
        }

        private void ConfigureResponsiveText()
        {
            ConfigureText(scenarioTitleText, 14f, 18f);
            ConfigureText(objectivesText, 11f, 15f);
            ConfigureText(promptText, 13f, 18f);
            ConfigureText(inventoryText, 12f, 17f);
            ConfigureText(characterStatsText, 12f, 17f);
            ConfigureText(japaneseText, 20f, 28f);
            ConfigureText(readingText, 12f, 16f);
            ConfigureText(romajiText, 12f, 16f);
            ConfigureText(translationText, 13f, 17f);
        }

        private void RepairRuntimeLayout()
        {
            if (scenarioTitleText != null)
            {
                RectTransform missionPanel = scenarioTitleText.transform.parent as RectTransform;
                if (missionPanel != null)
                {
                    missionPanel.anchorMin = new Vector2(0f, 1f);
                    missionPanel.anchorMax = new Vector2(0f, 1f);
                    missionPanel.pivot = new Vector2(0f, 1f);
                    missionPanel.anchoredPosition = new Vector2(24f, -28f);
                    missionPanel.sizeDelta = new Vector2(620f, 188f);
                }

                SetTopLeft(scenarioTitleText.rectTransform, new Vector2(18f, -14f), new Vector2(584f, 34f));
            }

            if (objectivesText != null)
            {
                SetTopLeft(objectivesText.rectTransform, new Vector2(18f, -54f), new Vector2(584f, 112f));
                objectivesText.lineSpacing = 8f;
                objectivesText.paragraphSpacing = 4f;
            }

            if (dialoguePanel != null)
            {
                RectTransform rect = dialoguePanel.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = new Vector2(0f, 90f);
                    rect.sizeDelta = new Vector2(980f, 332f);
                }
            }

            if (speakerText != null) SetTopLeft(speakerText.rectTransform, new Vector2(24f, -18f), new Vector2(920f, 28f));
            if (japaneseText != null) SetTopLeft(japaneseText.rectTransform, new Vector2(24f, -56f), new Vector2(920f, 50f));
            if (readingText != null) SetTopLeft(readingText.rectTransform, new Vector2(24f, -106f), new Vector2(920f, 28f));
            if (romajiText != null) SetTopLeft(romajiText.rectTransform, new Vector2(24f, -136f), new Vector2(920f, 28f));
            if (translationText != null) SetTopLeft(translationText.rectTransform, new Vector2(24f, -166f), new Vector2(920f, 70f));
        }

        private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
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

            HandleDialogueKeyboard();
        }

        private void HandleDialogueKeyboard()
        {
            if (dialoguePanel == null || !dialoguePanel.activeSelf) return;

            if (_activeChoiceButtons.Count > 0)
            {
                if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
                {
                    SelectChoiceVisual(_selectedChoiceIndex + 1);
                }
                else if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
                {
                    SelectChoiceVisual(_selectedChoiceIndex - 1);
                }
                else if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    OnChoiceSelected(Mathf.Clamp(_selectedChoiceIndex, 0, _activeChoiceButtons.Count - 1));
                }
            }
            else if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                OnContinueClicked();
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
                scenarioTitleText.text = Text(scenario.titleEn, scenario.titleEn, scenario.titleJa);
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
                builder.AppendLine($"<color={color}>{check} {Text(obj.titleEn, obj.titleEn, obj.titleJa)}</color>");
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

        private static string Text(string vi, string en, string ja)
        {
            return GameServices.TryGet(out GameSettingsService settings) ? settings.Text(vi, en, ja) : vi;
        }
    }
}
