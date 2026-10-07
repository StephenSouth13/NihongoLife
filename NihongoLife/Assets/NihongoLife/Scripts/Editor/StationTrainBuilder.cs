#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using static NihongoLife.EditorTools.ZoneBuildKit;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Rebuilds the commuter carriage the player rides from Hibari Station (20_StationDistrict):
    /// moquette bench seats, glass windows, side doors, hand straps, luggage racks, LED "つぎは" displays,
    /// ads, seated commuters (humanoid Sit) and layered parallax scenery (catenary masts, Kenney houses
    /// and trees, hills, the sea on the Minato side) plus the window-view camera.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.StationTrainBuilder.Build
    /// Only the TrainCarriageInterior hierarchy is touched; the rest of the station (and any manual edits)
    /// stays as saved. Idempotent: superseded carriage pieces are removed before the new ones are built.
    /// </summary>
    public static class StationTrainBuilder
    {
        public const string ScenePath = "Assets/NihongoLife/Scenes/20_StationDistrict.unity";
        private const string MatDir = "Assets/NihongoLife/Materials/Train";
        private const string Suburban = "Assets/ThirdParty/Kenney/kenney_city-kit-suburban_20/Models/FBX format/";
        private const string ControllerPath = "Assets/NihongoLife/Animations/NL_Humanoid.controller";
        private static readonly string[] CommuterPrefabs =
        {
            "Assets/NihongoLife/Prefabs/Characters/NL_Neighbor.prefab",
            "Assets/NihongoLife/Prefabs/Characters/NL_Guide.prefab",
            "Assets/NihongoLife/Prefabs/Characters/NL_Cashier.prefab",
        };

        // Carriage frame (root-local, the root itself is moved far away from the station hall).
        private static readonly Vector3 C = new Vector3(800f, 0f, 42f);
        private const float HalfLength = 7f, NorthWall = 44.35f, SouthWall = 39.65f, Height = 3.3f;
        private const float WindowLow = 0.95f, WindowHigh = 1.95f;
        private static readonly float[] DoorX = { -3.5f, 3.5f };
        private const float DoorHalf = 0.65f;
        public static readonly Vector3 CarriageOffset = new Vector3(0f, 0f, 260f);

        private static Material _floor, _wall, _ceiling, _lightStrip, _seat, _seatBase, _steel, _glass, _door, _black, _led, _grass, _gravel, _sea, _hill, _hillFar, _mast, _kenney, _adBlue, _adRed, _adOrange, _adGreen, _adGrey, _strap, _rail;

        public static void Build()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var root = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .FirstOrDefault(t => t.name == "TrainCarriageInterior");
                if (root == null) throw new InvalidOperationException("TrainCarriageInterior not found in " + ScenePath);
                bool wasActive = root.gameObject.activeSelf;
                root.gameObject.SetActive(true);
                root.position = CarriageOffset;

                CreateMaterials();
                RemoveSuperseded(root);
                var shell = Group(root, "CarriageShell");
                BuildShell(shell);
                BuildFittings(shell);
                BuildSignage(shell);
                BuildLights(shell);
                PlaceInteractions(root);
                BuildCommuters(Group(root, "CarriagePassengers"));
                BuildScenery(root);
                BuildWindowCamera(root);

                root.gameObject.SetActive(wasActive);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("[StationTrainBuilder] Carriage rebuilt and saved.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static Vector3 P(float dx, float y, float z) => new Vector3(C.x + dx, y, z);

        private static void CreateMaterials()
        {
            _floor = Lit(MatDir, "Train_Floor", new Color(0.42f, 0.44f, 0.47f), 0.35f);
            _wall = Lit(MatDir, "Train_Wall", new Color(0.9f, 0.89f, 0.85f), 0.45f);
            _ceiling = Lit(MatDir, "Train_Ceiling", new Color(0.95f, 0.95f, 0.94f), 0.3f);
            _lightStrip = Lit(MatDir, "Train_LightStrip", Color.white, 0.2f, new Color(1f, 0.98f, 0.92f) * 1.8f);
            _seat = Lit(MatDir, "Train_SeatMoquette", new Color(0.13f, 0.33f, 0.55f), 0.12f);
            _seatBase = Lit(MatDir, "Train_SeatBase", new Color(0.3f, 0.32f, 0.35f), 0.5f);
            _steel = Lit(MatDir, "Train_Steel", new Color(0.78f, 0.8f, 0.82f), 0.85f);
            _glass = Lit(MatDir, "Train_Glass", new Color(0.75f, 0.88f, 0.95f, 0.16f), 0.95f, transparent: true);
            _door = Lit(MatDir, "Train_Door", new Color(0.7f, 0.72f, 0.74f), 0.75f);
            _black = Lit(MatDir, "Train_Black", new Color(0.03f, 0.03f, 0.035f), 0.4f);
            _led = Lit(MatDir, "Train_LEDPanel", new Color(0.02f, 0.02f, 0.02f), 0.6f, new Color(0.05f, 0.03f, 0f));
            _strap = Lit(MatDir, "Train_Strap", new Color(0.95f, 0.95f, 0.92f), 0.4f);
            _rail = Lit(MatDir, "Train_Rail", new Color(0.86f, 0.87f, 0.88f), 0.9f);
            _grass = Lit(MatDir, "Train_Grass", new Color(0.36f, 0.56f, 0.3f), 0.1f);
            _gravel = Lit(MatDir, "Train_Gravel", new Color(0.5f, 0.47f, 0.43f), 0.05f);
            _sea = Lit(MatDir, "Train_Sea", new Color(0.16f, 0.45f, 0.68f), 0.9f);
            _hill = Lit(MatDir, "Train_Hill", new Color(0.25f, 0.45f, 0.28f), 0.05f);
            _hillFar = Lit(MatDir, "Train_HillFar", new Color(0.42f, 0.55f, 0.6f), 0.05f);
            _mast = Lit(MatDir, "Train_Mast", new Color(0.55f, 0.56f, 0.58f), 0.5f);
            _adBlue = Lit(MatDir, "Train_AdBlue", new Color(0.12f, 0.3f, 0.62f), 0.3f);
            _adRed = Lit(MatDir, "Train_AdRed", new Color(0.72f, 0.16f, 0.16f), 0.3f);
            _adOrange = Lit(MatDir, "Train_AdOrange", new Color(0.9f, 0.5f, 0.12f), 0.3f);
            _adGreen = Lit(MatDir, "Train_AdGreen", new Color(0.13f, 0.5f, 0.32f), 0.3f);
            _adGrey = Lit(MatDir, "Train_AdGrey", new Color(0.32f, 0.35f, 0.4f), 0.3f);

            // Every Kenney suburban model samples the same colormap atlas.
            var colormap = AssetDatabase.LoadAssetAtPath<Texture2D>(Suburban + "Textures/colormap.png");
            _kenney = Lit(MatDir, "Train_KenneyColormap", Color.white, 0.15f);
            if (colormap != null) { _kenney.SetTexture("_BaseMap", colormap); _kenney.mainTexture = colormap; EditorUtility.SetDirty(_kenney); }
        }

        private static void RemoveSuperseded(Transform root)
        {
            var keep = new HashSet<string> { "TravelCarriageSpawn", "StartRidePanel", "LeaveTrainDoor", "MovingWindowScenery" };
            foreach (Transform child in root.Cast<Transform>().ToArray())
            {
                if (keep.Contains(child.name)) continue;
                Object.DestroyImmediate(child.gameObject); // old box shell, posts, rails, capsule passenger, previous builds
            }
            var scenery = root.Find("MovingWindowScenery");
            if (scenery != null)
            {
                foreach (Transform child in scenery.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
                foreach (var old in scenery.GetComponents<TrainWindowScenery>()) Object.DestroyImmediate(old);
            }
        }

        // ─────────── Shell ───────────

        private static void BuildShell(Transform shell)
        {
            Block(shell, "Floor", P(0f, -0.05f, C.z), new Vector3(HalfLength * 2f, 0.1f, 4.8f), _floor, true);
            Block(shell, "Ceiling", P(0f, Height - 0.05f, C.z), new Vector3(HalfLength * 2f, 0.1f, 4.8f), _ceiling, true);
            Block(shell, "LightStrip_N", P(0f, Height - 0.12f, C.z + 0.8f), new Vector3(HalfLength * 2f - 0.6f, 0.04f, 0.28f), _lightStrip, false);
            Block(shell, "LightStrip_S", P(0f, Height - 0.12f, C.z - 0.8f), new Vector3(HalfLength * 2f - 0.6f, 0.04f, 0.28f), _lightStrip, false);
            Block(shell, "AirDuct", P(0f, Height - 0.2f, C.z), new Vector3(HalfLength * 2f - 0.6f, 0.12f, 0.7f), _ceiling, false);

            foreach (float sign in new[] { -1f, 1f })
            {
                float x = C.x + sign * HalfLength;
                Block(shell, sign < 0 ? "EndWall_W" : "EndWall_E", new Vector3(x, Height * 0.5f, C.z), new Vector3(0.12f, Height, 4.8f), _wall, true);
                Block(shell, "GangwayDoor" + sign, new Vector3(x - sign * 0.07f, 1.1f, C.z), new Vector3(0.04f, 2.2f, 0.9f), _door, false);
                Block(shell, "GangwayWindow" + sign, new Vector3(x - sign * 0.1f, 1.55f, C.z), new Vector3(0.02f, 0.75f, 0.5f), _black, false);
            }

            foreach (bool north in new[] { true, false })
            {
                float z = north ? NorthWall : SouthWall;
                string side = north ? "N" : "S";
                // Segments between doors: bench bays with windows.
                var bays = new List<(float a, float b)> { (-HalfLength, DoorX[0] - DoorHalf), (DoorX[0] + DoorHalf, DoorX[1] - DoorHalf), (DoorX[1] + DoorHalf, HalfLength) };
                int bayIndex = 0;
                foreach (var (a, b) in bays)
                {
                    float mid = (a + b) * 0.5f, len = b - a;
                    Block(shell, $"WallLow_{side}{bayIndex}", P(mid, WindowLow * 0.5f, z), new Vector3(len, WindowLow, 0.1f), _wall, true);
                    Block(shell, $"WallHigh_{side}{bayIndex}", P(mid, (WindowHigh + Height) * 0.5f, z), new Vector3(len, Height - WindowHigh, 0.1f), _wall, true); // camera collides with it
                    Block(shell, $"Glass_{side}{bayIndex}", P(mid, (WindowLow + WindowHigh) * 0.5f, z), new Vector3(len, WindowHigh - WindowLow, 0.03f), _glass, true);
                    Block(shell, $"Sill_{side}{bayIndex}", P(mid, WindowLow, z + (north ? -0.06f : 0.06f)), new Vector3(len, 0.04f, 0.12f), _steel, false);
                    int panes = Mathf.Max(1, Mathf.RoundToInt(len / 1.3f));
                    for (int i = 1; i < panes; i++)
                        Block(shell, $"Mullion_{side}{bayIndex}_{i}", P(a + len * i / panes, (WindowLow + WindowHigh) * 0.5f, z), new Vector3(0.07f, WindowHigh - WindowLow, 0.08f), _steel, false);
                    bayIndex++;
                }
                // Side doors: two stainless leaves with a glass light each.
                foreach (float dx in DoorX)
                {
                    Block(shell, $"DoorHead_{side}{dx}", P(dx, (2.25f + Height) * 0.5f, z), new Vector3(DoorHalf * 2f, Height - 2.25f, 0.1f), _wall, true);
                    foreach (float leaf in new[] { -0.5f, 0.5f })
                    {
                        float lx = dx + leaf * DoorHalf;
                        Block(shell, $"DoorLeaf_{side}{dx}{leaf}", P(lx, 1.125f, z), new Vector3(DoorHalf - 0.02f, 2.25f, 0.06f), _door, true);
                        Block(shell, $"DoorLight_{side}{dx}{leaf}", P(lx, 1.55f, z + (north ? -0.035f : 0.035f)), new Vector3(DoorHalf - 0.2f, 0.75f, 0.01f), _glass, false);
                    }
                    Block(shell, $"DoorStep_{side}{dx}", P(dx, 0.01f, z + (north ? -0.2f : 0.2f)), new Vector3(DoorHalf * 2f, 0.02f, 0.3f), _steel, false);
                }
            }
        }

        // ─────────── Seats, poles, straps, racks ───────────

        private static void BuildFittings(Transform shell)
        {
            var bays = new List<(float a, float b)> { (-HalfLength + 0.15f, DoorX[0] - DoorHalf - 0.15f), (DoorX[0] + DoorHalf + 0.15f, DoorX[1] - DoorHalf - 0.15f), (DoorX[1] + DoorHalf + 0.15f, HalfLength - 0.15f) };
            foreach (bool north in new[] { true, false })
            {
                float wall = north ? NorthWall : SouthWall, inward = north ? -1f : 1f;
                string side = north ? "N" : "S";
                int i = 0;
                foreach (var (a, b) in bays)
                {
                    float mid = (a + b) * 0.5f, len = b - a;
                    Block(shell, $"BenchBase_{side}{i}", P(mid, 0.2f, wall + inward * 0.3f), new Vector3(len, 0.4f, 0.48f), _seatBase, true);
                    Block(shell, $"BenchSeat_{side}{i}", P(mid, 0.45f, wall + inward * 0.33f), new Vector3(len, 0.1f, 0.55f), _seat, false);
                    Block(shell, $"BenchBack_{side}{i}", P(mid, 0.78f, wall + inward * 0.1f), new Vector3(len, 0.55f, 0.1f), _seat, false);
                    foreach (float end in new[] { a, b })
                    {
                        Block(shell, $"Armrest_{side}{i}_{end}", P(end, 0.62f, wall + inward * 0.34f), new Vector3(0.05f, 0.3f, 0.55f), _steel, false);
                        Cylinder(shell, $"Stanchion_{side}{i}_{end}", P(end, Height * 0.5f, wall + inward * 0.62f), new Vector3(0.045f, Height * 0.5f, 0.045f), _rail, false);
                    }
                    var rack = Block(shell, $"LuggageRack_{side}{i}", P(mid, 2.08f, wall + inward * 0.2f), new Vector3(len, 0.03f, 0.34f), _rail, false);
                    rack.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    i++;
                }

                // Hand strap rail along the whole aisle edge, straps every 0.55 m.
                float railZ = wall + inward * 0.95f;
                var bar = Cylinder(shell, "StrapRail_" + side, P(0f, 2.62f, railZ), new Vector3(0.035f, HalfLength - 0.2f, 0.035f), _rail, false);
                bar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                for (float dx = -HalfLength + 0.6f; dx < HalfLength - 0.4f; dx += 0.55f)
                {
                    if (DoorX.Any(d => Mathf.Abs(dx - d) < DoorHalf + 0.1f)) continue;
                    Block(shell, $"Strap_{side}{dx:0.00}", P(dx, 2.48f, railZ), new Vector3(0.025f, 0.26f, 0.015f), _strap, false);
                    Block(shell, $"StrapGrip_{side}{dx:0.00}", P(dx, 2.34f, railZ), new Vector3(0.11f, 0.03f, 0.025f), _strap, false);
                }
            }
        }

        // ─────────── LED displays, line map, ads ───────────

        private static void BuildSignage(Transform shell)
        {
            int led = 0;
            foreach (bool north in new[] { true, false })
            {
                float z = (north ? NorthWall : SouthWall) + (north ? -0.07f : 0.07f);
                float yaw = north ? 0f : 180f;
                foreach (float dx in DoorX)
                {
                    Block(shell, $"LEDPanel_{led}", P(dx, 2.62f, z), new Vector3(1.2f, 0.26f, 0.04f), _led, false);
                    var text = Text(shell, $"CarriageLED_{led}", "つぎは  がくえんまえ", P(dx, 2.62f, z + (north ? -0.03f : 0.03f)), yaw, 0.11f, new Color(1f, 0.6f, 0.1f), 1.15f);
                    text.enableAutoSizing = true; text.fontSizeMin = 0.4f; text.fontSizeMax = 1.2f;
                    text.rectTransform.sizeDelta = new Vector2(1.1f, 0.22f);
                    led++;
                }
            }

            // Line map above the north bench in the middle bay.
            float mz = NorthWall - 0.07f;
            Block(shell, "LineMap_Board", P(0f, 2.62f, mz), new Vector3(2.6f, 0.36f, 0.03f), _strap, false);
            Text(shell, "LineMap_Text", "ひばり ━━ がくえんまえ ━━ ミナト\n<size=60%>Hibari · Gakuen-mae ¥180 · Minato ¥320</size>", P(0f, 2.62f, mz - 0.025f), 0f, 0.085f, new Color(0.1f, 0.35f, 0.25f), 2.5f);

            var ads = new (string text, Material mat)[]
            {
                ("ひばり日本語学院\n<size=60%>「はじめまして」から はじめよう</size>", _adBlue),
                ("ひばり寿司 · ミナト\n<size=60%>しんせんな さかな、まいにち</size>", _adRed),
                ("ひばりまつり 8/20\n<size=60%>はなび · やたい · ぼんおどり</size>", _adOrange),
                ("ひばりマート\n<size=60%>おにぎり 120えん〜</size>", _adGreen),
                ("でんしゃの マナー\n<size=60%>でんわは しずかに おねがいします</size>", _adGrey),
                ("ミナト すいぞくかん\n<size=60%>うみの いきもの に あいに いこう</size>", _adBlue),
            };
            float[] adX = { -5.4f, -1.6f, 1.6f, 5.4f };
            int adIndex = 0;
            foreach (bool north in new[] { true, false })
            {
                float z = (north ? NorthWall : SouthWall) + (north ? -0.07f : 0.07f);
                float yaw = north ? 0f : 180f;
                foreach (float dx in adX)
                {
                    if (north && Mathf.Abs(dx) < 2f && adIndex % 2 == 0) { adIndex++; continue; } // line map sits there
                    var (value, mat) = ads[adIndex % ads.Length];
                    Block(shell, $"Ad_{adIndex}", P(dx, 2.62f, z), new Vector3(1.5f, 0.42f, 0.02f), mat, false);
                    var text = Text(shell, $"AdText_{adIndex}", value, P(dx, 2.62f, z + (north ? -0.02f : 0.02f)), yaw, 0.075f, Color.white, 1.4f);
                    text.enableAutoSizing = true; text.fontSizeMin = 0.3f; text.fontSizeMax = 0.9f;
                    text.rectTransform.sizeDelta = new Vector2(1.4f, 0.38f);
                    adIndex++;
                }
            }
        }

        private static void BuildLights(Transform shell)
        {
            for (int i = -1; i <= 1; i++)
                PointLight(shell, "CarriageLight_" + i, P(i * 4.6f, Height - 0.35f, C.z), 6.5f, 1.6f, new Color(1f, 0.97f, 0.9f));
        }

        // ─────────── Interactions & spawn ───────────

        private static void PlaceInteractions(Transform root)
        {
            var spawn = root.Find("TravelCarriageSpawn");
            if (spawn != null) spawn.SetPositionAndRotation(root.TransformPoint(P(-5.6f, 0.1f, C.z)), Quaternion.Euler(0f, 90f, 0f));

            // Alight through the south door nearest the spawn; the interactable is an invisible trigger.
            var leave = root.Find("LeaveTrainDoor");
            if (leave != null) MakeTrigger(leave, root.TransformPoint(P(DoorX[0], 1.1f, SouthWall + 0.25f)), new Vector3(1.3f, 2.2f, 0.5f));
            // Window view: the middle north window.
            var view = root.Find("StartRidePanel");
            if (view != null) MakeTrigger(view, root.TransformPoint(P(0.8f, 1.4f, NorthWall - 0.3f)), new Vector3(1.4f, 1f, 0.5f));
        }

        private static void MakeTrigger(Transform target, Vector3 worldPosition, Vector3 size)
        {
            target.position = worldPosition;
            target.rotation = Quaternion.identity;
            target.localScale = Vector3.one;
            var renderer = target.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.enabled = false;
            var box = target.GetComponent<BoxCollider>();
            if (box == null) box = target.gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = size;
            target.gameObject.layer = InteractableLayer;
        }

        // ─────────── Commuters ───────────

        private static void BuildCommuters(Transform parent)
        {
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            // (dx, north, prefab, talk id, label, always on board)
            var seats = new (float dx, bool north, int prefab, string talk, string label, bool always)[]
            {
                (-1.6f, true, 1, "grandma", "bà cụ", true),
                (1.9f, false, 0, "student", "học sinh", true),
                (0.6f, true, 2, "worker", "nhân viên văn phòng", true),
                (-6.1f, true, 0, null, null, false),
                (-1.1f, false, 2, null, null, false),
                (5.2f, false, 1, null, null, false),
                (6.0f, true, 2, null, null, false),
                (-2.3f, false, 0, null, null, false),
            };
            int index = 0;
            foreach (var seat in seats)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CommuterPrefabs[seat.prefab]);
                if (prefab == null) throw new InvalidOperationException("Missing " + CommuterPrefabs[seat.prefab]);
                float wall = seat.north ? NorthWall : SouthWall, inward = seat.north ? -1f : 1f;
                var commuter = new GameObject($"Commuter_{index}" + (seat.talk != null ? "_" + seat.talk : string.Empty));
                commuter.transform.SetParent(parent, false);
                commuter.transform.localPosition = P(seat.dx, 0.02f, wall + inward * 0.42f);
                commuter.transform.localRotation = Quaternion.Euler(0f, seat.north ? 180f : 0f, 0f);
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                visual.name = "Visual";
                visual.transform.SetParent(commuter.transform, false);
                foreach (var c in visual.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                var animator = visual.GetComponentInChildren<Animator>(true);
                if (animator != null && controller != null) animator.runtimeAnimatorController = controller;
                var animation = commuter.AddComponent<CharacterAnimationController>();
                animation.SetAnimator(animator);
                commuter.AddComponent<SeatedPassenger>().Configure(seat.always);
                if (seat.talk != null)
                {
                    commuter.layer = InteractableLayer;
                    var box = commuter.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    box.center = new Vector3(0f, 0.8f, 0.35f);
                    box.size = new Vector3(0.8f, 1.6f, 1.1f);
                    commuter.AddComponent<TrainPassengerTalk>().Configure(seat.talk, seat.label);
                }
                index++;
            }
        }

        // ─────────── Scenery ───────────

        private static void BuildScenery(Transform root)
        {
            var scenery = root.Find("MovingWindowScenery");
            if (scenery == null) { scenery = Group(root, "MovingWindowScenery"); }
            scenery.localPosition = C;
            scenery.localRotation = Quaternion.identity;

            var statics = Group(scenery, "Static");
            Block(statics, "TrackBed", new Vector3(0f, -1.05f, 0f), new Vector3(400f, 0.1f, 6f), _gravel, false);
            Block(statics, "Ground_North", new Vector3(0f, -1.1f, 60f), new Vector3(400f, 0.1f, 114f), _grass, false);
            Block(statics, "Ground_South", new Vector3(0f, -1.1f, -14f), new Vector3(400f, 0.1f, 22f), _grass, false);
            Block(statics, "Sea", new Vector3(0f, -1.3f, -160f), new Vector3(600f, 0.1f, 280f), _sea, false);
            Block(statics, "SeaWall", new Vector3(0f, -1.15f, -25.2f), new Vector3(400f, 0.4f, 0.6f), _gravel, false);
            Block(statics, "FarHills", new Vector3(0f, 6f, 210f), new Vector3(600f, 16f, 20f), _hillFar, false);

            var near = Group(scenery, "Layer_Near");
            for (float x = -60f; x < 60f; x += 10f)
            {
                foreach (float side in new[] { 1f, -1f })
                {
                    var mast = Group(near, $"Mast_{(side > 0 ? "N" : "S")}{x:0}");
                    mast.localPosition = new Vector3(x + (side > 0 ? 0f : 5f), 0f, side * 4.4f);
                    Cylinder(mast, "Pole", new Vector3(0f, 2.2f, 0f), new Vector3(0.18f, 3.3f, 0.18f), _mast, false);
                    Block(mast, "Arm", new Vector3(0f, 5.0f, -side * 1.6f), new Vector3(0.1f, 0.1f, 3.2f), _mast, false);
                }
            }
            Block(near, "Wire_N", new Vector3(0f, 4.95f, 1.2f), new Vector3(120f, 0.03f, 0.03f), _black, false).transform.SetAsLastSibling();

            var mid = Group(scenery, "Layer_Mid");
            string[] houses = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p" };
            var random = new System.Random(20260920);
            int h = 0;
            for (float x = -66f; x < 66f; x += 11f)
            {
                PlaceKenney(mid, "building-type-" + houses[h % houses.Length], new Vector3(x + (float)random.NextDouble() * 3f, -1.05f, 15f + (float)random.NextDouble() * 4f), 180f + random.Next(-12, 12), 9f);
                PlaceKenney(mid, random.Next(2) == 0 ? "tree-large" : "tree-small", new Vector3(x + 5.5f, -1.05f, 10.5f + (float)random.NextDouble() * 2f), random.Next(0, 360), 5f);
                if (h % 2 == 0) PlaceKenney(mid, "building-type-" + houses[(h + 7) % houses.Length], new Vector3(x + 3f, -1.05f, 27f + (float)random.NextDouble() * 6f), 180f, 10f);
                // South: a seaside promenade — trees and the odd house before the sea wall.
                if (h % 3 == 0) PlaceKenney(mid, "building-type-" + houses[(h + 3) % houses.Length], new Vector3(x + 2f, -1.05f, -13f), 0f, 8f);
                else PlaceKenney(mid, "tree-large", new Vector3(x + 2f, -1.05f, -12f + (float)random.NextDouble() * 3f), random.Next(0, 360), 5.5f);
                h++;
            }

            var far = Group(scenery, "Layer_Far");
            for (float x = -120f; x < 120f; x += 30f)
            {
                var hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hill.name = $"Hill_{x:0}";
                Object.DestroyImmediate(hill.GetComponent<Collider>());
                hill.transform.SetParent(far, false);
                hill.transform.localPosition = new Vector3(x + (float)random.NextDouble() * 10f, -2f, 75f + (float)random.NextDouble() * 25f);
                hill.transform.localScale = new Vector3(36f + (float)random.NextDouble() * 20f, 14f + (float)random.NextDouble() * 10f, 26f);
                hill.GetComponent<Renderer>().sharedMaterial = _hill;
                var boat = Block(far, $"Boat_{x:0}", new Vector3(x + 12f, -1.0f, -55f - (float)random.NextDouble() * 40f), new Vector3(4f, 0.8f, 1.4f), _strap, false);
                Block(boat.transform.parent, $"BoatCabin_{x:0}", boat.transform.localPosition + new Vector3(-0.5f, 0.7f, 0f), new Vector3(1.4f, 0.8f, 1f), _adRed, false);
            }

            foreach (var r in scenery.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true; }

            var component = scenery.gameObject.AddComponent<TrainWindowScenery>();
            component.Configure(17f,
                new TrainWindowScenery.Layer { root = near, parallax = 1f, loopWidth = 120f },
                new TrainWindowScenery.Layer { root = mid, parallax = 0.6f, loopWidth = 132f },
                new TrainWindowScenery.Layer { root = far, parallax = 0.12f, loopWidth = 240f });
        }

        private static void PlaceKenney(Transform parent, string model, Vector3 position, float yaw, float targetHeight)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Suburban + model + ".fbx");
            if (source == null) throw new InvalidOperationException("Missing Kenney model " + model);
            var holder = Group(parent, model);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.transform.SetParent(holder, false);
            foreach (var c in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterials = Enumerable.Repeat(_kenney, r.sharedMaterials.Length).ToArray();
            Bounds b = BoundsOf(instance);
            instance.transform.localScale *= targetHeight / Mathf.Max(0.01f, b.size.y);
            b = BoundsOf(instance);
            Vector3 anchor = holder.position; // the layer is not at the world origin
            instance.transform.position -= new Vector3(b.center.x - anchor.x, b.min.y - anchor.y, b.center.z - anchor.z);
            holder.localPosition = position;
            holder.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private static void BuildWindowCamera(Transform root)
        {
            var go = new GameObject("TrainWindowCamera");
            go.transform.SetParent(root, false);
            go.transform.localPosition = P(2.3f, 1.3f, NorthWall - 0.9f); // free window, no commuter in front
            go.transform.localRotation = Quaternion.Euler(3f, 0f, 0f);
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 600f;
            camera.depth = 5f;
            camera.enabled = false;
        }
    }
}
#endif
