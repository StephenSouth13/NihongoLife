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
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>
    /// Careers in the real town: locked before level 10; at level 10 the career card opens by itself; choosing a
    /// career; an on-time shift pays the salary and fills performance, a shift outside hours counts half; a full bar
    /// opens a Japanese promotion interview (a wrong answer is explained, the right one promotes); a missed workday
    /// costs performance; the branch interview picks the path; a work event's answer changes performance; quitting.
    /// Captures: Bao_Cao/career.
    /// </summary>
    public class CareerPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/career"));

        [UnityTest]
        public IEnumerator Career_ChooseWorkPromoteBranchAndQuit()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var dm = DialogueManager.Instance;
            while (dm != null && dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
            var status = PlayerStatus.Instance;
            var inventory = PlayerInventory.Instance;
            float eventChance = CareerService.EventChance;
            try
            {
                CareerService.EventChance = 0f;
                // Locked before level 10 (earlier tests may have levelled the shared guest up: start from level 1).
                status.DebugSetExp(0);
                Assert.Less(status.Level, 10);
                Assert.IsNotNull(CareerService.Choose("food"), "No career before level 10");
                CareerUI.Open();
                yield return null;
                Assert.IsFalse(Button("Choose_food").interactable, "Locked card");
                Capture("01_locked");
                CareerUI.Close();

                // Level 10: the career card opens by itself, once.
                status.AddExp(20000);
                Assert.GreaterOrEqual(status.Level, 10);
                CareerClock.PromptEnabled = true;
                for (float t = 0f; t < 4f && !CareerUI.IsOpen; t += Time.unscaledDeltaTime) yield return null;
                Assert.IsTrue(CareerUI.IsOpen, "At level 10 the career card opens");
                Assert.IsTrue(Button("Choose_food").interactable);
                Capture("02_choose");
                Button("Choose_food").onClick.Invoke();
                yield return null;
                Assert.AreEqual("food", CareerService.State.careerId);
                Assert.AreEqual(1, CareerService.State.level);
                Assert.AreEqual("皿洗い", CareerService.CurrentLevel.titleJa);
                Capture("03_career");
                CareerUI.Close();

                // On-time shift: salary and performance. Outside hours: half, no salary.
                var food = CareerService.Current;
                int workday = Enumerable.Range(DayNightCycle.Active.Day + 1, 7).First(d => CareerService.IsWorkDay(food, d));
                int yen = inventory.Yen;
                float mood = CareerService.MoodFactor;
                int firstShiftReward = CareerService.Catalog.milestones.First(m => m.id == "first_shift").rewardYen;
                CareerService.ShiftDone(workday, 12f);
                Assert.AreEqual(yen + 220 + firstShiftReward, inventory.Yen, "Salary for the on-time shift + the first-shift milestone");
                Assert.IsTrue(CareerService.State.milestones.Contains("first_shift"), "Milestone: first shift");
                Assert.Contains(workday, CareerService.State.workedDays, "The worked day is on the record");
                Assert.AreEqual(50f * mood, CareerService.State.performance, 0.5f, "Half the bar per shift at level 1 (two shifts), times the mood");
                CareerService.State.performance = 0f;
                yen = inventory.Yen;
                mood = CareerService.MoodFactor;
                CareerService.ShiftDone(workday + 1, 20f);
                Assert.AreEqual(25f * mood, CareerService.State.performance, 0.5f, "Outside working hours counts half");
                Assert.AreEqual(yen, inventory.Yen, "No salary outside working hours");

                // A full bar: the promotion interview in Japanese — wrong answers are explained, the right one promotes.
                CareerService.State.performance = 100f;
                Assert.IsTrue(CareerService.TryOfferPromotion());
                Assert.IsTrue(JobQuizCard.IsOpen, "Promotion interview");
                Capture("04_interview");
                int wrong = JobQuizCard.CurrentChoices.ToList().FindIndex(c => !c.Correct);
                JobQuizCard.Answer(wrong);
                Assert.IsTrue(JobQuizCard.IsOpen, "A wrong answer keeps the interview open");
                JobQuizCard.Answer(JobQuizCard.CurrentChoices.ToList().FindIndex(c => c.Correct));
                yield return null;
                Assert.AreEqual(2, CareerService.State.level, "Promoted");
                Assert.IsTrue(CareerService.State.milestones.Contains("first_promotion"), "Milestone: first promotion");
                Assert.AreEqual(1, CareerService.State.promotions.Count, "The promotion is on the record");
                Assert.AreEqual(0f, CareerService.State.performance, 0.01f);

                // A workday spent in the game without a shift costs performance the next day.
                CareerService.State.performance = 50f;
                CareerService.State.lastCheckedDay = workday;
                CareerService.State.presentAtWork = false;
                CareerService.State.lastShiftDay = -1;
                CareerService.Tick(workday, food.workStart + 1f, true);
                CareerService.Tick(workday + 1, 7f, true);
                Assert.AreEqual(50f - CareerService.Catalog.missedShiftPenalty, CareerService.State.performance, 0.01f, "Missed shift");

                // The branch interview chooses the path.
                CareerService.State.level = 3;
                CareerService.State.performance = 100f;
                status.DebugSetKnowledge(2000);
                Assert.IsTrue(CareerService.TryOfferPromotion());
                var choices = JobQuizCard.CurrentChoices.ToList();
                Assert.AreEqual(2, choices.Count, "Two paths");
                Capture("05_branch");
                JobQuizCard.Answer(choices.FindIndex(c => c.Id == "owner"));
                yield return null;
                Assert.AreEqual(4, CareerService.State.level);
                Assert.AreEqual("owner", CareerService.State.branchId);
                Assert.AreEqual("副店長", CareerService.CurrentLevel.titleJa);

                // A work event: the answer changes performance.
                CareerService.EventChance = 1f;
                CareerService.State.performance = 10f;
                CareerService.ShiftDone(workday, 12f);
                Assert.IsTrue(JobQuizCard.IsOpen, "An event at work");
                Capture("06_event");
                float beforeEvent = CareerService.State.performance;
                JobQuizCard.Answer(0);
                yield return null;
                Assert.Greater(CareerService.State.performance, beforeEvent, "A good answer raises performance");
                CareerService.EventChance = 0f;

                // The career window's tabs: overview, path tree, week schedule, profile, milestones, careers.
                CareerUI.Open();
                yield return null;
                Assert.AreEqual(CareerUI.Tab.Overview, CareerUI.CurrentTab);
                Capture("07_overview");
                Button("Tab_Path").onClick.Invoke();
                yield return null;
                Assert.IsNotNull(Named("Rung_6"), "The path shows the top rung");
                Assert.IsNotNull(Named("Branch_owner"), "The path shows the chosen branch");
                Capture("08_path");
                Button("Tab_Schedule").onClick.Invoke();
                yield return null;
                Assert.IsNotNull(Named("Day_6"), "Seven days in the week");
                Capture("09_schedule");
                Button("Tab_Profile").onClick.Invoke();
                yield return null;
                Assert.IsNotNull(Named("Promotion_0"), "The profile lists the promotions");
                Capture("10_profile");
                Button("Tab_Milestones").onClick.Invoke();
                yield return null;
                Assert.IsNotNull(Named("Milestone_first_shift"));
                Capture("11_milestones");
                Button("Tab_Careers").onClick.Invoke();
                yield return null;
                Capture("12_careers");

                // Switching careers asks first, then starts the new career at level 1.
                Button("Choose_retail").onClick.Invoke();
                yield return null;
                Assert.AreEqual("food", CareerService.State.careerId, "The first click only asks");
                Button("Choose_retail").onClick.Invoke();
                yield return null;
                Assert.AreEqual("retail", CareerService.State.careerId, "Switched");
                Assert.AreEqual(1, CareerService.State.level);
                Assert.IsTrue(CareerService.State.pastCareers.Contains("food"));
                Assert.IsTrue(CareerService.State.milestones.Contains("careers_2"), "Milestone: a second career");
                Assert.AreEqual(CareerUI.Tab.Overview, CareerUI.CurrentTab, "Back to the overview after switching");

                Button("Quit").onClick.Invoke();
                yield return null;
                Button("Quit").onClick.Invoke();
                yield return null;
                Assert.IsNull(CareerService.Current, "Quit");
                Assert.IsTrue(CareerService.State.pastCareers.Contains("food"));
                CareerUI.Close();
            }
            finally
            {
                CareerService.EventChance = eventChance;
                CareerClock.PromptEnabled = false;
                JobQuizCard.Close();
                CareerUI.Close();
            }
        }

        private static GameObject Named(string name) =>
            Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).Select(r => r.gameObject).FirstOrDefault(g => g.name == name);

        private static Button Button(string name) =>
            Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == name);

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
            Canvas.ForceUpdateCanvases(); // back to screen size now, or HudFitRect fits the next frame to the camera-mode size
            Object.Destroy(tex);
            rt.Release();
        }
    }
}
