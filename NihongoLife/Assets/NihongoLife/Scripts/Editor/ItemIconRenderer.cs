#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Renders real pictures for the konbini products and food items from the Kenney food kit models
    /// (3/4 view, soft key/fill/rim light, transparent background) into Resources/Items/&lt;itemId&gt;.png,
    /// used by ItemIcons in the shop, the bag and the slides. Runs in a throw-away scene; nothing is saved
    /// into the game scenes.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.ItemIconRenderer.Render
    /// </summary>
    public static class ItemIconRenderer
    {
        private const string Food = "Assets/ThirdParty/Kenney/kenney_food-kit/Models/FBX format/";
        private const string OutDir = "Assets/NihongoLife/Resources/Items";
        private const int Size = 512;

        // Only items whose Kenney model reads as the real product. Onigiri, PET bottles, cartons, can coffee,
        // senbei, Pocky, melonpan, karaage, nikuman, the egg sandwich, tickets and prizes are illustrated
        // pictures in the same folder; this renderer never touches them.
        private static readonly (string id, string model)[] Map =
        {
            ("ice_cream", "popsicle"), ("oden", "skewer"), ("prize_snack", "candy-bar"),
        };

        public static void Render()
        {
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Directory.CreateDirectory(OutDir);
                var colormap = AssetDatabase.LoadAssetAtPath<Texture2D>(Food + "Textures/colormap.png");
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (colormap != null) { material.SetTexture("_BaseMap", colormap); material.mainTexture = colormap; }
                material.SetFloat("_Smoothness", 0.35f);

                var cameraObject = new GameObject("IconCamera");
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.fieldOfView = 24f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                Light(new Vector3(45f, -35f, 0f), 1.25f, new Color(1f, 0.97f, 0.9f));
                Light(new Vector3(20f, 140f, 0f), 0.45f, new Color(0.75f, 0.85f, 1f));
                Light(new Vector3(-10f, 200f, 0f), 0.6f, Color.white);
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.58f);

                var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
                var readback = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                int written = 0;
                foreach (var (id, model) in Map)
                {
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(Food + model + ".fbx");
                    if (source == null) { Debug.LogWarning("[ItemIconRenderer] Missing model " + model); continue; }
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                    foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
                        r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();
                    Bounds b = BoundsOf(instance);
                    instance.transform.position -= b.center;
                    b = BoundsOf(instance);
                    float radius = b.extents.magnitude;
                    Vector3 dir = Quaternion.Euler(24f, -32f, 0f) * Vector3.back;
                    float distance = radius / Mathf.Sin(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.02f;
                    camera.transform.position = b.center + dir * distance;
                    camera.transform.LookAt(b.center);

                    camera.targetTexture = target;
                    camera.Render();
                    RenderTexture.active = target;
                    readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                    readback.Apply();
                    RenderTexture.active = null;
                    camera.targetTexture = null;
                    File.WriteAllBytes(Path.Combine(OutDir, id + ".png"), readback.EncodeToPNG());
                    Object.DestroyImmediate(instance);
                    written++;
                }
                Object.DestroyImmediate(readback);
                target.Release();

                AssetDatabase.Refresh();
                foreach (var path in Directory.GetFiles(OutDir, "*.png"))
                {
                    var importer = AssetImporter.GetAtPath(path.Replace('\\', '/')) as TextureImporter;
                    if (importer == null) continue;
                    importer.textureType = TextureImporterType.Default;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.maxTextureSize = 256;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.SaveAndReimport();
                }
                Debug.Log($"[ItemIconRenderer] Rendered {written} item pictures into {OutDir}.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static void Light(Vector3 euler, float intensity, Color color)
        {
            var light = new GameObject("IconLight").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(euler);
            light.intensity = intensity;
            light.color = color;
            light.shadows = LightShadows.Soft;
        }

        private static Bounds BoundsOf(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
#endif
