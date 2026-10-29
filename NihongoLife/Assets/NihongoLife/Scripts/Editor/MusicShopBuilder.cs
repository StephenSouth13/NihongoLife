#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Music;
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
    /// Hibiki Music &amp; Art (approved scene 55_MusicShop, ひびき楽器店):
    ///  • the music shop (south half): a grand piano and an upright, a guitar wall with amplifiers, the theory bookshelves
    ///    and reading table, a live stage with drums, microphone, speakers and the DJ booth, the easel corner, the counter
    ///    with Mr Hibiki and the CD rack, three job boards (gig, DJ set, painting commission);
    ///  • the gallery (north half, through the archway): every painting of Resources/Art/artworks.json hung in a gilt
    ///    frame with its plaque (title, artist, date) along the walls and a free-standing partition, benches, lights —
    ///    F in front of a painting opens its card (ArtworkSpot);
    ///  • the entrance on StreetBuilding_S_2 in 90_TestSandbox (next to Hibari Clinic), portal + return spawn;
    ///  • build settings.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.MusicShopBuilder.Build
    /// </summary>
    public static class MusicShopBuilder
    {
        public const string ScenePath = "Assets/NihongoLife/Scenes/55_MusicShop.unity";
        private const string CityPath = "Assets/NihongoLife/Scenes/90_TestSandbox.unity";
        private const string MatDir = "Assets/NihongoLife/Materials/MusicShop";
        private const string Kit = "Assets/ThirdParty/Kenney/kenney_furniture-kit/Models/FBX format/";
        private const string CityBuilding = "StreetBuilding_S_2";
        public static readonly Vector3 O = new Vector3(1300f, 0f, 0f);
        private const float HalfX = 12f, South = -8f, North = 24f, Divider = 8f, Height = 3.8f;
        private const float PaintingY = 1.65f, PaintingHeight = 1.1f, PaintingMaxWidth = 1.8f, Gap = 0.75f;

        private static Material _floor, _galleryFloor, _wall, _galleryWall, _ceiling, _wood, _darkWood, _black, _ivory, _glass, _sign, _lamp,
            _gold, _plaque, _stage, _curtain, _metal, _chrome, _red, _blue, _sunburst, _natural, _white, _cd, _neon, _canvas, _rug, _green;

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
                Debug.Log("[MusicShopBuilder] Hibiki Music & Art built.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        /// <summary>Rebuilds only the shop scene, leaving the town untouched.
        /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.MusicShopBuilder.BuildInterior</summary>
        public static void BuildInterior()
        {
            try
            {
                Materials();
                BuildZone();
                AssetDatabase.SaveAssets();
                Debug.Log("[MusicShopBuilder] Hibiki interior rebuilt.");
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
            _floor = Lit(MatDir, "Shop_Floor", new Color(0.55f, 0.4f, 0.28f), 0.45f);
            _galleryFloor = Lit(MatDir, "Gallery_Floor", new Color(0.78f, 0.7f, 0.58f), 0.5f);
            _wall = Lit(MatDir, "Shop_Wall", new Color(0.93f, 0.9f, 0.84f), 0.2f);
            _galleryWall = Lit(MatDir, "Gallery_Wall", new Color(0.97f, 0.96f, 0.93f), 0.15f);
            _ceiling = Lit(MatDir, "Shop_Ceiling", new Color(0.97f, 0.96f, 0.94f), 0.1f);
            _wood = Lit(MatDir, "Shop_Wood", new Color(0.66f, 0.47f, 0.3f), 0.35f);
            _darkWood = Lit(MatDir, "Shop_DarkWood", new Color(0.3f, 0.19f, 0.12f), 0.4f);
            _black = Lit(MatDir, "Piano_Black", new Color(0.04f, 0.04f, 0.05f), 0.92f);
            _ivory = Lit(MatDir, "Piano_Ivory", new Color(0.97f, 0.95f, 0.88f), 0.6f);
            _glass = Lit(MatDir, "Shop_Glass", new Color(0.8f, 0.9f, 0.95f, 0.25f), 0.95f, transparent: true);
            _sign = Lit(MatDir, "Shop_Sign", new Color(0.28f, 0.16f, 0.42f), 0.3f);
            _lamp = Lit(MatDir, "Shop_Lamp", Color.white, 0.2f, new Color(1f, 0.95f, 0.85f) * 1.4f);
            _gold = Lit(MatDir, "Frame_Gold", new Color(0.78f, 0.6f, 0.26f), 0.75f);
            _plaque = Lit(MatDir, "Gallery_Plaque", new Color(0.95f, 0.94f, 0.9f), 0.3f);
            _stage = Lit(MatDir, "Stage_Floor", new Color(0.12f, 0.1f, 0.12f), 0.55f);
            _curtain = Lit(MatDir, "Stage_Curtain", new Color(0.38f, 0.08f, 0.14f), 0.1f);
            _metal = Lit(MatDir, "Shop_Metal", new Color(0.2f, 0.21f, 0.23f), 0.6f);
            _chrome = Lit(MatDir, "Shop_Chrome", new Color(0.8f, 0.8f, 0.82f), 0.9f);
            _red = Lit(MatDir, "Guitar_Red", new Color(0.72f, 0.1f, 0.12f), 0.8f);
            _blue = Lit(MatDir, "Guitar_Blue", new Color(0.15f, 0.32f, 0.65f), 0.8f);
            _sunburst = Lit(MatDir, "Guitar_Sunburst", new Color(0.8f, 0.45f, 0.12f), 0.8f);
            _natural = Lit(MatDir, "Guitar_Natural", new Color(0.86f, 0.68f, 0.44f), 0.7f);
            _white = Lit(MatDir, "Shop_White", new Color(0.96f, 0.96f, 0.95f), 0.5f);
            _cd = Lit(MatDir, "Shop_CD", new Color(0.55f, 0.75f, 0.85f), 0.8f);
            _neon = Lit(MatDir, "Stage_Neon", new Color(0.95f, 0.3f, 0.8f), 0.3f, new Color(0.95f, 0.25f, 0.8f) * 2.2f);
            _canvas = Lit(MatDir, "Easel_Canvas", new Color(0.97f, 0.96f, 0.92f), 0.05f);
            _rug = Lit(MatDir, "Shop_Rug", new Color(0.55f, 0.2f, 0.22f), 0.05f);
            _green = Lit(MatDir, "Shop_Plant", new Color(0.3f, 0.6f, 0.32f), 0.2f);
        }

        private static Vector3 P(float x, float y, float z) => O + new Vector3(x, y, z);

        // ─────────── The building ───────────

        private static void BuildZone()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("HibikiMusicShop_Zone");
            root.AddComponent<SceneZoneVisibility>();
            var shell = Group(root.transform, "Shell");
            var shop = Group(root.transform, "Shop");
            var stage = Group(root.transform, "Stage");
            var gallery = Group(root.transform, "Gallery");
            var spots = Group(root.transform, "Spots");
            var job = Group(root.transform, "Jobs");
            var signs = Group(root.transform, "Signs");

            float depth = North - South, midZ = (North + South) / 2f;
            Block(shell, "Floor_Shop", P(0f, -0.1f, (South + Divider) / 2f), new Vector3(HalfX * 2f, 0.2f, Divider - South), _floor, true);
            Block(shell, "Floor_Gallery", P(0f, -0.1f, (Divider + North) / 2f), new Vector3(HalfX * 2f, 0.2f, North - Divider), _galleryFloor, true);
            Block(shell, "Ceiling", P(0f, Height, midZ), new Vector3(HalfX * 2f, 0.2f, depth), _ceiling, false);
            Block(shell, "Wall_N", P(0f, Height / 2f, North), new Vector3(HalfX * 2f, Height, 0.25f), _galleryWall, true);
            Block(shell, "Wall_W_Shop", P(-HalfX, Height / 2f, (South + Divider) / 2f), new Vector3(0.25f, Height, Divider - South), _wall, true);
            Block(shell, "Wall_E_Shop", P(HalfX, Height / 2f, (South + Divider) / 2f), new Vector3(0.25f, Height, Divider - South), _wall, true);
            Block(shell, "Wall_W_Gallery", P(-HalfX, Height / 2f, (Divider + North) / 2f), new Vector3(0.25f, Height, North - Divider), _galleryWall, true);
            Block(shell, "Wall_E_Gallery", P(HalfX, Height / 2f, (Divider + North) / 2f), new Vector3(0.25f, Height, North - Divider), _galleryWall, true);
            Block(shell, "Wall_S_L", P(-6.75f, Height / 2f, South), new Vector3(10.5f, Height, 0.25f), _wall, true);
            Block(shell, "Wall_S_R", P(6.75f, Height / 2f, South), new Vector3(10.5f, Height, 0.25f), _wall, true);
            Block(shell, "Wall_S_Header", P(0f, (2.4f + Height) / 2f, South), new Vector3(3f, Height - 2.4f, 0.25f), _wall, true);
            Block(shell, "EntranceGlass", P(0f, 1.2f, South - 0.06f), new Vector3(3f, 2.4f, 0.04f), _glass, false);
            // The divider between shop and gallery, with an archway in the middle.
            Block(shell, "Divider_L", P(-6.75f, Height / 2f, Divider), new Vector3(10.5f, Height, 0.25f), _wall, true);
            Block(shell, "Divider_R", P(6.75f, Height / 2f, Divider), new Vector3(10.5f, Height, 0.25f), _wall, true);
            Block(shell, "Divider_Header", P(0f, (2.9f + Height) / 2f, Divider), new Vector3(3f, Height - 2.9f, 0.25f), _wall, true);
            foreach (float x in new[] { -1.55f, 1.55f })
                Block(shell, "Arch_Post_" + (x < 0 ? "L" : "R"), P(x, 1.45f, Divider), new Vector3(0.18f, 2.9f, 0.36f), _darkWood, false);
            Block(shell, "Arch_Beam", P(0f, 2.95f, Divider), new Vector3(3.3f, 0.14f, 0.36f), _darkWood, false);
            // Wainscot and skirting in the shop, a picture rail in the gallery.
            foreach (var (name, pos, size) in new[]
            {
                ("Skirting_W", P(-HalfX + 0.14f, 0.06f, 0f), new Vector3(0.03f, 0.12f, Divider - South)),
                ("Skirting_E", P(HalfX - 0.14f, 0.06f, 0f), new Vector3(0.03f, 0.12f, Divider - South)),
                ("PictureRail_N", P(0f, 3.05f, North - 0.14f), new Vector3(HalfX * 2f, 0.04f, 0.03f)),
                ("PictureRail_W", P(-HalfX + 0.14f, 3.05f, 16f), new Vector3(0.03f, 0.04f, North - Divider)),
                ("PictureRail_E", P(HalfX - 0.14f, 3.05f, 16f), new Vector3(0.03f, 0.04f, North - Divider)),
            })
                Block(shell, name, pos, size, _darkWood, false);
            Block(shop, "EntranceRug", P(0f, 0.006f, -6.4f), new Vector3(2.6f, 0.012f, 2.6f), _rug, false);

            BuildIslands(shop);
            BuildPianos(shop, spots);
            BuildGuitars(shop, spots);
            BuildStage(stage, spots);
            BuildTheory(shop, spots);
            BuildEasel(shop, spots);
            BuildCounter(shop, spots, signs);
            BuildGallery(gallery);

            // Jobs: three boards along the south wall, by the door.
            JobSiteBuilder.Materials();
            JobSiteBuilder.Board(job, P(-3.4f, 0f, South + 0.3f), 180f, MusicShop.GigQuest, "ライブ ぼしゅう", "Biểu diễn buổi tối · ¥650/buổi");
            JobSiteBuilder.Board(job, P(-5.4f, 0f, South + 0.3f), 180f, MusicShop.DjQuest, "DJ ぼしゅう", "Chơi DJ · ¥650/buổi");
            JobSiteBuilder.Board(job, P(-7.4f, 0f, South + 0.3f), 180f, MusicShop.ArtQuest, "え の いらい", "Vẽ tranh theo đơn · ¥700/bức");

            // Shop signs.
            Block(signs, "MainSign_Back", P(5.5f, 2.95f, South + 0.16f), new Vector3(5f, 0.7f, 0.03f), _sign, false);
            Text(signs, "MainSign", "ひびき楽器店  <size=60%>MUSIC & ART</size>\n<size=50%>Tiệm nhạc & tranh Hibiki</size>", P(5.5f, 2.95f, South + 0.2f), 180f, 0.22f, Color.white, 4.8f);
            Text(signs, "Sign_Gallery_Shop", "ギャラリー  <size=60%>Phòng tranh · Gallery</size>  ↑", P(0f, 3.3f, Divider - 0.16f), 0f, 0.17f, _sign.color, 3.2f);
            Text(signs, "Sign_Gallery", "ひびきギャラリー\n<size=55%>Phòng tranh Hibiki · 名画の部屋</size>", P(0f, 3.3f, Divider + 0.16f), 180f, 0.2f, _darkWood.color, 3.4f);
            Text(signs, "Hours", "えいぎょう 10:00〜21:00\n<size=70%>Mở cửa 10:00–21:00</size>", P(-1.2f, 1.6f, South + 0.16f), 180f, 0.08f, _sign.color, 1.4f);

            // Lights: warm pendants in the shop, gallery lights along the walls, the stage's colours.
            for (int ix = -1; ix <= 1; ix++)
                for (int iz = 0; iz < 2; iz++)
                {
                    Vector3 at = P(ix * 7f, Height - 0.5f, -4.5f + iz * 7.5f);
                    Cylinder(shop, $"Pendant_{ix}_{iz}", at + Vector3.up * 0.25f, new Vector3(0.02f, 0.25f, 0.02f), _metal, false);
                    Cylinder(shop, $"PendantShade_{ix}_{iz}", at, new Vector3(0.45f, 0.08f, 0.45f), _lamp, false);
                    PointLight(root.transform, $"ShopLight_{ix}_{iz}", at + Vector3.down * 0.3f, 9f, 2.2f, new Color(1f, 0.9f, 0.76f));
                }
            foreach (var (x, z) in new[] { (-7f, 11.5f), (0f, 11.5f), (7f, 11.5f), (-7f, 20.5f), (0f, 20.5f), (7f, 20.5f) })
            {
                Block(gallery, $"CeilingLamp_{x}_{z}", P(x, Height - 0.12f, z), new Vector3(1.6f, 0.04f, 0.5f), _lamp, false);
                PointLight(root.transform, $"GalleryLight_{x}_{z}", P(x, Height - 0.45f, z), 9f, 2.3f, new Color(1f, 0.97f, 0.9f));
            }
            root.AddComponent<IndoorAmbience>().Configure(new Color(0.74f, 0.72f, 0.7f));
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.6f, 0.58f);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;

            // Owner and two customers.
            Person(shop, "Owner_Hibiki", P(5.5f, 0f, -6.1f), 0f, "ひびき てんちょう · Chú Hibiki", "店長");
            Person(shop, "Customer_Guitar", P(-3.2f, 0f, 5.8f), 20f, "おきゃくさん", "ギターを えらび中");
            Person(gallery, "Visitor_Gallery", P(-6.5f, 0f, 21.6f), 0f, "おきゃくさん", "名画を 見ています");

            // Spawn and exit.
            Spawn(root.transform, WorldLocationCatalog.MusicShopEntrance, P(0f, 0.05f, -6.6f), 0f);
            var exit = Trigger(root.transform, "ExitToCity", P(0f, 1.1f, South + 0.55f), new Vector3(2.8f, 2.2f, 1f));
            var portal = exit.AddComponent<ScenePortal>();
            portal.Configure(WorldLocationCatalog.CityScene, WorldLocationCatalog.CityMusicShopReturn, "街へ戻る / Trở lại thành phố", true);
            var so = new SerializedObject(portal);
            so.FindProperty("promptJa").stringValue = "そとに でる";
            so.FindProperty("promptEn").stringValue = "Ra phố";
            so.ApplyModifiedPropertiesWithoutUndo();

            var preview = new GameObject("PreviewCamera") { tag = "MainCamera" };
            preview.transform.SetParent(root.transform);
            preview.transform.position = P(0f, 2.8f, -7.4f);
            preview.transform.rotation = Quaternion.LookRotation(P(0f, 1f, 3f) - preview.transform.position);
            preview.AddComponent<Camera>().fieldOfView = 64f;
            preview.AddComponent<AudioListener>();
            preview.AddComponent<NihongoLife.Cameras.ZonePreviewCamera>();

            foreach (var r in root.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true; }
            if (!EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath)) throw new InvalidOperationException("Could not save " + ScenePath);
        }

        // ─────────── Instruments ───────────

        private static void BuildPianos(Transform parent, Transform spots)
        {
            // A grand piano (keyboard to the south) on a rug, with its bench and an open lid.
            var grand = Group(parent, "GrandPiano");
            float gx = -6.8f, gz = 2.4f;
            Block(grand, "Rug", P(gx, 0.006f, gz - 0.4f), new Vector3(3.4f, 0.012f, 3.8f), _rug, false);
            Block(grand, "Body", P(gx, 0.84f, gz), new Vector3(1.5f, 0.32f, 1.3f), _black, true);
            var tail = Cylinder(grand, "Tail", P(gx + 0.08f, 0.84f, gz + 0.62f), new Vector3(1.38f, 0.16f, 1.3f), _black, false);
            Block(grand, "KeyBed", P(gx, 0.68f, gz - 0.78f), new Vector3(1.5f, 0.14f, 0.32f), _black, false);
            Block(grand, "WhiteKeys", P(gx, 0.76f, gz - 0.8f), new Vector3(1.32f, 0.03f, 0.2f), _ivory, false);
            for (int i = 0; i < 26; i++)
            {
                int note = i % 5; // two-three pattern of black keys
                float x = gx - 0.63f + i * 0.05f + (note >= 2 ? 0.012f : 0f);
                Block(grand, "BlackKey_" + i, P(x, 0.785f, gz - 0.75f), new Vector3(0.022f, 0.022f, 0.11f), _black, false);
            }
            Block(grand, "Fallboard", P(gx, 0.86f, gz - 0.66f), new Vector3(1.38f, 0.1f, 0.04f), _black, false);
            Block(grand, "MusicDesk", P(gx, 1.1f, gz - 0.58f), new Vector3(0.62f, 0.28f, 0.02f), _black, false, 0f).transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            var lid = Block(grand, "Lid", P(gx + 0.42f, 1.3f, gz + 0.25f), new Vector3(1.4f, 0.025f, 1.9f), _black, false);
            lid.transform.localRotation = Quaternion.Euler(0f, 0f, 34f);
            Cylinder(grand, "LidStick", P(gx + 0.66f, 1.15f, gz + 0.3f), new Vector3(0.025f, 0.3f, 0.025f), _black, false);
            foreach (var (x, z) in new[] { (-0.65f, -0.5f), (0.65f, -0.5f), (0.1f, 1.1f) })
                Cylinder(grand, "Leg", P(gx + x, 0.34f, gz + z), new Vector3(0.11f, 0.34f, 0.11f), _black, false);
            Block(grand, "Bench", P(gx, 0.5f, gz - 1.45f), new Vector3(0.95f, 0.08f, 0.38f), _black, false);
            foreach (float x in new[] { -0.4f, 0.4f })
                Block(grand, "BenchLeg", P(gx + x, 0.25f, gz - 1.45f), new Vector3(0.06f, 0.5f, 0.3f), _black, false);
            Text(grand, "Label", "グランドピアノ\n<size=60%>Đàn grand piano · F để chơi</size>", P(gx, 2.35f, gz + 0.2f), 0f, 0.09f, _ivory.color, 1.6f).gameObject.AddComponent<BillboardUI>().lockRotationXAndZ = true;
            Spot(spots, "Spot_Piano", P(gx, 1f, gz - 1.9f), new Vector3(2.4f, 2f, 1.4f), MusicSpotKind.Piano).Configure(MusicSpotKind.Piano, P(gx, 0.02f, gz - 1.42f), 0f); // on the bench, facing the keys

            // An upright piano against the west wall, and a digital piano row for trying.
            var upright = Group(parent, "UprightPiano");
            Block(upright, "Body", P(-11.55f, 0.66f, 5.3f), new Vector3(0.6f, 1.32f, 1.5f), _darkWood, true);
            Block(upright, "KeyShelf", P(-11.12f, 0.72f, 5.3f), new Vector3(0.3f, 0.06f, 1.36f), _darkWood, false);
            Block(upright, "Keys", P(-11.1f, 0.765f, 5.3f), new Vector3(0.18f, 0.03f, 1.26f), _ivory, false);
            Block(upright, "Stool", P(-10.4f, 0.24f, 5.3f), new Vector3(0.4f, 0.48f, 0.7f), _black, true);
            for (int i = 0; i < 3; i++)
            {
                float z = -1.4f + i * 1.25f;
                Block(upright, "DigitalPiano_" + i, P(-11.2f, 0.78f, z), new Vector3(0.38f, 0.08f, 1.15f), _black, false);
                Block(upright, "DigitalKeys_" + i, P(-11.05f, 0.83f, z), new Vector3(0.14f, 0.02f, 1.05f), _ivory, false);
                Block(upright, "DigitalStand_" + i, P(-11.2f, 0.37f, z), new Vector3(0.3f, 0.74f, 0.05f), _metal, true);
                Text(upright, "PriceTag_" + i, $"¥{48000 + i * 22000:N0}", P(-10.98f, 0.62f, z), -90f, 0.06f, _sign.color, 0.6f); // faces the room (+X); 90° read mirrored
            }
            Spot(spots, "Spot_DigitalPianos", P(-10.2f, 1f, 0.6f), new Vector3(1.4f, 2f, 4f), MusicSpotKind.Piano);
        }

        /// <summary>Display islands either side of the walkway: ukuleles and violins on a table with a rug, a CD listening
        /// bin with headphones, and two guitars on floor stands.</summary>
        private static void BuildIslands(Transform parent)
        {
            var island = Group(parent, "DisplayIsland");
            float ix = -4.2f, iz = -3.2f;
            Block(island, "Rug", P(ix, 0.006f, iz), new Vector3(3.2f, 0.012f, 2.4f), _rug, false);
            Block(island, "Table", P(ix, 0.42f, iz), new Vector3(2.2f, 0.84f, 1.1f), _darkWood, true);
            Block(island, "TableTop", P(ix, 0.85f, iz), new Vector3(2.3f, 0.03f, 1.2f), _wood, false);
            var bodies = new[] { _natural, _sunburst, _red, _blue };
            for (int i = 0; i < 4; i++)
            {
                float x = ix - 0.8f + i * 0.53f;
                Cylinder(island, "Ukulele_" + i, P(x, 0.9f, iz - 0.2f), new Vector3(0.22f, 0.02f, 0.3f), bodies[i], false);
                Block(island, "UkuleleNeck_" + i, P(x, 0.9f, iz + 0.12f), new Vector3(0.04f, 0.02f, 0.36f), _darkWood, false);
            }
            for (int i = 0; i < 2; i++)
            {
                float x = ix - 0.4f + i * 0.8f;
                Cylinder(island, "ViolinCase_" + i, P(x, 0.92f, iz + 0.38f), new Vector3(0.3f, 0.04f, 0.5f), _black, false);
            }
            Text(island, "Sign", "ウクレレ・バイオリン\n<size=60%>Ukulele · Violin</size>", P(ix, 1.25f, iz - 0.58f), 0f, 0.09f, _sign.color, 1.8f).gameObject.AddComponent<BillboardUI>().lockRotationXAndZ = true;
            foreach (var (x, z, m) in new[] { (-2.6f, 5.9f, _sunburst), (-9.8f, 6.0f, _blue) })
            {
                Block(island, "Stand_" + x, P(x, 0.3f, z), new Vector3(0.36f, 0.06f, 0.3f), _metal, false);
                Cylinder(island, "StandGuitar_" + x, P(x, 0.62f, z), new Vector3(0.4f, 0.035f, 0.4f), m, false).transform.localRotation = Quaternion.Euler(80f, 0f, 0f);
                Cylinder(island, "StandGuitarTop_" + x, P(x, 0.9f, z - 0.03f), new Vector3(0.31f, 0.035f, 0.31f), m, false).transform.localRotation = Quaternion.Euler(80f, 0f, 0f);
                Block(island, "StandNeck_" + x, P(x, 1.3f, z - 0.09f), new Vector3(0.055f, 0.6f, 0.03f), _darkWood, false).transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
            }

            var bin = Group(parent, "ListeningBin");
            float bx = 3.6f, bz = -0.6f;
            Block(bin, "Bin", P(bx, 0.45f, bz), new Vector3(1.8f, 0.9f, 0.9f), _darkWood, true);
            var sleeves = new[] { _red, _blue, _sunburst, _green, _sign, _cd, _white };
            for (int i = 0; i < 14; i++)
                Block(bin, "Record_" + i, P(bx - 0.78f + i * 0.12f, 0.98f, bz), new Vector3(0.02f, 0.32f, 0.32f), sleeves[i % sleeves.Length], false);
            Block(bin, "ListeningPost", P(bx + 1.2f, 0.6f, bz), new Vector3(0.3f, 1.2f, 0.3f), _metal, true);
            Block(bin, "Headphones", P(bx + 1.2f, 1.25f, bz - 0.1f), new Vector3(0.22f, 0.18f, 0.06f), _black, false);
            Text(bin, "Sign", "しちょう コーナー\n<size=60%>Nghe thử · Listening</size>", P(bx, 1.5f, bz - 0.46f), 0f, 0.09f, _sign.color, 1.8f).gameObject.AddComponent<BillboardUI>().lockRotationXAndZ = true;
        }

        private static void BuildGuitars(Transform parent, Transform spots)
        {
            var wall = Group(parent, "GuitarWall");
            float faceZ = Divider - 0.14f;
            var bodies = new[] { _sunburst, _natural, _red, _black, _blue, _white, _sunburst, _natural };
            for (int i = 0; i < bodies.Length; i++)
            {
                float x = -10.6f + i * 1.0f;
                var g = Group(wall, "Guitar_" + i);
                Cylinder(g, "LowerBout", P(x, 1.32f, faceZ - 0.06f), new Vector3(0.4f, 0.035f, 0.4f), bodies[i], false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Cylinder(g, "UpperBout", P(x, 1.6f, faceZ - 0.06f), new Vector3(0.31f, 0.035f, 0.31f), bodies[i], false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Cylinder(g, "SoundHole", P(x, 1.45f, faceZ - 0.1f), new Vector3(0.09f, 0.004f, 0.09f), _black, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Block(g, "Bridge", P(x, 1.25f, faceZ - 0.1f), new Vector3(0.12f, 0.025f, 0.01f), _darkWood, false);
                Block(g, "Neck", P(x, 2.0f, faceZ - 0.07f), new Vector3(0.055f, 0.62f, 0.03f), _darkWood, false);
                Block(g, "Headstock", P(x, 2.38f, faceZ - 0.07f), new Vector3(0.085f, 0.16f, 0.03f), _darkWood, false);
                Block(g, "Hanger", P(x, 2.3f, faceZ - 0.02f), new Vector3(0.12f, 0.03f, 0.06f), _metal, false);
                Text(g, "Price", $"¥{19800 + i * 7000:N0}", P(x, 0.98f, faceZ - 0.04f), 0f, 0.055f, _sign.color, 0.6f);
            }
            Text(wall, "Sign", "ギター  <size=60%>Guitar</size>", P(-7.1f, 2.85f, faceZ - 0.02f), 0f, 0.2f, _sign.color, 2.4f);
            foreach (var (x, w) in new[] { (-11.2f, 0.55f), (-2.8f, 0.7f) })
            {
                Block(wall, "Amp_" + x, P(x, 0.35f, faceZ - 0.35f), new Vector3(w, 0.7f, 0.35f), _black, true);
                Block(wall, "AmpGrille_" + x, P(x, 0.32f, faceZ - 0.53f), new Vector3(w - 0.08f, 0.45f, 0.01f), _metal, false);
            }
            Spot(spots, "Spot_Guitar", P(-6.6f, 1f, 6.6f), new Vector3(5f, 2f, 1.6f), MusicSpotKind.Guitar);
        }

        private static void BuildStage(Transform parent, Transform spots)
        {
            const float x0 = 4f, x1 = 11.75f, z0 = 4f, z1 = 7.85f, h = 0.2f;
            float cx = (x0 + x1) / 2f, cz = (z0 + z1) / 2f;
            Block(parent, "Platform", P(cx, h / 2f, cz), new Vector3(x1 - x0, h, z1 - z0), _stage, true);
            Block(parent, "Edge", P(cx, h + 0.005f, z0 + 0.04f), new Vector3(x1 - x0, 0.01f, 0.06f), _neon, false);
            Block(parent, "Backdrop", P(cx, Height / 2f, z1 - 0.02f), new Vector3(x1 - x0, Height, 0.04f), _curtain, false);
            Text(parent, "StageSign", "LIVE  <size=70%>ひびきステージ</size>", P(cx, 2.95f, z1 - 0.06f), 0f, 0.3f, _neon.color, 5f);

            // Drums at the back.
            var drums = Group(parent, "Drums");
            float dx = 9.9f, dz = 6.7f;
            Cylinder(drums, "BassDrum", P(dx, h + 0.29f, dz), new Vector3(0.56f, 0.18f, 0.56f), _red, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Cylinder(drums, "BassHead", P(dx, h + 0.29f, dz - 0.19f), new Vector3(0.5f, 0.005f, 0.5f), _white, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Cylinder(drums, "Snare", P(dx - 0.55f, h + 0.62f, dz - 0.35f), new Vector3(0.36f, 0.07f, 0.36f), _chrome, false);
            Cylinder(drums, "Tom_1", P(dx - 0.2f, h + 0.8f, dz - 0.05f), new Vector3(0.28f, 0.1f, 0.28f), _red, false);
            Cylinder(drums, "Tom_2", P(dx + 0.22f, h + 0.8f, dz - 0.05f), new Vector3(0.3f, 0.1f, 0.3f), _red, false);
            Cylinder(drums, "FloorTom", P(dx + 0.6f, h + 0.3f, dz - 0.35f), new Vector3(0.42f, 0.2f, 0.42f), _red, false);
            foreach (var (name, x, z, y) in new[] { ("Crash", -0.6f, 0.15f, 1.35f), ("Ride", 0.75f, 0.1f, 1.25f), ("HiHat", -0.95f, -0.25f, 0.95f) })
            {
                Cylinder(drums, name + "_Stand", P(dx + x, h + y / 2f, dz + z), new Vector3(0.025f, y / 2f, 0.025f), _chrome, false);
                Cylinder(drums, name, P(dx + x, h + y, dz + z), new Vector3(0.42f, 0.004f, 0.42f), _gold, false);
            }
            Cylinder(drums, "Throne", P(dx, h + 0.25f, dz + 0.65f), new Vector3(0.36f, 0.25f, 0.36f), _black, false);
            Block(drums, "DrumsCollider", P(dx, h + 0.6f, dz), new Vector3(2f, 1.2f, 1.6f), _black, true).GetComponent<Renderer>().enabled = false;

            // The microphone at the front, the speakers either side, the DJ booth.
            Cylinder(parent, "MicBase", P(7.6f, h + 0.01f, 4.7f), new Vector3(0.3f, 0.01f, 0.3f), _black, false);
            Cylinder(parent, "MicPole", P(7.6f, h + 0.75f, 4.7f), new Vector3(0.025f, 0.75f, 0.025f), _chrome, false);
            Block(parent, "Mic", P(7.6f, h + 1.55f, 4.62f), new Vector3(0.05f, 0.05f, 0.18f), _black, false);
            foreach (float x in new[] { x0 + 0.35f, x1 - 0.35f })
            {
                Block(parent, "Speaker_" + (x < cx ? "L" : "R"), P(x, h + 0.6f, z0 + 0.35f), new Vector3(0.6f, 1.2f, 0.5f), _black, true);
                Cylinder(parent, "Woofer", P(x, h + 0.42f, z0 + 0.09f), new Vector3(0.36f, 0.005f, 0.36f), _metal, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Cylinder(parent, "Tweeter", P(x, h + 0.92f, z0 + 0.09f), new Vector3(0.14f, 0.005f, 0.14f), _metal, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            var booth = Group(parent, "DJBooth");
            float bx = 5.6f, bz = 6.2f;
            Block(booth, "Table", P(bx, h + 0.48f, bz), new Vector3(1.7f, 0.96f, 0.7f), _black, true);
            Block(booth, "Front", P(bx, h + 0.48f, bz - 0.36f), new Vector3(1.7f, 0.5f, 0.01f), _neon, false);
            Text(booth, "FrontText", "DJ", P(bx, h + 0.48f, bz - 0.38f), 0f, 0.3f, Color.white, 1f);
            foreach (float x in new[] { -0.52f, 0.52f })
            {
                Block(booth, "Deck", P(bx + x, h + 0.98f, bz), new Vector3(0.5f, 0.05f, 0.42f), _metal, false);
                Cylinder(booth, "Platter", P(bx + x - 0.03f, h + 1.02f, bz), new Vector3(0.34f, 0.01f, 0.34f), _black, false);
            }
            Block(booth, "Mixer", P(bx, h + 0.99f, bz), new Vector3(0.3f, 0.06f, 0.38f), _chrome, false);
            Block(booth, "Laptop", P(bx + 0.05f, h + 1.12f, bz + 0.25f), new Vector3(0.34f, 0.22f, 0.01f), _metal, false).transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);

            PointLight(parent, "StageLight_Magenta", P(6f, Height - 0.4f, 5.4f), 6f, 2.4f, new Color(1f, 0.35f, 0.75f));
            PointLight(parent, "StageLight_Cyan", P(9.8f, Height - 0.4f, 5.4f), 6f, 2.4f, new Color(0.35f, 0.8f, 1f));
            Block(parent, "LightingTruss", P(cx, Height - 0.3f, z0 + 0.3f), new Vector3(x1 - x0, 0.12f, 0.12f), _metal, false);

            Spot(spots, "Spot_Stage", P(7.8f, 1f, z0 - 0.9f), new Vector3(2.4f, 2f, 1.6f), MusicSpotKind.Stage).Configure(MusicSpotKind.Stage, P(7.6f, h + 0.02f, 5.3f), 180f); // at the microphone, facing the audience
            Spot(spots, "Spot_DJ", P(5.0f, 1f, z0 - 0.9f), new Vector3(2.2f, 2f, 1.6f), MusicSpotKind.DjBooth);
            Text(parent, "Hint_Stage", "ステージ\n<size=60%>Biểu diễn · F</size>", P(7.8f, 1.2f, z0 - 0.02f), 0f, 0.1f, Color.white, 1.4f);
            Text(parent, "Hint_DJ", "DJ ブース\n<size=60%>Chơi DJ · F</size>", P(5.0f, 1.2f, z0 - 0.02f), 0f, 0.1f, Color.white, 1.4f);
        }

        private static void BuildTheory(Transform parent, Transform spots)
        {
            var corner = Group(parent, "TheoryCorner");
            Furn(corner, "bookcaseOpen", "Bookcase_A", P(HalfX - 0.35f, 0f, -2.6f), -90f, new Vector3(1.4f, 2.0f, 0.45f), true);
            Furn(corner, "bookcaseOpen", "Bookcase_B", P(HalfX - 0.35f, 0f, -1.0f), -90f, new Vector3(1.4f, 2.0f, 0.45f), true);
            Furn(corner, "bookcaseOpenLow", "SheetMusic", P(HalfX - 0.35f, 0f, 0.6f), -90f, new Vector3(1.4f, 0.9f, 0.45f), true);
            var books = new[] { _red, _blue, _sunburst, _green, _sign, _natural };
            for (int i = 0; i < 12; i++)
                Block(corner, "Score_" + i, P(HalfX - 0.38f, 0.95f, 0.05f + i * 0.1f), new Vector3(0.3f, 0.32f, 0.03f), books[i % books.Length], false, 0f);
            Furn(corner, "table", "ReadingTable", P(9.3f, 0f, -1.8f), 0f, new Vector3(1.6f, 0.75f, 0.9f), true);
            Furn(corner, "chair", "ReadingChair_A", P(8.3f, 0f, -1.8f), 90f, new Vector3(0.5f, 0.9f, 0.5f), true);
            Furn(corner, "chair", "ReadingChair_B", P(10.3f, 0f, -1.8f), -90f, new Vector3(0.5f, 0.9f, 0.5f), true);
            Block(corner, "OpenBook", P(9.3f, 0.77f, -1.8f), new Vector3(0.42f, 0.02f, 0.3f), _ivory, false, 8f);
            Furn(corner, "lampRoundTable", "ReadingLamp", P(9.85f, 0.75f, -1.55f), 0f, new Vector3(0.25f, 0.45f, 0.25f), false);
            Text(corner, "Sign", "がくふ・おんがくりろん\n<size=60%>Bản nhạc · Nhạc lý · Music theory</size>", P(HalfX - 0.16f, 2.55f, -1.0f), 90f, 0.15f, _sign.color, 3f);
            Spot(spots, "Spot_Theory", P(9.6f, 1f, -0.4f), new Vector3(3f, 2f, 1.2f), MusicSpotKind.Theory);
        }

        private static void BuildEasel(Transform parent, Transform spots)
        {
            var corner = Group(parent, "EaselCorner");
            float ex = -10.2f, ez = -3.4f;
            foreach (float z in new[] { -0.32f, 0.32f })
                Block(corner, "Leg", P(ex, 0.85f, ez + z), new Vector3(0.05f, 1.7f, 0.05f), _wood, false);
            Block(corner, "BackLeg", P(ex - 0.35f, 0.8f, ez), new Vector3(0.05f, 1.6f, 0.05f), _wood, false).transform.localRotation = Quaternion.Euler(0f, 0f, -14f);
            Block(corner, "Ledge", P(ex + 0.04f, 0.82f, ez), new Vector3(0.1f, 0.04f, 0.8f), _wood, false);
            var art = MusicCatalog.ArtworkById("met-436528") ?? MusicCatalog.Artworks.FirstOrDefault();
            Block(corner, "CanvasBoard", P(ex + 0.03f, 1.32f, ez), new Vector3(0.03f, 0.78f, 0.98f), _canvas, false);
            if (art != null)
            {
                var (aw, ah) = Size(art);
                float k = Mathf.Min(0.94f / aw, 0.74f / ah);
                Picture(corner, "Canvas", P(ex + 0.05f, 1.32f, ez), aw * k, ah * k, ArtMaterial(art), -90f);
            }
            Block(corner, "Collider", P(ex, 0.85f, ez), new Vector3(0.6f, 1.7f, 0.9f), _canvas, true).GetComponent<Renderer>().enabled = false;
            Block(corner, "PaintTable", P(ex + 0.2f, 0.36f, ez + 1.2f), new Vector3(0.6f, 0.72f, 0.45f), _wood, true);
            Cylinder(corner, "Palette", P(ex + 0.2f, 0.73f, ez + 1.2f), new Vector3(0.34f, 0.006f, 0.26f), _natural, false);
            var dabs = new[] { _red, _blue, _sunburst, _green, _white };
            for (int i = 0; i < dabs.Length; i++)
                Cylinder(corner, "Dab_" + i, P(ex + 0.1f + (i % 3) * 0.08f, 0.74f, ez + 1.12f + (i / 3) * 0.1f), new Vector3(0.05f, 0.004f, 0.05f), dabs[i], false);
            Block(corner, "SpareCanvas_A", P(-11.7f, 0.5f, -5.6f), new Vector3(0.04f, 1f, 0.8f), _canvas, false);
            Block(corner, "SpareCanvas_B", P(-11.65f, 0.4f, -5.0f), new Vector3(0.04f, 0.8f, 0.6f), _canvas, false);
            Text(corner, "Sign", "アトリエ\n<size=60%>Góc vẽ tranh · Easel</size>", P(-HalfX + 0.16f, 2.55f, ez), -90f, 0.15f, _sign.color, 2.4f);
            Spot(spots, "Spot_Easel", P(ex + 1.4f, 1f, ez), new Vector3(1.6f, 2f, 1.8f), MusicSpotKind.Easel);
        }

        private static void BuildCounter(Transform parent, Transform spots, Transform signs)
        {
            var counter = Group(parent, "Counter");
            Block(counter, "Counter", P(5.5f, 0.53f, -5.0f), new Vector3(4f, 1.06f, 0.7f), _darkWood, true);
            Block(counter, "CounterTop", P(5.5f, 1.08f, -5.0f), new Vector3(4.1f, 0.04f, 0.8f), _wood, false);
            Block(counter, "Register", P(6.6f, 1.2f, -5.05f), new Vector3(0.36f, 0.2f, 0.32f), _metal, false);
            Block(counter, "RegisterScreen", P(6.6f, 1.36f, -5.15f), new Vector3(0.26f, 0.14f, 0.02f), _cd, false);
            for (int i = 0; i < 6; i++)
                Block(counter, "PickJar_" + i, P(4.2f + i * 0.12f, 1.14f, -4.85f), new Vector3(0.08f, 0.08f, 0.08f), i % 2 == 0 ? _red : _blue, false);
            // CD and record rack behind the counter.
            Block(counter, "CDRack", P(5.5f, 1.1f, South + 0.3f), new Vector3(4f, 2.2f, 0.35f), _wood, true);
            var sleeves = new[] { _red, _blue, _sunburst, _green, _sign, _cd, _white, _black };
            for (int row = 0; row < 4; row++)
                for (int i = 0; i < 14; i++)
                    Block(counter, $"CD_{row}_{i}", P(3.75f + i * 0.25f, 0.45f + row * 0.48f, South + 0.5f), new Vector3(0.2f, 0.32f, 0.03f), sleeves[(row * 3 + i) % sleeves.Length], false);
            Text(signs, "Sign_Counter", "おかいけい\n<size=60%>Quầy · Counter</size>", P(5.5f, 1.7f, -4.6f), 0f, 0.13f, _sign.color, 2f);
            Spot(spots, "Spot_Counter", P(5.5f, 1f, -3.9f), new Vector3(3f, 2f, 1.3f), MusicSpotKind.Counter);
        }

        // ─────────── The gallery ───────────

        /// <summary>Every painting in a gilt frame with its plaque: along the north, west and east walls, the gallery side of
        /// the divider and both faces of a free-standing partition; each segment's paintings are centred with even gaps.</summary>
        private static void BuildGallery(Transform gallery)
        {
            Block(gallery, "Partition", P(0f, 1.5f, 16f), new Vector3(10.4f, 3f, 0.3f), _galleryWall, true);
            Block(gallery, "PartitionCap", P(0f, 3.02f, 16f), new Vector3(10.5f, 0.05f, 0.34f), _darkWood, false);
            foreach (float z in new[] { 12.1f, 19.9f })
            {
                Block(gallery, "Bench_" + z, P(0f, 0.44f, z), new Vector3(3f, 0.08f, 0.6f), _darkWood, false);
                foreach (float x in new[] { -1.3f, 1.3f })
                    Block(gallery, "BenchLeg_" + z + "_" + x, P(x, 0.2f, z), new Vector3(0.08f, 0.4f, 0.5f), _metal, false);
                Block(gallery, "BenchCollider_" + z, P(0f, 0.25f, z), new Vector3(3f, 0.5f, 0.6f), _metal, true).GetComponent<Renderer>().enabled = false;
            }
            foreach (var (x, z) in new[] { (-11.2f, 8.9f), (11.2f, 8.9f), (-11.2f, 23.2f), (11.2f, 23.2f) })
                Furn(gallery, "pottedPlant", $"Plant_{x}_{z}", P(x, 0f, z), 0f, new Vector3(0.7f, 1.6f, 0.7f), true);

            var segments = new (Vector3 a, Vector3 b, Vector3 n)[]
            {
                (P(-11.2f, 0f, North - 0.13f), P(11.2f, 0f, North - 0.13f), Vector3.back),          // north wall
                (P(-HalfX + 0.13f, 0f, 9.8f), P(-HalfX + 0.13f, 0f, 22.4f), Vector3.right),          // west wall
                (P(HalfX - 0.13f, 0f, 9.8f), P(HalfX - 0.13f, 0f, 22.4f), Vector3.left),             // east wall
                (P(-5f, 0f, 16.15f), P(5f, 0f, 16.15f), Vector3.forward),                           // partition, north face
                (P(-5f, 0f, 15.85f), P(5f, 0f, 15.85f), Vector3.back),                              // partition, south face
                (P(-11f, 0f, Divider + 0.13f), P(-2.3f, 0f, Divider + 0.13f), Vector3.forward),      // divider, west of the arch
                (P(2.3f, 0f, Divider + 0.13f), P(11f, 0f, Divider + 0.13f), Vector3.forward),        // divider, east of the arch
            };
            var arts = MusicCatalog.Artworks;
            int next = 0;
            foreach (var (a, b, n) in segments)
            {
                float length = Vector3.Distance(a, b);
                var dir = (b - a) / length;
                var here = new List<(Artwork art, float w, float h)>();
                float used = 0f;
                while (next < arts.Length)
                {
                    var (w, h) = Size(arts[next]);
                    float need = used + (here.Count > 0 ? Gap : 0f) + w;
                    if (need > length - 0.6f) break;
                    here.Add((arts[next], w, h));
                    used = need;
                    next++;
                }
                float cursor = (length - used) / 2f;
                foreach (var (art, w, h) in here)
                {
                    Hang(gallery, art, a + dir * (cursor + w / 2f), n, w, h);
                    cursor += w + Gap;
                }
            }
            if (next < arts.Length) throw new InvalidOperationException($"The gallery walls hold {next} of {arts.Length} paintings: add wall space.");
        }

        private static (float w, float h) Size(Artwork art)
        {
            var tex = art.Picture;
            float aspect = tex != null ? tex.width / (float)tex.height : 0.8f;
            float h = PaintingHeight, w = h * aspect;
            if (w > PaintingMaxWidth) { w = PaintingMaxWidth; h = w / aspect; }
            return (w, h);
        }

        private static void Hang(Transform gallery, Artwork art, Vector3 wallPoint, Vector3 normal, float w, float h)
        {
            float yaw = Mathf.Atan2(-normal.x, -normal.z) * Mathf.Rad2Deg; // faces the viewer standing in front
            var group = Group(gallery, "Painting_" + art.id);
            Vector3 at(float y, float outward) => new Vector3(wallPoint.x, y, wallPoint.z) + normal * outward;
            Block(group, "Frame", at(PaintingY, 0.03f), new Vector3(w + 0.14f, h + 0.14f, 0.05f), _gold, false, yaw);
            Picture(group, "Canvas", at(PaintingY, 0.06f), w, h, ArtMaterial(art), yaw);
            Block(group, "PictureLight", at(PaintingY + h / 2f + 0.16f, 0.12f), new Vector3(Mathf.Min(0.6f, w * 0.6f), 0.035f, 0.08f), _metal, false, yaw);
            Block(group, "Plaque", at(PaintingY - h / 2f - 0.28f, 0.015f), new Vector3(0.8f, 0.24f, 0.012f), _plaque, false, yaw);
            string title = art.title.Length > 42 ? art.title.Substring(0, 40).TrimEnd(' ', ',', '(') + "…" : art.title;
            string artist = art.artist.Length > 34 ? art.artist.Substring(0, 32) + "…" : art.artist;
            Text(group, "PlaqueText", $"<b>{title}</b>\n<size=80%>{artist}{(string.IsNullOrEmpty(art.date) ? "" : " · " + art.date)}</size>",
                at(PaintingY - h / 2f - 0.28f, 0.025f), yaw, 0.042f, new Color(0.15f, 0.12f, 0.1f), 0.76f);
            var trigger = Trigger(group, "Look", at(1f, 0.8f), new Vector3(Mathf.Max(w, 1f), 2f, 1.3f));
            trigger.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            trigger.AddComponent<ArtworkSpot>().Configure(art.id);
        }

        /// <summary>A painting's face: a quad (a cube's faces flip and turn the picture), facing the viewer at yaw.</summary>
        private static GameObject Picture(Transform parent, string name, Vector3 position, float w, float h, Material material, float yaw)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = new Vector3(w, h, 1f);
            go.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static Material ArtMaterial(Artwork art)
        {
            var mat = Lit(MatDir + "/Art", "Art_" + art.id, Color.white, 0.12f);
            mat.SetTexture("_BaseMap", art.Picture);
            mat.SetTexture("_MainTex", art.Picture);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ─────────── Pieces ───────────

        private static MusicSpot Spot(Transform parent, string name, Vector3 position, Vector3 size, MusicSpotKind kind)
        {
            var spot = Trigger(parent, name, position, size).AddComponent<MusicSpot>();
            spot.Configure(kind);
            return spot;
        }

        private static void Person(Transform parent, string name, Vector3 position, float yaw, string nameplate, string role)
        {
            var person = JobSiteBuilder.Person(parent, name, position, yaw);
            person.SetActive(true);
            person.transform.position = position;
            var plate = Text(person.transform, "Nameplate", $"{nameplate}\n<size=70%>{role}</size>", new Vector3(0f, 2.15f, 0f), 0f, 0.11f, new Color(0.95f, 0.95f, 0.9f), 2f);
            plate.gameObject.AddComponent<BillboardUI>().lockRotationXAndZ = true;
        }

        private static void Spawn(Transform parent, string id, Vector3 position, float yaw)
        {
            var spawn = new GameObject("Spawn_" + id);
            spawn.transform.SetParent(parent);
            spawn.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            spawn.AddComponent<SceneSpawnPoint>().Configure(id);
        }

        private static GameObject Furn(Transform parent, string model, string name, Vector3 position, float yaw, Vector3 targetSize, bool collider)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + model + ".fbx");
            if (source == null) throw new InvalidOperationException("Missing furniture model " + model);
            var holder = new GameObject(name).transform;
            holder.SetParent(parent, false);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.transform.SetParent(holder, false);
            foreach (var c in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            Bounds b = BoundsOf(instance);
            float scale = Mathf.Min(targetSize.x / Mathf.Max(0.01f, b.size.x), targetSize.y / Mathf.Max(0.01f, b.size.y), targetSize.z / Mathf.Max(0.01f, b.size.z));
            instance.transform.localScale *= scale;
            b = BoundsOf(instance);
            instance.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);
            holder.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            if (collider)
            {
                Bounds world = BoundsOf(holder.gameObject);
                var box = holder.gameObject.AddComponent<BoxCollider>();
                box.center = holder.InverseTransformPoint(world.center);
                Vector3 size = holder.InverseTransformVector(world.size);
                box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            }
            return holder.gameObject;
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
            foreach (var stale in new[] { destinations.Find("Entrance_MusicShop"), portals.Find("MusicShopPortal"), portals.Find("Spawn_" + WorldLocationCatalog.CityMusicShopReturn) })
                if (stale != null) Object.DestroyImmediate(stale.gameObject);

            Bounds b = BoundsOf(building.gameObject);
            float frontZ = b.max.z; // the south row faces north, toward Sakura-dōri
            var group = Group(destinations, "Entrance_MusicShop");
            group.SetPositionAndRotation(new Vector3(building.position.x, 0f, frontZ), Quaternion.Euler(0f, 180f, 0f));
            // Local: +X along the facade, −Z toward the street.
            Block(group, "Canopy", new Vector3(0f, 2.9f, -1.0f), new Vector3(5.6f, 0.16f, 2.2f), _sign, true);
            Block(group, "CanopyStripe", new Vector3(0f, 2.8f, -2.08f), new Vector3(5.6f, 0.12f, 0.05f), _gold, false);
            foreach (float x in new[] { -2.6f, 2.6f })
                Block(group, "Post_" + (x < 0 ? "L" : "R"), new Vector3(x, 1.4f, -1.95f), new Vector3(0.14f, 2.8f, 0.14f), _darkWood, true);
            Block(group, "SignBoard", new Vector3(0f, 3.6f, -2.1f), new Vector3(5f, 0.9f, 0.14f), _sign, true);
            Block(group, "SignStripe", new Vector3(0f, 3.17f, -2.18f), new Vector3(5f, 0.08f, 0.02f), _gold, false);
            Text(group, "Sign_Note", "♪", new Vector3(-1.95f, 3.62f, -2.2f), 0f, 0.6f, _gold.color, 1f);
            Text(group, "Sign_JA", "ひびき楽器店", new Vector3(0.35f, 3.72f, -2.2f), 0f, 0.38f, Color.white, 4f);
            Text(group, "Sign_Sub", "がっき・がくふ・ギャラリー", new Vector3(0.35f, 3.38f, -2.2f), 0f, 0.13f, new Color(1f, 0.9f, 0.7f), 4f);
            Block(group, "Door", new Vector3(0f, 1.15f, -0.05f), new Vector3(2.4f, 2.3f, 0.06f), _glass, false);
            Block(group, "DoorFrame", new Vector3(0f, 2.35f, -0.08f), new Vector3(2.6f, 0.1f, 0.08f), _gold, false);
            // Shop windows either side: a guitar and a framed painting on show.
            foreach (float x in new[] { -2.0f, 2.0f })
                Block(group, "ShopWindow_" + (x < 0 ? "L" : "R"), new Vector3(x, 1.3f, -0.06f), new Vector3(1.3f, 1.6f, 0.04f), _glass, false);
            Cylinder(group, "WindowGuitar", new Vector3(-2.0f, 1.0f, 0.15f), new Vector3(0.4f, 0.035f, 0.4f), _sunburst, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Block(group, "WindowGuitarNeck", new Vector3(-2.0f, 1.55f, 0.15f), new Vector3(0.055f, 0.7f, 0.03f), _darkWood, false);
            var poster = MusicCatalog.ArtworkById("commons-Tsunami_by_hokusai_19th_century_jpg") ?? MusicCatalog.Artworks.FirstOrDefault();
            if (poster != null)
            {
                Block(group, "WindowFrame", new Vector3(2.0f, 1.35f, 0.16f), new Vector3(1.0f, 0.74f, 0.04f), _gold, false);
                Picture(group, "WindowPainting", new Vector3(2.0f, 1.35f, 0.13f), 0.9f, 0.62f, ArtMaterial(poster), 0f);
            }
            Text(group, "Door_Hours", "10:00〜21:00 · ギャラリー あり", new Vector3(0f, 1.9f, -0.1f), 0f, 0.09f, Color.white, 2f);
            PointLight(group, "CanopyLight", new Vector3(0f, 2.6f, -1.2f), 5f, 0.7f, new Color(1f, 0.92f, 0.8f));

            var portalObject = Trigger(portals, "MusicShopPortal", Vector3.zero, new Vector3(2.4f, 2.2f, 1.2f));
            portalObject.transform.SetPositionAndRotation(new Vector3(group.position.x, 1.1f, frontZ + 0.6f), Quaternion.Euler(0f, 180f, 0f));
            var portal = portalObject.AddComponent<ScenePortal>();
            portal.Configure(WorldLocationCatalog.MusicShopScene, WorldLocationCatalog.MusicShopEntrance, "ひびき楽器店 / Tiệm nhạc & tranh Hibiki", false);
            var so = new SerializedObject(portal);
            so.FindProperty("promptJa").stringValue = "みせに はいる";
            so.FindProperty("promptEn").stringValue = "Vào tiệm nhạc";
            so.ApplyModifiedPropertiesWithoutUndo();
            var spawn = new GameObject("Spawn_" + WorldLocationCatalog.CityMusicShopReturn);
            spawn.transform.SetParent(portals);
            spawn.transform.SetPositionAndRotation(new Vector3(group.position.x, 0.08f, frontZ + 2.4f), Quaternion.identity);
            spawn.AddComponent<SceneSpawnPoint>().Configure(WorldLocationCatalog.CityMusicShopReturn);

            foreach (var renderer in group.GetComponentsInChildren<Renderer>(true)) renderer.shadowCastingMode = ShadowCastingMode.Off;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + CityPath);
        }
    }
}
#endif
