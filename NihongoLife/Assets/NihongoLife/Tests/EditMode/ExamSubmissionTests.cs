using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Data;
using NihongoLife.Exam;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests
{
    /// <summary>Writing/Speaking are recorded (pending review) with AI grading off; optional paid AI grading.</summary>
    public class ExamSubmissionTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "nl_exam_submissions_" + Guid.NewGuid().ToString("N"));
            ExamSubmissionStore.RootOverride = _root;
            ExamAiSettings.AutoGradingOverride = null;
        }

        [TearDown]
        public void TearDown()
        {
            ExamSubmissionStore.RootOverride = null;
            ExamAiSettings.AutoGradingOverride = null;
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        private sealed class CountingGrader : ISubmissionGrader
        {
            public int Calls;
            public void Grade(ExamSubmissionManifest submission, Action<SubmissionGradeResult> done) { Calls++; done?.Invoke(new SubmissionGradeResult { ok = true }); }
        }

        private sealed class FakeWallet : IAiGradeWallet
        {
            public int Balance { get; set; }
            public bool TrySpend(int amount) { if (Balance < amount) return false; Balance -= amount; return true; }
            public void Refund(int amount) => Balance += amount;
        }

        [Test]
        public void AiGrading_IsOffByDefault_InControlDatabase()
        {
            var db = ScriptableObject.CreateInstance<GameControlDatabase>();
            Assert.IsFalse(db.aiGradingEnabled);
            Assert.AreEqual(3, db.aiFreeGradings);
            Assert.AreEqual(50, db.aiGradingPriceYen);
            var asset = Resources.Load<GameControlDatabase>("Control/NihongoLifeControlDatabase");
            if (asset != null) Assert.IsFalse(asset.aiGradingEnabled, "the shipped Control Database keeps AI grading off");
            UnityEngine.Object.DestroyImmediate(db);
        }

        [Test]
        public void Submission_SavesEssayWavAndPendingManifest()
        {
            var m = ExamSubmissionStore.Begin("ielts/mock 1", "Ielts", new DateTime(2026, 10, 25, 16, 0, 0, DateTimeKind.Utc));
            ExamSubmissionStore.AddWriting(m, "w1", "Describe the chart.", "The chart shows three clear trends overall.");
            var samples = new float[16000];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(i * 0.05f) * 0.3f;
            ExamSubmissionStore.AddSpeaking(m, "s1", "Talk about your hometown.", samples, 16000);
            string manifestPath = ExamSubmissionStore.Save(m);

            StringAssert.StartsWith(Path.Combine(_root, "ielts_mock_1"), m.folder);
            Assert.IsTrue(File.Exists(manifestPath));
            var loaded = ExamSubmissionStore.Load(m.folder);
            Assert.AreEqual(SubmissionStatus.Pending, loaded.status);
            Assert.AreEqual(2, loaded.items.Count);
            var w = loaded.items[0];
            Assert.AreEqual("writing", w.kind); Assert.AreEqual(7, w.wordCount); Assert.AreEqual("Describe the chart.", w.question);
            Assert.AreEqual("The chart shows three clear trends overall.", File.ReadAllText(Path.Combine(m.folder, w.answerPath)));
            var s = loaded.items[1];
            Assert.AreEqual("speaking", s.kind); Assert.AreEqual(1f, s.durationSeconds, 0.01f); Assert.AreEqual(SubmissionStatus.Pending, s.status);
            byte[] wav = File.ReadAllBytes(Path.Combine(m.folder, s.answerPath));
            Assert.AreEqual("RIFF", System.Text.Encoding.ASCII.GetString(wav, 0, 4));
        }

        [Test]
        public void FlagOff_NoGraderOrNetworkCall()
        {
            var grader = new CountingGrader();
            var m = ExamSubmissionStore.Begin("jlpt", "Jlpt", DateTime.UtcNow);
            Assert.IsFalse(ExamAiSettings.AiGradingEnabled, "no Control Database → default off");
            Assert.IsFalse(SubmissionGrading.TryAutoGrade(grader, m, null));
            Assert.AreEqual(0, grader.Calls);

            ExamAiSettings.AutoGradingOverride = true; // the plug point still works once enabled
            Assert.IsTrue(SubmissionGrading.TryAutoGrade(grader, m, null));
            Assert.AreEqual(1, grader.Calls);
        }

        [Test]
        public void Results_PendingSectionsShowPending_AndBandIsObjectiveOnly()
        {
            var sections = new List<ExamSectionResult>
            {
                new ExamSectionResult { sectionId = "reading", scoredPoints = 30, maxPoints = 40 },   // 6.75
                new ExamSectionResult { sectionId = "listening", scoredPoints = 36, maxPoints = 40 }, // 8.1
                new ExamSectionResult { sectionId = "writing", pendingReview = true },
                new ExamSectionResult { sectionId = "speaking", pendingReview = true },
            };
            Assert.AreEqual(7.5f, ExamPendingRules.ObjectiveBand(sections));
            Assert.IsTrue(ExamPendingRules.AnyPending(sections));
            var before = Loc.Language;
            try
            {
            foreach (GameLanguage lang in new[] { GameLanguage.Vietnamese, GameLanguage.English, GameLanguage.Japanese })
            {
                Loc.Set(lang);
                string expected = lang == GameLanguage.Vietnamese ? "Chờ chấm" : lang == GameLanguage.English ? "Pending review" : "採点待ち";
                Assert.AreEqual(expected, ExamPendingRules.SectionScoreLabel(sections[2]));
                StringAssert.StartsWith("Band ", ExamPendingRules.SectionScoreLabel(sections[0]));
            }
            }
            finally { Loc.Set(before); }
            Assert.AreEqual(0f, ExamPendingRules.ObjectiveBand(sections.Where(s => s.pendingReview).ToList()), "no fake score when only W/S");
        }

        [Test]
        public void PendingTexts_AreCompleteInAllLanguages()
        {
            foreach (var t in ExamPendingText.All())
            {
                Assert.AreEqual(3, t.Length);
                foreach (var s in t) Assert.IsFalse(string.IsNullOrWhiteSpace(s));
                Assert.AreNotEqual(t[0], t[1]);
                Assert.IsTrue(Loc.IsJapaneseText(t[2]), "ja text: " + t[2]);
                int holes = t[1].Count(c => c == '{');
                Assert.AreEqual(holes, t[0].Count(c => c == '{')); Assert.AreEqual(holes, t[2].Count(c => c == '{'));
            }
        }

        [Test]
        public void Credits_FirstThreeFree_ThenChargedPrice()
        {
            var p = new PlayerProgressDto();
            var wallet = new FakeWallet { Balance = 120 };
            for (int i = 0; i < 3; i++)
            {
                var q = AiGradeCredits.Quote(p, wallet, 3, 50);
                Assert.IsTrue(q.free); Assert.AreEqual(3 - i, q.freeLeft); Assert.AreEqual(0, q.cost);
                var t = AiGradeCredits.Reserve(p, wallet, 3, 50);
                AiGradeCredits.Commit(t);
            }
            Assert.AreEqual(3, p.aiFreeGradesUsed);
            Assert.AreEqual(120, wallet.Balance, "free uses cost nothing");

            var paid = AiGradeCredits.Quote(p, wallet, 3, 50);
            Assert.IsFalse(paid.free); Assert.AreEqual(50, paid.cost); Assert.AreEqual(120, paid.balance);
            var ticket = AiGradeCredits.Reserve(p, wallet, 3, 50);
            AiGradeCredits.Commit(ticket);
            Assert.AreEqual(70, wallet.Balance);
            AiGradeCredits.Commit(AiGradeCredits.Reserve(p, wallet, 3, 50));
            Assert.AreEqual(20, wallet.Balance);
            Assert.IsFalse(AiGradeCredits.Quote(p, wallet, 3, 50).CanProceed);
            Assert.IsNull(AiGradeCredits.Reserve(p, wallet, 3, 50), "not enough yen");
            Assert.AreEqual(20, wallet.Balance);
        }

        [Test]
        public void Credits_FailedGrade_RefundsFreeUseOrYen()
        {
            var p = new PlayerProgressDto();
            var wallet = new FakeWallet { Balance = 100 };
            var free = AiGradeCredits.Reserve(p, wallet, 3, 50);
            Assert.AreEqual(1, p.aiFreeGradesUsed);
            AiGradeCredits.Refund(free);
            AiGradeCredits.Refund(free); // idempotent
            Assert.AreEqual(0, p.aiFreeGradesUsed);

            p.aiFreeGradesUsed = 3;
            var paid = AiGradeCredits.Reserve(p, wallet, 3, 50);
            Assert.AreEqual(50, wallet.Balance);
            AiGradeCredits.Refund(paid);
            Assert.AreEqual(100, wallet.Balance);
            Assert.AreEqual(3, p.aiFreeGradesUsed);
        }

        [Test]
        public void FreeUses_PersistInSave_AndMergeKeepsHighest()
        {
            var p = new PlayerProgressDto { aiFreeGradesUsed = 2 };
            var back = JsonUtility.FromJson<PlayerProgressDto>(JsonUtility.ToJson(p));
            Assert.AreEqual(2, back.aiFreeGradesUsed);
            var merged = NihongoLife.Save.SupabaseProgressRepository.MergeProgress(new PlayerProgressDto { aiFreeGradesUsed = 3 }, new PlayerProgressDto { aiFreeGradesUsed = 1 });
            Assert.AreEqual(3, merged.aiFreeGradesUsed);
        }
    }
}
