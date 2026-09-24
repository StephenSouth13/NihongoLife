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
        private WorldMapUI _worldMap;
        private SettingsUI _settingsUI;
        private bool _objectivesExpanded = true;

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
            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.OnStatusChanged += RefreshPlayerPanels;
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
            _worldMap = gameObject.AddComponent<WorldMapUI>();
            _worldMap.Initialize(scenarioTitleText != null ? scenarioTitleText.font : null);
            _worldMap.OnVisibilityChanged += _ => UpdateOverlayInputLock();
            _settingsUI = gameObject.AddComponent<SettingsUI>();
            _settingsUI.Initialize(scenarioTitleText != null ? scenarioTitleText.font : null);
            SetInventoryVisible(false);
            SetCharacterVisible(false);
            SetChatVisible(false);
            RefreshPlayerPanels();
            RefreshOnlineStatus();
            UpdateObjectivesDisplay();
            EnsureTutorial();
            EnsureQuestLog();
            EnsureExamCenter();
            EnsureMobileControls();
        }

        /// <summary>
        /// On-screen joystick + Interact/Jump buttons for touch devices. Skipped entirely on desktop
        /// (HudCanvasFitter.IsTouchLayout already governs HUD sizing the same way). The input plumbing
        /// itself (GameInputService.SetMobileMove/SetMobileButton, consumed by PlayerController) already
        /// existed and worked — MobileJoystick/MobileActionButton (Scripts/UI/MobileGameControls.cs) were
        /// written but never actually placed anywhere, so touch devices had movement/interact logic with
        /// no visible control to drive it. This is the missing visual half.
        /// Menus that are keyboard-shortcut-only (inventory B, map M, quest log J, exam center K, character
        /// Tab, settings Esc) still have no on-screen button — out of scope here, flagged separately.
        /// </summary>
        private void EnsureMobileControls()
        {
            if (!HudCanvasFitter.IsTouchLayout(false)) return;
            if (transform.Find("MobileControls") != null) return;

            TMP_FontAsset font = scenarioTitleText != null ? scenarioTitleText.font : null;

            var root = new GameObject("MobileControls", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

            RectTransform joyBase = CreateMobileCircle(root.transform, "Joystick_Base", new Vector2(0f, 0f), new Vector2(24f, 24f), 160f, new Color(1f, 1f, 1f, 0.14f));
            RectTransform joyHandle = CreateMobileCircle(joyBase, "Joystick_Handle", new Vector2(0.5f, 0.5f), Vector2.zero, 70f, new Color(1f, 1f, 1f, 0.32f));
            joyBase.gameObject.AddComponent<MobileJoystick>().Configure(joyHandle, 45f);

            RectTransform interactButton = CreateMobileCircle(root.transform, "Button_Interact", new Vector2(1f, 0f), new Vector2(-24f, 24f), 120f, new Color(0.95f, 0.72f, 0.25f, 0.55f));
            interactButton.gameObject.AddComponent<MobileActionButton>().Configure(GameInputId.Interact);
            CreateMobileLabel(interactButton, "E", font);

            RectTransform jumpButton = CreateMobileCircle(root.transform, "Button_Jump", new Vector2(1f, 0f), new Vector2(-24f, 160f), 90f, new Color(1f, 1f, 1f, 0.28f));
            jumpButton.gameObject.AddComponent<MobileActionButton>().Configure(GameInputId.Jump);
            CreateMobileLabel(jumpButton, "JUMP", font);
        }

        private static RectTransform CreateMobileCircle(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, float diameter, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(diameter, diameter);
            var image = go.GetComponent<Image>();
            image.sprite = UIStyleKit.RoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = true;
            return rect;
        }

        private static void CreateMobileLabel(RectTransform parent, string value, TMP_FontAsset font)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = 24f;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(1f, 1f, 1f, 0.85f);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }

        private void EnsureQuestLog()
        {
            var questLog = GetComponent<QuestLogPopup>() ?? gameObject.AddComponent<QuestLogPopup>();
            questLog.SetDialoguePanel(dialoguePanel);
            questLog.Initialize(scenarioTitleText != null ? scenarioTitleText.font : null);
        }

        private void EnsureExamCenter()
        {
            var examCenter = GetComponent<ExamCenterPopup>() ?? gameObject.AddComponent<ExamCenterPopup>();
            examCenter.Initialize(scenarioTitleText != null ? scenarioTitleText.font : null);
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

            if (speakerText != null)
            {
                speakerText.fontStyle = FontStyles.Bold;
                speakerText.color = new Color(0.95f, 0.76f, 0.30f, 1f);
            }
            if (japaneseText != null)
            {
                japaneseText.fontStyle = FontStyles.Bold;
                japaneseText.color = new Color(0.98f, 0.98f, 0.96f, 1f);
                japaneseText.lineSpacing = 7f;
            }
            if (readingText != null) readingText.color = new Color(0.54f, 0.80f, 0.92f, 1f);
            if (romajiText != null)
            {
                romajiText.fontStyle = FontStyles.Italic;
                romajiText.color = new Color(0.68f, 0.73f, 0.78f, 1f);
            }
            if (translationText != null)
            {
                translationText.color = new Color(0.91f, 0.94f, 0.96f, 1f);
                translationText.lineSpacing = 6f;
            }
        }

        /// <summary>
        /// HUD positions and sizes are authored in the scene (Canvas > SafeArea > HUDPanel) and scaled to
        /// the device by HudCanvasFitter / HudSafeArea / HudFitRect, so nothing is repositioned here.
        /// Only colours are normalised for the panels that other code toggles at runtime.
        /// </summary>
        private void RepairRuntimeLayout()
        {
            StyleInfoPanelColor(inventoryPanel);
            StyleInfoPanelColor(characterPanel);
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
            if (chatPanel.GetComponent<HudFitRect>() == null)
            {
                chatPanel.AddComponent<HudFitRect>().Configure(0.9f, 0.6f, new Vector2(48f, 48f), 0.5f);
            }

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
            text.characterSpacing = 0f;
            text.wordSpacing = 0f;
            text.lineSpacing = 4f;
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
            button.onClick.RemoveListener(ToggleObjectivesPanel);
            button.onClick.AddListener(ToggleObjectivesPanel);
        }

        private void ShowQuestTarget()
        {
            EnsureQuestMarker();
            _questMarker.ShowCurrentObjectiveTarget();
        }

        private void ToggleObjectivesPanel()
        {
            _objectivesExpanded = !_objectivesExpanded;
            if (objectivesText != null) objectivesText.gameObject.SetActive(_objectivesExpanded);
            if (scenarioTitleText != null)
            {
                string title = scenarioTitleText.text.Replace("  ˅", string.Empty).Replace("  ˄", string.Empty);
                scenarioTitleText.text = title + (_objectivesExpanded ? "  ˄" : "  ˅");
            }
        }

        private static void StyleInfoPanel(GameObject panel, Vector2 anchor, Vector2 position, Vector2 size)
        {
            if (panel == null) return;

            var rect = panel.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = anchor;
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
            }

            var image = panel.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.028f, 0.038f, 0.048f, 0.94f);
            }
        }

        private static void StyleInfoPanelColor(GameObject panel)
        {
            if (panel == null) return;
            var image = panel.GetComponent<Image>();
            if (image != null) image.color = new Color(0.028f, 0.038f, 0.048f, 0.94f);
        }

        private void Update()
        {
            var input = GameInputService.GetOrCreate();
            bool dialogueOpen = dialoguePanel != null && dialoguePanel.activeSelf;

            if (chatPanel != null && chatPanel.activeSelf)
            {
                if (input.WasPressed(GameInputId.Pause))
                {
                    SetChatVisible(false);
                }

                return;
            }

            if (!dialogueOpen && input.WasPressed(GameInputId.Inventory))
            {
                SetInventoryVisible(inventoryPanel != null && !inventoryPanel.activeSelf);
            }

            if (!dialogueOpen && input.WasPressed(GameInputId.Character))
            {
                SetCharacterVisible(characterPanel != null && !characterPanel.activeSelf);
            }

            if (!dialogueOpen && input.WasPressed(GameInputId.Map))
            {
                bool show = _worldMap != null && !_worldMap.IsVisible;
                SetInventoryVisible(false);
                SetCharacterVisible(false);
                SetChatVisible(false);
                _worldMap?.SetVisible(show);
                UpdateOverlayInputLock();
            }

            if (input.WasPressed(GameInputId.Chat) && dialoguePanel != null && !dialoguePanel.activeSelf)
            {
                SetChatVisible(true);
            }

            if (input.WasPressed(GameInputId.Pause))
            {
                SetChatVisible(false);
                _worldMap?.SetVisible(false);
                _settingsUI?.ToggleFromEscape();
                UpdateOverlayInputLock();
            }

            HandleDialogueKeyboard();
        }

        private void HandleDialogueKeyboard()
        {
            if (dialoguePanel == null || !dialoguePanel.activeSelf) return;
            if (Keyboard.current == null) return;

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
            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.OnStatusChanged -= RefreshPlayerPanels;
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
                _worldMap?.SetVisible(false);
                SetCharacterVisible(false);
                SetChatVisible(false);
                RefreshPlayerPanels();
            }
            UpdateOverlayInputLock();
        }

        private void SetCharacterVisible(bool visible)
        {
            if (characterPanel == null) return;
            characterPanel.SetActive(visible);
            if (visible)
            {
                _worldMap?.SetVisible(false);
                SetInventoryVisible(false);
                SetChatVisible(false);
                RefreshPlayerPanels();
            }
            UpdateOverlayInputLock();
        }

        private void SetChatVisible(bool visible)
        {
            if (chatPanel == null) return;
            chatPanel.SetActive(visible);

            if (visible)
            {
                _worldMap?.SetVisible(false);
                SetInventoryVisible(false);
                SetCharacterVisible(false);
                RefreshChatHistory();
                chatInputField?.ActivateInputField();
            }
            else
            {
                chatInputField?.DeactivateInputField();
            }
            UpdateOverlayInputLock();
        }

        private void UpdateOverlayInputLock()
        {
            bool overlayOpen = (inventoryPanel != null && inventoryPanel.activeSelf) ||
                               (characterPanel != null && characterPanel.activeSelf) ||
                               (chatPanel != null && chatPanel.activeSelf) ||
                               (_worldMap != null && _worldMap.IsVisible);
            bool dialogueOpen = dialoguePanel != null && dialoguePanel.activeSelf;
            ScenarioManager.Instance?.SetPlayerInputLocked(overlayOpen || dialogueOpen);
            Cursor.lockState = overlayOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = overlayOpen;
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
                inventoryText.text = BuildInventoryGrid(inventory);
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
                inventoryText.text = BuildInventoryGrid(inventory);
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

                PlayerStatus status = PlayerStatus.Instance;
                EmploymentSystem employment = FindFirstObjectByType<EmploymentSystem>();
                BusinessSystem businessSystem = FindFirstObjectByType<BusinessSystem>();
                string careerSummary = employment != null && employment.CurrentJob.HasValue
                    ? $"<color=#78c7d4>Career</color>: {employment.CurrentJob} · {employment.CurrentRank} · {employment.CurrentCompletedShifts} shifts\n"
                    : "<color=#78c7d4>Career</color>: Not employed\n";
                string businessSummary = businessSystem != null && businessSystem.OwnsCompany
                    ? $"<color=#7fd39b>Company</color>: {businessSystem.Business.companyName} · Rep {businessSystem.Business.reputation}\n"
                    : string.Empty;
                characterStatsText.text = careerSummary + businessSummary +
                    $"<size=125%><b>{Text("Hồ sơ học viên", "Learner Profile", "学習者プロフィール")}</b></size>\n" +
                    $"<color=#f1c75b>{Text("Tên", "Name", "名前")}</color>: {learnerName}\n" +
                    $"<color=#f1c75b>{Text("Cấp độ", "Level", "レベル")}</color>: N5 · Lv.{level}\n" +
                    (status != null
                        ? $"{StatLine(Text("Máu", "Health", "体力"), status.CurrentHealth, status.MaxHealth, "#e35d6a")}\n" +
                          $"{StatLine(Text("Năng lượng", "Energy", "元気"), status.CurrentEnergy, status.MaxEnergy, "#64b5f6")}\n" +
                          $"{StatLine(Text("No", "Hunger", "満腹"), status.Hunger, 100f, "#f2b84b")}\n" +
                          $"{StatLine(Text("Khát", "Thirst", "水分"), status.Thirst, 100f, "#4dd0c8")}\n" +
                          $"<color=#b79cff>{Text("Kiến thức", "Knowledge", "知識")}: {status.Knowledge}</color>\n"
                        : string.Empty) +
                    $"<color=#f1c75b>{Text("Tiền mặt", "Cash", "所持金")}</color>: ¥{inventory.Yen}\n" +
                    $"<color=#f1c75b>{Text("Mục tiêu", "Goal", "目標")}</color>: {goalText}\n\n" +
                    "<color=#8fa3b8>Tab: profile  |  B: bag  |  V: mic  |  E: talk</color>";
            }
        }

        private static string BuildInventoryGrid(PlayerInventory inventory)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"<size=125%><b>{Text("Balo", "Bag", "バッグ")}</b></size>  <color=#8fa3b8>{inventory.UsedSlots}/{inventory.MaxSlots} slots</color>");
            for (int i = 0; i < inventory.MaxSlots; i++)
            {
                if (i < inventory.Items.Count)
                {
                    var item = inventory.Items[i];
                    string name = string.IsNullOrEmpty(item.displayNameJa) ? item.displayNameEn : item.displayNameJa;
                    string kind = item.useType == ItemUseType.Food ? " [FOOD]" : item.useType == ItemUseType.Drink ? " [DRINK]" : string.Empty;
                    builder.Append($"<color=#f1c75b>[{i + 1:00}]</color> <b>{name}</b>{kind}  x{item.quantity}/{inventory.MaxStackSize}");
                }
                else
                {
                    builder.Append($"<color=#536170>[{i + 1:00}]  --</color>");
                }
                builder.AppendLine();
            }
            return builder.ToString();
        }

        private static string StatLine(string label, float value, float max, string color)
        {
            const int segments = 10;
            int filled = Mathf.RoundToInt(Mathf.Clamp01(value / Mathf.Max(1f, max)) * segments);
            string bar = new string('|', filled) + new string('.', segments - filled);
            return $"<color={color}>{label} [{bar}] {value:0}/{max:0}</color>";
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
            string key = GameInputService.GetOrCreate().GetBindingLabel(GameInputId.Interact);
            bool japanese = GameServices.TryGet(out GameSettingsService settings)
                && settings.Language == GameLanguage.Japanese;
            promptText.text = $"[{key}] {(japanese ? interactable.GetPromptJa() : interactable.GetpromptEn())}";
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
