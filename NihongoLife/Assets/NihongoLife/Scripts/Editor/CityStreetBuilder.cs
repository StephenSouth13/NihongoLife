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
    public static class CityStreetBuilder
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

        // ─────────────────────────── lots ───────────────────────────

        /// <summary>A house lot: gravel, a garden strip, block walls on the sides and back (with colliders), gate stubs.</summary>
        private static void Lot(Ctx c, Bounds b, Vector3 front, float frontEdge, bool openFront)
        {
            Rect r;
            if (front == Vector3.back) r = Rect.MinMaxRect(b.min.x - 0.9f, frontEdge, b.max.x + 0.9f, b.max.z + 1.2f);
            else if (front == Vector3.forward) r = Rect.MinMaxRect(b.min.x - 0.9f, b.min.z - 1.2f, b.max.x + 0.9f, frontEdge);
            else if (front == Vector3.right) r = Rect.MinMaxRect(b.min.x - 1.2f, b.min.z - 0.9f, frontEdge, b.max.z + 0.9f);
            else r = Rect.MinMaxRect(frontEdge, b.min.z - 0.9f, b.max.x + 1.2f, b.max.z + 0.9f);
            var house = ToRect(b, 0f);
            foreach (var other in c.lots.Concat(c.reserved)) r = Trim(r, other, house);
            // A lot that still overlaps a reserved area (the konbini, the plaza, the station) is dropped: a fenced
            // yard must never reach into a shop.
            if (c.reserved.Any(x => x.Overlaps(r))) return;
            if (r.width < 3f || r.height < 3f) return;
            c.lots.Add(r);

            c.B(_gravel, 1.5f).Quad(r.xMin, r.xMax, r.yMin, r.yMax, 0.008f);
            // Garden strip along the front yard (the side away from the walls' gate).
            if (front == Vector3.back && b.min.z - r.yMin > 1.4f) c.B(_grass, 3f).Quad(r.xMin + 0.3f, r.xMin + 0.3f + r.width * 0.35f, r.yMin + 0.4f, b.min.z - 0.2f, 0.010f);
            if (front == Vector3.forward && r.yMax - b.max.z > 1.4f) c.B(_grass, 3f).Quad(r.xMax - 0.3f - r.width * 0.35f, r.xMax - 0.3f, b.max.z + 0.2f, r.yMax - 0.4f, 0.010f);
            if (front == Vector3.right && r.xMax - b.max.x > 1.4f) c.B(_grass, 3f).Quad(b.max.x + 0.2f, r.xMax - 0.4f, r.yMin + 0.3f, r.yMin + 0.3f + r.height * 0.4f, 0.010f);

            // Walls: everything but the front side; the front gets two stubs either side of a gate.
            var corners = new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) };
            int frontSide = front == Vector3.back ? 0 : front == Vector3.right ? 1 : front == Vector3.forward ? 2 : 3;
            for (int i = 0; i < 4; i++)
            {
                var a = corners[i]; var d = corners[(i + 1) % 4];
                if (i == frontSide)
                {
                    if (openFront) continue;
                    var dir = (d - a).normalized;
                    Wall(c, a, a + dir * 1.4f);
                    Wall(c, d - dir * 1.4f, d);
                    continue;
                }
                Wall(c, a, d);
            }
        }

        private static Rect Trim(Rect r, Rect other, Rect keep)
        {
            if (!r.Overlaps(other)) return r;
            var options = new List<Rect>();
            if (keep.yMin >= other.yMax - 0.01f) options.Add(Rect.MinMaxRect(r.xMin, other.yMax, r.xMax, r.yMax));
            if (keep.yMax <= other.yMin + 0.01f) options.Add(Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, other.yMin));
            if (keep.xMin >= other.xMax - 0.01f) options.Add(Rect.MinMaxRect(other.xMax, r.yMin, r.xMax, r.yMax));
            if (keep.xMax <= other.xMin + 0.01f) options.Add(Rect.MinMaxRect(r.xMin, r.yMin, other.xMin, r.yMax));
            return options.Count == 0 ? r : options.OrderByDescending(o => o.width * o.height).First();
        }

        /// <summary>A block wall from a to b, minus every stretch that would run through a building or a reserved area.</summary>
        private static void Wall(Ctx c, Vector2 a, Vector2 b)
        {
            var gaps = new List<(float t0, float t1)>();
            foreach (var solid in c.solids)
                if (ClipSegment(a, b, solid, out float t0, out float t1)) gaps.Add((t0, t1));
            float from = 0f;
            foreach (var (t0, t1) in gaps.OrderBy(g => g.t0))
            {
                if (t0 > from) WallPiece(c, Vector2.Lerp(a, b, from), Vector2.Lerp(a, b, t0));
                from = Mathf.Max(from, t1);
            }
            if (from < 1f) WallPiece(c, Vector2.Lerp(a, b, from), b);
        }

        /// <summary>The part of segment a→b inside rect r, as parameters [t0, t1] (Liang–Barsky).</summary>
        private static bool ClipSegment(Vector2 a, Vector2 b, Rect r, out float t0, out float t1)
        {
            t0 = 0f; t1 = 1f;
            Vector2 d = b - a;
            float[] p = { -d.x, d.x, -d.y, d.y };
            float[] q = { a.x - r.xMin, r.xMax - a.x, a.y - r.yMin, r.yMax - a.y };
            for (int i = 0; i < 4; i++)
            {
                if (Mathf.Abs(p[i]) < 1e-6f) { if (q[i] < 0f) return false; continue; }
                float t = q[i] / p[i];
                if (p[i] < 0f) t0 = Mathf.Max(t0, t); else t1 = Mathf.Min(t1, t);
                if (t0 > t1) return false;
            }
            return t1 - t0 > 1e-4f;
        }

        private static void WallPiece(Ctx c, Vector2 a, Vector2 b)
        {
            const float h = 1.15f, t = 0.15f;
            if ((b - a).magnitude < 0.3f) return;
            var dir = (b - a).normalized;
            var n = new Vector2(-dir.y, dir.x) * t * 0.5f;
            c.B(_wall, 1.2f).Prism(a - n, b - n, b + n, a + n, 0f, h);
            var cn = n * 1.35f;
            c.B(_wallCap, 1f).Prism(a - cn, b - cn, b + cn, a + cn, h, h + 0.06f);
            var col = new GameObject("Wall").AddComponent<BoxCollider>();
            col.transform.SetParent(c.walls, false);
            Vector2 mid = (a + b) * 0.5f;
            col.transform.position = new Vector3(mid.x, h * 0.5f, mid.y);
            col.transform.rotation = Quaternion.Euler(0f, -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, 0f);
            col.size = new Vector3((b - a).magnitude, h, t);
        }

        // ─────────────────────────── back blocks ───────────────────────────

        private static void BackBlock(Ctx c, List<Rect> existing, bool rowA, bool rowB)
        {
            var prefabs = "abcdefgh".Select(ch => AssetDatabase.LoadAssetAtPath<GameObject>($"{Houses}building_{ch}.prefab")).Where(p => p != null).ToArray();
            var group = Group(c.root, "BackHouses");
            int n = 0;
            foreach (float sx in new[] { -1f, 1f })
                foreach (float ax in new[] { 22f, 34f, 46f, 58f })
                {
                    float x = sx * ax;
                    if (rowA && !(sx < 0 && ax == 46f)) n += House(c, group, prefabs[Mod(n + c.k * 3 + 1, prefabs.Length)], new Vector3(x, 0f, c.dz + AlleyN1 + 1.2f + 3.7f), 180f, existing, Vector3.back, c.dz + AlleyN1) ? 1 : 0;
                    if (rowB) n += House(c, group, prefabs[Mod(n + c.k * 5 + 4, prefabs.Length)], new Vector3(x, 0f, c.dz + 100f + AlleyS0 - 1.2f - 3.7f), 0f, existing, Vector3.forward, c.dz + 100f + AlleyS0) ? 1 : 0;
                }
        }

        private static bool House(Ctx c, Transform group, GameObject prefab, Vector3 pos, float yaw, List<Rect> existing, Vector3 front, float frontEdge)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var b = RendererBounds(go.transform);
            var r = ToRect(b, 0.3f);
            if (existing.Any(e => e.Overlaps(r)) || c.reserved.Any(e => e.Overlaps(r)) || c.lots.Any(e => e.Overlaps(ToRect(b, -0.2f))))
            {
                Object.DestroyImmediate(go);
                return false;
            }
            go.name = "BackHouse_" + group.childCount;
            existing.Add(r);
            Lot(c, b, front, frontEdge, false);
            return true;
        }

        private static void Park(Ctx c)
        {
            float z0 = c.dz + AlleyN1 + 10f, z1 = c.dz + 100f + AlleyS0 - 10f;
            var r = Rect.MinMaxRect(-62f, z0, -19.5f, z1);
            if (c.reserved.Any(x => x.Overlaps(r))) return;
            var group = Group(c.root, "Park");
            c.B(_grass, 3f).Quad(r.xMin, r.xMax, r.yMin, r.yMax, 0.009f);
            // A gravel loop path and the way in from the alley (where row A leaves a gap at x = -46).
            var path = c.B(_gravel, 1.5f);
            path.Quad(r.xMin + 4f, r.xMax - 4f, r.yMin + 3f, r.yMin + 5f, 0.011f);
            path.Quad(r.xMin + 4f, r.xMax - 4f, r.yMax - 5f, r.yMax - 3f, 0.011f);
            path.Quad(r.xMin + 4f, r.xMin + 6f, r.yMin + 3f, r.yMax - 3f, 0.011f);
            path.Quad(r.xMax - 6f, r.xMax - 4f, r.yMin + 3f, r.yMax - 3f, 0.011f);
            path.Quad(-47f, -45f, c.dz + AlleyN1, r.yMin + 5f, 0.011f);
            var tree = AssetDatabase.LoadAssetAtPath<GameObject>(Houses + "tree_large.prefab");
            var small = AssetDatabase.LoadAssetAtPath<GameObject>(Houses + "tree_small.prefab");
            var rnd = new System.Random(17 + c.k);
            for (int i = 0; i < 14; i++)
            {
                float x = Mathf.Lerp(r.xMin + 8f, r.xMax - 8f, (float)rnd.NextDouble());
                float z = Mathf.Lerp(r.yMin + 8f, r.yMax - 8f, (float)rnd.NextDouble());
                var t = (GameObject)PrefabUtility.InstantiatePrefab(i % 3 == 0 ? small : tree, group);
                t.transform.SetPositionAndRotation(new Vector3(x, 0f, z), Quaternion.Euler(0f, rnd.Next(360), 0f));
                t.transform.localScale *= 1f + (float)rnd.NextDouble() * 0.5f;
            }
            Model(KayKit + "bench.obj", group, "ParkBench", new Vector3(r.xMin + 9f, 0f, r.yMin + 7f), 90f, 1.8f);
            Model(KayKit + "bench.obj", group, "ParkBench", new Vector3(r.xMax - 9f, 0f, r.yMax - 7f), 270f, 1.8f);
            Sign(group, "ParkSign", "ひばり公園", "Hibari Park", new Vector3(-43.5f, 0f, c.dz + AlleyN1 + 0.6f), 180f);
        }

        private static void Parking(Ctx c, Dictionary<string, GameObject> cars)
        {
            float z0 = c.dz + AlleyN1 + 10f, z1 = c.dz + 100f + AlleyS0 - 10f;
            var r = Rect.MinMaxRect(19.5f, z0, 62f, z1);
            if (c.reserved.Any(x => x.Overlaps(r))) return;
            var group = Group(c.root, "CoinParking");
            c.B(_parking, 4f).Quad(r.xMin, r.xMax, r.yMin, r.yMax, 0.009f);
            var paint = c.B(_paint, 1f);
            var keys = cars.Keys.ToArray();
            int i = 0;
            for (float x = r.xMin + 2f; x + 2.6f < r.xMax - 1f; x += 2.6f, i++)
            {
                paint.Quad(x, x + 0.1f, r.yMin + 1f, r.yMin + 6f, 0.016f);
                paint.Quad(x, x + 0.1f, r.yMax - 6f, r.yMax - 1f, 0.016f);
                if (i % 3 != 1 && keys.Length > 0)
                {
                    var car = (GameObject)PrefabUtility.InstantiatePrefab(cars[keys[Mod(i + c.k, keys.Length)]], group);
                    car.transform.SetPositionAndRotation(new Vector3(x + 1.3f, 0f, (i % 2 == 0 ? r.yMin + 3.6f : r.yMax - 3.6f)), Quaternion.Euler(0f, i % 2 == 0 ? 0f : 180f, 0f));
                    car.name = "ParkedCar_" + i;
                }
            }
            Sign(group, "ParkingSign", "P  コインパーキング", "Coin parking", new Vector3(r.xMin + 2f, 0f, r.yMin - 0.5f), 180f);
        }

        // ─────────────────────────── furniture ───────────────────────────

        private static void Crossing(Ctx c)
        {
            // Signals are built in Traffic(); here the pedestrian corner bollards.
            var group = Group(c.root, "CrossingBollards");
            foreach (float x in new[] { LaneW - 0.3f, LaneE + 0.3f })
                foreach (float z in new[] { AveN + 0.45f, AveS - 0.45f })
                {
                    var b = Prim(PrimitiveType.Cylinder, group, "Bollard", new Vector3(x, 0.45f, c.dz + z), new Vector3(0.14f, 0.45f, 0.14f), _signalBody);
                    Object.DestroyImmediate(b.GetComponent<Collider>());
                }
        }

        private static void Mirrors(Ctx c)
        {
            var group = Group(c.root, "CurveMirrors");
            Mirror(group, new Vector3(LaneE + 0.5f, 0f, c.dz + WalkS - 0.8f), 225f);
            Mirror(group, new Vector3(LaneW - 0.4f, 0f, c.dz + AlleyN1 + 0.6f), 135f);
        }

        /// <summary>カーブミラー: orange pole, round mirror with an orange rim, tilted to the street.</summary>
        private static void Mirror(Transform parent, Vector3 pos, float yaw)
        {
            var root = Group(parent, "CurveMirror");
            root.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var pole = Prim(PrimitiveType.Cylinder, root, "Pole", new Vector3(0f, 1.45f, 0f), new Vector3(0.09f, 1.45f, 0.09f), _mirrorPole);
            pole.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            var head = Group(root, "Head");
            head.localPosition = new Vector3(0f, 2.75f, 0.12f);
            head.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            var rim = Prim(PrimitiveType.Cylinder, head, "Rim", Vector3.zero, new Vector3(0.82f, 0.03f, 0.82f), _mirrorPole);
            rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var glass = Prim(PrimitiveType.Cylinder, head, "Glass", new Vector3(0f, 0f, 0.035f), new Vector3(0.72f, 0.01f, 0.72f), _mirror);
            glass.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            foreach (var col in root.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
            root.gameObject.AddComponent<CapsuleCollider>().center = new Vector3(0f, 1.4f, 0f);
            var cap = root.GetComponent<CapsuleCollider>(); cap.radius = 0.12f; cap.height = 2.8f;
        }

        private static void Vending(Ctx c)
        {
            var group = Group(c.root, "VendingMachines");
            VendingMachine(group, new Vector3(-13.8f, 0f, c.dz + WalkN + 0.45f), 180f);
            VendingMachine(group, new Vector3(-12.8f, 0f, c.dz + WalkN + 0.45f), 180f);
            VendingMachine(group, new Vector3(25.2f, 0f, c.dz + WalkS - 0.45f), 0f);
        }

        /// <summary>自動販売機: a body, a lit front of drink rows (generated texture), a coin strip.</summary>
        private static void VendingMachine(Transform parent, Vector3 pos, float yaw)
        {
            var root = Group(parent, "VendingMachine");
            root.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var body = Prim(PrimitiveType.Cube, root, "Body", new Vector3(0f, 0.92f, 0f), new Vector3(0.92f, 1.84f, 0.72f), _vendBody);
            var front = GameObject.CreatePrimitive(PrimitiveType.Quad);
            front.name = "Front"; Object.DestroyImmediate(front.GetComponent<Collider>());
            front.transform.SetParent(root, false);
            front.transform.localPosition = new Vector3(0f, 1.05f, 0.362f);
            front.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            front.transform.localScale = new Vector3(0.8f, 1.5f, 1f);
            front.GetComponent<Renderer>().sharedMaterial = _vendFront;
            var slot = Prim(PrimitiveType.Cube, root, "Slot", new Vector3(0f, 0.18f, 0.33f), new Vector3(0.6f, 0.2f, 0.1f), _iron);
            Object.DestroyImmediate(slot.GetComponent<Collider>());
        }

        private static void Manholes(Ctx c)
        {
            var group = Group(c.root, "Manholes");
            foreach (var (x, z) in new[] { (-40f, -8.4f), (22f, -11.6f), (50f, -8.4f), ((LaneW + LaneE) / 2f, -30f) })
            {
                var m = Prim(PrimitiveType.Cylinder, group, "Manhole", new Vector3(x, 0.013f, c.dz + z), new Vector3(0.7f, 0.004f, 0.7f), _iron);
                Object.DestroyImmediate(m.GetComponent<Collider>());
                m.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        // ─────────────────────────── traffic ───────────────────────────

        private static void Traffic(Ctx c, Dictionary<string, GameObject> cars)
        {
            var group = Group(c.root, "Traffic");
            var traffic = group.gameObject.AddComponent<StreetTraffic>();
            traffic.Configure(AveC + 1.6f, AveC - 1.6f, XMin + 0.5f, XMax - 0.5f, Rect.MinMaxRect(LaneW, AveS, LaneE, AveN), StopEastX, StopWestX);

            // Signals: horizontal car signals (青・黄・赤) on both sides, walking-figure signals at both ends.
            CarSignal(traffic, group, new Vector3(-1.6f, 0f, AveN + 0.7f), 270f);
            CarSignal(traffic, group, new Vector3(8.6f, 0f, AveS - 0.7f), 90f);
            var chirp = WalkSignal(traffic, group, new Vector3(LaneE + 0.25f, 0f, AveN + 0.9f), 180f);
            WalkSignal(traffic, group, new Vector3(LaneW - 0.25f, 0f, AveS - 0.9f), 0f);
            traffic.SetChirp(chirp);

            var keys = cars.Keys.Where(k => k != "car_police").ToArray();
            var rnd = new System.Random(5 + c.k);
            for (int i = 0; i < 6; i++)
            {
                int dir = i < 3 ? 1 : -1;
                float x = Mathf.Lerp(XMin + 6f, XMax - 6f, (i % 3 + (float)rnd.NextDouble() * 0.6f) / 3f);
                var prefab = cars[keys[Mod(i + c.k, keys.Length)]];
                var car = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                car.name = (dir > 0 ? "Car_East_" : "Car_West_") + i;
                car.transform.localPosition = new Vector3(x, 0f, dir > 0 ? AveC + 1.6f : AveC - 1.6f);
                car.transform.localRotation = Quaternion.Euler(0f, dir > 0 ? 90f : 270f, 0f);
                var wheels = car.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("WheelPivot")).ToArray();
                traffic.AddCar(car.transform, wheels, dir, 6.2f + (float)rnd.NextDouble() * 1.6f);
            }
        }

        private static void CarSignal(StreetTraffic traffic, Transform parent, Vector3 pos, float yaw)
        {
            var root = Group(parent, "CarSignal");
            root.localPosition = pos; root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Prim(PrimitiveType.Cylinder, root, "Pole", new Vector3(0f, 2.3f, 0f), new Vector3(0.14f, 2.3f, 0.14f), _signalBody);
            var arm = Prim(PrimitiveType.Cube, root, "Arm", new Vector3(0f, 4.4f, 1.2f), new Vector3(0.08f, 0.08f, 2.4f), _signalBody);
            Object.DestroyImmediate(arm.GetComponent<Collider>());
            var box = Prim(PrimitiveType.Cube, root, "Housing", new Vector3(0f, 4.4f, 2.2f), new Vector3(0.28f, 0.42f, 1.25f), _signalBody);
            Object.DestroyImmediate(box.GetComponent<Collider>());
            var lamps = new[] { (_green, StreetTraffic.Phase.CarGreen, 2.6f), (_yellow, StreetTraffic.Phase.CarYellow, 2.2f), (_red, StreetTraffic.Phase.AllRed1, 1.8f) };
            foreach (var (mat, phase, z) in lamps)
            {
                var lamp = Prim(PrimitiveType.Cylinder, root, "Lamp_" + phase, new Vector3(-0.15f, 4.4f, z), new Vector3(0.32f, 0.02f, 0.32f), _lampOff);
                lamp.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Object.DestroyImmediate(lamp.GetComponent<Collider>());
                traffic.AddLamp(lamp.GetComponent<Renderer>(), phase, mat, _lampOff);
            }
            var hood = root.GetComponentsInChildren<Collider>();
            foreach (var h in hood) if (h.name != "Pole") Object.DestroyImmediate(h);
        }

        private static AudioSource WalkSignal(StreetTraffic traffic, Transform parent, Vector3 pos, float yaw)
        {
            var root = Group(parent, "WalkSignal");
            root.localPosition = pos; root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Prim(PrimitiveType.Cylinder, root, "Pole", new Vector3(0f, 1.4f, 0f), new Vector3(0.1f, 1.4f, 0.1f), _signalBody);
            var box = Prim(PrimitiveType.Cube, root, "Housing", new Vector3(0f, 2.55f, 0.12f), new Vector3(0.36f, 0.72f, 0.2f), _signalBody);
            Object.DestroyImmediate(box.GetComponent<Collider>());
            foreach (var (mat, phase, y) in new[] { (_walkRed, StreetTraffic.Phase.AllRed2, 2.74f), (_walkGreen, StreetTraffic.Phase.WalkGreen, 2.36f) })
            {
                var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
                face.name = "Walk_" + phase; Object.DestroyImmediate(face.GetComponent<Collider>());
                face.transform.SetParent(root, false);
                face.transform.localPosition = new Vector3(0f, y, 0.225f);
                face.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                face.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
                face.GetComponent<Renderer>().sharedMaterial = _lampOff;
                traffic.AddLamp(face.GetComponent<Renderer>(), phase, mat, LampOffFor(mat));
            }
            var audio = root.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false; audio.spatialBlend = 1f; audio.minDistance = 3f; audio.maxDistance = 30f; audio.volume = 0.5f;
            return audio;
        }

        private static Material LampOffFor(Material lit) => lit == _walkGreen ? Mat("WalkGreenOff", WalkTexture(new Color(0.12f, 0.2f, 0.16f), true), new Color(0.6f, 0.6f, 0.6f), 0f)
            : Mat("WalkRedOff", WalkTexture(new Color(0.22f, 0.1f, 0.1f), false), new Color(0.6f, 0.6f, 0.6f), 0f);

        // ─────────────────────────── cars ───────────────────────────

        /// <summary>KayKit cars as prefabs: body scaled to ~4.3 m, each wheel under a pivot at its centre so it can spin.</summary>
        private static Dictionary<string, GameObject> CarPrefabs()
        {
            var result = new Dictionary<string, GameObject>();
            foreach (string name in new[] { "car_sedan", "car_taxi", "car_hatchback", "car_stationwagon", "car_police" })
            {
                var bodySrc = AssetDatabase.LoadAssetAtPath<GameObject>(KayKit + name + ".obj");
                if (bodySrc == null) continue;
                var root = new GameObject(name);
                var model = Group(root.transform, "Model");
                var body = (GameObject)PrefabUtility.InstantiatePrefab(bodySrc, model);
                PrefabUtility.UnpackPrefabInstance(body, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (string w in new[] { "front_left", "front_right", "rear_left", "rear_right" })
                {
                    var wheelSrc = AssetDatabase.LoadAssetAtPath<GameObject>($"{KayKit}{name}_wheel_{w}.obj");
                    if (wheelSrc == null) continue;
                    var wheel = (GameObject)PrefabUtility.InstantiatePrefab(wheelSrc);
                    PrefabUtility.UnpackPrefabInstance(wheel, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    var wb = RendererBounds(wheel.transform);
                    var pivot = Group(model, "WheelPivot_" + w);
                    pivot.position = wb.center;
                    wheel.transform.SetParent(pivot, true);
                }
                IslandBuilder.ConvertMaterials(root);
                foreach (var col in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
                var b = RendererBounds(root.transform);
                float length = Mathf.Max(b.size.x, b.size.z);
                model.localScale = Vector3.one * (4.3f / Mathf.Max(0.01f, length));
                // Face +z and sit on the ground at the origin.
                b = RendererBounds(root.transform);
                if (b.size.x > b.size.z) { model.localRotation = Quaternion.Euler(0f, 90f, 0f); b = RendererBounds(root.transform); }
                model.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
                b = RendererBounds(root.transform);
                var box = root.AddComponent<BoxCollider>();
                box.center = b.center; box.size = b.size;
                var rb = root.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
                foreach (var r in root.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
                result[name] = PrefabUtility.SaveAsPrefabAsset(root, $"{Gen}/Cars/{name}.prefab");
                Object.DestroyImmediate(root);
            }
            return result;
        }

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
