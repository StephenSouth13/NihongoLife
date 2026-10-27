using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.PhuQuoc;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Phú Quốc (65_PhuQuocIsland, PhuQuocIslandBuilder) is a zone like Midori: pressing Play in it boots the town and
    /// enters the island; the player arrives at Gánh Ngon, walks on the island, and has a way back to Hibari. The island
    /// is built from real OpenStreetMap coordinates. Captures: Bao_Cao/phuquoc.
    /// </summary>
    public class PhuQuocIslandPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/phuquoc"));

        [UnityTest]
        public IEnumerator Island_PlacesGanhNgonArrivalAndWayBack()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.PhuQuocIslandScene);
            PlayerController player = null;
            for (float t = 0f; t < 40f; t += Time.unscaledDeltaTime)
            {
                player = Object.FindFirstObjectByType<PlayerController>();
                var flow = Object.FindFirstObjectByType<SceneFlowController>();
                if (player != null && flow != null && !flow.IsLoading && SceneManager.GetActiveScene().name == WorldLocationCatalog.PhuQuocIslandScene) break;
                yield return null;
            }
            Assert.AreEqual(WorldLocationCatalog.PhuQuocIslandScene, SceneManager.GetActiveScene().name, "Play in the island boots the town and enters it");
            Assert.NotNull(player, "The town's player came along");
            yield return new WaitForSecondsRealtime(1f);

            var map = PhuQuocMap.Load();
            var root = GameObject.Find("PhuQuocIsland").transform;
            Vector3 P(string id) => root.TransformPoint(map.ToWorld(map.Place(id)));
            var places = root.Find("Places");
            Assert.AreEqual(map.places.Length, places.childCount, "Every real place has a marker");
            Assert.Greater(Vector3.Distance(root.position, Vector3.zero), 1000f, "The island sits far outside the town");
            var spots = Object.FindObjectsByType<GanhNgonSpot>(FindObjectsSortMode.None);
            Assert.AreEqual(8, spots.Length, "Gánh Ngon: reception, four tables, kitchen pass, pub bar, manager");

            // Arrived in front of Gánh Ngon, on the ground.
            Assert.Less(Vector3.Distance(player.transform.position, P("ganh_ngon")), 12f, "Arrives at Gánh Ngon: " + player.transform.position);
            // Walks on the island (the real CharacterController, a few metres along the street).
            var cc = player.GetComponent<CharacterController>();
            Vector3 start = player.transform.position;
            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                cc.Move(Vector3.right * 2.4f * Time.deltaTime + Vector3.down * 2f * Time.deltaTime);
                yield return null;
            }
            Assert.Greater(Vector3.Distance(start, player.transform.position), 2f, "Walks");
            Assert.Greater(player.transform.position.y, root.position.y - 0.5f, "Stays on the island, does not fall through");
            Assert.IsTrue(Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None).Any(p => p.gameObject.scene.name == WorldLocationCatalog.PhuQuocIslandScene), "A way back to Hibari");

            // Geography holds: An Thới south of Dương Đông, Hàm Ninh east, Gành Dầu north-west.
            Assert.Less(P("an_thoi").z, P("ganh_ngon").z);
            Assert.Greater(P("ham_ninh").x, P("ganh_ngon").x);
            Assert.Less(P("ganh_dau").x, P("ganh_ngon").x);
            Assert.Greater(P("ganh_dau").z, P("ganh_ngon").z);

            var cam = new GameObject("CaptureCam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.75f, 0.9f);
            cam.farClipPlane = 3000f;
            cam.transform.SetPositionAndRotation(root.position + new Vector3(-20f, 520f, -40f), Quaternion.Euler(90f, 0f, 0f));
            cam.fieldOfView = 60f;
            Capture(cam, "01_island_overview");
            var g = P("ganh_ngon");
            cam.transform.position = g + new Vector3(0f, 22f, -34f);
            cam.transform.LookAt(g + new Vector3(0f, 2f, -2f));
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
