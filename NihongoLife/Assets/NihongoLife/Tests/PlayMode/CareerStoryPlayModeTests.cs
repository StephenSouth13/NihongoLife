using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Player;
using NihongoLife.Progression;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// A milestone story plays before the promotion interview, once: the best answer gives trí tuệ and goes on to the
    /// interview; a poor answer costs performance and the interview waits; leaving with Esc keeps the story for later.
    /// Captures: Bao_Cao/career-story/.
    /// </summary>
    public class CareerStoryPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/career-story"));

        [UnityTest]
        public IEnumerator Story_BeforeTheInterview_BestGoesOn_PoorWaits()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var dm = DialogueManager.Instance;
            while (dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
            var status = PlayerStatus.Instance;
            float eventChance = CareerService.EventChance;
            DialogueDisplayData shown = default;
            void OnShown(DialogueDisplayData d) => shown = d;
            dm.OnDialogueUpdated += OnShown;
            try
            {
                CareerService.EventChance = 0f;
                CareerStories.Enabled = true;
                status.AddExp(20000);
                if (CareerService.State.HasCareer) CareerService.Quit();
                CareerService.State.majorIndustry = string.Empty;
                Assert.IsNull(CareerService.Choose("retail"));
                CareerService.State.stories.Clear();
                var story = CareerStories.Find("retail", 2, null);

                // A full bar: the story first, not the interview.
                CareerService.State.performance = 100f;
                int knowledge = status.Knowledge;
                Assert.IsTrue(CareerService.TryOfferPromotion());
                Assert.IsTrue(dm.IsOpen, "the milestone story plays");
                Assert.IsFalse(JobQuizCard.IsOpen, "no interview yet");
                Assert.AreEqual(story.lines[0].ja, shown.textJa, "it starts with the story's first line");
                Assert.AreEqual(CareerService.Current.BossName, shown.speakerName, "said by the boss");
                yield return Capture("01_story_line");

                // Esc: the story is kept for later, nothing changes.
                dm.CancelDialogue();
                yield return null;
                Assert.IsFalse(story.id.Length == 0 || CareerService.State.stories.Contains(story.id), "a story left with Esc plays again");
                Assert.AreEqual(100f, CareerService.State.performance, 0.01f);

                // Again: through the lines to the question, the best answer.
                Assert.IsTrue(CareerService.TryOfferPromotion());
                for (int guard = 0; guard < 10 && (shown.choices == null || shown.choices.Count == 0); guard++) { dm.ContinueDialogue(); yield return null; }
                Assert.AreEqual(3, shown.choices.Count, "three answers in Japanese");
                Assert.AreEqual(story.ask.ja, shown.textJa);
                yield return Capture("02_story_question");
                dm.SelectChoice(System.Array.FindIndex(story.choices, c => c.kind == "best"));
                yield return null;
                Assert.AreEqual(story.choices.First(c => c.kind == "best").reply.ja, shown.textJa, "the reply to the answer");
                yield return Capture("03_story_reply");
                dm.ContinueDialogue();
                yield return null;
                Assert.IsTrue(CareerService.State.stories.Contains(story.id), "the story is on the record");
                Assert.AreEqual(knowledge + 5, status.Knowledge, "the best answer gives trí tuệ");
                Assert.IsTrue(JobQuizCard.IsOpen, "then the promotion interview");
                yield return Capture("04_interview_after_story");
                JobQuizCard.Answer(JobQuizCard.CurrentChoices.ToList().FindIndex(c => c.Correct));
                yield return null;
                Assert.AreEqual(2, CareerService.State.level, "promoted");

                // Level 3: a poor answer costs performance, the interview waits; the next full bar goes straight to it.
                status.DebugSetKnowledge(2000);
                var next = CareerStories.Find("retail", 3, null);
                CareerService.State.performance = 100f;
                Assert.IsTrue(CareerService.TryOfferPromotion());
                for (int guard = 0; guard < 10 && (shown.choices == null || shown.choices.Count == 0 || shown.textJa != next.ask.ja); guard++) { dm.ContinueDialogue(); yield return null; }
                var poor = next.choices.First(c => c.kind == "poor");
                dm.SelectChoice(System.Array.IndexOf(next.choices, poor));
                yield return null;
                dm.ContinueDialogue();
                yield return null;
                Assert.AreEqual(100f + poor.performance, CareerService.State.performance, 0.01f, "a poor answer costs performance");
                Assert.IsFalse(JobQuizCard.IsOpen, "the interview waits for a full bar");
                Assert.IsTrue(CareerService.State.stories.Contains(next.id));
                CareerService.State.performance = 100f;
                Assert.IsTrue(CareerService.TryOfferPromotion());
                Assert.IsTrue(JobQuizCard.IsOpen, "a story plays once: the interview comes straight away");
                Assert.IsFalse(dm.IsOpen);
                JobQuizCard.Close();
            }
            finally
            {
                dm.OnDialogueUpdated -= OnShown;
                if (dm.IsOpen) dm.CancelDialogue();
                if (JobQuizCard.IsOpen) JobQuizCard.Close();
                CareerService.EventChance = eventChance;
                CareerStories.Enabled = false;
                if (CareerService.State.HasCareer) CareerService.Quit();
            }
        }

        /// <summary>The main camera with the overlay canvases drawn into it (as CareerPlayModeTests does: batch runs have no screen).</summary>
        private static IEnumerator Capture(string name)
        {
            yield return null;
            var camera = Camera.main;
            if (camera == null) yield break;
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
