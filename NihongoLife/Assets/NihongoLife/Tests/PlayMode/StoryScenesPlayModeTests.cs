using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Player;
using NihongoLife.Progression;
using NihongoLife.Scenario;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// StoryScenes in the real town: completing a story quest opens its cutscene in the dialogue window
    /// (lines → question with 3 answers → reply → ending), the chosen answer sets its story flag and affinity once,
    /// the scene plays only once, and nothing plays while StoryDirector.Bypass is on.
    /// </summary>
    public class StoryScenesPlayModeTests
    {
        [TearDown]
        public void TearDown() => StoryDirector.Bypass = true;

        [UnityTest]
        public IEnumerator Cutscene_PlaysOnQuestCompletion_ChoiceCountsOnce()
        {
            LogAssert.ignoreFailingMessages = true;
            StoryDirector.Bypass = true;
            SceneManager.LoadScene("00_Bootstrap");
            yield return new WaitForSecondsRealtime(0.5f);
            SceneManager.LoadScene(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 20f && (SceneManager.GetActiveScene().name != WorldLocationCatalog.CityScene || DialogueManager.Instance == null); t += Time.unscaledDeltaTime) yield return null;
            yield return new WaitForSecondsRealtime(2f); // the town finishes loading (its own DialogueManager, NPCs, zones)
            var dm = DialogueManager.Instance;
            Assert.NotNull(dm, "The town has a dialogue window.");
            if (dm.IsOpen) dm.CancelDialogue();

            var story = Storyline.Load();
            var quest = QuestService.Catalog.quests.First(q => story.CutsceneFor(q.id) != null && story.CutsceneFor(q.id).choices.Length >= 3);
            var scene = story.CutsceneFor(quest.id);
            StoryFlags.Remove(StoryScenes.PlayedPrefix + quest.id);
            foreach (var c in scene.choices) { StoryFlags.Remove(c.flag); StoryFlags.Remove(c.flag + "_counted"); }

            var complete = typeof(StoryScenes).GetMethod("OnQuestCompleted", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(complete);

            // Bypass on (tests, prologue tooling): nothing plays.
            complete.Invoke(null, new object[] { quest });
            yield return null;
            Assert.IsFalse(dm.IsOpen, "No cutscene while bypassed.");

            var shown = new List<DialogueDisplayData>();
            System.Action<DialogueDisplayData> rec = d => shown.Add(d);
            dm.OnDialogueUpdated += rec;
            try
            {
                StoryDirector.Bypass = false;
                StoryScenes.Enabled = true;
                complete.Invoke(null, new object[] { quest });
                yield return null;
                string St(string f) => typeof(StoryScenes).GetField(f, BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)?.ToString();
                Assert.AreSame(dm, DialogueManager.Instance);
                Assert.IsTrue(dm.IsOpen, $"The cutscene opens in the dialogue window. quest={quest.id} played={StoryFlags.Has(StoryScenes.PlayedPrefix + quest.id)} playing={St("_playing")} waiting={St("_waiting")} shown={shown.Count} enabled={StoryScenes.Enabled} isPlaying={Application.isPlaying}");
                Assert.IsTrue(StoryFlags.Has(StoryScenes.PlayedPrefix + quest.id));

                int pick = 1, affinityBefore = SocialStanding.AffinityOf(scene.prompt.speaker);
                for (int guard = 0; guard < 60 && dm.IsOpen; guard++)
                {
                    var last = shown.LastOrDefault();
                    if (shown.Count > 0 && last.choices != null && last.choices.Count > 0)
                    {
                        Assert.AreEqual(scene.choices.Length, last.choices.Count, "The question offers every answer.");
                        Assert.IsTrue(last.choices.All(c => !string.IsNullOrWhiteSpace(c.textJa)), "Answers have text.");
                        dm.SelectChoice(pick);
                    }
                    else dm.ContinueDialogue();
                    yield return new WaitForSecondsRealtime(0.05f);
                }
                Assert.IsFalse(dm.IsOpen, "The cutscene ends.");
                Assert.GreaterOrEqual(shown.Count, 3, "Lines, question and reply are shown.");
                Assert.IsTrue(shown.All(d => !string.IsNullOrWhiteSpace(d.textJa)), "Every line has Japanese text.");
                Assert.IsTrue(shown.Any(d => d.textJa == scene.prompt.ja), "The question is asked.");
                Assert.IsTrue(shown.Any(d => d.textJa == scene.choices[pick].reply.ja), "The reply to the chosen answer follows.");
                Assert.IsTrue(StoryFlags.Has(scene.choices[pick].flag), "The answer's story flag is set.");
                Assert.IsFalse(scene.choices.Where((c, i) => i != pick).Any(c => StoryFlags.Has(c.flag)), "Only that answer.");
                int gained = SocialStanding.AffinityOf(scene.prompt.speaker) - affinityBefore;
                Assert.AreEqual(scene.choices[pick].affinity, gained, "Affinity with the speaker changes by the answer's value.");

                // Once per quest; the answer counts once.
                complete.Invoke(null, new object[] { quest });
                yield return null;
                Assert.IsFalse(dm.IsOpen, "A finished cutscene does not replay.");
                StoryScenes.ApplyChoice(quest.id, StoryFlags.Has);
                Assert.AreEqual(gained, SocialStanding.AffinityOf(scene.prompt.speaker) - affinityBefore, "Affinity counts once.");
            }
            finally
            {
                dm.OnDialogueUpdated -= rec;
                StoryDirector.Bypass = true;
            }
        }
    }
}
