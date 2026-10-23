// LOCAL ONLY (trailer v4b capture) - not committed. Records real NPC conversations with the dialogue UI.
using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Cameras;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.NPC;
using NihongoLife.Player;
using NihongoLife.UI;
using NihongoLife.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    public class DialogueRecordingTests
    {
        private static string Root => System.Environment.GetEnvironmentVariable("NL_FRAME_DIR") is { Length: > 0 } d ? d : Path.Combine(Path.GetTempPath(), "NLDialogue");
        private RenderTexture _rt; private Texture2D _tex; private Camera _cam; private PlayerController _player;
        private static readonly string[] Allow = { "DialogueCanvas" };

        [UnityTest, Explicit, Timeout(1800000)]
        public IEnumerator Dialogue_RecordFrames()
        {
            Time.captureFramerate = 30;
            _rt = new RenderTexture(1920, 1080, 24); _tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            SceneManager.LoadScene(WorldLocationCatalog.CityScene);
            for (int i = 0; i < 150 && Object.FindFirstObjectByType<PlayerController>() == null; i++) yield return null;
            for (int i = 0; i < 60; i++) yield return null;
            _player = Object.FindFirstObjectByType<PlayerController>();
            _player.InputLocked = true;
            var det = _player.GetComponent<InteractionDetector>(); if (det != null) det.enabled = false;
            _cam = Camera.main;
            var follow = _cam.GetComponent<ThirdPersonCameraController>(); if (follow != null) follow.enabled = false;
            var city = SceneManager.GetSceneByName(WorldLocationCatalog.CityScene);
            yield return UiBeats();
            yield return DoNpcs("city", city, 3);
            string[][] zones = {
                new[]{"station", WorldLocationCatalog.StationScene, WorldLocationCatalog.StationEntrance},
                new[]{"school", WorldLocationCatalog.SchoolScene, WorldLocationCatalog.SchoolEntrance},
                new[]{"sushi", WorldLocationCatalog.SushiRestaurantScene, WorldLocationCatalog.SushiEntrance},
            };
            Scene prev = default;
            foreach (var z in zones)
            {
                var load = SceneManager.LoadSceneAsync(z[1], LoadSceneMode.Additive);
                float t0 = Time.realtimeSinceStartup;
                while (!load.isDone && Time.realtimeSinceStartup - t0 < 120f) yield return null;
                var zone = SceneManager.GetSceneByName(z[1]);
                foreach (var r in city.GetRootGameObjects()) if (r.GetComponent<SceneZoneVisibility>() != null) r.SetActive(false);
                SceneManager.SetActiveScene(zone);
                foreach (var r in zone.GetRootGameObjects()) foreach (var pc in r.GetComponentsInChildren<ZonePreviewCamera>(true)) pc.gameObject.SetActive(false);
                if (prev.IsValid() && prev.isLoaded) { var u = SceneManager.UnloadSceneAsync(prev); while (u != null && !u.isDone) yield return null; }
                prev = zone;
                for (int i = 0; i < 60; i++) yield return null;
                yield return DoNpcs(z[0], zone, 2);
            }
            Time.captureFramerate = 0;
            Assert.Pass();
        }

        private IEnumerator DoNpcs(string tag, Scene scene, int max)
        {
            var npcs = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<NPCController>(true)).Where(n => n.gameObject.activeInHierarchy).Take(max).ToList();
            Debug.Log($"[DialogueRec] {tag}: {npcs.Count} npcs: {string.Join(",", npcs.Select(n => n.NpcId))}");
            foreach (var npc in npcs)
            {
                Cancel();
                Vector3 p = npc.transform.position; Vector3 f = npc.transform.forward; f.y = 0; f.Normalize();
                Vector3 stand = p + f * 1.6f; stand.y = p.y + 0.08f;
                var body = _player.GetComponent<CharacterController>(); if (body) body.enabled = false;
                _player.transform.SetPositionAndRotation(stand, Quaternion.LookRotation(p - stand));
                if (body) body.enabled = true;
                Vector3 side = Vector3.Cross(Vector3.up, -f);
                Vector3 cam = stand + f * 1.5f + side * 0.8f + Vector3.up * 1.75f;
                Vector3 look = p + Vector3.up * 1.4f;
                for (int i = 0; i < 10; i++) yield return null;
                try { npc.Interact(_player.gameObject); } catch (System.Exception e) { Debug.LogWarning($"[DialogueRec] {npc.NpcId} {e.Message}"); continue; }
                for (int i = 0; i < 45; i++) yield return null;
                var dm = DialogueManager.Instance;
                if (dm != null && !dm.IsOpen)
                {
                    try { typeof(NPCController).GetMethod("StartFallbackDialogue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(npc, new object[] { null }); } catch (System.Exception e) { Debug.LogWarning($"[DialogueRec] fallback {npc.NpcId}: {e.Message}"); }
                    for (int i = 0; i < 10; i++) yield return null;
                }
                if (dm == null || !dm.IsOpen) { Debug.Log($"[DialogueRec] {npc.NpcId} no dialogue"); continue; }
                string dir = Path.Combine(Root, $"{tag}_{npc.NpcId}"); Directory.CreateDirectory(dir);
                int idx = 0;
                for (int beat = 0; beat < 2 && dm.IsOpen; beat++)
                {
                    for (int fr = 0; fr < 120; fr++)
                    {
                        float k = (idx) / 360f;
                        Vector3 c = cam + (look - cam).normalized * (0.5f * k);
                        _cam.transform.SetPositionAndRotation(c, Quaternion.LookRotation(look - c));
                        HideChrome();
                        yield return null;
                        Grab(Path.Combine(dir, $"f{idx++:00000}.jpg"));
                    }
                    try { dm.SelectChoice(0); } catch { }
                    for (int i = 0; i < 2; i++) yield return null;
                    if (dm.IsOpen) { try { dm.ContinueDialogue(); } catch { } }
                }
                Debug.Log($"[DialogueRec] {npc.NpcId} -> {idx} frames");
                Cancel();
            }
        }


        private IEnumerator UiBeats()
        {
            Vector3 c = _player.transform.position + new Vector3(4f, 3f, -6f);
            _cam.transform.SetPositionAndRotation(c, Quaternion.LookRotation(_player.transform.position + Vector3.up * 1.2f - c));
            var hud = Object.FindFirstObjectByType<HUDUI>();
            System.Func<string, Component> find = n => Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(m => m.GetType().Name == n);
            System.Action<object, string, object[]> call = (o, m, a) => { if (o == null) { Debug.Log($"[DialogueRec] missing target for {m}"); return; } try { o.GetType().GetMethod(m, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static)?.Invoke(o, a); } catch (System.Exception e) { Debug.LogWarning($"[DialogueRec] {m}: {e.InnerException?.Message ?? e.Message}"); } };
            var beats = new (string id, System.Action open, System.Action close)[] {
                ("ui_settings", () => { var su = (object)find("SettingsUI") ?? hud?.GetType().GetField("_settingsUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(hud); Debug.Log($"[DialogueRec] settings target {su}"); call(su, "Show", null); }, () => call(find("SettingsUI"), "Hide", null)),
                ("ui_inventory", () => call(hud, "SetInventoryVisible", new object[]{true}), () => call(hud, "SetInventoryVisible", new object[]{false})),
                ("ui_map", () => call(find("WorldMapUI"), "SetVisible", new object[]{true}), () => call(find("WorldMapUI"), "SetVisible", new object[]{false})),
                ("ui_journal", () => TaskJournalUI.Open(), () => TaskJournalUI.Close()),
                ("ui_friends", () => call(find("FriendsUI"), "Show", null), () => call(find("FriendsUI"), "Hide", null)),
            };
            foreach (var b in beats)
            {
                b.open();
                for (int i = 0; i < 20; i++) yield return null;
                string dir = Path.Combine(Root, b.id); Directory.CreateDirectory(dir);
                for (int f = 0; f < 75; f++) { yield return null; Grab(Path.Combine(dir, $"f{f:00000}.jpg")); }
                b.close();
                for (int i = 0; i < 10; i++) yield return null;
            }
        }

        private void HideChrome()
        {
            var hud = Object.FindFirstObjectByType<HUDUI>();
            if (hud == null) return;
            foreach (Transform child in hud.transform)
                if (System.Array.IndexOf(Allow, child.name) < 0 && child.gameObject.activeSelf) child.gameObject.SetActive(false);
            var img = hud.GetComponent<Image>(); if (img != null) img.enabled = false;
        }

        private static void Cancel()
        {
            var dm = DialogueManager.Instance;
            for (int i = 0; i < 8 && dm != null && dm.IsOpen; i++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
        }

        private void Grab(string path)
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera))
                .OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = _cam;
                canvases[i].planeDistance = _cam.nearClipPlane + 0.05f + 0.002f * (canvases.Length - i);
            }
            Canvas.ForceUpdateCanvases();
            var old = _cam.targetTexture; _cam.targetTexture = _rt; _cam.Render(); _cam.targetTexture = old;
            var a = RenderTexture.active; RenderTexture.active = _rt; _tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); _tex.Apply(); RenderTexture.active = a;
            File.WriteAllBytes(path, _tex.EncodeToJPG(92));
        }
    }
}
