using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using NihongoLife.Core;
using NihongoLife.Trailer;

namespace NihongoLife.Editor
{
    public static class TrailerSceneBuilder
    {
        private const string ScenesDir = "Assets/NihongoLife/Scenes";
        private const string TrailerScenePath = ScenesDir + "/99_Trailer.unity";
        private const string TrailerAssetsDir = "Assets/NihongoLife/Resources/Trailer";
        private const string CharacterPrefabDir = "Assets/NihongoLife/Prefabs/Characters";

        // Until dedicated Yamada/Kimura/etc. models exist (via NihongoLife/Characters/Scan New
        // Characters), reuse the closest already-built legacy character prefab instead of an
        // ugly primitive capsule. Update this table once real per-NPC prefabs exist.
        private static readonly Dictionary<string, string> CastPrefabOverrides = new Dictionary<string, string>
        {
            { "Tanaka", "NL_Neighbor" },
            { "FestivalTanaka", "NL_Neighbor" },
            { "Suzuki", "NL_Guide" },
            { "FestivalSuzuki", "NL_Guide" },
        };

        /// <summary>
        /// Safe default: only scaffolds the trailer scene/shot assets the first time.
        /// Re-running this after you've hand-tuned camera anchors, shot values or swapped in
        /// real character prefabs inside 99_Trailer.unity must NOT wipe that work.
        /// </summary>
        // No MenuItem and no editor auto-run. Call BuildTrailerSceneSafe() explicitly
        // from a temporary script, test, or batch step when the trailer build step is needed.
        public static void BuildTrailerSceneSafe()
        {
            EnsureFolderExists(ScenesDir);
            EnsureFolderExists(TrailerAssetsDir);

            var shots = EnsureSampleShots(overwriteExisting: false);

            if (System.IO.File.Exists(TrailerScenePath))
            {
                Debug.Log($"[TrailerSceneBuilder] {TrailerScenePath} already exists — scene left untouched. " +
                          "Use 'Force Rebuild Trailer Scene (Destructive)' if you really want to regenerate it from scratch.");
                EnsureBuildSettingsEntry();
                AssetDatabase.SaveAssets();
                return;
            }

            BuildSceneFromScratch(shots);
        }

        public static void ForceRebuildTrailerScene()
        {
            if (!EditorUtility.DisplayDialog(
                    "Force Rebuild Trailer Scene",
                    $"Việc này sẽ xoá và dựng lại toàn bộ {TrailerScenePath} cùng các shot asset mẫu từ đầu, " +
                    "mất mọi chỉnh sửa tay (vị trí camera, nhân vật thật đã thay, giá trị shot đã tinh chỉnh). Tiếp tục?",
                    "Rebuild (mất chỉnh sửa tay)",
                    "Huỷ"))
            {
                return;
            }

            EnsureFolderExists(ScenesDir);
            EnsureFolderExists(TrailerAssetsDir);
            var shots = EnsureSampleShots(overwriteExisting: true);
            BuildSceneFromScratch(shots);
        }

        private static void BuildSceneFromScratch(List<TrailerShotDefinition> shots)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            CreateLighting();
            CreateEnvironment();

            var camera = CreateTrailerCamera();
            CreateFocusCast();
            CreateDirector(camera, shots);

            EditorSceneManager.SaveScene(scene, TrailerScenePath);
            EnsureBuildSettingsEntry();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TrailerSceneBuilder] Built trailer scene at {TrailerScenePath} with {shots.Count} shot assets.");
        }

        private static List<TrailerShotDefinition> EnsureSampleShots(bool overwriteExisting)
        {
            var shots = new List<TrailerShotDefinition>
            {
                EnsureShot(overwriteExisting, "shot_01_establishing", 10,
                    new Vector3(-18f, 9f, -34f), new Vector3(24f, 34f, 0f),
                    new Vector3(8f, 8f, -22f), new Vector3(22f, -22f, 0f),
                    4.2f, "Hibari-cho e yokoso", "Welcome to Hibari-cho", "TownFocus", TrailerAnimationCue.None, true),
                EnsureShot(overwriteExisting, "shot_02_tanaka_greeting", 20,
                    new Vector3(-3.2f, 2.4f, -10.5f), new Vector3(10f, 28f, 0f),
                    new Vector3(-1.3f, 2.1f, -7.2f), new Vector3(9f, 18f, 0f),
                    3.1f, "Tanaka-san ga aisatsu shimasu", "Tanaka welcomes the player", "Tanaka", TrailerAnimationCue.Point, true),
                EnsureShot(overwriteExisting, "shot_03_konbini", 30,
                    new Vector3(6f, 3.1f, -13f), new Vector3(13f, -31f, 0f),
                    new Vector3(2.4f, 2.5f, -6.7f), new Vector3(9f, -8f, 0f),
                    3.3f, "Konbini de hajimete no kaiwa", "A first conversation at the convenience store", "KonbiniDoor", TrailerAnimationCue.None, true),
                EnsureShot(overwriteExisting, "shot_04_ramen", 40,
                    new Vector3(-8.2f, 2.6f, -1.8f), new Vector3(18f, 58f, 0f),
                    new Vector3(-5.8f, 2.1f, 0.1f), new Vector3(14f, 42f, 0f),
                    3.2f, "Yamada no ramen", "Warm ramen at Yamada's shop", "RamenBowl", TrailerAnimationCue.None, true),
                EnsureShot(overwriteExisting, "shot_05_suzuki_cat", 50,
                    new Vector3(9f, 2.4f, -0.5f), new Vector3(12f, -48f, 0f),
                    new Vector3(6.4f, 2.1f, 1.5f), new Vector3(10f, -30f, 0f),
                    3.1f, "Suzuki-san to neko", "Suzuki thanks you for helping", "Suzuki", TrailerAnimationCue.Bow, true),
                EnsureShot(overwriteExisting, "shot_06_station", 60,
                    new Vector3(-17f, 4.5f, 7f), new Vector3(18f, 72f, 0f),
                    new Vector3(-11f, 3.8f, 10f), new Vector3(14f, 52f, 0f),
                    3.5f, "Eki de kippu o kaimasu", "Buy a ticket and find the right train", "StationGate", TrailerAnimationCue.None, true),
                EnsureShot(overwriteExisting, "shot_07_festival", 70,
                    new Vector3(15f, 6f, 12f), new Vector3(22f, -138f, 0f),
                    new Vector3(4f, 4.8f, 7f), new Vector3(18f, -160f, 0f),
                    4.0f, "Natsu matsuri de mata aimashou", "Everyone meets again at the summer festival", "FestivalGroup", TrailerAnimationCue.Talk, true),
                EnsureShot(overwriteExisting, "shot_08_logo", 80,
                    new Vector3(0f, 3.2f, -18f), new Vector3(8f, 0f, 0f),
                    new Vector3(0f, 3.2f, -16f), new Vector3(8f, 0f, 0f),
                    3.4f, "NIHONGO LIFE", "Learn Japanese through a small town story", "LogoFocus", TrailerAnimationCue.None, false)
            };

            return shots;
        }

        private static TrailerShotDefinition EnsureShot(
            bool overwriteExisting,
            string assetName,
            int order,
            Vector3 startPosition,
            Vector3 startEulerAngles,
            Vector3 endPosition,
            Vector3 endEulerAngles,
            float duration,
            string textJa,
            string textEn,
            string focusTargetName,
            TrailerAnimationCue cue,
            bool fadeAfter)
        {
            string path = $"{TrailerAssetsDir}/{assetName}.asset";
            var shot = AssetDatabase.LoadAssetAtPath<TrailerShotDefinition>(path);
            if (shot != null && !overwriteExisting)
            {
                // Asset already exists and caller didn't ask for a reset — keep whatever the
                // user has hand-tuned in the Inspector since instead of stomping it every run.
                return shot;
            }

            if (shot == null)
            {
                shot = ScriptableObject.CreateInstance<TrailerShotDefinition>();
                AssetDatabase.CreateAsset(shot, path);
            }

            shot.order = order;
            shot.startPosition = startPosition;
            shot.startEulerAngles = startEulerAngles;
            shot.endPosition = endPosition;
            shot.endEulerAngles = endEulerAngles;
            shot.durationSeconds = duration;
            shot.easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            shot.lookAtFocusTarget = !string.IsNullOrEmpty(focusTargetName);
            shot.fadeToBlackAfterShot = fadeAfter;
            shot.focusTargetName = focusTargetName;
            shot.animationCueToTrigger = cue;
            shot.textJa = textJa;
            shot.textEn = textEn;

            EditorUtility.SetDirty(shot);
            return shot;
        }

        private static Camera CreateTrailerCamera()
        {
            var cameraGo = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(-18f, 9f, -34f);
            cameraGo.transform.rotation = Quaternion.Euler(24f, 34f, 0f);

            var camera = cameraGo.GetComponent<Camera>() ?? cameraGo.AddComponent<Camera>();
            camera.fieldOfView = 38f;
            camera.nearClipPlane = 0.2f;
            camera.farClipPlane = 240f;
            camera.clearFlags = CameraClearFlags.Skybox;

            if (cameraGo.GetComponent<AudioListener>() == null)
            {
                cameraGo.AddComponent<AudioListener>();
            }

            return camera;
        }

        private static void CreateDirector(Camera camera, List<TrailerShotDefinition> shots)
        {
            var directorGo = new GameObject("TrailerDirector");
            var director = directorGo.AddComponent<TrailerDirector>();
            var serialized = new SerializedObject(director);
            SetRef(serialized, "targetCamera", camera);
            SetBool(serialized, "playOnStart", true);
            SetBool(serialized, "loop", false);

            var shotsProperty = serialized.FindProperty("shots");
            shotsProperty.ClearArray();
            for (int i = 0; i < shots.Count; i++)
            {
                shotsProperty.InsertArrayElementAtIndex(i);
                shotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = shots[i];
            }

            serialized.ApplyModifiedProperties();
        }

        private static void CreateEnvironment()
        {
            var environment = new GameObject("TrailerEnvironment");
            VisualEnvironmentBuilder.GenerateVisualEnvironment(environment);
            CreateGround(environment.transform);
            CreateSimpleRamenShop(environment.transform);
            CreateSimpleStation(environment.transform);
            CreateFestivalBlockout(environment.transform);
        }

        private static void CreateFocusCast()
        {
            CreateFocus("TownFocus", new Vector3(0f, 1.2f, -10f));
            CreateTrailerPerson("Tanaka", new Vector3(-0.6f, 0f, -4.6f), new Color(0.18f, 0.38f, 0.82f));
            CreateFocus("KonbiniDoor", new Vector3(0f, 1.35f, -0.2f));
            CreateRamenBowl("RamenBowl", new Vector3(-4.2f, 0.92f, 2.9f));
            CreateTrailerPerson("Suzuki", new Vector3(5.2f, 0f, 3.2f), new Color(0.74f, 0.32f, 0.56f));
            CreateFocus("StationGate", new Vector3(-10.5f, 1.6f, 11.2f));
            CreateFocus("FestivalGroup", new Vector3(0.5f, 1.45f, 8.6f));
            CreateTrailerPerson("FestivalTanaka", new Vector3(-0.5f, 0f, 8.4f), new Color(0.18f, 0.38f, 0.82f));
            CreateTrailerPerson("FestivalSuzuki", new Vector3(1.2f, 0f, 8.9f), new Color(0.74f, 0.32f, 0.56f));
            CreateLogoFocus();
        }

        private static Transform CreateFocus(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            return go.transform;
        }

        private static void CreateTrailerPerson(string name, Vector3 position, Color bodyColor)
        {
            GameObject root = TryInstantiateRealCharacter(name);
            if (root != null)
            {
                root.name = name;
                root.transform.position = position;
                root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                if (root.GetComponent<CharacterAnimationController>() == null)
                {
                    root.AddComponent<CharacterAnimationController>();
                }

                return;
            }

            Debug.LogWarning($"[TrailerSceneBuilder] No real character prefab mapped/found for '{name}' yet — " +
                              "using a placeholder capsule. Run 'NihongoLife/Characters/Build Character System' " +
                              "first, then update CastPrefabOverrides once a dedicated model exists.");

            root = new GameObject(name);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var animation = root.AddComponent<CharacterAnimationController>();
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            CreatePersonPrimitive(visual.transform, bodyColor);

            var serialized = new SerializedObject(animation);
            SetRef(serialized, "_animator", null);
            serialized.ApplyModifiedProperties();
        }

        private static GameObject TryInstantiateRealCharacter(string castName)
        {
            if (!CastPrefabOverrides.TryGetValue(castName, out string prefabName))
            {
                return null;
            }

            string prefabPath = $"{CharacterPrefabDir}/{prefabName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return null;
            }

            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        private static void CreatePersonPrimitive(Transform parent, Color bodyColor)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(parent, false);
            body.transform.localPosition = new Vector3(0f, 0.92f, 0f);
            body.transform.localScale = new Vector3(0.52f, 0.82f, 0.52f);
            body.GetComponent<Renderer>().sharedMaterial = CreateMaterial("TrailerPersonBody", bodyColor);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(parent, false);
            head.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            head.transform.localScale = Vector3.one * 0.32f;
            head.GetComponent<Renderer>().sharedMaterial = CreateMaterial("TrailerPersonHead", new Color(0.9f, 0.72f, 0.58f));
            Object.DestroyImmediate(head.GetComponent<Collider>());
        }

        private static void CreateRamenBowl(string name, Vector3 position)
        {
            var root = new GameObject(name);
            root.transform.position = position;

            var bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowl.name = "Bowl";
            bowl.transform.SetParent(root.transform, false);
            bowl.transform.localScale = new Vector3(0.8f, 0.18f, 0.8f);
            bowl.GetComponent<Renderer>().sharedMaterial = CreateMaterial("TrailerRamenBowl", new Color(0.95f, 0.92f, 0.84f));
            Object.DestroyImmediate(bowl.GetComponent<Collider>());

            var soup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            soup.name = "Soup";
            soup.transform.SetParent(root.transform, false);
            soup.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            soup.transform.localScale = new Vector3(0.68f, 0.04f, 0.68f);
            soup.GetComponent<Renderer>().sharedMaterial = CreateMaterial("TrailerRamenSoup", new Color(0.82f, 0.46f, 0.18f));
            Object.DestroyImmediate(soup.GetComponent<Collider>());
        }

        private static void CreateLogoFocus()
        {
            var focus = CreateFocus("LogoFocus", new Vector3(0f, 2f, -8f));
            var logo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            logo.name = "NihongoLifeLogoBlock";
            logo.transform.SetParent(focus, false);
            logo.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            logo.transform.localScale = new Vector3(5.8f, 1.8f, 0.15f);
            logo.GetComponent<Renderer>().sharedMaterial = CreateMaterial("TrailerLogoBlock", new Color(0.08f, 0.12f, 0.16f));
            Object.DestroyImmediate(logo.GetComponent<Collider>());
        }

        private static void CreateGround(Transform parent)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "TrailerGround";
            ground.transform.SetParent(parent);
            ground.transform.position = new Vector3(0f, -0.55f, 0f);
            ground.transform.localScale = new Vector3(120f, 0.22f, 90f);
            ground.GetComponent<Renderer>().sharedMaterial = CreateMaterial("TrailerGroundMat", new Color(0.15f, 0.17f, 0.16f));
            Object.DestroyImmediate(ground.GetComponent<Collider>());
        }

        private static void CreateSimpleRamenShop(Transform parent)
        {
            CreateBlock(parent, "RamenShopBlockout", new Vector3(-5f, 1.4f, 4.2f), new Vector3(4.5f, 2.8f, 3.2f), new Color(0.55f, 0.24f, 0.16f));
            CreateBlock(parent, "RamenCounter", new Vector3(-4.2f, 0.55f, 2.9f), new Vector3(2.4f, 0.32f, 0.85f), new Color(0.22f, 0.16f, 0.11f));
        }

        private static void CreateSimpleStation(Transform parent)
        {
            CreateBlock(parent, "StationGateBlockout", new Vector3(-11f, 1.6f, 11.5f), new Vector3(5.5f, 3.2f, 0.45f), new Color(0.2f, 0.35f, 0.48f));
            CreateBlock(parent, "TicketMachineBlockout", new Vector3(-8.3f, 1f, 10.8f), new Vector3(0.8f, 2f, 0.45f), new Color(0.86f, 0.76f, 0.32f));
        }

        private static void CreateFestivalBlockout(Transform parent)
        {
            CreateBlock(parent, "FestivalLanternLine", new Vector3(0f, 3.2f, 8.8f), new Vector3(7f, 0.12f, 0.12f), new Color(0.95f, 0.64f, 0.24f));
            for (int i = 0; i < 6; i++)
            {
                CreateBlock(parent, $"FestivalLantern_{i}", new Vector3(-3f + i * 1.2f, 2.7f, 8.8f), new Vector3(0.32f, 0.42f, 0.32f), new Color(0.95f, 0.26f, 0.18f));
            }
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = CreateMaterial(name + "Mat", color);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void CreateLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.7f, 0.75f, 0.8f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.68f, 0.78f, 0.85f, 1f);
            RenderSettings.fogDensity = 0.005f;

            var lightGo = GameObject.Find("Directional Light") ?? new GameObject("Directional Light");
            var light = lightGo.GetComponent<Light>() ?? lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.94f, 0.82f);
            light.intensity = 1.35f;
            lightGo.transform.rotation = Quaternion.Euler(42f, -34f, 0f);
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard");
            return new Material(shader) { name = name, color = color };
        }

        private static void EnsureBuildSettingsEntry()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == TrailerScenePath))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(TrailerScenePath, false));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolderExists(string folderPath)
        {
            folderPath = folderPath.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && parent != "Assets" && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }

            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folderPath));
        }

        private static void SetRef(SerializedObject obj, string propertyName, Object value)
        {
            var prop = obj.FindProperty(propertyName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static void SetBool(SerializedObject obj, string propertyName, bool value)
        {
            var prop = obj.FindProperty(propertyName);
            if (prop != null)
            {
                prop.boolValue = value;
            }
        }
    }
}
