using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NihongoLife.Core;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The cars' night lamps sit on their own bumpers: each head-light at the front end, each tail-light at the rear,
    /// at a height inside the body — they used to be fixed offsets that left yellow and red blocks on roofs and beside
    /// the cars. Captures: Bao_Cao/car-lamps.
    /// </summary>
    public class CarLampsPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/car-lamps"));

        [UnityTest]
        public IEnumerator NightLamps_SitOnTheBumpers()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var cycle = Object.FindFirstObjectByType<DayNightCycle>();
            Assert.IsNotNull(cycle);
            cycle.SetHour(21.5f);
            yield return new WaitForSecondsRealtime(2f);
            var traffic = Object.FindFirstObjectByType<StreetTraffic>();
            Assert.IsNotNull(traffic);
            var report = new StringBuilder();
            int lamps = 0;
            foreach (var car in traffic.Cars.Where(c => c.body != null && c.bulbs != null))
            {
                var body = car.body.GetComponentsInChildren<Renderer>().Where(r => !r.name.Contains("light")).Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
                foreach (var bulb in car.bulbs.Where(b => b != null && b.activeInHierarchy))
                {
                    lamps++;
                    Vector3 p = bulb.transform.position;
                    float local = traffic.transform.InverseTransformPoint(p).x - traffic.transform.InverseTransformPoint(car.body.position).x;
                    bool head = bulb.name.StartsWith("Head");
                    bool rightEnd = head ? Mathf.Sign(local) == car.direction : Mathf.Sign(local) == -car.direction;
                    bool height = p.y > body.min.y + 0.1f && p.y < body.max.y - 0.2f;
                    var grown = body; grown.Expand(0.25f);
                    report.AppendLine($"{car.body.name}/{bulb.name}: y {p.y:0.00} (body {body.min.y:0.00}–{body.max.y:0.00}), end ok {rightEnd}, near body {grown.Contains(p)}");
                    Assert.IsTrue(rightEnd, bulb.name + " of " + car.body.name + " is at the wrong end\n" + report);
                    Assert.IsTrue(height, bulb.name + " of " + car.body.name + " is not at bumper height (on the roof or under the car)\n" + report);
                    Assert.IsTrue(grown.Contains(p), bulb.name + " of " + car.body.name + " floats beside the car\n" + report);
                }
            }
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Path.Combine(Folder, "lamps.txt"), report.ToString());
            Assert.Greater(lamps, 3, "lamps are lit at night");
            var first = traffic.Cars.First(c => c.body != null);
            Vector3 at = first.body.position;
            Shot("01_car_at_night", at + new Vector3(-5.5f, 2.2f, -5.5f), at + Vector3.up * 0.6f);
            Shot("02_street_at_night", at + new Vector3(-14f, 4f, -9f), at + new Vector3(6f, 0.5f, 0f));
        }

        private static void Shot(string name, Vector3 at, Vector3 look)
        {
            var go = new GameObject("ShotCamera");
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(look - at));
            var rt = new RenderTexture(1600, 900, 24);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tex.Apply();
            try { File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG()); }
            catch (IOException e) { Debug.LogWarning("Capture " + name + " skipped (the file is open elsewhere): " + e.Message); }
            RenderTexture.active = null;
            camera.targetTexture = null;
            rt.Release();
            Object.Destroy(tex);
            Object.Destroy(go);
        }
    }
}
