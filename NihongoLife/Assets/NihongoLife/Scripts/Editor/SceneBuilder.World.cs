using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NihongoLife.Audio;
using NihongoLife.Cameras;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Learning;
using NihongoLife.NPC;
using NihongoLife.Player;
using NihongoLife.Scenario;
using NihongoLife.Scoring;
using NihongoLife.UI;
using NihongoLife.World;

namespace NihongoLife.Editor
{
    /// <summary>Partial of SceneBuilder: menu preview, store, and neighborhood authoring. The public entry point stays SceneBuilder.cs.</summary>
    public static partial class SceneBuilder
    {
        private static void CreateMainMenuTownPreview()
        {
            var previewRoot = new GameObject("MenuTownPreview_90_TestSandbox");
            VisualEnvironmentBuilder.GenerateVisualEnvironment(previewRoot);
            CreateMenuPreviewGround(previewRoot.transform);
            // ToneDownMenuPreviewMaterials(previewRoot); // Removed to keep vibrant colors

            var target = new GameObject("MenuCameraTarget");
            target.transform.position = new Vector3(0f, 1.2f, -10f);

            var cameraGo = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>().OrNull() ?? cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 38f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 240f;

            if (cameraGo.GetComponent<AudioListener>() == null)
            {
                cameraGo.AddComponent<AudioListener>();
            }

            var orbit = cameraGo.GetComponent<MenuCameraOrbit>().OrNull() ?? cameraGo.AddComponent<MenuCameraOrbit>();
            orbit.Configure(44f, 15.5f, 2.15f, 205f, 3.1f);
            orbit.SetTarget(target.transform);
        }

        private static void CreateMenuPreviewGround(Transform parent)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "MenuPreviewGround";
            ground.transform.SetParent(parent);
            ground.transform.position = new Vector3(0f, -0.62f, -10f);
            ground.transform.localScale = new Vector3(96f, 0.18f, 72f);
            ground.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMat("NL_MenuPreviewGround_Mat", new Color(0.18f, 0.2f, 0.18f, 1f));
            UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
        }

        private static void ToneDownMenuPreviewMaterials(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null) continue;

                    Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
                    bool tooCyan = color.g > 0.7f && color.b > 0.7f && color.r < 0.25f;
                    bool tooBright = color.maxColorComponent > 0.95f;
                    if (!tooCyan && !tooBright) continue;

                    var toned = new Material(Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard"));
                    toned.name = material.name + "_MenuToned";
                    toned.color = new Color(0.16f, 0.2f, 0.22f, color.a);
                    materials[i] = toned;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static void CreateAppRootForDirectPlay()
        {
            var appRootGo = new GameObject("AppRoot");
            var appRoot = appRootGo.AddComponent<AppRoot>();
            var audioService = appRootGo.AddComponent<AudioService>();
            var sceneFlow = appRootGo.AddComponent<SceneFlowController>();
            var settings = appRootGo.AddComponent<GameSettingsService>();
            var control = appRootGo.AddComponent<GameControlService>();
            appRootGo.AddComponent<GeminiConversationService>();

            var so = new SerializedObject(appRoot);
            SetRef(so, "audioService", audioService);
            SetRef(so, "sceneFlowController", sceneFlow);
            SetRef(so, "settingsService", settings);
            SetRef(so, "controlService", control);
            so.ApplyModifiedProperties();
        }

        private static GameObject CreatePlayer()
        {
            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(0f, 0.05f, -13.5f);

            var cc = playerGo.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 1f, 0f);
            cc.height = 2f;
            cc.radius = 0.35f;
            cc.skinWidth = 0.05f;
            cc.stepOffset = 0.35f;

            var playerCtrl = playerGo.AddComponent<PlayerController>();
            var detector = playerGo.AddComponent<InteractionDetector>();
            playerGo.AddComponent<PlayerInventory>();
            playerGo.AddComponent<PlayerStatus>();
            playerGo.AddComponent<WorldBoundsGuard>();
            var animCtrl = playerGo.AddComponent<CharacterAnimationController>();

            var visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Characters/NL_Player.prefab");
            if (visualPrefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);
                visual.name = "Visual";
                visual.transform.SetParent(playerGo.transform, false);
                visual.transform.localPosition = Vector3.zero;
                StripColliders(visual);
                animCtrl.SetAnimator(visual.GetComponentInChildren<Animator>(true));
            }
            else
            {
                CreateFallbackPerson(playerGo.transform, "FallbackPlayer", new Color(0.2f, 0.45f, 0.95f));
            }

            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(playerGo.transform, false);
            groundCheck.transform.localPosition = new Vector3(0f, 0.12f, 0f);

            var playerSo = new SerializedObject(playerCtrl);
            SetRef(playerSo, "groundCheck", groundCheck.transform);
            SetFloat(playerSo, "groundDistance", 0.28f);
            SetInt(playerSo, "groundMask", 1);
            playerSo.ApplyModifiedProperties();

            var detectorSo = new SerializedObject(detector);
            SetFloat(detectorSo, "detectionRadius", 4.2f);
            SetInt(detectorSo, "interactableLayers", 1 << InteractableLayer);
            detectorSo.ApplyModifiedProperties();

            return playerGo;
        }

        private static void CreateCamera(Transform player)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 58f;
            cam.nearClipPlane = 0.03f;
            camGo.AddComponent<AudioListener>();
            var controller = camGo.AddComponent<ThirdPersonCameraController>();
            controller.SetTarget(player);
            controller.SetOrbit(0f, 16f, 7.2f);
        }

        private static void CreateManagers()
        {
            var managerGo = new GameObject("ScenarioManager");
            managerGo.AddComponent<ScenarioManager>();
            managerGo.AddComponent<ScoringManager>();
            managerGo.AddComponent<DialogueManager>();
            managerGo.AddComponent<LearningMasteryManager>();
            managerGo.AddComponent<SpeechPracticeController>();
            managerGo.AddComponent<TownAmbientAudio>();
            managerGo.AddComponent<SupabaseVoiceSyncService>();
            managerGo.AddComponent<GeminiConversationService>();
            managerGo.AddComponent<RuntimeCollisionRepair>();
        }

        private static void CreateEndlessCityVisuals(Transform environment)
        {
            const float tileLength = 100f;
            Transform[] tiles = new Transform[3];
            for (int i = 0; i < tiles.Length; i++)
            {
                var tile = new GameObject($"CityTile_{i - 1}");
                tile.transform.SetParent(environment);
                tile.transform.position = Vector3.zero;
                VisualEnvironmentBuilder.GenerateVisualEnvironment(tile);
                tile.transform.position = new Vector3(0f, 0f, (i - 1) * tileLength);
                tiles[i] = tile.transform;
            }

            var looper = environment.gameObject.AddComponent<EndlessCityLooper>();
            var so = new SerializedObject(looper);
            SetFloat(so, "tileLength", tileLength);
            var tileProp = so.FindProperty("tileRoots");
            tileProp.arraySize = tiles.Length;
            for (int i = 0; i < tiles.Length; i++)
            {
                tileProp.GetArrayElementAtIndex(i).objectReferenceValue = tiles[i];
            }
            so.ApplyModifiedProperties();
        }

        private static void CreateWorldSafety(Transform root)
        {
            var safety = new GameObject("WorldFallSafety");
            safety.transform.SetParent(root);
            CreateInvisibleWall(safety.transform, "WestWall", new Vector3(-66f, 2.4f, -6f), new Vector3(1f, 4.8f, 104f));
            CreateInvisibleWall(safety.transform, "EastWall", new Vector3(66f, 2.4f, -6f), new Vector3(1f, 4.8f, 104f));
        }

        private static void CreateInvisibleWall(Transform root, string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(root);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            DestroyRenderer(wall);
        }

        private static void BuildStoreGameplay(Transform root)
        {
            BuildNeighborhoodGameplay(root);

            var door = new GameObject("StoreDoor");
            door.layer = InteractableLayer;
            door.transform.SetParent(root);
            door.transform.position = new Vector3(0f, 1.15f, 0.65f);
            var trigger = door.AddComponent<BoxCollider>();
            trigger.size = new Vector3(3.4f, 2.5f, 1.9f);
            trigger.isTrigger = true;

            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "DoorBlocker";
            blocker.transform.SetParent(door.transform, false);
            blocker.transform.localPosition = new Vector3(0f, 0f, 0.25f);
            blocker.transform.localScale = new Vector3(2.2f, 2.2f, 0.12f);
            DestroyRenderer(blocker);

            var doorVisual = new GameObject("AutomaticSlidingGlassDoor");
            doorVisual.transform.SetParent(door.transform, false);
            doorVisual.transform.localPosition = Vector3.zero;

            var frameMat = CreateRuntimeMat("NL_Door_Frame_Mat", new Color(0.08f, 0.1f, 0.12f, 1f));
            var glassMat = CreateRuntimeMat("NL_Door_Glass_Mat", new Color(0.45f, 0.78f, 0.95f, 0.52f));
            var handleMat = CreateRuntimeMat("NL_Door_Handle_Mat", new Color(0.86f, 0.88f, 0.84f, 1f));

            CreateDoorPiece(doorVisual.transform, "TopRail", new Vector3(0f, 1.18f, -0.02f), new Vector3(3.75f, 0.13f, 0.16f), frameMat);
            CreateDoorPiece(doorVisual.transform, "BottomRail", new Vector3(0f, -1.05f, -0.02f), new Vector3(3.75f, 0.1f, 0.14f), frameMat);
            CreateDoorPiece(doorVisual.transform, "LeftFrame", new Vector3(-1.9f, 0.02f, -0.02f), new Vector3(0.12f, 2.35f, 0.16f), frameMat);
            CreateDoorPiece(doorVisual.transform, "RightFrame", new Vector3(1.9f, 0.02f, -0.02f), new Vector3(0.12f, 2.35f, 0.16f), frameMat);

            var leftPanel = CreateDoorPiece(doorVisual.transform, "GlassPanel_Left", new Vector3(-0.48f, 0.02f, -0.04f), new Vector3(0.92f, 2.05f, 0.055f), glassMat);
            var rightPanel = CreateDoorPiece(doorVisual.transform, "GlassPanel_Right", new Vector3(0.48f, 0.02f, -0.07f), new Vector3(0.92f, 2.05f, 0.055f), glassMat);
            CreateDoorPiece(leftPanel.transform, "Handle_Left", new Vector3(0.34f, 0f, -0.08f), new Vector3(0.05f, 0.55f, 0.05f), handleMat);
            CreateDoorPiece(rightPanel.transform, "Handle_Right", new Vector3(-0.34f, 0f, -0.08f), new Vector3(0.05f, 0.55f, 0.05f), handleMat);

            var doorSo = new SerializedObject(door.AddComponent<DoorInteractable>());
            SetString(doorSo, "areaId", "store_entrance");
            SetRef(doorSo, "doorVisual", doorVisual.transform);
            SetRef(doorSo, "leftDoorPanel", leftPanel.transform);
            SetRef(doorSo, "rightDoorPanel", rightPanel.transform);
            SetRef(doorSo, "blockingCollider", blocker.GetComponent<Collider>());
            doorSo.ApplyModifiedProperties();

            CreateStoreCameraZone(root);

            AddStoreSign(root, new Vector3(0f, 3.15f, 1.05f));
            CreateShelf(root, "Shelf_Food", new Vector3(-3.2f, 1.1f, 4.1f));
            CreateShelf(root, "Shelf_Drinks", new Vector3(3.2f, 1.1f, 4.1f));
            CreateCounter(root, new Vector3(0f, 0.55f, 8.1f));
            CreateItem(root, "Onigiri", "onigiri", "おにぎり", "Cơm nắm", "おにぎりを取る", "Lấy cơm nắm", 497, true, true, new Vector3(-4.55f, 1.82f, 5.55f), "Assets/ThirdParty/Kenney/kenney_food-kit/Models/FBX format/rice-ball.fbx");
            CreateItem(root, "Water", "water", "水", "Nước", "水を調べる", "Kiểm tra nước", 120, false, false, new Vector3(4.55f, 1.82f, 5.45f), "Assets/ThirdParty/Kenney/kenney_food-kit/Models/FBX format/soda-bottle.fbx");
            CreateItem(root, "Tea", "tea", "お茶", "Trà xanh", "お茶を調べる", "Kiểm tra trà xanh", 150, false, false, new Vector3(4.55f, 1.82f, 6.35f), "Assets/ThirdParty/Kenney/kenney_food-kit/Models/FBX format/carton-small.fbx");
            CreateCashier(root);
        }

        private static GameObject CreateDoorPiece(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = localScale;
            piece.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(piece.GetComponent<Collider>());
            return piece;
        }

        private static void CreateStoreCameraZone(Transform root)
        {
            var zone = new GameObject("StoreInteriorCameraZone");
            zone.transform.SetParent(root);
            zone.transform.position = new Vector3(0f, 1.1f, 5.2f);
            var collider = zone.AddComponent<BoxCollider>();
            collider.size = new Vector3(9f, 2.8f, 9.5f);
            collider.isTrigger = true;
            zone.AddComponent<StoreCameraZone>();
        }

        private static void BuildNeighborhoodGameplay(Transform root)
        {
            CreateGuideNPC(root);
            CreateNeighborNPC(root, "Neighbor_1", "npc_neighbor_1", "Tanaka", "scenario.house1.greeting", new Vector3(-22f, 0.05f, -0.5f));
            CreateNeighborNPC(root, "Neighbor_2", "npc_neighbor_2", "Suzuki", "scenario.house2.lostcat", new Vector3(-11f, 0.05f, -0.5f));
            CreateNeighborNPC(root, "Neighbor_3", "npc_neighbor_3", "Sato", "scenario.house3.garbage", new Vector3(12f, 0.05f, -0.5f));
        }

        private static void CreateGuideNPC(Transform root)
        {
            var guide = CreateNpcShell(root, "GuideNPC", "npc_guide", "Lilly", "Language Guide", new Vector3(-2.8f, 0.05f, -13.2f), Quaternion.Euler(0f, 25f, 0f));
            AddNpcVisual(guide, "Assets/NihongoLife/Prefabs/Characters/NL_Guide.prefab", new Color(0.9f, 0.64f, 0.28f));

            var so = new SerializedObject(guide.GetComponent<NPCController>());
            SetString(so, "promptJa", "練習する");
            SetString(so, "promptEn", "Practice phrases");
            so.ApplyModifiedProperties();
        }

        private static void CreateNeighborNPC(Transform root, string goName, string npcId, string displayName, string scenarioAreaId, Vector3 position)
        {
            var npc = CreateNpcShell(root, goName, npcId, displayName, "Neighbor", position, Quaternion.Euler(0f, 180f, 0f));
            AddNpcVisual(npc, "Assets/NihongoLife/Prefabs/Characters/NL_Neighbor.prefab", new Color(0.2f, 0.7f, 0.3f));
            if (npcId == "npc_neighbor_1")
            {
                AddPatrolRoute(npc, root, goName + "_Route", position + new Vector3(-2.5f, 0f, -1.4f), position + new Vector3(2.5f, 0f, -1.4f));
            }

            var so = new SerializedObject(npc.GetComponent<NPCController>());
            SetString(so, "npcId", npcId);
            SetString(so, "displayName", displayName);
            SetString(so, "role", "Neighbor");
            SetString(so, "promptJa", "話す");
            SetString(so, "promptEn", "Talk");
            SetString(so, "scenarioAreaIdOnInteract", scenarioAreaId);
            so.ApplyModifiedProperties();
        }

        private static GameObject CreateNpcShell(Transform root, string goName, string npcId, string displayName, string role, Vector3 position, Quaternion rotation)
        {
            var npc = new GameObject(goName);
            npc.layer = InteractableLayer;
            npc.transform.SetParent(root);
            npc.transform.position = position;
            npc.transform.rotation = rotation;

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
            so.ApplyModifiedProperties();
            return npc;
        }

        private static void AddPatrolRoute(GameObject npc, Transform root, string routeName, params Vector3[] points)
        {
            var route = new GameObject(routeName);
            route.transform.SetParent(root);
            Transform[] waypoints = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                var waypoint = new GameObject($"Waypoint_{i + 1}");
                waypoint.transform.SetParent(route.transform);
                waypoint.transform.position = points[i];
                waypoints[i] = waypoint.transform;
            }

            var patrol = npc.AddComponent<NPCStreetPatrol>();
            var so = new SerializedObject(patrol);
            var prop = so.FindProperty("waypoints");
            prop.arraySize = waypoints.Length;
            for (int i = 0; i < waypoints.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
            }
            SetFloat(so, "walkSpeed", 1.05f);
            SetFloat(so, "waitSeconds", 1.4f);
            so.ApplyModifiedProperties();
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

        private static void CreateShelf(Transform root, string name, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root);
            go.transform.position = position;
            go.GetComponent<BoxCollider>().size = new Vector3(2.3f, 2.2f, 0.9f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Furniture/shelf.prefab");
            if (prefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                visual.transform.SetParent(go.transform, false);
                visual.transform.localPosition = new Vector3(0f, -1.1f, 0f);
                StripColliders(visual);
                DestroyRenderer(go);
            }
        }

        private static void CreateCounter(Transform root, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "CashierCounter";
            go.transform.SetParent(root);
            go.transform.position = position;
            go.GetComponent<BoxCollider>().size = new Vector3(3.4f, 1.1f, 1.1f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Furniture/counter.prefab");
            if (prefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                visual.transform.SetParent(go.transform, false);
                visual.transform.localPosition = new Vector3(0f, -0.55f, 0f);
                StripColliders(visual);
                DestroyRenderer(go);
            }
        }

        private static void CreateItem(Transform root, string name, string itemId, string ja, string vi, string promptJa, string promptEn, int price, bool destroyOnInteract, bool addToInventory, Vector3 position, string prefabPath)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            item.name = name;
            item.layer = InteractableLayer;
            item.transform.SetParent(root);
            item.transform.position = position;
            item.transform.localScale = Vector3.one * 0.55f;
            item.GetComponent<SphereCollider>().isTrigger = true;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                visual.transform.SetParent(item.transform, false);
                visual.transform.localPosition = Vector3.zero;
                StripColliders(visual);
                DestroyRenderer(item);
            }

            var so = new SerializedObject(item.AddComponent<InteractiveItem>());
            SetString(so, "itemId", itemId);
            SetString(so, "displayNameJa", ja);
            SetString(so, "displayNameEn", vi);
            SetString(so, "promptJa", promptJa);
            SetString(so, "promptEn", promptEn);
            SetInt(so, "priceYen", price);
            SetBool(so, "addToInventory", addToInventory);
            SetBool(so, "destroyOnInteract", destroyOnInteract);
            so.ApplyModifiedProperties();
        }

        private static void CreateCashier(Transform root)
        {
            var npc = new GameObject("CashierNPC");
            npc.layer = InteractableLayer;
            npc.transform.SetParent(root);
            npc.transform.position = new Vector3(0f, 0.05f, 9.55f);
            npc.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var collider = npc.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.95f, 0f);
            collider.size = new Vector3(0.9f, 1.9f, 0.9f);
            collider.isTrigger = true;

            var animCtrl = npc.AddComponent<CharacterAnimationController>();
            npc.AddComponent<NPCAmbientTalker>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Characters/NL_Cashier.prefab");
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
                CreateFallbackPerson(npc.transform, "FallbackCashier", new Color(0.05f, 0.42f, 0.48f));
            }

            var so = new SerializedObject(npc.AddComponent<NPCController>());
            SetString(so, "npcId", "npc_cashier");
            SetString(so, "displayName", "Ito");
            SetString(so, "role", "Cashier");
            SetString(so, "promptJa", "会計する");
            SetString(so, "promptEn", "Thanh toán");
            SetString(so, "scenarioAreaIdOnInteract", "cashier");
            so.ApplyModifiedProperties();
        }
    }
}
