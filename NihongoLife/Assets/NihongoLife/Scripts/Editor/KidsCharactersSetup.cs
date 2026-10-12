#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Kids Characters for Survival (SigmoidButton), imported under ThirdParty/New Chac.
    /// · Run: moves the package there (if still at Assets/ForSurvival) and writes an audit of every model (rig, Avatar,
    ///   height, bones, materials) and of the humanoid clips.
    /// · Build: the playable kids — one prefab per humanoid model in Resources/Characters/Kids, animated by the
    ///   player's own NL_Humanoid controller through an override (the pack's idle/walk/happy-talk for locomotion; the
    ///   Mixamo work, fishing, bow, sit and rest clips stay, retargeted through the humanoid Avatar).
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.KidsCharactersSetup.Run|Build
    /// </summary>
    public static class KidsCharactersSetup
    {
        public const string Root = "Assets/ThirdParty/New Chac/ForSurvival/KidsCharacters/";

        public const string PrefabFolder = "Assets/NihongoLife/Resources/Characters/Kids/";
        private const string OverrideFolder = "Assets/NihongoLife/Animations/Kids/";
        private const string BaseController = "Assets/NihongoLife/Animations/NL_Humanoid.controller";
        /// <summary>The pack's kids are ~1.04 m chibis; at 1.35× (~1.4 m) they read as children next to the adult NPCs
        /// and still hold the farm tools and fishing rod sensibly.</summary>
        public const float KidScale = 1.35f;

        public static readonly (string model, string id)[] Kids =
        {
            ("Boy0", "kid_haruto"), ("Girl0", "kid_yui"), ("Boy1", "kid_sota"), ("Girl1", "kid_hina"),
            ("Boy2", "kid_riku"), ("Girl2", "kid_mio"), ("Boy3", "kid_yuto"), ("Girl3", "kid_sakura"),
        };

        public static void Build()
        {
            try
            {
                System.IO.Directory.CreateDirectory(PrefabFolder);
                System.IO.Directory.CreateDirectory(OverrideFolder);
                var boy = Override("boy");
                var girl = Override("girl");
                foreach (var (model, id) in Kids)
                {
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + $"Fbx/Humanoid/{model}_Humanoid.fbx");
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                    instance.name = "NL_" + id;
                    instance.transform.localScale = Vector3.one * KidScale;
                    var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
                    animator.runtimeAnimatorController = model.StartsWith("Girl") ? girl : boy;
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    foreach (var r in instance.GetComponentsInChildren<SkinnedMeshRenderer>()) r.updateWhenOffscreen = true;
                    PrefabUtility.SaveAsPrefabAsset(instance, PrefabFolder + "NL_" + id + ".prefab");
                    Object.DestroyImmediate(instance);
                }
                AssetDatabase.SaveAssets();
                Debug.Log($"[KidsSetup] Built {Kids.Length} kid prefabs.");
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
        /// Every character becomes a Kid: the NPC prefabs placed in the scenes (cashier, guide, neighbour) and the old
        /// player prefab keep their root object (so scene instances, NPC components and positions stay) and get a Kid
        /// model as their only child instead of the Mixamo adults (Lilly, Remy). Run with -executeMethod after Build().
        /// </summary>
        public static void BuildNpcs()
        {
            try
            {
                // Staff (cashiers, station staff, chefs, teachers) work behind counters sized for adults: they are the
                // "big kids", ~1.25× (≈1.7 m) so their faces clear the counter. Neighbours stay child-sized.
                var map = new (string prefab, string kid, float scale)[]
                {
                    ("NL_Cashier", "kid_mio", 1.25f), ("NL_Guide", "kid_sota", 1.25f), ("NL_Neighbor", "kid_sakura", 1f), ("NL_Player", "kid_haruto", 1f)
                };
                foreach (var folder in new[] { "Assets/NihongoLife/Prefabs/Characters/", "Assets/NihongoLife/Resources/Characters/" })
                {
                    foreach (var (prefabName, kid, scale) in map)
                    {
                        string path = folder + prefabName + ".prefab";
                        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
                        var kidPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "NL_" + kid + ".prefab");
                        if (kidPrefab == null) throw new System.Exception("Missing kid prefab " + kid + " — run Build() first.");
                        var root = PrefabUtility.LoadPrefabContents(path);
                        for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                        foreach (var a in root.GetComponents<Animator>()) Object.DestroyImmediate(a);
                        var model = (GameObject)PrefabUtility.InstantiatePrefab(kidPrefab, root.transform);
                        model.name = "Model";
                        model.transform.localPosition = Vector3.zero;
                        model.transform.localRotation = Quaternion.identity;
                        model.transform.localScale = kidPrefab.transform.localScale * scale;
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        PrefabUtility.UnloadPrefabContents(root);
                        Debug.Log($"[KidsSetup] {path} → {kid}");
                    }
                }
                AssetDatabase.SaveAssets();
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
        /// The ticket booth counter at Hibari Station is adult chest height: the staff member (Kimura) stands on a step
        /// behind it so a Kid's face clears it. Saved into 20_StationDistrict. Run with -executeMethod.
        /// </summary>
        public static void RaiseStationStaff()
        {
            try
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/NihongoLife/Scenes/20_StationDistrict.unity");
                int raised = 0;
                foreach (var npc in Object.FindObjectsByType<NihongoLife.NPC.NPCController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (npc.gameObject.scene != scene || npc.NpcId == null || npc.DisplayName == null) continue;
                    if (npc.DisplayName.IndexOf("Kimura", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var model = npc.transform.GetComponentInChildren<Animator>(true);
                    if (model == null) continue;
                    var t = model.transform.parent != null && model.transform.parent != npc.transform ? model.transform.parent : model.transform;
                    if (t.localPosition.y > 0.2f) continue; // already on the step
                    t.localPosition += Vector3.up * 0.35f / Mathf.Max(0.01f, npc.transform.lossyScale.y);
                    EditorUtility.SetDirty(t);
                    raised++;
                }
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                Debug.Log($"[KidsSetup] Station staff raised: {raised}");
                if (Application.isBatchMode) EditorApplication.Exit(raised > 0 ? 0 : 1);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        /// <summary>
        /// Resources/Characters/Kids/NL_KidAnimationSet.asset: the Kids clips KidMoodAnimator swaps in (run, tired,
        /// hurt, faint) and the NL_Humanoid motions they replace. Run with -executeMethod.
        /// </summary>
        public static void BuildAnimationSet()
        {
            try
            {
                var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(BaseController);
                AnimationClip Motion(string state)
                {
                    foreach (var layer in controller.layers)
                        foreach (var s in layer.stateMachine.states)
                            if (s.state.name == state) return s.state.motion as AnimationClip;
                    throw new System.Exception("No state " + state + " in " + BaseController);
                }
                string path = PrefabFolder + "NL_KidAnimationSet.asset";
                var set = AssetDatabase.LoadAssetAtPath<NihongoLife.Player.KidAnimationSet>(path);
                if (set == null) { set = ScriptableObject.CreateInstance<NihongoLife.Player.KidAnimationSet>(); AssetDatabase.CreateAsset(set, path); }
                set.walkKey = Motion("Walk");
                set.idleKey = Motion("Idle");
                set.layKey = Motion("Lay");
                foreach (var gender in new[] { "boy", "girl" })
                {
                    AnimationClip Kid(string clip)
                    {
                        var c = AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + $"AnimationClips/{gender}_Humanoid/{gender}_{clip}.anim");
                        if (c == null) throw new System.Exception($"Missing {gender}_{clip}");
                        return c;
                    }
                    var g = gender == "boy" ? set.boy : set.girl;
                    g.walk = Kid("move_walk"); g.idle = Kid("idle0"); g.run = Kid("move_run");
                    g.walkTired = Kid("move_walk_tired"); g.idleTired = Kid("idle_tired");
                    g.walkHurt = Kid("move_walk_injure"); g.idleHurt = Kid("idle_injure");
                    g.faint = Kid("faint_forward");
                }
                EditorUtility.SetDirty(set);
                AssetDatabase.SaveAssets();
                Debug.Log($"[KidsSetup] Animation set: walk={set.walkKey?.name} idle={set.idleKey?.name} lay={set.layKey?.name}");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        /// <summary>NL_Humanoid with the pack's own idle, walk and happy idle (talking) for boys or girls.</summary>
        private static AnimatorOverrideController Override(string gender)
        {
            string path = OverrideFolder + $"NL_Kid_{(gender == "boy" ? "Boy" : "Girl")}.overrideController";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (controller == null)
            {
                controller = new AnimatorOverrideController(AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BaseController));
                AssetDatabase.CreateAsset(controller, path);
            }
            controller.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BaseController);
            AnimationClip Kid(string clip) => AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + $"AnimationClips/{gender}_Humanoid/{gender}_{clip}.anim");
            var map = new System.Collections.Generic.Dictionary<string, AnimationClip>
            {
                ["Remy@Idle"] = Kid("idle0"),
                ["Remy@Walking"] = Kid("move_walk"),
                ["Remy@Talking"] = Kid("idle_happy"),
            };
            var overrides = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
            controller.GetOverrides(overrides);
            for (int i = 0; i < overrides.Count; i++)
            {
                var original = overrides[i].Key;
                string key = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(original)); // Mixamo clips are all "mixamo.com"
                if (!map.ContainsKey(key)) key = null;
                if (key != null && map[key] != null) overrides[i] = new System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>(original, map[key]);
            }
            controller.ApplyOverrides(overrides);
            Debug.Log($"[KidsSetup] {gender}: overrides {string.Join(", ", overrides.Where(o => o.Value != null).Select(o => System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(o.Key)) + "→" + o.Value.name))}");
            EditorUtility.SetDirty(controller);
            return controller;
        }

        public static void Run()
        {
            if (AssetDatabase.IsValidFolder("Assets/ForSurvival"))
            {
                string error = AssetDatabase.MoveAsset("Assets/ForSurvival", "Assets/ThirdParty/New Chac/ForSurvival");
                if (!string.IsNullOrEmpty(error)) Debug.LogError("[KidsAudit] Move failed: " + error);
            }
            var sb = new StringBuilder();
            foreach (var path in Directory.GetFiles(Root + "Fbx", "*.fbx", SearchOption.AllDirectories).Select(p => p.Replace('\\', '/')).OrderBy(p => p))
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToArray();
                var instance = (GameObject)Object.Instantiate(model);
                var renderers = instance.GetComponentsInChildren<Renderer>();
                Bounds b = renderers.Length > 0 ? renderers[0].bounds : new Bounds();
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                var smr = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
                sb.AppendLine($"{path.Replace(Root, "")}: rig={importer.animationType} avatar={(avatar != null ? $"{avatar.name} valid={avatar.isValid} human={avatar.isHuman}" : "none")} " +
                              $"scale={importer.globalScale} height={b.size.y:0.00} skinned={smr.Length} bones={smr.Sum(s => s.bones.Length)} clips={clips.Length} " +
                              $"mats={string.Join("|", renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name + ":" + m.shader.name).Distinct())}");
                if (avatar != null && avatar.isHuman)
                {
                    var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
                    animator.avatar = avatar;
                    var missing = new[] { HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.Neck, HumanBodyBones.Head, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftHand, HumanBodyBones.RightHand, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, HumanBodyBones.LeftToes }
                        .Where(bone => animator.GetBoneTransform(bone) == null).ToArray();
                    sb.AppendLine($"    humanoid bones missing: {(missing.Length == 0 ? "none" : string.Join(",", missing))}; head y={animator.GetBoneTransform(HumanBodyBones.Head)?.position.y:0.00}");
                }
                Object.DestroyImmediate(instance);
            }
            foreach (var folder in new[] { "boy_Humanoid", "girl_Humanoid" })
            {
                var clips = Directory.GetFiles(Root + "AnimationClips/" + folder, "*.anim").Select(p => AssetDatabase.LoadAssetAtPath<AnimationClip>(p.Replace('\\', '/'))).Where(c => c != null).ToArray();
                sb.AppendLine($"{folder}: {clips.Length} clips, humanMotion={clips.Count(c => c.humanMotion)}; {string.Join(", ", clips.Select(c => c.name).Take(200))}");
            }
            foreach (var controller in Directory.GetFiles(Root + "Animators", "*.controller"))
                sb.AppendLine("controller " + Path.GetFileName(controller));
            string outPath = null;
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-auditOut");
            if (i >= 0 && i + 1 < args.Length) outPath = args[i + 1];
            File.WriteAllText(outPath ?? "Temp/kids_audit.txt", sb.ToString());
            AssetDatabase.SaveAssets();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
#endif
