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
        [Test]
        public void Scene_HasLandMarketAlleysHillsTourAndPeople()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var all = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();
                int Count(string prefix) => all.Count(t => t.name.StartsWith(prefix));
                var crowd = roots.SelectMany(r => r.GetComponentsInChildren<PhuQuocCrowd>(true)).Single();
                Assert.GreaterOrEqual(crowd.PeopleCount, 150, "lots of island life");
                Assert.GreaterOrEqual(crowd.Count(PhuQuocCrowd.Pose.Swim), 30, "swimmers in the sea and the pool");
                Assert.GreaterOrEqual(crowd.Count(PhuQuocCrowd.Pose.Lie), 15, "sunbathers");
                Assert.GreaterOrEqual(crowd.GroupCount, 20, "families and shoppers walking");
                Assert.AreEqual(0, crowd.GetComponentsInChildren<Collider>(true).Length, "people never block the player");
                for (int i = 0; i < crowd.PeopleCount; i++)
                    Assert.IsNotNull(crowd.Person(i).GetComponentInChildren<SkinnedMeshRenderer>(), "CC0 character model on " + crowd.Person(i).name);
                // Swimmers stand in the water (head above, body below the surface).
                for (int i = 0; i < crowd.PeopleCount; i++)
                    if (crowd.PoseOf(i) == PhuQuocCrowd.Pose.Swim) Assert.Less(crowd.Person(i).localPosition.y, 0f, crowd.Person(i).name);

                Assert.GreaterOrEqual(Count("SeafoodStall_"), 15, "seafood market stalls");
                Assert.GreaterOrEqual(Count("Vendor_"), 15, "a vendor at every stall");
                Assert.GreaterOrEqual(Count("AlleyHouse_"), 40, "hẻm houses");
                Assert.GreaterOrEqual(Count("PQC_Hill_"), 8, "hills");
                Assert.GreaterOrEqual(Count("Tree_"), 250, "dense forest");
                Assert.GreaterOrEqual(Count("Group_Promenade"), 3, "tourist families on the promenade");
                foreach (var staff in new[] { "Staff_Receptionist", "Staff_Chef", "Staff_Bartender", "Staff_ManagerMai", "Owner_ChuNam", "Staff_Waiter0" })
                    Assert.IsTrue(all.Any(t => t.name == staff), "Gánh Ngon: " + staff);
                Assert.IsTrue(all.Any(t => t.name.StartsWith("ResortSwimmer_")) && all.Any(t => t.name.StartsWith("PoolSwimmer_")) && all.Any(t => t.name == "Resort_Receptionist"), "people at the resort");

                // The resort stands on land: ground under the lawn's edges and behind it, not open sea.
                var lands = all.Where(t => t.name.StartsWith("Land_")).Select(t => t.GetComponent<Renderer>().bounds).ToList();
                var restaurant = all.First(t => t.name == "GanhNgon_Restaurant");
                foreach (var local in new[] { new Vector3(280f, 0f, 20f), new Vector3(205f, 0f, 70f), new Vector3(120f, 0f, 60f), new Vector3(0f, 0f, 60f) })
                {
                    var w = restaurant.TransformPoint(local);
                    Assert.IsTrue(lands.Any(b => w.x > b.min.x && w.x < b.max.x && w.z > b.min.z && w.z < b.max.z && b.max.y > restaurant.position.y - 0.2f), "land at " + local);
                }

                // Every one of the 31 map places has a ticket to a destination; places outside town have a way back.
                var map = PhuQuocMap.Load();
                var tickets = roots.SelectMany(r => r.GetComponentsInChildren<PhuQuocTourTicket>(true)).ToList();
                Assert.AreEqual(map.places.Length, tickets.Count);
                CollectionAssert.AreEquivalent(map.places.Select(p => p.id), tickets.Select(t => t.placeId));
                var returns = roots.SelectMany(r => r.GetComponentsInChildren<PhuQuocTourReturn>(true)).ToList();
                foreach (var t in tickets)
                {
                    Assert.IsNotNull(t.destination, t.placeId);
                    Assert.AreEqual(6, t.gameObject.layer, "interactable layer");
                    bool inTown = t.destination.IsChildOf(t.transform.parent);
                    if (!inTown) Assert.IsTrue(returns.Any(r => Vector3.Distance(r.transform.position, t.destination.position) < 10f && r.destination != null), "way back from " + t.placeId);
                }
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
