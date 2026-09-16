using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;
using NihongoLife.NPC;

namespace NihongoLife.Editor
{
    public static class CharacterBuilder
    {
        private const string REMY_PATH = "Assets/ThirdParty/Mixamo/Characters/remy/source/Remy.fbx";
        private const string ELIZABETH_PATH = "Assets/ThirdParty/Mixamo/Characters/elizabeth-female-3-d-character/source/Elizabeth_Female_3_d_Character/Elizabeth_Female_3_d_Character.fbx";
        private const string LILLY_PATH = "Assets/ThirdParty/Mixamo/Characters/lilly/source/Lilly.fbx";
        private const string EMINEM_PATH = "Assets/ThirdParty/Mixamo/Characters/eminem/source/Eminem FBX.Fbx";
        
        private const string ANIM_IDLE = "Assets/ThirdParty/Mixamo/Animations/Remy@Idle.fbx";
        private const string ANIM_WALK = "Assets/ThirdParty/Mixamo/Animations/Remy@Walking.fbx";
        private const string ANIM_TALK = "Assets/ThirdParty/Mixamo/Animations/Remy@Talking.fbx";
        private const string ANIM_BOW = "Assets/ThirdParty/Mixamo/Animations/Remy@Quick Informal Bow.fbx";
        private const string ANIM_POINT = "Assets/ThirdParty/Mixamo/Animations/Remy@Pointing.fbx";

        private const string ANIMATOR_PATH = "Assets/NihongoLife/Animations/NL_Humanoid.controller";
        private const string PREFAB_DIR = "Assets/NihongoLife/Prefabs/Characters";
        private const string CHARACTER_MATERIAL_DIR = "Assets/NihongoLife/Materials/Characters";
        private const string MIXAMO_CHARACTER_ROOT = "Assets/ThirdParty/Mixamo/Characters";
        private const float DEFAULT_CHARACTER_HEIGHT = 1.72f;

        private static readonly (string speakerId, string legacyPrefabName)[] LegacyCharacterPrefabs =
        {
            ("remy", "NL_Player"),
            ("elizabeth-female-3-d-character", "NL_Cashier"),
            ("lilly", "NL_Guide"),
            ("eminem", "NL_Neighbor")
        };

        [MenuItem("NihongoLife/Characters/Build Character System")]
        public static void BuildCharacterSystem()
        {
            BuildCharacterSystem(forceRebuildLegacyPrefabs: false);
        }

        [MenuItem("NihongoLife/Characters/Force Rebuild Legacy Characters (Destructive)")]
        public static void ForceRebuildLegacyCharacters()
        {
            if (!EditorUtility.DisplayDialog(
                    "Force Rebuild Legacy Characters",
                    "Việc này sẽ ghi đè lại 4 prefab NL_Player/NL_Cashier/NL_Guide/NL_Neighbor từ FBX gốc, mất mọi chỉnh sửa tay đã làm trên các prefab đó (material, component thêm, v.v.). Tiếp tục?",
                    "Rebuild (mất chỉnh sửa tay)",
                    "Huỷ"))
            {
                return;
            }

            BuildCharacterSystem(forceRebuildLegacyPrefabs: true);
        }

        private static void BuildCharacterSystem(bool forceRebuildLegacyPrefabs)
        {
            EnsureFolderExists("Assets/NihongoLife/Animations");
            EnsureFolderExists(PREFAB_DIR);

            string playerPath = ResolveCharacterPath(REMY_PATH);
            string cashierPath = ResolveCharacterPath(ELIZABETH_PATH, REMY_PATH);
            string guidePath = ResolveCharacterPath(LILLY_PATH, playerPath);
            string neighborPath = ResolveCharacterPath(EMINEM_PATH, REMY_PATH);

            ConfigureModel(playerPath);
            ConfigureModel(cashierPath);
            ConfigureModel(guidePath);
            ConfigureModel(neighborPath);

            ConfigureAnimation(ANIM_IDLE, true);
            ConfigureAnimation(ANIM_WALK, true, true);
            ConfigureAnimation(ANIM_TALK, true);
            ConfigureAnimation(ANIM_BOW, false);
            ConfigureAnimation(ANIM_POINT, false);

            GenerateAnimatorController();

            // skipIfExists=true by default: re-running "Build Character System" must never wipe manual
            // tweaks already made on these prefabs. Use "Force Rebuild Legacy Characters" to intentionally reset.
            bool skipIfExists = !forceRebuildLegacyPrefabs;
            CreateVisualPrefab(playerPath, "NL_Player", 1.72f, skipIfExists);
            CreateVisualPrefab(cashierPath, "NL_Cashier", 1.68f, skipIfExists);
            CreateVisualPrefab(guidePath, "NL_Guide", 1.70f, skipIfExists);
            CreateVisualPrefab(neighborPath, "NL_Neighbor", 1.76f, skipIfExists);
            ScanNewCharacters();

            AssetDatabase.SaveAssets();
            Debug.Log("[CharacterBuilder] Character system built successfully.");
        }

        [MenuItem("NihongoLife/Characters/Scan New Characters")]
        public static void ScanNewCharacters()
        {
            EnsureFolderExists(PREFAB_DIR);
            EnsureFolderExists(CHARACTER_MATERIAL_DIR);

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ANIMATOR_PATH);
            if (controller == null)
            {
                Debug.LogWarning("[CharacterBuilder] Animator controller missing. Generating NL_Humanoid.controller before scanning characters.");
                ConfigureAnimation(ANIM_IDLE, true);
                ConfigureAnimation(ANIM_WALK, true, true);
                ConfigureAnimation(ANIM_TALK, true);
                ConfigureAnimation(ANIM_BOW, false);
                ConfigureAnimation(ANIM_POINT, false);
                GenerateAnimatorController();
            }

            int created = 0;
            foreach (var character in FindMixamoCharacters())
            {
                string prefabName = "NL_" + character.speakerId;
                string prefabPath = $"{PREFAB_DIR}/{prefabName}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                {
                    Debug.Log($"[CharacterBuilder] Skipping '{character.speakerId}' because {prefabPath} already exists.");
                    continue;
                }

                if (HasLegacyPrefab(character.speakerId))
                {
                    Debug.Log($"[CharacterBuilder] Skipping legacy character '{character.speakerId}' because its existing prefab is managed by the original build pipeline.");
                    continue;
                }

                ConfigureModel(character.modelPath);
                CreateVisualPrefab(character.modelPath, prefabName, DEFAULT_CHARACTER_HEIGHT, skipIfExists: true);
                created++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[CharacterBuilder] Character scan complete. Created {created} new prefab(s).");
        }

        private static void ConfigureModel(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return;

            bool changed = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                Debug.Log($"[CharacterBuilder] Configured {path} as Humanoid.");
            }
        }

        private static void ConfigureAnimation(string path, bool loop, bool bakeRoot = false)
        {
            if (string.IsNullOrEmpty(path)) return;
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return;

            bool changed = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.clipAnimations;
            }
            
            if (clips == null || clips.Length == 0)
            {
                // Fallback if Unity hasn't generated the default clips array yet
                importer.SaveAndReimport();
                clips = importer.defaultClipAnimations;
            }

            if (clips != null && clips.Length > 0)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i].loopTime != loop || clips[i].lockRootPositionXZ != bakeRoot || clips[i].lockRootHeightY != bakeRoot)
                    {
                        clips[i].loopTime = loop;
                        if (bakeRoot)
                        {
                            clips[i].lockRootPositionXZ = true;
                            clips[i].lockRootHeightY = true;
                            clips[i].lockRootRotation = true;
                        }
                        changed = true;
                    }
                }
                
                if (changed)
                {
                    importer.clipAnimations = clips;
                }
            }

            if (changed)
            {
                importer.SaveAndReimport();
                Debug.Log($"[CharacterBuilder] Configured animation {path}. Loop: {loop}, BakeRoot: {bakeRoot}");
            }
        }

        private static void GenerateAnimatorController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ANIMATOR_PATH);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ANIMATOR_PATH);
            }

            // Ensure parameters exist
            AddParameterIfNotExists(controller, "Speed", AnimatorControllerParameterType.Float);
            AddParameterIfNotExists(controller, "IsTalking", AnimatorControllerParameterType.Bool);
            AddParameterIfNotExists(controller, "Bow", AnimatorControllerParameterType.Trigger);
            AddParameterIfNotExists(controller, "Point", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;

            // Load clips
            AnimationClip idleClip = LoadAnimationClip(ANIM_IDLE);
            AnimationClip walkClip = LoadAnimationClip(ANIM_WALK);
            AnimationClip talkClip = LoadAnimationClip(ANIM_TALK);
            AnimationClip bowClip = LoadAnimationClip(ANIM_BOW);
            AnimationClip pointClip = LoadAnimationClip(ANIM_POINT);
            
            // Create or update states
            AnimatorState idleState = GetOrCreateState(rootStateMachine, "Idle");
            idleState.motion = idleClip;

            AnimatorState walkState = GetOrCreateState(rootStateMachine, "Walk");
            walkState.motion = walkClip;
            walkState.speed = ResolveWalkStateSpeed(walkClip);
            
            AnimatorState talkState = GetOrCreateState(rootStateMachine, "Talking");
            talkState.motion = talkClip;

            AnimatorState bowState = GetOrCreateState(rootStateMachine, "Bow");
            bowState.motion = bowClip;

            AnimatorState pointState = GetOrCreateState(rootStateMachine, "Point");
            pointState.motion = pointClip;

            // Transitions: Idle <-> Walk
            AddTransition(idleState, walkState, "Speed", AnimatorConditionMode.Greater, 0.1f);
            AddTransition(walkState, idleState, "Speed", AnimatorConditionMode.Less, 0.1f);

            // Transitions: Any -> Bow
            AddAnyStateTransition(rootStateMachine, bowState, "Bow");
            AddTransition(bowState, idleState, null, AnimatorConditionMode.If, 0, true); // Has exit time

            // Transitions: Any -> Point
            AddAnyStateTransition(rootStateMachine, pointState, "Point");
            AddTransition(pointState, idleState, null, AnimatorConditionMode.If, 0, true); // Has exit time

            // Transitions: Idle <-> Talking
            AddTransition(idleState, talkState, "IsTalking", AnimatorConditionMode.If, 0);
            AddTransition(talkState, idleState, "IsTalking", AnimatorConditionMode.IfNot, 0);

            rootStateMachine.defaultState = idleState;
            EditorUtility.SetDirty(controller);
        }

        private static float ResolveWalkStateSpeed(AnimationClip walkClip)
        {
            if (walkClip == null)
            {
                Debug.LogWarning("[CharacterBuilder] Walk clip is missing. Using animator walk speed 1.");
                return 1f;
            }

            float clipMetersPerSecond = new Vector2(walkClip.averageSpeed.x, walkClip.averageSpeed.z).magnitude;
            if (clipMetersPerSecond <= 0.01f)
            {
                clipMetersPerSecond = Mathf.Abs(walkClip.averageSpeed.y);
            }

            if (clipMetersPerSecond <= 0.01f)
            {
                Debug.LogWarning($"[CharacterBuilder] Could not infer foot speed from walk clip '{walkClip.name}'. Using animator walk speed 1.");
                return 1f;
            }

            float playbackSpeed = NPCStreetPatrol.DefaultWalkSpeed / clipMetersPerSecond;
            return Mathf.Clamp(playbackSpeed, 0.35f, 2.5f);
        }

        private static void AddParameterIfNotExists(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (var p in controller.parameters)
            {
                if (p.name == name) return;
            }
            controller.AddParameter(name, type);
        }

        private static AnimatorState GetOrCreateState(AnimatorStateMachine sm, string name)
        {
            foreach (var state in sm.states)
            {
                if (state.state.name == name) return state.state;
            }
            return sm.AddState(name);
        }

        private static void AddTransition(AnimatorState from, AnimatorState to, string paramName, AnimatorConditionMode mode, float threshold, bool hasExitTime = false)
        {
            foreach (var t in from.transitions)
            {
                if (t.destinationState == to) return;
            }
            var transition = from.AddTransition(to);
            transition.hasExitTime = hasExitTime;
            transition.hasFixedDuration = true;
            transition.duration = hasExitTime ? 0.18f : 0.12f;
            if (!string.IsNullOrEmpty(paramName))
            {
                transition.AddCondition(mode, threshold, paramName);
            }
        }

        private static void AddAnyStateTransition(AnimatorStateMachine sm, AnimatorState to, string paramName)
        {
            foreach (var t in sm.anyStateTransitions)
            {
                if (t.destinationState == to) return;
            }
            var transition = sm.AddAnyStateTransition(to);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.12f;
            transition.AddCondition(AnimatorConditionMode.If, 0, paramName);
        }

        private static AnimationClip LoadAnimationClip(string assetPath)
        {
            var direct = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (direct != null) return direct;

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", System.StringComparison.Ordinal))
                {
                    return clip;
                }
            }

            Debug.LogError($"[CharacterBuilder] Missing animation clip in {assetPath}");
            return null;
        }

        private static string ResolveCharacterPath(params string[] candidates)
        {
            foreach (string path in candidates)
            {
                if (string.IsNullOrEmpty(path)) continue;
                if (File.Exists(Path.Combine(Application.dataPath, path.Substring("Assets/".Length))))
                {
                    return path;
                }
            }

            return candidates.Length > 0 ? candidates[0] : string.Empty;
        }

        private static void CreateVisualPrefab(string modelPath, string prefabName, float targetHeight, bool skipIfExists = false)
        {
            string prefabPath = $"{PREFAB_DIR}/{prefabName}.prefab";
            if (skipIfExists && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[CharacterBuilder] Skipping '{prefabName}' because {prefabPath} already exists.");
                return;
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogError($"[CharacterBuilder] Missing character model: {modelPath}");
                return;
            }

            GameObject root = new GameObject(prefabName);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            FixCharacterMaterials(visual, prefabName);
            NormalizeCharacterVisual(visual.transform, targetHeight);
            visual.transform.localRotation = Quaternion.identity;

            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ANIMATOR_PATH);
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;
            }
            else
            {
                Debug.LogError($"[CharacterBuilder] Model has no Animator: {modelPath}");
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[CharacterBuilder] Generated {prefabName}");
        }

        private static IEnumerable<(string speakerId, string modelPath)> FindMixamoCharacters()
        {
            string absoluteRoot = Path.Combine(Application.dataPath, MIXAMO_CHARACTER_ROOT.Substring("Assets/".Length));
            if (!Directory.Exists(absoluteRoot))
            {
                Debug.LogWarning($"[CharacterBuilder] Mixamo character folder not found: {MIXAMO_CHARACTER_ROOT}");
                yield break;
            }

            foreach (string characterDirectory in Directory.GetDirectories(absoluteRoot))
            {
                string speakerId = Path.GetFileName(characterDirectory);
                string sourceDirectory = Path.Combine(characterDirectory, "source");
                if (!Directory.Exists(sourceDirectory))
                {
                    Debug.LogWarning($"[CharacterBuilder] Skipping '{speakerId}' because it has no source folder.");
                    continue;
                }

                string[] fbxFiles = Directory.GetFiles(sourceDirectory, "*.fbx", SearchOption.AllDirectories);
                if (fbxFiles.Length == 0)
                {
                    fbxFiles = Directory.GetFiles(sourceDirectory, "*.Fbx", SearchOption.AllDirectories);
                }

                if (fbxFiles.Length == 0)
                {
                    Debug.LogWarning($"[CharacterBuilder] Skipping '{speakerId}' because no FBX was found under source.");
                    continue;
                }

                System.Array.Sort(fbxFiles);
                string assetPath = ToAssetPath(fbxFiles[0]);
                yield return (speakerId, assetPath);
            }
        }

        private static bool HasLegacyPrefab(string speakerId)
        {
            foreach (var legacy in LegacyCharacterPrefabs)
            {
                if (legacy.speakerId != speakerId)
                {
                    continue;
                }

                string path = $"{PREFAB_DIR}/{legacy.legacyPrefabName}.prefab";
                return AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            }

            return false;
        }

        private static string ToAssetPath(string absolutePath)
        {
            string normalized = absolutePath.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (!normalized.StartsWith(dataPath, System.StringComparison.Ordinal))
            {
                return normalized;
            }

            return "Assets" + normalized.Substring(dataPath.Length);
        }

        private static void FixCharacterMaterials(GameObject visual, string prefabName)
        {
            EnsureFolderExists(CHARACTER_MATERIAL_DIR);

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null) continue;

                    string materialName = SanitizeAssetName($"{prefabName}_{renderer.name}_{i}_{source.name}_URP");
                    string materialPath = $"{CHARACTER_MATERIAL_DIR}/{materialName}.mat";
                    Material fixedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (fixedMaterial == null)
                    {
                        fixedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard"));
                        AssetDatabase.CreateAsset(fixedMaterial, materialPath);
                    }

                    CopyMaterialLook(source, fixedMaterial);
                    materials[i] = fixedMaterial;
                    EditorUtility.SetDirty(fixedMaterial);
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static void CopyMaterialLook(Material source, Material target)
        {
            Texture mainTexture = null;
            if (source.HasProperty("_BaseMap")) mainTexture = source.GetTexture("_BaseMap");
            if (mainTexture == null && source.HasProperty("_MainTex")) mainTexture = source.GetTexture("_MainTex");

            Color color = Color.white;
            if (source.HasProperty("_BaseColor")) color = source.GetColor("_BaseColor");
            else if (source.HasProperty("_Color")) color = source.GetColor("_Color");

            if (target.HasProperty("_BaseMap")) target.SetTexture("_BaseMap", mainTexture);
            if (target.HasProperty("_MainTex")) target.SetTexture("_MainTex", mainTexture);
            if (target.HasProperty("_BaseColor")) target.SetColor("_BaseColor", color);
            if (target.HasProperty("_Color")) target.SetColor("_Color", color);

            if (source.HasProperty("_BumpMap") && target.HasProperty("_BumpMap"))
            {
                target.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
            }
        }

        private static string SanitizeAssetName(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                builder.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
            }

            return builder.ToString();
        }

        private static void NormalizeCharacterVisual(Transform visual, float targetHeight)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float height = bounds.size.y;
            if (height <= 0.01f) return;

            float scale = targetHeight / height;
            visual.localScale = Vector3.one * scale;
            visual.localPosition = new Vector3(
                -bounds.center.x * scale,
                -bounds.min.y * scale,
                -bounds.center.z * scale
            );

            Renderer[] finalRenderers = visual.GetComponentsInChildren<Renderer>(true);
            if (finalRenderers.Length == 0) return;

            Bounds finalBounds = finalRenderers[0].bounds;
            for (int i = 1; i < finalRenderers.Length; i++)
            {
                finalBounds.Encapsulate(finalRenderers[i].bounds);
            }

            visual.localPosition += Vector3.up * Mathf.Max(0f, -finalBounds.min.y);
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && parent != "Assets" && !AssetDatabase.IsValidFolder(parent))
                EnsureFolderExists(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
        }
    }
}
