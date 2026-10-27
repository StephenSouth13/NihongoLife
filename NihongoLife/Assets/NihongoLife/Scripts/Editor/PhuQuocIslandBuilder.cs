using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.PhuQuoc;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// First pass of Phú Quốc island (scene 65_PhuQuocIsland), data-driven from Resources/PhuQuoc/phu_quoc_map.json:
    ///  • the coastline (real coordinates, 1 unit = 100 m → ~500 units north–south) as a flat sand-and-grass mesh
    ///    over a sea plane, with the 99-peak ridge as low hills in the north;
    ///  • a marker for every real place (Dương Đông, chợ đêm, Dinh Cậu, Bãi Sao, Bãi Dài, An Thới, Hòn Thơm …) using
    ///    CC0 Kenney models (Assets/ThirdParty/PhuQuocCC0, see Docs/PHU_QUOC_ASSETS.md) and a label;
    ///  • Gánh Ngon at 113 Trần Hưng Đạo with its service stations (GanhNgonSpot) and a player spawn.
    /// It never touches another scene. Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.PhuQuocIslandBuilder.Build
    /// </summary>
    public static class PhuQuocIslandBuilder
    {
        public const string ScenePath = "Assets/NihongoLife/Scenes/65_PhuQuocIsland.unity";
        public const string AssetRoot = "Assets/ThirdParty/PhuQuocCC0";
        private const string MatDir = "Assets/NihongoLife/Materials/PhuQuoc";
        /// <summary>Zones sit far outside the town (it stays loaded, hidden, while the player is here).</summary>
        public static readonly Vector3 Origin = new Vector3(4000f, 0f, 0f);

        // Headless only (AGENTS.md: no [MenuItem]).
        public static void Build()
        {
            try
            {
                // A zone like Midori and the clinic: no camera or sun of its own (the town's player, camera and sky
                // carry over), a spawn the scene flow places the player on, a way back, a preview camera.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var root = new GameObject("PhuQuocIsland");
                root.transform.position = Origin;
                root.AddComponent<NihongoLife.Core.SceneZoneVisibility>();
                BuildLayout(root.transform, PhuQuocMap.Load(), true);
                ZoneParts(root.transform, PhuQuocMap.Load());
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                if (!EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath))
                    throw new InvalidOperationException("Could not save " + ScenePath);
                var scenes = EditorBuildSettings.scenes.ToList();
                if (!scenes.Any(s => s.path == ScenePath)) { scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
                AssetDatabase.SaveAssets();
                Debug.Log("[PhuQuocIslandBuilder] " + ScenePath + " built.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static void ZoneParts(Transform root, PhuQuocMap map)
        {
            var restaurant = root.Find("GanhNgon_Restaurant");
            if (restaurant == null) throw new InvalidOperationException("Gánh Ngon is missing from the map data");
            var old = restaurant.Find("PlayerSpawn_GanhNgon");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var spawn = new GameObject("Spawn_" + NihongoLife.Core.WorldLocationCatalog.PhuQuocArrival).transform;
            spawn.SetParent(restaurant, false);
            spawn.localPosition = new Vector3(0f, 0.15f, -6f);
            spawn.gameObject.AddComponent<NihongoLife.Core.SceneSpawnPoint>().Configure(NihongoLife.Core.WorldLocationCatalog.PhuQuocArrival);

            // The way home: back to Hibari (the station square), as the trip will start from there.
            var exit = new GameObject("ExitToHibari");
            exit.layer = ZoneBuildKit.InteractableLayer;
            exit.transform.SetParent(restaurant, false);
            exit.transform.localPosition = new Vector3(4f, 1f, -6f);
            var box = exit.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.6f, 2f, 1.6f);
            var portal = exit.AddComponent<NihongoLife.Interaction.ScenePortal>();
            portal.Configure(NihongoLife.Core.WorldLocationCatalog.CityScene, NihongoLife.Core.WorldLocationCatalog.CityStationReturn, "ひばり町へ / Về Hibari", true);
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "ExitMarker";
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.transform.SetParent(exit.transform, false);
            marker.transform.localPosition = new Vector3(0f, -0.95f, 0f);
            marker.transform.localScale = new Vector3(1.4f, 0.05f, 1.4f);
            ZoneBuildKit.Text(exit.transform, "ExitLabel", "ひばり町へ\n<size=60%>Về Hibari</size>", new Vector3(0f, 1.6f, 0f), 0f, 0.35f, Color.white, 3f);

            var preview = new GameObject("PreviewCamera") { tag = "MainCamera" };
            preview.transform.SetParent(root, false);
            preview.transform.localPosition = restaurant.localPosition + new Vector3(0f, 14f, -24f);
            preview.transform.LookAt(restaurant.position);
            preview.AddComponent<Camera>().farClipPlane = 3000f;
            preview.AddComponent<AudioListener>();
            preview.AddComponent<NihongoLife.Cameras.ZonePreviewCamera>();
        }

        /// <summary>Builds the island under <paramref name="root"/>. withModels=false builds primitives only (tests).</summary>
        public static void BuildLayout(Transform root, PhuQuocMap map, bool withModels)
        {
            var sand = Mat("PQ_Sand", new Color(0.93f, 0.85f, 0.66f), AssetRoot + "/ambientcg_Ground054/Ground054_1K-JPG_Color.jpg", withModels);
            var grass = Mat("PQ_Grass", new Color(0.36f, 0.62f, 0.33f), null, withModels);
            var sea = Mat("PQ_Sea", new Color(0.16f, 0.56f, 0.72f), null, withModels);

            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Sea";
            water.transform.SetParent(root, false);
            water.transform.localPosition = new Vector3(0, -0.3f, -60);
            water.transform.localScale = new Vector3(90, 1, 90);
            water.GetComponent<Renderer>().sharedMaterial = sea;

            var outline = map.OutlineWorld();
            var beach = PolygonObject("Island_Beach", outline, 0f, sand, root);
            var inland = PolygonObject("Island_Inland", Shrink(outline, 0.9f), 0.05f, grass, root);
            beach.AddComponent<MeshCollider>();

            // The 99-peak ridge (dãy Hàm Ninh): Núi Chúa 603 m in the north, lower to the south (vertical 1 unit = 50 m).
            var ridge = new GameObject("Ridge_99Peaks").transform; ridge.SetParent(root, false);
            var chua = map.Place("nui_chua");
            if (chua != null)
            {
                var top = map.ToWorld(chua);
                for (int i = 0; i < 6; i++)
                {
                    var hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    hill.name = "Hill_" + i;
                    hill.transform.SetParent(ridge, false);
                    float h = 603f / map.verticalMetersPerUnit * (1f - i * 0.14f);
                    hill.transform.localPosition = top + new Vector3(-i * 6f, 0, -i * 22f);
                    hill.transform.localScale = new Vector3(40f, h * 2f, 30f);
                    hill.GetComponent<Renderer>().sharedMaterial = grass;
                }
            }

            var places = new GameObject("Places").transform; places.SetParent(root, false);
            var font = withModels ? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ZoneBuildKit.FontPath) : null;
            var placed = new List<Vector3>();
            foreach (var p in map.places)
            {
                var at = map.ToWorld(p);
                int crowd = placed.Count(q => (q - at).sqrMagnitude < 14f * 14f); // Dương Đông packs many places in a few hundred metres
                placed.Add(at);
                var marker = new GameObject("Place_" + p.id);
                marker.transform.SetParent(places, false);
                marker.transform.localPosition = map.ToWorld(p) + Vector3.up * 0.1f;
                if (withModels) AddModel(marker.transform, p.category);
                else { var c = GameObject.CreatePrimitive(PrimitiveType.Cube); c.transform.SetParent(marker.transform, false); }
                var label = new GameObject("Label").AddComponent<TextMeshPro>();
                label.transform.SetParent(marker.transform, false);
                label.transform.localPosition = Vector3.up * (6f + crowd * 3.4f);
                if (font != null) label.font = font;
                label.text = p.nameVi + "\n<size=70%>" + p.nameJa + "</size>";
                label.fontSize = 12; label.alignment = TextAlignmentOptions.Center;
                label.rectTransform.sizeDelta = new Vector2(30, 6);
            }

            var g = map.Place("ganh_ngon");
            if (g != null)
            {
                var r = new GameObject("GanhNgon_Restaurant").transform; r.SetParent(root, false);
                r.localPosition = map.ToWorld(g) + new Vector3(0, 0.1f, -3f);
                Spot(r, "Reception", GanhNgonStation.Reception, new Vector3(0, 0, -2));
                for (int i = 0; i < 4; i++) Spot(r, "Table_" + (i + 1), GanhNgonStation.Table, new Vector3(-3 + i * 2, 0, 0));
                Spot(r, "KitchenPass", GanhNgonStation.Kitchen, new Vector3(-3, 0, 3));
                Spot(r, "PubBar", GanhNgonStation.Bar, new Vector3(3, 0, 3));
                Spot(r, "Manager_Mai", GanhNgonStation.Manager, new Vector3(0, 0, 3));
                var spawn = new GameObject("PlayerSpawn_GanhNgon"); spawn.transform.SetParent(r, false); spawn.transform.localPosition = new Vector3(0, 0, -5);
                for (int i = 0; i < 9; i++) { var vip = new GameObject("VIPRoom_" + (i + 1)); vip.transform.SetParent(r, false); vip.transform.localPosition = new Vector3(-4 + i, 0, 5); }
            }
        }

        private static void Spot(Transform parent, string name, GanhNgonStation station, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(0.8f, 0.4f, 0.8f);
            go.AddComponent<GanhNgonSpot>().station = station;
        }

        private static readonly Dictionary<string, string> ModelFor = new Dictionary<string, string>
        {
            { "beach", "kenney_pirate-kit/FBX/palm-detailed-bend.fbx" },
            { "island", "kenney_pirate-kit/FBX/palm-straight.fbx" },
            { "village", "kenney_pirate-kit/FBX/boat-row-large.fbx" },
            { "transport", "kenney_pirate-kit/FBX/structure-platform-dock.fbx" },
            { "nature", "kenney_nature-kit/FBX/tree_palmTall.fbx" },
            { "farm", "kenney_nature-kit/FBX/plant_bushLarge.fbx" },
            { "landmark", "kenney_nature-kit/FBX/rock_largeA.fbx" },
            { "history", "kenney_pirate-kit/FBX/tower-complete-small.fbx" },
            { "market", "kenney_city-kit-commercial/FBX/detail-parasol-a.fbx" },
            { "restaurant", "kenney_city-kit-commercial/FBX/building-c.fbx" },
            { "factory", "kenney_city-kit-commercial/FBX/building-f.fbx" },
            { "town", "kenney_city-kit-commercial/FBX/building-a.fbx" },
            { "entertainment", "kenney_city-kit-commercial/FBX/building-skyscraper-a.fbx" },
        };

        private static void AddModel(Transform parent, string category)
        {
            GameObject prefab = ModelFor.TryGetValue(category, out var rel) ? AssetDatabase.LoadAssetAtPath<GameObject>(AssetRoot + "/" + rel) : null;
            if (prefab == null) { var c = GameObject.CreatePrimitive(PrimitiveType.Cube); c.transform.SetParent(parent, false); return; }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.localScale = Vector3.one * 3f;
        }

        private static Material Mat(string name, Color color, string texPath, bool save)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (!save) return new Material(shader) { color = color };
            Directory.CreateDirectory(MatDir);
            string path = MatDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            var tex = texPath != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) : null;
            if (tex != null) { mat.mainTexture = tex; mat.mainTextureScale = new Vector2(60, 60); }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Scales the polygon toward its centroid (inland ring).</summary>
        public static Vector2[] Shrink(Vector2[] poly, float factor)
        {
            var c = new Vector2(poly.Average(p => p.x), poly.Average(p => p.y));
            return poly.Select(p => c + (p - c) * factor).ToArray();
        }

        private static GameObject PolygonObject(string name, Vector2[] poly, float y, Material mat, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = PolygonMesh(poly, y);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>Flat mesh of a simple polygon (ear clipping), facing up.</summary>
        public static Mesh PolygonMesh(Vector2[] poly, float y)
        {
            var tris = Triangulate(poly);
            var mesh = new Mesh { name = "PhuQuocOutline" };
            mesh.vertices = poly.Select(p => new Vector3(p.x, y, p.y)).ToArray();
            mesh.uv = poly.Select(p => p / 50f).ToArray();
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static List<int> Triangulate(Vector2[] poly)
        {
            var result = new List<int>();
            var idx = Enumerable.Range(0, poly.Length).ToList();
            float area = 0; for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++) area += poly[j].x * poly[i].y - poly[i].x * poly[j].y;
            if (area < 0) idx.Reverse(); // make counter-clockwise (in x/z seen from above)
            int guard = 0;
            while (idx.Count > 3 && guard++ < 10000)
            {
                bool clipped = false;
                for (int i = 0; i < idx.Count; i++)
                {
                    int a = idx[(i + idx.Count - 1) % idx.Count], b = idx[i], c = idx[(i + 1) % idx.Count];
                    Vector2 A = poly[a], B = poly[b], C = poly[c];
                    if ((B.x - A.x) * (C.y - A.y) - (B.y - A.y) * (C.x - A.x) <= 0) continue;
                    bool any = false;
                    foreach (int k in idx) { if (k == a || k == b || k == c) continue; if (PhuQuocMap.PointInPolygon(poly[k], new[] { A, B, C })) { any = true; break; } }
                    if (any) continue;
                    result.Add(a); result.Add(c); result.Add(b); // clockwise when viewed from +y → faces up
                    idx.RemoveAt(i); clipped = true; break;
                }
                if (!clipped) break;
            }
            if (idx.Count == 3) { result.Add(idx[0]); result.Add(idx[2]); result.Add(idx[1]); }
            return result;
        }
    }
}
