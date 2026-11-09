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
    /// A shift in the real town: its work counts as tasks, then task cards in Japanese come up; a card right the first time
    /// is one more task, a card answered after a wrong try adds none (the hint explains); the bar shows tasks at this level.
    /// Captures: Bao_Cao/career-tasks/.
    /// </summary>
    public class CareerTasksPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/career-tasks"));

        [UnityTest]
        public IEnumerator Shift_HandsOutTaskCards_RightFirstTimeCounts()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var dm = DialogueManager.Instance;
            while (dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
            var status = PlayerStatus.Instance;
            float eventChance = CareerService.EventChance;
            try
            {
                CareerService.EventChance = 0f;
                CareerTasks.Enabled = true;
                status.AddExp(20000);
                if (CareerService.State.HasCareer) CareerService.Quit();
                CareerService.State.majorIndustry = string.Empty;
                Assert.IsNull(CareerService.Choose("retail"));
                var career = CareerService.Current;
                int workday = Enumerable.Range(DayNightCycle.Active.Day + 1, 7).First(d => CareerService.IsWorkDay(career, d));
                float perTask = CareerService.TaskGain(CareerService.CurrentLevel);

                CareerService.ShiftDone(workday, career.workStart + 1f);
                float afterShift = CareerService.State.performance;
                Assert.AreEqual(perTask * CareerTasks.Ladder.shiftTasks, afterShift, 0.01f, "the shift's own work");
                Assert.IsTrue(JobQuizCard.IsOpen, "then a task card");
                yield return null;
                yield return Capture("01_task_card");

                // Card 1: right the first time → one more task.
                JobQuizCard.Answer(JobQuizCard.CurrentChoices.ToList().FindIndex(c => c.Correct));
                yield return null;
                Assert.IsTrue(JobQuizCard.IsOpen, "the next card of the shift");
                yield return Capture("02_second_card");

                // Card 2: a wrong try first (the hint shows), then right → no extra task.
                JobQuizCard.Answer(JobQuizCard.CurrentChoices.ToList().FindIndex(c => !c.Correct));
                yield return null;
                Assert.IsTrue(JobQuizCard.IsOpen, "a wrong answer keeps the card");
                yield return Capture("03_wrong_hint");
                JobQuizCard.Answer(JobQuizCard.CurrentChoices.ToList().FindIndex(c => c.Correct));
                yield return null;
                Assert.IsFalse(JobQuizCard.IsOpen, "two cards a shift");
                Assert.AreEqual(afterShift + perTask, CareerService.State.performance, 0.01f, "only the card right the first time counts");
                Assert.AreEqual(CareerTasks.Ladder.shiftTasks + 1, CareerService.State.tasksDone);
                Assert.AreEqual(CareerTasks.Ladder.shiftTasks + 1, CareerTasks.DoneAtLevel(CareerService.State), "the bar in tasks");

                // The career window shows tasks at this level and to the top.
                CareerUI.Open();
                yield return null;
                var labels = Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None);
                Assert.IsTrue(labels.Any(t => t.name == "PerformanceLabel" && t.text.Contains($"{CareerTasks.Ladder.shiftTasks + 1}/{CareerTasks.TasksToPromote(1)}")), "tasks at this level");
                Assert.IsTrue(labels.Any(t => t.name == "TasksLabel" && t.text.Contains(CareerTasks.TasksToTop(career).ToString())), "tasks to the top");
                yield return Capture("04_career_tasks");
                CareerUI.Close();

                // Esc on a card ends the round; the shift still counts.
                CareerService.ShiftDone(workday + 7, career.workStart + 1f);
                Assert.IsTrue(JobQuizCard.IsOpen);
                JobQuizCard.Close();
                yield return null;
                Assert.IsFalse(JobQuizCard.IsOpen);
            }
            finally
            {
                if (JobQuizCard.IsOpen) JobQuizCard.Close();
                CareerUI.Close();
                CareerService.EventChance = eventChance;
                CareerTasks.Enabled = false;
                if (CareerService.State.HasCareer) CareerService.Quit();
            }
        }

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
