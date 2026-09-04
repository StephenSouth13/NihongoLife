using UnityEditor;
using UnityEngine;
using System.IO;

namespace NihongoLife.Editor
{
    public static class VisualEnvironmentBuilder
    {
        // Explicit paths based on verified Kenney layout
        private static readonly string PATH_ROAD = "Assets/ThirdParty/Kenney/kenney_city-kit-roads/Models/FBX format/road-straight.fbx";
        private static readonly string PATH_ROAD_SIDE = "Assets/ThirdParty/Kenney/kenney_city-kit-roads/Models/FBX format/road-side.fbx";
        private static readonly string PATH_CROSSING = "Assets/ThirdParty/Kenney/kenney_city-kit-roads/Models/FBX format/road-crossing.fbx";
        private static readonly string PATH_STREET_LIGHT = "Assets/ThirdParty/Kenney/kenney_city-kit-roads/Models/FBX format/light-curved.fbx";
        private static readonly string PATH_STREET_SIGN = "Assets/ThirdParty/Kenney/kenney_city-kit-roads/Models/FBX format/road-sign-street.fbx";
        private static readonly string PATH_BUILDING = "Assets/ThirdParty/Kenney/kenney_city-kit-suburban_20/Models/FBX format/building-type-a.fbx";
        private static readonly string PATH_BUILDING_B = "Assets/ThirdParty/Kenney/kenney_city-kit-suburban_20/Models/FBX format/building-type-b.fbx";
        private static readonly string PATH_BUILDING_C = "Assets/ThirdParty/Kenney/kenney_city-kit-suburban_20/Models/FBX format/building-type-c.fbx";
        private static readonly string PATH_BUILDING_D = "Assets/ThirdParty/Kenney/kenney_city-kit-suburban_20/Models/FBX format/building-type-d.fbx";
        private static readonly string PATH_TREE_SMALL = "Assets/ThirdParty/Kenney/kenney_city-kit-suburban_20/Models/FBX format/tree-small.fbx";
        private static readonly string PATH_PLANTER = "Assets/ThirdParty/Kenney/kenney_city-kit-suburban_20/Models/FBX format/planter.fbx";
        private static readonly string PATH_SHELF = "Assets/ThirdParty/Kenney/kenney_furniture-kit/Models/FBX format/bookcaseOpen.fbx";
        private static readonly string PATH_COUNTER = "Assets/ThirdParty/Kenney/kenney_furniture-kit/Models/FBX format/tableCoffee.fbx";
        private static readonly string PATH_BENCH = "Assets/ThirdParty/Kenney/kenney_furniture-kit/Models/FBX format/bench.fbx";
        private static readonly string PATH_TRASHCAN = "Assets/ThirdParty/Kenney/kenney_furniture-kit/Models/FBX format/trashcan.fbx";
        private static readonly string PATH_ONIGIRI = "Assets/ThirdParty/Kenney/kenney_food-kit/Models/FBX format/rice-ball.fbx";
        private static readonly string PATH_WATER = "Assets/ThirdParty/Kenney/kenney_food-kit/Models/FBX format/soda-bottle.fbx";

        private static string[] prefabFolders = new string[]
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

        private static void EnsureDirectories()
        {
            foreach (var folder in prefabFolders)
            {
                EnsureFolderExists(folder);
            }
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && parent != "Assets" && !AssetDatabase.IsValidFolder(parent))
                EnsureFolderExists(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
        }

        private static void GenerateWrapperPrefabs()
        {
            CreateWrapperDeterministic(PATH_ROAD, "Environment/Roads", "road_straight", 10f);
            CreateWrapperDeterministic(PATH_ROAD_SIDE, "Environment/Roads", "road_side", 10f);
            CreateWrapperDeterministic(PATH_CROSSING, "Environment/Roads", "road_crossing", 10f);
            CreateWrapperDeterministic(PATH_STREET_LIGHT, "Environment/Roads", "street_light", 3f, true);
            CreateWrapperDeterministic(PATH_STREET_SIGN, "Environment/Roads", "street_sign", 1.6f, true);
            CreateWrapperDeterministic(PATH_BUILDING, "Environment/Buildings", "building_a", 10f);
            CreateWrapperDeterministic(PATH_BUILDING_B, "Environment/Buildings", "building_b", 10f);
            CreateWrapperDeterministic(PATH_BUILDING_C, "Environment/Buildings", "building_c", 10f);
            CreateWrapperDeterministic(PATH_BUILDING_D, "Environment/Buildings", "building_d", 10f);
            CreateWrapperDeterministic(PATH_TREE_SMALL, "Environment/Buildings", "tree_small", 2.4f, true);
            CreateWrapperDeterministic(PATH_PLANTER, "Environment/Buildings", "planter", 1.2f);
            CreateWrapperDeterministic(PATH_SHELF, "Furniture", "shelf", 1.8f, true); // Target ~1.8m height
            CreateWrapperDeterministic(PATH_COUNTER, "Furniture", "counter", 2f); // Target ~2m width
            CreateWrapperDeterministic(PATH_BENCH, "Furniture", "bench", 1.8f);
            CreateWrapperDeterministic(PATH_TRASHCAN, "Furniture", "trashcan", 0.75f, true);
            CreateWrapperDeterministic(PATH_ONIGIRI, "Food", "food_apple", 0.15f); // Keep wrapper name food_apple for backwards compatibility, but it uses rice-ball
            CreateWrapperDeterministic(PATH_WATER, "Food", "food_bottle", 0.25f, true);
            
            AssetDatabase.SaveAssets();
        }

        private static void CreateWrapperDeterministic(string vendorModelPath, string folderSuffix, string name, float targetSize, bool useYAxis = false)
        {
            string prefabPath = $"Assets/NihongoLife/Prefabs/{folderSuffix}/{name}.prefab";

            GameObject vendorModel = AssetDatabase.LoadAssetAtPath<GameObject>(vendorModelPath);
            if (vendorModel == null)
            {
                Debug.LogError($"[VisualEnvironmentBuilder] Missing explicit asset at path: {vendorModelPath}. A primitive placeholder will be used instead.");
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
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var sharedMats = r.sharedMaterials;
                for (int i = 0; i < sharedMats.Length; i++)
                {
                    Material mat = sharedMats[i];
                    if (mat == null) continue;

                    if (mat.shader.name != "Universal Render Pipeline/Simple Lit" && mat.shader.name != "Universal Render Pipeline/Lit")
                    {
                        string sourcePath = AssetDatabase.GetAssetPath(mat);
                        string guid = AssetDatabase.AssetPathToGUID(sourcePath);
                        if (string.IsNullOrEmpty(guid)) guid = mat.name.Replace(" (Instance)", ""); // Fallback if it's built-in

                        string newMatPath = $"Assets/NihongoLife/Materials/Kenney/{guid}_URP.mat";
                        Material urpMat = AssetDatabase.LoadAssetAtPath<Material>(newMatPath);

                        if (urpMat == null)
                        {
                            urpMat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                            if (mat.HasProperty("_MainTex")) urpMat.mainTexture = mat.mainTexture;
                            if (mat.HasProperty("_BaseMap")) urpMat.mainTexture = mat.GetTexture("_BaseMap");
                            if (mat.HasProperty("_Color")) urpMat.color = mat.color;
                            if (mat.HasProperty("_BaseColor")) urpMat.color = mat.GetColor("_BaseColor");
                            
                            AssetDatabase.CreateAsset(urpMat, newMatPath);
                        }
                        
                        sharedMats[i] = urpMat;
                    }
                }
                r.sharedMaterials = sharedMats;
            }
        }

        private static void NormalizeScale(GameObject visual, float targetSize, bool useYAxis = false)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            // Center to base
            visual.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);

            // Recompute
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            float currentSize = useYAxis ? bounds.size.y : Mathf.Max(bounds.size.x, bounds.size.z);
            if (currentSize <= 0.001f || currentSize > 1000f)
            {
                Debug.LogWarning($"[VisualEnvironmentBuilder] Bounds for {visual.transform.parent.name} are abnormal ({currentSize}). Scale left at 1.");
                return;
            }

            float scaleFactor = targetSize / currentSize;
            visual.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            
            // Adjust local position based on scale
            visual.transform.localPosition = new Vector3(
                visual.transform.localPosition.x * scaleFactor,
                visual.transform.localPosition.y * scaleFactor,
                visual.transform.localPosition.z * scaleFactor
            );

            // Final check
            Bounds finalBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) finalBounds.Encapsulate(renderers[i].bounds);
            Debug.Log($"[VisualEnvironmentBuilder] Scaled {visual.transform.parent.name}. Target size: {targetSize}. Final bounds size: {finalBounds.size}.");
        }

        public static void AssembleKonbini(GameObject root)
        {
            // Clean up existing children if rebuilding
            while (root.transform.childCount > 0)
            {
                Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
            }

            GameObject roadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Environment/Roads/road_straight.prefab");
            GameObject roadSidePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Environment/Roads/road_side.prefab");
            GameObject crossingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Environment/Roads/road_crossing.prefab");
            GameObject streetLightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Environment/Roads/street_light.prefab");
            GameObject streetSignPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Environment/Roads/street_sign.prefab");
            GameObject buildingA = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Environment/Buildings/building_a.prefab");
            GameObject buildingB = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Environment/Buildings/building_b.prefab");
            GameObject buildingC = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Environment/Buildings/building_c.prefab");
            GameObject buildingD = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Environment/Buildings/building_d.prefab");
            GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Environment/Buildings/tree_small.prefab");
            GameObject planterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Environment/Buildings/planter.prefab");
            GameObject benchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Furniture/bench.prefab");
            GameObject trashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Furniture/trashcan.prefab");

            for (int i = -3; i <= 3; i++)
            {
                GameObject segmentPrefab = i == 0 && crossingPrefab != null ? crossingPrefab : roadPrefab;
                InstantiateScenePrefab(segmentPrefab, root.transform, $"Road_{i}", new Vector3(i * 10f, 0f, -10f), Quaternion.identity);
                InstantiateScenePrefab(roadSidePrefab, root.transform, $"NorthSidewalk_{i}", new Vector3(i * 10f, 0f, -4.7f), Quaternion.identity);
                InstantiateScenePrefab(roadSidePrefab, root.transform, $"SouthSidewalk_{i}", new Vector3(i * 10f, 0f, -15.3f), Quaternion.Euler(0f, 180f, 0f));
            }

            GameObject[] northBuildings = { buildingB, buildingC, buildingA, buildingD, buildingB };
            float[] northX = { -22f, -11f, 0f, 12f, 23f };
            for (int i = 0; i < northBuildings.Length; i++)
            {
                var go = InstantiateScenePrefab(northBuildings[i], root.transform, i == 2 ? "KonbiniStoreAsset" : $"StreetBuilding_N_{i}", new Vector3(northX[i], 0f, 4.8f), Quaternion.Euler(0f, 180f, 0f));
                if (go != null && i == 2)
                {
                    go.transform.localScale *= 0.82f;
                }
            }

            InstantiateScenePrefab(buildingC, root.transform, "StreetBuilding_S_0", new Vector3(-18f, 0f, -21.5f), Quaternion.identity);
            InstantiateScenePrefab(buildingD, root.transform, "StreetBuilding_S_1", new Vector3(18f, 0f, -21.5f), Quaternion.identity);

            for (int i = -2; i <= 2; i++)
            {
                InstantiateScenePrefab(streetLightPrefab, root.transform, $"StreetLight_N_{i}", new Vector3(i * 10f + 3f, 0f, -4.2f), Quaternion.Euler(0f, 180f, 0f));
                InstantiateScenePrefab(treePrefab, root.transform, $"Tree_N_{i}", new Vector3(i * 10f - 2f, 0f, -3.2f), Quaternion.identity);
                InstantiateScenePrefab(planterPrefab, root.transform, $"Planter_N_{i}", new Vector3(i * 10f + 1.4f, 0f, -2.8f), Quaternion.identity);
            }

            InstantiateScenePrefab(streetSignPrefab, root.transform, "StreetSign_Konbini", new Vector3(-4.2f, 0f, -3.2f), Quaternion.Euler(0f, 160f, 0f));
            InstantiateScenePrefab(benchPrefab, root.transform, "Bench_Stop", new Vector3(7.5f, 0f, -3.5f), Quaternion.Euler(0f, 180f, 0f));
            InstantiateScenePrefab(trashPrefab, root.transform, "Trashcan_Stop", new Vector3(9.6f, 0f, -3.4f), Quaternion.identity);
        }

        private static GameObject InstantiateScenePrefab(GameObject prefab, Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetParent(parent);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            return instance;
        }
    }
}
