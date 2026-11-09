using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    public partial class DemoRecordingTests
    {
        // scene | spawn | the playable rectangle (x min, z min, x max, z max; empty = from the scene's colliders around the player).
        private static readonly (string scene, string spawn, Rect area)[] MapAreas =
        {
            (WorldLocationCatalog.CityScene, null, Rect.MinMaxRect(-52f, -38f, 60f, 50f)),
            ("20_StationDistrict", "station_entrance", default),
            ("30_SushiRestaurant", "sushi_entrance", default),
            ("35_HibariClinic", "clinic_entrance", default),
            ("40_ HIBARICLASS", "school_entrance", default),
            ("45_HomeBedroom", "home_bedroom", default),
            ("50_GameCenter", "game_center_entrance", default),
            ("55_MusicShop", "music_shop_entrance", default),
            ("56_FashionStore", "fashion_store_entrance", default),
            ("60_MidoriIsland", "midori_station", default),
            ("65_PhuQuocIsland", "phuquoc_ganh_ngon", default),
        };

        /// <summary>Every area seen from above for the report and the deck: a straight-down map (roofs and ceilings of
        /// indoor rooms hidden) and a 50° aerial view. Output: Bao_Cao/maps/map_&lt;scene&gt;.png, aerial_&lt;scene&gt;.png.
        /// Explicit: -testFilter Maps_TopDown.</summary>
        [UnityTest, Explicit, Timeout(1800000)]
        public IEnumerator Maps_TopDown()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/maps"));
            Directory.CreateDirectory(dir);
            SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return WaitZone(WorldLocationCatalog.CityScene, 60f);
            foreach (var (scene, spawn, area) in MapAreas)
            {
                if (spawn != null)
                {
                    _flow.TransferZone(scene, spawn, scene);
                    yield return WaitZone(scene, 90f);
                }
                CloseDialogues();
                if (DayNightCycle.Active != null) DayNightCycle.Active.SetHour(11f);
                yield return Frames(1.5f);
                string name = scene.Replace(" ", "");
                bool indoor = spawn != null && !scene.Contains("Island"); // the station hall too: its roof hides the gates and platforms
                Rect rect = area.width > 0f ? area : PlayableRect(_player.transform.position, scene.Contains("PhuQuoc") ? 70f : 0f);
                var hidden = indoor ? HideCeilings(_player.transform.position.y + 2.4f) : new Renderer[0];
                bool fog = RenderSettings.fog;
                RenderSettings.fog = false; // from 150 m up the town's fog washed every outdoor picture out
                try
                {
                    Shoot(Path.Combine(dir, $"map_{name}.png"), rect, true);
                    Shoot(Path.Combine(dir, $"aerial_{name}.png"), rect, false);
                }
                finally { RenderSettings.fog = fog; foreach (var r in hidden) if (r != null) r.enabled = true; }
                Debug.Log($"[Maps] {scene}: {rect}");
            }
        }

        /// <summary>The colliders of the active scene near the player (the room, the island town); a fixed half-size for an open island.</summary>
        private static Rect PlayableRect(Vector3 around, float halfSize)
        {
            if (halfSize > 0f) return Rect.MinMaxRect(around.x - halfSize, around.z - halfSize * 0.62f, around.x + halfSize, around.z + halfSize * 0.62f);
            var active = SceneManager.GetActiveScene();
            Bounds b = new Bounds(around, Vector3.one * 4f);
            foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (c.gameObject.scene != active || c.isTrigger || !c.enabled) continue;
                var cb = c.bounds;
                // Ground planes, sea and far scenery would frame the whole horizon.
                if (cb.size.x > 150f || cb.size.z > 150f || Vector3.Distance(cb.center, around) > 120f) continue;
                b.Encapsulate(cb);
            }
            return Rect.MinMaxRect(b.min.x - 1f, b.min.z - 1f, b.max.x + 1f, b.max.z + 1f);
        }

        /// <summary>Indoors the ceiling, roof and hanging lamps would hide the room from above.</summary>
        private static Renderer[] HideCeilings(float above)
        {
            var active = SceneManager.GetActiveScene();
            var list = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                .Where(r => r.enabled && r.gameObject.scene == active && r.bounds.min.y > above).ToArray();
            foreach (var r in list) r.enabled = false;
            return list;
        }

        private static void Shoot(string path, Rect rect, bool straightDown)
        {
            const int W = 1600, H = 1000;
            var go = new GameObject("MapShotCamera");
            var cam = go.AddComponent<Camera>();
            cam.cullingMask = ~((1 << 5) | LayerMask.GetMask("MapRoof"));
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.11f, 0.16f);
            cam.aspect = (float)W / H;
            var centre = new Vector3(rect.center.x, 0f, rect.center.y);
            float ground = Physics.Raycast(centre + Vector3.up * 200f, Vector3.down, out var hit, 400f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : 0f;
            centre.y = ground;
            // A wide room turns the map on its side so it fills the 16:10 picture.
            bool turn = rect.height > rect.width * 1.15f;
            float w = turn ? rect.height : rect.width, h = turn ? rect.width : rect.height;
            if (straightDown)
            {
                cam.orthographic = true;
                cam.orthographicSize = Mathf.Max(h * 0.5f, w * 0.5f / cam.aspect) * 1.04f;
                cam.transform.SetPositionAndRotation(centre + Vector3.up * 150f, Quaternion.Euler(90f, turn ? 90f : 0f, 0f));
                cam.nearClipPlane = 1f; cam.farClipPlane = 400f;
            }
            else
            {
                cam.fieldOfView = 40f;
                float distance = Mathf.Max(w / cam.aspect, h) * 1.25f / (2f * Mathf.Tan(20f * Mathf.Deg2Rad));
                var rotation = Quaternion.Euler(50f, turn ? 90f : 0f, 0f);
                cam.transform.SetPositionAndRotation(centre - rotation * Vector3.forward * distance, rotation);
                cam.nearClipPlane = 0.5f; cam.farClipPlane = distance * 4f;
            }
            var rt = new RenderTexture(W, H, 24);
            cam.targetTexture = rt;
            cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = old;
            cam.targetTexture = null;
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(tex);
            Object.Destroy(go);
        }
    }
}
