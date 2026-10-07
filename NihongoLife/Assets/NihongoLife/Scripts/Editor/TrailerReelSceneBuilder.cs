#if UNITY_EDITOR
using System;
using NihongoLife.Trailer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// 99_Trailer now only boots the TrailerReel, which films the real game scenes. The old hand-built trailer
    /// set (ramen bowl, festival group, fake konbini door, TrailerDirector) is removed rather than kept beside it.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.TrailerReelSceneBuilder.Build
    /// </summary>
    public static class TrailerReelSceneBuilder
    {
        private const string ScenePath = "Assets/NihongoLife/Scenes/99_Trailer.unity";

        public static void Build()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                foreach (var root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
                var camera = new GameObject("BootCamera") { tag = "MainCamera" };
                var cam = camera.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                new GameObject("TrailerReel").AddComponent<TrailerReel>();
                RenderSettings.skybox = null;
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + ScenePath);
                Debug.Log("[TrailerReelSceneBuilder] 99_Trailer now plays the in-game TrailerReel.");
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
