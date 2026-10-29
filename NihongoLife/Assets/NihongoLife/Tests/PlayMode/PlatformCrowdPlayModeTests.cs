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
    /// The station's passengers (PlatformCrowd) stand on the floor: they were placed at the train door's height (its
    /// pivot is the door's middle, ≈ 1.15 m) and floated behind the ticket gates. Every walking / waiting passenger's
    /// feet must be on the solid floor under them; the seated ones are on their benches. Captures: Bao_Cao/station-crowd.
    /// </summary>
    public class PlatformCrowdPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/station-crowd"));

        [UnityTest]
        public IEnumerator Passengers_StandOnTheFloor()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            flow.EnterZone(WorldLocationCatalog.StationScene, WorldLocationCatalog.StationEntrance, "Station");
            for (float t = 0f; t < 25f && (flow.IsLoading || SceneManager.GetActiveScene().name != WorldLocationCatalog.StationScene); t += Time.unscaledDeltaTime) yield return null;
            Assert.AreEqual(WorldLocationCatalog.StationScene, SceneManager.GetActiveScene().name);
            yield return new WaitForSecondsRealtime(9f); // the first newcomers come down the stairs

            var report = new StringBuilder();
            int checkedPeople = 0, floating = 0;
            foreach (var person in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("PlatformPerson_") && !t.name.Contains("Bench")))
            {
                var animator = person.GetComponentInChildren<Animator>();
                if (animator == null || !animator.isHuman) continue;
                var left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                var right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                if (left == null || right == null) continue;
                float foot = Mathf.Min(left.position.y, right.position.y);
                Vector3 from = new Vector3(person.position.x, foot + 1.5f, person.position.z);
                var hit = Physics.RaycastAll(from, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(h => !h.collider.transform.IsChildOf(person) && h.collider.GetComponent<CharacterController>() == null)
                    .OrderBy(h => h.distance).FirstOrDefault();
                if (hit.collider == null) continue;
                float gap = foot - hit.point.y;
                checkedPeople++;
                if (gap > 0.2f) floating++;
                report.AppendLine($"{person.name}: feet {foot:0.00}, floor {hit.point.y:0.00} ({hit.collider.name}), gap {gap:0.00}");
            }
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Path.Combine(Folder, "feet.txt"), report.ToString());
            var gates = Object.FindObjectsByType<TicketGate>(FindObjectsSortMode.None);
            if (gates.Length > 0)
            {
                Vector3 centre = gates.Aggregate(Vector3.zero, (s, g) => s + g.transform.position) / gates.Length;
                var player = Object.FindFirstObjectByType<NihongoLife.Player.PlayerController>();
                Vector3 hall = player != null ? player.transform.position : centre + Vector3.forward * 4f;
                Vector3 away = hall - centre; away.y = 0f; away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward;
                Shot("01_gates_from_hall", centre + away * 4.5f + Vector3.up * 1.9f, centre + Vector3.up * 1.0f - away * 3f);
            }
            Assert.Greater(checkedPeople, 2, "passengers were found\n" + report);
            Assert.AreEqual(0, floating, "passengers standing in the air:\n" + report);
        }

        private static void Shot(string name, Vector3 at, Vector3 look)
        {
            var go = new GameObject("ShotCamera");
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 66f;
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
