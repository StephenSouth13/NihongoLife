using System.Collections;
using System.Collections.Generic;
using System.IO;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Gestures;
using NihongoLife.Health;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>A meal from the player's side: order, sit down, eat with chopsticks, stand up, pay. Frames go to Bao_Cao/sushi-flow.</summary>
    public class SushiFlowPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/sushi-flow"));

        private static IEnumerator Enter()
        {
            NihongoLife.Health.PlayerIllness.CureAll(); // an earlier test may leave the player ill
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(1.5f);
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            flow.TransferZone("30_SushiRestaurant", "sushi_entrance", "30_SushiRestaurant");
            for (float t = 0f; t < 25f && (flow.IsLoading || SceneManager.GetActiveScene().name != "30_SushiRestaurant"); t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(1f);
        }

        private static void Capture(string name)
        {
            Directory.CreateDirectory(Folder);
            var camera = Camera.main;
            if (camera == null) return;
            var rt = new RenderTexture(1280, 720, 24);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            Object.Destroy(rt);
            Object.Destroy(tex);
        }

        private static void Put(PlayerController player, Vector3 at, float yaw)
        {
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            body.enabled = true;
        }

        /// <summary>Ill and showing it every half second: the meal and the tray still win over the symptom.</summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator Ill_TheMealAndTheTrayComeFirst()
        {
            yield return Enter();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var gestures = GestureLayer.On(player.gameObject);
            PlayerIllness.CureAll();
            PlayerIllness.Catch("heatstroke", false);
            PlayerIllness.SymptomEvery = 0.5f;
            try
            {
                yield return new WaitForSecondsRealtime(1.2f);
                Assert.AreEqual("ill_heat", gestures.Current, "Idle, the illness shows");
                Put(player, new Vector3(500f, 0.1f, 1.6f), 0f);
                JobShift.Carry("dish", "maguro", "Maguro");
                yield return new WaitForSecondsRealtime(1.2f);
                Assert.AreEqual("carry_tray", gestures.Current, "The tray replaces the symptom");
                yield return new WaitForSecondsRealtime(1f);
                Assert.AreEqual("carry_tray", gestures.Current, "and stays");
                JobShift.DropCarry();
                yield return new WaitForSecondsRealtime(0.5f);

                RestaurantTable table = null;
                foreach (var t in Object.FindObjectsByType<RestaurantTable>(FindObjectsSortMode.None)) if (t.name.EndsWith("_A")) table = t;
                PlayerInventory.Instance.AddYen(5000, "test");
                var menu = Resources.Load<RestaurantMenuDefinition>("Restaurants/menu_sushi_hibari");
                Put(player, table.transform.position + new Vector3(0.9f, 0.1f, -2.2f), 0f);
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.IsTrue(table.TryPlaceOrder(menu, new List<KeyValuePair<string, int>> { new KeyValuePair<string, int>(menu.dishes[0].id, 2) }, out string error), error);
                for (float t = 0f; t < 15f && table.State != RestaurantTable.ServiceState.Served; t += Time.unscaledDeltaTime) yield return null;
                table.Interact(player.gameObject);
                for (float t = 0f; t < 3f; t += Time.unscaledDeltaTime)
                {
                    yield return null;
                    if (t > 0.3f) Assert.AreEqual("eat_rice", gestures.Current, "Eating at " + t);
                }
            }
            finally
            {
                PlayerIllness.SymptomEvery = null;
                PlayerIllness.CureAll();
            }
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Meal_SitEatStandAndPay()
        {
            yield return Enter();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var tables = Object.FindObjectsByType<RestaurantTable>(FindObjectsSortMode.None);
            Assert.IsNotEmpty(tables);
            RestaurantTable table = null;
            foreach (var t in tables) if (t.name.EndsWith("_A")) table = t;
            Assert.NotNull(table);
            PlayerInventory.Instance.AddYen(5000, "test");
            var menu = Resources.Load<RestaurantMenuDefinition>("Restaurants/menu_sushi_hibari");
            var lines = new List<KeyValuePair<string, int>> { new KeyValuePair<string, int>(menu.dishes[0].id, 2) };
            Put(player, table.transform.position + new Vector3(0.9f, 0.1f, -2.2f), 0f);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(table.TryPlaceOrder(menu, lines, out string error), error);
            for (float t = 0f; t < 15f && table.State != RestaurantTable.ServiceState.Served; t += Time.unscaledDeltaTime) yield return null;
            Assert.AreEqual(RestaurantTable.ServiceState.Served, table.State);
            Capture("01_served");

            Vector3 stood = player.transform.position;
            table.Interact(player.gameObject);
            yield return new WaitForSecondsRealtime(1.6f);
            Assert.AreEqual(RestaurantTable.ServiceState.Eating, table.State);
            Assert.AreEqual("eat_rice", GestureLayer.On(player.gameObject).Current, "Eating with chopsticks");
            Assert.Less(Vector2.Distance(new Vector2(player.transform.position.x, player.transform.position.z), new Vector2(table.transform.position.x, -3.75f)), 0.3f, "On the near chair");
            Capture("02_eating");

            for (float t = 0f; t < 12f && table.State != RestaurantTable.ServiceState.ReadyToPay; t += Time.unscaledDeltaTime) yield return null;
            Assert.AreEqual(RestaurantTable.ServiceState.ReadyToPay, table.State);
            yield return null;
            Assert.IsFalse(player.InputLocked, "Free again");
            Assert.IsTrue(player.enabled);
            Assert.Less(Vector3.Distance(player.transform.position, stood), 0.3f, "Back where the player stood");
            Capture("03_finished");

            int before = PlayerInventory.Instance.Yen;
            table.Interact(player.gameObject);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.Less(PlayerInventory.Instance.Yen, before, "The bill was paid");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Waiter_CarriesTheDishOnATrayWithBothHands()
        {
            yield return Enter();
            var player = Object.FindFirstObjectByType<PlayerController>();
            Put(player, new Vector3(500f, 0.1f, 1.6f), 0f);
            JobShift.Carry("dish", "maguro", "Maguro");
            yield return new WaitForSecondsRealtime(1.2f);
            Capture("04_tray_front");
            Assert.AreEqual("carry_tray", GestureLayer.On(player.gameObject).Current);
            Put(player, new Vector3(500f, 0.1f, 1.6f), 180f);
            yield return new WaitForSecondsRealtime(0.4f);
            Capture("05_tray_back");
            JobShift.DropCarry();
        }
    }
}
