#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Removes the facade accents an old VisualEnvironmentBuilder pass put on every StreetBuilding_* (trim bar,
    /// two glowing window plates, door mat). They sat at a fixed offset instead of on the wall, so they floated
    /// over yards and the street — the red / cream blocks seen at dusk. Both copies of the town are cleaned.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.CityFacadeAccentCleanup.Run
    /// </summary>
    public static class CityFacadeAccentCleanup
    {
        private static readonly string[] Scenes =
        {
            "Assets/NihongoLife/Scenes/90_TestSandbox.unity",
            "Assets/NihongoLife/Scenes/01_MainMenu.unity",
        };

        public static readonly HashSet<string> AccentNames = new HashSet<string> { "FacadeTrim", "WarmWindow_L", "WarmWindow_R", "DoorMat" };

        public static void Run()
        {
            try
            {
                foreach (var path in Scenes)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var doomed = scene.GetRootGameObjects()
                        .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                        .Where(t => AccentNames.Contains(t.name) && t.parent != null && t.parent.name.StartsWith("StreetBuilding_", StringComparison.Ordinal))
                        .Select(t => t.gameObject).ToList();
                    foreach (var go in doomed) Object.DestroyImmediate(go);
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + path);
                    Debug.Log($"[CityFacadeAccentCleanup] {path}: removed {doomed.Count} floating facade pieces.");
                }
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }
    }
}
#endif
