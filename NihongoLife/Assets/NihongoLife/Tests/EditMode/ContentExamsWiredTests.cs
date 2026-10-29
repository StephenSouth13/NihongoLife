using System.Linq;
using NihongoLife.Exam;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests.EditMode
{
    /// <summary>The 11 content-pack exams (JLPT N5-N1 mini, English A1-C1, 12 tenses) are in Resources/Exams and reach ExamRepository.</summary>
    public sealed class ContentExamsWiredTests
    {
        private static readonly string[] Ids = { "jlpt_n5_mini_1", "jlpt_n4_mini_1", "jlpt_n3_mini_1", "jlpt_n2_mini_1", "jlpt_n1_mini_1",
            "en_a1_practice_1", "en_a2_practice_1", "en_b1_practice_1", "en_b2_practice_1", "en_c1_practice_1", "en_tenses_12_mixed_1" };

        [Test]
        public void EveryContentExam_IsLoadable_WithQuestions()
        {
            var all = Resources.LoadAll<ExamDefinition>("Exams");
            foreach (var id in Ids)
            {
                var exam = all.FirstOrDefault(e => e != null && e.id == id);
                Assert.NotNull(exam, id + " is in Resources/Exams");
                int questions = exam.sections.Sum(s => (s.questions != null ? s.questions.Count : 0));
                Assert.Greater(questions, 15, id + " has questions");
            }
            Assert.AreEqual(all.Length, all.Select(e => e.id).Distinct().Count(), "no duplicate exam ids");
        }
    }
}
