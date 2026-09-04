using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using NihongoLife.Core;
using NihongoLife.Audio;
using NihongoLife.Player;
using NihongoLife.Cameras;
using NihongoLife.Interaction;
using NihongoLife.NPC;
using NihongoLife.Scenario;
using NihongoLife.Dialogue;
using NihongoLife.Scoring;
using NihongoLife.Learning;
using NihongoLife.UI;

namespace NihongoLife.Editor
{
    public static class SceneBuilder
    {
        [MenuItem("NihongoLife/Build All Scenes")]
        public static void BuildAllScenes()
        {
            Debug.Log("[SceneBuilder] Starting build of all scenes...");
            string previousScenePath = EditorSceneManager.GetActiveScene().path;
            string fallbackScenePath = "Assets/NihongoLife/Scenes/90_TestSandbox.unity";

            try
            {
                // Make sure the Scenes directory exists
                string scenesDir = "Assets/NihongoLife/Scenes";
                if (!AssetDatabase.IsValidFolder(scenesDir))
                {
                    AssetDatabase.CreateFolder("Assets/NihongoLife", "Scenes");
                }

                BuildBootstrapScene(scenesDir + "/00_Bootstrap.unity");
                BuildMainMenuScene(scenesDir + "/01_MainMenu.unity");
                BuildSandboxScene(fallbackScenePath);

                // Setup Editor Build Settings
                var buildScenes = new EditorBuildSettingsScene[]
                {
                    new EditorBuildSettingsScene(scenesDir + "/00_Bootstrap.unity", true),
                    new EditorBuildSettingsScene(scenesDir + "/01_MainMenu.unity", true),
                    new EditorBuildSettingsScene(fallbackScenePath, true)
                };
                EditorBuildSettings.scenes = buildScenes;

                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(fallbackScenePath);
                Debug.Log("[SceneBuilder] All scenes built and wired successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                string restorePath = !string.IsNullOrEmpty(previousScenePath) ? previousScenePath : fallbackScenePath;
                if (!string.IsNullOrEmpty(restorePath) && System.IO.File.Exists(restorePath))
                {
                    EditorSceneManager.OpenScene(restorePath);
                }
            }
        }

        private static void BuildBootstrapScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Create AppRoot GameObject
            var appRootGo = new GameObject("AppRoot");
            var appRoot = appRootGo.AddComponent<AppRoot>();

            // Attach Services
            var audioService = appRootGo.AddComponent<AudioService>();
            var sceneFlow = appRootGo.AddComponent<SceneFlowController>();

            // Wire references
            var serializedAppRoot = new SerializedObject(appRoot);
            serializedAppRoot.FindProperty("audioService").objectReferenceValue = audioService;
            serializedAppRoot.FindProperty("sceneFlowController").objectReferenceValue = sceneFlow;
            serializedAppRoot.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[SceneBuilder] Saved Bootstrap scene to: {path}");
        }

        private static void BuildMainMenuScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Create EventSystem if not exists
            CreateEventSystem();

            // Create UI Canvas
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasScaler = canvasGo.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Load/repair Font Asset. If repair fails, TMP falls back to its default font instead of aborting scene generation.
            var jpFont = FontSetup.EnsureJapaneseFontAsset();

            // Create MainMenu Panel
            var panelGo = new GameObject("MainMenuPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var rectPanel = panelGo.AddComponent<RectTransform>();
            rectPanel.anchorMin = Vector2.zero;
            rectPanel.anchorMax = Vector2.one;
            rectPanel.sizeDelta = Vector2.zero;
            panelGo.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 1f); // Charcoal background

            // Title
            var titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleText = titleGo.AddComponent<TextMeshProUGUI>();
            if (jpFont != null) titleText.font = jpFont;
            titleText.text = "NIHONGO LIFE";
            titleText.fontSize = 48;
            titleText.alignment = TextAlignmentOptions.Center;
            var rectTitle = titleGo.GetComponent<RectTransform>();
            rectTitle.anchoredPosition = new Vector2(0, 150);

            // Start Button
            var startBtnGo = CreateUIButton(panelGo.transform, "StartButton", "Start Game / Bắt đầu", new Vector2(0, 0), jpFont);
            var startBtn = startBtnGo.GetComponent<Button>();

            // Quit Button
            var quitBtnGo = CreateUIButton(panelGo.transform, "QuitButton", "Quit / Thoát", new Vector2(0, -60), jpFont);
            var quitBtn = quitBtnGo.GetComponent<Button>();

            // Profile Text Display
            var profileTextGo = new GameObject("ProfileText");
            profileTextGo.transform.SetParent(panelGo.transform, false);
            var profileText = profileTextGo.AddComponent<TextMeshProUGUI>();
            if (jpFont != null) profileText.font = jpFont;
            profileText.fontSize = 18;
            profileText.color = Color.white;
            profileText.alignment = TextAlignmentOptions.Center;
            var rectProfile = profileTextGo.GetComponent<RectTransform>();
            rectProfile.anchoredPosition = new Vector2(0, -180);
            rectProfile.sizeDelta = new Vector2(400, 150);

            // Add MainMenuUI script
            var uiManagerGo = new GameObject("UIManager");
            var uiManager = uiManagerGo.AddComponent<UIManager>();
            var mainMenuUI = panelGo.AddComponent<MainMenuUI>();

            // Wire MainMenuUI
            var serializedMenu = new SerializedObject(mainMenuUI);
            serializedMenu.FindProperty("startButton").objectReferenceValue = startBtn;
            serializedMenu.FindProperty("quitButton").objectReferenceValue = quitBtn;
            serializedMenu.FindProperty("profileText").objectReferenceValue = profileText;
            serializedMenu.ApplyModifiedProperties();

            // Wire UIManager
            var serializedUI = new SerializedObject(uiManager);
            serializedUI.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuUI;
            serializedUI.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[SceneBuilder] Saved Main Menu scene to: {path}");
        }

        private static void BuildSandboxScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Create AppRoot for Sandbox direct play testing
            var appRootGo = new GameObject("AppRoot");
            var appRoot = appRootGo.AddComponent<AppRoot>();
            var audioService = appRootGo.AddComponent<AudioService>();
            var sceneFlow = appRootGo.AddComponent<SceneFlowController>();

            var serializedAppRoot = new SerializedObject(appRoot);
            serializedAppRoot.FindProperty("audioService").objectReferenceValue = audioService;
            serializedAppRoot.FindProperty("sceneFlowController").objectReferenceValue = sceneFlow;
            serializedAppRoot.ApplyModifiedProperties();

            // Setup Lighting and physics environment
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.gray;

            // Create Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0, -0.55f, -8);
            floor.transform.localScale = new Vector3(72, 1, 42);
            var floorMat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
            floorMat.color = new Color(0.2f, 0.2f, 0.2f);
            floor.GetComponent<Renderer>().material = floorMat;

            // Create Player
            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(0, 0.5f, -13.5f);
            playerGo.transform.rotation = Quaternion.Euler(0, 0, 0);
            var cc = playerGo.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 1, 0);
            cc.height = 2f;
            
            var playerCtrl = playerGo.AddComponent<PlayerController>();
            var detector = playerGo.AddComponent<InteractionDetector>();
            playerGo.AddComponent<PlayerInventory>();
            playerGo.AddComponent<PlayerStatus>();
            
            var playerAnimCtrl = playerGo.AddComponent<NihongoLife.Core.CharacterAnimationController>();
            
            // Add visual
            var nlPlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Characters/NL_Player.prefab");
            if (nlPlayerPrefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(nlPlayerPrefab);
                visual.name = "Visual";
                visual.transform.SetParent(playerGo.transform, false);
                visual.transform.localPosition = new Vector3(0, 0, 0);
                
                // Normalization: Remy is usually ~1.75m but FBX scale can be off. Assuming it's already 1:1, we don't scale it wildly.
                visual.transform.localScale = Vector3.one; 
                
                playerAnimCtrl.SetAnimator(visual.GetComponent<Animator>());
            }
            else
            {
                // Fallback capsule
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.transform.SetParent(playerGo.transform, false);
                capsule.transform.localPosition = new Vector3(0, 1, 0);
                UnityEngine.Object.DestroyImmediate(capsule.GetComponent<Collider>());
            }
            
            // Set interactable layer
            int interactableLayer = 6;
            playerGo.layer = 0; // Default

            var serializedDetector = new SerializedObject(detector);
            serializedDetector.FindProperty("detectionRadius").floatValue = 2.4f;
            serializedDetector.FindProperty("interactableLayers").intValue = 1 << interactableLayer;
            serializedDetector.ApplyModifiedProperties();
            
            // Wire Ground Check pivot
            var groundCheckGo = new GameObject("GroundCheck");
            groundCheckGo.transform.SetParent(playerGo.transform, false);
            groundCheckGo.transform.localPosition = Vector3.zero;

            var serializedPlayer = new SerializedObject(playerCtrl);
            serializedPlayer.FindProperty("groundCheck").objectReferenceValue = groundCheckGo.transform;
            serializedPlayer.ApplyModifiedProperties();

            // Camera Setup
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                mainCamera = camGo.AddComponent<Camera>();
            }
            if (mainCamera.gameObject.GetComponent<AudioListener>() == null)
            {
                mainCamera.gameObject.AddComponent<AudioListener>();
            }
            var camController = mainCamera.gameObject.AddComponent<ThirdPersonCameraController>();
            camController.SetTarget(playerGo.transform);
            camController.SetOrbit(0f, 18f, 6.5f);

            // Create Managers
            var scenarioMgrGo = new GameObject("ScenarioManager");
            var scenarioMgr = scenarioMgrGo.AddComponent<ScenarioManager>();
            var scoringMgr = scenarioMgrGo.AddComponent<ScoringManager>();
            var dialogueMgr = scenarioMgrGo.AddComponent<DialogueManager>();
            var masteryMgr = scenarioMgrGo.AddComponent<LearningMasteryManager>();

            // Create EventSystem
            CreateEventSystem();

            // Create Canvas
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasScaler = canvasGo.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Load/repair Font Asset. If repair fails, TMP falls back to its default font instead of aborting scene generation.
            var jpFont = FontSetup.EnsureJapaneseFontAsset();

            // Create HUDUI Panel
            var hudPanelGo = new GameObject("HUDPanel");
            hudPanelGo.transform.SetParent(canvasGo.transform, false);
            var hudRect = hudPanelGo.AddComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.sizeDelta = Vector2.zero;
            var hudUI = hudPanelGo.AddComponent<HUDUI>();

            // HUD HUD elements
            // Prompt Tooltip
            var promptPanelGo = new GameObject("PromptPanel");
            promptPanelGo.transform.SetParent(hudPanelGo.transform, false);
            var promptRect = promptPanelGo.AddComponent<RectTransform>();
            promptRect.anchoredPosition = new Vector2(0, -100);
            var promptBg = promptPanelGo.AddComponent<Image>();
            promptBg.color = new Color(0.04f, 0.05f, 0.06f, 0.82f);
            var promptTextGo = new GameObject("PromptText");
            promptTextGo.transform.SetParent(promptPanelGo.transform, false);
            var promptTextRect = promptTextGo.AddComponent<RectTransform>();
            promptTextRect.anchorMin = Vector2.zero;
            promptTextRect.anchorMax = Vector2.one;
            promptTextRect.offsetMin = new Vector2(14, 6);
            promptTextRect.offsetMax = new Vector2(-14, -6);
            var promptText = promptTextGo.AddComponent<TextMeshProUGUI>();
            if (jpFont != null) promptText.font = jpFont;
            promptText.fontSize = 20;
            promptText.alignment = TextAlignmentOptions.Center;
            promptRect.anchorMin = new Vector2(0.5f, 0);
            promptRect.anchorMax = new Vector2(0.5f, 0);
            promptRect.pivot = new Vector2(0.5f, 0);
            promptRect.anchoredPosition = new Vector2(0, 22);
            promptRect.sizeDelta = new Vector2(460, 42);
            promptText.fontSize = 18;
            promptText.textWrappingMode = TextWrappingModes.NoWrap;
            promptText.overflowMode = TextOverflowModes.Ellipsis;
            promptText.text = "[E] Tương tác";

            // Scenario Title & Objectives List
            var objTitleGo = new GameObject("ScenarioTitle");
            objTitleGo.transform.SetParent(hudPanelGo.transform, false);
            var titleRect = objTitleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(0, 1);
            titleRect.pivot = new Vector2(0, 1);
            titleRect.anchoredPosition = new Vector2(20, -20);
            var titleText = objTitleGo.AddComponent<TextMeshProUGUI>();
            if (jpFont != null) titleText.font = jpFont;
            titleText.fontSize = 22;
            titleRect.sizeDelta = new Vector2(420, 34);
            titleText.fontSize = 20;
            titleText.color = new Color(1f, 0.92f, 0.45f);
            titleText.overflowMode = TextOverflowModes.Ellipsis;
            titleText.text = "Nhiệm vụ";

            var objListGo = new GameObject("ObjectivesList");
            objListGo.transform.SetParent(hudPanelGo.transform, false);
            var listRect = objListGo.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 1);
            listRect.anchorMax = new Vector2(0, 1);
            listRect.pivot = new Vector2(0, 1);
            listRect.anchoredPosition = new Vector2(20, -60);
            var listText = objListGo.AddComponent<TextMeshProUGUI>();
            if (jpFont != null) listText.font = jpFont;
            listText.fontSize = 18;
            listRect.sizeDelta = new Vector2(440, 190);
            listText.fontSize = 16;
            listText.textWrappingMode = TextWrappingModes.Normal;

            var walletGo = new GameObject("WalletText");
            walletGo.transform.SetParent(hudPanelGo.transform, false);
            var walletRect = walletGo.AddComponent<RectTransform>();
            walletRect.anchorMin = new Vector2(1, 1);
            walletRect.anchorMax = new Vector2(1, 1);
            walletRect.pivot = new Vector2(1, 1);
            walletRect.anchoredPosition = new Vector2(-24, -18);
            walletRect.sizeDelta = new Vector2(180, 36);
            var walletText = walletGo.AddComponent<TextMeshProUGUI>();
            if (jpFont != null) walletText.font = jpFont;
            walletText.fontSize = 22;
            walletText.alignment = TextAlignmentOptions.Right;
            walletText.color = new Color(1f, 0.92f, 0.45f);

            var inventoryPanelGo = CreateInfoPanel(hudPanelGo.transform, "InventoryPanel", "Balo", new Vector2(1, 1), new Vector2(-24, -70), jpFont);
            var inventoryText = inventoryPanelGo.transform.Find("Body").GetComponent<TextMeshProUGUI>();
            var characterPanelGo = CreateInfoPanel(hudPanelGo.transform, "CharacterPanel", "Nhan vat", new Vector2(1, 1), new Vector2(-24, -70), jpFont);
            var characterStatsText = characterPanelGo.transform.Find("Body").GetComponent<TextMeshProUGUI>();

            // Dialogue Panel
            var dialPanelGo = new GameObject("DialoguePanel");
            dialPanelGo.transform.SetParent(hudPanelGo.transform, false);
            var dialRect = dialPanelGo.AddComponent<RectTransform>();
            dialRect.anchorMin = new Vector2(0.5f, 0);
            dialRect.anchorMax = new Vector2(0.5f, 0);
            dialRect.pivot = new Vector2(0.5f, 0);
            dialRect.anchoredPosition = new Vector2(0, 82);
            dialRect.sizeDelta = new Vector2(760, 250);
            dialPanelGo.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            var spkrGo = new GameObject("SpeakerText");
            spkrGo.transform.SetParent(dialPanelGo.transform, false);
            var spkrText = spkrGo.AddComponent<TextMeshProUGUI>();
            if (jpFont != null) spkrText.font = jpFont;
            spkrText.fontSize = 18;
            spkrText.color = Color.yellow;
            spkrGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-280, 80);

            var readGo = new GameObject("ReadingText");
            readGo.transform.SetParent(dialPanelGo.transform, false);
            var readText = readGo.AddComponent<TextMeshProUGUI>();
            if (jpFont != null) readText.font = jpFont;
            readText.fontSize = 14;
            readText.color = Color.gray;
            readGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-280, 50);

            var jaGo = new GameObject("JapaneseText");
            jaGo.transform.SetParent(dialPanelGo.transform, false);
            var jaText = jaGo.AddComponent<TextMeshProUGUI>();
            if (jpFont != null) jaText.font = jpFont;
            jaText.fontSize = 20;
            jaGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-280, 20);

            var romGo = new GameObject("RomajiText");
            romGo.transform.SetParent(dialPanelGo.transform, false);
            var romText = romGo.AddComponent<TextMeshProUGUI>();
            if (jpFont != null) romText.font = jpFont;
            romText.fontSize = 14;
            romText.color = Color.cyan;
            romGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-280, -10);

            var viGo = new GameObject("TranslationText");
            viGo.transform.SetParent(dialPanelGo.transform, false);
            var viText = viGo.AddComponent<TextMeshProUGUI>();
            if (jpFont != null) viText.font = jpFont;
            viText.fontSize = 16;
            viText.color = Color.white;
            viGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-280, -40);

            // Continue button
            var contBtnGo = CreateUIButton(dialPanelGo.transform, "ContinueButton", "Tiếp tục", new Vector2(200, -60), jpFont);
            var contBtn = contBtnGo.GetComponent<Button>();

            // Choice Container
            var containerGo = new GameObject("ChoicesContainer");
            containerGo.transform.SetParent(dialPanelGo.transform, false);
            var contRect = containerGo.AddComponent<RectTransform>();
            contRect.anchoredPosition = new Vector2(0, -110);
            var layout = containerGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            // Generate placeholder Choice Button Prefab (saved inside current scene structure, or instantiated directly)
            var buttonPrefabGo = CreateUIButton(canvasGo.transform, "ChoiceButtonPrefab", "Choice", Vector2.zero, jpFont);
            buttonPrefabGo.SetActive(false);
            var btnPrefab = buttonPrefabGo.GetComponent<Button>();

            // Wire HUDUI
            var serializedHUD = new SerializedObject(hudUI);
            SetObjectReference(serializedHUD, "promptPanel", promptPanelGo);
            SetObjectReference(serializedHUD, "promptText", promptText);
            SetObjectReference(serializedHUD, "scenarioTitleText", titleText);
            SetObjectReference(serializedHUD, "objectivesText", listText);
            SetObjectReference(serializedHUD, "dialoguePanel", dialPanelGo);
            SetObjectReference(serializedHUD, "speakerText", spkrText);
            SetObjectReference(serializedHUD, "japaneseText", jaText);
            SetObjectReference(serializedHUD, "readingText", readText);
            SetObjectReference(serializedHUD, "romajiText", romText);
            SetObjectReference(serializedHUD, "translationText", viText);
            SetObjectReference(serializedHUD, "choicesContainer", containerGo.transform);
            SetObjectReference(serializedHUD, "choiceButtonPrefab", btnPrefab);
            SetObjectReference(serializedHUD, "continueButton", contBtn);
            SetObjectReference(serializedHUD, "inventoryPanel", inventoryPanelGo);
            SetObjectReference(serializedHUD, "inventoryText", inventoryText);
            SetObjectReference(serializedHUD, "walletText", walletText);
            SetObjectReference(serializedHUD, "characterPanel", characterPanelGo);
            SetObjectReference(serializedHUD, "characterStatsText", characterStatsText);
            serializedHUD.ApplyModifiedProperties();

            // Create ResultUI Panel
            var resultPanelGo = new GameObject("ResultPanel");
            resultPanelGo.transform.SetParent(canvasGo.transform, false);
            var resRect = resultPanelGo.AddComponent<RectTransform>();
            resRect.anchorMin = Vector2.zero;
            resRect.anchorMax = Vector2.one;
            resRect.sizeDelta = Vector2.zero;
            resultPanelGo.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.1f, 0.95f);
            var resultUI = resultPanelGo.AddComponent<ResultUI>();

            // Results details
            var resTitleGo = new GameObject("ResultTitle");
            resTitleGo.transform.SetParent(resultPanelGo.transform, false);
            var resTitleText = resTitleGo.AddComponent<TextMeshProUGUI>();
            if (jpFont != null) resTitleText.font = jpFont;
            resTitleText.fontSize = 32;
            resTitleText.alignment = TextAlignmentOptions.Center;
            resTitleGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 150);

            var overGo = new GameObject("OverallScore");
            overGo.transform.SetParent(resultPanelGo.transform, false);
            var overText = overGo.AddComponent<TextMeshProUGUI>();
            if (jpFont != null) overText.font = jpFont;
            overText.fontSize = 40;
            overText.alignment = TextAlignmentOptions.Center;
            overGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 80);

            // Metric labels
            var vocabScoreGo = CreateScoreText(resultPanelGo.transform, "VocabScore", "Từ vựng: ", new Vector2(0, 20), jpFont);
            var gramScoreGo = CreateScoreText(resultPanelGo.transform, "GrammarScore", "Ngữ pháp: ", new Vector2(0, -10), jpFont);
            var listScoreGo = CreateScoreText(resultPanelGo.transform, "ListeningScore", "Nghe hiểu: ", new Vector2(0, -40), jpFont);
            var readScoreGo = CreateScoreText(resultPanelGo.transform, "ReadingScore", "Đọc hiểu: ", new Vector2(0, -70), jpFont);
            var accScoreGo = CreateScoreText(resultPanelGo.transform, "AccuracyScore", "Độ chính xác: ", new Vector2(0, -100), jpFont);
            var complScoreGo = CreateScoreText(resultPanelGo.transform, "CompletionScore", "Hoàn thành: ", new Vector2(0, -130), jpFont);

            var exitBtnGo = CreateUIButton(resultPanelGo.transform, "ExitButton", "Quay lại Menu", new Vector2(0, -180), jpFont);
            var exitBtn = exitBtnGo.GetComponent<Button>();

            // Wire ResultUI
            var serializedResult = new SerializedObject(resultUI);
            serializedResult.FindProperty("missionTitleText").objectReferenceValue = resTitleText;
            serializedResult.FindProperty("overallScoreText").objectReferenceValue = overText;
            serializedResult.FindProperty("vocabularyScoreText").objectReferenceValue = vocabScoreGo.GetComponent<TextMeshProUGUI>();
            serializedResult.FindProperty("grammarScoreText").objectReferenceValue = gramScoreGo.GetComponent<TextMeshProUGUI>();
            serializedResult.FindProperty("listeningScoreText").objectReferenceValue = listScoreGo.GetComponent<TextMeshProUGUI>();
            serializedResult.FindProperty("readingScoreText").objectReferenceValue = readScoreGo.GetComponent<TextMeshProUGUI>();
            serializedResult.FindProperty("accuracyScoreText").objectReferenceValue = accScoreGo.GetComponent<TextMeshProUGUI>();
            serializedResult.FindProperty("completionScoreText").objectReferenceValue = complScoreGo.GetComponent<TextMeshProUGUI>();
            serializedResult.FindProperty("returnToMenuButton").objectReferenceValue = exitBtn;
            serializedResult.ApplyModifiedProperties();

            // Create Status Panel
            var statusPanelGo = new GameObject("StatusPanel");
            statusPanelGo.transform.SetParent(canvasGo.transform, false);
            var statusRect = statusPanelGo.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.5f, 0.5f);
            statusRect.anchorMax = new Vector2(0.5f, 0.5f);
            statusRect.sizeDelta = new Vector2(300, 200);
            statusPanelGo.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            var statusUI = statusPanelGo.AddComponent<StatusUI>();
            var statusTextGo = new GameObject("Text");
            statusTextGo.transform.SetParent(statusPanelGo.transform, false);
            var statusText = statusTextGo.AddComponent<TextMeshProUGUI>();
            statusText.font = jpFont;
            statusText.fontSize = 18;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.rectTransform.sizeDelta = new Vector2(280, 180);
            var serializedStatus = new SerializedObject(statusUI);
            serializedStatus.FindProperty("nameText").objectReferenceValue = statusText;
            serializedStatus.ApplyModifiedProperties();
            statusPanelGo.SetActive(false);

            // Create Inventory Panel
            var invPanelGo = new GameObject("InventoryPanel");
            invPanelGo.transform.SetParent(canvasGo.transform, false);
            var invRect = invPanelGo.AddComponent<RectTransform>();
            invRect.anchorMin = new Vector2(0.8f, 0.5f);
            invRect.anchorMax = new Vector2(0.8f, 0.5f);
            invRect.sizeDelta = new Vector2(250, 300);
            invPanelGo.AddComponent<Image>().color = new Color(0.1f, 0.15f, 0.2f, 0.9f);
            var invUI = invPanelGo.AddComponent<InventoryUI>();
            var invTextGo = new GameObject("Text");
            invTextGo.transform.SetParent(invPanelGo.transform, false);
            var invText = invTextGo.AddComponent<TextMeshProUGUI>();
            invText.font = jpFont;
            invText.fontSize = 16;
            invText.alignment = TextAlignmentOptions.TopLeft;
            invText.rectTransform.sizeDelta = new Vector2(230, 280);
            var serializedInv = new SerializedObject(invUI);
            serializedInv.FindProperty("inventoryText").objectReferenceValue = invText;
            serializedInv.ApplyModifiedProperties();
            invPanelGo.SetActive(false);

            // Create UIManager
            var uiMgrGo = new GameObject("UIManager");
            var uiMgr = uiMgrGo.AddComponent<UIManager>();
            var serializedUI = new SerializedObject(uiMgr);
            serializedUI.FindProperty("hudPanel").objectReferenceValue = hudUI;
            serializedUI.FindProperty("resultPanel").objectReferenceValue = resultUI;
            serializedUI.FindProperty("statusPanel").objectReferenceValue = statusUI;
            serializedUI.FindProperty("inventoryPanel").objectReferenceValue = invUI;
            serializedUI.ApplyModifiedProperties();

            // Build Environment Placeholders
            var walls = new GameObject("Environment");
            walls.transform.position = Vector3.zero;
            
            // Build Visuals and Wrappers
            VisualEnvironmentBuilder.GenerateVisualEnvironment(walls);

            // Store door. The scenario advances only when the player presses E on this interactable.
            var doorRoot = new GameObject("StoreDoor");
            doorRoot.layer = interactableLayer;
            doorRoot.transform.SetParent(walls.transform);
            doorRoot.transform.position = new Vector3(0, 1.15f, 0.65f);
            var doorTrigger = doorRoot.AddComponent<BoxCollider>();
            doorTrigger.size = new Vector3(3.2f, 2.4f, 1.6f);
            doorTrigger.isTrigger = true;

            var doorBlocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorBlocker.name = "DoorBlocker";
            doorBlocker.transform.SetParent(doorRoot.transform, false);
            doorBlocker.transform.localPosition = new Vector3(0, 0, 0.2f);
            doorBlocker.transform.localScale = new Vector3(2.2f, 2.2f, 0.12f);
            UnityEngine.Object.DestroyImmediate(doorBlocker.GetComponent<MeshRenderer>());
            UnityEngine.Object.DestroyImmediate(doorBlocker.GetComponent<MeshFilter>());

            var doorVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorVisual.name = "DoorVisual";
            doorVisual.transform.SetParent(doorRoot.transform, false);
            doorVisual.transform.localPosition = Vector3.zero;
            doorVisual.transform.localScale = new Vector3(1.6f, 2.2f, 0.08f);
            doorVisual.GetComponent<Renderer>().material.color = new Color(0.95f, 0.95f, 0.9f);

            var doorInteractable = doorRoot.AddComponent<DoorInteractable>();
            var serializedDoor = new SerializedObject(doorInteractable);
            serializedDoor.FindProperty("areaId").stringValue = "store_entrance";
            serializedDoor.FindProperty("doorVisual").objectReferenceValue = doorVisual.transform;
            serializedDoor.FindProperty("blockingCollider").objectReferenceValue = doorBlocker.GetComponent<Collider>();
            serializedDoor.ApplyModifiedProperties();

            // Shelves (Gameplay colliders only)
            var shelf1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf1.name = "Shelf_Food";
            shelf1.transform.SetParent(walls.transform);
            shelf1.transform.position = new Vector3(-3, 1.1f, 4);
            var shelf1Collider = shelf1.GetComponent<BoxCollider>();
            shelf1Collider.size = new Vector3(2.2f, 2.2f, 0.8f); // Fit standard shelf
            
            var shelfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Furniture/shelf.prefab");
            if (shelfPrefab != null) {
                var vis = (GameObject)PrefabUtility.InstantiatePrefab(shelfPrefab);
                vis.transform.SetParent(shelf1.transform, false);
                vis.transform.localPosition = new Vector3(0, -1.1f, 0); // Ground offset
                UnityEngine.Object.DestroyImmediate(shelf1.GetComponent<MeshRenderer>());
                UnityEngine.Object.DestroyImmediate(shelf1.GetComponent<MeshFilter>());
            }

            var shelf2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf2.name = "Shelf_Drinks";
            shelf2.transform.SetParent(walls.transform);
            shelf2.transform.position = new Vector3(3, 1.1f, 4);
            var shelf2Collider = shelf2.GetComponent<BoxCollider>();
            shelf2Collider.size = new Vector3(2.2f, 2.2f, 0.8f);

            if (shelfPrefab != null) {
                var vis = (GameObject)PrefabUtility.InstantiatePrefab(shelfPrefab);
                vis.transform.SetParent(shelf2.transform, false);
                vis.transform.localPosition = new Vector3(0, -1.1f, 0);
                UnityEngine.Object.DestroyImmediate(shelf2.GetComponent<MeshRenderer>());
                UnityEngine.Object.DestroyImmediate(shelf2.GetComponent<MeshFilter>());
            }

            // Cashier counter (Gameplay collider only)
            var counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "CashierCounter";
            counter.transform.SetParent(walls.transform);
            counter.transform.position = new Vector3(0, 0.5f, 8);
            var counterCollider = counter.GetComponent<BoxCollider>();
            counterCollider.size = new Vector3(3, 1, 1);
            
            var counterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Furniture/counter.prefab");
            if (counterPrefab != null) {
                var vis = (GameObject)PrefabUtility.InstantiatePrefab(counterPrefab);
                vis.transform.SetParent(counter.transform, false);
                vis.transform.localPosition = new Vector3(0, -0.5f, 0); // Ground offset
                UnityEngine.Object.DestroyImmediate(counter.GetComponent<MeshRenderer>());
                UnityEngine.Object.DestroyImmediate(counter.GetComponent<MeshFilter>());
            }

            // Instantiate items on shelf
            var onigiriGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            onigiriGo.name = "Onigiri";
            onigiriGo.transform.position = new Vector3(-3, 1.3f, 4); // On food shelf
            onigiriGo.layer = interactableLayer;

            // Attach visual wrapper
            var onigiriVisual = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Food/food_apple.prefab");
            if (onigiriVisual != null) {
                var vis = (GameObject)PrefabUtility.InstantiatePrefab(onigiriVisual);
                vis.transform.SetParent(onigiriGo.transform, false);
                UnityEngine.Object.DestroyImmediate(onigiriGo.GetComponent<MeshRenderer>());
                UnityEngine.Object.DestroyImmediate(onigiriGo.GetComponent<MeshFilter>());
            }
            
            var onigiriInteract = onigiriGo.AddComponent<InteractiveItem>();
            var serializedOnigiri = new SerializedObject(onigiriInteract);
            serializedOnigiri.FindProperty("itemId").stringValue = "onigiri";
            serializedOnigiri.FindProperty("displayNameJa").stringValue = "おにぎり";
            serializedOnigiri.FindProperty("displayNameVi").stringValue = "Cơm nắm";
            serializedOnigiri.FindProperty("priceYen").intValue = 497;
            serializedOnigiri.FindProperty("promptJa").stringValue = "おにぎりを取る";
            serializedOnigiri.FindProperty("promptVi").stringValue = "Lấy cơm nắm";
            serializedOnigiri.ApplyModifiedProperties();

            // Incorrect item (Water)
            var waterGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            waterGo.name = "Water";
            waterGo.transform.position = new Vector3(3, 1.4f, 4); // On drinks shelf
            waterGo.layer = interactableLayer;

            // Attach visual wrapper
            var waterVisual = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Food/food_bottle.prefab");
            if (waterVisual != null) {
                var vis = (GameObject)PrefabUtility.InstantiatePrefab(waterVisual);
                vis.transform.SetParent(waterGo.transform, false);
                UnityEngine.Object.DestroyImmediate(waterGo.GetComponent<MeshRenderer>());
                UnityEngine.Object.DestroyImmediate(waterGo.GetComponent<MeshFilter>());
            }

            var waterInteract = waterGo.AddComponent<InteractiveItem>();
            var serializedWater = new SerializedObject(waterInteract);
            serializedWater.FindProperty("itemId").stringValue = "water";
            serializedWater.FindProperty("displayNameJa").stringValue = "水";
            serializedWater.FindProperty("displayNameVi").stringValue = "Nước";
            serializedWater.FindProperty("priceYen").intValue = 120;
            serializedWater.FindProperty("promptJa").stringValue = "水を調べる";
            serializedWater.FindProperty("promptVi").stringValue = "Kiểm tra nước";
            serializedWater.FindProperty("destroyOnInteract").boolValue = false;
            serializedWater.ApplyModifiedProperties();

            // Cashier NPC
            var npcGo = CreateStylizedCashier();
            npcGo.name = "CashierNPC";
            npcGo.transform.position = new Vector3(0, 0.75f, 9.5f); // Behind cashier counter
            npcGo.layer = interactableLayer;
            var npcCtrl = npcGo.AddComponent<NPCController>();
            var serializedNPC = new SerializedObject(npcCtrl);
            serializedNPC.FindProperty("npcId").stringValue = "npc_cashier";
            serializedNPC.FindProperty("promptJa").stringValue = "会計する";
            serializedNPC.FindProperty("promptVi").stringValue = "Thanh toán";
            serializedNPC.FindProperty("scenarioAreaIdOnInteract").stringValue = "cashier";
            serializedNPC.FindProperty("displayName").stringValue = "Thu ngân";
            serializedNPC.FindProperty("role").stringValue = "Cashier";
            serializedNPC.ApplyModifiedProperties();

            // Service zone marker only. Checkout is intentionally driven by pressing E on the cashier NPC.
            var cashierArea = new GameObject("CashierAreaTrigger");
            cashierArea.transform.SetParent(walls.transform);
            cashierArea.transform.position = new Vector3(0, 1.5f, 8);
            var cashierCollider = cashierArea.AddComponent<BoxCollider>();
            cashierCollider.size = new Vector3(3, 3, 2);
            cashierCollider.isTrigger = true;

            // Scene Initializer
            var initGo = new GameObject("ScenarioSceneInitializer");
            initGo.AddComponent<ScenarioSceneInitializer>();

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[SceneBuilder] Saved Test Sandbox scene to: {path}");
        }

        private static GameObject CreateUIButton(Transform parent, string name, string text, Vector2 pos, TMP_FontAsset font = null)
        {
            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);
            var rect = btnGo.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(220, 50);

            // Add Image and Button
            btnGo.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var btn = btnGo.AddComponent<Button>();

            // Add Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(btnGo.transform, false);
            var txt = textGo.AddComponent<TextMeshProUGUI>();
            if (font != null) txt.font = font;
            txt.text = text;
            txt.fontSize = 16;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;
            
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return btnGo;
        }

        private static GameObject CreateInfoPanel(Transform parent, string name, string title, Vector2 anchor, Vector2 pos, TMP_FontAsset font = null)
        {
            var panelGo = new GameObject(name);
            panelGo.transform.SetParent(parent, false);
            var rect = panelGo.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(310, 230);
            panelGo.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.08f, 0.92f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -14);
            titleRect.sizeDelta = new Vector2(-28, 34);
            var titleText = titleGo.AddComponent<TextMeshProUGUI>();
            if (font != null) titleText.font = font;
            titleText.text = title;
            titleText.fontSize = 22;
            titleText.color = new Color(1f, 0.92f, 0.45f);

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(panelGo.transform, false);
            var bodyRect = bodyGo.AddComponent<RectTransform>();
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(18, 18);
            bodyRect.offsetMax = new Vector2(-18, -58);
            var bodyText = bodyGo.AddComponent<TextMeshProUGUI>();
            if (font != null) bodyText.font = font;
            bodyText.fontSize = 17;
            bodyText.color = Color.white;
            bodyText.textWrappingMode = TextWrappingModes.Normal;

            panelGo.SetActive(false);
            return panelGo;
        }

        private static GameObject CreateStylizedCashier()
        {
            var root = new GameObject("CashierNPC");
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, 0.35f, 0);
            collider.size = new Vector3(0.9f, 1.9f, 0.9f);
            
            var animCtrl = root.AddComponent<NihongoLife.Core.CharacterAnimationController>();

            var cashierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Characters/NL_Cashier.prefab");
            if (cashierPrefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(cashierPrefab);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = new Vector3(0, -0.6f, 0); // Ground adjust
                visual.transform.localRotation = Quaternion.Euler(0, 180, 0); // Facing player across counter
                
                // Elizabeth scaling normalization if needed
                visual.transform.localScale = Vector3.one;
                
                animCtrl.SetAnimator(visual.GetComponent<Animator>());
            }
            else
            {
                var apronMat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                apronMat.color = new Color(0.08f, 0.36f, 0.42f);
                var skinMat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                skinMat.color = new Color(0.92f, 0.72f, 0.56f);
                var hairMat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                hairMat.color = new Color(0.12f, 0.08f, 0.05f);

                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = "Body";
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = new Vector3(0, 0.2f, 0);
                body.transform.localScale = new Vector3(0.65f, 1.0f, 0.35f);
                body.GetComponent<Renderer>().material = apronMat;
                UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());

                var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "Head";
                head.transform.SetParent(root.transform, false);
                head.transform.localPosition = new Vector3(0, 0.95f, 0);
                head.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);
                head.GetComponent<Renderer>().material = skinMat;
                UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());

                var hair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hair.name = "Hair";
                hair.transform.SetParent(root.transform, false);
                hair.transform.localPosition = new Vector3(0, 1.13f, -0.02f);
                hair.transform.localScale = new Vector3(0.46f, 0.22f, 0.46f);
                hair.GetComponent<Renderer>().material = hairMat;
                UnityEngine.Object.DestroyImmediate(hair.GetComponent<Collider>());

                var nameTag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                nameTag.name = "NameTag";
                nameTag.transform.SetParent(root.transform, false);
                nameTag.transform.localPosition = new Vector3(0.18f, 0.45f, -0.19f);
                nameTag.transform.localScale = new Vector3(0.18f, 0.08f, 0.02f);
                nameTag.GetComponent<Renderer>().material.color = new Color(1f, 0.92f, 0.45f);
                UnityEngine.Object.DestroyImmediate(nameTag.GetComponent<Collider>());
            }

            return root;
        }

        private static GameObject CreateScoreText(Transform parent, string name, string label, Vector2 pos, TMP_FontAsset font = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(400, 30);
            
            var txt = go.AddComponent<TextMeshProUGUI>();
            if (font != null) txt.font = font;
            txt.text = label + "0";
            txt.fontSize = 16;
            txt.alignment = TextAlignmentOptions.Center;
            
            return go;
        }

        private static void SetObjectReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[SceneBuilder] Missing serialized property '{propertyName}' on {serializedObject.targetObject.name}. Wait for scripts to compile, then run Build All Scenes again.");
                return;
            }

            property.objectReferenceValue = value;
        }

        private static void CreateEventSystem()
        {
            var es = UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }
    }
}
