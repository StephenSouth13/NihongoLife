using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

            // Make sure the Scenes directory exists
            string scenesDir = "Assets/NihongoLife/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesDir))
            {
                AssetDatabase.CreateFolder("Assets/NihongoLife", "Scenes");
            }

            BuildBootstrapScene(scenesDir + "/00_Bootstrap.unity");
            BuildMainMenuScene(scenesDir + "/01_MainMenu.unity");
            BuildSandboxScene(scenesDir + "/90_TestSandbox.unity");

            // Setup Editor Build Settings
            var buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(scenesDir + "/00_Bootstrap.unity", true),
                new EditorBuildSettingsScene(scenesDir + "/01_MainMenu.unity", true),
                new EditorBuildSettingsScene(scenesDir + "/90_TestSandbox.unity", true)
            };
            EditorBuildSettings.scenes = buildScenes;

            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBuilder] All scenes built and wired successfully!");
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
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

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
            titleText.text = "NIHONGO LIFE";
            titleText.fontSize = 48;
            titleText.alignment = TextAlignmentOptions.Center;
            var rectTitle = titleGo.GetComponent<RectTransform>();
            rectTitle.anchoredPosition = new Vector2(0, 150);

            // Start Button
            var startBtnGo = CreateUIButton(panelGo.transform, "StartButton", "Start Game / Bắt đầu", new Vector2(0, 0));
            var startBtn = startBtnGo.GetComponent<Button>();

            // Quit Button
            var quitBtnGo = CreateUIButton(panelGo.transform, "QuitButton", "Quit / Thoát", new Vector2(0, -60));
            var quitBtn = quitBtnGo.GetComponent<Button>();

            // Profile Text Display
            var profileTextGo = new GameObject("ProfileText");
            profileTextGo.transform.SetParent(panelGo.transform, false);
            var profileText = profileTextGo.AddComponent<TextMeshProUGUI>();
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

            // Setup Lighting and physics environment
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.gray;

            // Create Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0, -0.5f, 0);
            floor.transform.localScale = new Vector3(30, 1, 30);
            var floorMat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
            floorMat.color = new Color(0.2f, 0.2f, 0.2f);
            floor.GetComponent<Renderer>().material = floorMat;

            // Create Player
            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(0, 0.5f, -5);
            var cc = playerGo.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 1, 0);
            cc.height = 2f;
            
            var playerCtrl = playerGo.AddComponent<PlayerController>();
            var detector = playerGo.AddComponent<InteractionDetector>();
            
            // Set interactable layer
            int interactableLayer = 6;
            playerGo.layer = 0; // Default
            
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
            mainCamera.gameObject.AddComponent<AudioListener>();
            var camController = mainCamera.gameObject.AddComponent<ThirdPersonCameraController>();
            camController.SetTarget(playerGo.transform);

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
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

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
            var promptText = promptPanelGo.AddComponent<TextMeshProUGUI>();
            promptText.fontSize = 20;
            promptText.alignment = TextAlignmentOptions.Center;
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
            titleText.fontSize = 22;
            titleText.text = "Nhiệm vụ";

            var objListGo = new GameObject("ObjectivesList");
            objListGo.transform.SetParent(hudPanelGo.transform, false);
            var listRect = objListGo.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 1);
            listRect.anchorMax = new Vector2(0, 1);
            listRect.pivot = new Vector2(0, 1);
            listRect.anchoredPosition = new Vector2(20, -60);
            var listText = objListGo.AddComponent<TextMeshProUGUI>();
            listText.fontSize = 18;

            // Dialogue Panel
            var dialPanelGo = new GameObject("DialoguePanel");
            dialPanelGo.transform.SetParent(hudPanelGo.transform, false);
            var dialRect = dialPanelGo.AddComponent<RectTransform>();
            dialRect.anchorMin = new Vector2(0.5f, 0);
            dialRect.anchorMax = new Vector2(0.5f, 0);
            dialRect.pivot = new Vector2(0.5f, 0);
            dialRect.anchoredPosition = new Vector2(0, 20);
            dialRect.sizeDelta = new Vector2(600, 220);
            dialPanelGo.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            var spkrGo = new GameObject("SpeakerText");
            spkrGo.transform.SetParent(dialPanelGo.transform, false);
            var spkrText = spkrGo.AddComponent<TextMeshProUGUI>();
            spkrText.fontSize = 18;
            spkrText.color = Color.yellow;
            spkrGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-280, 80);

            var readGo = new GameObject("ReadingText");
            readGo.transform.SetParent(dialPanelGo.transform, false);
            var readText = readGo.AddComponent<TextMeshProUGUI>();
            readText.fontSize = 14;
            readText.color = Color.gray;
            readGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-280, 50);

            var jaGo = new GameObject("JapaneseText");
            jaGo.transform.SetParent(dialPanelGo.transform, false);
            var jaText = jaGo.AddComponent<TextMeshProUGUI>();
            jaText.fontSize = 20;
            jaGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-280, 20);

            var romGo = new GameObject("RomajiText");
            romGo.transform.SetParent(dialPanelGo.transform, false);
            var romText = romGo.AddComponent<TextMeshProUGUI>();
            romText.fontSize = 14;
            romText.color = Color.cyan;
            romGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-280, -10);

            var viGo = new GameObject("TranslationText");
            viGo.transform.SetParent(dialPanelGo.transform, false);
            var viText = viGo.AddComponent<TextMeshProUGUI>();
            viText.fontSize = 16;
            viText.color = Color.white;
            viGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-280, -40);

            // Continue button
            var contBtnGo = CreateUIButton(dialPanelGo.transform, "ContinueButton", "Tiếp tục", new Vector2(200, -60));
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
            var buttonPrefabGo = CreateUIButton(canvasGo.transform, "ChoiceButtonPrefab", "Choice", Vector2.zero);
            buttonPrefabGo.SetActive(false);
            var btnPrefab = buttonPrefabGo.GetComponent<Button>();

            // Wire HUDUI
            var serializedHUD = new SerializedObject(hudUI);
            serializedHUD.FindProperty("promptPanel").objectReferenceValue = promptPanelGo;
            serializedHUD.FindProperty("promptText").objectReferenceValue = promptText;
            serializedHUD.FindProperty("scenarioTitleText").objectReferenceValue = titleText;
            serializedHUD.FindProperty("objectivesText").objectReferenceValue = listText;
            serializedHUD.FindProperty("dialoguePanel").objectReferenceValue = dialPanelGo;
            serializedHUD.FindProperty("speakerText").objectReferenceValue = spkrText;
            serializedHUD.FindProperty("japaneseText").objectReferenceValue = jaText;
            serializedHUD.FindProperty("readingText").objectReferenceValue = readText;
            serializedHUD.FindProperty("romajiText").objectReferenceValue = romText;
            serializedHUD.FindProperty("translationText").objectReferenceValue = viText;
            serializedHUD.FindProperty("choicesContainer").objectReferenceValue = containerGo.transform;
            serializedHUD.FindProperty("choiceButtonPrefab").objectReferenceValue = btnPrefab;
            serializedHUD.FindProperty("continueButton").objectReferenceValue = contBtn;
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
            resTitleText.fontSize = 32;
            resTitleText.alignment = TextAlignmentOptions.Center;
            resTitleGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 150);

            var overGo = new GameObject("OverallScore");
            overGo.transform.SetParent(resultPanelGo.transform, false);
            var overText = overGo.AddComponent<TextMeshProUGUI>();
            overText.fontSize = 40;
            overText.alignment = TextAlignmentOptions.Center;
            overGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 80);

            // Metric labels
            var vocabScoreGo = CreateScoreText(resultPanelGo.transform, "VocabScore", "Từ vựng: ", new Vector2(0, 20));
            var gramScoreGo = CreateScoreText(resultPanelGo.transform, "GrammarScore", "Ngữ pháp: ", new Vector2(0, -10));
            var listScoreGo = CreateScoreText(resultPanelGo.transform, "ListeningScore", "Nghe hiểu: ", new Vector2(0, -40));
            var readScoreGo = CreateScoreText(resultPanelGo.transform, "ReadingScore", "Đọc hiểu: ", new Vector2(0, -70));
            var accScoreGo = CreateScoreText(resultPanelGo.transform, "AccuracyScore", "Độ chính xác: ", new Vector2(0, -100));
            var complScoreGo = CreateScoreText(resultPanelGo.transform, "CompletionScore", "Hoàn thành: ", new Vector2(0, -130));

            var exitBtnGo = CreateUIButton(resultPanelGo.transform, "ExitButton", "Quay lại Menu", new Vector2(0, -180));
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

            // Create UIManager
            var uiMgrGo = new GameObject("UIManager");
            var uiMgr = uiMgrGo.AddComponent<UIManager>();
            var serializedUI = new SerializedObject(uiMgr);
            serializedUI.FindProperty("hudPanel").objectReferenceValue = hudUI;
            serializedUI.FindProperty("resultPanel").objectReferenceValue = resultUI;
            serializedUI.ApplyModifiedProperties();

            // Build Environment Placeholders
            var walls = new GameObject("Environment");
            walls.transform.position = Vector3.zero;

            // Entrance door placeholder
            var entrance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            entrance.name = "EntranceTrigger";
            entrance.transform.SetParent(walls.transform);
            entrance.transform.position = new Vector3(0, 1.5f, -8);
            entrance.transform.localScale = new Vector3(4, 3, 1);
            entrance.GetComponent<Renderer>().material.color = Color.green;

            // Shelves
            var shelf1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf1.name = "Shelf_Food";
            shelf1.transform.SetParent(walls.transform);
            shelf1.transform.position = new Vector3(-5, 1.5f, 0);
            shelf1.transform.localScale = new Vector3(2, 3, 8);
            shelf1.GetComponent<Renderer>().material.color = Color.cyan;

            var shelf2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf2.name = "Shelf_Drinks";
            shelf2.transform.SetParent(walls.transform);
            shelf2.transform.position = new Vector3(5, 1.5f, 0);
            shelf2.transform.localScale = new Vector3(2, 3, 8);
            shelf2.GetComponent<Renderer>().material.color = Color.blue;

            // Cashier counter
            var counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "CashierCounter";
            counter.transform.SetParent(walls.transform);
            counter.transform.position = new Vector3(0, 1f, 8);
            counter.transform.localScale = new Vector3(6, 2, 2);
            counter.GetComponent<Renderer>().material.color = Color.gray;

            // Instantiate items on shelf
            var onigiriGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            onigiriGo.name = "Onigiri";
            onigiriGo.transform.position = new Vector3(-5, 1.8f, 0); // On food shelf
            onigiriGo.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            onigiriGo.GetComponent<Renderer>().material.color = Color.white;
            onigiriGo.layer = interactableLayer;
            var onigiriInteract = onigiriGo.AddComponent<InteractiveItem>();
            var serializedOnigiri = new SerializedObject(onigiriInteract);
            serializedOnigiri.FindProperty("itemId").stringValue = "onigiri";
            serializedOnigiri.FindProperty("promptJa").stringValue = "おにぎりを取る";
            serializedOnigiri.FindProperty("promptVi").stringValue = "Lấy cơm nắm";
            serializedOnigiri.ApplyModifiedProperties();

            // Incorrect item (Water)
            var waterGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            waterGo.name = "Water";
            waterGo.transform.position = new Vector3(5, 1.8f, 0); // On drinks shelf
            waterGo.transform.localScale = new Vector3(0.4f, 0.6f, 0.4f);
            waterGo.GetComponent<Renderer>().material.color = Color.blue;
            waterGo.layer = interactableLayer;
            var waterInteract = waterGo.AddComponent<InteractiveItem>();
            var serializedWater = new SerializedObject(waterInteract);
            serializedWater.FindProperty("itemId").stringValue = "water";
            serializedWater.FindProperty("promptJa").stringValue = "水を調べる";
            serializedWater.FindProperty("promptVi").stringValue = "Kiểm tra nước";
            serializedWater.FindProperty("destroyOnInteract").boolValue = false;
            serializedWater.ApplyModifiedProperties();

            // Cashier NPC
            var npcGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npcGo.name = "CashierNPC";
            npcGo.transform.position = new Vector3(0, 1.5f, 9.5f); // Behind cashier counter
            npcGo.transform.localScale = new Vector3(1, 1.5f, 1);
            npcGo.GetComponent<Renderer>().material.color = Color.magenta;
            npcGo.layer = interactableLayer;
            var npcCtrl = npcGo.AddComponent<NPCController>();
            var serializedNPC = new SerializedObject(npcCtrl);
            serializedNPC.FindProperty("npcId").stringValue = "npc_cashier";
            serializedNPC.FindProperty("displayName").stringValue = "Thu ngân";
            serializedNPC.FindProperty("role").stringValue = "Cashier";
            serializedNPC.ApplyModifiedProperties();

            // Scene Initializer
            var initGo = new GameObject("ScenarioSceneInitializer");
            initGo.AddComponent<ScenarioSceneInitializer>();

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[SceneBuilder] Saved Test Sandbox scene to: {path}");
        }

        private static GameObject CreateUIButton(Transform parent, string name, string text, Vector2 pos)
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

        private static GameObject CreateScoreText(Transform parent, string name, string label, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(400, 30);
            
            var txt = go.AddComponent<TextMeshProUGUI>();
            txt.text = label + "0";
            txt.fontSize = 16;
            txt.alignment = TextAlignmentOptions.Center;
            
            return go;
        }

        private static void CreateEventSystem()
        {
            var es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }
    }
}
