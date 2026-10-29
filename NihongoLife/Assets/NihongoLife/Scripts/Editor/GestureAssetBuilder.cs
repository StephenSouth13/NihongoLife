#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Creates Resources/Animation/NL_GestureProp.mat, the lit material every gesture prop (watering can, broom, keyboard ...)
    /// copies and tints at runtime. Having it as an asset in Resources keeps the shader in the WebGL build.
    /// Run: Unity -batchmode -quit -executeMethod NihongoLife.EditorTools.GestureAssetBuilder.Build
    /// </summary>
    public static class GestureAssetBuilder
    {
        private const string Folder = "Assets/NihongoLife/Resources/Animation/";

        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new System.Exception("URP Lit shader not found");
            string path = Folder + "NL_GestureProp.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader) { name = "NL_GestureProp" }; AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            material.SetFloat("_Smoothness", 0.25f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GestureAssetBuilder] wrote " + path);
        }
    }
}
#endif