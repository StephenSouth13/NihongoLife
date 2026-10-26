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
    /// <summary>Partial of HUDUI: runtime layout repair, mobile controls, and panel styling. The MonoBehaviour entry point stays HUDUI.cs.</summary>
    public partial class HUDUI : MonoBehaviour
    {
        private void EnsureMapButton()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null || canvas.transform.Find("MapButton") != null) return;
            var go = new GameObject("MapButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(24f, 24f);
            rect.sizeDelta = new Vector2(160f, 42f);
            var image = go.GetComponent<Image>();
            image.color = new Color(.035f, .09f, .12f, .95f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => _worldMap.SetVisible(!_worldMap.IsVisible));
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(go.transform, false);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            if (scenarioTitleText != null) label.font = scenarioTitleText.font;
            label.text = "M | B\u1ea3n \u0111\u1ed3";
            label.fontSize = 17f;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>Vitals card + clickable action bar (bag, character, map, quests, exams, settings) and the
        /// new character window, shared by every zone because the city's HUD stays loaded underneath.</summary>
        private void EnsureStatusDock()
        {
            if (_statusDock != null) return;
            var oldMapButton = GetComponentInParent<Canvas>()?.transform.Find("MapButton");
            if (oldMapButton != null) Destroy(oldMapButton.gameObject);
            TMP_FontAsset font = scenarioTitleText != null ? scenarioTitleText.font : null;
            var buttons = new List<(string, string, string, System.Action)>
            {
                ("BagButton", "B", "Balo", () => SetInventoryVisible(inventoryPanel != null && !inventoryPanel.activeSelf)),
                ("CharacterButton", "Tab", "Nhân vật", () => SetCharacterVisible(characterPanel != null && !characterPanel.activeSelf)),
                ("MapButton", "M", "Bản đồ", () =>
                {
                    bool show = _worldMap != null && !_worldMap.IsVisible;
                    SetInventoryVisible(false);
                    SetCharacterVisible(false);
                    _worldMap?.SetVisible(show);
                    UpdateOverlayInputLock();
                }),
                ("QuestButton", "N", "Nhiệm vụ", () => TaskJournalUI.Toggle()),
                ("ExamButton", "K", "Luyện thi", () => TogglePopup(GetComponent<ExamCenterPopup>())),
                ("StudyButton", "U", "Học thêm", () => TogglePopup(GetComponent<StudyHubPopup>())),
                ("NotebookButton", "L", "Sổ tay", () => NotebookUI.Toggle()),
                ("OverviewButton", "H", "Góc nhìn", ToggleOverviewCamera),
                ("SettingsButton", "O", "Cài đặt", () => _settingsUI?.ToggleFromEscape()),
            };
            _statusDock = StatusDock.Create(transform, font, buttons, () => SetCharacterVisible(false));
            if (characterPanel != null) characterPanel.SetActive(false);
            characterPanel = _statusDock.CharacterRoot;
        }

        /// <summary>One visual language for the legacy HUD pieces that live in the city scene: the mission card,
        /// the interaction prompt and the nameplate toggle get the NLUi ink/gold look of the status dock, the
        /// duplicate wallet (the dock already shows ¥) is hidden, and the prompt sits above the action bar.</summary>
        private void UnifyHudStyle()
        {
            if (walletText != null) walletText.gameObject.SetActive(false);
            // The big mission card is replaced by the status dock's single tracked-objective chip (full list: N).
            if (scenarioTitleText != null && scenarioTitleText.transform.parent != null)
            {
                var mission = scenarioTitleText.transform.parent.gameObject;
                if (mission.GetComponent<CanvasGroup>() == null) mission.AddComponent<CanvasGroup>();
                var group = mission.GetComponent<CanvasGroup>();
                group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false;
            }
            if (scenarioTitleText != null && scenarioTitleText.transform.parent != null)
            {
                var card = scenarioTitleText.transform.parent;
                var image = card.GetComponent<Image>();
                if (image != null) image.color = NLUi.Ink;
                scenarioTitleText.color = NLUi.Gold;
                scenarioTitleText.fontStyle = FontStyles.Bold;
                if (objectivesText != null) objectivesText.lineSpacing = 8f;
            }
            if (promptPanel != null)
            {
                var rect = promptPanel.transform as RectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 104f);
                var image = promptPanel.GetComponent<Image>();
                if (image != null) { image.sprite = UiKit.Rounded(18); image.type = Image.Type.Sliced; image.color = new Color(0.035f, 0.05f, 0.075f, 0.88f); }
                if (promptPanel.transform.Find("Border") == null) UiKit.Border(rect, new Color(1f, 1f, 1f, 0.1f), 18);
                // A gold key cap on the left (the key), the action on the right.
                if (_keyCap == null)
                {
                    var cap = UiKit.Card(promptPanel.transform, "KeyCap", UiTheme.Gold, 10);
                    cap.raycastTarget = false;
                    UiKit.Pin(cap.rectTransform, new Vector2(0f, 0.5f), 12f, 0f, 38f, 34f);
                    _keyCap = UiKit.Label(cap.transform, "Key", "F", 18f, UiTheme.OnGold, FontStyles.Bold, TextAlignmentOptions.Center);
                    UiKit.Fill(_keyCap.rectTransform);
                }
                if (promptText != null)
                {
                    promptText.fontSize = Mathf.Max(promptText.fontSize, 20f); promptText.color = NLUi.Text;
                    promptText.alignment = TextAlignmentOptions.MidlineLeft;
                    var pr = promptText.rectTransform; pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one; pr.offsetMin = new Vector2(62f, 0f); pr.offsetMax = new Vector2(-16f, 0f);
                }
            }
            var toggle = GetComponentInParent<Canvas>()?.transform.Find("ToggleNPCNames");
            if (toggle != null) toggle.gameObject.SetActive(false);
            if (onlineStatusText != null) onlineStatusText.color = NLUi.Muted;
        }

        private void TogglePopup(MenuPopupBase popup)
        {
            if (popup == null) return;
            SetInventoryVisible(false);
            SetCharacterVisible(false);
            _worldMap?.SetVisible(false);
            if (popup.IsOpen) popup.Hide(); else popup.Show();
            UpdateOverlayInputLock();
        }

        private void EnsureMobileControls() => TouchControlOverlay.Attach(this);

        private void EnsureQuestLog()
        {
            var questLog = GetComponent<QuestLogPopup>();
            if (questLog == null) questLog = gameObject.AddComponent<QuestLogPopup>();
            questLog.SetDialoguePanel(dialoguePanel);
            questLog.Initialize(scenarioTitleText != null ? scenarioTitleText.font : null);
        }

        private void EnsureExamCenter()
        {
            var examCenter = GetComponent<ExamCenterPopup>();
            if (examCenter == null) examCenter = gameObject.AddComponent<ExamCenterPopup>();
            examCenter.Initialize(scenarioTitleText != null ? scenarioTitleText.font : null);
        }

        private void EnsureStudyHub() => StudyHubPopup.GetOrCreate(gameObject, scenarioTitleText != null ? scenarioTitleText.font : null);

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
            if (_inventoryWindow == null) StyleInfoPanelColor(inventoryPanel);
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
            _questMarker = SceneRefs.Find<QuestDirectionMarker>();
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

            var missionRect = missionPanel as RectTransform;
            if (missionRect != null)
            {
                missionRect.anchorMin = new Vector2(0f, 1f);
                missionRect.anchorMax = new Vector2(0f, 1f);
                missionRect.pivot = new Vector2(0f, 1f);
                missionRect.anchoredPosition = new Vector2(24f, -24f);
                missionRect.sizeDelta = new Vector2(430f, 170f);
            }

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

            var toggleObject = new GameObject("QuestDropdownToggle");
            toggleObject.transform.SetParent(missionPanel, false);
            var toggleRect = toggleObject.AddComponent<RectTransform>();
            toggleRect.anchorMin = toggleRect.anchorMax = new Vector2(1f, 1f);
            toggleRect.pivot = new Vector2(1f, 1f); toggleRect.anchoredPosition = new Vector2(-10f, -10f);
            toggleRect.sizeDelta = new Vector2(42f, 34f);
            var toggleImage = toggleObject.AddComponent<Image>(); toggleImage.color = new Color(0.95f, 0.68f, 0.2f, 0.9f);
            var toggleButton = toggleObject.AddComponent<Button>(); toggleButton.targetGraphic = toggleImage;
            var toggleLabel = new GameObject("Icon").AddComponent<TextMeshProUGUI>();
            toggleLabel.transform.SetParent(toggleObject.transform, false); toggleLabel.text = "≡";
            toggleLabel.fontSize = 22f; toggleLabel.alignment = TextAlignmentOptions.Center; toggleLabel.color = Color.black;
            toggleLabel.rectTransform.anchorMin = Vector2.zero; toggleLabel.rectTransform.anchorMax = Vector2.one; toggleLabel.rectTransform.sizeDelta = Vector2.zero;
            toggleButton.onClick.AddListener(ToggleObjectivesPanel);
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

        private void EnsureNameplateToggle()
        {
            // Nameplates stay. The extra top-right "Tên nhân vật" chip duplicated the player card, so it is not created.
            if (NPCNameplateSystem.Instance == null) gameObject.AddComponent<NPCNameplateSystem>();
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            HideNameToggles(canvas.transform);
        }

        private static void HideNameToggles(Transform root)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == "ToggleNPCNames") child.gameObject.SetActive(false);
                else HideNameToggles(child);
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
    }
}
