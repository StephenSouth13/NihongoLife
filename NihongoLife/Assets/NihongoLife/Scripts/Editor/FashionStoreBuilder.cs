#if UNITY_EDITOR
using System;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Fashion;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.Progression;
using NihongoLife.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using static NihongoLife.EditorTools.ZoneBuildKit;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Hibari Mode (ひばりモード, scene 56_FashionStore), the clothes shop asked for by the owner:
    ///  • the sales floor: clothes rails hung with every outfit of Resources/Fashion/outfits.json, folded tables,
    ///    mannequins in real sets, three fitting rooms, the mirror wall, the till with Ren, the manager, and the job board;
    ///  • the entrance on StreetBuilding_S_1 in 90_TestSandbox (next to Hibiki Music &amp; Art), portal + return spawn;
    ///  • build settings.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.FashionStoreBuilder.Build
    /// </summary>
    public static class FashionStoreBuilder
    {
        public const string ScenePath = "Assets/NihongoLife/Scenes/56_FashionStore.unity";
        private const string CityPath = "Assets/NihongoLife/Scenes/90_TestSandbox.unity";
        private const string MatDir = "Assets/NihongoLife/Materials/FashionStore";
        private const string CityBuilding = "StreetBuilding_S_1";
        private const string Quest = "job_fashion_shift";
        public static readonly Vector3 O = new Vector3(1600f, 0f, 0f);
        private const float HalfX = 13f, South = -9f, North = 13f, Height = 4.2f;

        /// <summary>Stepping out of a shop the player looks along the pavement: looking at the street, the camera behind
        /// them sat inside the canopy and a band of it covered the picture.</summary>
        public static readonly Quaternion ClearOfCanopy = Quaternion.Euler(0f, 90f, 0f);

        private static Material _floor, _wall, _accent, _ceiling, _wood, _darkWood, _metal, _chrome, _glass, _mirror, _sign, _lamp, _rug, _white, _curtain, _plant, _mannequin;

        public static void Build()
        {
            try
            {
                Materials();
                BuildZone();
                BuildCityEntrance();
                var scenes = EditorBuildSettings.scenes.ToList();
                if (!scenes.Any(s => s.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                AssetDatabase.SaveAssets();
                Debug.Log("[FashionStoreBuilder] Hibari Mode built.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static void Materials()
        {
            _floor = Lit(MatDir, "Store_Floor", new Color(0.86f, 0.82f, 0.76f), 0.55f);
            _wall = Lit(MatDir, "Store_Wall", new Color(0.96f, 0.94f, 0.9f), 0.2f);
            _accent = Lit(MatDir, "Store_Accent", new Color(0.82f, 0.33f, 0.55f), 0.3f);
            _ceiling = Lit(MatDir, "Store_Ceiling", new Color(0.97f, 0.96f, 0.95f), 0.1f);
            _wood = Lit(MatDir, "Store_Wood", new Color(0.72f, 0.54f, 0.36f), 0.4f);
            _darkWood = Lit(MatDir, "Store_DarkWood", new Color(0.32f, 0.22f, 0.16f), 0.4f);
            _metal = Lit(MatDir, "Store_Metal", new Color(0.22f, 0.22f, 0.25f), 0.6f);
            _chrome = Lit(MatDir, "Store_Chrome", new Color(0.82f, 0.82f, 0.84f), 0.9f);
            _glass = Lit(MatDir, "Store_Glass", new Color(0.8f, 0.9f, 0.95f, 0.25f), 0.95f, transparent: true);
            _mirror = Lit(MatDir, "Store_Mirror", new Color(0.78f, 0.85f, 0.9f), 0.98f);
            _sign = Lit(MatDir, "Store_Sign", new Color(0.13f, 0.12f, 0.16f), 0.4f);
            _lamp = Lit(MatDir, "Store_Lamp", Color.white, 0.2f, new Color(1f, 0.96f, 0.9f) * 1.5f);
            _rug = Lit(MatDir, "Store_Rug", new Color(0.3f, 0.26f, 0.32f), 0.05f);
            _white = Lit(MatDir, "Store_White", new Color(0.97f, 0.97f, 0.96f), 0.4f);
            _curtain = Lit(MatDir, "Store_Curtain", new Color(0.55f, 0.2f, 0.36f), 0.1f);
            _plant = Lit(MatDir, "Store_Plant", new Color(0.32f, 0.58f, 0.34f), 0.2f);
            _mannequin = Lit(MatDir, "Store_Mannequin", new Color(0.93f, 0.91f, 0.88f), 0.5f);
        }

        private static Vector3 P(float x, float y, float z) => O + new Vector3(x, y, z);

        private static Material Cloth(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return Lit(MatDir + "/Cloth", "Cloth_" + hex.TrimStart('#'), color, 0.12f);
        }

        // ─────────── The shop ───────────

        private static void BuildZone()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("HibariMode_Zone");
            root.AddComponent<SceneZoneVisibility>();
            var shell = Group(root.transform, "Shell");
            var floor = Group(root.transform, "SalesFloor");
            var spots = Group(root.transform, "Spots");
            var job = Group(root.transform, "Jobs");

            float width = HalfX * 2f, depth = North - South, midZ = (North + South) / 2f;
            Block(shell, "Floor", P(0f, -0.1f, midZ), new Vector3(width, 0.2f, depth), _floor, true);
            Block(shell, "Ceiling", P(0f, Height, midZ), new Vector3(width, 0.2f, depth), _ceiling, true);
            Block(shell, "Wall_N", P(0f, Height / 2f, North), new Vector3(width, Height, 0.25f), _wall, true);
            Block(shell, "Wall_W", P(-HalfX, Height / 2f, midZ), new Vector3(0.25f, Height, depth), _wall, true);
            Block(shell, "Wall_E", P(HalfX, Height / 2f, midZ), new Vector3(0.25f, Height, depth), _wall, true);
            Block(shell, "Wall_S_L", P(-7.75f, Height / 2f, South), new Vector3(10.5f, Height, 0.25f), _wall, true);
            Block(shell, "Wall_S_R", P(7.75f, Height / 2f, South), new Vector3(10.5f, Height, 0.25f), _wall, true);
            Block(shell, "Wall_S_Header", P(0f, (2.4f + Height) / 2f, South), new Vector3(5f, Height - 2.4f, 0.25f), _wall, true);
            Block(shell, "EntranceGlass", P(0f, 1.2f, South - 0.06f), new Vector3(5f, 2.4f, 0.04f), _glass, true); // solid: the camera stays inside
            Block(shell, "AccentBand_W", P(-HalfX + 0.14f, 3.5f, midZ), new Vector3(0.03f, 0.5f, depth), _accent, false);
            Block(shell, "AccentBand_E", P(HalfX - 0.14f, 3.5f, midZ), new Vector3(0.03f, 0.5f, depth), _accent, false);
            Block(shell, "AccentBand_N", P(0f, 3.5f, North - 0.14f), new Vector3(width, 0.5f, 0.03f), _accent, false);
            Block(shell, "Rug_Entrance", P(0f, 0.006f, -7.6f), new Vector3(4.4f, 0.012f, 2.2f), _rug, false);
            Block(shell, "Rug_Centre", P(0f, 0.006f, 2.6f), new Vector3(5.6f, 0.012f, 7f), _rug, false);

            BuildRails(floor, spots);
            BuildTables(floor, job);
            BuildMannequins(floor);
            BuildFittingRooms(floor, job, spots);
            BuildTill(floor, job);
            BuildDecor(floor);
            JobSiteBuilder.Materials();
            JobSiteBuilder.Board(job, P(-4.2f, 0f, South + 0.35f), 180f, Quest, "アルバイト ぼしゅう", "Hibari Mode · ¥1,900/ca");
            JobPerson(job, "Customer_Rail_W", P(-7.2f, 0f, -3.2f), 90f, JobStationKind.FashionAdvise, "advise_a");
            JobPerson(job, "Customer_Rail_E", P(7.2f, 0f, 1.8f), 270f, JobStationKind.FashionAdvise, "advise_b");

            foreach (var (ix, iz) in new[] { (-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 0), (0, 1), (1, -1), (1, 0), (1, 1) })
            {
                Vector3 at = P(ix * 8f, Height - 0.09f, 2f + iz * 7f);
                Block(shell, $"CeilingLight_{ix}_{iz}", at, new Vector3(2.6f, 0.04f, 0.6f), _lamp, false);
                PointLight(root.transform, $"Light_{ix}_{iz}", at + Vector3.down * 0.4f, 11f, 2.4f, new Color(1f, 0.96f, 0.9f));
            }
            root.AddComponent<IndoorAmbience>().Configure(new Color(0.78f, 0.76f, 0.74f));
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.66f, 0.64f, 0.62f);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;

            Spawn(root.transform, WorldLocationCatalog.FashionStoreEntrance, P(0f, 0.05f, -7f), 0f);
            var exit = Trigger(root.transform, "ExitToCity", P(0f, 1.1f, South + 0.55f), new Vector3(4.6f, 2.2f, 1f));
            var portal = exit.AddComponent<ScenePortal>();
            portal.Configure(WorldLocationCatalog.CityScene, WorldLocationCatalog.CityFashionStoreReturn, "街へ戻る / Trở lại thành phố", true);
            var so = new SerializedObject(portal);
            so.FindProperty("promptJa").stringValue = "そとに でる";
            so.FindProperty("promptEn").stringValue = "Ra phố";
            so.ApplyModifiedPropertiesWithoutUndo();

            var preview = new GameObject("PreviewCamera") { tag = "MainCamera" };
            preview.transform.SetParent(root.transform);
            preview.transform.position = P(0f, 2.8f, -8.2f);
            preview.transform.rotation = Quaternion.LookRotation(P(0f, 1f, 4f) - preview.transform.position);
            preview.AddComponent<Camera>().fieldOfView = 64f;
            preview.AddComponent<AudioListener>();
            preview.AddComponent<NihongoLife.Cameras.ZonePreviewCamera>();

            foreach (var r in root.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true; }
            if (!EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath)) throw new InvalidOperationException("Could not save " + ScenePath);
        }

        private static void JobPerson(Transform parent, string name, Vector3 position, float yaw, JobStationKind kind, string id)
        {
            var visual = JobSiteBuilder.Person(parent, name + "_Visual", position, yaw);
            var trigger = Trigger(parent, name, position + Vector3.up, new Vector3(1.4f, 2f, 1.4f));
            trigger.AddComponent<JobStation>().Configure(Quest, kind, id, "おきゃくさま", "Khách hàng", visual);
        }

        private static void Spawn(Transform parent, string id, Vector3 position, float yaw)
        {
            var spawn = new GameObject("Spawn_" + id);
            spawn.transform.SetParent(parent);
            spawn.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            spawn.AddComponent<SceneSpawnPoint>().Configure(id);
        }

        private static void Spot(Transform parent, string name, Vector3 position, Vector3 size, string category) =>
            Trigger(parent, name, position, size).AddComponent<FashionSpot>().Configure(category);

        // ─────────── Clothes ───────────

        /// <summary>A shirt on a hanger, front to the aisle: yaw 0 faces −Z.</summary>
        private static void Hanging(Transform parent, Vector3 position, float yaw, Material top, Material bottom)
        {
            var hanger = Group(parent, "Garment");
            hanger.position = position;
            hanger.rotation = Quaternion.Euler(0f, yaw, 0f);
            Block(hanger, "Hook", new Vector3(0f, -0.05f, 0f), new Vector3(0.02f, 0.1f, 0.02f), _chrome, false);
            Block(hanger, "Bar", new Vector3(0f, -0.1f, 0f), new Vector3(0.42f, 0.02f, 0.02f), _chrome, false);
            Block(hanger, "Body", new Vector3(0f, -0.42f, 0f), new Vector3(0.44f, 0.62f, 0.05f), top, false);
            Block(hanger, "Sleeve_L", new Vector3(-0.3f, -0.34f, 0f), new Vector3(0.14f, 0.46f, 0.05f), top, false).transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            Block(hanger, "Sleeve_R", new Vector3(0.3f, -0.34f, 0f), new Vector3(0.14f, 0.46f, 0.05f), top, false).transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
            if (bottom != null) Block(hanger, "Trousers", new Vector3(0f, -0.94f, 0.02f), new Vector3(0.3f, 0.42f, 0.04f), bottom, false);
        }

        /// <summary>A rail along Z (facing X) or along X (facing Z), hung with a category's outfits.</summary>
        private static void Rail(Transform parent, string name, Vector3 centre, float length, bool alongZ, FashionOutfit[] outfits, float garmentYaw)
        {
            var rail = Group(parent, name);
            Vector3 axis = alongZ ? Vector3.forward : Vector3.right;
            Cylinder(rail, "Bar", centre + Vector3.up * 1.72f, new Vector3(0.05f, length / 2f, 0.05f), _chrome, false).transform.rotation = Quaternion.LookRotation(Vector3.up, axis);
            foreach (float sign in new[] { -1f, 1f })
            {
                Vector3 post = centre + axis * (length / 2f * sign);
                Block(rail, "Post", post + Vector3.up * 0.86f, new Vector3(0.05f, 1.72f, 0.05f), _chrome, false);
                Block(rail, "Foot", post + Vector3.up * 0.02f, alongZ ? new Vector3(0.6f, 0.04f, 0.06f) : new Vector3(0.06f, 0.04f, 0.6f), _chrome, false);
            }
            var box = rail.gameObject.AddComponent<BoxCollider>();
            box.center = centre + Vector3.up * 0.9f;
            box.size = alongZ ? new Vector3(0.6f, 1.8f, length) : new Vector3(length, 1.8f, 0.6f);
            int count = Mathf.Max(1, Mathf.FloorToInt(length / 0.52f));
            for (int i = 0; i < count; i++)
            {
                var outfit = outfits[i % outfits.Length];
                bool setOnly = i % 3 == 2; // every third hanger shows the whole set with its trousers
                Hanging(rail, centre + axis * ((i - (count - 1) / 2f) * 0.5f) + Vector3.up * 1.7f, garmentYaw, Cloth(outfit.top), setOnly ? Cloth(outfit.bottom) : null);
            }
        }

        private static void BuildRails(Transform floor, Transform spots)
        {
            var catalog = FashionCatalog.Load();
            FashionOutfit[] Of(string id) => catalog.InCategory(id).ToArray();
            // West side: everyday and street. East side: office and sport. Middle: the seasons and the festival rail.
            var rails = new (string name, string category, string ja, Vector3 centre, float length, bool alongZ, float yaw)[]
            {
                ("Rail_Casual", "casual", "ふだん着", P(-12f, 0f, 2f), 9f, true, 90f),
                ("Rail_Street", "street", "ストリート", P(-8.2f, 0f, 1f), 7f, true, 270f),
                ("Rail_Office", "office", "オフィス", P(12f, 0f, 2f), 9f, true, 270f),
                ("Rail_Sport", "sport", "スポーツ", P(8.2f, 0f, -3f), 6f, true, 90f),
                ("Rail_Summer", "summer", "夏", P(10f, 0f, 3.6f), 5f, false, 180f),
                ("Rail_Winter", "winter", "冬", P(4.6f, 0f, -6.4f), 5f, false, 180f),
                ("Rail_Festival", "festival", "お祭り", P(-6f, 0f, -6.4f), 5f, false, 180f),
            };
            foreach (var r in rails)
            {
                Rail(floor, r.name, r.centre, r.length, r.alongZ, Of(r.category), r.yaw);
                Vector3 facing = r.alongZ ? new Vector3(Mathf.Sign(O.x - r.centre.x), 0f, 0f) : new Vector3(0f, 0f, r.yaw > 90f ? 1f : -1f);
                Spot(spots, "Spot_" + r.category, r.centre + facing * 0.9f + Vector3.up, r.alongZ ? new Vector3(1.4f, 2f, r.length) : new Vector3(r.length, 2f, 1.4f), r.category);
                // Readable from both sides of the rail.
                Block(floor, "SignBack_" + r.category, r.centre + Vector3.up * 2.15f, new Vector3(2f, 0.36f, 0.04f), _sign, false, r.yaw);
                foreach (float turn in new[] { 0f, 180f })
                {
                    var side = Quaternion.Euler(0f, r.yaw + turn, 0f);
                    Text(floor, "Sign_" + r.category, r.ja, r.centre + Vector3.up * 2.15f + side * Vector3.back * 0.03f, r.yaw + turn, 0.2f, Color.white, 2.4f);
                }
            }
        }

        private static void BuildTables(Transform floor, Transform job)
        {
            var outfits = FashionCatalog.Load().outfits;
            int colour = 0;
            foreach (var (x, z, id) in new[] { (-3.2f, -3.4f, "table_a"), (3.2f, -3.4f, "table_b"), (3.6f, 5.8f, "table_c") })
            {
                var table = Group(floor, "FoldTable_" + id);
                Block(table, "Top", P(x, 0.82f, z), new Vector3(2.2f, 0.08f, 1.2f), _wood, true);
                foreach (var (dx, dz) in new[] { (-1f, -0.5f), (1f, -0.5f), (-1f, 0.5f), (1f, 0.5f) })
                    Block(table, "Leg", P(x + dx, 0.39f, z + dz), new Vector3(0.08f, 0.78f, 0.08f), _darkWood, true);
                for (int stack = 0; stack < 6; stack++)
                {
                    float sx = x - 0.72f + (stack % 3) * 0.72f, sz = z - 0.28f + (stack / 3) * 0.56f;
                    for (int layer = 0; layer < 3 + stack % 2; layer++)
                        Block(table, "Folded", P(sx, 0.885f + layer * 0.05f, sz), new Vector3(0.54f, 0.045f, 0.42f), Cloth(outfits[colour++ % outfits.Length].top), false);
                }
                Trigger(job, "Fold_" + id, P(x, 1f, z - 1.2f), new Vector3(2.6f, 2f, 1.3f)).AddComponent<JobStation>()
                    .Configure(Quest, JobStationKind.FashionFold, id, "テーブル", "Bàn quần áo");
            }
        }

        /// <summary>A shop mannequin dressed in a real set from the catalogue.</summary>
        private static void Mannequin(Transform parent, Vector3 at, float yaw, FashionOutfit outfit)
        {
            var doll = Group(parent, "Mannequin_" + outfit.id);
            doll.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            var top = Cloth(outfit.top);
            var bottom = Cloth(outfit.bottom);
            Cylinder(doll, "Base", new Vector3(0f, 0.02f, 0f), new Vector3(0.5f, 0.02f, 0.5f), _metal, false);
            Cylinder(doll, "Pole", new Vector3(0f, 0.2f, 0f), new Vector3(0.03f, 0.2f, 0.03f), _metal, false);
            foreach (float x in new[] { -0.1f, 0.1f })
                Cylinder(doll, "Leg", new Vector3(x, 0.52f, 0f), new Vector3(0.13f, 0.34f, 0.13f), bottom, false);
            Cylinder(doll, "Hips", new Vector3(0f, 0.86f, 0f), new Vector3(0.34f, 0.08f, 0.24f), bottom, false);
            Cylinder(doll, "Torso", new Vector3(0f, 1.18f, 0f), new Vector3(0.36f, 0.27f, 0.25f), top, false);
            Cylinder(doll, "Shoulders", new Vector3(0f, 1.44f, 0f), new Vector3(0.42f, 0.05f, 0.25f), top, false);
            foreach (float x in new[] { -0.25f, 0.25f })
            {
                var arm = Cylinder(doll, "Arm", new Vector3(x, 1.16f, 0f), new Vector3(0.09f, 0.28f, 0.09f), top, false);
                arm.transform.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? -7f : 7f);
            }
            Cylinder(doll, "Neck", new Vector3(0f, 1.52f, 0f), new Vector3(0.1f, 0.05f, 0.1f), _mannequin, false);
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(head.GetComponent<Collider>());
            head.name = "HeadForm";
            head.transform.SetParent(doll, false);
            head.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            head.transform.localScale = new Vector3(0.2f, 0.26f, 0.22f);
            head.GetComponent<Renderer>().sharedMaterial = _mannequin;
        }

        private static void BuildMannequins(Transform floor)
        {
            var catalog = FashionCatalog.Load();
            Block(floor, "Plinth", P(0f, 0.1f, 2.4f), new Vector3(4.2f, 0.2f, 1.6f), _white, true);
            string[] centre = { "street_neon", "office_navy", "winter_camel" };
            for (int i = 0; i < centre.Length; i++)
                Mannequin(floor, P((i - 1) * 1.3f, 0.2f, 2.4f), 180f, catalog.Outfit(centre[i]));
            Block(floor, "Plinth_Window", P(-8.2f, 0.1f, -7.8f), new Vector3(3.4f, 0.2f, 1.2f), _white, true);
            Mannequin(floor, P(-9f, 0.2f, -7.8f), 200f, catalog.Outfit("summer_beach"));
            Mannequin(floor, P(-7.4f, 0.2f, -7.8f), 160f, catalog.Outfit("festival_gold"));
            Block(floor, "Plinth_WindowE", P(8.2f, 0.1f, -7.8f), new Vector3(3.4f, 0.2f, 1.2f), _white, true);
            Mannequin(floor, P(7.4f, 0.2f, -7.8f), 200f, catalog.Outfit("casual_white"));
            Mannequin(floor, P(9f, 0.2f, -7.8f), 160f, catalog.Outfit("sport_track"));
        }

        private static void BuildFittingRooms(Transform floor, Transform job, Transform spots)
        {
            var rooms = Group(floor, "FittingRooms");
            for (int i = 0; i < 3; i++)
            {
                float x = -11f + i * 2.2f;
                Block(rooms, "Divider", P(x - 1.1f, 1.2f, 11.6f), new Vector3(0.08f, 2.4f, 2.5f), _wood, true);
                var rail = Cylinder(rooms, "CurtainRail", P(x, 2.34f, 10.4f), new Vector3(0.03f, 1.1f, 0.03f), _chrome, false);
                rail.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                for (int fold = 0; fold < 7; fold++)
                    Block(rooms, "CurtainFold", P(x - 0.72f + fold * 0.24f, 1.25f, 10.4f + (fold % 2) * 0.05f), new Vector3(0.26f, 2.1f, 0.04f), _curtain, true);
                Text(rooms, "Number", (i + 1).ToString(), P(x, 2.6f, 10.36f), 0f, 0.2f, Color.white, 1f);
            }
            Block(rooms, "Divider_End", P(-4.5f, 1.2f, 11.6f), new Vector3(0.08f, 2.4f, 2.5f), _wood, true);
            Block(rooms, "Header", P(-7.7f, 2.7f, 10.36f), new Vector3(7.4f, 0.4f, 0.1f), _sign, false);
            Text(rooms, "Sign", "試着室 · FITTING ROOM", P(-7.7f, 2.7f, 10.3f), 0f, 0.2f, Color.white, 6f);

            // Mirror wall: a tall framed mirror beside the fitting rooms, with room to stand in front of it.
            Block(rooms, "MirrorFrame", P(-1.2f, 1.25f, North - 0.16f), new Vector3(2.6f, 2.5f, 0.06f), _darkWood, false);
            Block(rooms, "Mirror", P(-1.2f, 1.25f, North - 0.2f), new Vector3(2.4f, 2.3f, 0.03f), _mirror, false);
            Spot(spots, "Spot_mirror", P(-1.2f, 1f, North - 1.3f), new Vector3(3f, 2f, 2f), string.Empty);

            var visual = JobSiteBuilder.Person(job, "Customer_Fitting_Visual", P(-6f, 0f, 9.2f), 180f);
            Trigger(job, "Customer_Fitting", P(-6f, 1f, 9.2f), new Vector3(1.6f, 2f, 1.6f)).AddComponent<JobStation>()
                .Configure(Quest, JobStationKind.FashionFitting, "fitting", "しちゃくしつ", "Phòng thử đồ", visual);
        }

        private static void BuildTill(Transform floor, Transform job)
        {
            var till = Group(floor, "Till");
            Block(till, "Counter", P(6.4f, 0.52f, 9f), new Vector3(4.6f, 1.04f, 0.9f), _darkWood, true);
            Block(till, "CounterTop", P(6.4f, 1.07f, 9f), new Vector3(4.8f, 0.06f, 1.05f), _white, false);
            Block(till, "CounterAccent", P(6.4f, 0.5f, 8.53f), new Vector3(4.6f, 0.24f, 0.02f), _accent, false);
            Block(till, "Register", P(5.4f, 1.24f, 9f), new Vector3(0.44f, 0.3f, 0.36f), _metal, false);
            Block(till, "Screen", P(5.4f, 1.46f, 8.96f), new Vector3(0.34f, 0.22f, 0.03f), _lamp, false);
            for (int i = 0; i < 4; i++) Block(till, "Bags", P(7.4f + i * 0.2f, 1.24f, 9f), new Vector3(0.14f, 0.3f, 0.26f), i % 2 == 0 ? _accent : _white, false);
            Block(till, "Back", P(6.4f, 1.6f, North - 0.2f), new Vector3(5f, 3.1f, 0.1f), _wood, false);
            Text(till, "Logo", "ひばりモード", P(6.4f, 2.72f, North - 0.27f), 0f, 0.4f, Color.white, 5f);
            Text(till, "Logo_Sub", "HIBARI MODE", P(6.4f, 2.42f, North - 0.27f), 0f, 0.15f, new Color(1f, 0.85f, 0.92f), 5f);
            Block(till, "TillSign_Back", P(4.4f, 2.1f, 8.6f), new Vector3(0.9f, 0.4f, 0.04f), _sign, false);
            Text(till, "Till_Sign", "レジ", P(4.4f, 2.1f, 8.57f), 0f, 0.24f, Color.white, 1f);

            var manager = (GameObject)PrefabUtility.InstantiatePrefab(CharacterVariants.Prefab("fashion_riku"), floor);
            manager.name = "Manager_Ren";
            manager.transform.SetPositionAndRotation(P(6.4f, 0f, 10.1f), Quaternion.Euler(0f, 180f, 0f));
            var plate = Text(manager.transform, "Nameplate", "れん てんちょう · Anh Ren\n<size=70%>店長</size>", new Vector3(0f, 2.15f, 0f), 0f, 0.11f, new Color(0.95f, 0.95f, 0.9f), 2f);
            plate.gameObject.AddComponent<BillboardUI>().lockRotationXAndZ = true;
            var giver = Trigger(floor, "Manager_Ren_Talk", P(6.4f, 1f, 8.4f), new Vector3(4.6f, 2f, 1.3f)).AddComponent<JobGiver>();
            giver.Configure("npc_fashion_manager", "れん店長", "Anh Ren", Quest);
            var nameEn = new SerializedObject(giver);
            nameEn.FindProperty("nameEn").stringValue = "Ren";
            nameEn.ApplyModifiedPropertiesWithoutUndo();
            Trigger(job, "Till_Register", P(5f, 1f, 8.2f), new Vector3(1.8f, 2f, 1.2f)).AddComponent<JobStation>()
                .Configure(Quest, JobStationKind.FashionRegister, "register", "レジ", "Quầy thu ngân");
        }

        private static void BuildDecor(Transform floor)
        {
            foreach (var (x, z) in new[] { (-12f, -7.8f), (12f, -7.8f), (12f, 11.8f), (0.8f, 11.8f) })
            {
                Cylinder(floor, "PlantPot", P(x, 0.25f, z), new Vector3(0.5f, 0.25f, 0.5f), _white, true);
                var leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(leaves.GetComponent<Collider>());
                leaves.name = "PlantLeaves";
                leaves.transform.SetParent(floor, false);
                leaves.transform.position = P(x, 1.05f, z);
                leaves.transform.localScale = new Vector3(0.9f, 1.2f, 0.9f);
                leaves.GetComponent<Renderer>().sharedMaterial = _plant;
            }
            foreach (var (x, z, yaw, text, sub) in new[]
            {
                (0f, -8.8f, 180f, "ようこそ", "Hibari Mode"),
                (0f, 12.7f, 0f, "ファッションの しごと 募集中", "Fashion staff wanted"),
            })
            {
                Text(floor, "Banner", text, P(x, 3.1f, z), yaw, 0.26f, new Color(0.2f, 0.18f, 0.22f), 7f);
                Text(floor, "BannerSub", sub, P(x, 2.82f, z), yaw, 0.14f, new Color(0.45f, 0.2f, 0.32f), 7f);
            }
        }

        /// <summary>Turns the return spawns of the shops in the town, without rebuilding them.
        /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.FashionStoreBuilder.TurnReturnSpawns</summary>
        public static void TurnReturnSpawns()
        {
            var scene = EditorSceneManager.OpenScene(CityPath, OpenSceneMode.Single);
            var portals = scene.GetRootGameObjects().First(r => r.name == "AdditiveZonePortals").transform;
            foreach (var id in new[] { WorldLocationCatalog.CityFashionStoreReturn, WorldLocationCatalog.CityMusicShopReturn, WorldLocationCatalog.CityClinicReturn, WorldLocationCatalog.CityGameCenterReturn })
            {
                var spawn = portals.Find("Spawn_" + id);
                if (spawn == null) throw new InvalidOperationException("Spawn_" + id + " not found");
                spawn.rotation = ClearOfCanopy;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + CityPath);
            Debug.Log("[FashionStoreBuilder] return spawns turned.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        // ─────────── The entrance in the town ───────────

        private static void BuildCityEntrance()
        {
            var scene = EditorSceneManager.OpenScene(CityPath, OpenSceneMode.Single);
            var building = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == CityBuilding && t.parent != null && t.parent.name == "CityTile_0");
            if (building == null) throw new InvalidOperationException(CityBuilding + " not found in CityTile_0");
            var destinations = scene.GetRootGameObjects().First(r => r.name == "Town_Destinations").transform;
            var portals = scene.GetRootGameObjects().First(r => r.name == "AdditiveZonePortals").transform;
            foreach (var stale in new[] { destinations.Find("Entrance_FashionStore"), portals.Find("FashionStorePortal"), portals.Find("Spawn_" + WorldLocationCatalog.CityFashionStoreReturn) })
                if (stale != null) Object.DestroyImmediate(stale.gameObject);

            Bounds b = BoundsOf(building.gameObject);
            float frontZ = b.max.z; // the south row faces north, toward Sakura-dōri
            var group = Group(destinations, "Entrance_FashionStore");
            group.SetPositionAndRotation(new Vector3(building.position.x, 0f, frontZ), Quaternion.Euler(0f, 180f, 0f));
            // Local: +X along the facade, −Z toward the street.
            Block(group, "Canopy", new Vector3(0f, 2.9f, -1.0f), new Vector3(5.6f, 0.16f, 2.2f), _accent, true);
            Block(group, "CanopyStripe", new Vector3(0f, 2.8f, -2.08f), new Vector3(5.6f, 0.12f, 0.05f), _white, false);
            foreach (float x in new[] { -2.6f, 2.6f })
                Block(group, "Post_" + (x < 0 ? "L" : "R"), new Vector3(x, 1.4f, -1.95f), new Vector3(0.14f, 2.8f, 0.14f), _sign, true);
            Block(group, "SignBoard", new Vector3(0f, 3.6f, -2.1f), new Vector3(5f, 0.9f, 0.14f), _sign, true);
            Block(group, "SignStripe", new Vector3(0f, 3.17f, -2.18f), new Vector3(5f, 0.08f, 0.02f), _accent, false);
            Text(group, "Sign_JA", "ひばりモード", new Vector3(0f, 3.72f, -2.2f), 0f, 0.38f, Color.white, 4.6f);
            Text(group, "Sign_Sub", "HIBARI MODE · ファッション", new Vector3(0f, 3.38f, -2.2f), 0f, 0.13f, new Color(1f, 0.85f, 0.92f), 4.6f);
            Block(group, "Door", new Vector3(0f, 1.15f, -0.05f), new Vector3(2.4f, 2.3f, 0.06f), _glass, false);
            Block(group, "DoorFrame", new Vector3(0f, 2.35f, -0.08f), new Vector3(2.6f, 0.1f, 0.08f), _white, false);
            foreach (float x in new[] { -2.0f, 2.0f })
                Block(group, "ShopWindow_" + (x < 0 ? "L" : "R"), new Vector3(x, 1.3f, -0.06f), new Vector3(1.3f, 1.6f, 0.04f), _glass, false);
            var catalog = FashionCatalog.Load();
            Mannequin(group, Vector3.zero, 0f, catalog.Outfit("street_bomber"));
            Mannequin(group, Vector3.zero, 0f, catalog.Outfit("casual_mint"));
            group.Find("Mannequin_street_bomber").localPosition = new Vector3(-2f, 0f, 0.3f);
            group.Find("Mannequin_street_bomber").localRotation = Quaternion.identity;
            group.Find("Mannequin_casual_mint").localPosition = new Vector3(2f, 0f, 0.3f);
            group.Find("Mannequin_casual_mint").localRotation = Quaternion.identity;
            PointLight(group, "CanopyLight", new Vector3(0f, 2.6f, -1.2f), 5f, 0.7f, new Color(1f, 0.92f, 0.85f));

            var portalObject = Trigger(portals, "FashionStorePortal", Vector3.zero, new Vector3(2.4f, 2.2f, 1.2f));
            portalObject.transform.SetPositionAndRotation(new Vector3(group.position.x, 1.1f, frontZ + 0.6f), Quaternion.Euler(0f, 180f, 0f));
            var portal = portalObject.AddComponent<ScenePortal>();
            portal.Configure(WorldLocationCatalog.FashionStoreScene, WorldLocationCatalog.FashionStoreEntrance, "ひばりモード / Thời trang Hibari Mode", false);
            var so = new SerializedObject(portal);
            so.FindProperty("promptJa").stringValue = "みせに はいる";
            so.FindProperty("promptEn").stringValue = "Vào cửa hàng thời trang";
            so.ApplyModifiedPropertiesWithoutUndo();
            var spawn = new GameObject("Spawn_" + WorldLocationCatalog.CityFashionStoreReturn);
            spawn.transform.SetParent(portals);
            spawn.transform.SetPositionAndRotation(new Vector3(group.position.x, 0.08f, frontZ + 2.4f), ClearOfCanopy);
            spawn.AddComponent<SceneSpawnPoint>().Configure(WorldLocationCatalog.CityFashionStoreReturn);

            foreach (var renderer in group.GetComponentsInChildren<Renderer>(true)) renderer.shadowCastingMode = ShadowCastingMode.Off;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + CityPath);
        }

    }
}
#endif
