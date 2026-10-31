using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NihongoLife.Core;
using NihongoLife.Trailer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// A guest's very first game, as on a fresh device: the main menu starts the intro trailer, the whole reel plays
    /// (story, location gates and the guest session switched on, like a real first launch) and then the town loads.
    /// No error, exception or warning may be logged on the way. With NL_SHOTS_DIR set it also saves a frame each
    /// second (trailer_first_launch/) and the full log (intro_first_launch.txt).
    /// </summary>
    public class IntroTrailerFirstLaunchPlayModeTests
    {
        [UnityTest, Timeout(600000)]
        public IEnumerator FirstLaunch_IntroPlaysToTheEnd_WithoutErrors_ThenTheTownLoads()
        {
            LogAssert.ignoreFailingMessages = true;
            var problems = new List<string>();
            void OnLog(string message, string stack, LogType type)
            {
                if (type == LogType.Log || message.StartsWith("[SupabaseVoiceSync]")) return; // no online keys in a test checkout
                string firstFrame = (stack ?? "").Split('\n').FirstOrDefault(l => l.Trim().Length > 0) ?? "";
                lock (problems) problems.Add($"[{type}] {message}  @ {firstFrame.Trim()}");
            }

            bool storyBypass = NihongoLife.Scenario.StoryDirector.Bypass, unlockBypass = LocationUnlocks.Bypass, seen = IntroTrailer.Seen;
            string shots = Environment.GetEnvironmentVariable("NL_SHOTS_DIR");
            string frames = string.IsNullOrEmpty(shots) ? null : Path.Combine(shots, "trailer_first_launch");
            if (frames != null) { if (Directory.Exists(frames)) Directory.Delete(frames, true); Directory.CreateDirectory(frames); }
            bool entered = false;
            yield return SceneManager.LoadSceneAsync("01_MainMenu");
            yield return new WaitForSecondsRealtime(1.5f);

            Application.logMessageReceivedThreaded += OnLog;
            try
            {
                NihongoLife.Scenario.StoryDirector.Bypass = false;
                LocationUnlocks.Bypass = false;
                IntroTrailer.Bypass = false;
                IntroTrailer.Seen = false;
                PlayerSessionService.GetOrCreate()?.BeginOfflinePlay();
                Assert.IsTrue(IntroTrailer.PlayBefore(() =>
                {
                    entered = true;
                    if (GameServices.TryGet(out SceneFlowController flow)) flow.LoadScene(WorldLocationCatalog.CityScene);
                    else SceneContentLoader.LoadFallback(WorldLocationCatalog.CityScene);
                }), "A guest's first game plays the intro");

                TrailerReel reel = null;
                var shotLog = new StringBuilder();
                string lastShot = null;
                float nextFrame = 0f;
                int index = 0;
                var target = frames != null ? new RenderTexture(1280, 720, 24) : null;
                var image = frames != null ? new Texture2D(1280, 720, TextureFormat.RGB24, false) : null;
                float guard = Time.realtimeSinceStartup + 400f;
                while (!entered && Time.realtimeSinceStartup < guard)
                {
                    yield return null;
                    if (reel == null) reel = UnityEngine.Object.FindFirstObjectByType<TrailerReel>();
                    if (reel == null) continue;
                    if (reel.CurrentShot != lastShot) { lastShot = reel.CurrentShot; shotLog.AppendLine($"{Time.realtimeSinceStartup:F1}s {lastShot}"); }
                    if (frames == null || !reel.ShouldRecord || reel.ActiveCamera == null || reel.Elapsed < nextFrame) continue;
                    nextFrame = reel.Elapsed + 1f;
                    Capture(reel.ActiveCamera, target, image, Path.Combine(frames, $"t{index++:000}_{lastShot}.jpg"));
                }
                Assert.IsTrue(entered, "The reel ends and the game goes on");
                Assert.IsFalse(IntroTrailer.Playing);
                for (float t = 0f; t < 8f; t += Time.unscaledDeltaTime) yield return null; // the town after the intro
                if (frames != null)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(frames, "z_town_after.png"));
                    yield return null;
                    UnityEngine.Object.Destroy(image);
                    target.Release();
                }
                Assert.AreEqual(WorldLocationCatalog.CityScene, SceneManager.GetActiveScene().name, "The town loads after the intro");
                if (!string.IsNullOrEmpty(shots))
                    File.WriteAllText(Path.Combine(shots, "intro_first_launch.txt"), shotLog + "\n" + string.Join("\n", problems));
                Assert.IsEmpty(problems, "First launch logged:\n" + string.Join("\n", problems.Take(40)));
            }
            finally
            {
                Application.logMessageReceivedThreaded -= OnLog;
                if (IntroTrailer.Playing) IntroTrailer.Finish(continueGame: false);
                IntroTrailer.Bypass = true;
                IntroTrailer.Seen = seen;
                NihongoLife.Scenario.StoryDirector.Bypass = storyBypass;
                LocationUnlocks.Bypass = unlockBypass;
            }
        }

        private static void Capture(Camera camera, RenderTexture target, Texture2D image, string path)
        {
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).OrderBy(c => c.sortingOrder).ToArray();
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
            foreach (var c in canvases) c.renderMode = RenderMode.ScreenSpaceOverlay;
            var active = RenderTexture.active;
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            RenderTexture.active = active;
            File.WriteAllBytes(path, image.EncodeToJPG(85));
        }
    }
}
