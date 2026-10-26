using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.UI;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Hibari Station's ticket gates, walked with the real CharacterController: without a ticket the flaps stay shut
    /// and the railing stops anyone going round; with a ticket the lane opens and leads onto the platform; from the
    /// platform the gates always let you out. The town's "road leads out" note never shows inside the station.
    /// Captures: Bao_Cao/station-regression (gate_*).
    /// </summary>
    public class StationGatePlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/station-regression"));

        [UnityTest]
        public IEnumerator Gates_TicketOpensTheLane_RailingStopsTheWayRound()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            var dm = DialogueManager.Instance;
            while (dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
            player.GetComponent<PlayerInventory>().AddYen(1000);
            GameObject.Find("AdditiveZonePortals/StationPortal").GetComponent<ScenePortal>().Interact(player.gameObject);
            yield return null;
            for (float t = 0f; t < 30f && flow.IsLoading; t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
            while (dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
            Assert.AreEqual(WorldLocationCatalog.StationScene, SceneManager.GetActiveScene().name);

            var station = Object.FindFirstObjectByType<StationTravelController>();
            var gate = Object.FindFirstObjectByType<TicketGate>();
            Assert.NotNull(gate, "The station has ticket gates");
            Assert.AreEqual(2, gate.Lanes.Count, "Two lanes");
            Assert.IsNull(SceneRefs.FindNamed("GateLeft"), "The old solid gate block is gone");
            Assert.NotNull(SceneRefs.FindNamed("GateScanner"), "The F prompt still has its scanner");
            var trainWalls = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(col => col.enabled && !col.isTrigger && col.name.Contains("HighSpeed_")).Select(col => col.name).ToList();
            Assert.IsEmpty(trainWalls, "Parked trains must not have colliders lying across the platform: " + string.Join(", ", trainWalls));
            Vector3 c = gate.transform.position;
            float laneW = c.x + gate.Lanes[0].x, laneE = c.x + gate.Lanes[1].x;

            // No ticket: the flaps stay shut, the way round is railed off.
            Assert.IsFalse(station.HasTicket);
            yield return Walk(player, new Vector3(laneW, c.y, c.z + 2.6f), -1f, 2.5f);
            Assert.Greater(player.transform.position.z, c.z, "Without a ticket the flaps stop you: " + player.transform.position);
            Assert.IsFalse(gate.IsOpen(0));
            Capture("gate_closed");
            yield return Walk(player, new Vector3(c.x - 4.5f, c.y, c.z + 2.6f), -1f, 2.5f);
            Assert.Greater(player.transform.position.z, c.z, "The railing stops the way round the gates");
            yield return Walk(player, new Vector3(c.x + 4.5f, c.y, c.z + 2.6f), -1f, 2.5f);
            Assert.Greater(player.transform.position.z, c.z, "The railing stops the way round on the other side too");

            // With a ticket: walking into the lane taps it, the flaps open, the lane leads onto the platform.
            Assert.IsTrue(station.PurchaseTicket("minato"));
            yield return Walk(player, new Vector3(laneW, c.y, c.z + 2.6f), -1f, 0.9f);
            Capture("gate_open");
            yield return Walk(player, player.transform.position, -1f, 2.5f);
            Assert.IsTrue(station.GatePassed, "Walking through taps the ticket");
            Assert.Less(player.transform.position.z, c.z - 1f, "Through the gate onto the platform: " + player.transform.position);

            // From the platform the gates always let you out.
            yield return Walk(player, new Vector3(laneE, c.y, c.z - 2.6f), 1f, 3f);
            Assert.Greater(player.transform.position.z, c.z + 1f, "Out through the gate from the platform: " + player.transform.position);

            // The town's edge note belongs to the town, not to the station.
            Assert.IsFalse(HudFeed.HasKey("town-edge"), "No 'this road leads out of Hibari-chō' note in the station");
        }

        /// <summary>Walks along Z (dir −1 = towards the platform) at walking speed, one step per frame.</summary>
        private static IEnumerator Walk(PlayerController player, Vector3 from, float dir, float seconds)
        {
            var cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            player.transform.SetPositionAndRotation(from + Vector3.up * 0.05f, Quaternion.Euler(0f, dir < 0 ? 180f : 0f, 0f));
            cc.enabled = true;
            yield return null;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                cc.Move(new Vector3(0f, -0.5f, dir * 2.4f) * Time.deltaTime);
                yield return null;
            }
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            const int width = 1600, height = 900;
            var rt = new RenderTexture(width, height, 24);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            Object.Destroy(tex);
            rt.Release();
        }
    }
}
