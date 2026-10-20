using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.Progression;
using NihongoLife.Save;
using NihongoLife.Scenario;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Story first, then free play (StoryDirector), on a fresh save in the real town: the arrival starts by itself, jobs
    /// wait, each opening quest's result button goes straight to the next one (5 quests, finished through their Complete
    /// node) → after the konbini the result says "keep exploring" and the free-play card appears once → jobs open, the
    /// HUD points at chapter 3 with the knowledge it needs, the journal lists those steps → entering the town again
    /// starts nothing. Captures: Bao_Cao/story-flow.
    /// </summary>
    public class StoryFlowPlayModeTests
    {
        private const string Folder = "story-flow";
        private readonly List<string> _log = new();

        [TearDown]
        public void TearDown()
        {
            StoryDirector.Bypass = true;
            FreePlayCardUI.Close();
            TaskJournalUI.Close();
        }

        [UnityTest]
        public IEnumerator Story_OpeningThenFreePlay()
        {
            LogAssert.ignoreFailingMessages = true;
            StoryDirector.Bypass = false;
            SceneManager.LoadScene("00_Bootstrap");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsTrue(GameServices.TryGet(out IProgressRepository repository));
            var progress = repository.GetProgress();
            progress.completedScenarios.Clear();
            progress.activeScenarioId = string.Empty;
            progress.activeScenarioNodeId = string.Empty;
            progress.knowledge = 0;
            progress.storyFlags?.Remove(StoryDirector.FreePlayFlag);
            repository.SaveProgress(progress);

            SceneManager.LoadScene(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 20f && (ScenarioManager.Instance == null || ScenarioManager.Instance.CurrentScenario == null); t += Time.unscaledDeltaTime) yield return null;
            Assert.NotNull(ScenarioManager.Instance?.CurrentScenario, "A fresh game starts the story by itself.");
            PlayerStatus.Instance?.ReloadProgress(); // the status may have been loaded from the real save before the reset
            int knowledgeAtStart = LocationUnlocks.Knowledge; // 0 offline; a signed-in account may merge its cloud value
            Assert.AreEqual("scenario.intro.arrival", ScenarioManager.Instance.CurrentScenario.id);
            Assert.IsTrue(StoryDirector.InPrologue);
            var job = QuestService.Catalog.quests.First(q => q.IsJob);
            StringAssert.Contains("mở đầu", QuestService.Requirement(job), "Jobs wait for the end of the opening.");
            yield return new WaitForSecondsRealtime(2f);
            Capture("01_new_game_story_starts");

            var opening = StoryDirector.PrologueIds.ToList();
            Note("opening: " + string.Join(" → ", opening));
            Assert.AreEqual(5, opening.Count);
            for (int i = 0; i < opening.Count; i++)
            {
                Assert.AreEqual(opening[i], ScenarioManager.Instance.CurrentScenario?.id, $"Step {i + 1} runs.");
                var hud = StoryDirector.Hud();
                Finish(ScenarioManager.Instance.CurrentScenario);
                yield return new WaitForSecondsRealtime(0.6f);
                Assert.IsTrue(StoryDirector.IsDone(opening[i]));
                var result = Object.FindFirstObjectByType<ResultUI>();
                Assert.NotNull(result, "The result screen shows.");
                bool last = i == opening.Count - 1;
                Note($"{opening[i]} done → result button: {result.ContinueAction}");
                Assert.AreEqual(last ? "explore" : "next", result.ContinueAction);
                if (i == 0 || last) Capture(last ? "03_result_keep_exploring" : "02_result_next_mission");
                ContinueButton(result).onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.8f);
            }

            // ── Free play ──
            Assert.IsFalse(StoryDirector.InPrologue);
            Assert.NotNull(FreePlayCardUI.Open, "The free-play card appears once.");
            Capture("04_free_play_card");
            FreePlayCardUI.Close();
            Assert.IsTrue(StoryDirector.FreePlayAnnounced);
            Assert.IsFalse(FreePlayCardUI.ShowOnce(), "Only once.");
            string jobReq = QuestService.Requirement(job);
            Assert.IsTrue(jobReq == null || !jobReq.Contains("mở đầu"), "Jobs are open: " + jobReq);

            var next = StoryDirector.Next();
            Assert.NotNull(next);
            Assert.AreEqual(3, next.chapterIndex, "Chapter 3 is next.");
            int gained = LocationUnlocks.Knowledge - knowledgeAtStart;
            Note($"knowledge from the opening: +{gained}; next {next.id} blocker: {StoryDirector.Blocker(next) ?? "none"}");
            Assert.That(gained, Is.InRange(40, 120), "The opening is worth about one or two places, not the whole map.");
            // A later step that waits for knowledge (station 80, festival 150) is listed with what it needs.
            var later = StoryDirector.Remaining().FirstOrDefault(s => StoryDirector.Blocker(s) != null && s.requiredKnowledge > repository.GetProgress().knowledge);
            Assert.NotNull(later, "Some story steps wait for knowledge earned in free play.");
            var line = StoryDirector.Hud();
            Assert.NotNull(line);
            Note($"HUD: {line.Value.kicker} | {line.Value.text} | {line.Value.progress}");
            yield return new WaitForSecondsRealtime(1f);
            Capture("05_hud_next_story_step");

            TaskJournalUI.Open();
            yield return null;
            TaskJournalUI.SelectStory(next.id);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.NotNull(GameObject.Find("StartStory"), "The next chapter-3 step can start from the journal.");
            Capture("06a_journal_story_ready");
            TaskJournalUI.SelectStory(later.id);
            yield return new WaitForSecondsRealtime(0.4f);
            Note($"journal shows {later.id}: {StoryDirector.Blocker(later)}");
            if (StoryDirector.Blocker(later).Contains("kiến thức")) Assert.NotNull(GameObject.Find("StoryBlocker"), "The journal says what the story step needs.");
            Capture("06b_journal_story_needs_knowledge");
            TaskJournalUI.Close();

            // ── Back into town: free play starts nothing by itself ──
            SceneManager.LoadScene(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(4f);
            Assert.IsNull(ScenarioManager.Instance?.CurrentScenario, "In free play nothing starts by itself.");
            WriteLog();
        }

        private static void Finish(ScenarioDefinition scenario)
        {
            var complete = scenario.nodes.First(n => n.nodeType == ScenarioNodeType.Complete);
            ScenarioManager.Instance.TransitionToNode(complete.id);
        }

        private static Button ContinueButton(ResultUI result)
        {
            var field = typeof(ResultUI).GetField("returnToMenuButton", BindingFlags.NonPublic | BindingFlags.Instance);
            var button = field?.GetValue(result) as Button;
            Assert.NotNull(button, "The result screen has its continue button.");
            return button;
        }

        private void Note(string line) { _log.Add(line); Debug.Log("[StoryFlow] " + line); }
        private void WriteLog() => File.WriteAllLines(Path.Combine(FolderPath(), "story-flow.txt"), _log);

        private static string FolderPath()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/" + Folder));
            Directory.CreateDirectory(folder);
            return folder;
        }

        private static void Capture(string name, int width = 1600, int height = 900)
        {
            var camera = Camera.main;
            if (camera == null) return;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camera;
                canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i);
            }
            var target = new RenderTexture(width, height, 24);
            var old = RenderTexture.active;
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render(); RenderTexture.active = target;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(FolderPath(), name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = old;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Object.Destroy(tex); target.Release(); Object.Destroy(target);
        }
    }
}
