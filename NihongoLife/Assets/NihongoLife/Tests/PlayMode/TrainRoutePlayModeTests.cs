using System.Collections;
using System.IO;
using System.Linq;
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
    /// <summary>
    /// The whole line, Hibari → the airport, and what the passenger sees: each leg its own view out of the window
    /// (houses → the school → the harbour → the bridge to Midori → the airport), the platform with the station's name
    /// board stopping at the window at each stop, and the flight to Phú Quốc at the end of the line.
    /// Captures: Bao_Cao/train-route.
    /// </summary>
    public class TrainRoutePlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/train-route"));

        [UnityTest]
        public IEnumerator Line_ToTheAirport_ShowsEachLegAndFliesToPhuQuoc()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            var dm = DialogueManager.Instance;
            while (dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
            player.GetComponent<PlayerInventory>().AddYen(3000);
            GameObject.Find("AdditiveZonePortals/StationPortal").GetComponent<ScenePortal>().Interact(player.gameObject);
            yield return null;
            yield return WaitUntil(() => !flow.IsLoading && SceneManager.GetActiveScene().name == WorldLocationCatalog.StationScene, 30f, "Into the station");
            yield return new WaitForSecondsRealtime(1.5f);
            while (dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
            var station = Object.FindFirstObjectByType<StationTravelController>();
            Assert.AreEqual("airport", StationTravelController.Line.Last().Id, "The line ends at the airport");
            Assert.AreEqual(WorldLocationCatalog.PhuQuocIslandScene, StationTravelController.Line.Last().Scene, "…which flies to Phú Quốc");

            station.Execute(StationAction.BuyTicket, player.gameObject);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsTrue(station.PurchaseTicket("airport"), "A ticket to the airport");
            station.Execute(StationAction.PassGate, player.gameObject);
            yield return WaitUntil(() => station.IsTrainBoarding, 25f, "The train comes in");
            Capture(Camera.main, "00_platform_waiting");
            station.Execute(StationAction.BoardTrain, player.gameObject);
            yield return new WaitForSecondsRealtime(0.5f);
            var scenery = station.CarriageInterior.GetComponentInChildren<TrainWindowScenery>(true);
            Assert.NotNull(scenery);
            Assert.AreEqual("hibari", scenery.ThemeId, "Leaving Hibari: the town");
            Assert.IsTrue(scenery.PlatformVisible, "Standing at Hibari's platform");
            Capture(station.WindowCamera, "01_hibari_platform");

            // Leaving Hibari: its houses and paddies. Each leg then tunnels into the next station's surroundings.
            yield return new WaitForSeconds(2.5f);
            Capture(station.WindowCamera, "02_leaving_hibari");
            string[] views = { "hibari", "gakuen", "minato", "midori", "airport" };
            for (int leg = 0; leg < 4; leg++)
            {
                var stop = StationTravelController.Line[leg + 1];
                yield return WaitUntil(() => scenery.ThemeId == views[leg + 1] && !scenery.InTunnel, 20f, "Approaching " + stop.Id);
                yield return new WaitForSeconds(1.2f);
                Capture(station.WindowCamera, $"{leg * 2 + 3:00}_approaching_{stop.Id}");
                // At the stop: the platform has slid in and stopped at the window, its board names the station.
                yield return WaitUntil(() => scenery.PlatformVisible && scenery.CurrentSpeed < 0.05f, 15f, "Stopped at " + stop.Id);
                Assert.That(Mathf.Abs(scenery.PlatformX), Is.LessThan(6f), "The platform stops at the window");
                Assert.IsTrue(scenery.GetComponentsInChildren<TMPro.TextMeshPro>(true).Any(t => t.text.Contains(stop.Ja)), "The board names " + stop.Ja);
                Capture(station.WindowCamera, $"{leg * 2 + 4:00}_stop_{stop.Id}");
            }
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == WorldLocationCatalog.PhuQuocIslandScene && !flow.IsLoading, 60f, "The airport ticket flies to Phú Quốc");
            yield return new WaitForSecondsRealtime(1.5f);
            Capture(Camera.main, "11_arrived_phuquoc");
        }

        [UnityTest]
        public IEnumerator Hall_GuideBoardAtm_AndThePlatformQueueBoards()
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

            // The route map board names every stop; the ATM opens the bank.
            var guide = GameObject.Find("StationGuide");
            Assert.NotNull(guide, "StationGuideBuilder's board is in the hall");
            var texts = guide.GetComponentsInChildren<TMPro.TextMeshPro>(true);
            foreach (var stop in StationTravelController.Line)
                Assert.IsTrue(texts.Any(t => t.text.Contains(stop.Ja)), "The board shows " + stop.Ja);
            var cam = Camera.main;
            cam.transform.SetPositionAndRotation(guide.transform.position - guide.transform.forward * 3.4f + Vector3.up * 1.7f, Quaternion.LookRotation(guide.transform.forward));
            Capture(cam, "20_hall_route_board");
            var atm = Object.FindFirstObjectByType<NihongoLife.Bank.BankAtm>();
            Assert.NotNull(atm, "A bank ATM in the hall");
            atm.Interact(player.gameObject);
            yield return null;
            Assert.IsTrue(NihongoLife.UI.BankUI.IsOpen, "The ATM opens the bank");
            NihongoLife.UI.BankUI.Close();

            // The platform: two on the benches, a queue at the boarding marks that boards the train.
            var crowd = Object.FindFirstObjectByType<PlatformCrowd>();
            Assert.NotNull(crowd);
            Assert.AreEqual(2, crowd.Seated, "Two people sit on the benches");
            Assert.GreaterOrEqual(crowd.Queued, 2, "People wait in line");
            Assert.IsFalse(GameObject.Find("Passenger_Waiting_A") != null && GameObject.Find("Passenger_Waiting_A").activeInHierarchy, "No figure sitting in the air");
            foreach (var seated in crowd.GetComponentsInChildren<SeatedPassenger>())
            {
                var hips = seated.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Hips);
                Assert.That(hips.position.y - seated.transform.position.y, Is.InRange(0.25f, 0.85f), seated.name + " sits on the bench");
            }
            var platform = GameObject.Find("Platform").transform.position;
            cam.transform.SetPositionAndRotation(platform + new Vector3(-9f, 2.6f, 5.5f), Quaternion.LookRotation(platform + new Vector3(2f, 0.6f, -1f) - (platform + new Vector3(-9f, 2.6f, 5.5f))));
            Capture(cam, "21_platform_queue");
            var station = Object.FindFirstObjectByType<StationTravelController>();
            yield return WaitUntil(() => station.IsTrainBoarding, 70f, "The train comes in");
            yield return new WaitForSeconds(5f);
            Assert.AreEqual(0, crowd.Queued, "The queue boarded");
            Capture(cam, "22_platform_after_boarding");
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float seconds, string message)
        {
            float timeout = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.IsTrue(condition(), message);
        }

        private static void Capture(Camera camera, string name)
        {
            if (camera == null) return;
            const int width = 1600, height = 900;
            var rt = new RenderTexture(width, height, 24);
            var previous = camera.targetTexture;
            bool enabled = camera.enabled;
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = previous;
            camera.enabled = enabled;
            RenderTexture.active = null;
            Object.Destroy(tex);
            rt.Release();
        }
    }
}
