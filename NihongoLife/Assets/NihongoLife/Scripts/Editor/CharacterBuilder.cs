using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.IO;

namespace NihongoLife.Editor
{
    public static class CharacterBuilder
    {
        private const string REMY_PATH = "Assets/ThirdParty/Mixamo/Characters/remy/source/Remy.fbx";
        private const string ELIZABETH_PATH = "Assets/ThirdParty/Mixamo/Characters/elizabeth-female-3-d-character/source/Elizabeth_Female_3_d_Character/Elizabeth_Female_3_d_Character.fbx";
        
        private const string ANIM_IDLE = "Assets/ThirdParty/Mixamo/Animations/Remy@Idle.fbx";
        private const string ANIM_WALK = "Assets/ThirdParty/Mixamo/Animations/Remy@Walking.fbx";
        private const string ANIM_TALK = "Assets/ThirdParty/Mixamo/Animations/Remy@Talking.fbx";
        private const string ANIM_BOW = "Assets/ThirdParty/Mixamo/Animations/Remy@Quick Informal Bow.fbx";
        private const string ANIM_POINT = "Assets/ThirdParty/Mixamo/Animations/Remy@Pointing.fbx";

        private const string ANIMATOR_PATH = "Assets/NihongoLife/Animations/NL_Humanoid.controller";
        private const string PREFAB_DIR = "Assets/NihongoLife/Prefabs/Characters";

        [MenuItem("NihongoLife/Characters/Build Character System")]
        public static void BuildCharacterSystem()
        {
            EnsureFolderExists("Assets/NihongoLife/Animations");
            EnsureFolderExists(PREFAB_DIR);

            ConfigureModel(REMY_PATH);
            ConfigureModel(ELIZABETH_PATH);

            ConfigureAnimation(ANIM_IDLE, true);
            ConfigureAnimation(ANIM_WALK, true, true);
            ConfigureAnimation(ANIM_TALK, true);
            ConfigureAnimation(ANIM_BOW, false);
            ConfigureAnimation(ANIM_POINT, false);

            GenerateAnimatorController();

            CreateVisualPrefab(REMY_PATH, "NL_Player", 1.72f);
            CreateVisualPrefab(ELIZABETH_PATH, "NL_Cashier", 1.68f);

            AssetDatabase.SaveAssets();
            Debug.Log("[CharacterBuilder] Character system built successfully.");
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
            
            // Create or update states
            AnimatorState idleState = GetOrCreateState(rootStateMachine, "Idle");
            idleState.motion = idleClip;

            AnimatorState walkState = GetOrCreateState(rootStateMachine, "Walk");
            walkState.motion = walkClip;
            
            AnimatorState talkState = GetOrCreateState(rootStateMachine, "Talking");
            talkState.motion = talkClip;

            AnimatorState bowState = GetOrCreateState(rootStateMachine, "Bow");
            bowState.motion = bowClip;

            // Transitions: Idle <-> Walk
            AddTransition(idleState, walkState, "Speed", AnimatorConditionMode.Greater, 0.1f);
            AddTransition(walkState, idleState, "Speed", AnimatorConditionMode.Less, 0.1f);

            // Transitions: Any -> Bow
            AddAnyStateTransition(rootStateMachine, bowState, "Bow");
            AddTransition(bowState, idleState, null, AnimatorConditionMode.If, 0, true); // Has exit time

            // Transitions: Idle <-> Talking
            AddTransition(idleState, talkState, "IsTalking", AnimatorConditionMode.If, 0);
            AddTransition(talkState, idleState, "IsTalking", AnimatorConditionMode.IfNot, 0);

            rootStateMachine.defaultState = idleState;
            EditorUtility.SetDirty(controller);
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

        private static void CreateVisualPrefab(string modelPath, string prefabName, float targetHeight)
        {
            string prefabPath = $"{PREFAB_DIR}/{prefabName}.prefab";
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
            NormalizeCharacterVisual(visual.transform, targetHeight);

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
