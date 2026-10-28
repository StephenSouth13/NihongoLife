using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.UI;
using NihongoLife.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Life in the town, as a player meets it:
    /// · the travel screen shows the destination's picture, its name and a phrase to learn while it loads;
    /// · the classroom is in session — classmates seated at their desks (ClassroomBuilder "Students");
    /// · F picks what the player means: the nearest edge of a long counter beats a closer pivot, and someone behind a
    ///   wall loses to someone the player can see;
    /// · the map (M) on Phú Quốc draws the real coastline and every place.
    /// Captures: Bao_Cao/town-life.
    /// </summary>
    public class TownLifePlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/town-life"));

        private sealed class Probe : MonoBehaviour, IInteractable
        {
            public string GetPromptJa() => name;
            public string GetpromptEn() => name;
            public void Interact(GameObject player) { }
            public Transform GetTransform() => transform;
        }

        private static IEnumerator Town()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var dm = DialogueManager.Instance;
            while (dm != null && dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
        }

        [UnityTest]
        public IEnumerator Classroom_InSession_AndTheTravelScreenTeaches()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return Town();
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            flow.EnterZone(WorldLocationCatalog.SchoolScene, WorldLocationCatalog.SchoolEntrance, "School");
            // Mid-trip: the travel card.
            yield return new WaitForSecondsRealtime(0.55f);
            var card = Object.FindFirstObjectByType<LoadingCard>();
            Assert.NotNull(card, "The travel screen is the LoadingCard");
            Assert.IsFalse(string.IsNullOrEmpty(card.PlaceName.text), "Shows where you are going");
            var picture = card.transform.Find("Picture").GetComponent<UnityEngine.UI.RawImage>();
            Assert.NotNull(picture.texture, "A picture of the destination (Resources/UI/Loading)");
            Assert.IsFalse(string.IsNullOrEmpty(card.transform.Find("WordCard/Japanese").GetComponent<TMPro.TMP_Text>().text), "A phrase to learn while it loads");
            Capture("01_travel_screen");
            for (float t = 0f; t < 20f && (flow.IsLoading || SceneManager.GetActiveScene().name != WorldLocationCatalog.SchoolScene); t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(1.5f);

            var students = GameObject.Find("Students");
            Assert.NotNull(students, "ClassroomBuilder seats classmates");
            var seated = students.GetComponentsInChildren<SeatedPassenger>();
            Assert.AreEqual(9, seated.Length, "Nine of twelve desks taken");
            Assert.IsTrue(seated.All(s => s.IsSitting), "Everyone sits");
            Assert.IsTrue(students.GetComponentsInChildren<Collider>().Length == 0, "Classmates never block the aisle");
            foreach (var s in seated)
            {
                var animator = s.GetComponentInChildren<Animator>();
                var hips = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Hips) : null;
                Assert.NotNull(hips, s.name);
                float hipHeight = hips.position.y - s.transform.position.y;
                Assert.That(hipHeight, Is.InRange(0.25f, 0.85f), s.name + " sits on the chair, not the floor or the desk");
            }
            var cam = Camera.main;
            var player = Object.FindFirstObjectByType<PlayerController>();
            var room = students.transform.position;
            cam.transform.SetPositionAndRotation(new Vector3(1000f, 2.6f, 5.4f), Quaternion.Euler(22f, 180f, 0f));
            Capture("02_classroom_from_board");
            cam.transform.SetPositionAndRotation(new Vector3(1006.4f, 2.0f, -5.6f), Quaternion.LookRotation(new Vector3(1000f, 0.8f, 0f) - new Vector3(1006.4f, 2.0f, -5.6f)));
            Capture("03_classroom_side");
        }

        [UnityTest]
        public IEnumerator Interaction_PicksTheNearestEdge_AndWhatThePlayerCanSee()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return Town();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var detector = player.GetComponent<InteractionDetector>();
            Assert.NotNull(detector);
            var cc = player.GetComponent<CharacterController>();
            var guard = player.GetComponent<WorldBoundsGuard>();
            if (guard != null) guard.enabled = false; // the yard is outside the town on purpose
            Vector3 origin = new Vector3(-200f, 0.1f, -200f); // an empty test yard outside the town
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetPositionAndRotation(origin + Vector3.down * 0.5f, Quaternion.identity);
            floor.transform.localScale = new Vector3(30f, 1f, 30f);
            cc.enabled = false;
            player.transform.SetPositionAndRotation(origin, Quaternion.identity); // facing +Z
            cc.enabled = true;
            Camera.main.transform.SetPositionAndRotation(origin + new Vector3(0f, 3f, -5f), Quaternion.Euler(20f, 0f, 0f));

            // A long counter: pivot 3.2 m ahead, but its edge is 1 m away; a small box 1.8 m ahead.
            var counter = Make("Counter", origin + new Vector3(0f, 0.5f, 3.2f), new Vector3(6f, 1f, 4.4f));
            var box = Make("SmallBox", origin + new Vector3(1.1f, 0.5f, 1.5f), new Vector3(0.4f, 1f, 0.4f));
            yield return new WaitForSeconds(0.3f);
            Assert.AreSame(counter.GetComponent<Probe>(), detector.CurrentInteractable, "The counter's nearest edge counts, not its pivot");
            Object.Destroy(counter); Object.Destroy(box);

            // Behind a wall vs in sight: the hidden one is nearer, the visible one wins.
            var hidden = Make("Hidden", origin + new Vector3(0f, 0.9f, 2.0f), new Vector3(0.6f, 1.8f, 0.6f));
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetPositionAndRotation(origin + new Vector3(0f, 1.2f, 1.2f), Quaternion.identity);
            wall.transform.localScale = new Vector3(1.6f, 2.4f, 0.2f);
            var visible = Make("Visible", origin + new Vector3(1.6f, 0.9f, 1.9f), new Vector3(0.6f, 1.8f, 0.6f));
            yield return new WaitForSeconds(0.3f);
            Assert.AreSame(visible.GetComponent<Probe>(), detector.CurrentInteractable, "The person you can see wins over one behind a wall");
            Assert.AreEqual(2, detector.CandidateCount, "Both are in reach (Q still cycles to the hidden one)");
            Object.Destroy(hidden); Object.Destroy(wall); Object.Destroy(visible); Object.Destroy(floor);
            if (guard != null) guard.enabled = true;
        }

        private static GameObject Make(string name, Vector3 position, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetPositionAndRotation(position, Quaternion.identity);
            go.transform.localScale = size;
            go.GetComponent<Collider>().isTrigger = true;
            go.AddComponent<Probe>();
            return go;
        }

        [UnityTest]
        public IEnumerator PhuQuocMap_DrawsTheRealCoastAndPlaces()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.PhuQuocIslandScene);
            for (float t = 0f; t < 40f && (SceneManager.GetActiveScene().name != WorldLocationCatalog.PhuQuocIslandScene || Object.FindFirstObjectByType<PlayerController>() == null); t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
            var dm = DialogueManager.Instance;
            while (dm != null && dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
            var map = Object.FindFirstObjectByType<WorldMapUI>();
            Assert.NotNull(map);
            map.SetVisible(true);
            yield return null;
            var coast = map.GetComponentInParent<Canvas>().GetComponentsInChildren<UiPolygon>(true).FirstOrDefault(p => p.name == "Coast");
            Assert.NotNull(coast, "The island's coastline is drawn");
            int places = NihongoLife.PhuQuoc.PhuQuocMap.Load().places.Length;
            var labels = map.GetComponentInParent<Canvas>().GetComponentsInChildren<TMPro.TMP_Text>(true).Count(t => t.text != null && (t.text.Contains("Gánh Ngon") || t.text.Contains("Ganh Ngon") || t.text.Contains("ガインゴン")));
            Assert.Greater(labels, 0, "Gánh Ngon is on the map");
            Assert.Greater(places, 20);
            Capture("04_phuquoc_map");
            map.SetVisible(false);
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            const int width = 1600, height = 900;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            var rt = new RenderTexture(width, height, 24);
            camera.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Canvas.ForceUpdateCanvases();
            Object.Destroy(tex);
            rt.Release();
        }
    }
}
