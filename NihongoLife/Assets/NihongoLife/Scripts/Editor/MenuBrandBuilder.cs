#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// The main menu's brand, authored in 01_MainMenu like the rest of the menu: the real logo in place of the typed title
    /// and its kana line.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.MenuBrandBuilder.Build
    /// </summary>
    public static class MenuBrandBuilder
    {
        private const string ScenePath = "Assets/NihongoLife/Scenes/01_MainMenu.unity";
        private const string LogoPath = "Assets/NihongoLife/Resources/Branding/nihongolife_logo_light.png";
        private const string LogoName = "BrandLogo";

        public static void Build()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();
                var title = all.First(t => t.name == "TitleText");
                var kana = all.FirstOrDefault(t => t.parent == title.parent && t != title && t.GetComponent<TMP_Text>() != null && t.GetComponent<TMP_Text>().text.Contains("日本語"));
                var logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
                if (logoTexture == null) throw new InvalidOperationException("Missing " + LogoPath + " (Tools/release/web_branding.py makes it)");

                foreach (var old in all.Where(t => t != null && t.name == LogoName).ToList()) UnityEngine.Object.DestroyImmediate(old.gameObject);

                var logo = new GameObject(LogoName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(LayoutElement));
                var rect = (RectTransform)logo.transform;
                rect.SetParent(title.parent, false);
                rect.SetSiblingIndex(title.GetSiblingIndex());
                logo.GetComponent<RawImage>().texture = logoTexture;
                logo.GetComponent<RawImage>().raycastTarget = false;
                var size = logo.GetComponent<LayoutElement>();
                size.preferredWidth = size.minWidth = 324f;
                size.preferredHeight = size.minHeight = 90f;
                rect.anchorMin = title.GetComponent<RectTransform>().anchorMin;
                rect.anchorMax = title.GetComponent<RectTransform>().anchorMin;
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(324f, 90f);
                rect.anchoredPosition = title.GetComponent<RectTransform>().anchoredPosition;
                title.gameObject.SetActive(false);
                if (kana != null) kana.gameObject.SetActive(false);
                Debug.Log($"[MenuBrandBuilder] logo under {title.parent.name}; kana line {(kana != null ? kana.name : "not found")}; siblings: {string.Join(", ", Enumerable.Range(0, title.parent.childCount).Select(i => title.parent.GetChild(i).name))}");

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + ScenePath);
                AssetDatabase.SaveAssets();
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
