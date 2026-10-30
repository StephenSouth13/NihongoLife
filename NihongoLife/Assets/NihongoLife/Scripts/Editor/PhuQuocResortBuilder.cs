using System;
using NihongoLife.PhuQuoc;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Dương Đông beach resort at 1:1, east of the downtown (inside 65_PhuQuocIsland, under GanhNgon_Restaurant, no new scene).
    /// Real proportions: 2.6–3.2 m storeys, 0.45 m loungers, 2.2 m umbrellas, 9 m palms, 1.4 m deep pool.
    /// Layout (local metres, +z inland / north, the sea is -z):
    ///   open-air lobby with a thatched hip roof · infinity pool on a teak deck with loungers and parasols · thatched pool bar ·
    ///   eight thatched bungalows with private decks in two wings · palm-lined beach with loungers · boardwalk from the downtown.
    /// Surfaces use CC0 Poly Haven textures (Assets/ThirdParty/PhuQuocCC0/polyhaven_resort); furniture and palms are Kenney CC0.
    /// Headless only: Unity -batchmode -executeMethod NihongoLife.EditorTools.PhuQuocResortBuilder.Build
    /// Run after PhuQuocDowntownBuilder.Build.
    /// </summary>
    public static class PhuQuocResortBuilder
    {
        public const string RootName = "DuongDong_Resort";
        public static readonly Vector3 Origin = new Vector3(205f, 0f, -6f); // local to GanhNgon_Restaurant (downtown spans x ±84, harbour to ~x 131)
        private const string TexDir = "Assets/ThirdParty/PhuQuocCC0/polyhaven_resort";
        private const string MatDir = "Assets/NihongoLife/Materials/PhuQuocResort";
        private const string MeshDir = "Assets/NihongoLife/Materials/PhuQuocResort/Meshes";
        private const string Furniture = "Assets/ThirdParty/Kenney/kenney_furniture-kit/Models/FBX format/";
        private const string Nature = PhuQuocIslandBuilder.AssetRoot + "/kenney_nature-kit/FBX/";
        private const string Holiday = PhuQuocIslandBuilder.AssetRoot + "/kenney_holiday-kit";

        public const float DeckTop = 0.45f;
        public const float PoolHalfX = 13f, PoolHalfZ = 4.5f, PoolDepth = 1.4f;
        public static readonly Vector3 PoolCentre = new Vector3(0f, 0f, 17f);
        public const int VillaCount = 8;

        public static void Build()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(PhuQuocIslandBuilder.ScenePath, OpenSceneMode.Single);
                var island = GameObject.Find("PhuQuocIsland");
                if (island == null) throw new InvalidOperationException("Run PhuQuocIslandBuilder.Build first");
                var restaurant = island.transform.Find("GanhNgon_Restaurant");
                if (restaurant == null) throw new InvalidOperationException("GanhNgon_Restaurant missing");
                BuildInto(restaurant);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save scene");
                AssetDatabase.SaveAssets();
                Debug.Log("[PhuQuocResortBuilder] Dương Đông resort built.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1); else throw;
            }
        }

        public static Transform BuildInto(Transform restaurant)
        {
            var old = restaurant.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject(RootName).transform;
            root.SetParent(restaurant, false);
            root.localPosition = Origin;

            var m = new Mats();
            Ground(root, m);
            Boardwalk(restaurant, m);
            Lobby(root, m);
            Pool(root, m);
            PoolBar(root, m);
            for (int i = 0; i < VillaCount; i++) Villa(root, m, i);
            Beach(root, m);
            Garden(root, m);
            Lights(root);
            return root;
        }

        // ── ground, sea, paths ──
        private static void Ground(Transform root, Mats m)
        {
            var g = ZoneBuildKit.Group(root, "Ground");
            Slab(g, "Lawn", new Vector3(0, 0, 25f), new Vector3(124f, 0.06f, 50f), m.lawn, 0f);
            Slab(g, "Sand", new Vector3(0, 0, -12f), new Vector3(124f, 0.05f, 24f), m.sand, 0f);
            Slab(g, "WetSand", new Vector3(0, -0.12f, -27f), new Vector3(124f, 0.05f, 6f), m.wetSand, 0f);
            // Soft lawn edge: a sandy-green strip and a low hedge of shrubs instead of a hard green/sand line.
            Slab(g, "LawnEdge", new Vector3(0, 0.005f, -0.6f), new Vector3(124f, 0.055f, 1.4f), m.lawnEdge, 0f);
            for (float x = -60f; x <= 60f; x += 3.1f)
            {
                if (Mathf.Abs(x) < 2f) continue; // keep the spine path clear
                Slab(g, "Shrub", new Vector3(x, 0.25f, 0.6f), new Vector3(1.4f, 0.5f, 0.9f), m.shrub, 0f);
            }
            var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
            sea.name = "Sea"; Object.DestroyImmediate(sea.GetComponent<Collider>());
            sea.transform.SetParent(g, false); sea.transform.localPosition = new Vector3(0, -0.08f, -85f); sea.transform.localScale = new Vector3(24f, 1f, 11f);
            sea.GetComponent<Renderer>().sharedMaterial = m.sea;
            // Paths: teak boards, 2.4 m wide, lobby → pool → beach and along both villa wings.
            Slab(g, "Path_Spine", new Vector3(0, 0, 5f), new Vector3(2.4f, 0.12f, 6f), m.deck, 0f);
            Slab(g, "Path_Wings", new Vector3(-0.5f, 0, 3f), new Vector3(121f, 0.12f, 2.4f), m.deck, 0f);
            Slab(g, "Path_West", new Vector3(-41f, 0, 23f), new Vector3(2.4f, 0.12f, 40f), m.deck, 0f);
            Slab(g, "Path_East", new Vector3(41f, 0, 23f), new Vector3(2.4f, 0.12f, 40f), m.deck, 0f);
            Slab(g, "Path_Lobby", new Vector3(0, 0, 29f), new Vector3(2.4f, 0.12f, 6f), m.deck, 0f);
        }

        private static void Boardwalk(Transform restaurant, Mats m)
        {
            var g = ZoneBuildKit.Group(restaurant.Find(RootName), "Boardwalk_FromDowntown");
            // From the downtown's east end (x 84) along the shore to the resort gate (resort x -62).
            float fromX = 84f - Origin.x, toX = -62f;
            float len = toX - fromX;
            Slab(g, "Boardwalk", new Vector3((fromX + toX) * 0.5f, 0, 14f - Origin.z), new Vector3(len, 0.2f, 3f), m.deck, 0f);
            Slab(g, "Boardwalk_In", new Vector3(-60f, 0, (14f - Origin.z + 3f) * 0.5f), new Vector3(3f, 0.2f, Mathf.Abs(14f - Origin.z - 3f) + 3f), m.deck, 0f);
            var gate = ZoneBuildKit.Group(g, "Gate");
            gate.localPosition = new Vector3(-62f, 0, 14f - Origin.z);
            ZoneBuildKit.Cylinder(gate, "PostN", new Vector3(0, 2.2f, 2.2f), new Vector3(0.4f, 2.2f, 0.4f), m.timber, true);
            ZoneBuildKit.Cylinder(gate, "PostS", new Vector3(0, 2.2f, -2.2f), new Vector3(0.4f, 2.2f, 0.4f), m.timber, true);
            ZoneBuildKit.Block(gate, "Beam", new Vector3(0, 4.55f, 0), new Vector3(0.5f, 0.35f, 5.4f), m.timber, false);
            Roof(gate, "GateRoof", new Vector3(0, 4.7f, 0), 4, 2.2f, 3.6f, 1.2f, m.thatch, 45f);
            ZoneBuildKit.Text(gate, "Name", "Dương Đông Beach Resort\n<size=55%>ズオンドン・ビーチリゾート · Курорт</size>", new Vector3(-0.35f, 3.7f, 0), 90f, 0.42f, new Color(1f, 0.93f, 0.75f), 5f);
        }

        // ── open-air lobby ──
        private static void Lobby(Transform root, Mats m)
        {
            var g = ZoneBuildKit.Group(root, "Lobby"); g.localPosition = new Vector3(0, 0, 38f);
            Slab(g, "Plinth", Vector3.zero, new Vector3(18f, DeckTop, 12f), m.terrazzo, 0f);
            Slab(g, "Steps", new Vector3(0, 0, -6.6f), new Vector3(6f, DeckTop * 0.5f, 1.2f), m.terrazzo, 0f);
            for (int ix = 0; ix < 5; ix++)
                for (int iz = 0; iz < 2; iz++)
                {
                    float x = -8f + ix * 4f, z = iz == 0 ? -5.4f : 5.4f;
                    ZoneBuildKit.Cylinder(g, "Column_" + ix + "_" + iz, new Vector3(x, DeckTop + 2f, z), new Vector3(0.36f, 2f, 0.36f), m.timber, true);
                }
            ZoneBuildKit.Block(g, "RingBeam", new Vector3(0, DeckTop + 4.1f, 0), new Vector3(16.6f, 0.3f, 11.2f), m.timber, false).GetComponent<Renderer>().sharedMaterial = m.timber;
            Roof(g, "ThatchRoof", new Vector3(0, DeckTop + 4.2f, 0), 4, 15.5f, 10.5f, 4.2f, m.thatch, 45f);
            Fit(g, Furniture + "loungeDesignSofa.fbx", "Sofa_A", new Vector3(-4f, DeckTop, 1.5f), 180f, 2.2f);
            Fit(g, Furniture + "loungeDesignSofa.fbx", "Sofa_B", new Vector3(4f, DeckTop, 1.5f), 180f, 2.2f);
            Fit(g, Furniture + "tableCoffeeGlass.fbx", "CoffeeTable", new Vector3(0, DeckTop, 0.5f), 0f, 1.3f);
            Fit(g, Furniture + "rugRounded.fbx", "Rug", new Vector3(0, DeckTop + 0.01f, 0.8f), 0f, 4f);
            ZoneBuildKit.Block(g, "Reception", new Vector3(0, DeckTop + 0.55f, 3.6f), new Vector3(4.2f, 1.1f, 0.8f), m.timber, true);
            ZoneBuildKit.Block(g, "ReceptionTop", new Vector3(0, DeckTop + 1.13f, 3.6f), new Vector3(4.4f, 0.06f, 1f), m.terrazzo, false);
            for (int i = 0; i < 4; i++) Fit(g, Furniture + "pottedPlant.fbx", "Plant_" + i, new Vector3(i < 2 ? -7.6f : 7.6f, DeckTop, i % 2 == 0 ? -4.6f : 4.6f), 0f, 1.6f, true);
            ZoneBuildKit.Text(g, "Welcome", "Chào mừng · ようこそ · Welcome · Добро пожаловать", new Vector3(0, DeckTop + 3.0f, 4.2f), 0f, 0.62f, new Color(0.25f, 0.16f, 0.08f), 14f);
        }

        // ── infinity pool on a teak deck ──
        private static void Pool(Transform root, Mats m)
        {
            var g = ZoneBuildKit.Group(root, "Pool"); g.localPosition = PoolCentre;
            float hx = PoolHalfX, hz = PoolHalfZ, rim = 6f;
            // Deck ring around the basin (four slabs leave the hole open).
            Slab(g, "Deck_N", new Vector3(0, 0, hz + rim * 0.5f), new Vector3(2f * (hx + rim), DeckTop, rim), m.deck, 0f);
            Slab(g, "Deck_S", new Vector3(0, 0, -hz - rim * 0.5f), new Vector3(2f * (hx + rim), DeckTop, rim), m.deck, 0f);
            Slab(g, "Deck_W", new Vector3(-hx - rim * 0.5f, 0, 0), new Vector3(rim, DeckTop, 2f * hz), m.deck, 0f);
            Slab(g, "Deck_E", new Vector3(hx + rim * 0.5f, 0, 0), new Vector3(rim, DeckTop, 2f * hz), m.deck, 0f);
            float floorY = DeckTop - PoolDepth;
            Slab(g, "Basin_Floor", new Vector3(0, floorY - 0.2f, 0), new Vector3(2f * hx, 0.2f, 2f * hz), m.poolTile, 0f);
            Slab(g, "Basin_N", new Vector3(0, floorY, hz - 0.1f), new Vector3(2f * hx, PoolDepth, 0.2f), m.poolTile, 0f);
            Slab(g, "Basin_S", new Vector3(0, floorY, -hz + 0.1f), new Vector3(2f * hx, PoolDepth, 0.2f), m.poolTile, 0f);
            Slab(g, "Basin_W", new Vector3(-hx + 0.1f, floorY, 0), new Vector3(0.2f, PoolDepth, 2f * hz), m.poolTile, 0f);
            Slab(g, "Basin_E", new Vector3(hx - 0.1f, floorY, 0), new Vector3(0.2f, PoolDepth, 2f * hz), m.poolTile, 0f);
            Slab(g, "Coping_S", new Vector3(0, DeckTop, -hz - 0.15f), new Vector3(2f * hx + 0.6f, 0.06f, 0.3f), m.terrazzo, 0f);
            Slab(g, "Coping_N", new Vector3(0, DeckTop, hz + 0.15f), new Vector3(2f * hx + 0.6f, 0.06f, 0.3f), m.terrazzo, 0f);
            var water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = "PoolWater"; Object.DestroyImmediate(water.GetComponent<Collider>());
            water.transform.SetParent(g, false); water.transform.localPosition = new Vector3(0, DeckTop - 0.18f, 0); water.transform.localScale = new Vector3(2f * hx - 0.4f, 0.04f, 2f * hz - 0.4f);
            water.GetComponent<Renderer>().sharedMaterial = m.poolWater;
            // Safety: a walkable surface at water level so the player wades, not falls 1.4 m.
            var wade = new GameObject("PoolWade"); wade.transform.SetParent(g, false); wade.transform.localPosition = new Vector3(0, DeckTop - 0.55f, 0);
            var wc = wade.AddComponent<BoxCollider>(); wc.size = new Vector3(2f * hx - 0.4f, 0.1f, 2f * hz - 0.4f);
            // Loungers + parasols along the sea side of the pool.
            for (int i = 0; i < 8; i++)
            {
                float x = -hx + 1.6f + i * ((2f * hx - 3.2f) / 7f);
                Fit(g, Furniture + "loungeChairRelax.fbx", "Lounger_" + i, new Vector3(x, DeckTop, -hz - 2.6f), 180f, 1.95f, true);
                if (i % 2 == 0) Parasol(g, m, "Parasol_" + i, new Vector3(x + 1.2f, DeckTop, -hz - 3.6f), i);
            }
            for (int i = 0; i < 4; i++) Fit(g, Furniture + "loungeSofa.fbx", "DaybedN_" + i, new Vector3(-9f + i * 6f, DeckTop, hz + 3f), 0f, 2.1f, true);
        }

        private static void PoolBar(Transform root, Mats m)
        {
            var g = ZoneBuildKit.Group(root, "PoolBar"); g.localPosition = PoolCentre + new Vector3(PoolHalfX + 3f, DeckTop, 0);
            for (int i = 0; i < 4; i++)
                ZoneBuildKit.Cylinder(g, "Post_" + i, new Vector3(i < 2 ? -2.4f : 2.4f, 1.4f, i % 2 == 0 ? -2.4f : 2.4f), new Vector3(0.22f, 1.4f, 0.22f), m.timber, true);
            Roof(g, "BarRoof", new Vector3(0, 2.8f, 0), 4, 4.6f, 4.6f, 2.2f, m.thatch, 45f);
            ZoneBuildKit.Block(g, "Counter", new Vector3(0, 0.55f, 0), new Vector3(3.2f, 1.1f, 1.2f), m.bamboo, true);
            ZoneBuildKit.Block(g, "CounterTop", new Vector3(0, 1.12f, 0), new Vector3(3.5f, 0.06f, 1.5f), m.timber, false);
            for (int i = 0; i < 3; i++) ZoneBuildKit.Cylinder(g, "Stool_" + i, new Vector3(-1.1f + i * 1.1f, 0.38f, -1.2f), new Vector3(0.4f, 0.38f, 0.4f), m.timber, false);
            ZoneBuildKit.Text(g, "Name", "Quầy nước dừa · ココナッツバー", new Vector3(0, 2.4f, -2.5f), 0f, 0.26f, new Color(1f, 0.95f, 0.8f), 4f);
        }

        // ── thatched bungalows ──
        private static void Villa(Transform root, Mats m, int i)
        {
            bool west = i < VillaCount / 2;
            int k = i % (VillaCount / 2);
            var g = ZoneBuildKit.Group(root, "Villa_" + (i + 1));
            g.localPosition = new Vector3(west ? -48f : 48f, 0, 9f + k * 11f);
            g.localRotation = Quaternion.Euler(0, west ? 90f : -90f, 0); // fronts face the wing path (+z local → toward the centre)
            Slab(g, "Stilts", Vector3.zero, new Vector3(9.4f, DeckTop, 8f), m.timber, 0f);
            Slab(g, "Deck", new Vector3(0, DeckTop, 0), new Vector3(9.4f, 0.08f, 8f), m.deck, 0f);
            float y0 = DeckTop + 0.08f, h = 2.9f + (i % 3) * 0.2f, w = 6.6f + (i % 3) * 0.6f; // villa variety: width, height, colour, roof pitch
            var wall = Mats.Strip(ZoneBuildKit.Lit(MatDir, "Resort_VillaWall_" + (i % Mats.VillaTints.Length), Mats.VillaTints[i % Mats.VillaTints.Length], 0.15f));
            var body = ZoneBuildKit.Block(g, "Body", new Vector3(0, y0 + h * 0.5f, -1.2f), new Vector3(w, h, 4.6f), wall, true);
            ZoneBuildKit.Block(g, "GlassFront", new Vector3(0.8f, y0 + 1.25f, 1.12f), new Vector3(4.2f, 2.3f, 0.06f), m.glass, false);
            ZoneBuildKit.Block(g, "Door", new Vector3(-2.2f, y0 + 1.05f, 1.12f), new Vector3(1f, 2.1f, 0.08f), m.timber, false);
            ZoneBuildKit.Block(g, "WindowBack", new Vector3(0, y0 + 1.6f, -3.52f), new Vector3(2.4f, 1.1f, 0.06f), m.glass, false);
            for (int c = 0; c < 2; c++) ZoneBuildKit.Cylinder(g, "VerandaPost_" + c, new Vector3(c == 0 ? -4.2f : 4.2f, y0 + 1.5f, 3.2f), new Vector3(0.18f, 1.5f, 0.18f), m.timber, true);
            Roof(g, "Thatch", new Vector3(0, y0 + h, 0f), 4, 6.2f + (i % 3) * 0.4f, 5.6f, 2.3f + (i % 2) * 0.7f, m.thatch, 45f);
            Fit(g, Furniture + "loungeChairRelax.fbx", "DeckLounger", new Vector3(2.6f, y0, 2.5f), 0f, 1.95f, true);
            Fit(g, Furniture + "tableRound.fbx", "DeckTable", new Vector3(-1.6f, y0, 2.6f), 0f, 0.8f, true);
            Fit(g, Furniture + "chairRounded.fbx", "DeckChair", new Vector3(-0.8f, y0, 2.6f), -90f, 0.55f, true);
            var hang = AssetDatabase.LoadAssetAtPath<GameObject>(Holiday + "/Models/FBX format/lantern-hanging.fbx") ?? FindModel(Holiday, "lantern-hanging");
            if (hang != null) Place(g, hang, "Lantern", new Vector3(3.9f, y0 + 2.2f, 3.2f), 0f, 0.6f, false);
            ZoneBuildKit.Text(g, "Number", "Villa " + (i + 1), new Vector3(-2.2f, y0 + 2.45f, 1.2f), 0f, 0.22f, new Color(0.3f, 0.2f, 0.1f), 2f);
        }

        // ── beach ──
        private static void Beach(Transform root, Mats m)
        {
            var g = ZoneBuildKit.Group(root, "Beach");
            for (int i = 0; i < 10; i++)
            {
                float x = -45f + i * 10f;
                Fit(g, Furniture + "loungeChairRelax.fbx", "BeachLounger_" + i + "a", new Vector3(x - 0.8f, 0.05f, -14f), 180f, 1.95f, true);
                Fit(g, Furniture + "loungeChairRelax.fbx", "BeachLounger_" + i + "b", new Vector3(x + 0.8f, 0.05f, -14f), 180f, 1.95f, true);
                Parasol(g, m, "BeachParasol_" + i, new Vector3(x, 0.05f, -15.2f), i + 1);
            }
            string[] palms = { "tree_palmTall", "tree_palmBend", "tree_palmDetailedTall", "tree_palmTall", "tree_palmBend" };
            var rng = new System.Random(1102);
            for (int i = 0; i < 18; i++)
            {
                float x = -58f + i * 6.8f + (float)rng.NextDouble() * 2f;
                float z = -4f - (float)rng.NextDouble() * 5f;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Nature + palms[i % palms.Length] + ".fbx");
                if (prefab != null) Place(g, prefab, "Palm_" + i, new Vector3(x, 0.05f, z), (float)rng.NextDouble() * 360f, 8f + (float)rng.NextDouble() * 2.5f, false, true);
            }
            ZoneBuildKit.Text(g, "Sign", "Bãi Trường · ロングビーチ · Long Beach", new Vector3(0, 2.2f, -2f), 180f, 0.36f, Color.white, 6f);
            Slab(g, "Jetty", new Vector3(30f, 0, -40f), new Vector3(2.4f, 0.3f, 26f), m.deck, 0f);
        }

        private static void Garden(Transform root, Mats m)
        {
            var g = ZoneBuildKit.Group(root, "Garden");
            string[] plants = { "plant_bushLarge", "plant_bushDetailed", "flower_redA", "flower_yellowB", "flower_purpleA", "plant_flatTall" };
            var rng = new System.Random(77);
            for (int i = 0; i < 40; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * (22f + (float)rng.NextDouble() * 9f);
                float z = 6f + (float)rng.NextDouble() * 40f;
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(Nature + plants[i % plants.Length] + ".fbx");
                if (p != null) Place(g, p, "Plant_" + i, new Vector3(x, 0.06f, z), (float)rng.NextDouble() * 360f, i % plants.Length < 2 ? 1.4f : 0.5f, false, true);
            }
            var palm = AssetDatabase.LoadAssetAtPath<GameObject>(Nature + "tree_palmDetailedTall.fbx");
            if (palm != null)
                for (int i = 0; i < 6; i++) Place(g, palm, "LobbyPalm_" + i, new Vector3(i < 3 ? -12f : 12f, 0.06f, 32f + (i % 3) * 6f), i * 50f, 9f, false, true);
        }

        private static void Lights(Transform root)
        {
            var g = ZoneBuildKit.Group(root, "WarmLights");
            Color warm = new Color(1f, 0.78f, 0.5f);
            ZoneBuildKit.PointLight(g, "Lobby", new Vector3(0, 4f, 38f), 16f, 2.2f, warm);
            ZoneBuildKit.PointLight(g, "PoolW", new Vector3(-8f, 2.5f, 17f), 14f, 1.4f, new Color(0.6f, 0.85f, 1f));
            ZoneBuildKit.PointLight(g, "PoolE", new Vector3(8f, 2.5f, 17f), 14f, 1.4f, new Color(0.6f, 0.85f, 1f));
            ZoneBuildKit.PointLight(g, "Bar", PoolCentre + new Vector3(PoolHalfX + 3f, 2.6f, 0), 9f, 1.8f, warm);
        }

        // ── helpers ──
        private static void Parasol(Transform p, Mats m, string name, Vector3 at, int i)
        {
            var g = ZoneBuildKit.Group(p, name); g.localPosition = at;
            ZoneBuildKit.Cylinder(g, "Pole", new Vector3(0, 1.15f, 0), new Vector3(0.06f, 1.15f, 0.06f), m.timber, false);
            Roof(g, "Canopy", new Vector3(0, 2.05f, 0), 8, 1.3f, 1.3f, 0.45f, i % 2 == 0 ? m.thatch : m.canvas, 22.5f);
        }

        private static void Slab(Transform p, string name, Vector3 centreAtGround, Vector3 size, Material mat, float yaw)
        {
            var go = ZoneBuildKit.Block(p, name, centreAtGround + Vector3.up * (size.y * 0.5f), size, mat, true, yaw);
            var r = go.GetComponent<Renderer>(); if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            WorldUv(go, size);
        }

        /// <summary>Box UVs in metres so textures keep their real size on every slab (no stretched planks).</summary>
        private static void WorldUv(GameObject go, Vector3 size)
        {
            var mf = go.GetComponent<MeshFilter>(); if (mf == null) return;
            string key = "Box_" + Mathf.RoundToInt(size.x * 100) + "_" + Mathf.RoundToInt(size.y * 100) + "_" + Mathf.RoundToInt(size.z * 100);
            string path = MeshDir + "/" + key + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = Object.Instantiate(mf.sharedMesh); mesh.name = key;
                var v = mesh.vertices; var n = mesh.normals; var uv = new Vector2[v.Length];
                for (int i = 0; i < v.Length; i++)
                {
                    Vector3 s = Vector3.Scale(v[i], size), a = new Vector3(Mathf.Abs(n[i].x), Mathf.Abs(n[i].y), Mathf.Abs(n[i].z));
                    uv[i] = a.y > 0.5f ? new Vector2(s.x, s.z) : a.x > 0.5f ? new Vector2(s.z, s.y) : new Vector2(s.x, s.y);
                    uv[i] *= 0.5f; // one texture tile = 2 m
                }
                mesh.uv = uv;
                ZoneBuildKit.EnsureFolder(MeshDir);
                AssetDatabase.CreateAsset(mesh, path);
            }
            mf.sharedMesh = mesh;
        }

        /// <summary>Hip / conical roof: n sides, half-extents rx/rz at the eaves, apex height h. Double-sided (seen from under the lobby).</summary>
        private static GameObject Roof(Transform p, string name, Vector3 eaves, int n, float rx, float rz, float h, Material mat, float startDeg)
        {
            string key = "Roof_" + n + "_" + Mathf.RoundToInt(rx * 100) + "_" + Mathf.RoundToInt(rz * 100) + "_" + Mathf.RoundToInt(h * 100);
            string path = MeshDir + "/" + key + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                float k = n == 4 ? Mathf.Sqrt(2f) : 1f; // square hip: corners at rx/rz
                var verts = new System.Collections.Generic.List<Vector3>(); var uvs = new System.Collections.Generic.List<Vector2>(); var tris = new System.Collections.Generic.List<int>();
                Vector3 apex = new Vector3(0, h, 0);
                for (int i = 0; i < n; i++)
                {
                    float a0 = (startDeg + i * 360f / n) * Mathf.Deg2Rad, a1 = (startDeg + (i + 1) * 360f / n) * Mathf.Deg2Rad;
                    Vector3 p0 = new Vector3(Mathf.Cos(a0) * rx * k, 0, Mathf.Sin(a0) * rz * k), p1 = new Vector3(Mathf.Cos(a1) * rx * k, 0, Mathf.Sin(a1) * rz * k);
                    float edge = Vector3.Distance(p0, p1), slant = Vector3.Distance((p0 + p1) * 0.5f, apex);
                    for (int side = 0; side < 2; side++)
                    {
                        int b = verts.Count;
                        verts.Add(p0); verts.Add(p1); verts.Add(apex);
                        uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(edge * 0.5f, 0)); uvs.Add(new Vector2(edge * 0.25f, slant * 0.5f));
                        if (side == 0) { tris.Add(b); tris.Add(b + 2); tris.Add(b + 1); } else { tris.Add(b); tris.Add(b + 1); tris.Add(b + 2); }
                    }
                }
                mesh = new Mesh { name = key };
                mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                ZoneBuildKit.EnsureFolder(MeshDir);
                AssetDatabase.CreateAsset(mesh, path);
            }
            var go = new GameObject(name);
            go.transform.SetParent(p, false); go.transform.localPosition = eaves;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private static GameObject FindModel(string folder, string name)
        {
            foreach (var guid in AssetDatabase.FindAssets(name + " t:GameObject", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == name) return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return null;
        }

        /// <summary>Kenney furniture: scaled so its longest side is <paramref name="size"/> metres.</summary>
        private static GameObject Fit(Transform p, string path, string name, Vector3 at, float yaw, float size, bool collider = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab == null ? null : Place(p, prefab, name, at, yaw, size, collider);
        }

        /// <summary>byHeight: size is the height (palms, plants); otherwise the longest horizontal side.</summary>
        private static GameObject Place(Transform p, GameObject prefab, string name, Vector3 at, float yaw, float size, bool collider, bool byHeight = false)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, p);
            inst.name = name;
            inst.transform.localPosition = at; inst.transform.localRotation = Quaternion.Euler(0, yaw, 0); inst.transform.localScale = Vector3.one;
            var b = ZoneBuildKit.BoundsOf(inst);
            float measure = byHeight ? b.size.y : Mathf.Max(b.size.x, b.size.z);
            inst.transform.localScale = Vector3.one * (size / Mathf.Max(0.01f, measure));
            b = ZoneBuildKit.BoundsOf(inst);
            if (name.Contains("Lounger") && b.size.y > 0.9f) // Kenney relax chair is upright; flatten to a real 0.85 m sun lounger
            { var s = inst.transform.localScale; s.y *= 0.85f / b.size.y; inst.transform.localScale = s; b = ZoneBuildKit.BoundsOf(inst); }
            inst.transform.position += Vector3.up * (p.TransformPoint(at).y - b.min.y); // sit on the floor
            foreach (var c in inst.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            if (collider)
            {
                var col = inst.AddComponent<BoxCollider>();
                var lb = new Bounds(inst.transform.InverseTransformPoint(b.center), Vector3.zero);
                lb.Encapsulate(inst.transform.InverseTransformPoint(b.min)); lb.Encapsulate(inst.transform.InverseTransformPoint(b.max));
                col.center = lb.center; col.size = new Vector3(Mathf.Abs(lb.size.x), Mathf.Abs(lb.size.y), Mathf.Abs(lb.size.z));
            }
            return inst;
        }

        private sealed class Mats
        {
            public readonly Material sand, wetSand, sea, lawn, lawnEdge, shrub, deck, timber, thatch, plaster, terrazzo, poolTile, poolWater, glass, bamboo, canvas;
            public Mats()
            {
                ZoneBuildKit.EnsureFolder(MatDir);
                sand = ZoneBuildKit.Lit(MatDir, "Resort_Sand", new Color(0.97f, 0.91f, 0.78f), 0.12f); // bright Phu Quoc white sand
                foreach (var m in new[] { sand }) { if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null); m.mainTexture = null; if (m.HasProperty("_BumpMap")) m.SetTexture("_BumpMap", null); m.DisableKeyword("_NORMALMAP"); }
                wetSand = Strip(ZoneBuildKit.Lit(MatDir, "Resort_WetSand", new Color(0.86f, 0.79f, 0.64f), 0.45f));
                lawn = ZoneBuildKit.Lit(MatDir, "Resort_Lawn", new Color(0.36f, 0.58f, 0.27f), 0.1f);
                lawnEdge = ZoneBuildKit.Lit(MatDir, "Resort_LawnEdge", new Color(0.6f, 0.62f, 0.38f), 0.1f);
                shrub = ZoneBuildKit.Lit(MatDir, "Resort_Shrub", new Color(0.2f, 0.42f, 0.18f), 0.1f);
                deck = ZoneBuildKit.Lit(MatDir, "Resort_Teak", new Color(0.72f, 0.52f, 0.34f), 0.35f); // honey teak
                foreach (var m in new[] { deck }) { if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null); m.mainTexture = null; if (m.HasProperty("_BumpMap")) m.SetTexture("_BumpMap", null); m.DisableKeyword("_NORMALMAP"); }
                timber = ZoneBuildKit.Lit(MatDir, "Resort_Timber", new Color(0.55f, 0.37f, 0.22f), 0.35f);
                thatch = Strip(ZoneBuildKit.Lit(MatDir, "Resort_Thatch", new Color(0.82f, 0.66f, 0.42f), 0.05f)); // sun-bleached palm thatch
                plaster = Tex("Resort_Plaster", "white_plaster_02", new Color(0.98f, 0.96f, 0.92f), 0.15f);
                terrazzo = Tex("Resort_Terrazzo", "terrazzo_tiles", Color.white, 0.55f);
                poolTile = Tex("Resort_PoolTile", "rounded_square_tiled_wall", new Color(0.55f, 0.85f, 0.95f), 0.7f);
                bamboo = Tex("Resort_Bamboo", "bamboo_wall", Color.white, 0.2f);
                poolWater = ZoneBuildKit.Lit(MatDir, "Resort_PoolWater", new Color(0.2f, 0.75f, 0.9f, 0.55f), 0.95f, null, true);
                sea = ZoneBuildKit.Lit(MatDir, "Resort_Sea", new Color(0.05f, 0.55f, 0.72f, 1f), 0.8f); // opaque: the see-through sea read as haze
                glass = ZoneBuildKit.Lit(MatDir, "Resort_Glass", new Color(0.35f, 0.5f, 0.55f), 0.95f);
                canvas = ZoneBuildKit.Lit(MatDir, "Resort_Canvas", new Color(0.96f, 0.93f, 0.85f), 0.1f);
            }

            internal static Material Strip(Material m)
        { if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null); m.mainTexture = null; if (m.HasProperty("_BumpMap")) m.SetTexture("_BumpMap", null); m.DisableKeyword("_NORMALMAP"); return m; }

        internal static readonly Color[] VillaTints = { new Color(0.98f, 0.95f, 0.88f), new Color(0.78f, 0.92f, 0.88f), new Color(0.97f, 0.82f, 0.72f), new Color(0.98f, 0.92f, 0.68f) }; // cream, sea-foam, coral, frangipani

        private static Material Tex(string name, string tex, Color tint, float smooth)
            {
                var mat = ZoneBuildKit.Lit(MatDir, name, tint, smooth);
                string diff = TexDir + "/" + tex + "_diff_1k.jpg", nor = TexDir + "/" + tex + "_nor_gl_1k.jpg";
                var ni = AssetImporter.GetAtPath(nor) as TextureImporter;
                if (ni != null && ni.textureType != TextureImporterType.NormalMap) { ni.textureType = TextureImporterType.NormalMap; ni.SaveAndReimport(); }
                var d = AssetDatabase.LoadAssetAtPath<Texture2D>(diff);
                var n = AssetDatabase.LoadAssetAtPath<Texture2D>(nor);
                if (d != null) { mat.SetTexture("_BaseMap", d); mat.mainTexture = d; }
                if (n != null) { mat.SetTexture("_BumpMap", n); mat.EnableKeyword("_NORMALMAP"); }
                EditorUtility.SetDirty(mat);
                return mat;
            }
        }
    }
}
