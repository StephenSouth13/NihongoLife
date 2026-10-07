#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// NotoSansJP SDF was sampled at 90 pt with padding 9 on a 1024² atlas: ~81 glyphs per page, so the
    /// Japanese/Vietnamese UI spilled onto extra atlas pages, and world-space TextMeshPro signs did not draw
    /// glyphs from those pages (missing letters on the Game Center marquees). Re-sample at 56 pt, padding 6,
    /// on a 2048² page (~900 glyphs) — one page in practice, still crisp with SDF, 4 MB Alpha8 for WebGL.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.FontAtlasUpgrader.Upgrade
    /// </summary>
    public static class FontAtlasUpgrader
    {
        private const string FontAssetPath = "Assets/NihongoLife/Fonts/NotoSansJP SDF.asset";
        private const string SourceFontPath = "Assets/NihongoLife/Fonts/NotoSansJP.otf";
        private const int PointSize = 56, Padding = 6, AtlasSize = 2048;

        public static void Upgrade()
        {
            try
            {
                var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
                var source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
                if (asset == null || source == null) throw new InvalidOperationException("NotoSansJP font asset or source font missing.");

                FontEngine.InitializeFontEngine();
                if (FontEngine.LoadFontFace(source, PointSize) != FontEngineError.Success)
                    throw new InvalidOperationException("Could not load NotoSansJP at " + PointSize + " pt.");
                FaceInfo face = FontEngine.GetFaceInfo();

                var so = new SerializedObject(asset);
                so.FindProperty("m_AtlasWidth").intValue = AtlasSize;
                so.FindProperty("m_AtlasHeight").intValue = AtlasSize;
                so.FindProperty("m_AtlasPadding").intValue = Padding;
                so.ApplyModifiedPropertiesWithoutUndo();
                asset.faceInfo = face;
                asset.ClearFontAssetData(false); // drops cached glyphs and re-creates the atlas at the new size

                var material = asset.material;
                if (material != null)
                {
                    material.SetFloat(ShaderUtilities.ID_GradientScale, Padding + 1);
                    material.SetFloat(ShaderUtilities.ID_TextureWidth, AtlasSize);
                    material.SetFloat(ShaderUtilities.ID_TextureHeight, AtlasSize);
                    EditorUtility.SetDirty(material);
                }
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                Debug.Log($"[FontAtlasUpgrader] {asset.name}: {face.pointSize} pt, padding {Padding}, atlas {asset.atlasTextures[0].width}x{asset.atlasTextures[0].height}, pages {asset.atlasTextures.Length}.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }
    }
}
#endif
