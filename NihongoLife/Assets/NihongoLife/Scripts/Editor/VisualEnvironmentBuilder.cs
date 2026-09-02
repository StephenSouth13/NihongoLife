using UnityEditor;
using UnityEngine;
using System.IO;

namespace NihongoLife.Editor
{
    public static class VisualEnvironmentBuilder
    {
        // Explicit paths based on verified Kenney layout
        private static readonly string PATH_ROAD = "Assets/ThirdParty/Kenney/kenney_city-kit-roads/Models/FBX format/road-straight.fbx";
        private static readonly string PATH_BUILDING = "Assets/ThirdParty/Kenney/kenney_city-kit-suburban_20/Models/FBX format/building-type-a.fbx";
        private static readonly string PATH_SHELF = "Assets/ThirdParty/Kenney/kenney_furniture-kit/Models/FBX format/bookcaseOpen.fbx";
        private static readonly string PATH_COUNTER = "Assets/ThirdParty/Kenney/kenney_furniture-kit/Models/FBX format/tableCoffee.fbx";
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
            CreateWrapperDeterministic(PATH_BUILDING, "Environment/Buildings", "building_a", 10f);
            CreateWrapperDeterministic(PATH_SHELF, "Furniture", "shelf", 1.8f, true); // Target ~1.8m height
            CreateWrapperDeterministic(PATH_COUNTER, "Furniture", "counter", 2f); // Target ~2m width
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
            GameObject buildingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NihongoLife/Prefabs/Environment/Buildings/building_a.prefab");

            // 1. Exterior Road
            if (roadPrefab != null)
            {
                for (int i = -1; i <= 1; i++) // Compact 3 segments
                {
                    var road = (GameObject)PrefabUtility.InstantiatePrefab(roadPrefab);
                    road.transform.SetParent(root.transform);
                    road.transform.position = new Vector3(i * 10, 0, -4);
                }
            }

            // 2. The Main Store Building
            if (buildingPrefab != null)
            {
                var store = (GameObject)PrefabUtility.InstantiatePrefab(buildingPrefab);
                store.transform.SetParent(root.transform);
                store.transform.position = new Vector3(0, 0, 6);
                store.transform.rotation = Quaternion.Euler(0, 180, 0); // Face the road/player
            }
        }
    }
}
