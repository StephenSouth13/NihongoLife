using System;
using System.Collections.Generic;
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
    /// Dương Đông downtown at 1:1 around Gánh Ngon (113 Trần Hưng Đạo), inside 65_PhuQuocIsland.
    /// The island stays the 1:100 overview built by PhuQuocIslandBuilder (AGENTS.md: never 1:1 for the whole island);
    /// this adds the walkable street block the player arrives in: Trần Hưng Đạo running straight east–west with
    /// two lanes, kerbs, pavements, two cross streets with zebras (stop lines 2 m before), shophouse rows on a fixed
    /// lot grid (every façade on one building line), palms and street lamps on a fixed pitch, Bạch Đằng night market
    /// at the west end and Dinh Cậu / the fishing harbour at the east end, and the street-life walkers.
    /// Gánh Ngon is an open-front pavilion so its eight GanhNgonSpot stations (from the island builder) stay usable.
    /// Rebuild only through this builder (headless):
    ///   Unity -batchmode -executeMethod NihongoLife.EditorTools.PhuQuocDowntownBuilder.Build
    /// Run PhuQuocIslandBuilder.Build first when the island itself changes, then this.
    /// </summary>
    public static class PhuQuocDowntownBuilder
    {
        public const string RootName = "DuongDong_Downtown";
        private const string MatDir = "Assets/NihongoLife/Materials/PhuQuoc";
        private const string Kit = PhuQuocIslandBuilder.AssetRoot;

        // Street grid (local to GanhNgon_Restaurant, metres; +x east, +z north / inland).
        public const float HalfLength = 84f;           // Trần Hưng Đạo block: 168 m
        public const float NorthKerb = -3.5f;          // north pavement: z -3.5 … -9 (the arrival spawn is at z -6)
        public const float RoadNorth = -9f, RoadSouth = -21f; // 12 m carriageway, two lanes
        public const float SouthKerb = -26.5f;
        public const float CrossHalfWidth = 4.5f;      // cross streets: 9 m wide
        public static readonly float[] CrossX = { -44f, 44f };
        public const float LotWidth = 6f, LotDepth = 12f;
        public const float PavementTop = 0.22f, RoadTop = 0.08f;

        private static readonly string[] ShopNames =
        {
            "Nước mắm Phú Quốc|魚醤 フーコック", "Tiêu Phú Quốc|フーコック胡椒", "Ngọc trai|真珠", "Cà phê|カフェ",
            "Bánh canh ghẹ|カニのうどん", "Bún quậy|ブン・クアイ", "Gỏi cá trích|ニシンの和え物", "Rượu sim|シム酒",
            "Hải sản tươi|新鮮な海鮮", "Tiệm thuốc|薬局", "Đổi tiền|両替", "Thuê xe máy|バイクレンタル",
            "Tạp hoá|雑貨店", "Phở|フォー", "Tiệm may|仕立て屋", "Nhà sách|本屋", "Bánh mì|バインミー", "Kem dừa|ココナッツアイス",
        };

        private static readonly string[] Facades =
        {
            "building-a", "building-b", "building-c", "building-d", "building-e", "building-g", "building-h",
            "building-i", "building-j", "building-k", "building-l", "building-m", "building-n",
        };

        public static void Build()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(PhuQuocIslandBuilder.ScenePath, OpenSceneMode.Single);
                var island = GameObject.Find("PhuQuocIsland");
                if (island == null) throw new InvalidOperationException("Run PhuQuocIslandBuilder.Build first");
                var restaurant = island.transform.Find("GanhNgon_Restaurant");
                if (restaurant == null) throw new InvalidOperationException("GanhNgon_Restaurant missing");
                BuildInto(restaurant, true);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + PhuQuocIslandBuilder.ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("[PhuQuocDowntownBuilder] Dương Đông downtown built.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1); else throw;
            }
        }

        /// <summary>Replaces (never stacks) the downtown under the restaurant. withModels=false: primitives only.</summary>
        public static Transform BuildInto(Transform restaurant, bool withModels)
        {
            var old = restaurant.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject(RootName).transform;
            root.SetParent(restaurant, false);

            var m = new Mats(withModels);
            Streets(root, m);
            ShopRows(root, m, withModels);
            GanhNgonPavilion(root, m);
            StreetFurniture(root, m, withModels);
            NightMarket(root, m, withModels);
            DinhCauHarbour(root, m, withModels);
            Signs(root);

            var life = new GameObject("StreetLife");
            life.transform.SetParent(root, false);
            var walkers = life.AddComponent<NihongoLife.PhuQuoc.PhuQuocStreetLife>();
            walkers.Configure(Lanes(), 10);
            return root;
        }

        /// <summary>Walking lanes (local to the downtown root): both pavements end to end and the night-market lane.</summary>
        public static Vector3[] Lanes() => new[]
        {
            new Vector3(-HalfLength + 4f, PavementTop, (NorthKerb + RoadNorth) * 0.5f - 1.2f), new Vector3(HalfLength - 4f, PavementTop, (NorthKerb + RoadNorth) * 0.5f - 1.2f),
            new Vector3(-HalfLength + 4f, PavementTop, (RoadSouth + SouthKerb) * 0.5f), new Vector3(HalfLength - 4f, PavementTop, (RoadSouth + SouthKerb) * 0.5f),
            new Vector3(-HalfLength - 14f, RoadTop, -40f), new Vector3(-HalfLength - 14f, RoadTop, 12f),
        };

        // ── Ground ──
        private static void Streets(Transform root, Mats m)
        {
            var g = ZoneBuildKit.Group(root, "Streets");
            float len = HalfLength * 2f;
            Slab(g, "Road_TranHungDao", new Vector3(0, 0, (RoadNorth + RoadSouth) * 0.5f), new Vector3(len, RoadTop, RoadNorth - RoadSouth), m.asphalt);
            Slab(g, "Pavement_North", new Vector3(0, 0, (NorthKerb + RoadNorth) * 0.5f), new Vector3(len, PavementTop, NorthKerb - RoadNorth), m.pavement);
            Slab(g, "Pavement_South", new Vector3(0, 0, (RoadSouth + SouthKerb) * 0.5f), new Vector3(len, PavementTop, RoadSouth - SouthKerb), m.pavement);
            Slab(g, "Kerb_North", new Vector3(0, 0, RoadNorth + 0.15f), new Vector3(len, PavementTop + 0.02f, 0.3f), m.kerb);
            Slab(g, "Kerb_South", new Vector3(0, 0, RoadSouth - 0.15f), new Vector3(len, PavementTop + 0.02f, 0.3f), m.kerb);
            // Lot ground behind the building lines (so nothing floats over the 1:100 island).
            Slab(g, "Lots_North", new Vector3(0, 0, NorthKerb + 10f), new Vector3(len, RoadTop, 20f), m.lot);
            Slab(g, "Lots_South", new Vector3(0, 0, SouthKerb - 10f), new Vector3(len, RoadTop, 20f), m.lot);

            // Centre line (dashed), 3 m dash / 3 m gap, skipping the junctions.
            float mid = (RoadNorth + RoadSouth) * 0.5f;
            for (float x = -HalfLength + 1.5f; x < HalfLength - 1.5f; x += 6f)
            {
                if (CrossX.Any(c => Mathf.Abs(x - c) < CrossHalfWidth + 5f)) continue;
                Paint(g, "Dash", new Vector3(x, RoadTop, mid), new Vector3(3f, 0.012f, 0.15f), m.paint);
            }
            foreach (var cx in CrossX)
            {
                string name = cx < 0 ? "Cross_BachDang" : "Cross_NguyenTrai";
                Slab(g, name, new Vector3(cx, 0, 2f), new Vector3(CrossHalfWidth * 2f, RoadTop, 30f), m.asphalt);
                Slab(g, name + "_S", new Vector3(cx, 0, SouthKerb - 12f), new Vector3(CrossHalfWidth * 2f, RoadTop + 0.005f, 24f), m.asphalt);
                // Zebras run along the road (bars parallel to the traffic), stop lines 2 m before them.
                foreach (float side in new[] { -1f, 1f })
                {
                    float zx = cx + side * (CrossHalfWidth + 2.2f);
                    for (float z = RoadSouth + 0.6f; z < RoadNorth - 0.4f; z += 1.1f)
                        Paint(g, "Zebra", new Vector3(zx, RoadTop, z), new Vector3(3f, 0.012f, 0.5f), m.paint);
                    Paint(g, "StopLine", new Vector3(zx + side * 3.5f, RoadTop, side > 0 ? (RoadSouth + mid) * 0.5f : (RoadNorth + mid) * 0.5f),
                        new Vector3(0.3f, 0.012f, 5.6f), m.paint);
                }
            }
        }

        // ── Shophouses: one building line, fixed lots ──
        private static void ShopRows(Transform root, Mats m, bool withModels)
        {
            var g = ZoneBuildKit.Group(root, "Shophouses");
            int i = 0;
            foreach (int row in new[] { 1, -1 })
            {
                float front = row > 0 ? NorthKerb : SouthKerb;
                for (float x = -HalfLength + LotWidth * 0.5f; x < HalfLength; x += LotWidth)
                {
                    if (row > 0 && Mathf.Abs(x) < 9f) continue;                       // Gánh Ngon frontage
                    if (CrossX.Any(c => Mathf.Abs(x - c) < CrossHalfWidth + LotWidth * 0.5f)) continue;
                    var lot = new GameObject("Lot_" + (row > 0 ? "N" : "S") + "_" + i).transform;
                    lot.SetParent(g, false);
                    lot.localPosition = new Vector3(x, RoadTop, front + row * LotDepth * 0.5f);
                    lot.localRotation = Quaternion.Euler(0, row > 0 ? 0f : 180f, 0);   // local -z (façade) faces the street
                    float height = 7f + (i * 37 % 5) * 1.6f;                        // 2–4 storey nhà ống
                    Shophouse(lot, Facades[i % Facades.Length], height, m, withModels, i);
                    var name = ShopNames[i % ShopNames.Length].Split('|');
                    var sign = ZoneBuildKit.Text(lot, "ShopSign", name[0] + "\n<size=55%>" + name[1] + "</size>",
                        new Vector3(0, 3.4f, -LotDepth * 0.5f - 0.12f), 0f, 0.55f, Color.white, LotWidth - 0.6f);
                    sign.transform.localRotation = Quaternion.identity;
                    ZoneBuildKit.Block(lot, "SignBoard", new Vector3(0, 3.4f, -LotDepth * 0.5f - 0.05f), new Vector3(LotWidth - 0.4f, 1.1f, 0.08f), m.Sign(i), true);
                    // Awning over the pavement (has a collider so the camera never passes through it).
                    ZoneBuildKit.Block(lot, "Awning", new Vector3(0, 2.75f, -LotDepth * 0.5f - 1.1f), new Vector3(LotWidth - 0.2f, 0.08f, 2.2f), m.Awning(i), true);
                    i++;
                }
            }
        }

        private static void Shophouse(Transform lot, string model, float height, Mats m, bool withModels, int i)
        {
            GameObject prefab = withModels ? AssetDatabase.LoadAssetAtPath<GameObject>(Kit + "/kenney_city-kit-commercial/FBX/" + model + ".fbx") : null;
            if (prefab != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, lot);
                inst.name = "Facade_" + model;
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.Euler(0, 180f, 0); // Kenney fronts face +z; the lot's front is -z
                var b = ZoneBuildKit.BoundsOf(inst);
                float fit = Mathf.Min((LotWidth - 0.1f) / Mathf.Max(0.01f, b.size.x), (LotDepth - 0.2f) / Mathf.Max(0.01f, b.size.z));
                inst.transform.localScale = Vector3.one * fit;
                b = ZoneBuildKit.BoundsOf(inst);
                // Sit on the ground, front flush with the building line.
                var local = lot.InverseTransformPoint(b.center);
                inst.transform.localPosition -= new Vector3(local.x, lot.InverseTransformPoint(b.min).y, 0f);
                var col = lot.gameObject.AddComponent<BoxCollider>();
                col.center = new Vector3(0, b.size.y * 0.5f, 0); col.size = new Vector3(LotWidth - 0.1f, b.size.y, LotDepth - 0.2f);
            }
            else
            {
                ZoneBuildKit.Block(lot, "Body", new Vector3(0, height * 0.5f, 0), new Vector3(LotWidth - 0.1f, height, LotDepth - 0.2f), m.Wall(i), true);
                for (int f = 1; f < height / 3.2f; f++)
                    ZoneBuildKit.Block(lot, "Balcony_" + f, new Vector3(0, f * 3.2f + 0.5f, -LotDepth * 0.5f - 0.45f), new Vector3(LotWidth - 1f, 0.12f, 0.9f), m.kerb, true);
            }
        }

        // ── Gánh Ngon: open-front pavilion over the eight stations ──
        private static void GanhNgonPavilion(Transform root, Mats m)
        {
            var g = ZoneBuildKit.Group(root, "GanhNgon_Pavilion");
            Slab(g, "Floor", new Vector3(0, 0, 2.5f), new Vector3(16f, 0.14f, 12f), m.tile);
            foreach (var p in new[] { new Vector2(-7.6f, -3.2f), new Vector2(7.6f, -3.2f), new Vector2(-7.6f, 8.2f), new Vector2(7.6f, 8.2f), new Vector2(0, 8.2f) })
                ZoneBuildKit.Cylinder(g, "Post", new Vector3(p.x, 1.9f, p.y), new Vector3(0.35f, 1.9f, 0.35f), m.wood, true);
            ZoneBuildKit.Block(g, "Roof", new Vector3(0, 4.0f, 2.5f), new Vector3(17f, 0.25f, 13f), m.roof, true);
            ZoneBuildKit.Block(g, "BackWall", new Vector3(0, 2f, 8.6f), new Vector3(16f, 4f, 0.25f), m.Wall(3), true);
            ZoneBuildKit.Block(g, "SignBoard", new Vector3(0, 4.8f, -3.4f), new Vector3(9f, 1.3f, 0.12f), m.brand, true);
            ZoneBuildKit.Text(g, "Sign", "GÁNH NGON\n<size=45%>113 Trần Hưng Đạo · ガンゴン食堂</size>", new Vector3(0, 4.8f, -3.5f), 0f, 0.8f, new Color(1f, 0.93f, 0.7f), 8.6f);
            for (int i = 0; i < 6; i++) // lanterns along the eave
            {
                var l = ZoneBuildKit.Cylinder(g, "Lantern", new Vector3(-6.5f + i * 2.6f, 3.4f, -3.1f), new Vector3(0.45f, 0.3f, 0.45f), m.lantern, false);
                if (i % 2 == 0) ZoneBuildKit.PointLight(l.transform, "Glow", Vector3.zero, 6f, 1.2f, new Color(1f, 0.55f, 0.3f));
            }
        }

        // ── Palms, lamps, motorbike bays on a fixed pitch ──
        private static void StreetFurniture(Transform root, Mats m, bool withModels)
        {
            var g = ZoneBuildKit.Group(root, "StreetFurniture");
            var palm = withModels ? AssetDatabase.LoadAssetAtPath<GameObject>(Kit + "/kenney_nature-kit/FBX/tree_palmTall.fbx") : null;
            int n = 0;
            foreach (float z in new[] { RoadNorth + 0.9f, RoadSouth - 0.9f })
                for (float x = -HalfLength + 6f; x < HalfLength - 3f; x += 12f)
                {
                    if (CrossX.Any(c => Mathf.Abs(x - c) < CrossHalfWidth + 4.5f)) continue;
                    var at = new Vector3(x, PavementTop, z);
                    if (palm != null)
                    {
                        var p = (GameObject)PrefabUtility.InstantiatePrefab(palm, g);
                        p.name = "Palm_" + n; p.transform.localPosition = at; p.transform.localRotation = Quaternion.Euler(0, n * 47f, 0);
                        var b = ZoneBuildKit.BoundsOf(p); p.transform.localScale = Vector3.one * (7.5f / Mathf.Max(0.1f, b.size.y));
                    }
                    else ZoneBuildKit.Cylinder(g, "Palm_" + n, at + Vector3.up * 3.5f, new Vector3(0.35f, 3.5f, 0.35f), m.wood, false);
                    var trunk = ZoneBuildKit.Cylinder(g, "PalmTrunkCollider_" + n, at + Vector3.up, new Vector3(0.4f, 1f, 0.4f), m.wood, true);
                    trunk.GetComponent<Renderer>().enabled = false;
                    // Lamp half way to the next palm.
                    var lampAt = at + new Vector3(6f, 0, 0);
                    if (lampAt.x < HalfLength - 2f && !CrossX.Any(c => Mathf.Abs(lampAt.x - c) < CrossHalfWidth + 1f))
                    {
                        ZoneBuildKit.Cylinder(g, "LampPost_" + n, lampAt + Vector3.up * 2.6f, new Vector3(0.14f, 2.6f, 0.14f), m.metal, true);
                        var head = ZoneBuildKit.Block(g, "LampHead_" + n, lampAt + new Vector3(0, 5.25f, z > -15f ? -0.7f : 0.7f), new Vector3(0.4f, 0.18f, 1.4f), m.lamp, false);
                        if (n % 3 == 0) ZoneBuildKit.PointLight(head.transform, "Light", Vector3.down * 0.4f, 12f, 1f, new Color(1f, 0.85f, 0.6f));
                    }
                    n++;
                }
        }

        // ── Bạch Đằng night market (west end) ──
        private static void NightMarket(Transform root, Mats m, bool withModels)
        {
            var g = ZoneBuildKit.Group(root, "ChoDem_BachDang");
            float x0 = -HalfLength - 14f;
            Slab(g, "MarketLane", new Vector3(x0, 0, -14f), new Vector3(24f, RoadTop, 64f), m.pavement);
            ZoneBuildKit.Text(g, "Gate", "CHỢ ĐÊM PHÚ QUỐC\n<size=50%>ナイトマーケット · Night market · Ночной рынок</size>", new Vector3(x0, 5.6f, 16.9f), 0f, 1f, new Color(1f, 0.85f, 0.35f), 14f);
            ZoneBuildKit.Block(g, "GateBeam", new Vector3(x0, 5.6f, 17.1f), new Vector3(15f, 1.6f, 0.2f), m.brand, true);
            foreach (float s in new[] { -1f, 1f }) ZoneBuildKit.Cylinder(g, "GatePost", new Vector3(x0 + s * 7.3f, 2.8f, 17.1f), new Vector3(0.4f, 2.8f, 0.4f), m.brand, true);
            var parasol = withModels ? AssetDatabase.LoadAssetAtPath<GameObject>(Kit + "/kenney_city-kit-commercial/FBX/detail-parasol-a.fbx") : null;
            string[] stalls = { "Hải sản nướng", "Nhum nướng", "Gỏi cá trích", "Ốc", "Kem cuộn", "Ngọc trai", "Bánh tráng nướng", "Nước mía", "Mực một nắng", "Đồ lưu niệm", "Trái cây", "Rượu sim" };
            int k = 0;
            foreach (float side in new[] { -1f, 1f })
                for (float z = 12f; z > -40f; z -= 4.5f, k++)
                {
                    var st = new GameObject("Stall_" + k).transform; st.SetParent(g, false);
                    st.localPosition = new Vector3(x0 + side * 6.5f, RoadTop, z);
                    st.localRotation = Quaternion.Euler(0, side > 0 ? 90f : -90f, 0);
                    ZoneBuildKit.Block(st, "Counter", new Vector3(0, 0.45f, 0), new Vector3(3.2f, 0.9f, 1.4f), m.wood, true);
                    if (parasol != null)
                    {
                        var p = (GameObject)PrefabUtility.InstantiatePrefab(parasol, st);
                        var b = ZoneBuildKit.BoundsOf(p); p.transform.localScale = Vector3.one * (2.7f / Mathf.Max(0.1f, b.size.y));
                        p.transform.localPosition = new Vector3(0, 0, 0.9f);
                    }
                    ZoneBuildKit.Text(st, "Name", stalls[k % stalls.Length], new Vector3(0, 1.45f, -0.75f), 0f, 0.35f, Color.white, 3f);
                    var bulb = ZoneBuildKit.Cylinder(st, "Bulb", new Vector3(0, 2.1f, -0.4f), new Vector3(0.2f, 0.12f, 0.2f), m.lantern, false);
                    if (k % 4 == 0) ZoneBuildKit.PointLight(bulb.transform, "Glow", Vector3.zero, 7f, 1.1f, new Color(1f, 0.7f, 0.4f));
                }
        }

        // ── Dinh Cậu shrine on its rock, pier and fishing boats (east end) ──
        private static void DinhCauHarbour(Transform root, Mats m, bool withModels)
        {
            var g = ZoneBuildKit.Group(root, "DinhCau_Harbour");
            float x0 = HalfLength + 22f;
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "HarbourWater"; Object.DestroyImmediate(water.GetComponent<Collider>());
            water.transform.SetParent(g, false); water.transform.localPosition = new Vector3(x0 + 10f, 0.03f, -14f); water.transform.localScale = new Vector3(5f, 1f, 8f);
            water.GetComponent<Renderer>().sharedMaterial = m.water;
            Slab(g, "Quay", new Vector3(HalfLength + 5f, 0, -14f), new Vector3(10f, PavementTop, 40f), m.pavement);
            Slab(g, "Pier", new Vector3(x0, 0, -26f), new Vector3(24f, PavementTop, 3f), m.wood);
            var rock = ZoneBuildKit.Block(g, "ShrineRock", new Vector3(x0 + 14f, 1.2f, -8f), new Vector3(9f, 2.4f, 9f), m.rock, true);
            rock.transform.localRotation = Quaternion.Euler(0, 20f, 0);
            Slab(g, "Causeway", new Vector3(x0 + 5f, 0, -8f), new Vector3(12f, 1.2f, 2.4f), m.pavement);
            var shrine = ZoneBuildKit.Group(g, "DinhCau_Shrine"); shrine.localPosition = new Vector3(x0 + 14f, 2.4f, -8f);
            ZoneBuildKit.Block(shrine, "Hall", new Vector3(0, 1.3f, 0), new Vector3(4f, 2.6f, 3f), m.Wall(7), true);
            ZoneBuildKit.Block(shrine, "Roof", new Vector3(0, 2.8f, 0), new Vector3(5f, 0.4f, 4f), m.roof, true);
            ZoneBuildKit.Cylinder(shrine, "Lighthouse", new Vector3(3f, 3f, 2f), new Vector3(0.9f, 3f, 0.9f), m.kerb, true);
            ZoneBuildKit.Text(shrine, "Name", "DINH CẬU\n<size=50%>ディン・カウ（海の守り神）</size>", new Vector3(0, 4.2f, -2f), 0f, 0.7f, Color.white, 6f);
            var boat = withModels ? AssetDatabase.LoadAssetAtPath<GameObject>(Kit + "/kenney_watercraft-kit/FBX/boat-fishing-small.fbx")
                                    ?? AssetDatabase.LoadAssetAtPath<GameObject>(Kit + "/kenney_pirate-kit/FBX/boat-row-large.fbx") : null;
            for (int i = 0; i < 6; i++)
            {
                var at = new Vector3(x0 - 8f + i * 4.2f, 0.05f, -30f - (i % 2) * 3f);
                if (boat != null)
                {
                    var b = (GameObject)PrefabUtility.InstantiatePrefab(boat, g);
                    b.name = "FishingBoat_" + i; b.transform.localPosition = at; b.transform.localRotation = Quaternion.Euler(0, 90f + i * 7f, 0);
                    var bb = ZoneBuildKit.BoundsOf(b); b.transform.localScale = Vector3.one * (7f / Mathf.Max(0.1f, Mathf.Max(bb.size.x, bb.size.z)));
                }
                else ZoneBuildKit.Block(g, "FishingBoat_" + i, at + Vector3.up * 0.4f, new Vector3(2.2f, 0.8f, 7f), m.Awning(i), false);
            }
        }

        private static void Signs(Transform root)
        {
            var g = ZoneBuildKit.Group(root, "StreetNames");
            Color c = new Color(0.1f, 0.35f, 0.75f);
            ZoneBuildKit.Text(g, "TranHungDao", "Đường Trần Hưng Đạo · チャンフンダオ通り", new Vector3(-10f, 2.6f, NorthKerb - 0.4f), 0f, 0.4f, c, 6f);
            ZoneBuildKit.Text(g, "BachDang", "Đường Bạch Đằng → Chợ đêm", new Vector3(CrossX[0] + CrossHalfWidth + 0.5f, 2.6f, NorthKerb - 0.4f), 0f, 0.35f, c, 5f);
            ZoneBuildKit.Text(g, "NguyenTrai", "Đường Nguyễn Trãi · Dinh Cậu →", new Vector3(CrossX[1] - CrossHalfWidth - 0.5f, 2.6f, NorthKerb - 0.4f), 0f, 0.35f, c, 5f);
        }

        // ── helpers ──
        private static void Slab(Transform p, string name, Vector3 centreAtGround, Vector3 size, Material mat)
        {
            var go = ZoneBuildKit.Block(p, name, centreAtGround + Vector3.up * (size.y * 0.5f), size, mat, true);
            var r = go.GetComponent<Renderer>(); if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static void Paint(Transform p, string name, Vector3 at, Vector3 size, Material mat)
        {
            var go = ZoneBuildKit.Block(p, name, at + Vector3.up * size.y, size, mat, false);
            var r = go.GetComponent<Renderer>(); if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private sealed class Mats
        {
            public readonly Material asphalt, pavement, kerb, lot, paint, tile, wood, roof, brand, lantern, metal, lamp, water, rock;
            private readonly Material[] walls, awnings, signs;
            public Material Wall(int i) => walls[i % walls.Length];
            public Material Awning(int i) => awnings[i % awnings.Length];
            public Material Sign(int i) => signs[i % signs.Length];
            public Mats(bool save)
            {
                Material L(string n, Color c, float s = 0.2f, Color? e = null) => save ? ZoneBuildKit.Lit(MatDir, n, c, s, e)
                    : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = c };
                asphalt = L("PQ_Asphalt", new Color(0.22f, 0.23f, 0.25f), 0.1f);
                pavement = L("PQ_Pavement", new Color(0.78f, 0.72f, 0.64f));
                kerb = L("PQ_Kerb", new Color(0.85f, 0.85f, 0.82f));
                lot = L("PQ_Lot", new Color(0.66f, 0.6f, 0.5f));
                paint = L("PQ_RoadPaint", new Color(0.95f, 0.95f, 0.92f));
                tile = L("PQ_Tile", new Color(0.72f, 0.38f, 0.26f), 0.35f);
                wood = L("PQ_Wood", new Color(0.45f, 0.3f, 0.18f));
                roof = L("PQ_Roof", new Color(0.62f, 0.25f, 0.16f));
                brand = L("PQ_GanhNgonBrand", new Color(0.45f, 0.08f, 0.06f));
                lantern = L("PQ_Lantern", new Color(1f, 0.35f, 0.2f), 0.4f, new Color(1f, 0.4f, 0.15f) * 1.5f);
                metal = L("PQ_LampPost", new Color(0.3f, 0.32f, 0.35f), 0.5f);
                lamp = L("PQ_LampHead", new Color(1f, 0.95f, 0.85f), 0.5f, new Color(1f, 0.85f, 0.6f));
                water = L("PQ_HarbourWater", new Color(0.12f, 0.45f, 0.55f), 0.85f);
                rock = L("PQ_ShrineRock", new Color(0.42f, 0.4f, 0.37f));
                walls = new[] { new Color(0.96f, 0.86f, 0.55f), new Color(0.98f, 0.95f, 0.9f), new Color(0.6f, 0.8f, 0.75f), new Color(0.95f, 0.7f, 0.55f), new Color(0.75f, 0.85f, 0.95f) }
                    .Select((c, i) => L("PQ_Wall_" + i, c)).ToArray();
                awnings = new[] { new Color(0.15f, 0.45f, 0.75f), new Color(0.85f, 0.2f, 0.2f), new Color(0.2f, 0.6f, 0.35f), new Color(0.95f, 0.65f, 0.15f) }
                    .Select((c, i) => L("PQ_Awning_" + i, c)).ToArray();
                signs = new[] { new Color(0.55f, 0.1f, 0.1f), new Color(0.1f, 0.25f, 0.5f), new Color(0.12f, 0.4f, 0.22f), new Color(0.3f, 0.15f, 0.4f) }
                    .Select((c, i) => L("PQ_Sign_" + i, c)).ToArray();
            }
        }
    }
}
