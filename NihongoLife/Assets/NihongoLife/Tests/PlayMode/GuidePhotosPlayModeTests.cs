using System.Collections;
using System.IO;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Takes the photos of the location guide (map › Cẩm nang): each place rendered from the game camera, world only
    /// (no HUD, no dialogue), saved to Resources/Guide/{TYPE}.jpg. Explicit: run it when places change.
    /// </summary>
    public class GuidePhotosPlayModeTests
    {
        private static string Dir => Path.Combine(Application.dataPath, "NihongoLife/Resources/Guide");

        [UnityTest, Explicit, Timeout(400000)]
        public IEnumerator Shoot_AllPlaces()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return Settle();
            Shoot("KONBINI");

            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            var zones = new (string type, string scene, string spawn, string back)[]
            {
                ("STATION", WorldLocationCatalog.StationScene, WorldLocationCatalog.StationEntrance, WorldLocationCatalog.CityStationReturn),
                ("SUSHI", WorldLocationCatalog.SushiRestaurantScene, WorldLocationCatalog.SushiEntrance, WorldLocationCatalog.CitySushiReturn),
                ("SCHOOL", WorldLocationCatalog.SchoolScene, WorldLocationCatalog.SchoolEntrance, WorldLocationCatalog.CitySchoolReturn),
                ("HOME", WorldLocationCatalog.HomeBedroomScene, WorldLocationCatalog.HomeBedroomEntrance, WorldLocationCatalog.CityHomeBedroomReturn),
                ("ARCADE", WorldLocationCatalog.GameCenterScene, WorldLocationCatalog.GameCenterEntrance, WorldLocationCatalog.CityGameCenterReturn),
            };
            foreach (var z in zones)
            {
                flow.EnterZone(z.scene, z.spawn, z.type);
                for (float t = 0f; t < 25f && (SceneManager.GetActiveScene().name != z.scene || flow.IsLoading); t += Time.unscaledDeltaTime) yield return null;
                Assert.AreEqual(z.scene, SceneManager.GetActiveScene().name, z.type);
                yield return Settle();
                Shoot(z.type);
                if (z.type == "STATION")
                {
                    flow.TransferZone(WorldLocationCatalog.MidoriIslandScene, WorldLocationCatalog.MidoriStation, "MIDORI");
                    for (float t = 0f; t < 25f && (SceneManager.GetActiveScene().name != WorldLocationCatalog.MidoriIslandScene || flow.IsLoading); t += Time.unscaledDeltaTime) yield return null;
                    yield return Settle();
                    Shoot("MIDORI");
                    flow.ExitZone(WorldLocationCatalog.MidoriIslandScene, z.back);
                }
                else flow.ExitZone(z.scene, z.back);
                for (float t = 0f; t < 25f && (SceneManager.GetActiveScene().name != WorldLocationCatalog.CityScene || flow.IsLoading); t += Time.unscaledDeltaTime) yield return null;
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        private static IEnumerator Settle()
        {
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            Object.FindFirstObjectByType<DayNightCycle>()?.SetHour(10.5f);
            WeatherCycle.Instance?.Force(WeatherCycle.Kind.Clear);
            yield return new WaitForSecondsRealtime(1.2f);
        }

        private static void Shoot(string type)
        {
            var cam = Camera.main;
            var rt = new RenderTexture(960, 540, 24);
            cam.targetTexture = rt;
            cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Dir);
            File.WriteAllBytes(Path.Combine(Dir, type + ".jpg"), tex.EncodeToJPG(86));
            RenderTexture.active = old;
            cam.targetTexture = null;
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(tex);
            Debug.Log("[Guide] photo " + type);
        }
    }
}
