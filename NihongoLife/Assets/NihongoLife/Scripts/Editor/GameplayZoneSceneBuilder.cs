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
using NihongoLife.NPC;
using NihongoLife.Home;

namespace NihongoLife.EditorTools
{
    public static class GameplayZoneSceneBuilder
    {
        private const string StationMigrationKey = "NihongoLife.StationLayout.v3";
        private const string SceneDir = "Assets/NihongoLife/Scenes";
        private const string CityScene = SceneDir + "/90_TestSandbox.unity";
        private const string StationScene = SceneDir + "/20_StationDistrict.unity";
        private const string SushiScene = SceneDir + "/30_SushiRestaurant.unity";
        private const string SchoolScene = SceneDir + "/40_ HIBARICLASS.unity";
        private const string HomeBedroomScene = SceneDir + "/45_HomeBedroom.unity";
        private const string Sushi = "Assets/ThirdParty/Sushi Restaurant Kit - May 2023-20260920T035054Z-1-001";
        private const string Train = "Assets/ThirdParty/Train Pack - April 2019-20260920T035456Z-1-001";
        private const string House = "Assets/ThirdParty/Ultimate House Interior Pack - June 2020-20260920T035345Z-1-001";
        private const string Styloo = "Assets/ThirdParty/StylooClassroomAssetPack GLTF & FBX";
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
            BuildHibariSchool();
            AddCityPortals();
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[GameplayZoneSceneBuilder] Built Station District, Sushi Restaurant and Hibari School additive zones.");
        }

        /// <summary>
        /// ひばり日本語学院 — the story's school (chapter 1, scenario.school.self_intro: npc_teacher_morita,
        /// npc_classmate_kim). This zone is a visual/immersion addition only: the scenario itself is pure
        /// Dialogue nodes with no GoToArea/TalkToNPC gate, so it already plays regardless of location.
        /// Reaching this room is not required to progress the story; it gives Morita and Kim a real place
        /// to stand and be talked to, matching how npc_guide/npc_cashier exist outside any single scenario.
        /// </summary>
        public static void BuildHibariSchool()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Vector3 origin = new Vector3(1000f, 0f, 0f);
            var root = new GameObject("HibariSchool_Zone");
            root.AddComponent<SceneZoneVisibility>();
            root.AddComponent<StandaloneZoneBootstrap>().Configure("school_entrance");

            CreateBlock(root.transform, "SchoolFloor", origin + new Vector3(0f, -0.1f, 0f), new Vector3(16f, 0.2f, 14f), new Color(0.58f, 0.52f, 0.42f));
            CreateBlock(root.transform, "SchoolBackWall", origin + new Vector3(0f, 2.5f, 6.9f), new Vector3(16f, 5f, 0.25f), new Color(0.85f, 0.86f, 0.82f));
            CreateBlock(root.transform, "SchoolLeftWall", origin + new Vector3(-7.9f, 2.5f, 0f), new Vector3(0.25f, 5f, 14f), new Color(0.85f, 0.86f, 0.82f));
            CreateBlock(root.transform, "SchoolRightWall", origin + new Vector3(7.9f, 2.5f, 0f), new Vector3(0.25f, 5f, 14f), new Color(0.85f, 0.86f, 0.82f));
            CreateBlock(root.transform, "SchoolFrontWall_L", origin + new Vector3(-5.5f, 2.5f, -6.9f), new Vector3(5f, 5f, 0.25f), new Color(0.85f, 0.86f, 0.82f));
            CreateBlock(root.transform, "SchoolFrontWall_R", origin + new Vector3(5.5f, 2.5f, -6.9f), new Vector3(5f, 5f, 0.25f), new Color(0.85f, 0.86f, 0.82f));
            CreateBlock(root.transform, "SchoolFrontHeader", origin + new Vector3(0f, 4.3f, -6.9f), new Vector3(6f, 1.4f, 0.25f), new Color(0.16f, 0.32f, 0.22f));
            CreateSign(root.transform, "ひばり日本語学院 / HIBARI JAPANESE SCHOOL", origin + new Vector3(0f, 3.6f, -7.15f), new Vector2(9f, 0.9f));

            Place(Styloo, "blackboardbig", root.transform, "Blackboard", origin + new Vector3(0f, 1.65f, 6.6f), new Vector3(4.4f, 2f, 0.2f), Quaternion.Euler(0f, 180f, 0f), true);
            Place(Styloo, "table", root.transform, "TeacherTable", origin + new Vector3(0f, 0f, 5.1f), new Vector3(2f, 0.85f, 1f), Quaternion.identity, true);

            for (int row = 0; row < 2; row++)
            for (int column = 0; column < 3; column++)
            {
                Vector3 position = origin + new Vector3(-3.5f + column * 3.5f, 0f, 1f + row * 2.6f);
                Place(Styloo, "desk", root.transform, $"StudentDesk_{row}_{column}", position, new Vector3(1.3f, 0.9f, 0.8f), Quaternion.identity, true);
                Place(Styloo, "chairtable", root.transform, $"StudentChair_{row}_{column}", position + Vector3.back * 0.75f, new Vector3(0.75f, 0.95f, 0.75f), Quaternion.identity, true);
            }

            Place(Styloo, "shelf", root.transform, "BookShelf", origin + new Vector3(-7.3f, 0f, 5f), new Vector3(1.2f, 1.8f, 0.5f), Quaternion.Euler(0f, 90f, 0f), true);
            Place(Styloo, "locker", root.transform, "StudentLocker", origin + new Vector3(7.3f, 0f, 5f), new Vector3(1.4f, 1.8f, 0.5f), Quaternion.Euler(0f, 270f, 0f), true);

            CreatePlaceholderNpc(root.transform, "TeacherMorita", "npc_teacher_morita", "Morita", "Teacher",
                "Assets/NihongoLife/Prefabs/Characters/NL_Guide.prefab", new Color(0.75f, 0.35f, 0.55f),
                origin + new Vector3(0f, 0.05f, 4.1f), Quaternion.Euler(0f, 180f, 0f),
                "自己紹介の練習をしましょう。", "じこしょうかいのれんしゅうをしましょう。",
                "Let's practice self-introductions.", "Jikoshoukai no renshuu wo shimashou.");
            CreatePlaceholderNpc(root.transform, "ClassmateKim", "npc_classmate_kim", "Kim", "Classmate",
                "Assets/NihongoLife/Prefabs/Characters/NL_Neighbor.prefab", new Color(0.3f, 0.55f, 0.85f),
                origin + new Vector3(-3.5f, 0.05f, 1f), Quaternion.identity,
                "こんにちは。同じクラスですね。", "こんにちは。おなじクラスですね。",
                "Hi. Looks like we're in the same class.", "Konnichiwa. Onaji kurasu desu ne.");

            CreateSpawn(root.transform, "school_entrance", origin + new Vector3(0f, 0.25f, -6.2f), Quaternion.identity);
            CreateExitPortal(root.transform, "ExitToCity", SchoolScene, "city_school_return", "街へ戻る / Trở lại thành phố", origin + new Vector3(0f, 1.1f, -7.6f));
            CreatePreviewCamera(root.transform, origin, "SchoolSceneCamera", new Vector3(0f, 4.6f, -11f), new Vector3(0f, 1.3f, 2f));
            CreateLighting(root.transform, origin + new Vector3(0f, 5f, 1f));
            CreateInteriorLight(root.transform, "ClassroomLight_A", origin + new Vector3(-3f, 3.8f, 2f), 9f, 2f);
            CreateInteriorLight(root.transform, "ClassroomLight_B", origin + new Vector3(3f, 3.8f, 2f), 9f, 2f);
            EditorSceneManager.SaveScene(scene, SchoolScene);
        }

        /// <summary>Places a talkable NPC using an existing character prefab as a stand-in visual (no
        /// dedicated Morita/Kim model exists yet — same placeholder pattern used for Yamada/Kimura).</summary>
        private static void CreatePlaceholderNpc(Transform parent, string goName, string npcId, string displayName, string role,
            string visualPrefabPath, Color fallbackColor, Vector3 position, Quaternion rotation,
            string fallbackJa, string fallbackReading, string fallbackEn, string fallbackRomaji)
        {
            Debug.LogWarning($"[GameplayZoneSceneBuilder] '{displayName}' ({npcId}) is using a placeholder visual ({visualPrefabPath}) — no dedicated model exists yet.");

            var npc = new GameObject(goName);
            npc.layer = InteractableLayer;
            npc.transform.SetParent(parent);
            npc.transform.SetPositionAndRotation(position, rotation);

            var collider = npc.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.95f, 0f);
            collider.size = new Vector3(0.9f, 1.9f, 0.9f);
            collider.isTrigger = true;

            var body = npc.AddComponent<CharacterController>();
            body.center = new Vector3(0f, 0.95f, 0f);
            body.height = 1.85f;
            body.radius = 0.32f;
            body.stepOffset = 0.22f;

            npc.AddComponent<CharacterAnimationController>();
            npc.AddComponent<NPCAmbientTalker>();
            var controller = npc.AddComponent<NPCController>();
            var so = new SerializedObject(controller);
            SetString(so, "npcId", npcId);
            SetString(so, "displayName", displayName);
            SetString(so, "role", role);
            SetString(so, "fallbackJa", fallbackJa);
            SetString(so, "fallbackReading", fallbackReading);
            SetString(so, "fallbackEn", fallbackEn);
            SetString(so, "fallbackRomaji", fallbackRomaji);
            so.ApplyModifiedProperties();

            AddNpcVisual(npc, visualPrefabPath, fallbackColor);
        }

        private static void AddNpcVisual(GameObject npc, string prefabPath, Color fallbackColor)
        {
            var animCtrl = npc.GetComponent<CharacterAnimationController>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                visual.name = "Visual";
                visual.transform.SetParent(npc.transform, false);
                visual.transform.localPosition = Vector3.zero;
                StripColliders(visual);
                animCtrl.SetAnimator(visual.GetComponentInChildren<Animator>(true));
            }
            else
            {
                CreateFallbackPerson(npc.transform, "FallbackNPC", fallbackColor);
            }
        }

        private static void CreateFallbackPerson(Transform parent, string name, Color bodyColor)
        {
            var bodyMat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit")) { color = bodyColor };
            var skinMat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit")) { color = new Color(0.9f, 0.72f, 0.58f) };

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = name + "_Body";
            body.transform.SetParent(parent, false);
            body.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            body.transform.localScale = new Vector3(0.55f, 0.85f, 0.55f);
            body.GetComponent<Renderer>().sharedMaterial = bodyMat;
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = name + "_Head";
            head.transform.SetParent(parent, false);
            head.transform.localPosition = new Vector3(0f, 1.82f, 0f);
            head.transform.localScale = Vector3.one * 0.32f;
            head.GetComponent<Renderer>().sharedMaterial = skinMat;
            UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
        }

        private static void StripColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void SetString(SerializedObject obj, string propertyName, string value)
        {
            var prop = obj.FindProperty(propertyName);
            if (prop != null) prop.stringValue = value;
        }

        public static void BuildStation()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Vector3 origin = new Vector3(800f, 0f, 0f);
            var root = new GameObject("StationDistrict_Zone");
            root.AddComponent<SceneZoneVisibility>();
            root.AddComponent<StandaloneZoneBootstrap>().Configure("station_entrance");
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

        /// <summary>
        /// Adds npc_sushi_staff (Aoki) and npc_sushi_chef (Ota) to the existing, hand-tuned
        /// 30_SushiRestaurant.unity — additive only (opens the scene, checks for existing NPCs by name,
        /// adds only what's missing, saves). Never calls BuildSushiRestaurant() again: that would
        /// regenerate the whole zone from scratch and blow away every hand-adjustment made since
        /// (serve anchors, plate sizes — see Docs/TEAM_TASKS.md TASK-G).
        /// </summary>
        public static void AddSushiStaff()
        {
            Scene scene = EditorSceneManager.OpenScene(SushiScene, OpenSceneMode.Single);
            GameObject root = GameObject.Find("SushiRestaurant_Zone");
            if (root == null)
            {
                Debug.LogError("[GameplayZoneSceneBuilder] SushiRestaurant_Zone not found — scene structure has changed, aborting AddSushiStaff.");
                return;
            }

            Vector3 origin = new Vector3(500f, 0f, 0f);
            bool changed = false;

            if (root.transform.Find("SushiStaffAoki") == null)
            {
                CreatePlaceholderNpc(root.transform, "SushiStaffAoki", "npc_sushi_staff", "Aoki", "Staff",
                    "Assets/NihongoLife/Prefabs/Characters/NL_Neighbor.prefab", new Color(0.85f, 0.55f, 0.25f),
                    origin + new Vector3(0f, 0.05f, -5f), Quaternion.Euler(0f, 180f, 0f),
                    "いらっしゃいませ。何名様ですか。", "いらっしゃいませ。なんめいさまですか。",
                    "Welcome! How many people in your party?", "Irasshaimase. Nanmeisama desu ka.");
                changed = true;
            }

            if (root.transform.Find("SushiChefOta") == null)
            {
                CreatePlaceholderNpc(root.transform, "SushiChefOta", "npc_sushi_chef", "Ota", "Chef",
                    "Assets/NihongoLife/Prefabs/Characters/NL_Guide.prefab", new Color(0.3f, 0.3f, 0.32f),
                    origin + new Vector3(0f, 0.05f, 5f), Quaternion.Euler(0f, 180f, 0f),
                    "新鮮なネタが揃っていますよ。", "しんせんなネタがそろっていますよ。",
                    "We've got fresh ingredients today.", "Shinsen na neta ga sorotte imasu yo.");
                changed = true;
            }

            if (!changed)
            {
                Debug.Log("[GameplayZoneSceneBuilder] SushiStaffAoki/SushiChefOta already present — nothing to add.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SushiScene);
            Debug.Log("[GameplayZoneSceneBuilder] Added Aoki/Ota to 30_SushiRestaurant.unity.");
        }

        /// <summary>
        /// Bakes the player's own bedroom directly into 45_HomeBedroom.unity (floor/walls/bed/desk/rug,
        /// same layout HomeBedroomRuntime used to build at runtime every Play) plus a city portal, so the
        /// room exists in the finished scene instead of appearing only after pressing Play. Additive and
        /// check-first: opens the existing scene (created by the project owner directly in the Editor,
        /// already has HomeBedroom_YourRoom + Spawn_home_bedroom), only adds what's missing.
        /// HomeBedroomRuntime.BuildRoom() was trimmed to stop re-generating this geometry every Start()
        /// now that it is baked — see HomeBedroomRuntime.cs. Its BuildUi()/Update() (live energy/rest/
        /// knowledge/yen status panel) stays runtime-built, same as the rest of the HUD.
        /// </summary>
        public static void BuildHomeBedroom()
        {
            Scene scene = EditorSceneManager.OpenScene(HomeBedroomScene, OpenSceneMode.Single);
            GameObject root = GameObject.Find("HomeBedroom_YourRoom");
            if (root == null)
            {
                Debug.LogError("[GameplayZoneSceneBuilder] HomeBedroom_YourRoom not found in 45_HomeBedroom.unity — aborting BuildHomeBedroom.");
                return;
            }

            bool changed = false;

            if (root.transform.Find("Floor") == null)
            {
                BakeHomeBedroomGeometry(root.transform);
                changed = true;
            }

            if (root.GetComponent<ZoneEntrancePan>() == null)
            {
                root.AddComponent<ZoneEntrancePan>();
                changed = true;
            }

            if (!changed)
            {
                Debug.Log("[GameplayZoneSceneBuilder] HomeBedroom already fully baked — nothing to add.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, HomeBedroomScene);
            Debug.Log("[GameplayZoneSceneBuilder] Updated 45_HomeBedroom.unity.");
        }

        private static void BakeHomeBedroomGeometry(Transform root)
        {

            var wall = new Material(Shader.Find("Universal Render Pipeline/Simple Lit")) { color = new Color(0.82f, 0.88f, 0.9f) };
            var floor = new Material(Shader.Find("Universal Render Pipeline/Simple Lit")) { color = new Color(0.16f, 0.2f, 0.25f) };
            var wood = new Material(Shader.Find("Universal Render Pipeline/Simple Lit")) { color = new Color(0.42f, 0.22f, 0.12f) };
            var accent = new Material(Shader.Find("Universal Render Pipeline/Simple Lit")) { color = new Color(0.24f, 0.62f, 0.58f) };

            BedroomBlock(root.transform, "Floor", new Vector3(0f, -0.15f, 0f), new Vector3(12f, 0.3f, 9f), floor);
            BedroomBlock(root.transform, "BackWall", new Vector3(0f, 2.5f, 4.35f), new Vector3(12f, 5f, 0.3f), wall);
            BedroomBlock(root.transform, "LeftWall", new Vector3(-5.85f, 2.5f, 0f), new Vector3(0.3f, 5f, 9f), wall);
            BedroomBlock(root.transform, "RightWall", new Vector3(5.85f, 2.5f, 0f), new Vector3(0.3f, 5f, 9f), wall);
            BedroomBlock(root.transform, "BedFrame", new Vector3(-2.8f, 0.45f, 1.4f), new Vector3(4.2f, 0.55f, 2.1f), wood);
            BedroomBlock(root.transform, "Mattress", new Vector3(-2.8f, 0.82f, 1.4f), new Vector3(3.9f, 0.25f, 1.9f), accent);
            BedroomBlock(root.transform, "BedHeadboard", new Vector3(-2.8f, 1.5f, 2.25f), new Vector3(4.2f, 1.4f, 0.22f), wood);
            BedroomBlock(root.transform, "Desk", new Vector3(2.4f, 0.9f, 2.6f), new Vector3(2.3f, 0.18f, 1f), wood);
            BedroomBlock(root.transform, "DeskLeg_A", new Vector3(1.55f, 0.4f, 2.6f), new Vector3(0.16f, 0.8f, 0.16f), wood);
            BedroomBlock(root.transform, "DeskLeg_B", new Vector3(3.25f, 0.4f, 2.6f), new Vector3(0.16f, 0.8f, 0.16f), wood);
            BedroomBlock(root.transform, "WindowGlow", new Vector3(2.3f, 2.7f, 4.15f), new Vector3(3.2f, 1.8f, 0.08f), accent);
            BedroomBlock(root.transform, "Rug", new Vector3(1f, 0.03f, -1.5f), new Vector3(4.4f, 0.05f, 2.6f), accent);

            CreateDecorationSlot(root.transform, "DecorationSlot_Wall", new Vector3(0f, 2.35f, 4.12f));
            CreateDecorationSlot(root.transform, "DecorationSlot_Desk", new Vector3(2.4f, 1.08f, 2.6f));
            CreateDecorationSlot(root.transform, "DecorationSlot_Floor", new Vector3(1.2f, 0.08f, -1.5f));

            var restPoint = new GameObject("BedRestPoint");
            restPoint.transform.SetParent(root.transform, false);
            restPoint.transform.SetPositionAndRotation(new Vector3(-2.8f, 1.05f, 0.85f), Quaternion.Euler(0f, 180f, 0f));
            var restCollider = restPoint.AddComponent<BoxCollider>();
            restCollider.isTrigger = true;
            restCollider.size = new Vector3(2.4f, 1.5f, 1.5f);
            restPoint.AddComponent<BedroomRestInteractable>();

            CreateExitPortal(root.transform, "ExitToCity", HomeBedroomScene, "city_home_return", "街へ戻る / Trở lại thành phố", new Vector3(0f, 1.1f, -4f));
            CreatePreviewCamera(root.transform, Vector3.zero, "BedroomSceneCamera", new Vector3(0f, 3.2f, -7f), new Vector3(0f, 1.3f, 1f));
            CreateLighting(root.transform, new Vector3(0f, 4f, 1f));
        }

        private static void CreateDecorationSlot(Transform parent, string name, Vector3 position)
        {
            var slot = new GameObject(name);
            slot.transform.SetParent(parent, false);
            slot.transform.localPosition = position;
            slot.SetActive(false);
        }

        private static GameObject BedroomBlock(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
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

        public static void AddCityPortals()
        {
            Scene scene = EditorSceneManager.OpenScene(CityScene, OpenSceneMode.Single);
            GameObject existing = GameObject.Find("AdditiveZonePortals");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            var root = new GameObject("AdditiveZonePortals");

            CreateCityPortal(root.transform, "StationPortal", StationScene, "station_entrance", "駅前 / Khu nhà ga", new Vector3(9f, 1f, -4f), new Color(0.2f, 0.52f, 0.72f));
            CreateSpawn(root.transform, "city_station_return", new Vector3(9f, 0.1f, -6.2f), Quaternion.Euler(0f, 180f, 0f));
            CreateCityPortal(root.transform, "SushiPortal", SushiScene, "sushi_entrance", "すし店 / Nhà hàng sushi", new Vector3(-9f, 1f, -4f), new Color(0.76f, 0.2f, 0.18f));
            CreateSpawn(root.transform, "city_sushi_return", new Vector3(-9f, 0.1f, -6.2f), Quaternion.Euler(0f, 180f, 0f));
            CreateCityPortal(root.transform, "SchoolPortal", SchoolScene, "school_entrance", "学院 / Trường học", new Vector3(18f, 1f, -4f), new Color(0.2f, 0.55f, 0.32f));
            CreateSpawn(root.transform, "city_school_return", new Vector3(18f, 0.1f, -6.2f), Quaternion.Euler(0f, 180f, 0f));
            CreateCityPortal(root.transform, "HomeBedroomPortal", HomeBedroomScene, "home_bedroom", "自室 / Phòng riêng", new Vector3(-18f, 1f, -4f), new Color(0.55f, 0.4f, 0.75f));
            CreateSpawn(root.transform, "city_home_return", new Vector3(-18f, 0.1f, -6.2f), Quaternion.Euler(0f, 180f, 0f));

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
            AddOrEnable(scenes, SchoolScene);
            AddOrEnable(scenes, HomeBedroomScene);
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
