using System.Collections;
using System.IO;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>Train arrives, passengers get off, the queue gets on, the train leaves, a new queue forms. Captures: Bao_Cao/train/.</summary>
    public class TrainArrivalCyclePlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/train"));

        [UnityTest, Timeout(240000)]
        public IEnumerator Arrive_GetOff_GetOn_Depart()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            var dm = DialogueManager.Instance;
            while (dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
            GameObject.Find("AdditiveZonePortals/StationPortal").GetComponent<ScenePortal>().Interact(player.gameObject);
            yield return null;
            yield return WaitUntil(() => !flow.IsLoading && SceneManager.GetActiveScene().name == WorldLocationCatalog.StationScene, 30f, "Into the station");
            yield return new WaitForSecondsRealtime(2f);
            while (dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }

            var crowd = Object.FindFirstObjectByType<PlatformCrowd>();
            var station = Object.FindFirstObjectByType<StationTravelController>();
            Assert.NotNull(crowd); Assert.NotNull(station);
            var cam = Camera.main;
            var platform = GameObject.Find("Platform").transform.position;
            var eye = platform + new Vector3(-9f, 2.6f, 5.5f);
            cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(platform + new Vector3(2f, 0.6f, -1f) - eye));
            int alightedBefore = crowd.Alighted;
            yield return WaitUntil(() => station.IsTrainBoarding, 90f, "The train comes in");
            yield return WaitUntil(() => crowd.Alighting > 0, 10f, "Passengers step off");
            yield return new WaitForSeconds(0.8f);
            Capture(cam, "01_passengers_getting_off");
            yield return WaitUntil(() => crowd.Alighted >= alightedBefore + 3, 70f, "Everyone who got off reached the stairs", () => $"alighted={crowd.Alighted} alighting={crowd.Alighting} boarding={station.IsTrainBoarding} timeScale={Time.timeScale} {Dbg(crowd)}");
            yield return WaitUntil(() => crowd.Queued == 0, 15f, "The waiting queue boarded");
            Capture(cam, "02_queue_boarded");
            yield return WaitUntil(() => !station.IsTrainBoarding, 60f, "The train leaves");
            Capture(cam, "03_train_gone");
            yield return WaitUntil(() => crowd.Queued >= 1, 30f, "A new queue forms for the next train");
            Capture(cam, "04_next_queue");
        }

        private static string Dbg(PlatformCrowd crowd)
        {
            var stairs = (Vector3)typeof(PlatformCrowd).GetField("_stairs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(crowd);
            var sb = new System.Text.StringBuilder("stairs=" + stairs);
            foreach (var t in crowd.GetComponentsInChildren<Transform>()) if (t.name.StartsWith("PlatformPerson_Alighting")) sb.Append(" | " + t.name + "@" + t.position + " d=" + Vector3.Distance(new Vector3(t.position.x, 0, t.position.z), new Vector3(stairs.x, 0, stairs.z)).ToString("0.0"));
            return sb.ToString();
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float seconds, string message, System.Func<string> info = null)
        {
            float timeout = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.IsTrue(condition(), message + (info != null ? " [" + info() + "]" : ""));
        }

        private static void Capture(Camera camera, string name)
        {
            if (camera == null) return;
            const int width = 1600, height = 900;
            var rt = new RenderTexture(width, height, 24);
            var previous = camera.targetTexture;
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = previous;
            RenderTexture.active = null;
            Object.Destroy(tex);
            rt.Release();
        }
    }
}
