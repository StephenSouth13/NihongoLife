using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NihongoLife.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Fills every zone from its spawn points, as far as a player can walk, and lists the places with no floor. A doorway
    /// left open onto nothing dropped the player off the map; this finds every such hole at once instead of one by one.
    /// </summary>
    public class ZoneLeakTests
    {
        private const float Cell = 0.5f, StepUp = 0.45f, MaxDrop = 1.2f, Radius = 0.28f, Height = 1.5f;

        private static readonly string[] Zones =
        {
            "20_StationDistrict", "30_SushiRestaurant", "35_HibariClinic", "40_ HIBARICLASS",
            "45_HomeBedroom", "50_GameCenter", "55_MusicShop", "56_FashionStore",
        };

        [Test]
        public void EveryIndoorZone_HasNoHoleAPlayerCanWalkInto()
        {
            var report = new StringBuilder();
            foreach (var name in Zones)
            {
                var scene = EditorSceneManager.OpenScene("Assets/NihongoLife/Scenes/" + name + ".unity", OpenSceneMode.Single);
                Physics.SyncTransforms();
                foreach (var hole in Holes(scene).Take(600)) report.AppendLine(name + " " + hole);
            }
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/zone-leaks"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "holes.txt"), report.ToString(), new UTF8Encoding(false));
            Assert.AreEqual("", report.ToString());
        }

        private static IEnumerable<Vector3> Holes(Scene scene)
        {
            var seen = new HashSet<Vector2Int>();
            var open = new Queue<(Vector2Int cell, float y)>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var spawn in root.GetComponentsInChildren<NihongoLife.Core.SceneSpawnPoint>(true))
                {
                    var p = spawn.transform.position;
                    if (!Floor(p + Vector3.up, 3f, out float y)) continue;
                    var c = ToCell(p);
                    if (seen.Add(c)) open.Enqueue((c, y));
                }
            var holes = new List<Vector3>();
            int guard = 0;
            while (open.Count > 0 && guard++ < 200000)
            {
                var (cell, y) = open.Dequeue();
                foreach (var step in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                {
                    var next = cell + step;
                    if (seen.Contains(next)) continue;
                    var at = new Vector3(next.x * Cell, y, next.y * Cell);
                    if (Blocked(ToWorld(cell, y), at)) continue;
                    seen.Add(next);
                    if (!Floor(at + Vector3.up * StepUp, StepUp + MaxDrop, out float nextY))
                    {
                        holes.Add(at);
                        continue;
                    }
                    open.Enqueue((next, nextY));
                }
            }
            return holes;
        }

        private static Vector2Int ToCell(Vector3 p) => new Vector2Int(Mathf.RoundToInt(p.x / Cell), Mathf.RoundToInt(p.z / Cell));
        private static Vector3 ToWorld(Vector2Int c, float y) => new Vector3(c.x * Cell, y, c.y * Cell);

        private static bool Floor(Vector3 from, float distance, out float y)
        {
            bool hit = Physics.Raycast(from, Vector3.down, out RaycastHit floor, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            y = hit ? floor.point.y : 0f;
            return hit;
        }

        private static bool Blocked(Vector3 from, Vector3 to)
        {
            Vector3 foot = Vector3.up * (StepUp + Radius), head = Vector3.up * (Height - Radius);
            return Physics.CapsuleCast(from + foot, from + head, Radius, (to - from).normalized, (to - from).magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }
    }
}
