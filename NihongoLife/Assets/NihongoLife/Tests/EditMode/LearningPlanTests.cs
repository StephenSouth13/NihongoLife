using System;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.StudyModes.Plan;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.Tests
{
    /// <summary>Personal learning plan: estimate math (published hour figures), plan generation, ETA from real pace, the popup.</summary>
    public class LearningPlanTests
    {
        [Test]
        public void CumulativeHours_FollowPublishedFigures()
        {
            Assert.AreEqual(95, LearningPlanner.CumulativeHours(PlanTrack.English, "A1"));
            Assert.AreEqual(375, LearningPlanner.CumulativeHours(PlanTrack.English, "B1"));
            Assert.AreEqual(1100, LearningPlanner.CumulativeHours(PlanTrack.English, "C2"));
            Assert.AreEqual(375 * 1.3, LearningPlanner.CumulativeHours(PlanTrack.Russian, "B1"), 1e-9);
            Assert.AreEqual(462.5, LearningPlanner.CumulativeHours(PlanTrack.Japanese, "N5"));
            Assert.AreEqual(3900, LearningPlanner.CumulativeHours(PlanTrack.Japanese, "N1"));
            Assert.AreEqual(375 + 2.5 * 200, LearningPlanner.CumulativeHours(PlanTrack.Ielts, "6.5"), 1e-9);
            Assert.AreEqual(187.5, LearningPlanner.CumulativeHours(PlanTrack.Ielts, "2.0"), 1e-9);
            foreach (PlanTrack t in Enum.GetValues(typeof(PlanTrack)))
            {
                var lv = LearningPlanner.Levels(t);
                for (int i = 1; i < lv.Length; i++)
                    Assert.Greater(LearningPlanner.CumulativeHours(t, lv[i]), LearningPlanner.CumulativeHours(t, lv[i - 1]), t + " " + lv[i]);
            }
            Assert.Throws<ArgumentException>(() => LearningPlanner.CumulativeHours(PlanTrack.English, "N3"));
        }

        [Test]
        public void WeeksFor_RoundsUp()
        {
            Assert.AreEqual(7, LearningPlanner.WeeksFor(24.5, 30));      // 49 days of 30 min
            Assert.AreEqual(8, LearningPlanner.WeeksFor(24.6, 30));
            Assert.AreEqual(0, LearningPlanner.WeeksFor(0, 30));
            Assert.AreEqual(1, LearningPlanner.WeeksFor(0.1, 120));
            Assert.Throws<ArgumentOutOfRangeException>(() => LearningPlanner.WeeksFor(10, 0));
        }

        [TestCase(PlanTrack.Russian, "0", "B1", 30)]
        [TestCase(PlanTrack.Russian, "0", "C1", 60)]
        [TestCase(PlanTrack.Japanese, "0", "N3", 60)]
        [TestCase(PlanTrack.Japanese, "N5", "N1", 120)]
        [TestCase(PlanTrack.English, "0", "C2", 15)]
        [TestCase(PlanTrack.Ielts, "0", "8.0", 120)]
        [TestCase(PlanTrack.Ielts, "5.5", "6.5", 60)]
        public void Build_MakesContiguousPhasesThatSumToTheEstimate(PlanTrack t, string from, string goal, int minutes)
        {
            var plan = LearningPlanner.Build(t, from, goal, minutes);
            var lv = LearningPlanner.Levels(t);
            Assert.AreEqual(Array.IndexOf(lv, goal) - Array.IndexOf(lv, from), plan.phases.Count);
            Assert.AreEqual(LearningPlanner.CumulativeHours(t, goal) - LearningPlanner.CumulativeHours(t, from), plan.totalHours, 1e-6);
            Assert.AreEqual(plan.totalHours, plan.phases.Sum(p => p.hours), 1e-6);
            Assert.AreEqual(LearningPlanner.WeeksFor(plan.totalHours, minutes), plan.totalWeeks);
            Assert.AreEqual(1, plan.phases[0].startWeek);
            for (int i = 0; i < plan.phases.Count; i++)
            {
                var p = plan.phases[i];
                Assert.GreaterOrEqual(p.endWeek, p.startWeek);
                if (i > 0) Assert.AreEqual(plan.phases[i - 1].endWeek + 1, p.startWeek, "weeks are contiguous");
                Assert.GreaterOrEqual(p.items.Count, 2, p.levelId);
                Assert.IsTrue(p.items.All(x => x.arg == p.levelId));
                foreach (GameLanguage lang in Enum.GetValues(typeof(GameLanguage)))
                {
                    Loc.Set(lang);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(p.Milestone), p.levelId + " " + lang);
                    Assert.IsTrue(p.items.All(x => !string.IsNullOrWhiteSpace(x.Text) && !x.Text.Contains("{0}")), p.levelId + " " + lang);
                }
            }
            Assert.AreEqual(goal, plan.phases.Last().levelId);
            Loc.Set(GameLanguage.Vietnamese);
        }

        [Test]
        public void ZeroRussianToB1_At30MinADay_IsAboutTwoAndAHalfYears()
        {
            var plan = LearningPlanner.Build(PlanTrack.Russian, "0", "B1", 30);
            Assert.AreEqual(487.5, plan.totalHours, 1e-6);
            Assert.AreEqual(140, plan.totalWeeks); // 975 days → 139.3 weeks
            Assert.IsTrue(plan.phases[0].items.Any(i => i.en.Contains("Cyrillic")), "starts from the alphabet");
            Assert.IsTrue(plan.phases.Last().items.Any(i => i.target == PlanTarget.RussianPath && i.en.Contains("TORFL")));
            Assert.Less(LearningPlanner.Build(PlanTrack.Russian, "0", "B1", 120).totalWeeks, plan.totalWeeks / 3);
        }

        [Test]
        public void GoalNotAboveLevel_GivesAnEmptyPlan()
        {
            var plan = LearningPlanner.Build(PlanTrack.English, "B1", "A2", 30);
            Assert.IsEmpty(plan.phases);
            Assert.AreEqual(0, plan.totalWeeks);
        }

        [Test]
        public void RemainingDays_UsesPlanThenRealPace()
        {
            var plan = LearningPlanner.Build(PlanTrack.English, "0", "A1", 30); // 95 h, 0.5 h/day planned
            Assert.AreEqual(190, LearningPlanner.RemainingDays(plan, 0, 0));
            Assert.AreEqual(186, LearningPlanner.RemainingDays(plan, 2, 3));   // < 7 days: planned pace
            Assert.AreEqual(85, LearningPlanner.RemainingDays(plan, 10, 10));  // 1 h/day real pace → 85 days left
            Assert.AreEqual(0, LearningPlanner.RemainingDays(plan, 95, 30));
            Assert.AreEqual(1700, LearningPlanner.RemainingDays(plan, 10, 2000) , 1, "stalled: floor pace keeps a finite ETA");
            Assert.AreEqual(5, LearningPlanner.ExpectedHours(plan, 10), 1e-9);
            Assert.AreEqual(95, LearningPlanner.ExpectedHours(plan, 10000), 1e-9);
        }

        private string _file;
        private GameObject _canvas;

        [SetUp] public void SetUp()
        {
            _file = Path.Combine(Path.GetTempPath(), "nl_plan_" + Guid.NewGuid().ToString("N") + ".json");
            LearningPlanStore.PathOverride = _file;
            _canvas = new GameObject("PlanCanvas", typeof(Canvas), typeof(CanvasScaler));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        }

        [TearDown] public void TearDown()
        {
            LearningPlanStore.PathOverride = null;
            UnityEngine.Object.DestroyImmediate(_canvas);
            foreach (var f in new[] { _file, _file + ".bak", _file + ".tmp" }) if (File.Exists(f)) File.Delete(f);
            Loc.Set(GameLanguage.Vietnamese);
        }

        [TestCase(GameLanguage.Vietnamese)]
        [TestCase(GameLanguage.English)]
        [TestCase(GameLanguage.Japanese)]
        [TestCase(GameLanguage.Russian)]
        public void Popup_BuildsTracksAndSavesProgress(GameLanguage lang)
        {
            Loc.Set(lang);
            var now = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            var p = _canvas.AddComponent<LearningPlanPopup>();
            p.UtcNow = () => now;
            p.Initialize(null); p.Show();
            Assert.IsTrue(p.IsOpen);
            p.SetTrack(PlanTrack.Russian); p.SetFrom("0"); p.SetGoal("B1"); p.SetMinutes(60);
            var plan = p.Generate();
            Assert.IsNotNull(plan);
            Assert.AreEqual(3, plan.phases.Count);
            Assert.IsNotNull(_canvas.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Phase_B1"), "timeline drawn");
            int before = p.RemainingDays();
            now = now.AddDays(10);
            p.LogMinutes(60 * 20); // 20 h in 10 days = 2 h/day, faster than the 1 h/day plan
            Assert.Less(p.RemainingDays(), before - 10, "real pace shortens the ETA");
            var saved = LearningPlanStore.Load();
            Assert.AreEqual(1200, saved.minutesLogged);
            Assert.AreEqual(PlanTrack.Russian, saved.track);

            p.SetGoal("0");
            Assert.IsNull(p.Generate(), "goal below level is refused");
            p.ResetPlan();
            Assert.IsFalse(LearningPlanStore.Load().HasPlan);
        }

        [Test]
        public void StudyHub_OpensTheLearningPlan()
        {
            var hub = _canvas.AddComponent<StudyHubPopup>();
            hub.Initialize(null); hub.Show();
            hub.OpenLearningPlan();
            Assert.IsTrue(_canvas.GetComponentsInChildren<LearningPlanPopup>(true).Any(x => x.IsOpen));
        }

            // ---- milestone checkpoints, certificates, badges ----

        [TestCase(PlanTrack.Japanese, "N5", "A1")]
        [TestCase(PlanTrack.Japanese, "N1", "C1")]
        [TestCase(PlanTrack.English, "C2", "C1")]
        [TestCase(PlanTrack.Russian, "B1", "B1")]
        [TestCase(PlanTrack.Ielts, "4.5", "A2")]
        [TestCase(PlanTrack.Ielts, "6.5", "B2")]
        public void Checkpoint_MapsLevelsToSchoolContent(PlanTrack t, string level, string school)
        {
            Assert.AreEqual(school, MilestoneCheckpoint.SchoolLevel(t, level));
        }

        [TestCase(PlanTrack.Japanese, "N5")]
        [TestCase(PlanTrack.Japanese, "N3")]
        [TestCase(PlanTrack.English, "A1")]
        [TestCase(PlanTrack.English, "B2")]
        [TestCase(PlanTrack.Russian, "A1")]
        [TestCase(PlanTrack.Russian, "B2")]
        [TestCase(PlanTrack.Ielts, "6.5")]
        public void Checkpoint_BuildsAMixedSetFromExistingContent(PlanTrack t, string level)
        {
            var qs = MilestoneCheckpoint.BuildQuestions(t, level);
            Assert.That(qs.Count, Is.InRange(6, MilestoneCheckpoint.QuestionCount), t + " " + level);
            Assert.AreEqual(qs.Count, qs.Select(q => q.Id).Distinct().Count(), "no duplicates");
            Assert.GreaterOrEqual(qs.Select(MilestoneCheckpoint.SkillOf).Distinct().Count(), 2, "mixed skills");
            var same = MilestoneCheckpoint.BuildQuestions(t, level);
            CollectionAssert.AreEqual(qs.Select(q => q.Id), same.Select(q => q.Id), "deterministic for a seed");
        }

        [Test]
        public void Checkpoint_BreakdownRecordAndUnlock()
        {
            var qs = MilestoneCheckpoint.BuildQuestions(PlanTrack.English, "A1");
            var byId = qs.ToDictionary(q => q.Id, q => true);
            byId[qs[0].Id] = false;
            var br = MilestoneCheckpoint.Breakdown(qs, byId);
            Assert.AreEqual(qs.Count, br.Sum(b => b.total));
            Assert.AreEqual(qs.Count - 1, br.Sum(b => b.correct));

            var s = new LearningPlanState { track = PlanTrack.English, fromLevel = "0", goalLevel = "A2", minutesPerDay = 30 };
            var plan = s.Build();
            var now = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.IsTrue(MilestoneCheckpoint.IsUnlocked(plan, s, 0));
            Assert.IsFalse(MilestoneCheckpoint.IsUnlocked(plan, s, 1));
            Assert.IsNull(MilestoneCheckpoint.Record(s, PlanTrack.English, "A1", 6, 10, br, "Test", now), "60% < 70% fails");
            Assert.IsFalse(MilestoneCheckpoint.IsUnlocked(plan, s, 1));
            var rec = MilestoneCheckpoint.Record(s, PlanTrack.English, "A1", 7, 10, br, "Test", now);
            Assert.IsNotNull(rec, "70% passes");
            Assert.IsTrue(MilestoneCheckpoint.IsUnlocked(plan, s, 1));
            Assert.AreSame(rec, MilestoneCheckpoint.Record(s, PlanTrack.English, "A1", 7, 10, br, "Test", now.AddDays(1)), "a worse/equal retake keeps the best");
            MilestoneCheckpoint.Record(s, PlanTrack.English, "A1", 10, 10, br, "Test", now.AddDays(2));
            Assert.AreEqual(1, s.milestones.Count);
            Assert.AreEqual(100, s.milestones[0].Percent);
            StringAssert.StartsWith("NL-EN-A1-", s.milestones[0].Serial);
        }

        [Test]
        public void AnimatedBar_IsExactOutsidePlayMode()
        {
            var fill = UiKit.ProgressBar(_canvas.transform, "Bar", Color.green, Color.gray);
            var a = AnimatedBar.Attach(fill, 0.42f);
            Assert.AreEqual(0.42f, a.current, 1e-5f);
            Assert.AreEqual(0.42f, fill.anchorMax.x, 1e-5f);
        }

        private static void Play(CourseQuizView quiz, bool right)
        {
            int guard = 0;
            while (!quiz.Finished && guard++ < 200) { quiz.AnswerForTest(right); quiz.Next(); }
            Assert.IsTrue(quiz.Finished);
        }

        [TestCase(GameLanguage.Vietnamese)]
        [TestCase(GameLanguage.English)]
        [TestCase(GameLanguage.Japanese)]
        [TestCase(GameLanguage.Russian)]
        public void Popup_CheckpointUnlocksNextStageAndAwardsCertificate(GameLanguage lang)
        {
            Loc.Set(lang);
            var now = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            var p = _canvas.AddComponent<LearningPlanPopup>();
            p.UtcNow = () => now;
            p.PlayerName = () => "Playtest Chi";
            p.Initialize(null); p.Show();
            p.SetTrack(PlanTrack.Russian); p.SetFrom("0"); p.SetGoal("B1"); p.SetMinutes(30);
            p.Generate();
            Assert.IsNull(p.StartCheckpoint(1), "stage 2 starts locked");

            var quiz = p.StartCheckpoint(0);
            Assert.IsNotNull(quiz);
            Assert.AreEqual("quiz", p.OverlayMode);
            Assert.Greater(quiz.TimeLeft, 0f, "timed");
            Play(quiz, false);
            p.ShowResult();
            Assert.AreEqual("result", p.OverlayMode);
            Assert.IsNull(p.LastRecord);
            Assert.IsNull(p.StartCheckpoint(1), "a failed checkpoint keeps stage 2 locked");

            Play(p.StartCheckpoint(0), true);
            p.ShowResult();
            Assert.IsNotNull(p.LastRecord);
            Assert.AreEqual("Playtest Chi", p.LastRecord.playerName);
            Assert.AreEqual(100, p.LastRecord.Percent);
            Assert.IsNotEmpty(p.LastRecord.skills);
            Assert.IsTrue(_canvas.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("SkillBar_")), "score breakdown drawn");
            Assert.IsNotNull(p.StartCheckpoint(1), "stage 2 unlocked");
            p.CloseOverlay();

            p.ShowCertificate(p.LastRecord);
            Assert.AreEqual("certificate", p.OverlayMode);
            var texts = _canvas.GetComponentsInChildren<TMPro.TMP_Text>(true).Select(t => t.text).ToList();
            Assert.IsTrue(texts.Any(t => t.Contains("Playtest Chi")));
            Assert.IsTrue(texts.Any(t => t.Contains("JLPT / TORFL / IELTS")), "not-official disclaimer");
            Assert.IsTrue(texts.Any(t => t.Contains("NIHONGOLIFE")));
            Assert.IsNotNull(CertificateView.Logo, "official logo from Bao_Cao/Logo is in Resources/Branding");
            Assert.IsTrue(_canvas.GetComponentsInChildren<RawImage>(true).Any(r => r.name == "Logo" && r.texture == CertificateView.Logo));

            p.ShowGallery();
            Assert.AreEqual("gallery", p.OverlayMode);
            Assert.IsNotNull(_canvas.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Badge_Russian_A1"));
            Assert.IsNotNull(_canvas.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Badge_Russian_B1"), "locked milestones are listed too");
            Assert.AreEqual(1, LearningPlanStore.Load().milestones.Count, "badge saved");
            p.CloseOverlay();
            Assert.AreEqual("", p.OverlayMode);
        }

        [Test]
        public void Certificate_ExportsPng()
        {
            var rec = new MilestoneRecord { track = PlanTrack.Japanese, levelId = "N5", playerName = "Playtest An", correct = 15, total = 16, utcTicks = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc).Ticks };
            rec.skills.Add(new SkillScore { skill = "vocab", correct = 8, total = 8 });
            rec.skills.Add(new SkillScore { skill = "grammar", correct = 7, total = 8 });
            string dir = Path.Combine(Path.GetTempPath(), "nl_cert_" + Guid.NewGuid().ToString("N"));
            try
            {
                string path = CertificateView.ExportPng(rec, dir);
                if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) { Assert.IsNull(path); return; }
                Assert.IsNotNull(path);
                var bytes = File.ReadAllBytes(path);
                Assert.Greater(bytes.Length, 20000);
                Assert.AreEqual(0x89, bytes[0]);
                var tex = new Texture2D(2, 2);
                Assert.IsTrue(tex.LoadImage(bytes));
                Assert.AreEqual(1600, tex.width);
                UnityEngine.Object.DestroyImmediate(tex);
                string keep = Environment.GetEnvironmentVariable("NL_CERT_SAMPLE");
                if (!string.IsNullOrEmpty(keep)) File.Copy(path, keep, true);
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }
    }
}
