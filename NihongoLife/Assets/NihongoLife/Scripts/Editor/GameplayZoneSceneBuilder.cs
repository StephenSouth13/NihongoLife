#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Cameras;
using NihongoLife.World;

namespace NihongoLife.EditorTools
{
    public static class GameplayZoneSceneBuilder
    {
        private const string StationMigrationKey = "NihongoLife.StationLayout.v3";
        private const string SceneDir = "Assets/NihongoLife/Scenes";
        private const string CityScene = SceneDir + "/90_TestSandbox.unity";
        private const string StationScene = SceneDir + "/20_StationDistrict.unity";
        private const string SushiScene = SceneDir + "/30_SushiRestaurant.unity";
        private const string Sushi = "Assets/ThirdParty/Sushi Restaurant Kit - May 2023-20260920T035054Z-1-001";
        private const string Train = "Assets/ThirdParty/Train Pack - April 2019-20260920T035456Z-1-001";
        private const string House = "Assets/ThirdParty/Ultimate House Interior Pack - June 2020-20260920T035345Z-1-001";
        private const int InteractableLayer = 6;

        private static void ApplySushiLayoutMigration()
        {
            if (File.Exists(SushiScene))
            {
                string savedScene = File.ReadAllText(SushiScene);
                if (savedScene.Contains("DiningTable_A") && savedScene.Contains("SushiSceneCamera")) return;
            }
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                BuildSushiRestaurant();
                Debug.Log("[GameplayZoneSceneBuilder] Sushi layout v2 saved directly to scene.");
            };
        }

        private static void ApplyStationLayoutMigration()
        {
            if (File.Exists(StationScene))
            {
                string savedScene = File.ReadAllText(StationScene);
                if (savedScene.Contains("TrainCarriageInterior") && savedScene.Contains("StationTravelController")) return;
            }
            if (SessionState.GetBool(StationMigrationKey, false)) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                SessionState.SetBool(StationMigrationKey, true);
                BuildStation();
                Debug.Log("[GameplayZoneSceneBuilder] Station layout v2 saved directly to scene.");
            };
        }

        public static void BuildAll()
        {
            BuildStation();
            BuildSushiRestaurant();
            AddCityPortals();
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[GameplayZoneSceneBuilder] Built Station District and Sushi Restaurant additive zones.");
        }

        public static void BuildStation()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Vector3 origin = new Vector3(800f, 0f, 0f);
            var root = new GameObject("StationDistrict_Zone");
            root.AddComponent<SceneZoneVisibility>();
            root.AddComponent<StandaloneZoneBootstrap>().Configure("station_entrance");
            root.AddComponent<StationMetroEnvironment>();
            var travel = root.AddComponent<StationTravelController>();

            CreateBlock(root.transform, "StationGround", origin + new Vector3(0f, -0.15f, 0f), new Vector3(34f, 0.3f, 22f), new Color(0.2f, 0.23f, 0.25f));
            CreateBlock(root.transform, "Platform", origin + new Vector3(0f, 0.15f, 2.8f), new Vector3(30f, 0.3f, 5f), new Color(0.62f, 0.6f, 0.54f));
            CreateBlock(root.transform, "SafetyLine", origin + new Vector3(0f, 0.33f, 0.55f), new Vector3(30f, 0.035f, 0.28f), new Color(0.95f, 0.72f, 0.12f));
            for (int i = -2; i <= 2; i++)
                Place(Train, "RailwayTrack_Straight", root.transform, $"Track_{i}", origin + new Vector3(i * 6f, 0f, -3.2f), new Vector3(6.2f, 0.28f, 2.1f), Quaternion.identity, true);
            Place(Train, "HighSpeed_Front", root.transform, "HighSpeed_Front", origin + new Vector3(-10f, 0.1f, -3.2f), new Vector3(12f, 3.2f, 3f), Quaternion.identity, true);
            Place(Train, "HighSpeed_Wagon", root.transform, "HighSpeed_Wagon_A", origin + new Vector3(0f, 0.1f, -3.2f), new Vector3(12f, 3.2f, 3f), Quaternion.identity, true);
            Place(Train, "HighSpeed_Wagon", root.transform, "HighSpeed_Wagon_B", origin + new Vector3(10f, 0.1f, -3.2f), new Vector3(12f, 3.2f, 3f), Quaternion.identity, true);
            Place(Sushi, "Environment_Bench", root.transform, "PlatformBench_A", origin + new Vector3(-6f, 0.3f, 4f), new Vector3(2.4f, 1.0f, 0.8f), Quaternion.Euler(0f, 180f, 0f), true);
            Place(Sushi, "Environment_Bench", root.transform, "PlatformBench_B", origin + new Vector3(3f, 0.3f, 4f), new Vector3(2.4f, 1.0f, 0.8f), Quaternion.Euler(0f, 180f, 0f), true);
            Place(Sushi, "Environment_Arch", root.transform, "TicketGateArch", origin + new Vector3(0f, 0.3f, 8.5f), new Vector3(4f, 3.3f, 1.2f), Quaternion.identity, true);
            CreateBlock(root.transform, "PlatformCanopy", origin + new Vector3(0f, 3.4f, 4.7f), new Vector3(24f, 0.18f, 4.6f), new Color(0.16f, 0.23f, 0.28f));
            for (int i = -2; i <= 2; i++)
                CreateBlock(root.transform, $"CanopyPost_{i}", origin + new Vector3(i * 5f, 1.7f, 5.7f), new Vector3(0.18f, 3.4f, 0.18f), new Color(0.24f, 0.31f, 0.35f));
            Place(Sushi, "Environment_Counter_Doors", root.transform, "TicketGate_A", origin + new Vector3(-2.1f, 0f, 7.5f), new Vector3(1.4f, 1.05f, 0.8f), Quaternion.identity, true);
            Place(Sushi, "Environment_Counter_Doors", root.transform, "TicketGate_B", origin + new Vector3(2.1f, 0f, 7.5f), new Vector3(1.4f, 1.05f, 0.8f), Quaternion.identity, true);
            GameObject ticketMachine = Place(Sushi, "Environment_Cabinet_Doors", root.transform, "TicketMachine_A", origin + new Vector3(-6.4f, 0f, 7.5f), new Vector3(1.2f, 1.8f, 0.75f), Quaternion.identity, true);
            if (ticketMachine != null)
            {
                CreateSign(root.transform, "TICKETS / きっぷ", ticketMachine.transform.position + new Vector3(0f, 2.05f, 0f), new Vector2(3f, 0.5f));
                AddStationInteraction(ticketMachine, travel, StationAction.BuyTicket, "切符を買う", "Mua ve");
            }
            Transform gate = root.transform.Find("TicketGate_A");
            if (gate != null) AddStationInteraction(gate.gameObject, travel, StationAction.PassGate, "改札を通る", "Qua cong soat ve");
            GameObject stationJob = Place(Sushi, "Environment_Counter_Straight", root.transform, "StationJobDesk", origin + new Vector3(6.2f, 0f, 7.2f), new Vector3(2.1f, 1.05f, 0.9f), Quaternion.identity, true);
            if (stationJob != null) CreateJobPoint(stationJob.transform, JobRole.StationAssistant, 35, 520, 10, 24f);
            CreateSign(root.transform, "駅前 / KHU NHÀ GA", origin + new Vector3(0f, 3.8f, 8.5f), new Vector2(8f, 0.9f));
            CreateLighting(root.transform, origin + new Vector3(0f, 6f, 2f));
            CreateSpawn(root.transform, "station_entrance", origin + new Vector3(0f, 0.38f, 6.8f), Quaternion.Euler(0f, 180f, 0f));
            CreateExitPortal(root.transform, "ExitToCity", StationScene, "city_station_return", "街へ戻る / Trở lại thành phố", origin + new Vector3(0f, 1.1f, 9.8f));

            BuildTrainJourney(root.transform, travel, origin);
            CreatePreviewCamera(root.transform, origin, "StationSceneCamera", new Vector3(0f, 5.2f, 14.5f), new Vector3(0f, 1.2f, 1.5f));
            EditorSceneManager.SaveScene(scene, StationScene);
        }

        public static void BuildSushiRestaurant()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Vector3 origin = new Vector3(500f, 0f, 0f);
            var root = new GameObject("SushiRestaurant_Zone");
            root.AddComponent<SceneZoneVisibility>();
            root.AddComponent<StandaloneZoneBootstrap>().Configure("sushi_entrance");

            CreateBlock(root.transform, "Floor", origin + new Vector3(0f, -0.1f, 0f), new Vector3(16f, 0.2f, 18f), new Color(0.22f, 0.16f, 0.12f));
            CreateBlock(root.transform, "BackWall", origin + new Vector3(0f, 2.2f, 8.8f), new Vector3(16f, 4.4f, 0.25f), new Color(0.82f, 0.78f, 0.68f));
            CreateBlock(root.transform, "LeftWall", origin + new Vector3(-7.9f, 2.2f, 0f), new Vector3(0.25f, 4.4f, 18f), new Color(0.82f, 0.78f, 0.68f));
            CreateBlock(root.transform, "RightWall", origin + new Vector3(7.9f, 2.2f, 0f), new Vector3(0.25f, 4.4f, 18f), new Color(0.82f, 0.78f, 0.68f));
            CreateBlock(root.transform, "FrontWall_L", origin + new Vector3(-5.2f, 2.2f, -8.8f), new Vector3(5.5f, 4.4f, 0.25f), new Color(0.82f, 0.78f, 0.68f));
            CreateBlock(root.transform, "FrontWall_R", origin + new Vector3(5.2f, 2.2f, -8.8f), new Vector3(5.5f, 4.4f, 0.25f), new Color(0.82f, 0.78f, 0.68f));
            CreateBlock(root.transform, "FrontHeader", origin + new Vector3(0f, 3.75f, -8.8f), new Vector3(4.9f, 1.3f, 0.25f), new Color(0.25f, 0.12f, 0.07f));

            for (int i = -2; i <= 2; i++)
            {
                Place(Sushi, "Environment_Counter_Straight", root.transform, $"SushiCounter_{i}", origin + new Vector3(i * 2.05f, 0f, 3.7f), new Vector3(2.1f, 1.05f, 0.9f), Quaternion.identity, true);
                Place(Sushi, "Environment_Stool", root.transform, $"CounterStool_{i}", origin + new Vector3(i * 2.05f, 0f, 2.2f), new Vector3(0.65f, 0.8f, 0.65f), Quaternion.Euler(0f, 180f, 0f), true);
            }
            CreateDiningSet(root.transform, origin + new Vector3(-3.8f, 0f, -2.4f), "A");
            CreateDiningSet(root.transform, origin + new Vector3(3.8f, 0f, -2.4f), "B");
            Place(Sushi, "Environment_Fridge", root.transform, "KitchenFridge", origin + new Vector3(5.9f, 0f, 7.2f), new Vector3(1.8f, 2.5f, 1.3f), Quaternion.Euler(0f, 180f, 0f), true);
            Place(Sushi, "Environment_Counter_Sink", root.transform, "KitchenSink", origin + new Vector3(-5.5f, 0f, 7.2f), new Vector3(1.8f, 1.0f, 1.2f), Quaternion.Euler(0f, 180f, 0f), true);
            Place(Sushi, "Environment_Oven", root.transform, "KitchenOven", origin + new Vector3(-3.5f, 0f, 7.2f), new Vector3(1.4f, 1.7f, 1.2f), Quaternion.Euler(0f, 180f, 0f), true);
            Place(Sushi, "Environment_CuttingTable", root.transform, "PrepTable", origin + new Vector3(0f, 0f, 7.1f), new Vector3(3.4f, 1.0f, 1.4f), Quaternion.identity, true);
            Place(Sushi, "Decoration_Bamboo", root.transform, "Bamboo_L", origin + new Vector3(-6.6f, 0f, 7.4f), new Vector3(1f, 2.8f, 1f), Quaternion.identity, false);
            Place(Sushi, "Decoration_Bamboo", root.transform, "Bamboo_R", origin + new Vector3(6.6f, 0f, 7.4f), new Vector3(1f, 2.8f, 1f), Quaternion.identity, false);
            Place(Sushi, "Decoration_Painting", root.transform, "WallPainting", origin + new Vector3(0f, 1.8f, 8.55f), new Vector3(3.8f, 2f, 0.2f), Quaternion.Euler(0f, 180f, 0f), false);
            Place(House, "Light_Ceiling3", root.transform, "CeilingLight_A", origin + new Vector3(-3f, 3.8f, 1f), new Vector3(1f, 0.5f, 1f), Quaternion.identity, false);
            Place(House, "Light_Ceiling3", root.transform, "CeilingLight_B", origin + new Vector3(3f, 3.8f, 1f), new Vector3(1f, 0.5f, 1f), Quaternion.identity, false);

            string[] food = { "Food_Onigiri", "Food_EbiNigiri", "Food_MaguroNigiri", "Food_SalmonNigiri", "Food_TamagoNigiri", "Food_Roll" };
            for (int i = 0; i < food.Length; i++)
            {
                int counterIndex = Mathf.Clamp(Mathf.RoundToInt((-3f + i * 1.2f) / 2.05f), -2, 2);
                Transform support = root.transform.Find($"SushiCounter_{counterIndex}");
                PlaceOnSurface(Sushi, food[i], root.transform, $"Menu_{food[i]}", support != null ? support.gameObject : null,
                    new Vector2((-3f + i * 1.2f) - counterIndex * 2.05f, -0.1f), new Vector3(0.38f, 0.3f, 0.38f), Quaternion.identity);
            }

            GameObject clerkDesk = root.transform.Find("SushiCounter_2")?.gameObject;
            if (clerkDesk != null) CreateJobPoint(clerkDesk.transform, JobRole.StoreClerk, 20, 450, 8, 22f);

            CreateSign(root.transform, "すし店 / NHÀ HÀNG SUSHI", origin + new Vector3(0f, 3.5f, -8.4f), new Vector2(8f, 0.9f));
            CreateLighting(root.transform, origin + new Vector3(0f, 5f, 1f));
            CreateInteriorLight(root.transform, "DiningLight_L", origin + new Vector3(-3.5f, 3.5f, -1.5f), 8f, 2.2f);
            CreateInteriorLight(root.transform, "DiningLight_R", origin + new Vector3(3.5f, 3.5f, -1.5f), 8f, 2.2f);
            CreateInteriorLight(root.transform, "CounterLight", origin + new Vector3(0f, 3.4f, 3.2f), 9f, 2.6f);
            CreateInteriorLight(root.transform, "KitchenLight", origin + new Vector3(0f, 3.6f, 7f), 7f, 1.8f);
            CreateSpawn(root.transform, "sushi_entrance", origin + new Vector3(0f, 0.25f, -6.6f), Quaternion.identity);
            CreateExitPortal(root.transform, "ExitToCity", SushiScene, "city_sushi_return", "街へ戻る / Trở lại thành phố", origin + new Vector3(0f, 1.1f, -8f));
            CreatePreviewCamera(root.transform, origin, "SushiSceneCamera", new Vector3(0f, 4.2f, -11.5f), new Vector3(0f, 1.2f, 1.2f));
            ValidateSushiLayout(root);
            EditorSceneManager.SaveScene(scene, SushiScene);
        }

        private static void CreateDiningSet(Transform parent, Vector3 center, string suffix)
        {
            GameObject table = Place(Sushi, "Environment_Table", parent, $"DiningTable_{suffix}", center, new Vector3(2.5f, 0.86f, 1.45f), Quaternion.identity, true);
            Place(Sushi, "Environment_Chair1", parent, $"DiningChair_{suffix}_N", center + new Vector3(0f, 0f, 1.35f), new Vector3(0.75f, 1.05f, 0.75f), Quaternion.Euler(0f, 180f, 0f), true);
            Place(Sushi, "Environment_Chair1", parent, $"DiningChair_{suffix}_S", center + new Vector3(0f, 0f, -1.35f), new Vector3(0.75f, 1.05f, 0.75f), Quaternion.identity, true);
            PlaceOnSurface(Sushi, "Environment_Bottle", parent, $"TableBottle_{suffix}", table, new Vector2(0.55f, 0f), new Vector3(0.14f, 0.28f, 0.14f), Quaternion.identity);
            PlaceOnSurface(Sushi, "Environment_Bowl", parent, $"TableBowl_{suffix}", table, new Vector2(-0.45f, 0f), new Vector3(0.32f, 0.16f, 0.32f), Quaternion.identity);
        }

        private static void CreatePreviewCamera(Transform parent, Vector3 origin, string name, Vector3 cameraOffset, Vector3 lookOffset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = origin + cameraOffset;
            go.transform.rotation = Quaternion.LookRotation(origin + lookOffset - go.transform.position);
            go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 150f;
            go.AddComponent<AudioListener>();
            go.AddComponent<ZonePreviewCamera>();
        }

        private static void CreateInteriorLight(Transform parent, string name, Vector3 position, float range, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = range;
            light.intensity = intensity;
            light.color = new Color(1f, 0.78f, 0.55f);
            light.shadows = LightShadows.None;
        }

        private static void ValidateSushiLayout(GameObject root)
        {
            if (root.GetComponentInChildren<Camera>(true) == null) throw new InvalidOperationException("Sushi scene camera is missing.");
            if (root.GetComponentsInChildren<BoxCollider>(true).Length < 12) throw new InvalidOperationException("Sushi scene collision layout is incomplete.");
            if (root.transform.Find("DiningTable_A") == null || root.transform.Find("DiningTable_B") == null)
                throw new InvalidOperationException("Sushi dining tables are missing.");

        }

        private static void AddCityPortals()
        {
            Scene scene = EditorSceneManager.OpenScene(CityScene, OpenSceneMode.Single);
            GameObject existing = GameObject.Find("AdditiveZonePortals");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            var root = new GameObject("AdditiveZonePortals");

            CreateCityPortal(root.transform, "StationPortal", StationScene, "station_entrance", "駅前 / Khu nhà ga", new Vector3(9f, 1f, -4f), new Color(0.2f, 0.52f, 0.72f));
            CreateSpawn(root.transform, "city_station_return", new Vector3(9f, 0.1f, -6.2f), Quaternion.Euler(0f, 180f, 0f));
            CreateCityPortal(root.transform, "SushiPortal", SushiScene, "sushi_entrance", "すし店 / Nhà hàng sushi", new Vector3(-9f, 1f, -4f), new Color(0.76f, 0.2f, 0.18f));
            CreateSpawn(root.transform, "city_sushi_return", new Vector3(-9f, 0.1f, -6.2f), Quaternion.Euler(0f, 180f, 0f));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CityScene);
        }

        private static void CreateCityPortal(Transform parent, string name, string scene, string spawn, string display, Vector3 position, Color color)
        {
            var marker = CreateBlock(parent, name, position, new Vector3(2.8f, 2f, 0.35f), color);
            marker.layer = InteractableLayer;
            marker.GetComponent<BoxCollider>().isTrigger = true;
            marker.AddComponent<ScenePortal>().Configure(Path.GetFileNameWithoutExtension(scene), spawn, display, false);
            CreateSign(marker.transform, display, position + Vector3.up * 1.45f, new Vector2(4.5f, 0.55f));
        }

        private static void CreateExitPortal(Transform parent, string name, string zoneScene, string citySpawn, string display, Vector3 position)
        {
            var portal = new GameObject(name);
            portal.layer = InteractableLayer;
            portal.transform.SetParent(parent);
            portal.transform.position = position;
            var collider = portal.AddComponent<BoxCollider>();
            collider.size = new Vector3(3f, 2.2f, 1f);
            collider.isTrigger = true;
            portal.AddComponent<ScenePortal>().Configure(Path.GetFileNameWithoutExtension(CityScene), citySpawn, display, true);
        }

        private static void CreateSpawn(Transform parent, string id, Vector3 position, Quaternion rotation)
        {
            var spawn = new GameObject("Spawn_" + id);
            spawn.transform.SetParent(parent);
            spawn.transform.SetPositionAndRotation(position, rotation);
            spawn.AddComponent<SceneSpawnPoint>().Configure(id);
        }

        private static void BuildTrainJourney(Transform parent, StationTravelController travel, Vector3 stationOrigin)
        {
            var cabin = new GameObject("TrainCarriageInterior").transform;
            cabin.SetParent(parent);
            Vector3 center = stationOrigin + new Vector3(0f, 0f, 42f);

            CreateBlock(cabin, "CarriageFloor", center, new Vector3(14f, 0.2f, 4.8f), new Color(0.16f, 0.19f, 0.22f));
            CreateBlock(cabin, "CarriageCeiling", center + Vector3.up * 3.25f, new Vector3(14f, 0.18f, 4.8f), new Color(0.88f, 0.89f, 0.86f));
            CreateBlock(cabin, "CarriageEnd_W", center + new Vector3(-7f, 1.65f, 0f), new Vector3(0.2f, 3.3f, 4.8f), new Color(0.78f, 0.8f, 0.8f));
            CreateBlock(cabin, "CarriageEnd_E", center + new Vector3(7f, 1.65f, 0f), new Vector3(0.2f, 3.3f, 4.8f), new Color(0.78f, 0.8f, 0.8f));

            for (int i = -3; i <= 3; i++)
            {
                float x = i * 2f;
                CreateBlock(cabin, $"WindowPost_N_{i}", center + new Vector3(x, 1.65f, 2.35f), new Vector3(0.16f, 3.1f, 0.16f), new Color(0.18f, 0.24f, 0.28f));
                CreateBlock(cabin, $"WindowPost_S_{i}", center + new Vector3(x, 1.65f, -2.35f), new Vector3(0.16f, 3.1f, 0.16f), new Color(0.18f, 0.24f, 0.28f));
            }
            CreateBlock(cabin, "WindowRail_N_Low", center + new Vector3(0f, 0.65f, 2.35f), new Vector3(14f, 1.1f, 0.16f), new Color(0.75f, 0.78f, 0.78f));
            CreateBlock(cabin, "WindowRail_N_High", center + new Vector3(0f, 2.85f, 2.35f), new Vector3(14f, 0.75f, 0.16f), new Color(0.75f, 0.78f, 0.78f));
            CreateBlock(cabin, "WindowRail_S_Low", center + new Vector3(0f, 0.65f, -2.35f), new Vector3(14f, 1.1f, 0.16f), new Color(0.75f, 0.78f, 0.78f));
            CreateBlock(cabin, "WindowRail_S_High", center + new Vector3(0f, 2.85f, -2.35f), new Vector3(14f, 0.75f, 0.16f), new Color(0.75f, 0.78f, 0.78f));

            for (int i = -2; i <= 2; i++)
            {
                Place(Sushi, "Environment_Bench", cabin, $"CarriageSeat_N_{i}", center + new Vector3(i * 2.3f, 0.1f, 1.55f), new Vector3(1.65f, 0.85f, 0.7f), Quaternion.Euler(0f, 180f, 0f), true);
                Place(Sushi, "Environment_Bench", cabin, $"CarriageSeat_S_{i}", center + new Vector3(i * 2.3f, 0.1f, -1.55f), new Vector3(1.65f, 0.85f, 0.7f), Quaternion.identity, true);
            }

            var platformSpawn = new GameObject("TravelPlatformSpawn").transform;
            platformSpawn.SetParent(parent);
            platformSpawn.SetPositionAndRotation(stationOrigin + new Vector3(0f, 0.38f, 1.9f), Quaternion.identity);
            var carriageSpawn = new GameObject("TravelCarriageSpawn").transform;
            carriageSpawn.SetParent(cabin);
            carriageSpawn.SetPositionAndRotation(center + new Vector3(-5.3f, 0.3f, 0f), Quaternion.Euler(0f, 90f, 0f));

            GameObject board = CreateBlock(parent, "BoardTrainDoor", stationOrigin + new Vector3(-0.5f, 1.15f, 0.9f), new Vector3(1.6f, 2.2f, 0.18f), new Color(0.12f, 0.48f, 0.62f));
            AddStationInteraction(board, travel, StationAction.BoardTrain, "電車に乗る", "Len tau");
            GameObject ride = CreateBlock(cabin, "StartRidePanel", center + new Vector3(-5.8f, 1.25f, 2.2f), new Vector3(1.5f, 0.75f, 0.12f), new Color(0.12f, 0.48f, 0.62f));
            AddStationInteraction(ride, travel, StationAction.StartRide, "出発する", "Bat dau hanh trinh");
            GameObject leave = CreateBlock(cabin, "LeaveTrainDoor", center + new Vector3(6.85f, 1.15f, 0f), new Vector3(0.18f, 2.2f, 1.5f), new Color(0.12f, 0.48f, 0.62f));
            AddStationInteraction(leave, travel, StationAction.LeaveTrain, "電車を降りる", "Xuong tau");
            GameObject passenger = CreateBlock(cabin, "PassengerConversation", center + new Vector3(2.3f, 1.05f, 1.45f), new Vector3(0.45f, 1.7f, 0.45f), new Color(0.35f, 0.52f, 0.68f));
            AddStationInteraction(passenger, travel, StationAction.TalkPassenger, "話す", "Noi chuyen");

            var scenery = new GameObject("MovingWindowScenery").transform;
            scenery.SetParent(cabin);
            scenery.position = center;
            for (int i = 0; i < 8; i++)
            {
                float x = -21f + i * 6f;
                GameObject building = CreateBlock(scenery, $"SceneryBuilding_{i}", center + new Vector3(x, 1.4f + i % 3 * 0.35f, 6f),
                    new Vector3(3.5f, 2.8f + i % 3 * 0.7f, 2.2f), i % 2 == 0 ? new Color(0.38f, 0.48f, 0.52f) : new Color(0.52f, 0.44f, 0.38f));
                building.transform.SetParent(scenery, true);
            }
            CreateBlock(scenery, "SceneryGround", center + new Vector3(0f, -0.2f, 5.5f), new Vector3(54f, 0.2f, 8f), new Color(0.2f, 0.34f, 0.24f)).transform.SetParent(scenery, true);
            CreateInteriorLight(cabin, "CarriageLight_A", center + new Vector3(-3.5f, 2.8f, 0f), 7f, 1.6f);
            CreateInteriorLight(cabin, "CarriageLight_B", center + new Vector3(3.5f, 2.8f, 0f), 7f, 1.6f);
            travel.Configure(platformSpawn, carriageSpawn, scenery);
            cabin.gameObject.SetActive(false);
        }

        private static void AddStationInteraction(GameObject target, StationTravelController controller, StationAction action, string ja, string en)
        {
            if (target == null) return;
            target.layer = InteractableLayer;
            if (target.GetComponent<Collider>() == null) target.AddComponent<BoxCollider>();
            target.AddComponent<StationTravelInteractable>().Configure(controller, action, ja, en);
        }

        private static void CreateJobPoint(Transform support, JobRole role, int requiredKnowledge, int pay, int knowledgeReward, float energyCost)
        {
            var point = new GameObject("JobPoint_" + role);
            point.layer = InteractableLayer;
            point.transform.SetParent(support, false);
            point.transform.localPosition = new Vector3(0f, 1.2f, -0.8f);
            var trigger = point.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.4f, 1.8f, 1.4f);
            point.AddComponent<JobInteractable>().Configure(role, requiredKnowledge, pay, knowledgeReward, energyCost);
        }

        private static GameObject PlaceOnSurface(string folder, string model, Transform parent, string name,
            GameObject support, Vector2 horizontalOffset, Vector3 targetSize, Quaternion rotation)
        {
            if (support == null) return null;
            Bounds supportBounds = BoundsOf(support);
            Vector3 position = new Vector3(supportBounds.center.x + horizontalOffset.x, supportBounds.max.y, supportBounds.center.z + horizontalOffset.y);
            GameObject item = Place(folder, model, parent, name, position, targetSize, rotation, false);
            if (item == null) return null;
            Bounds itemBounds = BoundsOf(item);
            item.transform.position += Vector3.up * (supportBounds.max.y + 0.01f - itemBounds.min.y);
            return item;
        }

        private static GameObject Place(string folder, string model, Transform parent, string name, Vector3 position, Vector3 targetSize, Quaternion rotation, bool collider)
        {
            string path = FindFbx(folder, model);
            if (string.IsNullOrEmpty(path)) { Debug.LogWarning($"Missing zone asset: {model}"); return null; }
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = name;
            instance.transform.SetParent(parent);
            Quaternion importedRotation = instance.transform.rotation;
            instance.transform.SetPositionAndRotation(position, rotation * importedRotation);
            Bounds bounds = BoundsOf(instance);
            float scale = Mathf.Min(targetSize.x / Mathf.Max(bounds.size.x, 0.001f), targetSize.y / Mathf.Max(bounds.size.y, 0.001f), targetSize.z / Mathf.Max(bounds.size.z, 0.001f));
            instance.transform.localScale *= scale;
            bounds = BoundsOf(instance);
            instance.transform.position += Vector3.up * (position.y - bounds.min.y);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>()) renderer.shadowCastingMode = ShadowCastingMode.On;
            if (collider)
            {
                bounds = BoundsOf(instance);
                var box = instance.AddComponent<BoxCollider>();
                box.center = instance.transform.InverseTransformPoint(bounds.center);
                Vector3 s = instance.transform.lossyScale;
                box.size = new Vector3(bounds.size.x / Mathf.Abs(s.x), bounds.size.y / Mathf.Abs(s.y), bounds.size.z / Mathf.Abs(s.z));
            }
            return instance;
        }

        private static string FindFbx(string folder, string name)
        {
            foreach (string guid in AssetDatabase.FindAssets($"{name} t:Model", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) && string.Equals(Path.GetFileNameWithoutExtension(path), name, StringComparison.OrdinalIgnoreCase)) return path;
            }
            return null;
        }

        private static Bounds BoundsOf(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(root.transform.position, Vector3.one);
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = scale;
            var material = new Material(Shader.Find("Universal Render Pipeline/Simple Lit")) { color = color };
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static void CreateSign(Transform parent, string value, Vector3 position, Vector2 size)
        {
            var go = new GameObject("Sign_" + value);
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var text = go.AddComponent<TextMeshPro>();
            var font = NihongoLife.Editor.FontSetup.EnsureJapaneseFontAsset();
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = 3f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.86f, 0.42f);
            text.rectTransform.sizeDelta = size;
        }

        private static void CreateLighting(Transform parent, Vector3 position)
        {
            var go = new GameObject("ZoneLighting");
            go.transform.SetParent(parent);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 22f;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.86f, 0.68f);
            light.shadows = LightShadows.None;
        }

        private static void UpdateBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            AddOrEnable(scenes, StationScene);
            AddOrEnable(scenes, SushiScene);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AddOrEnable(System.Collections.Generic.List<EditorBuildSettingsScene> scenes, string path)
        {
            int index = scenes.FindIndex(scene => scene.path == path);
            if (index >= 0) scenes[index] = new EditorBuildSettingsScene(path, true);
            else scenes.Add(new EditorBuildSettingsScene(path, true));
        }
    }
}
#endif
