using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Authors the main menu's look into 01_MainMenu and saves it (no runtime objects): a global "MenuLook" volume
    /// (ACES tonemapping, soft bloom on the warm street lights, gentle vignette and grade), post-processing + SMAA on
    /// the menu camera and a low golden-hour sun. Idempotent: rerunning updates the same objects and profile.
    /// Run: Unity -batchmode -quit -executeMethod NihongoLife.EditorTools.MenuLookBuilder.Apply
    /// </summary>
    public static class MenuLookBuilder
    {
        public const string ProfilePath = "Assets/NihongoLife/Settings/MenuLookProfile.asset";

        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(MenuSyncCheck.ScenePath, OpenSceneMode.Single);
            if (!AssetDatabase.IsValidFolder("Assets/NihongoLife/Settings")) AssetDatabase.CreateFolder("Assets/NihongoLife", "Settings");
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, ProfilePath); }

            var tone = Get<Tonemapping>(profile); tone.mode.Override(TonemappingMode.ACES);
            var bloom = Get<Bloom>(profile); bloom.threshold.Override(1.05f); bloom.intensity.Override(0.55f); bloom.scatter.Override(0.68f); bloom.tint.Override(new Color(1f, 0.86f, 0.7f));
            var vig = Get<Vignette>(profile); vig.intensity.Override(0.24f); vig.smoothness.Override(0.42f); vig.color.Override(new Color(0.05f, 0.03f, 0.06f));
            var grade = Get<ColorAdjustments>(profile); grade.postExposure.Override(0.15f); grade.contrast.Override(10f); grade.saturation.Override(8f);
            var wb = Get<WhiteBalance>(profile); wb.temperature.Override(8f); wb.tint.Override(2f);
            var smh = Get<ShadowsMidtonesHighlights>(profile);
            smh.shadows.Override(new Vector4(0.92f, 0.95f, 1.08f, 0f)); smh.highlights.Override(new Vector4(1.06f, 1.0f, 0.92f, 0f));
            EditorUtility.SetDirty(profile);

            var go = GameObject.Find("MenuLook"); if (go == null) go = new GameObject("MenuLook");
                var volume = go.GetComponent<Volume>();
            if (volume == null) volume = go.AddComponent<Volume>();
            volume.isGlobal = true; volume.priority = 10f; volume.weight = 1f; volume.sharedProfile = profile;

            var cam = GameObject.Find("Main Camera");
            if (cam != null)
            {
                var data = cam.GetComponent<UniversalAdditionalCameraData>();
                if (data == null) data = cam.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;
            }

            var sunGo = GameObject.Find("Directional Light");
            if (sunGo != null && sunGo.TryGetComponent<Light>(out var sun))
            {
                sun.color = new Color(1f, 0.86f, 0.68f);
                sun.intensity = 1.25f;
                sun.shadowStrength = 0.82f;
                sunGo.transform.rotation = Quaternion.Euler(24f, -38f, 0f);
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.42f, 0.38f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.13f, 0.12f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[MenuLookBuilder] menu look saved into " + scene.path);
        }

        private static T Get<T>(VolumeProfile p) where T : VolumeComponent
        {
            if (!p.TryGet<T>(out var c)) { c = p.Add<T>(true); AssetDatabase.AddObjectToAsset(c, p); }
            c.active = true;
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            return c;
        }
    }
}
