#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Replaces the last retargeted adult (Mixamo) motions in NL_Humanoid with Kid motions and makes locomotion follow
    /// the ground speed:
    /// · Walk plays faster as the character moves faster (MoveScale, set by CharacterAnimationController), smoother
    ///   idle↔walk blends.
    /// · Work_Clear (weeding) = action_release_trapped (crouched tug at the ground) · Work_Feed = idle_crouch_treated
    ///   (crouched, hand held out low) · Work_Carry / Work_Hold / Fish_Wait = idle_flashlight (both hands in front)
    ///   · Work_Water = action_treat · Point = action_switch_r · Bow = idle0 (+ the procedural lean).
    /// · Sit and Lay = composed clips (Animations/Kids/Kid_Sit.anim, Kid_Sleep.anim): the seated/lying body pose in
    ///   muscle space, with the Kid idle's head and breathing on top, so a resting kid is never frozen.
    /// Girls get the girl_* version of every Kids clip through NL_Kid_Girl. Pose sheets for choosing clips:
    /// AnimationCandidateSheetTests. Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.KidMotionBuilder.Build
    /// </summary>
    public static class KidMotionBuilder
    {
        private const string Controller = "Assets/NihongoLife/Animations/NL_Humanoid.controller";
        private const string Folder = "Assets/NihongoLife/Animations/Kids/";
        private const string Mixamo = "Assets/ThirdParty/Mixamo/Animations/";
        public const string MoveScaleParam = "MoveScale";

        private static readonly (string state, string clip)[] Actions =
        {
            ("Work_Clear", "action_release_trapped"), ("Work_Feed", "idle_crouch_treated"),
            ("Work_Carry", "idle_flashlight"), ("Work_Hold", "idle_flashlight"), ("Fish_Wait", "idle_flashlight"),
            ("Work_Water", "action_treat"), ("Point", "action_switch_r"), ("Bow", "idle0"),
        };

        public static void Build()
        {
            try
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
                var machine = controller.layers[0].stateMachine;
                AnimatorState State(string name) => machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name)
                                                    ?? throw new System.Exception("No state " + name);

                // Locomotion cadence follows the ground speed.
                if (!controller.parameters.Any(p => p.name == MoveScaleParam))
                    controller.AddParameter(new AnimatorControllerParameter { name = MoveScaleParam, type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
                var walk = State("Walk");
                walk.speedParameterActive = true;
                walk.speedParameter = MoveScaleParam;
                foreach (var t in State("Idle").transitions.Concat(walk.transitions)) { t.duration = 0.2f; t.offset = 0f; }

                foreach (var (stateName, clip) in Actions) State(stateName).motion = Boy(clip);

                var sit = Compose("Kid_Sit", MixamoClip("Remy@Sitting.fbx"), MixamoClip("Remy@Sitting.fbx"), breath: 0.035f);
                var sleep = Compose("Kid_Sleep", MixamoClip("Remy@Laying Nodding.fbx"), MixamoClip("Remy@Laying Nodding.fbx"), breath: 0.05f, headFromIdle: false);
                State("Sit").motion = sit;
                State("Lay").motion = sleep;
                EditorUtility.SetDirty(controller);
                // KidMoodAnimator swaps the clip behind Lay for a faint: its key is now the sleep clip.
                var set = AssetDatabase.LoadAssetAtPath<NihongoLife.Player.KidAnimationSet>(KidsCharactersSetup.PrefabFolder + "NL_KidAnimationSet.asset");
                if (set != null) { set.layKey = sleep; EditorUtility.SetDirty(set); }

                AddJump(controller);
                RefreshGirlOverrides();
                AssetDatabase.SaveAssets();
                Debug.Log("[KidMotion] " + string.Join(", ", Actions.Select(a => a.state + "=" + a.clip)) + ", Sit=Kid_Sit, Lay=Kid_Sleep, Walk speed ← " + MoveScaleParam);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        /// <summary>
        /// Only the Jump state: a Kid jump (boy_jump_default; girls get girl_jump_default through NL_Kid_Girl) that
        /// returns to Idle by itself. Without it CharacterAnimationController fell back to a Mixamo clip that is never
        /// loaded in the game ("could not find Jump clip 'Remy@Jump'").
        /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.KidMotionBuilder.BuildJump
        /// </summary>
        public static void BuildJump()
        {
            try
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
                AddJump(controller);
                RefreshGirlOverrides();
                AssetDatabase.SaveAssets();
                Debug.Log("[KidMotion] Jump = boy_jump_default (girl_jump_default for girls), back to Idle on exit.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static void AddJump(AnimatorController controller)
        {
            var machine = controller.layers[0].stateMachine;
            var idle = machine.states.Select(s => s.state).First(s => s.name == "Idle");
            var jump = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Jump") ?? machine.AddState("Jump", new Vector3(520f, 240f, 0f));
            jump.motion = Boy("jump_default");
            foreach (var t in jump.transitions.ToArray()) jump.RemoveTransition(t);
            var back = jump.AddTransition(idle);
            back.hasExitTime = true;
            back.exitTime = 0.85f;
            back.duration = 0.15f;
            EditorUtility.SetDirty(controller);
        }

        /// <summary>Girls: every boy_* clip in the controller → girl_* of the same name.</summary>
        private static void RefreshGirlOverrides()
        {
            var girl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(Folder + "NL_Kid_Girl.overrideController");
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            girl.GetOverrides(overrides);
            for (int i = 0; i < overrides.Count; i++)
            {
                var key = overrides[i].Key;
                if (key == null || !key.name.StartsWith("boy_")) continue;
                var g = AssetDatabase.LoadAssetAtPath<AnimationClip>(KidsCharactersSetup.Root + "AnimationClips/girl_Humanoid/girl_" + key.name.Substring(4) + ".anim");
                if (g != null) overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(key, g);
            }
            girl.ApplyOverrides(overrides);
            EditorUtility.SetDirty(girl);
        }

        private static AnimationClip Boy(string clip) =>
            AssetDatabase.LoadAssetAtPath<AnimationClip>(KidsCharactersSetup.Root + $"AnimationClips/boy_Humanoid/boy_{clip}.anim") ?? throw new System.Exception("Missing boy_" + clip);

        private static AnimationClip MixamoClip(string file) =>
            AssetDatabase.LoadAllAssetsAtPath(Mixamo + file).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"))
            ?? throw new System.Exception("Missing " + file);

        private static bool IsHead(string p) => p.Contains("Neck") || p.Contains("Head") || p.Contains("Eye") || p.Contains("Jaw");
        private static bool IsArm(string p) => p.Contains("Arm") || p.Contains("Shoulder") || p.Contains("Hand") || p.Contains("Thumb") || p.Contains("Index")
                                               || p.Contains("Middle") || p.Contains("Ring") || p.Contains("Little");
        private static bool IsRoot(string p) => p.StartsWith("RootT") || p.StartsWith("RootQ");

        /// <summary>
        /// A looping resting clip: legs, hips and spine from <paramref name="body"/> (its last frame when bodyAtEnd,
        /// else its middle), arms and root from <paramref name="pose"/> (middle), head from the Kid idle (animated), and
        /// a slow breath on the chest and spine.
        /// </summary>
        private static AnimationClip Compose(string name, AnimationClip body, AnimationClip pose, float breath, bool bodyAtEnd = false, bool headFromIdle = true)
        {
            var idle = Boy("idle0");
            float cycle = idle.length;
            float length = cycle * Mathf.Max(1, Mathf.RoundToInt(4f / cycle)); // ~4 s: one calm breath
            float bodyTime = bodyAtEnd ? body.length - 0.01f : body.length * 0.5f;
            float poseTime = pose.length * 0.5f;
            var clip = new AnimationClip { name = name, frameRate = 30f };

            var idleCurves = AnimationUtility.GetCurveBindings(idle).ToDictionary(b => b.propertyName, b => AnimationUtility.GetEditorCurve(idle, b));
            var written = new HashSet<string>();
            void Constant(EditorCurveBinding b, float v) { AnimationUtility.SetEditorCurve(clip, b, AnimationCurve.Constant(0f, length, v)); written.Add(b.propertyName); }

            foreach (var b in AnimationUtility.GetCurveBindings(pose))
            {
                if (IsHead(b.propertyName) && headFromIdle) continue;
                if (IsArm(b.propertyName) || IsRoot(b.propertyName) || IsHead(b.propertyName)) Constant(b, AnimationUtility.GetEditorCurve(pose, b).Evaluate(poseTime));
            }
            foreach (var b in AnimationUtility.GetCurveBindings(body))
            {
                string p = b.propertyName;
                if (written.Contains(p) || IsHead(p) || IsArm(p) || IsRoot(p)) continue;
                float v = AnimationUtility.GetEditorCurve(body, b).Evaluate(bodyTime);
                if (p == "Chest Front-Back" || p == "Spine Front-Back" || p == "UpperChest Front-Back")
                {
                    var curve = new AnimationCurve();
                    for (float t = 0f; t <= length + 0.001f; t += length / 24f) curve.AddKey(t, v + breath * Mathf.Sin(t / length * Mathf.PI * 2f));
                    AnimationUtility.SetEditorCurve(clip, b, curve);
                    written.Add(p);
                }
                else Constant(b, v);
            }
            // Head and neck from the Kid idle, repeated to fill the clip: small natural movement.
            foreach (var (prop, idleCurve) in idleCurves.Where(kv => headFromIdle && IsHead(kv.Key)))
            {
                var b = EditorCurveBinding.FloatCurve("", typeof(Animator), prop);
                var curve = new AnimationCurve();
                for (float t = 0f; t <= length + 0.001f; t += 1f / 15f) curve.AddKey(t, idleCurve.Evaluate(t % cycle));
                AnimationUtility.SetEditorCurve(clip, b, curve);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(pose);
            settings.loopTime = true;
            settings.stopTime = length;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            string path = Folder + name + ".anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null) { EditorUtility.CopySerialized(clip, existing); EditorUtility.SetDirty(existing); return existing; }
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }
    }
}
#endif
