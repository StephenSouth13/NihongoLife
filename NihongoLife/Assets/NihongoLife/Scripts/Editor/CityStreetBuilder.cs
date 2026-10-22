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
    /// <summary>
    /// Rebuilds the streets of Hibari-chō (90_TestSandbox) as a Japanese neighbourhood instead of the old grid of road
    /// tiles that crossed every block. Per city tile (CityTile_-1/0/1, 100 m apart):
    /// · Sakura-dōri — two-lane avenue (traffic on the left), white edge / dashed centre lines, ◇ crossing-ahead marks,
    ///   a zebra crossing from the side lane to the konbini with horizontal car signals and walking-figure signals,
    ///   paved sidewalks with kerbs, manholes;
    /// · a residential lane (路地) north–south with 路側帯 edge lines and 止まれ painted before the avenue, back alleys;
    /// · every house on its own lot: concrete-block walls (with colliders) on the sides and back, gravel, a garden
    ///   strip, gate stubs; destinations and the konbini keep an open front;
    /// · back blocks: rows of background houses, a small park and a coin parking with parked cars;
    /// · curve mirrors, vending machines, and cars driving Sakura-dōri (World/StreetTraffic).
    /// The old road tiles, sidewalk pieces, lane lines, crossings, driveways and back-lot patches are removed (not
    /// stacked), and duplicate buildings where tiles overlap are removed. Textures are generated here.
    /// Run: -executeMethod NihongoLife.EditorTools.CityStreetBuilder.Build
    /// </summary>
    public static partial class CityStreetBuilder
    {
        // The town (90) by default; NL_STREET_SCENE=01_MainMenu rebuilds the copy of the town behind the main menu.
        private static string ScenePath => $"Assets/NihongoLife/Scenes/{System.Environment.GetEnvironmentVariable("NL_STREET_SCENE") ?? "90_TestSandbox"}.unity";
        private const string Gen = "Assets/NihongoLife/Generated/City";
        private const string KayKit = "Assets/ThirdParty/KayKit-City-Builder-Bits-1.0-main/KayKit-City-Builder-Bits-1.0-main/addons/kaykit_city_builder_bits/Assets/obj/";
        private const string Houses = "Assets/NihongoLife/Prefabs/Environment/Buildings/";
        private const float XMin = -64.5f, XMax = 64.5f;
        // Relative to the tile (tile k sits at z + 100k).
        private const float AveN = -6.4f, AveS = -13.6f, WalkN = -2.6f, WalkS = -17.4f, AveC = -10f;
        private const float LaneW = 0.7f, LaneE = 6.3f;
        // Stop lines 2 m before the crossing; StreetTraffic stops each car's front bumper behind them.
        private const float StopEastX = LaneW - 2f, StopWestX = LaneE + 2f;
        private const float AlleyN0 = 12.8f, AlleyN1 = 16f, AlleyS0 = -34.6f, AlleyS1 = -31.4f;
        private static readonly string[] OldPieces = { "road_straight", "road_crossroad", "road_side", "road_crossing", "driveway_short", "traffic_light", "road_intersection" };
        private static readonly string[] DestinationBuildings = { "StreetBuilding_N_2", "StreetBuilding_N_3", "StreetBuilding_N_7", "StreetBuilding_S_4", "StreetBuilding_S_5" };

        private static Material _asphalt, _laneAsphalt, _paver, _kerb, _paint, _wall, _wallCap, _gravel, _grass, _concrete, _parking,
            _iron, _signalBody, _lampOff, _green, _yellow, _red, _walkGreen, _walkRed, _mirrorPole, _mirror, _vendBody, _vendFront;
        private static TMP_FontAsset _font;

        public static void Build()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                foreach (var f in new[] { Gen, Gen + "/Textures", Gen + "/Materials", Gen + "/Meshes", Gen + "/Cars" }) ZoneBuildKit.EnsureFolder(f);
                _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/NihongoLife/Fonts/NotoSansJP SDF.asset");
                Materials();
                var cars = CarPrefabs();
                // The town has Environment/CityTile_*; the main menu has one flattened copy (MenuTownPreview_*) = tile 0.
                var envGo = GameObject.Find("Environment");
                var menuCopy = scene.GetRootGameObjects().FirstOrDefault(g => g.name.StartsWith("MenuTownPreview_"));
                if (envGo == null && menuCopy == null) throw new InvalidOperationException("No Environment root in " + ScenePath);
                var env = envGo != null ? envGo.transform : menuCopy.transform;
                var tiles = envGo != null ? env.Cast<Transform>().Where(t => t.name.StartsWith("CityTile_")).OrderBy(t => TileIndex(t)).ToList() : new List<Transform> { menuCopy.transform };
                RemoveSeamDuplicates(tiles);
                FixMissingMaterials(env);
                foreach (var tile in tiles) BuildTile(tile, TileIndex(tile), tiles, cars);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("[CityStreetBuilder] Streets rebuilt on " + tiles.Count + " tiles.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static int Mod(int a, int m) => ((a % m) + m) % m;

        private static int TileIndex(Transform t) => t.name.StartsWith("CityTile_") ? int.Parse(t.name.Substring("CityTile_".Length)) : 0;

        // ─────────────────────────── cleanup ───────────────────────────

        private static void RemoveSeamDuplicates(List<Transform> tiles)
        {
            var kept = new List<(Transform t, Bounds b)>();
            foreach (var tile in tiles)
                foreach (Transform child in tile.Cast<Transform>().ToList())
                {
                    if (!child.name.StartsWith("StreetBuilding_") || !child.gameObject.activeSelf) continue;
                    var b = RendererBounds(child);
                    var clash = kept.FirstOrDefault(k => k.t.parent != tile && Overlap(k.b, b) > 1.5f);
                    if (clash.t != null) { Debug.Log($"[CityStreetBuilder] removed seam duplicate {tile.name}/{child.name} (overlaps {clash.t.parent.name}/{clash.t.name})"); Object.DestroyImmediate(child.gameObject); continue; }
                    kept.Add((child, b));
                }
        }

        /// <summary>Facade accents and the door handles of an older builder lost their runtime materials (drawn magenta).</summary>
        private static void FixMissingMaterials(Transform env)
        {
            var warm = Lit("WarmWindow", new Color(1f, 0.78f, 0.42f));
            int fixedCount = 0;
            foreach (var r in env.GetComponentsInChildren<Renderer>(true))
            {
                if (r.sharedMaterials.Length > 0 && r.sharedMaterials.All(m => m != null)) continue;
                r.sharedMaterial = r.name.StartsWith("WarmWindow") ? warm : r.name.StartsWith("Handle") ? _iron : _wallCap;
                fixedCount++;
            }
            Debug.Log($"[CityStreetBuilder] materials restored on {fixedCount} renderers");
        }

        private static float Overlap(Bounds a, Bounds b)
        {
            float x = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
            float z = Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z);
            return x > 0f && z > 0f ? x * z : 0f;
        }

        private static void RemoveOldStreet(Transform tile)
        {
            var old = tile.Find("Streetscape");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            foreach (Transform child in tile.Cast<Transform>().ToList())
            {
                var src = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
                string prefab = src != null ? Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(src)) : "";
                bool old1 = OldPieces.Contains(prefab);
                bool old2 = child.name.StartsWith("LaneLine_") || child.name.StartsWith("BackLot_") || child.name.StartsWith("PocketPark_")
                    || child.name.StartsWith("Crosswalk_") || child.name == "MainCrossroad" || child.name.StartsWith("TrafficLight_")
                    || child.name.StartsWith("BackFence_"); // old fences that now stand inside the park / parking
                if (old1 || old2) Object.DestroyImmediate(child.gameObject);
            }
        }

        // ─────────────────────────── one tile ───────────────────────────

        private sealed class Ctx
        {
            public Transform root;
            public int k;
            public float dz;
            public readonly Dictionary<Material, Batch> batches = new();
            public Transform walls;
            public readonly List<Rect> lots = new();
            public readonly List<Rect> reserved = new();
            /// <summary>Footprints walls may never cross: every building on the tile and the reserved areas.</summary>
            public readonly List<Rect> solids = new();
            public Batch B(Material m, float uv) { if (!batches.TryGetValue(m, out var b)) batches[m] = b = new Batch(uv); return b; }
        }

        private static void BuildTile(Transform tile, int k, List<Transform> tiles, Dictionary<string, GameObject> cars)
        {
            RemoveOldStreet(tile);
            var c = new Ctx { k = k, dz = 100f * k };
            c.root = new GameObject("Streetscape").transform;
            c.root.SetParent(tile, false);
            c.root.position = new Vector3(0f, 0f, c.dz);
            c.walls = Group(c.root, "WallColliders");
            float dz = c.dz;

            // Things lots must leave alone.
            foreach (string name in new[] { "Town_Konbini", "Town_Plaza", "ThirdParty_Integrated_World/Story_ThirdParty_Visuals/StationLandmark" })
            {
                var go = GameObject.Find(name);
                if (go != null && k == 0) c.reserved.Add(ToRect(RendererBounds(go.transform), 0.4f));
            }
            var store = tile.Find("KonbiniStoreAsset");
            if (store != null && store.gameObject.activeSelf) c.reserved.Add(ToRect(RendererBounds(store), 0.4f));
            c.solids.AddRange(c.reserved);
            foreach (Transform t in tile)
                if (t.gameObject.activeSelf && t.name.StartsWith("StreetBuilding_")) c.solids.Add(ToRect(RendererBounds(t), -0.05f));

            // Ground layers.
            c.B(_laneAsphalt, 4f).Quad(XMin, XMax, dz - 55f, dz + 45f, 0.004f);                       // residential asphalt base
            c.B(_asphalt, 4f).Quad(XMin, XMax, dz + AveS, dz + AveN, 0.010f);                         // avenue
            c.B(_paver, 2f).Quad(XMin, XMax, dz + AveN + 0.2f, dz + WalkN, 0.012f);                    // north sidewalk
            c.B(_paver, 2f).Quad(XMin, XMax, dz + WalkS, dz + AveS - 0.2f, 0.012f);                    // south sidewalk
            c.B(_kerb, 1f).Box(new Vector3(XMin, 0f, dz + AveN), new Vector3(XMax, 0.07f, dz + AveN + 0.2f));
            c.B(_kerb, 1f).Box(new Vector3(XMin, 0f, dz + AveS - 0.2f), new Vector3(XMax, 0.07f, dz + AveS));
            AvenueMarkings(c);
            LaneMarkings(c);

            // Lots of the houses on this tile.
            var houses = tile.Cast<Transform>().Where(t => t.gameObject.activeSelf && (t.name.StartsWith("StreetBuilding_") || t.name == "KonbiniStoreAsset")).ToList();
            foreach (var h in houses.OrderBy(h => h.name.StartsWith("StreetBuilding_N") || h.name.StartsWith("StreetBuilding_S") ? 0 : 1))
            {
                if (h.name == "KonbiniStoreAsset") continue;
                var b = RendererBounds(h);
                char side = h.name[15]; // N S W E
                bool open = k == 0 && DestinationBuildings.Contains(h.name);
                switch (side)
                {
                    case 'N': Lot(c, b, Vector3.back, dz + WalkN, open); break;
                    case 'S': Lot(c, b, Vector3.forward, dz + WalkS, open); break;
                    case 'W': Lot(c, b, Vector3.right, LaneW - 0.5f, false); break;
                    case 'E': Lot(c, b, Vector3.left, LaneE + 0.5f, false); break;
                }
            }

            // Back blocks: background houses, park, parking.
            var existing = tiles.SelectMany(t => t.Cast<Transform>()).Where(t => t.gameObject.activeSelf && t.name.StartsWith("StreetBuilding_")).Select(t => ToRect(RendererBounds(t), 0.5f)).ToList();
            BackBlock(c, existing, rowA: true, rowB: k < 1);
            if (k < 1) { Park(c); Parking(c, cars); }

            // Furniture.
            Crossing(c);
            Mirrors(c);
            Vending(c);
            Manholes(c);

            foreach (var kv in c.batches) Emit(c.root, $"Surface_{kv.Key.name}", kv.Value, kv.Key, k);
            Traffic(c, cars);
        }

        // ─────────────────────────── markings ───────────────────────────

        private static void AvenueMarkings(Ctx c)
        {
            float dz = c.dz, y = 0.016f;
            var paint = c.B(_paint, 1f);
            foreach (float z in new[] { AveN - 0.35f, AveS + 0.35f })
            {
                paint.Quad(XMin, LaneW - 0.4f, dz + z - 0.075f, dz + z + 0.075f, y);
                paint.Quad(LaneE + 0.4f, XMax, dz + z - 0.075f, dz + z + 0.075f, y);
            }
            // Centre line: solid for 30 m on each side of the crossing (no overtaking there), dashed 5 m on / 3 m off beyond.
            float solidW = StopEastX - 28f, solidE = StopWestX + 28f;
            paint.Quad(solidW, StopEastX, dz + AveC - 0.075f, dz + AveC + 0.075f, y);
            paint.Quad(StopWestX, solidE, dz + AveC - 0.075f, dz + AveC + 0.075f, y);
            for (float x = solidW - 3f; x - 5f > XMin; x -= 8f) paint.Quad(x - 5f, x, dz + AveC - 0.075f, dz + AveC + 0.075f, y);
            for (float x = solidE + 3f; x + 5f < XMax; x += 8f) paint.Quad(x, x + 5f, dz + AveC - 0.075f, dz + AveC + 0.075f, y);
            // Zebra: bars along the road (the way cars drive), 0.45 m wide with 0.45 m gaps, across the whole roadway.
            for (float z = AveS + 0.5f; z + 0.45f <= AveN - 0.45f; z += 0.9f)
                paint.Quad(LaneW + 0.2f, LaneE - 0.2f, dz + z, dz + z + 0.45f, y);
            paint.Quad(StopEastX - 0.15f, StopEastX + 0.15f, dz + AveC, dz + AveN - 0.35f, y);   // stop line, eastbound (north lane)
            paint.Quad(StopWestX - 0.15f, StopWestX + 0.15f, dz + AveS + 0.35f, dz + AveC, y);   // stop line, westbound (south lane)
            Arrow(paint, new Vector3(StopEastX - 12f, y, dz + AveC + 1.6f), 1f);                // lane arrows: straight on
            Arrow(paint, new Vector3(StopWestX + 12f, y, dz + AveC - 1.6f), -1f);
            Diamond(paint, new Vector3(-24f, y, dz - 8.4f));                                    // ◇ crossing ahead
            Diamond(paint, new Vector3(-12f, y, dz - 8.4f));
            Diamond(paint, new Vector3(19f, y, dz - 11.6f));
            Diamond(paint, new Vector3(31f, y, dz - 11.6f));
        }

        /// <summary>A straight-on lane arrow pointing along +x (dir 1) or -x (dir -1).</summary>
        private static void Arrow(Batch b, Vector3 tail, float dir)
        {
            var head = tail + Vector3.right * (3.2f * dir);
            b.Strip(tail, head - Vector3.right * (0.4f * dir), 0.18f);
            var baseL = head - Vector3.right * (1.1f * dir) + Vector3.forward * 0.55f;
            var baseR = head - Vector3.right * (1.1f * dir) - Vector3.forward * 0.55f;
            b.Strip(baseL, head, 0.16f); b.Strip(baseR, head, 0.16f);
        }

        private static void Diamond(Batch b, Vector3 centre)
        {
            const float half = 1.6f, wide = 0.75f, t = 0.12f;
            var n = new Vector3(centre.x, centre.y, centre.z + wide);
            var s = new Vector3(centre.x, centre.y, centre.z - wide);
            var e = new Vector3(centre.x + half, centre.y, centre.z);
            var w = new Vector3(centre.x - half, centre.y, centre.z);
            b.Strip(w, n, t); b.Strip(n, e, t); b.Strip(e, s, t); b.Strip(s, w, t);
        }

        private static void LaneMarkings(Ctx c)
        {
            float dz = c.dz, y = 0.016f;
            var paint = c.B(_paint, 1f);
            // South lane: from the tile edge up to the sidewalk; north lane: from behind the konbini up to the tile edge.
            // The edge lines (路側帯) break where the back alleys join.
            var cuts = new[] { (dz + AlleyN0, dz + AlleyN1), (dz + AlleyS0, dz + AlleyS1) };
            foreach (var (z0, z1) in new[] { (dz - 55f, dz + WalkS), (dz + 10.5f, dz + 45f) })
                foreach (var (a, b) in Subtract(z0, z1, cuts))
                    foreach (float x in new[] { LaneW + 0.45f, LaneE - 0.45f })
                        paint.Quad(x - 0.06f, x + 0.06f, a, b, y);
            paint.Quad(LaneW + 0.5f, (LaneW + LaneE) * 0.5f, dz + WalkS - 0.9f, dz + WalkS - 0.6f, y);     // stop line before the avenue
            Text(c.root, "Tomare", "止まれ", new Vector3((LaneW * 3f + LaneE) / 4f, y + 0.002f, dz + WalkS - 3.2f), 0f, 1.6f, Color.white);
            // Alley edge lines.
            foreach (var (a0, a1, xs, xe) in new[] { (AlleyN0, AlleyN1, XMin + 0.5f, LaneW), (AlleyS0, AlleyS1, XMin + 0.5f, -17f), (AlleyS0, AlleyS1, 17f, XMax - 0.5f), (AlleyN0, AlleyN1, 17f, XMax - 0.5f) })
            {
                paint.Quad(xs, xe, dz + a0 + 0.3f, dz + a0 + 0.4f, y);
                paint.Quad(xs, xe, dz + a1 - 0.4f, dz + a1 - 0.3f, y);
            }
        }

        private static IEnumerable<(float, float)> Subtract(float from, float to, IEnumerable<(float a, float b)> cuts)
        {
            float start = from;
            foreach (var (a, b) in cuts.OrderBy(c => c.a))
            {
                if (b <= start || a >= to) continue;
                if (a > start) yield return (start, a);
                start = Mathf.Max(start, b);
            }
            if (start < to) yield return (start, to);
        }




    }
}
