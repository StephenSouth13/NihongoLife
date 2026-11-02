using System.Collections;
using System.IO;
using NihongoLife.Core;
using NihongoLife.Gestures;
using NihongoLife.Player;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>In the water the player floats and strokes instead of falling through the map, and swims back onto land.</summary>
    public class SwimPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/swim"));

        private static IEnumerator Enter(string scene, string spawn)
        {
            NihongoLife.Health.PlayerIllness.CureAll(); // an earlier test may leave the player ill
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(1.5f);
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            flow.TransferZone(scene, spawn, scene);
            for (float t = 0f; t < 30f && (flow.IsLoading || SceneManager.GetActiveScene().name != scene); t += Time.unscaledDeltaTime) yield return null;
            Assert.AreEqual(scene, SceneManager.GetActiveScene().name);
            yield return new WaitForSecondsRealtime(1.5f);
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

        private static IEnumerator SwimOffShore(string tag, Vector3 outward)
        {
            var player = Object.FindFirstObjectByType<PlayerController>();
            var swim = player.GetComponent<PlayerSwim>();
            Assert.NotNull(swim, "The player can swim");
            var body = player.GetComponent<CharacterController>();
            Vector3 land = player.transform.position;
            yield return new WaitForSecondsRealtime(1f);

            // Walk straight out until the ground ends.
            Vector3 probe = land;
            var water = WaterSurface.At(land);
            Assert.NotNull(water, tag + ": water around the place");
            for (int i = 0; i < 700; i++)
            {
                probe += outward;
                bool ground = Physics.Raycast(probe + Vector3.up * 4f, Vector3.down, out RaycastHit hit, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.point.y > water.Y - 0.5f;
                if (!ground) break;
            }
            Vector3 sea = probe + outward * 14f;
            sea.y = water.Y + 0.2f;
            body.enabled = false;
            player.transform.position = sea;
            body.enabled = true;
            water = WaterSurface.At(sea);

            for (float t = 0f; t < 4f && !swim.Swimming; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(swim.Swimming, tag + ": swimming");
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreEqual(water.Y - 0.4f, player.transform.position.y, 0.2f, tag + ": floats at the surface");
            Capture(tag + "_1_tread");

            player.SetClickDestination(probe - outward * 30f);
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreEqual("swim_crawl", GestureLayer.On(player.gameObject).Current, tag + ": strokes while moving");
            Capture(tag + "_2_stroke");
            for (float t = 0f; t < 45f && swim.Swimming; t += Time.unscaledDeltaTime)
            {
                player.SetClickDestination(probe - outward * 30f);
                yield return null;
            }
            Assert.IsFalse(swim.Swimming, tag + ": back on land");
            yield return new WaitForSecondsRealtime(1f);
            Assert.Greater(player.transform.position.y, water.Y - 0.5f, tag + ": standing, at most wading in the shallows");
            Capture(tag + "_3_ashore");
            Assert.AreNotEqual("swim_crawl", GestureLayer.On(player.gameObject).Current);
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator Midori_SwimOutAndBack()
        {
            yield return Enter("60_MidoriIsland", "midori_station");
            yield return SwimOffShore("midori", Vector3.back);
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator PhuQuoc_SwimOutAndBack()
        {
            yield return Enter("65_PhuQuocIsland", "phuquoc_ganh_ngon");
            yield return SwimOffShore("phuquoc", Vector3.back);
        }
    }
}
