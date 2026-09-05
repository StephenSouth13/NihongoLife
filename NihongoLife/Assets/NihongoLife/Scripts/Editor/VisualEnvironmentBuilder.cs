using System.IO;
using UnityEditor;
using UnityEngine;

namespace NihongoLife.Editor
{
    public static class VisualEnvironmentBuilder
    {
        private const string RoadBase = "Assets/ThirdParty/Kenney/kenney_city-kit-roads/Models/FBX format/";
        private const string SuburbanBase = "Assets/ThirdParty/Kenney/kenney_city-kit-suburban_20/Models/FBX format/";
        private const string FurnitureBase = "Assets/ThirdParty/Kenney/kenney_furniture-kit/Models/FBX format/";
        private const string FoodBase = "Assets/ThirdParty/Kenney/kenney_food-kit/Models/FBX format/";

        private static readonly string[] PrefabFolders =
        {
            "Assets/NihongoLife/Prefabs/Environment/Roads",
            "Assets/NihongoLife/Prefabs/Environment/Buildings",
            "Assets/NihongoLife/Prefabs/Furniture",
            "Assets/NihongoLife/Prefabs/Food",
            "Assets/NihongoLife/Materials/Kenney"
        };

        public static void GenerateVisualEnvironment(GameObject environmentRoot)
        {
            EnsureDirectories();
            GenerateWrapperPrefabs();
            AssembleKonbini(environmentRoot);
        }

        [MenuItem("NihongoLife/Rebuild Current Street")]
        public static void RebuildCurrentStreet()
        {
            SceneBuilder.RebuildGameplaySandbox();
        }

        private static void EnsureDirectories()
        {
            foreach (var folder in PrefabFolders)
            {
                EnsureFolderExists(folder);
            }
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && parent != "Assets" && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
        }

        private static void GenerateWrapperPrefabs()
        {
            CreateWrapperDeterministic(RoadBase + "road-straight.fbx", "Environment/Roads", "road_straight", 10f);
            CreateWrapperDeterministic(RoadBase + "road-crossroad-line.fbx", "Environment/Roads", "road_crossroad", 10f);
            CreateWrapperDeterministic(RoadBase + "road-crossing.fbx", "Environment/Roads", "road_crossing", 10f);
            CreateWrapperDeterministic(RoadBase + "road-side.fbx", "Environment/Roads", "road_side", 10f);
            CreateWrapperDeterministic(RoadBase + "road-intersection-line.fbx", "Environment/Roads", "road_intersection", 10f);
            CreateWrapperDeterministic(RoadBase + "light-curved.fbx", "Environment/Roads", "street_light", 3f, true);
            CreateWrapperDeterministic(RoadBase + "light-curved-double.fbx", "Environment/Roads", "street_light_double", 3.2f, true);
            CreateWrapperDeterministic(RoadBase + "traffic-light.fbx", "Environment/Roads", "traffic_light", 3.2f, true);
            CreateWrapperDeterministic(RoadBase + "road-sign-street.fbx", "Environment/Roads", "street_sign", 1.6f, true);
            CreateWrapperDeterministic(RoadBase + "road-sign-stop.fbx", "Environment/Roads", "stop_sign", 1.5f, true);
            CreateWrapperDeterministic(RoadBase + "electricity-pole.fbx", "Environment/Roads", "electricity_pole", 4f, true);
            CreateWrapperDeterministic(RoadBase + "electricity-wires.fbx", "Environment/Roads", "electricity_wires", 8f);

            for (char c = 'a'; c <= 'h'; c++)
            {
                CreateWrapperDeterministic(SuburbanBase + $"building-type-{c}.fbx", "Environment/Buildings", $"building_{c}", 10f);
            }

            CreateWrapperDeterministic(SuburbanBase + "tree-small.fbx", "Environment/Buildings", "tree_small", 2.4f, true);
            CreateWrapperDeterministic(SuburbanBase + "tree-large.fbx", "Environment/Buildings", "tree_large", 3.8f, true);
            CreateWrapperDeterministic(SuburbanBase + "planter.fbx", "Environment/Buildings", "planter", 1.2f);
            CreateWrapperDeterministic(SuburbanBase + "fence-1x4.fbx", "Environment/Buildings", "fence_1x4", 4f);
            CreateWrapperDeterministic(SuburbanBase + "driveway-short.fbx", "Environment/Buildings", "driveway_short", 4f);

            CreateWrapperDeterministic(FurnitureBase + "bookcaseOpen.fbx", "Furniture", "shelf", 1.8f, true);
            CreateWrapperDeterministic(FurnitureBase + "tableCoffee.fbx", "Furniture", "counter", 2f);
            CreateWrapperDeterministic(FurnitureBase + "bench.fbx", "Furniture", "bench", 1.8f);
            CreateWrapperDeterministic(FurnitureBase + "trashcan.fbx", "Furniture", "trashcan", 0.75f, true);
            CreateWrapperDeterministic(FurnitureBase + "pottedPlant.fbx", "Furniture", "potted_plant", 1.2f, true);
            CreateWrapperDeterministic(FurnitureBase + "kitchenFridgeLarge.fbx", "Furniture", "store_fridge", 2f, true);
            CreateWrapperDeterministic(FurnitureBase + "kitchenCoffeeMachine.fbx", "Furniture", "coffee_machine", 0.75f, true);
            CreateWrapperDeterministic(FurnitureBase + "cardboardBoxClosed.fbx", "Furniture", "delivery_box", 0.75f, true);

            CreateWrapperDeterministic(FoodBase + "rice-ball.fbx", "Food", "food_apple", 0.15f);
            CreateWrapperDeterministic(FoodBase + "soda-bottle.fbx", "Food", "food_bottle", 0.25f, true);

            AssetDatabase.SaveAssets();
        }

        private static void CreateWrapperDeterministic(string vendorModelPath, string folderSuffix, string name, float targetSize, bool useYAxis = false)
        {
            string prefabPath = $"Assets/NihongoLife/Prefabs/{folderSuffix}/{name}.prefab";
            GameObject vendorModel = AssetDatabase.LoadAssetAtPath<GameObject>(vendorModelPath);
            if (vendorModel == null)
            {
                Debug.LogError($"[VisualEnvironmentBuilder] Missing asset: {vendorModelPath}");
                return;
            }

            GameObject root = new GameObject(name);
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(vendorModel);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);

            FixMaterials(visual);
            NormalizeScale(visual, targetSize, useYAxis);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        private static void FixMaterials(GameObject visual)
        {
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null) continue;
                    if (source.shader.name == "Universal Render Pipeline/Simple Lit" || source.shader.name == "Universal Render Pipeline/Lit") continue;

                    string sourcePath = AssetDatabase.GetAssetPath(source);
                    string guid = AssetDatabase.AssetPathToGUID(sourcePath);
                    if (string.IsNullOrEmpty(guid)) guid = source.name.Replace(" (Instance)", string.Empty);

                    string materialPath = $"Assets/NihongoLife/Materials/Kenney/{guid}_URP.mat";
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (material == null)
                    {
                        material = new Material(Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard"));
                        if (source.HasProperty("_MainTex")) material.mainTexture = source.mainTexture;
                        if (source.HasProperty("_BaseMap")) material.mainTexture = source.GetTexture("_BaseMap");
                        if (source.HasProperty("_Color")) material.color = source.color;
                        if (source.HasProperty("_BaseColor")) material.color = source.GetColor("_BaseColor");
                        AssetDatabase.CreateAsset(material, materialPath);
                    }

                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static void NormalizeScale(GameObject visual, float targetSize, bool useYAxis)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            visual.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            float currentSize = useYAxis ? bounds.size.y : Mathf.Max(bounds.size.x, bounds.size.z);
            if (currentSize <= 0.001f || currentSize > 1000f)
            {
                Debug.LogWarning($"[VisualEnvironmentBuilder] Abnormal bounds for {visual.transform.parent.name}: {currentSize}");
                return;
            }

            float scaleFactor = targetSize / currentSize;
            visual.transform.localScale = Vector3.one * scaleFactor;
            visual.transform.localPosition *= scaleFactor;
        }

        public static void AssembleKonbini(GameObject root)
        {
            while (root.transform.childCount > 0)
            {
                Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
            }

            var road = LoadPrefab("Environment/Roads/road_straight");
            var crossroad = LoadPrefab("Environment/Roads/road_crossroad");
            var crossing = LoadPrefab("Environment/Roads/road_crossing");
            var sidewalk = LoadPrefab("Environment/Roads/road_side");
            var streetLight = LoadPrefab("Environment/Roads/street_light");
            var doubleLight = LoadPrefab("Environment/Roads/street_light_double");
            var trafficLight = LoadPrefab("Environment/Roads/traffic_light");
            var streetSign = LoadPrefab("Environment/Roads/street_sign");
            var stopSign = LoadPrefab("Environment/Roads/stop_sign");
            var electricityPole = LoadPrefab("Environment/Roads/electricity_pole");
            var electricityWires = LoadPrefab("Environment/Roads/electricity_wires");
            var treeSmall = LoadPrefab("Environment/Buildings/tree_small");
            var treeLarge = LoadPrefab("Environment/Buildings/tree_large");
            var planter = LoadPrefab("Environment/Buildings/planter");
            var fence = LoadPrefab("Environment/Buildings/fence_1x4");
            var driveway = LoadPrefab("Environment/Buildings/driveway_short");
            var bench = LoadPrefab("Furniture/bench");
            var trash = LoadPrefab("Furniture/trashcan");
            var pottedPlant = LoadPrefab("Furniture/potted_plant");
            var storeFridge = LoadPrefab("Furniture/store_fridge");
            var coffeeMachine = LoadPrefab("Furniture/coffee_machine");
            var deliveryBox = LoadPrefab("Furniture/delivery_box");

            CreateDistrictBase(root.transform);
            CreateRoadNetwork(root.transform, road, crossroad, crossing, sidewalk);
            CreateBuildingRows(root.transform);
            CreateStreetFurniture(root.transform, streetLight, doubleLight, trafficLight, streetSign, stopSign, treeSmall, treeLarge, planter, bench, trash, electricityPole, electricityWires, fence, driveway);
            CreateStorefrontDetails(root.transform, pottedPlant, storeFridge, coffeeMachine, deliveryBox);
        }

        private static GameObject LoadPrefab(string suffix)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/NihongoLife/Prefabs/{suffix}.prefab");
        }

        private static void CreateDistrictBase(Transform root)
        {
            CreateBlock(root, "BackLot_North", new Vector3(0f, -0.04f, 19f), new Vector3(126f, 0.08f, 26f), new Color(0.22f, 0.23f, 0.24f));
            CreateBlock(root, "BackLot_South", new Vector3(0f, -0.04f, -43f), new Vector3(126f, 0.08f, 24f), new Color(0.2f, 0.21f, 0.22f));
            CreateBlock(root, "PocketPark_East", new Vector3(48f, -0.035f, -4f), new Vector3(20f, 0.06f, 15f), new Color(0.18f, 0.3f, 0.19f));
            CreateBlock(root, "PocketPark_West", new Vector3(-50f, -0.035f, -24f), new Vector3(20f, 0.06f, 15f), new Color(0.18f, 0.29f, 0.2f));
        }

        private static void CreateRoadNetwork(Transform root, GameObject road, GameObject crossroad, GameObject crossing, GameObject sidewalk)
        {
            float[] horizontalRoadZ = { -50f, -30f, -10f, 10f, 30f };
            float[] verticalRoadX = { -50f, -30f, 0f, 30f, 50f };

            foreach (float zPos in horizontalRoadZ)
            {
                for (int x = -6; x <= 6; x++)
                {
                    bool isIntersection = Mathf.Approximately(x * 10f, -50f) || Mathf.Approximately(x * 10f, -30f) || x == 0 || Mathf.Approximately(x * 10f, 30f) || Mathf.Approximately(x * 10f, 50f);
                    GameObject segment = isIntersection && crossroad != null ? crossroad : road;
                    string name = Mathf.Approximately(zPos, -10f) && x == 0 ? "MainCrossroad" : $"RoadGrid_H_{zPos}_{x}";
                    InstantiateScenePrefab(segment, root, name, new Vector3(x * 10f, 0f, zPos), Quaternion.identity);
                    InstantiateScenePrefab(sidewalk, root, $"Sidewalk_H_N_{zPos}_{x}", new Vector3(x * 10f, 0f, zPos + 5.3f), Quaternion.identity);
                    InstantiateScenePrefab(sidewalk, root, $"Sidewalk_H_S_{zPos}_{x}", new Vector3(x * 10f, 0f, zPos - 5.3f), Quaternion.Euler(0f, 180f, 0f));
                }
            }

            foreach (float xPos in verticalRoadX)
            {
                for (int z = -6; z <= 4; z++)
                {
                    float zPos = z * 10f;
                    if (Mathf.Approximately(zPos, -50f) || Mathf.Approximately(zPos, -30f) || Mathf.Approximately(zPos, -10f) || Mathf.Approximately(zPos, 10f) || Mathf.Approximately(zPos, 30f)) continue;
                    InstantiateScenePrefab(road, root, $"RoadGrid_V_{xPos}_{z}", new Vector3(xPos, 0f, zPos), Quaternion.Euler(0f, 90f, 0f));
                    InstantiateScenePrefab(sidewalk, root, $"Sidewalk_V_W_{xPos}_{z}", new Vector3(xPos - 5.3f, 0f, zPos), Quaternion.Euler(0f, 90f, 0f));
                    InstantiateScenePrefab(sidewalk, root, $"Sidewalk_V_E_{xPos}_{z}", new Vector3(xPos + 5.3f, 0f, zPos), Quaternion.Euler(0f, -90f, 0f));
                }
            }

            InstantiateScenePrefab(crossing, root, "Crosswalk_KonbiniFront", new Vector3(0f, 0.015f, -9.9f), Quaternion.identity);
            InstantiateScenePrefab(crossing, root, "Crosswalk_SideStreet", new Vector3(0f, 0.02f, -10f), Quaternion.Euler(0f, 90f, 0f));

            for (int x = -6; x <= 6; x++)
            {
                CreateLaneLine(root, $"LaneLine_Main_{x}", new Vector3(x * 10f, 0.055f, -10f), Quaternion.identity);
            }
        }

        private static void CreateBuildingRows(Transform root)
        {
            GameObject[] buildings =
            {
                LoadPrefab("Environment/Buildings/building_a"),
                LoadPrefab("Environment/Buildings/building_b"),
                LoadPrefab("Environment/Buildings/building_c"),
                LoadPrefab("Environment/Buildings/building_d"),
                LoadPrefab("Environment/Buildings/building_e"),
                LoadPrefab("Environment/Buildings/building_f"),
                LoadPrefab("Environment/Buildings/building_g"),
                LoadPrefab("Environment/Buildings/building_h")
            };

            float[] northX = { -54f, -42f, -30f, -18f, -6f, 0f, 12f, 24f, 36f, 48f, 60f };
            for (int i = 0; i < northX.Length; i++)
            {
                var prefab = buildings[(i + 1) % buildings.Length];
                var instance = InstantiateScenePrefab(prefab, root, i == 5 ? "KonbiniStoreAsset" : $"StreetBuilding_N_{i}", new Vector3(northX[i], 0f, 4.8f), Quaternion.Euler(0f, 180f, 0f));
                if (instance != null && i == 5) instance.transform.localScale *= 0.82f;
                if (instance != null) AddFacadeAccent(instance.transform, i == 5);
            }

            float[] southX = { -54f, -42f, -30f, -18f, -6f, 18f, 30f, 42f, 54f };
            for (int i = 0; i < southX.Length; i++)
            {
                var instance = InstantiateScenePrefab(buildings[(i + 3) % buildings.Length], root, $"StreetBuilding_S_{i}", new Vector3(southX[i], 0f, -24.7f), Quaternion.identity);
                if (instance != null) AddFacadeAccent(instance.transform, false);
            }

            float[] sideZ = { -56f, -44f, -32f, 12f, 24f, 36f };
            for (int i = 0; i < sideZ.Length; i++)
            {
                var west = InstantiateScenePrefab(buildings[(i + 4) % buildings.Length], root, $"StreetBuilding_W_{i}", new Vector3(-12f, 0f, sideZ[i]), Quaternion.Euler(0f, 90f, 0f));
                var east = InstantiateScenePrefab(buildings[(i + 6) % buildings.Length], root, $"StreetBuilding_E_{i}", new Vector3(12f, 0f, sideZ[i]), Quaternion.Euler(0f, -90f, 0f));
                if (west != null) AddFacadeAccent(west.transform, false);
                if (east != null) AddFacadeAccent(east.transform, false);
            }
        }

        private static void AddFacadeAccent(Transform building, bool isStore)
        {
            var trimMat = CreateRuntimeMat(building.name + "_Trim_Mat", isStore ? new Color(0.95f, 0.55f, 0.18f) : new Color(0.08f, 0.11f, 0.14f));
            var glowMat = CreateRuntimeMat(building.name + "_WarmWindow_Mat", new Color(1f, 0.78f, 0.38f, 1f));
            AddFacadePiece(building, "FacadeTrim", new Vector3(0f, 2.15f, -3.62f), new Vector3(6.7f, 0.18f, 0.08f), trimMat);
            AddFacadePiece(building, "DoorMat", new Vector3(0f, 0.04f, -3.92f), new Vector3(1.55f, 0.06f, 0.82f), trimMat);

            if (!isStore)
            {
                AddFacadePiece(building, "WarmWindow_L", new Vector3(-1.65f, 1.85f, -3.66f), new Vector3(0.9f, 0.55f, 0.05f), glowMat);
                AddFacadePiece(building, "WarmWindow_R", new Vector3(1.65f, 1.85f, -3.66f), new Vector3(0.9f, 0.55f, 0.05f), glowMat);
            }
        }

        private static void AddFacadePiece(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = localScale;
            piece.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(piece.GetComponent<Collider>());
        }

        private static void CreateStreetFurniture(Transform root, GameObject streetLight, GameObject doubleLight, GameObject trafficLight, GameObject streetSign, GameObject stopSign, GameObject treeSmall, GameObject treeLarge, GameObject planter, GameObject bench, GameObject trash, GameObject electricityPole, GameObject electricityWires, GameObject fence, GameObject driveway)
        {
            for (int i = -6; i <= 6; i++)
            {
                float x = i * 10f + 3f;
                AddStreetLight(root, streetLight, $"StreetLight_N_{i}", new Vector3(x, 0f, -4.2f), Quaternion.identity, new Vector3(0f, 2.35f, -1.2f), Quaternion.Euler(62f, 180f, 0f));
                AddStreetLight(root, streetLight, $"StreetLight_S_{i}", new Vector3(x - 5f, 0f, -15.8f), Quaternion.Euler(0f, 180f, 0f), new Vector3(0f, 2.35f, -1.2f), Quaternion.Euler(62f, 180f, 0f));

                InstantiateScenePrefab(treeSmall, root, $"Tree_N_{i}", new Vector3(i * 10f - 2f, 0f, -3.15f), Quaternion.identity);
                InstantiateScenePrefab(treeSmall, root, $"Tree_S_{i}", new Vector3(i * 10f + 1.5f, 0f, -16.85f), Quaternion.Euler(0f, 35f, 0f));
                InstantiateScenePrefab(planter, root, $"Planter_N_{i}", new Vector3(i * 10f + 1.2f, 0f, -2.8f), Quaternion.identity);
                InstantiateScenePrefab(planter, root, $"Planter_S_{i}", new Vector3(i * 10f - 1.6f, 0f, -17.15f), Quaternion.identity);
            }

            AddStreetLight(root, doubleLight, "StreetLight_Crossroad_Double", new Vector3(-4.7f, 0f, -5.05f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0f, 2.45f, 0f), Quaternion.Euler(58f, 180f, 0f));
            InstantiateScenePrefab(trafficLight, root, "TrafficLight_NW", new Vector3(-5.8f, 0f, -5.2f), Quaternion.Euler(0f, 135f, 0f));
            InstantiateScenePrefab(trafficLight, root, "TrafficLight_SE", new Vector3(5.8f, 0f, -14.8f), Quaternion.Euler(0f, -45f, 0f));
            InstantiateScenePrefab(streetSign, root, "StreetSign_Konbini", new Vector3(-4.2f, 0f, -3.2f), Quaternion.Euler(0f, 160f, 0f));
            InstantiateScenePrefab(stopSign, root, "StopSign_Corner", new Vector3(4.9f, 0f, -4.2f), Quaternion.Euler(0f, -35f, 0f));

            InstantiateScenePrefab(bench, root, "Bench_Stop_North", new Vector3(8f, 0f, -3.45f), Quaternion.Euler(0f, 180f, 0f));
            InstantiateScenePrefab(bench, root, "Bench_Stop_South", new Vector3(-18f, 0f, -17.2f), Quaternion.identity);
            InstantiateScenePrefab(trash, root, "Trashcan_Stop_North", new Vector3(10.1f, 0f, -3.4f), Quaternion.identity);
            InstantiateScenePrefab(trash, root, "Trashcan_Stop_South", new Vector3(-15.8f, 0f, -17.2f), Quaternion.identity);

            for (int i = -5; i <= 5; i += 2)
            {
                InstantiateScenePrefab(electricityPole, root, $"ElectricPole_N_{i}", new Vector3(i * 10f, 0f, 12.2f), Quaternion.Euler(0f, 90f, 0f));
                InstantiateScenePrefab(electricityWires, root, $"ElectricWires_N_{i}", new Vector3(i * 10f + 5f, 3.8f, 12.2f), Quaternion.Euler(0f, 90f, 0f));
            }

            for (int i = 0; i < 11; i++)
            {
                InstantiateScenePrefab(fence, root, $"BackFence_N_{i}", new Vector3(-60f + i * 12f, 0f, 31.8f), Quaternion.identity);
                InstantiateScenePrefab(driveway, root, $"Driveway_S_{i}", new Vector3(-60f + i * 12f, 0.01f, -33.5f), Quaternion.identity);
            }

            Vector3[] parkTrees =
            {
                new Vector3(28f, 0f, -1f),
                new Vector3(34f, 0f, -4f),
                new Vector3(38f, 0f, -1f),
                new Vector3(-37f, 0f, -18f),
                new Vector3(-32f, 0f, -20f),
                new Vector3(-28f, 0f, -16f)
            };

            for (int i = 0; i < parkTrees.Length; i++)
            {
                InstantiateScenePrefab(i % 2 == 0 ? treeLarge : treeSmall, root, $"ParkTree_{i}", parkTrees[i], Quaternion.Euler(0f, i * 27f, 0f));
            }
        }

        private static void CreateStorefrontDetails(Transform root, GameObject pottedPlant, GameObject storeFridge, GameObject coffeeMachine, GameObject deliveryBox)
        {
            InstantiateScenePrefab(pottedPlant, root, "StorefrontPlant_L", new Vector3(-2.75f, 0f, -0.15f), Quaternion.identity);
            InstantiateScenePrefab(pottedPlant, root, "StorefrontPlant_R", new Vector3(2.75f, 0f, -0.15f), Quaternion.identity);
            InstantiateScenePrefab(storeFridge, root, "OutdoorDrinkFridge", new Vector3(4.2f, 0f, 0.8f), Quaternion.Euler(0f, 180f, 0f));
            InstantiateScenePrefab(coffeeMachine, root, "OutdoorCoffeeMachine", new Vector3(-4.2f, 0.55f, 0.75f), Quaternion.Euler(0f, 180f, 0f));

            for (int i = 0; i < 4; i++)
            {
                InstantiateScenePrefab(deliveryBox, root, $"DeliveryBox_{i}", new Vector3(-6.2f + i * 0.65f, 0f, 1.6f), Quaternion.Euler(0f, i * 17f, 0f));
            }
        }

        private static void AddStreetLight(Transform parent, GameObject prefab, string name, Vector3 position, Quaternion rotation, Vector3 localLampPosition, Quaternion localLampRotation)
        {
            var instance = InstantiateScenePrefab(prefab, parent, name, position, rotation);
            if (instance == null) return;

            var lightGo = new GameObject("WarmRoadLight");
            lightGo.transform.SetParent(instance.transform, false);
            lightGo.transform.localPosition = localLampPosition;
            lightGo.transform.localRotation = localLampRotation;

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(1f, 0.82f, 0.52f);
            light.intensity = 1.7f;
            light.range = 8.5f;
            light.spotAngle = 70f;
        }

        private static void CreateLaneLine(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            CreateBlock(parent, name + "_A", position + new Vector3(-2.4f, 0f, 0f), new Vector3(2.6f, 0.035f, 0.13f), new Color(0.92f, 0.92f, 0.86f), rotation);
            CreateBlock(parent, name + "_B", position + new Vector3(2.4f, 0f, 0f), new Vector3(2.6f, 0.035f, 0.13f), new Color(0.92f, 0.92f, 0.86f), rotation);
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 position, Vector3 scale, Color color, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.rotation = rotation ?? Quaternion.identity;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = CreateRuntimeMat(name + "_Mat", color);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject InstantiateScenePrefab(GameObject prefab, Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetParent(parent);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            AddBlockingFootprintIfNeeded(instance, name);
            return instance;
        }

        private static void AddBlockingFootprintIfNeeded(GameObject instance, string name)
        {
            if (name == "KonbiniStoreAsset")
            {
                AddKonbiniCollision(instance.transform);
                return;
            }

            if (!name.StartsWith("StreetBuilding_", System.StringComparison.Ordinal)) return;

            AddCollisionChild(instance.transform, "Collision_Footprint", new Vector3(0f, 2.3f, 0f), new Vector3(8.6f, 4.6f, 7.2f));
        }

        private static void AddKonbiniCollision(Transform root)
        {
            AddCollisionChild(root, "Collision_LeftWall", new Vector3(-3.9f, 2.1f, 0.2f), new Vector3(0.45f, 4.2f, 6.5f));
            AddCollisionChild(root, "Collision_RightWall", new Vector3(3.9f, 2.1f, 0.2f), new Vector3(0.45f, 4.2f, 6.5f));
            AddCollisionChild(root, "Collision_BackWall", new Vector3(0f, 2.1f, 3.1f), new Vector3(8.2f, 4.2f, 0.45f));
        }

        private static void AddCollisionChild(Transform parent, string name, Vector3 localPosition, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = false;
            collider.size = size;
        }

        private static Material CreateRuntimeMat(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard");
            return new Material(shader) { name = name, color = color };
        }
    }
}
