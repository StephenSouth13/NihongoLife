using UnityEngine;
using UnityEditor;
using TMPro;

namespace NihongoLife.Editor
{
    public static class FontSetup
    {
        private const string SourceFontPath = "Assets/NihongoLife/Fonts/NotoSansJP.otf";
        private const string FontAssetPath = "Assets/NihongoLife/Fonts/NotoSansJP SDF.asset";

        [MenuItem("NihongoLife/Setup Japanese Font")]
        public static void CreateJapaneseFontAsset()
        {
            EnsureJapaneseFontAsset(forceRecreate: true);
        }

        public static TMP_FontAsset EnsureJapaneseFontAsset(bool forceRecreate = false)
        {
            TMP_FontAsset existingAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (!forceRecreate && IsUsable(existingAsset))
            {
                return existingAsset;
            }

            if (existingAsset != null)
            {
                Debug.LogWarning($"[FontSetup] Recreating broken font asset at {FontAssetPath}. Its atlas texture was missing.");
                AssetDatabase.DeleteAsset(FontAssetPath);
            }

            // Import the font if necessary
            AssetDatabase.ImportAsset(SourceFontPath);
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);

            if (sourceFont == null)
            {
                Debug.LogError($"[FontSetup] Could not find source font at {SourceFontPath}. Please ensure it is downloaded.");
                return null;
            }

            // Create Dynamic Font Asset
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont, 
                90, // Sampling point size
                9,  // Padding
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 
                1024, 1024, 
                AtlasPopulationMode.Dynamic
            );

            if (fontAsset != null)
            {
                fontAsset.name = "NotoSansJP SDF";
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                
                AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
                if (fontAsset.atlasTexture != null)
                {
                    fontAsset.atlasTexture.name = "NotoSansJP SDF Atlas";
                    AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
                }

                if (fontAsset.material != null)
                {
                    fontAsset.material.name = "NotoSansJP SDF Material";
                    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(FontAssetPath, ImportAssetOptions.ForceUpdate);
                Debug.Log($"[FontSetup] Successfully created dynamic font asset at {FontAssetPath}");
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            }

            Debug.LogError("[FontSetup] Failed to create TMP_FontAsset.");
            return null;
        }

        private static bool IsUsable(TMP_FontAsset fontAsset)
        {
            return fontAsset != null
                && fontAsset.atlasTextures != null
                && fontAsset.atlasTextures.Length > 0
                && fontAsset.atlasTextures[0] != null;
        }
    }
}
