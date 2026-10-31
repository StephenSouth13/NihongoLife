using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NihongoLife.Core;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Frame rate in Phú Quốc (65_PhuQuocIsland) with its crowds: the player stands at busy spots (Gánh Ngon, the
    /// seafood market, a hẻm, the beach with swimmers, the resort pool, the promenade, the tour terminal), the camera
    /// follows as in play, vSync off; each spot is measured for 3 s after a 1 s warm-up. Writes phuquoc_fps.txt to
    /// NL_SHOTS_DIR (or the temp cache). Fails only when a spot averages under 20 fps on the machine running it.
    /// </summary>
    public class PhuQuocPerfPlayModeTests
    {
        static readonly (string name, Vector3 local, float yaw)[] Spots =
        {
            ("ganh_ngon", new Vector3(0f, 0.3f, -9f), 0f),
            ("seafood_market", new Vector3(-50f, 0.3f, 40f), 0f),
            ("hem_alley", new Vector3(44f, 0.3f, 39f), 0f),
            ("beach_swimmers", new Vector3(-10f, 0.4f, -60f), 180f),
            ("resort_pool", new Vector3(196f, 0.6f, 2f), 45f),
            ("promenade", new Vector3(-30f, 0.4f, -46f), 90f),
            ("tour_terminal", new Vector3(-12f, 0.4f, 40f), 0f),
        };

        [UnityTest]
        public IEnumerator PhuQuoc_FrameRateAtBusySpots()
        {
            LogAssert.ignoreFailingMessages = true;
            int vsync = QualitySettings.vSyncCount, target = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.PhuQuocIslandScene);
            PlayerController player = null;
            for (float t = 0f; t < 40f; t += Time.unscaledDeltaTime)
            {
                player = Object.FindFirstObjectByType<PlayerController>();
                var flow = Object.FindFirstObjectByType<SceneFlowController>();
                if (player != null && flow != null && !flow.IsLoading && SceneManager.GetActiveScene().name == WorldLocationCatalog.PhuQuocIslandScene) break;
                yield return null;
            }
            Assert.NotNull(player);
            var restaurant = GameObject.Find("GanhNgon_Restaurant").transform;
            var cc = player.GetComponent<CharacterController>();
            var report = new StringBuilder();
            report.AppendLine($"Phu Quoc frame rate ({SystemInfo.graphicsDeviceName}, {Screen.width}x{Screen.height}, {SystemInfo.graphicsDeviceType})");
            var averages = new List<float>();
            try
            {
                foreach (var (name, local, yaw) in Spots)
                {
                    if (cc != null) cc.enabled = false;
                    player.transform.SetPositionAndRotation(restaurant.TransformPoint(local), Quaternion.Euler(0f, yaw, 0f));
                    if (cc != null) cc.enabled = true;
                    yield return new WaitForSecondsRealtime(1f);
                    var frames = new List<float>();
                    float start = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - start < 3f)
                    {
                        yield return null;
                        frames.Add(Time.unscaledDeltaTime);
                    }
                    float avg = frames.Count / frames.Sum();
                    var sorted = frames.OrderByDescending(f => f).ToList();
                    float low = 1f / sorted.Take(Mathf.Max(1, frames.Count / 100)).Average();
                    averages.Add(avg);
                    report.AppendLine($"{name,-16} avg {avg,6:0.0} fps  1% low {low,6:0.0} fps  frames {frames.Count}");
                }
            }
            finally
            {
                QualitySettings.vSyncCount = vsync;
                Application.targetFrameRate = target;
            }
            report.AppendLine($"overall avg {averages.Average():0.0} fps, worst spot {averages.Min():0.0} fps");
            string dir = System.Environment.GetEnvironmentVariable("NL_SHOTS_DIR");
            if (string.IsNullOrEmpty(dir)) dir = Application.temporaryCachePath;
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "phuquoc_fps.txt"), report.ToString());
            Debug.Log("[PhuQuocPerf]\n" + report);
            Assert.GreaterOrEqual(averages.Min(), 20f, report.ToString());
        }
    }
}
