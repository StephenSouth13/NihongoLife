#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Bakes the city horizon into 90_TestSandbox so the player never sees the map's end or the black void
    /// below the sky: ground strips beyond the playable street, outer-district building silhouettes, a hill
    /// ring and a fog-tinted haze band, all under one "Horizon_Backdrop" root driven by HorizonBackdrop.
    /// Re-running replaces that root (never stacks). The playable city, colliders and WorldBoundsGuard are
    /// not touched; the backdrop has no colliders.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.HorizonBuilder.Build
    /// </summary>
    public static class HorizonBuilder
    {
        private const string ScenePath = "Assets/NihongoLife/Scenes/90_TestSandbox.unity";
        private const string RootName = "Horizon_Backdrop";
        private const string MaterialDir = "Assets/NihongoLife/Materials/Horizon";
        private const string MeshDir = "Assets/NihongoLife/Models/Horizon";
        private const float Period = 100f;

        public static void Build()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                foreach (var old in scene.GetRootGameObjects().Where(g => g.name == RootName).ToArray())
                    Object.DestroyImmediate(old);

                // Playable city extent across the street (the street itself loops endlessly along Z).
                float edgeX = 66f, groundY = 0f;
                var renderers = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                    .Where(r => r.bounds.size.x < 300f && r.bounds.size.z < 400f && !(r is ParticleSystemRenderer)).ToArray();
                if (renderers.Length > 0)
                {
                    float maxAbs = renderers.Max(r => Mathf.Max(Mathf.Abs(r.bounds.min.x), Mathf.Abs(r.bounds.max.x)));
                    edgeX = Mathf.Clamp(maxAbs, 60f, 160f);
                }
                var ground = GameObject.Find("GameplayGround");
                if (ground != null && ground.TryGetComponent(out Collider groundCollider)) groundY = groundCollider.bounds.max.y;
                Debug.Log($"[HorizonBuilder] City edge |x| = {edgeX:0.0}, ground y = {groundY:0.00}");

                Directory.CreateDirectory(MaterialDir);
                Directory.CreateDirectory(MeshDir);
                var groundMat = LitMaterial("Horizon_Ground", new Color(0.24f, 0.29f, 0.22f), 0.1f);
                var buildingMat = LitMaterial("Horizon_Buildings", Color.white, 0.2f);
                FacadeTextures(buildingMat);
                var hillMat = LitMaterial("Horizon_Hills", new Color(0.22f, 0.3f, 0.26f), 0.05f);
                var hazeMat = HazeMaterial();

                var root = new GameObject(RootName);
                root.AddComponent<SceneZoneVisibility>();
                var backdrop = root.AddComponent<HorizonBackdrop>();

                // Ground strips either side of the street, slightly below the city ground so they never z-fight.
                var alongStreet = Child(root.transform, "FollowStreet");
                var strip = SaveMesh(Box(new Vector3(0f, 0f, 0f), new Vector3(1000f, 0.2f, 2000f)), "Horizon_GroundStrip");
                foreach (int side in new[] { -1, 1 })
                    Part(alongStreet, "GroundStrip_" + (side < 0 ? "W" : "E"), strip, groundMat, new Vector3(side * (edgeX - 4f + 500f), groundY - 0.25f, 0f));

                // Outer-district silhouettes: one 100 m period of blocks, repeated along the street.
                var periodic = Child(root.transform, "OuterDistrict");
                var district = SaveMesh(DistrictMesh(edgeX, groundY), "Horizon_District");
                for (int k = -7; k <= 7; k++)
                    Part(periodic, "Blocks_" + k, district, buildingMat, new Vector3(0f, 0f, k * Period));

                // Hill ring + haze band around the camera.
                var around = Child(root.transform, "FollowCamera");
                Part(around, "HillRing", SaveMesh(HillRing(groundY), "Horizon_Hills"), hillMat, Vector3.zero);
                var haze = Part(around, "HazeBand", SaveMesh(HazeBand(groundY), "Horizon_Haze"), hazeMat, Vector3.zero);

                var so = new SerializedObject(backdrop);
                so.FindProperty("periodicRoot").objectReferenceValue = periodic;
                so.FindProperty("period").floatValue = Period;
                so.FindProperty("followAlongStreet").objectReferenceValue = alongStreet;
                so.FindProperty("followCamera").objectReferenceValue = around;
                var windowProp = so.FindProperty("windowRenderers");
                var blocks = periodic.GetComponentsInChildren<MeshRenderer>();
                windowProp.arraySize = blocks.Length;
                for (int i = 0; i < blocks.Length; i++) windowProp.GetArrayElementAtIndex(i).objectReferenceValue = blocks[i];
                var hazeProp = so.FindProperty("hazeRenderers");
                hazeProp.arraySize = 1;
                hazeProp.GetArrayElementAtIndex(0).objectReferenceValue = haze;
                so.ApplyModifiedPropertiesWithoutUndo();

                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + ScenePath);
                Debug.Log("[HorizonBuilder] Horizon backdrop baked into " + ScenePath);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        // ─────────── Geometry ───────────

        private static Mesh DistrictMesh(float edgeX, float groundY)
        {
            var random = new System.Random(1307);
            var parts = new List<Mesh>();
            foreach (int side in new[] { -1, 1 })
            {
                // Rows get taller and sparser the further they are from the street.
                for (int row = 0; row < 6; row++)
                {
                    float x0 = edgeX + 42f + row * 38f;
                    float z = -Period * 0.5f;
                    while (z < Period * 0.5f - 6f)
                    {
                        float width = Lerp(random, 10f, 22f);
                        float depth = Lerp(random, 12f, 26f);
                        if (z + width > Period * 0.5f) width = Period * 0.5f - z;
                        // Low-rise next to the city, office blocks behind, the odd tower far back.
                        float height = Lerp(random, 6f, 13f) + row * Lerp(random, 3f, 7f);
                        if (row >= 3 && random.NextDouble() < 0.1) height *= 1.9f;
                        height = Mathf.Round(height / 3.5f) * 3.5f + 0.5f; // whole storeys so windows line up
                        float x = side * (x0 + Lerp(random, 0f, 10f) + depth * 0.5f);
                        parts.Add(Box(new Vector3(x, groundY - 0.5f + height * 0.5f, z + width * 0.5f), new Vector3(depth, height + 1f, width - Lerp(random, 1.5f, 4f))));
                        z += width + Lerp(random, 0f, 4f);
                    }
                }
            }
            return Combine(parts);
        }

        private static Mesh HillRing(float groundY)
        {
            const int segments = 160;
            float[] radii = { 470f, 560f, 640f, 760f };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                float crest = 38f + 26f * Mathf.Sin(angle * 3f + 0.6f) + 16f * Mathf.Sin(angle * 7f + 1.9f) + 9f * Mathf.Sin(angle * 13f);
                float[] heights = { -30f, crest * 0.55f, crest, -30f };
                var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                for (int r = 0; r < radii.Length; r++)
                    vertices.Add(direction * radii[r] + Vector3.up * (groundY + heights[r]));
            }
            int ring = radii.Length;
            for (int i = 0; i < segments; i++)
                for (int r = 0; r < ring - 1; r++)
                {
                    int a = i * ring + r, b = a + 1, c = a + ring, d = c + 1;
                    triangles.AddRange(new[] { a, c, b, b, c, d });
                }
            return Finish(vertices, triangles, null);
        }

        private static Mesh HazeBand(float groundY)
        {
            const int segments = 96;
            const float radius = 880f;
            float[] heights = { -80f, 30f, 95f, 190f };
            float[] alpha = { 1f, 1f, 0.55f, 0f };
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                for (int h = 0; h < heights.Length; h++)
                {
                    vertices.Add(direction * radius + Vector3.up * (groundY + heights[h]));
                    uvs.Add(new Vector2(i / (float)segments, 1f - alpha[h]));
                }
            }
            int ring = heights.Length;
            for (int i = 0; i < segments; i++)
                for (int h = 0; h < ring - 1; h++)
                {
                    int a = i * ring + h, b = a + 1, c = a + ring, d = c + 1;
                    triangles.AddRange(new[] { a, b, c, b, d, c }); // faces inward, toward the camera
                }
            return Finish(vertices, triangles, uvs);
        }

        /// <summary>Box with UVs in storeys: one texture tile = one 4 m × 3.5 m window bay on every wall.</summary>
        private static Mesh Box(Vector3 center, Vector3 size)
        {
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var source = mesh.GetComponent<MeshFilter>().sharedMesh;
            var vertices = source.vertices.Select(v => center + Vector3.Scale(v, size)).ToArray();
            var normals = source.normals;
            var uv = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = vertices[i];
                Vector3 n = normals[i];
                uv[i] = Mathf.Abs(n.x) > 0.5f ? new Vector2(p.z / 4f, p.y / 3.5f)
                      : Mathf.Abs(n.z) > 0.5f ? new Vector2(p.x / 4f, p.y / 3.5f)
                      : new Vector2(0.02f, 0.02f); // roofs: plain wall corner of the tile
            }
            var result = new Mesh { vertices = vertices, triangles = source.triangles, normals = normals, uv = uv };
            Object.DestroyImmediate(mesh);
            return result;
        }

        private static Mesh Combine(List<Mesh> parts)
        {
            var combine = parts.Select(p => new CombineInstance { mesh = p, transform = Matrix4x4.identity }).ToArray();
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(combine, true, false);
            foreach (var p in parts) Object.DestroyImmediate(p);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh Finish(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
        {
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            if (uvs != null) mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float Lerp(System.Random random, float a, float b) => a + (float)random.NextDouble() * (b - a);

        // ─────────── Assets & objects ───────────

        private static Mesh SaveMesh(Mesh mesh, string name)
        {
            string path = $"{MeshDir}/{name}.asset";
            mesh.name = name;
            // Huge bounds so the backdrop is never culled while it follows the camera.
            mesh.bounds = new Bounds(mesh.bounds.center, mesh.bounds.size + Vector3.one * 50f);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Material LitMaterial(string name, Color color, float smoothness)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_EnvironmentReflections", 0f);
            material.SetFloat("_SpecularHighlights", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>4×4 atlas of window bays (varied wall colours) plus a matching emission atlas where about a
        /// third of the windows are lit; HorizonBackdrop turns the emission up after dusk.</summary>
        private static void FacadeTextures(Material material)
        {
            const int size = 64;
            var random = new System.Random(42);
            var albedo = new Texture2D(size * 4, size * 4, TextureFormat.RGBA32, false);
            var glow = new Texture2D(size * 4, size * 4, TextureFormat.RGBA32, false);
            var walls = new[] { new Color(0.78f, 0.76f, 0.72f), new Color(0.62f, 0.66f, 0.7f), new Color(0.72f, 0.66f, 0.6f), new Color(0.55f, 0.58f, 0.62f) };
            for (int by = 0; by < 4; by++)
                for (int bx = 0; bx < 4; bx++)
                {
                    Color wall = walls[(bx + by * 3) % walls.Length];
                    bool lit = random.NextDouble() < 0.35;
                    Color light = Color.Lerp(new Color(1f, 0.82f, 0.5f), new Color(0.85f, 0.92f, 1f), (float)random.NextDouble() * 0.6f);
                    for (int y = 0; y < size; y++)
                        for (int x = 0; x < size; x++)
                        {
                            bool window = x > 10 && x < 54 && y > 14 && y < 50;
                            bool frame = window && (x == 11 || x == 53 || y == 15 || y == 49 || x == 32);
                            Color c = window ? (frame ? wall * 0.7f : new Color(0.26f, 0.33f, 0.42f) + new Color(0.1f, 0.12f, 0.14f) * (y - 14) / 36f) : wall;
                            if (y < 4) c = wall * 0.8f; // floor slab line
                            c.a = 1f;
                            albedo.SetPixel(bx * size + x, by * size + y, c);
                            glow.SetPixel(bx * size + x, by * size + y, window && !frame && lit ? light : Color.black);
                        }
                }
            material.SetTexture("_BaseMap", SaveTexture(albedo, "Horizon_Facade"));
            material.SetTexture("_EmissionMap", SaveTexture(glow, "Horizon_FacadeGlow"));
            material.SetTextureScale("_BaseMap", new Vector2(0.25f, 0.25f));
            material.SetColor("_EmissionColor", Color.black);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }

        private static Texture2D SaveTexture(Texture2D texture, string name)
        {
            string path = $"{MaterialDir}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material HazeMaterial()
        {
            string texturePath = $"{MaterialDir}/Horizon_HazeGradient.png";
            var pixels = new Texture2D(4, 128, TextureFormat.RGBA32, false);
            for (int y = 0; y < 128; y++)
            {
                // uv.y = 1 - alpha at the band's height: bottom opaque, top clear (smoothstep falloff).
                float t = y / 127f;
                float a = 1f - Mathf.SmoothStep(0f, 1f, t);
                for (int x = 0; x < 4; x++) pixels.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            File.WriteAllBytes(texturePath, pixels.EncodeToPNG());
            Object.DestroyImmediate(pixels);
            AssetDatabase.ImportAsset(texturePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            var gradient = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            string path = $"{MaterialDir}/Horizon_Haze.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", gradient);
            material.SetColor("_BaseColor", new Color(0.67f, 0.76f, 0.83f, 1f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent - 50; // before shop windows / particles
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static MeshRenderer Part(Transform parent, string name, Mesh mesh, Material material, Vector3 position)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return renderer;
        }
    }
}
#endif
