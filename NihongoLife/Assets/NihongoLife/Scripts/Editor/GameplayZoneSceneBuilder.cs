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

namespace NihongoLife.EditorTools
{
    public static class GameplayZoneSceneBuilder
    {
        private const string SceneDir = "Assets/NihongoLife/Scenes";
        private const string CityScene = SceneDir + "/90_TestSandbox.unity";
        private const string StationScene = SceneDir + "/20_StationDistrict.unity";
        private const string SushiScene = SceneDir + "/30_SushiRestaurant.unity";
        private const string Sushi = "Assets/ThirdParty/Sushi Restaurant Kit - May 2023-20260920T035054Z-1-001";
        private const string Train = "Assets/ThirdParty/Train Pack - April 2019-20260920T035456Z-1-001";
        private const string House = "Assets/ThirdParty/Ultimate House Interior Pack - June 2020-20260920T035345Z-1-001";
        private const int InteractableLayer = 6;

        public static void BuildAll()
        {
            BuildStation();
            BuildSushiRestaurant();
            AddCityPortals();
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[GameplayZoneSceneBuilder] Built Station District and Sushi Restaurant additive zones.");
        }

        private static void BuildStation()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Vector3 origin = new Vector3(800f, 0f, 0f);
            var root = new GameObject("StationDistrict_Zone");
            root.AddComponent<SceneZoneVisibility>();

            CreateBlock(root.transform, "StationGround", origin + new Vector3(0f, -0.15f, 0f), new Vector3(34f, 0.3f, 22f), new Color(0.2f, 0.23f, 0.25f));
            CreateBlock(root.transform, "Platform", origin + new Vector3(0f, 0.15f, 2.8f), new Vector3(30f, 0.3f, 5f), new Color(0.62f, 0.6f, 0.54f));
            CreateBlock(root.transform, "SafetyLine", origin + new Vector3(0f, 0.33f, 0.55f), new Vector3(30f, 0.035f, 0.28f), new Color(0.95f, 0.72f, 0.12f));
            for (int i = -2; i <= 2; i++)
                Place(Train, "RailwayTrack_Straight", root.transform, $"Track_{i}", origin + new Vector3(i * 6f, 0f, -3.2f), new Vector3(6.2f, 0.28f, 2.1f), Quaternion.identity, true);
            Place(Train, "HighSpeed_Front", root.transform, "HighSpeed_Front", origin + new Vector3(-5f, 0.1f, -3.2f), new Vector3(7f, 2.5f, 2.4f), Quaternion.Euler(0f, 90f, 0f), true);
            Place(Train, "HighSpeed_Wagon", root.transform, "HighSpeed_Wagon_A", origin + new Vector3(2.2f, 0.1f, -3.2f), new Vector3(7f, 2.5f, 2.4f), Quaternion.Euler(0f, 90f, 0f), true);
            Place(Train, "HighSpeed_Wagon", root.transform, "HighSpeed_Wagon_B", origin + new Vector3(9.4f, 0.1f, -3.2f), new Vector3(7f, 2.5f, 2.4f), Quaternion.Euler(0f, 90f, 0f), true);
            Place(Sushi, "Environment_Bench", root.transform, "PlatformBench_A", origin + new Vector3(-6f, 0.3f, 4f), new Vector3(2.4f, 1.0f, 0.8f), Quaternion.Euler(0f, 180f, 0f), true);
            Place(Sushi, "Environment_Bench", root.transform, "PlatformBench_B", origin + new Vector3(3f, 0.3f, 4f), new Vector3(2.4f, 1.0f, 0.8f), Quaternion.Euler(0f, 180f, 0f), true);
            Place(Sushi, "Environment_Arch", root.transform, "TicketGateArch", origin + new Vector3(0f, 0.3f, 8.5f), new Vector3(4f, 3.3f, 1.2f), Quaternion.identity, true);
            CreateSign(root.transform, "駅前 / KHU NHÀ GA", origin + new Vector3(0f, 3.8f, 8.5f), new Vector2(8f, 0.9f));
            CreateLighting(root.transform, origin + new Vector3(0f, 6f, 2f));
            CreateSpawn(root.transform, "station_entrance", origin + new Vector3(0f, 0.38f, 6.8f), Quaternion.Euler(0f, 180f, 0f));
            CreateExitPortal(root.transform, "ExitToCity", StationScene, "city_station_return", "街へ戻る / Trở lại thành phố", origin + new Vector3(0f, 1.1f, 9.8f));

            EditorSceneManager.SaveScene(scene, StationScene);
        }

        private static void BuildSushiRestaurant()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Vector3 origin = new Vector3(500f, 0f, 0f);
            var root = new GameObject("SushiRestaurant_Zone");
            root.AddComponent<SceneZoneVisibility>();

            CreateBlock(root.transform, "Floor", origin + new Vector3(0f, -0.1f, 0f), new Vector3(16f, 0.2f, 18f), new Color(0.22f, 0.16f, 0.12f));
            CreateBlock(root.transform, "BackWall", origin + new Vector3(0f, 2.2f, 8.8f), new Vector3(16f, 4.4f, 0.25f), new Color(0.82f, 0.78f, 0.68f));
            CreateBlock(root.transform, "LeftWall", origin + new Vector3(-7.9f, 2.2f, 0f), new Vector3(0.25f, 4.4f, 18f), new Color(0.82f, 0.78f, 0.68f));
            CreateBlock(root.transform, "RightWall", origin + new Vector3(7.9f, 2.2f, 0f), new Vector3(0.25f, 4.4f, 18f), new Color(0.82f, 0.78f, 0.68f));

            for (int i = -2; i <= 2; i++)
            {
                Place(Sushi, "Environment_Counter_Straight", root.transform, $"SushiCounter_{i}", origin + new Vector3(i * 2.05f, 0f, 3.7f), new Vector3(2.1f, 1.05f, 0.9f), Quaternion.identity, true);
                Place(Sushi, "Environment_Stool", root.transform, $"CounterStool_{i}", origin + new Vector3(i * 2.05f, 0f, 2.2f), new Vector3(0.65f, 0.8f, 0.65f), Quaternion.Euler(0f, 180f, 0f), true);
            }
            Place(Sushi, "Environment_Fridge", root.transform, "KitchenFridge", origin + new Vector3(5.9f, 0f, 7.2f), new Vector3(1.8f, 2.5f, 1.3f), Quaternion.Euler(0f, 180f, 0f), true);
            Place(Sushi, "Environment_CuttingTable", root.transform, "PrepTable", origin + new Vector3(0f, 0f, 7.1f), new Vector3(3.4f, 1.0f, 1.4f), Quaternion.identity, true);
            Place(Sushi, "Decoration_Bamboo", root.transform, "Bamboo_L", origin + new Vector3(-6.6f, 0f, 7.4f), new Vector3(1f, 2.8f, 1f), Quaternion.identity, false);
            Place(Sushi, "Decoration_Bamboo", root.transform, "Bamboo_R", origin + new Vector3(6.6f, 0f, 7.4f), new Vector3(1f, 2.8f, 1f), Quaternion.identity, false);
            Place(Sushi, "Decoration_Painting", root.transform, "WallPainting", origin + new Vector3(0f, 1.8f, 8.55f), new Vector3(3.8f, 2f, 0.2f), Quaternion.Euler(0f, 180f, 0f), false);
            Place(House, "Light_Ceiling3", root.transform, "CeilingLight_A", origin + new Vector3(-3f, 3.8f, 1f), new Vector3(1f, 0.5f, 1f), Quaternion.identity, false);
            Place(House, "Light_Ceiling3", root.transform, "CeilingLight_B", origin + new Vector3(3f, 3.8f, 1f), new Vector3(1f, 0.5f, 1f), Quaternion.identity, false);

            string[] food = { "Food_Onigiri", "Food_EbiNigiri", "Food_MaguroNigiri", "Food_SalmonNigiri", "Food_TamagoNigiri", "Food_Roll" };
            for (int i = 0; i < food.Length; i++)
                Place(Sushi, food[i], root.transform, $"Menu_{food[i]}", origin + new Vector3(-3f + i * 1.2f, 1.12f, 3.5f), new Vector3(0.38f, 0.3f, 0.38f), Quaternion.identity, false);

            CreateSign(root.transform, "すし店 / NHÀ HÀNG SUSHI", origin + new Vector3(0f, 3.5f, -8.4f), new Vector2(8f, 0.9f));
            CreateLighting(root.transform, origin + new Vector3(0f, 5f, 1f));
            CreateSpawn(root.transform, "sushi_entrance", origin + new Vector3(0f, 0.25f, -6.6f), Quaternion.identity);
            CreateExitPortal(root.transform, "ExitToCity", SushiScene, "city_sushi_return", "街へ戻る / Trở lại thành phố", origin + new Vector3(0f, 1.1f, -8f));
            EditorSceneManager.SaveScene(scene, SushiScene);
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
            portal.AddComponent<ScenePortal>().Configure(Path.GetFileNameWithoutExtension(zoneScene), citySpawn, display, true);
        }

        private static void CreateSpawn(Transform parent, string id, Vector3 position, Quaternion rotation)
        {
            var spawn = new GameObject("Spawn_" + id);
            spawn.transform.SetParent(parent);
            spawn.transform.SetPositionAndRotation(position, rotation);
            spawn.AddComponent<SceneSpawnPoint>().Configure(id);
        }

        private static GameObject Place(string folder, string model, Transform parent, string name, Vector3 position, Vector3 targetSize, Quaternion rotation, bool collider)
        {
            string path = FindFbx(folder, model);
            if (string.IsNullOrEmpty(path)) { Debug.LogWarning($"Missing zone asset: {model}"); return null; }
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = name;
            instance.transform.SetParent(parent);
            instance.transform.SetPositionAndRotation(position, rotation);
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
