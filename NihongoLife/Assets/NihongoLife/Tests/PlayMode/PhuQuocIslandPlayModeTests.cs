using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.PhuQuoc;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The first pass of Phú Quốc (65_PhuQuocIsland, PhuQuocIslandBuilder): the island from real OpenStreetMap
    /// coordinates, every place marked, Gánh Ngon with its service stations. Captures: Bao_Cao/phuquoc.
    /// </summary>
    public class PhuQuocIslandPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/phuquoc"));

        [UnityTest]
        public IEnumerator Island_PlacesAndGanhNgon()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync("65_PhuQuocIsland");
            yield return null;
            var map = PhuQuocMap.Load();
            var places = GameObject.Find("PhuQuocIsland/Places");
            Assert.NotNull(places, "Places group");
            Assert.AreEqual(map.places.Length, places.transform.childCount, "Every real place has a marker");
            Assert.NotNull(GameObject.Find("PhuQuocIsland/Island_Beach").GetComponent<MeshCollider>(), "Walkable island");
            var spots = Object.FindObjectsByType<GanhNgonSpot>(FindObjectsSortMode.None);
            Assert.AreEqual(8, spots.Length, "Gánh Ngon: reception, four tables, kitchen pass, pub bar, manager");
            Assert.IsTrue(spots.Any(s => s.station == GanhNgonStation.Manager));

            // Geography holds: An Thới south of Dương Đông, Hàm Ninh east, Gành Dầu north-west.
            Vector3 P(string id) => map.ToWorld(map.Place(id));
            Assert.Less(P("an_thoi").z, P("ganh_ngon").z);
            Assert.Greater(P("ham_ninh").x, P("ganh_ngon").x);
            Assert.Less(P("ganh_dau").x, P("ganh_ngon").x);
            Assert.Greater(P("ganh_dau").z, P("ganh_ngon").z);

            var cam = new GameObject("CaptureCam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.75f, 0.9f);
            cam.farClipPlane = 3000f;
            var g = P("ganh_ngon");
            cam.transform.SetPositionAndRotation(new Vector3(-20f, 520f, -40f), Quaternion.Euler(90f, 0f, 0f));
            cam.fieldOfView = 60f;
            Capture(cam, "01_island_overview");
            cam.transform.position = g + new Vector3(0f, 18f, -26f);
            cam.transform.LookAt(g + new Vector3(0f, 0f, -2f));
            cam.fieldOfView = 50f;
            Capture(cam, "02_ganh_ngon");
            cam.transform.position = P("an_thoi") + new Vector3(-30f, 60f, -60f);
            cam.transform.LookAt(P("an_thoi") + new Vector3(0f, 0f, -20f));
            Capture(cam, "03_an_thoi_hon_thom");
            Object.Destroy(cam.gameObject);
        }

        private static void Capture(Camera camera, string name)
        {
            const int width = 1600, height = 900;
            var rt = new RenderTexture(width, height, 24);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            camera.targetTexture = null;
            RenderTexture.active = null;
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
        }
    }
}
