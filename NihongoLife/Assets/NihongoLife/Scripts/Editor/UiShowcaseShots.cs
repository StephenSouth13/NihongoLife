using System;
using System.IO;
using NihongoLife.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.EditorTools
{
    /// <summary>Batch-mode screenshots of the polished UI: -executeMethod NihongoLife.EditorTools.UiShowcaseShots.Capture
    /// (output folder: env NL_SHOTS_DIR, default Temp/ui_shots).</summary>
    public static class UiShowcaseShots
    {
        const float W = 1920f, H = 1080f;

        public static void Capture()
        {
            string dir = Environment.GetEnvironmentVariable("NL_SHOTS_DIR");
            if (string.IsNullOrEmpty(dir)) dir = Path.Combine(Application.dataPath, "../Temp/ui_shots");
            Directory.CreateDirectory(dir);
            int code = 0;
            try
            {
                Shot(dir, "01_learning_plan_popup.png", c => { var p = c.AddComponent<LearningPlanPopup>(); p.Initialize(null); p.Show(); });
                Shot(dir, "02_study_hub_popup.png", c => { var p = c.AddComponent<StudyHubPopup>(); p.Initialize(null); p.Show(); });
                Shot(dir, "03_video_player_loading.png", c =>
                {
                    var p = c.AddComponent<InGameVideoPlayerUI>(); p.Initialize(null); p.Show();
                    if (p.LoadingStatus != null) p.LoadingStatus.Set(true, 0.35f);
                });
                Shot(dir, "04_loading_screen.png", c =>
                {
                    var card = c.AddComponent<LoadingCard>(); card.Build(NLUi.ResolveFont());
                    card.Show("02_CityTown", "Hibari Town"); card.Fill.fillAmount = 0.62f; card.Status.text = "Preparing the area...";
                });
            }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        static string GetPath(Transform t) => t.parent == null ? t.name : GetPath(t.parent) + "/" + t.name;

        static void Shot(string dir, string file, Action<GameObject> build)
        {
            const int layer = 31;
            var rt = new RenderTexture((int)W, (int)H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var camGo = new GameObject("ShotCam");
            var canvasGo = new GameObject("ShotCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                camGo.transform.position = new Vector3(0f, -9000f, -10f);
                var cam = camGo.AddComponent<Camera>();
                cam.enabled = false; cam.cullingMask = 1 << layer; cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.16f, 0.2f, 0.26f); cam.orthographic = true; cam.orthographicSize = H / 2f; cam.aspect = W / H;
                cam.nearClipPlane = 0.1f; cam.farClipPlane = 100f; cam.targetTexture = rt;
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = cam;
                var crt = (RectTransform)canvasGo.transform;
                crt.position = new Vector3(0f, -9000f, 0f); crt.sizeDelta = new Vector2(W, H);
                canvasGo.layer = layer;
                build(canvasGo);
                foreach (var t in canvasGo.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
                for (int pass = 0; pass < 2; pass++)
                {
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvasGo.transform);
                }
                Canvas.ForceUpdateCanvases();
                // The pop-in animation only advances in Play Mode: render the settled state.
                foreach (var cg in canvasGo.GetComponentsInChildren<CanvasGroup>(true)) cg.alpha = 1f;
                foreach (var t in canvasGo.GetComponentsInChildren<RectTransform>(true)) if (t.name == "Card") t.localScale = Vector3.one;
                var prev = RenderTexture.active; RenderTexture.active = rt;
                cam.Render();
                var tex = new Texture2D((int)W, (int)H, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(dir, file), tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                Debug.Log("[UiShowcaseShots] " + file);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canvasGo); UnityEngine.Object.DestroyImmediate(camGo);
                rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            }
        }
    }
}
