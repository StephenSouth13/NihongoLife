using System;
using System.Linq;
using NihongoLife.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Makes the sea and the pools swimmable without rebuilding the islands: each water gets its WaterSurface and loses any
    /// collider, and Midori's invisible shoreline walls go, because the beach now leads into the water.
    /// IslandBuilder and PhuQuocIslandBuilder call <see cref="Prepare"/> on the water they build.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.WaterSurfaceBuilder.Build
    /// </summary>
    public static class WaterSurfaceBuilder
    {
        private const string Scenes = "Assets/NihongoLife/Scenes/";

        public static void Prepare(GameObject water)
        {
            foreach (var solid in water.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(solid);
            if (water.GetComponent<WaterSurface>() == null) water.AddComponent<WaterSurface>();
        }

        public static void Build()
        {
            try
            {
                foreach (string name in new[] { "60_MidoriIsland", "65_PhuQuocIsland" })
                {
                    var scene = EditorSceneManager.OpenScene(Scenes + name + ".unity", OpenSceneMode.Single);
                    var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();
                    int waters = 0;
                    foreach (var t in all.Where(t => (t.name == "Sea" || t.name == "PQ_SeaLocal" || t.name == "PoolWater") && t.GetComponent<Renderer>() != null))
                    {
                        Prepare(t.gameObject);
                        waters++;
                    }
                    if (waters == 0) throw new InvalidOperationException("No water in " + name);
                    int walls = 0;
                    foreach (var t in all.Where(t => t.name.StartsWith("Shore_")).ToList())
                    {
                        UnityEngine.Object.DestroyImmediate(t.gameObject);
                        walls++;
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + name);
                    Debug.Log($"[WaterSurfaceBuilder] {name}: {waters} waters, {walls} shore walls removed.");
                }
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }
    }
}
