using System.Collections;
using System.Diagnostics;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace NihongoLife.Tests
{
    /// <summary>Frame cost in the city with each online/social piece switched off in turn (diagnostics, Explicit).</summary>
    public class PerfProbePlayModeTests
    {
        [UnityTest, Explicit]
        public IEnumerator FrameCost_OnlinePieces()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(4f);
            yield return Measure("all on (1)");
            yield return Measure("all on (2)");
            yield return Measure("all on (3)");
            var pieces = new Behaviour[]
            {
                Object.FindFirstObjectByType<NihongoLife.Interaction.InteractionHighlight>(),
                Object.FindFirstObjectByType<SocialChatWindow>(),
                Object.FindFirstObjectByType<NihongoLife.Player.RemotePlayerManager>(),
                Object.FindFirstObjectByType<SocialHub>(),
                Object.FindFirstObjectByType<SupabaseOnlineWorldService>(),
                Object.FindFirstObjectByType<SupabaseRealtimeClient>(),
                Object.FindFirstObjectByType<NihongoLife.Interaction.InteractionDetector>(),
            };
            foreach (var piece in pieces.Where(p => p != null))
            {
                piece.enabled = false;
                yield return Measure("without " + piece.GetType().Name);
                piece.enabled = true;
            }
            var packages = NihongoLife.Exam.Ielts.IeltsLibrary.Discover().Where(p => p.Test.skill == "reading").Take(2).ToList();
            foreach (var package in packages)
            {
                var watch = Stopwatch.StartNew();
                var ui = IeltsTestUI.Open(package, IeltsTestUI.PracticeMode, false);
                Debug.Log($"[Perf] open {package.Id}: {watch.ElapsedMilliseconds} ms");
                yield return Measure("IELTS open");
                for (int part = 0; part < ui.Test.parts.Length; part++)
                {
                    watch.Restart();
                    ui.ShowPart(part);
                    Debug.Log($"[Perf] part {part}: {watch.ElapsedMilliseconds} ms");
                    yield return null;
                }
                ui.Close();
                yield return null;
            }
        }

        private static IEnumerator Measure(string label)
        {
            yield return null;
            int frames = 0;
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 2f) { frames++; yield return null; }
            Debug.Log($"[Perf] {label}: {2000f / frames:0.0} ms/frame ({frames} frames)");
        }
    }
}
