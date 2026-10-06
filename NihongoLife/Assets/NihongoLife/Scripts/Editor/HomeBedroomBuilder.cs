#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using NihongoLife.Cameras;
using NihongoLife.Core;
using NihongoLife.Home;
using NihongoLife.Interaction;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Bakes the player's room (45_HomeBedroom) as a small Japanese 1K apartment: wood floor, four walls
    /// and ceiling, window with curtains, entrance (genkan) door back to the city, kitchenette, bed,
    /// study desk, bookshelf, closet, low table and TV — all real furniture from the Kenney furniture
    /// kit with box colliders — plus the room interactables (bed, desk, sink, fridge, light switch,
    /// door). Run headless: Unity -batchmode -executeMethod NihongoLife.EditorTools.HomeBedroomBuilder.Build
    /// (no MenuItem, per repository rules). Idempotent: the previous "Room" container and the legacy
    /// primitive blocks are deleted before rebuilding, so the room is never stacked twice.
    /// </summary>
    public static class HomeBedroomBuilder
    {
        public const string ScenePath = "Assets/NihongoLife/Scenes/45_HomeBedroom.unity";
        private const string Kit = "Assets/ThirdParty/Kenney/kenney_furniture-kit/Models/FBX format/";
        private const string FontPath = "Assets/NihongoLife/Fonts/NotoSansJP SDF.asset";
        private const string ControllerPath = "Assets/NihongoLife/Animations/NL_Humanoid.controller";
        private const int InteractableLayer = 6;

        // Interior: x ∈ [-HalfW, HalfW], z ∈ [-HalfD, HalfD], floor at y = 0.
        public const float HalfW = 3.6f;
        public const float HalfD = 3.0f;
        public const float Height = 2.7f;
        private const float Wall = 0.2f;

        private static readonly string[] LegacyChildren =
        {
            "Floor", "BackWall", "LeftWall", "RightWall", "BedFrame", "Mattress", "BedHeadboard", "Desk", "DeskLeg_A",
            "DeskLeg_B", "WindowGlow", "Rug", "DecorationSlot_Wall", "DecorationSlot_Desk", "DecorationSlot_Floor",
            "BedRestPoint", "ZoneLighting", "BedroomHUD", "Room"
        };

        private static Material _wallMat, _floorMat, _ceilingMat, _trimMat, _woodMat, _clothMat, _glassMat, _skyMat, _paperMat, _tileMat, _metalMat, _shadeMat;

        public static void Build()
        {
            try
            {
                EnsureAnimatorStates();
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var root = GameObject.Find("HomeBedroom_YourRoom");
                if (root == null) throw new InvalidOperationException("HomeBedroom_YourRoom is missing from " + ScenePath);

                foreach (string name in LegacyChildren)
                {
                    Transform old;
                    while ((old = root.transform.Find(name)) != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                }

                CreateMaterials();
                var room = new GameObject("Room").transform;
                room.SetParent(root.transform, false);

                BuildShell(room);
                var furniture = BuildFurniture(room);
                var lights = BuildLights(room, furniture);
                BuildInteractables(room, root.transform, furniture, lights);
                ConfigureSceneObjects(root);

                Validate(root);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Scene save failed.");
                AssetDatabase.SaveAssets();
                Debug.Log("[HomeBedroomBuilder] 45_HomeBedroom rebuilt and saved.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        // ─────────── Shell ───────────

        private static void BuildShell(Transform room)
        {
            var shell = Group(room, "Shell");
            float w = HalfW * 2f, d = HalfD * 2f;
            Block(shell, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(w + 2 * Wall, 0.2f, d + 2 * Wall), _floorMat, true);
            Block(shell, "Ceiling", new Vector3(0f, Height + 0.1f, 0f), new Vector3(w + 2 * Wall, 0.2f, d + 2 * Wall), _ceilingMat, true);
            Block(shell, "Genkan_Tile", new Vector3(-2.4f, 0.004f, -2.55f), new Vector3(1.5f, 0.008f, 0.9f), _tileMat, false);

            // Side walls.
            Block(shell, "Wall_Left", new Vector3(-HalfW - Wall / 2f, Height / 2f, 0f), new Vector3(Wall, Height, d + 2 * Wall), _wallMat, true);
            Block(shell, "Wall_Right", new Vector3(HalfW + Wall / 2f, Height / 2f, 0f), new Vector3(Wall, Height, d + 2 * Wall), _wallMat, true);

            // Back wall with a window opening x ∈ [0.6, 2.6], y ∈ [0.95, 2.15].
            WallWithOpening(shell, "Wall_Back", HalfD + Wall / 2f, 0.6f, 2.6f, 0.95f, 2.15f);
            // Front wall with the entrance door opening x ∈ [-2.95, -1.85], y ∈ [0, 2.1].
            WallWithOpening(shell, "Wall_Front", -HalfD - Wall / 2f, -2.95f, -1.85f, 0f, 2.1f);

            // Baseboards.
            Block(shell, "Baseboard_Left", new Vector3(-HalfW + 0.01f, 0.05f, 0f), new Vector3(0.02f, 0.1f, d), _trimMat, false);
            Block(shell, "Baseboard_Right", new Vector3(HalfW - 0.01f, 0.05f, 0f), new Vector3(0.02f, 0.1f, d), _trimMat, false);
            Block(shell, "Baseboard_Back", new Vector3(0f, 0.05f, HalfD - 0.01f), new Vector3(w, 0.1f, 0.02f), _trimMat, false);

            // Window: frame, mullion, glass and an evening sky behind it.
            var window = Group(shell, "Window");
            float wx = 1.6f, wy = 1.55f, wz = HalfD;
            Block(window, "Frame_Top", new Vector3(wx, 2.15f, wz), new Vector3(2.1f, 0.08f, 0.24f), _trimMat, false);
            Block(window, "Frame_Bottom", new Vector3(wx, 0.95f, wz - 0.04f), new Vector3(2.2f, 0.08f, 0.32f), _trimMat, false);
            Block(window, "Frame_Left", new Vector3(0.6f, wy, wz), new Vector3(0.08f, 1.28f, 0.24f), _trimMat, false);
            Block(window, "Frame_Right", new Vector3(2.6f, wy, wz), new Vector3(0.08f, 1.28f, 0.24f), _trimMat, false);
            Block(window, "Mullion", new Vector3(wx, wy, wz), new Vector3(0.05f, 1.2f, 0.06f), _trimMat, false);
            Block(window, "Glass", new Vector3(wx, wy, wz + 0.02f), new Vector3(2.0f, 1.2f, 0.01f), _glassMat, true);
            Block(window, "SkyView", new Vector3(wx, wy, wz + 0.9f), new Vector3(4.5f, 3f, 0.02f), _skyMat, false);
            Block(window, "Curtain_Left", new Vector3(0.38f, 1.5f, wz - 0.12f), new Vector3(0.42f, 1.55f, 0.06f), _clothMat, false);
            Block(window, "Curtain_Right", new Vector3(2.82f, 1.5f, wz - 0.12f), new Vector3(0.42f, 1.55f, 0.06f), _clothMat, false);
            Block(window, "CurtainRail", new Vector3(wx, 2.3f, wz - 0.12f), new Vector3(3.0f, 0.04f, 0.04f), _metalMat, false);

            // Entrance door (closed panel in the front-wall opening) with a knob.
            var door = Group(shell, "EntranceDoor");
            Block(door, "DoorFrame_Top", new Vector3(-2.4f, 2.14f, -HalfD), new Vector3(1.2f, 0.08f, 0.24f), _trimMat, false);
            Block(door, "DoorFrame_L", new Vector3(-2.96f, 1.05f, -HalfD), new Vector3(0.06f, 2.1f, 0.24f), _trimMat, false);
            Block(door, "DoorFrame_R", new Vector3(-1.84f, 1.05f, -HalfD), new Vector3(0.06f, 2.1f, 0.24f), _trimMat, false);
            Block(door, "DoorPanel", new Vector3(-2.4f, 1.05f, -HalfD - 0.02f), new Vector3(1.06f, 2.08f, 0.06f), _woodMat, true);
            var knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            knob.name = "DoorKnob";
            knob.transform.SetParent(door, false);
            knob.transform.localPosition = new Vector3(-2.0f, 1.0f, -HalfD + 0.04f);
            knob.transform.localScale = Vector3.one * 0.07f;
            UnityEngine.Object.DestroyImmediate(knob.GetComponent<Collider>());
            knob.GetComponent<Renderer>().sharedMaterial = _metalMat;
            Sign(door, "Genkan_Sign", "げんかん · Lối ra", new Vector3(-2.4f, 2.32f, -HalfD + 0.13f), 180f, 1.6f, 0.16f, new Color(0.25f, 0.2f, 0.16f));

            // Hiragana chart on the wall above the bed side — the room teaches too.
            Block(shell, "HiraganaPoster", new Vector3(-0.35f, 1.65f, HalfD - 0.015f), new Vector3(0.95f, 0.72f, 0.02f), _paperMat, false);
            Sign(shell, "HiraganaPoster_Text", "<b>ひらがな</b>\nあ い う え お\nか き く け こ\nさ し す せ そ\nた ち つ て と",
                new Vector3(-0.35f, 1.65f, HalfD - 0.03f), 0f, 0.9f, 0.075f, new Color(0.18f, 0.2f, 0.26f));
            // Calendar next to the desk.
            Block(shell, "Calendar", new Vector3(3.2f, 1.6f, HalfD - 0.015f), new Vector3(0.42f, 0.55f, 0.02f), _paperMat, false);
            Sign(shell, "Calendar_Text", "<b>8がつ</b>\n<size=70%>月 火 水 木 金</size>\n<color=#C8463D>あした 9:00 がっこう</color>",
                new Vector3(3.2f, 1.6f, HalfD - 0.03f), 0f, 0.4f, 0.05f, new Color(0.18f, 0.2f, 0.26f));
        }

        private static void WallWithOpening(Transform parent, string name, float z, float x0, float x1, float y0, float y1)
        {
            float left = -HalfW - Wall, right = HalfW + Wall;
            Block(parent, name + "_L", new Vector3((left + x0) / 2f, Height / 2f, z), new Vector3(x0 - left, Height, Wall), _wallMat, true);
            Block(parent, name + "_R", new Vector3((x1 + right) / 2f, Height / 2f, z), new Vector3(right - x1, Height, Wall), _wallMat, true);
            if (y0 > 0.01f) Block(parent, name + "_Below", new Vector3((x0 + x1) / 2f, y0 / 2f, z), new Vector3(x1 - x0, y0, Wall), _wallMat, true);
            Block(parent, name + "_Above", new Vector3((x0 + x1) / 2f, (y1 + Height) / 2f, z), new Vector3(x1 - x0, Height - y1, Wall), _wallMat, true);
        }

        // ─────────── Furniture ───────────

        private static Dictionary<string, GameObject> BuildFurniture(Transform room)
        {
            var f = Group(room, "Furniture");
            var map = new Dictionary<string, GameObject>();
            void Add(string key, GameObject go) { if (go != null) map[key] = go; }

            // Sleeping corner (back-left). Kenney fronts face −Z; yaw 0 keeps the headboard at +Z.
            Add("Bed", Furn(f, "bedSingle", "Bed", new Vector3(-2.5f, 0f, 1.92f), 0f, new Vector3(1.15f, 0.9f, 2.1f), true));
            Add("SideTable", Furn(f, "sideTableDrawers", "SideTable", new Vector3(-1.48f, 0f, 2.72f), 0f, new Vector3(0.55f, 0.55f, 0.45f), true));
            Add("BedLamp", OnTop(f, "lampRoundTable", "BedsideLamp", map["SideTable"], Vector2.zero, 0f, new Vector3(0.28f, 0.42f, 0.28f)));

            // Study corner under the window (back-right). Chair turned to face the desk.
            Add("Desk", Furn(f, "desk", "StudyDesk", new Vector3(1.6f, 0f, 2.6f), 0f, new Vector3(1.45f, 0.76f, 0.72f), true));
            Add("Chair", Furn(f, "chairDesk", "DeskChair", new Vector3(1.55f, 0f, 1.88f), 180f, new Vector3(0.55f, 0.98f, 0.55f), true));
            Add("Laptop", OnTop(f, "laptop", "Laptop", map["Desk"], new Vector2(-0.05f, -0.05f), 0f, new Vector3(0.4f, 0.28f, 0.3f)));
            Add("DeskLamp", OnTop(f, "lampSquareTable", "DeskLamp", map["Desk"], new Vector2(0.52f, 0.12f), 0f, new Vector3(0.2f, 0.45f, 0.2f)));
            Add("Books", OnTop(f, "books", "DeskBooks", map["Desk"], new Vector2(-0.52f, 0.12f), 0f, new Vector3(0.3f, 0.24f, 0.22f)));
            Add("Trash", Furn(f, "trashcan", "Trashcan", new Vector3(2.62f, 0f, 2.1f), 0f, new Vector3(0.3f, 0.38f, 0.3f), true));

            // Right wall: bookshelf and closet facing the room (front → −X, yaw 90).
            Add("Bookcase", Furn(f, "bookcaseOpen", "Bookshelf", new Vector3(HalfW - 0.24f, 0f, 1.05f), 90f, new Vector3(1.0f, 1.8f, 0.42f), true));
            Add("ShelfBooksA", OnShelf(f, "books", "ShelfBooks_A", map["Bookcase"], 0.52f, 0.1f));
            Add("ShelfBooksB", OnShelf(f, "books", "ShelfBooks_B", map["Bookcase"], 0.26f, -0.18f));
            Add("Plant", Furn(f, "pottedPlant", "Houseplant", new Vector3(HalfW - 0.32f, 0f, HalfD - 0.32f), 0f, new Vector3(0.48f, 1.05f, 0.48f), true));
            Add("Closet", Furn(f, "bookcaseClosedDoors", "Closet", new Vector3(HalfW - 0.3f, 0f, -0.75f), 90f, new Vector3(1.25f, 2.05f, 0.55f), true));

            // Kitchenette along the left wall facing the room (front → +X, yaw −90).
            Add("Fridge", Furn(f, "kitchenFridgeSmall", "Fridge", new Vector3(-HalfW + 0.32f, 0f, -1.5f), -90f, new Vector3(0.62f, 0.9f, 0.6f), true));
            Add("Microwave", OnTop(f, "kitchenMicrowave", "Microwave", map["Fridge"], Vector2.zero, -90f, new Vector3(0.5f, 0.3f, 0.36f)));
            Add("Sink", Furn(f, "kitchenSink", "KitchenSink", new Vector3(-HalfW + 0.32f, 0f, -0.66f), -90f, new Vector3(0.82f, 0.92f, 0.62f), true));
            Add("Stove", Furn(f, "kitchenStoveElectric", "KitchenStove", new Vector3(-HalfW + 0.32f, 0f, 0.18f), -90f, new Vector3(0.82f, 0.92f, 0.62f), true));
            Add("UpperCabinet", Furn(f, "kitchenCabinetUpper", "UpperCabinet", new Vector3(-HalfW + 0.2f, 1.55f, -0.24f), 90f, new Vector3(1.6f, 0.6f, 0.36f), false));
            Add("Kettle", OnTop(f, "kitchenCoffeeMachine", "Kettle", map["Stove"], new Vector2(0.05f, 0.12f), -90f, new Vector3(0.22f, 0.3f, 0.22f)));

            // Living area: rug, low table, TV.
            Add("Rug", Furn(f, "rugRectangle", "Rug", new Vector3(0.75f, 0f, -0.35f), 0f, new Vector3(2.6f, 0.03f, 1.8f), false));
            Add("LowTable", Furn(f, "tableCoffee", "LowTable", new Vector3(0.75f, 0.03f, -0.35f), 0f, new Vector3(1.15f, 0.4f, 0.72f), true));
            // Zabuton floor cushions around the low table.
            Block(f, "Zabuton_A", new Vector3(0.75f, 0.07f, 0.42f), new Vector3(0.55f, 0.08f, 0.55f), _clothMat, false);
            Block(f, "Zabuton_B", new Vector3(-0.22f, 0.07f, -0.35f), new Vector3(0.55f, 0.08f, 0.55f), _clothMat, false);
            Add("TVStand", Furn(f, "cabinetTelevision", "TVStand", new Vector3(0.75f, 0f, -HalfD + 0.26f), 180f, new Vector3(1.45f, 0.5f, 0.45f), true));
            Add("TV", OnTop(f, "televisionModern", "Television", map["TVStand"], Vector2.zero, 180f, new Vector3(1.15f, 0.72f, 0.14f)));

            // Entrance.
            Add("Doormat", Furn(f, "rugDoormat", "Doormat", new Vector3(-2.4f, 0.01f, -2.45f), 0f, new Vector3(1.0f, 0.02f, 0.6f), false));
            Add("CoatRack", Furn(f, "coatRackStanding", "CoatRack", new Vector3(-1.42f, 0f, -HalfD + 0.32f), 0f, new Vector3(0.5f, 1.7f, 0.5f), true));

            // Ceiling lamp (shade gets an emissive material so the switch visibly turns it off).
            Add("CeilingLamp", Furn(f, "lampSquareCeiling", "CeilingLamp", new Vector3(0.3f, Height - 0.38f, 0.1f), 0f, new Vector3(0.55f, 0.38f, 0.55f), false));
            foreach (var r in map["CeilingLamp"].GetComponentsInChildren<Renderer>())
                r.sharedMaterials = r.sharedMaterials.Select(_ => _shadeMat).ToArray();
            return map;
        }

        /// <summary>Instantiates a Kenney model, scales it uniformly to fit targetSize (model space,
        /// before yaw), rotates it, centres it on (x, z) and rests its bottom on y.</summary>
        private static GameObject Furn(Transform parent, string model, string name, Vector3 position, float yaw, Vector3 targetSize, bool collider)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + model + ".fbx");
            if (source == null) throw new InvalidOperationException("Missing furniture model " + model);
            var holder = new GameObject(name).transform;
            holder.SetParent(parent, false);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.transform.SetParent(holder, false);
            Bounds b = BoundsOf(instance);
            float scale = Mathf.Min(targetSize.x / b.size.x, targetSize.y / b.size.y, targetSize.z / b.size.z);
            instance.transform.localScale *= scale;
            b = BoundsOf(instance);
            instance.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);
            holder.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            foreach (var r in holder.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }
            if (collider)
            {
                var box = holder.gameObject.AddComponent<BoxCollider>();
                Bounds local = LocalBounds(holder);
                box.center = local.center;
                box.size = local.size;
            }
            return holder.gameObject;
        }

        private static GameObject OnTop(Transform parent, string model, string name, GameObject support, Vector2 offset, float yaw, Vector3 targetSize)
        {
            Bounds s = BoundsOf(support);
            var go = Furn(parent, model, name, new Vector3(s.center.x + offset.x, s.max.y + 0.002f, s.center.z + offset.y), yaw, targetSize, false);
            return go;
        }

        private static GameObject OnShelf(Transform parent, string model, string name, GameObject shelf, float heightFraction, float alongOffset)
        {
            Bounds s = BoundsOf(shelf);
            float y = s.min.y + s.size.y * heightFraction;
            // Probe down from just under the next board for the shelf surface.
            Vector3 probe = new Vector3(s.center.x, y + 0.25f, s.center.z + alongOffset);
            Physics.SyncTransforms();
            return Furn(parent, model, name, new Vector3(probe.x, y, probe.z), 90f, new Vector3(0.32f, 0.22f, 0.22f), false);
        }

        // ─────────── Lights ───────────

        private sealed class RoomLights
        {
            public Light Ceiling, Kitchen, Desk, Bedside, Moon;
        }

        private static RoomLights BuildLights(Transform room, Dictionary<string, GameObject> f)
        {
            var group = Group(room, "Lights");
            var lights = new RoomLights
            {
                Ceiling = PointLight(group, "CeilingLight", new Vector3(0.3f, Height - 0.5f, 0.1f), 9.5f, 2.1f, new Color(1f, 0.88f, 0.72f), LightShadows.Soft),
                Kitchen = PointLight(group, "KitchenLight", new Vector3(-HalfW + 0.8f, 1.2f, -0.4f), 2.8f, 0.7f, new Color(1f, 0.9f, 0.78f), LightShadows.None),
                Desk = PointLight(group, "DeskLampLight", BoundsOf(f["DeskLamp"]).center + Vector3.up * 0.12f, 2.2f, 0.75f, new Color(1f, 0.82f, 0.55f), LightShadows.None),
                Bedside = PointLight(group, "BedsideLampLight", BoundsOf(f["BedLamp"]).center + Vector3.up * 0.1f, 2.0f, 0.55f, new Color(1f, 0.76f, 0.5f), LightShadows.None),
                Moon = PointLight(group, "WindowFill", new Vector3(1.6f, 1.6f, HalfD - 0.5f), 4f, 0.45f, new Color(0.62f, 0.72f, 1f), LightShadows.None),
            };
            return lights;
        }

        private static Light PointLight(Transform parent, string name, Vector3 position, float range, float intensity, Color color, LightShadows shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = range;
            light.intensity = intensity;
            light.color = color;
            light.shadows = shadows;
            return light;
        }

        // ─────────── Interactables ───────────

        private static void BuildInteractables(Transform room, Transform root, Dictionary<string, GameObject> f, RoomLights lights)
        {
            var group = Group(room, "Interactables");

            // Bed: trigger along the open side; lie on the mattress, wake standing beside the bed.
            Bounds bed = BoundsOf(f["Bed"]);
            var bedPoint = Trigger(group, "BedRestPoint", new Vector3(bed.center.x + 0.2f, 0.6f, bed.center.z - 0.1f), new Vector3(1.6f, 1.2f, 2.3f));
            var lie = new GameObject("LieAnchor").transform;
            lie.SetParent(bedPoint.transform, false);
            lie.position = new Vector3(bed.center.x, bed.min.y + bed.size.y * 0.5f, bed.center.z + 0.15f);
            lie.rotation = Quaternion.Euler(0f, 180f, 0f);
            var stand = new GameObject("StandAnchor").transform;
            stand.SetParent(bedPoint.transform, false);
            stand.position = new Vector3(bed.max.x + 0.45f, 0.02f, bed.center.z - 0.4f);
            stand.rotation = Quaternion.Euler(0f, 90f, 0f);
            bedPoint.AddComponent<BedroomRestInteractable>().Configure(lie, stand);

            Bounds desk = BoundsOf(f["Desk"]);
            var deskPoint = Trigger(group, "StudyDeskPoint", new Vector3(desk.center.x, 0.8f, desk.min.z - 0.35f), new Vector3(1.6f, 1.6f, 1.3f));
            deskPoint.AddComponent<StudyDeskInteractable>();

            Bounds sink = BoundsOf(f["Sink"]);
            var sinkPoint = Trigger(group, "SinkPoint", new Vector3(sink.max.x + 0.35f, 0.9f, sink.center.z), new Vector3(0.9f, 1.6f, 0.8f));
            sinkPoint.AddComponent<HomeNeedsStation>().Configure(HomeNeedsStation.Kind.Sink);

            Bounds fridge = BoundsOf(f["Fridge"]);
            var fridgePoint = Trigger(group, "FridgePoint", new Vector3(fridge.max.x + 0.35f, 0.9f, fridge.center.z), new Vector3(0.9f, 1.6f, 0.75f));
            fridgePoint.AddComponent<HomeNeedsStation>().Configure(HomeNeedsStation.Kind.Fridge);

            // Light switch on the wall beside the door.
            var plate = Block(group, "LightSwitch_Plate", new Vector3(-1.62f, 1.2f, -HalfD + 0.012f), new Vector3(0.09f, 0.13f, 0.02f), _paperMat, false);
            var knob = Block(plate.transform, "LightSwitch_Toggle", Vector3.zero, Vector3.one, _trimMat, false);
            knob.transform.localPosition = new Vector3(0f, 0f, 0.8f);
            knob.transform.localScale = new Vector3(0.4f, 0.45f, 1f);
            var switchPoint = Trigger(group, "LightSwitchPoint", new Vector3(-1.62f, 1.1f, -HalfD + 0.45f), new Vector3(0.7f, 1.4f, 0.8f));
            var shades = f["CeilingLamp"].GetComponentsInChildren<Renderer>();
            switchPoint.AddComponent<RoomLightSwitch>().Configure(new[] { lights.Ceiling, lights.Kitchen }, shades, knob.transform);
        }

        private static GameObject Trigger(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.layer = InteractableLayer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            return go;
        }

        private static void ConfigureSceneObjects(GameObject root)
        {
            var runtime = root.GetComponent<HomeBedroomRuntime>();
            var so = new SerializedObject(runtime);
            so.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            so.ApplyModifiedPropertiesWithoutUndo();

            var spawn = UnityEngine.Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(s => s.Id == "home_bedroom");
            if (spawn == null) throw new InvalidOperationException("Spawn home_bedroom is missing.");
            spawn.transform.SetPositionAndRotation(new Vector3(-2.4f, 0.05f, -2.15f), Quaternion.identity);

            var exit = root.transform.Find("ExitToCity");
            if (exit == null) throw new InvalidOperationException("ExitToCity portal is missing.");
            exit.gameObject.layer = InteractableLayer;
            exit.position = new Vector3(-2.4f, 1.0f, -HalfD + 0.3f);
            var exitBox = exit.GetComponent<BoxCollider>();
            exitBox.size = new Vector3(1.1f, 2f, 0.6f);
            exitBox.center = Vector3.zero;
            var portal = new SerializedObject(exit.GetComponent<ScenePortal>());
            portal.FindProperty("promptJa").stringValue = "そとに でる";
            portal.FindProperty("promptEn").stringValue = "Ra phố (về Hibari-chō)";
            portal.ApplyModifiedPropertiesWithoutUndo();

            var pan = root.GetComponent<ZoneEntrancePan>();
            if (pan != null)
            {
                var p = new SerializedObject(pan);
                p.FindProperty("startYaw").floatValue = -38f;
                p.FindProperty("endYaw").floatValue = 22f;
                p.FindProperty("pitch").floatValue = 20f;
                p.FindProperty("distance").floatValue = 2.7f;
                p.FindProperty("panDuration").floatValue = 2.2f;
                p.ApplyModifiedPropertiesWithoutUndo();
            }

            var preview = root.transform.Find("BedroomSceneCamera");
            if (preview != null)
            {
                preview.position = new Vector3(-0.4f, 2.3f, -HalfD + 0.25f);
                preview.rotation = Quaternion.LookRotation(new Vector3(0.4f, 0.8f, 1.6f) - preview.position);
            }
        }

        private static void Validate(GameObject root)
        {
            string[] required = { "Room/Shell/Floor", "Room/Shell/Ceiling", "Room/Furniture/Bed", "Room/Furniture/StudyDesk", "Room/Furniture/Fridge",
                "Room/Interactables/BedRestPoint", "Room/Interactables/StudyDeskPoint", "Room/Interactables/SinkPoint", "Room/Interactables/FridgePoint",
                "Room/Interactables/LightSwitchPoint", "ExitToCity" };
            foreach (string path in required)
                if (root.transform.Find(path) == null) throw new InvalidOperationException("Bedroom validation: missing " + path);
            foreach (string legacy in new[] { "Mattress", "BedFrame", "WindowGlow" })
                if (root.transform.Find(legacy) != null) throw new InvalidOperationException("Legacy block still present: " + legacy);
            int colliders = root.GetComponentsInChildren<Collider>(true).Count(c => !c.isTrigger);
            if (colliders < 20) throw new InvalidOperationException("Bedroom collision layout incomplete: " + colliders);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.GetComponents<Component>().Any(c => c == null)) throw new InvalidOperationException("Missing script on " + t.name);
        }

        // ─────────── Animator states (Sit / Lay) ───────────

        private static void EnsureAnimatorStates()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) throw new InvalidOperationException("Missing " + ControllerPath);
            var machine = controller.layers[0].stateMachine;
            bool changed = false;
            changed |= EnsureState(machine, "Sit", "Assets/ThirdParty/Mixamo/Animations/Remy@Sitting.fbx", new Vector3(520f, 260f, 0f));
            changed |= EnsureState(machine, "Lay", "Assets/ThirdParty/Mixamo/Animations/Remy@Laying Nodding.fbx", new Vector3(520f, 330f, 0f));
            if (changed)
            {
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
            }
        }

        private static bool EnsureState(AnimatorStateMachine machine, string state, string fbx, Vector3 position)
        {
            if (machine.states.Any(s => s.state.name == state)) return false;
            var clip = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null) throw new InvalidOperationException("No animation clip in " + fbx);
            var added = machine.AddState(state, position);
            added.motion = clip;
            added.writeDefaultValues = true;
            return true;
        }

        // ─────────── Helpers ───────────

        private static void CreateMaterials()
        {
            _wallMat = Mat("Room_Wall", new Color(0.93f, 0.9f, 0.84f), 0.15f);
            _floorMat = Mat("Room_FloorWood", new Color(0.6f, 0.43f, 0.29f), 0.35f);
            _ceilingMat = Mat("Room_Ceiling", new Color(0.96f, 0.95f, 0.92f), 0.05f);
            _trimMat = Mat("Room_Trim", new Color(0.97f, 0.97f, 0.95f), 0.3f);
            _woodMat = Mat("Room_DoorWood", new Color(0.47f, 0.32f, 0.21f), 0.3f);
            _clothMat = Mat("Room_Curtain", new Color(0.36f, 0.5f, 0.56f), 0.05f);
            _paperMat = Mat("Room_Paper", new Color(0.99f, 0.97f, 0.92f), 0.05f);
            _tileMat = Mat("Room_GenkanTile", new Color(0.5f, 0.51f, 0.52f), 0.25f);
            _metalMat = Mat("Room_Metal", new Color(0.72f, 0.74f, 0.78f), 0.7f);
            _glassMat = Mat("Room_Glass", new Color(0.75f, 0.85f, 0.95f, 0.25f), 0.9f, transparent: true);
            _skyMat = Unlit("Room_SkyView", new Color(0.98f, 0.62f, 0.42f));
            _shadeMat = Mat("Room_LampShade", new Color(1f, 0.96f, 0.88f), 0.1f);
            _shadeMat.EnableKeyword("_EMISSION");
            _shadeMat.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.6f) * 1.6f);
        }

        private static Material Mat(string name, Color color, float smoothness, bool transparent = false)
        {
            string path = "Assets/NihongoLife/Materials/Home/" + name + ".mat";
            if (!AssetDatabase.IsValidFolder("Assets/NihongoLife/Materials/Home")) AssetDatabase.CreateFolder("Assets/NihongoLife/Materials", "Home");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)RenderQueue.Transparent;
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material Unlit(string name, Color color)
        {
            string path = "Assets/NihongoLife/Materials/Home/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name).transform;
            go.SetParent(parent, false);
            return go;
        }

        private static GameObject Block(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void Sign(Transform parent, string name, string value, Vector3 position, float yaw, float width, float fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var text = go.AddComponent<TextMeshPro>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = fontSize * 10f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(width, width);
        }

        private static Bounds BoundsOf(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private static Bounds LocalBounds(Transform holder)
        {
            var renderers = holder.GetComponentsInChildren<Renderer>();
            var inverse = holder.worldToLocalMatrix;
            Bounds result = new Bounds();
            bool first = true;
            foreach (var r in renderers)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var toLocal = inverse * r.transform.localToWorldMatrix;
                foreach (var v in mf.sharedMesh.vertices)
                {
                    Vector3 p = toLocal.MultiplyPoint3x4(v);
                    if (first) { result = new Bounds(p, Vector3.zero); first = false; }
                    else result.Encapsulate(p);
                }
            }
            return result;
        }
    }
}
#endif
