using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NihongoLife.Audio;
using NihongoLife.Cameras;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Learning;
using NihongoLife.NPC;
using NihongoLife.Player;
using NihongoLife.Scenario;
using NihongoLife.Scoring;
using NihongoLife.UI;
using NihongoLife.World;

namespace NihongoLife.Editor
{
    /// <summary>Partial of SceneBuilder: gameplay HUD and menu UI authoring. The public entry point stays SceneBuilder.cs.</summary>
    public static partial class SceneBuilder
    {
        private static void CreateGameUI(GameObject player)
        {
            CreateEventSystem();
            var font = FontSetup.EnsureJapaneseFontAsset();
            var canvasGo = CreateCanvas("Canvas");

            var hudPanel = new GameObject("HUDPanel");
            hudPanel.transform.SetParent(canvasGo.transform, false);
            Stretch(hudPanel.AddComponent<RectTransform>());
            var hud = hudPanel.AddComponent<HUDUI>();
            hudPanel.AddComponent<QuestDirectionMarker>();

            var topLeft = CreatePanel(hudPanel.transform, "MissionPanel", new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(430f, 170f), new Color(0.025f, 0.035f, 0.045f, 0.88f));
            var scenarioTitle = CreateText(topLeft.transform, "ScenarioTitle", "Nhiệm vụ", font, 21, new Vector2(18f, -20f), new Vector2(350f, 34f), TextAlignmentOptions.Left);
            scenarioTitle.rectTransform.anchorMin = scenarioTitle.rectTransform.anchorMax = scenarioTitle.rectTransform.pivot = new Vector2(0f, 1f);
            scenarioTitle.color = new Color(1f, 0.91f, 0.54f);
            var objectives = CreateText(topLeft.transform, "ObjectivesList", "", font, 17, new Vector2(18f, -68f), new Vector2(394f, 104f), TextAlignmentOptions.TopLeft);
            objectives.rectTransform.anchorMin = objectives.rectTransform.anchorMax = objectives.rectTransform.pivot = new Vector2(0f, 1f);
            objectives.textWrappingMode = TextWrappingModes.Normal;

            var wallet = CreateText(hudPanel.transform, "WalletText", "¥ 1500", font, 24, new Vector2(-28f, -22f), new Vector2(220f, 42f), TextAlignmentOptions.Right);
            var walletRect = wallet.rectTransform;
            walletRect.anchorMin = new Vector2(1f, 1f);
            walletRect.anchorMax = new Vector2(1f, 1f);
            walletRect.pivot = new Vector2(1f, 1f);
            wallet.color = new Color(1f, 0.91f, 0.54f);

            var prompt = CreatePanel(hudPanel.transform, "PromptPanel", new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(560f, 48f), new Color(0.035f, 0.045f, 0.055f, 0.9f));
            var promptText = CreateText(prompt.transform, "PromptText", "[E] Tương tác", font, 18, Vector2.zero, new Vector2(520f, 38f), TextAlignmentOptions.Center);
            prompt.SetActive(false);

            var inventoryPanel = CreateInfoPanel(hudPanel.transform, "InventoryPanel", "Balo", "B", font);
            var inventoryText = inventoryPanel.transform.Find("Body").GetComponent<TextMeshProUGUI>();
            var characterPanel = CreateInfoPanel(hudPanel.transform, "CharacterPanel", "Nhân vật", "Tab", font);
            var characterText = characterPanel.transform.Find("Body").GetComponent<TextMeshProUGUI>();

            var dialoguePanel = CreatePanel(hudPanel.transform, "DialoguePanel", new Vector2(0.5f, 0f), new Vector2(0f, 74f), new Vector2(1120f, 378f), new Color(0.035f, 0.045f, 0.055f, 0.94f));
            var speaker = CreateText(dialoguePanel.transform, "SpeakerText", "", font, 18, new Vector2(28f, -20f), new Vector2(1048f, 30f), TextAlignmentOptions.Left);
            speaker.color = new Color(1f, 0.91f, 0.54f);
            var japanese = CreateText(dialoguePanel.transform, "JapaneseText", "", font, 28, new Vector2(28f, -58f), new Vector2(1048f, 58f), TextAlignmentOptions.Left);
            var reading = CreateText(dialoguePanel.transform, "ReadingText", "", font, 16, new Vector2(28f, -116f), new Vector2(1048f, 30f), TextAlignmentOptions.Left);
            reading.color = new Color(0.72f, 0.82f, 0.9f);
            var romaji = CreateText(dialoguePanel.transform, "RomajiText", "", font, 15, new Vector2(28f, -148f), new Vector2(1048f, 30f), TextAlignmentOptions.Left);
            romaji.color = new Color(0.72f, 0.82f, 0.9f);
            var translation = CreateText(dialoguePanel.transform, "TranslationText", "", font, 17, new Vector2(28f, -180f), new Vector2(1048f, 92f), TextAlignmentOptions.Left);

            var choicesContainer = new GameObject("ChoicesContainer");
            choicesContainer.transform.SetParent(dialoguePanel.transform, false);
            var choicesRect = choicesContainer.AddComponent<RectTransform>();
            choicesRect.anchorMin = new Vector2(0f, 0f);
            choicesRect.anchorMax = new Vector2(1f, 0f);
            choicesRect.pivot = new Vector2(0.5f, 0f);
            choicesRect.anchoredPosition = new Vector2(0f, 18f);
            choicesRect.sizeDelta = new Vector2(-56f, 72f);
            var layout = choicesContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var choicePrefab = CreateUIButton(canvasGo.transform, "ChoiceButtonPrefab", "Choice", Vector2.zero, new Vector2(240f, 58f), font, false).GetComponent<Button>();
            choicePrefab.gameObject.SetActive(false);
            var continueButton = CreateUIButton(dialoguePanel.transform, "ContinueButton", "Tiếp tục", new Vector2(330f, 28f), new Vector2(170f, 48f), font, true).GetComponent<Button>();

            var continueRect = continueButton.GetComponent<RectTransform>();
            continueRect.anchorMin = new Vector2(1f, 0f);
            continueRect.anchorMax = new Vector2(1f, 0f);
            continueRect.pivot = new Vector2(1f, 0f);
            continueRect.anchoredPosition = new Vector2(-28f, 18f);
            continueRect.sizeDelta = new Vector2(180f, 52f);

            var hudSo = new SerializedObject(hud);
            SetRef(hudSo, "promptPanel", prompt);
            SetRef(hudSo, "promptText", promptText);
            SetRef(hudSo, "scenarioTitleText", scenarioTitle);
            SetRef(hudSo, "objectivesText", objectives);
            SetRef(hudSo, "dialoguePanel", dialoguePanel);
            SetRef(hudSo, "speakerText", speaker);
            SetRef(hudSo, "japaneseText", japanese);
            SetRef(hudSo, "readingText", reading);
            SetRef(hudSo, "romajiText", romaji);
            SetRef(hudSo, "translationText", translation);
            SetRef(hudSo, "choicesContainer", choicesContainer.transform);
            SetRef(hudSo, "choiceButtonPrefab", choicePrefab);
            SetRef(hudSo, "continueButton", continueButton);
            SetRef(hudSo, "inventoryPanel", inventoryPanel);
            SetRef(hudSo, "inventoryText", inventoryText);
            SetRef(hudSo, "walletText", wallet);
            SetRef(hudSo, "characterPanel", characterPanel);
            SetRef(hudSo, "characterStatsText", characterText);
            hudSo.ApplyModifiedProperties();

            var result = CreateResultPanel(canvasGo.transform, font);
            var status = CreateStatusPanel(canvasGo.transform, font);
            var legacyInventory = CreateLegacyInventoryPanel(canvasGo.transform, font);

            var uiGo = new GameObject("UIManager");
            var uiManager = uiGo.AddComponent<UIManager>();
            var uiSo = new SerializedObject(uiManager);
            SetRef(uiSo, "hudPanel", hud);
            SetRef(uiSo, "resultPanel", result);
            SetRef(uiSo, "statusPanel", status);
            SetRef(uiSo, "inventoryPanel", legacyInventory);
            uiSo.ApplyModifiedProperties();
        }

        private static ResultUI CreateResultPanel(Transform parent, TMP_FontAsset font)
        {
            var panel = CreateFullScreenPanel(parent, "ResultPanel", new Color(0.035f, 0.045f, 0.055f, 0.98f));
            var result = panel.AddComponent<ResultUI>();
            var title = CreateText(panel.transform, "ResultTitle", "", font, 42, new Vector2(0f, 168f), new Vector2(760f, 58f), TextAlignmentOptions.Center);
            var overall = CreateText(panel.transform, "OverallScore", "", font, 54, new Vector2(0f, 96f), new Vector2(760f, 72f), TextAlignmentOptions.Center);
            var vocab = CreateScoreText(panel.transform, "VocabScore", "Từ vựng: ", new Vector2(0f, 36f), font);
            var grammar = CreateScoreText(panel.transform, "GrammarScore", "Ngữ pháp: ", new Vector2(0f, 4f), font);
            var listening = CreateScoreText(panel.transform, "ListeningScore", "Nghe hiểu: ", new Vector2(0f, -28f), font);
            var reading = CreateScoreText(panel.transform, "ReadingScore", "Đọc hiểu: ", new Vector2(0f, -60f), font);
            var accuracy = CreateScoreText(panel.transform, "AccuracyScore", "Độ chính xác: ", new Vector2(0f, -92f), font);
            var completion = CreateScoreText(panel.transform, "CompletionScore", "Hoàn thành: ", new Vector2(0f, -124f), font);
            var exit = CreateUIButton(panel.transform, "ExitButton", "Nhiệm tiếp theo", new Vector2(0f, -196f), new Vector2(280f, 54f), font, true).GetComponent<Button>();

            var so = new SerializedObject(result);
            SetRef(so, "missionTitleText", title);
            SetRef(so, "overallScoreText", overall);
            SetRef(so, "vocabularyScoreText", vocab);
            SetRef(so, "grammarScoreText", grammar);
            SetRef(so, "listeningScoreText", listening);
            SetRef(so, "readingScoreText", reading);
            SetRef(so, "accuracyScoreText", accuracy);
            SetRef(so, "completionScoreText", completion);
            SetRef(so, "returnToMenuButton", exit);
            SetRef(so, "returnToMenuButtonText", exit.GetComponentInChildren<TextMeshProUGUI>());
            so.ApplyModifiedProperties();
            panel.SetActive(false);
            return result;
        }

        private static StatusUI CreateStatusPanel(Transform parent, TMP_FontAsset font)
        {
            var panel = CreatePanel(parent, "StatusPanel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 220f), new Color(0.035f, 0.045f, 0.055f, 0.95f));
            var status = panel.AddComponent<StatusUI>();
            var text = CreateText(panel.transform, "Text", "Học viên N5", font, 18, Vector2.zero, new Vector2(320f, 180f), TextAlignmentOptions.Center);
            var so = new SerializedObject(status);
            SetRef(so, "nameText", text);
            so.ApplyModifiedProperties();
            panel.SetActive(false);
            return status;
        }

        private static InventoryUI CreateLegacyInventoryPanel(Transform parent, TMP_FontAsset font)
        {
            var panel = CreatePanel(parent, "LegacyInventoryPanel", new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(320f, 300f), new Color(0.035f, 0.045f, 0.055f, 0.95f));
            var inv = panel.AddComponent<InventoryUI>();
            var text = CreateText(panel.transform, "Text", "", font, 16, Vector2.zero, new Vector2(280f, 250f), TextAlignmentOptions.TopLeft);
            var so = new SerializedObject(inv);
            SetRef(so, "inventoryText", text);
            so.ApplyModifiedProperties();
            panel.SetActive(false);
            return inv;
        }

        private static TextMeshProUGUI CreateScoreText(Transform parent, string name, string label, Vector2 pos, TMP_FontAsset font)
        {
            return CreateText(parent, name, label + "0", font, 18, pos, new Vector2(460f, 30f), TextAlignmentOptions.Center);
        }

        private static GameObject CreateInfoPanel(Transform parent, string name, string title, string key, TMP_FontAsset font)
        {
            var panel = CreatePanel(parent, name, new Vector2(1f, 1f), new Vector2(-28f, -78f), new Vector2(360f, 250f), new Color(0.035f, 0.045f, 0.055f, 0.94f));
            var titleText = CreateText(panel.transform, "Title", $"{title}  [{key}]", font, 23, new Vector2(18f, -18f), new Vector2(312f, 36f), TextAlignmentOptions.Left);
            titleText.color = new Color(1f, 0.91f, 0.54f);
            var body = CreateText(panel.transform, "Body", "", font, 17, new Vector2(18f, -68f), new Vector2(315f, 160f), TextAlignmentOptions.TopLeft);
            body.textWrappingMode = TextWrappingModes.Normal;
            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateCanvas(string name)
        {
            var canvasGo = new GameObject(name);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            return canvasGo;
        }

        private static GameObject CreateFullScreenPanel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Stretch(panel.AddComponent<RectTransform>());
            panel.AddComponent<Image>().color = color;
            return panel;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            panel.AddComponent<Image>().color = color;
            return panel;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, TMP_FontAsset font, float size, Vector2 pos, Vector2 box, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = box;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = Color.white;
            tmp.alignment = alignment;
            tmp.enableAutoSizing = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        private static GameObject CreateUIButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, TMP_FontAsset font, bool primary)
        {
            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);
            var rect = btnGo.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var image = btnGo.AddComponent<Image>();
            image.color = primary ? new Color(0.12f, 0.38f, 0.56f, 1f) : new Color(0.12f, 0.15f, 0.17f, 1f);
            var button = btnGo.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = primary ? new Color(0.17f, 0.48f, 0.68f, 1f) : new Color(0.18f, 0.22f, 0.24f, 1f);
            colors.pressedColor = new Color(0.08f, 0.18f, 0.24f, 1f);
            button.colors = colors;

            // Add premium hover scale
            btnGo.AddComponent<NihongoLife.UI.UIHoverScale>();

            var label = CreateText(btnGo.transform, "Text", text, font, primary ? 24 : 20, Vector2.zero, size - new Vector2(24f, 10f), TextAlignmentOptions.Center);
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.fontStyle = primary ? FontStyles.Bold : FontStyles.Normal;
            return btnGo;
        }

        private static Button CreateMenuButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, TMP_FontAsset font)
        {
            var button = CreateUIButton(parent, name, text, pos, size, font, false).GetComponent<Button>();
            UIStyleKit.StyleButton(button, UIStyleKit.PanelBase, UIStyleKit.PanelHover, UIStyleKit.PanelPressed);
            return button;
        }

        private static void AddTopAccent(Transform parent)
        {
            var accent = new GameObject("TopAccent");
            accent.transform.SetParent(parent, false);
            var rect = accent.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 8f);
            rect.anchoredPosition = Vector2.zero;
            accent.AddComponent<Image>().color = new Color(1f, 0.82f, 0.32f, 1f);
        }

        private static void AddStoreSign(Transform parent, Vector3 position)
        {
            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "KonbiniSign";
            sign.transform.SetParent(parent);
            sign.transform.position = position;
            sign.transform.localScale = new Vector3(3.5f, 0.55f, 0.12f);
            sign.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMat("NL_Sign_Mat", new Color(0.08f, 0.34f, 0.38f));

            var textGo = new GameObject("SignText");
            textGo.transform.SetParent(sign.transform, false);
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.07f);
            textGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            textGo.transform.localScale = Vector3.one * 0.055f;
            var text = textGo.AddComponent<TextMeshPro>();
            var font = FontSetup.EnsureJapaneseFontAsset();
            if (font != null) text.font = font;
            text.text = "コンビニ";
            text.fontSize = 5f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
        }
    }
}
