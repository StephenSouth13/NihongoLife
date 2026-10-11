#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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

        // ---- Stationery (Hibari Mart ぶんぼうぐ) -------------------------------------------------------------------
        private const string Classroom = "Assets/ThirdParty/StylooClassroomAssetPack GLTF & FBX/StylooClassroomAssetPack GLTF & FBX/classroom/FBX/";
        private const string Office = "Assets/ThirdParty/StylooClassroomAssetPack GLTF & FBX/StylooClassroomAssetPack GLTF & FBX/principal office/FBX/";
        private static readonly Vector3 StationeryView = new(34f, -32f, 0f);

        /// <summary>Every stationery-like model in the project, one picture each, for choosing the product models by eye.
        /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.ItemIconRenderer.PreviewStationery -iconOut &lt;dir&gt;</summary>
        public static void PreviewStationery()
        {
            Batch(() =>
            {
                string outDir = Argument("-iconOut") ?? "Temp/StationeryPreview";
                var pattern = new System.Text.RegularExpressions.Regex("book|postit|pen|pencil|marker|scissors|compass|chalk|notebook|binder", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var models = Directory.GetFiles(Classroom, "*.fbx").Concat(Directory.GetFiles(Office, "*.fbx"))
                    .Select(p => p.Replace('\\', '/'))
                    .Where(p => pattern.IsMatch(Path.GetFileName(p)))
                    .Append("Assets/ThirdParty/Kenney/kenney_furniture-kit/Models/FBX format/books.fbx")
                    .ToArray();
                using var studio = new Studio(StationeryView);
                foreach (var path in models)
                {
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (source == null) { Debug.LogWarning("[ItemIconRenderer] Missing model " + path); continue; }
                    studio.Shoot(new[] { (source, Vector3.zero, Vector3.zero) }, Path.Combine(outDir, Path.GetFileNameWithoutExtension(path) + ".png"));
                }
                Debug.Log($"[ItemIconRenderer] Previewed {models.Length} stationery models into {outDir}.");
            });
        }

        // One picture per product, built only from project models, all lying flat as on a shop shelf.
        // note_paper_5 (loose-leaf sheets) has no model in the project; it keeps its 紙 glyph until one is added.
        private static readonly (string id, (string model, Vector3 offset, Vector3 euler)[] parts)[] Stationery =
        {
            ("note_paper_12", new[] { (Office + "PRINCIPALOFFICEnotebook.fbx", Vector3.zero, Vector3.zero) }),
            ("note_paper_30", new[] { (Classroom + "book__001.fbx", Vector3.zero, new Vector3(90f, 0f, 0f)) }),
        };

        /// <summary>The stationery products with one camera, light and framing (each model fills the same frame),
        /// into Resources/Items/&lt;itemId&gt;.png. -iconOut &lt;dir&gt; writes there instead, without importing.
        /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.ItemIconRenderer.RenderStationery</summary>
        public static void RenderStationery()
        {
            Batch(() =>
            {
                var shots = new List<(string id, (GameObject, Vector3, Vector3)[] parts)>();
                foreach (var (id, parts) in Stationery)
                {
                    var loaded = parts.Select(p => (AssetDatabase.LoadAssetAtPath<GameObject>(p.model), p.offset, p.euler)).ToArray();
                    if (loaded.Any(p => p.Item1 == null)) { Debug.LogWarning("[ItemIconRenderer] Missing model for " + id); continue; }
                    shots.Add((id, loaded));
                }
                string outDir = Argument("-iconOut") ?? OutDir;
                using var studio = new Studio(StationeryView);
                foreach (var (id, parts) in shots) studio.Shoot(parts, Path.Combine(outDir, id + ".png"));
                if (outDir == OutDir)
                {
                    AssetDatabase.Refresh();
                    foreach (var (id, _) in shots) ImportAsIcon(OutDir + "/" + id + ".png");
                }
                Debug.Log($"[ItemIconRenderer] Rendered {shots.Count} stationery pictures into {outDir}.");
            });
        }

        private static void Batch(Action body)
        {
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                body();
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        private static void ImportAsIcon(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 256;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        /// <summary>The food icons' camera and key/fill/rim light, keeping each model's own materials (a non-URP
        /// shader is swapped for URP Lit with the same colour and texture).</summary>
        private sealed class Studio : IDisposable
        {
            private readonly Camera _camera;
            private readonly Vector3 _view;
            private readonly RenderTexture _target = new(Size, Size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            private readonly Texture2D _readback = new(Size, Size, TextureFormat.RGBA32, false);
            private readonly Dictionary<Material, Material> _converted = new();
            private readonly Dictionary<string, Material> _paletteMaterials = new();
            private Texture2D _palette;

            public Studio(Vector3 view)
            {
                _view = view;
                _camera = new GameObject("IconCamera").AddComponent<Camera>();
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                _camera.fieldOfView = 24f;
                _camera.nearClipPlane = 0.01f;
                _camera.farClipPlane = 100f;
                Light(new Vector3(45f, -35f, 0f), 1.25f, new Color(1f, 0.97f, 0.9f));
                Light(new Vector3(20f, 140f, 0f), 0.45f, new Color(0.75f, 0.85f, 1f));
                Light(new Vector3(-10f, 200f, 0f), 0.6f, Color.white);
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.58f);
            }

            private GameObject Build((GameObject model, Vector3 offset, Vector3 euler)[] parts)
            {
                var root = new GameObject("IconSubject");
                foreach (var (model, offset, euler) in parts)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    instance.transform.SetParent(root.transform, false);
                    instance.transform.localPosition = offset;
                    instance.transform.localRotation = Quaternion.Euler(euler) * instance.transform.localRotation;
                    var palette = StylooPalette(AssetDatabase.GetAssetPath(model));
                    foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
                        r.sharedMaterials = r.sharedMaterials.Select(m => palette != null ? Palette(m, palette) : Urp(m)).ToArray();
                }
                root.transform.position -= BoundsOf(root).center;
                return root;
            }

            /// <summary>Styloo FBX files lose their only texture (the shared Imphenzia gradient palette the UVs point into);
            /// the pack's own GLB of the same model still embeds it, so it is read from there.</summary>
            private Texture2D StylooPalette(string fbxPath)
            {
                if (!fbxPath.Contains("Styloo")) return null;
                string glb = fbxPath.Replace("/FBX/", "/GLTF/").Replace(".fbx", ".glb");
                if (_palette != null || !File.Exists(glb)) return _palette;
                var bytes = File.ReadAllBytes(glb);
                int jsonLength = BitConverter.ToInt32(bytes, 12);
                var gltf = JsonUtility.FromJson<Gltf>(System.Text.Encoding.UTF8.GetString(bytes, 20, jsonLength));
                if (gltf.images == null || gltf.images.Length == 0) return null;
                var view = gltf.bufferViews[gltf.images[0].bufferView];
                int bin = 20 + jsonLength + 8; // BIN chunk data after its length + type words
                var image = new byte[view.byteLength];
                Array.Copy(bytes, bin + view.byteOffset, image, 0, view.byteLength);
                _palette = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = "ImphenziaPalette", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                _palette.LoadImage(image);
                return _palette;
            }

            private Material Palette(Material source, Texture2D palette)
            {
                string key = source != null && System.Text.RegularExpressions.Regex.IsMatch(source.name, "fer|metal", System.Text.RegularExpressions.RegexOptions.IgnoreCase) ? "metal" : "normal";
                if (_paletteMaterials.TryGetValue(key, out var done)) return done;
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetTexture("_BaseMap", palette);
                material.SetFloat("_Smoothness", key == "metal" ? 0.6f : 0.3f);
                material.SetFloat("_Metallic", key == "metal" ? 0.8f : 0f);
                material.SetFloat("_Cull", 0f); // the pack's materials are double-sided (single-sheet paper)
                material.doubleSidedGI = true;
                return _paletteMaterials[key] = material;
            }

            [Serializable] private sealed class Gltf { public GltfImage[] images; public GltfView[] bufferViews; }
            [Serializable] private sealed class GltfImage { public int bufferView; }
            [Serializable] private sealed class GltfView { public int byteOffset, byteLength; }

            /// <summary>Frames the subject's bounding sphere, so every picture has the same margin.</summary>
            public void Shoot((GameObject, Vector3, Vector3)[] parts, string file)
            {
                var root = Build(parts);
                // Normalise to a unit sphere so tiny FBX scales never fall inside the near clip plane.
                float radius = BoundsOf(root).extents.magnitude;
                root.transform.localScale = Vector3.one / radius;
                root.transform.position /= radius;
                Bounds b = BoundsOf(root);
                radius = 1f;
                Vector3 dir = Quaternion.Euler(_view) * Vector3.back;
                float distance = radius / Mathf.Sin(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.02f;
                _camera.transform.position = b.center + dir * distance;
                _camera.transform.LookAt(b.center);
                _camera.targetTexture = _target;
                _camera.Render();
                RenderTexture.active = _target;
                _readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                _readback.Apply();
                RenderTexture.active = null;
                _camera.targetTexture = null;
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.WriteAllBytes(file, _readback.EncodeToPNG());
                Object.DestroyImmediate(root);
            }

            private Material Urp(Material source)
            {
                if (source == null || source.shader.name.StartsWith("Universal Render Pipeline")) return source;
                if (_converted.TryGetValue(source, out var done)) return done;
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (source.HasProperty("_Color")) material.SetColor("_BaseColor", source.GetColor("_Color"));
                if (source.HasProperty("_MainTex") && source.mainTexture != null) material.SetTexture("_BaseMap", source.mainTexture);
                material.SetFloat("_Smoothness", 0.3f);
                return _converted[source] = material;
            }

            public void Dispose()
            {
                Object.DestroyImmediate(_readback);
                _target.Release();
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
