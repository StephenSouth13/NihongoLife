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
    /// Fourth pass of 65_PhuQuocIsland (after the island, downtown and resort builders): everything that makes the
    /// 1:1 Dương Đông area feel lived in. All under GanhNgon_Restaurant/PQ_Life (local metres, +z inland, sea at -z):
    ///   • Traffic: cars, taxis, vans, a bus, scooters, motorbikes and bicycles on waypoint lanes (PhuQuocTraffic),
    ///     traffic lights at both intersections;
    ///   • Gánh Ngon guests who walk in, sit, order, eat and leave (GanhNgonCustomers, hooked to the shift);
    ///   • Đường 30/4 north of the shophouses: hotels, guesthouses, homestays, convenience stores, cafés, pharmacy,
    ///     bank, mini-mart, Chợ Dương Đông and the island bus stop whose board lists all 31 map places;
    ///   • the beach: golden sand with a gentle slope into a shader sea, wet sand, foam line, shells, starfish,
    ///     pebbles, rocks, crabs, palms, parasols, a promenade, the Dinh Cậu night-market stalls and two small
    ///     beach resorts; beach-goers walking the promenade;
    ///   • the sea shader on every sea plane, the PQ_Skybox material, overview labels/markers hidden over the 1:1 town;
    ///   • static batching flags and LOD culling on every static prop.
    /// CC0 only: Kenney kits, Pixor (vehicles, shells, see Docs/PHU_QUOC_ASSETS.md), ambientCG, Poly Haven.
    /// </summary>
    public static class PhuQuocLifeBuilder
    {
        public const string RootName = "PQ_Life";
        public const string MatDir = "Assets/NihongoLife/Materials/PhuQuocLife";
        public const string Pixor = PhuQuocIslandBuilder.AssetRoot + "/pixor_cc0";
        public const string SeaShaderName = "NihongoLife/SeaWater";
        public const string SkyboxPath = "Assets/NihongoLife/Materials/PhuQuoc/PQ_Skybox.mat";
        private const string Cars = PhuQuocIslandBuilder.AssetRoot + "/kenney_car-kit/FBX/";
        private const string Nature = PhuQuocIslandBuilder.AssetRoot + "/kenney_nature-kit/FBX/";
        private const string Commercial = PhuQuocIslandBuilder.AssetRoot + "/kenney_city-kit-commercial";
        private const string Suburban = "Assets/ThirdParty/Kenney/kenney_city-kit-suburban_20";
        private const string Furniture = "Assets/ThirdParty/Kenney/kenney_furniture-kit/Models/FBX format/";
        private const string Minis = PhuQuocIslandBuilder.AssetRoot + "/kenney_mini-characters";
        private const string Holiday = PhuQuocIslandBuilder.AssetRoot + "/kenney_holiday-kit";
        private const string SandTex = PhuQuocIslandBuilder.AssetRoot + "/ambientcg_Ground054/Ground054_1K-JPG_Color.jpg";
        private const string SandNrm = PhuQuocIslandBuilder.AssetRoot + "/ambientcg_Ground054/Ground054_1K-JPG_NormalGL.jpg";
        private const string SkyTex = PhuQuocIslandBuilder.AssetRoot + "/polyhaven_sky/kloofendal_48d_partly_cloudy_puresky_1k.hdr";

        // Street geometry from the downtown builder.
        private const float RoadTop = PhuQuocDowntownBuilder.RoadTop;
        private const float HalfLen = PhuQuocDowntownBuilder.HalfLength;
        private static float[] CrossX => PhuQuocDowntownBuilder.CrossX;
        public const float NorthStreetZ0 = 18f, NorthStreetZ1 = 26f, NorthLotFront = 27.5f;
        public const float BeachStartZ = -40f, PromenadeZ = -46f;

        public static readonly string[] Hotels =
        {
            "Khách sạn Biển Xanh ★★★|ホテル", "Nhà nghỉ Hải Âu|ゲストハウス", "Cửa hàng tiện lợi 24h|コンビニ", "Cà phê Phố Biển|カフェ",
            "Homestay Sao Biển|ホームステイ", "Khách sạn Hoàng Hôn ★★★★|ホテル", "Chợ Dương Đông|ズオンドン市場", "Nhà thuốc|薬局",
            "Ngân hàng · ATM|銀行・ATM", "Siêu thị mini|ミニスーパー", "Nhà nghỉ Ngọc Trai|ゲストハウス", "Cà phê muối|塩コーヒー",
            "Khách sạn Ngọc Biển ★★★|ホテル", "Bến xe buýt · Tour đảo|バス停・島ツアー", "Cửa hàng tiện lợi 24h|コンビニ", "Homestay Rạch Tràm|ホームステイ",
        };

        public static void Build()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(PhuQuocIslandBuilder.ScenePath, OpenSceneMode.Single);
                var island = GameObject.Find("PhuQuocIsland");
                if (island == null) throw new InvalidOperationException("Run PhuQuocIslandBuilder.Build first");
                var restaurant = island.transform.Find("GanhNgon_Restaurant");
                if (restaurant == null || restaurant.Find(PhuQuocDowntownBuilder.RootName) == null)
                    throw new InvalidOperationException("Run PhuQuocDowntownBuilder.Build first");
                BuildInto(island.transform, restaurant);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + PhuQuocIslandBuilder.ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("[PhuQuocLife] built");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[PhuQuocLife] " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        public static Transform BuildInto(Transform island, Transform restaurant)
        {
            var old = restaurant.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject(RootName).transform;
            root.SetParent(restaurant, false);
            var m = new Mats();
            NorthStreet(root, m);
            Beach(root, m);
            NightMarket(root, m);
            BeachResorts(root, m);
            Traffic(root, m);
            Customers(root, restaurant);
            Walkers(root);
            SeaEverywhere(island, m);
            HideOverviewNearTown(island, restaurant);
            root.gameObject.AddComponent<PhuQuocSkybox>().Sky = Skybox();
            Optimise(root);
            return root;
        }

        /// <summary>Batch diagnostic: logs every renderer under PQ_Life whose bounds exceed 40 m (catches mis-scaled models).</summary>
        public static void Diagnose()
        {
            EditorSceneManager.OpenScene(PhuQuocIslandBuilder.ScenePath, OpenSceneMode.Single);
            var root = GameObject.Find(RootName);
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                if (r.bounds.size.magnitude > 40f && !r.name.StartsWith("PQ_Sea") && r.name != "GoldenSand" && r.name != "WetSand" && r.name != "Foam")
                    Debug.Log("[PQBig] " + AnimationUtility.CalculateTransformPath(r.transform, root.transform) + " " + r.bounds.size + " mat " + r.sharedMaterial?.name);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        // ───────────────────────── helpers ─────────────────────────
        private static GameObject Asset(string folderOrFile, string name = null)
        {
            if (name == null) return AssetDatabase.LoadAssetAtPath<GameObject>(folderOrFile);
            foreach (var guid in AssetDatabase.FindAssets(name, new[] { folderOrFile }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var file = Path.GetFileNameWithoutExtension(path);
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (file == name && (ext == ".fbx" || ext == ".obj")) return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return null;
        }

        private static GameObject PixorModel(string n) => Asset(Pixor + "/" + n + "/pq_" + n + ".obj");

        /// <summary>Instantiate a model in a wrapper whose pivot sits on the ground, scaled so its largest horizontal
        /// extent (or height when byHeight) equals size; the wrapper's +z is the model's long axis.</summary>
        private static Transform Fit(Transform parent, GameObject asset, string name, Vector3 pos, float yaw, float size, bool byHeight = false, float modelYaw = 0f)
        {
            var wrap = new GameObject(name).transform;
            wrap.SetParent(parent, false);
            if (asset == null) return wrap;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            inst.transform.SetParent(wrap, false);
            inst.transform.localRotation = Quaternion.Euler(0, modelYaw, 0);
            var b = LocalBounds(wrap, inst);
            float measure = byHeight ? b.size.y : Mathf.Max(b.size.x, b.size.z);
            float s = size / Mathf.Max(0.001f, measure);
            inst.transform.localScale *= s;
            b = LocalBounds(wrap, inst);
            inst.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);
            wrap.localPosition = pos;
            wrap.localRotation = Quaternion.Euler(0, yaw, 0);
            foreach (var c in inst.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            return wrap;
        }

        private static Bounds LocalBounds(Transform space, GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            var b = new Bounds(Vector3.zero, Vector3.zero); bool any = false;
            foreach (var r in rs)
            {
                var mf = r.GetComponent<MeshFilter>();
                var mesh = mf != null ? mf.sharedMesh : (r as SkinnedMeshRenderer)?.sharedMesh;
                if (mesh == null) continue;
                foreach (var v in Corners(mesh.bounds))
                {
                    var p = space.InverseTransformPoint(r.transform.TransformPoint(v));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            return b;
        }

        private static IEnumerable<Vector3> Corners(Bounds b)
        {
            for (int i = 0; i < 8; i++)
                yield return new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
        }

        /// <summary>Pixor OBJ materials → shared URP Lit materials with the .mtl diffuse colours.</summary>
        private static void PixorMaterials(Transform t, string n, Mats m)
        {
            var colours = m.PixorColours(n);
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string key = mats[i] != null ? mats[i].name : "default";
                    if (!colours.TryGetValue(key, out var c)) c = colours.Count > 0 ? colours.Values.First() : Color.gray;
                    mats[i] = ZoneBuildKit.Lit(MatDir + "/Pixor", "PX_" + n + "_" + Safe(key), c, 0.35f);
                }
                r.sharedMaterials = mats;
            }
        }

        private static string Safe(string s) => new string(s.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());

        private static GameObject Slab(Transform parent, string name, Vector3 centreBottom, Vector3 size, Material mat, bool collider = true)
            => ZoneBuildKit.Block(parent, name, centreBottom + Vector3.up * size.y * 0.5f, size, mat, collider);

        private static void Sign(Transform parent, string text, Vector3 pos, float yaw, float width, Material board)
        {
            var parts = text.Split('|');
            ZoneBuildKit.Block(parent, "SignBoard", pos, new Vector3(width, 1.1f, 0.1f), board, false, yaw);
            var q = Quaternion.Euler(0, yaw, 0);
            ZoneBuildKit.Text(parent, "Sign", parts[0] + (parts.Length > 1 ? "\n<size=60%>" + parts[1] + "</size>" : ""),
                pos + q * new Vector3(0, 0, -0.07f), yaw, 0.42f, new Color(1f, 0.95f, 0.8f), width - 0.3f);
        }

        // ───────────────────────── Đường 30/4: hotels, guesthouses, shops ─────────────────────────
        private static void NorthStreet(Transform root, Mats m)
        {
            var g = ZoneBuildKit.Group(root, "Duong30Thang4");
            float zc = (NorthStreetZ0 + NorthStreetZ1) * 0.5f, w = NorthStreetZ1 - NorthStreetZ0;
            Slab(g, "Road", new Vector3(0, 0, zc), new Vector3(HalfLen * 2f, RoadTop, w), m.asphalt);
            for (float x = -HalfLen + 2f; x < HalfLen; x += 6f)
                if (!CrossX.Any(c => Mathf.Abs(x - c) < 6f))
                    Slab(g, "Dash", new Vector3(x, RoadTop, zc), new Vector3(3f, 0.01f, 0.15f), m.paint, false);
            Slab(g, "Pavement", new Vector3(0, 0, NorthStreetZ1 + 0.75f), new Vector3(HalfLen * 2f, 0.22f, 1.5f), m.pavement);
            foreach (var cx in CrossX)
                Slab(g, "CrossLink_" + cx, new Vector3(cx, 0, (17f + NorthStreetZ0) * 0.5f), new Vector3(9f, RoadTop, NorthStreetZ0 - 17f + 0.4f), m.asphalt);
            ZoneBuildKit.Text(g, "StreetName", "Đường 30 Tháng 4\n<size=60%>30/4通り</size>", new Vector3(-HalfLen + 6f, 2.6f, NorthStreetZ1 + 0.4f), 0f, 0.4f, Color.white, 5f);

            string[] facades = { "building-skyscraper-a", "building-type-c", "building-b", "building-e", "building-type-f", "building-skyscraper-b", null,
                "building-g", "building-h", "building-i", "building-type-k", "building-j", "building-skyscraper-c", null, "building-c", "building-type-m" };
            int i = 0;
            for (float x = -HalfLen + 6f; x < HalfLen - 4f && i < Hotels.Length; x += 9.5f)
            {
                if (CrossX.Any(c => Mathf.Abs(x - c) < 8f)) continue;
                var lot = new GameObject("Place_" + i).transform; lot.SetParent(g, false);
                lot.localPosition = new Vector3(x, 0.22f, NorthLotFront);
                string label = Hotels[i], model = facades[i];
                if (model == null && label.StartsWith("Chợ")) Market(lot, m);
                else if (model == null) BusStop(lot, m);
                else
                {
                    bool tall = model.Contains("skyscraper"), house = model.StartsWith("building-type");
                    var asset = Asset(house ? Suburban : Commercial, model);
                    var b = Fit(lot, asset, "Building", new Vector3(0, 0, 5f), 180f, tall ? 8.8f : 8.6f);
                    float hgt = ZoneBuildKit.BoundsOf(b.gameObject).size.y, cap = tall ? 26f : 12f;
                    if (hgt > cap) b.localScale = Vector3.one * (cap / hgt);
                    Sign(lot, label, new Vector3(0, house ? 3.2f : 3.8f, -0.4f), 0f, 6.5f, m.Board(i));
                }
                lot.gameObject.AddComponent<PhuQuocPlaceTag>().Set(label.Split('|')[0], label.Split('|')[1]);
                i++;
            }
        }

        private static void Market(Transform lot, Mats m)
        {
            Slab(lot, "Floor", new Vector3(0, 0, 5f), new Vector3(10f, 0.05f, 9f), m.tile);
            foreach (var p in new[] { new Vector2(-4.6f, 1), new Vector2(4.6f, 1), new Vector2(-4.6f, 9), new Vector2(4.6f, 9) })
                ZoneBuildKit.Cylinder(lot, "Post", new Vector3(p.x, 2.2f, p.y), new Vector3(0.3f, 2.2f, 0.3f), m.wood, true);
            ZoneBuildKit.Block(lot, "Roof", new Vector3(0, 4.5f, 5f), new Vector3(11f, 0.3f, 10f), m.roof, false);
            for (int s = 0; s < 6; s++)
            {
                var at = new Vector3(-3f + (s % 3) * 3f, 0, 3f + (s / 3) * 4f);
                ZoneBuildKit.Block(lot, "Stall_" + s, at + Vector3.up * 0.45f, new Vector3(2.2f, 0.9f, 1.2f), m.Board(s + 3), true);
            }
            Sign(lot, "Chợ Dương Đông|ズオンドン市場", new Vector3(0, 3.6f, -0.2f), 0f, 7f, m.Board(1));
        }

        private static void BusStop(Transform lot, Mats m)
        {
            Slab(lot, "Platform", new Vector3(0, 0, 2f), new Vector3(9f, 0.05f, 3.5f), m.tile);
            ZoneBuildKit.Block(lot, "Shelter", new Vector3(0, 2.6f, 2.6f), new Vector3(6f, 0.15f, 2.4f), m.roof, false);
            foreach (var x in new[] { -2.8f, 2.8f }) ZoneBuildKit.Cylinder(lot, "Post", new Vector3(x, 1.3f, 3.6f), new Vector3(0.12f, 1.3f, 0.12f), m.metal, true);
            Fit(lot, Asset(PhuQuocIslandBuilder.AssetRoot + "/kenney_holiday-kit", "bench"), "Bench", new Vector3(0, 0, 3.2f), 180f, 2.2f);
            // Destination board: every place on the island map (the 1:100 overview), so nothing on PHU_QUOC.md is missing.
            var places = PhuQuocMap.Load().places;
            var lines = places.Select(p => "• " + p.nameVi).ToList();
            ZoneBuildKit.Block(lot, "Board", new Vector3(0, 2.4f, 6.2f), new Vector3(8.5f, 3.4f, 0.12f), m.Board(0), true);
            ZoneBuildKit.Text(lot, "Destinations", "<b>Tuyến xe buýt đảo · 島内バス</b>\n<size=70%>" + string.Join("   ", lines) + "</size>",
                new Vector3(0, 2.4f, 6.12f), 0f, 0.22f, Color.white, 8.2f);
            Sign(lot, "Bến xe buýt · Tour đảo|バス停・島ツアー", new Vector3(0, 3.1f, 1.3f), 0f, 6f, m.Board(2));
        }

        // ───────────────────────── beach ─────────────────────────
        /// <summary>Coast line (z where sand meets water) along the 1:1 beach; turns north to meet the resort beach.</summary>
        public static float CoastZ(float x)
            => Mathf.Lerp(-74f + 2.5f * Mathf.Sin(x * 0.07f), -40f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(112f, 142f, x)));

        /// <summary>Sand height: dry sand rises gently inland, the wet slope dips under the sea (y -0.02).</summary>
        public static float SandHeight(float x, float z)
        {
            float d = z - CoastZ(x);
            if (d >= 0f) return 0.03f + 0.14f * Mathf.SmoothStep(0f, 1f, d / 26f) + 0.03f * Mathf.Sin(x * 0.35f) * Mathf.Sin(z * 0.27f) * Mathf.Clamp01(d / 4f);
            return Mathf.Max(-1.2f, 0.03f + d * 0.05f);
        }

        public const float SeaLevel = -0.02f, BeachX0 = -112f, BeachX1 = 142f;

        private static void Beach(Transform root, Mats m)
        {
            var g = ZoneBuildKit.Group(root, "PQ_BeachDetail");
            // Sand surface.
            var sand = MeshGrid("GoldenSand", BeachX0, BeachX1, -110f, BeachStartZ, 2f, (x, z) => SandHeight(x, z), 0.25f);
            Attach(g, sand, m.sand, true);
            // Wet sand and foam ribbons along the water line.
            Attach(g, Ribbon("WetSand", -0.6f, 3.2f, 0.004f), m.wetSand, false);
            Attach(g, Ribbon("Foam", -2.4f, -0.5f, 0.0f, true), m.foam, false);
            // Sea (shader) in front of the town; the resort keeps its own sea plane.
            var sea = new Mesh { name = "PQ_SeaLocal" };
            sea.vertices = new[] { new Vector3(-420, SeaLevel, BeachStartZ), new Vector3(BeachX1, SeaLevel, BeachStartZ), new Vector3(-420, SeaLevel, -620), new Vector3(BeachX1, SeaLevel, -620) };
            sea.triangles = new[] { 0, 1, 2, 2, 1, 3 };
            sea.normals = Enumerable.Repeat(Vector3.up, 4).ToArray();
            sea.RecalculateBounds();
            SaveMesh(sea);
            var localSea = Attach(g, sea, m.sea, false);
            localSea.name = "PQ_SeaLocal";
            WaterSurfaceBuilder.Prepare(localSea.gameObject);

            // Promenade along the top of the beach.
            Slab(g, "Promenade", new Vector3((BeachX0 + 110f) * 0.5f, 0.1f, PromenadeZ), new Vector3(110f - BeachX0, 0.15f, 4f), m.tile);
            for (float x = BeachX0 + 4f; x < 108f; x += 12f)
                if (!CrossX.Any(c => Mathf.Abs(x - c) < 4f))
                {
                    ZoneBuildKit.Cylinder(g, "PromLamp", new Vector3(x, 1.85f, PromenadeZ - 2.2f), new Vector3(0.1f, 1.6f, 0.1f), m.metal, true);
                    ZoneBuildKit.Block(g, "PromLampHead", new Vector3(x, 3.55f, PromenadeZ - 2.2f), new Vector3(0.4f, 0.2f, 0.4f), m.lamp, false);
                }
            foreach (var cx in CrossX) // cross streets end at the beach: short ramps
                Slab(g, "BeachRamp_" + cx, new Vector3(cx, 0.02f, -49.5f), new Vector3(8f, 0.12f, 4f), m.tile);

            var rnd = new System.Random(6565);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var detail = ZoneBuildKit.Group(g, "Detail");
            var shell = PixorModel("seashell"); var star = PixorModel("starfish"); var pebbles = PixorModel("pebbles");
            var rock = PixorModel("rock-small"); var crab = PixorModel("crab");
            var rocksK = new[] { Asset(Nature + "rock_smallA.fbx"), Asset(Nature + "rock_smallC.fbx"), Asset(Nature + "rock_smallFlatB.fbx") };
            int n = 0;
            void Scatter(GameObject asset, string pix, int count, float minD, float maxD, float s0, float s1, bool byHeight)
            {
                if (asset == null) return;
                for (int k = 0; k < count; k++)
                {
                    float x = R(BeachX0 + 4f, 108f), d = R(minD, maxD), z = CoastZ(x) + d;
                    if (z > BeachStartZ - 2f || z > PromenadeZ - 3f && z < PromenadeZ + 3f) continue;
                    var t = Fit(detail, asset, (pix ?? asset?.name ?? "Prop") + "_" + n++, new Vector3(x, SandHeight(x, z) - 0.02f, z), R(0, 360), R(s0, s1), byHeight);
                    if (pix != null) PixorMaterials(t, pix, m);
                }
            }
            Scatter(shell, "seashell", 70, -0.3f, 10f, 0.08f, 0.16f, false);
            Scatter(star, "starfish", 28, -0.5f, 6f, 0.14f, 0.24f, false);
            Scatter(pebbles, "pebbles", 30, 0.5f, 14f, 0.35f, 0.7f, false);
            Scatter(rock, "rock-small", 18, -1.5f, 12f, 0.3f, 0.8f, false);
            foreach (var rk in rocksK) Scatter(rk, null, 8, -2.5f, 3f, 0.5f, 1.3f, false);
            Scatter(crab, "crab", 6, 0.5f, 5f, 0.22f, 0.3f, false);

            // Palms on the dry sand, parasols + loungers for the public beach.
            var palms = new[] { Asset(Nature + "tree_palmTall.fbx"), Asset(Nature + "tree_palmBend.fbx"), Asset(Nature + "tree_palmDetailedTall.fbx") };
            int p = 0;
            for (float x = BeachX0 + 5f; x < 105f; x += R(6f, 10f))
            {
                if (CrossX.Any(c => Mathf.Abs(x - c) < 6f)) continue;
                float z = Mathf.Min(PromenadeZ - 4f, CoastZ(x) + R(16f, 24f));
                Fit(detail, palms[p % palms.Length], "Palm_" + p++, new Vector3(x, SandHeight(x, z) - 0.05f, z), R(0, 360), R(6.5f, 9.5f), true);
            }
            var parasol = Asset(Commercial, "detail-parasol-a"); var parasolB = Asset(Commercial, "detail-parasol-b");
            var lounger = Asset(Furniture + "loungeChairRelax.fbx");
            for (int k = 0; k < 12; k++)
            {
                float x = -100f + k * 9f + R(-1f, 1f);
                if (CrossX.Any(c => Mathf.Abs(x - c) < 5f)) continue;
                float z = CoastZ(x) + 9f;
                Fit(detail, k % 2 == 0 ? parasol : parasolB, "Parasol_" + k, new Vector3(x, SandHeight(x, z) - 0.05f, z), 0f, 2.6f, true);
                Fit(detail, lounger, "Lounger_" + k + "a", new Vector3(x - 0.8f, SandHeight(x, z - 1.4f), z - 1.4f), 180f, 1.9f);
                Fit(detail, lounger, "Lounger_" + k + "b", new Vector3(x + 0.8f, SandHeight(x, z - 1.4f), z - 1.4f), 180f, 1.9f);
            }
        }

        private static Mesh MeshGrid(string name, float x0, float x1, float z0, float z1, float step, Func<float, float, float> h, float uvPerMetre)
        {
            int nx = Mathf.CeilToInt((x1 - x0) / step) + 1, nz = Mathf.CeilToInt((z1 - z0) / step) + 1;
            var v = new Vector3[nx * nz]; var uv = new Vector2[v.Length];
            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    float x = Mathf.Min(x1, x0 + i * step), z = Mathf.Min(z1, z0 + j * step);
                    v[j * nx + i] = new Vector3(x, h(x, z), z);
                    uv[j * nx + i] = new Vector2(x, z) * uvPerMetre;
                }
            var t = new List<int>();
            for (int j = 0; j < nz - 1; j++)
                for (int i = 0; i < nx - 1; i++)
                {
                    int a = j * nx + i, b = a + 1, c = a + nx, d = c + 1;
                    t.AddRange(new[] { a, c, b, b, c, d });
                }
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = v; mesh.uv = uv; mesh.triangles = t.ToArray();
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            SaveMesh(mesh);
            return mesh;
        }

        /// <summary>A strip following the coast between d0 and d1 metres from the water line, hugging the sand.</summary>
        private static Mesh Ribbon(string name, float d0, float d1, float lift, bool flatAtSea = false)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            int k = 0;
            for (float x = BeachX0; x <= BeachX1; x += 1.5f, k++)
            {
                float za = CoastZ(x) + d0, zb = CoastZ(x) + d1;
                float ya = flatAtSea ? SeaLevel + 0.012f : SandHeight(x, za) + lift, yb = flatAtSea ? SeaLevel + 0.012f : SandHeight(x, zb) + lift;
                v.Add(new Vector3(x, ya, za)); v.Add(new Vector3(x, yb, zb));
                uv.Add(new Vector2(x * 0.25f, 0)); uv.Add(new Vector2(x * 0.25f, 1));
                if (k > 0) { int a = (k - 1) * 2; t.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3 }); }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            SaveMesh(mesh);
            return mesh;
        }

        private static void SaveMesh(Mesh mesh)
        {
            ZoneBuildKit.EnsureFolder(MatDir + "/Meshes");
            string path = MatDir + "/Meshes/" + mesh.name + ".asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
        }

        private static GameObject Attach(Transform parent, Mesh mesh, Material mat, bool collider)
        {
            var go = new GameObject(mesh.name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        // ───────────────────────── night market + beach resorts ─────────────────────────
        private static readonly string[] Stalls =
        {
            "Hải sản nướng|焼き海鮮", "Ngọc trai|真珠", "Nước mắm · Tiêu|魚醤・胡椒", "Kem cuộn|ロールアイス", "Rượu sim|シム酒",
            "Bánh tráng nướng|ベトナムピザ", "Sinh tố|スムージー", "Quà lưu niệm|お土産",
        };

        private static void NightMarket(Transform root, Mats m)
        {
            var g = ZoneBuildKit.Group(root, "ChoDem_DinhCau");
            ZoneBuildKit.Text(g, "Gate", "CHỢ ĐÊM DINH CẬU\n<size=55%>ズィンカウ夜市 · Night market</size>", new Vector3(62f, 4.2f, PromenadeZ + 1.8f), 0f, 0.6f, new Color(1f, 0.85f, 0.4f), 10f);
            var lantern = Asset(Holiday, "lantern-hanging");
            for (int i = 0; i < Stalls.Length; i++)
            {
                var s = new GameObject("Stall_" + i).transform; s.SetParent(g, false);
                s.localPosition = new Vector3(36f + i * 7f, 0.17f, PromenadeZ + 3.6f);
                ZoneBuildKit.Block(s, "Counter", new Vector3(0, 0.5f, 0), new Vector3(3.2f, 1f, 1.4f), m.wood, true);
                foreach (var x in new[] { -1.6f, 1.6f }) ZoneBuildKit.Cylinder(s, "Pole", new Vector3(x, 1.3f, 0.6f), new Vector3(0.08f, 1.3f, 0.08f), m.metal, false);
                ZoneBuildKit.Block(s, "Canopy", new Vector3(0, 2.65f, 0.1f), new Vector3(3.8f, 0.1f, 2.4f), m.Board(i), false);
                Sign(s, Stalls[i], new Vector3(0, 2.2f, -0.75f), 0f, 3.2f, m.Board(i + 2));
                Fit(s, lantern, "Lantern", new Vector3(0, 1.9f, -1.0f), 0f, 0.5f, true);
                ZoneBuildKit.PointLight(s, "Glow", new Vector3(0, 2f, -1.2f), 5f, 0.8f, new Color(1f, 0.7f, 0.4f));
            }
        }

        private static void BeachResorts(Transform root, Mats m)
        {
            var g = ZoneBuildKit.Group(root, "BeachResorts");
            (string name, float x)[] resorts = { ("Resort Hàng Dừa|ハンズア・リゾート", -95f), ("Sea Breeze Resort|シーブリーズ・リゾート", -66f) };
            int n = 0;
            foreach (var (name, x0) in resorts)
            {
                var r = new GameObject("Resort_" + n).transform; r.SetParent(g, false); r.localPosition = new Vector3(x0, 0, 0);
                Sign(r, name, new Vector3(0, 2.8f, PromenadeZ + 2.3f), 0f, 7f, m.Board(5 + n));
                for (int b = 0; b < 3; b++)
                {
                    float bx = -8f + b * 8f, bz = PromenadeZ - 8f - (b % 2) * 3f;
                    float y = SandHeight(x0 + bx, bz);
                    var bung = new GameObject("Bungalow_" + b).transform; bung.SetParent(r, false); bung.localPosition = new Vector3(bx, y, bz);
                    ZoneBuildKit.Block(bung, "Deck", new Vector3(0, 0.3f, 0), new Vector3(5f, 0.2f, 5f), m.wood, true);
                    ZoneBuildKit.Block(bung, "Walls", new Vector3(0, 1.6f, 0.4f), new Vector3(3.6f, 2.4f, 3.4f), m.plaster, true);
                    var roof = ZoneBuildKit.Cylinder(bung, "Roof", new Vector3(0, 3.4f, 0.4f), new Vector3(5.6f, 0.9f, 5.6f), m.thatch, false);
                    roof.GetComponent<MeshFilter>().sharedMesh = Pyramid();
                    roof.transform.localScale = new Vector3(5.4f, 1.8f, 5.0f);
                }
                n++;
            }
        }

        private static Mesh _pyramid;
        private static Mesh Pyramid()
        {
            if (_pyramid != null) return _pyramid;
            var v = new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(-0.5f, 0, 0.5f), new Vector3(0, 1, 0) };
            var verts = new List<Vector3>(); var tris = new List<int>();
            int[][] faces = { new[] { 0, 4, 1 }, new[] { 1, 4, 2 }, new[] { 2, 4, 3 }, new[] { 3, 4, 0 } };
            foreach (var f in faces) { foreach (var i in f) { tris.Add(verts.Count); verts.Add(v[i]); } }
            _pyramid = new Mesh { name = "PQ_Pyramid" };
            _pyramid.SetVertices(verts); _pyramid.SetTriangles(tris, 0); _pyramid.RecalculateNormals(); _pyramid.RecalculateBounds();
            SaveMesh(_pyramid);
            return _pyramid;
        }

        // ───────────────────────── traffic ─────────────────────────
        public static PhuQuocTraffic.Lane[] TrafficLanes()
        {
            var lanes = new List<PhuQuocTraffic.Lane>();
            float y = RoadTop;
            float L = HalfLen + 2f;
            // Drive on the right: eastbound (+x) uses the south half, westbound the north half; motorbikes by the kerb.
            foreach (var (z, east) in new[] { (-17.2f, true), (-19.8f, true), (-12.8f, false), (-10.2f, false) })
            {
                var a = new Vector3(east ? -L : L, y, z); var b = new Vector3(east ? L : -L, y, z);
                var stops = CrossX.Select(cx => east ? (cx - 6f) - a.x : a.x - (cx + 6f)).OrderBy(s => s).ToArray();
                lanes.Add(new PhuQuocTraffic.Lane { start = a, end = b, axis = TrafficRules.AxisEastWest, stops = stops });
            }
            foreach (var cx in CrossX)
            {
                // Southbound on the west half (x - 2.2), northbound on the east half.
                lanes.Add(new PhuQuocTraffic.Lane { start = new Vector3(cx - 2.2f, y, 17f), end = new Vector3(cx - 2.2f, y, -50f), axis = TrafficRules.AxisNorthSouth, stops = new[] { 17f - (-7.5f) } });
                lanes.Add(new PhuQuocTraffic.Lane { start = new Vector3(cx + 2.2f, y, -50f), end = new Vector3(cx + 2.2f, y, 17f), axis = TrafficRules.AxisNorthSouth, stops = new[] { -22.5f - (-50f) } });
            }
            return lanes.ToArray();
        }

        private static void Traffic(Transform root, Mats m)
        {
            var g = ZoneBuildKit.Group(root, "Traffic");
            var lanes = TrafficLanes();
            var bodies = new List<Transform>(); var laneOf = new List<int>(); var s0 = new List<float>(); var vmax = new List<float>(); var len = new List<float>();
            var riders = new[] { "character-male-a", "character-female-b", "character-male-c", "character-female-d", "character-male-e", "character-female-f" };
            int n = 0;
            void Add(int lane, string kind, float at)
            {
                Transform t; float l, v;
                switch (kind)
                {
                    case "bus": t = Fit(g, PixorModel("bus"), "Bus_" + n, Vector3.zero, 0, 10.5f); PixorMaterials(t, "bus", m); l = 10.5f; v = 7f; break;
                    case "scooter": case "motorbike": case "bicycle":
                        t = Fit(g, PixorModel(kind), kind + "_" + n, Vector3.zero, 0, kind == "bicycle" ? 1.7f : 1.9f); PixorMaterials(t, kind, m);
                        var rider = Fit(t, Asset(Minis, riders[n % riders.Length]), "Rider", new Vector3(0, kind == "bicycle" ? 0.3f : 0.36f, -0.15f), 0f, 1.12f, true);
                        rider.localRotation = Quaternion.identity;
                        l = 1.9f; v = kind == "bicycle" ? 4.5f : 7.5f; break;
                    default: t = Fit(g, Asset(Cars + kind + ".fbx"), kind + "_" + n, Vector3.zero, 0, kind == "truck" || kind == "delivery" ? 5.6f : 4.4f); l = 4.6f; v = 9f; break;
                }
                bodies.Add(t); laneOf.Add(lane); vmax.Add(v); len.Add(l);
                s0.Add(at * Vector3.Distance(lanes[lane].start, lanes[lane].end)); n++;
            }
            string[][] mix =
            {
                new[] { "sedan", "bus", "taxi", "suv", "van" },
                new[] { "scooter", "motorbike", "scooter", "scooter", "motorbike", "bicycle", "scooter" },
                new[] { "taxi", "delivery", "sedan", "truck", "suv" },
                new[] { "motorbike", "scooter", "scooter", "motorbike", "scooter", "bicycle", "scooter" },
                new[] { "taxi", "scooter", "scooter" }, new[] { "scooter", "sedan", "motorbike" },
                new[] { "van", "scooter", "motorbike" }, new[] { "scooter", "taxi", "scooter" },
            };
            for (int lane = 0; lane < mix.Length; lane++)
                for (int k = 0; k < mix[lane].Length; k++) Add(lane, mix[lane][k], (k + 0.3f) / mix[lane].Length);

            // Traffic lights: one head per approach at both intersections (index 0 red, 1 yellow, 2 green).
            var lamps = new List<Renderer>(); var axis = new List<int>();
            var lg = ZoneBuildKit.Group(g, "TrafficLights");
            foreach (var cx in CrossX)
            {
                var heads = new[]
                {
                    (new Vector3(cx - 6.6f, 0.22f, -22.4f), TrafficRules.AxisEastWest),
                    (new Vector3(cx + 6.6f, 0.22f, -7.6f), TrafficRules.AxisEastWest),
                    (new Vector3(cx - 5.4f, 0.22f, -6.8f), TrafficRules.AxisNorthSouth),
                    (new Vector3(cx + 5.4f, 0.22f, -23.2f), TrafficRules.AxisNorthSouth),
                };
                foreach (var (pos, ax) in heads)
                {
                    var h = new GameObject("Signal_" + lamps.Count / 3).transform; h.SetParent(lg, false); h.localPosition = pos;
                    ZoneBuildKit.Cylinder(h, "Pole", new Vector3(0, 1.6f, 0), new Vector3(0.14f, 1.6f, 0.14f), m.metal, true);
                    ZoneBuildKit.Block(h, "Housing", new Vector3(0, 3.35f, 0), new Vector3(0.4f, 1.05f, 0.4f), m.dark, false);
                    for (int k = 0; k < 3; k++)
                    {
                        var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        Object.DestroyImmediate(s.GetComponent<Collider>());
                        s.name = new[] { "Red", "Yellow", "Green" }[k];
                        s.transform.SetParent(h, false);
                        s.transform.localPosition = new Vector3(0, 3.67f - k * 0.32f, 0);
                        s.transform.localScale = Vector3.one * 0.44f;
                        var r = s.GetComponent<Renderer>(); r.sharedMaterial = m.lampOff[k];
                        lamps.Add(r);
                    }
                    axis.Add(ax);
                }
            }
            var traffic = g.gameObject.AddComponent<PhuQuocTraffic>();
            traffic.Configure(lanes, bodies.ToArray(), laneOf.ToArray(), s0.ToArray(), vmax.ToArray(), len.ToArray(), lamps.ToArray(), axis.ToArray(), m.lampOn, m.lampOff);
            // Start positions so the scene (and screenshots) look right before Play.
            for (int i = 0; i < bodies.Count; i++)
            {
                var lane = lanes[laneOf[i]];
                float L = Vector3.Distance(lane.start, lane.end);
                bodies[i].localPosition = Vector3.Lerp(lane.start, lane.end, s0[i] / L);
                bodies[i].localRotation = Quaternion.LookRotation(lane.end - lane.start);
            }
        }

        // ───────────────────────── Gánh Ngon guests, walkers ─────────────────────────
        private static void Customers(Transform root, Transform restaurant)
        {
            var seats = new List<Vector3>(); var facing = new List<Vector3>();
            for (int i = 1; i <= 4; i++)
            {
                var t = restaurant.Find("Table_" + i);
                var c = t != null ? t.localPosition : new Vector3(-3 + (i - 1) * 2, 0, 0);
                seats.Add(Floor(restaurant, c + new Vector3(0, 0, 0.75f))); facing.Add(Vector3.back);
                seats.Add(Floor(restaurant, c + new Vector3(0, 0, -0.75f))); facing.Add(Vector3.forward);
            }
            var go = new GameObject("GanhNgon_Guests");
            go.transform.SetParent(root.parent, false); // restaurant space, same as the stations
            go.transform.SetParent(root, true);
            go.AddComponent<GanhNgonCustomers>().Configure(seats.ToArray(), facing.ToArray(), Floor(restaurant, new Vector3(0, 0, -3f)),
                new[] { Floor(restaurant, new Vector3(-16f, 0f, -6f)), Floor(restaurant, new Vector3(16f, 0f, -6f)) });
        }

        /// <summary>Local point dropped onto the first solid collider below (pavilion floor, pavement).</summary>
        private static Vector3 Floor(Transform space, Vector3 local)
        {
            Physics.SyncTransforms();
            var top = space.TransformPoint(local + Vector3.up * 4f);
            if (Physics.Raycast(top, Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore))
                local.y = space.InverseTransformPoint(hit.point).y;
            return local;
        }

        private static void Walkers(Transform root)
        {
            var go = new GameObject("BeachAndPlaceWalkers");
            go.transform.SetParent(root, false);
            go.AddComponent<PhuQuocStreetLife>().Configure(new[]
            {
                new Vector3(-100f, 0.25f, PromenadeZ + 0.8f), new Vector3(100f, 0.25f, PromenadeZ + 0.8f),
                new Vector3(-90f, 0.25f, PromenadeZ - 0.8f), new Vector3(30f, 0.25f, PromenadeZ - 0.8f),
                new Vector3(-80f, 0.25f, NorthStreetZ1 + 0.8f), new Vector3(80f, 0.25f, NorthStreetZ1 + 0.8f),
                new Vector3(-60f, 0.2f, CoastZ(-60f) + 4f), new Vector3(60f, 0.2f, CoastZ(60f) + 4f),
            }, 10);
        }

        // ───────────────────────── sea, sky, overview ─────────────────────────
        private static void SeaEverywhere(Transform island, Mats m)
        {
            foreach (var r in island.GetComponentsInChildren<Renderer>(true))
            {
                var n = r.gameObject.name;
                if (n == "Sea" || n == "HarbourWater") r.sharedMaterial = m.sea;
            }
        }

        /// <summary>The 1:100 overview markers (Dương Đông, the night market, Dinh Cậu…) sit on top of the 1:1 town; hide those.</summary>
        public const float OverviewHideRadius = 9f;
        private static void HideOverviewNearTown(Transform island, Transform restaurant)
        {
            var places = island.Find("Places");
            if (places == null) return;
            var c = restaurant.position;
            foreach (Transform marker in places)
            {
                var d = marker.position - c; d.y = 0f;
                if (d.magnitude < OverviewHideRadius) marker.gameObject.SetActive(false);
            }
        }

        private static Material Skybox()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture>(SkyTex);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Skybox/Panoramic"));
                ZoneBuildKit.EnsureFolder(Path.GetDirectoryName(SkyboxPath).Replace('\\', '/'));
                AssetDatabase.CreateAsset(mat, SkyboxPath);
            }
            mat.shader = Shader.Find("Skybox/Panoramic");
            if (tex != null) mat.SetTexture("_MainTex", tex);
            mat.SetFloat("_Exposure", 1.1f);
            mat.SetColor("_Tint", new Color(0.55f, 0.6f, 0.65f));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void Optimise(Transform root)
        {
            var moving = new HashSet<Transform>();
            foreach (var t in root.GetComponentsInChildren<PhuQuocTraffic>(true)) moving.Add(t.transform);
            foreach (var t in root.GetComponentsInChildren<GanhNgonCustomers>(true)) moving.Add(t.transform);
            foreach (var t in root.GetComponentsInChildren<PhuQuocStreetLife>(true)) moving.Add(t.transform);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (moving.Any(mv => t == mv || t.IsChildOf(mv))) continue;
                if (t.GetComponent<TextMeshPro>() != null || t.GetComponent<Light>() != null) continue;
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            }
            // LOD culling: small beach props disappear first, buildings and palms much later.
            foreach (var group in new[] { "PQ_BeachDetail/Detail", "Duong30Thang4", "ChoDem_DinhCau", "BeachResorts" })
            {
                var parent = root.Find(group);
                if (parent == null) continue;
                foreach (Transform item in parent)
                {
                    var rs = item.GetComponentsInChildren<Renderer>(true);
                    if (rs.Length == 0) continue;
                    var lod = item.gameObject.AddComponent<LODGroup>();
                    float size = ZoneBuildKit.BoundsOf(item.gameObject).size.magnitude;
                    float cull = size < 1f ? 0.02f : size < 4f ? 0.008f : 0.003f;
                    lod.SetLODs(new[] { new LOD(cull, rs) });
                    lod.RecalculateBounds();
                }
            }
        }

        // ───────────────────────── materials ─────────────────────────
        private sealed class Mats
        {
            public readonly Material asphalt, paint, pavement, tile, wood, roof, metal, lamp, dark, plaster, thatch, sand, wetSand, foam, sea;
            public readonly Material[] lampOn = new Material[3], lampOff = new Material[3];
            private readonly Dictionary<string, Dictionary<string, Color>> _pixor = new Dictionary<string, Dictionary<string, Color>>();
            private static readonly Color[] SignColours =
            {
                new Color(0.75f, 0.15f, 0.12f), new Color(0.1f, 0.35f, 0.6f), new Color(0.1f, 0.5f, 0.35f), new Color(0.85f, 0.5f, 0.1f),
                new Color(0.45f, 0.2f, 0.55f), new Color(0.15f, 0.45f, 0.5f), new Color(0.6f, 0.35f, 0.15f), new Color(0.2f, 0.25f, 0.35f),
            };

            public Mats()
            {
                asphalt = AssetDatabase.LoadAssetAtPath<Material>("Assets/NihongoLife/Materials/PhuQuoc/PQ_Asphalt.mat") ?? L("PQL_Asphalt", new Color(0.2f, 0.21f, 0.23f), 0.15f);
                paint = L("PQL_Paint", new Color(0.95f, 0.95f, 0.9f), 0.2f);
                pavement = L("PQL_Pavement", new Color(0.72f, 0.68f, 0.62f), 0.1f);
                tile = L("PQL_Tile", new Color(0.82f, 0.74f, 0.62f), 0.2f);
                wood = L("PQL_Wood", new Color(0.55f, 0.37f, 0.22f), 0.15f);
                roof = L("PQL_Roof", new Color(0.62f, 0.28f, 0.18f), 0.1f);
                metal = L("PQL_Metal", new Color(0.3f, 0.32f, 0.35f), 0.5f);
                dark = L("PQL_Dark", new Color(0.08f, 0.08f, 0.09f), 0.3f);
                lamp = ZoneBuildKit.Lit(MatDir, "PQL_Lamp", new Color(1f, 0.92f, 0.7f), 0.3f, new Color(1f, 0.85f, 0.55f) * 1.5f);
                plaster = L("PQL_Plaster", new Color(0.95f, 0.92f, 0.85f), 0.1f);
                thatch = L("PQL_Thatch", new Color(0.62f, 0.5f, 0.3f), 0.05f);
                sand = L("PQ_GoldenSand", new Color(1f, 0.86f, 0.6f), 0.05f);
                var st = AssetDatabase.LoadAssetAtPath<Texture2D>(SandTex);
                if (st != null) { sand.SetTexture("_BaseMap", st); sand.SetColor("_BaseColor", new Color(1.12f, 0.95f, 0.66f)); }
                var sn = AssetDatabase.LoadAssetAtPath<Texture2D>(SandNrm);
                if (sn != null) { sand.SetTexture("_BumpMap", sn); sand.EnableKeyword("_NORMALMAP"); }
                wetSand = L("PQ_WetSand", new Color(0.7f, 0.58f, 0.4f), 0.55f);
                if (st != null) wetSand.SetTexture("_BaseMap", st);
                foam = ZoneBuildKit.Lit(MatDir, "PQ_Foam", new Color(1f, 1f, 1f, 0.55f), 0.6f, null, true);
                sea = SeaMaterial();
                Color[] on = { new Color(1f, 0.1f, 0.05f), new Color(1f, 0.75f, 0.05f), new Color(0.1f, 1f, 0.3f) };
                for (int k = 0; k < 3; k++)
                {
                    lampOn[k] = ZoneBuildKit.Lit(MatDir, "PQL_Signal" + k + "_On", on[k], 0.6f, on[k] * 3f);
                    lampOff[k] = L("PQL_Signal" + k + "_Off", on[k] * 0.18f, 0.6f);
                }
                foreach (var mm in new[] { sand, wetSand }) EditorUtility.SetDirty(mm);
            }

            private static Material SeaMaterial()
            {
                ZoneBuildKit.EnsureFolder(MatDir);
                string path = MatDir + "/PQ_SeaWater.mat";
                var shader = Shader.Find(SeaShaderName);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
                if (shader != null) mat.shader = shader;
                EditorUtility.SetDirty(mat);
                return mat;
            }

            public Material Board(int i) => L("PQL_Board" + (i % SignColours.Length), SignColours[i % SignColours.Length], 0.3f);
            private static Material L(string name, Color c, float s) => ZoneBuildKit.Lit(MatDir, name, c, s);

            public Dictionary<string, Color> PixorColours(string n)
            {
                if (_pixor.TryGetValue(n, out var d)) return d;
                d = new Dictionary<string, Color>();
                string mtl = Pixor + "/" + n + "/pq_" + n + ".mtl";
                if (File.Exists(mtl))
                {
                    string current = null;
                    foreach (var line in File.ReadAllLines(mtl))
                    {
                        var p = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (p.Length >= 2 && p[0] == "newmtl") current = p[1];
                        else if (p.Length >= 4 && p[0] == "Kd" && current != null)
                            d[current] = new Color(F(p[1]), F(p[2]), F(p[3]));
                    }
                }
                _pixor[n] = d;
                return d;
            }

            private static float F(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
