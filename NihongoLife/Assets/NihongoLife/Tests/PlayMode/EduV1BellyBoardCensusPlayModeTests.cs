using System.Collections;
using System.Collections.Generic;
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
    /// <summary>Belly-board hunt, per character: for every NPC rig (street walkers, tourists, staff) look in a box in front of
    /// its belly for any renderer that is not its own skinned body - own children, unparented gesture props, or world
    /// geometry it walks through. Sampled 4 times. Writes Bao_Cao/edu-v1/belly_board_census.txt; fails on a flat board-like hit.</summary>
    public class EduV1BellyBoardCensusPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/edu-v1"));

        [UnityTest]
        public IEnumerator Npcs_CarryNoBoardInFrontOfBelly()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.PhuQuocIslandScene);
            for (float t = 0f; t < 40f; t += Time.unscaledDeltaTime)
            {
                var flow = Object.FindFirstObjectByType<SceneFlowController>();
                if (Object.FindFirstObjectByType<PlayerController>() != null && flow != null && !flow.IsLoading) break;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(4f);
            var sb = new StringBuilder();
            var hits = new HashSet<string>();
            int rigs = 0;
            for (int sample = 0; sample < 4; sample++)
            {
                var all = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.gameObject.activeInHierarchy && !(r is SkinnedMeshRenderer) && !(r is ParticleSystemRenderer)).ToArray();
                var animators = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).Where(a => a.isHuman && a.GetComponentInParent<PlayerController>() == null).ToArray();
                rigs = Mathf.Max(rigs, animators.Length);
                foreach (var a in animators)
                {
                    var hips = a.GetBoneTransform(HumanBodyBones.Hips);
                    if (hips == null) continue;
                    var owner = a.GetComponentInParent<CharacterAnimationController>() is Component c ? c.transform : a.transform;
                    Vector3 fwd = Vector3.ProjectOnPlane(owner.forward, Vector3.up).normalized;
                    var box = new Bounds(hips.position + fwd * 0.28f + Vector3.up * 0.1f, new Vector3(0.45f, 0.5f, 0.45f));
                    foreach (var r in all)
                    {
                        var b = r.bounds;
                        if (!b.Intersects(box)) continue;
                        float thin = Mathf.Min(b.size.x, Mathf.Min(b.size.y, b.size.z));
                        float wide = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                        if (wide > 3f) continue; // floors, walls, counters: scenery, not a carried board
                        bool own = r.transform.IsChildOf(owner);
                        string path = Path2(r.transform);
                        string key = owner.name + " <- " + path;
                        if (!hits.Add(key)) continue;
                        bool boardLike = thin < 0.12f && wide > 0.25f;
                        sb.AppendLine((boardLike ? "BOARD " : "item  ") + key + (own ? "  [own child]" : "  [not parented]") +
                                      "  size " + b.size.ToString("0.00") + "  s" + sample);
                    }
                }
                yield return new WaitForSecondsRealtime(1.5f);
            }
            Directory.CreateDirectory(Folder);
            string report = "rigs checked: " + rigs + "\n" + (sb.Length == 0 ? "nothing in front of any NPC belly\n" : sb.ToString());
            File.WriteAllText(Path.Combine(Folder, "belly_board_census.txt"), report);
            Debug.Log("[BellyBoard]\n" + report);
            Assert.Greater(rigs, 0, "found NPC rigs");
            Assert.IsFalse(report.Contains("BOARD "), "a board-like mesh sits in front of an NPC belly:\n" + report);
        }


        /// <summary>Same belly check in every scene of the build (restaurant guests, clinic patients, classroom, city NPCs).</summary>
        // Explicit: loads every build scene (bootstrap/menu/trailer flows keep running afterwards), so it must not run inside the full suite.
        [UnityTest, Explicit, Timeout(1800000)]
        public IEnumerator AllScenes_NpcsCarryNoBoard()
        {
            LogAssert.ignoreFailingMessages = true;
            var sb = new StringBuilder();
            bool any = false;
            for (int si = 0; si < SceneManager.sceneCountInBuildSettings; si++)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(si));
                var op = SceneManager.LoadSceneAsync(si);
                float t0 = Time.realtimeSinceStartup;
                while (op != null && !op.isDone && Time.realtimeSinceStartup - t0 < 30f) yield return null;
                yield return new WaitForSecondsRealtime(6f);
                int rigs = 0; var lines = new StringBuilder();
                for (int sample = 0; sample < 3; sample++)
                {
                    rigs = Mathf.Max(rigs, Check(lines));
                    yield return new WaitForSecondsRealtime(1.5f);
                }
                if (lines.ToString().Contains("BOARD ")) any = true;
                sb.AppendLine("== " + name + " : rigs " + rigs).Append(lines);
            }
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Path.Combine(Folder, "belly_board_all_scenes.txt"), sb.ToString());
            Debug.Log("[BellyBoardAll]\n" + sb);
            LogAssert.ignoreFailingMessages = false;
            Assert.IsFalse(any, sb.ToString());
        }

        private static readonly HashSet<string> Seen = new HashSet<string>();

        private static int Check(StringBuilder sb)
        {
            var all = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.gameObject.activeInHierarchy && !(r is SkinnedMeshRenderer) && !(r is ParticleSystemRenderer)).ToArray();
            var animators = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).Where(a => a.isHuman && a.GetComponentInParent<PlayerController>() == null).ToArray();
            foreach (var a in animators)
            {
                var hips = a.GetBoneTransform(HumanBodyBones.Hips);
                if (hips == null) continue;
                var owner = a.GetComponentInParent<CharacterAnimationController>() is Component c ? c.transform : a.transform;
                Vector3 fwd = Vector3.ProjectOnPlane(owner.forward, Vector3.up).normalized;
                var box = new Bounds(hips.position + fwd * 0.28f + Vector3.up * 0.1f, new Vector3(0.45f, 0.5f, 0.45f));
                foreach (var r in all)
                {
                    var b = r.bounds;
                    if (!b.Intersects(box)) continue;
                    float thin = Mathf.Min(b.size.x, Mathf.Min(b.size.y, b.size.z));
                    float wide = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                    if (wide > 3f) continue;
                    bool own = r.transform.IsChildOf(owner);
                    string key = owner.name + " <- " + Path2(r.transform);
                    if (!Seen.Add(key)) continue;
                    bool boardLike = thin < 0.12f && wide > 0.25f && (own || r.name.StartsWith("GestureProp") || r.transform.root.name.StartsWith("GestureProp"));
                    sb.AppendLine((boardLike ? "BOARD " : "item  ") + key + (own ? "  [own child]" : "  [not parented]") + "  size " + b.size.ToString("0.00"));
                }
            }
            return animators.Length;
        }

        private static string Path2(Transform t) => t.parent == null ? t.name : (t.parent.parent == null ? t.parent.name : t.parent.parent.name + "/" + t.parent.name) + "/" + t.name;
    }
}
