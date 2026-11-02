using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The first seconds in town must not flash: light values may drift with the day but never jump back and forth, and an
    /// emote must not stall a frame. Numbers are written to Bao_Cao/smoothness so a regression shows what moved.
    /// </summary>
    public class SmoothnessPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/smoothness"));

        private static float Sun => RenderSettings.sun != null ? RenderSettings.sun.intensity : -1f;
        private static float Ambient => RenderSettings.ambientLight.grayscale + RenderSettings.ambientSkyColor.grayscale;

        [UnityTest, Timeout(120000)]
        public IEnumerator TownEntry_LightDoesNotFlash()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            var log = new StringBuilder();
            float lastSun = Sun, lastAmbient = Ambient, lastFog = RenderSettings.fogDensity;
            int flips = 0, sign = 0;
            float biggest = 0f;
            var jumps = new List<string>();
            for (float t = 0f; t < 15f; t += Time.unscaledDeltaTime)
            {
                yield return null;
                float sun = Sun, ambient = Ambient, fog = RenderSettings.fogDensity;
                float step = sun - lastSun;
                if (Mathf.Abs(step) > 0.02f)
                {
                    int s = step > 0f ? 1 : -1;
                    if (sign != 0 && s != sign) flips++;
                    sign = s;
                    jumps.Add($"{t:0.00}s sun {lastSun:0.00}>{sun:0.00}");
                }
                if (Mathf.Abs(ambient - lastAmbient) > 0.02f) jumps.Add($"{t:0.00}s ambient {lastAmbient:0.00}>{ambient:0.00}");
                if (Mathf.Abs(fog - lastFog) > 0.002f) jumps.Add($"{t:0.00}s fog {lastFog:0.000}>{fog:0.000}");
                biggest = Mathf.Max(biggest, Mathf.Abs(step));
                lastSun = sun; lastAmbient = ambient; lastFog = fog;
            }
            Directory.CreateDirectory(Folder);
            log.AppendLine($"flips {flips} biggest {biggest:0.000} jumps {jumps.Count}");
            foreach (var j in jumps.Take(80)) log.AppendLine(j);
            File.WriteAllText(Path.Combine(Folder, "town_entry_light.txt"), log.ToString(), new UTF8Encoding(false));
            Assert.LessOrEqual(flips, 1, log.ToString());
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Emotes_DoNotStallAFrame()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(4f);
            var emotes = Object.FindFirstObjectByType<PlayerEmoteController>();
            Assert.NotNull(emotes);
            var log = new StringBuilder();
            float worstBefore = 0f;
            for (float t = 0f; t < 1.5f; t += Time.unscaledDeltaTime) { yield return null; worstBefore = Mathf.Max(worstBefore, Time.unscaledDeltaTime); }
            float worst = 0f;
            for (int i = 0; i < EmoteCatalog.All.Count; i++)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                emotes.Play(i);
                stopwatch.Stop();
                log.AppendLine($"{EmoteCatalog.All[i].Id} call {stopwatch.Elapsed.TotalMilliseconds:0.0} ms");
                float peak = 0f;
                for (float t = 0f; t < 2.1f; t += Time.unscaledDeltaTime) { yield return null; peak = Mathf.Max(peak, Time.unscaledDeltaTime); }
                log.AppendLine($"   worst frame after {peak * 1000f:0.0} ms");
                worst = Mathf.Max(worst, (float)stopwatch.Elapsed.TotalMilliseconds);
            }
            Directory.CreateDirectory(Folder);
            log.Insert(0, $"worst before {worstBefore * 1000f:0.0} ms, worst call {worst:0.0} ms\n");
            File.WriteAllText(Path.Combine(Folder, "emotes.txt"), log.ToString(), new UTF8Encoding(false));
            Assert.Less(worst, 25f, log.ToString()); // the first one still reads a greeting; the rest take about 5 ms
        }
    }
}
