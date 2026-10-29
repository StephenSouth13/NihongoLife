using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Midori Island entered like a player: every person stands on the ground (an old angler floated by the fishing river),
    /// birds are out where they can be seen (some pecking on the ground), and walking up to one sends it flying.
    /// Captures: Bao_Cao/island-people.
    /// </summary>
    public class IslandPeopleBirdsPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/island-people"));

        [UnityTest]
        public IEnumerator Island_PeopleOnTheGround_BirdsFlyOffWhenApproached()
        {
            LogAssert.ignoreFailingMessages = true;
            AmbientWildlife.HourOverride = 10f;
            AmbientWildlife.SeasonOverride = Season.Spring;
            try
            {
                yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
                yield return new WaitForSecondsRealtime(2.5f);
                var flow = Object.FindFirstObjectByType<SceneFlowController>();
                flow.EnterZone(WorldLocationCatalog.MidoriIslandScene, WorldLocationCatalog.MidoriStation, "Midori");
                for (float t = 0f; t < 30f && (flow.IsLoading || SceneManager.GetActiveScene().name != WorldLocationCatalog.MidoriIslandScene); t += Time.unscaledDeltaTime) yield return null;
                Assert.AreEqual(WorldLocationCatalog.MidoriIslandScene, SceneManager.GetActiveScene().name);
                yield return new WaitForSecondsRealtime(5f);
                var player = Object.FindFirstObjectByType<PlayerController>();
                Assert.IsNotNull(player);

                // People: feet on whatever solid is under them — on arrival, and after they have wandered (time × 5).
                var report = new StringBuilder();
                Measure(report, "arrival");
                Time.timeScale = 5f;
                yield return new WaitForSecondsRealtime(15f);
                Time.timeScale = 1f;
                Measure(report, "after wandering");
                Directory.CreateDirectory(Folder);
                File.WriteAllText(Path.Combine(Folder, "people.txt"), report.ToString());
                Assert.IsFalse(report.ToString().Contains("FLOAT"), "people standing in the air:\n" + report);

                // Birds: some on the ground near the player; walking up to one scares it off.
                var wildlife = AmbientWildlife.Instance;
                Assert.IsNotNull(wildlife);
                Critter bird = null;
                for (float t = 0f; t < 20f && bird == null; t += 0.5f)
                {
                    bird = wildlife.Critters.FirstOrDefault(c => c != null && c.gameObject.activeInHierarchy && c.Kind == WildKind.Bird && c.OnGround);
                    if (bird == null) { wildlife.ApplyBudget(); yield return new WaitForSecondsRealtime(0.5f); }
                }
                Assert.IsNotNull(bird, "a bird pecking on the ground");
                Shot("01_bird_on_ground", bird.transform.position + new Vector3(2.2f, 1.2f, 2.2f), bird.transform.position + Vector3.up * 0.2f);
                Vector3 toBird = bird.transform.position - player.transform.position; toBird.y = 0f;
                var controller = player.GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;
                player.transform.position = bird.transform.position - toBird.normalized * 2.5f;
                if (controller != null) controller.enabled = true;
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.AreEqual(Critter.State.Flying, bird.Current, "walking up to the bird sends it flying");
                Assert.Greater(bird.Startled, 0);
                yield return new WaitForSecondsRealtime(1.2f);
                Assert.Greater(bird.transform.position.y, player.transform.position.y + 1f, "up in the air");
                Shot("02_bird_flies_off", player.transform.position + new Vector3(-3f, 2.2f, -3f), bird.transform.position);
            }
            finally
            {
                Time.timeScale = 1f;
                AmbientWildlife.HourOverride = null;
                AmbientWildlife.SeasonOverride = null;
            }
        }

        private static void Measure(StringBuilder report, string when)
        {
            report.AppendLine("— " + when);
            int people = 0, floating = 0;
            foreach (var animator in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).Where(a => a.isHuman && a.gameObject.activeInHierarchy))
            {
                var body = animator.transform;
                if (body.GetComponentInParent<PlayerController>() != null) continue;
                var left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                var right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                if (left == null || right == null) continue;
                float foot = Mathf.Min(left.position.y, right.position.y);
                Vector3 under = (left.position + right.position) * 0.5f;
                var hit = Physics.RaycastAll(new Vector3(under.x, foot + 1.2f, under.z), Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(h => !h.collider.transform.IsChildOf(body) && h.collider.GetComponent<CharacterController>() == null)
                    .OrderBy(h => h.distance).FirstOrDefault();
                if (hit.collider == null) { report.AppendLine($"FLOAT {Name(body)}: nothing under it at {under}"); floating++; continue; }
                float gap = foot - hit.point.y;
                people++;
                string path = Name(body);
                bool sitting = path.IndexOf("Bench", System.StringComparison.OrdinalIgnoreCase) >= 0 || path.IndexOf("Seat", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (gap > 0.25f && !sitting) { floating++; report.Append("FLOAT "); Shot("float_" + Name(body).Replace('/', '_'), under + new Vector3(3f, 1.8f, 3f), under + Vector3.up); }
                report.AppendLine($"{Name(body)}: feet {foot:0.00}, ground {hit.point.y:0.00} ({hit.collider.name}), gap {gap:0.00}");
            }
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Path.Combine(Folder, "people.txt"), report.ToString());
            Assert.Greater(people, 3, "island residents were found\n" + report);
            Assert.AreEqual(0, floating, "people standing in the air:\n" + report);
        }

        private static string Name(Transform t) => t.parent != null ? t.parent.name + "/" + t.name : t.name;

        private static void Shot(string name, Vector3 at, Vector3 look)
        {
            var go = new GameObject("ShotCamera");
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.05f;
            go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(look - at));
            var rt = new RenderTexture(1280, 720, 24);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Folder);
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
