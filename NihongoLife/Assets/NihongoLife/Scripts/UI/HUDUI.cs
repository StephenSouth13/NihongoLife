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

        [Header("Online Simulation")]
        [SerializeField] private GameObject chatPanel;
        [SerializeField] private TextMeshProUGUI chatHistoryText;
        [SerializeField] private TMP_InputField chatInputField;
        [SerializeField] private TextMeshProUGUI onlineStatusText;

        private readonly List<Button> _activeChoiceButtons = new List<Button>();
        private int _selectedChoiceIndex = -1;
        private QuestDirectionMarker _questMarker;
        private IOnlineWorldService _onlineWorld;

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

            if (GameServices.TryGet(out _onlineWorld))
            {
                _onlineWorld.OnChatMessageReceived += HandleChatMessageReceived;
                _onlineWorld.OnPlayerJoinedOrUpdated += HandlePlayerPresenceChanged;
                _onlineWorld.OnPlayerLeft += HandlePlayerLeft;
            }

            HideDialogue();
            HidePrompt();
            EnsureQuestMarker();
            WireMissionPanelClick();
            RepairRuntimeLayout();
            ConfigureResponsiveText();
            EnsureOnlineChatPanel();
            SetInventoryVisible(false);
            SetCharacterVisible(false);
            SetChatVisible(false);
            RefreshPlayerPanels();
            RefreshOnlineStatus();
            UpdateObjectivesDisplay();
            EnsureTutorial();
        }

        private void EnsureTutorial()
        {
            var tutorial = gameObject.AddComponent<TutorialUI>();
            TMP_FontAsset font = scenarioTitleText != null ? scenarioTitleText.font : null;
            tutorial.Initialize(font);
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
            ConfigureText(chatHistoryText, 10f, 13f);
            ConfigureText(onlineStatusText, 11f, 14f);
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

            if (onlineStatusText != null)
            {
                SetTopLeft(onlineStatusText.rectTransform, new Vector2(24f, -228f), new Vector2(620f, 28f));
            }

            StyleInfoPanel(inventoryPanel, new Vector2(1f, 1f), new Vector2(-28f, -88f), new Vector2(420f, 390f));
            StyleInfoPanel(characterPanel, new Vector2(1f, 1f), new Vector2(-28f, -88f), new Vector2(420f, 310f));
            StyleInfoPanel(chatPanel, new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(470f, 250f));

            if (dialoguePanel != null)
            {
                RectTransform rect = dialoguePanel.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = new Vector2(0f, 74f);
                    rect.sizeDelta = new Vector2(1120f, 378f);
                }
            }

            if (speakerText != null) SetTopLeft(speakerText.rectTransform, new Vector2(28f, -20f), new Vector2(1048f, 30f));
            if (japaneseText != null) SetTopLeft(japaneseText.rectTransform, new Vector2(28f, -58f), new Vector2(1048f, 58f));
            if (readingText != null) SetTopLeft(readingText.rectTransform, new Vector2(28f, -116f), new Vector2(1048f, 30f));
            if (romajiText != null) SetTopLeft(romajiText.rectTransform, new Vector2(28f, -148f), new Vector2(1048f, 30f));
            if (translationText != null) SetTopLeft(translationText.rectTransform, new Vector2(28f, -180f), new Vector2(1048f, 92f));

            if (choicesContainer is RectTransform choicesRect)
            {
                choicesRect.anchorMin = new Vector2(0f, 0f);
                choicesRect.anchorMax = new Vector2(1f, 0f);
                choicesRect.pivot = new Vector2(0.5f, 0f);
                choicesRect.anchoredPosition = new Vector2(0f, 18f);
                choicesRect.sizeDelta = new Vector2(-56f, 72f);
            }

            if (continueButton != null)
            {
                var continueRect = continueButton.GetComponent<RectTransform>();
                if (continueRect != null)
                {
                    continueRect.anchorMin = new Vector2(1f, 0f);
                    continueRect.anchorMax = new Vector2(1f, 0f);
                    continueRect.pivot = new Vector2(1f, 0f);
                    continueRect.anchoredPosition = new Vector2(-28f, 18f);
                    continueRect.sizeDelta = new Vector2(180f, 52f);
                }
            }
        }

        private void EnsureOnlineChatPanel()
        {
            TMP_FontAsset font = scenarioTitleText != null ? scenarioTitleText.font : null;

            if (onlineStatusText == null)
            {
                onlineStatusText = CreateHudText("OnlineStatusText", transform, new Vector2(24f, -228f), new Vector2(620f, 28f), 13f, font);
                onlineStatusText.alignment = TextAlignmentOptions.Left;
                onlineStatusText.color = new Color(0.58f, 0.72f, 0.86f, 1f);
            }

            if (chatPanel == null)
            {
                chatPanel = new GameObject("OnlineChatPanel");
                chatPanel.transform.SetParent(transform, false);
                chatPanel.AddComponent<Image>().color = new Color(0.025f, 0.035f, 0.045f, 0.94f);
            }

            StyleInfoPanel(chatPanel, new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(470f, 250f));

            if (chatHistoryText == null)
            {
                chatHistoryText = CreateHudText("ChatHistory", chatPanel.transform, new Vector2(16f, -14f), new Vector2(438f, 176f), 12f, font);
                chatHistoryText.alignment = TextAlignmentOptions.TopLeft;
                chatHistoryText.color = new Color(0.91f, 0.96f, 1f, 1f);
            }

            if (chatInputField == null)
            {
                var inputObject = new GameObject("ChatInput");
                inputObject.transform.SetParent(chatPanel.transform, false);
                var inputRect = inputObject.AddComponent<RectTransform>();
                inputRect.anchorMin = new Vector2(0f, 0f);
                inputRect.anchorMax = new Vector2(1f, 0f);
                inputRect.pivot = new Vector2(0.5f, 0f);
                inputRect.anchoredPosition = new Vector2(0f, 14f);
                inputRect.sizeDelta = new Vector2(-32f, 42f);

                var inputImage = inputObject.AddComponent<Image>();
                inputImage.color = new Color(0.11f, 0.13f, 0.15f, 1f);

                chatInputField = inputObject.AddComponent<TMP_InputField>();
                chatInputField.textViewport = inputRect;
                chatInputField.lineType = TMP_InputField.LineType.SingleLine;
                chatInputField.characterLimit = 120;

                var text = CreateHudText("Text", inputObject.transform, Vector2.zero, new Vector2(408f, 34f), 14f, font);
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.margin = new Vector4(10f, 0f, 10f, 0f);
                chatInputField.textComponent = text;

                var placeholder = CreateHudText("Placeholder", inputObject.transform, Vector2.zero, new Vector2(408f, 34f), 14f, font);
                placeholder.alignment = TextAlignmentOptions.MidlineLeft;
                placeholder.margin = new Vector4(10f, 0f, 10f, 0f);
                placeholder.color = new Color(0.58f, 0.64f, 0.7f, 0.72f);
                placeholder.text = "Type a town chat message...";
                chatInputField.placeholder = placeholder;

                chatInputField.onSubmit.AddListener(SendChatMessage);
            }

            RefreshChatHistory();
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

        private void EnsureQuestMarker()
        {
            _questMarker = FindFirstObjectByType<QuestDirectionMarker>();
            if (_questMarker == null)
            {
                _questMarker = gameObject.AddComponent<QuestDirectionMarker>();
            }
        }

        private void WireMissionPanelClick()
        {
            if (scenarioTitleText == null) return;

            var missionPanel = scenarioTitleText.transform.parent;
            if (missionPanel == null) return;

            var image = missionPanel.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                image.color = new Color(0.025f, 0.035f, 0.045f, 0.88f);
            }

            var button = missionPanel.GetComponent<Button>();
            if (button == null) button = missionPanel.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.68f, 1f);
            colors.pressedColor = new Color(0.95f, 0.62f, 0.18f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.RemoveListener(ShowQuestTarget);
            button.onClick.AddListener(ShowQuestTarget);
        }

        private void ShowQuestTarget()
        {
            EnsureQuestMarker();
            _questMarker.ShowCurrentObjectiveTarget();
        }

        private static void StyleInfoPanel(GameObject panel, Vector2 anchor, Vector2 position, Vector2 size)
        {
            if (panel == null) return;

            var rect = panel.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
            }

            var image = panel.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.028f, 0.038f, 0.048f, 0.94f);
            }
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (chatPanel != null && chatPanel.activeSelf)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    SetChatVisible(false);
                }

                return;
            }

            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                SetInventoryVisible(inventoryPanel != null && !inventoryPanel.activeSelf);
            }

            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                SetCharacterVisible(characterPanel != null && !characterPanel.activeSelf);
            }

            if (Keyboard.current.enterKey.wasPressedThisFrame && dialoguePanel != null && !dialoguePanel.activeSelf)
            {
                SetChatVisible(true);
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetChatVisible(false);
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

            if (_onlineWorld != null)
            {
                _onlineWorld.OnChatMessageReceived -= HandleChatMessageReceived;
                _onlineWorld.OnPlayerJoinedOrUpdated -= HandlePlayerPresenceChanged;
                _onlineWorld.OnPlayerLeft -= HandlePlayerLeft;
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
                SetChatVisible(false);
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
                SetChatVisible(false);
                RefreshPlayerPanels();
            }
        }

        private void SetChatVisible(bool visible)
        {
            if (chatPanel == null) return;
            chatPanel.SetActive(visible);
            ScenarioManager.Instance?.SetPlayerInputLocked(visible);

            if (visible)
            {
                SetInventoryVisible(false);
                SetCharacterVisible(false);
                RefreshChatHistory();
                chatInputField?.ActivateInputField();
            }
            else
            {
                chatInputField?.DeactivateInputField();
            }
        }

        private void SendChatMessage(string message)
        {
            if (_onlineWorld == null || string.IsNullOrWhiteSpace(message)) return;

            _onlineWorld.SendChatMessage("town", message);
            if (chatInputField != null)
            {
                chatInputField.text = string.Empty;
                chatInputField.ActivateInputField();
            }
        }

        private void HandleChatMessageReceived(OnlineChatMessage message)
        {
            RefreshChatHistory();
        }

        private void HandlePlayerPresenceChanged(OnlinePlayerSnapshot player)
        {
            RefreshOnlineStatus();
        }

        private void HandlePlayerLeft(string playerId)
        {
            RefreshOnlineStatus();
        }

        private void RefreshOnlineStatus()
        {
            if (onlineStatusText == null) return;

            int onlineCount = _onlineWorld != null && _onlineWorld.IsConnected ? _onlineWorld.VisiblePlayers.Count : 0;
            bool isRealOnline = _onlineWorld is SupabaseOnlineWorldService;
            string modeLabel = isRealOnline ? "Online" : Text("Mô phỏng", "Simulation", "模擬");

            string status = _onlineWorld != null && _onlineWorld.IsConnected
                ? Text($"{modeLabel}: {onlineCount} người chơi  |  Enter: chat", $"{modeLabel}: {onlineCount} player(s)  |  Enter: chat", $"{modeLabel}: {onlineCount}人  |  Enter: チャット")
                : Text($"{modeLabel}: chưa kết nối", $"{modeLabel}: offline", $"{modeLabel}: オフライン");
            onlineStatusText.text = status;
        }

        private void RefreshChatHistory()
        {
            if (chatHistoryText == null) return;

            if (_onlineWorld == null || _onlineWorld.ChatHistory.Count == 0)
            {
                chatHistoryText.text = Text("Chưa có tin nhắn.", "No messages yet.", "メッセージはまだありません。");
                return;
            }

            var builder = new StringBuilder();
            int start = Mathf.Max(0, _onlineWorld.ChatHistory.Count - 8);
            for (int i = start; i < _onlineWorld.ChatHistory.Count; i++)
            {
                OnlineChatMessage message = _onlineWorld.ChatHistory[i];
                builder.Append("<color=#f1c75b>")
                    .Append(message.senderDisplayName)
                    .Append("</color>: ")
                    .Append(message.text)
                    .AppendLine();
            }

            chatHistoryText.text = builder.ToString();
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

            RenderPolishedPlayerPanels(inventory);
        }

        private void RenderPolishedPlayerPanels(PlayerInventory inventory)
        {
            if (walletText != null)
            {
                walletText.text = $"¥ {inventory.Yen}";
            }

            if (inventoryText != null)
            {
                if (inventory.Items.Count == 0)
                {
                    inventoryText.text =
                        $"<size=125%><b>{Text("Balo", "Bag", "バッグ")}</b></size>\n" +
                        $"<color=#8fa3b8>{Text("Chưa có vật phẩm. Nhấn E để nhặt hoặc mua đồ khi tới đúng điểm.", "No items yet. Press E to pick up or buy at the right spot.", "目的地でEキーを押して、拾う・買う練習をします。")}</color>";
                }
                else
                {
                    var builder = new StringBuilder();
                    builder.AppendLine($"<size=125%><b>{Text("Balo", "Bag", "バッグ")}</b></size>");
                    builder.AppendLine($"<color=#f1c75b>{Text("Vật phẩm đang có", "Current items", "持ち物")}</color>");
                    foreach (var item in inventory.Items)
                    {
                        string ja = string.IsNullOrEmpty(item.displayNameJa) ? item.itemId : item.displayNameJa;
                        string en = string.IsNullOrEmpty(item.displayNameEn) ? item.itemId : item.displayNameEn;
                        builder.AppendLine($"<b>{ja}</b>  <color=#8fa3b8>{en}</color>");
                        builder.AppendLine($"<color=#f5f2e8>x{item.quantity}</color>    <color=#f1c75b>¥{item.priceYen}</color>");
                        builder.AppendLine();
                    }
                    inventoryText.text = builder.ToString();
                }
            }

            if (characterStatsText != null)
            {
                string learnerName = "Học viên Nihongo";
                int level = 1;
                if (GameServices.TryGet(out Save.IProgressRepository progressRepo))
                {
                    var progress = progressRepo.GetProgress();
                    if (!string.IsNullOrWhiteSpace(progress.displayName)) learnerName = progress.displayName;
                    level = progress.level;
                }
                if (GameServices.TryGet(out IAuthService authSvc) && authSvc.IsAuthenticated && !string.IsNullOrWhiteSpace(authSvc.DisplayName))
                {
                    learnerName = authSvc.DisplayName;
                }

                string goalText = Text("Chưa có nhiệm vụ", "No active goal", "目標なし");
                if (ScenarioManager.Instance != null && ScenarioManager.Instance.Objectives.Count > 0)
                {
                    var activeObjective = ScenarioManager.Instance.Objectives.Find(o => o.state == ObjectiveState.Active)
                        ?? ScenarioManager.Instance.Objectives[0];
                    goalText = Text(activeObjective.titleEn, activeObjective.titleEn, activeObjective.titleJa);
                }

                characterStatsText.text =
                    $"<size=125%><b>{Text("Hồ sơ học viên", "Learner Profile", "学習者プロフィール")}</b></size>\n" +
                    $"<color=#f1c75b>{Text("Tên", "Name", "名前")}</color>: {learnerName}\n" +
                    $"<color=#f1c75b>{Text("Cấp độ", "Level", "レベル")}</color>: N5 · Lv.{level}\n" +
                    $"<color=#f1c75b>{Text("Tiền mặt", "Cash", "所持金")}</color>: ¥{inventory.Yen}\n" +
                    $"<color=#f1c75b>{Text("Mục tiêu", "Goal", "目標")}</color>: {goalText}\n\n" +
                    "<color=#8fa3b8>Tab: profile  |  B: bag  |  V: mic  |  E: talk</color>";
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
                string check = obj.state == ObjectiveState.Completed ? "[x]" : obj.state == ObjectiveState.Failed ? "[!]" : "[ ]";
                string color = obj.state switch
                {
                    ObjectiveState.Completed => "#74d680",
                    ObjectiveState.Failed => "#ff7676",
                    ObjectiveState.Active => "#f5f2e8",
                    _ => "#8fa3b8"
                };
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
                        ConfigureText(btnText, 11f, 16f);
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

        private static TextMeshProUGUI CreateHudText(string name, Transform parent, Vector2 position, Vector2 size, float fontSize, TMP_FontAsset font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = go.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = fontSize;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }
    }
}
