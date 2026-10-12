using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.NPC;
using NihongoLife.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Every NPC is a Kid now (no Mixamo adults left): checks each place's NPCs have a Kid model with an animator
    /// controller, measures their height against the player, and photographs them in place.
    /// Captures: Bao_Cao/character-regression/npc_*.jpg
    /// </summary>
    public class KidsNpcScalePlayModeTests
    {
        private static string Out => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/character-regression"));

        [UnityTest, Timeout(400000)]
        public IEnumerator AllNpcs_AreKids_AtPlayerScale()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return Settle();
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            yield return CheckPlace("city");
            var zones = new (string name, string scene, string spawn, string back)[]
            {
                ("station", WorldLocationCatalog.StationScene, WorldLocationCatalog.StationEntrance, WorldLocationCatalog.CityStationReturn),
                ("sushi", WorldLocationCatalog.SushiRestaurantScene, WorldLocationCatalog.SushiEntrance, WorldLocationCatalog.CitySushiReturn),
                ("school", WorldLocationCatalog.SchoolScene, WorldLocationCatalog.SchoolEntrance, WorldLocationCatalog.CitySchoolReturn),
            };
            foreach (var z in zones)
            {
                flow.EnterZone(z.scene, z.spawn, z.name);
                for (float t = 0f; t < 25f && (SceneManager.GetActiveScene().name != z.scene || flow.IsLoading); t += Time.unscaledDeltaTime) yield return null;
                yield return Settle();
                yield return CheckPlace(z.name);
                flow.ExitZone(z.scene, z.back);
                for (float t = 0f; t < 25f && (SceneManager.GetActiveScene().name != WorldLocationCatalog.CityScene || flow.IsLoading); t += Time.unscaledDeltaTime) yield return null;
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        private static IEnumerator CheckPlace(string place)
        {
            var player = Object.FindFirstObjectByType<PlayerController>();
            float playerHeight = Height(player.transform);
            var npcs = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).Where(n => n.gameObject.scene == SceneManager.GetActiveScene() && n.gameObject.activeInHierarchy).ToList();
            Debug.Log($"[KidsNpc] {place}: {npcs.Count} NPCs, player {playerHeight:0.00} m");
            int shot = 0;
            foreach (var npc in npcs)
            {
                var animator = npc.GetComponentInChildren<Animator>();
                Assert.NotNull(animator, $"{place}/{npc.name}: has a model");
                Assert.NotNull(animator.runtimeAnimatorController, $"{place}/{npc.name}: animated");
                StringAssert.StartsWith("NL_Kid_", animator.runtimeAnimatorController.name, $"{place}/{npc.name}: Kid controller");
                float h = Height(npc.transform);
                Debug.Log($"[KidsNpc] {place}/{npc.DisplayName}: {h:0.00} m, model {animator.gameObject.name}");
                Assert.That(h, Is.InRange(playerHeight * 0.85f, playerHeight * 1.4f), $"{place}/{npc.name}: kid-sized, staff a little taller");
                if (shot < 2)
                {
                    shot++;
                    var cam = Camera.main;
                    var controller = cam.GetComponent<NihongoLife.Cameras.ThirdPersonCameraController>();
                    if (controller != null) controller.enabled = false;
                    // Eye-level, in front of the NPC (across the counter when they stand behind one).
                    Vector3 head = npc.transform.position + Vector3.up * (h * 0.85f);
                    cam.transform.position = head + npc.transform.forward * 2.4f + Vector3.up * 0.15f;
                    cam.transform.LookAt(head - Vector3.up * 0.35f);
                    yield return null;
                    Shoot(cam, $"npc_{place}_{shot}");
                    if (controller != null) controller.enabled = true;
                }
            }
        }

        private static float Height(Transform t)
        {
            var renderers = t.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.enabled).ToArray();
            if (renderers.Length == 0) return 0f;
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b.size.y;
        }

        private static IEnumerator Settle()
        {
            for (float t = 0f; t < 25f && (Object.FindFirstObjectByType<PlayerController>() == null || t < 2.5f); t += Time.unscaledDeltaTime) yield return null;
            var dm = DialogueManager.Instance;
            for (int guard = 0; guard < 40 && dm != null && dm.IsOpen; guard++) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); }
            Object.FindFirstObjectByType<DayNightCycle>()?.SetHour(10.5f);
            WeatherCycle.Instance?.Force(WeatherCycle.Kind.Clear);
            yield return new WaitForSecondsRealtime(1f);
        }

        private static void Shoot(Camera cam, string name)
        {
            var rt = new RenderTexture(960, 540, 24);
            cam.targetTexture = rt;
            cam.Render();
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Out);
            File.WriteAllBytes(Path.Combine(Out, name + ".jpg"), tex.EncodeToJPG(86));
            RenderTexture.active = old;
            cam.targetTexture = null;
            rt.Release();
            Object.Destroy(rt);
            Object.Destroy(tex);
        }
    }
}
