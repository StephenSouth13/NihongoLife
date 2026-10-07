#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NihongoLife.EditorTools
{
    /// <summary>Small shared helpers for the headless scene builders (materials, blocks, world text).</summary>
    public static class ZoneBuildKit
    {
        public const string FontPath = "Assets/NihongoLife/Fonts/NotoSansJP SDF.asset";
        public const int InteractableLayer = 6;

        public static Material Lit(string folder, string name, Color color, float smoothness, Color? emission = null, bool transparent = false)
        {
            EnsureFolder(folder);
            string path = folder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)RenderQueue.Transparent;
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material Unlit(string folder, string name, Color color)
        {
            EnsureFolder(folder);
            string path = folder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            int slash = folder.LastIndexOf('/');
            EnsureFolder(folder.Substring(0, slash));
            AssetDatabase.CreateFolder(folder.Substring(0, slash), folder.Substring(slash + 1));
        }

        public static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name).transform;
            go.SetParent(parent, false);
            return go;
        }

        public static GameObject Block(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collider, float yaw = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Cylinder(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>World-space text. TextMeshPro faces −Z by default: yaw 0 is read by a viewer looking +Z.</summary>
        public static TextMeshPro Text(Transform parent, string name, string value, Vector3 position, float yaw, float height, Color color, float width = 4f,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var text = go.AddComponent<TextMeshPro>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = height * 10f;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(width, height * 4f);
            return text;
        }

        public static GameObject Trigger(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.layer = InteractableLayer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            return go;
        }

        public static Light PointLight(Transform parent, string name, Vector3 position, float range, float intensity, Color color, LightShadows shadows = LightShadows.None)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = range;
            light.intensity = intensity;
            light.color = color;
            light.shadows = shadows;
            return light;
        }

        public static Bounds BoundsOf(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }
    }
}
#endif
