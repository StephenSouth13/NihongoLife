#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NihongoLife.EditorTools
{
    public static class ThirdPartyAssetIntegrator
    {
        private const string ScenePath = "Assets/NihongoLife/Scenes/90_TestSandbox.unity";
        private const string GeneratedRoot = "Assets/NihongoLife/Generated/ThirdPartyIntegration";
        private const string SushiFolder = "Assets/ThirdParty/Sushi Restaurant Kit - May 2023-20260920T035054Z-1-001";
        private const string FoodFolder = "Assets/ThirdParty/Ultimate Food Pack - Oct 2019-20260920T035121Z-1-001";
        private const string AnimalFolder = "Assets/ThirdParty/Ultimate Animated Animals - July 2021-20260920T035520Z-1-001";
        private static readonly Dictionary<Material, Material> ConvertedMaterials = new();

        public static void IntegrateAndSave()
        {
            EnsureFolders();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new InvalidOperationException($"Cannot open {ScenePath}");

            RemoveExistingRoot("ThirdParty_Integrated_World");
            var world = new GameObject("ThirdParty_Integrated_World");
            Undo.RegisterCreatedObjectUndo(world, "Integrate third-party assets");

            BuildStore(world.transform);
            BuildStoryProps(world.transform);
            ConfigureMixamoImports();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[ThirdPartyAssetIntegrator] Store and story visuals integrated and scene saved.");
        }

        public static void ValidateIntegratedScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = GameObject.Find("ThirdParty_Integrated_World");
            if (root == null) throw new InvalidOperationException("ThirdParty_Integrated_World is missing from gameplay scene.");

            int renderers = 0;
            int colliders = 0;
            int animators = 0;
            int missingMaterials = 0;
            int pinkMaterials = 0;
            Bounds bounds = new Bounds(root.transform.position, Vector3.zero);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderers++;
                bounds.Encapsulate(renderer.bounds);
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null) missingMaterials++;
                    else if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader") pinkMaterials++;
                }
            }
            colliders = root.GetComponentsInChildren<Collider>(true).Length;
            animators = root.GetComponentsInChildren<Animator>(true).Length;
            string animalPath = FindFbx(AnimalFolder, "ShibaInu");
            var animalClips = new List<string>();
            if (!string.IsNullOrEmpty(animalPath))
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(animalPath))
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")) animalClips.Add(clip.name);
            }

            var store = root.transform.Find("Store_ThirdParty_Visuals");
            var story = root.transform.Find("Story_ThirdParty_Visuals");
            if (store == null || story == null) throw new InvalidOperationException("Integrated store/story root is incomplete.");
            if (missingMaterials > 0 || pinkMaterials > 0)
                throw new InvalidOperationException($"Material validation failed: missing={missingMaterials}, errorShader={pinkMaterials}.");

            Debug.Log($"[ThirdPartyAssetIntegrator] VALID: scene={scene.name}, objects={root.GetComponentsInChildren<Transform>(true).Length}, " +
                      $"renderers={renderers}, colliders={colliders}, animators={animators}, bounds={bounds.size}, " +
                      $"ShibaClips=[{string.Join(", ", animalClips)}].");
        }

        private static void BuildStore(Transform world)
        {
            var store = new GameObject("Store_ThirdParty_Visuals").transform;
            store.SetParent(world, false);

            DisableLegacyVisual("StoreMerchandiseDecor");

            // Fixtures stay inside the established store footprint: x [-4.8, 4.8], z [1, 9].
            Place(SushiFolder, "Environment_Fridge", store, "DrinkFridge", new Vector3(3.55f, 0.05f, 6.65f), new Vector3(2.0f, 2.5f, 1.4f), Quaternion.Euler(0f, 180f, 0f), true);
            Place(SushiFolder, "Environment_CanFridge", store, "ColdDisplay", new Vector3(3.45f, 0.05f, 3.35f), new Vector3(2.2f, 1.55f, 1.25f), Quaternion.Euler(0f, 180f, 0f), true);
            Place(SushiFolder, "Environment_Cabinet_Shelves", store, "FoodShelf_A", new Vector3(-3.45f, 0.05f, 3.55f), new Vector3(2.1f, 2.15f, 0.85f), Quaternion.Euler(0f, 90f, 0f), true);
            Place(SushiFolder, "Environment_Cabinet_Shelves_2", store, "FoodShelf_B", new Vector3(-3.45f, 0.05f, 6.25f), new Vector3(2.1f, 2.15f, 0.85f), Quaternion.Euler(0f, 90f, 0f), true);
            Place(SushiFolder, "Environment_Counter_Straight", store, "CheckoutCounter_L", new Vector3(-1.25f, 0.05f, 8.15f), new Vector3(2.2f, 1.0f, 0.8f), Quaternion.identity, true);
            Place(SushiFolder, "Environment_Counter_Drawers", store, "CheckoutCounter_R", new Vector3(1.15f, 0.05f, 8.15f), new Vector3(2.2f, 1.0f, 0.8f), Quaternion.identity, true);
            Place(SushiFolder, "Environment_ChukamanSteamer", store, "HotFoodSteamer", new Vector3(-1.35f, 1.08f, 8.1f), new Vector3(0.62f, 0.62f, 0.62f), Quaternion.identity, false);
            Place(SushiFolder, "Decoration_Bell", store, "CounterBell", new Vector3(0.45f, 1.08f, 7.82f), new Vector3(0.18f, 0.18f, 0.18f), Quaternion.identity, false);
            Place(SushiFolder, "Decoration_Sign", store, "StoreSignVisual", new Vector3(0f, 2.8f, 0.82f), new Vector3(2.9f, 0.75f, 0.18f), Quaternion.Euler(0f, 180f, 0f), false);
            Place(SushiFolder, "Decoration_Plant1", store, "EntrancePlant_L", new Vector3(-4.15f, 0.05f, 1.25f), new Vector3(0.65f, 1.05f, 0.65f), Quaternion.identity, false);
            Place(SushiFolder, "Decoration_Plant2", store, "EntrancePlant_R", new Vector3(4.15f, 0.05f, 1.25f), new Vector3(0.65f, 1.05f, 0.65f), Quaternion.identity, false);

            CreateShelfProducts(store, -3.05f, 3.15f);
            CreateShelfProducts(store, -3.05f, 5.85f);
            CreateColdProducts(store);
            UpgradeInteractiveItem("Onigiri", SushiFolder, "Food_Onigiri", 0.34f);
            UpgradeInteractiveItem("Water", FoodFolder, "Bottle1", 0.38f);
            UpgradeInteractiveItem("Tea", FoodFolder, "Bottle2", 0.38f);
        }

        private static void CreateShelfProducts(Transform parent, float x, float z)
        {
            string[] products = { "Food_Onigiri", "Food_Dango", "Food_Gyoza", "Food_Roll", "Food_TamagoNigiri", "Food_Chukaman" };
            for (int row = 0; row < 2; row++)
            {
                for (int i = 0; i < products.Length; i++)
                {
                    var position = new Vector3(x + row * 0.22f, 0.68f + i % 3 * 0.46f, z + (i / 3) * 0.48f - 0.25f);
                    Place(SushiFolder, products[i], parent, $"Stock_{products[i]}_{row}_{i}", position,
                        new Vector3(0.24f, 0.24f, 0.24f), Quaternion.Euler(0f, 90f, 0f), false, true);
                }
            }
        }

        private static void CreateColdProducts(Transform parent)
        {
            string[] products = { "Bottle1", "Bottle2", "Soda", "SoySauce" };
            for (int row = 0; row < 3; row++)
            {
                for (int i = 0; i < products.Length; i++)
                {
                    Place(FoodFolder, products[i], parent, $"ColdStock_{row}_{i}",
                        new Vector3(3.15f + i * 0.24f, 0.45f + row * 0.34f, 3.0f),
                        new Vector3(0.16f, 0.32f, 0.16f), Quaternion.identity, false, true);
                }
            }
        }

        private static void BuildStoryProps(Transform world)
        {
            var story = new GameObject("Story_ThirdParty_Visuals").transform;
            story.SetParent(world, false);

            var dog = Place(AnimalFolder, "ShibaInu", story, "LostPet_Shiba", new Vector3(-8.6f, 0.05f, 1.8f),
                new Vector3(0.75f, 0.8f, 1.1f), Quaternion.Euler(0f, 215f, 0f), false);
            if (dog != null)
            {
                ConfigureAnimalAnimator(dog, FindFbx(AnimalFolder, "ShibaInu"), "ShibaInu");
            }

            // Visual-only station landmark for the ticket chapter. It is kept far from the store
            // and uses two repeated wagons instead of a long train to control draw calls.
            var station = new GameObject("StationLandmark").transform;
            station.SetParent(story, false);
            PlaceByFolderName("Assets/ThirdParty/Train Pack - April 2019-20260920T035456Z-1-001", "RailwayTrack_Straight", station,
                "Track_A", new Vector3(30f, 0.02f, 21f), new Vector3(10f, 0.25f, 2.2f), Quaternion.identity, true);
            PlaceByFolderName("Assets/ThirdParty/Train Pack - April 2019-20260920T035456Z-1-001", "HighSpeed_Front", station,
                "TrainFront", new Vector3(27f, 0.18f, 21f), new Vector3(6f, 2.1f, 2.15f), Quaternion.Euler(0f, 90f, 0f), true);
            PlaceByFolderName("Assets/ThirdParty/Train Pack - April 2019-20260920T035456Z-1-001", "HighSpeed_Wagon", station,
                "TrainWagon", new Vector3(34f, 0.18f, 21f), new Vector3(6f, 2.1f, 2.15f), Quaternion.Euler(0f, 90f, 0f), true);
        }

        private static GameObject Place(string folder, string modelName, Transform parent, string instanceName, Vector3 position,
            Vector3 targetSize, Quaternion rotation, bool addCollider, bool smallProp = false) =>
            PlaceByFolderName(folder, modelName, parent, instanceName, position, targetSize, rotation, addCollider, smallProp);

        private static GameObject PlaceByFolderName(string folder, string modelName, Transform parent, string instanceName,
            Vector3 position, Vector3 targetSize, Quaternion rotation, bool addCollider, bool smallProp = false)
        {
            string path = FindFbx(folder, modelName);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"[ThirdPartyAssetIntegrator] Missing FBX: {modelName} under {folder}");
                return null;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return null;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = instanceName;
            instance.transform.SetParent(parent, true);
            instance.transform.SetPositionAndRotation(position, rotation);
            FitAndGround(instance, position.y, targetSize);
            ConvertMaterials(instance);
            ConfigureRenderers(instance, smallProp);
            if (addCollider) AddBoundsCollider(instance);
            SetStatic(instance, !smallProp);
            return instance;
        }

        private static void UpgradeInteractiveItem(string objectName, string folder, string modelName, float size)
        {
            GameObject target = GameObject.Find(objectName);
            if (target == null) return;
            var previous = target.transform.Find("ThirdPartyVisual");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var visual = Place(folder, modelName, target.transform, "ThirdPartyVisual", target.transform.position,
                Vector3.one * size, target.transform.rotation, false, true);
            if (visual == null) return;
            visual.transform.SetParent(target.transform, true);
            foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.transform.IsChildOf(visual.transform)) renderer.enabled = false;
            }
        }

        private static string FindFbx(string folder, string modelName)
        {
            string[] guids = AssetDatabase.FindAssets($"{modelName} t:Model", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(Path.GetFileNameWithoutExtension(path), modelName, StringComparison.OrdinalIgnoreCase))
                    return path;
            }
            return null;
        }

        private static void FitAndGround(GameObject instance, float groundY, Vector3 targetSize)
        {
            Bounds bounds = GetBounds(instance);
            if (bounds.size.sqrMagnitude < 0.0001f) return;
            float scale = Mathf.Min(targetSize.x / Mathf.Max(0.001f, bounds.size.x),
                targetSize.y / Mathf.Max(0.001f, bounds.size.y),
                targetSize.z / Mathf.Max(0.001f, bounds.size.z));
            instance.transform.localScale *= scale;
            bounds = GetBounds(instance);
            instance.transform.position += Vector3.up * (groundY - bounds.min.y);
        }

        private static Bounds GetBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void AddBoundsCollider(GameObject root)
        {
            Bounds bounds = GetBounds(root);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = root.transform.InverseTransformPoint(bounds.center);
            Vector3 scale = root.transform.lossyScale;
            collider.size = new Vector3(bounds.size.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                bounds.size.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)), bounds.size.z / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
        }

        private static void ConfigureRenderers(GameObject root, bool smallProp)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = smallProp ? ShadowCastingMode.Off : ShadowCastingMode.On;
                renderer.receiveShadows = !smallProp;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            }
        }

        private static void ConvertMaterials(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = GetUrpMaterial(materials[i]);
                renderer.sharedMaterials = materials;
            }
        }

        private static Material GetUrpMaterial(Material source)
        {
            if (source == null) return null;
            if (source.shader != null && source.shader.name.StartsWith("Universal Render Pipeline", StringComparison.Ordinal)) return source;
            if (ConvertedMaterials.TryGetValue(source, out Material cached)) return cached;

            string safeName = Sanitize(source.name) + "_" + Mathf.Abs(source.GetInstanceID());
            string path = $"{GeneratedRoot}/Materials/{safeName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader) { name = safeName };
                Color color = source.HasProperty("_Color") ? source.color : Color.white;
                Texture texture = source.HasProperty("_MainTex") ? source.mainTexture : null;
                material.SetColor("_BaseColor", color);
                if (texture != null) material.SetTexture("_BaseMap", texture);
                AssetDatabase.CreateAsset(material, path);
            }
            ConvertedMaterials[source] = material;
            return material;
        }

        private static void ConfigureMixamoImports()
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/ThirdParty/Mixamo/Animations" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not ModelImporter importer) continue;
                bool dirty = importer.animationType != ModelImporterAnimationType.Human || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel;
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                var clips = importer.defaultClipAnimations;
                string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                bool loop = name.Contains("idle") || name.Contains("walking") || name.Contains("talking");
                foreach (var clip in clips) clip.loopTime = loop;
                importer.clipAnimations = clips;
                if (dirty) importer.SaveAndReimport();
            }
        }

        private static void ConfigureAnimalAnimator(GameObject instance, string modelPath, string controllerName)
        {
            if (instance == null || string.IsNullOrEmpty(modelPath)) return;
            var clips = new List<AnimationClip>();
            Avatar avatar = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")) clips.Add(clip);
                else if (asset is Avatar foundAvatar) avatar = foundAvatar;
            }
            if (clips.Count == 0) return;

            string controllerPath = $"{GeneratedRoot}/{controllerName}.controller";
            AssetDatabase.DeleteAsset(controllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Eat", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

            var machine = controller.layers[0].stateMachine;
            AnimationClip idle = FindClip(clips, "|Idle") ?? clips[0];
            AnimationClip walk = FindClip(clips, "|Walk");
            var idleState = machine.AddState("Idle");
            idleState.motion = idle;
            machine.defaultState = idleState;
            if (walk != null)
            {
                var walkState = machine.AddState("Walk");
                walkState.motion = walk;
                var toWalk = idleState.AddTransition(walkState);
                toWalk.hasExitTime = false;
                toWalk.duration = 0.18f;
                toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
                var toIdle = walkState.AddTransition(idleState);
                toIdle.hasExitTime = false;
                toIdle.duration = 0.18f;
                toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            }
            AddTriggerState(machine, idleState, clips, "Eating", "Eat");
            AddTriggerState(machine, idleState, clips, "HitReact", "Hit");
            AddTriggerState(machine, idleState, clips, "|Attack", "Attack");
            AddTriggerState(machine, idleState, clips, "Jump", "Jump");

            var animator = instance.GetComponent<Animator>();
            if (!animator) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        private static void AddTriggerState(AnimatorStateMachine machine, AnimatorState idle, List<AnimationClip> clips, string clipToken, string trigger)
        {
            AnimationClip clip = FindClip(clips, clipToken);
            if (clip == null) return;
            var state = machine.AddState(trigger);
            state.motion = clip;
            var enter = machine.AddAnyStateTransition(state);
            enter.hasExitTime = false;
            enter.duration = 0.1f;
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            var exit = state.AddTransition(idle);
            exit.hasExitTime = true;
            exit.exitTime = 0.92f;
            exit.duration = 0.15f;
        }

        private static AnimationClip FindClip(List<AnimationClip> clips, string token) =>
            clips.Find(clip => clip.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);

        private static void DisableLegacyVisual(string name)
        {
            var target = GameObject.Find(name);
            if (target != null) target.SetActive(false);
        }

        private static void RemoveExistingRoot(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
        }

        private static void SetStatic(GameObject root, bool isStatic)
        {
            if (!isStatic) return;
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(transform.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
        }

        private static string Sanitize(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value.Replace('/', '_').Replace(' ', '_');
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/NihongoLife", "Generated");
            EnsureFolder("Assets/NihongoLife/Generated", "ThirdPartyIntegration");
            EnsureFolder(GeneratedRoot, "Materials");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
