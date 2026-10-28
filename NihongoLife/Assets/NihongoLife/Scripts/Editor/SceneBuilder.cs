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
    public static partial class SceneBuilder
    {
        private const int InteractableLayer = 6;
        private const string ScenesDir = "Assets/NihongoLife/Scenes";
        private const string SandboxScenePath = ScenesDir + "/90_TestSandbox.unity";
        private const string ControlScenePath = ScenesDir + "/99_ControlRoom.unity";
        private const string ControlDatabasePath = "Assets/NihongoLife/Resources/Control/NihongoLifeControlDatabase.asset";

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
                // BuildMainMenuScene(ScenesDir + "/01_MainMenu.unity");
                BuildSandboxScene(SandboxScenePath);
                BuildControlScene(ControlScenePath);

                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(ScenesDir + "/00_Bootstrap.unity", true),
                    // new EditorBuildSettingsScene(ScenesDir + "/01_MainMenu.unity", true),
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

        // Intentionally not exposed as a menu item: this replaces the entire scene.
        // Keep it callable from BuildAllScenes for initial project generation only.
        public static void RebuildMainMenu()
        {
            EnsureFolder("Assets/NihongoLife", "Scenes");
            FontSetup.EnsureJapaneseFontAsset(forceRecreate: true);
            BuildMainMenuScene(ScenesDir + "/01_MainMenu.unity");
            EditorSceneManager.OpenScene(ScenesDir + "/01_MainMenu.unity");
            Debug.Log("[SceneBuilder] Rebuilt 01_MainMenu with 90_TestSandbox town preview.");
        }

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

        public static void RebuildControlRoom()
        {
            EnsureFolder("Assets/NihongoLife", "Scenes");
            BuildControlScene(ControlScenePath);
            EditorSceneManager.OpenScene(ControlScenePath);
            Debug.Log("[SceneBuilder] Rebuilt 99_ControlRoom. Edit NihongoLifeControlDatabase in the Inspector.");
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
            var light = lightGo.GetComponent<Light>().OrNull() ?? lightGo.AddComponent<Light>();
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
