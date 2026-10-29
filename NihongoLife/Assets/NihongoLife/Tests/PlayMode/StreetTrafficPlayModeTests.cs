using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The rebuilt streets of Hibari-chō (CityStreetBuilder) in Play Mode: the old road-tile grid is gone, cars drive
    /// Sakura-dōri on the left, keep apart, never sit inside a building, stop at the line on red and for the player on
    /// the zebra crossing, the signal cycle reaches the walking phase; the player walks from the spawn across the
    /// crossing to the konbini door, and every zone portal is still reachable on foot from the street (no lot wall in the
    /// way). Captures at eye level and from above: Bao_Cao/street-regression.
    /// </summary>
    public class StreetTrafficPlayModeTests
    {
        private const string Folder = "street-regression";
        private PlayerController _player;

        [UnityTest]
        public IEnumerator Streets_TrafficCrossingAndViews()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 20f && (_player = Object.FindFirstObjectByType<PlayerController>()) == null; t += Time.unscaledDeltaTime) yield return null;
            // A player left weak by earlier tests faints at the first bump and wakes in the clinic (x ≈ 650): start healthy.
            var status = NihongoLife.Player.PlayerStatus.Instance;
            if (status != null) status.DebugSetNeeds(health: 100f, energy: 100f, hunger: 100f, thirst: 100f, sleepiness: 0f);
            yield return new WaitForSecondsRealtime(3f);
            NihongoLife.Dialogue.DialogueManager.Instance?.CancelDialogue();

            // Old grid removed, new streets present.
            var tile = GameObject.Find("Environment/CityTile_0").transform;
            Assert.IsFalse(tile.Cast<Transform>().Any(t => t.name.StartsWith("RoadGrid_") || t.name.StartsWith("Sidewalk_") || t.name.StartsWith("LaneLine_")), "The old road-tile grid is gone.");
            Assert.NotNull(tile.Find("Streetscape/Traffic"), "Sakura-dōri has traffic.");
            var traffic = tile.Find("Streetscape/Traffic").GetComponent<StreetTraffic>();
            Assert.GreaterOrEqual(traffic.Cars.Count, 6);

            // Cars move, keep to the left lanes and never overlap a building.
            var start = traffic.Cars.Select(c => c.body.position).ToList();
            traffic.ForcePhase(StreetTraffic.Phase.CarGreen);
            yield return new WaitForSecondsRealtime(2f);
            int moved = traffic.Cars.Where((c, i) => (c.body.position - start[i]).magnitude > 3f).Count();
            Assert.GreaterOrEqual(moved, 4, "Cars drive along the avenue.");
            foreach (var car in traffic.Cars)
            {
                float z = car.body.localPosition.z;
                Assert.AreEqual(car.direction > 0 ? -8.4f : -11.6f, z, 0.05f, "Left-hand traffic: eastbound north lane, westbound south lane.");
                var hits = Physics.OverlapBox(car.body.position + Vector3.up * 0.8f, new Vector3(0.8f, 0.5f, 0.8f), car.body.rotation, ~0, QueryTriggerInteraction.Ignore)
                    .Where(h => !h.transform.IsChildOf(car.body) && h.GetComponentInParent<PlayerController>() == null && h.name != "GameplayGround").ToList();
                Assert.IsEmpty(hits.Where(h => h.name.StartsWith("Collision_") || h.name == "Wall").Select(h => h.name), "A car is inside a building or wall.");
            }

            // No yard wall runs through a shop: the konbini (shell and its interior) holds no wall collider.
            foreach (var shopName in new[] { "Town_Konbini", "KonbiniStoreAsset" })
            {
                var shop = shopName == "Town_Konbini" ? GameObject.Find(shopName) : tile.Find(shopName)?.gameObject;
                if (shop == null) continue;
                var renderers = shop.GetComponentsInChildren<Renderer>();
                var box = renderers[0].bounds; foreach (var r in renderers) box.Encapsulate(r.bounds);
                box.Expand(new Vector3(-0.4f, 0f, -0.4f));
                var inside = tile.Find("Streetscape/WallColliders").GetComponentsInChildren<Collider>().Where(w => w.bounds.Intersects(box)).Select(w => w.bounds.center.ToString("0.0")).ToList();
                Assert.IsEmpty(inside, $"Yard walls run through {shopName}.");
            }

            // Red: cars stop at the line (the player waits far up the north sidewalk, so only the light counts).
            Teleport(new Vector3(40f, 0.08f, -3.5f), 0f);
            traffic.ForcePhase(StreetTraffic.Phase.WalkGreen);
            // Cars that can still stop in time (≥ 6 m before the line) must not cross it on red.
            // Stop lines 2 m before the zebra (x -1.3 eastbound, 8.3 westbound); distances are from the front bumper.
            float BumperToLine(StreetTraffic.Car c) => (c.direction > 0 ? -1.3f - c.body.localPosition.x : c.body.localPosition.x - 8.3f) - c.halfLength;
            var waiting = traffic.Cars.Where(c => BumperToLine(c) > 6f && BumperToLine(c) < 40f).ToList();
            yield return new WaitForSecondsRealtime(6f);
            foreach (var car in waiting)
            {
                float toLine = BumperToLine(car);
                Assert.Greater(toLine, -0.3f, $"{car.body.name} ran the red light (bumper {toLine:0.00} m past the line).");
                if (toLine < 2f) Assert.Less(car.speed, 0.3f, $"{car.body.name} waits at the line.");
            }
            Assert.IsNotEmpty(waiting, "Some car was approaching the crossing.");
            foreach (var car in traffic.Cars.Where(c => c.speed < 0.3f))
            {
                float x = car.body.localPosition.x;
                bool onZebra = x + car.halfLength > 0.7f && x - car.halfLength < 6.3f;
                Assert.IsFalse(onZebra, $"{car.body.name} waits on the zebra crossing (x {x:0.00}, half {car.halfLength:0.00}). Cars: "
                    + string.Join("; ", traffic.Cars.Select(o => $"{o.body.name} x{o.body.localPosition.x:0.0} d{o.direction} v{o.speed:0.0} h{o.halfLength:0.0}")));
            }
            CaptureFrom("01_crossing_walk_phase", new Vector3(3.5f, 1.6f, -19.5f), new Vector3(3.5f, 1.2f, 0f));
            CaptureFrom("07_crossing_red_from_above", new Vector3(3.5f, 9f, -24f), new Vector3(3.5f, 0f, -10f));

            // Player on the zebra: cars on green still stop.
            Teleport(new Vector3(3.5f, 0.08f, -10f), 0f);
            traffic.ForcePhase(StreetTraffic.Phase.CarGreen);
            yield return new WaitForSecondsRealtime(3f);
            foreach (var car in traffic.Cars)
            {
                float x = car.body.localPosition.x;
                // Bumper to the stop line (x -1.3 eastbound, 8.3 westbound): a car not yet at the line waits; one already
                // on the zebra clears it (it must not stop on the stripes) but never drives into the player.
                float bumperToLine = (car.direction > 0 ? -1.3f - x : x - 8.3f) - car.halfLength;
                if (bumperToLine > -0.2f && bumperToLine < 12f)
                    Assert.LessOrEqual(car.speed, StreetTraffic.Braking(Mathf.Max(0f, bumperToLine - StreetTraffic.LineClearance)) + 0.6f,
                        $"{car.body.name} is not braking to wait for the player on the crossing ({bumperToLine:0.0} m from the line).");
                bool touches = Mathf.Abs(x - 3.5f) < car.halfLength + 0.3f && Mathf.Abs(car.body.localPosition.z + 10f) < car.halfWidth + 0.3f;
                Assert.IsFalse(touches, $"{car.body.name} drove into the player.");
            }

            // On foot: spawn → across the zebra → konbini door; and every portal from the street.
            Teleport(new Vector3(3.5f, 0.08f, -15.5f), 0f);
            yield return Walk(new Vector3(3.5f, 0f, -4f), 8f);
            yield return Walk(new Vector3(0.5f, 0f, -1.2f), 6f);
            Assert.Less(Vector3.Distance(Flat(_player.transform.position), new Vector3(0.5f, 0f, -1.2f)), 1.2f, "The player crosses the avenue and reaches the konbini front.");
            foreach (var portal in GameObject.Find("AdditiveZonePortals").GetComponentsInChildren<Collider>().Where(c => c.isTrigger))
            {
                var p = portal.bounds.center;
                Vector3 street = new Vector3(p.x, 0f, p.z > -10f ? -4.5f : -15.5f);
                Teleport(street + Vector3.up * 0.08f, 0f);
                yield return Walk(new Vector3(p.x, 0f, p.z), 10f);
                float d = Vector3.Distance(Flat(_player.transform.position), Flat(portal.ClosestPoint(_player.transform.position)));
                Debug.Log($"[Streets] {portal.name}: {d:0.00} m from the trigger");
                Assert.Less(d, 1.6f, $"{portal.name} is reachable from the street.");
                Teleport(street + Vector3.up * 0.08f, 0f);
                yield return null;
            }

            // Views.
            Teleport(new Vector3(0f, 0.08f, -14f), 0f);
            yield return new WaitForSecondsRealtime(0.5f);
            CaptureFrom("02_spawn_to_konbini", new Vector3(-2f, 1.7f, -20f), new Vector3(2f, 1.5f, 2f));
            CaptureFrom("03_avenue_east", new Vector3(-30f, 1.7f, -4f), new Vector3(30f, 1f, -10f));
            CaptureFrom("04_avenue_west_low", new Vector3(30f, 0.9f, -15.8f), new Vector3(-30f, 1.5f, -9f));
            CaptureFrom("05_side_lane_tomare", new Vector3(3.5f, 1.7f, -40f), new Vector3(3.5f, 0.5f, -15f));
            CaptureFrom("06_back_alley", new Vector3(-40f, 1.7f, 14.4f), new Vector3(-10f, 1f, 14.4f));
            CaptureFrom("07_park", new Vector3(-48f, 6f, 20f), new Vector3(-40f, 0f, 40f));
            CaptureFrom("08_parking", new Vector3(30f, 7f, 20f), new Vector3(40f, 0f, 38f));
            CaptureFrom("09_overview", new Vector3(-28f, 32f, -48f), new Vector3(2f, 0f, -6f));
            CaptureFrom("10_vending_mirror", new Vector3(9f, 1.6f, -21f), new Vector3(5f, 1.2f, -16f));
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private IEnumerator Walk(Vector3 target, float timeout)
        {
            var body = _player.GetComponent<CharacterController>();
            for (float t = 0f; t < timeout; t += Time.deltaTime)
            {
                Vector3 to = Flat(target) - Flat(_player.transform.position);
                if (to.magnitude < 0.4f) yield break;
                body.Move(to.normalized * Mathf.Min(4f * Time.deltaTime, to.magnitude) + Vector3.down * 0.05f);
                yield return null;
            }
        }

        private void Teleport(Vector3 p, float yaw)
        {
            var body = _player.GetComponent<CharacterController>();
            body.enabled = false;
            _player.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, yaw, 0f));
            body.enabled = true;
            Physics.SyncTransforms();
        }

        private static void CaptureFrom(string name, Vector3 eye, Vector3 look)
        {
            var camera = Camera.main;
            if (camera == null) return;
            var follow = camera.GetComponent<NihongoLife.Cameras.ThirdPersonCameraController>();
            if (follow != null) follow.enabled = false;
            var t = camera.transform; Vector3 p = t.position; Quaternion r = t.rotation;
            t.position = eye; t.LookAt(look);
            const int w = 1600, h = 900;
            var rt = new RenderTexture(w, h, 24);
            camera.targetTexture = rt; camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/" + Folder));
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null;
            Object.Destroy(tex); rt.Release();
            t.SetPositionAndRotation(p, r);
            if (follow != null) follow.enabled = true;
        }
    }
}
