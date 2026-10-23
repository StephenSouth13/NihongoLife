// LOCAL ONLY (trailer v4 capture) - not committed. Records a slow camera pan in each zone.
using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;using NihongoLife.Cameras;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    public class ZoneTourRecordingTests
    {
        private static string Root => System.Environment.GetEnvironmentVariable("NL_FRAME_DIR") is { Length: > 0 } d ? d : Path.Combine(Path.GetTempPath(), "NLZoneTour");
        private RenderTexture _rt; private Texture2D _tex;

        [UnityTest, Explicit, Timeout(1800000)]
        public IEnumerator ZoneTour_RecordFrames()
        {
            Time.captureFramerate = 30;
            _rt = new RenderTexture(1920, 1080, 24); _tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            // Main menu
            SceneManager.LoadScene("01_MainMenu");
            for (int i = 0; i < 60; i++) yield return null;
            yield return Rec("mainmenu", Camera.main, 120, null);
            // City
            SceneManager.LoadScene(WorldLocationCatalog.CityScene);
            for (int i = 0; i < 120; i++) yield return null;
            var city = SceneManager.GetSceneByName(WorldLocationCatalog.CityScene);
            var cam = new GameObject("TourCam").AddComponent<Camera>();
            cam.depth = 100; cam.fieldOfView = 55f; cam.farClipPlane = 2000f;
            Object.DontDestroyOnLoad(cam.gameObject);
            var player = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).FirstOrDefault(m => m.GetType().Name == "PlayerController");
            Vector3 p0 = player != null ? player.transform.position : Vector3.zero;
            yield return Orbit("city", cam, p0, 14f, 7f, 150);
            string[][] zones = {
                new[]{"station", WorldLocationCatalog.StationScene, WorldLocationCatalog.StationEntrance},
                new[]{"sushi", WorldLocationCatalog.SushiRestaurantScene, WorldLocationCatalog.SushiEntrance},
                new[]{"school", WorldLocationCatalog.SchoolScene, WorldLocationCatalog.SchoolEntrance},
                new[]{"bedroom", WorldLocationCatalog.HomeBedroomScene, WorldLocationCatalog.HomeBedroomEntrance},
                new[]{"gamecenter", WorldLocationCatalog.GameCenterScene, WorldLocationCatalog.GameCenterEntrance},
                new[]{"island", WorldLocationCatalog.MidoriIslandScene, WorldLocationCatalog.MidoriStation},
            };
            Scene prev = default;
            foreach (var z in zones)
            {
                var load = SceneManager.LoadSceneAsync(z[1], LoadSceneMode.Additive);
                float t0 = Time.realtimeSinceStartup;
                while (!load.isDone && Time.realtimeSinceStartup - t0 < 120f) yield return null;
                var zone = SceneManager.GetSceneByName(z[1]);
                foreach (var r in city.GetRootGameObjects()) if (r.GetComponent<SceneZoneVisibility>() != null) r.SetActive(false);
                SceneManager.SetActiveScene(zone);
                foreach (var r in zone.GetRootGameObjects()) foreach (var pc in r.GetComponentsInChildren<ZonePreviewCamera>(true)) pc.gameObject.SetActive(false);
                if (prev.IsValid() && prev.isLoaded) { var u = SceneManager.UnloadSceneAsync(prev); while (u != null && !u.isDone) yield return null; }
                prev = zone;
                for (int i = 0; i < 60; i++) yield return null;
                var spawn = zone.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SceneSpawnPoint>(true)).FirstOrDefault(s => s.Id == z[2])
                            ?? zone.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SceneSpawnPoint>(true)).FirstOrDefault();
                Vector3 c = spawn != null ? spawn.transform.position : Vector3.zero;
                if (z[0] == "island") yield return Orbit(z[0], cam, c, 30f, 14f, 150);
                else yield return Pan(z[0], cam, c + Vector3.up * 1.7f, spawn != null ? spawn.transform.eulerAngles.y : 0f, 150);
            }
            Time.captureFramerate = 0;
            Assert.Pass("zone tour recorded");
        }

        private IEnumerator Orbit(string id, Camera cam, Vector3 c, float r, float h, int frames)
        {
            return Rec(id, cam, frames, f => { float a = Mathf.Lerp(-30f, 30f, f / (float)frames) * Mathf.Deg2Rad; cam.transform.position = c + new Vector3(Mathf.Sin(a) * r, h, -Mathf.Cos(a) * r); cam.transform.LookAt(c + Vector3.up * 1.5f); });
        }
        private IEnumerator Pan(string id, Camera cam, Vector3 eye, float yaw, int frames)
        {
            return Rec(id, cam, frames, f => { cam.transform.position = eye; cam.transform.rotation = Quaternion.Euler(8f, yaw + Mathf.Lerp(-40f, 40f, f / (float)frames), 0f); });
        }
        private IEnumerator Rec(string id, Camera cam, int frames, System.Action<int> move)
        {
            string dir = Path.Combine(Root, id); Directory.CreateDirectory(dir);
            for (int f = 0; f < frames; f++)
            {
                move?.Invoke(f);
                yield return null;
                if (cam == null) cam = Camera.main; if (cam == null) continue;
                var old = cam.targetTexture; cam.targetTexture = _rt; cam.Render(); cam.targetTexture = old;
                var a = RenderTexture.active; RenderTexture.active = _rt; _tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); _tex.Apply(); RenderTexture.active = a;
                File.WriteAllBytes(Path.Combine(dir, $"f{f:00000}.jpg"), _tex.EncodeToJPG(92));
            }
        }
    }
}

