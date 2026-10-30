using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>Dương Đông beach resort (PhuQuocResortBuilder): pool, villas, loungers, palms exist at real proportions; screenshots. Captures: Bao_Cao/edu-v1/resort.</summary>
    public class PhuQuocResortPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/edu-v1/resort"));

        [UnityTest]
        public IEnumerator Resort_IsBuilt_Proportioned_AndShot()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.PhuQuocIslandScene);
            PlayerController player = null;
            for (float t = 0f; t < 40f; t += Time.unscaledDeltaTime)
            {
                player = Object.FindFirstObjectByType<PlayerController>();
                var flow = Object.FindFirstObjectByType<SceneFlowController>();
                if (player != null && flow != null && !flow.IsLoading) break;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(3f);
            var all = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();
            var resort = all.FirstOrDefault(t => t.name == "DuongDong_Resort");
            Assert.NotNull(resort, "run PhuQuocResortBuilder.Build");
            var kids = resort.GetComponentsInChildren<Transform>(true);
            Assert.AreEqual(8, kids.Count(t => t.name.StartsWith("Villa_") && t.parent == resort));
            Assert.NotNull(kids.FirstOrDefault(t => t.name == "PoolWater"));
            Assert.GreaterOrEqual(kids.Count(t => t.name.StartsWith("BeachLounger_")), 20);
            var lounger = kids.First(t => t.name.StartsWith("BeachLounger_"));
            float lh = Bounds(lounger.gameObject).size.y;
            Assert.That(lh, Is.InRange(0.3f, 1.3f), "a lounger is lounger-sized");
            var palm = kids.First(t => t.name.StartsWith("Palm_"));
            Assert.That(Bounds(palm.gameObject).size.y, Is.InRange(6f, 12f), "palms 6–12 m");
            var villaBody = kids.First(t => t.name == "Body" && t.parent.name.StartsWith("Villa_"));
            Assert.That(villaBody.GetComponent<Renderer>().bounds.size.y, Is.InRange(2.6f, 3.4f), "one storey");

            Directory.CreateDirectory(Folder);
            var o = resort.position;
            RenderSettings.fog = false; // resort shots: clear tropical air
            Shot("01_overview", o + new Vector3(0f, 38f, -70f), o + new Vector3(0, 0, 15f));
            Shot("02_pool", o + new Vector3(-20f, 6f, -2f), o + new Vector3(4f, 0.5f, 20f));
            Shot("03_lobby", o + new Vector3(0f, 2.2f, 26f), o + new Vector3(0f, 2.5f, 40f));
            Shot("04_beach", o + new Vector3(-30f, 2.5f, -24f), o + new Vector3(10f, 1.5f, -8f));
            Shot("05_villas", o + new Vector3(30f, 3f, 4f), o + new Vector3(48f, 2f, 26f));
            Shot("06_gate", o + new Vector3(-80f, 3f, 20f), o + new Vector3(-62f, 2.5f, 20f));
            Assert.IsTrue(File.Exists(Path.Combine(Folder, "01_overview.png")));
        }

        private static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }

        private static void Shot(string name, Vector3 from, Vector3 at)
        {
            var go = new GameObject("ResortShotCam");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 60f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 3000f;
            go.transform.SetPositionAndRotation(from, Quaternion.LookRotation(at - from));
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null; cam.targetTexture = null;
            Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(go);
        }
    }
}
