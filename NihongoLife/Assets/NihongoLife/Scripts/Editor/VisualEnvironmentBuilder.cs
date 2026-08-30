using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace NihongoLife.Editor
{
    public static class VisualEnvironmentBuilder
    {
        private static string[] prefabFolders = new string[]
        {
            "Assets/NihongoLife/Prefabs/Environment/Roads",
            "Assets/NihongoLife/Prefabs/Environment/Buildings",
            "Assets/NihongoLife/Prefabs/Environment/Konbini",
            "Assets/NihongoLife/Prefabs/Furniture",
            "Assets/NihongoLife/Prefabs/Food",
            "Assets/NihongoLife/Prefabs/Props"
        };

        // This is called by SceneBuilder.cs
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
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                    string child = Path.GetFileName(folder);
                    AssetDatabase.CreateFolder(parent, child);
                }
            }
        }

        private static void GenerateWrapperPrefabs()
        {
            // Just create some necessary wrappers based on exact known filenames if they exist
            CreateWrapperFallback("road_straight", "Roads", "road-straight");
            CreateWrapperFallback("building_a", "Buildings", "building-type-a");
            CreateWrapperFallback("shelf", "Furniture", "bookcaseOpen"); 
            CreateWrapperFallback("counter", "Furniture", "tableCoffee");
            CreateWrapperFallback("food_apple", "Food", "apple");
            CreateWrapperFallback("food_bottle", "Food", "bottle-water");
            
            AssetDatabase.SaveAssets();
        }

        private static void CreateWrapperFallback(string customName, string folderSuffix, string searchName)
        {
            string[] guids = AssetDatabase.FindAssets(searchName + " t:Model", new[] { "Assets/ThirdParty/Kenney" });
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                CreateWrapper(path, folderSuffix, customName);
            }
        }

        private static void CreateWrapper(string vendorModelPath, string folderSuffix, string overrideName = null)
        {
            string name = overrideName ?? Path.GetFileNameWithoutExtension(vendorModelPath);
            string prefabPath = $"Assets/NihongoLife/Prefabs/Environment/{folderSuffix}";
            if (folderSuffix == "Furniture" || folderSuffix == "Food" || folderSuffix == "Props")
            {
                prefabPath = $"Assets/NihongoLife/Prefabs/{folderSuffix}";
            }
            
            string fullPath = $"{prefabPath}/{name}.prefab";

            if (!File.Exists(fullPath))
            {
                GameObject vendorModel = AssetDatabase.LoadAssetAtPath<GameObject>(vendorModelPath);
                if (vendorModel != null)
                {
                    GameObject root = new GameObject(name);
                    GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(vendorModel);
                    visual.name = "Visual";
                    visual.transform.SetParent(root.transform, false);
                    
                    PrefabUtility.SaveAsPrefabAsset(root, fullPath);
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static void AssembleKonbini(GameObject root)
        {
            // Clean up existing environment visual placeholders if they exist
            // (Assuming the root passed in is "Environment" object in SceneBuilder)

            GameObject roadPrefab = LoadPrefab("Roads", "road_straight");
            GameObject buildingPrefab = LoadPrefab("Buildings", "building_a");
            GameObject shelfPrefab = LoadPrefab("Furniture", "shelf"); 
            GameObject counterPrefab = LoadPrefab("Furniture", "counter");

            // 1. Exterior Road
            if (roadPrefab != null)
            {
                for (int i = -3; i <= 3; i++)
                {
                    var road = (GameObject)PrefabUtility.InstantiatePrefab(roadPrefab);
                    road.transform.SetParent(root.transform);
                    road.transform.position = new Vector3(i * 10, 0, -15);
                }
            }

            // 2. Background buildings
            if (buildingPrefab != null)
            {
                var bldg1 = (GameObject)PrefabUtility.InstantiatePrefab(buildingPrefab);
                bldg1.transform.SetParent(root.transform);
                bldg1.transform.position = new Vector3(-15, 0, -12);

                var bldg2 = (GameObject)PrefabUtility.InstantiatePrefab(buildingPrefab);
                bldg2.transform.SetParent(root.transform);
                bldg2.transform.position = new Vector3(15, 0, -12);
            }

            // 3. Interior Walls/Floor
            var konbiniFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            konbiniFloor.name = "KonbiniFloor";
            konbiniFloor.transform.SetParent(root.transform);
            konbiniFloor.transform.position = new Vector3(0, -0.1f, 0);
            konbiniFloor.transform.localScale = new Vector3(20, 0.2f, 20);
            konbiniFloor.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Simple Lit")) { color = Color.white };

            // Exterior walls
            CreateWall(root, new Vector3(-10, 2.5f, 0), new Vector3(0.5f, 5, 20)); // Left
            CreateWall(root, new Vector3(10, 2.5f, 0), new Vector3(0.5f, 5, 20));  // Right
            CreateWall(root, new Vector3(0, 2.5f, 10), new Vector3(20, 5, 0.5f));  // Back
            
            // Signage
            CreateSignage(root, "コンビニ", new Vector3(0, 4, -9.5f));
            CreateSignage(root, "入口 (Entrance)", new Vector3(-3, 2, -9.5f));
            CreateSignage(root, "出口 (Exit)", new Vector3(3, 2, -9.5f));
        }

        private static void CreateWall(GameObject root, Vector3 pos, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(root.transform);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Simple Lit")) { color = new Color(0.9f, 0.9f, 0.9f) };
        }

        private static void CreateSignage(GameObject root, string text, Vector3 pos)
        {
            var sign = new GameObject("Sign_" + text);
            sign.transform.SetParent(root.transform);
            sign.transform.position = pos;
            var tm = sign.AddComponent<TextMesh>();
            tm.text = text;
            tm.characterSize = 0.5f;
            tm.color = Color.black;
            tm.anchor = TextAnchor.MiddleCenter;
        }

        private static GameObject LoadPrefab(string folder, string name)
        {
            string path = $"Assets/NihongoLife/Prefabs/Environment/{folder}/{name}.prefab";
            if (folder == "Furniture" || folder == "Food" || folder == "Props")
            {
                path = $"Assets/NihongoLife/Prefabs/{folder}/{name}.prefab";
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
    }
}
