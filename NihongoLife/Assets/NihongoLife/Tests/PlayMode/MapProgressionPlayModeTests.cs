using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Exam;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.Save;
using NihongoLife.Scenario;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The map roadmap on a fresh save in the real town: at first only Hibari Mart and the school are open (the room
    /// opens with the arrival); Sushi Hibari needs the opening AND 100 knowledge and announces itself with the unlock
    /// card; the station needs 160 and the sushi quest; the two gateway quests (Kim's Game Center afternoon, the Midori
    /// trip) are played through their dialogue and open the Game Center and the island; opened places stay open;
    /// exams pay knowledge. Captures: Bao_Cao/map-progression.
    /// </summary>
    public class MapProgressionPlayModeTests
    {
        private const string Folder = "map-progression";
        private IProgressRepository _repo;

        [TearDown]
        public void TearDown()
        {
            LocationUnlocks.Bypass = true;
            StoryDirector.Bypass = true;
            MapUnlockCardUI.Close();
            FreePlayCardUI.Close();
            TaskJournalUI.Close();
        }

        [UnityTest]
        public IEnumerator Roadmap_FreshSave_OpensPlaceByPlace()
        {
            LogAssert.ignoreFailingMessages = true;
            SceneManager.LoadScene("00_Bootstrap");
            yield return new WaitForSecondsRealtime(0.5f);
            _repo = GameServices.Get<IProgressRepository>();
            var progress = _repo.GetProgress();
            progress.completedScenarios.Clear();
            progress.activeScenarioId = progress.activeScenarioNodeId = string.Empty;
            progress.knowledge = 0;
            progress.storyFlags?.RemoveAll(f => f.StartsWith("map.") || f.StartsWith("exam.done:") || f == StoryDirector.FreePlayFlag);
            _repo.SaveProgress(progress);
            LocationUnlocks.Bypass = false;
            StoryDirector.Bypass = false;
            LocationUnlocks.ResetVisits();

            SceneManager.LoadScene(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 20f && ScenarioManager.Instance?.CurrentScenario == null; t += Time.unscaledDeltaTime) yield return null;
            PlayerStatus.Instance?.ReloadProgress();
            PlayerStatus.Instance?.DebugSetKnowledge(0);
            Assert.AreEqual("scenario.intro.arrival", ScenarioManager.Instance.CurrentScenario.id);

            // Day one: Mart and school; the room opens with the arrival; the rest is locked.
            Assert.IsTrue(LocationUnlocks.IsUnlocked(WorldLocationCatalog.SchoolScene), "The school is open.");
            Assert.IsTrue(LocationUnlocks.IsUnlocked(WorldLocationCatalog.HomeBedroomScene), "The room opens with the arrival.");
            foreach (var scene in new[] { WorldLocationCatalog.SushiRestaurantScene, WorldLocationCatalog.StationScene, WorldLocationCatalog.GameCenterScene, WorldLocationCatalog.MidoriIslandScene })
                Assert.IsFalse(LocationUnlocks.IsUnlocked(scene), scene + " is locked on day one.");
            var sushiDoor = Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None).First(p => p.TargetScene == WorldLocationCatalog.SushiRestaurantScene);
            StringAssert.Contains("Đã khóa", sushiDoor.GetpromptEn());
            StringAssert.Contains("trí tuệ", sushiDoor.GetpromptEn());
            Note("sushi door: " + sushiDoor.GetpromptEn());

            // The opening (five quests), as the story flow plays it.
            foreach (var id in StoryDirector.PrologueIds.ToList())
            {
                yield return EnsureRunning(id);
                Finish();
                yield return new WaitForSecondsRealtime(0.4f);
                Object.FindFirstObjectByType<ResultUI>()?.BackToTown();
                FreePlayCardUI.Close();
                MapUnlockCardUI.Close();
                yield return null;
            }
            Assert.IsFalse(StoryDirector.InPrologue);
            Note($"after the opening: knowledge={PlayerStatus.Instance.Knowledge}, map.sushi={StoryFlags.Has("map." + WorldLocationCatalog.SushiRestaurantScene)}, running={ScenarioManager.Instance.CurrentScenario?.id}");
            Assert.IsFalse(LocationUnlocks.IsUnlocked(WorldLocationCatalog.SushiRestaurantScene), "The opening alone does not open Sushi Hibari: study comes first.");

            // Sushi Hibari: the opening is done, now knowledge decides.
            SetKnowledge(99);
            Assert.IsFalse(LocationUnlocks.IsUnlocked(WorldLocationCatalog.SushiRestaurantScene), "99 knowledge is not enough for Sushi Hibari.");
            Assert.AreEqual(WorldLocationCatalog.SushiRestaurantScene, LocationUnlocks.Next().sceneName);
            PlayerStatus.Instance.AddKnowledge(1);
            Assert.IsTrue(LocationUnlocks.IsUnlocked(WorldLocationCatalog.SushiRestaurantScene), "100 opens Sushi Hibari.");
            // The card waits for an open conversation to end (by design); finish any that is still on screen.
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) DialogueManager.Instance.CancelDialogue();
            for (float t = 0f; t < 5f && MapUnlockCardUI.Open == null; t += Time.unscaledDeltaTime) yield return null;
            if (MapUnlockCardUI.Open == null)
                Note($"card blocked: modal {UiModalStack.TopName}, dialogue {NihongoLife.Dialogue.DialogueManager.Instance?.IsOpen}, result {Object.FindFirstObjectByType<ResultUI>()?.isActiveAndEnabled}, freeplay {FreePlayCardUI.Open != null}, queued {MapUnlockCardUI.QueueCount}, scene {SceneManager.GetActiveScene().name}");
            Assert.NotNull(MapUnlockCardUI.Open, "The unlock card shows.");
            Assert.AreEqual(WorldLocationCatalog.SushiRestaurantScene, MapUnlockCardUI.Open.Stage.sceneName);
            StringAssert.DoesNotContain("Đã khóa", sushiDoor.GetpromptEn(), "The door prompt drops the lock.");
            Assert.IsFalse(Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsSortMode.None).Any(t => t.name == "Sign" && t.text.Contains("cần 100")), "The door sign drops the lock line.");
            yield return new WaitForSecondsRealtime(0.3f);
            Capture("01_unlock_card_sushi");
            MapUnlockCardUI.Close();

            // The station: knowledge alone is not enough, the sushi quest comes first.
            PlayerStatus.Instance.AddKnowledge(80); // 180
            Assert.IsFalse(LocationUnlocks.IsUnlocked(WorldLocationCatalog.StationScene), "Station waits for the sushi quest.");
            string stationStatus = LocationUnlocks.ShortStatus(WorldLocationCatalog.StationScene);
            Note("station: " + stationStatus);
            StringAssert.Contains("xong", stationStatus);
            TaskJournalUI.Open();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.NotNull(GameObject.Find("Stage_" + WorldLocationCatalog.StationScene), "The roadmap lists the station stage.");
            Capture("02_journal_roadmap");
            TaskJournalUI.Close();
            yield return EnsureRunning("scenario.restaurant.sushi_dining");
            Finish();
            yield return new WaitForSecondsRealtime(0.4f);
            Object.FindFirstObjectByType<ResultUI>()?.BackToTown();
            Assert.IsTrue(LocationUnlocks.IsUnlocked(WorldLocationCatalog.StationScene), "Sushi quest + 160 open the station.");
            MapUnlockCardUI.Close();

            // Game Center: 240 + station quest, opened by Kim's quest, played through its dialogue.
            yield return EnsureRunning("scenario.station.buy_ticket");
            Finish();
            yield return new WaitForSecondsRealtime(0.4f);
            Object.FindFirstObjectByType<ResultUI>()?.BackToTown();
            MapUnlockCardUI.Close();
            SetKnowledge(240);
            LocationUnlocks.Evaluate();
            Assert.IsTrue(LocationUnlocks.IsUnlocked(WorldLocationCatalog.GameCenterScene), "240 + station quest open the Game Center.");
            yield return PlayThrough("scenario.arcade.kim_invite");
            Assert.IsTrue(StoryDirector.IsDone("scenario.arcade.kim_invite"), "Kim's quest can be played to the end.");
            Assert.IsTrue(StoryFlags.Has("kim.friend"), "Kind words to Kim are remembered.");
            Object.FindFirstObjectByType<ResultUI>()?.BackToTown();
            MapUnlockCardUI.Close();

            // Midori Island: 320 + Kim's quest; Tanaka remembers the Game Center (flag branch).
            SetKnowledge(320);
            LocationUnlocks.Evaluate();
            Assert.IsTrue(LocationUnlocks.IsUnlocked(WorldLocationCatalog.MidoriIslandScene), "320 + Kim's quest open the island.");
            for (float t = 0f; t < 5f && MapUnlockCardUI.Open == null; t += Time.unscaledDeltaTime) yield return null;
            if (MapUnlockCardUI.Open != null) { Capture("03_unlock_card_island"); MapUnlockCardUI.Close(); }
            var visited = new List<string>();
            yield return PlayThrough("scenario.island.midori_trip", visited);
            Assert.Contains("n_hello_kim", visited, "Tanaka mentions Kim (story memory).");
            Assert.IsTrue(StoryDirector.IsDone("scenario.island.midori_trip"));
            Object.FindFirstObjectByType<ResultUI>()?.BackToTown();
            MapUnlockCardUI.Close();

            // Opened places stay open; the map shows them.
            SetKnowledge(0);
            foreach (var scene in new[] { WorldLocationCatalog.SushiRestaurantScene, WorldLocationCatalog.StationScene, WorldLocationCatalog.GameCenterScene, WorldLocationCatalog.MidoriIslandScene })
                Assert.IsTrue(LocationUnlocks.IsUnlocked(scene), scene + " stays open.");
            var map = Object.FindFirstObjectByType<WorldMapUI>();
            map.SetVisible(true);
            yield return new WaitForSecondsRealtime(0.4f);
            Capture("04_map_all_open");
            map.SetVisible(false);

            // Exams pay knowledge: first completion by score, then a little for retries.
            var exam = new ExamAttemptResult { record = new ExamAttemptRecord { totalScore = 150, totalMaxScore = 180 } };
            Assert.AreEqual(33, ExamManager.KnowledgeFor(exam, true));
            Assert.AreEqual(3, ExamManager.KnowledgeFor(exam, false));
            WriteLog();
        }

        // ─────────── helpers ───────────

        private readonly List<string> _log = new();
        private void Note(string s) { _log.Add(s); Debug.Log("[Roadmap] " + s); }

        /// <summary>Knowledge in the player and the save (quest requirements read the save).</summary>
        private void SetKnowledge(int value)
        {
            PlayerStatus.Instance.DebugSetKnowledge(value);
            var p = _repo.GetProgress(); p.knowledge = value; _repo.SaveProgress(p);
            LocationUnlocks.Evaluate();
        }

        private IEnumerator EnsureRunning(string id)
        {
            if (ScenarioManager.Instance.CurrentScenario?.id != id || StoryDirector.IsDone(id))
                ScenarioManager.Instance.StartScenario(GameServices.Get<IScenarioRepository>().GetScenarioById(id));
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(id, ScenarioManager.Instance.CurrentScenario?.id, id + " starts.");
        }

        private static void Finish()
        {
            var s = ScenarioManager.Instance.CurrentScenario;
            ScenarioManager.Instance.TransitionToNode(s.nodes.First(n => n.nodeType == ScenarioNodeType.Complete).id);
        }

        /// <summary>Plays a quest through its real dialogue, always taking the first (best) answer.</summary>
        private IEnumerator PlayThrough(string id, List<string> visited = null)
        {
            yield return EnsureRunning(id);
            var dm = DialogueManager.Instance;
            for (int step = 0; step < 120 && !StoryDirector.IsDone(id); step++)
            {
                var node = ScenarioManager.Instance.CurrentNode;
                if (node == null) break;
                visited?.Add(node.id);
                if (node.nodeType == ScenarioNodeType.TalkToNPC) ScenarioManager.Instance.TransitionToNode(node.nextNodeId);
                else if (dm != null && dm.IsOpen)
                {
                    if (node.choices != null && node.choices.Count > 0) dm.SelectChoice(0);
                    else dm.ContinueDialogue();
                }
                else if (!string.IsNullOrEmpty(node.nextNodeId)) ScenarioManager.Instance.TransitionToNode(node.nextNodeId);
                yield return null;
            }
            Note($"{id}: played {visited?.Count ?? 0} steps, done={StoryDirector.IsDone(id)}");
        }

        private void WriteLog()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/" + Folder));
            Directory.CreateDirectory(folder);
            File.WriteAllLines(Path.Combine(folder, "roadmap.txt"), _log);
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
            camera.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/" + Folder));
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Object.Destroy(tex); rt.Release();
        }
    }
}
