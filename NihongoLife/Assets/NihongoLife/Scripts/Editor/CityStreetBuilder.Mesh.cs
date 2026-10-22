using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NihongoLife.EditorTools
{
    /// <summary>Partial of CityStreetBuilder: mesh batches, materials, and textures. The public entry point stays CityStreetBuilder.cs.</summary>
    public static partial class CityStreetBuilder
    {
        // ─────────────────────────── helpers ───────────────────────────

        private static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        private static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 scale, Material m)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = m;
            return go;
        }

        private static GameObject Model(string path, Transform parent, string name, Vector3 pos, float yaw, float length)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (src == null) return null;
            var holder = Group(parent, name);
            var m = (GameObject)PrefabUtility.InstantiatePrefab(src, holder);
            PrefabUtility.UnpackPrefabInstance(m, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            IslandBuilder.ConvertMaterials(m);
            var b = RendererBounds(m.transform);
            m.transform.localScale *= length / Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.z));
            b = RendererBounds(m.transform);
            m.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
            holder.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            b = RendererBounds(holder);
            var box = holder.gameObject.AddComponent<BoxCollider>();
            box.center = holder.InverseTransformPoint(b.center);
            box.size = new Vector3(length, b.size.y, 0.6f);
            return holder.gameObject;
        }

        private static void Text(Transform parent, string name, string text, Vector3 pos, float yaw, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(90f, yaw, 0f));
            var tmp = go.GetComponent<TextMeshPro>();
            if (_font != null) tmp.font = _font;
            tmp.text = text; tmp.fontSize = size * 10f; tmp.color = color; tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            ((RectTransform)go.transform).sizeDelta = new Vector2(6f, 2.5f);
            go.transform.localScale = new Vector3(1f, 1.6f, 1f); // painted road text is stretched along the lane
        }

        private static void Sign(Transform parent, string name, string ja, string en, Vector3 pos, float yaw)
        {
            var root = Group(parent, name);
            root.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            Prim(PrimitiveType.Cylinder, root, "Post", new Vector3(0f, 1f, 0f), new Vector3(0.08f, 1f, 0.08f), _signalBody);
            var board = Prim(PrimitiveType.Cube, root, "Board", new Vector3(0f, 2.1f, 0f), new Vector3(1.8f, 0.6f, 0.06f), _concrete);
            Object.DestroyImmediate(board.GetComponent<Collider>());
            foreach (float side in new[] { -1f, 1f })
            {
                var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshPro));
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(0f, 2.1f, side * 0.035f);
                go.transform.localRotation = Quaternion.Euler(0f, side > 0 ? 180f : 0f, 0f);
                var tmp = go.GetComponent<TextMeshPro>();
                if (_font != null) tmp.font = _font;
                tmp.text = $"{ja}\n<size=55%>{en}</size>"; tmp.fontSize = 3f; tmp.color = new Color(0.15f, 0.2f, 0.25f); tmp.alignment = TextAlignmentOptions.Center;
                ((RectTransform)go.transform).sizeDelta = new Vector2(1.7f, 0.55f);
            }
        }

        private static Bounds RendererBounds(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(t.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        private static Rect ToRect(Bounds b, float pad) => Rect.MinMaxRect(b.min.x - pad, b.min.z - pad, b.max.x + pad, b.max.z + pad);

        private static void Emit(Transform parent, string name, Batch batch, Material material, int tile)
        {
            var mesh = batch.ToMesh(name);
            string path = $"{Gen}/Meshes/Tile{tile}_{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, true);
            go.transform.position = Vector3.zero;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            bool flat = material != _wall && material != _wallCap && material != _kerb;
            r.shadowCastingMode = flat ? ShadowCastingMode.Off : ShadowCastingMode.On;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
        }

        /// <summary>World-space mesh builder (UVs from world x/z or along walls, in metres / uvMetres).</summary>
        private sealed class Batch
        {
            private readonly List<Vector3> _v = new(), _n = new();
            private readonly List<Vector2> _uv = new();
            private readonly List<int> _t = new();
            private readonly float _scale;
            public Batch(float uvMetres) { _scale = 1f / uvMetres; }

            public void Quad(float x0, float x1, float z0, float z1, float y)
            {
                if (x1 <= x0 || z1 <= z0) return;
                Face(new Vector3(x0, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, y, z0), Vector3.up,
                    new Vector2(x0, z0), new Vector2(x0, z1), new Vector2(x1, z1), new Vector2(x1, z0));
            }

            /// <summary>A thin painted strip from a to b (both on the ground).</summary>
            public void Strip(Vector3 a, Vector3 b, float width)
            {
                var d = (b - a); d.y = 0f; d.Normalize();
                var n = new Vector3(-d.z, 0f, d.x) * width * 0.5f;
                Face(a - n, a + n, b + n, b - n, Vector3.up, new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));
            }

            public void Box(Vector3 min, Vector3 max) => Prism(new Vector2(min.x, min.z), new Vector2(max.x, min.z), new Vector2(max.x, max.z), new Vector2(min.x, max.z), min.y, max.y);

            /// <summary>An upright prism over the quad a-b-c-d (counter-clockwise from above) between y0 and y1.</summary>
            public void Prism(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float y0, float y1)
            {
                var p = new[] { a, b, c, d };
                Face(V(a, y1), V(d, y1), V(c, y1), V(b, y1), Vector3.up, a, d, c, b);
                for (int i = 0; i < 4; i++)
                {
                    var p0 = p[i]; var p1 = p[(i + 1) % 4];
                    var edge = p1 - p0;
                    var normal = new Vector3(edge.y, 0f, -edge.x).normalized;
                    float len = edge.magnitude;
                    float u0 = (p0.x + p0.y);
                    Face(V(p0, y0), V(p0, y1), V(p1, y1), V(p1, y0), normal,
                        new Vector2(u0, y0), new Vector2(u0, y1), new Vector2(u0 + len, y1), new Vector2(u0 + len, y0));
                }
            }

            private static Vector3 V(Vector2 p, float y) => new Vector3(p.x, y, p.y);

            private void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
            {
                int i = _v.Count;
                _v.AddRange(new[] { a, b, c, d });
                _n.AddRange(new[] { normal, normal, normal, normal });
                _uv.AddRange(new[] { ua * _scale, ub * _scale, uc * _scale, ud * _scale });
                // Wind so the face points along `normal`.
                var cross = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(cross, normal) >= 0f) _t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
                else _t.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            }

            public Mesh ToMesh(string name)
            {
                var m = new Mesh { name = name, indexFormat = _v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                m.SetVertices(_v); m.SetNormals(_n); m.SetUVs(0, _uv); m.SetTriangles(_t, 0);
                m.RecalculateBounds(); m.RecalculateTangents();
                return m;
            }
        }

        // ─────────────────────────── materials & textures ───────────────────────────

        private static void Materials()
        {
            _asphalt = Mat("Asphalt", Noise("asphalt", new Color(0.2f, 0.21f, 0.23f), 0.035f, 0.05f, new Color(0.42f, 0.42f, 0.43f), 1), Color.white, 0.18f);
            _laneAsphalt = Mat("LaneAsphalt", Noise("lane_asphalt", new Color(0.31f, 0.31f, 0.32f), 0.035f, 0.04f, new Color(0.5f, 0.5f, 0.5f), 2), Color.white, 0.12f);
            _paver = Mat("Paver", Pavers(), Color.white, 0.2f);
            _kerb = Mat("Kerb", Noise("kerb", new Color(0.78f, 0.77f, 0.74f), 0.03f, 0.01f, Color.white, 3), Color.white, 0.15f);
            _paint = Mat("RoadPaint", null, new Color(0.93f, 0.93f, 0.9f), 0.3f);
            _wall = Mat("BlockWall", Blocks(), Color.white, 0.1f);
            _wallCap = Mat("WallCap", null, new Color(0.5f, 0.5f, 0.5f), 0.1f);
            _gravel = Mat("Gravel", Noise("gravel", new Color(0.6f, 0.58f, 0.55f), 0.09f, 0.25f, new Color(0.82f, 0.8f, 0.76f), 4), Color.white, 0.05f);
            _grass = Mat("Grass", Noise("grass", new Color(0.34f, 0.52f, 0.26f), 0.08f, 0.12f, new Color(0.45f, 0.62f, 0.3f), 5), Color.white, 0.05f);
            _concrete = Mat("Concrete", Noise("concrete", new Color(0.86f, 0.85f, 0.82f), 0.03f, 0.01f, Color.white, 6), Color.white, 0.2f);
            _parking = Mat("ParkingAsphalt", Noise("parking_asphalt", new Color(0.24f, 0.25f, 0.27f), 0.03f, 0.04f, new Color(0.45f, 0.45f, 0.45f), 7), Color.white, 0.15f);
            _iron = Mat("Iron", null, new Color(0.18f, 0.18f, 0.19f), 0.5f);
            _signalBody = Mat("SignalBody", null, new Color(0.62f, 0.64f, 0.66f), 0.35f);
            _lampOff = Mat("LampOff", null, new Color(0.12f, 0.13f, 0.14f), 0.6f);
            _green = Lit("LampGreen", new Color(0.1f, 0.85f, 0.65f));          // Japanese "blue" signal
            _yellow = Lit("LampYellow", new Color(1f, 0.72f, 0.1f));
            _red = Lit("LampRed", new Color(1f, 0.15f, 0.12f));
            _walkGreen = Mat("WalkGreen", WalkTexture(new Color(0.15f, 0.95f, 0.7f), true), Color.white, 0f, true);
            _walkRed = Mat("WalkRed", WalkTexture(new Color(1f, 0.2f, 0.15f), false), Color.white, 0f, true);
            _mirrorPole = Mat("MirrorOrange", null, new Color(0.95f, 0.45f, 0.1f), 0.4f);
            _mirror = Mat("MirrorGlass", null, new Color(0.75f, 0.82f, 0.9f), 0.95f);
            _mirror.SetFloat("_Metallic", 0.9f);
            _vendBody = Mat("VendingBody", null, new Color(0.9f, 0.92f, 0.95f), 0.5f);
            _vendFront = Mat("VendingFront", VendingTexture(), Color.white, 0.6f, true);
        }

        private static Material Lit(string name, Color c)
        {
            var m = Mat(name, null, c, 0.6f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 2.2f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material Mat(string name, Texture2D tex, Color color, float smooth, bool emissive = false)
        {
            string path = $"{Gen}/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name }; AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", color);
            m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", 0f);
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetTexture("_EmissionMap", tex);
                m.SetColor("_EmissionColor", Color.white * 1.2f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Texture2D Save(string name, Texture2D tex)
        {
            string path = $"{Gen}/Textures/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            if (AssetImporter.GetAtPath(path) is TextureImporter imp)
            {
                imp.wrapMode = TextureWrapMode.Repeat; imp.anisoLevel = 4; imp.mipmapEnabled = true; imp.sRGBTexture = true;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Texture2D Noise(string name, Color baseColor, float variation, float speckle, Color speckleColor, int seed)
        {
            const int n = 512;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true);
            var rnd = new System.Random(seed);
            var px = new Color[n * n];
            float o = seed * 13.7f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // Tileable noise: sample on a torus.
                    float u = x / (float)n * Mathf.PI * 2f, v = y / (float)n * Mathf.PI * 2f;
                    float nz = Mathf.PerlinNoise(o + Mathf.Cos(u) * 3f + Mathf.Sin(v) * 1.7f, o + Mathf.Sin(u) * 3f + Mathf.Cos(v) * 2.3f)
                             + 0.5f * Mathf.PerlinNoise(o * 2 + Mathf.Cos(u) * 9f + Mathf.Sin(v) * 5f, o + Mathf.Sin(u) * 9f + Mathf.Cos(v) * 7f);
                    var c = baseColor * (1f + (nz - 0.75f) * variation * 2.5f);
                    double s = rnd.NextDouble();
                    if (s < speckle * 0.25) c = Color.Lerp(c, speckleColor, 0.55f);
                    else if (s < speckle * 0.4) c *= 0.82f;
                    c.a = 1f;
                    px[y * n + x] = c;
                }
            tex.SetPixels(px); tex.Apply();
            return Save(name, tex);
        }

        /// <summary>Interlocking pavers 20 × 10 cm (2 m per texture), warm greys, thin joints.</summary>
        private static Texture2D Pavers()
        {
            const int n = 512; const int bw = 51, bh = 26;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true);
            var px = new Color[n * n];
            var rnd = new System.Random(9);
            var shades = Enumerable.Range(0, 64).Select(_ => 0.68f + (float)rnd.NextDouble() * 0.12f).ToArray();
            for (int y = 0; y < n; y++)
            {
                int row = y / bh;
                int off = row % 2 == 0 ? 0 : bw / 2;
                for (int x = 0; x < n; x++)
                {
                    int col = (x + off) / bw;
                    bool joint = (y % bh) < 2 || ((x + off) % bw) < 2;
                    float s = shades[(row * 7 + col * 13) & 63];
                    var c = joint ? new Color(0.48f, 0.47f, 0.45f) : new Color(s, s * 0.97f, s * 0.92f);
                    float grain = Mathf.PerlinNoise(x * 0.11f, y * 0.11f) * 0.06f - 0.03f;
                    px[y * n + x] = new Color(c.r + grain, c.g + grain, c.b + grain, 1f);
                }
            }
            tex.SetPixels(px); tex.Apply();
            return Save("pavers", tex);
        }

        /// <summary>Concrete blocks 40 × 20 cm (1.2 m per texture), mortar joints.</summary>
        private static Texture2D Blocks()
        {
            const int n = 512;
            float perMetre = n / 1.2f;
            int bw = Mathf.RoundToInt(0.4f * perMetre), bh = Mathf.RoundToInt(0.2f * perMetre);
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true);
            var px = new Color[n * n];
            var rnd = new System.Random(11);
            for (int y = 0; y < n; y++)
            {
                int row = y / bh; int off = row % 2 == 0 ? 0 : bw / 2;
                for (int x = 0; x < n; x++)
                {
                    bool joint = (y % bh) < 3 || ((x + off) % bw) < 3;
                    float g = 0.74f + Mathf.PerlinNoise(x * 0.07f + row, y * 0.07f) * 0.08f + (float)rnd.NextDouble() * 0.03f;
                    var c = joint ? new Color(0.6f, 0.6f, 0.58f) : new Color(g, g, g * 0.97f);
                    px[y * n + x] = new Color(c.r, c.g, c.b, 1f);
                }
            }
            tex.SetPixels(px); tex.Apply();
            return Save("blocks", tex);
        }

        /// <summary>The lit front of a vending machine: three shelves of drinks, price tags, buttons.</summary>
        private static Texture2D VendingTexture()
        {
            const int w = 256, h = 480;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
            var px = Enumerable.Repeat(new Color(0.95f, 0.96f, 0.98f), w * h).ToArray();
            var cans = new[] { new Color(0.85f, 0.15f, 0.15f), new Color(0.15f, 0.45f, 0.85f), new Color(0.2f, 0.7f, 0.35f), new Color(0.95f, 0.75f, 0.15f), new Color(0.5f, 0.3f, 0.2f), new Color(0.9f, 0.9f, 0.9f) };
            for (int row = 0; row < 3; row++)
            {
                int y0 = h - 40 - row * 100;
                for (int i = 0; i < 6; i++)
                {
                    int x0 = 18 + i * 38;
                    var c = cans[(i + row * 2) % cans.Length];
                    for (int y = y0 - 62; y < y0; y++)
                        for (int x = x0; x < x0 + 26; x++)
                        {
                            float edge = Mathf.Abs((x - x0 - 13) / 13f);
                            px[y * w + x] = c * (1f - edge * edge * 0.35f);
                        }
                    for (int y = y0 - 80; y < y0 - 68; y++) for (int x = x0 - 2; x < x0 + 28; x++) px[y * w + x] = new Color(0.1f, 0.1f, 0.12f); // price strip
                    for (int y = y0 - 92; y < y0 - 84; y++) for (int x = x0 + 9; x < x0 + 17; x++) px[y * w + x] = new Color(0.2f, 0.9f, 0.4f);  // button
                }
            }
            for (int y = 0; y < 150; y++) for (int x = 0; x < w; x++) px[y * w + x] = new Color(0.25f, 0.27f, 0.3f);                 // lower panel
            tex.SetPixels(px); tex.Apply();
            return Save("vending_front", tex);
        }

        /// <summary>A walking (green) or standing (red) figure on a dark lens.</summary>
        private static Texture2D WalkTexture(Color c, bool walking)
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true);
            var px = Enumerable.Repeat(new Color(0.06f, 0.06f, 0.07f), n * n).ToArray();
            void Disc(int cx, int cy, int r) { for (int y = cy - r; y <= cy + r; y++) for (int x = cx - r; x <= cx + r; x++) if (x >= 0 && y >= 0 && x < n && y < n && (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) px[y * n + x] = c; }
            void Line(int x0, int y0, int x1, int y1, int r) { for (int i = 0; i <= 20; i++) Disc(Mathf.RoundToInt(Mathf.Lerp(x0, x1, i / 20f)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, i / 20f)), r); }
            Disc(32, 52, 6);
            Line(32, 44, 32, 26, 5);
            if (walking) { Line(32, 26, 22, 8, 3); Line(32, 26, 42, 10, 3); Line(32, 40, 20, 30, 2); Line(32, 40, 44, 32, 2); }
            else { Line(32, 26, 26, 6, 3); Line(32, 26, 38, 6, 3); Line(32, 40, 22, 26, 2); Line(32, 40, 42, 26, 2); }
            tex.SetPixels(px); tex.Apply();
            return Save(walking ? "walk_green" + (c.g > 0.5f ? "" : "_off") : "walk_red" + (c.r > 0.5f ? "" : "_off"), tex);
        }
    }
}
