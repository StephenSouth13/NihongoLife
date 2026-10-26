using System.IO;
using NihongoLife.Core;
using NihongoLife.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.EditorTools
{
    /// <summary>
    /// Batch: Unity -batchmode -projectPath . -executeMethod NihongoLife.EditorTools.StudyModesScreenshots.Capture
    /// Renders the Study Hub, 文型 patterns (browse + drill) and video page at 1920x1080 and 2400x1080 into
    /// Screenshots/learning-modes/.
    /// </summary>
    public static class StudyModesScreenshots
    {
        [MenuItem("NihongoLife/Debug/Capture study mode screenshots")]
        public static void Capture()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots", "learning-modes"));
            Directory.CreateDirectory(dir);
            foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(2400, 1080), new Vector2Int(1280, 960) })
            {
                Shot(dir, size, "hub", GameLanguage.Vietnamese, c => { var p = c.AddComponent<StudyHubPopup>(); p.Initialize(null); p.Show(); });
                Shot(dir, size, "patterns", GameLanguage.Vietnamese, c => { var p = c.AddComponent<JapanesePatternsUI>(); p.Initialize(null); p.Show(); });
                Shot(dir, size, "patterns-n4-en", GameLanguage.English, c => { var p = c.AddComponent<JapanesePatternsUI>(); p.Initialize(null); p.Show(); p.SelectLevel("N4"); });
                Shot(dir, size, "drill", GameLanguage.Vietnamese, c => { var p = c.AddComponent<JapanesePatternsUI>(); p.Initialize(null); p.Show(); p.StartDrill(); p.Answer(1); });
                Shot(dir, size, "drill-reorder-ja", GameLanguage.Japanese, c => { var p = c.AddComponent<JapanesePatternsUI>(); p.Initialize(null); p.Show(); p.StartDrill(); p.Answer(0); p.Next(); p.Answer(2); p.Next(); });
                Shot(dir, size, "video", GameLanguage.Vietnamese, c => { var p = c.AddComponent<InGameVideoPlayerUI>(); p.Initialize(null); p.Show(); });
            }
            Debug.Log("[StudyModesScreenshots] saved to " + dir);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void Shot(string dir, Vector2Int size, string name, GameLanguage lang, System.Action<GameObject> build)
        {
            var prev = Loc.Language;
            Loc.Set(lang);
            var camGo = new GameObject("ShotCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.12f, 0.16f, 0.2f);
            var rt = new RenderTexture(size.x, size.y, 24);
            cam.targetTexture = rt;
            var canvasGo = new GameObject("ShotCanvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 1f;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            try
            {
                build(canvasGo);
                for (int i = 0; i < 3; i++) { Canvas.ForceUpdateCanvases(); foreach (var f in canvasGo.GetComponentsInChildren<HudFitRect>(true)) f.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver); }
                // Finish pop-in animations instantly.
                foreach (var g in canvasGo.GetComponentsInChildren<CanvasGroup>(true)) g.alpha = 1f;
                foreach (var t in canvasGo.GetComponentsInChildren<RectTransform>(true)) if (t.localScale != Vector3.one && t.localScale.x > 0.5f) t.localScale = Vector3.one;
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, $"{name}-{size.x}x{size.y}.png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            catch (System.Exception e) { Debug.LogException(e); }
            finally
            {
                RenderTexture.active = null;
                Object.DestroyImmediate(canvasGo); Object.DestroyImmediate(camGo); rt.Release(); Object.DestroyImmediate(rt);
                Loc.Set(prev);
            }
        }
    }
}
