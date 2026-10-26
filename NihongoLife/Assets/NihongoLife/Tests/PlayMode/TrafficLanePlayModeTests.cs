using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Every traffic car on every town tile stays in its lane for a full minute of driving (light cycles, queues,
    /// the drive-out / rejoin at the street ends): never on a sidewalk, a shop front or a lot, never inside another
    /// car. Logs the first offenders with what they were doing.
    /// </summary>
    public class TrafficLanePlayModeTests
    {
        [UnityTest]
        public IEnumerator Traffic_StaysInLanes_ForAMinute()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(1f);
            var systems = Object.FindObjectsByType<StreetTraffic>(FindObjectsSortMode.None);
            Assert.IsNotEmpty(systems);
            Time.timeScale = 4f; // a minute of traffic in 15 s
            var offenders = new List<string>();
            try
            {
                for (float t = 0f; t < 15f; t += Time.unscaledDeltaTime)
                {
                    foreach (var traffic in systems)
                        foreach (var car in traffic.Cars)
                        {
                            if (car.body == null) continue;
                            Vector3 local = traffic.transform.InverseTransformPoint(car.body.position);
                            float laneZ = car.direction > 0 ? -8.4f : -11.6f;
                            bool offLane = Mathf.Abs(local.z - laneZ) > 0.3f || Mathf.Abs(car.body.position.y - traffic.transform.position.y) > 0.3f;
                            float yaw = car.body.eulerAngles.y;
                            bool turned = Mathf.Abs(Mathf.DeltaAngle(yaw, car.direction > 0 ? 90f : 270f)) > 3f;
                            if ((offLane || turned) && offenders.Count < 20)
                                offenders.Add($"{traffic.transform.parent?.parent?.name}/{car.body.name} local ({local.x:0.0}, {local.y:0.0}, {local.z:0.0}) yaw {yaw:0} speed {car.speed:0.0}");
                        }
                    yield return null;
                }
            }
            finally { Time.timeScale = 1f; }
            foreach (var o in offenders) Debug.Log("[Lanes] " + o);
            Assert.IsEmpty(offenders, "Cars left their lanes.");
        }

        /// <summary>
        /// A player standing in a lane in front of a moving car: the car brakes / bumps, the player stays on the ground
        /// next to the road (never lifted onto the car or a roof), and the car stays in its lane.
        /// </summary>
        [UnityTest]
        public IEnumerator CarHitsPlayer_PlayerStaysOnGround_CarStaysInLane()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(1f);
            var players = Object.FindObjectsByType<NihongoLife.Player.PlayerController>(FindObjectsSortMode.None);
            Debug.Log("[Lanes] players: " + string.Join(", ", players.Select(p => $"{p.name} tag {p.tag} enabled {p.enabled} at {p.transform.position}")));
            var player = players.First(p => p.enabled && p.CompareTag("Player"));
            var traffic = GameObject.Find("Environment/CityTile_0").transform.Find("Streetscape/Traffic").GetComponent<StreetTraffic>();
            traffic.ForcePhase(StreetTraffic.Phase.CarGreen);
            var car = traffic.Cars.Where(c => c.direction > 0).OrderBy(c => c.body.localPosition.x).Last(c => c.body.localPosition.x < 20f);
            Vector3 carLocal = car.body.localPosition;
            // Steps into the eastbound lane 0.6 m in front of a moving car (too late for it to brake).
            Vector3 spot = traffic.transform.TransformPoint(new Vector3(carLocal.x + car.halfLength + 0.6f, 0.08f, -8.4f));
            var body = player.GetComponent<CharacterController>();
            body.enabled = false; player.transform.position = spot; body.enabled = true;
            Physics.SyncTransforms();
            car.speed = car.cruise;
            float maxPlayerY = 0f, maxCarOff = 0f;
            for (float t = 0f; t < 4f; t += Time.deltaTime)
            {
                maxPlayerY = Mathf.Max(maxPlayerY, player.transform.position.y);
                var local = traffic.transform.InverseTransformPoint(car.body.position);
                maxCarOff = Mathf.Max(maxCarOff, Mathf.Abs(local.z + 8.4f), Mathf.Abs(local.y));
                yield return null;
            }
            Debug.Log($"[Lanes] hit: player max y {maxPlayerY:0.00}, car max off-lane {maxCarOff:0.00}");
            Assert.Less(maxCarOff, 0.05f, "The car stays in its lane.");
            Assert.AreEqual(Vector3.zero, traffic.transform.localPosition, "The Traffic root never moves.");
            Assert.Less(maxPlayerY, 1.2f, "The player is not lifted onto the car or a roof.");
        }

        /// <summary>Standing on a car's roof (how players used to end up riding cars onto roofs): set down beside it.</summary>
        [UnityTest]
        public IEnumerator PlayerOnCarRoof_IsSetDownBesideIt()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(1f);
            var players = Object.FindObjectsByType<NihongoLife.Player.PlayerController>(FindObjectsSortMode.None);
            Debug.Log("[Lanes] players: " + string.Join(", ", players.Select(p => $"{p.name} tag {p.tag} enabled {p.enabled} at {p.transform.position}")));
            var player = players.First(p => p.enabled && p.CompareTag("Player"));
            var body = player.GetComponent<CharacterController>();
            var traffic = GameObject.Find("Environment/CityTile_0").transform.Find("Streetscape/Traffic").GetComponent<StreetTraffic>();
            var roofCar = traffic.Cars.Where(c => c.direction < 0).OrderBy(c => Mathf.Abs(c.body.localPosition.x - 20f)).First(); // inside the town
            body.enabled = false; player.transform.position = roofCar.body.position + Vector3.up * 1.6f; body.enabled = true;
            Physics.SyncTransforms();
            Debug.Log($"[Lanes] roof teleport to {player.transform.position}, car world {roofCar.body.position}, traffic root {traffic.transform.position} rot {traffic.transform.eulerAngles} scale {traffic.transform.lossyScale} parent {traffic.transform.parent.name}@{traffic.transform.parent.position} name {traffic.name} id {traffic.GetInstanceID()}");
            Debug.Log($"[Lanes] roof inv check: {traffic.transform.InverseTransformPoint(roofCar.body.position)} vs car local {roofCar.body.localPosition} carParent {roofCar.body.parent.name} same {roofCar.body.parent == traffic.transform}");
            for (int f = 0; f < 10; f++)
            {
                yield return null;
                var q = traffic.transform.InverseTransformPoint(player.transform.position);
                Debug.Log($"[Lanes] roof f{f}: world {player.transform.position} local ({q.x:0.00}, {q.y:0.00}, {q.z:0.00}) trafficRoot {traffic.transform.position} parent {traffic.transform.parent?.name} playerParent {player.transform.parent?.name}");
            }
            var p = traffic.transform.InverseTransformPoint(player.transform.position);
            var cl = roofCar.body.localPosition;
            bool onCar = Mathf.Abs(p.x - cl.x) < roofCar.halfLength && Mathf.Abs(p.z - cl.z) < roofCar.halfWidth;
            Debug.Log($"[Lanes] roof: player local ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}) car ({cl.x:0.0}, {cl.z:0.0})");
            Assert.IsFalse(onCar, "The player is set down beside the car, not left riding it.");
            Assert.AreEqual(Vector3.zero, traffic.transform.localPosition, "The Traffic root never moves (a honk used to drag it — and every car — to the honking car).");
            Assert.Less(player.transform.position.y, 1.2f);
        }
    }
}
