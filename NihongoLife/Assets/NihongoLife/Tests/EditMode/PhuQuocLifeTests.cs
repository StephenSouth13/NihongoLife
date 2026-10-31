using System.Linq;
using NihongoLife.PhuQuoc;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NihongoLife.Tests
{
    public class PhuQuocLifeTests
    {
        private const string ScenePath = "Assets/NihongoLife/Scenes/65_PhuQuocIsland.unity";

        [Test]
        public void Signals_NeverGreenBothWays_AndCycle()
        {
            for (float t = 0f; t < TrafficRules.Cycle * 2f; t += 0.25f)
            {
                int ew = TrafficRules.Signal(t, TrafficRules.AxisEastWest), ns = TrafficRules.Signal(t, TrafficRules.AxisNorthSouth);
                Assert.IsFalse(ew != TrafficRules.Red && ns != TrafficRules.Red, "both axes moving at t=" + t);
            }
            Assert.AreEqual(TrafficRules.Green, TrafficRules.Signal(1f, TrafficRules.AxisEastWest));
            Assert.AreEqual(TrafficRules.Green, TrafficRules.Signal(TrafficRules.EwGreen + TrafficRules.YellowTime + TrafficRules.AllRed + 1f, TrafficRules.AxisNorthSouth));
        }

        [Test]
        public void CarFollowing_StopsBeforeTheGap()
        {
            Assert.AreEqual(0f, TrafficRules.SafeSpeed(9f, TrafficRules.MinGap));
            Assert.Less(TrafficRules.SafeSpeed(9f, 3f), TrafficRules.SafeSpeed(9f, 10f));
            Assert.AreEqual(9f, TrafficRules.SafeSpeed(9f, 100f));
            Assert.Less(TrafficRules.Approach(8f, 0f, 0.1f), 8f);
        }

        [Test]
        public void Guests_GoThroughTheWholeVisit()
        {
            var s = GanhNgonCustomers.State.Away;
            s = GanhNgonCustomers.Next(s, GanhNgonCustomers.AwayTime, false, false); Assert.AreEqual(GanhNgonCustomers.State.Arriving, s);
            s = GanhNgonCustomers.Next(s, 1f, false, true); Assert.AreEqual(GanhNgonCustomers.State.Ordering, s);
            s = GanhNgonCustomers.Next(s, GanhNgonCustomers.OrderTime, false, false); Assert.AreEqual(GanhNgonCustomers.State.Waiting, s);
            Assert.AreEqual(GanhNgonCustomers.State.Waiting, GanhNgonCustomers.Next(s, 1f, false, false));
            s = GanhNgonCustomers.Next(s, 1f, true, false); Assert.AreEqual(GanhNgonCustomers.State.Eating, s, "served by the player");
            Assert.AreEqual(GanhNgonCustomers.State.Eating, GanhNgonCustomers.Next(GanhNgonCustomers.State.Waiting, GanhNgonCustomers.StaffServeAfter, false, false), "staff serve eventually");
            s = GanhNgonCustomers.Next(s, GanhNgonCustomers.EatTime, false, false); Assert.AreEqual(GanhNgonCustomers.State.Leaving, s);
            s = GanhNgonCustomers.Next(s, 1f, false, true); Assert.AreEqual(GanhNgonCustomers.State.Away, s);
            Assert.GreaterOrEqual(GanhNgonCustomers.Orders.Length, 10);
        }

        [Test]
        public void Scene_HasTrafficGuestsPlacesAndBeach()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var root = scene.GetRootGameObjects();
                var traffic = root.SelectMany(r => r.GetComponentsInChildren<PhuQuocTraffic>(true)).Single();
                Assert.GreaterOrEqual(traffic.VehicleCount, 30);
                var names = traffic.GetComponentsInChildren<Transform>(true).Select(t => t.name).ToList();
                Assert.IsTrue(names.Any(n => n.StartsWith("Bus_")), "a bus");
                Assert.GreaterOrEqual(names.Count(n => n.StartsWith("scooter_") || n.StartsWith("motorbike_")), 12, "motorbikes");
                Assert.GreaterOrEqual(names.Count(n => n.StartsWith("Signal_")), 8, "traffic lights");

                var guests = root.SelectMany(r => r.GetComponentsInChildren<GanhNgonCustomers>(true)).Single();
                Assert.AreEqual(8, guests.SeatCount);
                var sky = root.SelectMany(r => r.GetComponentsInChildren<PhuQuocSkybox>(true)).Single();
                Assert.IsNotNull(sky.Sky, "PQ_Skybox is used in play");
                Assert.Less(PhuQuocSkybox.ExposureFor(1.1f, 0f), PhuQuocSkybox.ExposureFor(1.1f, 1f), "night sky is darker");

                var places = root.SelectMany(r => r.GetComponentsInChildren<PhuQuocPlaceTag>(true)).Select(p => p.nameVi).ToList();
                foreach (var kind in new[] { "Khách sạn", "Nhà nghỉ", "Homestay", "tiện lợi", "Cà phê", "Chợ", "Nhà thuốc", "Bến xe buýt" })
                    Assert.IsTrue(places.Any(p => p.Contains(kind)), "missing " + kind);
                var board = root.SelectMany(r => r.GetComponentsInChildren<TextMeshPro>(true)).First(t => t.name == "Destinations").text;
                foreach (var p in PhuQuocMap.Load().places) StringAssert.Contains(p.nameVi, board);

                var all = root.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();
                Assert.GreaterOrEqual(all.Count(t => t.name.StartsWith("seashell_")), 40, "shells on the beach");
                Assert.GreaterOrEqual(all.Count(t => t.name.StartsWith("starfish_")), 15);
                Assert.GreaterOrEqual(all.Count(t => t.name.StartsWith("Palm_")), 20);
                Assert.GreaterOrEqual(all.Count(t => t.name.StartsWith("Stall_") && t.parent.name == "ChoDem_DinhCau"), 6, "night market");
                var sea = all.First(t => t.name == "PQ_SeaLocal").GetComponent<Renderer>().sharedMaterial;
                Assert.AreEqual("NihongoLife/SeaWater", sea.shader.name);
                Assert.IsTrue(all.Any(t => t.name.StartsWith("Resort_")) && all.Any(t => t.name == "DuongDong_Resort"), "resorts");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
