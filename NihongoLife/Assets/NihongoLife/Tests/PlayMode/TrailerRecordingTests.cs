using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Trailer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Records the in-game trailer at a fixed 30 fps (1920×1080 JPG frames) into the temp folder, for ffmpeg to
    /// turn into Bao_Cao/NihongoLife_Trailer.mp4. Explicit: run it on purpose with
    /// -testFilter Trailer_RecordFrames. The run also proves the whole reel plays without runtime errors.
    /// </summary>
    public class TrailerRecordingTests
    {
        public static string FrameFolder => Path.Combine(Path.GetTempPath(), "NihongoLifeTrailerFrames");

        [UnityTest, Explicit, Timeout(1800000)]
        public IEnumerator Trailer_RecordFrames()
        {
            if (Directory.Exists(FrameFolder)) Directory.Delete(FrameFolder, true);
            Directory.CreateDirectory(FrameFolder);
            Time.captureFramerate = 30;
            var host = new GameObject("TrailerReel_Recording");
            var reel = host.AddComponent<TrailerReel>();
            reel.ReturnToMenuWhenDone = false;

            const int width = 1920, height = 1080;
            var target = new RenderTexture(width, height, 24);
            var frame = new Texture2D(width, height, TextureFormat.RGB24, false);
            int index = 0;
            float guard = Time.realtimeSinceStartup + 1500f;
            while (!reel.Finished && Time.realtimeSinceStartup < guard)
            {
                yield return null;
                if (!reel.ShouldRecord || reel.ActiveCamera == null) continue;
                var camera = reel.ActiveCamera;
                var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                    .Where(c => c.isRootCanvas && (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera))
                    .OrderBy(c => c.sortingOrder).ToArray();
                for (int i = 0; i < canvases.Length; i++)
                {
                    canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                    canvases[i].worldCamera = camera;
                    canvases[i].planeDistance = camera.nearClipPlane + 0.05f + 0.002f * (canvases.Length - i);
                }
                Canvas.ForceUpdateCanvases();
                var old = camera.targetTexture;
                camera.targetTexture = target;
                camera.Render();
                camera.targetTexture = old;
                var active = RenderTexture.active;
                RenderTexture.active = target;
                frame.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                frame.Apply();
                RenderTexture.active = active;
                File.WriteAllBytes(Path.Combine(FrameFolder, $"f{index++:00000}.jpg"), frame.EncodeToJPG(92));
            }
            Time.captureFramerate = 0;
            Object.Destroy(frame);
            target.Release();
            Debug.Log($"[TrailerRecording] {index} frames → {FrameFolder}");
            Assert.IsTrue(reel.Finished, "The trailer must play to the end.");
            Assert.Greater(index, 30 * 55, "Expected roughly a minute of footage.");
        }
    }
}
