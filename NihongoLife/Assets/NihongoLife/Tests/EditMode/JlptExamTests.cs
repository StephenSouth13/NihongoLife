using System.Collections.Generic;
using System.Linq;
using NihongoLife.Exam;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests
{
    /// <summary>The full N5 mock follows the official layout, and the per-もんだい analysis gives exact, correct advice.</summary>
    public class JlptExamTests
    {
        [Test]
        public void N5Mock2_FollowsOfficialLayout()
        {
            var exam = Resources.Load<ExamDefinition>("Exams/jlpt_n5_mock_2");
            Assert.NotNull(exam, "jlpt_n5_mock_2 in Resources/Exams");
            var expected = new Dictionary<string, int[]>
            {
                { "n5m2_vocab", new[] { 7, 5, 6, 3 } },
                { "n5m2_grammar", new[] { 9, 4, 4, 2, 2, 1 } },
                { "n5m2_listening", new[] { 7, 6, 5, 6 } },
            };
            Assert.AreEqual(3, exam.sections.Count);
            foreach (var section in exam.sections)
            {
                var counts = expected[section.id];
                for (int m = 0; m < counts.Length; m++)
                    Assert.AreEqual(counts[m], section.questions.Count(q => q.mondai == "m" + (m + 1)), $"{section.id} もんだい{m + 1}");
                foreach (var q in section.questions)
                {
                    Assert.IsTrue(section.mondai.Any(x => x.id == q.mondai), $"{q.id}: its もんだい is described");
                    Assert.That(q.correctChoiceIndex, Is.InRange(0, q.choices.Count - 1), q.id);
                    if (!string.IsNullOrEmpty(q.passageId)) Assert.NotNull(section.FindPassage(q.passageId), q.id + " passage");
                }
            }
            var listening = exam.sections.First(s => s.type == ExamSectionType.JlptListening);
            foreach (var p in listening.passages)
            {
                Assert.NotNull(p.audioClip, p.id + " has its audio");
                Assert.AreEqual(1, p.maxPlays, p.id + " plays once, as in the real test");
                Assert.IsFalse(string.IsNullOrWhiteSpace(p.bodyJa), p.id + " has a script for the review");
            }
            Assert.AreEqual(2, exam.scoreGroups.Count);
            Assert.AreEqual(180, exam.scoreGroups.Sum(g => g.max));
            Assert.AreEqual(80, exam.jlptTotalPassScore);
            var positions = exam.sections.SelectMany(s => s.questions).Where(q => q.choices.Count == 4).GroupBy(q => q.correctChoiceIndex).ToDictionary(g => g.Key, g => g.Count());
            Assert.IsTrue(positions.Values.All(n => n >= 5), "Right answers are spread over all four positions");
        }

        [Test]
        public void Analysis_NamesWeakestMondaiAndExactGain()
        {
            var exam = ScriptableObject.CreateInstance<ExamDefinition>();
            exam.examType = ExamType.Jlpt;
            exam.jlptTotalPassScore = 80;
            exam.scoreGroups = new List<JlptScoreGroup>
            {
                new JlptScoreGroup { id = "gengo", titleJa = "言語知識・読解", max = 120, passMin = 38 },
                new JlptScoreGroup { id = "choukai", titleJa = "聴解", max = 60, passMin = 19 },
            };
            var vocab = new ExamSection { id = "v", scoreGroup = "gengo", mondai = { new ExamMondai { id = "m1", titleJa = "もんだい1" }, new ExamMondai { id = "m5", titleJa = "もんだい5", titleVi = "cách dùng từ" } } };
            var listen = new ExamSection { id = "l", scoreGroup = "choukai", mondai = { new ExamMondai { id = "m1", titleJa = "もんだい1" } } };
            var record = new ExamAttemptRecord();
            for (int i = 0; i < 9; i++) Add(vocab, record, "a" + i, "m1", i < 6);   // 6/9
            for (int i = 0; i < 3; i++) Add(vocab, record, "b" + i, "m5", i < 1);   // 1/3 — weakest
            for (int i = 0; i < 6; i++) Add(listen, record, "c" + i, "m1", i < 2);  // 2/6
            exam.sections = new List<ExamSection> { vocab, listen };
            // gengo 7/12 → 70/120, choukai 2/6 → 20/60, total 90 (as ExamManager scales)
            record.groups.Add(new ExamGroupResult { groupId = "gengo", scored = 70, max = 120, passMin = 38, rawCorrect = 7, rawMax = 12 });
            record.groups.Add(new ExamGroupResult { groupId = "choukai", scored = 20, max = 60, passMin = 19, rawCorrect = 2, rawMax = 6 });
            record.totalScore = 90; record.totalMaxScore = 180;

            var report = ExamAnalysis.Analyze(exam, record);
            Assert.AreEqual(3, report.mondai.Count);
            StringAssert.Contains("khá an toàn", report.status, "90 vs pass 80: 10 points of margin");
            var gengo = report.advice.First(a => a.Contains("言語知識・読解") && a.Contains("Làm đúng thêm"));
            StringAssert.Contains("もんだい5", gengo, "The weakest もんだい of the division is named");
            StringAssert.Contains("đúng 1/3", gengo);
            StringAssert.Contains("thêm 2 câu", gengo);
            StringAssert.Contains("từ 70 lên 90/120", gengo, "9/12 of 120 = 90");
            StringAssert.Contains("tổng từ 90 lên 110/180", gengo);
        }

        [Test]
        public void Analysis_FlagsDivisionBelowMinimum()
        {
            var exam = ScriptableObject.CreateInstance<ExamDefinition>();
            exam.examType = ExamType.Jlpt;
            exam.jlptTotalPassScore = 80;
            exam.scoreGroups = new List<JlptScoreGroup> { new JlptScoreGroup { id = "gengo", titleJa = "言語知識", max = 120, passMin = 38 }, new JlptScoreGroup { id = "choukai", titleJa = "聴解", max = 60, passMin = 19 } };
            var record = new ExamAttemptRecord { totalScore = 115, totalMaxScore = 180 };
            record.groups.Add(new ExamGroupResult { groupId = "gengo", scored = 100, max = 120, passMin = 38, rawCorrect = 10, rawMax = 12 });
            record.groups.Add(new ExamGroupResult { groupId = "choukai", scored = 15, max = 60, passMin = 19, rawCorrect = 1, rawMax = 4 });
            exam.sections = new List<ExamSection>();
            var report = ExamAnalysis.Analyze(exam, record);
            Assert.IsTrue(report.advice.Any(a => a.Contains("聴解") && a.Contains("điểm liệt")), "A division under its minimum is called out");
            StringAssert.Contains("điểm liệt", report.status);
            Assert.AreEqual("an toàn", ExamAnalysis.Status(110, 80, new List<ExamGroupResult>()));
            Assert.AreEqual("khá an toàn", ExamAnalysis.Status(95, 80, new List<ExamGroupResult>()));
            StringAssert.StartsWith("chưa đạt", ExamAnalysis.Status(79, 80, new List<ExamGroupResult>()));
        }

        private static void Add(ExamSection section, ExamAttemptRecord record, string id, string mondai, bool correct)
        {
            section.questions.Add(new ExamQuestion { id = id, mondai = mondai, points = 1 });
            record.answers.Add(new ExamQuestionRecord { questionId = id, correct = correct });
        }
    }
}
