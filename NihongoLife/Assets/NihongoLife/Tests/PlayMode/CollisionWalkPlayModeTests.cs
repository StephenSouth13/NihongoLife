using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Automated collider walk-through for the tight indoor zones (school doors and desks, station gate and counter,
    /// sushi tables, the bedroom, the arcade). Each zone is opened the way Play opens it in the Editor. From the
    /// entrance the floor is flooded on a 0.2 m grid with the player's own capsule (height, radius, step offset), so a
    /// cell counts only if the real CharacterController fits there; every interactable in the zone must have a
    /// reachable cell inside the interaction radius. Then the player really walks (CharacterController.Move) along the
    /// found path to each one and must arrive. Report: Bao_Cao/collision-walk/&lt;scene&gt;.txt.
    /// </summary>
    public class CollisionWalkPlayModeTests
    {
        private const float Cell = 0.2f;
        private const float Reach = 1.9f;    // InteractionDetector radius (2 m) minus a margin
        private const int MaxCells = 90000;

        private static string Out => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/collision-walk"));

        [UnityTest, Timeout(240000)] public IEnumerator Station_AllInteractablesReachable() => Run(WorldLocationCatalog.StationScene);
        [UnityTest, Timeout(240000)] public IEnumerator School_AllInteractablesReachable() => Run(WorldLocationCatalog.SchoolScene);
        [UnityTest, Timeout(240000)] public IEnumerator Sushi_AllInteractablesReachable() => Run(WorldLocationCatalog.SushiRestaurantScene);
        [UnityTest, Timeout(240000)] public IEnumerator Bedroom_AllInteractablesReachable() => Run(WorldLocationCatalog.HomeBedroomScene);
        [UnityTest, Timeout(240000)] public IEnumerator GameCenter_AllInteractablesReachable() => Run(WorldLocationCatalog.GameCenterScene);

        private static IEnumerator Run(string sceneName)
        {
            yield return SceneManager.LoadSceneAsync(sceneName);
            PlayerController player = null;
            for (float t = 0f; t < 25f; t += Time.unscaledDeltaTime)
            {
                player = Object.FindFirstObjectByType<PlayerController>();
                var flow = GameServices.TryGet(out SceneFlowController f) ? f : null;
                if (player != null && (flow == null || !flow.IsLoading) && t > 4f && SceneManager.GetActiveScene().name == sceneName) break;
                yield return null;
            }
            Assert.NotNull(player, "A player must exist after the standalone boot.");
            Assert.AreEqual(sceneName, SceneManager.GetActiveScene().name, "The zone must be the active scene.");
            yield return new WaitForSecondsRealtime(1f);

            var body = player.GetComponent<CharacterController>();
            var zone = SceneManager.GetActiveScene();
            var own = new HashSet<Collider>(player.GetComponentsInChildren<Collider>(true));

            // Targets: every interactable that lives in this zone and can currently be used.
            var targets = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(m => m is IInteractable && m.isActiveAndEnabled && m.gameObject.scene == zone && !(m is RemotePlayerAvatar))
                .Where(m => !(m is IConditionalInteractable c) || c.IsInteractionAvailable)
                .ToList();
            Assert.Greater(targets.Count, 0, "The zone has interactables.");

            // Flood the floor from where the player stands.
            Vector3 start = player.transform.position;
            var floor = new Dictionary<Vector2Int, float>();
            var parent = new Dictionary<Vector2Int, Vector2Int>();
            var queue = new Queue<Vector2Int>();
            var origin = new Vector2Int(Mathf.RoundToInt(start.x / Cell), Mathf.RoundToInt(start.z / Cell));
            floor[origin] = start.y; parent[origin] = origin; queue.Enqueue(origin);
            var steps = new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
            while (queue.Count > 0 && floor.Count < MaxCells)
            {
                var c = queue.Dequeue();
                float y = floor[c];
                foreach (var d in steps)
                {
                    var n = c + d;
                    if (floor.ContainsKey(n)) continue;
                    if (!FloorAt(n, y, body, own, out float ny)) continue;
                    if (!Fits(n, ny, body, own)) continue;
                    floor[n] = ny; parent[n] = c; queue.Enqueue(n);
                }
            }

            var report = new StringBuilder($"{sceneName}: start {start} · capsule h={body.height} r={body.radius} step={body.stepOffset} · {floor.Count} reachable cells\n");
            var unreachable = new List<string>();
            var goals = new List<(MonoBehaviour target, Vector2Int cell)>();
            foreach (var target in targets)
            {
                Vector3 point = ClosestPoint(target);
                var best = floor.Keys.Select(k => (k, d: Flat(Center(k, floor[k])) - Flat(point)))
                    .Select(x => (x.k, dist: x.d.magnitude)).OrderBy(x => x.dist).FirstOrDefault();
                bool ok = floor.Count > 0 && best.dist <= Reach && Mathf.Abs(floor[best.k] - point.y) < 3f;
                report.AppendLine($"  {(ok ? "OK  " : "MISS")} {target.GetType().Name} '{target.name}' at {point} — nearest walkable {best.dist:0.00} m");
                if (ok) goals.Add((target, best.k));
                else
                {
                    unreachable.Add($"{target.GetType().Name} '{target.name}' ({best.dist:0.00} m)");
                    // What stands between the target and the nearest walkable spot.
                    Vector3 from = Center(best.k, floor[best.k]);
                    Vector3 dir = Flat(point) - Flat(from);
                    float lastY = floor[best.k];
                    for (float s = Cell; s < dir.magnitude; s += Cell)
                    {
                        Vector3 p = from + dir.normalized * s;
                        var cell = new Vector2Int(Mathf.RoundToInt(p.x / Cell), Mathf.RoundToInt(p.z / Cell));
                        bool hasFloor = FloorAt(cell, lastY, body, own, out float y);
                        float r = body.radius;
                        Vector3 c0 = Center(cell, y);
                        var blockers = Physics.OverlapCapsule(c0 + Vector3.up * Mathf.Max(r + 0.02f, body.stepOffset + r * 0.5f), c0 + Vector3.up * (body.height - r), r * 0.98f, ~0, QueryTriggerInteraction.Ignore)
                            .Where(h => !own.Contains(h)).Select(h => $"{h.name} ({h.GetType().Name}, parent {h.transform.parent?.name}, top {h.bounds.max.y:0.00})").Distinct().ToList();
                        report.AppendLine($"      at {p.x:0.0},{p.z:0.0}: floor={hasFloor} y={y:0.00} (from {lastY:0.00}) blocked by [{string.Join("; ", blockers)}]");
                        if (blockers.Count > 0 || !hasFloor) break;
                        lastY = y;
                    }
                }
            }

            // Real walking along the found paths: the CharacterController itself must get there.
            var stuck = new List<string>();
            foreach (var (target, goal) in goals.OrderBy(g => (Flat(Center(g.cell, floor[g.cell])) - Flat(start)).sqrMagnitude).Take(8))
            {
                Teleport(player, start);
                var path = new List<Vector3>();
                for (var k = goal; ; k = parent[k]) { path.Add(Center(k, floor[k])); if (k == origin) break; }
                path.Reverse();
                float until = Time.realtimeSinceStartup + 4f + path.Count * 0.12f;
                int next = 0;
                while (next < path.Count && Time.realtimeSinceStartup < until)
                {
                    Vector3 to = Flat(path[next]) - Flat(player.transform.position);
                    if (to.magnitude < 0.25f) { next++; continue; }
                    player.transform.rotation = Quaternion.LookRotation(to.normalized);
                    body.Move((to.normalized * 3.5f + Vector3.down * 3f) * Time.deltaTime);
                    yield return null;
                }
                float left = (Flat(path[path.Count - 1]) - Flat(player.transform.position)).magnitude;
                report.AppendLine($"  walk → {target.name}: {path.Count} cells, ended {left:0.00} m from goal");
                if (left > 0.6f) stuck.Add($"{target.name} ({left:0.00} m short)");
            }
            Teleport(player, start);

            Directory.CreateDirectory(Out);
            File.WriteAllText(Path.Combine(Out, sceneName.Trim().Replace(' ', '_') + ".txt"), report.ToString());
            Debug.Log("[CollisionWalk] " + report);
            Assert.IsEmpty(unreachable, $"{sceneName}: interactables out of reach of any walkable spot: " + string.Join(", ", unreachable));
            Assert.IsEmpty(stuck, $"{sceneName}: the player got stuck walking to: " + string.Join(", ", stuck));
        }

        private static Vector3 Center(Vector2Int c, float y) => new Vector3(c.x * Cell, y, c.y * Cell);
        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>The floor under a cell, within a step of the neighbour it was reached from.</summary>
        private static bool FloorAt(Vector2Int c, float fromY, CharacterController body, HashSet<Collider> own, out float y)
        {
            y = 0f;
            Vector3 top = Center(c, fromY) + Vector3.up * (body.stepOffset + 0.4f);
            var hits = Physics.RaycastAll(top, Vector3.down, body.stepOffset + 1.2f, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => !own.Contains(h.collider)).OrderBy(h => h.distance).ToArray();
            if (hits.Length == 0) return false;
            if (Vector3.Angle(hits[0].normal, Vector3.up) > 46f) return false;
            y = hits[0].point.y;
            return y - fromY <= body.stepOffset + 0.02f && fromY - y <= 0.6f;
        }

        /// <summary>Whether the player's capsule (above step height) fits on the cell.</summary>
        private static bool Fits(Vector2Int c, float y, CharacterController body, HashSet<Collider> own)
        {
            float r = body.radius;
            Vector3 bottom = Center(c, y) + Vector3.up * Mathf.Max(r + 0.02f, body.stepOffset + r * 0.5f);
            Vector3 top = Center(c, y) + Vector3.up * Mathf.Max(body.height - r, bottom.y - y + 0.01f);
            var hits = Physics.OverlapCapsule(bottom, top, r * 0.98f, ~0, QueryTriggerInteraction.Ignore);
            return hits.All(h => own.Contains(h));
        }

        private static Vector3 ClosestPoint(MonoBehaviour target)
        {
            var interactable = (IInteractable)target;
            Vector3 p = interactable.GetTransform() != null ? interactable.GetTransform().position : target.transform.position;
            var col = target.GetComponentsInChildren<Collider>().FirstOrDefault(c => c.enabled);
            return col != null ? col.ClosestPointOnBounds(p) : p;
        }

        private static void Teleport(PlayerController player, Vector3 position)
        {
            var body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.position = position;
            body.enabled = true;
            Physics.SyncTransforms();
        }
    }
}
