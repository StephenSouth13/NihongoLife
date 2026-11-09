using System.Collections;
using System.Collections.Generic;
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
    /// <summary>
    /// Going in and out of the areas again and again must not pile up textures, meshes, materials or sounds: on a phone's
    /// browser every leftover stays in the page's memory until the tab is killed. Counts every loaded object of each kind
    /// after each round (after an asset cleanup) and fails when a round adds more than a few.
    /// </summary>
    public class ZoneMemoryPlayModeTests
    {
        private static readonly (string scene, string spawn)[] Round =
        {
            ("55_MusicShop", "music_shop_entrance"), ("30_SushiRestaurant", "sushi_entrance"), ("65_PhuQuocIsland", "phuquoc_ganh_ngon"),
        };

        [UnityTest, Timeout(900000)]
        public IEnumerator Cycling_Areas_DoesNotPileUpAssets()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            SceneFlowController flow = null;
            for (float t = 0f; t < 30f && (flow == null || Object.FindFirstObjectByType<PlayerController>() == null || t < 2f); t += Time.unscaledDeltaTime)
            {
                flow = Object.FindFirstObjectByType<SceneFlowController>();
                yield return null;
            }
            Assert.NotNull(flow, "the town has its scene flow");
            var rounds = new List<Dictionary<string, int>>();
            HashSet<int> seen = null;
            var log = new StringBuilder();
            var managedRounds = new List<long>();
            Dictionary<string, int> lastStatics = null;
            for (int r = 0; r < 4; r++)
            {
                foreach (var (scene, spawn) in Round)
                {
                    flow.TransferZone(scene, spawn, scene);
                    for (float t = 0f; t < 60f && (flow.IsLoading || SceneManager.GetActiveScene().name != scene || t < 1.5f); t += Time.unscaledDeltaTime) yield return null;
                    Assert.AreEqual(scene, SceneManager.GetActiveScene().name, "reached " + scene);
                    yield return new WaitForSecondsRealtime(1f);
                }
                flow.ExitZone(SceneManager.GetActiveScene().name, WorldLocationCatalog.CityClinicReturn);
                for (float t = 0f; t < 60f && (flow.IsLoading || SceneManager.GetActiveScene().name != WorldLocationCatalog.CityScene || t < 1.5f); t += Time.unscaledDeltaTime) yield return null;
                yield return Resources.UnloadUnusedAssets();
                System.GC.Collect();
                var counts = Count();
                rounds.Add(counts);
                long managed = System.GC.GetTotalMemory(true);
                managedRounds.Add(managed);
                var statics = StaticSizes();
                if (lastStatics != null)
                    foreach (var kv in statics)
                        if (lastStatics.TryGetValue(kv.Key, out int before) && kv.Value > before) log.AppendLine($"  static grows: {kv.Key} {before} -> {kv.Value}");
                lastStatics = statics;
                log.AppendLine($"round {r + 1}: managed {managed / 1048576f:0.0} MB, " + string.Join(", ", counts.Select(kv => $"{kv.Key} {kv.Value}")));
                var materials = Resources.FindObjectsOfTypeAll<Material>();
                if (seen != null)
                    foreach (var group in materials.Where(m => !seen.Contains(m.GetInstanceID())).GroupBy(m => m.name + " / " + (m.shader != null ? m.shader.name : "-")))
                        log.AppendLine($"  new material: {group.Key} x{group.Count()}");
                seen = new HashSet<int>(materials.Select(m => m.GetInstanceID()));
            }
            Debug.Log("[ZoneMemory]\n" + log);
            // Round 1 warms every pool and cache once; rounds 2-4 must stay flat.
            foreach (var kind in rounds[1].Keys)
            {
                int growth = rounds[3][kind] - rounds[1][kind];
                Assert.LessOrEqual(growth, 6, $"{kind} grows by {growth} over two more rounds of areas:\n{log}");
            }
            // C# objects too: a static list or event that keeps every area's objects grows a heap the browser never returns.
            Assert.LessOrEqual((managedRounds[3] - managedRounds[1]) / 1048576f, 4f, "managed memory grows over two more rounds of areas:\n" + log);
        }

        /// <summary>Size of every static collection and event of the game's own code (and TMP's), to name what piles up.</summary>
        private static Dictionary<string, int> StaticSizes()
        {
            var sizes = new Dictionary<string, int>();
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                string an = asm.GetName().Name;
                if (!an.StartsWith("NihongoLife") && !an.StartsWith("Unity.TextMeshPro") && an != "Assembly-CSharp") continue;
                System.Type[] types;
                try { types = asm.GetTypes(); } catch (System.Reflection.ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
                foreach (var type in types)
                {
                    if (type.ContainsGenericParameters) continue;
                    foreach (var field in type.GetFields(flags))
                    {
                        object value;
                        try { value = field.GetValue(null); } catch { continue; }
                        int n = value is System.Collections.ICollection c ? c.Count : value is System.Delegate d ? d.GetInvocationList().Length : -1;
                        if (n >= 0) sizes[type.FullName + "." + field.Name] = n;
                    }
                }
            }
            return sizes;
        }

        private static Dictionary<string, int> Count() => new Dictionary<string, int>
        {
            ["Texture2D"] = Resources.FindObjectsOfTypeAll<Texture2D>().Length,
            ["RenderTexture"] = Resources.FindObjectsOfTypeAll<RenderTexture>().Length,
            ["Mesh"] = Resources.FindObjectsOfTypeAll<Mesh>().Length,
            ["Material"] = Resources.FindObjectsOfTypeAll<Material>().Length,
            ["AudioClip"] = Resources.FindObjectsOfTypeAll<AudioClip>().Length,
            ["GameObject"] = Resources.FindObjectsOfTypeAll<GameObject>().Length,
            ["Sprite"] = Resources.FindObjectsOfTypeAll<Sprite>().Length,
        };
    }
}
