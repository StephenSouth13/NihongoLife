using UnityEngine;
using UnityEditor;
using TMPro;

namespace NihongoLife.Editor
{
    public static class FontSetup
    {
        [MenuItem("NihongoLife/Setup Japanese Font")]
        public static void CreateJapaneseFontAsset()
        {
            string fontPath = "Assets/NihongoLife/Fonts/NotoSansJP.otf";
            string assetPath = "Assets/NihongoLife/Fonts/NotoSansJP SDF.asset";

            // Import the font if necessary
            AssetDatabase.ImportAsset(fontPath);
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(fontPath);

            if (sourceFont == null)
            {
                Debug.LogError($"[FontSetup] Could not find source font at {fontPath}. Please ensure it is downloaded.");
                return;
            }

            // Check if asset already exists
            TMP_FontAsset existingAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existingAsset != null)
            {
                Debug.Log($"[FontSetup] Font asset already exists at {assetPath}.");
                return;
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
                
                AssetDatabase.CreateAsset(fontAsset, assetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[FontSetup] Successfully created dynamic font asset at {assetPath}");
            }
            else
            {
                Debug.LogError("[FontSetup] Failed to create TMP_FontAsset.");
            }
        }
    }
}
