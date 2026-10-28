using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NihongoLife.EditorTools
{
    /// <summary>Partial of CityStreetBuilder: house lots, back blocks, park, and parking. The public entry point stays CityStreetBuilder.cs.</summary>
    public static partial class CityStreetBuilder
    {
        // ─────────────────────────── lots ───────────────────────────

        /// <summary>A house lot: gravel, a garden strip, block walls on the sides and back (with colliders), gate stubs.</summary>
        private static void Lot(Ctx c, Bounds b, Vector3 front, float frontEdge, bool openFront)
        {
            Rect r;
            if (front == Vector3.back) r = Rect.MinMaxRect(b.min.x - 0.9f, frontEdge, b.max.x + 0.9f, b.max.z + 1.2f);
            else if (front == Vector3.forward) r = Rect.MinMaxRect(b.min.x - 0.9f, b.min.z - 1.2f, b.max.x + 0.9f, frontEdge);
            else if (front == Vector3.right) r = Rect.MinMaxRect(b.min.x - 1.2f, b.min.z - 0.9f, frontEdge, b.max.z + 0.9f);
            else r = Rect.MinMaxRect(frontEdge, b.min.z - 0.9f, b.max.x + 1.2f, b.max.z + 0.9f);
            var house = ToRect(b, 0f);
            foreach (var other in c.lots.Concat(c.reserved)) r = Trim(r, other, house);
            // A lot that still overlaps a reserved area (the konbini, the plaza, the station) is dropped: a fenced
            // yard must never reach into a shop.
            if (c.reserved.Any(x => x.Overlaps(r))) return;
            if (r.width < 3f || r.height < 3f) return;
            c.lots.Add(r);

            c.B(_gravel, 1.5f).Quad(r.xMin, r.xMax, r.yMin, r.yMax, 0.008f);
            // Garden strip along the front yard (the side away from the walls' gate).
            if (front == Vector3.back && b.min.z - r.yMin > 1.4f) c.B(_grass, 3f).Quad(r.xMin + 0.3f, r.xMin + 0.3f + r.width * 0.35f, r.yMin + 0.4f, b.min.z - 0.2f, 0.010f);
            if (front == Vector3.forward && r.yMax - b.max.z > 1.4f) c.B(_grass, 3f).Quad(r.xMax - 0.3f - r.width * 0.35f, r.xMax - 0.3f, b.max.z + 0.2f, r.yMax - 0.4f, 0.010f);
            if (front == Vector3.right && r.xMax - b.max.x > 1.4f) c.B(_grass, 3f).Quad(b.max.x + 0.2f, r.xMax - 0.4f, r.yMin + 0.3f, r.yMin + 0.3f + r.height * 0.4f, 0.010f);

            // Walls: everything but the front side; the front gets two stubs either side of a gate.
            var corners = new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) };
            int frontSide = front == Vector3.back ? 0 : front == Vector3.right ? 1 : front == Vector3.forward ? 2 : 3;
            for (int i = 0; i < 4; i++)
            {
                var a = corners[i]; var d = corners[(i + 1) % 4];
                if (i == frontSide)
                {
                    if (openFront) continue;
                    var dir = (d - a).normalized;
                    Wall(c, a, a + dir * 1.4f);
                    Wall(c, d - dir * 1.4f, d);
                    continue;
                }
                Wall(c, a, d);
            }
        }

        private static Rect Trim(Rect r, Rect other, Rect keep)
        {
            if (!r.Overlaps(other)) return r;
            var options = new List<Rect>();
            if (keep.yMin >= other.yMax - 0.01f) options.Add(Rect.MinMaxRect(r.xMin, other.yMax, r.xMax, r.yMax));
            if (keep.yMax <= other.yMin + 0.01f) options.Add(Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, other.yMin));
            if (keep.xMin >= other.xMax - 0.01f) options.Add(Rect.MinMaxRect(other.xMax, r.yMin, r.xMax, r.yMax));
            if (keep.xMax <= other.xMin + 0.01f) options.Add(Rect.MinMaxRect(r.xMin, r.yMin, other.xMin, r.yMax));
            return options.Count == 0 ? r : options.OrderByDescending(o => o.width * o.height).First();
        }

        /// <summary>A block wall from a to b, minus every stretch that would run through a building or a reserved area.</summary>
        private static void Wall(Ctx c, Vector2 a, Vector2 b)
        {
            var gaps = new List<(float t0, float t1)>();
            foreach (var solid in c.solids)
                if (ClipSegment(a, b, solid, out float t0, out float t1)) gaps.Add((t0, t1));
            float from = 0f;
            foreach (var (t0, t1) in gaps.OrderBy(g => g.t0))
            {
                if (t0 > from) WallPiece(c, Vector2.Lerp(a, b, from), Vector2.Lerp(a, b, t0));
                from = Mathf.Max(from, t1);
            }
            if (from < 1f) WallPiece(c, Vector2.Lerp(a, b, from), b);
        }

        /// <summary>The part of segment a→b inside rect r, as parameters [t0, t1] (Liang–Barsky).</summary>
        private static bool ClipSegment(Vector2 a, Vector2 b, Rect r, out float t0, out float t1)
        {
            t0 = 0f; t1 = 1f;
            Vector2 d = b - a;
            float[] p = { -d.x, d.x, -d.y, d.y };
            float[] q = { a.x - r.xMin, r.xMax - a.x, a.y - r.yMin, r.yMax - a.y };
            for (int i = 0; i < 4; i++)
            {
                if (Mathf.Abs(p[i]) < 1e-6f) { if (q[i] < 0f) return false; continue; }
                float t = q[i] / p[i];
                if (p[i] < 0f) t0 = Mathf.Max(t0, t); else t1 = Mathf.Min(t1, t);
                if (t0 > t1) return false;
            }
            return t1 - t0 > 1e-4f;
        }

        private static void WallPiece(Ctx c, Vector2 a, Vector2 b)
        {
            const float h = 1.15f, t = 0.15f;
            if ((b - a).magnitude < 0.3f) return;
            var dir = (b - a).normalized;
            var n = new Vector2(-dir.y, dir.x) * t * 0.5f;
            c.B(_wall, 1.2f).Prism(a - n, b - n, b + n, a + n, 0f, h);
            var cn = n * 1.35f;
            c.B(_wallCap, 1f).Prism(a - cn, b - cn, b + cn, a + cn, h, h + 0.06f);
            var col = new GameObject("Wall").AddComponent<BoxCollider>();
            col.transform.SetParent(c.walls, false);
            Vector2 mid = (a + b) * 0.5f;
            col.transform.position = new Vector3(mid.x, h * 0.5f, mid.y);
            col.transform.rotation = Quaternion.Euler(0f, -Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, 0f);
            col.size = new Vector3((b - a).magnitude, h, t);
        }

        // ─────────────────────────── back blocks ───────────────────────────

        private static void BackBlock(Ctx c, List<Rect> existing, bool rowA, bool rowB)
        {
            var prefabs = "abcdefgh".Select(ch => AssetDatabase.LoadAssetAtPath<GameObject>($"{Houses}building_{ch}.prefab")).Where(p => p != null).ToArray();
            var group = Group(c.root, "BackHouses");
            int n = 0;
            foreach (float sx in new[] { -1f, 1f })
                foreach (float ax in new[] { 22f, 34f, 46f, 58f })
                {
                    float x = sx * ax;
                    if (rowA && !(sx < 0 && ax == 46f)) n += House(c, group, prefabs[Mod(n + c.k * 3 + 1, prefabs.Length)], new Vector3(x, 0f, c.dz + AlleyN1 + 1.2f + 3.7f), 180f, existing, Vector3.back, c.dz + AlleyN1) ? 1 : 0;
                    if (rowB) n += House(c, group, prefabs[Mod(n + c.k * 5 + 4, prefabs.Length)], new Vector3(x, 0f, c.dz + 100f + AlleyS0 - 1.2f - 3.7f), 0f, existing, Vector3.forward, c.dz + 100f + AlleyS0) ? 1 : 0;
                }
        }

        private static bool House(Ctx c, Transform group, GameObject prefab, Vector3 pos, float yaw, List<Rect> existing, Vector3 front, float frontEdge)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var b = RendererBounds(go.transform);
            var r = ToRect(b, 0.3f);
            if (existing.Any(e => e.Overlaps(r)) || c.reserved.Any(e => e.Overlaps(r)) || c.lots.Any(e => e.Overlaps(ToRect(b, -0.2f))))
            {
                Object.DestroyImmediate(go);
                return false;
            }
            go.name = "BackHouse_" + group.childCount;
            existing.Add(r);
            Lot(c, b, front, frontEdge, false);
            return true;
        }

        private static void Park(Ctx c)
        {
            float z0 = c.dz + AlleyN1 + 10f, z1 = c.dz + 100f + AlleyS0 - 10f;
            var r = Rect.MinMaxRect(-62f, z0, -19.5f, z1);
            if (c.reserved.Any(x => x.Overlaps(r))) return;
            var group = Group(c.root, "Park");
            c.B(_grass, 3f).Quad(r.xMin, r.xMax, r.yMin, r.yMax, 0.009f);
            // A gravel loop path and the way in from the alley (where row A leaves a gap at x = -46).
            var path = c.B(_gravel, 1.5f);
            path.Quad(r.xMin + 4f, r.xMax - 4f, r.yMin + 3f, r.yMin + 5f, 0.011f);
            path.Quad(r.xMin + 4f, r.xMax - 4f, r.yMax - 5f, r.yMax - 3f, 0.011f);
            path.Quad(r.xMin + 4f, r.xMin + 6f, r.yMin + 3f, r.yMax - 3f, 0.011f);
            path.Quad(r.xMax - 6f, r.xMax - 4f, r.yMin + 3f, r.yMax - 3f, 0.011f);
            path.Quad(-47f, -45f, c.dz + AlleyN1, r.yMin + 5f, 0.011f);
            var tree = AssetDatabase.LoadAssetAtPath<GameObject>(Houses + "tree_large.prefab");
            var small = AssetDatabase.LoadAssetAtPath<GameObject>(Houses + "tree_small.prefab");
            var rnd = new System.Random(17 + c.k);
            for (int i = 0; i < 14; i++)
            {
                float x = Mathf.Lerp(r.xMin + 8f, r.xMax - 8f, (float)rnd.NextDouble());
                float z = Mathf.Lerp(r.yMin + 8f, r.yMax - 8f, (float)rnd.NextDouble());
                var t = (GameObject)PrefabUtility.InstantiatePrefab(i % 3 == 0 ? small : tree, group);
                t.transform.SetPositionAndRotation(new Vector3(x, 0f, z), Quaternion.Euler(0f, rnd.Next(360), 0f));
                t.transform.localScale *= 1f + (float)rnd.NextDouble() * 0.5f;
            }
            Model(KayKit + "bench.obj", group, "ParkBench", new Vector3(r.xMin + 9f, 0f, r.yMin + 7f), 90f, 1.8f);
            Model(KayKit + "bench.obj", group, "ParkBench", new Vector3(r.xMax - 9f, 0f, r.yMax - 7f), 270f, 1.8f);
            Sign(group, "ParkSign", "ひばり公園", "Hibari Park", new Vector3(-43.5f, 0f, c.dz + AlleyN1 + 0.6f), 180f);
        }

        private static void Parking(Ctx c, Dictionary<string, GameObject> cars)
        {
            float z0 = c.dz + AlleyN1 + 10f, z1 = c.dz + 100f + AlleyS0 - 10f;
            var r = Rect.MinMaxRect(19.5f, z0, 62f, z1);
            if (c.reserved.Any(x => x.Overlaps(r))) return;
            var group = Group(c.root, "CoinParking");
            c.B(_parking, 4f).Quad(r.xMin, r.xMax, r.yMin, r.yMax, 0.009f);
            var paint = c.B(_paint, 1f);
            var keys = cars.Keys.ToArray();
            int i = 0;
            for (float x = r.xMin + 2f; x + 2.6f < r.xMax - 1f; x += 2.6f, i++)
            {
                paint.Quad(x, x + 0.1f, r.yMin + 1f, r.yMin + 6f, 0.016f);
                paint.Quad(x, x + 0.1f, r.yMax - 6f, r.yMax - 1f, 0.016f);
                if (i % 3 != 1 && keys.Length > 0)
                {
                    var car = (GameObject)PrefabUtility.InstantiatePrefab(cars[keys[Mod(i + c.k, keys.Length)]], group);
                    car.transform.SetPositionAndRotation(new Vector3(x + 1.3f, 0f, (i % 2 == 0 ? r.yMin + 3.6f : r.yMax - 3.6f)), Quaternion.Euler(0f, i % 2 == 0 ? 0f : 180f, 0f));
                    car.name = "ParkedCar_" + i;
                }
            }
            Sign(group, "ParkingSign", "P  コインパーキング", "Coin parking", new Vector3(r.xMin + 2f, 0f, r.yMin - 0.5f), 180f);
        }
    }
}
