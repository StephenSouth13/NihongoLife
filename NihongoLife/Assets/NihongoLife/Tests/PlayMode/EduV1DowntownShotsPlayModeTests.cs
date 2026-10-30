using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NihongoLife.Core;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>Edu v1 evidence: downtown screenshots (facade facing) and a census of anything held in front of NPCs. Captures: Bao_Cao/edu-v1.</summary>
    public class EduV1DowntownShotsPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/edu-v1"));

        [UnityTest]
        public IEnumerator Downtown_Shots_And_HeldPropCensus()
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
            Assert.NotNull(player);
            yield return new WaitForSecondsRealtime(8f);
            var down = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == "DuongDong_Downtown");
            Assert.NotNull(down);
            Directory.CreateDirectory(Folder);

            var sb = new StringBuilder();
            foreach (var a in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
            {
                if (a.GetComponentInParent<PlayerController>() != null) continue;
                var held = a.transform.root.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.gameObject.activeInHierarchy && (t.name.StartsWith("GestureProp_") || t.name.StartsWith("HeldProp_") || t.name.ToLowerInvariant().Contains("tray") || t.name.ToLowerInvariant().Contains("board") || t.name.ToLowerInvariant().Contains("sign")))
                    .Select(t => t.name).ToArray();
                var gl = a.GetComponentInParent<NihongoLife.Gestures.GestureLayer>();
                var glProps = gl != null ? gl.ActiveProps.Select(p => p.id + "@" + p.scale.ToString("0.00")).ToArray() : new string[0];
                if (held.Length > 0 || glProps.Length > 0)
                    sb.AppendLine(a.transform.root.name + "/" + a.name + " : " + string.Join(",", held) + " | gesture: " + string.Join(",", glProps));
            }
            File.WriteAllText(Path.Combine(Folder, "held_prop_census.txt"), sb.Length == 0 ? "none" : sb.ToString());
            Debug.Log("[EduShots] census\n" + sb);

            var o = down.position;
            Shot("01_street_east", o + new Vector3(-70f, 9f, 0f), o + new Vector3(10f, 2f, 0f));
            Shot("02_north_row", o + new Vector3(12f, 7f, 0f), o + new Vector3(45f, 3f, 9f));
            Shot("03_south_row", o + new Vector3(12f, 7f, 0f), o + new Vector3(45f, 3f, -9f));
            var gn = GameObject.Find("GanhNgon_Pavilion");
            if (gn != null) Shot("02_north_row", gn.transform.TransformPoint(new Vector3(-6f, 3f, -14f)), gn.transform.TransformPoint(new Vector3(25f, 3f, -4f)));
            if (gn != null) Shot("03_south_row", gn.transform.TransformPoint(new Vector3(6f, 3f, -8f)), gn.transform.TransformPoint(new Vector3(-25f, 3f, -20f)));
            if (gn != null) Shot("06_gn_front", gn.transform.TransformPoint(new Vector3(0f, 2.6f, -16f)), gn.transform.TransformPoint(new Vector3(0f, 4f, 0f)));
            Shot("04_overview", o + new Vector3(0f, 90f, -60f), o);
            Shot("05_player_view", player.transform.position + player.transform.rotation * new Vector3(0f, 2.2f, -4f), player.transform.position + Vector3.up * 1.2f);
            Assert.IsTrue(File.Exists(Path.Combine(Folder, "04_overview.png")));
        }

        private static void Shot(string name, Vector3 from, Vector3 at)
        {
            var go = new GameObject("EduShotCam");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 60f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 3000f;
            go.transform.SetPositionAndRotation(from, Quaternion.LookRotation(at - from));
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(go);
        }
    }
}