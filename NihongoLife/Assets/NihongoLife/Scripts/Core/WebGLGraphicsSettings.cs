using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace NihongoLife.Core
{
    /// <summary>
    /// WebGL quality preset. The pixel cap is stored for the page template (it applies on the next load).
    /// Render scale applies immediately on the active URP asset.
    /// </summary>
    public static class WebGLGraphicsSettings
    {
        public const string PrefKey = "nl.webgl.preset";
        public const float DefaultDprCap = 1.5f;

        static readonly float[] DprCaps = { 1f, 1.5f, 2f };
        static readonly float[] RenderScales = { 0.75f, 1f, 1f };

        public static int Preset
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, 1), 0, 2);
            private set => PlayerPrefs.SetInt(PrefKey, Mathf.Clamp(value, 0, 2));
        }

        public static float DprCap => DprCaps[Preset];
        public static float RenderScale => RenderScales[Preset];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ApplyQualityLevel();
            ApplyPreset();
#endif
        }

        public static void ApplyQualityLevel()
        {
            string[] names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] != "WebGL") continue;
                if (QualitySettings.GetQualityLevel() != i) QualitySettings.SetQualityLevel(i, true);
                return;
            }
        }

        public static void Cycle()
        {
            Preset = (Preset + 1) % 3;
            ApplyPreset();
        }

        public static void ApplyPreset()
        {
            PublishDprCap(DprCap);
            SetRenderScale(RenderScale);
        }

        public static string LabelVi() => Preset switch
        {
            0 => "Đồ họa web: Mượt",
            2 => "Đồ họa web: Nét",
            _ => "Đồ họa web: Cân bằng"
        };

        public static string LabelEn() => Preset switch
        {
            0 => "Web graphics: Smooth",
            2 => "Web graphics: Sharp",
            _ => "Web graphics: Balanced"
        };

        public static string LabelJa() => Preset switch
        {
            0 => "Web画質: 軽め",
            2 => "Web画質: 精細",
            _ => "Web画質: 標準"
        };

        public static string ReloadHint() => Loc.T(
            "Độ phân giải màn hình áp dụng khi tải lại trang.",
            "Display sharpness applies when the page is reloaded.",
            "画面の精細さはページを再読み込みすると反映されます。");

        static void SetRenderScale(float scale)
        {
            object pipeline = GraphicsSettings.currentRenderPipeline;
            PropertyInfo property = pipeline?.GetType().GetProperty("renderScale");
            if (property == null || !property.CanWrite || property.PropertyType != typeof(float)) return;
            try { property.SetValue(pipeline, scale); }
            catch (Exception ex) { Debug.LogWarning("[WebGL] renderScale: " + ex.Message); }
        }

        static void PublishDprCap(float cap)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            NL_SetDprCap(cap);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void NL_SetDprCap(float cap);
#endif
    }
}
