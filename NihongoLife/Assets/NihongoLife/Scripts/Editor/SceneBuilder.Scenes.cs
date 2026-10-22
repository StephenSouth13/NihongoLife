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
    /// <summary>Partial of SceneBuilder: bootstrap, menu, sandbox, and control-room scene builders. The public entry point stays SceneBuilder.cs.</summary>
    public static partial class SceneBuilder
    {
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

            var title = CreateText(panel.transform, "TitleText", "", font, 86, new Vector2(0, 200), new Vector2(900, 110), TextAlignmentOptions.Center);
            title.color = new Color(1f, 0.98f, 0.9f);
            title.fontStyle = FontStyles.Bold;
            var subtitle = CreateText(panel.transform, "SubtitleText", "コンビニで買い物 / luyện hội thoại mua hàng", font, 28, new Vector2(0, 115), new Vector2(900, 44), TextAlignmentOptions.Center);
            subtitle.color = new Color(0.9f, 0.95f, 1f);

            var startBtn = CreateUIButton(panel.transform, "StartButton", "Bắt đầu", new Vector2(0, -10), new Vector2(340, 68), font, true).GetComponent<Button>();
            var quitBtn = CreateUIButton(panel.transform, "QuitButton", "Thoát", new Vector2(0, -95), new Vector2(340, 62), font, false).GetComponent<Button>();
            var profileText = CreateText(panel.transform, "ProfileText", "WASD di chuyển  |  E tương tác  |  B balo  |  Tab nhân vật", font, 20, new Vector2(0, -195), new Vector2(760, 48), TextAlignmentOptions.Center);
            profileText.color = new Color(0.95f, 0.95f, 0.95f);

            CreateText(panel.transform, "LanguageLabel", "Ngôn ngữ giao diện", font, 19, new Vector2(0f, -118f), new Vector2(560f, 32f), TextAlignmentOptions.Center).color = new Color(1f, 0.91f, 0.54f);
            /*
            CreateMenuButton(panel.transform, "LanguageVietnameseButton", "VI", new Vector2(-132f, -166f), new Vector2(92f, 44f), font);
            CreateMenuButton(panel.transform, "LanguageEnglishButton", "EN", new Vector2(0f, -166f), new Vector2(92f, 44f), font);
            CreateMenuButton(panel.transform, "LanguageJapaneseButton", "JP", new Vector2(132f, -166f), new Vector2(92f, 44f), font);

            CreateMenuButton(panel.transform, "LoginButton", "Đăng nhập", new Vector2(-420f, 220f), new Vector2(140f, 44f), font);
            CreateMenuButton(panel.transform, "SettingsButton", "Cài đặt", new Vector2(420f, 220f), new Vector2(140f, 44f), font);
            CreateMenuButton(panel.transform, "LeaderboardBtn", "Xếp hạng", new Vector2(-280f, -230f), new Vector2(130f, 46f), font);
            CreateMenuButton(panel.transform, "GuideButton", "Cách chơi", new Vector2(-112f, -230f), new Vector2(170f, 46f), font);
            CreateMenuButton(panel.transform, "AboutButton", "Về tôi", new Vector2(112f, -230f), new Vector2(170f, 46f), font);
            CreateMenuButton(panel.transform, "FriendsBtn", "Bạn bè", new Vector2(280f, -230f), new Vector2(130f, 46f), font);
            CreateMenuButton(panel.transform, "ProfileBtn", "Hồ sơ", new Vector2(0f, -290f), new Vector2(130f, 46f), font);
            CreateMenuButton(panel.transform, "OnlineBtn", "Online", new Vector2(0f, -350f), new Vector2(150f, 46f), font);
            */

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
            // The finished menu layout (what Play shows) is baked into the scene, not arranged at runtime.
            if (path == NihongoLife.EditorTools.MenuSyncCheck.ScenePath) NihongoLife.EditorTools.MainMenuBaker.BakeScene();
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

            database.activeScenarioId = "scenario.intro.arrival";
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
    }
}
