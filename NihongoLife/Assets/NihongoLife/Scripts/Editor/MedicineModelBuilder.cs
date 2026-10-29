#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using NihongoLife.Shop;
using Object = UnityEngine.Object;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Builds the low-poly 3D products of the Hibari Mart medicine shelf (one prefab per medicine id in
    /// Resources/Health/illnesses.json) and a ready-to-place medicine shelf prefab that carries the
    /// <see cref="KonbiniShelf"/> trigger for <see cref="KonbiniSection.Medicine"/>. Primitives and flat colours
    /// only (no third-party art, no brand logos); nothing is written into any scene.
    /// Run: Unity -batchmode -quit -executeMethod NihongoLife.EditorTools.MedicineModelBuilder.Build
    /// Proof pictures: ... -executeMethod NihongoLife.EditorTools.MedicineModelBuilder.Preview [-iconOut dir]
    /// Output: Assets/NihongoLife/Generated/Konbini/Medicine/Med_&lt;id&gt;.prefab + Med_Shelf.prefab (+ Materials).
    /// Products stand on y = 0 with their front toward +Z; sizes are real-world metres.
    /// </summary>
    public static class MedicineModelBuilder
    {
        public const string Dir = "Assets/NihongoLife/Generated/Konbini/Medicine";
        public const string MatDir = Dir + "/Materials";
        public const string ShelfPath = Dir + "/Med_Shelf.prefab";
        private const string IconDir = "Assets/NihongoLife/Resources/Items";

        public static string PrefabPath(string id) => Dir + "/Med_" + id + ".prefab";

        private static readonly Dictionary<string, Action<Transform>> Products = new()
        {
            ["kazegusuri"] = Kazegusuri, ["itamidome"] = Itamidome, ["shippu"] = Shippu, ["igusuri"] = Igusuri,
            ["allergy_med"] = AllergyMed, ["ors"] = Ors, ["mask"] = Mask,
        };

        // Width (m) of one product on the shelf, and the shelf rows (shelf boards at the konbini gondola heights).
        private static readonly Dictionary<string, float> Widths = new()
        {
            ["kazegusuri"] = 0.075f, ["itamidome"] = 0.07f, ["shippu"] = 0.10f, ["igusuri"] = 0.05f,
            ["allergy_med"] = 0.075f, ["ors"] = 0.056f, ["mask"] = 0.09f,
        };
        private static readonly (float level, (string id, int count)[] items)[] Rows =
        {
            (1.59f, new[] { ("kazegusuri", 6), ("itamidome", 6) }),
            (1.14f, new[] { ("allergy_med", 5), ("shippu", 5) }),
            (0.64f, new[] { ("igusuri", 7), ("ors", 7) }),
            (0.20f, new[] { ("mask", 8) }),
        };
        private const float ShelfWidth = 1.4f, ShelfDepth = 0.45f;

        public static void Build()
        {
            Batch(() =>
            {
                var ids = NihongoLife.Health.IllnessCatalog.Load().medicines.Select(m => m.id).ToArray();
                var missing = ids.Where(id => !Products.ContainsKey(id)).ToArray();
                if (missing.Length > 0) throw new InvalidOperationException("No 3D recipe for medicine ids: " + string.Join(", ", missing));
                EnsureFolder(Dir);
                EnsureFolder(MatDir);
                ImportIcons(ids);
                foreach (string id in ids)
                {
                    var root = new GameObject("Med_" + id);
                    Products[id](root.transform);
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(id));
                    Object.DestroyImmediate(root);
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                BuildShelf();
                AssetDatabase.SaveAssets();
                Debug.Log($"[MedicineModelBuilder] Built {ids.Length} medicine prefabs and the shelf in {Dir}.");
            });
        }

        // ---- Products ------------------------------------------------------------------------------------------
        private static readonly Color White = new(0.97f, 0.97f, 0.95f), Teal = new(0.16f, 0.66f, 0.67f), Red = new(0.92f, 0.37f, 0.43f),
            Orange = new(0.97f, 0.58f, 0.22f), Yellow = new(0.98f, 0.82f, 0.38f), Blue = new(0.18f, 0.55f, 0.80f), Purple = new(0.58f, 0.40f, 0.78f),
            Lavender = new(0.90f, 0.84f, 0.95f), Pink = new(0.96f, 0.67f, 0.78f), Brown = new(0.43f, 0.24f, 0.11f), Green = new(0.17f, 0.62f, 0.36f),
            LightBlue = new(0.68f, 0.87f, 0.96f), Grey = new(0.78f, 0.80f, 0.83f);

        private static void Kazegusuri(Transform r)
        {
            Carton(r, 0.075f, 0.115f, 0.04f, White, Teal, 0.036f);
            Disc(r, "Dot", new Vector3(-0.016f, 0.055f, 0.0206f), 0.022f, Red);
            Face(r, "Line_A", new Vector3(0.012f, 0.060f, 0.0206f), new Vector3(0.030f, 0.003f, 0.001f), Teal);
            Face(r, "Line_B", new Vector3(0.008f, 0.050f, 0.0206f), new Vector3(0.022f, 0.002f, 0.001f), Grey);
        }

        private static void Itamidome(Transform r)
        {
            Carton(r, 0.07f, 0.105f, 0.035f, White, Orange, 0.032f);
            Face(r, "Bolt_A", new Vector3(0.002f, 0.050f, 0.0181f), new Vector3(0.012f, 0.026f, 0.001f), Yellow, 12f);
            Face(r, "Bolt_B", new Vector3(-0.004f, 0.040f, 0.0183f), new Vector3(0.012f, 0.020f, 0.001f), Yellow, 12f);
            Face(r, "Line", new Vector3(0f, 0.022f, 0.0181f), new Vector3(0.044f, 0.002f, 0.001f), Grey);
        }

        private static void Shippu(Transform r)
        {
            Part(r, PrimitiveType.Cube, "Pouch", new Vector3(0, 0.07f, 0), new Vector3(0.10f, 0.14f, 0.012f), MatC(Blue));
            Part(r, PrimitiveType.Cube, "Seal", new Vector3(0, 0.134f, 0.0062f), new Vector3(0.10f, 0.012f, 0.001f), MatC(new Color(0.45f, 0.75f, 0.9f)));
            Disc(r, "Badge", new Vector3(0, 0.075f, 0.0066f), 0.042f, White);
            Face(r, "Cool_A", new Vector3(0, 0.075f, 0.0074f), new Vector3(0.006f, 0.026f, 0.001f), Blue);
            Face(r, "Cool_B", new Vector3(0, 0.075f, 0.0074f), new Vector3(0.026f, 0.006f, 0.001f), Blue);
            Face(r, "Patch", new Vector3(0.016f, 0.028f, 0.0068f), new Vector3(0.05f, 0.035f, 0.002f), new Color(0.78f, 0.93f, 0.85f), -6f);
        }

        private static void Igusuri(Transform r)
        {
            Cyl(r, "Bottle", new Vector3(0, 0.040f, 0), 0.050f, 0.080f, Brown, 0.55f);
            Part(r, PrimitiveType.Sphere, "Shoulder", new Vector3(0, 0.080f, 0), new Vector3(0.050f, 0.024f, 0.050f), MatC(Brown, 0.55f));
            Cyl(r, "Neck", new Vector3(0, 0.094f, 0), 0.026f, 0.020f, Brown, 0.55f);
            Cyl(r, "Cap", new Vector3(0, 0.110f, 0), 0.032f, 0.016f, White);
            Cyl(r, "Label", new Vector3(0, 0.036f, 0), 0.0508f, 0.040f, new Color(0.99f, 0.98f, 0.94f));
            Cyl(r, "LabelBand", new Vector3(0, 0.050f, 0), 0.0512f, 0.012f, Green);
            Disc(r, "Stomach", new Vector3(0, 0.032f, 0.0257f), 0.016f, new Color(0.96f, 0.60f, 0.55f));
        }

        private static void AllergyMed(Transform r)
        {
            Carton(r, 0.075f, 0.115f, 0.04f, Lavender, Purple, 0.034f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                Disc(r, "Petal_" + i, new Vector3(Mathf.Cos(a) * 0.011f, 0.056f + Mathf.Sin(a) * 0.011f, 0.0206f), 0.013f, Pink);
            }
            Disc(r, "Core", new Vector3(0, 0.056f, 0.0210f), 0.010f, Yellow);
            Face(r, "Line", new Vector3(0, 0.020f, 0.0206f), new Vector3(0.046f, 0.002f, 0.001f), Purple);
        }

        private static void Ors(Transform r)
        {
            Cyl(r, "Bottle", new Vector3(0, 0.050f, 0), 0.055f, 0.100f, LightBlue, 0.75f);
            Part(r, PrimitiveType.Sphere, "Shoulder", new Vector3(0, 0.100f, 0), new Vector3(0.055f, 0.034f, 0.055f), MatC(LightBlue, 0.75f));
            Cyl(r, "Neck", new Vector3(0, 0.118f, 0), 0.026f, 0.016f, LightBlue, 0.75f);
            Cyl(r, "Cap", new Vector3(0, 0.131f, 0), 0.030f, 0.014f, White);
            Cyl(r, "Label", new Vector3(0, 0.048f, 0), 0.0556f, 0.044f, new Color(0.98f, 0.99f, 1f));
            Cyl(r, "LabelBand", new Vector3(0, 0.066f, 0), 0.0560f, 0.012f, Blue);
            Disc(r, "Drop", new Vector3(0, 0.044f, 0.0281f), 0.016f, Blue);
        }

        private static void Mask(Transform r)
        {
            // A flat retail pack standing on its long edge: white pouch, blue band, a pleated mask picture.
            Part(r, PrimitiveType.Cube, "Pack", new Vector3(0, 0.080f, 0), new Vector3(0.090f, 0.160f, 0.016f), MatC(White));
            Part(r, PrimitiveType.Cube, "Band", new Vector3(0, 0.150f, 0.0082f), new Vector3(0.090f, 0.020f, 0.001f), MatC(Blue));
            for (int i = 0; i < 3; i++)
                Face(r, "Pleat_" + i, new Vector3(0, 0.092f - i * 0.016f, 0.0084f), new Vector3(0.062f, 0.012f, 0.001f), i % 2 == 0 ? new Color(0.88f, 0.93f, 0.96f) : new Color(0.80f, 0.88f, 0.93f));
            Face(r, "Loop_L", new Vector3(-0.036f, 0.076f, 0.0084f), new Vector3(0.004f, 0.050f, 0.001f), Grey);
            Face(r, "Loop_R", new Vector3(0.036f, 0.076f, 0.0084f), new Vector3(0.004f, 0.050f, 0.001f), Grey);
        }

        // ---- The shelf -----------------------------------------------------------------------------------------
        private static void BuildShelf()
        {
            var shelfWhite = MatC(new Color(0.94f, 0.95f, 0.95f)); var shelfGreen = MatC(Green); var white = MatC(White);
            var root = new GameObject("Med_Shelf");
            float w = ShelfWidth, d = ShelfDepth;
            Part(root.transform, PrimitiveType.Cube, "Plinth", new Vector3(0, 0.08f, 0), new Vector3(w, 0.16f, d), shelfWhite);
            Part(root.transform, PrimitiveType.Cube, "BackPanel", new Vector3(0, 1.0f, -d / 2f + 0.02f), new Vector3(w, 2.0f, 0.04f), shelfWhite);
            Part(root.transform, PrimitiveType.Cube, "Side_L", new Vector3(-w / 2f + 0.02f, 1.0f, 0), new Vector3(0.04f, 2.0f, d), shelfWhite);
            Part(root.transform, PrimitiveType.Cube, "Side_R", new Vector3(w / 2f - 0.02f, 1.0f, 0), new Vector3(0.04f, 2.0f, d), shelfWhite);
            Part(root.transform, PrimitiveType.Cube, "Header", new Vector3(0, 2.08f, 0.02f), new Vector3(w, 0.16f, d - 0.04f), shelfGreen);
            Part(root.transform, PrimitiveType.Cube, "Sign_Plus_A", new Vector3(0, 2.08f, d / 2f - 0.0f), new Vector3(0.025f, 0.10f, 0.004f), white);
            Part(root.transform, PrimitiveType.Cube, "Sign_Plus_B", new Vector3(0, 2.08f, d / 2f - 0.0f), new Vector3(0.10f, 0.025f, 0.004f), white);
            foreach (var (level, items) in Rows)
            {
                Part(root.transform, PrimitiveType.Cube, "Board_" + level.ToString("0.00"), new Vector3(0, level - 0.02f, 0), new Vector3(w - 0.08f, 0.04f, d - 0.04f), shelfWhite);
                Part(root.transform, PrimitiveType.Cube, "Rail_" + level.ToString("0.00"), new Vector3(0, level - 0.05f, d / 2f - 0.01f), new Vector3(w - 0.08f, 0.05f, 0.02f), shelfGreen);
                float total = items.Sum(it => it.count * (Widths[it.id] + 0.01f));
                float x = -total / 2f;
                foreach (var (id, count) in items)
                    for (int i = 0; i < count; i++)
                    {
                        float step = Widths[id] + 0.01f;
                        var source = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(id));
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                        instance.transform.SetParent(root.transform, false);
                        instance.transform.localPosition = new Vector3(x + step / 2f, level, 0.02f);
                        x += step;
                    }
            }
            // The F-to-browse trigger for the medicine section (same component the other shelves use).
            var trigger = new GameObject("Shelf_Medicine") { layer = ZoneBuildKit.InteractableLayer }; // F finds it on the interactable layer
            trigger.transform.SetParent(root.transform, false);
            trigger.transform.localPosition = new Vector3(0, 1.0f, d / 2f);
            var box = trigger.AddComponent<BoxCollider>();
            box.size = new Vector3(w, 2.0f, 0.9f);
            box.isTrigger = true;
            trigger.AddComponent<KonbiniShelf>().Configure(KonbiniSection.Medicine);
            PrefabUtility.SaveAsPrefabAsset(root, ShelfPath);
            Object.DestroyImmediate(root);
        }

        // ---- Proof pictures ------------------------------------------------------------------------------------
        public static void Preview()
        {
            Batch(() =>
            {
                string outDir = Argument("-iconOut") ?? "../Bao_Cao/health/medicine_3d";
                Directory.CreateDirectory(outDir);
                var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.93f, 0.90f, 0.85f, 1f);
                camera.fieldOfView = 24f; camera.nearClipPlane = 0.01f; camera.farClipPlane = 100f;
                AddLight(new Vector3(45f, -35f, 0f), 1.25f, new Color(1f, 0.97f, 0.9f));
                AddLight(new Vector3(20f, 140f, 0f), 0.45f, new Color(0.75f, 0.85f, 1f));
                AddLight(new Vector3(-10f, 200f, 0f), 0.6f, Color.white);
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.58f);
                var ids = NihongoLife.Health.IllnessCatalog.Load().medicines.Select(m => m.id).ToList();
                var jobs = ids.Select(id => (PrefabPath(id), "medicine_3d_" + id)).Append((ShelfPath, "medicine_3d_shelf")).ToArray();
                var target = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
                var read = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
                foreach (var (path, name) in jobs)
                {
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (source == null) { Debug.LogWarning("[MedicineModelBuilder] Missing prefab " + path); continue; }
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                    Bounds b = BoundsOf(instance);
                    float radius = b.extents.magnitude;
                    Vector3 dir = Quaternion.Euler(-22f, 30f, 0f) * Vector3.forward;
                    float distance = radius / Mathf.Sin(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.04f;
                    camera.transform.position = b.center + dir * distance;
                    camera.transform.LookAt(b.center);
                    camera.targetTexture = target;
                    camera.Render();
                    RenderTexture.active = target;
                    read.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
                    read.Apply();
                    RenderTexture.active = null;
                    camera.targetTexture = null;
                    File.WriteAllBytes(Path.Combine(outDir, name + ".png"), read.EncodeToPNG());
                    Object.DestroyImmediate(instance);
                }
                Debug.Log($"[MedicineModelBuilder] Preview pictures in {Path.GetFullPath(outDir)}.");
            });
        }

        // ---- Helpers -------------------------------------------------------------------------------------------
        private static void Carton(Transform r, float w, float h, float d, Color body, Color band, float bandH)
        {
            Part(r, PrimitiveType.Cube, "Body", new Vector3(0, h / 2f, 0), new Vector3(w, h, d), MatC(body));
            Part(r, PrimitiveType.Cube, "Band", new Vector3(0, h - bandH / 2f - 0.012f, 0), new Vector3(w + 0.0016f, bandH, d + 0.0016f), MatC(band));
        }

        private static GameObject Face(Transform r, string name, Vector3 pos, Vector3 size, Color c, float rollDegrees = 0f)
            => Part(r, PrimitiveType.Cube, name, pos, size, MatC(c), new Vector3(0, 0, rollDegrees));

        private static GameObject Disc(Transform r, string name, Vector3 pos, float diameter, Color c)
            => Part(r, PrimitiveType.Cylinder, name, pos, new Vector3(diameter, 0.0006f, diameter), MatC(c), new Vector3(90f, 0, 0));

        private static GameObject Cyl(Transform r, string name, Vector3 pos, float diameter, float height, Color c, float smooth = 0.35f)
            => Part(r, PrimitiveType.Cylinder, name, pos, new Vector3(diameter, height / 2f, diameter), MatC(c, smooth));

        private static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 size, Material material, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static Material MatC(Color c, float smooth = 0.35f)
        {
            string path = $"{MatDir}/Med_{ColorUtility.ToHtmlStringRGB(c)}_{(int)(smooth * 100)}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader not found.");
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void ImportIcons(IEnumerable<string> ids)
        {
            AssetDatabase.Refresh();
            foreach (string id in ids)
            {
                string path = IconDir + "/" + id + ".png";
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 512; // the icons are 512 px pictures: never import them smaller
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        private static void AddLight(Vector3 euler, float intensity, Color color)
        {
            var l = new GameObject("PreviewLight").AddComponent<Light>();
            l.type = LightType.Directional; l.intensity = intensity; l.color = color; l.transform.rotation = Quaternion.Euler(euler);
        }

        private static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
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
    }
}
#endif
