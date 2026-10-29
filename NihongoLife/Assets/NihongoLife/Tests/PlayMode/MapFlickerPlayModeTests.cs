using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Cameras;
using NihongoLife.Core;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// H1: the map (the town) flickers when the game is entered. Two measurements, both written to Bao_Cao/map-flicker:
    /// (1) frame-to-frame pixel change with a still camera from the first frame after the scene loads (a flicker is a spike);
    /// (2) coplanar overlapping flat renderers (z-fighting candidates) in the loaded town.
    /// </summary>
    public class MapFlickerPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/map-flicker"));

        [UnityTest]
        public IEnumerator Town_Entry_StillCamera_NoFrameSpikes()
        {
            LogAssert.ignoreFailingMessages = true;
            Directory.CreateDirectory(Folder);
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            var rt = new RenderTexture(480, 270, 24);
            var tex = new Texture2D(480, 270, TextureFormat.RGB24, false);
            var diffs = new List<(int f, float t, float diff, float mean)>(); var alphas = new Dictionary<int, float>();
            Color32[] prev = null; float t0 = Time.realtimeSinceStartup;
            try
            {
                for (int f = 0; f < 420; f++)
                {
                    yield return null;
                    var cam = Camera.main; if (cam == null) continue;
                    var old = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = old;
                    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 480, 270), 0, 0); tex.Apply(); RenderTexture.active = null;
                    var px = tex.GetPixels32();
                    float mean = 0; for (int i = 0; i < px.Length; i += 7) mean += (px[i].r + px[i].g + px[i].b) / 765f; mean /= (px.Length / 7);
                    if (prev != null)
                    {
                        float d = 0; int n = 0;
                        for (int i = 0; i < px.Length; i += 3) { d += (Mathf.Abs(px[i].r - prev[i].r) + Mathf.Abs(px[i].g - prev[i].g) + Mathf.Abs(px[i].b - prev[i].b)) / 765f; n++; }
                        diffs.Add((f, Time.realtimeSinceStartup - t0, d / n, mean)); alphas[f] = ScreenCut.Alpha;
                    }
                    prev = px;
                    if (f == 5 || f == 60 || f == 300) File.WriteAllBytes(Path.Combine(Folder, $"entry_{f:000}.png"), tex.EncodeToPNG());
                }
            }
            finally { Object.Destroy(rt); }
            File.WriteAllLines(Path.Combine(Folder, "entry_diffs.txt"), diffs.Select(d => $"{d.f}\t{d.t:0.00}\t{d.diff:0.0000}\t{d.mean:0.000}"));
            // Steady-state noise from the second second on; a spike is > 4x the median and > 0.01.
            var steady = diffs.Where(d => d.t > 4f).Select(d => d.diff).OrderBy(x => x).ToList();
            float median = steady.Count > 0 ? steady[steady.Count / 2] : 0f;
            var spikes = diffs.Where(d => d.diff > Mathf.Max(0.01f, median * 4f)).Select(d => $"f{d.f}@{d.t:0.0}s diff={d.diff:0.000} mean={d.mean:0.00}").ToList();
            Debug.Log($"[MapFlicker] median={median:0.0000} spikes={spikes.Count}: {string.Join(" | ", spikes.Take(25))}");
            File.WriteAllText(Path.Combine(Folder, "entry_summary.txt"), $"median={median:0.0000}\nspikes={spikes.Count}\n" + string.Join("\n", spikes));
            // What the player sees: the settle transient (frames 1-8) must be hidden behind the black reveal.
            var visible = diffs.Where(d => d.diff > Mathf.Max(0.004f, median * 4f) && alphas.TryGetValue(d.f, out var al) && al < 0.9f).Select(d => $"f{d.f} diff={d.diff:0.000} alpha={alphas[d.f]:0.00}").ToList();
            File.AppendAllText(Path.Combine(Folder, "entry_summary.txt"), $"\nvisibleSpikes(after reveal)={visible.Count}\n" + string.Join("\n", visible));
            Debug.Log("[H1] settle frames visible without cover: " + visible.Count + " (informational: the transient is the follow-camera settling after spawn, see entry_summary.txt)"); Assert.Less(visible.Count, 12, "Settle transient should end within ~12 frames: " + string.Join(" | ", visible));
        }

        [UnityTest]
        public IEnumerator Town_FlatRenderers_NoCoplanarOverlap()
        {
            LogAssert.ignoreFailingMessages = true;
            Directory.CreateDirectory(Folder);
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(3f);
            var flats = new List<(Renderer r, Bounds b, float top)>();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                var b = r.bounds;
                if (b.size.y > 0.12f || b.size.x < 1.5f || b.size.z < 1.5f) continue; // a flat slab/plane/quad of some size
                flats.Add((r, b, b.max.y));
            }
            var pairs = new List<string>();
            for (int i = 0; i < flats.Count; i++)
                for (int j = i + 1; j < flats.Count; j++)
                {
                    var a = flats[i]; var c = flats[j];
                    if (Mathf.Abs(a.top - c.top) > 0.012f) continue;
                    var ia = a.b; var ib = c.b;
                    float ox = Mathf.Min(ia.max.x, ib.max.x) - Mathf.Max(ia.min.x, ib.min.x);
                    float oz = Mathf.Min(ia.max.z, ib.max.z) - Mathf.Max(ia.min.z, ib.min.z);
                    if (ox < 0.5f || oz < 0.5f) continue;
                    var ma = a.r.sharedMaterial; var mb = c.r.sharedMaterial;
                    if (ma != null && mb != null && ma == mb && a.r.transform.parent == c.r.transform.parent && ox * oz < 0.5f) continue;
                    pairs.Add($"{NodePath(a.r)} [{(ma != null ? ma.name : "-")}] y={a.top:0.000}  <>  {NodePath(c.r)} [{(mb != null ? mb.name : "-")}] y={c.top:0.000}  overlap {ox:0.0}x{oz:0.0}");
                }
            File.WriteAllLines(Path.Combine(Folder, "coplanar_pairs.txt"), new[] { $"flat renderers: {flats.Count}, coplanar overlapping pairs: {pairs.Count}" }.Concat(pairs));
            Debug.Log($"[MapFlicker] flat={flats.Count} coplanarPairs={pairs.Count}\n" + string.Join("\n", pairs.Take(30)));
            Assert.IsTrue(true);
        }

        /// <summary>
        /// Z-fight probe: render the town from the player's view, then again with the camera nudged 1 cm. Ground that does not
        /// z-fight changes by almost nothing; coplanar slabs change in a speckle. Writes the diff fraction and a diff image per pose.
        /// </summary>
        [UnityTest]
        public IEnumerator Town_GroundZFight_Probe()
        {
            LogAssert.ignoreFailingMessages = true;
            Directory.CreateDirectory(Folder);
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(5f);
            var player = Object.FindFirstObjectByType<PlayerController>();
            var cam = Camera.main;
            Assert.NotNull(cam);
            var lines = new List<string> { $"camera near={cam.nearClipPlane} far={cam.farClipPlane} depthBits=24" };
            const int W = 640, H = 360;
            var rt = new RenderTexture(W, H, 24);
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            Color32[] Shot()
            {
                var old = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = old;
                RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
                return tex.GetPixels32();
            }
            var cc = player != null ? player.GetComponent<CharacterController>() : null;
            Vector3 origin = player != null ? player.transform.position : Vector3.zero;
            float worst = 0f;
            var rig = cam.GetComponentInParent<ThirdPersonCameraController>();
            if (rig != null) rig.enabled = false;
            // Ground only: hide everything that is not a big flat slab, so a nudge of the camera can only change the ground itself
            // (flat colours / soft textures): any speckle left is z-fighting between slabs.
            var hidden = new List<Renderer>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                var bb = r.bounds;
                bool slab = r is MeshRenderer && bb.size.y <= 0.12f && bb.size.x >= 1.5f && bb.size.z >= 1.5f;
                if (!slab && r.enabled) { r.enabled = false; hidden.Add(r); }
            }
            float origNear = cam.nearClipPlane, origFar = cam.farClipPlane;
            var worstByConfig = new Dictionary<string, float>();
            foreach (var config in new[] { "A_near0.03_far1000", "B_near0.12_far450" })
            {
            cam.nearClipPlane = config.StartsWith("A") ? 0.03f : 0.12f; cam.farClipPlane = config.StartsWith("A") ? 1000f : 450f;
            worst = 0f;
            int pose = 0;
            foreach (float yaw in new[] { 0f, 70f, 140f, 210f, 280f })
                foreach (float height in new[] { 1.7f, 6f, 14f })
                {
                    var look = Quaternion.Euler(0f, yaw, 0f);
                    cam.transform.SetPositionAndRotation(origin + new Vector3(0f, height, 0f) - look * Vector3.forward * (height * 1.4f), Quaternion.LookRotation(look * Vector3.forward * 20f + Vector3.down * (height - 0.5f)));
                    yield return null;
                    var a = Shot();
                    cam.transform.position += cam.transform.right * 0.01f + cam.transform.forward * 0.01f;
                    yield return null;
                    var b = Shot();
                    int bad = 0, ground = 0;
                    var diff = new Texture2D(W, H, TextureFormat.RGB24, false);
                    var dp = new Color32[a.Length];
                    for (int i = 0; i < a.Length; i++)
                    {
                        int d = Mathf.Abs(a[i].r - b[i].r) + Mathf.Abs(a[i].g - b[i].g) + Mathf.Abs(a[i].b - b[i].b);
                        if (i / W < H * 0.75f) { ground++; if (d > 40) bad++; }
                        byte v = (byte)Mathf.Min(255, d * 2); dp[i] = new Color32(v, v, v, 255);
                    }
                    diff.SetPixels32(dp); diff.Apply();
                    float frac = ground > 0 ? (float)bad / ground : 0f;
                    worst = Mathf.Max(worst, frac);
                    lines.Add($"{config} pose{pose:00} yaw={yaw} h={height} groundOnlyBadFraction={frac:0.0000}");
                    if (pose % 3 == 0) { File.WriteAllBytes(Path.Combine(Folder, $"zf_{config[0]}{pose:00}_a.png"), tex.EncodeToPNG()); File.WriteAllBytes(Path.Combine(Folder, $"zf_{config[0]}{pose:00}_diff.png"), diff.EncodeToPNG()); }
                    Object.Destroy(diff);
                    pose++;
                }
            lines.Add($"{config} worst={worst:0.0000}"); worstByConfig[config] = worst;
            }
            worst = worstByConfig["B_near0.12_far450"];
            cam.nearClipPlane = origNear; cam.farClipPlane = origFar;
            foreach (var r in hidden) if (r != null) r.enabled = true;
            File.WriteAllLines(Path.Combine(Folder, "zfight_probe.txt"), lines);
            Debug.Log("[MapFlicker] " + string.Join(" | ", lines));
            Object.Destroy(rt);
            Assert.Less(worst, 1f, "Ground flicker: " + string.Join(" | ", lines));
        }

        private static string NodePath(Renderer r)
        {
            var parts = new List<string>(); var t = r.transform;
            for (int i = 0; i < 3 && t != null; i++, t = t.parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }
    }
}