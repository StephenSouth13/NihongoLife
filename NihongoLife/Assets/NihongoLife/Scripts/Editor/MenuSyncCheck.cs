#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Renders 01_MainMenu exactly as it is saved (what the Editor shows before Play) to
    /// Bao_Cao/menu-sync/editor_view.png, so it can be compared with the Play capture of the same screen.
    /// Run: Unity -batchmode -executeMethod NihongoLife.EditorTools.MenuSyncCheck.CaptureEditorView
    /// </summary>
    public static class MenuSyncCheck
    {
        public const string ScenePath = "Assets/NihongoLife/Scenes/01_MainMenu.unity";

        public static void CaptureEditorView()
        {
            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Render(Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/menu-sync/editor_view.png")));
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        public static void Render(string file)
        {
            var camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            const int width = 1600, height = 900;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                if (!canvases[i].isRootCanvas || canvases[i].renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = camera.nearClipPlane + 0.05f;
            }
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var old = RenderTexture.active;
            RenderTexture.active = target;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllBytes(file, tex.EncodeToPNG());
            RenderTexture.active = old;
            camera.targetTexture = null;
            for (int i = 0; i < canvases.Length; i++) canvases[i].renderMode = modes[i];
            Debug.Log("[MenuSync] Editor view → " + file);
        }
    }
}
#endif
