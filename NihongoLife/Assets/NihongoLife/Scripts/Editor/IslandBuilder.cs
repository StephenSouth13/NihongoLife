#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Island;
using NihongoLife.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using static NihongoLife.EditorTools.ZoneBuildKit;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Midori Island (approved scene 60_MidoriIsland), composed from the assets in "みどり島 – Midori Island (Đảo Xanh)"
    /// (Quaternius Farm Buildings + Ultimate Crops, Kenney Survival Kit) plus the project's Quaternius animated animals
    /// and train pack. One island, laid out as a walk: station (south) → plaza → farm (west) / animal pen (east) /
    /// Midori store (beside the station) → lookout (north). Gameplay components: FarmPlot ×6, IslandAnimal ×5,
    /// IslandShopCounter, IslandWordSpot signs, IslandTicketKiosk + IslandTrainDoor (return trip), IslandRuntime.
    /// Crop stage prefabs and animal controllers are generated under Generated/Island.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.IslandBuilder.Build
    /// </summary>
    public static class IslandBuilder
    {
        public const string ScenePath = "Assets/NihongoLife/Scenes/60_MidoriIsland.unity";
        private const string Pack = "Assets/みどり島 – Midori Island (Đảo Xanh)/";
        private const string Farm = Pack + "Farm Building/FBX/";
        private const string Crops = Pack + "Ultimate Crops Pack/FBX/";
        private const string Survival = Pack + "Survival Kit-zip/";
        private const string Animals = "Assets/ThirdParty/Ultimate Animated Animals - July 2021-20260920T035520Z-1-001/Ultimate Animated Animals - July 2021/FBX/";
        private const string Trains = "Assets/ThirdParty/Train Pack - April 2019-20260920T035456Z-1-001";
        private const string GenDir = "Assets/NihongoLife/Generated/Island";
        private const string MatDir = "Assets/NihongoLife/Materials/Island";
        private static readonly Vector3 O = new Vector3(-1200f, 0f, 0f);
        private const float IslandRX = 50f, IslandRZ = 42f;

        private static Material _grass, _sand, _sea, _path, _plaza, _soil, _platform, _stationRoof, _stationPost, _kioskBody, _kioskScreen, _signWood, _lineYellow, _embankment, _sky;
        private static readonly Dictionary<Material, Material> Converted = new();

        public static void Build()
        {
            try
            {
                EnsureFolder(GenDir); EnsureFolder(GenDir + "/Crops"); EnsureFolder(GenDir + "/Animals"); EnsureFolder(GenDir + "/Materials"); EnsureFolder(MatDir);
                CreateMaterials();
                var library = BuildCropPrefabs();
                BuildScene(library);
                UpdateBuildSettings();
                AssetDatabase.SaveAssets();
                Debug.Log("[IslandBuilder] Midori Island built.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static void CreateMaterials()
        {
            _grass = Lit(MatDir, "MI_Grass", new Color(0.42f, 0.62f, 0.3f), 0.1f);
            _sand = Lit(MatDir, "MI_Sand", new Color(0.86f, 0.79f, 0.6f), 0.15f);
            _sea = Lit(MatDir, "MI_Sea", new Color(0.18f, 0.48f, 0.62f), 0.88f);
            _path = Lit(MatDir, "MI_Path", new Color(0.78f, 0.68f, 0.5f), 0.12f);
            _plaza = Lit(MatDir, "MI_Plaza", new Color(0.72f, 0.7f, 0.64f), 0.2f);
            _soil = Lit(MatDir, "MI_Soil", new Color(0.55f, 0.38f, 0.24f), 0.05f);
            _platform = Lit(MatDir, "MI_Platform", new Color(0.66f, 0.64f, 0.6f), 0.25f);
            _stationRoof = Lit(MatDir, "MI_StationRoof", new Color(0.18f, 0.38f, 0.27f), 0.35f);
            _stationPost = Lit(MatDir, "MI_StationPost", new Color(0.93f, 0.9f, 0.82f), 0.3f);
            _kioskBody = Lit(MatDir, "MI_KioskBody", new Color(0.2f, 0.42f, 0.3f), 0.4f);
            _kioskScreen = Lit(MatDir, "MI_KioskScreen", new Color(0.95f, 0.92f, 0.8f), 0.6f, new Color(0.45f, 0.42f, 0.3f));
            _signWood = Lit(MatDir, "MI_SignWood", new Color(0.5f, 0.36f, 0.22f), 0.2f);
            _lineYellow = Lit(MatDir, "MI_PlatformLine", new Color(0.95f, 0.8f, 0.2f), 0.3f);
            _embankment = Lit(MatDir, "MI_Embankment", new Color(0.52f, 0.5f, 0.46f), 0.15f);

            string skyPath = MatDir + "/MI_Sky.mat";
            _sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (_sky == null) { _sky = new Material(Shader.Find("Skybox/Procedural")); AssetDatabase.CreateAsset(_sky, skyPath); }
            _sky.SetFloat("_SunSize", 0.035f);
            _sky.SetFloat("_AtmosphereThickness", 0.85f);
            _sky.SetColor("_SkyTint", new Color(0.55f, 0.72f, 0.9f));
            _sky.SetColor("_GroundColor", new Color(0.36f, 0.48f, 0.52f));
            _sky.SetFloat("_Exposure", 1.15f);
            EditorUtility.SetDirty(_sky);
        }

        // ─────────── Crop stage prefabs ───────────

        /// <summary>Each stage prefab is a 2×2 bed of the Quaternius crop model, scaled by one factor per crop (taken from
        /// stage 4) so the stages visibly grow, with URP materials and no colliders.</summary>
        private static List<IslandModelLibrary.Entry> BuildCropPrefabs()
        {
            var entries = new List<IslandModelLibrary.Entry>();
            foreach (var crop in IslandCatalog.Load().crops)
            {
                var ripe = AssetDatabase.LoadAssetAtPath<GameObject>(Crops + crop.model + "_4.fbx");
                if (ripe == null) throw new InvalidOperationException("Missing crop model " + crop.model + "_4");
                var probe = (GameObject)PrefabUtility.InstantiatePrefab(ripe);
                float h = Mathf.Max(0.01f, BoundsOf(probe).size.y);
                float w = Mathf.Max(0.01f, Mathf.Max(BoundsOf(probe).size.x, BoundsOf(probe).size.z));
                Object.DestroyImmediate(probe);
                float targetHeight = crop.id == "corn" ? 1.5f : crop.id == "pumpkin" ? 0.7f : 0.75f;
                float scale = Mathf.Min(targetHeight / h, 1.05f / w);
                for (int stage = 1; stage <= 4; stage++)
                {
                    string name = $"{crop.model}_{stage}";
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(Crops + name + ".fbx");
                    if (model == null) throw new InvalidOperationException("Missing crop model " + name);
                    var root = new GameObject(name);
                    for (int i = 0; i < 4; i++)
                    {
                        var plant = (GameObject)PrefabUtility.InstantiatePrefab(model);
                        PrefabUtility.UnpackPrefabInstance(plant, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                        plant.transform.SetParent(root.transform, false);
                        plant.transform.localScale *= scale;
                        plant.transform.localPosition = new Vector3(i % 2 == 0 ? -0.6f : 0.6f, 0f, i < 2 ? -0.6f : 0.6f);
                        plant.transform.localRotation = Quaternion.Euler(0f, i * 83f, 0f);
                        foreach (var c in plant.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                    }
                    // Ground the bed at y = 0.
                    Bounds b = BoundsOf(root);
                    foreach (Transform child in root.transform) child.localPosition += Vector3.up * -b.min.y;
                    ConvertMaterials(root);
                    foreach (var r in root.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true; }
                    var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{GenDir}/Crops/{name}.prefab");
                    Object.DestroyImmediate(root);
                    entries.Add(new IslandModelLibrary.Entry { name = name, prefab = prefab });
                }
            }
            return entries;
        }

        // ─────────── Scene ───────────

        private static void BuildScene(List<IslandModelLibrary.Entry> library)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("MidoriIsland_Zone");
            root.transform.position = O;
            root.AddComponent<SceneZoneVisibility>();
            root.AddComponent<IslandModelLibrary>().Set(library);
            var ground = Group(root.transform, "Ground");
            var station = Group(root.transform, "Station");
            var farm = Group(root.transform, "Farm");
            var pen = Group(root.transform, "AnimalPen");
            var shop = Group(root.transform, "MidoriStore");
            var lookout = Group(root.transform, "Lookout");
            var nature = Group(root.transform, "Nature");
            var signs = Group(root.transform, "Signs");
            var bounds = Group(root.transform, "Bounds");

            BuildGround(ground, bounds);
            var spawn = BuildStation(station);
            BuildPaths(ground);
            BuildFarm(farm);
            BuildPen(pen);
            BuildShop(shop);
            BuildLookout(lookout);
            BuildNature(nature);
            BuildSigns(signs);
            root.AddComponent<IslandRuntime>().Configure(spawn);

            // Sun, sky, fog: a bright late-morning island. Only this sun lights the island (IslandRuntime switches the
            // host city's sun off while the island is loaded).
            var sunGo = new GameObject("IslandSun");
            sunGo.transform.SetParent(root.transform, false);
            sunGo.transform.rotation = Quaternion.Euler(48f, -38f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.35f; sun.color = new Color(1f, 0.95f, 0.86f);
            sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.7f;
            RenderSettings.sun = sun;
            RenderSettings.skybox = _sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.56f, 0.6f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.32f, 0.26f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0065f;
            RenderSettings.fogColor = new Color(0.72f, 0.82f, 0.88f);

            var preview = new GameObject("PreviewCamera") { tag = "MainCamera" };
            preview.transform.SetParent(root.transform, false);
            preview.transform.position = O + new Vector3(0f, 14f, -46f);
            preview.transform.rotation = Quaternion.LookRotation(O + new Vector3(0f, 0f, 0f) - preview.transform.position);
            preview.AddComponent<Camera>().fieldOfView = 55f;
            preview.AddComponent<AudioListener>();
            preview.AddComponent<NihongoLife.Cameras.ZonePreviewCamera>();

            // Static batching for everything that never moves (not animals, crops, text or interaction triggers).
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponentInParent<IslandAnimal>() != null || t.GetComponentInParent<FarmPlot>() != null) continue;
                if (t.GetComponent<TextMeshPro>() != null || t.GetComponent<Light>() != null || t.GetComponent<Camera>() != null) continue;
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            }

            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save " + ScenePath);
        }

        private static void BuildGround(Transform ground, Transform boundsRoot)
        {
            Disc(ground, "Sea", new Vector3(0f, -0.55f, 0f), 420f, 420f, 0.1f, _sea, false);
            Disc(ground, "Beach", new Vector3(0f, -0.3f, 0f), IslandRX + 4f, IslandRZ + 4f, 0.5f, _sand, true);
            Disc(ground, "Island", new Vector3(0f, -0.25f, 0f), IslandRX, IslandRZ, 0.5f, _grass, true);
            // Invisible shore: the player stays on the grass (the fall guard in IslandRuntime is a second safety net).
            const int segments = 44;
            float rx = IslandRX - 3.5f, rz = IslandRZ - 3.5f;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * rx, 0f, Mathf.Sin(a0) * rz), p1 = new Vector3(Mathf.Cos(a1) * rx, 0f, Mathf.Sin(a1) * rz);
                var wall = new GameObject("Shore_" + i);
                wall.transform.SetParent(boundsRoot, false);
                wall.transform.localPosition = (p0 + p1) * 0.5f + Vector3.up * 1.5f;
                wall.transform.localRotation = Quaternion.LookRotation(p1 - p0);
                var box = wall.AddComponent<BoxCollider>();
                box.size = new Vector3(0.6f, 3f, (p1 - p0).magnitude + 0.6f);
            }
        }

        private static GameObject Disc(Transform parent, string name, Vector3 position, float rx, float rz, float height, Material material, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(rx * 2f, height * 0.5f, rz * 2f);
            go.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        // ─────────── Station (south) ───────────

        private static Transform BuildStation(Transform station)
        {
            // Platform along x at z = -30; the line arrives over the sea from the west on an embankment.
            Block(station, "Platform", new Vector3(0f, 0.1f, -30f), new Vector3(26f, 0.2f, 5f), _platform, true);
            Block(station, "PlatformEdgeLine", new Vector3(0f, 0.205f, -32.1f), new Vector3(26f, 0.01f, 0.25f), _lineYellow, false);
            Block(station, "Embankment", new Vector3(-40f, -0.2f, -34.6f), new Vector3(170f, 0.6f, 4.2f), _embankment, false);
            var edge = new GameObject("PlatformEdgeBarrier");
            edge.transform.SetParent(station, false);
            edge.transform.localPosition = new Vector3(0f, 1.2f, -32.75f);
            edge.AddComponent<BoxCollider>().size = new Vector3(64f, 2.4f, 0.3f);
            for (int i = 0; i < 13; i++)
                Model(station, Trains, "RailwayTrack_Straight", "Track_" + i, new Vector3(-95f + i * 10f, 0.1f, -34.6f), new Vector3(10f, 0.25f, 2.4f), 0f, false, false);
            // Waiting train back to Hibari (two cars).
            Model(station, Trains, "HighSpeed_Front", "Train_Front", new Vector3(8.5f, 0.2f, -34.6f), new Vector3(13f, 3.3f, 3.1f), 0f, true, true);
            Model(station, Trains, "HighSpeed_Wagon", "Train_Wagon", new Vector3(-4.8f, 0.2f, -34.6f), new Vector3(13f, 3.3f, 3.1f), 0f, true, true);

            // Canopy.
            foreach (float x in new[] { -10f, -3.5f, 3.5f, 10f })
                Block(station, "CanopyPost_" + x, new Vector3(x, 2.4f, -28.0f), new Vector3(0.25f, 4.4f, 0.25f), _stationPost, true);
            Block(station, "CanopyRoof", new Vector3(0f, 4.7f, -29.8f), new Vector3(24f, 0.22f, 4.6f), _stationRoof, false);
            Block(station, "CanopyFascia", new Vector3(0f, 4.4f, -27.5f), new Vector3(24f, 0.6f, 0.12f), _stationPost, false);
            Text(station, "StationName_JA", "みどりじま", new Vector3(0f, 4.47f, -27.42f), 180f, 0.34f, new Color(0.13f, 0.32f, 0.22f), 8f);
            Text(station, "StationName_EN", "MIDORI ISLAND · Đảo Xanh", new Vector3(-5.6f, 4.43f, -27.42f), 180f, 0.15f, new Color(0.3f, 0.4f, 0.32f), 6f);
            // Station name boards hang between the north posts, facing passengers who step off the train.
            foreach (float x in new[] { -6.75f, 6.75f })
            {
                Block(station, "NameBoard_" + x, new Vector3(x, 3.1f, -27.9f), new Vector3(3.2f, 0.8f, 0.06f), _stationPost, false);
                Text(station, "NameBoard_Text_" + x, "みどりじま\n<size=50%>Midori Island · ひばり ←</size>", new Vector3(x, 3.12f, -27.95f), 0f, 0.24f, new Color(0.13f, 0.32f, 0.22f), 3.1f);
            }
            foreach (float x in new[] { -6.5f, 6.5f })
            {
                Block(station, "Bench_" + x, new Vector3(x, 0.45f, -28.6f), new Vector3(2.2f, 0.12f, 0.6f), _signWood, true);
                Block(station, "BenchBack_" + x, new Vector3(x, 0.75f, -28.32f), new Vector3(2.2f, 0.5f, 0.08f), _signWood, false);
            }

            // Ticket kiosk.
            var kiosk = Group(station, "TicketKiosk");
            kiosk.localPosition = new Vector3(10.5f, 0.2f, -28.6f);
            kiosk.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Block(kiosk, "Body", new Vector3(0f, 0.9f, 0f), new Vector3(1.1f, 1.8f, 0.7f), _kioskBody, true);
            Block(kiosk, "Screen", new Vector3(0f, 1.25f, 0.36f), new Vector3(0.8f, 0.5f, 0.02f), _kioskScreen, false);
            Text(kiosk, "Label", "きっぷ\n<size=55%>Vé tàu ¥" + IslandCatalog.Load().ticketPrice + "</size>", new Vector3(0f, 1.27f, 0.38f), 180f, 0.14f, new Color(0.13f, 0.32f, 0.22f), 0.8f);
            var kioskTrigger = Trigger(kiosk, "KioskInteraction", new Vector3(0f, 1f, 0.9f), new Vector3(1.6f, 2f, 1.4f));
            kioskTrigger.AddComponent<IslandTicketKiosk>();

            var door = Trigger(station, "TrainDoorInteraction", new Vector3(-4.8f, 1.1f, -31.7f), new Vector3(3f, 2.2f, 1.6f));
            door.AddComponent<IslandTrainDoor>();

            var spawn = new GameObject("Spawn_" + WorldLocationCatalog.MidoriStation);
            spawn.transform.SetParent(station, false);
            spawn.transform.localPosition = new Vector3(-1.5f, 0.3f, -29.6f);
            spawn.transform.localRotation = Quaternion.identity;
            spawn.AddComponent<SceneSpawnPoint>().Configure(WorldLocationCatalog.MidoriStation);
            return spawn.transform;
        }

        // ─────────── Paths ───────────

        private static void BuildPaths(Transform ground)
        {
            Block(ground, "Path_Main", new Vector3(0f, 0.015f, -3f), new Vector3(3f, 0.03f, 46f), _path, false);         // station → lookout
            Block(ground, "Path_Cross", new Vector3(0f, 0.02f, -4f), new Vector3(34f, 0.03f, 3f), _path, false);        // farm ↔ pen
            Block(ground, "Path_Store", new Vector3(4.5f, 0.025f, -18f), new Vector3(6f, 0.03f, 2.4f), _path, false);    // → store
            Block(ground, "Path_PenGate", new Vector3(18f, 0.03f, -2f), new Vector3(2.4f, 0.03f, 3f), _path, false);
            Disc(ground, "Plaza", new Vector3(0f, 0.03f, -4f), 4.2f, 4.2f, 0.04f, _plaza, false);
        }

        // ─────────── Farm (west) ───────────

        private static void BuildFarm(Transform farm)
        {
            int n = 1;
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 3; col++, n++)
                {
                    var plotRoot = Group(farm, "FarmPlot_" + n);
                    plotRoot.localPosition = new Vector3(-23.8f + col * 3.5f, 0f, 1.2f + row * 3.6f);
                    var soil = Block(plotRoot, "Soil", new Vector3(0f, 0f, 0f), new Vector3(2.7f, 0.08f, 2.7f), _soil, false);
                    soil.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    var anchor = Group(plotRoot, "CropAnchor");
                    anchor.localPosition = new Vector3(0f, 0.09f, 0f);
                    var marker = Text(plotRoot, "StatusMarker", "", new Vector3(0f, 2.1f, 0f), 0f, 0.5f, Color.white, 1.4f);
                    marker.fontStyle = FontStyles.Bold;
                    marker.outlineWidth = 0.2f;
                    marker.outlineColor = new Color32(255, 255, 255, 255);
                    marker.gameObject.AddComponent<BillboardUI>().lockRotationXAndZ = true;
                    var number = Text(plotRoot, "Number", n.ToString(), new Vector3(-1.2f, 0.12f, -1.42f), 0f, 0.3f, new Color(0.95f, 0.92f, 0.82f), 0.6f);
                    number.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    plotRoot.gameObject.layer = InteractableLayer;
                    var box = plotRoot.gameObject.AddComponent<BoxCollider>();
                    box.isTrigger = true; box.center = new Vector3(0f, 0.6f, 0f); box.size = new Vector3(2.7f, 1.2f, 2.7f);
                    plotRoot.gameObject.AddComponent<FarmPlot>().Configure("plot_" + n, n, soil.GetComponent<Renderer>(), anchor, marker);
                }
            Model(farm, Farm, "Barn", "Barn", new Vector3(-22f, 0f, 15f), new Vector3(10f, 8f, 9f), 180f, true, true);
            Model(farm, Farm, "Silo", "Silo", new Vector3(-31f, 0f, 12f), new Vector3(4f, 9f, 4f), 0f, true, true);
            Model(farm, Farm, "Windmill", "Windmill", new Vector3(-34f, 0f, 2f), new Vector3(6f, 11f, 6f), 90f, true, true);
            Model(farm, Farm, "Well", "Well", new Vector3(-12.5f, 0f, 8.5f), new Vector3(2f, 2.6f, 2f), 0f, true, true);
            Model(farm, Farm, "WaterTower", "WaterTower", new Vector3(-30.5f, 0f, -8f), new Vector3(4f, 7f, 4f), 0f, true, true);
            // Fence on the west/north sides of the field (open toward the path).
            for (int i = 0; i < 4; i++) Model(farm, Farm, "Fence", "FieldFence_N" + i, new Vector3(-24.6f + i * 3f, 0f, 9.2f), new Vector3(3f, 1.2f, 0.4f), 0f, true, true);
            for (int i = 0; i < 3; i++) Model(farm, Farm, "Fence", "FieldFence_W" + i, new Vector3(-27.4f, 0f, 0.4f + i * 3f), new Vector3(0.4f, 1.2f, 3f), 90f, true, true);
            Obj(farm, "Box", "box", "SeedCrates", new Vector3(-14.2f, 0f, 2.2f), 0.9f, 20f, true);
            Obj(farm, "Barrel", "barrel", "WaterBarrel", new Vector3(-14.4f, 0f, 4.2f), 1.0f, 0f, true);
            Obj(farm, "Tool Hoe", "toolHoe", "HoeProp", new Vector3(-14.6f, 0.45f, 5.6f), 1.1f, 75f, false, tilt: 18f);
        }

        // ─────────── Animal pen (east) ───────────

        private static void BuildPen(Transform pen)
        {
            Vector3 c = new Vector3(18f, 0f, 6f);
            const float radius = 7.5f;
            const int pieces = 16;
            for (int i = 0; i < pieces; i++)
            {
                float a = (i + 0.5f) * Mathf.PI * 2f / pieces;
                Vector3 p = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                if (Mathf.Abs(p.x - c.x) < 1.6f && p.z < c.z) continue; // gate gap facing the path (south)
                float yaw = -(a * Mathf.Rad2Deg + 90f); // local X (the fence run) along the circle tangent
                Model(pen, Farm, "Fence2", "PenFence_" + i, p, new Vector3(3.1f, 1.2f, 3.1f), yaw, true, true, fitLongest: true);
            }
            Model(pen, Farm, "OpenBarn", "Stable", new Vector3(28.5f, 0f, 10f), new Vector3(7f, 5f, 6f), -90f, true, true);
            Model(pen, Farm, "ChickenCoop", "Coop", new Vector3(27.5f, 0f, -6.5f), new Vector3(3.5f, 3f, 3.5f), -90f, true, true);
            Obj(pen, "Barrel Open", "barrelOpen", "Trough", new Vector3(c.x - 3.2f, 0f, c.z + 3f), 0.9f, 0f, true);

            var catalog = IslandCatalog.Load();
            var spots = new Dictionary<string, (Vector3 pos, float height, Vector3 center, float wander)>
            {
                ["cow"] = (c + new Vector3(-2.5f, 0f, 2f), 1.55f, c, 4.6f),
                ["alpaca"] = (c + new Vector3(2.6f, 0f, 2.4f), 1.7f, c, 4.6f),
                ["donkey"] = (c + new Vector3(-2.2f, 0f, -2.2f), 1.45f, c, 4.6f),
                ["horse"] = (c + new Vector3(2.4f, 0f, -1.6f), 1.85f, c, 4.6f),
                ["dog"] = (new Vector3(2.4f, 0f, -6.8f), 0.62f, new Vector3(0f, 0f, -5f), 4.2f),
            };
            foreach (var def in catalog.animals)
            {
                if (!spots.TryGetValue(def.id, out var spot)) continue;
                var holder = Group(pen, "Animal_" + def.id);
                holder.localPosition = spot.pos;
                holder.localRotation = Quaternion.Euler(0f, def.id.Length * 67f, 0f);
                string path = Animals + def.model + ".fbx";
                EnsureAnimalClipsLoop(path);
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (source == null) throw new InvalidOperationException("Missing animal model " + path);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
                model.transform.SetParent(holder, false);
                Bounds b = BoundsOf(model);
                model.transform.localScale *= spot.height / Mathf.Max(0.01f, b.size.y);
                b = BoundsOf(model);
                model.transform.position += Vector3.up * (holder.position.y - b.min.y);
                ConvertMaterials(model);
                foreach (var r in model.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
                ConfigureAnimalAnimator(model, path, "Island_" + def.model);
                b = BoundsOf(model);
                holder.gameObject.layer = InteractableLayer;
                var trigger = holder.gameObject.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.center = holder.InverseTransformPoint(b.center);
                float size = Mathf.Max(b.size.x, b.size.z) + 0.8f;
                trigger.size = new Vector3(size, b.size.y + 0.4f, size);
                var body = new GameObject("Body");
                body.transform.SetParent(holder, false);
                var capsule = body.AddComponent<CapsuleCollider>();
                capsule.direction = 2; capsule.center = holder.InverseTransformPoint(b.center);
                capsule.radius = Mathf.Min(b.size.x, b.size.z) * 0.35f; capsule.height = Mathf.Max(b.size.x, b.size.z) * 0.9f;
                holder.gameObject.AddComponent<IslandAnimal>().Configure(def.id, O + spot.center, spot.wander);
            }
        }

        /// <summary>Marks idle/walk/eat clips as looping on the Quaternius animal FBX (only reimports when something changes).
        /// Shared asset: the city's Shiba Inu uses the same file and simply gains looping idle/walk.</summary>
        private static void EnsureAnimalClipsLoop(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer) return;
            var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            bool dirty = importer.clipAnimations.Length == 0;
            foreach (var clip in clips)
            {
                bool loop = clip.name.Contains("Idle") || clip.name.Contains("Walk") || clip.name.Contains("Gallop") || clip.name.Contains("Eating");
                if (clip.loopTime != loop) { clip.loopTime = loop; dirty = true; }
            }
            if (!dirty) return;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static void ConfigureAnimalAnimator(GameObject instance, string modelPath, string controllerName)
        {
            var clips = new List<AnimationClip>();
            Avatar avatar = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")) clips.Add(clip);
                else if (asset is Avatar a) avatar = a;
            }
            if (clips.Count == 0) return;
            string controllerPath = $"{GenDir}/Animals/{controllerName}.controller";
            AssetDatabase.DeleteAsset(controllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Eat", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            AnimationClip idle = FindClip(clips, "|Idle") ?? FindClip(clips, "Idle") ?? clips[0];
            AnimationClip walk = FindClip(clips, "|Walk") ?? FindClip(clips, "Walk");
            var idleState = machine.AddState("Idle"); idleState.motion = idle; machine.defaultState = idleState;
            if (walk != null)
            {
                var walkState = machine.AddState("Walk"); walkState.motion = walk;
                var toWalk = idleState.AddTransition(walkState); toWalk.hasExitTime = false; toWalk.duration = 0.18f; toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
                var toIdle = walkState.AddTransition(idleState); toIdle.hasExitTime = false; toIdle.duration = 0.18f; toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            }
            AddTrigger(machine, idleState, FindClip(clips, "Eating"), "Eat");
            AddTrigger(machine, idleState, FindClip(clips, "Jump") ?? FindClip(clips, "Idle_2") ?? FindClip(clips, "HitReact"), "Jump");
            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        private static void AddTrigger(AnimatorStateMachine machine, AnimatorState idle, AnimationClip clip, string trigger)
        {
            if (clip == null) return;
            var state = machine.AddState(trigger); state.motion = clip;
            var enter = machine.AddAnyStateTransition(state); enter.hasExitTime = false; enter.duration = 0.1f; enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            var exit = state.AddTransition(idle); exit.hasExitTime = true; exit.exitTime = 0.92f; exit.duration = 0.15f;
        }

        private static AnimationClip FindClip(List<AnimationClip> clips, string token) =>
            clips.Find(clip => clip.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);

        // ─────────── Midori store (beside the station) ───────────

        private static void BuildShop(Transform shop)
        {
            Model(shop, Farm, "SmallBarn", "StoreBuilding", new Vector3(12.5f, 0f, -18f), new Vector3(6f, 5f, 6f), -90f, true, true);
            Block(shop, "Counter", new Vector3(8.6f, 0.5f, -18f), new Vector3(1f, 1f, 3.2f), _signWood, true);
            Block(shop, "CounterTop", new Vector3(8.6f, 1.03f, -18f), new Vector3(1.2f, 0.06f, 3.4f), _stationPost, false);
            Block(shop, "Awning", new Vector3(8.9f, 2.9f, -18f), new Vector3(2.4f, 0.12f, 4.2f), _stationRoof, false);
            foreach (float z in new[] { -19.9f, -16.1f }) Block(shop, "AwningPost_" + z, new Vector3(7.8f, 1.45f, z), new Vector3(0.14f, 2.9f, 0.14f), _stationPost, true);
            Block(shop, "SignBoard", new Vector3(7.75f, 3.35f, -18f), new Vector3(0.08f, 0.8f, 3.8f), _stationPost, false);
            Text(shop, "Sign_JA", "みどりしょうてん", new Vector3(7.68f, 3.45f, -18f), -90f, 0.3f, new Color(0.13f, 0.32f, 0.22f), 3.7f);
            Text(shop, "Sign_EN", "MIDORI STORE · Cửa hàng  [P]", new Vector3(7.68f, 3.12f, -18f), -90f, 0.13f, new Color(0.62f, 0.45f, 0.1f), 3.7f);
            foreach (var crop in IslandCatalog.Load().crops.Take(3).Select((c, i) => (c, i)))
            {
                var display = AssetDatabase.LoadAssetAtPath<GameObject>($"{GenDir}/Crops/{crop.c.model}_4.prefab");
                if (display == null) continue;
                var d = (GameObject)PrefabUtility.InstantiatePrefab(display);
                d.transform.SetParent(shop, false);
                d.transform.localPosition = new Vector3(8.6f, 1.06f, -19.1f + crop.i * 1.1f);
                d.transform.localScale = Vector3.one * 0.32f;
            }
            Obj(shop, "Box Open", "boxOpen", "Crate_1", new Vector3(7.4f, 0f, -21.2f), 0.8f, 10f, true);
            Obj(shop, "Barrel", "barrel", "Barrel_1", new Vector3(7.5f, 0f, -14.8f), 1.0f, 0f, true);
            var counter = Trigger(shop, "StoreInteraction", new Vector3(7.4f, 1f, -18f), new Vector3(1.8f, 2f, 3.4f));
            counter.AddComponent<IslandShopCounter>();
        }

        // ─────────── Lookout (north) ───────────

        private static void BuildLookout(Transform lookout)
        {
            Vector3 c = new Vector3(0f, 0f, 30f);
            Disc(lookout, "LookoutDeck", c + new Vector3(0f, 0.02f, 0f), 5f, 4f, 0.06f, _plaza, false);
            Obj(lookout, "Campfire", "campfire", "Campfire", c + new Vector3(0f, 0f, 0.5f), 0.9f, 0f, true);
            Obj(lookout, "Tree Fall", "treeFall", "LogBench_L", c + new Vector3(-2.4f, 0f, 0.3f), 0.55f, 90f, true);
            Obj(lookout, "Tree Fall", "treeFall", "LogBench_R", c + new Vector3(2.4f, 0f, 0.3f), 0.55f, -90f, true);
            Obj(lookout, "Tent", "tentClosed", "Tent", c + new Vector3(-6.5f, 0f, 2.5f), 2.2f, 30f, true);
            foreach (var (name, pos, size) in new[] { ("rockA", new Vector3(5.5f, 0f, 3.5f), 1.4f), ("rockB", new Vector3(6.8f, 0f, 1.2f), 1.0f), ("rockC", new Vector3(-4.8f, 0f, 5.2f), 1.2f), ("rockFlatGrass", new Vector3(3.6f, 0f, 5.6f), 0.6f) })
                Obj(lookout, name.StartsWith("rockFlat") ? "Rock Flat Grass" : "Rock", name, "Rock_" + name, c + pos, size, pos.x * 40f, true);
            Obj(lookout, "Fishing Stand", "fishingStand", "FishingStand", new Vector3(-14f, 0f, 33.5f), 1.6f, 160f, true);
        }

        // ─────────── Nature ───────────

        private static void BuildNature(Transform nature)
        {
            var random = new System.Random(4207);
            var keepOut = new List<(Vector2 c, float r)>
            {
                (new Vector2(0f, -30f), 16f), (new Vector2(-20f, 4f), 13f), (new Vector2(18f, 6f), 11f), (new Vector2(10f, -18f), 6f),
                (new Vector2(0f, 30f), 8f), (new Vector2(-22f, 15f), 8f), (new Vector2(-33f, 2f), 5f), (new Vector2(-31f, 12f), 4f), (new Vector2(28f, 10f), 6f), (new Vector2(27f, -6f), 4f),
            };
            int trees = 0;
            for (int attempt = 0; attempt < 400 && trees < 34; attempt++)
            {
                float a = (float)random.NextDouble() * Mathf.PI * 2f;
                float d = Mathf.Lerp(0.55f, 0.9f, (float)random.NextDouble());
                var p = new Vector2(Mathf.Cos(a) * IslandRX * d, Mathf.Sin(a) * IslandRZ * d);
                if (Mathf.Abs(p.x) < 2.5f || Mathf.Abs(p.y + 4f) < 2.5f) continue;             // paths
                if (keepOut.Any(k => (p - k.c).magnitude < k.r)) continue;
                bool large = random.NextDouble() < 0.4;
                Obj(nature, large ? "Tree Large" : "Tree", large ? "treeLarge" : "tree", "Tree_" + trees, new Vector3(p.x, 0f, p.y), large ? 6.5f : 4.6f, (float)random.NextDouble() * 360f, true, trunkOnly: true);
                keepOut.Add((p, 3.2f));
                trees++;
            }
            // Flower and grass tufts (no colliders).
            string[] tufts = { "Flowers_1", "Grass_1", "Flower_3", "Grass_2", "Flowers_2" };
            for (int i = 0; i < 46; i++)
            {
                float a = (float)random.NextDouble() * Mathf.PI * 2f;
                float d = Mathf.Lerp(0.15f, 0.85f, (float)random.NextDouble());
                var p = new Vector2(Mathf.Cos(a) * IslandRX * d, Mathf.Sin(a) * IslandRZ * d);
                if (Mathf.Abs(p.x) < 2f || Mathf.Abs(p.y + 4f) < 2f || keepOut.Take(10).Any(k => (p - k.c).magnitude < k.r * 0.8f)) continue;
                string name = tufts[i % tufts.Length];
                if (AssetDatabase.LoadAssetAtPath<GameObject>(Crops + name + ".fbx") == null) continue;
                Model(nature, Crops.TrimEnd('/'), name, "Tuft_" + i, new Vector3(p.x, 0f, p.y), new Vector3(0.9f, 0.6f, 0.9f), (float)random.NextDouble() * 360f, false, false);
            }
        }

        // ─────────── Signs (place vocabulary) ───────────

        private static void BuildSigns(Transform signs)
        {
            var places = IslandCatalog.Load().places;
            Signpost(signs, "station", places.station, new Vector3(-2.6f, 0f, -26f), 0f);
            Signpost(signs, "island", places.island, new Vector3(2.8f, 0f, -7.2f), 0f);
            Signpost(signs, "farm", places.farm, new Vector3(-14.5f, 0f, -1.6f), 0f);
            Signpost(signs, "barn", places.barn, new Vector3(13.6f, 0f, -1.6f), 0f);
            Signpost(signs, "shop", places.shop, new Vector3(5.2f, 0f, -16.2f), 0f);
            Signpost(signs, "view", places.view, new Vector3(2.2f, 0f, 24.5f), 0f);
        }

        private static void Signpost(Transform parent, string id, IslandWord word, Vector3 position, float yaw)
        {
            var group = Group(parent, "Sign_" + id);
            group.localPosition = position;
            group.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Block(group, "Post", new Vector3(0f, 0.9f, 0f), new Vector3(0.14f, 1.8f, 0.14f), _signWood, true);
            Block(group, "Board", new Vector3(0f, 1.75f, 0f), new Vector3(1.9f, 0.62f, 0.07f), _signWood, false);
            // Text on both faces, so it reads correctly from either side.
            Text(group, "JA_Front", word.ja, new Vector3(0f, 1.84f, -0.045f), 0f, 0.24f, new Color(0.98f, 0.95f, 0.85f), 1.8f);
            Text(group, "EN_Front", word.en, new Vector3(0f, 1.6f, -0.045f), 0f, 0.11f, new Color(0.95f, 0.85f, 0.55f), 1.8f);
            Text(group, "JA_Back", word.ja, new Vector3(0f, 1.84f, 0.045f), 180f, 0.24f, new Color(0.98f, 0.95f, 0.85f), 1.8f);
            Text(group, "EN_Back", word.en, new Vector3(0f, 1.6f, 0.045f), 180f, 0.11f, new Color(0.95f, 0.85f, 0.55f), 1.8f);
            var trigger = Trigger(group, "WordInteraction", new Vector3(0f, 1f, 0f), new Vector3(1.6f, 2f, 1.6f));
            trigger.AddComponent<IslandWordSpot>().Configure(id);
        }

        // ─────────── Placement helpers ───────────

        /// <summary>Places an FBX model fitted into <paramref name="size"/> (world box after rotation), standing on y = position.y.</summary>
        private static GameObject Model(Transform parent, string folder, string model, string name, Vector3 position, Vector3 size, float yaw, bool collider, bool shadows, bool fitLongest = false)
        {
            string path = folder.EndsWith(".fbx") ? folder : FindModel(folder, model, ".fbx");
            var source = path != null ? AssetDatabase.LoadAssetAtPath<GameObject>(path) : null;
            if (source == null) throw new InvalidOperationException($"Missing model {model} in {folder}");
            return Place(parent, source, name, position, size, yaw, collider, shadows, fitLongest);
        }

        /// <summary>Survival Kit OBJ, scaled to <paramref name="height"/>.</summary>
        private static GameObject Obj(Transform parent, string folder, string model, string name, Vector3 position, float height, float yaw, bool collider, float tilt = 0f, bool trunkOnly = false)
        {
            string path = $"{Survival}{folder}/{model}.obj";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null) throw new InvalidOperationException("Missing survival model " + path);
            var go = Place(parent, source, name, position, new Vector3(999f, height, 999f), yaw, false, true, false);
            if (tilt != 0f) go.transform.Rotate(tilt, 0f, 0f, Space.Self);
            if (collider)
            {
                Bounds b = BoundsOf(go);
                var box = go.AddComponent<BoxCollider>();
                if (trunkOnly)
                {
                    box.center = go.transform.InverseTransformPoint(new Vector3(b.center.x, b.min.y + 1.2f, b.center.z));
                    Vector3 s = go.transform.InverseTransformVector(new Vector3(0.6f, 2.4f, 0.6f));
                    box.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                }
                else
                {
                    box.center = go.transform.InverseTransformPoint(b.center);
                    Vector3 s = go.transform.InverseTransformVector(b.size);
                    box.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                }
            }
            return go;
        }

        private static GameObject Place(Transform parent, GameObject source, string name, Vector3 position, Vector3 size, float yaw, bool collider, bool shadows, bool fitLongest)
        {
            var holder = Group(parent, name);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.transform.SetParent(holder, false);
            foreach (var c in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            holder.localRotation = Quaternion.Euler(0f, yaw, 0f);
            holder.localPosition = position;
            Bounds b = BoundsOf(instance);
            float scale;
            if (fitLongest) scale = Mathf.Max(size.x, size.z) / Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.z));
            else scale = Mathf.Min(size.x / Mathf.Max(0.01f, b.size.x), size.y / Mathf.Max(0.01f, b.size.y), size.z / Mathf.Max(0.01f, b.size.z));
            instance.transform.localScale *= scale;
            b = BoundsOf(instance);
            instance.transform.position += new Vector3(holder.position.x - b.center.x, holder.position.y - b.min.y, holder.position.z - b.center.z);
            ConvertMaterials(instance);
            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                r.receiveShadows = true;
            }
            if (collider)
            {
                b = BoundsOf(instance);
                var box = holder.gameObject.AddComponent<BoxCollider>();
                box.center = holder.InverseTransformPoint(b.center);
                Vector3 s = holder.InverseTransformVector(b.size);
                box.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            }
            return holder.gameObject;
        }

        private static string FindModel(string folder, string model, string extension)
        {
            foreach (string guid in AssetDatabase.FindAssets(model + " t:Model", new[] { folder.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) && string.Equals(Path.GetFileNameWithoutExtension(path), model, StringComparison.OrdinalIgnoreCase))
                    return path;
            }
            return null;
        }

        /// <summary>Imported FBX/OBJ materials that are not URP are replaced by URP Simple Lit copies (colour + texture kept).</summary>
        private static void ConvertMaterials(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = Urp(materials[i]);
                renderer.sharedMaterials = materials;
            }
        }

        private static Material Urp(Material source)
        {
            if (source == null) return null;
            if (source.shader != null && source.shader.name.StartsWith("Universal Render Pipeline", StringComparison.Ordinal)) return source;
            if (Converted.TryGetValue(source, out var cached)) return cached;
            string sourcePath = AssetDatabase.GetAssetPath(source);
            string key = Path.GetFileNameWithoutExtension(sourcePath) + "_" + source.name;
            foreach (char invalid in Path.GetInvalidFileNameChars()) key = key.Replace(invalid, '_');
            key = key.Replace(' ', '_');
            string path = $"{GenDir}/Materials/{key}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Simple Lit")) { name = key };
                AssetDatabase.CreateAsset(material, path);
            }
            Color color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.HasProperty("_Color") ? source.color : Color.white;
            Texture texture = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.HasProperty("_MainTex") ? source.mainTexture : null;
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", texture);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            Converted[source] = material;
            return material;
        }

        private static void UpdateBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
