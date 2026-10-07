#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Re-links the textures of the NL_Neighbor character (Eminem FBX → URP materials were created without
    /// any texture, so every neighbour, commuter and diner rendered pure white). Each material
    /// "NL_Neighbor_&lt;part&gt;_&lt;n&gt;_&lt;Key&gt;_URP" gets &lt;Key&gt;_Diffuse as base map and &lt;Key&gt;_Normal as normal
    /// map; the eyelashes use an alpha-clipped RGBA texture and the corneas a faint transparent coat.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.CharacterMaterialFixer.Fix
    /// </summary>
    public static class CharacterMaterialFixer
    {
        private const string MaterialDir = "Assets/NihongoLife/Materials/Characters";
        private const string TextureDir = "Assets/ThirdParty/Mixamo/Characters/eminem/textures";

        public static void Fix()
        {
            try
            {
                int fixedCount = 0;
                foreach (string path in Directory.GetFiles(MaterialDir, "NL_Neighbor_*_URP.mat"))
                {
                    string assetPath = path.Replace('\\', '/');
                    var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                    if (material == null) continue;
                    string key = KeyOf(Path.GetFileNameWithoutExtension(assetPath));
                    var diffuse = Load(key + "_Diffuse");
                    if (diffuse == null) { Debug.LogWarning($"[CharacterMaterialFixer] No diffuse for {key}"); continue; }

                    material.SetColor("_BaseColor", Color.white);
                    material.SetTexture("_BaseMap", diffuse);
                    material.mainTexture = diffuse;
                    var normal = LoadNormal(key + "_Normal");
                    if (normal != null)
                    {
                        material.SetTexture("_BumpMap", normal);
                        material.SetFloat("_BumpScale", 1f);
                        material.EnableKeyword("_NORMALMAP");
                    }
                    material.SetFloat("_Smoothness", key.Contains("Eye") || key.Contains("Cornea") ? 0.85f : key.Contains("Skin") ? 0.32f : 0.15f);
                    material.SetFloat("_Metallic", 0f);

                    if (key == "Std_Eyelash")
                    {
                        var rgba = Load(key + "_RGBA");
                        if (rgba != null) { material.SetTexture("_BaseMap", rgba); material.mainTexture = rgba; }
                        material.SetFloat("_AlphaClip", 1f);
                        material.SetFloat("_Cutoff", 0.35f);
                        material.EnableKeyword("_ALPHATEST_ON");
                        material.SetFloat("_Cull", (float)CullMode.Off);
                        material.renderQueue = (int)RenderQueue.AlphaTest;
                    }
                    else if (key.StartsWith("Std_Cornea"))
                    {
                        material.SetTexture("_BaseMap", null);
                        material.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.06f));
                        MakeTransparent(material);
                    }
                    EditorUtility.SetDirty(material);
                    fixedCount++;
                }
                AssetDatabase.SaveAssets();
                Debug.Log($"[CharacterMaterialFixer] Relinked {fixedCount} NL_Neighbor materials.");
                if (Application.isBatchMode) EditorApplication.Exit(fixedCount > 0 ? 0 : 1);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        /// <summary>"NL_Neighbor_CC_Base_Body_2_Std_Skin_Body_URP" → "Std_Skin_Body"; "NL_Neighbor_Shirt_0_Shirt_URP" → "Shirt".</summary>
        private static string KeyOf(string materialName)
        {
            string trimmed = materialName.Substring("NL_Neighbor_".Length);
            trimmed = trimmed.Substring(0, trimmed.Length - "_URP".Length);
            var parts = trimmed.Split('_');
            int index = Array.FindLastIndex(parts, p => p.All(char.IsDigit));
            return string.Join("_", parts.Skip(index + 1));
        }

        private static Texture2D Load(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureDir}/{name}.png");

        private static Texture2D LoadNormal(string name)
        {
            string path = $"{TextureDir}/{name}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return null;
            if (importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void MakeTransparent(Material material)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
    }
}
#endif
