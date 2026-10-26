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
    public partial class HUDUI : MonoBehaviour
    {
        [Header("Interaction Tooltip")]
        [SerializeField] private GameObject promptPanel;
        private TextMeshProUGUI _keyCap;
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
        private StatusDock _statusDock;
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
        private TextMeshProUGUI _inventoryDetailText;
        private int _selectedInventoryIndex;
        private IInteractable _currentInteractable;
        private DialogueView _dialogueView;
        private InventoryWindow _inventoryWindow;

        private DialogueManager _boundDialogue;

        /// <summary>Subscribes to the dialogue manager and shows a dialogue that was already open before the HUD
        /// existed — otherwise the dock hides for a dialogue nobody can see and the screen stays empty.</summary>
        private void BindDialogue()
        {
            var manager = DialogueManager.Instance;
            if (manager == null || manager == _boundDialogue) return;
            if (_boundDialogue != null)
            {
                _boundDialogue.OnDialogueUpdated -= DisplayDialogue;
                _boundDialogue.OnDialogueClosed -= HideDialogue;
            }
            _boundDialogue = manager;
            manager.OnDialogueUpdated += DisplayDialogue;
            manager.OnDialogueClosed += HideDialogue;
            if (manager.IsOpen && (dialoguePanel == null || !dialoguePanel.activeSelf)) manager.RefreshView();
        }

        private void Start()
        {
            EnsureDialogueView();
            EnsureInventoryWindow();
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var detector = player.GetComponent<InteractionDetector>();
                if (detector != null)
                {
                    detector.OnInteractableChanged += HandleInteractableChanged;
                }
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
            BindDialogue(); // after HideDialogue: re-shows a dialogue that opened while the scene was loading
            HidePrompt();
            EnsureQuestMarker();
            WireMissionPanelClick();
            EnsureNameplateToggle();
            RepairRuntimeLayout();
            ConfigureResponsiveText();
            EnsureInventoryActions();
            EnsureOnlineChatPanel();
            _worldMap = gameObject.AddComponent<WorldMapUI>();
            _worldMap.Initialize(scenarioTitleText != null ? scenarioTitleText.font : null);
            _worldMap.OnVisibilityChanged += _ => UpdateOverlayInputLock();
            _settingsUI = gameObject.AddComponent<SettingsUI>();
            _settingsUI.Initialize(scenarioTitleText != null ? scenarioTitleText.font : null);
            EnsureStatusDock();
            UnifyHudStyle();
            SetInventoryVisible(false);
            SetCharacterVisible(false);
            SetChatVisible(false);
            RefreshPlayerPanels();
            RefreshOnlineStatus();
            UpdateObjectivesDisplay();
            EnsureTutorial();
            EnsureQuestLog();
            EnsureExamCenter();
            EnsureStudyHub();
            EnsureMobileControls();
        }



        private void Update()
        {
            BindDialogue();
            UpdateOverviewControls();
            // Prompt lane: the [F] prompt steps aside while a timed action runs or a window is open, and comes back after.
            bool suppressPrompt = TimedAction.Busy || UiModalStack.AnyOpen;
            if (promptPanel != null)
            {
                if (suppressPrompt && promptPanel.activeSelf) HidePrompt();
                else if (!suppressPrompt && !promptPanel.activeSelf && _currentInteractable != null && (_currentInteractable as UnityEngine.Object) != null) ShowPrompt(_currentInteractable);
            }
            var input = GameInputService.GetOrCreate();
            bool dialogueOpen = dialoguePanel != null && dialoguePanel.activeSelf;

            RegisterModalPanels();
            bool socialOpen = SocialChatWindow.IsOpen || SocialChatWindow.CardOpen;
            if (socialOpen != _socialWasOpen)
            {
                // Closed with Esc / × or opened by F on a player: keep the movement lock in step.
                _socialWasOpen = socialOpen;
                UpdateOverlayInputLock();
            }
            if (UiModalStack.BlocksHotkeys) return;               // exam / text field: no B, M, Tab, O…

            if (!UiModalStack.BlocksHotkeys && input.WasPressed(GameInputId.Journal) && (dialoguePanel == null || !dialoguePanel.activeSelf))
                TaskJournalUI.Toggle();
            if (!UiModalStack.IsTyping && input.WasPressed(GameInputId.Settings))
            {
                if (_settingsUI != null && _settingsUI.IsOpen) _settingsUI.Hide();
                else if (_settingsUI != null && !dialogueOpen) _settingsUI.Show();
            }

            if (!dialogueOpen && input.WasPressed(GameInputId.Inventory))
            {
                SetInventoryVisible(inventoryPanel != null && !inventoryPanel.activeSelf);
            }

            if (inventoryPanel != null && inventoryPanel.activeSelf)
            {
                HandleInventoryShortcuts();
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
            if (input.WasPressed(GameInputId.Social) && (dialoguePanel == null || !dialoguePanel.activeSelf))
            {
                if (SocialChatWindow.IsOpen && SocialChatWindow.Instance.CurrentTab == SocialChatWindow.Tab.Friends) SetChatVisible(false);
                else { SetChatVisible(true); SocialChatWindow.Instance.SelectTab(SocialChatWindow.Tab.Friends); }
            }

            HandleDialogueKeyboard();
        }

        private bool _modalPanelsRegistered;

        /// <summary>Esc for the HUD's own panels goes through UiModalStack like every other overlay; with nothing
        /// open Esc does nothing (Settings is on O).</summary>
        private void RegisterModalPanels()
        {
            if (_modalPanelsRegistered) return;
            _modalPanelsRegistered = true;
            UiModalStack.Register(this, () => inventoryPanel != null && inventoryPanel.activeSelf, () => { SetInventoryVisible(false); UpdateOverlayInputLock(); }, "Bag");
            UiModalStack.Register(this, () => characterPanel != null && characterPanel.activeSelf, () => { SetCharacterVisible(false); UpdateOverlayInputLock(); }, "Character");
            UiModalStack.Register(this, () => _worldMap != null && _worldMap.IsVisible, () => { _worldMap.SetVisible(false); UpdateOverlayInputLock(); }, "Map");
        }

        private void HandleDialogueKeyboard()
        {
            if (dialoguePanel == null || !dialoguePanel.activeSelf) return;
            if (_dialogueView != null) return; // DialogueView handles its own keys.
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
            if (_currentInteractable != null) ShowPrompt(_currentInteractable);
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
