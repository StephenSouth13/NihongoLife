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
    public static class SceneBuilder
    {
        private const int InteractableLayer = 6;
        private const string ScenesDir = "Assets/NihongoLife/Scenes";
        private const string SandboxScenePath = ScenesDir + "/90_TestSandbox.unity";
        private const string ControlScenePath = ScenesDir + "/99_ControlRoom.unity";
        private const string ControlDatabasePath = "Assets/NihongoLife/Resources/Control/NihongoLifeControlDatabase.asset";

        // [MenuItem("NihongoLife/Build All Scenes")]
        public static void BuildAllScenes()
        {
            Debug.Log("[SceneBuilder] Building real playable NihongoLife scenes...");
            string previousScenePath = EditorSceneManager.GetActiveScene().path;

            try
            {
                EnsureFolder("Assets/NihongoLife", "Scenes");
                FontSetup.EnsureJapaneseFontAsset(forceRecreate: true);
                ScenarioAssetBuilder.BuildScenarioAssets();
                CharacterBuilder.BuildCharacterSystem();

                BuildBootstrapScene(ScenesDir + "/00_Bootstrap.unity");
                BuildMainMenuScene(ScenesDir + "/01_MainMenu.unity");
                BuildSandboxScene(SandboxScenePath);
                BuildControlScene(ControlScenePath);

                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(ScenesDir + "/00_Bootstrap.unity", true),
                    new EditorBuildSettingsScene(ScenesDir + "/01_MainMenu.unity", true),
                    new EditorBuildSettingsScene(SandboxScenePath, true),
                    new EditorBuildSettingsScene(ControlScenePath, false)
                };

                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(SandboxScenePath);
                Debug.Log("[SceneBuilder] Done. Opened 90_TestSandbox.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                if (!string.IsNullOrEmpty(previousScenePath) && System.IO.File.Exists(previousScenePath))
                {
                    EditorSceneManager.OpenScene(previousScenePath);
                }
            }
        }

        // [MenuItem("NihongoLife/Rebuild Main Menu")]
        public static void RebuildMainMenu()
        {
            EnsureFolder("Assets/NihongoLife", "Scenes");
            FontSetup.EnsureJapaneseFontAsset(forceRecreate: true);
            BuildMainMenuScene(ScenesDir + "/01_MainMenu.unity");
            EditorSceneManager.OpenScene(ScenesDir + "/01_MainMenu.unity");
            Debug.Log("[SceneBuilder] Rebuilt 01_MainMenu with 90_TestSandbox town preview.");
        }

        // [MenuItem("NihongoLife/Rebuild Gameplay Sandbox")]
        public static void RebuildGameplaySandbox()
        {
            EnsureFolder("Assets/NihongoLife", "Scenes");
            FontSetup.EnsureJapaneseFontAsset(forceRecreate: true);
            ScenarioAssetBuilder.BuildScenarioAssets();
            CharacterBuilder.BuildCharacterSystem();
            BuildSandboxScene(SandboxScenePath);
            EditorSceneManager.OpenScene(SandboxScenePath);
            Debug.Log("[SceneBuilder] Rebuilt 90_TestSandbox with gameplay, shop door, and interior camera zone.");
        }

        // [MenuItem("NihongoLife/Rebuild Control Room")]
        public static void RebuildControlRoom()
        {
            EnsureFolder("Assets/NihongoLife", "Scenes");
            BuildControlScene(ControlScenePath);
            EditorSceneManager.OpenScene(ControlScenePath);
            Debug.Log("[SceneBuilder] Rebuilt 99_ControlRoom. Edit NihongoLifeControlDatabase in the Inspector.");
        }

        private static void BuildBootstrapScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var appRootGo = new GameObject("AppRoot");
            var appRoot = appRootGo.AddComponent<AppRoot>();
            var audioService = appRootGo.AddComponent<AudioService>();
            var sceneFlow = appRootGo.AddComponent<SceneFlowController>();
            var settings = appRootGo.AddComponent<GameSettingsService>();
            var control = appRootGo.AddComponent<GameControlService>();
            appRootGo.AddComponent<GeminiConversationService>();

            var so = new SerializedObject(appRoot);
            SetRef(so, "audioService", audioService);
            SetRef(so, "sceneFlowController", sceneFlow);
            SetRef(so, "settingsService", settings);
            SetRef(so, "controlService", control);
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, path);
        }

        private static void BuildMainMenuScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            CreateAppRootForDirectPlay();
            CreateEventSystem();
            CreateLighting();
            CreateMainMenuTownPreview();

            var font = FontSetup.EnsureJapaneseFontAsset();
            var canvasGo = CreateCanvas("Canvas");

            var panel = CreateFullScreenPanel(canvasGo.transform, "MainMenuPanel", new Color(0.03f, 0.035f, 0.04f, 0.08f));
            AddTopAccent(panel.transform);

            var title = CreateText(panel.transform, "TitleText", "NIHONGO LIFE", font, 86, new Vector2(0, 200), new Vector2(900, 110), TextAlignmentOptions.Center);
            title.color = new Color(1f, 0.98f, 0.9f);
            title.fontStyle = FontStyles.Bold;
            var subtitle = CreateText(panel.transform, "SubtitleText", "コンビニで買い物 / luyện hội thoại mua hàng", font, 28, new Vector2(0, 115), new Vector2(900, 44), TextAlignmentOptions.Center);
            subtitle.color = new Color(0.9f, 0.95f, 1f);

            var startBtn = CreateUIButton(panel.transform, "StartButton", "Bắt đầu", new Vector2(0, -10), new Vector2(340, 68), font, true).GetComponent<Button>();
            var quitBtn = CreateUIButton(panel.transform, "QuitButton", "Thoát", new Vector2(0, -95), new Vector2(340, 62), font, false).GetComponent<Button>();
            var profileText = CreateText(panel.transform, "ProfileText", "WASD di chuyển  |  E tương tác  |  B balo  |  Tab nhân vật", font, 20, new Vector2(0, -195), new Vector2(760, 48), TextAlignmentOptions.Center);
            profileText.color = new Color(0.95f, 0.95f, 0.95f);

            var uiManagerGo = new GameObject("UIManager");
            var uiManager = uiManagerGo.AddComponent<UIManager>();
            var mainMenu = panel.AddComponent<MainMenuUI>();

            var menuSo = new SerializedObject(mainMenu);
            SetRef(menuSo, "startButton", startBtn);
            SetRef(menuSo, "quitButton", quitBtn);
            SetRef(menuSo, "profileText", profileText);
            menuSo.ApplyModifiedProperties();

            var uiSo = new SerializedObject(uiManager);
            SetRef(uiSo, "mainMenuPanel", mainMenu);
            uiSo.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, path);
        }

        private static void BuildSandboxScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            CreateAppRootForDirectPlay();
            CreateLighting();

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "GameplayGround";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(132f, 1f, 10000f);
            ground.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMat("NL_Ground_Mat", new Color(0.16f, 0.17f, 0.17f));

            var environment = new GameObject("Environment");
            CreateEndlessCityVisuals(environment.transform);
            BuildStoreGameplay(environment.transform);
            CreateWorldSafety(environment.transform);

            var player = CreatePlayer();
            CreateCamera(player.transform);
            CreateManagers();
            CreateGameUI(player);

            var initGo = new GameObject("ScenarioSceneInitializer");
            initGo.AddComponent<ScenarioSceneInitializer>();

            EditorSceneManager.SaveScene(scene, path);
        }

        private static void BuildControlScene(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            CreateEventSystem();
            CreateLighting();

            var database = EnsureControlDatabaseAsset();
            var root = new GameObject("NihongoLifeControlPanel");
            var controlService = root.AddComponent<GameControlService>();
            var so = new SerializedObject(controlService);
            SetRef(so, "database", database);
            so.ApplyModifiedProperties();

            var font = FontSetup.EnsureJapaneseFontAsset();
            var canvasGo = CreateCanvas("Canvas");
            var panel = CreateFullScreenPanel(canvasGo.transform, "ControlRoomPanel", new Color(0.025f, 0.03f, 0.036f, 1f));
            AddTopAccent(panel.transform);
            var title = CreateText(panel.transform, "TitleText", "NIHONGO LIFE CONTROL ROOM", font, 44, new Vector2(0f, 220f), new Vector2(1100f, 70f), TextAlignmentOptions.Center);
            title.color = new Color(1f, 0.91f, 0.54f);
            var body = CreateText(panel.transform, "BodyText",
                "Edit the NihongoLifeControlDatabase asset in the Inspector.\n\n" +
                "- Active scenario controls which mission starts in 90_TestSandbox.\n" +
                "- Menu copy controls the main menu text.\n" +
                "- Voice lines let you assign Japanese/English clips and IPA per dialogue node.\n" +
                "- Online database fields are config only. Keep passwords in environment variables.\n\n" +
                "This scene is for creators and is not added to runtime build settings.",
                font, 22, new Vector2(0f, 40f), new Vector2(980f, 360f), TextAlignmentOptions.Center);
            body.textWrappingMode = TextWrappingModes.Normal;

            Selection.activeObject = database;
            EditorSceneManager.SaveScene(scene, path);
        }

        private static GameControlDatabase EnsureControlDatabaseAsset()
        {
            EnsureFolderExists("Assets/NihongoLife/Resources/Control");
            var database = AssetDatabase.LoadAssetAtPath<GameControlDatabase>(ControlDatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<GameControlDatabase>();
                AssetDatabase.CreateAsset(database, ControlDatabasePath);
            }

            database.scenarios.Clear();
            foreach (var scenario in Resources.LoadAll<ScenarioDefinition>("Scenarios"))
            {
                if (scenario != null && !database.scenarios.Contains(scenario))
                {
                    database.scenarios.Add(scenario);
                }
            }

            database.activeScenarioId = "scenario.street.first_talk";
            EnsureVoiceLineRows(database);

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            return database;
        }

        private static void EnsureVoiceLineRows(GameControlDatabase database)
        {
            if (database.voiceLines == null)
            {
                database.voiceLines = new System.Collections.Generic.List<VoiceLineEntry>();
            }

            foreach (var scenario in database.scenarios)
            {
                if (scenario == null || scenario.nodes == null) continue;
                foreach (var node in scenario.nodes)
                {
                    if (node == null || node.nodeType != ScenarioNodeType.Dialogue) continue;
                    EnsureVoiceLine(database, node.id, GameLanguage.Japanese, node.textEnglishIpa);
                    EnsureVoiceLine(database, node.id, GameLanguage.English, node.textEnglishIpa);
                }
            }
        }

        private static void EnsureVoiceLine(GameControlDatabase database, string nodeId, GameLanguage language, string ipa)
        {
            if (database.voiceLines.Exists(v => v != null && v.nodeId == nodeId && v.language == language)) return;
            database.voiceLines.Add(new VoiceLineEntry
            {
                nodeId = nodeId,
                language = language,
                englishIpa = ipa
            });
        }

        private static void CreateMainMenuTownPreview()
        {
            var previewRoot = new GameObject("MenuTownPreview_90_TestSandbox");
            VisualEnvironmentBuilder.GenerateVisualEnvironment(previewRoot);
            CreateMenuPreviewGround(previewRoot.transform);
            // ToneDownMenuPreviewMaterials(previewRoot); // Removed to keep vibrant colors

            var target = new GameObject("MenuCameraTarget");
            target.transform.position = new Vector3(0f, 1.2f, -10f);

            var cameraGo = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>() ?? cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 38f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 240f;

            if (cameraGo.GetComponent<AudioListener>() == null)
            {
                cameraGo.AddComponent<AudioListener>();
            }

            var orbit = cameraGo.GetComponent<MenuCameraOrbit>() ?? cameraGo.AddComponent<MenuCameraOrbit>();
            orbit.Configure(44f, 15.5f, 2.15f, 205f, 3.1f);
            orbit.SetTarget(target.transform);
        }

        private static void CreateMenuPreviewGround(Transform parent)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "MenuPreviewGround";
            ground.transform.SetParent(parent);
            ground.transform.position = new Vector3(0f, -0.62f, -10f);
            ground.transform.localScale = new Vector3(96f, 0.18f, 72f);
            ground.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMat("NL_MenuPreviewGround_Mat", new Color(0.18f, 0.2f, 0.18f, 1f));
            UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
        }

        private static void ToneDownMenuPreviewMaterials(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null) continue;

                    Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
                    bool tooCyan = color.g > 0.7f && color.b > 0.7f && color.r < 0.25f;
                    bool tooBright = color.maxColorComponent > 0.95f;
                    if (!tooCyan && !tooBright) continue;

                    var toned = new Material(Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard"));
                    toned.name = material.name + "_MenuToned";
                    toned.color = new Color(0.16f, 0.2f, 0.22f, color.a);
                    materials[i] = toned;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static void CreateAppRootForDirectPlay()
        {
            var appRootGo = new GameObject("AppRoot");
            var appRoot = appRootGo.AddComponent<AppRoot>();
            var audioService = appRootGo.AddComponent<AudioService>();
            var sceneFlow = appRootGo.AddComponent<SceneFlowController>();
            var settings = appRootGo.AddComponent<GameSettingsService>();
            var control = appRootGo.AddComponent<GameControlService>();
            appRootGo.AddComponent<GeminiConversationService>();

            var so = new SerializedObject(appRoot);
            SetRef(so, "audioService", audioService);
            SetRef(so, "sceneFlowController", sceneFlow);
            SetRef(so, "settingsService", settings);
            SetRef(so, "controlService", control);
            so.ApplyModifiedProperties();
        }

        private static GameObject CreatePlayer()
        {
            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(0f, 0.05f, -13.5f);

            var cc = playerGo.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 1f, 0f);
            cc.height = 2f;
            cc.radius = 0.35f;
            cc.skinWidth = 0.05f;
            cc.stepOffset = 0.35f;

            var playerCtrl = playerGo.AddComponent<PlayerController>();
            var detector = playerGo.AddComponent<InteractionDetector>();
            playerGo.AddComponent<PlayerInventory>();
            playerGo.AddComponent<PlayerStatus>();
            playerGo.AddComponent<WorldBoundsGuard>();
            var animCtrl = playerGo.AddComponent<CharacterAnimationController>();

            var visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Characters/NL_Player.prefab");
            if (visualPrefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);
                visual.name = "Visual";
                visual.transform.SetParent(playerGo.transform, false);
                visual.transform.localPosition = Vector3.zero;
                StripColliders(visual);
                animCtrl.SetAnimator(visual.GetComponentInChildren<Animator>(true));
            }
            else
            {
                CreateFallbackPerson(playerGo.transform, "FallbackPlayer", new Color(0.2f, 0.45f, 0.95f));
            }

            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(playerGo.transform, false);
            groundCheck.transform.localPosition = new Vector3(0f, 0.12f, 0f);

            var playerSo = new SerializedObject(playerCtrl);
            SetRef(playerSo, "groundCheck", groundCheck.transform);
            SetFloat(playerSo, "groundDistance", 0.28f);
            SetInt(playerSo, "groundMask", 1);
            playerSo.ApplyModifiedProperties();

            var detectorSo = new SerializedObject(detector);
            SetFloat(detectorSo, "detectionRadius", 4.2f);
            SetInt(detectorSo, "interactableLayers", 1 << InteractableLayer);
            detectorSo.ApplyModifiedProperties();

            return playerGo;
        }

        private static void CreateCamera(Transform player)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 58f;
            cam.nearClipPlane = 0.03f;
            camGo.AddComponent<AudioListener>();
            var controller = camGo.AddComponent<ThirdPersonCameraController>();
            controller.SetTarget(player);
            controller.SetOrbit(0f, 16f, 7.2f);
        }

        private static void CreateManagers()
        {
            var managerGo = new GameObject("ScenarioManager");
            managerGo.AddComponent<ScenarioManager>();
            managerGo.AddComponent<ScoringManager>();
            managerGo.AddComponent<DialogueManager>();
            managerGo.AddComponent<LearningMasteryManager>();
            managerGo.AddComponent<SpeechPracticeController>();
            managerGo.AddComponent<TownAmbientAudio>();
            managerGo.AddComponent<SupabaseVoiceSyncService>();
            managerGo.AddComponent<GeminiConversationService>();
            managerGo.AddComponent<RuntimeCollisionRepair>();
        }

        private static void CreateEndlessCityVisuals(Transform environment)
        {
            const float tileLength = 100f;
            Transform[] tiles = new Transform[3];
            for (int i = 0; i < tiles.Length; i++)
            {
                var tile = new GameObject($"CityTile_{i - 1}");
                tile.transform.SetParent(environment);
                tile.transform.position = Vector3.zero;
                VisualEnvironmentBuilder.GenerateVisualEnvironment(tile);
                tile.transform.position = new Vector3(0f, 0f, (i - 1) * tileLength);
                tiles[i] = tile.transform;
            }

            var looper = environment.gameObject.AddComponent<EndlessCityLooper>();
            var so = new SerializedObject(looper);
            SetFloat(so, "tileLength", tileLength);
            var tileProp = so.FindProperty("tileRoots");
            tileProp.arraySize = tiles.Length;
            for (int i = 0; i < tiles.Length; i++)
            {
                tileProp.GetArrayElementAtIndex(i).objectReferenceValue = tiles[i];
            }
            so.ApplyModifiedProperties();
        }

        private static void CreateWorldSafety(Transform root)
        {
            var safety = new GameObject("WorldFallSafety");
            safety.transform.SetParent(root);
            CreateInvisibleWall(safety.transform, "WestWall", new Vector3(-66f, 2.4f, -6f), new Vector3(1f, 4.8f, 104f));
            CreateInvisibleWall(safety.transform, "EastWall", new Vector3(66f, 2.4f, -6f), new Vector3(1f, 4.8f, 104f));
        }

        private static void CreateInvisibleWall(Transform root, string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(root);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            DestroyRenderer(wall);
        }

        private static void BuildStoreGameplay(Transform root)
        {
            BuildNeighborhoodGameplay(root);

            var door = new GameObject("StoreDoor");
            door.layer = InteractableLayer;
            door.transform.SetParent(root);
            door.transform.position = new Vector3(0f, 1.15f, 0.65f);
            var trigger = door.AddComponent<BoxCollider>();
            trigger.size = new Vector3(3.4f, 2.5f, 1.9f);
            trigger.isTrigger = true;

            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "DoorBlocker";
            blocker.transform.SetParent(door.transform, false);
            blocker.transform.localPosition = new Vector3(0f, 0f, 0.25f);
            blocker.transform.localScale = new Vector3(2.2f, 2.2f, 0.12f);
            DestroyRenderer(blocker);

            var doorVisual = new GameObject("AutomaticSlidingGlassDoor");
            doorVisual.transform.SetParent(door.transform, false);
            doorVisual.transform.localPosition = Vector3.zero;

            var frameMat = CreateRuntimeMat("NL_Door_Frame_Mat", new Color(0.08f, 0.1f, 0.12f, 1f));
            var glassMat = CreateRuntimeMat("NL_Door_Glass_Mat", new Color(0.45f, 0.78f, 0.95f, 0.52f));
            var handleMat = CreateRuntimeMat("NL_Door_Handle_Mat", new Color(0.86f, 0.88f, 0.84f, 1f));

            CreateDoorPiece(doorVisual.transform, "TopRail", new Vector3(0f, 1.18f, -0.02f), new Vector3(3.75f, 0.13f, 0.16f), frameMat);
            CreateDoorPiece(doorVisual.transform, "BottomRail", new Vector3(0f, -1.05f, -0.02f), new Vector3(3.75f, 0.1f, 0.14f), frameMat);
            CreateDoorPiece(doorVisual.transform, "LeftFrame", new Vector3(-1.9f, 0.02f, -0.02f), new Vector3(0.12f, 2.35f, 0.16f), frameMat);
            CreateDoorPiece(doorVisual.transform, "RightFrame", new Vector3(1.9f, 0.02f, -0.02f), new Vector3(0.12f, 2.35f, 0.16f), frameMat);

            var leftPanel = CreateDoorPiece(doorVisual.transform, "GlassPanel_Left", new Vector3(-0.48f, 0.02f, -0.04f), new Vector3(0.92f, 2.05f, 0.055f), glassMat);
            var rightPanel = CreateDoorPiece(doorVisual.transform, "GlassPanel_Right", new Vector3(0.48f, 0.02f, -0.07f), new Vector3(0.92f, 2.05f, 0.055f), glassMat);
            CreateDoorPiece(leftPanel.transform, "Handle_Left", new Vector3(0.34f, 0f, -0.08f), new Vector3(0.05f, 0.55f, 0.05f), handleMat);
            CreateDoorPiece(rightPanel.transform, "Handle_Right", new Vector3(-0.34f, 0f, -0.08f), new Vector3(0.05f, 0.55f, 0.05f), handleMat);

            var doorSo = new SerializedObject(door.AddComponent<DoorInteractable>());
            SetString(doorSo, "areaId", "store_entrance");
            SetRef(doorSo, "doorVisual", doorVisual.transform);
            SetRef(doorSo, "leftDoorPanel", leftPanel.transform);
            SetRef(doorSo, "rightDoorPanel", rightPanel.transform);
            SetRef(doorSo, "blockingCollider", blocker.GetComponent<Collider>());
            doorSo.ApplyModifiedProperties();

            CreateStoreCameraZone(root);

            AddStoreSign(root, new Vector3(0f, 3.15f, 1.05f));
            CreateShelf(root, "Shelf_Food", new Vector3(-3.2f, 1.1f, 4.1f));
            CreateShelf(root, "Shelf_Drinks", new Vector3(3.2f, 1.1f, 4.1f));
            CreateCounter(root, new Vector3(0f, 0.55f, 8.1f));
            CreateItem(root, "Onigiri", "onigiri", "おにぎり", "Cơm nắm", "おにぎりを取る", "Lấy cơm nắm", 497, true, true, new Vector3(-3.2f, 1.85f, 4.05f), "Assets/NihongoLife/Prefabs/Food/food_apple.prefab");
            CreateItem(root, "Water", "water", "水", "Nước", "水を調べる", "Kiểm tra nước", 120, false, false, new Vector3(3.2f, 1.9f, 4.05f), "Assets/NihongoLife/Prefabs/Food/food_bottle.prefab");
            CreateItem(root, "Tea", "tea", "お茶", "Trà xanh", "お茶を調べる", "Kiểm tra trà xanh", 150, false, false, new Vector3(3.95f, 1.9f, 4.05f), "Assets/NihongoLife/Prefabs/Food/food_bottle.prefab");
            CreateCashier(root);
        }

        private static GameObject CreateDoorPiece(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = localScale;
            piece.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(piece.GetComponent<Collider>());
            return piece;
        }

        private static void CreateStoreCameraZone(Transform root)
        {
            var zone = new GameObject("StoreInteriorCameraZone");
            zone.transform.SetParent(root);
            zone.transform.position = new Vector3(0f, 1.1f, 5.2f);
            var collider = zone.AddComponent<BoxCollider>();
            collider.size = new Vector3(9f, 2.8f, 9.5f);
            collider.isTrigger = true;
            zone.AddComponent<StoreCameraZone>();
        }

        private static void BuildNeighborhoodGameplay(Transform root)
        {
            CreateGuideNPC(root);
            CreateNeighborNPC(root, "Neighbor_1", "npc_neighbor_1", "Tanaka", "scenario.house1.greeting", new Vector3(-22f, 0.05f, -0.5f));
            CreateNeighborNPC(root, "Neighbor_2", "npc_neighbor_2", "Suzuki", "scenario.house2.lostcat", new Vector3(-11f, 0.05f, -0.5f));
            CreateNeighborNPC(root, "Neighbor_3", "npc_neighbor_3", "Sato", "scenario.house3.garbage", new Vector3(12f, 0.05f, -0.5f));
        }

        private static void CreateGuideNPC(Transform root)
        {
            var guide = CreateNpcShell(root, "GuideNPC", "npc_guide", "Lilly", "Language Guide", new Vector3(-2.8f, 0.05f, -13.2f), Quaternion.Euler(0f, 25f, 0f));
            AddNpcVisual(guide, "Assets/NihongoLife/Prefabs/Characters/NL_Guide.prefab", new Color(0.9f, 0.64f, 0.28f));

            var so = new SerializedObject(guide.GetComponent<NPCController>());
            SetString(so, "promptJa", "練習する");
            SetString(so, "promptEn", "Practice phrases");
            SetString(so, "fallbackJa", "こんにちは。私はリリーです。一緒に日本語を練習しましょう。");
            SetString(so, "fallbackReading", "こんにちは。わたしはリリーです。いっしょににほんごをれんしゅうしましょう。");
            SetString(so, "fallbackEn", "Hi, I am Lilly. Let's practice useful Japanese phrases together.");
            SetString(so, "fallbackRomaji", "Konnichiwa. Watashi wa Riri desu. Issho ni nihongo wo renshuu shimashou.");
            so.ApplyModifiedProperties();
        }

        private static void CreateNeighborNPC(Transform root, string goName, string npcId, string displayName, string scenarioAreaId, Vector3 position)
        {
            var npc = CreateNpcShell(root, goName, npcId, displayName, "Neighbor", position, Quaternion.Euler(0f, 180f, 0f));
            AddNpcVisual(npc, "Assets/NihongoLife/Prefabs/Characters/NL_Neighbor.prefab", new Color(0.2f, 0.7f, 0.3f));
            if (npcId == "npc_neighbor_1")
            {
                AddPatrolRoute(npc, root, goName + "_Route", position + new Vector3(-2.5f, 0f, -1.4f), position + new Vector3(2.5f, 0f, -1.4f));
            }

            var so = new SerializedObject(npc.GetComponent<NPCController>());
            SetString(so, "npcId", npcId);
            SetString(so, "displayName", displayName);
            SetString(so, "role", "Neighbor");
            SetString(so, "promptJa", "話す");
            SetString(so, "promptEn", "Talk");
            SetString(so, "scenarioAreaIdOnInteract", scenarioAreaId);
            SetString(so, "fallbackJa", "こんにちは。どこへ行きますか。");
            SetString(so, "fallbackReading", "こんにちは。どこへいきますか。");
            SetString(so, "fallbackEn", "Hello. Where are you going?");
            SetString(so, "fallbackRomaji", "Konnichiwa. Doko e ikimasu ka.");
            so.ApplyModifiedProperties();
        }

        private static GameObject CreateNpcShell(Transform root, string goName, string npcId, string displayName, string role, Vector3 position, Quaternion rotation)
        {
            var npc = new GameObject(goName);
            npc.layer = InteractableLayer;
            npc.transform.SetParent(root);
            npc.transform.position = position;
            npc.transform.rotation = rotation;

            var collider = npc.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.95f, 0f);
            collider.size = new Vector3(0.9f, 1.9f, 0.9f);
            collider.isTrigger = true;

            var body = npc.AddComponent<CharacterController>();
            body.center = new Vector3(0f, 0.95f, 0f);
            body.height = 1.85f;
            body.radius = 0.32f;
            body.stepOffset = 0.22f;

            npc.AddComponent<CharacterAnimationController>();
            npc.AddComponent<NPCAmbientTalker>();
            var controller = npc.AddComponent<NPCController>();
            var so = new SerializedObject(controller);
            SetString(so, "npcId", npcId);
            SetString(so, "displayName", displayName);
            SetString(so, "role", role);
            so.ApplyModifiedProperties();
            return npc;
        }

        private static void AddPatrolRoute(GameObject npc, Transform root, string routeName, params Vector3[] points)
        {
            var route = new GameObject(routeName);
            route.transform.SetParent(root);
            Transform[] waypoints = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                var waypoint = new GameObject($"Waypoint_{i + 1}");
                waypoint.transform.SetParent(route.transform);
                waypoint.transform.position = points[i];
                waypoints[i] = waypoint.transform;
            }

            var patrol = npc.AddComponent<NPCStreetPatrol>();
            var so = new SerializedObject(patrol);
            var prop = so.FindProperty("waypoints");
            prop.arraySize = waypoints.Length;
            for (int i = 0; i < waypoints.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
            }
            SetFloat(so, "walkSpeed", 1.05f);
            SetFloat(so, "waitSeconds", 1.4f);
            so.ApplyModifiedProperties();
        }

        private static void AddNpcVisual(GameObject npc, string prefabPath, Color fallbackColor)
        {
            var animCtrl = npc.GetComponent<CharacterAnimationController>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                visual.name = "Visual";
                visual.transform.SetParent(npc.transform, false);
                visual.transform.localPosition = Vector3.zero;
                StripColliders(visual);
                animCtrl.SetAnimator(visual.GetComponentInChildren<Animator>(true));
            }
            else
            {
                CreateFallbackPerson(npc.transform, "FallbackNPC", fallbackColor);
            }
        }

        private static void CreateShelf(Transform root, string name, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root);
            go.transform.position = position;
            go.GetComponent<BoxCollider>().size = new Vector3(2.3f, 2.2f, 0.9f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Furniture/shelf.prefab");
            if (prefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                visual.transform.SetParent(go.transform, false);
                visual.transform.localPosition = new Vector3(0f, -1.1f, 0f);
                StripColliders(visual);
                DestroyRenderer(go);
            }
        }

        private static void CreateCounter(Transform root, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "CashierCounter";
            go.transform.SetParent(root);
            go.transform.position = position;
            go.GetComponent<BoxCollider>().size = new Vector3(3.4f, 1.1f, 1.1f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Furniture/counter.prefab");
            if (prefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                visual.transform.SetParent(go.transform, false);
                visual.transform.localPosition = new Vector3(0f, -0.55f, 0f);
                StripColliders(visual);
                DestroyRenderer(go);
            }
        }

        private static void CreateItem(Transform root, string name, string itemId, string ja, string vi, string promptJa, string promptEn, int price, bool destroyOnInteract, bool addToInventory, Vector3 position, string prefabPath)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            item.name = name;
            item.layer = InteractableLayer;
            item.transform.SetParent(root);
            item.transform.position = position;
            item.transform.localScale = Vector3.one * 0.55f;
            item.GetComponent<SphereCollider>().isTrigger = true;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                visual.transform.SetParent(item.transform, false);
                visual.transform.localPosition = Vector3.zero;
                StripColliders(visual);
                DestroyRenderer(item);
            }

            var so = new SerializedObject(item.AddComponent<InteractiveItem>());
            SetString(so, "itemId", itemId);
            SetString(so, "displayNameJa", ja);
            SetString(so, "displayNameEn", vi);
            SetString(so, "promptJa", promptJa);
            SetString(so, "promptEn", promptEn);
            SetInt(so, "priceYen", price);
            SetBool(so, "addToInventory", addToInventory);
            SetBool(so, "destroyOnInteract", destroyOnInteract);
            so.ApplyModifiedProperties();
        }

        private static void CreateCashier(Transform root)
        {
            var npc = new GameObject("CashierNPC");
            npc.layer = InteractableLayer;
            npc.transform.SetParent(root);
            npc.transform.position = new Vector3(0f, 0.05f, 9.55f);
            npc.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var collider = npc.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.95f, 0f);
            collider.size = new Vector3(0.9f, 1.9f, 0.9f);
            collider.isTrigger = true;

            var animCtrl = npc.AddComponent<CharacterAnimationController>();
            npc.AddComponent<NPCAmbientTalker>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Characters/NL_Cashier.prefab");
            if (prefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                visual.name = "Visual";
                visual.transform.SetParent(npc.transform, false);
                visual.transform.localPosition = Vector3.zero;
                StripColliders(visual);
                animCtrl.SetAnimator(visual.GetComponentInChildren<Animator>(true));
            }
            else
            {
                CreateFallbackPerson(npc.transform, "FallbackCashier", new Color(0.05f, 0.42f, 0.48f));
            }

            var so = new SerializedObject(npc.AddComponent<NPCController>());
            SetString(so, "npcId", "npc_cashier");
            SetString(so, "displayName", "Thu ngân");
            SetString(so, "role", "Cashier");
            SetString(so, "promptJa", "会計する");
            SetString(so, "promptEn", "Thanh toán");
            SetString(so, "scenarioAreaIdOnInteract", "cashier");
            so.ApplyModifiedProperties();
        }

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

            var topLeft = CreatePanel(hudPanel.transform, "MissionPanel", new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(620f, 188f), new Color(0.025f, 0.035f, 0.045f, 0.88f));
            var scenarioTitle = CreateText(topLeft.transform, "ScenarioTitle", "Nhiệm vụ", font, 21, new Vector2(18f, -20f), new Vector2(480f, 34f), TextAlignmentOptions.Left);
            scenarioTitle.color = new Color(1f, 0.91f, 0.54f);
            var objectives = CreateText(topLeft.transform, "ObjectivesList", "", font, 17, new Vector2(18f, -68f), new Vector2(584f, 104f), TextAlignmentOptions.TopLeft);
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

            var dialoguePanel = CreatePanel(hudPanel.transform, "DialoguePanel", new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(900f, 275f), new Color(0.035f, 0.045f, 0.055f, 0.94f));
            var speaker = CreateText(dialoguePanel.transform, "SpeakerText", "", font, 18, new Vector2(24f, -22f), new Vector2(830f, 28f), TextAlignmentOptions.Left);
            speaker.color = new Color(1f, 0.91f, 0.54f);
            var japanese = CreateText(dialoguePanel.transform, "JapaneseText", "", font, 28, new Vector2(24f, -62f), new Vector2(830f, 42f), TextAlignmentOptions.Left);
            var reading = CreateText(dialoguePanel.transform, "ReadingText", "", font, 16, new Vector2(24f, -104f), new Vector2(830f, 26f), TextAlignmentOptions.Left);
            reading.color = new Color(0.72f, 0.82f, 0.9f);
            var romaji = CreateText(dialoguePanel.transform, "RomajiText", "", font, 15, new Vector2(24f, -132f), new Vector2(830f, 24f), TextAlignmentOptions.Left);
            romaji.color = new Color(0.72f, 0.82f, 0.9f);
            var translation = CreateText(dialoguePanel.transform, "TranslationText", "", font, 17, new Vector2(24f, -160f), new Vector2(830f, 34f), TextAlignmentOptions.Left);

            var choicesContainer = new GameObject("ChoicesContainer");
            choicesContainer.transform.SetParent(dialoguePanel.transform, false);
            var choicesRect = choicesContainer.AddComponent<RectTransform>();
            choicesRect.anchorMin = new Vector2(0f, 0f);
            choicesRect.anchorMax = new Vector2(1f, 0f);
            choicesRect.pivot = new Vector2(0.5f, 0f);
            choicesRect.anchoredPosition = new Vector2(0f, 16f);
            choicesRect.sizeDelta = new Vector2(-36f, 68f);
            var layout = choicesContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var choicePrefab = CreateUIButton(canvasGo.transform, "ChoiceButtonPrefab", "Choice", Vector2.zero, new Vector2(240f, 58f), font, false).GetComponent<Button>();
            choicePrefab.gameObject.SetActive(false);
            var continueButton = CreateUIButton(dialoguePanel.transform, "ContinueButton", "Tiếp tục", new Vector2(330f, 28f), new Vector2(170f, 48f), font, true).GetComponent<Button>();

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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
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

        private static void CreateFallbackPerson(Transform parent, string name, Color bodyColor)
        {
            var bodyMat = CreateRuntimeMat(name + "_BodyMat", bodyColor);
            var skinMat = CreateRuntimeMat(name + "_SkinMat", new Color(0.9f, 0.72f, 0.58f));

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = name + "_Body";
            body.transform.SetParent(parent, false);
            body.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            body.transform.localScale = new Vector3(0.55f, 0.85f, 0.55f);
            body.GetComponent<Renderer>().sharedMaterial = bodyMat;
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = name + "_Head";
            head.transform.SetParent(parent, false);
            head.transform.localPosition = new Vector3(0f, 1.82f, 0f);
            head.transform.localScale = Vector3.one * 0.32f;
            head.GetComponent<Renderer>().sharedMaterial = skinMat;
            UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
        }

        private static void CreateLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.76f, 0.78f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.74f, 0.82f, 0.86f, 1f);
            RenderSettings.fogDensity = 0.0045f;

            var lightGo = GameObject.Find("Directional Light") ?? new GameObject("Directional Light");
            var light = lightGo.GetComponent<Light>() ?? lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.84f, 1f);
            light.intensity = 1.45f;
            lightGo.transform.rotation = Quaternion.Euler(42f, -34f, 0f);
            RenderSettings.sun = light;
        }

        private static Material CreateRuntimeMat(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = name, color = color };
            return mat;
        }

        private static void DestroyRenderer(GameObject go)
        {
            var renderer = go.GetComponent<Renderer>();
            var mesh = go.GetComponent<MeshFilter>();
            if (renderer != null) UnityEngine.Object.DestroyImmediate(renderer);
            if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
        }

        private static void StripColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void CreateEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + folderName))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static void EnsureFolderExists(string folderPath)
        {
            folderPath = folderPath.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && parent != "Assets" && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }

            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folderPath));
        }

        private static void SetRef(SerializedObject obj, string propertyName, UnityEngine.Object value)
        {
            var prop = obj.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogError($"[SceneBuilder] Missing property '{propertyName}' on {obj.targetObject.name}");
                return;
            }
            prop.objectReferenceValue = value;
        }

        private static void SetString(SerializedObject obj, string propertyName, string value)
        {
            var prop = obj.FindProperty(propertyName);
            if (prop != null) prop.stringValue = value;
        }

        private static void SetFloat(SerializedObject obj, string propertyName, float value)
        {
            var prop = obj.FindProperty(propertyName);
            if (prop != null) prop.floatValue = value;
        }

        private static void SetInt(SerializedObject obj, string propertyName, int value)
        {
            var prop = obj.FindProperty(propertyName);
            if (prop != null) prop.intValue = value;
        }

        private static void SetBool(SerializedObject obj, string propertyName, bool value)
        {
            var prop = obj.FindProperty(propertyName);
            if (prop != null) prop.boolValue = value;
        }
    }
}
