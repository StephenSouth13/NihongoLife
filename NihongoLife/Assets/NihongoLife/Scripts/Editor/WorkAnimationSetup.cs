using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NihongoLife.Editor
{
    /// <summary>
    /// Work and fishing animations for the shared NL_Humanoid controller (batch: -executeMethod
    /// NihongoLife.Editor.WorkAnimationSetup.Run). Configures the Mixamo Remy clips as Humanoid (avatar from each file,
    /// root baked into the pose so kneeling stays on the ground), then adds one state per work pose. Gameplay plays them
    /// by state name through CharacterAnimationController.PlayWork, so nothing depends on clips being loaded by name.
    /// Probe() renders every clip and the fishing models to a folder (NL_PROBE_DIR) to check the real assets.
    /// </summary>
    public static class WorkAnimationSetup
    {
        private const string AnimDir = "Assets/ThirdParty/Mixamo/Animations";
        private const string ControllerPath = "Assets/NihongoLife/Animations/NL_Humanoid.controller";
        public const string FishDir = "Assets/ThirdParty/Mixamo/fish and tool/FBX-20261010T144531Z-1-001/FBX";

        /// <summary>Clip file (relative to AnimDir) → loop. Files here are configured by this class only
        /// (ThirdPartyAssetIntegrator skips them).</summary>
        public static readonly Dictionary<string, bool> Clips = new()
        {
            { "Remy@Fishing Cast.fbx", false },
            { "Remy@Fishing Idle.fbx", true },
            { "Remy@Harvesting.fbx", true },
            { "Remy@Watering.fbx", true },
            { "Farming Pack/dig and plant seeds.fbx", true },
            { "Farming Pack/plant a plant.fbx", true },
            { "Farming Pack/pull plant.fbx", true },
            { "Farming Pack/pull plant (2).fbx", true },
            { "Farming Pack/pick fruit.fbx", true },
            { "Farming Pack/watering.fbx", true },
            { "Farming Pack/cow milking.fbx", true },
            { "Farming Pack/kneeling idle.fbx", true },
            { "Farming Pack/box idle.fbx", true },
            { "Farming Pack/holding idle.fbx", true },
            { "Farming Pack/plant tree.fbx", true },
        };

        public static bool Owns(string assetPath) => Clips.Keys.Any(k => assetPath.Replace('\\', '/').EndsWith("/" + k, StringComparison.OrdinalIgnoreCase));

        // ─────────── import ───────────

        public static void ConfigureImports()
        {
            foreach (var (file, loop) in Clips)
            {
                string path = AnimDir + "/" + file;
                if (AssetImporter.GetAtPath(path) is not ModelImporter importer) { Debug.LogWarning("[WorkAnimation] missing " + path); continue; }
                bool dirty = importer.animationType != ModelImporterAnimationType.Human || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel;
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                if (dirty) importer.SaveAndReimport();
                var clips = importer.defaultClipAnimations;
                foreach (var clip in clips)
                {
                    clip.loopTime = loop;
                    clip.lockRootRotation = true;
                    clip.lockRootHeightY = true;
                    clip.lockRootPositionXZ = true;
                    clip.keepOriginalPositionY = true;
                    clip.keepOriginalOrientation = true;
                    clip.keepOriginalPositionXZ = true;
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }

        /// <summary>Animator state → clip file. The names are the contract with gameplay (WorkPose).</summary>
        public static readonly (string state, string file, bool feetIk)[] States =
        {
            ("Work_DigHand", "Farming Pack/dig and plant seeds.fbx", false),
            ("Work_Hoe", "Remy@Harvesting.fbx", true),
            ("Work_Plant", "Farming Pack/plant a plant.fbx", false),
            ("Work_Water", "Remy@Watering.fbx", true),
            ("Work_Harvest", "Farming Pack/pull plant (2).fbx", false),
            ("Work_Clear", "Farming Pack/pull plant.fbx", false),
            ("Work_Feed", "Farming Pack/kneeling idle.fbx", false),
            ("Work_Shelf", "Farming Pack/pick fruit.fbx", true),
            ("Work_Carry", "Farming Pack/box idle.fbx", true),
            ("Work_Hold", "Farming Pack/holding idle.fbx", true),
            ("Fish_Cast", "Remy@Fishing Cast.fbx", true),
            ("Fish_Wait", "Remy@Fishing Idle.fbx", true),
        };

        /// <summary>Batch entry: import settings + states on NL_Humanoid (idempotent).</summary>
        public static void Run()
        {
            try
            {
                AssetDatabase.Refresh();
                ConfigureImports();
                AddStates();
                AssetDatabase.SaveAssets();
                Debug.Log("[WorkAnimation] NL_Humanoid work states ready.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        public static void AddStates()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ?? throw new InvalidOperationException("Missing " + ControllerPath);
            var sm = controller.layers[0].stateMachine;
            int row = 0;
            foreach (var (stateName, file, feetIk) in States)
            {
                var clip = LoadClip(file) ?? throw new InvalidOperationException("Missing clip " + file);
                var state = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == stateName)
                            ?? sm.AddState(stateName, new Vector3(650f, -120f + row * 55f, 0f));
                state.motion = clip;
                state.iKOnFeet = feetIk;
                state.writeDefaultValues = true;
                row++;
            }
            EditorUtility.SetDirty(controller);
        }

        public static AnimationClip LoadClip(string file) =>
            AssetDatabase.LoadAllAssetsAtPath(AnimDir + "/" + file).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));

        // ─────────── probe ───────────

        public static void Probe()
        {
            try
            {
                string dir = Environment.GetEnvironmentVariable("NL_PROBE_DIR");
                if (string.IsNullOrEmpty(dir)) dir = Path.Combine(Path.GetTempPath(), "nl_probe");
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
                ConfigureImports();
                var info = new StringBuilder();

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var light = new GameObject("Sun").AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.3f;
                light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
                RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
                var cam = new GameObject("Cam").AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.82f, 0.85f, 0.88f);
                cam.fieldOfView = 35f;
                var rt = new RenderTexture(360, 360, 24);
                cam.targetTexture = rt;

                var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Characters/NL_Player.prefab");
                var body = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                var animator = body.GetComponentInChildren<Animator>();
                animator.runtimeAnimatorController = null;
                info.AppendLine($"player avatar: {animator.avatar?.name} human={animator.avatar?.isHuman}");

                foreach (var file in Clips.Keys)
                {
                    var importer = AssetImporter.GetAtPath(AnimDir + "/" + file) as ModelImporter;
                    var clip = LoadClip(file);
                    var avatar = AssetDatabase.LoadAllAssetsAtPath(AnimDir + "/" + file).OfType<Avatar>().FirstOrDefault();
                    info.AppendLine($"clip {file}: {(clip == null ? "MISSING" : $"name={clip.name} len={clip.length:0.00}s human={clip.isHumanMotion} loop={clip.isLooping}")} avatar={(avatar != null && avatar.isValid && avatar.isHuman)} type={importer?.animationType}");
                    if (clip == null) continue;
                    for (int i = 0; i < 6; i++)
                    {
                        float t = clip.length * i / 6f;
                        AnimationMode.StartAnimationMode();
                        AnimationMode.BeginSampling();
                        AnimationMode.SampleAnimationClip(animator.gameObject, clip, t);
                        AnimationMode.EndSampling();
                        body.transform.position = Vector3.zero;
                        cam.transform.position = new Vector3(2.2f, 1.3f, 2.4f);
                        cam.transform.LookAt(new Vector3(0f, 0.8f, 0f));
                        Save(cam, rt, Path.Combine(dir, $"clip_{Safe(file)}_{i}.png"));
                        AnimationMode.StopAnimationMode();
                    }
                }
                UnityEngine.Object.DestroyImmediate(body);

                foreach (var path in Directory.GetFiles(FishDir, "*.fbx").Select(p => p.Replace('\\', '/')).OrderBy(p => p))
                {
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (model == null) { info.AppendLine("model MISSING " + path); continue; }
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    var renderers = go.GetComponentsInChildren<Renderer>();
                    var b = renderers.Length > 0 ? renderers[0].bounds : new Bounds();
                    foreach (var r in renderers) b.Encapsulate(r.bounds);
                    var mats = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct()
                        .Select(m => $"{m.name}[{m.shader.name}|{(m.HasProperty("_Color") ? ColorUtility.ToHtmlStringRGB(m.color) : m.HasProperty("_BaseColor") ? ColorUtility.ToHtmlStringRGB(m.GetColor("_BaseColor")) : "-")}|tex={(m.mainTexture != null ? m.mainTexture.name : "-")}]");
                    info.AppendLine($"model {Path.GetFileNameWithoutExtension(path)}: center={b.center} size={b.size} children={string.Join(",", go.GetComponentsInChildren<Transform>().Skip(1).Select(t => t.name).Take(8))} mats={string.Join(" ", mats)}");
                    float s = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                    foreach (var (view, dirv) in new[] { ("front", new Vector3(0f, 0.2f, 1f)), ("side", new Vector3(1f, 0.2f, 0f)) })
                    {
                        cam.transform.position = b.center + dirv.normalized * s * 2.2f;
                        cam.transform.LookAt(b.center);
                        Save(cam, rt, Path.Combine(dir, $"model_{Path.GetFileNameWithoutExtension(path)}_{view}.png"));
                    }
                    UnityEngine.Object.DestroyImmediate(go);
                }
                File.WriteAllText(Path.Combine(dir, "info.txt"), info.ToString());
                Debug.Log("[WorkAnimation] probe written to " + dir);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static string Safe(string file) => Path.GetFileNameWithoutExtension(file).Replace("Remy@", "").Replace(' ', '_').Replace("(", "").Replace(")", "");

        private static void Save(Camera cam, RenderTexture rt, string file)
        {
            cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = old;
            File.WriteAllBytes(file, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
